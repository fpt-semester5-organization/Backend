using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace VeggieLife.BLL.Services.Cache;

/// <summary>
/// Triển khai dịch vụ phân tán Redis thông qua IDistributedCache kết hợp cơ chế xử lý lỗi an toàn (graceful fallback).
/// </summary>
public class RedisService : IRedisService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisService> _logger;

    public RedisService(IDistributedCache cache, ILogger<RedisService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var data = await _cache.GetStringAsync(key, ct);
            if (string.IsNullOrEmpty(data))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi lấy dữ liệu từ cache cho khóa '{Key}'. Trả về null.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        try
        {
            var options = new DistributedCacheEntryOptions();
            if (expiry.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiry;
            }

            var json = JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, json, options, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi ghi dữ liệu vào cache cho khóa '{Key}'.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _cache.RemoveAsync(key, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi xóa khóa '{Key}' khỏi cache.", key);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var data = await _cache.GetAsync(key, ct);
            return data != null && data.Length > 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi kiểm tra tồn tại khóa '{Key}' trong cache.", key);
            return false;
        }
    }

    public async Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        try
        {
            var strVal = await _cache.GetStringAsync(key, ct);
            long current = 0;
            if (!string.IsNullOrEmpty(strVal))
            {
                long.TryParse(strVal, out current);
            }

            current += value;

            var options = new DistributedCacheEntryOptions();
            if (expiry.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiry;
            }

            await _cache.SetStringAsync(key, current.ToString(), options, ct);
            return current;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi tăng giá trị khóa '{Key}' trong cache.", key);
            return 0;
        }
    }
}
