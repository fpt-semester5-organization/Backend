using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Recipes.Requests;
using VeggieLife.BLL.DTOs.Recipes.Responses;

namespace VeggieLife.BLL.Services.Recipes;

/// <summary>
/// Interface dịch vụ quản lý và tra cứu công thức nấu ăn món chay (FN-POST-01, FN-POST-02).
/// </summary>
public interface IRecipeService
{
    /// <summary>
    /// Tìm kiếm và lọc danh sách công thức, video nấu ăn đã được duyệt công khai (FN-POST-01).
    /// </summary>
    /// <param name="filter">Bộ lọc tìm kiếm (từ khóa, danh mục, trường phái, nguyên liệu, thời gian, loại nội dung, sắp xếp, phân trang).</param>
    /// <param name="isGuest">Cờ xác định người gọi là Khách vãng lai hay Thành viên.</param>
    /// <param name="guestId">Mã định danh của khách (Guest_UUID) dùng để lưu lịch sử tìm kiếm vào Redis.</param>
    /// <param name="ct">Mã hủy tác vụ bất đồng bộ.</param>
    /// <returns>Danh sách tóm tắt công thức và thông tin phân trang (kèm signUpPromptAtItem = 5 cho Khách).</returns>
    Task<(IReadOnlyList<RecipeSummaryDto> Items, PagedMeta Meta)> ListRecipesAsync(
        RecipeFilterRequest filter,
        bool isGuest,
        string? guestId,
        CancellationToken ct = default);

    /// <summary>
    /// Xem chi tiết đầy đủ một công thức nấu ăn đã được duyệt (FN-POST-02).
    /// </summary>
    /// <param name="id">Mã định danh duy nhất của công thức (Recipe_ID > 0).</param>
    /// <param name="isGuest">Cờ xác định người gọi là Khách vãng lai hay Thành viên.</param>
    /// <param name="currentUserId">Mã người dùng hiện tại (nếu đã đăng nhập, cho phép tác giả xem bài của mình).</param>
    /// <param name="ct">Mã hủy tác vụ bất đồng bộ.</param>
    /// <returns>Chi tiết công thức gồm nguyên liệu định lượng, các bước nấu, cờ transcript và quyền tương tác.</returns>
    Task<RecipeDetailDto> GetRecipeByIdAsync(
        int id,
        bool isGuest,
        int? currentUserId = null,
        CancellationToken ct = default);
}
