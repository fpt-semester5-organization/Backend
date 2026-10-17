using System;
using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Community.Responses;

/// <summary>
/// Dữ liệu chi tiết của bình luận dạng phân cấp cây (FN-COMM-01, FN-COMM-02, FN-COMM-03).
/// </summary>
public class CommentDto
{
    /// <summary>
    /// Mã định danh bình luận.
    /// </summary>
    public int CommentId { get; set; }

    /// <summary>
    /// Thông tin tác giả bình luận.
    /// </summary>
    public CommentAuthorDto Author { get; set; } = new();

    /// <summary>
    /// Nội dung bình luận.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Mã bình luận cha (null nếu là bình luận gốc cấp 1).
    /// </summary>
    public int? ParentCommentId { get; set; }

    /// <summary>
    /// Trạng thái kiểm duyệt của bình luận: ACTIVE, FLAGGED, QUARANTINED_HIDDEN.
    /// </summary>
    public string Status { get; set; } = "ACTIVE";

    /// <summary>
    /// Thời điểm khởi tạo bình luận.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Danh sách các phản hồi trực tiếp bên dưới (Cấp 2).
    /// </summary>
    public List<CommentDto> Replies { get; set; } = new();
}
