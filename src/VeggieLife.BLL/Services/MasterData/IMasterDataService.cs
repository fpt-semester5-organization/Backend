using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.MasterData.Responses;

namespace VeggieLife.BLL.Services.MasterData;

/// <summary>
/// Interface dịch vụ tra cứu danh mục dùng chung hệ thống (FN-PROF-08):
/// Trường phái ăn chay, nhóm dị ứng, danh mục món ăn, nguyên liệu thực phẩm.
/// </summary>
public interface IMasterDataService
{
    /// <summary>
    /// Lấy danh sách 07 trường phái ăn chay cốt lõi kèm quy tắc cấm JSON (D109).
    /// Bộ nhớ đệm Redis 1 giờ.
    /// </summary>
    Task<IReadOnlyList<DietarySchoolDto>> GetDietarySchoolsAsync(CancellationToken ct = default);

    /// <summary>
    /// Lấy danh mục nhóm chất dị ứng thực phẩm.
    /// Bộ nhớ đệm Redis 1 giờ.
    /// </summary>
    Task<IReadOnlyList<AllergenDto>> GetAllergensAsync(CancellationToken ct = default);

    /// <summary>
    /// Lấy danh mục món ăn thuần chay đang hoạt động (Is_Active = 1).
    /// Bộ nhớ đệm Redis 1 giờ.
    /// </summary>
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default);

    /// <summary>
    /// Tra cứu danh sách nguyên liệu thực phẩm lọc theo từ khóa, có phân trang.
    /// Bộ nhớ đệm Redis 1 giờ theo keyword và trang.
    /// </summary>
    Task<(IReadOnlyList<IngredientDto> Items, PagedMeta Meta)> GetIngredientsAsync(string? keyword, int page, int size, CancellationToken ct = default);
}
