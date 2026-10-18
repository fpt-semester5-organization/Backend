using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VeggieLife.API.BackgroundJobs;
using VeggieLife.BLL.DTOs.Videos.Responses;
using VeggieLife.BLL.Services.AI;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.BackgroundJobs;

public class SttProcessingWorkerTests
{
    private readonly string _dbName = "SttProcessingWorkerTests_" + Guid.NewGuid();
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly Mock<IAiSttClient> _mockAiSttClient = new();
    private readonly ServiceProvider _serviceProvider;
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public SttProcessingWorkerTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_dbName)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(opt =>
        {
            opt.UseInMemoryDatabase(_dbName);
            opt.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
        });
        services.AddScoped(_ => _mockAiSttClient.Object);
        _serviceProvider = services.BuildServiceProvider();
    }

    private AppDbContext CreateContext() => new(_dbOptions);

    [Fact]
    public async Task ExecuteAsync_WhenValidJob_ProcessesSTTAndUpdatesDatabaseToReady()
    {
        // Arrange
        using (var context = CreateContext())
        {
            var user = new User { UserId = 10, Email = "chef10@veggie.vn", FullName = "Chef 10", Role = "MEMBER", AccountStatus = "ACTIVE" };
            var post = new Post { PostId = 10, UserId = 10, Title = "Post 10", Content = "Content 10", ModerationStatus = "APPROVED_ACTIVE", Visibility = "PUBLIC" };
            var video = new Video
            {
                VideoId = 100,
                PostId = 10,
                Title = "Video Nấu Chay 100",
                VideoUrl = "https://res.cloudinary.com/veggie/video/upload/v100.mp4",
                Status = "PROCESSING",
                DurationSeconds = 65,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            context.Posts.Add(post);
            context.Videos.Add(video);
            await context.SaveChangesAsync();
        }

        var job = new VideoSttJobPayload
        {
            VideoId = 100,
            VideoUrl = "https://res.cloudinary.com/veggie/video/upload/v100.mp4",
            DurationSeconds = 65
        };

        var aiResult = new AiSttResultDto
        {
            DurationSeconds = 65,
            SttModelVersion = "faster-whisper-small-int8",
            RawTranscript = "Hôm nay mình làm món đậu hũ xào nấm rơm",
            Recipe = new ExtractedRecipeDto
            {
                Title = "Đậu hũ xào nấm rơm",
                Ingredients = new List<ExtractedIngredientDto>
                {
                    new() { Name = "Đậu hũ", Quantity = 200, Unit = "g" },
                    new() { Name = "Nấm rơm", Quantity = 100, Unit = "g" }
                },
                Steps = new List<string> { "Sơ chế nấm", "Xào chín với gia vị" }
            }
        };

        var cts = new CancellationTokenSource();
        var dequeueCallCount = 0;

        _mockRedis.Setup(r => r.DequeueAsync<VideoSttJobPayload>("stt_queue", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                dequeueCallCount++;
                if (dequeueCallCount == 1) return job;
                cts.Cancel(); // Hủy sau 1 job để kết thúc worker
                return null;
            });

        _mockAiSttClient.Setup(a => a.ExtractRecipeFromVideoAsync(job.VideoUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(aiResult);

        var worker = new SttProcessingWorker(
            _mockRedis.Object,
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<SttProcessingWorker>.Instance);

        // Act
        await worker.StartAsync(cts.Token);
        await Task.Delay(200); // Chờ worker xử lý
        await worker.StopAsync(CancellationToken.None);

        // Assert
        using (var assertContext = CreateContext())
        {
            var updatedVideo = await assertContext.Videos
                .Include(v => v.VideoTranscript)
                .FirstOrDefaultAsync(v => v.VideoId == 100);

            updatedVideo.Should().NotBeNull();
            updatedVideo!.Status.Should().Be("READY");
            updatedVideo.VideoTranscript.Should().NotBeNull();
            updatedVideo.VideoTranscript!.Status.Should().Be("READY");
            updatedVideo.VideoTranscript.RawTranscript.Should().Be("Hôm nay mình làm món đậu hũ xào nấm rơm");
            updatedVideo.VideoTranscript.SttModelVersion.Should().Be("faster-whisper-small-int8");
            updatedVideo.VideoTranscript.ExtractedRecipeJson.Should().Contain("Đậu hũ xào nấm rơm");
        }
    }

    [Fact]
    public async Task ExecuteAsync_WhenAiFails_MarksVideoAsFailed()
    {
        // Arrange
        using (var context = CreateContext())
        {
            var user = new User { UserId = 11, Email = "chef11@veggie.vn", FullName = "Chef 11", Role = "MEMBER", AccountStatus = "ACTIVE" };
            var post = new Post { PostId = 11, UserId = 11, Title = "Post 11", Content = "Content 11", ModerationStatus = "APPROVED_ACTIVE", Visibility = "PUBLIC" };
            var video = new Video
            {
                VideoId = 101,
                PostId = 11,
                Title = "Video Nhạc Không Lời",
                VideoUrl = "https://res.cloudinary.com/veggie/video/upload/nospeech.mp4",
                Status = "PROCESSING",
                DurationSeconds = 30,
                CreatedAt = DateTime.UtcNow
            };
            var transcript = new VideoTranscript
            {
                VideoId = 101,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            context.Posts.Add(post);
            context.Videos.Add(video);
            context.VideoTranscripts.Add(transcript);
            await context.SaveChangesAsync();
        }

        var job = new VideoSttJobPayload
        {
            VideoId = 101,
            VideoUrl = "https://res.cloudinary.com/veggie/video/upload/nospeech.mp4",
            DurationSeconds = 30
        };

        var cts = new CancellationTokenSource();
        var dequeueCallCount = 0;

        _mockRedis.Setup(r => r.DequeueAsync<VideoSttJobPayload>("stt_queue", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                dequeueCallCount++;
                if (dequeueCallCount == 1) return job;
                cts.Cancel();
                return null;
            });

        _mockAiSttClient.Setup(a => a.ExtractRecipeFromVideoAsync(job.VideoUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiSttResultDto?)null); // AI service trả về null

        var worker = new SttProcessingWorker(
            _mockRedis.Object,
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<SttProcessingWorker>.Instance);

        // Act
        await worker.StartAsync(cts.Token);
        await Task.Delay(200);
        await worker.StopAsync(CancellationToken.None);

        // Assert
        using (var assertContext = CreateContext())
        {
            var updatedVideo = await assertContext.Videos
                .Include(v => v.VideoTranscript)
                .FirstOrDefaultAsync(v => v.VideoId == 101);

            updatedVideo.Should().NotBeNull();
            updatedVideo!.Status.Should().Be("FAILED");
            updatedVideo.VideoTranscript.Should().NotBeNull();
            updatedVideo.VideoTranscript!.Status.Should().Be("FAILED");
        }
    }
}
