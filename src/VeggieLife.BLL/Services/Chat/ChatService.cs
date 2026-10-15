using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Ai;
using VeggieLife.BLL.DTOs.Chat.Requests;
using VeggieLife.BLL.DTOs.Chat.Responses;
using VeggieLife.BLL.DTOs.Surveys.Requests;
using VeggieLife.BLL.Services.AI;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.Chat;

/// <summary>
/// Triển khai nghiệp vụ Chatbot tư vấn dinh dưỡng thuần chay (FN-CHAT-01..06).
/// Quản lý hạn mức nghiêm ngặt, gọi AI service kèm RAG và nguồn dẫn chứng, xử lý hoàn lượt khi lỗi (S5-N1, S6-T8).
/// </summary>
public class ChatService : IChatService
{
    private readonly AppDbContext _dbContext;
    private readonly IRedisService _redisService;
    private readonly IAiServiceClient _aiServiceClient;
    private readonly ILogger<ChatService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public ChatService(
        AppDbContext dbContext,
        IRedisService redisService,
        IAiServiceClient aiServiceClient,
        ILogger<ChatService> logger)
    {
        _dbContext = dbContext;
        _redisService = redisService;
        _aiServiceClient = aiServiceClient;
        _logger = logger;
    }

    /// <summary>
    /// FN-CHAT-01: Tạo phiên hội thoại mới cho Khách hoặc Thành viên.
    /// </summary>
    public async Task<ChatSessionDto> CreateSessionAsync(
        long? userId,
        string? guestUuid,
        CreateChatSessionRequest request,
        CancellationToken ct = default)
    {
        // 1. Kiểm tra tính hợp lệ của dữ liệu đầu vào
        if (request.Title?.Length > 100)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tiêu đề phiên hội thoại không được vượt quá 100 ký tự.");
        }

        var contextType = string.IsNullOrWhiteSpace(request.ContextType)
            ? ChatConstants.ContextNutritionConsultation
            : request.ContextType.Trim().ToUpperInvariant();

