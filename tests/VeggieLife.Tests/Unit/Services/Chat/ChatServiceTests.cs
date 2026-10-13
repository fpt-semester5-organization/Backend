using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Ai;
using VeggieLife.BLL.DTOs.Chat.Requests;
using VeggieLife.BLL.DTOs.Surveys.Requests;
using VeggieLife.BLL.Services.AI;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Chat;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Chat;

/// <summary>
/// Unit Tests toàn diện cho ChatService (FN-CHAT-01..06).
/// Bao gồm kiểm tra hạn mức Khách/Thành viên/VIP, gọi AI RAG có dẫn nguồn, xử lý lỗi Qdrant hoàn lượt, và đánh giá phản hồi.
/// </summary>
public class ChatServiceTests
{
    private class InMemoryTestRedisService : IRedisService
    {
        private readonly ConcurrentDictionary<string, object> _storage = new();

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            if (_storage.TryGetValue(key, out var val))
            {
                if (val is T typedVal) return Task.FromResult<T?>(typedVal);
                if (typeof(T) == typeof(long?) && long.TryParse(val.ToString(), out var parsedLong))
                {
                    return Task.FromResult<T?>((T)(object)parsedLong);
                }
            }
            return Task.FromResult<T?>(default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
        {
            _storage[key] = value!;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            _storage.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        {
            return Task.FromResult(_storage.ContainsKey(key));
        }

        public Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null, CancellationToken ct = default)
        {
            long current = 0;
            if (_storage.TryGetValue(key, out var val) && long.TryParse(val.ToString(), out var parsed))
            {
                current = parsed;
            }
            current += value;
            _storage[key] = current;
            return Task.FromResult(current);
        }

        public Task<(bool Allowed, long Current, long Remaining)> TryConsumeQuotaAsync(
            string key, int maxLimit, TimeSpan? expiry = null, CancellationToken ct = default)
        {
            lock (_storage)
            {
                long current = 0;
                if (_storage.TryGetValue(key, out var val) && long.TryParse(val.ToString(), out var parsed))
                {
                    current = parsed;
                }

                if (current >= maxLimit)
                {
                    return Task.FromResult((false, current, 0L));
                }

                current++;
                _storage[key] = current;
                var remaining = maxLimit - current;
                return Task.FromResult((true, current, remaining));
            }
        }

        public Task<(bool Allowed, bool IsBanned, long CurrentCount, TimeSpan? RetryAfter)> CheckIpRateLimitAsync(
            string clientIp, int maxRequestsPerMinute = 10, int banDurationMinutes = 10, CancellationToken ct = default)
        {
            return Task.FromResult((true, false, 1L, (TimeSpan?)null));
        }

        public Task EnqueueAsync<T>(string queueName, T item, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }

    private static (AppDbContext DbContext, InMemoryTestRedisService Redis, Mock<IAiServiceClient> AiMock, ChatService Service) CreateHarness()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbContext = new AppDbContext(options);
        var redis = new InMemoryTestRedisService();
        var aiMock = new Mock<IAiServiceClient>();
        var service = new ChatService(dbContext, redis, aiMock.Object, NullLogger<ChatService>.Instance);

        return (dbContext, redis, aiMock, service);
    }

    #region FN-CHAT-01: CreateSessionAsync Tests

    [Fact]
    public async Task CreateSession_Member_ShouldAssociateHealthProfile_AndReturn201()
    {
        // Arrange
        var (db, _, _, service) = CreateHarness();
        var user = new User { UserId = 10, Email = "member@veggielife.vn", FullName = "Member Test", Role = UserRoles.Member, AccountStatus = "ACTIVE" };
        var profile = new HealthProfile { UserId = 10, DietarySchoolId = 1, Gender = "MALE", RegionPreference = "NORTH", HealthGoal = "HEALTH" };
        db.Users.Add(user);
        db.HealthProfiles.Add(profile);
        await db.SaveChangesAsync();

        var request = new CreateChatSessionRequest
        {
            Title = "Tư vấn đạm chay",
            ContextType = "NUTRITION_CONSULTATION"
        };

        // Act
        var result = await service.CreateSessionAsync(10, null, request);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().BeGreaterThan(0);
        result.Title.Should().Be("Tư vấn đạm chay");
        result.ContextType.Should().Be("NUTRITION_CONSULTATION");

        var sessionInDb = await db.ChatSessions.FindAsync(result.SessionId);
        sessionInDb.Should().NotBeNull();
        sessionInDb!.UserId.Should().Be(10);
        sessionInDb.ProfilesId.Should().Be(10);
        sessionInDb.GuestUuid.Should().BeNull();
    }

