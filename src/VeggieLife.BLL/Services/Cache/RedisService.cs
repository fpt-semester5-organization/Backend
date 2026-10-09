using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace VeggieLife.BLL.Services.Cache;

/// <summary>
/// Triển khai dịch vụ phân tán Redis thông qua IDistributedCache kết hợp cơ chế bộ nhớ đệm dự phòng cục bộ (In-Memory Fallback)
/// giúp hệ thống duy trì hoạt động thông suốt ngay cả khi cụm Redis gặp sự cố hoặc môi trường phát triển chưa bật Redis daemon.
/// </summary>
public class RedisService : IRedisService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisService> _logger;
    private readonly ConcurrentDictionary<string, MemoryCacheEntry> _fallbackStore = new();

    private record MemoryCacheEntry(string JsonValue, DateTimeOffset? ExpiresAt);

    private bool _redisAvailable = true;
    private DateTimeOffset _lastRedisFailure = DateTimeOffset.MinValue;
    private static readonly TimeSpan RedisRetryInterval = TimeSpan.FromSeconds(30);

    public RedisService(IDistributedCache cache, ILogger<RedisService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    private bool ShouldTryRedis()
    {
        if (_redisAvailable) return true;
        if (DateTimeOffset.UtcNow - _lastRedisFailure > RedisRetryInterval)
        {
            _redisAvailable = true;
            return true;
        }
        return false;
    }

    private void MarkRedisFailure(Exception ex, string operation)
    {
        _redisAvailable = false;
        _lastRedisFailure = DateTimeOffset.UtcNow;
        _logger.LogWarning(ex, "Redis kết nối thất bại khi thực hiện thao tác '{Operation}'. Tự động chuyển sang In-Memory Fallback trong 30 giây.", operation);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (ShouldTryRedis())
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
                MarkRedisFailure(ex, $"GetAsync({key})");
            }
        }

        // Fallback sang bộ nhớ RAM cục bộ
        if (_fallbackStore.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt.HasValue && entry.ExpiresAt.Value <= DateTimeOffset.UtcNow)
            {
                _fallbackStore.TryRemove(key, out _);
                return default;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(entry.JsonValue);
            }
            catch
            {
                return default;
            }
        }

        return default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value);
        var expiresAt = expiry.HasValue ? DateTimeOffset.UtcNow.Add(expiry.Value) : (DateTimeOffset?)null;

        // Luôn ghi nhận vào in-memory fallback
        _fallbackStore[key] = new MemoryCacheEntry(json, expiresAt);

        if (ShouldTryRedis())
        {
            try
            {
                var options = new DistributedCacheEntryOptions();
                if (expiry.HasValue)
                {
                    options.AbsoluteExpirationRelativeToNow = expiry;
                }

                await _cache.SetStringAsync(key, json, options, ct);
            }
            catch (Exception ex)
            {
                MarkRedisFailure(ex, $"SetAsync({key})");
            }
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _fallbackStore.TryRemove(key, out _);

        if (ShouldTryRedis())
        {
            try
            {
                await _cache.RemoveAsync(key, ct);
            }
            catch (Exception ex)
            {
                MarkRedisFailure(ex, $"RemoveAsync({key})");
            }
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        if (ShouldTryRedis())
        {
            try
            {
                var data = await _cache.GetAsync(key, ct);
                if (data != null && data.Length > 0) return true;
            }
            catch (Exception ex)
            {
                MarkRedisFailure(ex, $"ExistsAsync({key})");
            }
        }

        if (_fallbackStore.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt.HasValue && entry.ExpiresAt.Value <= DateTimeOffset.UtcNow)
            {
                _fallbackStore.TryRemove(key, out _);
                return false;
            }
            return true;
        }

        return false;
    }

    public async Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        long inMemoryResult = 0;
        _fallbackStore.AddOrUpdate(key,
            _ =>
            {
                inMemoryResult = value;
                var exp = expiry.HasValue ? DateTimeOffset.UtcNow.Add(expiry.Value) : (DateTimeOffset?)null;
                return new MemoryCacheEntry(inMemoryResult.ToString(), exp);
            },
            (_, existing) =>
            {
                long current = 0;
                long.TryParse(existing.JsonValue, out current);
                inMemoryResult = current + value;
                var exp = expiry.HasValue ? DateTimeOffset.UtcNow.Add(expiry.Value) : existing.ExpiresAt;
                return new MemoryCacheEntry(inMemoryResult.ToString(), exp);
            });

        if (ShouldTryRedis())
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
                MarkRedisFailure(ex, $"IncrementAsync({key})");
            }
        }

        return inMemoryResult;
    }
}
