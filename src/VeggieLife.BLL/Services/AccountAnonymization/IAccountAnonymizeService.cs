using System.Threading;
using System.Threading.Tasks;

namespace VeggieLife.BLL.Services.AccountAnonymization;

/// <summary>
/// Dịch vụ quét và ẩn danh hóa dữ liệu cá nhân các tài khoản đã quá hạn ân hạn 30 ngày (FN-PROF-07, BR-ADM-USR-003, BR-ADM-USR-004, task S7-K7).
/// </summary>
public interface IAccountAnonymizeService
{
    /// <summary>
    /// Thực hiện quét và ẩn danh hóa các tài khoản có trạng thái SOFT_DELETED quá 30 ngày.
    /// Chạy lặp lại an toàn (Idempotent).
    /// </summary>
    /// <param name="ct">Cancellation Token.</param>
    /// <returns>Số lượng tài khoản đã được ẩn danh hóa thành công.</returns>
    Task<int> AnonymizeOverdueSoftDeletedAccountsAsync(CancellationToken ct = default);
}
