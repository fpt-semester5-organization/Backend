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
}
