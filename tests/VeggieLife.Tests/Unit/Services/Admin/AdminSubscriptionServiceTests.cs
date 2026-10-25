using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.Subscriptions;
using VeggieLife.BLL.Services.Admin;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Admin;

/// <summary>
/// Unit Tests cho dịch vụ Quản trị gói VIP và đối soát giao dịch (FN-ADM-13).
/// Kiểm thử đầy đủ các quy tắc:
/// - BR-ADM-PAY-001: Quản lý gói VIP, giá mới không áp hồi tố.
/// - BR-ADM-PAY-002: Đối soát giao dịch (COMPENSATE / REFUND), mỗi giao dịch chỉ đối soát 1 lần.
/// - BR-ADM-SYS-002: Ghi nhật ký kiểm toán AuditLogs.
/// </summary>
public class AdminSubscriptionServiceTests
{
    private readonly AppDbContext _db;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly AdminSubscriptionService _service;
    private const int AdminId = 1;

    public AdminSubscriptionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("AdminSubscriptionServiceTestsDb_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _service = new AdminSubscriptionService(_db, _mockRedis.Object, NullLogger<AdminSubscriptionService>.Instance);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var admin = new User
        {
            UserId = AdminId,
            Email = "admin@veggielife.vn",
            FullName = "Admin Quản Trị",
            Role = UserRoles.Admin,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };

        var member = new User
        {
            UserId = 2,
            Email = "member@veggielife.vn",
            FullName = "Người Dùng Thường",
            Role = UserRoles.Member,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };

        var vipUser = new User
        {
            UserId = 3,
            Email = "vip@veggielife.vn",
            FullName = "Người Dùng VIP",
            Role = UserRoles.Vip,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.AddRange(admin, member, vipUser);

        var plan1 = new SubscriptionPlan
        {
            PlanId = 1,
            AdminId = AdminId,
            PlanName = "VIP 1 tháng",
            DurationDays = 30,
            Price = 49000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var plan2 = new SubscriptionPlan
        {
            PlanId = 2,
            AdminId = AdminId,
            PlanName = "VIP 12 tháng",
            DurationDays = 365,
            Price = 449000,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.SubscriptionPlans.AddRange(plan1, plan2);

        // Giao dịch lỗi của member (đã trừ tiền MoMo nhưng hệ thống lỗi PENDING)
        var txnFailed = new PaymentTransaction
        {
            TransactionId = 1001,
            PlanId = 1,
            UserId = 2,
            GatewayProvider = "MOMO",
            PartnerTransactionCode = "VL_20261022_0001",
            AmountVnd = 49000,
            TransactionStatus = "PENDING",
            SignatureHash = "sig_hash_1",
            CreatedAt = DateTime.UtcNow.AddMinutes(-30),
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddDays(30)
        };

        // Giao dịch thành công của vipUser
        var txnSuccess = new PaymentTransaction
        {
            TransactionId = 1002,
            PlanId = 1,
            UserId = 3,
            GatewayProvider = "VIETQR",
            PartnerTransactionCode = "VL_20261022_0002",
            AmountVnd = 49000,
            TransactionStatus = "SUCCESS",
            SignatureHash = "sig_hash_2",
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            CompletedAt = DateTime.UtcNow.AddDays(-5),
            StartsAt = DateTime.UtcNow.AddDays(-5),
            EndsAt = DateTime.UtcNow.AddDays(25)
        };

        _db.PaymentTransactions.AddRange(txnFailed, txnSuccess);
        _db.SaveChanges();
    }

    // ==========================================
    // 1. GET ALL SUBSCRIPTION PLANS
    // ==========================================

    [Fact]
    public async Task GetAllSubscriptionPlansAsync_ReturnsAllPlansOrdered()
    {
        // Act
        var result = await _service.GetAllSubscriptionPlansAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].PlanName.Should().Be("VIP 1 tháng");
        result[0].PriceVnd.Should().Be(49000);
        result[0].IsActive.Should().BeTrue();

        result[1].PlanName.Should().Be("VIP 12 tháng");
        result[1].IsActive.Should().BeFalse();
    }

    // ==========================================
    // 2. CREATE SUBSCRIPTION PLAN
    // ==========================================

    [Fact]
    public async Task CreateSubscriptionPlanAsync_ValidRequest_CreatesPlanAndAuditLog()
    {
        // Arrange
        var request = new CreateAdminSubscriptionPlanRequest
        {
            PlanName = "VIP 3 tháng",
            DurationDays = 90,
            PriceVnd = 129000
        };

        // Act
        var result = await _service.CreateSubscriptionPlanAsync(request, AdminId);

        // Assert
        result.Should().NotBeNull();
        result.PlanId.Should().BeGreaterThan(0);

        var created = await _db.SubscriptionPlans.FirstOrDefaultAsync(p => p.PlanId == result.PlanId);
        created.Should().NotBeNull();
        created!.PlanName.Should().Be("VIP 3 tháng");
        created.Price.Should().Be(129000);
        created.DurationDays.Should().Be(90);
        created.IsActive.Should().BeTrue();

        // Kiểm tra AuditLogs
        var audit = await _db.AuditLogs.FirstOrDefaultAsync(a =>
            a.TargetTable == "SubscriptionPlans" &&
            a.TargetId == created.PlanId.ToString() &&
            a.Action == "UPDATE_VIP_PRICE");
        audit.Should().NotBeNull();
        audit!.ActorUserId.Should().Be(AdminId);

        // Kiểm tra xóa cache
        _mockRedis.Verify(r => r.RemoveAsync("plans_cache", default), Times.Once);
    }

    [Fact]
    public async Task CreateSubscriptionPlanAsync_DuplicateName_ThrowsConflictErrPlanDuplicate()
    {
        // Arrange
        var request = new CreateAdminSubscriptionPlanRequest
        {
            PlanName = "vip 1 tháng", // trùng hoa thường
            DurationDays = 30,
            PriceVnd = 50000
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            _service.CreateSubscriptionPlanAsync(request, AdminId));

        ex.ErrorCode.Should().Be(ErrorCodes.ErrPlanDuplicate);
        ex.StatusCode.Should().Be(409);
    }

    [Theory]
    [InlineData("", 30, 10000)]
    [InlineData("VIP", 0, 10000)]
    [InlineData("VIP", 30, 0)]
    [InlineData("VIP", 30, -500)]
    public async Task CreateSubscriptionPlanAsync_InvalidInput_ThrowsBadRequestErrValidation(
        string planName, int durationDays, long priceVnd)
    {
        // Arrange
        var request = new CreateAdminSubscriptionPlanRequest
        {
            PlanName = planName,
            DurationDays = durationDays,
            PriceVnd = priceVnd
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            _service.CreateSubscriptionPlanAsync(request, AdminId));

        ex.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
        ex.StatusCode.Should().Be(400);
    }

    // ==========================================
    // 3. UPDATE SUBSCRIPTION PLAN
    // ==========================================

    [Fact]
    public async Task UpdateSubscriptionPlanAsync_ValidPriceAndIsActive_UpdatesPlanAndAuditLog()
    {
        // Arrange
        var request = new UpdateAdminSubscriptionPlanRequest
        {
            PriceVnd = 59000,
            IsActive = false
        };

        // Act
        var result = await _service.UpdateSubscriptionPlanAsync(1, request, AdminId);

        // Assert
        result.Should().NotBeNull();
        result.PlanId.Should().Be(1);
        result.PriceVnd.Should().Be(59000);

        var updated = await _db.SubscriptionPlans.FindAsync(1);
        updated!.Price.Should().Be(59000);
        updated.IsActive.Should().BeFalse();

        // Kiểm tra AuditLogs
        var audit = await _db.AuditLogs.FirstOrDefaultAsync(a =>
            a.TargetTable == "SubscriptionPlans" &&
            a.TargetId == "1" &&
            a.Action == "UPDATE_VIP_PRICE");
        audit.Should().NotBeNull();

        // Kiểm tra xóa cache
        _mockRedis.Verify(r => r.RemoveAsync("plans_cache", default), Times.Once);
    }

    [Fact]
    public async Task UpdateSubscriptionPlanAsync_NonExistentPlan_ThrowsNotFoundErrAdmNotFound()
    {
        // Arrange
        var request = new UpdateAdminSubscriptionPlanRequest { PriceVnd = 60000 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            _service.UpdateSubscriptionPlanAsync(999, request, AdminId));

        ex.ErrorCode.Should().Be(ErrorCodes.ErrAdmNotFound);
        ex.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateSubscriptionPlanAsync_InvalidPrice_ThrowsBadRequestErrValidation()
    {
        // Arrange
        var request = new UpdateAdminSubscriptionPlanRequest { PriceVnd = 0 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            _service.UpdateSubscriptionPlanAsync(1, request, AdminId));

        ex.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
        ex.StatusCode.Should().Be(400);
    }

    // ==========================================
    // 4. RECONCILE PAYMENT
    // ==========================================

    [Fact]
    public async Task ReconcilePaymentAsync_Compensate_FailedTransaction_ActivatesVipAndUpdatesRole()
    {
        // Arrange: giao dịch 1001 đang PENDING của user 2 (MEMBER)
        var request = new ReconcileAdminPaymentRequest
        {
            Action = "COMPENSATE",
            Reason = "Đã trừ tiền MoMo nhưng chưa kích hoạt VIP."
        };

        // Act
        var result = await _service.ReconcilePaymentAsync(1001, request, AdminId);

        // Assert
        result.Should().NotBeNull();
        result.TransactionId.Should().Be(1001);
        result.Status.Should().Be("SUCCESS");
        result.VipEndsAt.Should().BeAfter(DateTime.UtcNow);

        var txn = await _db.PaymentTransactions.FindAsync(1001L);
        txn!.TransactionStatus.Should().Be("SUCCESS");
        txn.CompletedAt.Should().NotBeNull();

        var user = await _db.Users.FindAsync(2);
        user!.Role.Should().Be(UserRoles.Vip);

        // Kiểm tra AuditLogs ghi PAYMENT_RECONCILE
        var audit = await _db.AuditLogs.FirstOrDefaultAsync(a =>
            a.TargetTable == "PaymentTransactions" &&
            a.TargetId == "1001" &&
            a.Action == "PAYMENT_RECONCILE");
        audit.Should().NotBeNull();
        audit!.Note.Should().Be(request.Reason);

        // Kiểm tra xóa cache vip_status và plans_cache
        _mockRedis.Verify(r => r.RemoveAsync("vip_status:2", default), Times.Once);
        _mockRedis.Verify(r => r.RemoveAsync("plans_cache", default), Times.Once);
    }

    [Fact]
    public async Task ReconcilePaymentAsync_Compensate_AlreadySuccessTransaction_ExtendsVipDuration()
    {
        // Arrange: giao dịch 1002 đang SUCCESS của user 3
        var initialEndsAt = (await _db.PaymentTransactions.FindAsync(1002L))!.EndsAt;
        var request = new ReconcileAdminPaymentRequest
        {
            Action = "COMPENSATE",
            Reason = "Bù 1 tháng do lỗi hệ thống bảo trì."
        };

        // Act
        var result = await _service.ReconcilePaymentAsync(1002, request, AdminId);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("SUCCESS");
        result.VipEndsAt.Should().BeAfter(initialEndsAt);

        var txn = await _db.PaymentTransactions.FindAsync(1002L);
        txn!.EndsAt.Should().BeAfter(initialEndsAt);
    }

    [Fact]
    public async Task ReconcilePaymentAsync_Refund_RevokesVipAndSetsStatusRefunded()
    {
        // Arrange: hoàn tiền giao dịch 1002 của user 3
        var request = new ReconcileAdminPaymentRequest
        {
            Action = "REFUND",
            Reason = "Người dùng yêu cầu hoàn tiền trong 3 ngày."
        };

        // Act
        var result = await _service.ReconcilePaymentAsync(1002, request, AdminId);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("REFUNDED");

        var txn = await _db.PaymentTransactions.FindAsync(1002L);
        txn!.TransactionStatus.Should().Be("REFUNDED");

        // User 3 không còn giao dịch SUCCESS nào khác nên bị hạ về MEMBER
        var user = await _db.Users.FindAsync(3);
        user!.Role.Should().Be(UserRoles.Member);

        // Kiểm tra xóa cache vip_status
        _mockRedis.Verify(r => r.RemoveAsync("vip_status:3", default), Times.Once);
    }

    [Fact]
    public async Task ReconcilePaymentAsync_SecondCall_ThrowsConflictErrPayAlreadyReconciled()
    {
        // Arrange
        var request1 = new ReconcileAdminPaymentRequest
        {
            Action = "COMPENSATE",
            Reason = "Bù đắp giao dịch lỗi lần thứ nhất."
        };

        // Lần 1: Thành công
        await _service.ReconcilePaymentAsync(1001, request1, AdminId);

        // Lần 2: Phải bị từ chối với 409 ERR_PAY_ALREADY_RECONCILED
        var request2 = new ReconcileAdminPaymentRequest
        {
            Action = "COMPENSATE",
            Reason = "Cố gắng đối soát lần 2 trên cùng giao dịch."
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            _service.ReconcilePaymentAsync(1001, request2, AdminId));

        ex.ErrorCode.Should().Be(ErrorCodes.ErrPayAlreadyReconciled);
        ex.StatusCode.Should().Be(409);
        ex.Message.Should().Be("Giao dịch đã được đối soát.");
    }

    [Fact]
    public async Task ReconcilePaymentAsync_NonExistentTransaction_ThrowsNotFoundErrAdmNotFound()
    {
        // Arrange
        var request = new ReconcileAdminPaymentRequest
        {
            Action = "COMPENSATE",
            Reason = "Giao dịch không tồn tại trong hệ thống."
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            _service.ReconcilePaymentAsync(9999, request, AdminId));

        ex.ErrorCode.Should().Be(ErrorCodes.ErrAdmNotFound);
        ex.StatusCode.Should().Be(404);
    }

    [Theory]
    [InlineData("INVALID_ACTION", "Đã trừ tiền MoMo nhưng chưa kích hoạt VIP.")]
    [InlineData("COMPENSATE", "Ngắn")]
    [InlineData("COMPENSATE", "")]
    public async Task ReconcilePaymentAsync_InvalidActionOrReason_ThrowsBadRequestErrValidation(
        string action, string reason)
    {
        // Arrange
        var request = new ReconcileAdminPaymentRequest
        {
            Action = action,
            Reason = reason
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            _service.ReconcilePaymentAsync(1001, request, AdminId));

        ex.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
        ex.StatusCode.Should().Be(400);
    }

    // ==========================================
    // 5. GET PAYMENT TRANSACTIONS
    // ==========================================

    [Fact]
    public async Task GetPaymentTransactionsAsync_NoFilter_ReturnsAllTransactionsPaged()
    {
        // Arrange
        var filter = new AdminPaymentListFilterRequest { Page = 1, Size = 10 };

        // Act
        var (items, meta) = await _service.GetPaymentTransactionsAsync(filter);

        // Assert
        items.Should().NotBeNull();
        items.Should().HaveCount(2);
        meta.Total.Should().Be(2);

        var pendingItem = items.First(x => x.TransactionId == 1001);
        pendingItem.CanApprove.Should().BeTrue();
        pendingItem.CanReconcile.Should().BeTrue();

        var successItem = items.First(x => x.TransactionId == 1002);
        successItem.CanApprove.Should().BeFalse();
        successItem.CanReconcile.Should().BeTrue();
    }

    [Fact]
    public async Task GetPaymentTransactionsAsync_FilterByStatus_ReturnsMatchingOnly()
    {
        // Arrange
        var filter = new AdminPaymentListFilterRequest { Page = 1, Size = 10, Status = "PENDING" };

        // Act
        var (items, meta) = await _service.GetPaymentTransactionsAsync(filter);

        // Assert
        items.Should().HaveCount(1);
        items[0].TransactionId.Should().Be(1001);
        meta.Total.Should().Be(1);
    }

    [Fact]
    public async Task GetPaymentTransactionsAsync_FilterByKeyword_ReturnsMatching()
    {
        // Arrange (Mã đơn VL_20261022_0001)
        var filter = new AdminPaymentListFilterRequest { Page = 1, Size = 10, Keyword = "VL_20261022_0001" };

        // Act
        var (items, meta) = await _service.GetPaymentTransactionsAsync(filter);

        // Assert
        items.Should().HaveCount(1);
        items[0].TransactionId.Should().Be(1001);
    }

    // ==========================================
    // 6. APPROVE PAYMENT
    // ==========================================

    [Fact]
    public async Task ApprovePaymentAsync_PendingTransaction_SuccessfullyActivatesVipAndUpdatesStatus()
    {
        // Act (Duyệt đơn 1001 đang PENDING của user 2)
        var result = await _service.ApprovePaymentAsync(1001, AdminId);

        // Assert
        result.Should().NotBeNull();
        result.TransactionId.Should().Be(1001);
        result.Status.Should().Be("SUCCESS");
        result.VipEndsAt.Should().BeAfter(DateTime.UtcNow);

        var txn = await _db.PaymentTransactions.FindAsync(1001L);
        txn!.TransactionStatus.Should().Be("SUCCESS");
        txn.CompletedAt.Should().NotBeNull();

        var user = await _db.Users.FindAsync(2);
        user!.Role.Should().Be(UserRoles.Vip);

        // Kiểm tra xóa cache
        _mockRedis.Verify(r => r.RemoveAsync("vip_status:2", default), Times.Once);
        _mockRedis.Verify(r => r.RemoveAsync("cache:profile:2", default), Times.Once);
    }

    [Fact]
    public async Task ApprovePaymentAsync_AlreadySuccessTransaction_ThrowsConflict()
    {
        // Act & Assert (Đơn 1002 đã SUCCESS)
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            _service.ApprovePaymentAsync(1002, AdminId));

        ex.ErrorCode.Should().Be(ErrorCodes.ErrPayOrderAlreadySuccess);
        ex.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task ApprovePaymentAsync_RefundedTransaction_ThrowsConflict()
    {
        // Arrange: hoàn tiền đơn 1002 trước
        await _service.ReconcilePaymentAsync(1002, new ReconcileAdminPaymentRequest
        {
            Action = "REFUND",
            Reason = "Hoàn tiền cho người dùng yêu cầu."
        }, AdminId);

        // Act & Assert (Cố tình duyệt đơn đã bị REFUNDED)
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            _service.ApprovePaymentAsync(1002, AdminId));

        ex.ErrorCode.Should().Be(ErrorCodes.ErrPayOrderRefunded);
        ex.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task ApprovePaymentAsync_NonExistentTransaction_ThrowsNotFound()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            _service.ApprovePaymentAsync(99999, AdminId));

        ex.ErrorCode.Should().Be(ErrorCodes.ErrAdmNotFound);
        ex.StatusCode.Should().Be(404);
    }
}
