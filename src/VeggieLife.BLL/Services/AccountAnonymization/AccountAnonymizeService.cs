using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.AccountAnonymization;

/// <summary>
/// Dịch vụ ẩn danh hóa dữ liệu cá nhân các tài khoản đã quá 30 ngày ân hạn kể từ khi xóa mềm (FN-PROF-07, BR-ADM-USR-003, BR-ADM-USR-004).
/// </summary>
public class AccountAnonymizeService : IAccountAnonymizeService
{
    private readonly AppDbContext _db;
    private readonly IRedisService _redis;
    private readonly ILogger<AccountAnonymizeService> _logger;

    public AccountAnonymizeService(
        AppDbContext db,
        IRedisService redis,
        ILogger<AccountAnonymizeService> logger)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// Quét các tài khoản SOFT_DELETED quá 30 ngày, ẩn danh hóa thông tin định danh cá nhân và ghi nhận AuditLog.
    /// Chạy lặp lại an toàn (Idempotent).
    /// </summary>
    public async Task<int> AnonymizeOverdueSoftDeletedAccountsAsync(CancellationToken ct = default)
    {
        // 1. Quét các tài khoản đang ở trạng thái SOFT_DELETED (bỏ qua Global Query Filter)
        var softDeletedUsers = await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.HealthProfile)
            .Where(u => u.AccountStatus == AccountStatuses.SoftDeleted)
            .ToListAsync(ct);

        if (softDeletedUsers.Count == 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        var anonymizedCount = 0;

        foreach (var user in softDeletedUsers)
        {
            // 2. Tìm mốc thời gian xóa mềm trong AuditLogs hoặc lấy UpdatedAt/CreatedAt
            var auditLog = await _db.AuditLogs
                .Where(a => a.TargetTable == "Users" &&
                            a.TargetId == user.UserId.ToString() &&
                            a.Action == "ACCOUNT_SOFT_DELETED")
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync(ct);

            var deletedAt = auditLog?.CreatedAt ?? user.UpdatedAt ?? user.CreatedAt;
            var daysSinceDeleted = (now - deletedAt).TotalDays;

            // Nếu đã quá 30 ngày ân hạn phục hồi
            if (daysSinceDeleted >= 30)
            {
                _logger.LogInformation(
                    "Bắt đầu ẩn danh hóa tài khoản User ID {UserId} (đã xóa mềm {Days:F1} ngày trước).",
                    user.UserId, daysSinceDeleted);

                // 3. Ẩn danh hóa dữ liệu cá nhân của Users (BR-ADM-USR-003)
                user.FullName = $"Người dùng #{user.UserId}";
                user.Email = $"anonymized_{user.UserId}@deleted.veggielife.vn";
                user.PhoneNumber = null;
                user.AvatarUrl = null;
                user.PasswordHash = null;
                user.GoogleId = null;
                user.AccountStatus = AccountStatuses.AnonymizedDeleted;
                user.UpdatedAt = now;

                // 4. Ẩn danh hóa chỉ số thể trạng HealthProfile nếu có
                if (user.HealthProfile != null)
                {
                    user.HealthProfile.CurrentWeightKg = 0;
                    user.HealthProfile.HeightCm = 0;
                    user.HealthProfile.TargetWeightKg = null;
                    user.HealthProfile.BmiValue = 0;
                    user.HealthProfile.BmrKcal = 0;
                    user.HealthProfile.TdeeKcal = 0;
                    user.HealthProfile.TargetDailyCalories = 0;
                    user.HealthProfile.UpdatedAt = now;
                }

                // 5. Xóa lịch sử cân nặng và dị ứng cá nhân
                var weightHistories = await _db.UserWeightHistories
                    .Where(w => w.UserId == user.UserId)
                    .ToListAsync(ct);
                if (weightHistories.Count > 0)
                {
                    _db.UserWeightHistories.RemoveRange(weightHistories);
                }

                var allergies = await _db.UserAllergies
                    .Where(a => a.UserId == user.UserId)
                    .ToListAsync(ct);
                if (allergies.Count > 0)
                {
                    _db.UserAllergies.RemoveRange(allergies);
                }

                // 6. Ghi AuditLogs với Actor_Role = SYSTEM, Action = ACCOUNT_ANONYMIZED
                _db.AuditLogs.Add(new AuditLog
                {
                    ActorUserId = null,
                    ActorRole = "SYSTEM",
                    Action = "ACCOUNT_ANONYMIZED",
                    TargetTable = "Users",
                    TargetId = user.UserId.ToString(),
                    Note = "Ẩn danh hóa dữ liệu cá nhân sau thời gian ân hạn 30 ngày (BR-ADM-USR-003).",
                    CreatedAt = now
                });

                // 7. Xóa bộ nhớ đệm còn sót lại
                try
                {
                    await _redis.RemoveAsync($"cache:profile:{user.UserId}", ct);
                    await _redis.RemoveAsync($"user_sessions:{user.UserId}", ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi xóa Redis cache cho user {UserId}.", user.UserId);
                }

                anonymizedCount++;
            }
        }

        if (anonymizedCount > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Đã ẩn danh hóa thành công {Count} tài khoản quá hạn 30 ngày.", anonymizedCount);
        }

        return anonymizedCount;
    }
}
