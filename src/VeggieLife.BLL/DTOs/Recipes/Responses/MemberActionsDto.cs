namespace VeggieLife.BLL.DTOs.Recipes.Responses;

/// <summary>
/// Quyền tương tác cộng đồng của người dùng đối với bài viết công thức (BR-AUTH-03, FN-POST-02).
/// Khách vãng lai (GUEST) toàn bộ false; Thành viên đăng nhập (MEMBER/VIP/ADMIN) toàn bộ true.
/// </summary>
public class MemberActionsDto
{
    /// <summary>
    /// Cho phép thả tim / bình chọn công thức.
    /// </summary>
    public bool CanVote { get; set; }

    /// <summary>
    /// Cho phép viết bình luận cho bài viết công thức.
    /// </summary>
    public bool CanComment { get; set; }

    /// <summary>
    /// Cho phép báo cáo bài viết vi phạm.
    /// </summary>
    public bool CanReport { get; set; }
}
