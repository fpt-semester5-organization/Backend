using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Services.Email;
using VeggieLife.DAL.External.Email;
using Xunit;

namespace VeggieLife.Tests.Unit.Services;

/// <summary>
/// Kiểm thử đơn vị cho nghiệp vụ gửi thư điện tử SmtpEmailService.
/// </summary>
public class SmtpEmailServiceTests
{
    private readonly Mock<ISmtpEmailClient> _mockSmtpClient = new();
    private readonly Mock<ILogger<SmtpEmailService>> _mockLogger = new();
    private readonly SmtpEmailService _service;

    public SmtpEmailServiceTests()
    {
        _service = new SmtpEmailService(_mockSmtpClient.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task SendOtpEmailAsync_WithValidEmailAndVerifyPurpose_CallsSmtpClientWithFormattedHtml()
    {
        // Arrange
        const string email = "user@veggielife.vn";
        const string otp = "123456";
        const string purpose = OtpPurposes.EmailVerify;

        string? capturedSubject = null;
        string? capturedHtml = null;
        string? capturedPlain = null;

        _mockSmtpClient.Setup(x => x.SendEmailAsync(
                email,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, string, string?, CancellationToken>((to, sub, html, plain, ct) =>
            {
                capturedSubject = sub;
                capturedHtml = html;
                capturedPlain = plain;
            })
            .Returns(Task.CompletedTask);

        // Act
        await _service.SendOtpEmailAsync(email, otp, purpose);

        // Assert
        capturedSubject.Should().Contain("Mã OTP kích hoạt tài khoản");
        capturedHtml.Should().NotBeNullOrEmpty();
        capturedHtml.Should().Contain(otp);
        capturedHtml.Should().Contain("VeggieLife");
        capturedHtml.Should().Contain("5 phút");
        capturedPlain.Should().Contain(otp);

        // Kiểm tra bộ nhớ đệm LastSentOtps
        EmailServiceStub.LastSentOtps.Should().ContainKey(email);
        EmailServiceStub.LastSentOtps[email].Should().Be(otp);
    }

    [Fact]
    public async Task SendOtpEmailAsync_WithPasswordResetPurpose_FormatsPasswordResetSubject()
    {
        // Arrange
        const string email = "reset@veggielife.vn";
        const string otp = "654321";
        const string purpose = "PASSWORD_RESET";

        string? capturedSubject = null;
        string? capturedHtml = null;

        _mockSmtpClient.Setup(x => x.SendEmailAsync(
                email,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, string, string?, CancellationToken>((to, sub, html, plain, ct) =>
            {
                capturedSubject = sub;
                capturedHtml = html;
            })
            .Returns(Task.CompletedTask);

        // Act
        await _service.SendOtpEmailAsync(email, otp, purpose);

        // Assert
        capturedSubject.Should().Contain("Mã OTP đặt lại mật khẩu");
        capturedHtml.Should().Contain("Yêu cầu đặt lại mật khẩu");
        capturedHtml.Should().Contain(otp);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendOtpEmailAsync_WithEmptyEmail_ThrowsAppExceptionBadRequest(string invalidEmail)
    {
        // Act
        Func<Task> act = async () => await _service.SendOtpEmailAsync(invalidEmail, "123456", OtpPurposes.EmailVerify);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task SendOtpEmailAsync_WhenSmtpClientThrowsAndConfigured_ThrowsAppExceptionBadGateway()
    {
        // Arrange
        const string email = "failing@veggielife.vn";
        _mockSmtpClient.Setup(x => x.IsConfigured).Returns(true);
        _mockSmtpClient.Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SmtpEmailException("Connection refused by SMTP server"));

        // Act
        Func<Task> act = async () => await _service.SendOtpEmailAsync(email, "999888", OtpPurposes.EmailVerify);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(502);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrExtEmailFailed);
    }

    [Fact]
    public async Task SendOtpEmailAsync_WhenSmtpClientThrowsAndNotConfigured_DoesNotThrow()
    {
        // Arrange
        const string email = "unconfigured@veggielife.vn";
        _mockSmtpClient.Setup(x => x.IsConfigured).Returns(false);
        _mockSmtpClient.Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SmtpEmailException("SMTP unconfigured test exception"));

        // Act
        Func<Task> act = async () => await _service.SendOtpEmailAsync(email, "999888", OtpPurposes.EmailVerify);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendNotificationEmailAsync_WithValidEmail_CallsSmtpClientWithWrappedTemplate()
    {
        // Arrange
        const string email = "notify@veggielife.vn";
        const string subject = "Tài khoản của bạn đã được kích hoạt";
        const string body = "<p>Chào mừng bạn đã trở thành thành viên chính thức!</p>";

        string? capturedHtml = null;
        _mockSmtpClient.Setup(x => x.SendEmailAsync(
                email,
                subject,
                It.IsAny<string>(),
                null,
                It.IsAny<CancellationToken>()))
            .Callback<string, string, string, string?, CancellationToken>((to, sub, html, plain, ct) =>
            {
                capturedHtml = html;
            })
            .Returns(Task.CompletedTask);

        // Act
        await _service.SendNotificationEmailAsync(email, subject, body);

        // Assert
        capturedHtml.Should().NotBeNullOrEmpty();
        capturedHtml.Should().Contain("VeggieLife");
        capturedHtml.Should().Contain("Chào mừng bạn đã trở thành thành viên chính thức!");
    }
}
