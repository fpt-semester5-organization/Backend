using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using VeggieLife.BLL.Services.AI;
using Xunit;

namespace VeggieLife.Tests.Integration.AI;

/// <summary>
/// Kiểm thử tích hợp thực tế kết nối từ Backend (AiServiceClient) sang microservice AI (POST /rag/*, GET /rag/status).
/// Bỏ qua kiểm thử nếu dịch vụ AI Service chưa chạy tại máy local.
/// </summary>
public class AiServiceClientLiveIntegrationTests
{
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public AiServiceClientLiveIntegrationTests()
    {
        // Ưu tiên đọc biến môi trường hoặc cổng 8081/8080
        _baseUrl = Environment.GetEnvironmentVariable("AI_SERVICE_URL") ?? "http://localhost:8081";
        _apiKey = Environment.GetEnvironmentVariable("AI_SERVICE_KEY") ?? "veggielife-secret-internal-key-2026";
    }

    private async Task<bool> IsAiServiceReachableAsync()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            http.DefaultRequestHeaders.Add("X-Internal-Api-Key", _apiKey);
            var res = await http.GetAsync($"{_baseUrl}/health");
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private AiServiceClient CreateClient()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiService:BaseUrl"] = _baseUrl,
                ["AiService:InternalApiKey"] = _apiKey
            })
            .Build();

        var httpClient = new HttpClient { BaseAddress = new Uri(_baseUrl) };
        httpClient.DefaultRequestHeaders.Add("X-Internal-Api-Key", _apiKey);

        var circuitBreaker = new AiCircuitBreaker(NullLogger<AiCircuitBreaker>.Instance);

        return new AiServiceClient(
            httpClient,
            circuitBreaker,
            NullLogger<AiServiceClient>.Instance,
            config);
    }

    [Fact]
    public async Task GetKnowledgeStatusAsync_ConnectsToAiServiceAndReturnsValidCollection()
    {
        if (!await IsAiServiceReachableAsync())
        {
            return; // Bỏ qua nếu AI Service chưa khởi động
        }

        // Arrange
        var client = CreateClient();

        // Act
        var status = await client.GetKnowledgeStatusAsync();

        // Assert
        status.Should().NotBeNull();
        status.Collection.Should().Be("veggielife_knowledge");
        status.TotalPoints.Should().BeGreaterThanOrEqualTo(0);
        status.BySource.Should().NotBeNull();
        status.BySource.Should().ContainKey("NUTRITION_DOC");
    }

    [Fact]
    public async Task UpsertAndDeleteKnowledgeAsync_RoundsTripsSuccessfullyWithAiService()
    {
        if (!await IsAiServiceReachableAsync())
        {
            return; // Bỏ qua nếu AI Service chưa khởi động
        }

        // Arrange
        var client = CreateClient();
        var testSourceId = "8888";
        var title = "Kiểm thử kết nối Backend và AI Service";
        var text = "Đây là văn bản mẫu kiểm tra tính liên thông thực tế giữa Backend .NET và AI Microservice qua API /rag/upsert và /rag/delete.";

        // Act 1: Nạp tri thức (UPSERT)
        var upsertOk = await client.UpsertKnowledgeAsync("NUTRITION_DOC", testSourceId, title, text);

        // Assert 1
        upsertOk.Should().BeTrue();

        // Act 2: Xóa tri thức vừa nạp (DELETE)
        var deletedCount = await client.DeleteKnowledgeAsync("NUTRITION_DOC", testSourceId);

        // Assert 2
        deletedCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task UpsertAndDeleteKnowledgeAsync_WithAlphanumericSourceId_Succeeds()
    {
        if (!await IsAiServiceReachableAsync())
        {
            return; // Bỏ qua nếu AI Service chưa khởi động
        }

        // Arrange
        var client = CreateClient();
        var testSourceId = "E999";
        var title = "Mã phụ gia E999 - Thử nghiệm";
        var text = "Mã số phụ gia: E999. Tên gọi phổ biến: Thử nghiệm liên thông. Đánh giá ăn chay: VEGAN.";

        // Act 1: Nạp tri thức E-number với ID chữ cái
        var upsertOk = await client.UpsertKnowledgeAsync("E_NUMBER", testSourceId, title, text);

        // Assert 1
        upsertOk.Should().BeTrue();

        // Act 2: Xóa tri thức vừa nạp
        var deletedCount = await client.DeleteKnowledgeAsync("E_NUMBER", testSourceId);

        // Assert 2
        deletedCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task ReindexNutritionAsync_ConnectsAndExecutesSuccessfully()
    {
        if (!await IsAiServiceReachableAsync())
        {
            return; // Bỏ qua nếu AI Service chưa khởi động
        }

        // Arrange
        var client = CreateClient();

        // Act
        var result = await client.ReindexNutritionAsync();

        // Assert
        result.Should().BeTrue();
    }
}
