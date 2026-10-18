using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VeggieLife.BLL.Services.Cache;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Cache;

public class RedisServiceQueueTests
{
    private readonly RedisService _redisService;

    public RedisServiceQueueTests()
    {
        var mockCache = new Mock<IDistributedCache>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var logger = NullLogger<RedisService>.Instance;

        // Khởi tạo RedisService không có IConnectionMultiplexer để kích hoạt cơ chế In-Memory fallback queue
        _redisService = new RedisService(mockCache.Object, logger, memoryCache, multiplexer: null);
    }

    private class TestQueueItem
    {
        public int VideoId { get; set; }
        public string VideoUrl { get; set; } = string.Empty;
        public int DurationSeconds { get; set; }
    }

    [Fact]
    public async Task EnqueueAndDequeue_ShouldMaintainFifoOrder()
    {
        // Arrange
        var queueName = $"test_queue_fifo_{Guid.NewGuid():N}";
        var item1 = new TestQueueItem { VideoId = 1, VideoUrl = "http://example.com/1.mp4", DurationSeconds = 30 };
        var item2 = new TestQueueItem { VideoId = 2, VideoUrl = "http://example.com/2.mp4", DurationSeconds = 60 };
        var item3 = new TestQueueItem { VideoId = 3, VideoUrl = "http://example.com/3.mp4", DurationSeconds = 90 };

        // Act
        await _redisService.EnqueueAsync(queueName, item1);
        await _redisService.EnqueueAsync(queueName, item2);
        await _redisService.EnqueueAsync(queueName, item3);

        var popped1 = await _redisService.DequeueAsync<TestQueueItem>(queueName);
        var popped2 = await _redisService.DequeueAsync<TestQueueItem>(queueName);
        var popped3 = await _redisService.DequeueAsync<TestQueueItem>(queueName);
        var poppedEmpty = await _redisService.DequeueAsync<TestQueueItem>(queueName);

        // Assert
        Assert.NotNull(popped1);
        Assert.Equal(1, popped1!.VideoId);
        Assert.Equal("http://example.com/1.mp4", popped1.VideoUrl);

        Assert.NotNull(popped2);
        Assert.Equal(2, popped2!.VideoId);

        Assert.NotNull(popped3);
        Assert.Equal(3, popped3!.VideoId);

        Assert.Null(poppedEmpty);
    }

    [Fact]
    public async Task DequeueAsync_WhenQueueIsEmpty_ShouldReturnNull()
    {
        // Arrange
        var emptyQueueName = $"empty_queue_{Guid.NewGuid():N}";

        // Act
        var result = await _redisService.DequeueAsync<TestQueueItem>(emptyQueueName);

        // Assert
        Assert.Null(result);
    }
}
