using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Videos.Requests;
using VeggieLife.BLL.DTOs.Videos.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Media;
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

    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly string[] AllowedVideoExtensions = { ".mp4", ".mov" };

    public VideoService(
        AppDbContext context,
        IMediaService mediaService,
        IRedisService redisService,
        ILogger<VideoService> logger)
    {
        _context = context;
        _mediaService = mediaService;
        _redisService = redisService;
        _logger = logger;
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
                    Visibility = "PUBLIC",
                    ModerationStatus = targetRecipe.Status == "APPROVED_ACTIVE" ? "APPROVED_ACTIVE" : "DRAFT",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Posts.Add(targetPost);
                await _context.SaveChangesAsync(ct);
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

            // Gắn URL ảnh vào bài viết hoặc công thức đích
            if (targetPost != null)
            {
                targetPost.ImageUrl = imageUploadResult.SecureUrl;
                targetPost.UpdatedAt = DateTime.UtcNow;
            }

            if (targetRecipe != null)
            {
                targetRecipe.ImageUrl = imageUploadResult.SecureUrl;
                targetRecipe.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(ct);

            return new CreateVideoResponseData
            {
                VideoId = null,
                Status = "READY",
                ImageUrl = imageUploadResult.SecureUrl
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

        var thumbnailUrl = _mediaService.GetImageUrl(videoUploadResult.PublicId, format: "jpg");
        if (thumbnailUrl.Length > 255) thumbnailUrl = thumbnailUrl[..255];

        var durationSeconds = (int)Math.Round(videoUploadResult.DurationSeconds ?? 0);

        // INSERT Videos (Status = PROCESSING)
        var video = new Video
        {
            PostId = targetPost!.PostId,
            Title = finalTitle,
            VideoUrl = videoUploadResult.SecureUrl.Length > 255 ? videoUploadResult.SecureUrl[..255] : videoUploadResult.SecureUrl,
            ThumbnailUrl = thumbnailUrl,
            DurationSeconds = durationSeconds,
            ViewCount = 0,
            Status = "PROCESSING",
            CreatedAt = DateTime.UtcNow
        };

        _context.Videos.Add(video);
        await _context.SaveChangesAsync(ct);

        // 6. Đưa job bóc tách vào hàng đợi Redis stt_queue (LPUSH)
        await _redisService.EnqueueAsync("stt_queue", new
        {
            VideoId = video.VideoId,
            VideoUrl = video.VideoUrl,
            DurationSeconds = video.DurationSeconds,
            CreatedAt = video.CreatedAt
        }, ct);

        _logger.LogInformation("Đã khởi tạo bản ghi video id={VideoId} với trạng thái PROCESSING và xếp hàng STT.", video.VideoId);

        return new CreateVideoResponseData
        {
            VideoId = video.VideoId,
            Status = "PROCESSING"
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

        // 2. Xử lý hạn ngạch nếu là Khách vãng lai (BR-GUEST-02, BR-AI-STT-03)
        VideoTranscriptQuotaDto? quota = null;
        if (isGuest)
        {
            if (string.IsNullOrWhiteSpace(guestUuid))
            {
                throw AppException.Unauthorized(ErrorCodes.ErrAuthInvalidToken, "Thiếu mã định danh phiên khách vãng lai (guestUuid).");
            }

            var guestKey = $"guest_quota:ai:{guestUuid}";
            const int limit = 3;

            var (allowed, current, remaining) = await _redisService.TryConsumeQuotaAsync(guestKey, limit, TimeSpan.FromHours(24), ct);
            if (!allowed)
            {
                throw AppException.Forbidden(ErrorCodes.ErrQuotaAiExceeded,
                    "Khách đã dùng hết 03 lượt AI dùng thử (FN-POST-09, BR-GUEST-02). Vui lòng đăng ký tài khoản để tiếp tục.");
            }

            quota = new VideoTranscriptQuotaDto
            {
                Used = (int)current,
                Limit = limit,
                Remaining = (int)remaining
            };
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

        // 5. Kiểm tra trạng thái bản phiên âm
        var transcript = video.VideoTranscript;
        if (transcript == null || transcript.Status != "READY")
        {
            throw AppException.NotFound(ErrorCodes.ErrTranscriptNotReady, "Chưa có bản phiên âm cho video này.");
        }

        // 6. Xây dựng ExtractedRecipeDto
        ExtractedRecipeDto? extractedRecipe = null;
        if (!string.IsNullOrWhiteSpace(transcript.ExtractedRecipeJson))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(transcript.ExtractedRecipeJson);
                var root = doc.RootElement;
                var title = root.TryGetProperty("title", out var titleElem) ? titleElem.GetString() ?? string.Empty : string.Empty;
                var steps = new List<string>();
                if (root.TryGetProperty("steps", out var stepsElem) && stepsElem.ValueKind == System.Text.Json.JsonValueKind.Array)
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
                if (root.TryGetProperty("ingredients", out var ingElem) && ingElem.ValueKind == System.Text.Json.JsonValueKind.Array)
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
                            var name = item.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
                            decimal? qty = null;
                            if (item.TryGetProperty("quantity", out var q) && q.TryGetDecimal(out var dec))
                            {
                                qty = dec;
                            }
                            var unit = item.TryGetProperty("unit", out var u) ? u.GetString() : null;
                            ingredients.Add(new ExtractedIngredientDto { Name = name, Quantity = qty, Unit = unit });
                        }
                    }
                }

                extractedRecipe = new ExtractedRecipeDto
                {
                    Title = title,
                    Ingredients = ingredients,
                    Steps = steps
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
}

