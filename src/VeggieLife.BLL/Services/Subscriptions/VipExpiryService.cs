using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.Subscriptions;

/// <summary>
/// Hiện thực dịch vụ hạ cấp người dùng VIP đã hết hạn về MEMBER (FN-SUB-04, BR-PAY-SUB-02).
/// Giữ nguyên toàn bộ dữ liệu dinh dưỡng (HealthProfile, cân nặng, dị ứng, thực đơn).
/// Chạy lặp lại an toàn (Idempotent).
/// </summary>
public class VipExpiryService : IVipExpiryService
{
    private readonly AppDbContext _db;
    private readonly IRedisService _redis;
    private readonly ILogger<VipExpiryService> _logger;
    private readonly TimeProvider _timeProvider;

    public VipExpiryService(
        AppDbContext db,
        IRedisService redis,
        ILogger<VipExpiryService> logger,
        TimeProvider? timeProvider = null)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Quét các tài khoản có Role = VIP mà không còn PaymentTransactions SUCCESS còn hạn (EndsAt > now).
    /// Hạ vai trò về MEMBER, giữ nguyên 100% dữ liệu dinh dưỡng, xóa cache Redis và ghi AuditLog.
    /// </summary>
    public async Task<int> DowngradeExpiredVipUsersAsync(DateTime? customNow = null, CancellationToken ct = default)
    {
        var now = customNow ?? _timeProvider.GetUtcNow().UtcDateTime;

        // Chọn các người dùng có Role = VIP kèm lịch sử giao dịch thanh toán
        var vipUsers = await _db.Users
            .Include(u => u.PaymentTransactions)
            .Where(u => u.Role == UserRoles.Vip)
            .ToListAsync(ct);

        if (vipUsers.Count == 0)
        {
            return 0;
        }

        var downgradedCount = 0;

        foreach (var user in vipUsers)
        {
            // Kiểm tra xem người dùng có còn bất kỳ giao dịch SUCCESS nào có Ends_At > hiện tại không
            var hasActiveVip = user.PaymentTransactions.Any(pt =>
                string.Equals(pt.TransactionStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase) &&
                pt.EndsAt > now);

            if (!hasActiveVip)
            {
                var oldRole = user.Role;
                user.Role = UserRoles.Member;
                user.UpdatedAt = now;

                // Quy tắc nghiệp vụ (FN-SUB-04, BR-PAY-SUB-02):
                // Toàn bộ dữ liệu dinh dưỡng (HealthProfile, UserWeightHistories, UserAllergies, MealPlans)
                // ĐƯỢC BẢO LƯU VÀ GIỮ NGUYÊN TOÀN BỘ, tuyệt đối không can thiệp hay xóa bỏ.

                // Ghi AuditLogs (Actor_Role = SYSTEM, Action = VIP_EXPIRED)
                _db.AuditLogs.Add(new AuditLog
                {
                    ActorUserId = null,
                    ActorRole = "SYSTEM",
                    Action = "VIP_EXPIRED",
                    TargetTable = "Users",
                    TargetId = user.UserId.ToString(),
                    OldValue = JsonSerializer.Serialize(new { role = oldRole }),
                    NewValue = JsonSerializer.Serialize(new { role = UserRoles.Member }),
                    Note = "Hết hạn gói VIP, tự động hạ về thành viên thường (giữ nguyên dữ liệu dinh dưỡng) (FN-SUB-04, BR-PAY-SUB-02).",
                    CreatedAt = now
                });

                // Xóa cache Redis vip_status:{userId} để yêu cầu sau phản ánh role MEMBER và isVip = false (D104)
                try
                {
                    await _redis.RemoveAsync($"vip_status:{user.UserId}", ct);
                    await _redis.RemoveAsync($"cache:profile:{user.UserId}", ct);
                    await _redis.RemoveAsync($"user_sessions:{user.UserId}", ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi xóa cache Redis cho user {UserId}", user.UserId);
                }

                downgradedCount++;
            }
        }

        if (downgradedCount > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("VipExpiryService: Đã hạ cấp thành công {Count} thành viên VIP hết hạn về MEMBER.", downgradedCount);
        }

        return downgradedCount;
    }
}
