namespace VeggieLife.BLL.DTOs.Media.Responses;

/// <summary>
/// DTO phản hồi kết quả xóa tệp đa phương tiện trên Cloudinary.
/// </summary>
public class MediaDeleteResponse
{
    /// <summary>
    /// Mã định danh công khai của tài nguyên đã yêu cầu xóa.
    /// </summary>
    public string PublicId { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái xóa: true nếu thành công.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Thông báo kết quả thao tác xóa.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
