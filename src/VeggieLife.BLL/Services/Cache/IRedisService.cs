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
}
