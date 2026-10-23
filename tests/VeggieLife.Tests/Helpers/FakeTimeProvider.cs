using System;

namespace VeggieLife.Tests.Helpers;

/// <summary>
/// Đồng hồ giả (Fake TimeProvider) cho phép thiết lập và tua nhanh thời gian giả định trong kiểm thử tự động.
/// </summary>
public class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public FakeTimeProvider(DateTimeOffset initialUtcNow)
    {
        _utcNow = initialUtcNow;
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void SetUtcNow(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public void Advance(TimeSpan timeSpan)
    {
        _utcNow = _utcNow.Add(timeSpan);
    }
}
