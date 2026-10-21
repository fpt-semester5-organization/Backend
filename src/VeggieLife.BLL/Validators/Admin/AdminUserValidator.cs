using System;
using System.Linq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.Users;

namespace VeggieLife.BLL.Validators.Admin;

/// <summary>
/// Bộ kiểm tra hợp lệ dữ liệu đầu vào cho các API Quản trị người dùng (FN-ADM-01, FN-ADM-02, FN-ADM-03).
/// </summary>
public static class AdminUserValidator
{
    private static readonly string[] ValidActions =
    {
        AdminSanctionActions.Warn,
        AdminSanctionActions.RestrictComment,
        AdminSanctionActions.Suspend,
        AdminSanctionActions.Ban,
        AdminSanctionActions.Reactivate
    };

    private static readonly string[] ValidAccountStatuses =
    {
        AccountStatuses.PendingActivation,
        AccountStatuses.Active,
        AccountStatuses.Quarantined,
        AccountStatuses.Suspended,
        AccountStatuses.Banned,
        AccountStatuses.SoftDeleted,
        AccountStatuses.AnonymizedDeleted
    };

    private static readonly string[] ValidContentTypes =
    {
        AdminUserContentTypes.Post,
        AdminUserContentTypes.Recipe,
        AdminUserContentTypes.Video,
        AdminUserContentTypes.Comment
    };

    public static void ValidateFilter(AdminUserListFilterRequest filter)
    {
        if (filter.Page < 1)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Số trang (page) phải lớn hơn hoặc bằng 1.");
        }

        if (filter.Size < 1 || filter.Size > 100)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Kích thước trang (size) phải từ 1 đến 100.");
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword) && filter.Keyword.Trim().Length > 100)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Từ khóa tìm kiếm không được vượt quá 100 ký tự.");
        }

        if (!string.IsNullOrWhiteSpace(filter.AccountStatus) &&
            !ValidAccountStatuses.Contains(filter.AccountStatus.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation,
                $"Trạng thái tài khoản không hợp lệ. Cho phép: {string.Join(", ", ValidAccountStatuses)}.");
        }

        if (filter.DietarySchoolId.HasValue && (filter.DietarySchoolId.Value < 1 || filter.DietarySchoolId.Value > 7))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã trường phái chay (dietarySchoolId) phải từ 1 đến 7.");
        }
    }

    public static void ValidateUpdateStatus(UpdateAdminUserStatusRequest request)
    {
        if (request == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Dữ liệu yêu cầu không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(request.Action))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Hành động kỷ luật (action) là bắt buộc.");
        }

        var normalizedAction = request.Action.Trim().ToUpperInvariant();
        if (!ValidActions.Contains(normalizedAction))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation,
                $"Hành động kỷ luật không hợp lệ. Các hành động cho phép: {string.Join(", ", ValidActions)}.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Lý do xử lý kỷ luật là bắt buộc.");
        }

        var trimmedReason = request.Reason.Trim();
        if (trimmedReason.Length < 10 || trimmedReason.Length > 500)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Lý do xử lý phải có độ dài từ 10 đến 500 ký tự.");
        }

        if (normalizedAction is AdminSanctionActions.Suspend or AdminSanctionActions.RestrictComment)
        {
            if (!request.DurationDays.HasValue || request.DurationDays.Value <= 0)
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation,
                    "Thời hạn áp dụng (durationDays) là bắt buộc và phải lớn hơn 0 ngày khi chọn SUSPEND hoặc RESTRICT_COMMENT.");
            }
        }
    }

    public static void ValidateContentsFilter(AdminUserContentsFilterRequest filter)
    {
        if (filter.Page < 1)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Số trang (page) phải lớn hơn hoặc bằng 1.");
        }

        if (filter.Size < 1 || filter.Size > 100)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Kích thước trang (size) phải từ 1 đến 100.");
        }

        if (!string.IsNullOrWhiteSpace(filter.Type) &&
            !ValidContentTypes.Contains(filter.Type.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation,
                $"Loại nội dung không hợp lệ. Cho phép: {string.Join(", ", ValidContentTypes)}.");
        }
    }
}
