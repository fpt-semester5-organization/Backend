using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.DTOs.Community.Requests;
using VeggieLife.BLL.DTOs.Community.Responses;

namespace VeggieLife.BLL.Services.Community;

/// <summary>
/// Giao diện dịch vụ tương tác cộng đồng: Thả tim/bỏ tim bài viết, công thức và báo cáo vi phạm nội dung (FN-COMM-04, FN-COMM-05).
/// Tuân thủ các quy tắc nghiệp vụ: BR-INT-LIKE-01, BR-INT-LIKE-02, BR-AUTH-03, BR-USR-04, BR-INT-REPORT-01, BR-ADM-CNT-001, BR-ADM-CMT-001.
/// </summary>
public interface ICommunityInteractionService
{
    /// <summary>
    /// FN-COMM-04: Thả tim hoặc bỏ tim cho bài viết (mỗi thành viên tối đa 1 tim, thả lần 2 = bỏ tim).
    /// </summary>
    /// <param name="postId">Mã định danh bài viết (> 0).</param>
    /// <param name="userId">Mã định danh thành viên thực hiện.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Trạng thái voted và tổng số vote mới nhất của bài viết.</returns>
    Task<VoteResponseData> VotePostAsync(int postId, int userId, CancellationToken ct = default);

    /// <summary>
    /// FN-COMM-04: Thả tim hoặc bỏ tim cho công thức nấu ăn (tính qua bài viết đại diện của công thức).
    /// </summary>
    /// <param name="recipeId">Mã định danh công thức (> 0).</param>
    /// <param name="userId">Mã định danh thành viên thực hiện.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Trạng thái voted và tổng số vote mới nhất của công thức.</returns>
    Task<VoteResponseData> VoteRecipeAsync(int recipeId, int userId, CancellationToken ct = default);

    /// <summary>
    /// FN-COMM-05: Gửi báo cáo vi phạm nội dung (bài viết, công thức, video hoặc bình luận).
    /// Giới hạn tối đa 10 báo cáo/ngày/tài khoản, từ chối báo cáo trùng lặp, ẩn ngay nội dung bị báo cáo.
    /// </summary>
    /// <param name="request">Thông tin phiếu báo cáo vi phạm.</param>
    /// <param name="userId">Mã định danh thành viên gửi báo cáo.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Mã báo cáo và trạng thái PENDING ban đầu.</returns>
    Task<ReportResponseData> CreateReportAsync(CreateReportRequest request, int userId, CancellationToken ct = default);
}

/// <summary>
/// Giao diện tương đương (Alias) theo đặc tả kỹ thuật Sprint Plan / API Specification (FN-COMM-04, FN-COMM-05).
/// </summary>
public interface IInteractionService : ICommunityInteractionService
{
}
