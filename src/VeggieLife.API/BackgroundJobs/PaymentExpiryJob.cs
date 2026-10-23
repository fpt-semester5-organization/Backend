using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Services.Subscriptions;

namespace VeggieLife.API.BackgroundJobs;

/// <summary>
/// Background Service chạy định kỳ mỗi phút để quét và chuyển các đơn thanh toán chờ (PENDING)
/// quá 5 phút sang trạng thái hết hạn (EXPIRED) (FN-SUB-02, BR-PAY-TXN-01, task S7-H4).
/// Chạy nền bất đồng bộ, lặp lại an toàn (Idempotent).
/// </summary>
public class PaymentExpiryJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentExpiryJob> _logger;

    public PaymentExpiryJob(
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentExpiryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Khởi động PaymentExpiryJob (quét đơn chờ thanh toán quá 5 phút)...");

        // Chờ 5 giây sau khi ứng dụng khởi động thành công
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var paymentExpiryService = scope.ServiceProvider.GetRequiredService<IPaymentExpiryService>();

                var expiredCount = await paymentExpiryService.ExpirePendingPaymentsAsync(null, stoppingToken);
                if (expiredCount > 0)
                {
                    _logger.LogInformation("PaymentExpiryJob: Đã chuyển {Count} đơn thanh toán quá hạn sang EXPIRED.", expiredCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình thực thi PaymentExpiryJob.");
            }

            // Chờ chu kỳ tiếp theo (mỗi 1 phút)
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("PaymentExpiryJob đã dừng.");
    }
}
