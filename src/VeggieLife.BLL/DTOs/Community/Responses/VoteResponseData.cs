namespace VeggieLife.BLL.DTOs.Community.Responses;

/// <summary>
/// Dữ liệu phản hồi sau khi thả hoặc bỏ tim bài viết / công thức (FN-COMM-04).
/// </summary>
public class VoteResponseData
{
    /// <summary>
    /// Trạng thái thả tim hiện tại của người dùng đối với đối tượng: true nếu vừa thả tim, false nếu vừa bỏ tim.
    /// </summary>
    public bool Voted { get; set; }

    /// <summary>
    /// Tổng số lượt tim mới nhất của đối tượng sau thao tác.
    /// </summary>
    public int VoteCount { get; set; }
}
