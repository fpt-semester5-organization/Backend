using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Community.Requests;

/// <summary>
/// Yêu cầu sửa nội dung bình luận của chính mình (FN-COMM-03).
/// </summary>
public class UpdateCommentRequest
{
    /// <summary>
    /// Nội dung bình luận đã cập nhật (2-500 ký tự).
    /// </summary>
    [Required(ErrorMessage = "Nội dung bình luận không được để trống.")]
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Nội dung bình luận phải từ 2 đến 500 ký tự.")]
    public string Content { get; set; } = string.Empty;
}
