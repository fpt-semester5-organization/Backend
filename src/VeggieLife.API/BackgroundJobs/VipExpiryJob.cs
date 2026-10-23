using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Services.Subscriptions;

namespace VeggieLife.API.BackgroundJobs;

/// <summary>
/// Background Service chạy định kỳ hằng ngày sau nửa đêm (00:00:05 UTC) để quét và hạ cấp
/// các thành viên VIP đã hết hạn về thành viên thường (MEMBER) (FN-SUB-04, BR-PAY-SUB-02, task S7-H4).
/// Giữ nguyên toàn bộ dữ liệu dinh dưỡng (HealthProfile, cân nặng, dị ứng, thực đơn).
/// Chạy nền bất đồng bộ, lặp lại an toàn (Idempotent).
/// </summary>
public class VipExpiryJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VipExpiryJob> _logger;

    public VipExpiryJob(
        IServiceScopeFactory scopeFactory,
        ILogger<VipExpiryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Khởi động VipExpiryJob (quét và hạ cấp VIP hết hạn sau nửa đêm)...");

        // Chờ 15 giây sau khi ứng dụng khởi động thành công để thực hiện lần kiểm tra đầu tiên
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var vipExpiryService = scope.ServiceProvider.GetRequiredService<IVipExpiryService>();

                var count = await vipExpiryService.DowngradeExpiredVipUsersAsync(null, stoppingToken);
                if (count > 0)
                {
                    _logger.LogInformation("VipExpiryJob hoàn tất: Đã hạ cấp {Count} thành viên VIP hết hạn về MEMBER.", count);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình thực thi VipExpiryJob.");
            }

            // Chờ đến sau nửa đêm tiếp theo (00:00:05 UTC)
            try
            {
                var now = DateTime.UtcNow;
                var nextRun = now.Date.AddDays(1).AddSeconds(5);
                var delay = nextRun - now;
                if (delay <= TimeSpan.Zero)
                {
                    delay = TimeSpan.FromHours(24);
                }

                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("VipExpiryJob đã dừng.");
    }
}
