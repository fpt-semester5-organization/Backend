using System;
using FluentAssertions;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.AuditLogs;
using VeggieLife.BLL.Validators.Admin;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Admin;

public class AdminAuditLogValidatorTests
{
    [Fact]
    public void ValidateFilter_ValidParams_PassesAndParsesDates()
    {
        var filter = new AdminAuditLogFilterRequest
        {
            ActorUserId = 1,
            Action = "USER_SANCTION",
            TargetTable = "Users",
            TargetId = "1024",
            From = "2026-10-01",
            To = "2026-10-15",
            Page = 1,
            Size = 20
        };

        var (from, to) = AdminAuditLogValidator.ValidateFilter(filter);

        from.Should().Be(new DateTime(2026, 10, 1));
        to.Should().Be(new DateTime(2026, 10, 15));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidateFilter_InvalidPage_ThrowsValidationException(int page)
    {
        var filter = new AdminAuditLogFilterRequest { Page = page, Size = 20 };

        var ex = Assert.Throws<AppException>(() => AdminAuditLogValidator.ValidateFilter(filter));
        ex.StatusCode.Should().Be(400);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-5)]
    public void ValidateFilter_InvalidSize_ThrowsValidationException(int size)
    {
        var filter = new AdminAuditLogFilterRequest { Page = 1, Size = size };

        var ex = Assert.Throws<AppException>(() => AdminAuditLogValidator.ValidateFilter(filter));
        ex.StatusCode.Should().Be(400);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void ValidateFilter_InvalidActorUserId_ThrowsValidationException(int actorUserId)
    {
        var filter = new AdminAuditLogFilterRequest { ActorUserId = actorUserId };

        var ex = Assert.Throws<AppException>(() => AdminAuditLogValidator.ValidateFilter(filter));
        ex.StatusCode.Should().Be(400);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public void ValidateFilter_ActionExceeds50Chars_ThrowsValidationException()
    {
        var filter = new AdminAuditLogFilterRequest { Action = new string('A', 51) };

        var ex = Assert.Throws<AppException>(() => AdminAuditLogValidator.ValidateFilter(filter));
        ex.StatusCode.Should().Be(400);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public void ValidateFilter_TargetTableExceeds50Chars_ThrowsValidationException()
    {
        var filter = new AdminAuditLogFilterRequest { TargetTable = new string('T', 51) };

        var ex = Assert.Throws<AppException>(() => AdminAuditLogValidator.ValidateFilter(filter));
        ex.StatusCode.Should().Be(400);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public void ValidateFilter_TargetIdExceeds50Chars_ThrowsValidationException()
    {
        var filter = new AdminAuditLogFilterRequest { TargetId = new string('1', 51) };

        var ex = Assert.Throws<AppException>(() => AdminAuditLogValidator.ValidateFilter(filter));
        ex.StatusCode.Should().Be(400);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public void ValidateFilter_InvalidDateFormat_ThrowsValidationException()
    {
        var filter = new AdminAuditLogFilterRequest { From = "2026/10/01" };

        var ex = Assert.Throws<AppException>(() => AdminAuditLogValidator.ValidateFilter(filter));
        ex.StatusCode.Should().Be(400);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public void ValidateFilter_FromGreaterThanTo_ThrowsValidationException()
    {
        var filter = new AdminAuditLogFilterRequest
        {
            From = "2026-10-20",
            To = "2026-10-10"
        };

        var ex = Assert.Throws<AppException>(() => AdminAuditLogValidator.ValidateFilter(filter));
        ex.StatusCode.Should().Be(400);
        ex.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }
}
