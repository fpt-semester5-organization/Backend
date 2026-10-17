using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VeggieLife.DAL.External.Email;
using Xunit;

namespace VeggieLife.Tests.Unit.DAL;

/// <summary>
/// Kiểm thử đơn vị cho thành phần hạ tầng SmtpEmailClient và SmtpSettings.
/// </summary>
public class SmtpEmailClientTests
{
    private readonly Mock<ILogger<SmtpEmailClient>> _mockLogger = new();

    [Fact]
    public void SmtpSettings_DefaultValues_AreCorrect()
    {
        // Act
        var settings = new SmtpSettings();

        // Assert
        settings.Port.Should().Be(587);
        settings.EnableSsl.Should().BeTrue();
        settings.DisplayName.Should().Be("VeggieLife");
        settings.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void SmtpSettings_WhenAllRequiredFieldsProvided_IsConfiguredReturnsTrue()
    {
        // Arrange
        var settings = new SmtpSettings
        {
            Host = "smtp.gmail.com",
            Port = 587,
            User = "test@gmail.com",
            Password = "app_password_123"
        };

        // Assert
        settings.IsConfigured.Should().BeTrue();
    }

    [Theory]
    [InlineData("", 587, "user", "pass")]
    [InlineData("smtp.gmail.com", 0, "user", "pass")]
    [InlineData("smtp.gmail.com", 587, "", "pass")]
    [InlineData("smtp.gmail.com", 587, "user", "")]
    public void SmtpSettings_WhenMissingAnyField_IsConfiguredReturnsFalse(string host, int port, string user, string pass)
    {
        // Arrange
        var settings = new SmtpSettings
        {
            Host = host,
            Port = port,
            User = user,
            Password = pass
        };

        // Assert
        settings.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task SendEmailAsync_WhenEmptyRecipient_ThrowsArgumentException()
    {
        // Arrange
        var options = Options.Create(new SmtpSettings());
        var client = new SmtpEmailClient(options, _mockLogger.Object);

        // Act
        Func<Task> act = async () => await client.SendEmailAsync("", "Tiêu đề", "Nội dung");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SendEmailAsync_WhenNotConfigured_LogsWarningAndDoesNotThrow()
    {
        // Arrange: Cấu hình rỗng (môi trường dev chưa có SMTP thật)
        var options = Options.Create(new SmtpSettings());
        var client = new SmtpEmailClient(options, _mockLogger.Object);

        // Act
        Func<Task> act = async () => await client.SendEmailAsync("test@veggielife.vn", "OTP Test", "<p>123456</p>");

        // Assert
        await act.Should().NotThrowAsync();
        client.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void SmtpSettings_WhenMultiProvidersConfigured_GetConfiguredProvidersReturnsInPriorityOrder()
    {
        // Arrange
        var settings = new SmtpSettings
        {
            ActiveProvider = "Auto",
            SendGrid = new EmailProviderConfig
            {
                Name = "SendGrid",
                Host = "smtp.sendgrid.net",
                Port = 587,
                User = "apikey",
                Password = "SG.secret_key"
            },
            Gmail = new EmailProviderConfig
            {
                Name = "Gmail",
                Host = "smtp.gmail.com",
                Port = 587,
                User = "myemail@gmail.com",
                Password = "app_password"
            },
            Mailtrap = new EmailProviderConfig
            {
                Name = "Mailtrap",
                Host = "sandbox.smtp.mailtrap.io",
                Port = 587,
                User = "trap_user",
                Password = "trap_pass"
            }
        };

        // Act
        var providers = settings.GetConfiguredProviders();

        // Assert
        providers.Should().HaveCount(3);
        providers[0].Name.Should().Be("SendGrid");
        providers[1].Name.Should().Be("Gmail");
        providers[2].Name.Should().Be("Mailtrap");
        settings.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public void SmtpSettings_WhenActiveProviderSetToSpecific_ReturnsOnlyThatProvider()
    {
        // Arrange
        var settings = new SmtpSettings
        {
            ActiveProvider = "Gmail",
            SendGrid = new EmailProviderConfig
            {
                Host = "smtp.sendgrid.net",
                Port = 587,
                User = "apikey",
                Password = "SG.secret_key"
            },
            Gmail = new EmailProviderConfig
            {
                Host = "smtp.gmail.com",
                Port = 587,
                User = "myemail@gmail.com",
                Password = "app_password"
            }
        };

        // Act
        var providers = settings.GetConfiguredProviders();

        // Assert
        providers.Should().HaveCount(1);
        providers[0].Name.Should().Be("Gmail");
    }
}
