using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.Settings;
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
/// D121 Q1-B: SystemSettings là nguồn hạn mức VIP; lưu Settings VIP thì các gói lên phiên bản mới,
/// người mua trước giữ snapshot, người mua sau nhận hạn mức mới (2.plan solving GĐ3).
/// </summary>
public class SystemSettingVipSyncTests
{
    private const int AdminId = 1;

    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase($"VeggieLife_VipSync_{Guid.NewGuid()}")
        .Options);

    private static SystemSetting IntSetting(string key, string value, string defaultValue) => new()
    {
        SettingKey = key,
        SettingValue = value,
        DefaultValue = defaultValue,
        DataType = "INT",
        Category = "AI_QUOTA",
        UpdatedAt = DateTime.UtcNow
    };

    private static SubscriptionPlan Plan(int id, string name, bool isActive = true) => new()
    {
        PlanId = id,
        AdminId = AdminId,
        PlanName = name,
        Price = 49000,
        DurationDays = 30,
        FeaturesJson = "{\"chatLimitDaily\":200,\"mealPlanLimitWeekly\":3,\"fridgeScanLimitDaily\":30,\"description\":\"Gói tiết kiệm\"}",
        Version = 1,
        IsActive = isActive,
        CreatedAt = DateTime.UtcNow.AddDays(-30)
    };

    private static async Task<AppDbContext> SeedAsync()
    {
        var db = CreateDb();
        db.Users.Add(new User
        {
            UserId = AdminId,
            Email = "admin@veggielife.vn",
            FullName = "Quản trị viên 01",
            Role = UserRoles.Admin,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        });
        db.SystemSettings.AddRange(
            IntSetting(VipBenefitJson.ChatSettingKey, "200", "200"),
            IntSetting(VipBenefitJson.MealSettingKey, "3", "3"),
            IntSetting(VipBenefitJson.FridgeSettingKey, "30", "30"),
            IntSetting(VipBenefitJson.BarcodeSettingKey, "100", "100"),
            IntSetting("BARCODE_SCAN_LIMIT_DAILY_FREE", "20", "20"),
            IntSetting("CHAT_LIMIT_DAILY_FREE", "20", "20"));
        db.SubscriptionPlans.AddRange(Plan(1, "Gói VIP 1 Tháng"), Plan(2, "Gói VIP 3 Tháng"), Plan(3, "Gói VIP Đã Ẩn", isActive: false));
        await db.SaveChangesAsync();
        return db;
    }

    private static SystemSettingService SettingService(AppDbContext db) =>
        new(db, new Mock<IRedisService>().Object, NullLogger<SystemSettingService>.Instance);

    [Fact]
    public async Task UpdateVipSetting_BumpsVersionOfEveryPlan_AndKeepsOtherKeys()
    {
        using var db = await SeedAsync();

        await SettingService(db).UpdateSettingAsync(VipBenefitJson.ChatSettingKey, new UpdateSystemSettingRequest { SettingValue = "300" }, AdminId);

        var plans = await db.SubscriptionPlans.IgnoreQueryFilters().AsNoTracking().OrderBy(p => p.PlanId).ToListAsync();
        plans.Should().OnlyContain(p => p.Version == 2, "mọi gói, kể cả gói đang ẩn, lên phiên bản mới");
        plans.Should().OnlyContain(p => VipBenefitJson.ReadQuota(p.FeaturesJson, VipBenefitJson.ChatLimitDaily) == 300);
        plans.Should().OnlyContain(p => VipBenefitJson.ReadQuota(p.FeaturesJson, VipBenefitJson.MealPlanLimitWeekly) == 3);
        plans.Should().OnlyContain(p => p.FeaturesJson!.Contains("\"description\":\"Gói tiết kiệm\""));

        var planAudits = await db.AuditLogs.Where(a => a.Action == "UPDATE_VIP_PLAN").ToListAsync();
        planAudits.Should().HaveCount(3);
        planAudits.Should().OnlyContain(a => a.Note!.Contains("CHAT_LIMIT_DAILY_VIP") && a.Note.Contains("200 → 300"));
        (await db.SystemSettings.FindAsync(VipBenefitJson.ChatSettingKey))!.SettingValue.Should().Be("300");
    }

    [Fact]
    public async Task UpdateVipSetting_SameValue_DoesNotBumpVersion()
    {
        using var db = await SeedAsync();

        await SettingService(db).UpdateSettingAsync(VipBenefitJson.FridgeSettingKey, new UpdateSystemSettingRequest { SettingValue = "30" }, AdminId);

        (await db.SubscriptionPlans.IgnoreQueryFilters().AsNoTracking().ToListAsync()).Should().OnlyContain(p => p.Version == 1);
        (await db.AuditLogs.CountAsync(a => a.Action == "UPDATE_VIP_PLAN")).Should().Be(0);
    }

    [Fact]
    public async Task UpdateFreeSetting_DoesNotTouchPlans()
    {
        using var db = await SeedAsync();

        await SettingService(db).UpdateSettingAsync("CHAT_LIMIT_DAILY_FREE", new UpdateSystemSettingRequest { SettingValue = "25" }, AdminId);

        (await db.SubscriptionPlans.IgnoreQueryFilters().AsNoTracking().ToListAsync()).Should().OnlyContain(p => p.Version == 1);
    }

    [Fact]
    public async Task ResetVipSetting_SyncsDefaultIntoPlans()
    {
        using var db = await SeedAsync();
        var service = SettingService(db);
        await service.UpdateSettingAsync(VipBenefitJson.MealSettingKey, new UpdateSystemSettingRequest { SettingValue = "2" }, AdminId);

        await service.ResetToDefaultAsync(VipBenefitJson.MealSettingKey, AdminId);

        var plans = await db.SubscriptionPlans.IgnoreQueryFilters().AsNoTracking().ToListAsync();
        plans.Should().OnlyContain(p => p.Version == 3);
        plans.Should().OnlyContain(p => VipBenefitJson.ReadQuota(p.FeaturesJson, VipBenefitJson.MealPlanLimitWeekly) == 3);
    }

    [Fact]
    public async Task CheckoutAfterVipSettingChange_SnapshotsNewVersion_WhileEarlierBuyerKeepsOld()
    {
        using var db = await SeedAsync();
        var redis = new Mock<IRedisService>();
        var settings = SettingService(db);
        var benefits = new UserBenefitService(db, redis.Object, settings, NullLogger<UserBenefitService>.Instance);
        var gateway = new Mock<IPaymentGatewayService>();
        gateway.Setup(g => g.CreateMoMoPaymentAsync(It.IsAny<long>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new PaymentCheckoutResult { PaymentUrl = "https://sandbox.momo.vn/pay/x", PartnerTransactionId = "MOMO_X" });
        var subscriptions = new SubscriptionService(db, redis.Object, gateway.Object, new Mock<IEmailService>().Object,
            NullLogger<SubscriptionService>.Instance, settings, benefits);

        db.Users.AddRange(
            new User { UserId = 11, Email = "a@veggielife.vn", FullName = "A", Role = UserRoles.Member, AccountStatus = "ACTIVE", CreatedAt = DateTime.UtcNow },
            new User { UserId = 12, Email = "b@veggielife.vn", FullName = "B", Role = UserRoles.Member, AccountStatus = "ACTIVE", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var checkoutA = await subscriptions.CheckoutSubscriptionAsync(11, UserRoles.Member, new CheckoutSubscriptionRequest { PlanId = 1, Gateway = "MOMO" });
        await settings.UpdateSettingAsync(VipBenefitJson.ChatSettingKey, new UpdateSystemSettingRequest { SettingValue = "300" }, AdminId);
        var checkoutB = await subscriptions.CheckoutSubscriptionAsync(12, UserRoles.Member, new CheckoutSubscriptionRequest { PlanId = 1, Gateway = "MOMO" });

        var txA = await db.PaymentTransactions.AsNoTracking().FirstAsync(t => t.TransactionId == checkoutA.TransactionId);
        var txB = await db.PaymentTransactions.AsNoTracking().FirstAsync(t => t.TransactionId == checkoutB.TransactionId);
        txA.PlanVersion.Should().Be(1);
        VipBenefitJson.ReadQuota(txA.SnapshotFeaturesJson, VipBenefitJson.ChatLimitDaily).Should().Be(200);
        txB.PlanVersion.Should().Be(2);
        VipBenefitJson.ReadQuota(txB.SnapshotFeaturesJson, VipBenefitJson.ChatLimitDaily).Should().Be(300);
    }

    [Fact]
    public async Task AdminUpdatePlan_QuotaDifferentFromSettings_ThrowsValidation()
    {
        using var db = await SeedAsync();
        var admin = new AdminSubscriptionService(db, new Mock<IRedisService>().Object, NullLogger<AdminSubscriptionService>.Instance);

        var act = () => admin.UpdateSubscriptionPlanAsync(1, new UpdateAdminSubscriptionPlanRequest
        {
            FeaturesJson = "{\"chatLimitDaily\":999}"
        }, AdminId);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
        (await db.SubscriptionPlans.AsNoTracking().FirstAsync(p => p.PlanId == 1)).Version.Should().Be(1);
    }

    [Fact]
    public async Task AdminCreatePlan_WithoutFeatures_FillsQuotasFromSettings()
    {
        using var db = await SeedAsync();
        var admin = new AdminSubscriptionService(db, new Mock<IRedisService>().Object, NullLogger<AdminSubscriptionService>.Instance);

        var created = await admin.CreateSubscriptionPlanAsync(new CreateAdminSubscriptionPlanRequest
        {
            PlanName = "Gói VIP 6 Tháng",
            DurationDays = 180,
            PriceVnd = 249000
        }, AdminId);

        var plan = await db.SubscriptionPlans.AsNoTracking().FirstAsync(p => p.PlanId == created.PlanId);
        plan.FeaturesJson.Should().Be("{\"chatLimitDaily\":200,\"mealPlanLimitWeekly\":3,\"fridgeScanLimitDaily\":30,\"barcodeScanLimitDaily\":100}");
    }

    [Fact]
    public async Task PlanVersionHistory_UsesAuditTimesAsEffectiveRange()
    {
        using var db = await SeedAsync();
        var settings = SettingService(db);
        await settings.UpdateSettingAsync(VipBenefitJson.ChatSettingKey, new UpdateSystemSettingRequest { SettingValue = "300" }, AdminId);
        await settings.UpdateSettingAsync(VipBenefitJson.FridgeSettingKey, new UpdateSystemSettingRequest { SettingValue = "50" }, AdminId);
        var plan = await db.SubscriptionPlans.AsNoTracking().FirstAsync(p => p.PlanId == 1);
        var audits = await db.AuditLogs.AsNoTracking()
            .Where(a => a.Action == "UPDATE_VIP_PLAN" && a.TargetId == "1")
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();

        var admin = new AdminSubscriptionService(db, new Mock<IRedisService>().Object, NullLogger<AdminSubscriptionService>.Instance);
        var history = await admin.GetSubscriptionPlanVersionsAsync(1);

        history.Select(v => v.Version).Should().Equal(3, 2, 1);
        history[0].EffectiveFrom.Should().Be(audits[1].CreatedAt);
        history[0].EffectiveTo.Should().BeNull();
        history[1].EffectiveFrom.Should().Be(audits[0].CreatedAt);
        history[1].EffectiveTo.Should().Be(audits[1].CreatedAt);
        VipBenefitJson.ReadQuota(history[1].FeaturesJson, VipBenefitJson.ChatLimitDaily).Should().Be(300);
        history[2].EffectiveFrom.Should().Be(plan.CreatedAt);
        history[2].EffectiveTo.Should().Be(audits[0].CreatedAt);
        VipBenefitJson.ReadQuota(history[2].FeaturesJson, VipBenefitJson.ChatLimitDaily).Should().Be(200);
    }

    [Fact]
    public async Task BarcodeVipSetting_SyncsPlans_AndSeparatesEarlierAndLaterBuyers()
    {
        using var db = await SeedAsync();
        var redis = new Mock<IRedisService>();
        var settings = SettingService(db);
        var benefits = new UserBenefitService(db, redis.Object, settings, NullLogger<UserBenefitService>.Instance);
        var gateway = new Mock<IPaymentGatewayService>();
        gateway.Setup(g => g.CreateMoMoPaymentAsync(It.IsAny<long>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new PaymentCheckoutResult { PaymentUrl = "https://sandbox.momo.vn/pay/x", PartnerTransactionId = "MOMO_X" });
        var subscriptions = new SubscriptionService(db, redis.Object, gateway.Object, new Mock<IEmailService>().Object,
            NullLogger<SubscriptionService>.Instance, settings, benefits);

        db.Users.AddRange(
            new User { UserId = 21, Email = "a2@veggielife.vn", FullName = "A", Role = UserRoles.Member, AccountStatus = "ACTIVE", CreatedAt = DateTime.UtcNow },
            new User { UserId = 22, Email = "b2@veggielife.vn", FullName = "B", Role = UserRoles.Member, AccountStatus = "ACTIVE", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var checkoutA = await subscriptions.CheckoutSubscriptionAsync(21, UserRoles.Member, new CheckoutSubscriptionRequest { PlanId = 1, Gateway = "MOMO" });
        await settings.UpdateSettingAsync(VipBenefitJson.BarcodeSettingKey, new UpdateSystemSettingRequest { SettingValue = "150" }, AdminId);
        var checkoutB = await subscriptions.CheckoutSubscriptionAsync(22, UserRoles.Member, new CheckoutSubscriptionRequest { PlanId = 1, Gateway = "MOMO" });

        var plans = await db.SubscriptionPlans.IgnoreQueryFilters().AsNoTracking().ToListAsync();
        plans.Should().OnlyContain(p => p.Version == 2 && VipBenefitJson.ReadQuota(p.FeaturesJson, VipBenefitJson.BarcodeScanLimitDaily) == 150);

        var txA = await db.PaymentTransactions.AsNoTracking().FirstAsync(t => t.TransactionId == checkoutA.TransactionId);
        var txB = await db.PaymentTransactions.AsNoTracking().FirstAsync(t => t.TransactionId == checkoutB.TransactionId);
        VipBenefitJson.ReadQuota(txA.SnapshotFeaturesJson, VipBenefitJson.BarcodeScanLimitDaily).Should().Be(100);
        txA.PlanVersion.Should().Be(1);
        VipBenefitJson.ReadQuota(txB.SnapshotFeaturesJson, VipBenefitJson.BarcodeScanLimitDaily).Should().Be(150);
        txB.PlanVersion.Should().Be(2);

        var publicPlans = await subscriptions.GetSubscriptionPlansAsync();
        publicPlans.Plans.Should().OnlyContain(p => p.Benefits!.BarcodeScanLimitDaily == 150);
        publicPlans.Benefits.Free.BarcodeScansPerDay.Should().Be(20);
        publicPlans.Benefits.Vip.BarcodeScansPerDay.Should().Be(150);
    }
}