    [Fact]
    public async Task CreateSession_Guest_WithinQuota_ShouldCreateSession_AndCacheRedis()
    {
        // Arrange
        var (db, redis, _, service) = CreateHarness();
        var guestUuid = Guid.NewGuid().ToString();

        var request = new CreateChatSessionRequest
        {
            Title = "Khách hỏi ăn chay",
            ContextType = "NUTRITION_CONSULTATION"
        };

        // Act
        var result = await service.CreateSessionAsync(null, guestUuid, request);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().BeGreaterThan(0);

        var sessionInDb = await db.ChatSessions.FindAsync(result.SessionId);
        sessionInDb.Should().NotBeNull();
        sessionInDb!.UserId.Should().BeNull();
        sessionInDb.GuestUuid.Should().Be(guestUuid);

        var isCached = await redis.ExistsAsync($"chat_context:{result.SessionId}");
        isCached.Should().BeTrue();
    }

    [Fact]
    public async Task CreateSession_Guest_QuotaExceeded_ShouldThrow403()
    {
        // Arrange
        var (_, redis, _, service) = CreateHarness();
        var guestUuid = Guid.NewGuid().ToString();
        // Giả lập khách đã dùng hết 3 lượt AI
        await redis.SetAsync($"guest_quota:ai:{guestUuid}", 3L);

        var request = new CreateChatSessionRequest { Title = "Phiên mới" };

        // Act
        Func<Task> act = async () => await service.CreateSessionAsync(null, guestUuid, request);

        // Assert
        var ex = (await act.Should().ThrowAsync<AppException>()).Which;
        ex.StatusCode.Should().Be(403);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrQuotaAiExceeded);
    }

    [Fact]
    public async Task CreateSession_EmptyTitle_ShouldGenerateDefaultTitle()
    {
        // Arrange
        var (_, _, _, service) = CreateHarness();
        var guestUuid = Guid.NewGuid().ToString();
        var request = new CreateChatSessionRequest { Title = "" };

        // Act
        var result = await service.CreateSessionAsync(null, guestUuid, request);

        // Assert
        result.Title.Should().StartWith("Hội thoại ngày ");
    }

