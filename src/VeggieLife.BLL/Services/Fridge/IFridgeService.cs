using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.DTOs.Fridge.Requests;
using VeggieLife.BLL.DTOs.Fridge.Responses;

namespace VeggieLife.BLL.Services.Fridge;

/// <summary>
/// Hợp đồng dịch vụ quản lý tủ lạnh ảo và nhận diện thị giác máy tính Gemini Vision (FN-VIS-01, FN-VIS-02).
/// </summary>
public interface IFridgeService
{
    /// <summary>
    /// FN-VIS-01: Nhận diện nguyên liệu thực vật và độ tươi từ ảnh tủ lạnh/rổ rau bằng Gemini Vision.
    /// Hỗ trợ cả Khách (dùng thử 03 lượt/phiên, không lưu DB) và Thành viên (lưu DB, 10-30 ảnh/ngày).
    /// </summary>
    Task<CreateFridgeDetectionResponseData> DetectIngredientsAsync(
        FridgeDetectionInputDto input,
        int? userId,
        string userRole,
        string? guestUuid,
        CancellationToken ct = default);

    /// <summary>
    /// FN-VIS-02: Lấy danh sách nguyên liệu trong tủ lạnh ảo của người dùng, ưu tiên NON-FRESH xếp trước.
    /// </summary>
    Task<IReadOnlyList<FridgeItemDto>> GetFridgeItemsAsync(
        int userId,
        string? freshnessFilter = null,
        CancellationToken ct = default);

    /// <summary>
    /// FN-VIS-02: Thêm thủ công một nguyên liệu vào tủ lạnh ảo.
    /// </summary>
    Task<FridgeItemDto> AddFridgeItemAsync(
        int userId,
        CreateFridgeItemRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// FN-VIS-02: Cập nhật độ tươi (FRESH / NON-FRESH) hoặc tên hiển thị của nguyên liệu trong tủ lạnh ảo.
    /// </summary>
    Task<FridgeItemDto> UpdateFridgeItemAsync(
        int userId,
        int inventoryId,
        UpdateFridgeItemRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// FN-VIS-02: Xóa một mục nguyên liệu khỏi tủ lạnh ảo của người dùng.
    /// </summary>
    Task DeleteFridgeItemAsync(
        int userId,
        int inventoryId,
        CancellationToken ct = default);

    /// <summary>
    /// FN-VIS-03: Gợi ý công thức món ăn có độ tương đồng nguyên liệu >= 70% từ tủ lạnh ảo.
    /// Ưu tiên các món tận dụng nguyên liệu đang NON-FRESH, loại trừ vi phạm trường phái và dị ứng.
    /// </summary>
    Task<IReadOnlyList<FridgeRecipeSuggestionDto>> GetRecipeSuggestionsAsync(
        int userId,
        double minSimilarity = 0.7,
        int limit = 10,
        CancellationToken ct = default);
}

