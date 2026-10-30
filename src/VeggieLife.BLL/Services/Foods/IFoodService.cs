using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Foods.Responses;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.Foods;

/// <summary>
/// Interface dịch vụ quản lý thực thể món ăn (Food) — tra cứu và tự động tạo mới theo pattern "Combobox with Create".
/// </summary>
public interface IFoodService
{
    /// <summary>
    /// Lấy danh sách tất cả món ăn có phân trang và tìm kiếm tùy chọn (không bắt buộc truyền ID).
    /// </summary>
    Task<(IReadOnlyList<FoodSummaryDto> Items, PagedMeta Meta)> ListFoodsAsync(string? query, int page = 1, int size = 20, CancellationToken ct = default);

    /// <summary>
    /// Tìm kiếm Food theo tên (dùng cho autocomplete combobox). Trả tối đa 10 kết quả.
    /// </summary>
    /// <param name="query">Từ khóa tìm kiếm tên món ăn.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Danh sách tối đa 10 món ăn khớp với từ khóa.</returns>
    Task<IReadOnlyList<FoodSummaryDto>> SearchFoodsAsync(string query, CancellationToken ct = default);

    /// <summary>
    /// Tìm Food theo tên. Nếu chưa tồn tại → tự động tạo mới rồi trả về.
    /// </summary>
    /// <param name="foodName">Tên món ăn cần lấy hoặc tạo.</param>
    /// <param name="categoryId">Danh mục sẽ gán khi tạo mới.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Entity Food đã tồn tại hoặc vừa được tạo mới.</returns>
    Task<Food> GetOrCreateFoodAsync(string foodName, int categoryId, CancellationToken ct = default);

    /// <summary>
    /// Lấy thông tin chi tiết món ăn theo mã định danh (FoodId).
    /// </summary>
    /// <param name="foodId">Mã định danh món ăn.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>FoodSummaryDto hoặc null nếu không tìm thấy.</returns>
    Task<FoodSummaryDto?> GetFoodByIdAsync(int foodId, CancellationToken ct = default);
}
