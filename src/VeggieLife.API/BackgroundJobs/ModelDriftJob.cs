using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Services.AI;

namespace VeggieLife.API.BackgroundJobs;

/// <summary>
/// Background Service chạy định kỳ hằng ngày lúc 01:00 UTC để kiểm tra tỷ lệ đánh giá 1-2 sao của AI
/// trong 07 ngày liên tiếp và phát cảnh báo MODEL_DRIFT_ALERT cho Admin nếu vượt quá 20% (FN-CHAT-06, BR-ADM-AI-004, task S7-H4).
/// Chạy nền bất đồng bộ, lặp lại an toàn (Idempotent).
/// </summary>
public class ModelDriftJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ModelDriftJob> _logger;

    public ModelDriftJob(
        IServiceScopeFactory scopeFactory,
        ILogger<ModelDriftJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Khởi động ModelDriftJob (đánh giá suy thoái độ chính xác AI hằng ngày lúc 01:00)...");

        // Chờ 20 giây sau khi ứng dụng khởi động thành công để thực hiện lần kiểm tra đầu tiên
        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var driftService = scope.ServiceProvider.GetRequiredService<IModelDriftAlertService>();

                var alerted = await driftService.CheckAndAlertModelDriftAsync(null, stoppingToken);
                if (alerted)
                {
                    _logger.LogWarning("ModelDriftJob: Đã phát hiện và ghi nhận cảnh báo MODEL_DRIFT_ALERT cho Admin.");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình thực thi ModelDriftJob.");
            }

            // Chờ đến 01:00 UTC ngày tiếp theo
            try
            {
                var now = DateTime.UtcNow;
                var nextRun = now.Date.AddDays(1).AddHours(1);
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

        _logger.LogInformation("ModelDriftJob đã dừng.");
    }
}
