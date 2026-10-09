namespace VeggieLife.BLL.DTOs.Profile.Responses;

/// <summary>
/// DTO phản hồi sau khi cập nhật ảnh đại diện người dùng thành công (FN-PROF-03).
/// </summary>
public class UploadCurrentUserAvatarResponse
{
    /// <summary>
    /// Đường dẫn ảnh đại diện mới trên Cloudinary CDN (đã tối ưu hóa 500x500 px, định dạng WebP).
    /// </summary>
    public string AvatarUrl { get; set; } = string.Empty;
}
