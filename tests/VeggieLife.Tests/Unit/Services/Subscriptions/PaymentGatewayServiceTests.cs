using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using VeggieLife.BLL.Services.Payment;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Subscriptions;

public class PaymentGatewayServiceTests
{
    [Fact]
    public async Task CreateVietQrPaymentAsync_WithoutPayOSCredentials_ReturnsVietQrFallbackUrl()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payment:VietQR:BankId"] = "MB",
                ["Payment:VietQR:AccountNo"] = "0865736967",
                ["Payment:VietQR:AccountName"] = "VEGGIELIFE",
                ["Payment:PayOS:ReturnUrl"] = "http://localhost:3000/payment/success",
                ["Payment:PayOS:CancelUrl"] = "http://localhost:3000/payment/cancel"
            })
            .Build();

        var gateway = new PaymentGatewayServiceStub(config, NullLogger<PaymentGatewayServiceStub>.Instance);

        // Act
        var result = await gateway.CreateVietQrPaymentAsync(100, 50000, "VL 123456");

        // Assert
        result.Should().NotBeNull();
        result.PaymentUrl.Should().Contain("img.vietqr.io/image/MB-0865736967-compact2.png");
        result.QrCodeUrl.Should().Contain("amount=50000");
    }

    [Fact]
    public async Task CreateVietQrPaymentAsync_WithCustomReturnUrls_FallbackSucceeds()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var gateway = new PaymentGatewayServiceStub(config, NullLogger<PaymentGatewayServiceStub>.Instance);

        // Act
        var result = await gateway.CreateVietQrPaymentAsync(
            200,
            99000,
            "VL 999999",
            "http://custom-frontend:3000/success",
            "http://custom-frontend:3000/cancel");

        // Assert
        result.Should().NotBeNull();
        result.PaymentUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void VerifySignature_WithValidSignature_ReturnsTrue()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payment:MoMo:SecretKey"] = "test_momo_secret",
                ["Payment:VietQR:SecretKey"] = "test_vietqr_secret"
            })
            .Build();

        var gateway = new PaymentGatewayServiceStub(config, NullLogger<PaymentGatewayServiceStub>.Instance);

        var data = new Dictionary<string, string>
        {
            ["orderId"] = "123",
            ["amount"] = "50000"
        };

        var signature = gateway.CreateSignature("vietqr", data);

        // Act
        var isValid = gateway.VerifySignature("vietqr", data, signature);

        // Assert
        isValid.Should().BeTrue();
    }
}
