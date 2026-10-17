namespace VeggieLife.DAL.External.Cloudinary;

/// <summary>
/// Kết quả xóa tài nguyên trên Cloudinary.
/// </summary>
public class CloudinaryDeleteResult
{
    /// <summary>
    /// Mã định danh công khai của tài nguyên cần xóa.
    /// </summary>
    public string PublicId { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái xóa: true nếu thành công.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Chi tiết phản hồi từ Cloudinary ("ok", "not found", ...).
    /// </summary>
    public string Result { get; set; } = string.Empty;
}
