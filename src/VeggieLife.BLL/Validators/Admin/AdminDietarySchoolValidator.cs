using System;
using System.Collections.Generic;
using System.Linq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.DietarySchools;

namespace VeggieLife.BLL.Validators.Admin;

/// <summary>
/// Bộ kiểm tra hợp lệ dữ liệu cho chức năng Quản lý 07 trường phái ăn chay (FN-ADM-10, BR-ADM-MAS-001, D109).
/// </summary>
public static class AdminDietarySchoolValidator
{
    public static readonly HashSet<string> AllowedForbiddenFoodGroups = new(StringComparer.OrdinalIgnoreCase)
    {
        "EGG",
        "DAIRY",
        "POULTRY",
        "SEAFOOD"
    };

    /// <summary>
    /// Kiểm tra ID trường phái ăn chay cố định (1..7).
    /// </summary>
    public static void ValidateId(int id)
    {
        if (id < 1 || id > 7)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Mã trường phái không hợp lệ (hệ thống chỉ định nghĩa cố định 07 trường phái từ 1 đến 7).");
        }
    }

    /// <summary>
    /// Kiểm tra tính hợp lệ của request cập nhật trường phái (FN-ADM-10).
    /// </summary>
    public static void ValidateUpdate(UpdateAdminDietarySchoolRequest request)
    {
        if (request == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Dữ liệu yêu cầu không được để trống.");
        }

        if (request.SchoolName == null && request.ForbiddenFoodRules == null)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Cần cung cấp ít nhất tên trường phái (schoolName) hoặc quy tắc thực phẩm cấm (forbiddenFoodRules) để cập nhật.");
        }

        if (request.SchoolName != null)
        {
            var trimmedName = request.SchoolName.Trim();
            if (string.IsNullOrEmpty(trimmedName))
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên trường phái không được để trống.");
            }

            if (trimmedName.Length > 100)
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên trường phái tối đa 100 ký tự.");
            }
        }

        if (request.ForbiddenFoodRules != null)
        {
            ValidateForbiddenFoodRules(request.ForbiddenFoodRules);
        }
    }

    /// <summary>
    /// Kiểm tra schema quy tắc thực phẩm cấm {forbiddenFoodGroups: mảng con của [EGG, DAIRY, POULTRY, SEAFOOD], forbidAllium: bool} (D109).
    /// </summary>
    public static void ValidateForbiddenFoodRules(ForbiddenFoodRulesDto rules)
    {
        if (rules == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Quy tắc thực phẩm cấm không được để trống.");
        }

        if (rules.ForbiddenFoodGroups == null)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Danh sách nhóm thực phẩm cấm (forbiddenFoodGroups) không được là null.");
        }

        foreach (var group in rules.ForbiddenFoodGroups)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                throw AppException.BadRequest(
                    ErrorCodes.ErrValidation,
                    "Mã nhóm thực phẩm cấm không được để trống.");
            }

            var upperGroup = group.Trim().ToUpperInvariant();
            if (!AllowedForbiddenFoodGroups.Contains(upperGroup))
            {
                throw AppException.BadRequest(
                    ErrorCodes.ErrValidation,
                    $"Nhóm thực phẩm cấm '{group}' không hợp lệ. Chỉ chấp nhận tập con của [EGG, DAIRY, POULTRY, SEAFOOD].");
            }
        }

        if (rules.Description != null && rules.Description.Trim().Length > 500)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mô tả quy tắc cấm tối đa 500 ký tự.");
        }
    }
}
