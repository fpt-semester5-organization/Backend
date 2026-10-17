using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VeggieLife.DAL.External.Google;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Auth;

/// <summary>
/// Unit Tests kiểm thử client GoogleAuthClient.
/// </summary>
public class GoogleAuthClientTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateIdTokenAsync_WhenTokenIsNullOrWhitespace_ReturnsNull(string? token)
    {
        var settings = Options.Create(new GoogleAuthSettings
        {
            ClientId = "test_client_id"
        });

        var client = new GoogleAuthClient(settings, NullLogger<GoogleAuthClient>.Instance);

        var result = await client.ValidateIdTokenAsync(token!);

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateIdTokenAsync_WhenInvalidTokenString_ReturnsNullWithoutThrowing()
    {
        var settings = Options.Create(new GoogleAuthSettings
        {
            ClientId = "test_client_id"
        });

        var client = new GoogleAuthClient(settings, NullLogger<GoogleAuthClient>.Instance);

        var result = await client.ValidateIdTokenAsync("not.a.valid.jwt.token");

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateIdTokenAsync_WhenDevGoogleToken_ReturnsValidMockPayload()
    {
        var settings = Options.Create(new GoogleAuthSettings
        {
            AllowDevMockTokens = true
        });

        var client = new GoogleAuthClient(settings, NullLogger<GoogleAuthClient>.Instance);

        var result = await client.ValidateIdTokenAsync("dev-google-token");

        Assert.NotNull(result);
        Assert.Equal("an.nguyen@gmail.com", result.Email);
        Assert.True(result.EmailVerified);
        Assert.NotEmpty(result.Subject);
    }

    [Fact]
    public async Task ValidateIdTokenAsync_WhenDevCustomEmail_ReturnsCustomEmailPayload()
    {
        var settings = Options.Create(new GoogleAuthSettings
        {
            AllowDevMockTokens = true
        });

        var client = new GoogleAuthClient(settings, NullLogger<GoogleAuthClient>.Instance);

        var result = await client.ValidateIdTokenAsync("dev:custom.tester@gmail.com");

        Assert.NotNull(result);
        Assert.Equal("custom.tester@gmail.com", result.Email);
        Assert.True(result.EmailVerified);
        Assert.Equal("custom.tester", result.Name);
    }

    [Fact]
    public async Task ValidateIdTokenAsync_WhenAllowDevMockTokensFalse_DoesNotReturnMock()
    {
        var settings = Options.Create(new GoogleAuthSettings
        {
            AllowDevMockTokens = false
        });

        var client = new GoogleAuthClient(settings, NullLogger<GoogleAuthClient>.Instance);

        var result = await client.ValidateIdTokenAsync("dev-google-token");

        Assert.Null(result);
    }
}
