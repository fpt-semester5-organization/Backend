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
/// Triá»ƒn khai dá»‹ch vá»¥ phÃ¢n tÃ¡n Redis thÃ´ng qua IDistributedCache káº¿t há»£p IConnectionMultiplexer vÃ  In-Memory fallback
/// Ä‘áº£m báº£o tÃ­nh nguyÃªn tá»­ tuyá»‡t Ä‘á»‘i (Atomic Concurrency) cho cÃ¡c bá»™ Ä‘áº¿m háº¡n má»©c vÃ  Rate Limiting.
/// </summary>
public class RedisService : IRedisService
{
    private readonly IDistributedCache _cache;
    private readonly IMemoryCache? _memoryCache;
    private readonly IConnectionMultiplexer? _multiplexer;
    private readonly ILogger<RedisService> _logger;
    private static readonly object _syncLock = new();
    private static readonly ConcurrentDictionary<string, ConcurrentQueue<string>> _memoryQueues = new();
    private static readonly ConcurrentDictionary<string, byte> _memoryKeys = new();
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
            _logger.LogWarning("Redis thao tÃ¡c '{Operation}' tháº¥t báº¡i ({Message}). Chuyá»ƒn sang In-Memory Fallback Cache trong 30 giÃ¢y.", operation, ex.Message);
        }
        else
        {
            _logger.LogWarning("Redis táº¡m thá»i khÃ´ng pháº£n há»“i ({Message}). Tá»± Ä‘á»™ng chuyá»ƒn sang In-Memory Fallback Cache trong 30 giÃ¢y.", ex.Message);
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

        // Fallback Ä‘á»c tá»« In-Memory Cache cá»¥c bá»™
        if (_memoryCache != null && _memoryCache.TryGetValue(key, out T? memoryValue))
        {
            return memoryValue;
        }

        return default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        // 1. LuÃ´n ghi vÃ o bá»™ nhá»› Ä‘á»‡m cá»¥c bá»™ In-Memory Ä‘á»ƒ dá»± phÃ²ng vÃ  tÄƒng tá»‘c
        if (_memoryCache != null)
        {
            var memOptions = new MemoryCacheEntryOptions();
            if (expiry.HasValue)
            {
                memOptions.AbsoluteExpirationRelativeToNow = expiry;
            }
            _memoryCache.Set(key, value, memOptions);
            _memoryKeys[key] = 0;
        }

        // 2. Ghi vÃ o Redis náº¿u khÃ´ng trong thá»i gian cooldown
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
            if (key.Contains('*'))
            {
                // Khóa theo mẫu (vd post_list:*): gỡ mọi khóa In-Memory có cùng tiền tố
                var prefix = key[..key.IndexOf('*')];
                foreach (var memoryKey in _memoryKeys.Keys)
                {
                    if (memoryKey.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        _memoryCache.Remove(memoryKey);
                        _memoryKeys.TryRemove(memoryKey, out _);
                    }
                }
            }
            else
            {
                _memoryCache.Remove(key);
                _memoryKeys.TryRemove(key, out _);
            }
        }

        if (!IsInCooldown)
        {
            try
            {
                if (key.Contains('*') && _multiplexer != null && _multiplexer.IsConnected)
                {
                    try
                    {
                        var endpoints = _multiplexer.GetEndPoints();
                        foreach (var ep in endpoints)
                        {
                            var server = _multiplexer.GetServer(ep);
                            if (server.IsConnected)
                            {
                                var keys = System.Linq.Enumerable.ToArray(server.Keys(pattern: "*" + key));
                                if (keys.Length > 0)
                                {
                                    var db = _multiplexer.GetDatabase();
                                    await db.KeyDeleteAsync(keys);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Lá»—i khi xÃ³a khÃ³a Redis theo máº«u {Pattern}", key);
                    }
                }
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

        // In-Memory Thread-Safe Fallback: Ä‘áº£m báº£o 100% khÃ´ng bá»‹ Ä‘ua lá»‡nh khi cháº¡y test hoáº·c khÃ´ng cÃ³ Redis
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

    public async Task EnqueueAsync<T>(string queueName, T item, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(item);
        if (_multiplexer != null && _multiplexer.IsConnected && !IsInCooldown)
        {
            try
            {
                var db = _multiplexer.GetDatabase();
                await db.ListLeftPushAsync(queueName, json);
                _logger.LogInformation("ÄÃ£ Ä‘áº©y tÃ¡c vá»¥ vÃ o hÃ ng Ä‘á»£i Redis [{QueueName}]: {Payload}", queueName, json);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lá»—i khi Ä‘áº©y tÃ¡c vá»¥ vÃ o hÃ ng Ä‘á»£i Redis [{QueueName}], chuyá»ƒn sang hÃ ng Ä‘á»£i bá»™ nhá»›", queueName);
                TriggerCooldown(ex, $"EnqueueAsync({queueName})");
            }
        }

        // Fallback In-Memory Queue (khi khÃ´ng cÃ³ káº¿t ná»‘i Redis)
        var queue = _memoryQueues.GetOrAdd(queueName, _ => new ConcurrentQueue<string>());
        queue.Enqueue(json);
        _logger.LogInformation("ÄÃ£ Ä‘áº©y tÃ¡c vá»¥ vÃ o hÃ ng Ä‘á»£i In-Memory [{QueueName}]: {Payload}", queueName, json);
    }

    public async Task<T?> DequeueAsync<T>(string queueName, CancellationToken ct = default)
    {
        if (_multiplexer != null && _multiplexer.IsConnected && !IsInCooldown)
        {
            try
            {
                var db = _multiplexer.GetDatabase();
                var value = await db.ListRightPopAsync(queueName);
                if (value.HasValue)
                {
                    return JsonSerializer.Deserialize<T>(value.ToString()!);
                }
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lá»—i khi láº¥y tÃ¡c vá»¥ tá»« hÃ ng Ä‘á»£i Redis [{QueueName}], thá»­ hÃ ng Ä‘á»£i bá»™ nhá»›", queueName);
                TriggerCooldown(ex, $"DequeueAsync({queueName})");
            }
        }

        // Fallback In-Memory Queue
        if (_memoryQueues.TryGetValue(queueName, out var memQueue) && memQueue.TryDequeue(out var memJson))
        {
            return JsonSerializer.Deserialize<T>(memJson);
        }

        return default;
    }
}
