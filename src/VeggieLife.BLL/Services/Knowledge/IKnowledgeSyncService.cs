using System.Threading;
using System.Threading.Tasks;

namespace VeggieLife.BLL.Services.Knowledge;

/// <summary>
/// Interface dịch vụ bóc tách và đồng bộ các thực thể CSDL sang kho tri thức vector Qdrant (FN-CHAT-07).
/// </summary>
public interface IKnowledgeSyncService
{
    /// <summary>
    /// Đồng bộ công thức món ăn đã duyệt (RECIPE) sang Qdrant.
    /// </summary>
    Task<bool> SyncRecipeAsync(int recipeId, CancellationToken ct = default);

    /// <summary>
    /// Đồng bộ bài viết cộng đồng đã duyệt (POST) sang Qdrant.
    /// </summary>
    Task<bool> SyncPostAsync(int postId, CancellationToken ct = default);

    /// <summary>
    /// Đồng bộ mã phụ gia thực phẩm từ điển (E_NUMBER) sang Qdrant.
    /// </summary>
    Task<bool> SyncENumberAsync(string code, CancellationToken ct = default);

    /// <summary>
    /// Đồng bộ nguyên liệu đã duyệt (INGREDIENT) sang Qdrant.
    /// </summary>
    Task<bool> SyncIngredientAsync(int ingredientId, CancellationToken ct = default);

    /// <summary>
    /// Đồng bộ nhóm dị ứng (ALLERGEN) sang Qdrant.
    /// </summary>
    Task<bool> SyncAllergenAsync(int allergenId, CancellationToken ct = default);

    /// <summary>
    /// Xóa một mẩu tri thức khỏi Qdrant theo loại nguồn và mã định danh.
    /// </summary>
    Task<int> DeleteAsync(string sourceType, string sourceId, CancellationToken ct = default);
}
