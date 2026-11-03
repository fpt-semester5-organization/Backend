using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VeggieLife.API.Controllers.Admin;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.Knowledge;
using VeggieLife.BLL.Services.AI;
using VeggieLife.BLL.Services.Knowledge;
using VeggieLife.DAL.Context;
using Xunit;

namespace VeggieLife.Tests.Unit.Controllers;

public class AdminAiKnowledgeControllerTests
{
    private readonly Mock<IAiCircuitBreaker> _circuitBreakerMock = new();
    private readonly Mock<IModelDriftAlertService> _driftServiceMock = new();
    private readonly Mock<IAiServiceClient> _aiClientMock = new();
    private readonly Mock<IKnowledgeReindexService> _reindexServiceMock = new();
    private readonly Mock<INutritionDocumentService> _nutritionDocServiceMock = new();

    private AdminAiController CreateController(string role = UserRoles.Admin, int userId = 1)
    {
        var controller = new AdminAiController(
            _circuitBreakerMock.Object,
            null!, // AppDbContext không dùng trong các endpoint knowledge
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

    [Fact]
    public async Task GetKnowledgeStatus_WhenAdmin_Returns200WithRealQdrantData()
    {
        // Arrange
        var controller = CreateController();
        var expectedData = new AdminKnowledgeStatusResponseData
        {
            Collection = "veggielife_knowledge",
            TotalPoints = 2667,
            BySource = new Dictionary<string, int>
            {
                ["NUTRITION_DOC"] = 2667,
                ["RECIPE"] = 0
            },
            LastIndexedAt = "2026-11-03T18:00:00Z",
            Pending = 0,
            Failed = 0
        };

        _aiClientMock
            .Setup(c => c.GetKnowledgeStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedData);

        // Act
        var result = await controller.GetKnowledgeStatus();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

        var apiResponse = Assert.IsType<ApiResponse<AdminKnowledgeStatusResponseData>>(okResult.Value);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal("veggielife_knowledge", apiResponse.Data.Collection);
        Assert.Equal(2667, apiResponse.Data.TotalPoints);
        Assert.True(apiResponse.Data.BySource.ContainsKey("NUTRITION_DOC"));
    }

    [Fact]
    public async Task ReindexKnowledge_WhenCalled_Returns202AcceptedWithJobId()
    {
        // Arrange
        var controller = CreateController();
        var request = new CreateAdminKnowledgeReindexRequest { Scope = "RECIPE" };
        var expectedJob = new CreateAdminKnowledgeReindexResponseData
        {
            JobId = "kb_20261103_01",
            Scope = "RECIPE",
            Status = "ACCEPTED"
        };

        _reindexServiceMock
            .Setup(s => s.TriggerReindexAsync("RECIPE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedJob);

        // Act
        var result = await controller.ReindexKnowledge(request);

        // Assert
        var acceptedResult = Assert.IsType<AcceptedResult>(result);
        Assert.Equal(StatusCodes.Status202Accepted, acceptedResult.StatusCode);

        var apiResponse = Assert.IsType<ApiResponse<CreateAdminKnowledgeReindexResponseData>>(acceptedResult.Value);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal("kb_20261103_01", apiResponse.Data.JobId);
        Assert.Equal("RECIPE", apiResponse.Data.Scope);
    }

    [Fact]
    public async Task ReindexKnowledge_WhenJobAlreadyRunning_Throws409Conflict()
    {
        // Arrange
        var controller = CreateController();
        var request = new CreateAdminKnowledgeReindexRequest { Scope = "ALL" };

        _reindexServiceMock
            .Setup(s => s.TriggerReindexAsync("ALL", It.IsAny<CancellationToken>()))
            .ThrowsAsync(AppException.Conflict(ErrorCodes.ErrKnowledgeReindexRunning, "Đang có job đồng bộ kho tri thức đang chạy."));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() => controller.ReindexKnowledge(request));
        Assert.Equal(StatusCodes.Status409Conflict, ex.StatusCode);
        Assert.Equal(ErrorCodes.ErrKnowledgeReindexRunning, ex.ErrorCode);
    }

    [Fact]
    public async Task GetNutritionDocuments_WhenAdmin_Returns200WithDocumentList()
    {
        // Arrange
        var controller = CreateController();
        var sampleDocs = new List<AdminNutritionDocumentDto>
        {
            new() { Id = "DOC_BOOK_1", Title = "Becoming Vegan", Chunks = 1113, IsActive = true }
        };

        _nutritionDocServiceMock
            .Setup(s => s.GetDocumentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleDocs);

        // Act
        var result = await controller.GetNutritionDocuments();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

        var apiResponse = Assert.IsType<ApiResponse<List<AdminNutritionDocumentDto>>>(okResult.Value);
        Assert.NotNull(apiResponse.Data);
        Assert.Single(apiResponse.Data);
        Assert.Equal("Becoming Vegan", apiResponse.Data[0].Title);
    }

    [Fact]
    public async Task UpdateNutritionDocumentStatus_WhenAdmin_Returns200WithUpdatedStatus()
    {
        // Arrange
        var controller = CreateController();
        var updatedDoc = new AdminNutritionDocumentDto
        {
            Id = "DOC_GUIDE_1",
            Title = "Đạm thực vật",
            IsActive = false
        };

        _nutritionDocServiceMock
            .Setup(s => s.UpdateStatusAsync("DOC_GUIDE_1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDoc);

        // Act
        var result = await controller.UpdateNutritionDocumentStatus("DOC_GUIDE_1", new UpdateNutritionDocumentStatusRequest { IsActive = false });

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

        var apiResponse = Assert.IsType<ApiResponse<AdminNutritionDocumentDto>>(okResult.Value);
        Assert.NotNull(apiResponse.Data);
        Assert.False(apiResponse.Data.IsActive);
    }

    [Fact]
    public async Task DeleteNutritionDocument_WhenAdmin_Returns200Ok()
    {
        // Arrange
        var controller = CreateController();

        _nutritionDocServiceMock
            .Setup(s => s.DeleteDocumentAsync("DOC_GUIDE_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await controller.DeleteNutritionDocument("DOC_GUIDE_1");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }
}
