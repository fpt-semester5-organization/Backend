using FluentAssertions;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.Llmops;
using VeggieLife.BLL.Validators.Admin;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Admin;

public class AdminLlmopsValidatorTests
{
    [Fact]
    public void ValidateFilter_ValidDateRangeAndGroupBy_ReturnsParsedValues()
    {
        var filter = new AdminLlmopsFilterRequest
        {
            From = "2026-10-01",
            To = "2026-10-15",
            GroupBy = "DAY"
        };

        var (fromDate, toDate, groupBy) = AdminLlmopsValidator.ValidateFilter(filter);

        fromDate.Should().Be(new System.DateTime(2026, 10, 1));
        toDate.Should().Be(new System.DateTime(2026, 10, 15));
        groupBy.Should().Be("DAY");
    }

    [Fact]
    public void ValidateFilter_EmptyFilter_ReturnsNullValues()
    {
        var filter = new AdminLlmopsFilterRequest();

        var (fromDate, toDate, groupBy) = AdminLlmopsValidator.ValidateFilter(filter);

        fromDate.Should().BeNull();
        toDate.Should().BeNull();
        groupBy.Should().BeNull();
    }

    [Fact]
    public void ValidateFilter_InvalidFromFormat_ThrowsBadRequest()
    {
        var filter = new AdminLlmopsFilterRequest { From = "15-10-2026" };

        var act = () => AdminLlmopsValidator.ValidateFilter(filter);

        act.Should().Throw<AppException>()
            .WithMessage("Tham số 'from' phải đúng định dạng YYYY-MM-DD.");
    }

    [Fact]
    public void ValidateFilter_InvalidToFormat_ThrowsBadRequest()
    {
        var filter = new AdminLlmopsFilterRequest { To = "invalid-date" };

        var act = () => AdminLlmopsValidator.ValidateFilter(filter);

        act.Should().Throw<AppException>()
            .WithMessage("Tham số 'to' phải đúng định dạng YYYY-MM-DD.");
    }

    [Fact]
    public void ValidateFilter_FromAfterTo_ThrowsBadRequest()
    {
        var filter = new AdminLlmopsFilterRequest
        {
            From = "2026-10-20",
            To = "2026-10-10"
        };

        var act = () => AdminLlmopsValidator.ValidateFilter(filter);

        act.Should().Throw<AppException>()
            .WithMessage("Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.");
    }

    [Theory]
    [InlineData("USER")]
    [InlineData("HOUR")]
    [InlineData("INVALID")]
    public void ValidateFilter_InvalidGroupBy_ThrowsBadRequest(string invalidGroupBy)
    {
        var filter = new AdminLlmopsFilterRequest { GroupBy = invalidGroupBy };

        var act = () => AdminLlmopsValidator.ValidateFilter(filter);

        act.Should().Throw<AppException>()
            .WithMessage("Tham số 'groupBy' chỉ chấp nhận 'DAY' hoặc 'FEATURE'.");
    }
}
