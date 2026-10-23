using System;
using System.Collections.Generic;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.ENumbers;

namespace VeggieLife.BLL.Validators.Admin;

/// <summary>
/// Bộ kiểm tra hợp lệ dữ liệu cho chức năng Quản lý từ điển E-number (FN-ADM-11, BR-ADM-MAS-002, BR-SCI-05).
/// </summary>
public static class AdminENumberValidator
{
    public static readonly HashSet<string> AllowedVeganStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "VEGAN",
        "DOUBTFUL",
        "NON_VEGAN"
    };

    /// <summary>
    /// Kiểm tra định dạng mã phụ gia E-number trên route URL hoặc DTO.
    /// </summary>
    public static void ValidateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã E-number không được để trống.");
        }

        if (code.Trim().Length > 15)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã E-number tối đa 15 ký tự.");
        }
    }

    /// <summary>
    /// Kiểm tra hợp lệ bộ lọc tra cứu từ điển E-number.
    /// </summary>
    public static void ValidateFilter(AdminENumberFilterRequest filter)
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

        if (!string.IsNullOrWhiteSpace(filter.CategoryType) && filter.CategoryType.Trim().Length > 100)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Loại danh mục phụ gia tối đa 100 ký tự.");
        }

        if (!string.IsNullOrWhiteSpace(filter.VeganStatus))
        {
            var upperStatus = filter.VeganStatus.Trim().ToUpperInvariant();
            if (!AllowedVeganStatuses.Contains(upperStatus))
            {
                throw AppException.BadRequest(
                    ErrorCodes.ErrValidation,
                    "Trạng thái chay không hợp lệ (chấp nhận: VEGAN, DOUBTFUL, NON_VEGAN).");
            }
        }
    }

    /// <summary>
    /// Kiểm tra hợp lệ yêu cầu thêm mới E-number (FN-ADM-11).
    /// </summary>
    public static void ValidateCreate(CreateAdminENumberRequest request)
    {
        if (request == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Dữ liệu yêu cầu không được để trống.");
        }

        ValidateCode(request.Code);

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên chất phụ gia không được để trống.");
        }

        if (request.Name.Trim().Length > 150)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên chất phụ gia tối đa 150 ký tự.");
        }

        if (string.IsNullOrWhiteSpace(request.VeganStatus))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Trạng thái chay không được để trống.");
        }

        var upperStatus = request.VeganStatus.Trim().ToUpperInvariant();
        if (!AllowedVeganStatuses.Contains(upperStatus))
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Trạng thái chay không hợp lệ (chấp nhận: VEGAN, DOUBTFUL, NON_VEGAN).");
        }

        if (string.IsNullOrWhiteSpace(request.OriginSource))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Nguồn gốc xuất xứ không được để trống.");
        }

        if (request.OriginSource.Trim().Length > 100)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Nguồn gốc xuất xứ tối đa 100 ký tự.");
        }

        if (request.CategoryType != null && request.CategoryType.Trim().Length > 100)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Loại nhóm phụ gia tối đa 100 ký tự.");
        }
    }

    /// <summary>
    /// Kiểm tra hợp lệ yêu cầu cập nhật E-number (FN-ADM-11).
    /// </summary>
    public static void ValidateUpdate(UpdateAdminENumberRequest request)
    {
        if (request == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Dữ liệu yêu cầu không được để trống.");
        }

        if (request.Name == null &&
            request.VeganStatus == null &&
            request.OriginSource == null &&
            request.CategoryType == null &&
            request.HealthRiskNotes == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Cần cung cấp ít nhất một trường để cập nhật.");
        }

        if (request.Name != null)
        {
            var trimmedName = request.Name.Trim();
            if (string.IsNullOrEmpty(trimmedName))
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên chất phụ gia không được để trống.");
            }

            if (trimmedName.Length > 150)
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên chất phụ gia tối đa 150 ký tự.");
            }
        }

        if (request.VeganStatus != null)
        {
            var upperStatus = request.VeganStatus.Trim().ToUpperInvariant();
            if (!AllowedVeganStatuses.Contains(upperStatus))
            {
                throw AppException.BadRequest(
                    ErrorCodes.ErrValidation,
                    "Trạng thái chay không hợp lệ (chấp nhận: VEGAN, DOUBTFUL, NON_VEGAN).");
            }
        }

        if (request.OriginSource != null)
        {
            var trimmedOrigin = request.OriginSource.Trim();
            if (string.IsNullOrEmpty(trimmedOrigin))
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Nguồn gốc xuất xứ không được để trống.");
            }

            if (trimmedOrigin.Length > 100)
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Nguồn gốc xuất xứ tối đa 100 ký tự.");
            }
        }

        if (request.CategoryType != null && request.CategoryType.Trim().Length > 100)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Loại nhóm phụ gia tối đa 100 ký tự.");
        }
    }
}
