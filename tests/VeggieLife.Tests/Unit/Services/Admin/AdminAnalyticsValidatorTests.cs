using System;
using FluentAssertions;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.Analytics;
using VeggieLife.BLL.Validators.Admin;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Admin;

public class AdminAnalyticsValidatorTests
{
    [Fact]
    public void ValidateFilter_ValidDates_PassesAndParses()
    {
        var filter = new AdminAnalyticsOverviewFilterRequest
        {
            From = "2026-10-01",
            To = "2026-10-31"
        };

        var (from, to) = AdminAnalyticsValidator.ValidateFilter(filter);

        from.Should().Be(new DateTime(2026, 10, 1));
        to.Should().Be(new DateTime(2026, 10, 31));
    }

    [Fact]
    public void ValidateFilter_NullDates_PassesWithNulls()
    {
        var filter = new AdminAnalyticsOverviewFilterRequest();

        var (from, to) = AdminAnalyticsValidator.ValidateFilter(filter);

        from.Should().BeNull();
        to.Should().BeNull();
    }

    [Fact]
    public void ValidateFilter_InvalidFromDate_ThrowsValidationException()
    {
        var filter = new AdminAnalyticsOverviewFilterRequest { From = "not-a-date" };

        var ex = Assert.Throws<AppException>(() => AdminAnalyticsValidator.ValidateFilter(filter));
        ex.StatusCode.Should().Be(400);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public void ValidateFilter_InvalidToDate_ThrowsValidationException()
    {
        var filter = new AdminAnalyticsOverviewFilterRequest { To = "31-10-2026" };

        var ex = Assert.Throws<AppException>(() => AdminAnalyticsValidator.ValidateFilter(filter));
        ex.StatusCode.Should().Be(400);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public void ValidateFilter_FromGreaterThanTo_ThrowsValidationException()
    {
        var filter = new AdminAnalyticsOverviewFilterRequest
        {
            From = "2026-11-01",
            To = "2026-10-01"
        };

        var ex = Assert.Throws<AppException>(() => AdminAnalyticsValidator.ValidateFilter(filter));
        ex.StatusCode.Should().Be(400);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }
}
