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
using VeggieLife.DAL.External.AiService;
using VeggieLife.DAL.External.OpenFoodFacts;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Products;

public class ProductServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IOpenFoodFactsClient> _mockOffClient;
    private readonly Mock<IAiServiceClient> _mockAiClient;
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
        _mockAiClient = new Mock<IAiServiceClient>();
        _mockRedis = new Mock<IRedisService>();
        _mockLogger = new Mock<ILogger<ProductService>>();

        _service = new ProductService(
            _db,
            _mockOffClient.Object,
            _mockAiClient.Object,
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
                VerdictText = "Thuần chay",
                ImageUrl = "https://images.openfoodfacts.org/images/products/893/999/988/8777/front_en.jpg"
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
        result.ImageUrl.Should().Be("https://images.openfoodfacts.org/images/products/893/999/988/8777/front_en.jpg");
        _mockOffClient.Verify(c => c.GetProductByBarcodeAsync(barcode, It.IsAny<CancellationToken>()), Times.Once);

        // Kiểm tra CSDL đã được cập nhật LastSyncedAt và ImageUrl
        var updatedInDb = await _db.Products.FirstAsync(p => p.Barcode == barcode);
        updatedInDb.ProductName.Should().Be("Bánh gạo lứt cao cấp");
        updatedInDb.ImageUrl.Should().Be("https://images.openfoodfacts.org/images/products/893/999/988/8777/front_en.jpg");
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
        var p1 = new Product { Barcode = "8930000000010", ProductName = "Món 1", ImageUrl = "https://images.openfoodfacts.org/1.jpg", VeganVerdict = "VEGAN", LastSyncedAt = DateTime.UtcNow };
        var p2 = new Product { Barcode = "8930000000020", ProductName = "Món 2", ImageUrl = "https://images.openfoodfacts.org/2.jpg", VeganVerdict = "NON_VEGAN", LastSyncedAt = DateTime.UtcNow };
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
        allScans.Items.First().ImageUrl.Should().Be("https://images.openfoodfacts.org/2.jpg");

        // Act 2: Lọc theo verdict RED
        var redScans = await _service.GetUserScanHistoryAsync(10, verdict: "RED", page: 1, size: 20);
        redScans.TotalItems.Should().Be(1);
        redScans.Items.First().ProductName.Should().Be("Món 2");
        redScans.Items.First().VeganVerdict.Should().Be("RED");
        redScans.Items.First().ImageUrl.Should().Be("https://images.openfoodfacts.org/2.jpg");
    }

    [Fact]
    public async Task ScanBarcode_NewProductWithImageUrl_SavesImageUrlToDatabaseAndReturnsInResponse()
    {
        // Arrange
        var barcode = "8938888777666";
        var expectedImageUrl = "https://images.openfoodfacts.org/images/products/893/888/877/7666/front_en.400.jpg";
        _mockOffClient.Setup(c => c.GetProductByBarcodeAsync(barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenFoodFactsProductDto
            {
                Barcode = barcode,
                ProductName = "Sữa yến mạch hữu cơ",
                Brand = "OatLife",
                ImageUrl = expectedImageUrl,
                IngredientsText = "Yến mạch, nước tinh khiết, muối biển",
                VeganVerdict = "VEGAN",
                VerdictText = "Thuần chay"
            });

        _mockRedis.Setup(r => r.GetAsync<int?>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mockRedis.Setup(r => r.IncrementAsync(It.IsAny<string>(), 1, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.ScanBarcodeAsync(barcode, null, "guest-1");

        // Assert
        result.Should().NotBeNull();
        result.ImageUrl.Should().Be(expectedImageUrl);

        var savedProduct = await _db.Products.FirstOrDefaultAsync(p => p.Barcode == barcode);
        savedProduct.Should().NotBeNull();
        savedProduct!.ImageUrl.Should().Be(expectedImageUrl);
    }

    [Fact]
    public async Task ScanBarcode_ProductInDbWithoutImage_QueriesOffAndBackfillsImage()
    {
        // Arrange - Sản phẩm đã có trong DB nhưng ImageUrl là null và chưa AdminVerified
        var barcode = "8931234567890";
        var expectedImageUrl = "https://images.openfoodfacts.org/images/products/893/123/456/7890/front_vi.400.jpg";

        _db.Products.Add(new Product
        {
            Barcode = barcode,
            ProductName = "Bánh quy thuần chay",
            Brand = "VeggieBrand",
            ImageUrl = null, // Chưa có ảnh
            IngredientsText = "Bột mì, dầu thực vật, đường",
            VeganVerdict = "VEGAN",
            AdminVerified = false,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            LastSyncedAt = DateTime.UtcNow.AddDays(-1)
        });
        await _db.SaveChangesAsync();

        _mockOffClient.Setup(c => c.GetProductByBarcodeAsync(barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenFoodFactsProductDto
            {
                Barcode = barcode,
                ProductName = "Bánh quy thuần chay cao cấp",
                Brand = "VeggieBrand",
                ImageUrl = expectedImageUrl,
                IngredientsText = "Bột mì, dầu thực vật, đường",
                VeganVerdict = "VEGAN",
                VerdictText = "Thuần chay"
            });

        _mockRedis.Setup(r => r.GetAsync<int?>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mockRedis.Setup(r => r.IncrementAsync(It.IsAny<string>(), 1, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.ScanBarcodeAsync(barcode, null, "guest-1");

        // Assert
        result.Should().NotBeNull();
        result.ImageUrl.Should().Be(expectedImageUrl);
        _mockOffClient.Verify(c => c.GetProductByBarcodeAsync(barcode, It.IsAny<CancellationToken>()), Times.Once);

        var updatedProduct = await _db.Products.FirstOrDefaultAsync(p => p.Barcode == barcode);
        updatedProduct.Should().NotBeNull();
        updatedProduct!.ImageUrl.Should().Be(expectedImageUrl);
    }

    [Fact]
    public async Task ScanBarcode_WithAiClassification_SavesAiQuotaLogsAndAuditLogs()
    {
        // Arrange
        var barcode = "8939999000111";
        var memberId = 10;

        _mockOffClient.Setup(c => c.GetProductByBarcodeAsync(barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenFoodFactsProductDto
            {
                Barcode = barcode,
                ProductName = "Bánh đậu xanh thuần chay",
                Brand = "Hải Dương",
                IngredientsText = "Bột đậu xanh, đường cát, dầu thực vật",
                VeganVerdict = "VEGAN",
                Additives = new List<string> { "E300" }
            });

        _mockAiClient.Setup(a => a.IsCircuitOpen).Returns(false);
        _mockAiClient.Setup(a => a.ClassifyBarcodeAsync(It.IsAny<AiBarcodeClassifyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiBarcodeClassifyResponse
            {
                VeganVerdict = "VEGAN",
                VerdictText = "Sản phẩm thuần chay 100%.",
                Confidence = 0.98,
                DetectedAdditives = new List<AiDetectedAdditiveDto>
                {
                    new() { Code = "E300", Name = "Acid ascorbic", VeganStatus = "VEGAN", Source = "DICTIONARY" }
                },
                Allergens = new List<AiBarcodeAllergenDto>
                {
                    new() { Name = "Đậu phộng", PresenceType = "MAY_CONTAIN" }
                },
                Usage = new AiUsageDto
                {
                    Model = "gemini-2.5-flash",
                    PromptTokens = 120,
                    CompletionTokens = 40,
                    TotalTokens = 160,
                    LatencyMs = 280,
                    Temperature = 0.15m
                },
                Audit = new AiAuditDto
                {
                    Prompt = "Thẩm định bánh đậu xanh",
                    RawResult = "{\"veganVerdict\":\"VEGAN\"}"
                }
            });

        // Act
        var result = await _service.ScanBarcodeAsync(barcode, memberId, null, "CAMERA_BARCODE");

        // Assert
        result.Should().NotBeNull();
        result.VeganVerdict.Should().Be("GREEN");
        result.VerdictText.Should().Be("Sản phẩm thuần chay 100%.");
        result.DetectedAdditives.Should().ContainSingle(a => a.Code == "E300");

        // Kiểm tra AIQuotaLogs và AuditLogs đã được ghi nhận cho member
        var quotaLog = await _db.AiquotaLogs.FirstOrDefaultAsync(l => l.UserId == memberId && l.FeatureCode == "AI_BARCODE");
        quotaLog.Should().NotBeNull();
        quotaLog!.TokensConsumed.Should().Be(160);
        quotaLog.RequestStatus.Should().Be("SUCCESS");

        var auditLog = await _db.AuditLogs.FirstOrDefaultAsync(a => a.TargetTable == "Products" && a.Action == "AI_BARCODE_CLASSIFY");
        auditLog.Should().NotBeNull();
        auditLog!.ActorUserId.Should().Be(memberId);
        auditLog.NewValue.Should().Contain("gemini-2.5-flash");

        // Kiểm tra ProductAllergens được lưu và kích hoạt cảnh báo dị ứng Đậu phộng của member
        var product = await _db.Products.Include(p => p.ProductAllergens).FirstOrDefaultAsync(p => p.Barcode == barcode);
        product.Should().NotBeNull();
        product!.ProductAllergens.Should().NotBeEmpty();
        result.PersonalAllergyCheck.Should().BeTrue();
    }

    [Fact]
    public async Task ScanBarcode_AdminVerifiedProduct_NeverOverwritten()
    {
        // Arrange - Sản phẩm đã được Admin duyệt VEGAN
        var barcode = "8934444555666";
        _db.Products.Add(new Product
        {
            Barcode = barcode,
            ProductName = "Sữa hạt điều hữu cơ",
            Brand = "Hạt Việt",
            IngredientsText = "Hạt điều nguyên chất",
            VeganVerdict = "VEGAN",
            AdminVerified = true, // Đã được Admin kiểm định
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            LastSyncedAt = DateTime.UtcNow.AddDays(-35) // Đã quá hạn 30 ngày
        });
        await _db.SaveChangesAsync();

        // OFF trả về NON_VEGAN
        _mockOffClient.Setup(c => c.GetProductByBarcodeAsync(barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenFoodFactsProductDto
            {
                Barcode = barcode,
                ProductName = "Sữa hạt điều công nghiệp (tên mới)",
                Brand = "Hạt Việt Mới",
                IngredientsText = "Hạt điều, sữa bò",
                VeganVerdict = "NON_VEGAN"
            });

        // AI cũng trả về NON_VEGAN
        _mockAiClient.Setup(a => a.IsCircuitOpen).Returns(false);
        _mockAiClient.Setup(a => a.ClassifyBarcodeAsync(It.IsAny<AiBarcodeClassifyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiBarcodeClassifyResponse
            {
                VeganVerdict = "NON_VEGAN",
                VerdictText = "Có sữa bò"
            });

        _mockRedis.Setup(r => r.GetAsync<int?>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _service.ScanBarcodeAsync(barcode, null, "guest-1");

        // Assert: Kết luận vẫn phải là GREEN vì AdminVerified = true không bao giờ bị ghi đè
        result.VeganVerdict.Should().Be("GREEN");

        var inDb = await _db.Products.FirstAsync(p => p.Barcode == barcode);
        inDb.VeganVerdict.Should().Be("VEGAN");
        inDb.AdminVerified.Should().BeTrue();
    }

    [Fact]
    public async Task ScanBarcode_AiCircuitOpen_UsesRuleBasedFallbackAndWritesAuditLogs()
    {
        // Arrange - Mạch AI đang OPEN
        var barcode = "8936666777888";
        _mockOffClient.Setup(c => c.GetProductByBarcodeAsync(barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenFoodFactsProductDto
            {
                Barcode = barcode,
                ProductName = "Thạch dâu tây",
                Brand = "Jelly",
                IngredientsText = "Đường, nước ép dâu, chất tạo màu carmine (E120)",
                VeganVerdict = "NON_VEGAN",
                Additives = new List<string> { "E120" }
            });

        _mockAiClient.Setup(a => a.IsCircuitOpen).Returns(true); // Circuit Open!

        _mockRedis.Setup(r => r.GetAsync<int?>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mockRedis.Setup(r => r.IncrementAsync(It.IsAny<string>(), 1, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.ScanBarcodeAsync(barcode, null, "guest-1");

        // Assert: Fallback engine dùng từ điển ENumberDictionary nhận diện E120 là NON_VEGAN -> RED
        result.VeganVerdict.Should().Be("RED");
        result.DetectedAdditives.Should().ContainSingle(a => a.Code == "E120" && a.VeganStatus == "NON_VEGAN");

        // Xác nhận đã ghi AuditLog với Action AI_BARCODE_FALLBACK
        var fallbackAudit = await _db.AuditLogs.FirstOrDefaultAsync(a => a.TargetTable == "Products" && a.Action == "AI_BARCODE_FALLBACK");
        fallbackAudit.Should().NotBeNull();
    }

    [Fact]
    public async Task ScanBarcode_GuestQuotaBoundary_ThirdAttemptSucceeds_FourthAttemptBlocked()
    {
        // Arrange - Lần quét thứ 3 (đang ở 2, chưa quá 3)
        var barcode = "8931111222999";
        var guestId = "guest-boundary-uuid";
        var guestKey = $"guest_quota:{guestId}:barcode";

        _mockOffClient.Setup(c => c.GetProductByBarcodeAsync(barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenFoodFactsProductDto
            {
                Barcode = barcode,
                ProductName = "Nước khoáng thiên nhiên",
                VeganVerdict = "VEGAN"
            });

        _mockRedis.Setup(r => r.GetAsync<int?>(guestKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2); // Đã dùng 2 lần -> lần này là lần 3
        _mockRedis.Setup(r => r.IncrementAsync(guestKey, 1, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3); // Tăng lên 3

        // Act - Lần 3 thành công
        var resultThird = await _service.ScanBarcodeAsync(barcode, null, guestId);
        resultThird.Should().NotBeNull();
        resultThird.Quota.Used.Should().Be(3);
        resultThird.Quota.Remaining.Should().Be(0);

        // Lần 4: Redis trả về 3 -> Bị chặn 403
        _mockRedis.Setup(r => r.GetAsync<int?>(guestKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        Func<Task> actFourth = async () => await _service.ScanBarcodeAsync(barcode, null, guestId);
        var ex = await actFourth.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrQuotaBarcodeExceeded);
    }

    [Fact]
    public async Task ScanBarcode_OffProductWithoutIngredientsOrAdditives_ConcludesDoubtful()
    {
        // Arrange - Open Food Facts trả về không có ingredients_text và không có additives
        var barcode = "8935555111222";
        _mockOffClient.Setup(c => c.GetProductByBarcodeAsync(barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenFoodFactsProductDto
            {
                Barcode = barcode,
                ProductName = "Trà giải khát",
                Brand = "TeaBrand",
                IngredientsText = null, // Không có thành phần
                Additives = new List<string>(), // Không có phụ gia
                AdditivesTags = new List<string>(),
                VeganVerdict = "DOUBTFUL"
            });

        _mockRedis.Setup(r => r.GetAsync<int?>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _service.ScanBarcodeAsync(barcode, null, "guest-1");

        // Assert: Tuyệt đối không đoán VEGAN (GREEN) khi thiếu dữ liệu, phải trả DOUBTFUL (YELLOW)
        result.VeganVerdict.Should().Be("YELLOW");
    }
}

