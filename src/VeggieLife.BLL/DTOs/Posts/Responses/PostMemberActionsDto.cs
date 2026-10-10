namespace VeggieLife.BLL.DTOs.Posts.Responses;

/// <summary>
/// Quyền tương tác của người xem đối với bài viết (FN-POST-06, BR-AUTH-03).
/// Khách vãng lai: canVote = false, canComment = false.
/// Thành viên đã đăng nhập: canVote = true, canComment = true.
/// </summary>
public class PostMemberActionsDto
{
    /// <summary>
    /// Cho phép thả tim / bình chọn bài viết.
    /// </summary>
    public bool CanVote { get; set; }

    /// <summary>
    /// Cho phép bình luận trên bài viết.
    /// </summary>
    public bool CanComment { get; set; }
}
