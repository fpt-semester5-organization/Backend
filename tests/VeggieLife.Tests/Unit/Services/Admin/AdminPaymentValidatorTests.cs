using System;
using FluentAssertions;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.Subscriptions;
using VeggieLife.BLL.Validators.Admin;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Admin;

public class AdminPaymentValidatorTests
{
    [Fact]
    public void ValidateFilter_ValidInput_ReturnsParsedDates()
    {
        var filter = new AdminPaymentListFilterRequest
        {
            Page = 2,
            Size = 20,
            Status = "SUCCESS",
            Gateway = "VIETQR",
            Keyword = "test@example.com",
            From = "2026-10-01",
            To = "2026-10-25"
        };

        var (fromDate, toDate) = AdminPaymentValidator.ValidateFilter(filter);

        fromDate.Should().Be(new DateTime(2026, 10, 1));
        toDate.Should().Be(new DateTime(2026, 10, 25));
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void ValidateFilter_InvalidPageOrSize_ThrowsBadRequest(int page, int size)
    {
        var filter = new AdminPaymentListFilterRequest
        {
            Page = page,
            Size = size
        };

        var ex = Assert.Throws<AppException>(() => AdminPaymentValidator.ValidateFilter(filter));
        ex.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
        ex.StatusCode.Should().Be(400);
    }

    [Fact]
    public void ValidateFilter_InvalidStatus_ThrowsBadRequest()
    {
        var filter = new AdminPaymentListFilterRequest
        {
            Status = "UNKNOWN_STATUS"
        };

        var ex = Assert.Throws<AppException>(() => AdminPaymentValidator.ValidateFilter(filter));
        ex.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
        ex.StatusCode.Should().Be(400);
    }

    [Fact]
    public void ValidateFilter_InvalidGateway_ThrowsBadRequest()
    {
        var filter = new AdminPaymentListFilterRequest
        {
            Gateway = "PAYPAL"
        };

        var ex = Assert.Throws<AppException>(() => AdminPaymentValidator.ValidateFilter(filter));
        ex.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
        ex.StatusCode.Should().Be(400);
    }

    [Fact]
    public void ValidateFilter_FromAfterTo_ThrowsBadRequest()
    {
        var filter = new AdminPaymentListFilterRequest
        {
            From = "2026-10-25",
            To = "2026-10-01"
        };

        var ex = Assert.Throws<AppException>(() => AdminPaymentValidator.ValidateFilter(filter));
        ex.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
        ex.StatusCode.Should().Be(400);
    }

    [Fact]
    public void ValidateFilter_InvalidDateFormat_ThrowsBadRequest()
    {
        var filter = new AdminPaymentListFilterRequest
        {
            From = "25-10-2026"
        };

        var ex = Assert.Throws<AppException>(() => AdminPaymentValidator.ValidateFilter(filter));
        ex.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
        ex.StatusCode.Should().Be(400);
    }
}
