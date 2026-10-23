using FluentAssertions;
using VeggieLife.BLL.Common.Utils;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Admin;

public class AuditLogSanitizerTests
{
    [Fact]
    public void MaskSensitiveData_NullOrEmpty_ReturnsSame()
    {
        AuditLogSanitizer.MaskSensitiveData(null).Should().BeNull();
        AuditLogSanitizer.MaskSensitiveData("").Should().BeEmpty();
        AuditLogSanitizer.MaskSensitiveData("   ").Should().Be("   ");
    }

    [Fact]
    public void MaskSensitiveData_JsonWithPasswordAndToken_MasksSensitiveFields()
    {
        var rawJson = "{\"email\":\"admin@veggielife.vn\",\"password\":\"SuperSecret123!\",\"passwordHash\":\"$2a$12$abcdefg\",\"role\":\"ADMIN\",\"token\":\"jwt_secret_token\"}";

        var sanitized = AuditLogSanitizer.MaskSensitiveData(rawJson);

        sanitized.Should().NotBeNull();
        sanitized.Should().Contain("\"email\":\"admin@veggielife.vn\"");
        sanitized.Should().Contain("\"role\":\"ADMIN\"");
        sanitized.Should().NotContain("SuperSecret123!");
        sanitized.Should().NotContain("$2a$12$abcdefg");
        sanitized.Should().NotContain("jwt_secret_token");
        sanitized.Should().Contain("\"password\":\"***\"");
        sanitized.Should().Contain("\"passwordHash\":\"***\"");
        sanitized.Should().Contain("\"token\":\"***\"");
    }

    [Fact]
    public void MaskSensitiveData_NestedJsonWithApiKeyAndOtp_MasksRecursively()
    {
        var nestedJson = "{\"user\":{\"id\":1024,\"otp\":\"123456\"},\"config\":{\"apiKey\":\"live_secret_key_999\"}}";

        var sanitized = AuditLogSanitizer.MaskSensitiveData(nestedJson);

        sanitized.Should().NotContain("123456");
        sanitized.Should().NotContain("live_secret_key_999");
        sanitized.Should().Contain("\"otp\":\"***\"");
        sanitized.Should().Contain("\"apiKey\":\"***\"");
    }

    [Fact]
    public void MaskSensitiveData_PlainTextWithJwtAndCredentials_RedactsTokens()
    {
        var plainText = "User logged in with password: MyPlainPassword and Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.do_not_leak";

        var sanitized = AuditLogSanitizer.MaskSensitiveData(plainText);

        sanitized.Should().NotContain("MyPlainPassword");
        sanitized.Should().NotContain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.do_not_leak");
        sanitized.Should().Contain("[REDACTED_JWT]");
    }
}
