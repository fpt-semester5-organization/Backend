using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.DTOs.Admin.Categories;

namespace VeggieLife.BLL.Services.Admin;

/// <summary>
/// Giao diện dịch vụ Quản trị danh mục món ăn (FN-ADM-07, FN-ADM-08).
/// </summary>
public interface IAdminCategoryService
{
    /// <summary>
    /// FN-ADM-07: Tạo mới danh mục món ăn (CategoryName duy nhất, 2-100 ký tự).
    /// </summary>
    Task<CreateAdminCategoryResponseData> CreateCategoryAsync(
        CreateAdminCategoryRequest request,
        int adminId,
        CancellationToken ct = default);

    /// <summary>
    /// FN-ADM-07: Cập nhật danh mục món ăn (CategoryName duy nhất, IsActive, Description). IsCore không đổi được.
    /// </summary>
    Task<UpdateAdminCategoryResponseData> UpdateCategoryAsync(
        int id,
        UpdateAdminCategoryRequest request,
        int adminId,
        CancellationToken ct = default);

    /// <summary>
    /// FN-ADM-07: Lấy toàn bộ danh mục món ăn dành cho Admin (bao gồm cả danh mục ẩn, danh mục cốt lõi).
    /// </summary>
    Task<System.Collections.Generic.IReadOnlyList<AdminCategoryItemResponseData>> GetAllCategoriesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// FN-ADM-08: Xóa mềm danh mục món ăn kèm chuyển dữ liệu công thức và quán ăn sang danh mục đích.
    /// Danh mục cốt lõi (IsCore = true) không được xóa.
    /// </summary>
    Task<DeleteAdminCategoryResponseData> DeleteCategoryAsync(
        int id,
        int? targetCategoryId,
        int adminId,
        CancellationToken ct = default);
}
