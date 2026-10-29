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
using VeggieLife.BLL.DTOs.Subscriptions.Requests;
using VeggieLife.BLL.DTOs.Subscriptions.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Email;
using VeggieLife.BLL.Services.Payment;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.Subscriptions;

/// <summary>
/// Dịch vụ quản lý các gói hội viên VIP và quy trình thanh toán MoMo / VietQR
/// Đáp ứng các quy tắc nghiệp vụ: BR-PAY-SUB-01, BR-PAY-SUB-02, BR-PAY-TXN-01, BR-PAY-TXN-02, BR-ADM-PAY-001.
/// </summary>
public class SubscriptionService : ISubscriptionService
{
    private readonly AppDbContext _db;
    private readonly IRedisService _redis;
    private readonly IPaymentGatewayService _paymentGateway;
    private readonly IEmailService _emailService;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        AppDbContext db,
        IRedisService redis,
        IPaymentGatewayService paymentGateway,
        IEmailService emailService,
        ILogger<SubscriptionService> logger)
    {
        _db = db;
        _redis = redis;
        _paymentGateway = paymentGateway;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// FN-SUB-01: Lấy danh sách gói VIP đang mở bán và bảng so sánh đặc quyền Free / VIP.
    /// Cache Redis plans_cache với TTL 10 phút.
    /// </summary>
    public async Task<ListSubscriptionPlansResponseDto> GetSubscriptionPlansAsync(CancellationToken ct = default)
    {
        const string cacheKey = "plans_cache";

        try
        {
            var cached = await _redis.GetAsync<ListSubscriptionPlansResponseDto>(cacheKey, ct);
            if (cached != null)
            {
                return cached;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[GetSubscriptionPlansAsync] Không thể đọc cache Redis, chuyển sang truy vấn trực tiếp cơ sở dữ liệu.");
        }

        var plans = await _db.SubscriptionPlans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.DurationDays)
            .Select(p => new SubscriptionPlanItemDto
            {
                PlanId = p.PlanId,
                PlanName = p.PlanName,
                DurationDays = p.DurationDays,
                PriceVnd = (long)p.Price,
                IsActive = p.IsActive
            })
            .ToListAsync(ct);

        var response = new ListSubscriptionPlansResponseDto
        {
            Plans = plans,
            Benefits = new SubscriptionBenefitsDto
            {
                Free = new BenefitDetailDto
                {
                    MealPlansPerWeek = 1,
                    ChatPerDay = 20,
                    FridgePhotosPerDay = 10
                },
                Vip = new BenefitDetailDto
                {
                    MealPlansPerWeek = 3,
                    ChatPerDay = null, // Không giới hạn (hoặc 200 câu/ngày theo BR-PAY-SUB-01)
                    FridgePhotosPerDay = 30
                }
            }
        };

        try
        {
            await _redis.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[GetSubscriptionPlansAsync] Không thể ghi cache Redis plans_cache.");
        }

        return response;
    }

    /// <summary>
    /// FN-SUB-02: Khởi tạo đơn thanh toán gói VIP qua MoMo hoặc VietQR.
    /// Giá lấy từ DB máy chủ (BR-ADM-PAY-001), chặn đơn PENDING chưa quá 5 phút (BR-PAY-TXN-01).
    /// </summary>
    public async Task<CheckoutSubscriptionResponseDto> CheckoutSubscriptionAsync(
        int userId,
        string userRole,
        CheckoutSubscriptionRequest request,
        CancellationToken ct = default)
    {
        // 1. Kiểm tra quyền hạn: Từ chối khách vãng lai gọi tính năng cá nhân hóa (FN-AUTH-03)
        if (string.Equals(userRole, UserRoles.Guest, StringComparison.OrdinalIgnoreCase))
        {
            throw AppException.Forbidden(
                ErrorCodes.ErrGuestPersonalizationRestricted,
                "Khách vãng lai không thể mua gói (FN-AUTH-03).");
        }

        // 2. Kiểm tra cổng thanh toán hợp lệ
        var gateway = request.Gateway?.Trim().ToUpperInvariant() ?? string.Empty;
        if (gateway != "MOMO" && gateway != "VIETQR")
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Cổng thanh toán không hợp lệ. Vui lòng chọn MOMO hoặc VIETQR.");
        }

        // 3. Kiểm tra gói VIP tồn tại và đang mở bán (Giá lấy từ DB, không nhận từ client - BR-ADM-PAY-001)
        var plan = await _db.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlanId == request.PlanId, ct);

        if (plan == null || !plan.IsActive)
        {
            throw AppException.NotFound(
                ErrorCodes.ErrPlanNotFound,
                "Gói không tồn tại hoặc ngừng bán.");
        }

        // 4. Kiểm tra đơn chờ thanh toán chưa hết hạn (trong vòng 5 phút, BR-PAY-TXN-01)
        var now = DateTime.UtcNow;
        var pendingCutoff = now.AddMinutes(-5);

        var pendingTx = await _db.PaymentTransactions
            .Where(pt => pt.UserId == userId &&
                         pt.TransactionStatus == "PENDING" &&
                         pt.CreatedAt >= pendingCutoff)
            .OrderByDescending(pt => pt.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (pendingTx != null)
        {
            // Tự động kiểm tra trạng thái trên PayOS:
            // Nếu người dùng đã bấm 'Hủy' trên trang PayOS, PayOS đánh dấu CANCELLED,
            // ta lập tức giải phóng đơn để người dùng không bị kẹt 5 phút.
            if (string.Equals(pendingTx.GatewayProvider, "VIETQR", StringComparison.OrdinalIgnoreCase))
            {
                var payOsClient = _paymentGateway.GetPayOSClient();
                if (payOsClient != null)
                {
                    var orderCodeToLog = pendingTx.PartnerTransactionCode;
                    try
                    {
                        var digits = new string(pendingTx.PartnerTransactionCode.Where(char.IsDigit).ToArray());
                        var paymentLinkId = !string.IsNullOrWhiteSpace(pendingTx.SignatureHash)
                            ? pendingTx.SignatureHash
                            : digits;

                        if (!string.IsNullOrWhiteSpace(paymentLinkId))
                        {
                            var info = await payOsClient.PaymentRequests.GetAsync(paymentLinkId);
                            var statusStr = info != null ? info.Status.ToString() : null;
                            if (string.Equals(statusStr, "CANCELLED", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(statusStr, "EXPIRED", StringComparison.OrdinalIgnoreCase))
                            {
                                pendingTx.TransactionStatus = statusStr!.ToUpperInvariant();
                                pendingTx.CompletedAt = now;
                                await _db.SaveChangesAsync(ct);
                                pendingTx = null; // Đã hủy bên PayOS, cho phép tạo đơn mới ngay lập tức!
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[CheckoutSubscriptionAsync] Không thể kiểm tra trạng thái đơn PayOS {OrderCode}", orderCodeToLog);
                    }
                }
            }

            if (pendingTx != null)
            {
                throw AppException.Conflict(
                    ErrorCodes.ErrPaymentPendingExists,
                    "Đang có đơn chờ thanh toán chưa hết hạn.");
            }
        }

        // 5. Tính thời gian bắt đầu và kết thúc dự kiến của kỳ VIP:
        // Nếu người dùng VIP còn hạn: nối tiếp từ Ends_At lớn nhất, ngược lại bắt đầu từ hiện tại
        var maxSuccessEndsAt = await _db.PaymentTransactions
            .Where(pt => pt.UserId == userId &&
                         pt.TransactionStatus == "SUCCESS" &&
                         pt.EndsAt > now)
            .MaxAsync(pt => (DateTime?)pt.EndsAt, ct);

        DateTime startsAt = maxSuccessEndsAt.HasValue && maxSuccessEndsAt.Value > now
            ? maxSuccessEndsAt.Value
            : now;

        DateTime endsAt = startsAt.AddDays(plan.DurationDays);
        // BR-PAY-SUB-02: Gói thuê bao VIP tự động hết hạn vào 23:59:59 của ngày kết thúc chu kỳ thanh toán
        endsAt = new DateTime(endsAt.Year, endsAt.Month, endsAt.Day, 23, 59, 59, DateTimeKind.Utc);

        // 6. Sinh mã đơn hàng duy nhất theo định dạng VL_{payOsOrderCode} tương thích PayOS (dưới 9007199254740991)
        long payOsOrderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var orderCode = $"VL_{payOsOrderCode}";

        // 7. Gọi cổng thanh toán lấy liên kết hoặc mã QR
        PaymentCheckoutResult checkoutResult;
        try
        {
            if (gateway == "MOMO")
            {
                checkoutResult = await _paymentGateway.CreateMoMoPaymentAsync(
                    0,
                    plan.Price,
                    $"Nâng cấp {plan.PlanName} ({orderCode})",
                    "/payment/callback",
                    ct);
            }
            else
            {
                checkoutResult = await _paymentGateway.CreateVietQrPaymentAsync(
                    0,
                    plan.Price,
                    orderCode,
                    ct);
            }
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CheckoutSubscriptionAsync] Cổng thanh toán {Gateway} gặp sự cố khi tạo đơn.", gateway);
            throw AppException.BadGateway(ErrorCodes.ErrExtGatewayFailed, "Cổng thanh toán lỗi.");
        }

        var payUrl = !string.IsNullOrEmpty(checkoutResult.PaymentUrl) ? checkoutResult.PaymentUrl : checkoutResult.QrCodeUrl;

        // 8. Lưu PaymentTransactions ở trạng thái PENDING
        var transaction = new PaymentTransaction
        {
            PlanId = plan.PlanId,
            UserId = userId,
            GatewayProvider = gateway,
            PartnerTransactionCode = orderCode,
            AmountVnd = plan.Price,
            TransactionStatus = "PENDING",
            SignatureHash = checkoutResult.PartnerTransactionId ?? string.Empty,
            CreatedAt = now,
            StartsAt = startsAt,
            EndsAt = endsAt,
            ReceiptUrl = payUrl
        };

        _db.PaymentTransactions.Add(transaction);

        // 9. Ghi AuditLogs
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = userId,
            ActorRole = userRole,
            Action = "CHECKOUT_SUBSCRIPTION",
            TargetTable = "PaymentTransactions",
            TargetId = orderCode,
            NewValue = JsonSerializer.Serialize(new { planId = plan.PlanId, amount = plan.Price, gateway }),
            Note = $"Khởi tạo đơn nâng cấp VIP {plan.PlanName} qua {gateway}",
            CreatedAt = now
        });

        await _db.SaveChangesAsync(ct);

        // 10. Đặt Redis payment_expire:{transactionId} TTL 5 phút
        try
        {
            await _redis.SetAsync($"payment_expire:{transaction.TransactionId}", orderCode, TimeSpan.FromMinutes(5), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CheckoutSubscriptionAsync] Không thể ghi khóa hết hạn Redis cho đơn #{TransactionId}", transaction.TransactionId);
        }

        return new CheckoutSubscriptionResponseDto
        {
            TransactionId = transaction.TransactionId,
            OrderCode = orderCode,
            AmountVnd = (long)plan.Price,
            PayUrl = payUrl,
            ExpiresAt = now.AddMinutes(5)
        };
    }

    /// <summary>
    /// FN-SUB-03: Tiếp nhận và xử lý kết quả thanh toán từ cổng qua Webhook IPN.
    /// Xác thực chữ ký số HMAC-SHA256 (BR-PAY-TXN-02), xử lý bất biến (idempotent), đối soát số tiền và kích hoạt VIP.
    /// </summary>
    public async Task HandlePaymentIpnAsync(
        string gateway,
        HandlePaymentIpnRequest request,
        CancellationToken ct = default)
    {
        var gw = gateway?.Trim().ToLowerInvariant() ?? string.Empty;
        if (gw != "momo" && gw != "vietqr")
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Cổng thanh toán không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(request.OrderCode) ||
            string.IsNullOrWhiteSpace(request.PartnerTransactionId) ||
            request.Amount <= 0 ||
            string.IsNullOrWhiteSpace(request.Signature))
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Dữ liệu đầu vào không hợp lệ.");
        }

        // 1. Xác thực chữ ký HMAC-SHA256 theo khóa của từng cổng; sai chữ ký thì từ chối (BR-PAY-TXN-02)
        var requestData = new Dictionary<string, string>
        {
            ["orderCode"] = request.OrderCode,
            ["partnerTransactionId"] = request.PartnerTransactionId,
            ["amount"] = request.Amount.ToString(),
            ["resultCode"] = request.ResultCode.ToString()
        };

        if (!_paymentGateway.VerifySignature(gw, requestData, request.Signature))
        {
            _logger.LogWarning("[HandlePaymentIpnAsync] Chữ ký số HMAC-SHA256 không hợp lệ từ cổng {Gateway} cho đơn {OrderCode}", gw, request.OrderCode);
            throw AppException.Unauthorized(
                ErrorCodes.ErrPayInvalidSignature,
                "Chữ ký không hợp lệ.");
        }

        // 2. Tìm giao dịch theo PartnerTransactionCode
        var transaction = await _db.PaymentTransactions
            .Include(pt => pt.Plan)
            .Include(pt => pt.User)
            .FirstOrDefaultAsync(pt => pt.PartnerTransactionCode == request.OrderCode, ct);

        if (transaction == null)
        {
            throw AppException.NotFound(
                ErrorCodes.ErrPayOrderNotFound,
                "Không tìm thấy giao dịch.");
        }

        // 3. Cơ chế bất biến (Idempotent Consumer, BR-PAY-TXN-02):
        // Nếu giao dịch đã ở trạng thái SUCCESS -> trả về OK ngay, không cộng hạn lần hai
        if (string.Equals(transaction.TransactionStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("[HandlePaymentIpnAsync] Giao dịch {OrderCode} đã được xử lý SUCCESS trước đó (Idempotent). Bỏ qua không cộng hạn lần hai.", request.OrderCode);
            return;
        }

        // Nếu giao dịch không còn ở trạng thái PENDING -> Đã xử lý hoặc đã kết thúc
        if (!string.Equals(transaction.TransactionStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
        {
            throw AppException.Conflict(
                ErrorCodes.ErrPayOrderExpired,
                "Giao dịch đã hết hạn hoặc đã xử lý.");
        }

        // Kiểm tra quá hạn 5 phút
        var now = DateTime.UtcNow;
        if (transaction.CreatedAt.AddMinutes(5) < now)
        {
            transaction.TransactionStatus = "EXPIRED";
            transaction.CompletedAt = now;
            await _db.SaveChangesAsync(ct);

            throw AppException.Conflict(
                ErrorCodes.ErrPayOrderExpired,
                "Giao dịch đã hết hạn hoặc đã xử lý.");
        }

        // 4. Đối soát số tiền khớp Amount_VND; lệch thì ghi FAILED và cảnh báo Admin
        if ((decimal)request.Amount != transaction.AmountVnd)
        {
            transaction.TransactionStatus = "FAILED";
            transaction.CompletedAt = now;
            transaction.SignatureHash = request.Signature;

            _logger.LogError("[HandlePaymentIpnAsync] CẢNH BÁO ADMIN: Số tiền không khớp đơn hàng {OrderCode}. Nhận: {Received}, Kỳ vọng: {Expected}",
                request.OrderCode, request.Amount, transaction.AmountVnd);

            _db.AuditLogs.Add(new AuditLog
            {
                ActorUserId = transaction.UserId,
                ActorRole = "SYSTEM",
                Action = "PAYMENT_AMOUNT_MISMATCH",
                TargetTable = "PaymentTransactions",
                TargetId = transaction.TransactionId.ToString(),
                NewValue = JsonSerializer.Serialize(new { receivedAmount = request.Amount, expectedAmount = transaction.AmountVnd }),
                Note = "Lệch số tiền thanh toán từ Webhook IPN.",
                CreatedAt = now
            });

            await _db.SaveChangesAsync(ct);

            throw AppException.UnprocessableEntity(
                ErrorCodes.ErrPayAmountMismatch,
                "Số tiền không khớp đơn.");
        }

        // 5. Kiểm tra kết quả thanh toán
        if (request.ResultCode != 0)
        {
            // Cổng báo thanh toán thất bại hoặc người dùng hủy
            transaction.TransactionStatus = "FAILED";
            transaction.CompletedAt = now;
            transaction.SignatureHash = request.Signature;

            _db.AuditLogs.Add(new AuditLog
            {
                ActorUserId = transaction.UserId,
                ActorRole = "SYSTEM",
                Action = "PAYMENT_FAILED",
                TargetTable = "PaymentTransactions",
                TargetId = transaction.TransactionId.ToString(),
                NewValue = JsonSerializer.Serialize(new { resultCode = request.ResultCode }),
                Note = "Thanh toán thất bại từ cổng trung gian.",
                CreatedAt = now
            });

            await _db.SaveChangesAsync(ct);
            return;
        }

        // 6. Thanh toán THÀNH CÔNG: Kích hoạt VIP và cập nhật thời hạn (Transaction atomic)
        // Tính lại Starts_At: nối tiếp kỳ VIP còn hạn hoặc hiện tại
        var activeMaxEndsAt = await _db.PaymentTransactions
            .Where(pt => pt.UserId == transaction.UserId &&
                         pt.TransactionId != transaction.TransactionId &&
                         pt.TransactionStatus == "SUCCESS" &&
                         pt.EndsAt > now)
            .MaxAsync(pt => (DateTime?)pt.EndsAt, ct);

        DateTime startsAt = activeMaxEndsAt.HasValue && activeMaxEndsAt.Value > now
            ? activeMaxEndsAt.Value
            : now;

        DateTime endsAt = startsAt.AddDays(transaction.Plan.DurationDays);
        // BR-PAY-SUB-02: Hết hạn vào 23:59:59 của ngày cuối
        endsAt = new DateTime(endsAt.Year, endsAt.Month, endsAt.Day, 23, 59, 59, DateTimeKind.Utc);

        transaction.TransactionStatus = "SUCCESS";
        transaction.CompletedAt = now;
        transaction.SignatureHash = request.Signature;
        transaction.StartsAt = startsAt;
        transaction.EndsAt = endsAt;

        // Cập nhật vai trò người dùng thành VIP (giữ nguyên nếu là ADMIN)
        var user = transaction.User;
        if (!string.Equals(user.Role, UserRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            user.Role = UserRoles.Vip;
        }
        user.UpdatedAt = now;

        // Ghi AuditLogs
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = user.UserId,
            ActorRole = "SYSTEM",
            Action = "PAYMENT_SUCCESS",
            TargetTable = "PaymentTransactions",
            TargetId = transaction.TransactionId.ToString(),
            NewValue = JsonSerializer.Serialize(new { role = "VIP", status = "SUCCESS", startsAt = startsAt.ToString("O"), endsAt = endsAt.ToString("O") }),
            Note = $"Kích hoạt VIP thành công gói {transaction.Plan.PlanName} qua {gw}.",
            CreatedAt = now
        });

        await _db.SaveChangesAsync(ct);

        // Làm mới cache VIP của người dùng: Xóa vip_status:{userId} và cache:profile:{userId}
        try
        {
            await _redis.RemoveAsync($"vip_status:{user.UserId}", ct);
            await _redis.RemoveAsync($"cache:profile:{user.UserId}", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[HandlePaymentIpnAsync] Không thể xóa cache vip_status hoặc profile cho user {UserId}", user.UserId);
        }

        // Gửi email biên lai bất đồng bộ (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try
            {
                if (!string.IsNullOrEmpty(user.Email))
                {
                    await _emailService.SendOtpEmailAsync(
                        user.Email,
                        $"Giao dịch #{transaction.TransactionId} thành công",
                        "PAYMENT_RECEIPT");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[HandlePaymentIpnAsync] Không thể gửi email biên lai cho người dùng {UserId}", user.UserId);
            }
        });
    }

    /// <summary>
    /// Tiếp nhận và xử lý Webhook tự động từ PayOS VietQR Napas247 (xác thực chữ ký ChecksumKey HMAC-SHA256, tự động kích hoạt VIP).
    /// </summary>
    public async Task HandlePayOsWebhookAsync(
        PayOS.Models.Webhooks.Webhook webhook,
        CancellationToken ct = default)
    {
        if (webhook == null)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Webhook data không hợp lệ.");
        }

        // 1. Xác thực chữ ký số bằng PayOS SDK nếu có PayOSClient
        var payOsClient = _paymentGateway.GetPayOSClient();
        if (payOsClient != null)
        {
            try
            {
                await payOsClient.Webhooks.VerifyAsync(webhook);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[HandlePayOsWebhookAsync] Chữ ký Webhook PayOS không hợp lệ: Signature={Signature}", webhook.Signature);
                throw AppException.Unauthorized(ErrorCodes.ErrPayInvalidSignature, "Chữ ký PayOS không hợp lệ.");
            }
        }

        // 2. Kiểm tra nếu là webhook ping xác thực URL của PayOS Dashboard
        if (webhook.Data == null)
        {
            _logger.LogInformation("[HandlePayOsWebhookAsync] Nhận Webhook ping xác thực URL thành công từ PayOS.");
            return;
        }

        var orderCodeStr = webhook.Data.OrderCode.ToString();

        // 3. Tìm giao dịch trong DB theo PartnerTransactionCode
        var transaction = await _db.PaymentTransactions
            .Include(pt => pt.Plan)
            .Include(pt => pt.User)
            .FirstOrDefaultAsync(pt =>
                pt.PartnerTransactionCode == orderCodeStr ||
                pt.PartnerTransactionCode == $"VL_{orderCodeStr}" ||
                pt.PartnerTransactionCode.EndsWith(orderCodeStr), ct);

        if (transaction == null)
        {
            // Nếu là đơn test từ hệ thống hoặc ping test Webhook trên Dashboard PayOS
            if (webhook.Data.OrderCode <= 1000 ||
                (webhook.Data.Description?.Contains("test", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                _logger.LogInformation("[HandlePayOsWebhookAsync] Bỏ qua đơn test PayOS: {OrderCode}, Description: {Description}",
                    webhook.Data.OrderCode, webhook.Data.Description);
                return;
            }

            throw AppException.NotFound(ErrorCodes.ErrPayOrderNotFound, "Không tìm thấy giao dịch tương ứng với đơn hàng.");
        }

        // 4. Cơ chế bất biến (Idempotent Consumer, BR-PAY-TXN-02):
        if (string.Equals(transaction.TransactionStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("[HandlePayOsWebhookAsync] Giao dịch {OrderCode} đã được xử lý SUCCESS trước đó (Idempotent). Bỏ qua không cộng hạn lần hai.", transaction.PartnerTransactionCode);
            return;
        }

        // 5. Kiểm tra nếu đơn hàng đã bị hủy hoặc xử lý trước đó
        var isExpired = string.Equals(transaction.TransactionStatus, "EXPIRED", StringComparison.OrdinalIgnoreCase);
        var isPending = string.Equals(transaction.TransactionStatus, "PENDING", StringComparison.OrdinalIgnoreCase);

        if (!isPending && !isExpired)
        {
            throw AppException.Conflict(ErrorCodes.ErrPayOrderExpired, "Giao dịch đã kết thúc hoặc không ở trạng thái hợp lệ để xử lý.");
        }

        var now = DateTime.UtcNow;

        // Nếu đơn hàng đã bị EXPIRED, kiểm tra thời gian ân hạn (Grace Period 30 phút tính từ lúc tạo đơn)
        if (isExpired)
        {
            var isWithinGracePeriod = transaction.CreatedAt >= now.AddMinutes(-30);
            if (!isWithinGracePeriod)
            {
                // Giao dịch thanh toán trễ bất thường (> 30 phút kể từ lúc tạo đơn).
                // Không tự động lên VIP để chống trục lợi/rủi ro tranh chấp giá.
                // Lưu AuditLog chuyển sang hàng đợi Admin Reconcile (COMPENSATE hoặc REFUND).
                _logger.LogWarning("[HandlePayOsWebhookAsync] GIAO DỊCH BẤT THƯỜNG / CHUYỂN TIỀN TRỄ: Giao dịch {OrderCode} đã EXPIRED quá 30 phút (tạo lúc {CreatedAt:yyyy-MM-dd HH:mm:ss} UTC). PayOS báo nhận {Amount} VND. Không tự động lên VIP, chuyển sang hàng đợi Admin Reconcile.",
                    transaction.PartnerTransactionCode, transaction.CreatedAt, webhook.Data.Amount);

                _db.AuditLogs.Add(new AuditLog
                {
                    ActorUserId = transaction.UserId,
                    ActorRole = "SYSTEM",
                    Action = "PAYOS_LATE_PAYMENT_DETECTED",
                    TargetTable = "PaymentTransactions",
                    TargetId = transaction.PartnerTransactionCode,
                    NewValue = JsonSerializer.Serialize(new
                    {
                        transactionId = transaction.TransactionId,
                        orderCode = transaction.PartnerTransactionCode,
                        amount = webhook.Data.Amount,
                        reference = webhook.Data.Reference,
                        createdAt = transaction.CreatedAt,
                        receivedAt = now
                    }),
                    Note = $"Thanh toán trễ quá 30 phút ({(now - transaction.CreatedAt).TotalMinutes:F1} phút). Tiền đã vào tài khoản PayOS, chuyển sang hàng đợi Admin Reconcile để xem xét COMPENSATE hoặc REFUND.",
                    CreatedAt = now
                });

                await _db.SaveChangesAsync(ct);
                return;
            }

            _logger.LogInformation("[HandlePayOsWebhookAsync] Giao dịch {OrderCode} trước đó bị EXPIRED do quá hạn 5 phút nhưng nằm trong Grace Period 30 phút ({ElapsedMinutes:F1} phút). Tự động phục hồi và xử lý kích hoạt VIP.",
                transaction.PartnerTransactionCode, (now - transaction.CreatedAt).TotalMinutes);
        }

        // 6. Đối soát số tiền
        if (webhook.Data.Amount < (long)transaction.AmountVnd)
        {
            _logger.LogError("[HandlePayOsWebhookAsync] CẢNH BÁO ADMIN: Số tiền không khớp đơn hàng {OrderCode}. Nhận: {Received}, Kỳ vọng: {Expected}",
                transaction.PartnerTransactionCode, webhook.Data.Amount, transaction.AmountVnd);
            throw AppException.Conflict(ErrorCodes.ErrPayAmountMismatch, "Số tiền thanh toán không khớp đơn hàng.");
        }

        // 7. Kiểm tra mã kết quả từ PayOS (code "00" = thành công)
        if (!string.Equals(webhook.Data.Code, "00", StringComparison.OrdinalIgnoreCase))
        {
            transaction.TransactionStatus = "FAILED";
            transaction.CompletedAt = now;
            await _db.SaveChangesAsync(ct);
            return;
        }

        // 8. Cập nhật trạng thái SUCCESS
        transaction.TransactionStatus = "SUCCESS";
        transaction.CompletedAt = now;
        transaction.SignatureHash = webhook.Signature ?? string.Empty;

        // 9. Kích hoạt hoặc gia hạn VIP (BR-PAY-SUB-01, BR-PAY-SUB-02)
        var user = transaction.User;
        var plan = transaction.Plan;
        DateTime startsAt;
        DateTime endsAt;

        if (string.Equals(user.Role, "VIP", StringComparison.OrdinalIgnoreCase) && transaction.EndsAt > now)
        {
            startsAt = transaction.StartsAt;
            endsAt = transaction.EndsAt.AddDays(plan.DurationDays);
        }
        else
        {
            startsAt = now;
            endsAt = now.AddDays(plan.DurationDays);
            user.Role = "VIP";
        }

        endsAt = new DateTime(endsAt.Year, endsAt.Month, endsAt.Day, 23, 59, 59, DateTimeKind.Utc);
        transaction.StartsAt = startsAt;
        transaction.EndsAt = endsAt;

        // 10. Ghi AuditLog
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = user.UserId,
            ActorRole = user.Role,
            Action = "PAYOS_IPN_SUCCESS",
            TargetTable = "PaymentTransactions",
            TargetId = transaction.PartnerTransactionCode,
            NewValue = JsonSerializer.Serialize(new
            {
                transactionId = transaction.TransactionId,
                amount = transaction.AmountVnd,
                planId = plan.PlanId,
                vipStartsAt = startsAt,
                vipEndsAt = endsAt,
                reference = webhook.Data.Reference
            }),
            Note = $"Thanh toán PayOS VietQR tự động thành công cho đơn {transaction.PartnerTransactionCode}. Kích hoạt VIP đến {endsAt:yyyy-MM-dd HH:mm:ss} UTC.",
            CreatedAt = now
        });

        await _db.SaveChangesAsync(ct);

        // 11. Xóa cache Redis vip_status:{userId} và cache:profile:{userId}
        try
        {
            await _redis.RemoveAsync($"vip_status:{user.UserId}", ct);
            await _redis.RemoveAsync($"cache:profile:{user.UserId}", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[HandlePayOsWebhookAsync] Không thể xóa cache vip_status hoặc profile cho user {UserId}", user.UserId);
        }

        // 12. Gửi email biên lai (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try
            {
                if (!string.IsNullOrEmpty(user.Email))
                {
                    await _emailService.SendOtpEmailAsync(
                        user.Email,
                        $"Giao dịch #{transaction.TransactionId} thành công qua PayOS VietQR",
                        "PAYMENT_RECEIPT");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[HandlePayOsWebhookAsync] Không thể gửi email biên lai cho người dùng {UserId}", user.UserId);
            }
        });
    }

    /// <summary>
    /// FN-SUB-04: Lấy trạng thái gói VIP hiện tại và toàn bộ lịch sử giao dịch của người dùng.
    /// VIP hết hạn thì isVip = false (BR-PAY-SUB-02). Cache Redis vip_status:{userId} với TTL 5m.
    /// </summary>
    public async Task<CurrentUserSubscriptionResponseDto> GetCurrentUserSubscriptionAsync(
        int userId,
        CancellationToken ct = default)
    {
        var cacheKey = $"vip_status:{userId}";

        try
        {
            var cached = await _redis.GetAsync<CurrentUserSubscriptionResponseDto>(cacheKey, ct);
            if (cached != null)
            {
                return cached;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[GetCurrentUserSubscriptionAsync] Không thể đọc cache Redis cho user {UserId}", userId);
        }

        var now = DateTime.UtcNow;

        var transactions = await _db.PaymentTransactions
            .AsNoTracking()
            .Include(pt => pt.Plan)
            .Where(pt => pt.UserId == userId)
            .OrderByDescending(pt => pt.CreatedAt)
            .ToListAsync(ct);

        // Tìm PaymentTransactions SUCCESS có Starts_At <= hiện tại < Ends_At; không có thì isVip = false
        var activeVipTx = transactions
            .Where(pt => string.Equals(pt.TransactionStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase) &&
                         pt.StartsAt <= now && now < pt.EndsAt)
            .OrderByDescending(pt => pt.EndsAt)
            .FirstOrDefault();

        bool isVip = activeVipTx != null;
        SubscriptionPlanItemDto? activePlanDto = null;
        DateTime? endsAt = null;

        if (activeVipTx != null)
        {
            activePlanDto = new SubscriptionPlanItemDto
            {
                PlanId = activeVipTx.Plan.PlanId,
                PlanName = activeVipTx.Plan.PlanName,
                DurationDays = activeVipTx.Plan.DurationDays,
                PriceVnd = (long)activeVipTx.Plan.Price,
                IsActive = activeVipTx.Plan.IsActive
            };
            endsAt = activeVipTx.EndsAt;
        }

        var txDtos = transactions.Select(pt => new SubscriptionTransactionDto
        {
            TransactionId = pt.TransactionId,
            Gateway = pt.GatewayProvider,
            AmountVnd = (long)pt.AmountVnd,
            Status = pt.TransactionStatus,
            CreatedAt = pt.CreatedAt
        }).ToList();

        var result = new CurrentUserSubscriptionResponseDto
        {
            IsVip = isVip,
            Plan = activePlanDto,
            EndsAt = endsAt,
            Transactions = txDtos
        };

        try
        {
            await _redis.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[GetCurrentUserSubscriptionAsync] Không thể ghi cache Redis cho user {UserId}", userId);
        }

        return result;
    }

    /// <summary>
    /// Hủy đơn thanh toán đang chờ của người dùng để giải phóng và cho phép tạo đơn mới ngay lập tức.
    /// </summary>
    public async Task<CancelPendingSubscriptionResponseDto> CancelPendingSubscriptionAsync(
        int userId,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var pendingCutoff = now.AddMinutes(-5);

        var pendingTx = await _db.PaymentTransactions
            .Where(pt => pt.UserId == userId &&
                         pt.TransactionStatus == "PENDING" &&
                         pt.CreatedAt >= pendingCutoff)
            .OrderByDescending(pt => pt.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (pendingTx == null)
        {
            return new CancelPendingSubscriptionResponseDto
            {
                Cancelled = false,
                Message = "Không có đơn chờ thanh toán nào cần hủy."
            };
        }

        pendingTx.TransactionStatus = "CANCELLED";
        pendingTx.CompletedAt = now;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("[CancelPendingSubscriptionAsync] Người dùng {UserId} đã hủy đơn chờ {TxId} ({PartnerCode})",
            userId, pendingTx.TransactionId, pendingTx.PartnerTransactionCode);

        try
        {
            await _redis.RemoveAsync($"cache:user_sub:{userId}", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CancelPendingSubscriptionAsync] Không thể xóa cache Redis cho user {UserId}", userId);
        }

        return new CancelPendingSubscriptionResponseDto
        {
            Cancelled = true,
            Message = "Đã hủy đơn chờ thanh toán thành công. Bạn có thể tiến hành tạo đơn mới."
        };
    }
}
