using System;
using System.Threading;
using System.Threading.Tasks;

namespace VeggieLife.BLL.Services.Cache;

/// <summary>
/// Interface dịch vụ bộ nhớ đệm phân tán Redis (Rate Limiting, Cache thực đơn, Tra cứu hạn mức AI).
/// </summary>
public interface IRedisService
{
    /// <summary>
    /// Đọc đối tượng từ Redis theo khóa key.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    /// <summary>
    /// Lưu trữ đối tượng vào Redis với thời gian sống (TTL) tùy chọn.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);

    /// <summary>
    /// Xóa một khóa khỏi Redis.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Kiểm tra sự tồn tại của khóa trong Redis.
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Tăng giá trị số nguyên nguyên tử trên Redis (phục vụ bộ đếm Quota, Rate Limit).
    /// </summary>
    Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null, CancellationToken ct = default);

    /// <summary>
    /// Kiểm tra và tiêu hao hạn ngạch dùng thử nguyên tử (Atomic Quota Consumption) chống đua lệnh (Race Condition).
    /// </summary>
    /// <param name="key">Khóa Redis đại diện cho bộ đếm hạn ngạch.</param>
    /// <param name="maxLimit">Giới hạn tối đa cho phép.</param>
    /// <param name="expiry">Thời gian sống của khóa.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Bộ tuple (Allowed: có được phép tiếp tục hay không, Current: số lượt đã dùng sau khi tăng, Remaining: số lượt còn lại).</returns>
    Task<(bool Allowed, long Current, long Remaining)> TryConsumeQuotaAsync(string key, int maxLimit, TimeSpan? expiry = null, CancellationToken ct = default);

    /// <summary>
    /// Kiểm tra Rate Limit theo địa chỉ IP và thực hiện tạm khóa (Ban) 10 phút nếu vượt quá 10 req/phút (BR-GUEST-SEC-01).
    /// </summary>
    /// <param name="clientIp">Địa chỉ IP của người dùng.</param>
    /// <param name="maxRequestsPerMinute">Số yêu cầu tối đa mỗi phút (mặc định: 10).</param>
    /// <param name="banDurationMinutes">Thời gian tạm khóa (phút, mặc định: 10).</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Bộ tuple (Allowed: có được phép, IsBanned: đã bị khóa hay chưa, CurrentCount: số request hiện tại, RetryAfter: thời gian còn lại trước khi hết khóa).</returns>
    Task<(bool Allowed, bool IsBanned, long CurrentCount, TimeSpan? RetryAfter)> CheckIpRateLimitAsync(string clientIp, int maxRequestsPerMinute = 10, int banDurationMinutes = 10, CancellationToken ct = default);
}
