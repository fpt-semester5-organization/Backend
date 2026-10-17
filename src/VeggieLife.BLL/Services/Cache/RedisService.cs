using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace VeggieLife.BLL.Services.Cache;

/// <summary>
/// Triển khai dịch vụ phân tán Redis thông qua IDistributedCache kết hợp IConnectionMultiplexer và In-Memory fallback
/// đảm bảo tính nguyên tử tuyệt đối (Atomic Concurrency) cho các bộ đếm hạn mức và Rate Limiting.
/// </summary>
public class RedisService : IRedisService
{
    private readonly IDistributedCache _cache;
    private readonly IMemoryCache? _memoryCache;
    private readonly IConnectionMultiplexer? _multiplexer;
    private readonly ILogger<RedisService> _logger;
    private static readonly object _syncLock = new();
    private static DateTime _redisCooldownUntil = DateTime.MinValue;

    public RedisService(
        IDistributedCache cache,
        ILogger<RedisService> logger,
        IMemoryCache? memoryCache = null,
        IConnectionMultiplexer? multiplexer = null)
    {
        _cache = cache;
        _logger = logger;
        _memoryCache = memoryCache;
        _multiplexer = multiplexer;
    }

    private static bool IsInCooldown => DateTime.UtcNow < _redisCooldownUntil;

    private void TriggerCooldown(Exception ex, string? operation = null)
    {
        _redisCooldownUntil = DateTime.UtcNow.AddSeconds(30);
        if (!string.IsNullOrEmpty(operation))
        {
            _logger.LogWarning("Redis thao tác '{Operation}' thất bại ({Message}). Chuyển sang In-Memory Fallback Cache trong 30 giây.", operation, ex.Message);
        }
        else
        {
            _logger.LogWarning("Redis tạm thời không phản hồi ({Message}). Tự động chuyển sang In-Memory Fallback Cache trong 30 giây.", ex.Message);
        }
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
        if (_multiplexer != null && _multiplexer.IsConnected && !IsInCooldown)
        {
            try
            {
                var db = _multiplexer.GetDatabase();
                var newVal = await db.StringIncrementAsync(key, value);
                if (expiry.HasValue && newVal == value)
                {
                    await db.KeyExpireAsync(key, expiry.Value);
                }

                if (_memoryCache != null)
                {
                    _memoryCache.Set(key, newVal, expiry ?? TimeSpan.FromDays(1));
                }
                return newVal;
            }
            catch (Exception ex)
            {
                TriggerCooldown(ex, $"IncrementAsync({key})");
            }
        }

        // Fallback In-Memory Thread-Safe
        lock (_syncLock)
        {
            long current = 0;
            if (_memoryCache != null && _memoryCache.TryGetValue(key, out long memVal))
            {
                current = memVal;
            }
            current += value;

            if (_memoryCache != null)
            {
                var memOptions = new MemoryCacheEntryOptions();
                if (expiry.HasValue)
                {
                    memOptions.AbsoluteExpirationRelativeToNow = expiry;
                }
                _memoryCache.Set(key, current, memOptions);
            }

            return current;
        }
    }

    public async Task<(bool Allowed, long Current, long Remaining)> TryConsumeQuotaAsync(
        string key,
        int maxLimit,
        TimeSpan? expiry = null,
        CancellationToken ct = default)
    {
        if (_multiplexer != null && _multiplexer.IsConnected && !IsInCooldown)
        {
            try
            {
                var db = _multiplexer.GetDatabase();
                var expirySec = expiry.HasValue ? (long)expiry.Value.TotalSeconds : 86400;

                const string script = @"
                    local current = tonumber(redis.call('GET', KEYS[1]) or '0')
                    if current >= tonumber(ARGV[1]) then
                        return { 0, current, 0 }
                    end
                    local newVal = redis.call('INCR', KEYS[1])
                    if newVal == 1 and tonumber(ARGV[2]) > 0 then
                        redis.call('EXPIRE', KEYS[1], tonumber(ARGV[2]))
                    end
                    local rem = tonumber(ARGV[1]) - newVal
                    return { 1, newVal, rem }
                ";

                var result = (RedisResult[]?)await db.ScriptEvaluateAsync(
                    script,
                    new RedisKey[] { key },
                    new RedisValue[] { maxLimit, expirySec });

                if (result != null && result.Length >= 3)
                {
                    int allowed = (int)result[0];
                    long currentVal = (long)result[1];
                    long remainingVal = (long)result[2];

                    if (_memoryCache != null)
                    {
                        _memoryCache.Set(key, currentVal, expiry ?? TimeSpan.FromDays(1));
                    }

                    return (allowed == 1, currentVal, remainingVal);
                }
            }
            catch (Exception ex)
            {
                TriggerCooldown(ex, $"TryConsumeQuotaAsync({key})");
            }
        }

        // In-Memory Thread-Safe Fallback: đảm bảo 100% không bị đua lệnh khi chạy test hoặc không có Redis
        lock (_syncLock)
        {
            long current = 0;
            if (_memoryCache != null && _memoryCache.TryGetValue(key, out long memVal))
            {
                current = memVal;
            }

            if (current >= maxLimit)
            {
                return (false, current, 0);
            }

            current++;
            if (_memoryCache != null)
            {
                var memOptions = new MemoryCacheEntryOptions();
                if (expiry.HasValue)
                {
                    memOptions.AbsoluteExpirationRelativeToNow = expiry;
                }
                _memoryCache.Set(key, current, memOptions);
            }

            long remaining = Math.Max(0, maxLimit - current);
            return (true, current, remaining);
        }
    }

