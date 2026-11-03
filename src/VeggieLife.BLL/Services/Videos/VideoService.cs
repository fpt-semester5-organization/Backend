using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Posts.Responses;
using VeggieLife.BLL.DTOs.Videos.Requests;
using VeggieLife.BLL.DTOs.Videos.Responses;
using VeggieLife.BLL.Services.AI;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Knowledge;
using VeggieLife.BLL.Services.Media;
using VeggieLife.BLL.Services.Moderation;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.Videos;

/// <summary>
/// Triển khai dịch vụ quản lý video nấu ăn và phương tiện đa truyền thông (FN-POST-08).
/// Tuân thủ nghiêm ngặt các quy tắc nghiệp vụ BR-USR-MEDIA-01, BR-AI-STT-01, BR-AI-STT-03, BR-USR-03.
/// </summary>
public class VideoService : IVideoService
{
    private readonly AppDbContext _context;
    private readonly IMediaService _mediaService;
    private readonly IRedisService _redisService;
    private readonly ILogger<VideoService> _logger;
    private readonly IContentModerationService _moderationService;
    private readonly IKnowledgeSyncQueue _knowledgeSyncQueue;
    private readonly IAiSttClient? _aiSttClient;

    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly string[] AllowedVideoExtensions = { ".mp4", ".mov" };

    public VideoService(
        AppDbContext context,
        IMediaService mediaService,
        IRedisService redisService,
        ILogger<VideoService> logger,
        IContentModerationService? moderationService = null,
        IKnowledgeSyncQueue? knowledgeSyncQueue = null,
        IAiSttClient? aiSttClient = null)
    {
        _context = context;
        _mediaService = mediaService;
        _redisService = redisService;
        _logger = logger;
        _moderationService = moderationService ?? new SimulatedContentModerationService(new Microsoft.Extensions.Logging.Abstractions.NullLogger<SimulatedContentModerationService>());
        _knowledgeSyncQueue = knowledgeSyncQueue ?? new KnowledgeSyncQueueStub(new Microsoft.Extensions.Logging.Abstractions.NullLogger<KnowledgeSyncQueueStub>());
        _aiSttClient = aiSttClient;
    }

