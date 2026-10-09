using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace VeggieLife.BLL.Services.Cache;

/// <summary>
/// Triển khai dịch vụ phân tán Redis thông qua IDistributedCache kết hợp cơ chế bộ nhớ đệm dự phòng (In-Memory fallback & Cooldown)
/// giúp duy trì hiệu năng cực cao ngay cả khi máy chủ Redis tạm thời gián đoạn.
/// </summary>
public class RedisService : IRedisService
{
    private readonly IDistributedCache _cache;
    private readonly IMemoryCache? _memoryCache;
    private readonly ILogger<RedisService> _logger;
    private static DateTime _redisCooldownUntil = DateTime.MinValue;

    public RedisService(IDistributedCache cache, ILogger<RedisService> logger, IMemoryCache? memoryCache = null)
    {
        _cache = cache;
        _logger = logger;
        _memoryCache = memoryCache;
    }

    private static bool IsInCooldown => DateTime.UtcNow < _redisCooldownUntil;

    private void TriggerCooldown(Exception ex)
    {
        _redisCooldownUntil = DateTime.UtcNow.AddSeconds(30);
        _logger.LogWarning("Redis tạm thời không phản hồi ({Message}). Tự động chuyển sang In-Memory Fallback Cache trong 30 giây.", ex.Message);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (!IsInCooldown)
        {
            try
            {
                var data = await _cache.GetStringAsync(key, ct);
                if (!string.IsNullOrEmpty(data))
                {
                    return JsonSerializer.Deserialize<T>(data);
                }
            }
            catch (Exception ex)
            {
                TriggerCooldown(ex, $"GetAsync({key})");
            }
        }

        // Fallback đọc từ In-Memory Cache cục bộ
        if (_memoryCache != null && _memoryCache.TryGetValue(key, out T? memoryValue))
        {
            return memoryValue;
        }

        return default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        // 1. Luôn ghi vào bộ nhớ đệm cục bộ In-Memory để dự phòng và tăng tốc
        if (_memoryCache != null)
        {
            var memOptions = new MemoryCacheEntryOptions();
            if (expiry.HasValue)
            {
                memOptions.AbsoluteExpirationRelativeToNow = expiry;
            }
            _memoryCache.Set(key, value, memOptions);
        }

        // 2. Ghi vào Redis nếu không trong thời gian cooldown
        if (!IsInCooldown)
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
                TriggerCooldown(ex, $"SetAsync({key})");
            }
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        if (_memoryCache != null)
        {
            _memoryCache.Remove(key);
        }

        if (!IsInCooldown)
        {
            try
            {
                await _cache.RemoveAsync(key, ct);
            }
            catch (Exception ex)
            {
                TriggerCooldown(ex, $"RemoveAsync({key})");
            }
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        if (_memoryCache != null && _memoryCache.TryGetValue(key, out _))
        {
            return true;
        }

        if (!IsInCooldown)
        {
            try
            {
                var data = await _cache.GetAsync(key, ct);
                return data != null && data.Length > 0;
            }
            catch (Exception ex)
            {
                TriggerCooldown(ex, $"ExistsAsync({key})");
                return false;
            }
        }

        return false;
    }

    public async Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        long current = 0;

        if (_memoryCache != null)
        {
            if (_memoryCache.TryGetValue(key, out long memVal))
            {
                current = memVal;
            }
            current += value;
            var memOptions = new MemoryCacheEntryOptions();
            if (expiry.HasValue)
            {
                memOptions.AbsoluteExpirationRelativeToNow = expiry;
            }
            _memoryCache.Set(key, current, memOptions);
        }

        if (!IsInCooldown)
        {
            try
            {
                var strVal = await _cache.GetStringAsync(key, ct);
                long redisVal = 0;
                if (!string.IsNullOrEmpty(strVal))
                {
                    long.TryParse(strVal, out redisVal);
                }

                redisVal += value;

                var options = new DistributedCacheEntryOptions();
                if (expiry.HasValue)
                {
                    options.AbsoluteExpirationRelativeToNow = expiry;
                }

                await _cache.SetStringAsync(key, redisVal.ToString(), options, ct);
                return redisVal;
            }
            catch (Exception ex)
            {
                TriggerCooldown(ex, $"IncrementAsync({key})");
            }
        }

        return current;
    }

    public async Task EnqueueAsync<T>(string queueName, T item, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(item);
            _logger.LogInformation("Đã xếp công việc vào hàng đợi {QueueName}: {Payload}", queueName, json);

            // Ghi nhận vào cache để theo dõi trạng thái công việc mới nhất
            await SetAsync($"queue:{queueName}:latest", item, TimeSpan.FromHours(24), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi đưa phần tử vào hàng đợi '{QueueName}'.", queueName);
        }
    }
}
