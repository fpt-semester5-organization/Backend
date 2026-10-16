using System;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Services.AI;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Chat;

/// <summary>
/// Unit Tests cho AiCircuitBreaker: Cơ chế ngắt mạch AI (BR-ADM-AI-002).
/// </summary>
public class AiCircuitBreakerTests
{
    private readonly AiCircuitBreaker _circuitBreaker;

    public AiCircuitBreakerTests()
    {
        _circuitBreaker = new AiCircuitBreaker(NullLogger<AiCircuitBreaker>.Instance);
    }

    [Fact]
    public void InitialState_ShouldBeClosed()
    {
        _circuitBreaker.IsOpen.Should().BeFalse();
        Action act = () => _circuitBreaker.ThrowIfOpen();
        act.Should().NotThrow();
    }

    [Fact]
    public void ConsecutiveFailures_Over50Percent_ShouldTripCircuitToOpen()
    {
        // 2 failures out of 2 requests = 100% failure rate (> 50%)
        _circuitBreaker.RecordFailure();
        _circuitBreaker.RecordFailure();

        _circuitBreaker.IsOpen.Should().BeTrue();

        Action act = () => _circuitBreaker.ThrowIfOpen();
        var ex = act.Should().Throw<AppException>().Which;
        ex.StatusCode.Should().Be(503);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrAiCircuitOpen);
    }

    [Fact]
    public void MixedRequests_Under50Percent_ShouldRemainClosed()
    {
        // 3 successes, 1 failure = 25% failure rate (<= 50%)
        _circuitBreaker.RecordSuccess();
        _circuitBreaker.RecordSuccess();
        _circuitBreaker.RecordSuccess();
        _circuitBreaker.RecordFailure();

        _circuitBreaker.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void Reset_ShouldCloseCircuit()
    {
        _circuitBreaker.RecordFailure();
        _circuitBreaker.RecordFailure();
        _circuitBreaker.IsOpen.Should().BeTrue();

        _circuitBreaker.Reset();

        _circuitBreaker.IsOpen.Should().BeFalse();
    }
}
