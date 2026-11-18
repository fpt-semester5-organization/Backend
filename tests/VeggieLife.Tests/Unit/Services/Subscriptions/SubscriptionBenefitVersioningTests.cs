using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.DTOs.Admin.Subscriptions;
using VeggieLife.BLL.DTOs.Subscriptions.Requests;
using VeggieLife.BLL.Services.Admin;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Email;
using VeggieLife.BLL.Services.Payment;
using VeggieLife.BLL.Services.Settings;
using VeggieLife.BLL.Services.Subscriptions;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Subscriptions;

/// <summary>
/// Bộ kiểm thử nghiệp vụ cho tính năng Bảo lưu quyền lợi gói VIP và Quản trị phiên bản theo Kiến trúc Tinh gọn
/// (VIP Benefit Snapshots, Grandfathering & Plan Versioning via Lean Architecture) [FN-SUB-01..04][FN-ADM-13][BR-PAY-SUB-01].
/// </summary>
public class SubscriptionBenefitVersioningTests
{
    private AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"VeggieLife_Benefit_Test_{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    private static Mock<ISystemSettingService> CreateMockSettingService()
    {
        var mock = new Mock<ISystemSettingService>();
        var settings = new Dictionary<string, int>
        {
            ["CHAT_LIMIT_DAILY_FREE"] = 20,
            ["CHAT_LIMIT_DAILY_VIP"] = 200,
            ["MEAL_PLAN_WEEKLY_LIMIT_FREE"] = 1,
            ["MEAL_PLAN_WEEKLY_LIMIT_VIP"] = 3,
            ["FRIDGE_SCAN_LIMIT_DAILY_FREE"] = 10,
            ["FRIDGE_SCAN_LIMIT_DAILY_VIP"] = 30
        };

        mock.Setup(s => s.GetIntSettingAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, int def, CancellationToken ct) => settings.TryGetValue(key, out var val) ? val : def);

