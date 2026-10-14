using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Products.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Products;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using VeggieLife.DAL.External.OpenFoodFacts;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Products;

/// <summary>
/// Bộ kiểm thử đơn vị toàn diện cho ProductService (FN-BAR-01, FN-BAR-02).
/// Kiểm tra:
/// 1. Định dạng mã vạch EAN-8/13, UPC-A (^\d{8,14}$) - từ chối 400 ERR_VALIDATION.
/// 2. Hạn mức Khách (BR-GUEST-07) tối đa 03 lần/ngày qua Redis - vượt quá từ chối 403 ERR_QUOTA_BARCODE_EXCEEDED.
/// 3. Không tìm thấy ở cả CSDL và Open Food Facts - trả 404 ERR_PRODUCT_NOT_FOUND và không trừ lượt.
/// 4. Lỗi mạng/timeout Open Food Facts - trả 503 ERR_EXT_OPENFOODFACTS_FAILED.
/// 5. Sản phẩm CSDL còn hạn 30 ngày - dùng trực tiếp từ CSDL, không gọi Open Food Facts.
/// 6. Bóc tách phụ gia E-Numbers và kết luận chuẩn GREEN / YELLOW / RED.
/// 7. Thành viên: đối chiếu dị ứng cá nhân và ghi lịch sử UserScanHistory.
/// 8. Lấy danh sách lịch sử quét có phân trang và lọc theo verdict.
/// </summary>
public class ProductServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IOpenFoodFactsClient> _mockOffClient;
    private readonly Mock<IRedisService> _mockRedis;
    private readonly Mock<ILogger<ProductService>> _mockLogger;
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _mockOffClient = new Mock<IOpenFoodFactsClient>();
        _mockRedis = new Mock<IRedisService>();
        _mockLogger = new Mock<ILogger<ProductService>>();

        _service = new ProductService(
            _db,
            _mockOffClient.Object,
            _mockRedis.Object,
            _mockLogger.Object);

        SeedTestData();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private void SeedTestData()
    {
        // 1. Từ điển E-Numbers
        _db.EnumberDictionaries.AddRange(
            new EnumberDictionary
            {
                Code = "E120",
                Name = "Carmine / Cochineal",
                OriginSource = "ANIMAL",
                VeganStatus = "NON_VEGAN",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new EnumberDictionary
            {
                Code = "E471",
                Name = "Mono- and diglycerides of fatty acids",
                OriginSource = "VARIOUS",
                VeganStatus = "DOUBTFUL",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new EnumberDictionary
            {
                Code = "E300",
                Name = "Ascorbic acid (Vitamin C)",
                OriginSource = "PLANT",
                VeganStatus = "VEGAN",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );

        // 2. Dị ứng chuẩn
        _db.Allergens.AddRange(
            new Allergen { AllergenId = 1, AllergenName = "Đậu phộng", Code = "PEANUT", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Allergen { AllergenId = 2, AllergenName = "Sữa", Code = "MILK", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );

        // 3. Người dùng mẫu
        _db.Users.Add(new User
        {
            UserId = 10,
            Email = "member@veggielife.vn",
            FullName = "Nguyễn Văn Member",
            PasswordHash = "hash",
            Role = "MEMBER",
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        });

        // 4. Khai báo dị ứng của Member: dị ứng Đậu phộng
        _db.UserAllergies.Add(new UserAllergy
        {
            UserId = 10,
            AllergenId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _db.SaveChanges();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1234567")]      // 7 ký tự (< 8)
    [InlineData("123456789012345")] // 15 ký tự (> 14)
    [InlineData("8934567ABC")]   // chứa chữ cái
    public async Task ScanBarcode_InvalidBarcode_ThrowsBadRequestValidation(string invalidBarcode)
    {
        // Act
        Func<Task> act = async () => await _service.ScanBarcodeAsync(invalidBarcode, null);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task ScanBarcode_GuestQuotaExceeded_ThrowsForbiddenQuotaExceeded()
    {
        // Arrange
        var barcode = "8934567890123";
        var guestId = "guest-uuid-123";

        // Giả lập Redis đã ghi nhận 3 lượt quét của Khách
        _mockRedis.Setup(r => r.GetAsync<int?>($"guest_quota:{guestId}:barcode", It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        // Act
        Func<Task> act = async () => await _service.ScanBarcodeAsync(barcode, null, guestId);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrQuotaBarcodeExceeded);
    }

    [Fact]
    public async Task ScanBarcode_NotFoundInDbAndOff_ThrowsNotFound_DoesNotDeductQuota()
    {
        // Arrange
        var barcode = "8930000000001";
        var guestId = "guest-new";

        _mockRedis.Setup(r => r.GetAsync<int?>($"guest_quota:{guestId}:barcode", It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockOffClient.Setup(c => c.GetProductByBarcodeAsync(barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpenFoodFactsProductDto?)null);

        // Act
        Func<Task> act = async () => await _service.ScanBarcodeAsync(barcode, null, guestId);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrProductNotFound);

        // Đảm bảo không tăng bộ đếm lượt quét khi không tìm thấy
        _mockRedis.Verify(r => r.IncrementAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ScanBarcode_OpenFoodFactsNetworkFailure_ThrowsServiceUnavailable()
    {
        // Arrange
        var barcode = "8930000000002";
        _mockRedis.Setup(r => r.GetAsync<int?>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockOffClient.Setup(c => c.GetProductByBarcodeAsync(barcode, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OpenFoodFactsException("Timeout"));

        // Act
        Func<Task> act = async () => await _service.ScanBarcodeAsync(barcode, null, "guest-1");

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(503);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrExtOpenfoodfactsFailed);
    }

    [Fact]
    public async Task ScanBarcode_ProductFreshInDb_ReturnsCachedDataWithoutCallingOff()
    {
        // Arrange
        var barcode = "8931111222333";
        _db.Products.Add(new Product
        {
            Barcode = barcode,
            ProductName = "Sữa chua hạt sen thuần chay",
            Brand = "VeggieLife Brand",
            IngredientsText = "Hạt sen, nước, đường phèn, chất điều chỉnh độ acid (E300)",
            VeganVerdict = "VEGAN",
            AdminVerified = true,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            LastSyncedAt = DateTime.UtcNow.AddDays(-2) // Mới đồng bộ 2 ngày trước (< 30 ngày)
        });
        await _db.SaveChangesAsync();

        _mockRedis.Setup(r => r.GetAsync<int?>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mockRedis.Setup(r => r.IncrementAsync(It.IsAny<string>(), 1, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.ScanBarcodeAsync(barcode, null, "guest-1");

        // Assert
        result.Should().NotBeNull();
        result.Barcode.Should().Be(barcode);
        result.ProductName.Should().Be("Sữa chua hạt sen thuần chay");
        result.VeganVerdict.Should().Be("GREEN");
        result.Quota.Used.Should().Be(1);
        result.Quota.Remaining.Should().Be(2);

        // Khẳng định không gọi API ngoại vi khi cache CSDL còn hạn
        _mockOffClient.Verify(c => c.GetProductByBarcodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ScanBarcode_ExpiredInDb_CallsOffAndRefreshes()
    {
        // Arrange
        var barcode = "8939999888777";
        _db.Products.Add(new Product
        {
            Barcode = barcode,
            ProductName = "Bánh gạo lứt",
            Brand = "Thương hiệu cũ",
            IngredientsText = "Gạo lứt",
            VeganVerdict = "DOUBTFUL",
            AdminVerified = false,
            CreatedAt = DateTime.UtcNow.AddDays(-40),
            LastSyncedAt = DateTime.UtcNow.AddDays(-35) // Quá 30 ngày -> cần đồng bộ lại
        });
        await _db.SaveChangesAsync();

        _mockOffClient.Setup(c => c.GetProductByBarcodeAsync(barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenFoodFactsProductDto
            {
                Barcode = barcode,
                ProductName = "Bánh gạo lứt cao cấp",
                Brand = "Thương hiệu mới",
                IngredientsText = "Gạo lứt, muối khoáng",
                VeganVerdict = "VEGAN",
                VerdictText = "Thuần chay"
            });

        _mockRedis.Setup(r => r.GetAsync<int?>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mockRedis.Setup(r => r.IncrementAsync(It.IsAny<string>(), 1, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        var result = await _service.ScanBarcodeAsync(barcode, null, "guest-1");

        // Assert
        result.ProductName.Should().Be("Bánh gạo lứt cao cấp");
        result.VeganVerdict.Should().Be("GREEN");
        _mockOffClient.Verify(c => c.GetProductByBarcodeAsync(barcode, It.IsAny<CancellationToken>()), Times.Once);

        // Kiểm tra CSDL đã được cập nhật LastSyncedAt
        var updatedInDb = await _db.Products.FirstAsync(p => p.Barcode == barcode);
        updatedInDb.ProductName.Should().Be("Bánh gạo lứt cao cấp");
        (DateTime.UtcNow - updatedInDb.LastSyncedAt).TotalMinutes.Should().BeLessThan(1);
    }

    [Fact]
    public async Task ScanBarcode_ProductWithNonVeganAdditives_ReturnsRedVerdict()
    {
        // Arrange
        var barcode = "8935555666777";
        _mockOffClient.Setup(c => c.GetProductByBarcodeAsync(barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenFoodFactsProductDto
            {
                Barcode = barcode,
                ProductName = "Kẹo dẻo dâu tây",
                Brand = "Sweet Candy",
                IngredientsText = "Đường, siro ngô, phẩm màu tự nhiên (INS 120), hương dâu",
                VeganVerdict = "NON_VEGAN",
                Additives = new List<string> { "E120" }
            });

        _mockRedis.Setup(r => r.GetAsync<int?>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mockRedis.Setup(r => r.IncrementAsync(It.IsAny<string>(), 1, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.ScanBarcodeAsync(barcode, null, "guest-1");

        // Assert
        result.VeganVerdict.Should().Be("RED");
        result.DetectedAdditives.Should().ContainSingle(a => a.Code == "E120" && a.VeganStatus == "NON_VEGAN");
    }

    [Fact]
    public async Task ScanBarcode_MemberWithAllergies_TriggersWarningAndSavesHistory()
    {
        // Arrange
        var barcode = "8937777888999";
        _mockOffClient.Setup(c => c.GetProductByBarcodeAsync(barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenFoodFactsProductDto
            {
                Barcode = barcode,
                ProductName = "Bơ đậu phộng hạt giòn",
                Brand = "Nutty",
                IngredientsText = "Đậu phộng rang 90%, dầu thực vật, muối",
                VeganVerdict = "VEGAN"
            });

        // Act
        var result = await _service.ScanBarcodeAsync(barcode, userId: 10, guestId: null, method: "CAMERA_BARCODE");

        // Assert
        result.PersonalAllergyCheck.Should().BeTrue();
        result.AllergenWarnings.Should().Contain(w => w.Contains("Đậu phộng"));

        // Xác nhận đã lưu vào UserScanHistory
        var history = await _db.UserScanHistories.FirstOrDefaultAsync(h => h.UserId == 10 && h.Product.Barcode == barcode);
        history.Should().NotBeNull();
        history!.ScanMethod.Should().Be("CAMERA_BARCODE");
        history.IsNonVeganTriggered.Should().BeTrue(); // Vì có kích hoạt dị ứng cá nhân
    }

    [Fact]
    public async Task GetUserScanHistory_ReturnsPaginatedAndFiltered()
    {
        // Arrange
        var p1 = new Product { Barcode = "8930000000010", ProductName = "Món 1", VeganVerdict = "VEGAN", LastSyncedAt = DateTime.UtcNow };
        var p2 = new Product { Barcode = "8930000000020", ProductName = "Món 2", VeganVerdict = "NON_VEGAN", LastSyncedAt = DateTime.UtcNow };
        _db.Products.AddRange(p1, p2);
        await _db.SaveChangesAsync();

        _db.UserScanHistories.AddRange(
            new UserScanHistory { UserId = 10, ProductId = p1.ProductId, ScanMethod = "CAMERA_BARCODE", IsNonVeganTriggered = false, ScannedAt = DateTime.UtcNow.AddMinutes(-10) },
            new UserScanHistory { UserId = 10, ProductId = p2.ProductId, ScanMethod = "MANUAL_INPUT", IsNonVeganTriggered = true, ScannedAt = DateTime.UtcNow.AddMinutes(-5) }
        );
        await _db.SaveChangesAsync();

        // Act 1: Lấy tất cả
        var allScans = await _service.GetUserScanHistoryAsync(10, page: 1, size: 20);
        allScans.TotalItems.Should().Be(2);
        allScans.Items.First().ProductName.Should().Be("Món 2"); // Xếp ScannedAt giảm dần

        // Act 2: Lọc theo verdict RED
        var redScans = await _service.GetUserScanHistoryAsync(10, verdict: "RED", page: 1, size: 20);
        redScans.TotalItems.Should().Be(1);
        redScans.Items.First().ProductName.Should().Be("Món 2");
        redScans.Items.First().VeganVerdict.Should().Be("RED");
    }
}