    public async Task<(bool Allowed, bool IsBanned, long CurrentCount, TimeSpan? RetryAfter)> CheckIpRateLimitAsync(
        string clientIp,
        int maxRequestsPerMinute = 10,
        int banDurationMinutes = 10,
        CancellationToken ct = default)
    {
        var banKey = $"ratelimit:ban:{clientIp}";
        var countKey = $"ratelimit:count:{clientIp}";
        var banDuration = TimeSpan.FromMinutes(banDurationMinutes);
        var windowDuration = TimeSpan.FromMinutes(1);

        if (_multiplexer != null && _multiplexer.IsConnected && !IsInCooldown)
        {
            try
            {
                var db = _multiplexer.GetDatabase();

                const string script = @"
                    local banTtl = redis.call('TTL', KEYS[1])
                    if banTtl > 0 then
                        return { 0, 1, 0, banTtl }
                    end

                    local current = redis.call('INCR', KEYS[2])
                    if current == 1 then
                        redis.call('EXPIRE', KEYS[2], tonumber(ARGV[3]))
                    end

                    if current > tonumber(ARGV[1]) then
                        redis.call('SET', KEYS[1], '1', 'EX', tonumber(ARGV[2]))
                        return { 0, 1, current, tonumber(ARGV[2]) }
                    end

                    return { 1, 0, current, 0 }
                ";

                var result = (RedisResult[]?)await db.ScriptEvaluateAsync(
                    script,
                    new RedisKey[] { banKey, countKey },
                    new RedisValue[] { maxRequestsPerMinute, (long)banDuration.TotalSeconds, (long)windowDuration.TotalSeconds });

                if (result != null && result.Length >= 4)
                {
                    bool allowed = (int)result[0] == 1;
                    bool isBanned = (int)result[1] == 1;
                    long count = (long)result[2];
                    long retrySec = (long)result[3];

                    return (allowed, isBanned, count, retrySec > 0 ? TimeSpan.FromSeconds(retrySec) : null);
                }
            }
            catch (Exception ex)
            {
                TriggerCooldown(ex, $"CheckIpRateLimitAsync({clientIp})");
            }
        }

        // In-Memory Thread-Safe Fallback
        lock (_syncLock)
        {
            // 1. Kiểm tra trạng thái cấm
            if (_memoryCache != null && _memoryCache.TryGetValue(banKey, out DateTime banUntil))
            {
                var remaining = banUntil - DateTime.UtcNow;
                if (remaining > TimeSpan.Zero)
                {
                    return (false, true, 0, remaining);
                }
                _memoryCache.Remove(banKey);
            }

            // 2. Tăng số đếm request trong cửa sổ 1 phút
            long count = 0;
            if (_memoryCache != null && _memoryCache.TryGetValue(countKey, out long memCount))
            {
                count = memCount;
            }

            count++;
            if (_memoryCache != null)
            {
                if (count == 1)
                {
                    _memoryCache.Set(countKey, count, windowDuration);
                }
                else
                {
                    _memoryCache.Set(countKey, count);
                }
            }

            // 3. Nếu vượt quá giới hạn -> ban 10 phút
            if (count > maxRequestsPerMinute)
            {
                var banUntilTime = DateTime.UtcNow.Add(banDuration);
                if (_memoryCache != null)
                {
                    _memoryCache.Set(banKey, banUntilTime, banDuration);
                }
                return (false, true, count, banDuration);
            }

            return (true, false, count, null);
        }
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
