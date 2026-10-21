using System;
using System.Linq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.Moderation;

namespace VeggieLife.BLL.Validators.Admin;

/// <summary>
/// Bộ kiểm tra hợp lệ dữ liệu đầu vào cho các API Quản trị kiểm duyệt nội dung (FN-ADM-04, FN-ADM-05, FN-ADM-06).
/// </summary>
public static class AdminModerationValidator
{
    private static readonly string[] ValidReportSources =
    {
        "AI_SCAN",
        "USER_REPORT"
    };

    private static readonly string[] ValidViolationTypes =
    {
        "VIO_MEAT",
        "VIO_LABEL",
        "VIO_HATE",
        "VIO_SPAM"
    };

    private static readonly string[] ValidTargetTypes =
    {
        "POST",
        "RECIPE",
        "VIDEO",
        "COMMENT"
    };

    private static readonly string[] ValidDecisions =
    {
        "UPHOLD",
        "DISMISS"
    };

    public static void ValidateQueueFilter(AdminModerationQueueFilterRequest filter)
    {
        if (filter.Page < 1)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Chỉ số trang (page) phải từ 1 trở lên.");
        }

        if (filter.Size < 1 || filter.Size > 100)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Số lượng mỗi trang (size) phải từ 1 đến 100.");
        }

        if (!string.IsNullOrWhiteSpace(filter.ReportSource) &&
            !ValidReportSources.Contains(filter.ReportSource.Trim().ToUpperInvariant()))
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Nguồn báo cáo không hợp lệ (chấp nhận: AI_SCAN, USER_REPORT).");
        }

        if (!string.IsNullOrWhiteSpace(filter.ViolationType) &&
            !ValidViolationTypes.Contains(filter.ViolationType.Trim().ToUpperInvariant()))
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Loại vi phạm không hợp lệ (chấp nhận: VIO_MEAT, VIO_LABEL, VIO_HATE, VIO_SPAM).");
        }

        if (!string.IsNullOrWhiteSpace(filter.TargetType) &&
            !ValidTargetTypes.Contains(filter.TargetType.Trim().ToUpperInvariant()))
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Loại nội dung đích không hợp lệ (chấp nhận: POST, RECIPE, VIDEO, COMMENT).");
        }
    }

    public static void ValidateDecision(int reportId, DecideAdminModerationReportRequest request)
    {
        if (reportId <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã báo cáo không hợp lệ.");
        }

        if (request == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Dữ liệu yêu cầu không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(request.Decision))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Quyết định xử lý không được để trống.");
        }

        if (!ValidDecisions.Contains(request.Decision.Trim().ToUpperInvariant()))
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Quyết định xử lý không hợp lệ (chấp nhận: UPHOLD, DISMISS).");
        }

        if (string.IsNullOrWhiteSpace(request.Note))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Ghi chú xử lý không được để trống.");
        }

        var trimmedNote = request.Note.Trim();
        if (trimmedNote.Length < 10 || trimmedNote.Length > 500)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Ghi chú xử lý phải từ 10 đến 500 ký tự.");
        }
    }

    public static void ValidateDeleteComment(int commentId, string? reason)
    {
        if (commentId <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã bình luận không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Lý do xóa bình luận không được để trống.");
        }

        var trimmedReason = reason.Trim();
        if (trimmedReason.Length < 10 || trimmedReason.Length > 500)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Lý do xóa bình luận phải từ 10 đến 500 ký tự.");
        }
    }
}
