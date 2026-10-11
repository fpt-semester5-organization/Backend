using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Posts.Requests;
using VeggieLife.BLL.DTOs.Posts.Responses;

namespace VeggieLife.BLL.Services.Posts;

/// <summary>
/// Interface dịch vụ quản lý và tra cứu bài viết cộng đồng (FN-POST-05, FN-POST-06).
/// </summary>
public interface IPostService
{
    /// <summary>
    /// Tìm kiếm và xem danh sách bài viết chia sẻ đã duyệt, có phân trang, lọc theo từ khóa và tác giả (FN-POST-05).
    /// </summary>
    /// <param name="filter">Bộ lọc tìm kiếm và phân trang.</param>
    /// <param name="isGuest">Cờ xác định người xem là khách vãng lai hay thành viên đã đăng nhập.</param>
    /// <param name="guestId">Mã định danh khách vãng lai (Guest_UUID) dùng lưu lịch sử tìm kiếm vào Redis.</param>
    /// <param name="currentUserId">Mã người dùng hiện tại (nếu đã đăng nhập, cho phép tác giả xem bài của chính mình).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Danh sách bài viết tóm tắt kèm metadata phân trang.</returns>
    Task<(IReadOnlyList<PostSummaryDto> Items, PagedMeta Meta)> ListPostsAsync(
        PostFilterRequest filter,
        bool isGuest,
        string? guestId,
        int? currentUserId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Xem chi tiết đầy đủ một bài viết, ảnh, video và công thức đính kèm; tăng lượt xem View_Count (FN-POST-06).
    /// </summary>
    /// <param name="id">Mã định danh bài viết (PostId > 0).</param>
    /// <param name="isGuest">Cờ xác định người xem là khách vãng lai hay thành viên đã đăng nhập.</param>
    /// <param name="currentUserId">Mã người dùng hiện tại (nếu đã đăng nhập, cho phép tác giả xem bài của chính mình).</param>
    /// <param name="ct">CancellationToken.</param>
    Task<PostDetailDto> GetPostByIdAsync(
        int id,
        bool isGuest,
        int? currentUserId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Đăng bài viết chia sẻ mới (lưu nháp hoặc gửi kiểm duyệt AI) (FN-POST-07).
    /// </summary>
    /// <param name="request">Thông tin bài viết cần đăng.</param>
    /// <param name="userId">Mã định danh tác giả.</param>
    /// <param name="userRole">Vai trò của tác giả (MEMBER, VIP, ADMIN).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Thông tin bài viết được tạo gồm postId và moderationStatus.</returns>
    Task<CreatePostResponseDto> CreatePostAsync(
        CreatePostRequest request,
        int userId,
        string userRole,
        CancellationToken ct = default);

    /// <summary>
    /// Cập nhật bài viết của mình, gỡ khỏi cộng đồng (chuyển PRIVATE) hoặc gửi duyệt lại (FN-POST-07).
    /// </summary>
    /// <param name="id">Mã định danh bài viết (> 0).</param>
    /// <param name="request">Thông tin cập nhật bài viết.</param>
    /// <param name="userId">Mã định danh người thực hiện (phải là tác giả).</param>
    /// <param name="userRole">Vai trò của người thực hiện.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Thông tin bài viết sau khi cập nhật gồm postId và moderationStatus.</returns>
    Task<UpdatePostResponseDto> UpdatePostAsync(
        int id,
        UpdatePostRequest request,
        int userId,
        string userRole,
        CancellationToken ct = default);

    /// <summary>
    /// Xóa mềm bài viết của mình (chuyển trạng thái sang DELETED) (FN-POST-07).
    /// </summary>
    /// <param name="id">Mã định danh bài viết (> 0).</param>
    /// <param name="userId">Mã định danh người thực hiện (phải là tác giả).</param>
    /// <param name="userRole">Vai trò của người thực hiện.</param>
    /// <param name="ct">CancellationToken.</param>
    Task DeletePostAsync(
        int id,
        int userId,
        string userRole,
        CancellationToken ct = default);
}

