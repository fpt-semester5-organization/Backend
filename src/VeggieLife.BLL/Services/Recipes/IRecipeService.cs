using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Recipes.Requests;
using VeggieLife.BLL.DTOs.Recipes.Responses;

namespace VeggieLife.BLL.Services.Recipes;

/// <summary>
/// Interface dịch vụ quản lý và tra cứu công thức nấu ăn món chay (FN-POST-01, FN-POST-02, FN-POST-03, FN-POST-04).
/// </summary>
public interface IRecipeService
{
    /// <summary>
    /// Tìm kiếm và lọc danh sách công thức, video nấu ăn đã được duyệt công khai (FN-POST-01).
    /// </summary>
    Task<(IReadOnlyList<RecipeSummaryDto> Items, PagedMeta Meta)> ListRecipesAsync(
        RecipeFilterRequest filter,
        bool isGuest,
        string? guestId,
        CancellationToken ct = default);

    /// <summary>
    /// Xem chi tiết đầy đủ một công thức nấu ăn đã được duyệt (FN-POST-02).
    /// </summary>
    Task<RecipeDetailDto> GetRecipeByIdAsync(
        int id,
        bool isGuest,
        int? currentUserId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Đăng công thức nấu ăn món chay mới: lưu nháp hoặc gửi duyệt kèm kiểm duyệt AI và Red Alert (FN-POST-03).
    /// </summary>
    Task<CreateRecipeResponseDto> CreateRecipeAsync(
        int userId,
        CreateRecipeRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Cập nhật công thức nấu ăn của mình: kiểm duyệt AI đồng bộ trước khi ghi nếu đổi nội dung (FN-POST-04).
    /// </summary>
    Task<UpdateRecipeResponseDto> UpdateRecipeAsync(
        int id,
        int userId,
        string userRole,
        UpdateRecipeRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gỡ khỏi trang cá nhân (mode=archive) hoặc xóa mềm toàn hệ thống (mode=all) (FN-POST-04).
    /// </summary>
    Task<DeleteRecipeResponseDto> DeleteRecipeAsync(
        int id,
        int userId,
        string userRole,
        string mode,
        CancellationToken ct = default);
}
