using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Community.Requests;
using VeggieLife.BLL.DTOs.Community.Responses;

namespace VeggieLife.BLL.Services.Community;

/// <summary>
/// Giao diện dịch vụ quản lý bình luận cộng đồng đa tầng dưới bài viết và công thức nấu ăn.
/// Triển khai các chức năng:
/// - FN-COMM-01: Xem bình luận dạng cây (tối đa 2 cấp hiển thị) của bài viết và công thức đã duyệt.
/// - FN-COMM-02: Bình luận mới hoặc trả lời bình luận, chống spam (15 giây cooldown, tối đa 5 bình luận trùng lặp - BR-USR-04), kiểm duyệt AI (BR-USR-03).
/// - FN-COMM-03: Sửa hoặc xóa mềm bình luận của mình, ẩn dây chuyền các bình luận con (Cascade Soft-Delete - BR-ADM-CMT-001).
/// </summary>
public interface ICommentService
{
    /// <summary>
    /// FN-COMM-01: Lấy danh sách bình luận dạng cây theo phân trang của bài viết (chỉ bài APPROVED_ACTIVE).
    /// </summary>
    /// <param name="postId">Mã bài viết (> 0).</param>
    /// <param name="page">Số trang (>= 1, mặc định 1).</param>
    /// <param name="size">Kích thước trang (1-50, mặc định 20).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Danh sách bình luận gốc kèm các phản hồi con và thông tin phân trang.</returns>
    Task<(IReadOnlyList<CommentDto> Items, PagedMeta Meta)> ListPostCommentsAsync(
        int postId,
        int page = 1,
        int size = 20,
        CancellationToken ct = default);

    /// <summary>
    /// FN-COMM-01: Lấy danh sách bình luận dạng cây theo phân trang của công thức nấu ăn (qua bài viết đại diện).
    /// </summary>
    /// <param name="recipeId">Mã công thức (> 0).</param>
    /// <param name="page">Số trang (>= 1, mặc định 1).</param>
    /// <param name="size">Kích thước trang (1-50, mặc định 20).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Danh sách bình luận gốc kèm các phản hồi con và thông tin phân trang.</returns>
    Task<(IReadOnlyList<CommentDto> Items, PagedMeta Meta)> ListRecipeCommentsAsync(
        int recipeId,
        int page = 1,
        int size = 20,
        CancellationToken ct = default);

    /// <summary>
    /// FN-COMM-02: Viết bình luận mới hoặc trả lời bình luận trên bài viết chia sẻ.
    /// </summary>
    /// <param name="postId">Mã bài viết (> 0).</param>
    /// <param name="request">Nội dung bình luận và mã bình luận cha (nếu có).</param>
    /// <param name="userId">Mã thành viên gửi bình luận.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Thông tin bình luận vừa tạo.</returns>
    Task<CommentDto> CreatePostCommentAsync(
        int postId,
        CreateCommentRequest request,
        int userId,
        CancellationToken ct = default);

    /// <summary>
    /// FN-COMM-02: Viết bình luận mới hoặc trả lời bình luận trên công thức nấu ăn.
    /// </summary>
    /// <param name="recipeId">Mã công thức (> 0).</param>
    /// <param name="request">Nội dung bình luận và mã bình luận cha (nếu có).</param>
    /// <param name="userId">Mã thành viên gửi bình luận.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Thông tin bình luận vừa tạo.</returns>
    Task<CommentDto> CreateRecipeCommentAsync(
        int recipeId,
        CreateCommentRequest request,
        int userId,
        CancellationToken ct = default);

    /// <summary>
    /// FN-COMM-03: Sửa nội dung bình luận của chính mình.
    /// </summary>
    /// <param name="commentId">Mã bình luận (> 0).</param>
    /// <param name="request">Nội dung bình luận mới (2-500 ký tự).</param>
    /// <param name="userId">Mã thành viên thực hiện sửa.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Thông tin bình luận sau khi cập nhật.</returns>
    Task<CommentDto> UpdateCommentAsync(
        int commentId,
        UpdateCommentRequest request,
        int userId,
        CancellationToken ct = default);

    /// <summary>
    /// FN-COMM-03: Xóa mềm bình luận của chính mình và ẩn đệ quy toàn bộ cây phản hồi con (Cascade Soft-Delete).
    /// </summary>
    /// <param name="commentId">Mã bình luận (> 0).</param>
    /// <param name="userId">Mã thành viên thực hiện xóa.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Số lượng bình luận phản hồi con bị ẩn kèm theo.</returns>
    Task<DeleteCommentResponseData> DeleteCommentAsync(
        int commentId,
        int userId,
        CancellationToken ct = default);
}
