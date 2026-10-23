using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VeggieLife.API.BackgroundJobs;
using VeggieLife.BLL.Services.AI;
using VeggieLife.BLL.Services.Subscriptions;
using Xunit;

namespace VeggieLife.Tests.Unit.BackgroundJobs;

/// <summary>
/// Unit Tests kiểm thử việc khởi tạo và vòng đời của các BackgroundService:
/// PaymentExpiryJob, VipExpiryJob, ModelDriftJob.
/// </summary>
public class BackgroundJobsExecutionTests
{
    [Fact]
    public async Task PaymentExpiryJob_CanStartAndStopGracefully()
    {
        // ARRANGE
        var mockService = new Mock<IPaymentExpiryService>();
        mockService.Setup(s => s.ExpirePendingPaymentsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var services = new ServiceCollection();
        services.AddScoped(_ => mockService.Object);
        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        var job = new PaymentExpiryJob(scopeFactory, NullLogger<PaymentExpiryJob>.Instance);

        // ACT
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        await job.StartAsync(cts.Token);
        await Task.Delay(100);
        await job.StopAsync(CancellationToken.None);

        // ASSERT: Dừng an toàn không văng lỗi
        job.ExecuteTask.Should().NotBeNull();
    }

    [Fact]
    public async Task VipExpiryJob_CanStartAndStopGracefully()
    {
        // ARRANGE
        var mockService = new Mock<IVipExpiryService>();
        mockService.Setup(s => s.DowngradeExpiredVipUsersAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var services = new ServiceCollection();
        services.AddScoped(_ => mockService.Object);
        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        var job = new VipExpiryJob(scopeFactory, NullLogger<VipExpiryJob>.Instance);

        // ACT
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        await job.StartAsync(cts.Token);
        await Task.Delay(100);
        await job.StopAsync(CancellationToken.None);

        // ASSERT
        job.ExecuteTask.Should().NotBeNull();
    }

    [Fact]
    public async Task ModelDriftJob_CanStartAndStopGracefully()
    {
        // ARRANGE
        var mockService = new Mock<IModelDriftAlertService>();
        mockService.Setup(s => s.CheckAndAlertModelDriftAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var services = new ServiceCollection();
        services.AddScoped(_ => mockService.Object);
        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        var job = new ModelDriftJob(scopeFactory, NullLogger<ModelDriftJob>.Instance);

        // ACT
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        await job.StartAsync(cts.Token);
        await Task.Delay(100);
        await job.StopAsync(CancellationToken.None);

        // ASSERT
        job.ExecuteTask.Should().NotBeNull();
    }
}