    [Fact]
    public async Task CreateSession_TitleTooLong_ShouldThrow400()
    {
        // Arrange
        var (_, _, _, service) = CreateHarness();
        var request = new CreateChatSessionRequest { Title = new string('A', 101) };

        // Act
        Func<Task> act = async () => await service.CreateSessionAsync(1, null, request);

        // Assert
        var ex = (await act.Should().ThrowAsync<AppException>()).Which;
        ex.StatusCode.Should().Be(400);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    #endregion

    #region FN-CHAT-03: ListSessionsAsync Tests

    [Fact]
    public async Task ListSessions_Member_ShouldReturnSessions_WithLastMessagePreview()
    {
        // Arrange
        var (db, _, _, service) = CreateHarness();
        var session1 = new ChatSession
        {
            ChatSessionId = 1,
            UserId = 5,
            SessionTitle = "Phiên 1",
            ContextType = "NUTRITION_CONSULTATION",
            StartedAt = DateTime.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        var session2 = new ChatSession
        {
            ChatSessionId = 2,
            UserId = 5,
            SessionTitle = "Phiên 2",
            ContextType = "NUTRITION_CONSULTATION",
            StartedAt = DateTime.UtcNow.AddMinutes(-30),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-20)
        };
        db.ChatSessions.AddRange(session1, session2);

        var msg1 = new ChatMessage
        {
            MessageId = 101,
            ChatSessionId = 1,
            SenderType = "ASSISTANT",
            Content = "Bạn nên bổ sung đậu lăng...",
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        db.ChatMessages.Add(msg1);
        await db.SaveChangesAsync();

        // Act
        var result = await service.ListSessionsAsync(5, 1, 20);

        // Assert
        result.TotalItems.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items[0].SessionId.Should().Be(1);
        result.Items[0].LastMessagePreview.Should().Be("Bạn nên bổ sung đậu lăng...");
    }

    #endregion

    #region FN-CHAT-02: SendMessageAsync Tests

    [Fact]
    public async Task SendMessage_Guest_UnderQuota_ShouldSucceed_WithRealSources_AndDisclaimer()
    {
        // Arrange
        var (db, _, aiMock, service) = CreateHarness();
        var guestUuid = Guid.NewGuid().ToString();
        var session = new ChatSession
        {
            ChatSessionId = 100,
            GuestUuid = guestUuid,
            SessionTitle = "Tư vấn khách",
            ContextType = "NUTRITION_CONSULTATION",
            StartedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.ChatSessions.Add(session);
        await db.SaveChangesAsync();

        aiMock.Setup(a => a.GetChatAnswerAsync(It.IsAny<AiChatAnswerRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiChatAnswerResponse
            {
                Answer = "Bạn có thể bổ sung đạm từ đậu phụ, tempeh...",
                Sources = new List<AiSourceDto>
                {
                    new() { Type = "RECIPE", Id = 512, Title = "Canh nấm đậu phụ" }
                },
                RagUsed = true,
                RecommendedRecipeIds = new List<int> { 512 },
                Usage = new ModerationUsage { PromptTokens = 50, CompletionTokens = 30, LatencyMs = 800 }
            });

        var request = new CreateChatSessionMessageRequest { Message = "Ăn chay nên bổ sung đạm từ đâu?" };

        // Act
        var result = await service.SendMessageAsync(100, null, guestUuid, UserRoles.Guest, request);

        // Assert
        result.Should().NotBeNull();
        result.Message.Content.Should().Contain("Bạn có thể bổ sung đạm từ đậu phụ");
        result.Message.Disclaimer.Should().Be(ChatConstants.MedicalDisclaimer);
        result.Message.Sources.Should().HaveCount(1);
        result.Message.Sources![0].Title.Should().Be("Canh nấm đậu phụ");
        result.Message.RagUsed.Should().BeTrue();
        result.Quota.Used.Should().Be(1);
        result.Quota.Limit.Should().Be(3);
        result.Quota.Remaining.Should().Be(2);

        // Kiểm tra lưu DB
        var savedMessages = await db.ChatMessages.Where(m => m.ChatSessionId == 100).ToListAsync();
        savedMessages.Should().HaveCount(2); // 1 USER, 1 ASSISTANT
        savedMessages.Any(m => m.SenderType == "USER").Should().BeTrue();
        var assistantMsg = savedMessages.First(m => m.SenderType == "ASSISTANT");
        assistantMsg.MetadataJson.Should().Contain("Canh nấm đậu phụ");
    }

    [Fact]
    public async Task SendMessage_Guest_QuotaExceeded_ShouldThrow403()
    {
        // Arrange
        var (db, redis, _, service) = CreateHarness();
        var guestUuid = Guid.NewGuid().ToString();
        var session = new ChatSession
        {
            ChatSessionId = 200,
            GuestUuid = guestUuid,
            SessionTitle = "Tư vấn khách",
            ContextType = "NUTRITION_CONSULTATION",
            StartedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.ChatSessions.Add(session);
        await db.SaveChangesAsync();

        // Tiêu hao hết 3 lượt của khách
        await redis.SetAsync($"guest_quota:ai:{guestUuid}", 3L);

        var request = new CreateChatSessionMessageRequest { Message = "Câu hỏi thứ tư" };

        // Act
        Func<Task> act = async () => await service.SendMessageAsync(200, null, guestUuid, UserRoles.Guest, request);

        // Assert
        var ex = (await act.Should().ThrowAsync<AppException>()).Which;
        ex.StatusCode.Should().Be(403);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrQuotaAiExceeded);
    }

    [Fact]
    public async Task SendMessage_FreeMember_20QuestionsDailyLimit_ShouldThrow403On21st()
    {
        // Arrange
        var (db, _, _, service) = CreateHarness();
        var session = new ChatSession
        {
            ChatSessionId = 300,
            UserId = 7,
            SessionTitle = "Tư vấn Member",
            ContextType = "NUTRITION_CONSULTATION",
            StartedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.ChatSessions.Add(session);

        // Thêm 20 tin nhắn USER trong ngày hôm nay
        for (int i = 0; i < 20; i++)
        {
            db.ChatMessages.Add(new ChatMessage
            {
                ChatSessionId = 300,
                SenderType = "USER",
                Content = $"Câu hỏi {i + 1}",
                CreatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();

        var request = new CreateChatSessionMessageRequest { Message = "Câu hỏi thứ 21" };

        // Act
        Func<Task> act = async () => await service.SendMessageAsync(300, 7, null, UserRoles.Member, request);

        // Assert
        var ex = (await act.Should().ThrowAsync<AppException>()).Which;
        ex.StatusCode.Should().Be(403);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrQuotaAiExceeded);
    }

    [Fact]
    public async Task SendMessage_VipMember_AllowsUpTo50Questions()
    {
        // Arrange
        var (db, _, aiMock, service) = CreateHarness();
        var session = new ChatSession
        {
            ChatSessionId = 400,
            UserId = 8,
            SessionTitle = "Tư vấn VIP",
            ContextType = "NUTRITION_CONSULTATION",
            StartedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.ChatSessions.Add(session);

        // Thêm 20 tin nhắn trong ngày (vượt mốc 20 của Free nhưng chưa tới 50)
        for (int i = 0; i < 20; i++)
        {
            db.ChatMessages.Add(new ChatMessage
            {
                ChatSessionId = 400,
                SenderType = "USER",
                Content = $"Câu hỏi VIP {i + 1}",
                CreatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();

        aiMock.Setup(a => a.GetChatAnswerAsync(It.IsAny<AiChatAnswerRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiChatAnswerResponse
            {
                Answer = "Chào bạn VIP, đây là câu trả lời...",
                Sources = new List<AiSourceDto>(),
                RagUsed = false,
                Usage = new ModerationUsage { PromptTokens = 20, CompletionTokens = 20 }
            });

        var request = new CreateChatSessionMessageRequest { Message = "Câu hỏi thứ 21 của VIP" };

        // Act
        var result = await service.SendMessageAsync(400, 8, null, UserRoles.Vip, request);

        // Assert
        result.Should().NotBeNull();
        result.Quota.Used.Should().Be(21);
        result.Quota.Limit.Should().Be(50);
        result.Quota.Remaining.Should().Be(29);
    }

    [Fact]
    public async Task SendMessage_UnsafeContent_ShouldSaveUserMessage_AndThrow400_NotRefundQuota()
    {
        // Arrange
        var (db, redis, aiMock, service) = CreateHarness();
        var guestUuid = Guid.NewGuid().ToString();
        var session = new ChatSession
        {
            ChatSessionId = 500,
            GuestUuid = guestUuid,
            SessionTitle = "Khách hỏi",
            ContextType = "NUTRITION_CONSULTATION",
            StartedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.ChatSessions.Add(session);
        await db.SaveChangesAsync();

        aiMock.Setup(a => a.GetChatAnswerAsync(It.IsAny<AiChatAnswerRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(AppException.BadRequest(ErrorCodes.ErrChatUnsafeContent, "Nội dung vi phạm tiêu chuẩn an toàn."));

        var request = new CreateChatSessionMessageRequest { Message = "Nội dung không an toàn" };

        // Act
        Func<Task> act = async () => await service.SendMessageAsync(500, null, guestUuid, UserRoles.Guest, request);

        // Assert
        var ex = (await act.Should().ThrowAsync<AppException>()).Which;
        ex.StatusCode.Should().Be(400);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrChatUnsafeContent);

        // Tin USER vẫn được lưu (D119)
        var userMsg = await db.ChatMessages.FirstOrDefaultAsync(m => m.ChatSessionId == 500);
        userMsg.Should().NotBeNull();
        userMsg!.SenderType.Should().Be("USER");

        // Quota không được hoàn lại (đã dùng 1)
        var quotaVal = await redis.GetAsync<long?>($"guest_quota:ai:{guestUuid}");
        quotaVal.Should().Be(1);
    }

    [Fact]
    public async Task SendMessage_QdrantUnavailable_ShouldThrow503_AndRefundGuestQuota()
    {
        // Arrange
        var (db, redis, aiMock, service) = CreateHarness();
        var guestUuid = Guid.NewGuid().ToString();
        var session = new ChatSession
        {
            ChatSessionId = 600,
            GuestUuid = guestUuid,
            SessionTitle = "Khách hỏi",
            ContextType = "NUTRITION_CONSULTATION",
            StartedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.ChatSessions.Add(session);
        await db.SaveChangesAsync();

        aiMock.Setup(a => a.GetChatAnswerAsync(It.IsAny<AiChatAnswerRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(AppException.ServiceUnavailable(ErrorCodes.ErrExtQdrantUnavailable, "Qdrant không phản hồi hoặc quá 3 giây (AI service báo)."));

        var request = new CreateChatSessionMessageRequest { Message = "Tôi muốn hỏi dinh dưỡng" };

        // Act
        Func<Task> act = async () => await service.SendMessageAsync(600, null, guestUuid, UserRoles.Guest, request);

        // Assert
        var ex = (await act.Should().ThrowAsync<AppException>()).Which;
        ex.StatusCode.Should().Be(503);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrExtQdrantUnavailable);

        // Không lưu tin nhắn ASSISTANT
        var assistantMsg = await db.ChatMessages.FirstOrDefaultAsync(m => m.ChatSessionId == 600 && m.SenderType == "ASSISTANT");
        assistantMsg.Should().BeNull();

        // LƯỢT ĐƯỢC HOÀN LẠI (counter giảm về 0)
        var quotaVal = await redis.GetAsync<long?>($"guest_quota:ai:{guestUuid}");
        quotaVal.Should().Be(0);
    }

    #endregion

    #region FN-CHAT-04: ListMessagesAsync Tests

    [Fact]
    public async Task ListMessages_ShouldReturnMessagesInOrder_WithSourcesForAssistant()
    {
        // Arrange
        var (db, _, _, service) = CreateHarness();
        var session = new ChatSession
        {
            ChatSessionId = 700,
            UserId = 12,
            SessionTitle = "Lịch sử tin",
            ContextType = "NUTRITION_CONSULTATION",
            StartedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.ChatSessions.Add(session);

        var userMsg = new ChatMessage
        {
            MessageId = 1,
            ChatSessionId = 700,
            SenderType = "USER",
            Content = "Hỏi đạm",
            CreatedAt = DateTime.UtcNow.AddMinutes(-2)
        };
        var assistantMsg = new ChatMessage
        {
            MessageId = 2,
            ChatSessionId = 700,
            SenderType = "ASSISTANT",
            Content = "Ăn đậu phụ",
            MetadataJson = "{\"sources\":[{\"type\":\"RECIPE\",\"id\":512,\"title\":\"Canh nấm\"}],\"ragUsed\":true,\"recommendedRecipes\":[]}",
            CreatedAt = DateTime.UtcNow.AddMinutes(-1)
        };
        db.ChatMessages.AddRange(userMsg, assistantMsg);
        await db.SaveChangesAsync();

        // Act
        var result = await service.ListMessagesAsync(700, 12, null, null, 20);

        // Assert
        result.Messages.Should().HaveCount(2);
        result.Messages[0].SenderType.Should().Be("USER");
        result.Messages[0].Disclaimer.Should().BeNull();
        result.Messages[0].Sources.Should().BeNull();

        result.Messages[1].SenderType.Should().Be("ASSISTANT");
        result.Messages[1].Disclaimer.Should().Be(ChatConstants.MedicalDisclaimer);
        result.Messages[1].Sources.Should().HaveCount(1);
        result.Messages[1].Sources![0].Title.Should().Be("Canh nấm");
        result.Messages[1].RagUsed.Should().BeTrue();
    }

    #endregion

    #region FN-CHAT-05: DeleteSessionAsync Tests

    [Fact]
    public async Task DeleteSession_Member_ShouldNullifySurveys_AndRemoveMessagesAndSession()
    {
        // Arrange
        var (db, redis, _, service) = CreateHarness();
        var session = new ChatSession
        {
            ChatSessionId = 800,
            UserId = 15,
            SessionTitle = "Cần xóa",
            ContextType = "NUTRITION_CONSULTATION",
            StartedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.ChatSessions.Add(session);

        var msg = new ChatMessage { MessageId = 801, ChatSessionId = 800, SenderType = "USER", Content = "Tin cần xóa" };
        db.ChatMessages.Add(msg);

        var survey = new Survey { SurveyId = 802, UserId = 15, SessionId = 800, ContextQuestion = "Đánh giá", RatingScore = 5 };
        db.Surveys.Add(survey);

        await db.SaveChangesAsync();
        await redis.SetAsync("chat_context:800", "some-context");

        // Act
        await service.DeleteSessionAsync(800, 15);

        // Assert
        var sessionCheck = await db.ChatSessions.FindAsync(800L);
        sessionCheck.Should().BeNull();

        var msgCheck = await db.ChatMessages.Where(m => m.ChatSessionId == 800).ToListAsync();
        msgCheck.Should().BeEmpty();

        var surveyCheck = await db.Surveys.FindAsync(802L);
        surveyCheck.Should().NotBeNull();
        surveyCheck!.SessionId.Should().BeNull(); // Giữ lại Survey với SessionId = null (FN-CHAT-05)

        var redisCheck = await redis.ExistsAsync("chat_context:800");
        redisCheck.Should().BeFalse();
    }

    #endregion

    #region FN-CHAT-06: CreateSurveyAsync Tests

    [Fact]
    public async Task CreateSurvey_ValidData_ShouldSaveSurvey_AndReturnSurveyId()
    {
        // Arrange
        var (db, _, _, service) = CreateHarness();
        var request = new CreateSurveyRequest
        {
            ContextQuestion = "Gợi ý món cho người dị ứng đậu phộng",
            RatingScore = 4,
            FeedbackContent = "Rất hữu ích"
        };

        // Act
        var surveyId = await service.CreateSurveyAsync(25, request);

        // Assert
        surveyId.Should().BeGreaterThan(0);
        var survey = await db.Surveys.FindAsync(surveyId);
        survey.Should().NotBeNull();
        survey!.RatingScore.Should().Be(4);
        survey.ContextQuestion.Should().Be("Gợi ý món cho người dị ứng đậu phộng");
    }

    [Fact]
    public async Task CreateSurvey_InvalidRatingScore_ShouldThrow400()
    {
        // Arrange
        var (_, _, _, service) = CreateHarness();
        var request = new CreateSurveyRequest
        {
            ContextQuestion = "Câu hỏi",
            RatingScore = 6 // Vượt quá 1-5
        };

        // Act
        Func<Task> act = async () => await service.CreateSurveyAsync(25, request);

        // Assert
        var ex = (await act.Should().ThrowAsync<AppException>()).Which;
        ex.StatusCode.Should().Be(400);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    #endregion
}
