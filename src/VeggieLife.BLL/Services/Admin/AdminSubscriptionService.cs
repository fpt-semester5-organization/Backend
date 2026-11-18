using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.Subscriptions;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Email;
using VeggieLife.BLL.Services.Subscriptions;
using VeggieLife.BLL.Validators.Admin;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.Admin;

/// <summary>
/// Triển khai dịch vụ Quản trị gói VIP và đối soát giao dịch dành cho Quản trị viên
/// (FN-ADM-13, BR-ADM-PAY-001, BR-ADM-PAY-002, BR-ADM-SYS-002, BR-PAY-TXN-01).
/// </summary>
public class AdminSubscriptionService : IAdminSubscriptionService
{
    private readonly AppDbContext _db;
    private readonly IRedisService _redis;
    private readonly ILogger<AdminSubscriptionService> _logger;
    private readonly IEmailService? _emailService;

    public AdminSubscriptionService(
        AppDbContext db,
        IRedisService redis,
        ILogger<AdminSubscriptionService> logger,
        IEmailService? emailService = null)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
        _emailService = emailService;
    }

    /// <summary>
    /// FN-ADM-13: Lấy toàn bộ danh sách gói VIP dành cho Quản trị viên (cả đang bán và ngừng bán).
    /// </summary>
    public async Task<IReadOnlyList<AdminSubscriptionPlanItemDto>> GetAllSubscriptionPlansAsync(CancellationToken ct = default)
    {
        var plans = await _db.SubscriptionPlans
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderBy(p => p.PlanId)
            .Select(p => new AdminSubscriptionPlanItemDto
            {
                PlanId = p.PlanId,
                PlanName = p.PlanName,
                DurationDays = p.DurationDays,
                PriceVnd = (long)p.Price,
                IsActive = p.IsActive,
                Version = p.Version,
                FeaturesJson = p.FeaturesJson,
                EffectiveFrom = p.UpdatedAt ?? p.CreatedAt
            })
            .ToListAsync(ct);

        return plans;
    }

    /// <summary>
    /// FN-ADM-13: Tạo mới gói VIP (PlanName duy nhất ≤ 100 ký tự, DurationDays > 0, PriceVnd > 0).
    /// Ghi AuditLogs UPDATE_VIP_PRICE (BR-ADM-SYS-002) và xóa cache plans_cache.
    /// </summary>
    public async Task<CreateAdminSubscriptionPlanResponseData> CreateSubscriptionPlanAsync(
        CreateAdminSubscriptionPlanRequest request,
        int adminId,
        CancellationToken ct = default)
    {
        if (request == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Dữ liệu đầu vào không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(request.PlanName))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên gói không được để trống.");
        }

        var trimmedName = request.PlanName.Trim();
        if (trimmedName.Length > 100)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên gói tối đa 100 ký tự.");
        }

        if (request.DurationDays <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Thời hạn gói phải lớn hơn 0.");
        }

        if (request.PriceVnd <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Giá gói phải lớn hơn 0.");
        }

        // Kiểm tra tên gói duy nhất
        var isDuplicate = await _db.SubscriptionPlans
            .IgnoreQueryFilters()
            .AnyAsync(p => p.PlanName.ToLower() == trimmedName.ToLower(), ct);
        if (isDuplicate)
        {
            throw AppException.Conflict(ErrorCodes.ErrPlanDuplicate, "Tên gói đã tồn tại.");
        }

        var featuresJson = await ResolvePlanFeaturesAsync(request.FeaturesJson, ct);

        var now = DateTime.UtcNow;
        var plan = new SubscriptionPlan
        {
            AdminId = adminId,
            PlanName = trimmedName,
            DurationDays = request.DurationDays,
            Price = request.PriceVnd,
            FeaturesJson = featuresJson,
            IsActive = true,
            Version = 1,
            CreatedAt = now
        };

        _db.SubscriptionPlans.Add(plan);
        await _db.SaveChangesAsync(ct);

        // Ghi AuditLogs theo BR-ADM-SYS-002
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminId,
            ActorRole = UserRoles.Admin,
            Action = "UPDATE_VIP_PRICE",
            TargetTable = "SubscriptionPlans",
            TargetId = plan.PlanId.ToString(),
            NewValue = JsonSerializer.Serialize(new
            {
                planId = plan.PlanId,
                planName = plan.PlanName,
                durationDays = plan.DurationDays,
                price = (long)plan.Price,
                features = plan.FeaturesJson,
                version = plan.Version,
                isActive = plan.IsActive
            }),
            Note = $"Tạo mới gói VIP {plan.PlanName}",
            CreatedAt = now
        });
        await _db.SaveChangesAsync(ct);

        // Xóa bộ nhớ đệm
        await InvalidatePlanCacheAsync(ct);

        return new CreateAdminSubscriptionPlanResponseData
        {
            PlanId = plan.PlanId
        };
    }

    /// <summary>
    /// FN-ADM-13: Cập nhật giá hoặc trạng thái kích hoạt của gói VIP (BR-ADM-PAY-001).
    /// Giá mới chỉ áp dụng cho đơn mới, không hồi tố đơn cũ.
    /// Ghi AuditLogs UPDATE_VIP_PRICE (BR-ADM-SYS-002) và xóa cache plans_cache.
    /// </summary>
    public async Task<UpdateAdminSubscriptionPlanResponseData> UpdateSubscriptionPlanAsync(
        int id,
        UpdateAdminSubscriptionPlanRequest request,
        int adminId,
        CancellationToken ct = default)
    {
        if (id <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã gói không hợp lệ.");
        }

        if (request != null && request.PriceVnd.HasValue && request.PriceVnd.Value <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Giá gói phải lớn hơn 0.");
        }

        var plan = await _db.SubscriptionPlans
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.PlanId == id, ct);
        if (plan == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrAdmNotFound, "Đối tượng không tồn tại.");
        }

        var oldValue = JsonSerializer.Serialize(new
        {
            planName = plan.PlanName,
            durationDays = plan.DurationDays,
            price = (long)plan.Price,
            isActive = plan.IsActive,
            version = plan.Version,
            featuresJson = plan.FeaturesJson
        });

        bool isPlanSpecificationChanged = false;

        if (!string.IsNullOrWhiteSpace(request?.PlanName))
        {
            var trimmedName = request.PlanName.Trim();
            if (trimmedName.Length > 100)
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên gói tối đa 100 ký tự.");
            }

            var isDuplicate = await _db.SubscriptionPlans
                .IgnoreQueryFilters()
                .AnyAsync(p => p.PlanId != id && p.PlanName.ToLower() == trimmedName.ToLower(), ct);
            if (isDuplicate)
            {
                throw AppException.Conflict(ErrorCodes.ErrPlanDuplicate, "Tên gói đã tồn tại.");
            }

            if (!string.Equals(plan.PlanName, trimmedName, StringComparison.Ordinal))
            {
                plan.PlanName = trimmedName;
                isPlanSpecificationChanged = true;
            }
        }

        if (request?.DurationDays.HasValue == true)
        {
            if (request.DurationDays.Value <= 0)
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Thời hạn gói phải lớn hơn 0.");
            }
            if (plan.DurationDays != request.DurationDays.Value)
            {
                plan.DurationDays = request.DurationDays.Value;
                isPlanSpecificationChanged = true;
            }
        }

        if (request?.PriceVnd.HasValue == true && (long)plan.Price != request.PriceVnd.Value)
        {
            plan.Price = request.PriceVnd.Value;
            isPlanSpecificationChanged = true;
        }

        if (request?.FeaturesJson != null)
        {
            var featuresJson = await ResolvePlanFeaturesAsync(request.FeaturesJson, ct);
            if (!string.Equals(plan.FeaturesJson, featuresJson, StringComparison.Ordinal))
            {
                plan.FeaturesJson = featuresJson;
                isPlanSpecificationChanged = true;
            }
        }

        if (request?.IsActive.HasValue == true)
        {
            plan.IsActive = request.IsActive.Value;
        }

        var now = DateTime.UtcNow;

        // Nếu thông số gói thay đổi -> Tăng version và ghi nhận vết AuditLog
        if (isPlanSpecificationChanged)
        {
            plan.Version += 1;
        }

        plan.UpdatedAt = now;

        // Ghi AuditLogs theo BR-ADM-SYS-002
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminId,
            ActorRole = UserRoles.Admin,
            Action = "UPDATE_VIP_PRICE",
            TargetTable = "SubscriptionPlans",
            TargetId = plan.PlanId.ToString(),
            OldValue = oldValue,
            NewValue = JsonSerializer.Serialize(new
            {
                version = plan.Version,
                planName = plan.PlanName,
                durationDays = plan.DurationDays,
                price = (long)plan.Price,
                features = plan.FeaturesJson,
                isActive = plan.IsActive
            }),
            Note = isPlanSpecificationChanged 
                ? (!string.IsNullOrWhiteSpace(request?.ChangeReason) 
                    ? request.ChangeReason.Trim() 
                    : $"Cập nhật gói VIP {plan.PlanName} lên Version {plan.Version}")
                : $"Cập nhật trạng thái gói VIP {plan.PlanName}",
            CreatedAt = now
        });
        await _db.SaveChangesAsync(ct);

        // Xóa bộ nhớ đệm
        await InvalidatePlanCacheAsync(ct);

        return new UpdateAdminSubscriptionPlanResponseData
        {
            PlanId = plan.PlanId,
            PriceVnd = (long)plan.Price
        };
    }

    /// <summary>
    /// Lấy lịch sử tất cả các phiên bản của một gói VIP (Audit & Plan Versioning từ AuditLogs).
    /// </summary>
    public async Task<IReadOnlyList<AdminSubscriptionPlanVersionDto>> GetSubscriptionPlanVersionsAsync(
        int planId,
        CancellationToken ct = default)
    {
        if (planId <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã gói không hợp lệ.");
        }

        var plan = await _db.SubscriptionPlans
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.PlanId == planId, ct);
        if (plan == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrAdmNotFound, "Gói VIP không tồn tại.");
        }

        var auditLogs = await _db.AuditLogs
            .AsNoTracking()
            .Where(a => a.TargetTable == "SubscriptionPlans"
                     && a.TargetId == planId.ToString()
                     && (a.Action == "UPDATE_VIP_PLAN" || a.Action == "UPDATE_VIP_PRICE" || a.Action == "CREATE_VIP_PLAN"))
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        var adminIds = auditLogs.Where(a => a.ActorUserId.HasValue).Select(a => a.ActorUserId!.Value).Distinct().ToList();
        var adminNames = await _db.Users
            .Where(u => adminIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId, u => !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Email, ct);

        // Chỉ các lần sửa làm tăng Version mới tạo phiên bản; bật/tắt bán không đổi phiên bản
        var changes = auditLogs
            .Select(log => (Log: log, Old: ParsePlanSnapshot(log.OldValue), New: ParsePlanSnapshot(log.NewValue)))
            .Where(c => c.Old?.Version != null && c.New?.Version != null && c.New.Version > c.Old.Version)
            .ToList(); // mới nhất trước

        string? AdminName(int? id) => id.HasValue && adminNames.TryGetValue(id.Value, out var name) ? name : null;

        var versions = new List<AdminSubscriptionPlanVersionDto>();
        var seenVersions = new HashSet<int> { plan.Version };

        // 1. Phiên bản hiện hành: có hiệu lực từ lần tăng Version gần nhất
        var latest = changes.FirstOrDefault();
        var currentAdminId = latest.Log?.ActorUserId ?? plan.AdminId;
        versions.Add(new AdminSubscriptionPlanVersionDto
        {
            VersionId = plan.PlanId * 1000 + plan.Version,
            PlanId = plan.PlanId,
            Version = plan.Version,
            PlanName = plan.PlanName,
            Price = plan.Price,
            DurationDays = plan.DurationDays,
            FeaturesJson = plan.FeaturesJson,
            EffectiveFrom = latest.Log?.CreatedAt ?? plan.CreatedAt,
            EffectiveTo = null,
            UpdatedByAdminId = currentAdminId,
            UpdatedByAdminName = AdminName(currentAdminId),
            ChangeReason = latest.Log?.Note ?? $"Phiên bản hiện hành (Version {plan.Version})",
            CreatedAt = latest.Log?.CreatedAt ?? plan.CreatedAt
        });

        // 2. Phiên bản cũ (Old_Value của mỗi lần tăng Version): hiệu lực từ lần tăng Version trước đó đến lần này
        for (var i = 0; i < changes.Count; i++)
        {
            var (log, old, _) = changes[i];
            if (!seenVersions.Add(old!.Version!.Value)) continue;

            var previous = i + 1 < changes.Count ? changes[i + 1].Log : null;
            var effectiveFrom = previous?.CreatedAt ?? plan.CreatedAt;
            var adminId = previous?.ActorUserId ?? plan.AdminId;
            versions.Add(new AdminSubscriptionPlanVersionDto
            {
                VersionId = plan.PlanId * 1000 + old.Version.Value,
                PlanId = plan.PlanId,
                Version = old.Version.Value,
                PlanName = old.PlanName ?? plan.PlanName,
                Price = old.Price ?? plan.Price,
                DurationDays = old.DurationDays ?? plan.DurationDays,
                FeaturesJson = old.FeaturesJson,
                EffectiveFrom = effectiveFrom,
                EffectiveTo = log.CreatedAt,
                UpdatedByAdminId = adminId,
                UpdatedByAdminName = AdminName(adminId),
                ChangeReason = previous?.Note ?? $"Phiên bản {old.Version.Value}",
                CreatedAt = effectiveFrom
            });
        }

        return versions.OrderByDescending(v => v.Version).ToList();
    }

    private sealed record PlanAuditSnapshot(int? Version, string? PlanName, decimal? Price, int? DurationDays, string? FeaturesJson);

    /// <summary>Đọc ảnh chụp gói trong Old_Value/New_Value của AuditLogs; JSON hỏng thì trả null.</summary>
    private static PlanAuditSnapshot? ParsePlanSnapshot(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;

            int? version = root.TryGetProperty("version", out var vProp) && vProp.TryGetInt32(out var vVal) ? vVal : null;
            string? name = root.TryGetProperty("planName", out var nProp) && nProp.ValueKind == JsonValueKind.String ? nProp.GetString() : null;
            decimal? price = root.TryGetProperty("price", out var pProp) && pProp.TryGetDecimal(out var pVal) ? pVal : null;
            int? duration = root.TryGetProperty("durationDays", out var dProp) && dProp.TryGetInt32(out var dVal) ? dVal : null;

            string? features = null;
            if (root.TryGetProperty("features", out var fProp) || root.TryGetProperty("featuresJson", out fProp))
            {
                features = fProp.ValueKind switch
                {
                    JsonValueKind.String => fProp.GetString(),
                    JsonValueKind.Null => null,
                    _ => fProp.GetRawText()
                };
            }

            return new PlanAuditSnapshot(version, name, price, duration, features);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// FN-ADM-13: Xóa gói VIP (nếu chưa có giao dịch phát sinh) hoặc tự động ẩn gói nếu đã có giao dịch phát sinh.
    /// Ghi AuditLogs DELETE_VIP_PLAN và xóa cache plans_cache.
    /// </summary>
    public async Task DeleteSubscriptionPlanAsync(
        int id,
        int adminId,
        CancellationToken ct = default)
    {
        if (id <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã gói không hợp lệ.");
        }

        var plan = await _db.SubscriptionPlans
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.PlanId == id, ct);
        if (plan == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrAdmNotFound, "Đối tượng không tồn tại.");
        }

        var hasTransactions = await _db.PaymentTransactions
            .AnyAsync(t => t.PlanId == id, ct);

        if (hasTransactions)
        {
            plan.IsActive = false;
            plan.UpdatedAt = DateTime.UtcNow;

            _db.AuditLogs.Add(new AuditLog
            {
                ActorUserId = adminId,
                ActorRole = UserRoles.Admin,
                Action = "UPDATE_VIP_PRICE",
                TargetTable = "SubscriptionPlans",
                TargetId = plan.PlanId.ToString(),
                Note = $"Tự động chuyển sang ẩn gói VIP '{plan.PlanName}' khi yêu cầu xóa do đã có giao dịch phát sinh",
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);
            await InvalidatePlanCacheAsync(ct);

            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Gói này đã phát sinh giao dịch thanh toán, hệ thống đã chuyển sang trạng thái 'Ẩn gói' để bảo lưu lịch sử giao dịch và đối soát.");
        }

        _db.SubscriptionPlans.Remove(plan);
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminId,
            ActorRole = UserRoles.Admin,
            Action = "DELETE_VIP_PLAN",
            TargetTable = "SubscriptionPlans",
            TargetId = id.ToString(),
            Note = $"Xóa hoàn toàn gói VIP '{plan.PlanName}' (chưa có giao dịch phát sinh)",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        await InvalidatePlanCacheAsync(ct);
    }

    /// <summary>
    /// FN-ADM-13: Đối soát giao dịch thanh toán khiếu nại (BR-ADM-PAY-002, BR-ADM-SYS-002).
    /// - COMPENSATE: Kích hoạt VIP (nếu giao dịch lỗi) hoặc gia hạn thêm một chu kỳ VIP (nếu giao dịch đã thành công), cập nhật Users.Role thành VIP.
    /// - REFUND: Chuyển Transaction_Status sang REFUNDED, thu hồi kỳ VIP (Ends_At = hiện tại), hoàn về MEMBER nếu không còn kỳ VIP khác.
    /// - Mỗi giao dịch chỉ được đối soát đúng 1 lần (từ chối 409 ERR_PAY_ALREADY_RECONCILED nếu đối soát lần 2).
    /// - Ghi AuditLogs PAYMENT_RECONCILE và xóa cache Redis (plans_cache, vip_status:{userId}).
    /// </summary>
    public async Task<ReconcileAdminPaymentResponseData> ReconcilePaymentAsync(
        long transactionId,
        ReconcileAdminPaymentRequest request,
        int adminId,
        CancellationToken ct = default)
    {
        if (transactionId <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã giao dịch không hợp lệ.");
        }

        if (request == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Dữ liệu đầu vào không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(request.Action))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Hành động đối soát không được để trống.");
        }

        var action = request.Action.Trim().ToUpperInvariant();
        if (action != "COMPENSATE" && action != "REFUND")
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Hành động đối soát không hợp lệ. Chỉ chấp nhận COMPENSATE hoặc REFUND.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Lý do đối soát không được để trống.");
        }

        var reason = request.Reason.Trim();
        if (reason.Length < 10 || reason.Length > 500)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Lý do đối soát phải từ 10 đến 500 ký tự.");
        }

        var transaction = await _db.PaymentTransactions
            .IgnoreQueryFilters()
            .Include(pt => pt.Plan)
            .Include(pt => pt.User)
            .FirstOrDefaultAsync(pt => pt.TransactionId == transactionId, ct);

        if (transaction == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrAdmNotFound, "Đối tượng không tồn tại.");
        }

        // Mỗi giao dịch chỉ được đối soát 1 lần duy nhất:
        // Nếu giao dịch đã bị REFUNDED hoặc đã có AuditLog đối soát PAYMENT_RECONCILE -> từ chối 409
        var isAlreadyReconciled = string.Equals(transaction.TransactionStatus, "REFUNDED", StringComparison.OrdinalIgnoreCase)
            || await _db.AuditLogs.AnyAsync(a =>
                a.TargetTable == "PaymentTransactions" &&
                a.TargetId == transaction.TransactionId.ToString() &&
                a.Action == "PAYMENT_RECONCILE", ct);

        if (isAlreadyReconciled)
        {
            throw AppException.Conflict(ErrorCodes.ErrPayAlreadyReconciled, "Giao dịch đã được đối soát.");
        }

        var now = DateTime.UtcNow;
        var oldStatus = transaction.TransactionStatus;
        var oldEndsAt = transaction.EndsAt;

        var user = transaction.User ?? await _db.Users.FirstOrDefaultAsync(u => u.UserId == transaction.UserId, ct);

        if (action == "COMPENSATE")
        {
            int durationDays = transaction.Plan?.DurationDays ?? 30;

            if (string.Equals(transaction.TransactionStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                // Giao dịch đã thành công nhưng người dùng được bù thêm kỳ VIP
                var baseDate = transaction.EndsAt > now ? transaction.EndsAt : now;
                var extendedEndsAt = baseDate.AddDays(durationDays);
                transaction.EndsAt = new DateTime(extendedEndsAt.Year, extendedEndsAt.Month, extendedEndsAt.Day, 23, 59, 59, DateTimeKind.Utc);
            }
            else
            {
                // Giao dịch lỗi (FAILED, EXPIRED, PENDING, CANCELLED): chuyển sang SUCCESS và cấp kỳ VIP
                transaction.TransactionStatus = "SUCCESS";

                var activeMaxEndsAt = await _db.PaymentTransactions
                    .Where(pt => pt.UserId == transaction.UserId &&
                                 pt.TransactionId != transaction.TransactionId &&
                                 pt.TransactionStatus == "SUCCESS" &&
                                 pt.EndsAt > now)
                    .MaxAsync(pt => (DateTime?)pt.EndsAt, ct);

                DateTime startsAt = activeMaxEndsAt.HasValue && activeMaxEndsAt.Value > now
                    ? activeMaxEndsAt.Value
                    : now;

                DateTime endsAt = startsAt.AddDays(durationDays);
                endsAt = new DateTime(endsAt.Year, endsAt.Month, endsAt.Day, 23, 59, 59, DateTimeKind.Utc);

                transaction.StartsAt = startsAt;
                transaction.EndsAt = endsAt;
            }

            transaction.CompletedAt = now;

            // Cập nhật vai trò người dùng thành VIP (nếu không phải ADMIN)
            if (user != null && !string.Equals(user.Role, UserRoles.Admin, StringComparison.OrdinalIgnoreCase))
            {
                user.Role = UserRoles.Vip;
                user.UpdatedAt = now;
            }
        }
        else // REFUND
        {
            transaction.TransactionStatus = "REFUNDED";
            transaction.CompletedAt = now;
            transaction.EndsAt = now;

            // Kiểm tra xem người dùng còn giao dịch SUCCESS nào khác đang hiệu lực không
            var hasOtherActiveVip = await _db.PaymentTransactions
                .AnyAsync(pt => pt.UserId == transaction.UserId &&
                                pt.TransactionId != transaction.TransactionId &&
                                pt.TransactionStatus == "SUCCESS" &&
                                pt.EndsAt > now, ct);

            if (user != null && string.Equals(user.Role, UserRoles.Vip, StringComparison.OrdinalIgnoreCase) && !hasOtherActiveVip)
            {
                user.Role = UserRoles.Member;
                user.UpdatedAt = now;
            }
        }

        // Ghi AuditLogs theo BR-ADM-SYS-002
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminId,
            ActorRole = UserRoles.Admin,
            Action = "PAYMENT_RECONCILE",
            TargetTable = "PaymentTransactions",
            TargetId = transaction.TransactionId.ToString(),
            OldValue = JsonSerializer.Serialize(new
            {
                status = oldStatus,
                endsAt = oldEndsAt.ToString("O")
            }),
            NewValue = JsonSerializer.Serialize(new
            {
                status = transaction.TransactionStatus,
                endsAt = transaction.EndsAt.ToString("O"),
                action
            }),
            Note = reason,
            CreatedAt = now
        });

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Admin #{AdminId} đã đối soát giao dịch #{TransactionId} ({Action}): Status {OldStatus} -> {NewStatus}, EndsAt {OldEndsAt} -> {NewEndsAt}. Lý do: {Reason}",
            adminId, transaction.TransactionId, action, oldStatus, transaction.TransactionStatus, oldEndsAt, transaction.EndsAt, reason);

        // Xóa cache Redis vip_status:{userId}, cache:profile:{userId} và plans_cache
        try
        {
            await _redis.RemoveAsync($"vip_status:{transaction.UserId}", ct);
            await _redis.RemoveAsync($"user_benefits:{transaction.UserId}", ct);
            await _redis.RemoveAsync($"cache:profile:{transaction.UserId}", ct);
            await _redis.RemoveAsync("plans_cache", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi xóa bộ nhớ đệm Redis sau khi đối soát giao dịch #{TransactionId}.", transaction.TransactionId);
        }

        return new ReconcileAdminPaymentResponseData
        {
            TransactionId = transaction.TransactionId,
            Status = transaction.TransactionStatus,
            VipEndsAt = transaction.EndsAt
        };
    }

    /// <summary>
    /// FN-ADM-13: Tra cứu, tìm kiếm, lọc và phân trang danh sách giao dịch nạp VIP (mới nhất lên đầu).
    /// </summary>
    public async Task<(IReadOnlyList<AdminPaymentTransactionItemDto> Items, PagedMeta Meta)> GetPaymentTransactionsAsync(
        AdminPaymentListFilterRequest filter,
        CancellationToken ct = default)
    {
        var (fromDate, toDate) = AdminPaymentValidator.ValidateFilter(filter);

        var query = _db.PaymentTransactions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(pt => pt.User)
            .Include(pt => pt.Plan)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            var status = filter.Status.Trim().ToUpperInvariant();
            query = query.Where(pt => pt.TransactionStatus == status);
        }

        if (!string.IsNullOrWhiteSpace(filter.Gateway))
        {
            var gateway = filter.Gateway.Trim().ToUpperInvariant();
            query = query.Where(pt => pt.GatewayProvider == gateway);
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            query = query.Where(pt =>
                pt.PartnerTransactionCode.Contains(keyword) ||
                (pt.User != null && pt.User.Email.Contains(keyword)) ||
                (pt.User != null && pt.User.FullName.Contains(keyword)));
        }

        // Múi giờ Việt Nam (UTC+7) chuẩn cho đối soát và giám sát dòng tiền VeggieLife
        var vietnamOffset = TimeSpan.FromHours(7);
        if (fromDate.HasValue)
        {
            var fromUtc = fromDate.Value.Date - vietnamOffset;
            query = query.Where(pt => pt.CreatedAt >= fromUtc);
        }

        if (toDate.HasValue)
        {
            var toUtc = toDate.Value.Date.AddDays(1) - vietnamOffset;
            query = query.Where(pt => pt.CreatedAt < toUtc);
        }

        var total = await query.CountAsync(ct);

        // Giám sát dòng tiền Admin: Tính tổng doanh thu các giao dịch SUCCESS theo bộ lọc thời gian & điều kiện hiện tại
        var totalRevenue = (long)await query
            .Where(pt => pt.TransactionStatus == "SUCCESS")
            .SumAsync(pt => (decimal?)pt.AmountVnd ?? 0m, ct);

        var pagedList = await query
            .OrderByDescending(pt => pt.CreatedAt)
            .Skip((filter.Page - 1) * filter.Size)
            .Take(filter.Size)
            .ToListAsync(ct);

        var pagedIds = pagedList.Select(pt => pt.TransactionId.ToString()).ToList();
        var reconciledIds = await _db.AuditLogs
            .AsNoTracking()
            .Where(a => a.TargetTable == "PaymentTransactions" &&
                        pagedIds.Contains(a.TargetId) &&
                        a.Action == "PAYMENT_RECONCILE")
            .Select(a => a.TargetId)
            .ToListAsync(ct);
        var reconciledSet = new HashSet<string>(reconciledIds);

        var items = pagedList.Select(pt =>
        {
            var isRefunded = string.Equals(pt.TransactionStatus, "REFUNDED", StringComparison.OrdinalIgnoreCase);
            var isAlreadyReconciled = isRefunded || reconciledSet.Contains(pt.TransactionId.ToString());

            var canApprove = !string.Equals(pt.TransactionStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase) &&
                             !isRefunded;

            var canReconcile = !isAlreadyReconciled;

            return new AdminPaymentTransactionItemDto
            {
                TransactionId = pt.TransactionId,
                PartnerTransactionCode = pt.PartnerTransactionCode,
                UserId = pt.UserId,
                UserEmail = pt.User?.Email ?? string.Empty,
                UserFullName = pt.User?.FullName ?? string.Empty,
                PlanId = pt.PlanId,
                PlanName = pt.Plan?.PlanName ?? string.Empty,
                PlanDurationDays = pt.Plan?.DurationDays ?? 0,
                GatewayProvider = pt.GatewayProvider,
                AmountVnd = (long)pt.AmountVnd,
                TransactionStatus = pt.TransactionStatus,
                CreatedAt = pt.CreatedAt,
                CompletedAt = pt.CompletedAt,
                StartsAt = pt.StartsAt,
                EndsAt = pt.EndsAt,
                ReceiptUrl = pt.ReceiptUrl,
                CanApprove = canApprove,
                CanReconcile = canReconcile
            };
        }).ToList();

        var meta = new PagedMeta(filter.Page, filter.Size, total)
        {
            TotalRevenueVnd = totalRevenue
        };
        return (items, meta);
    }

    /// <summary>
    /// FN-ADM-13: Duyệt thanh toán thủ công giao dịch MoMo / VietQR (PENDING, EXPIRED, FAILED).
    /// Kích hoạt gói VIP, ghi AuditLog và xóa cache liên quan.
    /// </summary>
    public async Task<ApproveAdminPaymentResponseData> ApprovePaymentAsync(
        long transactionId,
        int adminId,
        CancellationToken ct = default)
    {
        var transaction = await _db.PaymentTransactions
            .IgnoreQueryFilters()
            .Include(pt => pt.Plan)
            .Include(pt => pt.User)
            .FirstOrDefaultAsync(pt => pt.TransactionId == transactionId, ct);

        if (transaction == null)
        {
            throw AppException.NotFound(ErrorCodes.ErrAdmNotFound, "Giao dịch không tồn tại.");
        }

        if (string.Equals(transaction.TransactionStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            throw AppException.Conflict(ErrorCodes.ErrPayOrderAlreadySuccess, "Giao dịch đã được thanh toán thành công trước đó.");
        }

        if (string.Equals(transaction.TransactionStatus, "REFUNDED", StringComparison.OrdinalIgnoreCase))
        {
            throw AppException.Conflict(ErrorCodes.ErrPayOrderRefunded, "Giao dịch đã bị hoàn tiền, không thể duyệt.");
        }

        var now = DateTime.UtcNow;
        var oldStatus = transaction.TransactionStatus;
        int durationDays = transaction.Plan?.DurationDays ?? 30;

        var activeMaxEndsAt = await _db.PaymentTransactions
            .Where(pt => pt.UserId == transaction.UserId &&
                         pt.TransactionId != transaction.TransactionId &&
                         pt.TransactionStatus == "SUCCESS" &&
                         pt.EndsAt > now)
            .MaxAsync(pt => (DateTime?)pt.EndsAt, ct);

        DateTime startsAt = activeMaxEndsAt.HasValue && activeMaxEndsAt.Value > now
            ? activeMaxEndsAt.Value
            : now;

        DateTime endsAt = startsAt.AddDays(durationDays);
        endsAt = new DateTime(endsAt.Year, endsAt.Month, endsAt.Day, 23, 59, 59, DateTimeKind.Utc);

        transaction.TransactionStatus = "SUCCESS";
        transaction.CompletedAt = now;
        transaction.StartsAt = startsAt;
        transaction.EndsAt = endsAt;

        var user = transaction.User ?? await _db.Users.FirstOrDefaultAsync(u => u.UserId == transaction.UserId, ct);
        if (user != null && !string.Equals(user.Role, UserRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            user.Role = UserRoles.Vip;
            user.UpdatedAt = now;
        }

        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = adminId,
            ActorRole = UserRoles.Admin,
            Action = "PAYMENT_APPROVE",
            TargetTable = "PaymentTransactions",
            TargetId = transaction.TransactionId.ToString(),
            OldValue = JsonSerializer.Serialize(new { status = oldStatus }),
            NewValue = JsonSerializer.Serialize(new
            {
                status = "SUCCESS",
                gateway = transaction.GatewayProvider,
                startsAt = startsAt.ToString("O"),
                endsAt = endsAt.ToString("O")
            }),
            Note = $"Admin #{adminId} duyệt thanh toán thủ công giao dịch #{transaction.TransactionId} ({transaction.PartnerTransactionCode}) qua {transaction.GatewayProvider}.",
            CreatedAt = now
        });

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Admin #{AdminId} đã duyệt thanh toán thủ công giao dịch #{TransactionId}: Status {OldStatus} -> SUCCESS, StartsAt {StartsAt}, EndsAt {EndsAt}",
            adminId, transaction.TransactionId, oldStatus, startsAt, endsAt);

        try
        {
            await _redis.RemoveAsync($"vip_status:{transaction.UserId}", ct);
            await _redis.RemoveAsync($"user_benefits:{transaction.UserId}", ct);
            await _redis.RemoveAsync($"cache:profile:{transaction.UserId}", ct);
            await _redis.RemoveAsync("plans_cache", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi xóa bộ nhớ đệm Redis sau khi duyệt giao dịch #{TransactionId}.", transaction.TransactionId);
        }

        if (_emailService != null && user != null && !string.IsNullOrEmpty(user.Email))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendOtpEmailAsync(
                        user.Email,
                        $"Giao dịch #{transaction.TransactionId} thành công",
                        "PAYMENT_RECEIPT");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể gửi email biên lai cho người dùng {UserId}", user.UserId);
                }
            });
        }

        return new ApproveAdminPaymentResponseData
        {
            TransactionId = transaction.TransactionId,
            PartnerTransactionCode = transaction.PartnerTransactionCode,
            UserId = transaction.UserId,
            Status = transaction.TransactionStatus,
            VipStartsAt = startsAt,
            VipEndsAt = endsAt
        };
    }

    /// <summary>
    /// Features_JSON của gói khi tạo/sửa (D121 Q1-B): ba hạn mức VIP luôn theo SystemSettings.
    /// Request ghi hạn mức khác SystemSettings thì từ chối; khóa thiếu được bổ sung; các khóa khác giữ nguyên.
    /// </summary>
    private async Task<string> ResolvePlanFeaturesAsync(string? requestedJson, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(requestedJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(requestedJson);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    throw AppException.BadRequest(ErrorCodes.ErrValidation, "Quyền lợi gói (featuresJson) phải là một đối tượng JSON.");
                }
            }
            catch (JsonException)
            {
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Quyền lợi gói (featuresJson) không phải JSON hợp lệ.");
            }
        }

        var (chat, meal, fridge, barcode) = await VipPlanBenefitSync.ReadVipQuotasAsync(_db, ct);
        var expected = new[]
        {
            (Key: VipBenefitJson.ChatLimitDaily, Value: chat),
            (Key: VipBenefitJson.MealPlanLimitWeekly, Value: meal),
            (Key: VipBenefitJson.FridgeScanLimitDaily, Value: fridge),
            (Key: VipBenefitJson.BarcodeScanLimitDaily, Value: barcode)
        };
        if (expected.Any(q => VipBenefitJson.ReadQuota(requestedJson, q.Key) is int requested && requested != q.Value))
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Hạn mức VIP (Trợ lý AI, Thực đơn tuần, Quét tủ lạnh, Quét mã vạch) chỉnh tại Cấu hình hệ thống > Chỉ số hệ thống và áp dụng cho mọi gói, không sửa riêng từng gói.");
        }

        return VipBenefitJson.EnsureQuotas(requestedJson, chat, meal, fridge, barcode);
    }

    private async Task InvalidatePlanCacheAsync(CancellationToken ct)
    {
        try
        {
            await _redis.RemoveAsync("plans_cache", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể xóa bộ nhớ đệm Redis 'plans_cache'.");
        }
    }
}
