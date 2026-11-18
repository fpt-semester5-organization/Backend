using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Subscriptions.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Products;
using VeggieLife.BLL.Services.Settings;
using VeggieLife.BLL.Services.Subscriptions;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using VeggieLife.DAL.External.AiService;
using VeggieLife.DAL.External.OpenFoodFacts;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Products;

/// <summary>
/// D122: hạn mức quét mã vạch/ngày cấu hình ở SystemSettings (Khách qua Redis, thành viên qua UserScanHistory, VIP theo biên lai).
/// </summary>
public class ProductServiceBarcodeQuotaTests : IDisposable
{
    private const string Barcode = "8930000000999";
    private const int MemberId = 77;

    private readonly AppDbContext _db;
    private readonly Mock<IRedisService> _redis = new();
    private readonly Mock<ISystemSettingService> _settings = new();
    private readonly Mock<IUserBenefitService> _benefits = new();
    private readonly ProductService _service;

    public ProductServiceBarcodeQuotaTests()
    {
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"VeggieLife_BarcodeQuota_{Guid.NewGuid()}")
            .Options);

        // Sản phẩm đã đồng bộ trong 30 ngày: đi nhánh dữ liệu đã lưu, không gọi OFF/AI
        _db.Products.Add(new Product
        {
            Barcode = Barcode,
            ProductName = "Sữa đậu nành",
            ImageUrl = "https://images.openfoodfacts.org/x.jpg",
            VeganVerdict = "VEGAN",
            LastSyncedAt = DateTime.UtcNow
        });
        _db.SaveChanges();

        _settings.Setup(s => s.GetIntSettingAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, int fallback, CancellationToken _) => fallback);

        _service = new ProductService(
            _db,
            new Mock<IOpenFoodFactsClient>().Object,
            new Mock<IAiServiceClient>().Object,
            _redis.Object,
            new Mock<ILogger<ProductService>>().Object,
            _settings.Object,
            _benefits.Object);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private void MemberBenefits(bool isVip, int barcodeLimit) =>
        _benefits.Setup(b => b.GetUserBenefitsAsync(MemberId, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VipBenefitFeaturesDto { IsVip = isVip, BarcodeScanLimitDaily = barcodeLimit });

    private async Task SeedScansAsync(int count, DateTime scannedAt)
    {
        var productId = (await _db.Products.FirstAsync()).ProductId;
        for (var i = 0; i < count; i++)
        {
            _db.UserScanHistories.Add(new UserScanHistory
            {
                UserId = MemberId,
                ProductId = productId,
                ScanMethod = "CAMERA_BARCODE",
                ScannedAt = scannedAt
            });
        }
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task FreeMember_UnderLimit_ReturnsRealQuota()
    {
        MemberBenefits(isVip: false, barcodeLimit: 5);
        await SeedScansAsync(1, DateTime.UtcNow);

        var result = await _service.ScanBarcodeAsync(Barcode, MemberId);

        result.Quota.Used.Should().Be(2);
        result.Quota.Limit.Should().Be(5);
        result.Quota.Remaining.Should().Be(3);
    }

    [Fact]
    public async Task FreeMember_AtLimit_Throws403WithUpgradeHint()
    {
        MemberBenefits(isVip: false, barcodeLimit: 2);
        await SeedScansAsync(2, DateTime.UtcNow);

        var act = () => _service.ScanBarcodeAsync(Barcode, MemberId);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrQuotaBarcodeExceeded);
        ex.Which.Message.Should().Contain("Nâng cấp VIP");
    }

    [Fact]
    public async Task VipMember_UsesReceiptLimit_AndYesterdayScansDoNotCount()
    {
        MemberBenefits(isVip: true, barcodeLimit: 100);
        await SeedScansAsync(150, DateTime.UtcNow.Date.AddMinutes(-5));
        await SeedScansAsync(99, DateTime.UtcNow);

        var result = await _service.ScanBarcodeAsync(Barcode, MemberId);
        result.Quota.Used.Should().Be(100);
        result.Quota.Remaining.Should().Be(0);

        var act = () => _service.ScanBarcodeAsync(Barcode, MemberId);
        (await act.Should().ThrowAsync<AppException>()).Which.Message.Should().Contain("gói VIP");
    }

    [Fact]
    public async Task Guest_LimitComesFromSystemSettings()
    {
        _settings.Setup(s => s.GetIntSettingAsync("BARCODE_SCAN_LIMIT_DAILY_GUEST", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _redis.Setup(r => r.GetAsync<int?>("guest_quota:guest-x:barcode", It.IsAny<CancellationToken>())).ReturnsAsync(4);
        _redis.Setup(r => r.IncrementAsync("guest_quota:guest-x:barcode", 1, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var result = await _service.ScanBarcodeAsync(Barcode, null, "guest-x");
        result.Quota.Limit.Should().Be(5);
        result.Quota.Remaining.Should().Be(0);

        _redis.Setup(r => r.GetAsync<int?>("guest_quota:guest-x:barcode", It.IsAny<CancellationToken>())).ReturnsAsync(5);
        var act = () => _service.ScanBarcodeAsync(Barcode, null, "guest-x");
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrQuotaBarcodeExceeded);
        ex.Which.Message.Should().Contain("tối đa 05 lần/ngày");
    }

    [Fact]
    public async Task GetBarcodeQuota_BeforeAnyScanToday_ReturnsFullRemaining()
    {
        MemberBenefits(isVip: true, barcodeLimit: 100);
        await SeedScansAsync(3, DateTime.UtcNow.Date.AddHours(-2));

        var quota = await _service.GetBarcodeQuotaAsync(MemberId, null);

        quota.Used.Should().Be(0);
        quota.Limit.Should().Be(100);
        quota.Remaining.Should().Be(100);
    }

    [Fact]
    public async Task GetBarcodeQuota_Guest_ReadsRedisAndSettings()
    {
        _redis.Setup(r => r.GetAsync<int?>("guest_quota:guest-y:barcode", It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var quota = await _service.GetBarcodeQuotaAsync(null, "guest-y");

        quota.Used.Should().Be(2);
        quota.Limit.Should().Be(3);
        quota.Remaining.Should().Be(1);
    }
}
