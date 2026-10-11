using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Ai;
using VeggieLife.BLL.Services.Moderation;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Moderation;

public class ContentModerationServiceTests
{
    private readonly SimulatedContentModerationService _simulatedService;

    public ContentModerationServiceTests()
    {
        _simulatedService = new SimulatedContentModerationService(NullLogger<SimulatedContentModerationService>.Instance);
    }

    [Fact]
    public async Task SimulatedCheckContent_SafeVeganContent_ReturnsLowScoreAndNoneType()
    {
        // Arrange
        var req = new ModerationCheckRequest
        {
            Title = "Đậu phụ sốt cà chua hành hoa",
            Description = "Món ăn chay thanh đạm, dễ làm cho bữa cơm gia đình.",
            IngredientNames = new List<string> { "Đậu phụ trắng 3 bìa", "Cà chua chín 2 quả", "Hành hoa" },
            Steps = new List<string> { "Rán vàng đậu phụ", "Xào nhuyễn cà chua", "Cho đậu vào om" },
            DietarySchoolCode = "VEGAN"
        };

        // Act
        var result = await _simulatedService.CheckContentAsync(req);

        // Assert
        result.Should().NotBeNull();
        result.ViolationScore.Should().BeLessThan(0.85);
        result.ViolationType.Should().Be("NONE");
        result.Reasons.Should().BeEmpty();
        result.Usage.Model.Should().Be("gemini-2.5-flash");
    }

    [Fact]
    public async Task SimulatedCheckContent_MeatInIngredients_ReturnsVioMeatAndHighViolationScore()
    {
        // Arrange
        var req = new ModerationCheckRequest
        {
            Title = "Súp bí đỏ đặc biệt",
            Description = "Món súp béo ngậy bổ dưỡng.",
            IngredientNames = new List<string> { "Bí đỏ 300g", "Thịt bò xay 200g", "Hạt tiêu" },
            DietarySchoolCode = "VEGAN"
        };

        // Act
        var result = await _simulatedService.CheckContentAsync(req);

        // Assert
        result.Should().NotBeNull();
        result.ViolationScore.Should().BeGreaterThanOrEqualTo(0.85);
        result.ViolationType.Should().Be("VIO_MEAT");
        result.Reasons.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SimulatedCheckContent_HateSpeechContent_ReturnsVioHateAndHighViolationScore()
    {
        // Arrange
        var req = new ModerationCheckRequest
        {
            Title = "Bọn ăn chay giả tạo",
            Description = "Ăn chay là lũ ngu ngốc, tao sẽ đến phá nát quán các người!",
            DietarySchoolCode = "VEGAN"
        };

        // Act
        var result = await _simulatedService.CheckContentAsync(req);

        // Assert
        result.Should().NotBeNull();
        result.ViolationScore.Should().BeGreaterThanOrEqualTo(0.85);
        result.ViolationType.Should().Be("VIO_HATE");
        result.Reasons.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SimulatedCheckContent_VeganWithEggOrMilk_ReturnsVioLabel()
    {
        // Arrange
        var req = new ModerationCheckRequest
        {
            Title = "Bánh ngọt thuần chay",
            Description = "Bánh làm từ trứng gà tươi và sữa đặc.",
            DietarySchoolCode = "VEGAN"
        };

        // Act
        var result = await _simulatedService.CheckContentAsync(req);

        // Assert
        result.Should().NotBeNull();
        result.ViolationScore.Should().BeGreaterThanOrEqualTo(0.85);
        result.ViolationType.Should().Be("VIO_LABEL");
    }

    [Fact]
    public async Task HttpContentModerationService_OnSuccessResponse_ParsesResultCorrectly()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var fakeResponse = new ModerationCheckResponse
        {
            ViolationScore = 0.12,
            ViolationType = "NONE",
            Reasons = new List<string>(),
            Usage = new ModerationUsage { Model = "gemini-2.5-flash", PromptTokens = 100, CompletionTokens = 20 }
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

        var client = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8080") };
        var service = new ContentModerationService(client, NullLogger<ContentModerationService>.Instance);

        var req = new ModerationCheckRequest { Title = "Canh nấm chay thanh đạm" };

        // Act
        var response = await service.CheckContentAsync(req);

        // Assert
        response.Should().NotBeNull();
        response.ViolationScore.Should().Be(0.12);
        response.ViolationType.Should().Be("NONE");
    }

    [Fact]
    public async Task HttpContentModerationService_OnConnectionFailure_FallsBackToSimulatedService()
    {
        // Arrange: Handler throws HttpRequestException simulating network down
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var client = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8080") };
        var service = new ContentModerationService(client, NullLogger<ContentModerationService>.Instance, _simulatedService);

        var req = new ModerationCheckRequest
        {
            Title = "Món ăn chứa thịt bò xào",
            Description = "Thịt bò thơm ngon"
        };

        // Act
        var response = await service.CheckContentAsync(req);

        // Assert: Fallback to simulated service handles and detects meat violation
        response.Should().NotBeNull();
        response.ViolationScore.Should().BeGreaterThanOrEqualTo(0.85);
        response.ViolationType.Should().Be("VIO_MEAT");
    }
}
