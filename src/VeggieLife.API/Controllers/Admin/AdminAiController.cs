using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.Knowledge;
using VeggieLife.BLL.DTOs.Admin.Surveys;
using VeggieLife.BLL.Services.AI;
using VeggieLife.BLL.Services.Knowledge;
using VeggieLife.DAL.Context;

namespace VeggieLife.API.Controllers.Admin;

/// <summary>
/// API Controller giám sát chỉ số vận hành AI, LlmOps và kho tri thức Qdrant dành cho Quản trị viên (FN-ADM-15, FN-ADM-16, FN-CHAT-07).
/// </summary>
[Route("api/v1/admin")]
[Authorize(Policy = PolicyNames.RequireAdmin)]
public class AdminAiController : BaseApiController
{
    private readonly IAiCircuitBreaker _circuitBreaker;
    private readonly AppDbContext _db;
    private readonly IModelDriftAlertService _modelDriftAlertService;
    private readonly IAiServiceClient _aiServiceClient;
    private readonly IKnowledgeReindexService _knowledgeReindexService;
    private readonly INutritionDocumentService _nutritionDocumentService;

    public AdminAiController(
        IAiCircuitBreaker circuitBreaker,
        AppDbContext db,
        IModelDriftAlertService modelDriftAlertService,
        IAiServiceClient aiServiceClient,
        IKnowledgeReindexService knowledgeReindexService,
        INutritionDocumentService nutritionDocumentService)
    {
        _circuitBreaker = circuitBreaker;
        _db = db;
        _modelDriftAlertService = modelDriftAlertService;
        _aiServiceClient = aiServiceClient;
        _knowledgeReindexService = knowledgeReindexService;
        _nutritionDocumentService = nutritionDocumentService;
    }

