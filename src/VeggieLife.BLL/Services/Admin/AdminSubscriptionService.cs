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
                IsActive = p.IsActive
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

        var plan = new SubscriptionPlan
        {
            AdminId = adminId,
            PlanName = trimmedName,
            DurationDays = request.DurationDays,
            Price = request.PriceVnd,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
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
                priceVnd = request.PriceVnd,
                isActive = plan.IsActive
            }),
            Note = $"Tạo mới gói VIP {plan.PlanName}",
            CreatedAt = DateTime.UtcNow
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
            price = (long)plan.Price,
            isActive = plan.IsActive
        });

        if (request?.PriceVnd.HasValue == true)
        {
            plan.Price = request.PriceVnd.Value;
        }

        if (request?.IsActive.HasValue == true)
        {
            plan.IsActive = request.IsActive.Value;
        }

        plan.UpdatedAt = DateTime.UtcNow;

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
                price = (long)plan.Price,
                isActive = plan.IsActive
            }),
            Note = $"Cập nhật gói VIP {plan.PlanName}",
            CreatedAt = DateTime.UtcNow
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

        // Xóa cache Redis vip_status:{userId} và plans_cache
        try
        {
            await _redis.RemoveAsync($"vip_status:{transaction.UserId}", ct);
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

        if (fromDate.HasValue)
        {
            var fromUtc = fromDate.Value.Date;
            query = query.Where(pt => pt.CreatedAt >= fromUtc);
        }

        if (toDate.HasValue)
        {
            var toUtc = toDate.Value.Date.AddDays(1);
            query = query.Where(pt => pt.CreatedAt < toUtc);
        }

        var total = await query.CountAsync(ct);

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

        var meta = new PagedMeta(filter.Page, filter.Size, total);
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
