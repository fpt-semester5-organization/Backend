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
}