    private static readonly Dictionary<string, string[]> ModuleToFeatureCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CHATBOT"] = new[] { "AI_CHAT", "CHATBOT" },
        ["MEAL_PLANNER"] = new[] { "MEAL_PLAN_GEN", "MEAL_PLANNER" },
        ["FRIDGE_VISION"] = new[] { "AI_FRIDGE", "FRIDGE_VISION" },
        ["VIDEO_TO_RECIPE"] = new[] { "SUMMARIZE_VIDEO", "VIDEO_TO_RECIPE" }
    };

    /// <summary>
    /// FN-ADM-15: Lấy chỉ số vận hành AI, tỷ lệ lỗi, độ trễ và trạng thái Circuit Breaker.
    /// </summary>
    [HttpGet("ai/metrics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMetrics(
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] string? module,
        CancellationToken ct = default)
    {
        EnsureAdmin();

        // 1. Phân tích khoảng thời gian lọc (mặc định 30 ngày gần nhất)
        var today = DateTime.UtcNow.Date;
        var fromUtc = DateTime.TryParse(from, out var f) ? f.Date : today.AddDays(-29);
        var toUtc = DateTime.TryParse(to, out var t) ? t.Date.AddDays(1) : today.AddDays(1);

        // 2. Trạng thái Circuit Breaker (BR-ADM-AI-002)
        var circuitState = _circuitBreaker.IsOpen ? "OPEN" : "CLOSED";

        // 3. Truy vấn AiquotaLogs trong khoảng thời gian lọc
        var query = _db.AiquotaLogs
            .AsNoTracking()
            .Where(l => l.CreatedAt >= fromUtc && l.CreatedAt < toUtc);

        if (!string.IsNullOrWhiteSpace(module))
        {
            var trimmed = module.Trim();
            if (ModuleToFeatureCodes.TryGetValue(trimmed, out var matchingCodes))
            {
                query = query.Where(l => matchingCodes.Contains(l.FeatureCode));
            }
            else
            {
                query = query.Where(l => l.FeatureCode == trimmed);
            }
        }

        var logs = await query
            .Select(l => new
            {
                l.FeatureCode,
                l.RequestStatus,
                l.ExecutionTimeMs
            })
            .ToListAsync(ct);

        // 4. Khảo sát đánh giá tiêu cực (Surveys với RatingScore <= 2)
        var surveys = await _db.Surveys
            .AsNoTracking()
            .Where(s => s.CreatedAt >= fromUtc && s.CreatedAt < toUtc)
            .Select(s => new { s.RatingScore })
            .ToListAsync(ct);

        var totalSurveys = surveys.Count;
        var negativeSurveys = surveys.Count(s => s.RatingScore <= 2);
        var negativeRate = totalSurveys > 0
            ? Math.Round((double)negativeSurveys / totalSurveys, 4)
            : 0.0;

        // 5. Cảnh báo Model Drift (BR-ADM-AI-004: kiểm tra AuditLogs hoặc gọi ModelDriftAlertService)
        var hasDriftAudit = await _db.AuditLogs
            .AsNoTracking()
            .AnyAsync(a => a.Action == "MODEL_DRIFT_ALERT" && a.CreatedAt >= DateTime.UtcNow.AddDays(-7), ct);

        var modelDriftAlert = hasDriftAudit;
        if (!modelDriftAlert && _modelDriftAlertService != null)
        {
            try
            {
                modelDriftAlert = await _modelDriftAlertService.CheckAndAlertModelDriftAsync(null, ct);
            }
            catch
            {
                // Bỏ qua ngoại lệ phát sinh khi kiểm tra drift ngầm
            }
        }

        // 6. Tổng hợp chỉ số theo các module chuẩn
        var standardModules = new[] { "CHATBOT", "MEAL_PLANNER", "FRIDGE_VISION", "VIDEO_TO_RECIPE" };
        var targetModules = !string.IsNullOrWhiteSpace(module)
            ? new[] { module.Trim().ToUpperInvariant() }
            : standardModules;

        var moduleMetrics = targetModules.Select(m =>
        {
            var matchingCodes = ModuleToFeatureCodes.TryGetValue(m, out var codes) ? codes : new[] { m };
            var mLogs = logs.Where(l => matchingCodes.Contains(l.FeatureCode, StringComparer.OrdinalIgnoreCase)).ToList();
            var total = mLogs.Count;
            var errors = mLogs.Count(l => string.Equals(l.RequestStatus, "ERROR", StringComparison.OrdinalIgnoreCase));
            var errRate = total > 0 ? Math.Round((double)errors / total, 4) : 0.0;
            var avgLatency = total > 0 ? (int)Math.Round(mLogs.Average(l => (double)l.ExecutionTimeMs)) : 0;

            // Khảo sát tiêu cực chỉ áp dụng cho Chatbot AI (FN-CHAT-06); các module khác không có khảo sát
            var modNegRate = string.Equals(m, "CHATBOT", StringComparison.OrdinalIgnoreCase) ? negativeRate : 0.0;

            return new
            {
                module = m,
                negativeRate = modNegRate,
                errorRate = errRate,
                avgLatencyMs = avgLatency,
                totalRequests = total
            };
        }).ToArray();

        var result = new
        {
            modules = moduleMetrics,
            modelDriftAlert = modelDriftAlert,
            circuit = new
            {
                state = circuitState
            }
        };

        return OkResponse(result, "Lấy chỉ số vận hành AI thành công.");
    }

    /// <summary>
    /// FN-ADM-15, FN-CHAT-06: Lấy danh sách khảo sát phản hồi Chatbot AI kèm bộ lọc và thống kê tổng hợp.
    /// </summary>
    [HttpGet("ai/surveys")]
    [ProducesResponseType(typeof(ApiResponse<AdminSurveyListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSurveys(
        [FromQuery] int? rating,
        [FromQuery] bool? isNegative,
        [FromQuery] string? search,
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        EnsureAdmin();

        var today = DateTime.UtcNow.Date;
        var fromUtc = DateTime.TryParse(from, out var f) ? f.Date : (DateTime?)null;
        var toUtc = DateTime.TryParse(to, out var t) ? t.Date.AddDays(1) : (DateTime?)null;

        var baseQuery = _db.Surveys
            .AsNoTracking();

        if (fromUtc.HasValue)
        {
            baseQuery = baseQuery.Where(s => s.CreatedAt >= fromUtc.Value);
        }
        if (toUtc.HasValue)
        {
            baseQuery = baseQuery.Where(s => s.CreatedAt < toUtc.Value);
        }

        // 1. Thống kê tổng quan chất lượng khảo sát trong khoảng thời gian đã chọn
        var allSurveysInScope = await baseQuery
            .Select(s => new { s.RatingScore })
            .ToListAsync(ct);

        var totalCount = allSurveysInScope.Count;
        var avgRating = totalCount > 0 ? Math.Round(allSurveysInScope.Average(s => s.RatingScore), 1) : 0.0;
        var positiveCount = allSurveysInScope.Count(s => s.RatingScore >= 4);
        var positiveRate = totalCount > 0 ? Math.Round((double)positiveCount / totalCount, 4) : 0.0;
        var negativeCount = allSurveysInScope.Count(s => s.RatingScore <= 2);
        var negativeRate = totalCount > 0 ? Math.Round((double)negativeCount / totalCount, 4) : 0.0;
        var neutralCount = allSurveysInScope.Count(s => s.RatingScore == 3);

        var summary = new AdminSurveySummaryDto
        {
            TotalSurveys = totalCount,
            AverageRating = avgRating,
            PositiveCount = positiveCount,
            PositiveRate = positiveRate,
            NegativeCount = negativeCount,
            NegativeRate = negativeRate,
            NeutralCount = neutralCount
        };

        // 2. Bộ lọc chi tiết theo số sao hoặc tiêu cực
        var filteredQuery = baseQuery;

        if (isNegative == true)
        {
            filteredQuery = filteredQuery.Where(s => s.RatingScore <= 2);
        }
        else if (rating.HasValue && rating.Value >= 1 && rating.Value <= 5)
        {
            filteredQuery = filteredQuery.Where(s => s.RatingScore == rating.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            filteredQuery = filteredQuery.Where(s =>
                s.ContextQuestion.ToLower().Contains(keyword) ||
                (s.FeedbackContent != null && s.FeedbackContent.ToLower().Contains(keyword)) ||
                s.User.FullName.ToLower().Contains(keyword) ||
                s.User.Email.ToLower().Contains(keyword));
        }

        var totalItems = await filteredQuery.CountAsync(ct);

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : (pageSize > 100 ? 100 : pageSize);

        var items = await filteredQuery
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new AdminSurveyDto
            {
                SurveyId = s.SurveyId,
                UserId = s.UserId,
                UserFullName = s.User.FullName,
                UserEmail = s.User.Email,
                UserAvatarUrl = s.User.AvatarUrl,
                SessionId = s.SessionId,
                SessionTitle = s.Session != null ? s.Session.SessionTitle : null,
                RatingScore = s.RatingScore,
                ContextQuestion = s.ContextQuestion,
                FeedbackContent = s.FeedbackContent,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync(ct);

        var pagedResult = new PagedResult<AdminSurveyDto>(items, totalItems, page, pageSize);

        var response = new AdminSurveyListResponse
        {
            Surveys = pagedResult,
            Summary = summary
        };

        return OkResponse(response, "Lấy danh sách khảo sát phản hồi AI thành công.");
    }

    /// <summary>
    /// FN-CHAT-07: Trạng thái kho vector Qdrant từ AI Service.
    /// </summary>
    [HttpGet("knowledge/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetKnowledgeStatus(CancellationToken ct = default)
    {
        EnsureAdmin();
        var result = await _aiServiceClient.GetKnowledgeStatusAsync(ct);
        return OkResponse(result, "Lấy trạng thái kho vector thành công.");
    }

    /// <summary>
    /// FN-CHAT-07: Xếp job đồng bộ lại kho tri thức.
    /// </summary>
    [HttpPost("knowledge/reindex")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReindexKnowledge(
        [FromBody] CreateAdminKnowledgeReindexRequest? request,
        CancellationToken ct = default)
    {
        EnsureAdmin();
        var scope = request?.Scope?.ToUpperInvariant() ?? "ALL";
        var result = await _knowledgeReindexService.TriggerReindexAsync(scope, ct);
        return Accepted(new ApiResponse<CreateAdminKnowledgeReindexResponseData>
        {
            StatusCode = StatusCodes.Status202Accepted,
            Message = "Đã tiếp nhận yêu cầu đồng bộ lại kho tri thức.",
            Data = result
        });
    }

    /// <summary>
    /// FN-CHAT-07: Lấy danh sách tài liệu dinh dưỡng PDF lưu trữ trong Qdrant Vector DB.
    /// </summary>
    [HttpGet("knowledge/documents")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetNutritionDocuments(CancellationToken ct = default)
    {
        EnsureAdmin();
        var result = await _nutritionDocumentService.GetDocumentsAsync(ct);
        return OkResponse(result, "Lấy danh sách tài liệu dinh dưỡng thành công.");
    }

    /// <summary>
    /// FN-CHAT-07: Tải lên tài liệu PDF mới và vector hoá vào Qdrant.
    /// </summary>
    [HttpPost("knowledge/documents/upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UploadNutritionDocument(
        [FromForm] UploadNutritionDocumentForm form,
        CancellationToken ct = default)
    {
        EnsureAdmin();

        if (form.File == null || form.File.Length == 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tệp tài liệu không được để trống.");
        }

        using var stream = form.File.OpenReadStream();
        var request = new UploadNutritionDocumentRequest
        {
            Title = form.Title,
            Author = form.Author,
            FileName = form.File.FileName,
            FileStream = stream,
            FileLength = form.File.Length
        };

        var result = await _nutritionDocumentService.UploadDocumentAsync(request, ct);
        return CreatedAtAction(nameof(GetNutritionDocuments), null, new ApiResponse<AdminNutritionDocumentDto>
        {
            StatusCode = StatusCodes.Status201Created,
            Message = "Tải lên và vector hoá tài liệu dinh dưỡng thành công.",
            Data = result
        });
    }

    /// <summary>
    /// FN-CHAT-07: Bật hoặc tắt trạng thái kích hoạt tài liệu dinh dưỡng cho Chatbot RAG.
    /// </summary>
    [HttpPut("knowledge/documents/{id}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateNutritionDocumentStatus(
        [FromRoute] string id,
        [FromBody] UpdateNutritionDocumentStatusRequest request,
        CancellationToken ct = default)
    {
        EnsureAdmin();
        var result = await _nutritionDocumentService.UpdateStatusAsync(id, request.IsActive, ct);
        return OkResponse(result, "Cập nhật trạng thái tài liệu thành công.");
    }

    /// <summary>
    /// FN-CHAT-07: Xóa tài liệu dinh dưỡng và vector embeddings khỏi Qdrant.
    /// </summary>
    [HttpDelete("knowledge/documents/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNutritionDocument(
        [FromRoute] string id,
        CancellationToken ct = default)
    {
        EnsureAdmin();
        await _nutritionDocumentService.DeleteDocumentAsync(id, ct);
        return OkResponse<object?>(null, "Đã xóa tài liệu dinh dưỡng khỏi Vector DB.");
    }

    /// <summary>
    /// FN-CHAT-07: Xem hoặc tải tệp tài liệu dinh dưỡng nguyên bản (PDF/Markdown) trực tiếp từ hệ thống.
    /// Cho phép nhúng trực tiếp vào iframe hoặc xem toàn màn hình mà không bị chặn bởi CORS/Cloudinary ACL deny.
    /// </summary>
    [HttpGet("knowledge/documents/{id}/file")]
    [HttpGet("ai/knowledge/documents/{id}/file")]
    [HttpHead("knowledge/documents/{id}/file")]
    [HttpHead("ai/knowledge/documents/{id}/file")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNutritionDocumentFile([FromRoute] string id, CancellationToken ct = default)
    {
        var fileResult = await _nutritionDocumentService.GetDocumentFileAsync(id, ct);
        if (fileResult == null)
        {
            return NotFound(new ApiErrorResponse(StatusCodes.Status404NotFound, ErrorCodes.ErrAdmNotFound, $"Không tìm thấy tệp tài liệu cho ID '{id}'."));
        }

        var (stream, contentType, fileName) = fileResult.Value;
        Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
        return File(stream, contentType, enableRangeProcessing: true);
    }

    private int EnsureAdmin()
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated)
        {
            throw AppException.Unauthorized(ErrorCodes.ErrAuthInvalidToken, "Thiếu, sai chữ ký hoặc hết hạn JWT.");
        }

        if (!string.Equals(CurrentUserRole, UserRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            throw AppException.Forbidden(ErrorCodes.ErrAdmForbidden, "Claim role khác ADMIN.");
        }

        if (CurrentUserId == null)
        {
            throw AppException.Unauthorized(ErrorCodes.ErrAuthInvalidToken, "JWT thiếu claim sub / user_id.");
        }

        return (int)CurrentUserId.Value;
    }
}

/// <summary>
/// Form dữ liệu tải lên tài liệu PDF từ Quản trị viên (FN-CHAT-07).
/// </summary>
public class UploadNutritionDocumentForm
{
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public IFormFile File { get; set; } = default!;
}
