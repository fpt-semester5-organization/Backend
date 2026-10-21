using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Services.AccountAnonymization;

namespace VeggieLife.API.BackgroundJobs;

/// <summary>
/// Background Service chạy định kỳ hằng ngày (02:00 UTC) để quét và ẩn danh hóa dữ liệu cá nhân
/// các tài khoản đã ở trạng thái SOFT_DELETED quá 30 ngày (FN-PROF-07, BR-ADM-USR-003, BR-ADM-USR-004, task S7-K7).
/// Chạy nền bất đồng bộ, không làm nghẽn luồng xử lý HTTP request chính, chạy lặp lại an toàn (Idempotent).
/// </summary>
public class AccountAnonymizeJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AccountAnonymizeJob> _logger;

    public AccountAnonymizeJob(
        IServiceScopeFactory scopeFactory,
        ILogger<AccountAnonymizeJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Khởi động AccountAnonymizeJob (quét ẩn danh hóa tài khoản quá hạn 30 ngày)...");

        // Chạy lần đầu sau 10 giây khi ứng dụng khởi động thành công
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var anonymizeService = scope.ServiceProvider.GetRequiredService<IAccountAnonymizeService>();

                var count = await anonymizeService.AnonymizeOverdueSoftDeletedAccountsAsync(stoppingToken);
                if (count > 0)
                {
                    _logger.LogInformation("AccountAnonymizeJob hoàn tất: Đã ẩn danh {Count} tài khoản.", count);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình thực thi AccountAnonymizeJob.");
            }

            // Chờ đến chu kỳ tiếp theo (mỗi 24 giờ)
            try
            {
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("AccountAnonymizeJob đã dừng.");
    }
}
