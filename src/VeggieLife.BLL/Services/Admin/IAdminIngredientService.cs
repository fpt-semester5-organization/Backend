using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.Ingredients;

namespace VeggieLife.BLL.Services.Admin;

/// <summary>
/// Interface dịch vụ quản trị nguyên liệu thực phẩm và nhóm dị ứng (FN-ADM-12).
/// </summary>
public interface IAdminIngredientService
{
    /// <summary>
    /// Lấy danh sách nguyên liệu theo bộ lọc và phân trang dành cho Quản trị viên.
    /// </summary>
    Task<(IReadOnlyList<AdminIngredientListItemDto> Items, PagedMeta Meta)> GetIngredientsAsync(
        AdminIngredientFilterRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Thêm mới nguyên liệu thực phẩm.
    /// </summary>
    Task<CreateAdminIngredientResponseData> CreateIngredientAsync(
        CreateAdminIngredientRequest request,
        int adminId,
        CancellationToken ct = default);

    /// <summary>
    /// Cập nhật thông tin nguyên liệu thực phẩm.
    /// </summary>
    Task<UpdateAdminIngredientResponseData> UpdateIngredientAsync(
        int id,
        UpdateAdminIngredientRequest request,
        int adminId,
        CancellationToken ct = default);

    /// <summary>
    /// Thay thế toàn bộ nhóm dị ứng gán cho nguyên liệu.
    /// </summary>
    Task<ReplaceAdminIngredientAllergensResponseData> ReplaceIngredientAllergensAsync(
        int id,
        ReplaceAdminIngredientAllergensRequest request,
        int adminId,
        CancellationToken ct = default);

    /// <summary>
    /// Thêm mới nhóm dị ứng thực phẩm.
    /// </summary>
    Task<CreateAdminAllergenResponseData> CreateAllergenAsync(
        CreateAdminAllergenRequest request,
        int adminId,
        CancellationToken ct = default);
}