        return mock;
    }

    private static Mock<IPaymentGatewayService> CreateMockPaymentGateway()
    {
        var mock = new Mock<IPaymentGatewayService>();
        var dummyResult = new PaymentCheckoutResult
        {
            PaymentUrl = "https://sandbox.momo.vn/pay/test12345",
            QrCodeUrl = "https://sandbox.momo.vn/pay/test12345",
            PartnerTransactionId = "MOMO_TRANS_12345"
        };

        mock.Setup(g => g.CreateMoMoPaymentAsync(It.IsAny<long>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dummyResult);

        mock.Setup(g => g.CreateVietQrPaymentAsync(It.IsAny<long>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dummyResult);

        return mock;
    }

    // =========================================================================
    // 1. KIỂM THỬ PHÂN GIẢI QUYỀN LỢI ĐỘNG (UserBenefitService)
    // =========================================================================

    [Fact]
    public async Task UserBenefitService_FreeUser_ReturnsFreeDefaultLimits()
    {
        // Arrange
        using var db = CreateInMemoryDb();
        var redisMock = new Mock<IRedisService>();
        var settingsMock = CreateMockSettingService();
        var service = new UserBenefitService(db, redisMock.Object, settingsMock.Object, NullLogger<UserBenefitService>.Instance);

        var freeUser = new User
        {
            UserId = 101,
            Email = "freeuser@veggielife.vn",
            FullName = "Nguyễn Văn Free",
            Role = UserRoles.Member,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(freeUser);
        await db.SaveChangesAsync();

        // Act
        var benefits = await service.GetUserBenefitsAsync(freeUser.UserId, freeUser.Role);

        // Assert
        benefits.Should().NotBeNull();
        benefits.IsVip.Should().BeFalse();
        benefits.ChatLimitDaily.Should().Be(20);
        benefits.MealPlanLimitWeekly.Should().Be(1);
        benefits.FridgeScanLimitDaily.Should().Be(10);
        benefits.VideoToRecipeStt.Should().BeFalse();
    }

    [Fact]
    public async Task UserBenefitService_VipUserWithV1Snapshot_RetainsV1GrandfatheredLimits()
    {
        // Arrange
        using var db = CreateInMemoryDb();
        var redisMock = new Mock<IRedisService>();
        var settingsMock = CreateMockSettingService();
        var service = new UserBenefitService(db, redisMock.Object, settingsMock.Object, NullLogger<UserBenefitService>.Instance);

        var vipUser1 = new User
        {
            UserId = 201,
            Email = "vip_v1@veggielife.vn",
            FullName = "Trần Thị V1",
            Role = UserRoles.Vip,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow.AddMonths(-2)
        };
        db.Users.Add(vipUser1);

        var plan = new SubscriptionPlan
        {
            PlanId = 1,
            AdminId = 1,
            PlanName = "Gói VIP 1 Tháng",
            Price = 49000,
            DurationDays = 30,
            Version = 2, // Gói hiện hành trên hệ thống đã nâng lên Version 2
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-2)
        };
        db.SubscriptionPlans.Add(plan);

        // Giao dịch mua khi gói còn ở Version 1 (lưu snapshot của V1)
        var v1Tx = new PaymentTransaction
        {
            TransactionId = 1001,
            PlanId = plan.PlanId,
            UserId = vipUser1.UserId,
            GatewayProvider = "MOMO",
            PartnerTransactionCode = "MOMO_VIP_V1_001",
            AmountVnd = 49000,
            TransactionStatus = "SUCCESS",
            SignatureHash = "HASH",
            SnapshotPlanName = "Gói VIP 1 Tháng - Bản V1",
            SnapshotFeaturesJson = "{\"chatLimitDaily\":200,\"mealPlanLimitWeekly\":3,\"fridgeScanLimitDaily\":30,\"videoToRecipeStt\":false}",
            PlanVersion = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            StartsAt = DateTime.UtcNow.AddDays(-10),
            EndsAt = DateTime.UtcNow.AddDays(20) // Còn hạn 20 ngày
        };
        db.PaymentTransactions.Add(v1Tx);
        await db.SaveChangesAsync();

        // Act
        var benefits = await service.GetUserBenefitsAsync(vipUser1.UserId, vipUser1.Role);

        // Assert: Khách V1 phải được bảo lưu đúng hạn mức Version 1 (200 chat, 3 meal, 30 fridge, video STT = false)
        benefits.Should().NotBeNull();
        benefits.IsVip.Should().BeTrue();
        benefits.PlanVersion.Should().Be(1, "phiên bản lấy theo giao dịch lúc mua, không theo gói hiện hành (Version 2)");
        benefits.PlanName.Should().Be("Gói VIP 1 Tháng - Bản V1");
        benefits.ChatLimitDaily.Should().Be(200);
        benefits.MealPlanLimitWeekly.Should().Be(3);
        benefits.FridgeScanLimitDaily.Should().Be(30);
        benefits.VideoToRecipeStt.Should().BeFalse();
    }

    [Fact]
    public async Task UserBenefitService_VipUserWithV2Snapshot_ReceivesUpgradedV2Limits()
    {
        // Arrange
        using var db = CreateInMemoryDb();
        var redisMock = new Mock<IRedisService>();
        var settingsMock = CreateMockSettingService();
        var service = new UserBenefitService(db, redisMock.Object, settingsMock.Object, NullLogger<UserBenefitService>.Instance);

        var vipUser2 = new User
        {
            UserId = 202,
            Email = "vip_v2@veggielife.vn",
            FullName = "Lê Văn V2",
            Role = UserRoles.Vip,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };
        db.Users.Add(vipUser2);

        var plan = new SubscriptionPlan
        {
            PlanId = 2,
            AdminId = 1,
            PlanName = "Gói VIP 1 Tháng",
            Price = 59000,
            DurationDays = 30,
            Version = 2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-1)
        };
        db.SubscriptionPlans.Add(plan);

        // Giao dịch mua sau khi Admin nâng cấp lên Version 2
        var v2Tx = new PaymentTransaction
        {
            TransactionId = 1002,
            PlanId = plan.PlanId,
            UserId = vipUser2.UserId,
            GatewayProvider = "VIETQR",
            PartnerTransactionCode = "PAYOS_VIP_V2_002",
            AmountVnd = 59000,
            TransactionStatus = "SUCCESS",
            SignatureHash = "HASH",
            SnapshotPlanName = "Gói VIP 1 Tháng - Nâng Cấp V2",
            SnapshotFeaturesJson = "{\"chatLimitDaily\":300,\"mealPlanLimitWeekly\":5,\"fridgeScanLimitDaily\":50,\"videoToRecipeStt\":true}",
            PlanVersion = 2,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            StartsAt = DateTime.UtcNow.AddDays(-2),
            EndsAt = DateTime.UtcNow.AddDays(28)
        };
        db.PaymentTransactions.Add(v2Tx);
        await db.SaveChangesAsync();

        // Act
        var benefits = await service.GetUserBenefitsAsync(vipUser2.UserId, vipUser2.Role);

        // Assert: Khách V2 được hưởng đúng quyền lợi nâng cấp Version 2 (300 chat, 5 meal, 50 fridge, video STT = true)
        benefits.Should().NotBeNull();
        benefits.IsVip.Should().BeTrue();
        benefits.PlanVersion.Should().Be(2);
        benefits.PlanName.Should().Be("Gói VIP 1 Tháng - Nâng Cấp V2");
        benefits.ChatLimitDaily.Should().Be(300);
        benefits.MealPlanLimitWeekly.Should().Be(5);
        benefits.FridgeScanLimitDaily.Should().Be(50);
        benefits.VideoToRecipeStt.Should().BeTrue();
    }

    // =========================================================================
    // 2. KIỂM THỬ QUẢN TRỊ PHIÊN BẢN GÓI (AdminSubscriptionService)
    // =========================================================================

    [Fact]
    public async Task AdminSubscriptionService_UpdatePlanPriceOrFeatures_IncrementsVersionAndCreatesHistoryRecord()
    {
        // Arrange
        using var db = CreateInMemoryDb();
        var redisMock = new Mock<IRedisService>();
        var adminService = new AdminSubscriptionService(db, redisMock.Object, NullLogger<AdminSubscriptionService>.Instance);

        var adminUser = new User
        {
            UserId = 1,
            Email = "admin@veggielife.vn",
            FullName = "Admin VeggieLife",
            Role = UserRoles.Admin,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(adminUser);

        var plan = new SubscriptionPlan
        {
            PlanId = 10,
            AdminId = adminUser.UserId,
            PlanName = "Gói VIP Thử Nghiệm",
            Price = 49000,
            DurationDays = 30,
            FeaturesJson = "{\"chatLimitDaily\":200}",
            Version = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };
        db.SubscriptionPlans.Add(plan);
        await db.SaveChangesAsync();

        // Act: Admin cập nhật giá từ 49k lên 59k và bổ sung mô tả (hạn mức VIP giữ theo SystemSettings - D121 Q1-B)
        var updateRequest = new UpdateAdminSubscriptionPlanRequest
        {
            PriceVnd = 59000,
            FeaturesJson = "{\"chatLimitDaily\":200,\"description\":\"Bổ sung mô tả\"}",
            ChangeReason = "Tăng giá do bổ sung thêm lượt chat AI"
        };
        var updateResult = await adminService.UpdateSubscriptionPlanAsync(plan.PlanId, updateRequest, adminUser.UserId);

        // Assert
        updateResult.Should().NotBeNull();
        updateResult.PriceVnd.Should().Be(59000);

        // Kiểm tra Plan đã được tăng Version lên 2
        var updatedPlan = await db.SubscriptionPlans.FindAsync(plan.PlanId);
        updatedPlan!.Version.Should().Be(2);
        updatedPlan.Price.Should().Be(59000);
        updatedPlan.FeaturesJson.Should().Be("{\"chatLimitDaily\":200,\"description\":\"Bổ sung mô tả\",\"mealPlanLimitWeekly\":3,\"fridgeScanLimitDaily\":30,\"barcodeScanLimitDaily\":50}");

        // Kiểm tra AuditLogs được ghi nhận với UPDATE_VIP_PRICE
        var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.TargetTable == "SubscriptionPlans" && a.TargetId == plan.PlanId.ToString());
        audit.Should().NotBeNull();
        audit!.Action.Should().Be("UPDATE_VIP_PRICE");
        audit.Note.Should().Be("Tăng giá do bổ sung thêm lượt chat AI");

        // Kiểm tra API lấy lịch sử phiên bản từ AuditLogs và trạng thái hiện hành
        var historyList = await adminService.GetSubscriptionPlanVersionsAsync(plan.PlanId);
        historyList.Should().HaveCount(2);
        historyList[0].Version.Should().Be(2);
        historyList[0].Price.Should().Be(59000);
        historyList[1].Version.Should().Be(1);
        historyList[1].Price.Should().Be(49000);
    }

    [Fact]
    public async Task AdminSubscriptionService_UpdateOnlyIsActive_DoesNotIncrementVersion()
    {
        // Arrange
        using var db = CreateInMemoryDb();
        var redisMock = new Mock<IRedisService>();
        var adminService = new AdminSubscriptionService(db, redisMock.Object, NullLogger<AdminSubscriptionService>.Instance);

        var plan = new SubscriptionPlan
        {
            PlanId = 20,
            AdminId = 1,
            PlanName = "Gói VIP Tạm Ẩn",
            Price = 99000,
            DurationDays = 90,
            Version = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.SubscriptionPlans.Add(plan);
        await db.SaveChangesAsync();

        // Act: Chỉ tắt kích hoạt gói
        var updateRequest = new UpdateAdminSubscriptionPlanRequest
        {
            IsActive = false
        };
        await adminService.UpdateSubscriptionPlanAsync(plan.PlanId, updateRequest, 1);

        // Assert: Version vẫn giữ nguyên là 1
        var updatedPlan = await db.SubscriptionPlans.FindAsync(plan.PlanId);
        updatedPlan!.IsActive.Should().BeFalse();
        updatedPlan.Version.Should().Be(1);
    }

    // =========================================================================
    // 3. KIỂM THỬ KHỞI TẠO ĐƠN & BIÊN LAI (SubscriptionService)
    // =========================================================================

    [Fact]
    public async Task SubscriptionService_Checkout_FreezesCurrentPlanVersionAndSnapshotFeatures()
    {
        // Arrange
        using var db = CreateInMemoryDb();
        var redisMock = new Mock<IRedisService>();
        var gatewayMock = CreateMockPaymentGateway();
        var emailMock = new Mock<IEmailService>();
        var settingsMock = CreateMockSettingService();
        var benefitService = new UserBenefitService(db, redisMock.Object, settingsMock.Object, NullLogger<UserBenefitService>.Instance);

        var subService = new SubscriptionService(
            db, redisMock.Object, gatewayMock.Object, emailMock.Object, NullLogger<SubscriptionService>.Instance, settingsMock.Object, benefitService);

        var user = new User
        {
            UserId = 301,
            Email = "buyer@veggielife.vn",
            FullName = "Người Mua Hàng",
            Role = UserRoles.Member,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);

        var planV3 = new SubscriptionPlan
        {
            PlanId = 30,
            AdminId = 1,
            PlanName = "Gói VIP Đặc Biệt",
            Price = 89000,
            DurationDays = 60,
            FeaturesJson = "{\"chatLimitDaily\":500,\"exclusiveBadge\":true}",
            Version = 3,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        db.SubscriptionPlans.Add(planV3);
        await db.SaveChangesAsync();

        // Act
        var checkoutResult = await subService.CheckoutSubscriptionAsync(
            user.UserId, user.Role, new CheckoutSubscriptionRequest { PlanId = planV3.PlanId, Gateway = "MOMO" });

        // Assert: Giao dịch được lưu với đầy đủ Snapshot của Version 3
        checkoutResult.Should().NotBeNull();

        var createdTx = await db.PaymentTransactions.FirstAsync(t => t.TransactionId == checkoutResult.TransactionId);
        createdTx.SnapshotPlanName.Should().Be("Gói VIP Đặc Biệt");
        createdTx.AmountVnd.Should().Be(89000);
        createdTx.PlanVersion.Should().Be(3);
        // Khóa đã có giữ nguyên (500), khóa thiếu lấy SystemSettings VIP lúc checkout (3, 30, 50)
        createdTx.SnapshotFeaturesJson.Should().Be("{\"chatLimitDaily\":500,\"exclusiveBadge\":true,\"mealPlanLimitWeekly\":3,\"fridgeScanLimitDaily\":30,\"barcodeScanLimitDaily\":50}");
    }

    [Fact]
    public async Task SubscriptionService_Receipt_DisplaysSnapshotPlanAndVersion()
    {
        // Arrange
        using var db = CreateInMemoryDb();
        var redisMock = new Mock<IRedisService>();
        var gatewayMock = CreateMockPaymentGateway();
        var emailMock = new Mock<IEmailService>();
        var settingsMock = CreateMockSettingService();
        var benefitService = new UserBenefitService(db, redisMock.Object, settingsMock.Object, NullLogger<UserBenefitService>.Instance);

        var subService = new SubscriptionService(
            db, redisMock.Object, gatewayMock.Object, emailMock.Object, NullLogger<SubscriptionService>.Instance, settingsMock.Object, benefitService);

        var user = new User
        {
            UserId = 401,
            Email = "receipt_user@veggielife.vn",
            FullName = "Khách Nhận Biên Lai",
            Role = UserRoles.Vip,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);

        var plan = new SubscriptionPlan
        {
            PlanId = 40,
            AdminId = 1,
            PlanName = "Gói VIP Hiện Tại (V2 Đã Tăng Giá 79k)",
            Price = 79000,
            DurationDays = 30,
            Version = 2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.SubscriptionPlans.Add(plan);

        // Giao dịch hoàn tất mua từ lúc V1 giá 49k
        var completedTx = new PaymentTransaction
        {
            TransactionId = 9999,
            PlanId = plan.PlanId,
            UserId = user.UserId,
            GatewayProvider = "VIETQR",
            PartnerTransactionCode = "PAYOS_ORDER_9999",
            AmountVnd = 49000,
            TransactionStatus = "SUCCESS",
            SignatureHash = "HASH",
            SnapshotPlanName = "Gói VIP Ban Đầu (V1 Giá 49k)",
            SnapshotFeaturesJson = "{\"chatLimitDaily\":200}",
            PlanVersion = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            CompletedAt = DateTime.UtcNow.AddDays(-5),
            StartsAt = DateTime.UtcNow.AddDays(-5),
            EndsAt = DateTime.UtcNow.AddDays(25)
        };
        db.PaymentTransactions.Add(completedTx);
        await db.SaveChangesAsync();

        // Act
        var receipt = await subService.GetPaymentReceiptAsync(completedTx.TransactionId, user.UserId, user.Role);

        // Assert: Biên lai phải thể hiện đúng thông tin Snapshot lúc mua (49k, SnapshotPlanName), không bị ảnh hưởng bởi giá hiện hành 79k
        receipt.Should().NotBeNull();
        receipt.PlanVersion.Should().Be(1, "biên lai in phiên bản lúc mua, không theo gói hiện hành (Version 2)");
        receipt.PlanName.Should().Be("Gói VIP Ban Đầu (V1 Giá 49k)");
        receipt.AmountVnd.Should().Be(49000);
        receipt.DurationDays.Should().Be(30);
        receipt.SnapshotFeaturesJson.Should().Be("{\"chatLimitDaily\":200}");
        receipt.Note.Should().Contain("Version 1", "Note ghi phiên bản lúc mua");
    }


    // =========================================================================
    // 4. TÁCH BIỆT NGƯỜI MUA TRƯỚC / SAU KHI GÓI CẬP NHẬT (D121, 2.plan solving GĐ2)
    // =========================================================================

    private static User NewMember(int id) => new()
    {
        UserId = id,
        Email = $"member{id}@veggielife.vn",
        FullName = $"Thành viên {id}",
        Role = UserRoles.Member,
        AccountStatus = "ACTIVE",
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Checkout_BeforeAndAfterPlanUpdate_SeparatesBenefitsAndVersion()
    {
        // Arrange
        using var db = CreateInMemoryDb();
        var redisMock = new Mock<IRedisService>();
        var settingsMock = CreateMockSettingService();
        var benefitService = new UserBenefitService(db, redisMock.Object, settingsMock.Object, NullLogger<UserBenefitService>.Instance);
        var subService = new SubscriptionService(
            db, redisMock.Object, CreateMockPaymentGateway().Object, new Mock<IEmailService>().Object,
            NullLogger<SubscriptionService>.Instance, settingsMock.Object, benefitService);

        var userA = NewMember(501);
        var userB = NewMember(502);
        db.Users.AddRange(userA, userB);
        var plan = new SubscriptionPlan
        {
            PlanId = 50,
            AdminId = 1,
            PlanName = "Gói VIP 1 Tháng",
            Price = 49000,
            DurationDays = 30,
            FeaturesJson = "{\"chatLimitDaily\":200,\"mealPlanLimitWeekly\":3,\"fridgeScanLimitDaily\":30}",
            Version = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };
        db.SubscriptionPlans.Add(plan);
        await db.SaveChangesAsync();

        // A mua khi gói ở Version 1
        var checkoutA = await subService.CheckoutSubscriptionAsync(userA.UserId, userA.Role, new CheckoutSubscriptionRequest { PlanId = plan.PlanId, Gateway = "MOMO" });

        // Admin nâng gói lên Version 2 với quyền lợi mới
        plan.FeaturesJson = "{\"chatLimitDaily\":300,\"mealPlanLimitWeekly\":5,\"fridgeScanLimitDaily\":50}";
        plan.Version = 2;
        await db.SaveChangesAsync();

        // B mua sau khi gói cập nhật
        var checkoutB = await subService.CheckoutSubscriptionAsync(userB.UserId, userB.Role, new CheckoutSubscriptionRequest { PlanId = plan.PlanId, Gateway = "MOMO" });

        // Kích hoạt cả hai giao dịch như IPN thành công
        foreach (var tx in db.PaymentTransactions)
        {
            tx.TransactionStatus = "SUCCESS";
            tx.CompletedAt = DateTime.UtcNow;
            tx.StartsAt = DateTime.UtcNow.AddMinutes(-1);
        }
        userA.Role = UserRoles.Vip;
        userB.Role = UserRoles.Vip;
        await db.SaveChangesAsync();

        // Act
        var benefitsA = await benefitService.GetUserBenefitsAsync(userA.UserId, UserRoles.Vip);
        var benefitsB = await benefitService.GetUserBenefitsAsync(userB.UserId, UserRoles.Vip);
        var receiptA = await subService.GetPaymentReceiptAsync(checkoutA.TransactionId, userA.UserId, UserRoles.Vip);
        var receiptB = await subService.GetPaymentReceiptAsync(checkoutB.TransactionId, userB.UserId, UserRoles.Vip);

        // Assert: A giữ Version 1, B nhận Version 2
        benefitsA.PlanVersion.Should().Be(1);
        benefitsA.ChatLimitDaily.Should().Be(200);
        benefitsA.MealPlanLimitWeekly.Should().Be(3);
        benefitsA.FridgeScanLimitDaily.Should().Be(30);
        receiptA.PlanVersion.Should().Be(1);
        receiptA.SnapshotFeaturesJson.Should().Contain("\"chatLimitDaily\":200");

        benefitsB.PlanVersion.Should().Be(2);
        benefitsB.ChatLimitDaily.Should().Be(300);
        benefitsB.MealPlanLimitWeekly.Should().Be(5);
        benefitsB.FridgeScanLimitDaily.Should().Be(50);
        receiptB.PlanVersion.Should().Be(2);
        receiptB.SnapshotFeaturesJson.Should().Contain("\"chatLimitDaily\":300");
    }

    [Fact]
    public async Task UserBenefitService_SnapshotMissing_DoesNotLeakCurrentPlanFeatures()
    {
        // Arrange
        using var db = CreateInMemoryDb();
        var settingsMock = CreateMockSettingService();
        var service = new UserBenefitService(db, new Mock<IRedisService>().Object, settingsMock.Object, NullLogger<UserBenefitService>.Instance);

        var user = NewMember(601);
        user.Role = UserRoles.Vip;
        db.Users.Add(user);
        db.SubscriptionPlans.Add(new SubscriptionPlan
        {
            PlanId = 60,
            AdminId = 1,
            PlanName = "Gói VIP Đã Đổi",
            Price = 49000,
            DurationDays = 30,
            FeaturesJson = "{\"chatLimitDaily\":999,\"mealPlanLimitWeekly\":9,\"fridgeScanLimitDaily\":99}",
            Version = 5,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        db.PaymentTransactions.Add(new PaymentTransaction
        {
            TransactionId = 6001,
            PlanId = 60,
            UserId = user.UserId,
            GatewayProvider = "MOMO",
            PartnerTransactionCode = "LEGACY_6001",
            AmountVnd = 49000,
            TransactionStatus = "SUCCESS",
            SignatureHash = "HASH",
            SnapshotFeaturesJson = null,
            PlanVersion = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            StartsAt = DateTime.UtcNow.AddDays(-3),
            EndsAt = DateTime.UtcNow.AddDays(27)
        });
        await db.SaveChangesAsync();

        // Act
        var benefits = await service.GetUserBenefitsAsync(user.UserId, UserRoles.Vip);

        // Assert: không lấy quyền lợi hiện hành của gói (999/9/99), chỉ dùng SystemSettings dự phòng
        benefits.PlanVersion.Should().Be(1);
        benefits.ChatLimitDaily.Should().Be(200);
        benefits.MealPlanLimitWeekly.Should().Be(3);
        benefits.FridgeScanLimitDaily.Should().Be(30);
    }

    [Fact]
    public async Task UserBenefitService_CachedVipBenefits_IgnoredWhenRoleIsMember()
    {
        // Arrange
        using var db = CreateInMemoryDb();
        var redisMock = new Mock<IRedisService>();
        redisMock.Setup(r => r.GetAsync<VeggieLife.BLL.DTOs.Subscriptions.Responses.VipBenefitFeaturesDto>("user_benefits:701", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VeggieLife.BLL.DTOs.Subscriptions.Responses.VipBenefitFeaturesDto { IsVip = true, ChatLimitDaily = 200 });
        var service = new UserBenefitService(db, redisMock.Object, CreateMockSettingService().Object, NullLogger<UserBenefitService>.Instance);

        // Act: VIP vừa hết hạn, role truyền vào là MEMBER nhưng cache còn bản VIP
        var benefits = await service.GetUserBenefitsAsync(701, UserRoles.Member);

        // Assert
        benefits.IsVip.Should().BeFalse();
        benefits.ChatLimitDaily.Should().Be(20);
    }

    [Theory]
    [InlineData(null, "{\"chatLimitDaily\":200,\"mealPlanLimitWeekly\":3,\"fridgeScanLimitDaily\":30,\"barcodeScanLimitDaily\":50}")]
    [InlineData("{\"chat_limit_daily\":50,\"mealPlanLimitWeekly\":2}", "{\"chat_limit_daily\":50,\"mealPlanLimitWeekly\":2,\"fridgeScanLimitDaily\":30,\"barcodeScanLimitDaily\":50}")]
    [InlineData("{\"chatLimitDaily\":1,\"mealPlanLimitWeekly\":1,\"fridgeScanLimitDaily\":1,\"barcodeScanLimitDaily\":1,\"description\":\"Gói tiết kiệm\"}", "{\"chatLimitDaily\":1,\"mealPlanLimitWeekly\":1,\"fridgeScanLimitDaily\":1,\"barcodeScanLimitDaily\":1,\"description\":\"Gói tiết kiệm\"}")]
    public void VipBenefitJson_EnsureQuotas_AddsOnlyMissingKeys(string? input, string expected)
    {
        VipBenefitJson.EnsureQuotas(input, 200, 3, 30, 50).Should().Be(expected);
    }
}
