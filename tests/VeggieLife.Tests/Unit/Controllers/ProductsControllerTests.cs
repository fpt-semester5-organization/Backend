using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VeggieLife.API.Controllers;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Products.Responses;
using VeggieLife.BLL.Services.Products;
using Xunit;

namespace VeggieLife.Tests.Unit.Controllers;

/// <summary>
/// Bộ kiểm thử đơn vị cho ProductsController (FN-BAR-01).
/// Kiểm thử cấu trúc phản hồi chuẩn ApiResponse, giải mã UserId từ Claims và Guest UUID từ Header.
/// </summary>
public class ProductsControllerTests
{
    private readonly Mock<IProductService> _mockProductService;
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _mockProductService = new Mock<IProductService>();
        _controller = new ProductsController(_mockProductService.Object);

        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task ScanBarcode_GuestWithHeader_PassesGuestUuidAndReturns200Ok()
    {
        // Arrange
        var barcode = "8934567890123";
        var guestUuid = "guest-uuid-999";
        _controller.HttpContext.Request.Headers["X-Guest-UUID"] = guestUuid;

        var expectedResponse = new BarcodeScanResponse
        {
            Barcode = barcode,
            ProductName = "Sữa đậu nành hữu cơ",
            Brand = "Soy Milk",
            VeganVerdict = "GREEN",
            VerdictText = "Sản phẩm thuần chay.",
            Quota = new ScanQuotaDto { Used = 1, Limit = 3, Remaining = 2 }
        };

        _mockProductService.Setup(s => s.ScanBarcodeAsync(
                barcode,
                null,
                guestUuid,
                "CAMERA_BARCODE",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ScanBarcode(barcode);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var apiResp = okResult.Value.Should().BeOfType<ApiResponse<BarcodeScanResponse>>().Subject;
        apiResp.StatusCode.Should().Be(200);
        apiResp.Message.Should().Be("Tra cứu mã vạch thành công.");
        apiResp.Data.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task ScanBarcode_AuthenticatedMember_PassesUserIdFromClaimsAndReturns200Ok()
    {
        // Arrange
        var barcode = "8934567890123";
        var userId = 42;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, UserRoles.Member),
            new Claim(ClaimTypes.Email, "member@veggielife.vn")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.HttpContext.User = new ClaimsPrincipal(identity);

        var expectedResponse = new BarcodeScanResponse
        {
            Barcode = barcode,
            ProductName = "Bánh quy yến mạch",
            Brand = "Oatmeal",
            VeganVerdict = "GREEN",
            Quota = new ScanQuotaDto { Used = 1, Limit = 100, Remaining = 99 }
        };

        _mockProductService.Setup(s => s.ScanBarcodeAsync(
                barcode,
                userId,
                It.IsAny<string?>(),
                "CAMERA_BARCODE",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ScanBarcode(barcode);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResp = okResult.Value.Should().BeOfType<ApiResponse<BarcodeScanResponse>>().Subject;
        apiResp.Data.ProductName.Should().Be("Bánh quy yến mạch");
    }

    [Fact]
    public async Task ScanBarcode_ServiceThrowsQuotaExceeded_PropagatesAppException()
    {
        // Arrange
        var barcode = "8934567890123";
        _mockProductService.Setup(s => s.ScanBarcodeAsync(
                barcode,
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(AppException.Forbidden(ErrorCodes.ErrQuotaBarcodeExceeded, "Hết lượt"));

        // Act
        Func<Task> act = async () => await _controller.ScanBarcode(barcode);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrQuotaBarcodeExceeded);
    }
}
