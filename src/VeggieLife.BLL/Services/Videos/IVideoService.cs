using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.DTOs.Videos.Requests;
using VeggieLife.BLL.DTOs.Videos.Responses;

namespace VeggieLife.BLL.Services.Videos;

/// <summary>
/// Interface dịch vụ quản lý video nấu ăn và phương tiện truyền thông đa phương tiện (FN-POST-08).
/// </summary>
public interface IVideoService
{
    /// <summary>
    /// Tải video hoặc ảnh lên Cloudinary, xác thực quyền tác giả, kiểm tra thời lượng/dung lượng và ghi nhận vào CSDL.
    /// FN-POST-08.
    /// </summary>
    /// <param name="userId">Mã người dùng đang thực hiện.</param>
    /// <param name="dto">Dữ liệu yêu cầu gồm luồng tệp và postId/recipeId.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Dữ liệu kết quả tiếp nhận xử lý.</returns>
    Task<CreateVideoResponseData> CreateVideoAsync(int userId, CreateVideoDto dto, CancellationToken ct = default);

    /// <summary>
    /// FN-POST-09: Xem bản phiên âm và công thức bóc tách sẵn của video công khai.
    /// Khách vãng lai bị trừ 1 trong 3 lượt dùng thử AI; Thành viên không giới hạn.
    /// </summary>
    /// <param name="id">Mã định danh video (> 0).</param>
    /// <param name="isGuest">Cờ xác định người dùng là Khách vãng lai.</param>
    /// <param name="guestUuid">Mã định danh phiên khách vãng lai.</param>
    /// <param name="currentUserId">Mã định danh người dùng đăng nhập (nếu có).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Dữ liệu bản phiên âm, công thức bóc tách và hạn ngạch.</returns>
    Task<VideoTranscriptResponseData> GetVideoTranscriptAsync(
        int id,
        bool isGuest,
        string? guestUuid,
        int? currentUserId,
        CancellationToken ct = default);

    /// <summary>
    /// FN-POST-10: Kích hoạt tác vụ ngầm bóc tách công thức từ video (Whisper + Gemini).
    /// Áp dụng cho tác giả của video (Member | VIP).
    /// </summary>
    /// <param name="videoId">Mã định danh video (> 0).</param>
    /// <param name="userId">Mã người dùng yêu cầu (tác giả video).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Dữ liệu xác nhận tiếp nhận tác vụ bóc tách.</returns>
    Task<ExtractVideoRecipeResponseData> ExtractRecipeAsync(
        int videoId,
        int userId,
        CancellationToken ct = default);
}

