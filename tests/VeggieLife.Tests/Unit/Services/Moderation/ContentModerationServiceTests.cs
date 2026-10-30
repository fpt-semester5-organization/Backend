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
    public async Task SimulatedCheckContent_VeganDishTitleWithMeatName_ReturnsLowScoreAndNoneType()
    {
        // Arrange - User's exact scenario: Vegan Bun Bo Hue recipe
        var req = new ModerationCheckRequest
        {
            Title = "Bún bò Huế chay giảm cân thanh đạm ít calo",
            Description = "Công thức nấu bún bò Huế ít tinh bột, giàu chất xơ từ nấm và đậu hũ non.",
            IngredientNames = new List<string> { "Bún gạo khô", "Đậu hũ non (Silken Tofu)", "Nấm hương tươi (Shiitake)" },
            Steps = new List<string>
            {
                "Bước 1: Nấu nước dùng từ mía, dứa và sả đập dập để tạo vị ngọt thơm tự nhiên.",
                "Bước 2: Thả đậu hũ non và nấm hương tươi vào nấu chín trong 15 phút.",
                "Bước 3: Cho bún ra tô, chan nước dùng và ăn kèm rau chuối bào cùng giá đỗ."
            },
            DietarySchoolCode = "VEGAN"
        };

        // Act
        var result = await _simulatedService.CheckContentAsync(req);

        // Assert
        result.Should().NotBeNull();
        result.ViolationScore.Should().BeLessThan(0.85);
        result.ViolationType.Should().Be("NONE");
        result.Reasons.Should().BeEmpty();
    }

    [Fact]
    public async Task SimulatedCheckContent_VeganMockMeatAndBotanicalNames_ReturnsLowScoreAndNoneType()
    {
        // Arrange - Plant items with animal names (nấm đùi gà, đậu gà) and mock meat (bò lát chay)
        var req = new ModerationCheckRequest
        {
            Title = "Cơm bò kho chay bánh mì",
            Description = "Món bò kho thơm lừng nấu cùng nấm đùi gà và đậu gà dinh dưỡng.",
            IngredientNames = new List<string> { "Bò lát chay 100g", "Nấm đùi gà 150g", "Đậu gà 50g", "Cà rốt 1 củ" },
            Steps = new List<string> { "Xào bò lát chay với sả", "Hầm nấm đùi gà và đậu gà cho mềm" },
            DietarySchoolCode = "VEGAN"
        };

        // Act
        var result = await _simulatedService.CheckContentAsync(req);

        // Assert
        result.Should().NotBeNull();
        result.ViolationScore.Should().BeLessThan(0.85);
        result.ViolationType.Should().Be("NONE");
        result.Reasons.Should().BeEmpty();
    }

    [Fact]
    public async Task SimulatedCheckContent_VeganTitle_WithRealMeatInIngredients_ReturnsVioMeat()
    {
        // Arrange - Deceptive title "chay" but contains real meat in ingredients
        var req = new ModerationCheckRequest
        {
            Title = "Bún bò Huế chay đặc biệt",
            Description = "Công thức nấu bún bò Huế tại nhà.",
            IngredientNames = new List<string> { "Thịt bò tươi 300g", "Bún tươi 500g" },
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
    public async Task SimulatedCheckContent_VeganTitle_WithRealMeatInSteps_ReturnsVioMeat()
    {
        // Arrange - Deceptive title "chay" with clean ingredients but real meat in steps
        var req = new ModerationCheckRequest
        {
            Title = "Phở bò chay thanh đạm",
            Description = "Nấu phở bò chay tại nhà.",
            IngredientNames = new List<string> { "Bánh phở", "Rau thơm" },
            Steps = new List<string> { "Luộc 500g thịt bò nạm cho chín mềm rồi thái mỏng" },
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