    /// <summary>
    /// FN-POST-08: Tải video hoặc ảnh lên Cloudinary, xác thực quyền tác giả, kiểm tra thời lượng/kích thước và ghi nhận vào CSDL.
    /// </summary>
    public async Task<CreateVideoResponseData> CreateVideoAsync(int userId, CreateVideoDto dto, CancellationToken ct = default)
    {
        // 1. Kiểm tra tham số cơ bản (ERR_VALIDATION)
        if (dto.FileStream == null || dto.FileLength <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tệp tải lên không được để trống.");
        }

        var hasPostId = dto.PostId.HasValue;
        var hasRecipeId = dto.RecipeId.HasValue;

        // BR-POST-08: Đúng 1 trong postId/recipeId
        if ((!hasPostId && !hasRecipeId) || (hasPostId && hasRecipeId))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation,
                "Yêu cầu phải cung cấp đúng một trong hai tham số postId hoặc recipeId.");
        }

        if (hasPostId && dto.PostId!.Value <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "postId phải lớn hơn 0.");
        }

        if (hasRecipeId && dto.RecipeId!.Value <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "recipeId phải lớn hơn 0.");
        }

        if (!string.IsNullOrWhiteSpace(dto.Title) && dto.Title.Length > 150)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tiêu đề video không được vượt quá 150 ký tự.");
        }

        // 2. Xác thực quyền tác giả (PRE-1: ERR_CONTENT_NOT_OWNER)
        Post? targetPost = null;
        Recipe? targetRecipe = null;

        if (hasPostId)
        {
            targetPost = await _context.Posts.FirstOrDefaultAsync(p => p.PostId == dto.PostId!.Value, ct);
            if (targetPost == null || targetPost.UserId != userId)
            {
                // PRE-1: Không phải tác giả của nội dung
                throw AppException.Forbidden(ErrorCodes.ErrContentNotOwner, "Không phải tác giả của nội dung.");
            }
        }
        else if (hasRecipeId)
        {
            targetRecipe = await _context.Recipes.FirstOrDefaultAsync(r => r.RecipeId == dto.RecipeId!.Value, ct);
            if (targetRecipe == null || (targetRecipe.UserId.HasValue && targetRecipe.UserId.Value != userId))
            {
                // PRE-1: Không phải tác giả của nội dung
                throw AppException.Forbidden(ErrorCodes.ErrContentNotOwner, "Không phải tác giả của nội dung.");
            }

            // Tìm hoặc tạo bài viết đại diện cho công thức để liên kết khóa ngoại Videos.Post_ID
            targetPost = await _context.Posts.FirstOrDefaultAsync(p => p.RecipeId == targetRecipe.RecipeId, ct);
            if (targetPost == null)
            {
                targetPost = new Post
                {
                    UserId = userId,
                    RecipeId = targetRecipe.RecipeId,
                    Title = targetRecipe.Title,
                    Content = targetRecipe.Description ?? targetRecipe.Title,
                    Visibility = targetRecipe.Status == "APPROVED_ACTIVE" ? "PUBLIC" : "PRIVATE",
                    ModerationStatus = targetRecipe.Status,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Posts.Add(targetPost);
                await _context.SaveChangesAsync(ct);
            }
            else
            {
                targetPost.ModerationStatus = targetRecipe.Status;
                targetPost.Visibility = targetRecipe.Status == "APPROVED_ACTIVE" ? "PUBLIC" : "PRIVATE";
            }
        }

        // 3. Phân loại định dạng tệp (Video vs Ảnh)
        var fileName = !string.IsNullOrWhiteSpace(dto.FileName) ? dto.FileName : "media";
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var isVideo = AllowedVideoExtensions.Contains(extension);
        var isImage = AllowedImageExtensions.Contains(extension);

        if (!isVideo && !isImage)
        {
            throw AppException.BadRequest(ErrorCodes.ErrMediaInvalidFile,
                $"Định dạng tệp '{extension}' không được hỗ trợ. Chỉ chấp nhận video MP4/MOV hoặc ảnh JPEG/PNG/WEBP.");
        }

        // 4. Xử lý trường hợp tệp là ẢNH (BR-USR-MEDIA-01)
        if (isImage)
        {
            if (dto.FileLength > 5 * 1024 * 1024) // 5 MB
            {
                throw new AppException(413, ErrorCodes.ErrMediaFileTooLarge, "Dung lượng ảnh vượt quá giới hạn tối đa 5 MB.");
            }

            var imageUploadResult = await _mediaService.UploadImageAsync(dto.FileStream, fileName, "veggielife/images", ct);

            var safeImageUrl = imageUploadResult.SecureUrl?.Length > 1000
                ? imageUploadResult.SecureUrl[..1000]
                : imageUploadResult.SecureUrl;

            string? imageModerationStatus = null;

            // Gắn URL ảnh vào bài viết hoặc công thức đích
            if (targetPost != null)
            {
                targetPost.ImageUrl = safeImageUrl;
                targetPost.UpdatedAt = DateTime.UtcNow;

                // Quét kiểm duyệt AI đối với bài viết nếu không phải DRAFT (BR-USR-03, BR-ADM-CNT-002)
                if (targetPost.ModerationStatus != "DRAFT")
                {
                    var modResult = await _moderationService.ModeratePostAsync(targetPost.Title, targetPost.Content, safeImageUrl, ct);
                    await LogAiQuotaAsync(userId, modResult, ct);

                    var isQuarantined = modResult.ViolationScore >= 85.0
                        || (!string.IsNullOrEmpty(modResult.ViolationType) && modResult.ViolationType != "NONE")
                        || !modResult.IsApproved
                        || modResult.ModerationStatus == "QUARANTINED_HIDDEN";

                    if (isQuarantined)
                    {
                        targetPost.ModerationStatus = "QUARANTINED_HIDDEN";
                        _context.ViolationReports.Add(new ViolationReport
                        {
                            PostId = targetPost.PostId,
                            ViolationType = MapToValidViolationType(modResult.ViolationType),
                            EvidenceSummary = (modResult.Note ?? "AI phát hiện vi phạm hình ảnh.").Length > 500
                                ? (modResult.Note ?? "AI phát hiện vi phạm hình ảnh.").Substring(0, 500)
                                : (modResult.Note ?? "AI phát hiện vi phạm hình ảnh."),
                            ReportSource = "AI_SCAN",
                            Status = "PENDING",
                            CreatedAt = DateTime.UtcNow
                        });
                        await _knowledgeSyncQueue.EnqueueDeleteAsync("POST", targetPost.PostId, ct);
                    }
                    else
                    {
                        targetPost.ModerationStatus = "APPROVED_ACTIVE";
                        if (targetPost.Visibility == "PUBLIC")
                        {
                            await _knowledgeSyncQueue.EnqueueUpsertAsync("POST", targetPost.PostId, ct);
                        }
                    }

                    imageModerationStatus = targetPost.ModerationStatus;
                    await InvalidatePostListCacheAsync(ct);
                }
            }

            if (targetRecipe != null)
            {
                targetRecipe.ImageUrl = safeImageUrl;
                targetRecipe.UpdatedAt = DateTime.UtcNow;

                if (targetRecipe.Status != "DRAFT")
                {
                    var modResult = await _moderationService.ModeratePostAsync(targetRecipe.Title, targetRecipe.Description ?? targetRecipe.Title, safeImageUrl, ct);
                    await LogAiQuotaAsync(userId, modResult, ct);

                    var isQuarantined = modResult.ViolationScore >= 85.0
                        || (!string.IsNullOrEmpty(modResult.ViolationType) && modResult.ViolationType != "NONE")
                        || !modResult.IsApproved
                        || modResult.ModerationStatus == "QUARANTINED_HIDDEN";

                    if (isQuarantined)
                    {
                        targetRecipe.Status = "QUARANTINED_HIDDEN";
                        _context.ViolationReports.Add(new ViolationReport
                        {
                            RecipeId = targetRecipe.RecipeId,
                            ViolationType = MapToValidViolationType(modResult.ViolationType),
                            EvidenceSummary = (modResult.Note ?? "AI phát hiện vi phạm hình ảnh.").Length > 500
                                ? (modResult.Note ?? "AI phát hiện vi phạm hình ảnh.").Substring(0, 500)
                                : (modResult.Note ?? "AI phát hiện vi phạm hình ảnh."),
                            ReportSource = "AI_SCAN",
                            Status = "PENDING",
                            CreatedAt = DateTime.UtcNow
                        });
                        await _knowledgeSyncQueue.EnqueueDeleteAsync("RECIPE", targetRecipe.RecipeId, ct);
                    }
                    else
                    {
                        targetRecipe.Status = "APPROVED_ACTIVE";
                        await _knowledgeSyncQueue.EnqueueUpsertAsync("RECIPE", targetRecipe.RecipeId, ct);
                    }

                    imageModerationStatus = targetRecipe.Status;
                    await InvalidateRecipeListCacheAsync(ct);
                }
            }

            await _context.SaveChangesAsync(ct);

            return new CreateVideoResponseData
            {
                VideoId = null,
                Status = "READY",
                ImageUrl = imageUploadResult.SecureUrl,
                ModerationStatus = imageModerationStatus
            };
        }

        // 5. Xử lý trường hợp tệp là VIDEO (BR-AI-STT-01, BR-AI-STT-03)
        if (dto.FileLength > 100 * 1024 * 1024) // 100 MB
        {
            throw new AppException(413, ErrorCodes.ErrMediaFileTooLarge, "Dung lượng video vượt quá giới hạn tối đa 100 MB.");
        }

        var videoUploadResult = await _mediaService.UploadVideoAsync(dto.FileStream, fileName, "veggielife/videos", ct);

        // Sinh tiêu đề video hợp lệ không rỗng và tối đa 150 ký tự
        var rawTitle = !string.IsNullOrWhiteSpace(dto.Title)
            ? dto.Title.Trim()
            : (targetPost?.Title ?? targetRecipe?.Title ?? "Video hướng dẫn nấu ăn");
        var finalTitle = rawTitle.Length > 150 ? rawTitle[..150] : rawTitle;

        var thumbnailUrl = !string.IsNullOrEmpty(videoUploadResult.SecureUrl) && videoUploadResult.SecureUrl.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
            ? videoUploadResult.SecureUrl[..^4] + ".jpg"
            : _mediaService.GetImageUrl(videoUploadResult.PublicId, format: "jpg");
        if (thumbnailUrl.Length > 1000) thumbnailUrl = thumbnailUrl[..1000];

        var durationSeconds = (int)Math.Round(videoUploadResult.DurationSeconds ?? 0);

        // Gán ảnh thu nhỏ của video làm ảnh đại diện bài viết / công thức nếu chưa có ảnh
        if (targetPost != null && string.IsNullOrEmpty(targetPost.ImageUrl))
        {
            targetPost.ImageUrl = thumbnailUrl;
            targetPost.UpdatedAt = DateTime.UtcNow;
        }

        if (targetRecipe != null && string.IsNullOrEmpty(targetRecipe.ImageUrl))
        {
            targetRecipe.ImageUrl = thumbnailUrl;
            targetRecipe.UpdatedAt = DateTime.UtcNow;
        }

        string? moderationStatus = null;
        var videoStatus = "PROCESSING";

        // Quét kiểm duyệt AI ngay với hình ảnh trích từ video (thumbnail) (BR-USR-03, BR-ADM-CNT-002)
        if (targetPost != null && targetPost.ModerationStatus != "DRAFT")
        {
            var modResult = await _moderationService.ModeratePostAsync(targetPost.Title, targetPost.Content, thumbnailUrl, ct);
            await LogAiQuotaAsync(userId, modResult, ct);

            var isQuarantined = modResult.ViolationScore >= 85.0
                || (!string.IsNullOrEmpty(modResult.ViolationType) && modResult.ViolationType != "NONE")
                || !modResult.IsApproved
                || modResult.ModerationStatus == "QUARANTINED_HIDDEN";

            if (isQuarantined)
            {
                targetPost.ModerationStatus = "QUARANTINED_HIDDEN";
                _context.ViolationReports.Add(new ViolationReport
                {
                    PostId = targetPost.PostId,
                    ViolationType = MapToValidViolationType(modResult.ViolationType),
                    EvidenceSummary = (modResult.Note ?? "AI phát hiện hình ảnh video vi phạm tiêu chuẩn cộng đồng.").Length > 500
                        ? (modResult.Note ?? "AI phát hiện hình ảnh video vi phạm tiêu chuẩn cộng đồng.")[..500]
                        : (modResult.Note ?? "AI phát hiện hình ảnh video vi phạm tiêu chuẩn cộng đồng."),
                    ReportSource = "AI_SCAN",
                    Status = "PENDING",
                    CreatedAt = DateTime.UtcNow
                });
                await _knowledgeSyncQueue.EnqueueDeleteAsync("POST", targetPost.PostId, ct);
                videoStatus = "BANNED";
            }
            else
            {
                // Hình ảnh video hợp lệ, nhưng bài viết phải chờ STT hoàn thành để kiểm tra kịch bản/lời thoại trước khi công khai
                targetPost.ModerationStatus = "PENDING_REVIEW";
                videoStatus = "PROCESSING";
            }

            moderationStatus = targetPost.ModerationStatus;
            await InvalidatePostListCacheAsync(ct);
        }

        if (targetRecipe != null && targetRecipe.Status != "DRAFT")
        {
            var modResult = await _moderationService.ModeratePostAsync(targetRecipe.Title, targetRecipe.Description ?? targetRecipe.Title, thumbnailUrl, ct);
            await LogAiQuotaAsync(userId, modResult, ct);

            var isQuarantined = modResult.ViolationScore >= 85.0
                || (!string.IsNullOrEmpty(modResult.ViolationType) && modResult.ViolationType != "NONE")
                || !modResult.IsApproved
                || modResult.ModerationStatus == "QUARANTINED_HIDDEN";

            if (isQuarantined)
            {
                targetRecipe.Status = "QUARANTINED_HIDDEN";
                if (targetPost != null && targetPost.RecipeId == targetRecipe.RecipeId)
                {
                    targetPost.ModerationStatus = "QUARANTINED_HIDDEN";
                }
                _context.ViolationReports.Add(new ViolationReport
                {
                    RecipeId = targetRecipe.RecipeId,
                    ViolationType = MapToValidViolationType(modResult.ViolationType),
                    EvidenceSummary = (modResult.Note ?? "AI phát hiện hình ảnh video vi phạm tiêu chuẩn cộng đồng.").Length > 500
                        ? (modResult.Note ?? "AI phát hiện hình ảnh video vi phạm tiêu chuẩn cộng đồng.")[..500]
                        : (modResult.Note ?? "AI phát hiện hình ảnh video vi phạm tiêu chuẩn cộng đồng."),
                    ReportSource = "AI_SCAN",
                    Status = "PENDING",
                    CreatedAt = DateTime.UtcNow
                });
                await _knowledgeSyncQueue.EnqueueDeleteAsync("RECIPE", targetRecipe.RecipeId, ct);
                videoStatus = "BANNED";
            }
            else
            {
                // Hình ảnh video đạt chuẩn kiểm duyệt AI
                if (targetRecipe.Status != "DRAFT")
                {
                    targetRecipe.Status = "APPROVED_ACTIVE";
                    if (targetPost != null && targetPost.RecipeId == targetRecipe.RecipeId)
                    {
                        targetPost.ModerationStatus = "APPROVED_ACTIVE";
                        targetPost.Visibility = "PUBLIC";
                    }
                }
                videoStatus = "READY";
            }
            moderationStatus = targetRecipe.Status;
            await InvalidateRecipeListCacheAsync(ct);
            await _redisService.RemoveAsync($"recipe_detail:{targetRecipe.RecipeId}", ct);
        }

        string? rawTranscript = null;
        ExtractedRecipeDto? extractedRecipe = null;

        // Bóc tách STT và kiểm duyệt nội dung lời thoại đồng bộ ngay tại giao diện đăng bài (BR-AI-STT-01, BR-USR-03)
        if (videoStatus != "BANNED" && _aiSttClient != null)
        {
            try
            {
                _logger.LogInformation("Bắt đầu xử lý bóc tách STT và kiểm duyệt lời thoại đồng bộ cho video {Url}...", videoUploadResult.SecureUrl);
                var sttResult = await _aiSttClient.ExtractRecipeFromVideoAsync(videoUploadResult.SecureUrl, ct);
                if (sttResult != null)
                {
                    rawTranscript = sttResult.RawTranscript;
                    extractedRecipe = sttResult.Recipe;

                    if (!string.IsNullOrWhiteSpace(rawTranscript))
                    {
                        var transcriptModResult = await _moderationService.ModeratePostAsync(
                            targetPost?.Title ?? targetRecipe?.Title ?? "Video hướng dẫn",
                            rawTranscript,
                            thumbnailUrl,
                            ct);
                        await LogAiQuotaAsync(userId, transcriptModResult, ct);

                        var isTranscriptQuarantined = transcriptModResult.ViolationScore >= 85.0
                            || (!string.IsNullOrEmpty(transcriptModResult.ViolationType) && transcriptModResult.ViolationType != "NONE")
                            || !transcriptModResult.IsApproved
                            || transcriptModResult.ModerationStatus == "QUARANTINED_HIDDEN";

                        if (isTranscriptQuarantined)
                        {
                            _logger.LogWarning("Lời thoại video vi phạm tiêu chuẩn cộng đồng: score={Score}, type={Type}",
                                transcriptModResult.ViolationScore, transcriptModResult.ViolationType);

                            videoStatus = "BANNED";
                            if (targetPost != null)
                            {
                                targetPost.ModerationStatus = "QUARANTINED_HIDDEN";
                                _context.ViolationReports.Add(new ViolationReport
                                {
                                    PostId = targetPost.PostId,
                                    ViolationType = MapToValidViolationType(transcriptModResult.ViolationType),
                                    EvidenceSummary = (transcriptModResult.Note ?? $"Lời thoại video vi phạm {transcriptModResult.ViolationType}: {rawTranscript}").Length > 500
                                        ? (transcriptModResult.Note ?? $"Lời thoại video vi phạm {transcriptModResult.ViolationType}: {rawTranscript}")[..500]
                                        : (transcriptModResult.Note ?? $"Lời thoại video vi phạm {transcriptModResult.ViolationType}: {rawTranscript}"),
                                    ReportSource = "AI_SCAN",
                                    Status = "PENDING",
                                    CreatedAt = DateTime.UtcNow
                                });
                                await _knowledgeSyncQueue.EnqueueDeleteAsync("POST", targetPost.PostId, ct);
                                moderationStatus = targetPost.ModerationStatus;
                            }
                            if (targetRecipe != null)
                            {
                                targetRecipe.Status = "QUARANTINED_HIDDEN";
                                if (targetPost != null && targetPost.RecipeId == targetRecipe.RecipeId)
                                {
                                    targetPost.ModerationStatus = "QUARANTINED_HIDDEN";
                                }
                                _context.ViolationReports.Add(new ViolationReport
                                {
                                    RecipeId = targetRecipe.RecipeId,
                                    ViolationType = MapToValidViolationType(transcriptModResult.ViolationType),
                                    EvidenceSummary = (transcriptModResult.Note ?? $"Lời thoại video vi phạm {transcriptModResult.ViolationType}: {rawTranscript}").Length > 500
                                        ? (transcriptModResult.Note ?? $"Lời thoại video vi phạm {transcriptModResult.ViolationType}: {rawTranscript}")[..500]
                                        : (transcriptModResult.Note ?? $"Lời thoại video vi phạm {transcriptModResult.ViolationType}: {rawTranscript}"),
                                    ReportSource = "AI_SCAN",
                                    Status = "PENDING",
                                    CreatedAt = DateTime.UtcNow
                                });
                                await _knowledgeSyncQueue.EnqueueDeleteAsync("RECIPE", targetRecipe.RecipeId, ct);
                                moderationStatus = targetRecipe.Status;
                                await InvalidateRecipeListCacheAsync(ct);
                                await _redisService.RemoveAsync($"recipe_detail:{targetRecipe.RecipeId}", ct);
                            }
                        }
                        else
                        {
                            // Cả hình ảnh video và kịch bản lời thoại đều hợp lệ tiêu chuẩn cộng đồng
                            if (targetPost != null && targetPost.ModerationStatus != "DRAFT")
                            {
                                targetPost.ModerationStatus = "APPROVED_ACTIVE";
                                moderationStatus = targetPost.ModerationStatus;
                                if (targetPost.Visibility == "PUBLIC")
                                {
                                    await _knowledgeSyncQueue.EnqueueUpsertAsync("POST", targetPost.PostId, ct);
                                }
                            }
                            if (targetRecipe != null && targetRecipe.Status != "DRAFT")
                            {
                                targetRecipe.Status = "APPROVED_ACTIVE";
                                if (targetPost != null && targetPost.RecipeId == targetRecipe.RecipeId)
                                {
                                    targetPost.ModerationStatus = "APPROVED_ACTIVE";
                                }
                                moderationStatus = targetRecipe.Status;
                                await _knowledgeSyncQueue.EnqueueUpsertAsync("RECIPE", targetRecipe.RecipeId, ct);
                                await InvalidateRecipeListCacheAsync(ct);
                                await _redisService.RemoveAsync($"recipe_detail:{targetRecipe.RecipeId}", ct);
                            }
                            videoStatus = "READY";
                        }
                    }
                    else
                    {
                        // Video không có lời thoại (im lặng/nhạc), hình ảnh đã duyệt hợp lệ
                        if (targetPost != null && targetPost.ModerationStatus != "DRAFT")
                        {
                            targetPost.ModerationStatus = "APPROVED_ACTIVE";
                            moderationStatus = targetPost.ModerationStatus;
                            if (targetPost.Visibility == "PUBLIC")
                            {
                                await _knowledgeSyncQueue.EnqueueUpsertAsync("POST", targetPost.PostId, ct);
                            }
                        }
                        if (targetRecipe != null && targetRecipe.Status != "DRAFT")
                        {
                            targetRecipe.Status = "APPROVED_ACTIVE";
                            if (targetPost != null && targetPost.RecipeId == targetRecipe.RecipeId)
                            {
                                targetPost.ModerationStatus = "APPROVED_ACTIVE";
                            }
                            moderationStatus = targetRecipe.Status;
                            await _knowledgeSyncQueue.EnqueueUpsertAsync("RECIPE", targetRecipe.RecipeId, ct);
                            await InvalidateRecipeListCacheAsync(ct);
                            await _redisService.RemoveAsync($"recipe_detail:{targetRecipe.RecipeId}", ct);
                        }
                        videoStatus = "READY";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi trích xuất STT đồng bộ cho video {Url}, sẽ tiếp tục xếp hàng nền.", videoUploadResult.SecureUrl);
            }
        }

        var video = new Video
        {
            PostId = targetPost != null ? targetPost.PostId : (targetRecipe?.RecipeId ?? 0),
            Title = finalTitle,
            VideoUrl = videoUploadResult.SecureUrl.Length > 1000 ? videoUploadResult.SecureUrl[..1000] : videoUploadResult.SecureUrl,
            ThumbnailUrl = thumbnailUrl,
            DurationSeconds = durationSeconds,
            ViewCount = 0,
            Status = videoStatus,
            CreatedAt = DateTime.UtcNow
        };

        _context.Videos.Add(video);

        var transcript = new VideoTranscript
        {
            Video = video,
            RecipeId = targetRecipe?.RecipeId,
            Status = videoStatus == "BANNED" ? "FAILED" : (videoStatus == "READY" ? "COMPLETED" : "PENDING"),
            RawTranscript = rawTranscript ?? string.Empty,
            ExtractedRecipeJson = extractedRecipe != null ? JsonSerializer.Serialize(extractedRecipe) : null,
            SttModelVersion = "faster-whisper-small-int8",
            CompletedAt = videoStatus == "READY" ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow
        };
        _context.VideoTranscripts.Add(transcript);

        await _context.SaveChangesAsync(ct);
        await InvalidatePostListCacheAsync(ct);

        // Chỉ đưa vào hàng đợi STT nền nếu chưa hoàn thành hoặc chưa bị cấm (videoStatus == "PROCESSING")
        if (videoStatus == "PROCESSING")
        {
            await _redisService.EnqueueAsync("stt_queue", new
            {
                VideoId = video.VideoId,
                VideoUrl = video.VideoUrl,
                DurationSeconds = video.DurationSeconds,
                CreatedAt = video.CreatedAt
            }, ct);
            _logger.LogInformation("Đã khởi tạo bản ghi video id={VideoId} với trạng thái PROCESSING và xếp hàng bóc tách STT.", video.VideoId);
        }
        else if (videoStatus == "BANNED")
        {
            _logger.LogWarning("Video id={VideoId} vi phạm tiêu chuẩn cộng đồng và đã bị BANNED.", video.VideoId);
        }
        else
        {
            _logger.LogInformation("Video id={VideoId} đã kiểm duyệt thành công và sẵn sàng hiển thị (READY).", video.VideoId);
        }

        return new CreateVideoResponseData
        {
            VideoId = video.VideoId,
            Status = videoStatus,
            ImageUrl = thumbnailUrl,
            ModerationStatus = moderationStatus ?? targetPost?.ModerationStatus ?? targetRecipe?.Status
        };
    }

    /// <summary>
    /// FN-POST-09: Xem bản phiên âm và công thức bóc tách sẵn của video công khai.
    /// Khách vãng lai bị trừ 1 trong 3 lượt dùng thử AI; Thành viên không giới hạn.
    /// </summary>
    public async Task<VideoTranscriptResponseData> GetVideoTranscriptAsync(
        int id,
        bool isGuest,
        string? guestUuid,
        int? currentUserId,
        CancellationToken ct = default)
    {
        // 1. Kiểm tra tham số id hợp lệ
        if (id <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã video phải lớn hơn 0.");
        }

        // 2. Xử lý hạn ngạch nếu là Khách vãng lai (BR-GUEST-02, BR-AI-STT-03, Leader câu 7: mở lại cùng video không bị trừ thêm)
        VideoTranscriptQuotaDto? quota = null;
        if (isGuest)
        {
            if (string.IsNullOrWhiteSpace(guestUuid))
            {
                throw AppException.Unauthorized(ErrorCodes.ErrAuthInvalidToken, "Thiếu mã định danh phiên khách vãng lai (guestUuid).");
            }

            var guestKey = $"guest_quota:ai:{guestUuid}";
            var viewedKey = $"guest_quota:ai:{guestUuid}:video:{id}";
            const int limit = 3;

            var alreadyViewed = await _redisService.ExistsAsync(viewedKey, ct);
            if (!alreadyViewed)
            {
                var (allowed, current, remaining) = await _redisService.TryConsumeQuotaAsync(guestKey, limit, TimeSpan.FromHours(24), ct);
                if (!allowed)
                {
                    throw AppException.Forbidden(ErrorCodes.ErrQuotaAiExceeded,
                        "Khách đã dùng hết 03 lượt AI dùng thử (FN-POST-09, BR-GUEST-02). Vui lòng đăng ký tài khoản để tiếp tục.");
                }

                await _redisService.SetAsync(viewedKey, true, TimeSpan.FromHours(24), ct);

                quota = new VideoTranscriptQuotaDto
                {
                    Used = (int)current,
                    Limit = limit,
                    Remaining = (int)remaining
                };
            }
            else
            {
                var currentUsed = await _redisService.GetAsync<int?>(guestKey, ct) ?? 1;
                quota = new VideoTranscriptQuotaDto
                {
                    Used = currentUsed,
                    Limit = limit,
                    Remaining = Math.Max(0, limit - currentUsed)
                };
            }
        }

        // 3. Truy vấn Video cùng VideoTranscript và Recipe
        var video = await _context.Videos
            .AsNoTracking()
            .Include(v => v.Post)
            .Include(v => v.VideoTranscript)
                .ThenInclude(vt => vt!.Recipe)
                    .ThenInclude(r => r!.RecipeIngredients)
                        .ThenInclude(ri => ri.Ingredient)
            .FirstOrDefaultAsync(v => v.VideoId == id, ct);

        if (video == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Video không tồn tại.");
        }

        // 4. Kiểm tra quyền xem nội dung (nếu không phải tác giả thì nội dung bài viết phải được duyệt)
        var isAuthor = currentUserId.HasValue && video.Post.UserId == currentUserId.Value;
        if (!isAuthor && video.Post.ModerationStatus != "APPROVED_ACTIVE" && video.Post.ModerationStatus != "APPROVED")
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Nội dung không tồn tại hoặc chưa ở trạng thái APPROVED_ACTIVE.");
        }

        // 5. Kiểm tra trạng thái bản phiên âm (hỗ trợ cả READY lẫn COMPLETED)
        var transcript = video.VideoTranscript;
        if (transcript == null || (transcript.Status != "READY" && transcript.Status != "COMPLETED"))
        {
            throw AppException.NotFound(ErrorCodes.ErrTranscriptNotReady, "Chưa có bản phiên âm cho video này.");
        }

        // 6. Xây dựng ExtractedRecipeDto và Segments
        ExtractedRecipeDto? extractedRecipe = null;
        List<TranscriptSegmentDto>? segments = null;
        if (!string.IsNullOrWhiteSpace(transcript.ExtractedRecipeJson))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(transcript.ExtractedRecipeJson);
                var root = doc.RootElement;
                // Trích xuất segments nếu có
                if (root.TryGetProperty("segments", out var segElem) && segElem.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    segments = new List<TranscriptSegmentDto>();
                    foreach (var s in segElem.EnumerateArray())
                    {
                        var start = s.TryGetProperty("start", out var st) ? st.GetDouble() : 0.0;
                        var end = s.TryGetProperty("end", out var en) ? en.GetDouble() : 0.0;
                        var text = s.TryGetProperty("text", out var tx) ? tx.GetString() ?? string.Empty : string.Empty;
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            segments.Add(new TranscriptSegmentDto { Start = start, End = end, Text = text });
                        }
                    }
                }

                // Trích xuất recipe (hỗ trợ cả recipe lồng trong "recipe" hoặc ở root, hỗ trợ cả camelCase và PascalCase)
                var recipeElem = (root.TryGetProperty("recipe", out var rElem) || root.TryGetProperty("Recipe", out rElem)) ? rElem : root;
                var title = (recipeElem.TryGetProperty("title", out var titleElem) || recipeElem.TryGetProperty("Title", out titleElem))
                    ? titleElem.GetString() ?? string.Empty
                    : string.Empty;

                var steps = new List<string>();
                if ((recipeElem.TryGetProperty("steps", out var stepsElem) || recipeElem.TryGetProperty("Steps", out stepsElem)) && stepsElem.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var s in stepsElem.EnumerateArray())
                    {
                        var str = s.GetString();
                        if (!string.IsNullOrEmpty(str))
                        {
                            steps.Add(str);
                        }
                    }
                }

                var ingredients = new List<ExtractedIngredientDto>();
                if ((recipeElem.TryGetProperty("ingredients", out var ingElem) || recipeElem.TryGetProperty("Ingredients", out ingElem)) && ingElem.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var item in ingElem.EnumerateArray())
                    {
                        if (item.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            ingredients.Add(new ExtractedIngredientDto
                            {
                                Name = item.GetString() ?? string.Empty
                            });
                        }
                        else if (item.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            var name = (item.TryGetProperty("name", out var n) || item.TryGetProperty("Name", out n)) ? n.GetString() ?? string.Empty : string.Empty;
                            decimal? qty = null;
                            if ((item.TryGetProperty("quantity", out var q) || item.TryGetProperty("Quantity", out q)) && q.TryGetDecimal(out var dec))
                            {
                                qty = dec;
                            }
                            var unit = (item.TryGetProperty("unit", out var u) || item.TryGetProperty("Unit", out u)) ? u.GetString() : null;
                            ingredients.Add(new ExtractedIngredientDto { Name = name, Quantity = qty, Unit = unit });
                        }
                    }
                }

                int? servings = null;
                if ((recipeElem.TryGetProperty("servings", out var sElem) || recipeElem.TryGetProperty("Servings", out sElem)) && sElem.TryGetInt32(out var sVal))
                {
                    servings = sVal;
                }

                int? cookTime = null;
                if ((recipeElem.TryGetProperty("cookTimeMinutes", out var ctElem) || recipeElem.TryGetProperty("CookTimeMinutes", out ctElem)) && ctElem.TryGetInt32(out var ctVal))
                {
                    cookTime = ctVal;
                }

                var tips = new List<string>();
                if ((recipeElem.TryGetProperty("tips", out var tElem) || recipeElem.TryGetProperty("Tips", out tElem)) && tElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var tip in tElem.EnumerateArray())
                    {
                        var str = tip.GetString();
                        if (!string.IsNullOrEmpty(str)) tips.Add(str);
                    }
                }

                extractedRecipe = new ExtractedRecipeDto
                {
                    Title = title,
                    Servings = servings,
                    CookTimeMinutes = cookTime,
                    Ingredients = ingredients,
                    Steps = steps,
                    Tips = tips.Count > 0 ? tips : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi phân tích cú pháp ExtractedRecipeJson cho video id={VideoId}", id);
            }
        }

        if (extractedRecipe == null && transcript.Recipe != null)
        {
            var recipe = transcript.Recipe;
            var steps = (recipe.Instructions ?? string.Empty)
                .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            extractedRecipe = new ExtractedRecipeDto
            {
                Title = recipe.Title,
                Ingredients = recipe.RecipeIngredients.Select(ri => new ExtractedIngredientDto
                {
                    Name = ri.Ingredient.IngredientName,
                    Quantity = ri.QuantityNeeded,
                    Unit = ri.Unit
                }).ToList(),
                Steps = steps
            };
        }

        return new VideoTranscriptResponseData
        {
            VideoId = video.VideoId,
            RawTranscript = transcript.RawTranscript ?? string.Empty,
            TranscriptStatus = transcript.Status == "READY" ? "COMPLETED" : transcript.Status,
            AiSummary = transcript.AiSummary,
            Segments = (segments != null && segments.Count > 0) ? segments : null,
            ExtractedRecipe = extractedRecipe,
            Quota = quota
        };
    }

    /// <summary>
    /// FN-POST-10: Kích hoạt tác vụ ngầm bóc tách công thức từ video (Whisper + Gemini).
    /// Áp dụng cho tác giả của video (Member | VIP).
    /// </summary>
    public async Task<ExtractVideoRecipeResponseData> ExtractRecipeAsync(
        int videoId,
        int userId,
        CancellationToken ct = default)
    {
        // 1. Kiểm tra tham số videoId
        if (videoId <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã video phải lớn hơn 0.");
        }

        // 2. Tìm video kèm Post và VideoTranscript
        var video = await _context.Videos
            .Include(v => v.Post)
            .Include(v => v.VideoTranscript)
            .FirstOrDefaultAsync(v => v.VideoId == videoId, ct);

        if (video == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Nội dung không tồn tại hoặc chưa ở trạng thái APPROVED_ACTIVE.");
        }

        // 3. Kiểm tra quyền tác giả (BR-USR-03: ERR_CONTENT_NOT_OWNER)
        if (video.Post.UserId != userId)
        {
            throw AppException.Forbidden(ErrorCodes.ErrContentNotOwner, "Không phải tác giả của nội dung.");
        }

        // 4. Kiểm tra thời lượng video (BR-AI-STT-01, BR-AI-STT-03: 10 giây đến 5 phút)
        if (video.DurationSeconds < 10 || video.DurationSeconds > 300)
        {
            throw new AppException(422, ErrorCodes.ErrSttDurationInvalid, "Thời lượng video phải từ 10 giây đến tối đa 5 phút (300 giây).");
        }

        // 5. Kiểm tra trạng thái bản phiên âm hiện tại
        if (video.VideoTranscript != null)
        {
            if (video.VideoTranscript.Status == "READY" || video.VideoTranscript.Status == "PROCESSING")
            {
                throw AppException.Conflict(ErrorCodes.ErrSttAlreadyDone, "Video đã có bản phiên âm hoặc đang được xử lý.");
            }

            // Cho phép chạy lại nếu lần đầu FAILED
            video.VideoTranscript.Status = "PENDING";
        }
        else
        {
            var transcript = new VideoTranscript
            {
                VideoId = video.VideoId,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };
            _context.VideoTranscripts.Add(transcript);
        }

        video.Status = "PROCESSING";
        await _context.SaveChangesAsync(ct);

        // 6. Xếp hàng STT vào hàng đợi Redis
        await _redisService.EnqueueAsync("stt_queue", new
        {
            VideoId = video.VideoId,
            VideoUrl = video.VideoUrl,
            DurationSeconds = video.DurationSeconds,
            CreatedAt = DateTime.UtcNow
        }, ct);

        _logger.LogInformation("Người dùng {UserId} đã kích hoạt bóc tách công thức cho video id={VideoId}.", userId, video.VideoId);

        return new ExtractVideoRecipeResponseData
        {
            VideoId = video.VideoId,
            TranscriptStatus = "PENDING"
        };
    }

    /// <summary>
    /// FN-POST-12: Lấy thông tin chi tiết siêu dữ liệu của một video độc lập theo mã video.
    /// Cho phép khách vãng lai và thành viên xem video trực tiếp mà không bắt buộc có query param ?postId=.
    /// </summary>
    public async Task<VideoDetailResponseData> GetVideoByIdAsync(
        int id,
        int? currentUserId = null,
        CancellationToken ct = default)
    {
        if (id <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã video phải lớn hơn 0.");
        }

        var video = await _context.Videos
            .AsNoTracking()
            .Include(v => v.Post)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(v => v.VideoId == id, ct);

        if (video == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Video không tồn tại.");
        }

        // Kiểm tra quyền xem nội dung: nếu không phải tác giả thì bài viết phải APPROVED_ACTIVE
        var isAuthor = currentUserId.HasValue && video.Post.UserId == currentUserId.Value;
        if (!isAuthor && video.Post.ModerationStatus != "APPROVED_ACTIVE" && video.Post.ModerationStatus != "APPROVED")
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Nội dung không tồn tại hoặc chưa ở trạng thái APPROVED_ACTIVE.");
        }

        return new VideoDetailResponseData
        {
            VideoId = video.VideoId,
            PostId = video.PostId,
            RecipeId = video.Post.RecipeId,
            Title = video.Title,
            VideoUrl = video.VideoUrl,
            ThumbnailUrl = video.ThumbnailUrl,
            DurationSeconds = video.DurationSeconds,
            Status = video.Status,
            ViewCount = video.ViewCount,
            Author = video.Post.User != null ? new VideoAuthorDto
            {
                UserId = video.Post.User.UserId,
                FullName = video.Post.User.FullName,
                AvatarUrl = video.Post.User.AvatarUrl
            } : null,
            CreatedAt = video.CreatedAt,
            UpdatedAt = video.UpdatedAt
        };
    }

    /// <summary>
    /// Lưu bản phiên âm và công thức bóc tách từ video xuống CSDL (FN-POST-09, FN-POST-10).
    /// Hỗ trợ lưu trữ trực tiếp sau khi AI phân tích để không phải gọi AI lần thứ hai.
    /// </summary>
    public async Task<VideoTranscriptResponseData> SaveVideoTranscriptAsync(
        int videoId,
        SaveVideoTranscriptDto dto,
        CancellationToken ct = default)
    {
        if (videoId <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã video phải lớn hơn 0.");
        }

        var video = await _context.Videos
            .Include(v => v.Post)
            .Include(v => v.VideoTranscript)
                .ThenInclude(vt => vt!.Recipe)
            .FirstOrDefaultAsync(v => v.VideoId == videoId, ct);

        if (video == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrContentNotFound, "Video không tồn tại.");
        }

        if (video.VideoTranscript == null)
        {
            video.VideoTranscript = new VideoTranscript
            {
                VideoId = video.VideoId,
                CreatedAt = DateTime.UtcNow
            };
            _context.VideoTranscripts.Add(video.VideoTranscript);
        }

        var combinedData = new Dictionary<string, object?>();
        if (dto.Recipe != null)
        {
            combinedData["recipe"] = dto.Recipe;
        }
        if (dto.Segments != null && dto.Segments.Count > 0)
        {
            combinedData["segments"] = dto.Segments;
        }

        var recipeJson = combinedData.Count > 0
            ? JsonSerializer.Serialize(combinedData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
            })
            : null;

        var aiSummary = dto.AiSummary;
        if (string.IsNullOrWhiteSpace(aiSummary) && dto.Recipe != null)
        {
            var ingCount = dto.Recipe.Ingredients?.Count ?? 0;
            var stepCount = dto.Recipe.Steps?.Count ?? 0;
            aiSummary = $"{dto.Recipe.Title} (Khẩu phần: {dto.Recipe.Servings ?? 2} người, {ingCount} nguyên liệu, {stepCount} bước chế biến)";
        }

        video.VideoTranscript.RawTranscript = dto.RawTranscript ?? string.Empty;
        video.VideoTranscript.ExtractedRecipeJson = recipeJson;
        video.VideoTranscript.AiSummary = aiSummary;
        video.VideoTranscript.SttModelVersion = dto.SttModelVersion ?? "faster-whisper-small-int8";
        video.VideoTranscript.Status = "READY";
        video.VideoTranscript.CompletedAt = DateTime.UtcNow;
        video.Status = "READY";

        // Tự động tạo công thức nháp (Draft Recipe) nếu video chưa có liên kết recipe
        if (dto.Recipe != null && video.VideoTranscript.RecipeId == null && video.Post != null)
        {
            try
            {
                var defaultCategory = await _context.FoodCategories.FirstOrDefaultAsync(ct);
                var defaultDietarySchool = await _context.DietarySchools.FirstOrDefaultAsync(ct);

                var rawTitle = !string.IsNullOrWhiteSpace(video.Title)
                    ? $"Công thức từ video {video.Title.Trim()}"
                    : (dto.Recipe.Title ?? "Công thức từ video");
                var recipeTitle = rawTitle.Length > 150 ? rawTitle[..150] : rawTitle;

                var steps = dto.Recipe.Steps ?? new List<string>();
                var instructions = steps.Count > 0
                    ? JsonSerializer.Serialize(steps, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
                    : "[]";

                var draftRecipe = new Recipe
                {
                    UserId = video.Post.UserId,
                    CategoryId = defaultCategory?.CategoryId ?? 1,
                    DietarySchoolId = defaultDietarySchool?.DietarySchoolId ?? 1,
                    Title = recipeTitle,
                    Description = dto.Recipe.Title ?? video.Title,
                    Instructions = instructions,
                    PrepTimeMinutes = 15,
                    CookTimeMinutes = dto.Recipe.CookTimeMinutes ?? 20,
                    Servings = dto.Recipe.Servings ?? 2,
                    CaloriesKcal = 150.0m,
                    DifficultyLevel = "MEDIUM",
                    IsVerified = false,
                    ImageUrl = video.ThumbnailUrl ?? video.Post.ImageUrl,
                    Status = "DRAFT",
                    ViewCount = 0,
                    Source = "AI",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Recipes.Add(draftRecipe);
                await _context.SaveChangesAsync(ct);

                video.VideoTranscript.RecipeId = draftRecipe.RecipeId;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể tạo bản ghi Recipe nháp khi lưu transcript cho video id={VideoId}", videoId);
            }
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Đã lưu bản phiên âm và công thức bóc tách thành công cho video id={VideoId}.", videoId);

        return new VideoTranscriptResponseData
        {
            VideoId = video.VideoId,
            RawTranscript = video.VideoTranscript.RawTranscript ?? string.Empty,
            TranscriptStatus = "COMPLETED",
            AiSummary = video.VideoTranscript.AiSummary,
            ExtractedRecipe = dto.Recipe,
            Segments = dto.Segments
        };
    }

    private async Task InvalidatePostListCacheAsync(CancellationToken ct)
    {
        try
        {
            await _redisService.RemoveAsync("post_list:*", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi xóa bộ nhớ đệm post_list:* trên Redis.");
        }
    }

    private async Task InvalidateRecipeListCacheAsync(CancellationToken ct)
    {
        try
        {
            await _redisService.RemoveAsync("recipe_search:*", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi xóa bộ nhớ đệm recipe_search:* trên Redis.");
        }
    }

    private async Task LogAiQuotaAsync(int userId, ContentModerationResult modResult, CancellationToken ct)
    {
        try
        {
            var quotaLog = new AiquotaLog
            {
                UserId = userId,
                FeatureCode = "CONTENT_MODERATION",
                TokensConsumed = modResult.TokensConsumed,
                CostEstimateUsd = modResult.CostEstimateUsd,
                RequestStatus = "SUCCESS",
                ExecutionTimeMs = modResult.ExecutionTimeMs,
                CreatedAt = DateTime.UtcNow
            };
            _context.AiquotaLogs.Add(quotaLog);
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi ghi nhật ký AIQuotaLogs cho người dùng {UserId}.", userId);
        }
    }

    private static string MapToValidViolationType(string? violationType)
    {
        return violationType switch
        {
            "VIO_MEAT" => "VIO_MEAT",
            "VIO_LABEL" => "VIO_LABEL",
            "VIO_SPAM" => "VIO_SPAM",
            _ => "VIO_HATE"
        };
    }
}

