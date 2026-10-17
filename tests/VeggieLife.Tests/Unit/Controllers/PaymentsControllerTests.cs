using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VeggieLife.API.Controllers;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Subscriptions.Requests;
using VeggieLife.BLL.Services.Subscriptions;
using Xunit;

namespace VeggieLife.Tests.Unit.Controllers;

/// <summary>
/// Unit Tests kiểm thử endpoint tiếp nhận webhook IPN của PaymentsController (FN-SUB-03).
/// </summary>
public class PaymentsControllerTests
{
    private readonly Mock<ISubscriptionService> _mockService;
    private readonly PaymentsController _controller;

    public PaymentsControllerTests()
    {
        _mockService = new Mock<ISubscriptionService>();
        _controller = new PaymentsController(_mockService.Object);
    }

    [Fact]
    public async Task HandlePaymentIpn_WhenValid_Returns200OkEnvelope()
    {
        var request = new HandlePaymentIpnRequest
        {
            OrderCode = "VL_20261015_001",
            PartnerTransactionId = "MOMO_123",
            Amount = 49000,
            ResultCode = 0,
            Signature = "valid_sig"
        };

        _mockService.Setup(s => s.HandlePaymentIpnAsync("momo", request, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.HandlePaymentIpn("momo", request, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var envelope = okResult.Value.Should().BeOfType<ApiResponse<object?>>().Subject;
        envelope.StatusCode.Should().Be(200);
        envelope.Message.Should().Be("OK");
        envelope.Data.Should().BeNull();
    }

    [Fact]
    public async Task HandlePaymentIpn_WhenSignatureInvalid_PropagatesUnauthorizedException()
    {
        var request = new HandlePaymentIpnRequest
        {
            OrderCode = "VL_20261015_001",
            PartnerTransactionId = "MOMO_123",
            Amount = 49000,
            ResultCode = 0,
            Signature = "bad_sig"
        };

        _mockService.Setup(s => s.HandlePaymentIpnAsync("momo", request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(AppException.Unauthorized(ErrorCodes.ErrPayInvalidSignature, "Chữ ký không hợp lệ."));

        var act = () => _controller.HandlePaymentIpn("momo", request, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(401);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrPayInvalidSignature);
    }
}
