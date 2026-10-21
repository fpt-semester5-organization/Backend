using System;
using System.Linq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.Ingredients;

namespace VeggieLife.BLL.Validators.Admin;

/// <summary>
/// Bộ kiểm tra hợp lệ dữ liệu đầu vào cho các API Quản trị nguyên liệu & nhóm dị ứng (FN-ADM-12).
/// </summary>
public static class AdminIngredientValidator
{
    private static readonly string[] ValidStallTypes =
    {
        "PRODUCE",
        "CONDIMENTS",
        "DRY_GOODS",
        "PLANT_PROTEIN"
    };

    private static readonly string[] ValidFoodGroups =
    {
        "PLANT",
        "EGG",
        "DAIRY",
        "POULTRY",
        "SEAFOOD"
    };

    public static void ValidateFilter(AdminIngredientFilterRequest filter)
    {
        if (filter.Page < 1)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Chỉ số trang (page) phải từ 1 trở lên.");
        }

        if (filter.Size < 1 || filter.Size > 100)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Số lượng mỗi trang (size) phải từ 1 đến 100.");
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword) && filter.Keyword.Trim().Length > 100)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Từ khóa tìm kiếm tối đa 100 ký tự.");
        }

        if (filter.AllergenId.HasValue && filter.AllergenId.Value <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã nhóm dị ứng phải lớn hơn 0.");
        }
    }

    public static void ValidateCreate(CreateAdminIngredientRequest request)
    {
        if (request == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Dữ liệu yêu cầu không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên nguyên liệu không được để trống.");
        }

        if (request.Name.Trim().Length > 100)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên nguyên liệu tối đa 100 ký tự.");
        }

        if (string.IsNullOrWhiteSpace(request.DefaultStallType))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Quầy hàng mặc định không được để trống.");
        }

        if (!ValidStallTypes.Contains(request.DefaultStallType.Trim().ToUpperInvariant()))
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Quầy hàng không hợp lệ (chấp nhận: PRODUCE, CONDIMENTS, DRY_GOODS, PLANT_PROTEIN).");
        }

        var foodGroup = string.IsNullOrWhiteSpace(request.FoodGroup) ? "PLANT" : request.FoodGroup.Trim().ToUpperInvariant();
        if (!ValidFoodGroups.Contains(foodGroup))
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Nhóm thực phẩm không hợp lệ (chấp nhận: PLANT, EGG, DAIRY, POULTRY, SEAFOOD).");
        }

        if (request.CaloriesPer100g < 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Định mức calo/100g phải lớn hơn hoặc bằng 0.");
        }

        if (request.StandardUnit != null && request.StandardUnit.Trim().Length > 20)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Đơn vị tiêu chuẩn tối đa 20 ký tự.");
        }
    }

    public static void ValidateUpdate(int id, UpdateAdminIngredientRequest request)
    {
        if (id <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã nguyên liệu không hợp lệ.");
        }

        if (request == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Dữ liệu cập nhật không được để trống.");
        }

        if (request.Name != null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên nguyên liệu không được để trống.");
            }

            if (request.Name.Trim().Length > 100)
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên nguyên liệu tối đa 100 ký tự.");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.DefaultStallType) &&
            !ValidStallTypes.Contains(request.DefaultStallType.Trim().ToUpperInvariant()))
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Quầy hàng không hợp lệ (chấp nhận: PRODUCE, CONDIMENTS, DRY_GOODS, PLANT_PROTEIN).");
        }

        if (!string.IsNullOrWhiteSpace(request.FoodGroup) &&
            !ValidFoodGroups.Contains(request.FoodGroup.Trim().ToUpperInvariant()))
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Nhóm thực phẩm không hợp lệ (chấp nhận: PLANT, EGG, DAIRY, POULTRY, SEAFOOD).");
        }

        if (request.CaloriesPer100g.HasValue && request.CaloriesPer100g.Value < 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Định mức calo/100g phải lớn hơn hoặc bằng 0.");
        }

        if (request.StandardUnit != null && request.StandardUnit.Trim().Length > 20)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Đơn vị tiêu chuẩn tối đa 20 ký tự.");
        }
    }

    public static void ValidateReplaceAllergens(int id, ReplaceAdminIngredientAllergensRequest request)
    {
        if (id <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã nguyên liệu không hợp lệ.");
        }

        if (request == null || request.AllergenIds == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Danh sách mã nhóm dị ứng không được để trống.");
        }

        if (request.AllergenIds.Any(a => a <= 0))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã nhóm dị ứng phải lớn hơn 0.");
        }
    }

    public static void ValidateCreateAllergen(CreateAdminAllergenRequest request)
    {
        if (request == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Dữ liệu yêu cầu không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(request.AllergenName))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên nhóm dị ứng không được để trống.");
        }

        if (request.AllergenName.Trim().Length > 100)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên nhóm dị ứng tối đa 100 ký tự.");
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã nhóm dị ứng không được để trống.");
        }

        if (request.Code.Trim().Length > 50)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã nhóm dị ứng tối đa 50 ký tự.");
        }

        if (request.Description != null && request.Description.Trim().Length > 500)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mô tả nhóm dị ứng tối đa 500 ký tự.");
        }
    }
}
