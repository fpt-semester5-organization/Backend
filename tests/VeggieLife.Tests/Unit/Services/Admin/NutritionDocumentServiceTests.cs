using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.Knowledge;
using VeggieLife.BLL.Services.AI;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Knowledge;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Admin;

public class NutritionDocumentServiceTests
{
    private readonly Mock<IRedisService> _redisMock = new();
    private readonly Mock<IAiServiceClient> _aiClientMock = new();
    private readonly Mock<ILogger<NutritionDocumentService>> _loggerMock = new();
    private readonly NutritionDocumentService _service;

    public NutritionDocumentServiceTests()
    {
        _service = new NutritionDocumentService(_redisMock.Object, _aiClientMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetDocumentsAsync_WhenRedisEmpty_Returns23DefaultDocumentsAndStoresToRedis()
    {
        // Arrange
        _redisMock
            .Setup(r => r.GetAsync<List<AdminNutritionDocumentDto>>(NutritionDocumentService.StorageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<AdminNutritionDocumentDto>?)null);

        // Act
        var result = await _service.GetDocumentsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(23, result.Count);
        _redisMock.Verify(r => r.SetAsync(NutritionDocumentService.StorageKey, It.Is<List<AdminNutritionDocumentDto>>(l => l.Count == 23), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadDocumentAsync_WhenTextFile_UpsertsToAiServiceAndPrependsToList()
    {
        // Arrange
        var cached = new List<AdminNutritionDocumentDto>
        {
            new() { Id = "DOC_1", Title = "Doc 1", Chunks = 2 }
        };
        _redisMock
            .Setup(r => r.GetAsync<List<AdminNutritionDocumentDto>>(NutritionDocumentService.StorageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        _aiClientMock
            .Setup(c => c.UpsertKnowledgeAsync("NUTRITION_DOC", It.IsAny<string>(), "Tài liệu mới", It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var contentBytes = Encoding.UTF8.GetBytes("Dinh dưỡng thực vật chuyên sâu với các vi chất sắt, kẽm, canxi và vitamin B12.");
        var stream = new MemoryStream(contentBytes);

        var request = new UploadNutritionDocumentRequest
        {
            Title = "Tài liệu mới",
            Author = "Bác sĩ Dinh dưỡng",
            FileName = "tailieu.md",
            FileStream = stream,
            FileLength = contentBytes.Length
        };

        // Act
        var result = await _service.UploadDocumentAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Tài liệu mới", result.Title);
        Assert.Equal("Bác sĩ Dinh dưỡng", result.Author);
        Assert.Equal("tailieu.md", result.FileName);
        _aiClientMock.Verify(c => c.UpsertKnowledgeAsync("NUTRITION_DOC", result.Id, "Tài liệu mới", It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenFound_UpdatesActiveState()
    {
        // Arrange
        var cached = new List<AdminNutritionDocumentDto>
        {
            new() { Id = "DOC_1", Title = "Doc 1", IsActive = true }
        };
        _redisMock
            .Setup(r => r.GetAsync<List<AdminNutritionDocumentDto>>(NutritionDocumentService.StorageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        // Act
        var result = await _service.UpdateStatusAsync("DOC_1", false);

        // Assert
        Assert.False(result.IsActive);
        _redisMock.Verify(r => r.SetAsync(NutritionDocumentService.StorageKey, It.IsAny<List<AdminNutritionDocumentDto>>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteDocumentAsync_WhenFound_CallsAiDeleteAndRemovesFromList()
    {
        // Arrange
        var cached = new List<AdminNutritionDocumentDto>
        {
            new() { Id = "DOC_TO_DELETE", Title = "Doc To Delete", IsActive = true }
        };
        _redisMock
            .Setup(r => r.GetAsync<List<AdminNutritionDocumentDto>>(NutritionDocumentService.StorageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        _aiClientMock
            .Setup(c => c.DeleteKnowledgeAsync("NUTRITION_DOC", "DOC_TO_DELETE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(15);

        // Act
        var ok = await _service.DeleteDocumentAsync("DOC_TO_DELETE");

        // Assert
        Assert.True(ok);
        _aiClientMock.Verify(c => c.DeleteKnowledgeAsync("NUTRITION_DOC", "DOC_TO_DELETE", It.IsAny<CancellationToken>()), Times.Once);
        Assert.Empty(cached);
    }
}
