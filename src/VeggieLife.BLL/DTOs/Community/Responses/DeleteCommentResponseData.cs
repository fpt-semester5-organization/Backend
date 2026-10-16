namespace VeggieLife.BLL.DTOs.Community.Responses;

/// <summary>
/// Dữ liệu phản hồi sau khi xóa mềm bình luận (FN-COMM-03, BR-ADM-CMT-001).
/// </summary>
public class DeleteCommentResponseData
{
    /// <summary>
    /// Số lượng bình luận phản hồi con bị ẩn đồng thời theo cơ chế Cascade Soft-Delete.
    /// </summary>
    public int HiddenReplies { get; set; }
}
