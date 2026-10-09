using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Settings;
using VeggieLife.BLL.Services.Auth;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Guest;
using Xunit;

namespace VeggieLife.Tests.Unit.Services;

/// <summary>
/// Bộ kiểm thử đơn vị cho dịch vụ GuestService: Quản lý phiên khách, đếm hạn mức nguyên tử và bảo vệ Rate Limit / Vùng địa lý.
/// </summary>
public class GuestServiceTests
{
    private class ThreadSafeTestRedisService : IRedisService
    {
        private readonly ConcurrentDictionary<string, object> _storage = new();
        private readonly object _lock = new();

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            if (_storage.TryGetValue(key, out var val))
            {
                if (val is T typedVal) return Task.FromResult<T?>(typedVal);
                if (typeof(T) == typeof(long?) && long.TryParse(val.ToString(), out var parsedLong))
                {
                    return Task.FromResult<T?>((T)(object)parsedLong);
                }
            }
            return Task.FromResult<T?>(default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
        {
            _storage[key] = value!;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            _storage.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        {
            return Task.FromResult(_storage.ContainsKey(key));
        }

        public Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null, CancellationToken ct = default)
        {
            lock (_lock)
            {
                long current = 0;
                if (_storage.TryGetValue(key, out var val) && long.TryParse(val.ToString(), out var parsed))
                {
                    current = parsed;
                }
                current += value;
                _storage[key] = current;
                return Task.FromResult(current);
            }
        }

        public Task<(bool Allowed, long Current, long Remaining)> TryConsumeQuotaAsync(
            string key, int maxLimit, TimeSpan? expiry = null, CancellationToken ct = default)
        {
            lock (_lock)
            {
                long current = 0;
                if (_storage.TryGetValue(key, out var val) && long.TryParse(val.ToString(), out var parsed))
                {
                    current = parsed;
                }

                if (current >= maxLimit)
                {
                    return Task.FromResult((false, current, 0L));
                }

                current++;
                _storage[key] = current;
                return Task.FromResult((true, current, (long)(maxLimit - current)));
            }
        }

        public Task<(bool Allowed, bool IsBanned, long CurrentCount, TimeSpan? RetryAfter)> CheckIpRateLimitAsync(
            string clientIp, int maxRequestsPerMinute = 10, int banDurationMinutes = 10, CancellationToken ct = default)
        {
            lock (_lock)
            {
                var banKey = $"ban:{clientIp}";
                if (_storage.ContainsKey(banKey))
                {
                    return Task.FromResult((false, true, 0L, (TimeSpan?)TimeSpan.FromMinutes(banDurationMinutes)));
                }

                var countKey = $"count:{clientIp}";
                long count = 0;
                if (_storage.TryGetValue(countKey, out var val) && long.TryParse(val.ToString(), out var parsed))
                {
                    count = parsed;
                }

                count++;
                _storage[countKey] = count;

                if (count > maxRequestsPerMinute)
                {
                    _storage[banKey] = "banned";
                    return Task.FromResult((false, true, count, (TimeSpan?)TimeSpan.FromMinutes(banDurationMinutes)));
                }

                return Task.FromResult((true, false, count, (TimeSpan?)null));
            }
        }
    }

    private readonly ThreadSafeTestRedisService _redisService = new();
    private readonly Mock<IJwtTokenService> _mockJwtTokenService = new();
    private readonly Mock<ILogger<GuestService>> _mockLogger = new();

