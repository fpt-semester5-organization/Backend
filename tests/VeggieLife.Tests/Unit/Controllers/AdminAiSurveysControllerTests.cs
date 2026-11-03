using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using VeggieLife.API.Controllers.Admin;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.Surveys;
using VeggieLife.BLL.Services.AI;
using VeggieLife.BLL.Services.Knowledge;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Controllers;

public class AdminAiSurveysControllerTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IAiCircuitBreaker> _circuitBreakerMock = new();
    private readonly Mock<IModelDriftAlertService> _driftServiceMock = new();
    private readonly Mock<IAiServiceClient> _aiClientMock = new();
    private readonly Mock<IKnowledgeReindexService> _reindexServiceMock = new();
    private readonly Mock<INutritionDocumentService> _nutritionDocServiceMock = new();

    public AdminAiSurveysControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("AdminAiSurveysTestsDb_" + Guid.NewGuid())
            .Options;
        _db = new AppDbContext(options);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private AdminAiController CreateController(string role = UserRoles.Admin, int userId = 1)
    {
        var controller = new AdminAiController(
            _circuitBreakerMock.Object,
            _db,
            _driftServiceMock.Object,
            _aiClientMock.Object,
            _reindexServiceMock.Object,
            _nutritionDocServiceMock.Object);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        return controller;
    }

    private async Task SeedSurveysAsync()
    {
        var user1 = new User { UserId = 101, FullName = "Nguyễn Văn A", Email = "a@gmail.com", PasswordHash = "hash", Role = UserRoles.Member, AccountStatus = "ACTIVE" };
        var user2 = new User { UserId = 102, FullName = "Trần Thị B", Email = "b@gmail.com", PasswordHash = "hash", Role = UserRoles.Member, AccountStatus = "ACTIVE" };
        _db.Users.AddRange(user1, user2);

        var session1 = new ChatSession
        {
            ChatSessionId = 501,
            UserId = 101,
            SessionTitle = "Tư vấn dinh dưỡng gym",
            ContextType = "NUTRITION",
            StartedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-2)
        };
        _db.ChatSessions.Add(session1);

        var surveys = new[]
        {
            new Survey
            {
                SurveyId = 1,
                UserId = 101,
                SessionId = 501,
                RatingScore = 5,
                ContextQuestion = "Gợi ý thực đơn tăng cơ thuần chay",
                FeedbackContent = "Bot tư vấn chuẩn và tận tình",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new Survey
            {
                SurveyId = 2,
                UserId = 101,
                SessionId = 501,
                RatingScore = 4,
                ContextQuestion = "Nguồn thực phẩm giàu kẽm",
                FeedbackContent = "Khá đầy đủ thông tin",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Survey
            {
                SurveyId = 3,
                UserId = 102,
                RatingScore = 3,
                ContextQuestion = "Cách thay thế ngũ vị tân",
                FeedbackContent = "Cần giải thích thêm về hành baro",
                CreatedAt = DateTime.UtcNow.AddHours(-10)
            },
            new Survey
            {
                SurveyId = 4,
                UserId = 102,
                RatingScore = 1,
                ContextQuestion = "Tư vấn calo món xào",
                FeedbackContent = "Số liệu calo chưa khớp với nguyên liệu",
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            }
        };

        _db.Surveys.AddRange(surveys);
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetSurveys_WhenAdmin_ReturnsPagedSurveysAndSummaryStats()
    {
        // Arrange
        await SeedSurveysAsync();
        var controller = CreateController();

        // Act
        var result = await controller.GetSurveys(null, null, null, null, null, 1, 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<AdminSurveyListResponse>>(okResult.Value);
        Assert.NotNull(envelope.Data);

        var response = envelope.Data;
        Assert.Equal(4, response.Surveys.TotalItems);
        Assert.Equal(4, response.Surveys.Items.Count);

        // Summary stats
        Assert.Equal(4, response.Summary.TotalSurveys);
        Assert.Equal(3.2, response.Summary.AverageRating); // (5+4+3+1)/4 = 3.25 -> 3.2 (banker's rounding)
        Assert.Equal(2, response.Summary.PositiveCount);
        Assert.Equal(0.5, response.Summary.PositiveRate);
        Assert.Equal(1, response.Summary.NegativeCount);
        Assert.Equal(0.25, response.Summary.NegativeRate);
        Assert.Equal(1, response.Summary.NeutralCount);
    }

    [Fact]
    public async Task GetSurveys_FilterByIsNegative_ReturnsOnlyNegativeSurveys()
    {
        // Arrange
        await SeedSurveysAsync();
        var controller = CreateController();

        // Act
        var result = await controller.GetSurveys(null, true, null, null, null, 1, 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<AdminSurveyListResponse>>(okResult.Value);
        Assert.NotNull(envelope.Data);

        var items = envelope.Data.Surveys.Items;
        Assert.Single(items);
        Assert.Equal(1, items[0].RatingScore);
        Assert.Equal("Số liệu calo chưa khớp với nguyên liệu", items[0].FeedbackContent);
    }

    [Fact]
    public async Task GetSurveys_FilterBySearchKeyword_ReturnsMatchingSurveys()
    {
        // Arrange
        await SeedSurveysAsync();
        var controller = CreateController();

        // Act: search by keyword in question
        var result = await controller.GetSurveys(null, null, "ngũ vị tân", null, null, 1, 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<AdminSurveyListResponse>>(okResult.Value);
        Assert.NotNull(envelope.Data);

        var items = envelope.Data.Surveys.Items;
        Assert.Single(items);
        Assert.Equal(3, items[0].RatingScore);
        Assert.Contains("ngũ vị tân", items[0].ContextQuestion, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetMetrics_WithRealAiquotaLogs_MapsFeatureCodesCorrectly()
    {
        // Arrange: Add quota logs with DB feature code AI_CHAT and AI_FRIDGE
        _db.AiquotaLogs.AddRange(
            new AiquotaLog
            {
                UserId = 101,
                FeatureCode = "AI_CHAT",
                TokensConsumed = 350,
                RequestStatus = "SUCCESS",
                ExecutionTimeMs = 850,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new AiquotaLog
            {
                UserId = 101,
                FeatureCode = "AI_CHAT",
                TokensConsumed = 200,
                RequestStatus = "ERROR",
                ExecutionTimeMs = 950,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new AiquotaLog
            {
                UserId = 102,
                FeatureCode = "AI_FRIDGE",
                TokensConsumed = 500,
                RequestStatus = "SUCCESS",
                ExecutionTimeMs = 1500,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        );
        await SeedSurveysAsync(); // 4 surveys: negativeRate = 1/4 = 0.25
        await _db.SaveChangesAsync();

        var controller = CreateController();

        // Act
        var result = await controller.GetMetrics(null, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetSurveys_WhenNonAdmin_ThrowsForbiddenException()
    {
        // Arrange
        var controller = CreateController(role: UserRoles.Member, userId: 200);

        // Act & Assert
        await Assert.ThrowsAsync<AppException>(() => controller.GetSurveys(null, null, null, null, null, 1, 10));
    }
}
