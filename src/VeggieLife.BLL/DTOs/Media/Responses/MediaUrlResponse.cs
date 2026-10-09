namespace VeggieLife.BLL.DTOs.Media.Responses;

/// <summary>
/// DTO phản hồi đường dẫn URL tối ưu hóa của tài nguyên Cloudinary.
/// </summary>
public class MediaUrlResponse
{
    /// <summary>
    /// Mã định danh công khai của tài nguyên.
    /// </summary>
    public string PublicId { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn URL tối ưu hóa đã được áp dụng transformation.
    /// </summary>
    public string Url { get; set; } = string.Empty;
}
