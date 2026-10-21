using System;
using System.Linq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.Venues;

namespace VeggieLife.BLL.Validators.Admin;

/// <summary>
/// Bộ kiểm tra hợp lệ dữ liệu đầu vào cho các API Quản trị quán chay & đồng bộ nguồn (FN-ADM-14).
/// </summary>
public static class AdminVenueValidator
{
    private static readonly string[] ValidSources =
    {
        "HAPPYCOW",
        "GOOGLE_PLACES"
    };

    public static void ValidateFilter(AdminVenueFilterRequest filter)
    {
        if (filter.Page < 1)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Chỉ số trang (page) phải từ 1 trở lên.");
        }

        if (filter.Size < 1 || filter.Size > 100)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Số lượng mỗi trang (size) phải từ 1 đến 100.");
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword) && filter.Keyword.Trim().Length > 150)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Từ khóa tìm kiếm tối đa 150 ký tự.");
        }

        if (!string.IsNullOrWhiteSpace(filter.Source) &&
            !ValidSources.Contains(filter.Source.Trim().ToUpperInvariant()))
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Nguồn dữ liệu không hợp lệ (chấp nhận: HAPPYCOW, GOOGLE_PLACES).");
        }
    }

    public static void ValidateUpdate(int id, UpdateAdminVenueRequest request)
    {
        if (id <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã quán chay không hợp lệ.");
        }

        if (request == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Dữ liệu yêu cầu không được để trống.");
        }
    }

    public static void ValidateSync(SyncAdminVenuesRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Province))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên tỉnh/thành phố không được để trống.");
        }

        var trimmed = request.Province.Trim();
        if (trimmed.Length < 1 || trimmed.Length > 50)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên tỉnh/thành phố phải từ 1 đến 50 ký tự.");
        }
    }
}
