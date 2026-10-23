using System;
using System.Globalization;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.AuditLogs;

namespace VeggieLife.BLL.Validators.Admin;

/// <summary>
/// Bộ kiểm tra tính hợp lệ của tham số lọc và phân trang tra cứu nhật ký kiểm toán (FN-ADM-17).
/// </summary>
public static class AdminAuditLogValidator
{
    private const string DateFormat = "yyyy-MM-dd";

    /// <summary>
    /// Kiểm tra các tham số đầu vào của filter và trả về khoảng ngày đã parse (nếu có).
    /// </summary>
    public static (DateTime? FromDate, DateTime? ToDate) ValidateFilter(AdminAuditLogFilterRequest filter)
    {
        if (filter.Page < 1)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Số trang (page) phải lớn hơn hoặc bằng 1.");
        }

        if (filter.Size < 1 || filter.Size > 100)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Kích thước trang (size) phải từ 1 đến 100.");
        }

        if (filter.ActorUserId.HasValue && filter.ActorUserId.Value <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "actorUserId phải là số nguyên dương (> 0).");
        }

        if (!string.IsNullOrWhiteSpace(filter.Action) && filter.Action.Trim().Length > 50)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Hành động (action) không được vượt quá 50 ký tự.");
        }

        if (!string.IsNullOrWhiteSpace(filter.TargetTable) && filter.TargetTable.Trim().Length > 50)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên bảng đích (targetTable) không được vượt quá 50 ký tự.");
        }

        if (!string.IsNullOrWhiteSpace(filter.TargetId) && filter.TargetId.Trim().Length > 50)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã đối tượng đích (targetId) không được vượt quá 50 ký tự.");
        }

        DateTime? fromDate = null;
        if (!string.IsNullOrWhiteSpace(filter.From))
        {
            if (!DateTime.TryParseExact(filter.From.Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedFrom))
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tham số 'from' phải đúng định dạng YYYY-MM-DD.");
            }
            fromDate = parsedFrom;
        }

        DateTime? toDate = null;
        if (!string.IsNullOrWhiteSpace(filter.To))
        {
            if (!DateTime.TryParseExact(filter.To.Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTo))
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tham số 'to' phải đúng định dạng YYYY-MM-DD.");
            }
            toDate = parsedTo;
        }

        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Thời gian bắt đầu 'from' không được lớn hơn thời gian kết thúc 'to'.");
        }

        return (fromDate, toDate);
    }
}
