using System;
using System.Globalization;
using System.Linq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.Subscriptions;

namespace VeggieLife.BLL.Validators.Admin;

/// <summary>
/// Bộ kiểm tra tính hợp lệ dữ liệu truy vấn giao dịch thanh toán cho Quản trị viên (FN-ADM-13).
/// </summary>
public static class AdminPaymentValidator
{
    private const string DateFormat = "yyyy-MM-dd";

    private static readonly string[] ValidStatuses =
    {
        "PENDING",
        "SUCCESS",
        "EXPIRED",
        "FAILED",
        "REFUNDED"
    };

    private static readonly string[] ValidGateways =
    {
        "MOMO",
        "VIETQR"
    };

    public static (DateTime? FromDate, DateTime? ToDate) ValidateFilter(AdminPaymentListFilterRequest filter)
    {
        if (filter.Page < 1)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Số trang (page) phải lớn hơn hoặc bằng 1.");
        }

        if (filter.Size < 1 || filter.Size > 100)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Kích thước trang (size) phải từ 1 đến 100.");
        }

        if (!string.IsNullOrWhiteSpace(filter.Status) &&
            !ValidStatuses.Contains(filter.Status.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Trạng thái giao dịch không hợp lệ. Chỉ chấp nhận PENDING, SUCCESS, EXPIRED, FAILED, REFUNDED.");
        }

        if (!string.IsNullOrWhiteSpace(filter.Gateway) &&
            !ValidGateways.Contains(filter.Gateway.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Cổng thanh toán không hợp lệ. Chỉ chấp nhận MOMO, VIETQR.");
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword) && filter.Keyword.Trim().Length > 100)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Từ khóa tìm kiếm không được vượt quá 100 ký tự.");
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
