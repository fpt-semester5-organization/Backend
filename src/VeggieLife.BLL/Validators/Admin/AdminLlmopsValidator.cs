using System;
using System.Globalization;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.Llmops;

namespace VeggieLife.BLL.Validators.Admin;

/// <summary>
/// Bộ kiểm tra tính hợp lệ của tham số truy vấn bảng điều khiển LLMOps (FN-ADM-16).
/// </summary>
public static class AdminLlmopsValidator
{
    private const string DateFormat = "yyyy-MM-dd";

    /// <summary>
    /// Kiểm tra khoảng ngày from/to và tham số groupBy.
    /// </summary>
    public static (DateTime? FromDate, DateTime? ToDate, string? GroupBy) ValidateFilter(AdminLlmopsFilterRequest filter)
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
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.");
        }

        string? groupBy = null;
        if (!string.IsNullOrWhiteSpace(filter.GroupBy))
        {
            var upper = filter.GroupBy.Trim().ToUpperInvariant();
            if (upper != "DAY" && upper != "FEATURE")
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tham số 'groupBy' chỉ chấp nhận 'DAY' hoặc 'FEATURE'.");
            }
            groupBy = upper;
        }

        return (fromDate, toDate, groupBy);
    }
}
