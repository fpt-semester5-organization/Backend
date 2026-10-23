using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.Subscriptions;

/// <summary>
/// Hiện thực dịch vụ quét các đơn thanh toán PENDING quá 5 phút sang EXPIRED (FN-SUB-02, BR-PAY-TXN-01).
/// Chạy lặp lại an toàn (Idempotent).
/// </summary>
public class PaymentExpiryService : IPaymentExpiryService
{
    private readonly AppDbContext _db;
    private readonly IRedisService _redis;
    private readonly ILogger<PaymentExpiryService> _logger;
    private readonly TimeProvider _timeProvider;

    public PaymentExpiryService(
        AppDbContext db,
        IRedisService redis,
        ILogger<PaymentExpiryService> logger,
        TimeProvider? timeProvider = null)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Chuyển các đơn thanh toán PENDING quá 5 phút sang EXPIRED và ghi nhận AuditLog hệ thống.
    /// </summary>
    public async Task<int> ExpirePendingPaymentsAsync(DateTime? customNow = null, CancellationToken ct = default)
    {
        var now = customNow ?? _timeProvider.GetUtcNow().UtcDateTime;
        var cutoff = now.AddMinutes(-5);

        // Lấy danh sách các đơn thanh toán PENDING tạo cách đây quá 5 phút
        var pendingExpiredTransactions = await _db.PaymentTransactions
            .Where(pt => pt.TransactionStatus == "PENDING" && pt.CreatedAt < cutoff)
            .ToListAsync(ct);

        if (pendingExpiredTransactions.Count == 0)
        {
            return 0;
        }

        var count = 0;
        foreach (var transaction in pendingExpiredTransactions)
        {
            var oldStatus = transaction.TransactionStatus;
            transaction.TransactionStatus = "EXPIRED";
            transaction.CompletedAt = now;

            // Ghi AuditLogs (Actor_Role = SYSTEM, Action = PAYMENT_EXPIRED)
            _db.AuditLogs.Add(new AuditLog
            {
                ActorUserId = null,
                ActorRole = "SYSTEM",
                Action = "PAYMENT_EXPIRED",
                TargetTable = "PaymentTransactions",
                TargetId = transaction.TransactionId.ToString(),
                OldValue = JsonSerializer.Serialize(new { status = oldStatus }),
                NewValue = JsonSerializer.Serialize(new { status = "EXPIRED" }),
                Note = "Đơn thanh toán chờ quá 5 phút tự động chuyển sang hết hạn (FN-SUB-02, BR-PAY-TXN-01).",
                CreatedAt = now
            });

            // Xóa khóa Redis payment_expire:{transactionId} nếu còn
            try
            {
                await _redis.RemoveAsync($"payment_expire:{transaction.TransactionId}", ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi xóa Redis cache payment_expire cho đơn #{TransactionId}", transaction.TransactionId);
            }

            count++;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("PaymentExpiryService: Đã chuyển {Count} đơn thanh toán sang EXPIRED.", count);

        return count;
    }
}
