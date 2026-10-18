using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using VeggieLife.DAL.External.AiService;
using Xunit;

namespace VeggieLife.Tests.Unit.DAL;

/// <summary>
/// Bộ kiểm thử đơn vị cho AiServiceClient liên quan đến chức năng nhận diện ảnh tủ lạnh (FN-VIS-01).
/// </summary>
public class AiServiceClientFridgeTests
{
    private readonly Mock<ILogger<AiServiceClient>> _mockLogger;

    public AiServiceClientFridgeTests()
    {
        _mockLogger = new Mock<ILogger<AiServiceClient>>();
    }

    private static (AiServiceClient client, Mock<HttpMessageHandler> handler) CreateClientWithHandler(
        Func<HttpRequestMessage, HttpResponseMessage> handlerFunc,
        int timeoutSeconds = 15)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) => handlerFunc(req));

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8081")
        };

        var settings = Options.Create(new AiServiceSettings
        {
            BaseUrl = "http://localhost:8081",
            InternalApiKey = "test-internal-key",
            TimeoutSeconds = timeoutSeconds
        });

        var client = new AiServiceClient(httpClient, new Mock<ILogger<AiServiceClient>>().Object, settings);
        return (client, mockHandler);
    }

    [Fact]
    public async Task DetectFridgeIngredientsAsync_SuccessfulResponse_ParsesBox2DAndMetadata()
    {
        // Arrange
        var jsonResponse = """
        {
            "items": [
                {
                    "name": "Nấm rơm",
                    "confidence": 0.95,
                    "freshnessStatus": "FRESH",
                    "foodGroup": "Nấm",
                    "isAnimalProduct": false,
                    "isRedMeat": false,
                    "box2d": [145, 230, 420, 510]
                },
                {
                    "name": "Thịt bò",
                    "confidence": 0.98,
                    "freshnessStatus": "FRESH",
                    "foodGroup": "Thịt đỏ",
                    "isAnimalProduct": true,
                    "isRedMeat": true,
                    "box2d": [10, 50, 120, 180]
                }
            ],
            "usage": {
                "model": "gemini-2.5-flash",
                "promptTokens": 100,
                "completionTokens": 200,
                "latencyMs": 500,
                "temperature": 0.2
            }
        }
        """;

        var (client, _) = CreateClientWithHandler(req =>
        {
            req.RequestUri!.PathAndQuery.Should().Be("/vision/detect-ingredients");
            req.Headers.Contains("X-Internal-Api-Key").Should().BeTrue();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };
        });

        using var ms = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });

        // Act
        var result = await client.DetectFridgeIngredientsAsync(ms, "image/jpeg");

        // Assert
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);

        // Mục 1: Nấm rơm
        var mushroom = result.Items[0];
        mushroom.Name.Should().Be("Nấm rơm");
        mushroom.FreshnessStatus.Should().Be("FRESH");
        mushroom.FoodGroup.Should().Be("Nấm");
        mushroom.IsAnimalProduct.Should().BeFalse();
        mushroom.IsRedMeat.Should().BeFalse();
        mushroom.Box2D.Should().Equal(new List<int> { 145, 230, 420, 510 });

        // Mục 2: Thịt bò
        var meat = result.Items[1];
        meat.Name.Should().Be("Thịt bò");
        meat.IsAnimalProduct.Should().BeTrue();
        meat.IsRedMeat.Should().BeTrue();
        meat.Box2D.Should().Equal(new List<int> { 10, 50, 120, 180 });
    }

    [Fact]
    public async Task DetectFridgeIngredientsAsync_WithLegacyBox_2DAlias_ParsesBox2DCorrectly()
    {
        // Arrange
        var jsonResponse = """
        {
            "items": [
                {
                    "name": "Cà rốt",
                    "confidence": 0.90,
                    "freshnessStatus": "FRESH",
                    "foodGroup": "Rau củ",
                    "box_2d": [50, 100, 200, 300]
                }
            ]
        }
        """;

        var (client, _) = CreateClientWithHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
        });

        using var ms = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });

        // Act
        var result = await client.DetectFridgeIngredientsAsync(ms, "image/jpeg");

        // Assert
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items[0].Box2D.Should().Equal(new List<int> { 50, 100, 200, 300 });
    }

    [Fact]
    public async Task DetectFridgeIngredientsAsync_WhenVisionEndpointReturns404_FallsBackToFridgeDetect()
    {
        // Arrange
        var jsonResponse = """
        {
            "items": [
                {
                    "name": "Bí ngô",
                    "freshnessStatus": "FRESH",
                    "box2d": [10, 20, 30, 40]
                }
            ]
        }
        """;

        var callCount = 0;
        var (client, _) = CreateClientWithHandler(req =>
        {
            callCount++;
            if (req.RequestUri!.PathAndQuery == "/vision/detect-ingredients")
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            req.RequestUri!.PathAndQuery.Should().Be("/fridge/detect");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };
        });

        using var ms = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });

        // Act
        var result = await client.DetectFridgeIngredientsAsync(ms, "image/jpeg");

        // Assert
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Bí ngô");
        result.Items[0].Box2D.Should().Equal(new List<int> { 10, 20, 30, 40 });
        callCount.Should().Be(2); // 1 lần chính 404, 1 lần fallback 200
    }

    [Fact]
    public async Task DetectFridgeIngredientsAsync_WhenAiServiceFails_ReturnsNull()
    {
        // Arrange
        var (client, _) = CreateClientWithHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        using var ms = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });

        // Act
        var result = await client.DetectFridgeIngredientsAsync(ms, "image/jpeg");

        // Assert
        result.Should().BeNull();
    }
}
