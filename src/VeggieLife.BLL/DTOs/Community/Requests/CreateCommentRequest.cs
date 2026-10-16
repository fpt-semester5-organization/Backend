using System.ComponentModel.DataAnnotations;

namespace VeggieLife.BLL.DTOs.Community.Requests;

/// <summary>
/// Yêu cầu gửi bình luận mới hoặc trả lời bình luận (FN-COMM-02).
/// </summary>
public class CreateCommentRequest
{
    /// <summary>
    /// Nội dung bình luận (2-500 ký tự).
    /// </summary>
    [Required(ErrorMessage = "Nội dung bình luận không được để trống.")]
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Nội dung bình luận phải từ 2 đến 500 ký tự.")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Mã bình luận cha nếu là câu trả lời (tùy chọn).
    /// </summary>
    public int? ParentCommentId { get; set; }
}

/// <summary>
/// Alias theo OpenAPI specification cho bài viết (FN-COMM-02).
/// </summary>
public class CreatePostCommentRequest : CreateCommentRequest
{
}

/// <summary>
/// Alias theo OpenAPI specification cho công thức (FN-COMM-02).
/// </summary>
public class CreateRecipeCommentRequest : CreateCommentRequest
{
}
