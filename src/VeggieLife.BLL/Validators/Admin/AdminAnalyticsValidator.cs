using System;
using System.Globalization;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.Analytics;

namespace VeggieLife.BLL.Validators.Admin;

/// <summary>
/// Bộ kiểm tra tính hợp lệ của tham số truy vấn thống kê tổng quan (FN-ADM-18).
/// </summary>
public static class AdminAnalyticsValidator
{
    private const string DateFormat = "yyyy-MM-dd";

    /// <summary>
    /// Kiểm tra tính hợp lệ của khoảng ngày from/to và trả về DateTime tương ứng.
    /// </summary>
    public static (DateTime? FromDate, DateTime? ToDate) ValidateFilter(AdminAnalyticsOverviewFilterRequest filter)
    {
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
