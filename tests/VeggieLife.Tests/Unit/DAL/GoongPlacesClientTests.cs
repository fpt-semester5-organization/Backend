using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using VeggieLife.DAL.External.Goong;
using Xunit;

namespace VeggieLife.Tests.Unit.DAL;

/// <summary>
/// Kiểm thử đơn vị cho GoongPlacesClient (FN-MAP-01).
/// </summary>
public class GoongPlacesClientTests
{
    private readonly Mock<ILogger<GoongPlacesClient>> _mockLogger;

    public GoongPlacesClientTests()
    {
        _mockLogger = new Mock<ILogger<GoongPlacesClient>>();
    }

    [Fact]
    public void IsConfigured_WhenApiKeyIsEmpty_ReturnsFalse()
    {
        // Arrange
        var settings = Options.Create(new GoongSettings { ApiKey = "" });
        var client = new GoongPlacesClient(new HttpClient(), settings, _mockLogger.Object);

        // Act & Assert
        client.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_WhenApiKeyIsProvided_ReturnsTrue()
    {
        // Arrange
        var settings = Options.Create(new GoongSettings { ApiKey = "fake_goong_key_123" });
        var client = new GoongPlacesClient(new HttpClient(), settings, _mockLogger.Object);

        // Act & Assert
        client.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public async Task SearchNearbyPlacesAsync_WhenNotConfigured_ReturnsEmptyListWithoutHttpCall()
    {
        // Arrange
        var settings = Options.Create(new GoongSettings { ApiKey = "" });
        var client = new GoongPlacesClient(new HttpClient(), settings, _mockLogger.Object);

        // Act
        var result = await client.SearchNearbyPlacesAsync("quán chay", 21.0285, 105.8542, 5000);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchNearbyPlacesAsync_WhenSuccessResponse_ParsesPredictionsCorrectly()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var fakeResponse = new GoongAutocompleteResponse
        {
            Status = "OK",
            Predictions = new List<GoongPlacePredictionDto>
            {
                new()
                {
                    PlaceId = "GOONG_TEST_01",
                    Description = "Quán Cơm Chay An Lạc, 123 Nguyễn Văn Linh, Đà Nẵng",
                    StructuredFormatting = new GoongStructuredFormatting
                    {
                        MainText = "Quán Cơm Chay An Lạc",
                        SecondaryText = "123 Nguyễn Văn Linh, Đà Nẵng"
                    }
                }
            }
        };

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(fakeResponse)
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var settings = Options.Create(new GoongSettings
        {
            ApiKey = "valid_key",
            BaseUrl = "https://rsapi.goong.io"
        });

        var client = new GoongPlacesClient(httpClient, settings, _mockLogger.Object);

        // Act
        var result = await client.SearchNearbyPlacesAsync("quán chay", 16.0678, 108.2208, 5000);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].PlaceId.Should().Be("GOONG_TEST_01");
        result[0].StructuredFormatting?.MainText.Should().Be("Quán Cơm Chay An Lạc");
    }

    [Fact]
    public async Task GetPlaceDetailAsync_WhenSuccessResponse_ParsesPlaceDetailCorrectly()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var fakeResponse = new GoongPlaceDetailResponse
        {
            Status = "OK",
            Result = new GoongPlaceDetailDto
            {
                PlaceId = "GOONG_TEST_01",
                Name = "Quán Cơm Chay An Lạc",
                FormattedAddress = "123 Nguyễn Văn Linh, Đà Nẵng",
                Geometry = new GoongGeometry
                {
                    Location = new GoongLocation
                    {
                        Lat = 16.0678,
                        Lng = 108.2208
                    }
                }
            }
        };

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(fakeResponse)
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var settings = Options.Create(new GoongSettings
        {
            ApiKey = "valid_key",
            BaseUrl = "https://rsapi.goong.io"
        });

        var client = new GoongPlacesClient(httpClient, settings, _mockLogger.Object);

        // Act
        var result = await client.GetPlaceDetailAsync("GOONG_TEST_01");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Quán Cơm Chay An Lạc");
        result.Geometry?.Location?.Lat.Should().Be(16.0678);
        result.Geometry?.Location?.Lng.Should().Be(108.2208);
    }

    [Fact]
    public async Task GeocodeAddressAsync_WhenNotConfigured_ReturnsNull()
    {
        // Arrange
        var settings = Options.Create(new GoongSettings { ApiKey = "" });
        var client = new GoongPlacesClient(new HttpClient(), settings, _mockLogger.Object);

        // Act
        var result = await client.GeocodeAddressAsync("Hà Nội");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GeocodeAddressAsync_WhenAddressIsEmpty_ReturnsNull()
    {
        // Arrange
        var settings = Options.Create(new GoongSettings { ApiKey = "valid_key" });
        var client = new GoongPlacesClient(new HttpClient(), settings, _mockLogger.Object);

        // Act
        var result = await client.GeocodeAddressAsync("   ");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GeocodeAddressAsync_WhenSuccessResponse_ReturnsLocation()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var fakeResponse = new GoongGeocodeResponse
        {
            Status = "OK",
            Results = new List<GoongGeocodeResult>
            {
                new()
                {
                    PlaceId = "GOONG_GEO_01",
                    FormattedAddress = "Hà Nội, Việt Nam",
                    Geometry = new GoongGeometry
                    {
                        Location = new GoongLocation
                        {
                            Lat = 21.0285,
                            Lng = 105.8542
                        }
                    }
                }
            }
        };

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(fakeResponse)
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var settings = Options.Create(new GoongSettings
        {
            ApiKey = "valid_key",
            BaseUrl = "https://rsapi.goong.io"
        });

        var client = new GoongPlacesClient(httpClient, settings, _mockLogger.Object);

        // Act
        var result = await client.GeocodeAddressAsync("Hà Nội");

        // Assert
        result.Should().NotBeNull();
        result!.Lat.Should().Be(21.0285);
        result.Lng.Should().Be(105.8542);
    }
}

