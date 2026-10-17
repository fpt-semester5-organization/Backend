using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;

namespace VeggieLife.BLL.Services.AI;

/// <summary>
/// Triển khai cơ chế ngắt mạch AI (BR-ADM-AI-002).
/// Khung thời gian theo dõi: 60 giây liên tiếp.
/// Ngưỡng lỗi: > 50% (với tối thiểu 2 yêu cầu).
/// Thời gian ngắt (OPEN): 05 phút.
/// </summary>
public class AiCircuitBreaker : IAiCircuitBreaker
{
    private readonly ILogger<AiCircuitBreaker> _logger;
    private readonly object _lock = new();
    private readonly List<(DateTime Timestamp, bool IsSuccess)> _events = new();
    private DateTime? _openedUntil;

    private static readonly TimeSpan WindowDuration = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan OpenDuration = TimeSpan.FromMinutes(5);
    private const int MinRequestsThreshold = 2;
    private const double FailureRateThreshold = 0.50; // 50%

    public AiCircuitBreaker(ILogger<AiCircuitBreaker> logger)
    {
        _logger = logger;
    }

    public bool IsOpen
    {
        get
        {
            lock (_lock)
            {
                if (!_openedUntil.HasValue)
                {
                    return false;
                }

                if (DateTime.UtcNow >= _openedUntil.Value)
                {
                    // Hết thời gian ngắt 5 phút -> Chuyển về trạng thái mở thử nghiệm (HALF-OPEN/CLOSED)
                    _logger.LogInformation("[AiCircuitBreaker] Hết thời gian ngắt 5 phút. Mạch AI đã tự động phục hồi.");
                    _openedUntil = null;
                    _events.Clear();
                    return false;
                }

                return true;
            }
        }
    }

    public void ThrowIfOpen()
    {
        if (IsOpen)
        {
            throw AppException.ServiceUnavailable(
                ErrorCodes.ErrAiCircuitOpen,
                "Mạch AI đang ngắt tạm thời (BR-ADM-AI-002). Vui lòng thử lại sau.");
        }
    }

    public void RecordSuccess()
    {
        lock (_lock)
        {
            PruneOldEvents();
            _events.Add((DateTime.UtcNow, true));
        }
    }

    public void RecordFailure()
    {
        lock (_lock)
        {
            PruneOldEvents();
            _events.Add((DateTime.UtcNow, false));

            var total = _events.Count;
            if (total >= MinRequestsThreshold)
            {
                var failures = _events.FindAll(e => !e.IsSuccess).Count;
                var failureRate = (double)failures / total;

                if (failureRate > FailureRateThreshold)
                {
                    _openedUntil = DateTime.UtcNow.Add(OpenDuration);
                    _logger.LogWarning(
                        "[AiCircuitBreaker] Tỷ lệ lỗi gọi AI đạt {Rate:P0} ({Failures}/{Total}) trong 60 giây. Mạch AI CHUYỂN SANG TRẠNG THÁI OPEN trong 5 phút đến {OpenedUntil:HH:mm:ss}.",
                        failureRate, failures, total, _openedUntil.Value);
                }
            }
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _openedUntil = null;
            _events.Clear();
            _logger.LogInformation("[AiCircuitBreaker] Đã reset trạng thái mạch AI.");
        }
    }

    private void PruneOldEvents()
    {
        var cutoff = DateTime.UtcNow - WindowDuration;
        _events.RemoveAll(e => e.Timestamp < cutoff);
    }
}