    private GuestService CreateService(GuestSecuritySettings? settings = null)
    {
        var options = Options.Create(settings ?? new GuestSecuritySettings());
        return new GuestService(_redisService, _mockJwtTokenService.Object, options, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateAnonymousSessionAsync_WhenValid_ShouldReturnGuestTokenWith24hExpiry()
    {
        // Arrange
        var service = CreateService();
        var guestUuid = Guid.NewGuid().ToString();
        _mockJwtTokenService.Setup(j => j.GenerateGuestToken(It.IsAny<string>()))
            .Returns(new TokenResult
            {
                AccessToken = "mock_jwt_token",
                RefreshToken = "mock_refresh",
                ExpiresInSeconds = 86400
            });

        // Act
        var result = await service.CreateAnonymousSessionAsync(guestUuid, "127.0.0.1", "VN");

        // Assert
        result.Should().NotBeNull();
        result.TokenType.Should().Be("Bearer");
        result.AccessToken.Should().Be("mock_jwt_token");
        result.Role.Should().Be("GUEST");
        result.GuestId.Should().Be(guestUuid);
        result.CountryCode.Should().Be("VN");
    }

    [Fact]
    public async Task CreateAnonymousSessionAsync_WhenVietnamEnforced_AndCountryIsForeign_ShouldThrowForbidden()
    {
        // Arrange
        var service = CreateService(new GuestSecuritySettings
        {
            EnforceVietnamOnly = true,
            AllowedCountryCodes = new List<string> { "VN" }
        });

        // Act
        var act = () => service.CreateAnonymousSessionAsync(null, "203.0.113.1", "US");

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrGuestRegionUnsupported);
    }

    [Fact]
    public async Task ConsumeTrialQuotaAsync_UnderLimit_ShouldSucceed()
    {
        // Arrange
        var service = CreateService();
        var guestUuid = Guid.NewGuid().ToString();

        // Act
        var result = await service.ConsumeTrialQuotaAsync(guestUuid, GuestTrialType.Ai);

        // Assert
        result.Should().NotBeNull();
        result.Used.Should().Be(1);
        result.Limit.Should().Be(3);
        result.Remaining.Should().Be(2);
    }

    [Fact]
    public async Task ConsumeTrialQuotaAsync_WhenExceeded_ShouldThrowTooManyRequests429()
    {
        // Arrange
        var service = CreateService();
        var guestUuid = Guid.NewGuid().ToString();

        // Dùng hết 3 lượt cho phép
        await service.ConsumeTrialQuotaAsync(guestUuid, GuestTrialType.Ai);
        await service.ConsumeTrialQuotaAsync(guestUuid, GuestTrialType.Ai);
        await service.ConsumeTrialQuotaAsync(guestUuid, GuestTrialType.Ai);

        // Act: lượt thứ 4
        var act = () => service.ConsumeTrialQuotaAsync(guestUuid, GuestTrialType.Ai);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(429);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrQuotaAiExceeded);
    }

    [Fact]
    public async Task ConsumeTrialQuotaAsync_20ConcurrentRequests_ShouldNeverExceedLimitOf3()
    {
        // Arrange: Mục tiêu nghiệm thu: Thử 20 yêu cầu cùng lúc không vượt hạn mức
        var service = CreateService();
        var guestUuid = Guid.NewGuid().ToString();
        var successfulCount = 0;
        var rejectedCount = 0;

        // Act: Bắn đồng thời 20 yêu cầu
        var tasks = Enumerable.Range(0, 20).Select(async _ =>
        {
            try
            {
                var res = await service.ConsumeTrialQuotaAsync(guestUuid, GuestTrialType.Ai);
                if (res != null)
                {
                    Interlocked.Increment(ref successfulCount);
                }
            }
            catch (AppException ex) when (ex.ErrorCode == ErrorCodes.ErrQuotaAiExceeded)
            {
                Interlocked.Increment(ref rejectedCount);
            }
        });

        await Task.WhenAll(tasks);

        // Assert
        successfulCount.Should().Be(3, "Hạn mức tối đa chỉ cho phép đúng 3 lượt AI dùng thử");
        rejectedCount.Should().Be(17, "17 yêu cầu còn lại phải bị từ chối với lỗi ERR_QUOTA_AI_EXCEEDED");

        // Kiểm tra tra cứu lại hạn mức
        var quota = await service.GetGuestQuotaAsync(guestUuid);
        quota.Ai.Used.Should().Be(3);
        quota.Ai.Remaining.Should().Be(0);
    }
}