        if (contextType != ChatConstants.ContextNutritionConsultation &&
            contextType != ChatConstants.ContextMealPlanAssistant &&
            contextType != ChatConstants.ContextEmptyFridge)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Loại ngữ cảnh (contextType) không hợp lệ.");
        }

        var sessionTitle = !string.IsNullOrWhiteSpace(request.Title)
            ? request.Title.Trim()
            : $"Hội thoại ngày {DateTime.UtcNow:dd/MM/yyyy HH:mm}";

        ChatSession session;

        // 2. Phân loại tác nhân: Thành viên hoặc Khách vãng lai
        if (userId.HasValue && userId.Value > 0)
        {
            var profile = await _dbContext.HealthProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == (int)userId.Value, ct);

            session = new ChatSession
            {
                UserId = (int)userId.Value,
                GuestUuid = null,
                ProfilesId = profile?.UserId,
                SessionTitle = sessionTitle,
                ContextType = contextType,
                StartedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        else if (!string.IsNullOrWhiteSpace(guestUuid))
        {
            var cleanedGuestUuid = guestUuid.Trim();
            if (!Guid.TryParse(cleanedGuestUuid, out _))
            {
                throw AppException.Unauthorized(ErrorCodes.ErrAuthInvalidToken, "Mã định danh khách (Guest_UUID) không hợp lệ.");
            }

            // Kiểm tra hạn mức dùng thử AI của khách (BR-GUEST-02, BR-GUEST-AI-01)
            var usedTurns = await _redisService.GetAsync<long?>($"guest_quota:ai:{cleanedGuestUuid}", ct) ?? 0;
            if (usedTurns >= ChatConstants.GuestAiLimit)
            {
                throw AppException.Forbidden(
                    ErrorCodes.ErrQuotaAiExceeded,
                    "Khách đã dùng hết 03 lượt AI dùng thử.");
            }

            session = new ChatSession
            {
                UserId = null,
                GuestUuid = cleanedGuestUuid,
                ProfilesId = null,
                SessionTitle = sessionTitle,
                ContextType = contextType,
                StartedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        else
        {
            throw AppException.Unauthorized(ErrorCodes.ErrAuthInvalidToken, "Thiếu thông tin xác thực phiên hội thoại.");
        }

        // 3. Lưu vào cơ sở dữ liệu
        _dbContext.ChatSessions.Add(session);
        await _dbContext.SaveChangesAsync(ct);

        // 4. Lưu ngữ cảnh phiên vào Redis chat_context:{sessionId} TTL 2 giờ
        var cacheKey = $"chat_context:{session.ChatSessionId}";
        var cachePayload = new
        {
            sessionId = session.ChatSessionId,
            title = session.SessionTitle,
            contextType = session.ContextType,
            startedAt = session.StartedAt
        };
        await _redisService.SetAsync(cacheKey, cachePayload, TimeSpan.FromHours(2), ct);

        return new ChatSessionDto
        {
            SessionId = session.ChatSessionId,
            Title = session.SessionTitle,
            ContextType = session.ContextType,
            StartedAt = session.StartedAt
        };
    }

    /// <summary>
    /// FN-CHAT-03: Lấy danh sách phiên hội thoại của Thành viên (mới nhất trước).
    /// </summary>
    public async Task<PagedResult<ChatSessionSummaryDto>> ListSessionsAsync(
        long userId,
        int page,
        int size,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (size < 1) size = 20;
        if (size > 50) size = 50;

        var query = _dbContext.ChatSessions
            .AsNoTracking()
            .Where(s => s.UserId == (int)userId);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(s => s.UpdatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(s => new ChatSessionSummaryDto
            {
                SessionId = s.ChatSessionId,
                Title = s.SessionTitle,
                LastMessagePreview = s.ChatMessages
                    .OrderByDescending(m => m.MessageId)
                    .Select(m => m.Content)
                    .FirstOrDefault() ?? string.Empty,
                UpdatedAt = s.UpdatedAt
            })
            .ToListAsync(ct);

        return new PagedResult<ChatSessionSummaryDto>(items, total, page, size);
    }

    /// <summary>
    /// FN-CHAT-02: Gửi câu hỏi, kiểm tra hạn mức, gọi AI và nhận tư vấn kèm nguồn dẫn chứng.
    /// </summary>
    public async Task<CreateChatMessageResultDto> SendMessageAsync(
        long sessionId,
        long? userId,
        string? guestUuid,
        string userRole,
        CreateChatSessionMessageRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Nội dung câu hỏi không được để trống.");
        }

        if (request.Message.Length > 1000)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Nội dung câu hỏi không được vượt quá 1000 ký tự.");
        }

        // 1. Kiểm tra quyền sở hữu phiên
        var session = await _dbContext.ChatSessions
            .FirstOrDefaultAsync(s => s.ChatSessionId == sessionId, ct);

        if (session == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrChatSessionNotFound, "Phiên hội thoại không tồn tại hoặc không thuộc về bạn.");
        }

        var isGuest = !userId.HasValue || userId.Value <= 0;
        var cleanedGuestUuid = guestUuid?.Trim();

        if (isGuest)
        {
            if (string.IsNullOrEmpty(cleanedGuestUuid) || session.GuestUuid != cleanedGuestUuid || session.UserId != null)
            {
                throw AppException.NotFound(ErrorCodes.ErrChatSessionNotFound, "Phiên hội thoại không tồn tại hoặc không thuộc về bạn.");
            }

            // Khách chỉ đọc và gửi tin trong 24 giờ kể từ khi mở phiên
            if (session.StartedAt < DateTime.UtcNow.AddHours(-24))
            {
                throw AppException.NotFound(ErrorCodes.ErrChatSessionNotFound, "Phiên hội thoại đã hết hạn (chỉ lưu trữ trong 24 giờ đối với khách vãng lai).");
            }
        }
        else
        {
            if (session.UserId != (int)userId!.Value)
            {
                throw AppException.NotFound(ErrorCodes.ErrChatSessionNotFound, "Phiên hội thoại không tồn tại hoặc không thuộc về bạn.");
            }
        }

        // 2. Kiểm tra và trừ hạn mức
        long guestUsed = 0;
        int memberUsedToday = 0;
        int quotaLimit;
        string quotaScope = "DAILY";

        if (isGuest)
        {
            quotaLimit = ChatConstants.GuestAiLimit;
            quotaScope = "DAILY";

            var (allowed, current, _) = await _redisService.TryConsumeQuotaAsync(
                $"guest_quota:ai:{cleanedGuestUuid}",
                ChatConstants.GuestAiLimit,
                TimeSpan.FromHours(24),
                ct);

            if (!allowed)
            {
                throw AppException.Forbidden(
                    ErrorCodes.ErrQuotaAiExceeded,
                    "Khách hết 03 lượt AI hoặc Free hết 20 câu/ngày.");
            }

            guestUsed = current;
        }
        else
        {
            var isVip = string.Equals(userRole, UserRoles.Vip, StringComparison.OrdinalIgnoreCase);
            quotaLimit = isVip ? ChatConstants.VipDailyLimit : ChatConstants.FreeMemberDailyLimit;
            quotaScope = "DAILY";

            var todayStart = DateTime.UtcNow.Date;
            memberUsedToday = await _dbContext.ChatMessages
                .CountAsync(m => m.ChatSession.UserId == (int)userId!.Value &&
                                 m.SenderType == ChatConstants.SenderUser &&
                                 m.CreatedAt >= todayStart, ct);

            if (memberUsedToday >= quotaLimit)
            {
                throw AppException.Forbidden(
                    ErrorCodes.ErrQuotaAiExceeded,
                    "Khách hết 03 lượt AI hoặc Free hết 20 câu/ngày.");
            }
        }

        // 3. Dựng ngữ cảnh gửi sang AI Service (6 tin gần nhất + Hồ sơ thể trạng + Công thức ứng viên)
        var history = await _dbContext.ChatMessages
            .AsNoTracking()
            .Where(m => m.ChatSessionId == sessionId)
            .OrderByDescending(m => m.MessageId)
            .Take(6)
            .OrderBy(m => m.MessageId)
            .Select(m => new AiChatMessageHistoryDto
            {
                Role = m.SenderType == ChatConstants.SenderAssistant ? "model" : "user",
                Text = m.Content
            })
            .ToListAsync(ct);

        AiUserProfileDto? userProfile = null;
        List<int> userAllergenIds = new();
        int? dietarySchoolId = null;
        bool requiresAlliumFree = false;

        if (!isGuest)
        {
            var profile = await _dbContext.HealthProfiles
                .Include(p => p.DietarySchool)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == (int)userId!.Value, ct);

            var userAllergies = await _dbContext.UserAllergies
                .Include(ua => ua.Allergen)
                .AsNoTracking()
                .Where(ua => ua.UserId == (int)userId!.Value)
                .ToListAsync(ct);

            userAllergenIds = userAllergies.Select(ua => ua.AllergenId).ToList();

            if (profile != null)
            {
                dietarySchoolId = profile.DietarySchoolId;
                requiresAlliumFree = profile.RequiresAlliumFree;

                userProfile = new AiUserProfileDto
                {
                    DietarySchoolCode = profile.DietarySchool?.SchoolName ?? "Thuần chay",
                    ForbiddenFoodGroups = profile.RequiresAlliumFree ? new List<string> { "ALLIUM" } : new List<string>(),
                    AllergenGroups = userAllergies.Select(ua => ua.Allergen.AllergenName).ToList(),
                    HeightCm = profile.HeightCm,
                    CurrentWeightKg = profile.CurrentWeightKg,
                    TargetWeightKg = profile.TargetWeightKg,
                    TargetDailyCalories = profile.TargetDailyCalories,
                    HealthGoal = profile.HealthGoal
                };
            }
        }

        // Lọc danh sách công thức ứng viên phù hợp trường phái và an toàn dị ứng (BR-SCI-04, BR-USR-07)
        var recipeQuery = _dbContext.Recipes
            .AsNoTracking()
            .Where(r => r.Status == "APPROVED_ACTIVE");

        if (dietarySchoolId.HasValue)
        {
            recipeQuery = recipeQuery.Where(r => r.DietarySchoolId == dietarySchoolId.Value);
        }

        if (requiresAlliumFree)
        {
            recipeQuery = recipeQuery.Where(r => !r.RecipeIngredients.Any(ri => ri.Ingredient.IsAllium));
        }

        if (userAllergenIds.Count > 0)
        {
            recipeQuery = recipeQuery.Where(r => !r.RecipeIngredients.Any(ri =>
                ri.Ingredient.IngredientAllergens.Any(ia => userAllergenIds.Contains(ia.AllergenId))));
        }

        var candidateRecipes = await recipeQuery
            .OrderByDescending(r => r.ViewCount)
            .Take(10)
            .Select(r => new AiCandidateRecipeDto
            {
                RecipeId = r.RecipeId,
                Title = r.Title,
                CookTimeMinutes = r.CookTimeMinutes,
                CaloriesKcal = (int)Math.Round(r.CaloriesKcal)
            })
            .ToListAsync(ct);

        var aiRequest = new AiChatAnswerRequest
        {
            Question = request.Message,
            History = history,
            Profile = userProfile,
            CandidateRecipes = candidateRecipes,
            UserId = !isGuest ? (int?)userId!.Value : null,
            GuestId = isGuest ? cleanedGuestUuid : null
        };

        // 4. Gọi AI Service POST /chat/answer & xử lý an toàn, hoàn lượt khi lỗi
        AiChatAnswerResponse aiResponse;
        try
        {
            aiResponse = await _aiServiceClient.GetChatAnswerAsync(aiRequest, ct);
        }
        catch (AppException ex) when (ex.ErrorCode == ErrorCodes.ErrChatUnsafeContent)
        {
            // Vi phạm an toàn nội dung: Lưu tin USER, KHÔNG lưu tin ASSISTANT, KHÔNG hoàn lượt (D119)
            var unsafeUserMessage = new ChatMessage
            {
                ChatSessionId = sessionId,
                SenderType = ChatConstants.SenderUser,
                Content = request.Message,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.ChatMessages.Add(unsafeUserMessage);
            session.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(CancellationToken.None);

            throw;
        }
        catch (Exception ex)
        {
            // Các lỗi mạng, timeout, Qdrant chết -> HOÀN LẠI LƯỢT CHO KHÁCH
            _logger.LogWarning(ex, "[ChatService] Lỗi gọi AI Service. Thực hiện hoàn lượt cho khách.");
            if (isGuest && !string.IsNullOrEmpty(cleanedGuestUuid))
            {
                try
                {
                    await _redisService.IncrementAsync($"guest_quota:ai:{cleanedGuestUuid}", -1, ct: CancellationToken.None);
                }
                catch (Exception redisEx)
                {
                    _logger.LogError(redisEx, "[ChatService] Không thể hoàn lượt trên Redis.");
                }
            }

            throw;
        }

        // 5. Chuẩn hóa nguồn tham chiếu và món gợi ý
        var publicSources = aiResponse.Sources.Select(s => new ChatSourceDto
        {
            Type = s.Type,
            Id = s.Id,
            Title = s.Title
        }).ToList();

        var recommendedRecipes = candidateRecipes
            .Where(c => aiResponse.RecommendedRecipeIds.Contains(c.RecipeId))
            .Select(c => new ChatRecommendedRecipeDto
            {
                RecipeId = c.RecipeId,
                Title = c.Title,
                CookTimeMinutes = c.CookTimeMinutes
            })
            .ToList();

        // 6. Lưu tin nhắn USER và ASSISTANT vào CSDL
        var userMessage = new ChatMessage
        {
            ChatSessionId = sessionId,
            SenderType = ChatConstants.SenderUser,
            Content = request.Message,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.ChatMessages.Add(userMessage);

        var metadata = new ChatMessageMetadataDto
        {
            Sources = publicSources,
            RagUsed = aiResponse.RagUsed,
            RecommendedRecipes = recommendedRecipes
        };

        var assistantMessage = new ChatMessage
        {
            ChatSessionId = sessionId,
            SenderType = ChatConstants.SenderAssistant,
            Content = aiResponse.Answer,
            MetadataJson = JsonSerializer.Serialize(metadata, JsonOptions),
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.ChatMessages.Add(assistantMessage);

        session.UpdatedAt = DateTime.UtcNow;

        // 7. Ghi nhận nhật ký AI Quota & Audit Logs (BR-ADM-AI-003)
        var totalTokens = (aiResponse.Usage?.PromptTokens ?? 0) + (aiResponse.Usage?.CompletionTokens ?? 0);

        if (!isGuest)
        {
            var quotaLog = new AiquotaLog
            {
                UserId = (int)userId!.Value,
                FeatureCode = ChatConstants.FeatureCodeAiChat,
                TokensConsumed = totalTokens,
                CostEstimateUsd = 0.0002m,
                RequestStatus = "SUCCESS",
                ExecutionTimeMs = aiResponse.Usage?.LatencyMs ?? 0,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.AiquotaLogs.Add(quotaLog);
        }

        var auditRole = isGuest || string.Equals(userRole, UserRoles.Guest, StringComparison.OrdinalIgnoreCase)
            ? "SYSTEM"
            : userRole;

        var auditLog = new AuditLog
        {
            ActorUserId = !isGuest ? (int)userId!.Value : null,
            ActorRole = auditRole,
            Action = ChatConstants.ActionAiRequest,
            TargetTable = "ChatMessages",
            TargetId = sessionId.ToString(),
            NewValue = JsonSerializer.Serialize(new
            {
                model = aiResponse.Usage?.Model,
                prompt = aiResponse.Audit?.Prompt,
                temperature = aiResponse.Usage?.Temperature,
                rawResponse = aiResponse.Audit?.RawResponse,
                guestUuid = isGuest ? cleanedGuestUuid : null
            }, JsonOptions),
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.AuditLogs.Add(auditLog);

        await _dbContext.SaveChangesAsync(ct);

        // 8. Tính toán thông tin hạn ngạch phản hồi
        int quotaUsed = isGuest ? (int)guestUsed : memberUsedToday + 1;
        int quotaRemaining = Math.Max(0, quotaLimit - quotaUsed);

        return new CreateChatMessageResultDto
        {
            Message = new ChatSessionMessageDto
            {
                MessageId = assistantMessage.MessageId,
                SenderType = ChatConstants.SenderAssistant,
                Content = assistantMessage.Content,
                Disclaimer = ChatConstants.MedicalDisclaimer,
                Sources = publicSources,
                RagUsed = aiResponse.RagUsed,
                RecommendedRecipes = recommendedRecipes,
                CreatedAt = assistantMessage.CreatedAt
            },
            Quota = new ChatQuotaDto
            {
                Used = quotaUsed,
                Limit = quotaLimit,
                Remaining = quotaRemaining,
                Scope = quotaScope
            }
        };
    }

    /// <summary>
    /// FN-CHAT-04: Đọc lịch sử tin nhắn của phiên (kèm nguồn và miễn trừ y tế).
    /// </summary>
    public async Task<ListChatSessionMessagesResponseDto> ListMessagesAsync(
        long sessionId,
        long? userId,
        string? guestUuid,
        long? before,
        int size,
        CancellationToken ct = default)
    {
        if (size < 1) size = 20;
        if (size > 100) size = 100;

        var session = await _dbContext.ChatSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ChatSessionId == sessionId, ct);

        if (session == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrChatSessionNotFound, "Phiên hội thoại không tồn tại hoặc không thuộc về bạn.");
        }

        var isGuest = !userId.HasValue || userId.Value <= 0;
        var cleanedGuestUuid = guestUuid?.Trim();

        if (isGuest)
        {
            if (string.IsNullOrEmpty(cleanedGuestUuid) || session.GuestUuid != cleanedGuestUuid || session.UserId != null)
            {
                throw AppException.NotFound(ErrorCodes.ErrChatSessionNotFound, "Phiên hội thoại không tồn tại hoặc không thuộc về bạn.");
            }

            if (session.StartedAt < DateTime.UtcNow.AddHours(-24))
            {
                throw AppException.NotFound(ErrorCodes.ErrChatSessionNotFound, "Phiên hội thoại đã hết hạn (chỉ lưu trữ trong 24 giờ đối với khách vãng lai).");
            }
        }
        else
        {
            if (session.UserId != (int)userId!.Value)
            {
                throw AppException.NotFound(ErrorCodes.ErrChatSessionNotFound, "Phiên hội thoại không tồn tại hoặc không thuộc về bạn.");
            }
        }

        var query = _dbContext.ChatMessages
            .AsNoTracking()
            .Where(m => m.ChatSessionId == sessionId);

        if (before.HasValue && before.Value > 0)
        {
            query = query.Where(m => m.MessageId < before.Value);
        }

        // Lấy thêm 1 bản ghi để kiểm tra hasMore
        var rawMessages = await query
            .OrderByDescending(m => m.MessageId)
            .Take(size + 1)
            .ToListAsync(ct);

        var hasMore = rawMessages.Count > size;
        var pagedList = rawMessages.Take(size).OrderBy(m => m.MessageId).ToList();

        var messageDtos = pagedList.Select(m =>
        {
            var isAssistant = string.Equals(m.SenderType, ChatConstants.SenderAssistant, StringComparison.OrdinalIgnoreCase);
            ChatMessageMetadataDto? meta = null;

            if (isAssistant && !string.IsNullOrWhiteSpace(m.MetadataJson))
            {
                try
                {
                    meta = JsonSerializer.Deserialize<ChatMessageMetadataDto>(m.MetadataJson, JsonOptions);
                }
                catch
                {
                    // Trường hợp JSON không hợp lệ, giữ danh sách rỗng
                }
            }

            return new ChatSessionMessageDto
            {
                MessageId = m.MessageId,
                SenderType = m.SenderType,
                Content = m.Content,
                Disclaimer = isAssistant ? ChatConstants.MedicalDisclaimer : null,
                Sources = isAssistant ? (meta?.Sources ?? new List<ChatSourceDto>()) : null,
                RagUsed = isAssistant ? (meta?.RagUsed ?? false) : null,
                RecommendedRecipes = isAssistant ? (meta?.RecommendedRecipes ?? new List<ChatRecommendedRecipeDto>()) : null,
                CreatedAt = m.CreatedAt
            };
        }).ToList();

        return new ListChatSessionMessagesResponseDto
        {
            Messages = messageDtos,
            Meta = new ChatMessagesMeta
            {
                Size = messageDtos.Count,
                HasMore = hasMore
            }
        };
    }

    /// <summary>
    /// FN-CHAT-05: Xóa phiên hội thoại và toàn bộ tin nhắn.
    /// </summary>
    public async Task DeleteSessionAsync(
        long sessionId,
        long userId,
        CancellationToken ct = default)
    {
        var session = await _dbContext.ChatSessions
            .FirstOrDefaultAsync(s => s.ChatSessionId == sessionId, ct);

        if (session == null || session.UserId != (int)userId)
        {
            throw AppException.NotFound(ErrorCodes.ErrChatSessionNotFound, "Phiên hội thoại không tồn tại hoặc không thuộc về bạn.");
        }

        // Transaction: Giữ lại Surveys với Session_ID = NULL, Xóa ChatMessages, Xóa ChatSessions
        var surveys = await _dbContext.Surveys
            .Where(s => s.SessionId == sessionId)
            .ToListAsync(ct);

        foreach (var s in surveys)
        {
            s.SessionId = null;
        }

        var messages = await _dbContext.ChatMessages
            .Where(m => m.ChatSessionId == sessionId)
            .ToListAsync(ct);

        _dbContext.ChatMessages.RemoveRange(messages);
        _dbContext.ChatSessions.Remove(session);

        await _dbContext.SaveChangesAsync(ct);

        // Xóa bộ nhớ đệm
        await _redisService.RemoveAsync($"chat_context:{sessionId}", ct);
    }

    /// <summary>
    /// FN-CHAT-06: Đánh giá câu trả lời của AI hoặc tính năng AI.
    /// </summary>
    public async Task<long> CreateSurveyAsync(
        long userId,
        CreateSurveyRequest request,
        CancellationToken ct = default)
    {
        if (request.RatingScore < 1 || request.RatingScore > 5)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Điểm đánh giá phải từ 1 đến 5 sao.");
        }

        if (string.IsNullOrWhiteSpace(request.ContextQuestion))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Ngữ cảnh hoặc câu hỏi đánh giá không được để trống.");
        }

        if (request.ContextQuestion.Length > 255)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Ngữ cảnh câu hỏi không được vượt quá 255 ký tự.");
        }

        if (request.FeedbackContent?.Length > 1000)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Nội dung góp ý không được vượt quá 1000 ký tự.");
        }

        if (request.ChatSessionId.HasValue && request.ChatSessionId.Value > 0)
        {
            var sessionExists = await _dbContext.ChatSessions
                .AnyAsync(s => s.ChatSessionId == request.ChatSessionId.Value && s.UserId == (int)userId, ct);

            if (!sessionExists)
            {
                throw AppException.NotFound(ErrorCodes.ErrChatSessionNotFound, "Phiên hội thoại không tồn tại hoặc không thuộc về bạn.");
            }
        }

        var survey = new Survey
        {
            UserId = (int)userId,
            SessionId = request.ChatSessionId,
            RatingScore = request.RatingScore,
            ContextQuestion = request.ContextQuestion.Trim(),
            FeedbackContent = request.FeedbackContent?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Surveys.Add(survey);
        await _dbContext.SaveChangesAsync(ct);

        return survey.SurveyId;
    }
}
