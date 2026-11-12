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
            .Setup(c => c.UpsertKnowledgeChunksAsync("NUTRITION_DOC", It.IsAny<string>(), "Tài liệu mới", It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

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
        Assert.Equal(3, result.Chunks); // số đoạn thật do AI service trả, không ước lượng
        _aiClientMock.Verify(c => c.UpsertKnowledgeChunksAsync("NUTRITION_DOC", result.Id, "Tài liệu mới", It.Is<string>(t => t.Contains("vitamin B12")), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
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
        _redisMock.Verify(r => r.SetAsync(NutritionDocumentService.StorageKey, It.IsAny<List<AdminNutritionDocumentDto>>(), null, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        // Ẩn tài liệu phải ẩn các đoạn vector trên Qdrant (trước đây chỉ đổi cờ Redis)
        _aiClientMock.Verify(c => c.SetKnowledgeVisibilityAsync("NUTRITION_DOC", It.Is<IReadOnlyCollection<string>>(ids => ids.Contains("DOC_1")), null, "DOC_1", false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("DOC_BOOK_3", 3, null)]
    [InlineData("DOC_GUIDE_101", null, "101")]
    public async Task UpdateStatusAsync_DefaultDocuments_MapToQdrantSourceIds(string docId, int? bookNo, string? sourceId)
    {
        var cached = new List<AdminNutritionDocumentDto> { new() { Id = docId, Title = "Doc", IsActive = true, FileUrl = "/x" } };
        _redisMock
            .Setup(r => r.GetAsync<List<AdminNutritionDocumentDto>>(NutritionDocumentService.StorageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        await _service.UpdateStatusAsync(docId, false);

        _aiClientMock.Verify(c => c.SetKnowledgeVisibilityAsync("NUTRITION_DOC",
            It.Is<IReadOnlyCollection<string>>(ids => sourceId == null ? ids.Count == 0 : ids.Contains(sourceId)),
            bookNo, null, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenAiServiceFails_KeepsRedisStateUnchanged()
    {
        var cached = new List<AdminNutritionDocumentDto> { new() { Id = "DOC_GUIDE_1", Title = "Doc", IsActive = true, FileUrl = "/x" } };
        _redisMock
            .Setup(r => r.GetAsync<List<AdminNutritionDocumentDto>>(NutritionDocumentService.StorageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);
        _aiClientMock
            .Setup(c => c.SetKnowledgeVisibilityAsync("NUTRITION_DOC", It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<int?>(), It.IsAny<string?>(), false, It.IsAny<CancellationToken>()))
            .ThrowsAsync(AppException.ServiceUnavailable(ErrorCodes.ErrExtQdrantUnavailable, "down"));

        await Assert.ThrowsAsync<AppException>(() => _service.UpdateStatusAsync("DOC_GUIDE_1", false));

        Assert.True(cached[0].IsActive);
        _redisMock.Verify(r => r.SetAsync(NutritionDocumentService.StorageKey, It.IsAny<List<AdminNutritionDocumentDto>>(), null, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void ExtractTextFromStream_ScannedPdfWithoutTextLayer_Throws422()
    {
        // PDF 1 trang hợp lệ, không có lớp văn bản (giống bản scan)
        var pdf = Encoding.ASCII.GetBytes(
            "%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n" +
            "3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]>>endobj\ntrailer<</Root 1 0 R>>\n%%EOF");
        var ex = Assert.Throws<AppException>(() => NutritionDocumentService.ExtractTextFromStream(new MemoryStream(pdf), ".pdf"));
        Assert.Equal(ErrorCodes.ErrMediaInvalidFile, ex.ErrorCode);
    }

    private static byte[] BlankPdf(int pages)
    {
        var builder = new UglyToad.PdfPig.Writer.PdfDocumentBuilder();
        for (var i = 0; i < pages; i++) builder.AddPage(UglyToad.PdfPig.Content.PageSize.A4);
        return builder.Build();
    }

    [Fact]
    public async Task UploadDocumentAsync_ScannedPdf_UsesAiOcrTextInsteadOfRejecting()
    {
        _redisMock
            .Setup(r => r.GetAsync<List<AdminNutritionDocumentDto>>(NutritionDocumentService.StorageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AdminNutritionDocumentDto> { new() { Id = "DOC_1", Title = "Doc 1", FileUrl = "/x" } });
        const string ocrText = "Canh chua nấm rơm\n\nNguyên liệu: 200 g nấm rơm, 1 trái cà chua, me chua, đậu bắp.";
        _aiClientMock
            .Setup(c => c.OcrPdfAsync(It.IsAny<byte[]>(), "sach-scan.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VeggieLife.BLL.DTOs.Ai.AiOcrPdfResult { Text = ocrText, Pages = 3 });
        _aiClientMock
            .Setup(c => c.UpsertKnowledgeChunksAsync("NUTRITION_DOC", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var pdf = BlankPdf(3);

        var result = await _service.UploadDocumentAsync(new UploadNutritionDocumentRequest
        {
            Title = "Sách scan", FileName = "sach-scan.pdf", FileStream = new MemoryStream(pdf), FileLength = pdf.Length, ActorUserId = 1
        });

        Assert.Equal(1, result.Chunks);
        _aiClientMock.Verify(c => c.OcrPdfAsync(It.Is<byte[]>(b => b.Length == pdf.Length), "sach-scan.pdf", It.IsAny<CancellationToken>()), Times.Once);
        _aiClientMock.Verify(c => c.UpsertKnowledgeChunksAsync("NUTRITION_DOC", result.Id, "Sách scan", It.Is<string>(t => t.Contains("Canh chua nấm rơm")), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void IsScannedPdf_BlankPagesHaveNoTextLayer()
    {
        var (text, pages) = NutritionDocumentService.ReadPdfTextLayer(new MemoryStream(BlankPdf(2)));
        Assert.Equal(2, pages);
        Assert.True(NutritionDocumentService.IsScannedPdf(text, pages));
        Assert.False(NutritionDocumentService.IsScannedPdf(new string('a', 200), 2));
    }

    [Fact]
    public void SplitIntoParts_LongText_SplitsAtParagraphsWithinLimit()
    {
        var para = new string('a', 400);
        var text = string.Join("\n\n", System.Linq.Enumerable.Repeat(para, 10));
        var parts = NutritionDocumentService.SplitIntoParts(text, 1000);
        Assert.True(parts.Count > 1);
        Assert.All(parts, p => Assert.True(p.Length <= 1000));
        Assert.Equal(10 * 400, string.Concat(parts).Replace("\n", string.Empty).Length);
        Assert.Equal("DOC_X_P2", NutritionDocumentService.PartSourceId("DOC_X", 2));
        Assert.Equal("DOC_X", NutritionDocumentService.PartSourceId("DOC_X", 1));
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
