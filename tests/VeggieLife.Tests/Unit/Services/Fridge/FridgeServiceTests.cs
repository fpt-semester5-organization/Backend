using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Helpers;
using VeggieLife.BLL.DTOs.Fridge.Requests;
using VeggieLife.BLL.DTOs.Media.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Fridge;
using VeggieLife.BLL.Services.Media;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using VeggieLife.DAL.External.AiService;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Fridge;

/// <summary>
/// Bộ kiểm thử đơn vị toàn diện cho FridgeService (FN-VIS-01, FN-VIS-02).
/// Kiểm tra:
/// 1. Kiểm tra kích thước ảnh < 640x480 bị từ chối 422 (ERR_FRIDGE_IMAGE_TOO_SMALL).
/// 2. Kiểm tra ảnh > 10 MB bị từ chối 413 (ERR_MEDIA_FILE_TOO_LARGE).
/// 3. Kiểm tra chữ ký magic bytes giả mạo bị từ chối 400 (ERR_VALIDATION).
/// 4. Hết hạn mức: Khách vượt 3 lượt, Member vượt 10 lượt/ngày, VIP vượt 30 lượt/ngày báo 403 (ERR_QUOTA_AI_EXCEEDED).
/// 5. AI ngắt mạch (Circuit Breaker OPEN) báo 503 (ERR_AI_CIRCUIT_OPEN).
/// 6. Đối chiếu tên nguyên liệu độ giống >= 80%: khớp thì lưu vào tủ lạnh ảo, không khớp thì không lưu.
/// 7. Khách vãng lai: chỉ xem kết quả, không lưu DB, trừ lượt AI.
/// 8. Thành viên: lưu FridgeSessions, UPSERT FridgeInventory, ghi AIQuotaLogs.
/// 9. Quản lý tủ lạnh ảo (FN-VIS-02): xem (NON-FRESH xếp trước), thêm tay, sửa độ tươi, xóa.
/// </summary>
public class FridgeServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IAiServiceClient> _mockAiClient;
    private readonly Mock<IRedisService> _mockRedis;
    private readonly Mock<IMediaService> _mockMediaService;
    private readonly Mock<ILogger<FridgeService>> _mockLogger;
    private readonly FridgeService _service;

    public FridgeServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _mockAiClient = new Mock<IAiServiceClient>();
        _mockRedis = new Mock<IRedisService>();
        _mockMediaService = new Mock<IMediaService>();
        _mockLogger = new Mock<ILogger<FridgeService>>();

        // Mặc định Circuit Breaker đóng (bình thường)
        _mockAiClient.Setup(c => c.IsCircuitOpen).Returns(false);

        // Mặc định MediaService upload thành công
        _mockMediaService.Setup(m => m.UploadImageAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaUploadResponse
            {
                SecureUrl = "https://cdn.veggielife.vn/fridge/mock-image.jpg",
                PublicId = "mock-public-id"
            });

        _service = new FridgeService(
            _db,
            _mockAiClient.Object,
            _mockRedis.Object,
            _mockMediaService.Object,
            _mockLogger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // 1. Danh mục nguyên liệu mẫu
        _db.Ingredients.AddRange(
            new Ingredient
            {
                IngredientId = 12,
                IngredientName = "Nấm rơm",
                FoodGroup = "MUSHROOM",
                StandardUnit = "g",
                DefaultStallType = "PRODUCE",
                CaloriesPer100g = 32m
            },
            new Ingredient
            {
                IngredientId = 13,
                IngredientName = "Cà chua",
                FoodGroup = "VEGETABLE",
                StandardUnit = "quả",
                DefaultStallType = "PRODUCE",
                CaloriesPer100g = 18m
            },
            new Ingredient
            {
                IngredientId = 14,
                IngredientName = "Đậu hũ non",
                FoodGroup = "SOY",
                StandardUnit = "miếng",
                DefaultStallType = "TOFU",
                CaloriesPer100g = 76m
            },
            new Ingredient
            {
                IngredientId = 15,
                IngredientName = "Hành tây",
                FoodGroup = "ALLIUM",
                IsAllium = true,
                StandardUnit = "củ",
                DefaultStallType = "PRODUCE",
                CaloriesPer100g = 40m
            }
        );

        // 2. Trường phái & người dùng mẫu
        var veganSchool = new DietarySchool
        {
            DietarySchoolId = 1,
            SchoolCode = "VEGAN",
            SchoolName = "Thuần chay",
            ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"MEAT\",\"SEAFOOD\",\"DAIRY\",\"EGG\"]}"
        };
        _db.DietarySchools.Add(veganSchool);

        var memberUser = new User
        {
            UserId = 10,
            Email = "member@veggielife.vn",
            FullName = "Nguyễn Văn A",
            Role = UserRoles.Member,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(memberUser);

        var memberProfile = new HealthProfile
        {
            UserId = 10,
            DietarySchoolId = 1,
            Gender = "MALE",
            HealthGoal = "MAINTAIN",
            RegionPreference = "MIEN_BAC",
            RequiresAlliumFree = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.HealthProfiles.Add(memberProfile);

        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    // =========================================================================
    // NHÓM 1: KIỂM THỬ RÀNG BUỘC KÍCH THƯỚC VÀ ĐỊNH DẠNG ẢNH (BR-AI-CV-01)
    // =========================================================================

    [Fact]
    public async Task DetectIngredients_ImageSmallerThan640x480_Throws422FridgeImageTooSmall()
    {
        // Arrange (Ảnh PNG kích thước 300x300 px < 640x480)
        using var smallImageStream = CreateValidPngStream(300, 300);
        var input = new FridgeDetectionInputDto
        {
            ImageStream = smallImageStream,
            FileName = "small_fridge.png",
            FileLength = smallImageStream.Length,
            ContentType = "image/png"
        };

        // Act & Assert
        var act = async () => await _service.DetectIngredientsAsync(
            input,
            userId: 10,
            userRole: UserRoles.Member,
            guestUuid: null,
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(422);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrFridgeImageTooSmall);
        ex.Which.Message.Should().Contain("640×480");
    }

    [Fact]
    public async Task DetectIngredients_ImageResolutionAtLeast640x480_PassesDimensionCheck()
    {
        // Arrange (Ảnh PNG đạt chuẩn 640x480 px)
        using var validImageStream = CreateValidPngStream(640, 480);
        var input = new FridgeDetectionInputDto
        {
            ImageStream = validImageStream,
            FileName = "valid_fridge.png",
            FileLength = validImageStream.Length,
            ContentType = "image/png"
        };

        _mockAiClient.Setup(c => c.DetectFridgeIngredientsAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiFridgeDetectionResult
            {
                Success = true,
                Items = new List<AiDetectedItemDto>
                {
                    new() { Name = "Nấm rơm", FreshnessStatus = "FRESH" }
                }
            });

        // Act
        var result = await _service.DetectIngredientsAsync(
            input,
            userId: 10,
            userRole: UserRoles.Member,
            guestUuid: null,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Detected.Should().HaveCount(1);
    }

    [Fact]
    public async Task DetectIngredients_ImageFileLargerThan10MB_Throws413MediaFileTooLarge()
    {
        // Arrange
        using var stream = CreateValidPngStream(800, 600);
        var input = new FridgeDetectionInputDto
        {
            ImageStream = stream,
            FileName = "large_fridge.png",
            FileLength = 10 * 1024 * 1024 + 1, // > 10 MB
            ContentType = "image/png"
        };

        // Act & Assert
        var act = async () => await _service.DetectIngredientsAsync(
            input,
            userId: 10,
            userRole: UserRoles.Member,
            guestUuid: null,
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(413);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrMediaFileTooLarge);
    }

    [Fact]
    public async Task DetectIngredients_InvalidMagicBytes_Throws400Validation()
    {
        // Arrange (Tệp đuôi .png nhưng chứa nội dung text giả mạo)
        using var fakeStream = new MemoryStream(Encoding.UTF8.GetBytes("Fake PNG content not having magic bytes"));
        var input = new FridgeDetectionInputDto
        {
            ImageStream = fakeStream,
            FileName = "fake.png",
            FileLength = fakeStream.Length,
            ContentType = "image/png"
        };

        // Act & Assert
        var act = async () => await _service.DetectIngredientsAsync(
            input,
            userId: 10,
            userRole: UserRoles.Member,
            guestUuid: null,
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    // =========================================================================
    // NHÓM 2: KIỂM THỬ HẠN MỨC QUOTA (BR-GUEST-02, BR-PAY-SUB-01)
    // =========================================================================

    [Fact]
    public async Task DetectIngredients_GuestExceeded3Trials_Throws403QuotaAiExceeded()
    {
        // Arrange
        var guestUuid = "guest-exceeded-uuid";
        _mockRedis.Setup(r => r.GetAsync<long?>($"guest_quota:ai:{guestUuid}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(3); // Đã dùng 3 lượt

        using var stream = CreateValidPngStream(640, 480);
        var input = new FridgeDetectionInputDto
        {
            ImageStream = stream,
            FileName = "fridge.png",
            FileLength = stream.Length,
            ContentType = "image/png"
        };

        // Act & Assert
        var act = async () => await _service.DetectIngredientsAsync(
            input,
            userId: null,
            userRole: UserRoles.Guest,
            guestUuid: guestUuid,
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrQuotaAiExceeded);
        ex.Which.Message.Should().Contain("03 lượt AI dùng thử");
    }

    [Fact]
    public async Task DetectIngredients_MemberExceeded10PerDay_Throws403QuotaAiExceeded()
    {
        // Arrange: Tạo 10 bản ghi thành công trong ngày hôm nay cho Member
        var today = DateTime.UtcNow;
        for (int i = 0; i < 10; i++)
        {
            _db.AiquotaLogs.Add(new AiquotaLog
            {
                UserId = 10,
                FeatureCode = "AI_FRIDGE",
                RequestStatus = "SUCCESS",
                CreatedAt = today
            });
        }
        await _db.SaveChangesAsync();

        using var stream = CreateValidPngStream(640, 480);
        var input = new FridgeDetectionInputDto
        {
            ImageStream = stream,
            FileName = "fridge.png",
            FileLength = stream.Length,
            ContentType = "image/png"
        };

        // Act & Assert
        var act = async () => await _service.DetectIngredientsAsync(
            input,
            userId: 10,
            userRole: UserRoles.Member,
            guestUuid: null,
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrQuotaAiExceeded);
        ex.Which.Message.Should().Contain("10 ảnh/ngày");
    }

    [Fact]
    public async Task DetectIngredients_VipExceeded30PerDay_Throws403QuotaAiExceeded()
    {
        // Arrange: VIP người dùng ID 20 có 30 bản ghi hôm nay
        var vipUserId = 20;
        var today = DateTime.UtcNow;
        for (int i = 0; i < 30; i++)
        {
            _db.AiquotaLogs.Add(new AiquotaLog
            {
                UserId = vipUserId,
                FeatureCode = "AI_FRIDGE",
                RequestStatus = "SUCCESS",
                CreatedAt = today
            });
        }
        await _db.SaveChangesAsync();

        using var stream = CreateValidPngStream(640, 480);
        var input = new FridgeDetectionInputDto
        {
            ImageStream = stream,
            FileName = "fridge.png",
            FileLength = stream.Length,
            ContentType = "image/png"
        };

        // Act & Assert
        var act = async () => await _service.DetectIngredientsAsync(
            input,
            userId: vipUserId,
            userRole: UserRoles.Vip,
            guestUuid: null,
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrQuotaAiExceeded);
        ex.Which.Message.Should().Contain("30 ảnh/ngày");
    }

    // =========================================================================
    // NHÓM 3: KIỂM THỬ CIRCUIT BREAKER & DỊCH VỤ AI (BR-ADM-AI-002)
    // =========================================================================

    [Fact]
    public async Task DetectIngredients_CircuitBreakerOpen_Throws503AiCircuitOpen()
    {
        // Arrange
        _mockAiClient.Setup(c => c.IsCircuitOpen).Returns(true);

        using var stream = CreateValidPngStream(640, 480);
        var input = new FridgeDetectionInputDto
        {
            ImageStream = stream,
            FileName = "fridge.png",
            FileLength = stream.Length,
            ContentType = "image/png"
        };

        // Act & Assert
        var act = async () => await _service.DetectIngredientsAsync(
            input,
            userId: 10,
            userRole: UserRoles.Member,
            guestUuid: null,
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(503);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrAiCircuitOpen);
    }

    // =========================================================================
    // NHÓM 4: LUỒNG THÀNH VIÊN VÀ KHÁCH VÃNG LAI (FN-VIS-01)
    // =========================================================================

    [Fact]
    public async Task DetectIngredients_MemberSuccess_SavesSessionAndUpsertsFridgeInventoryAndQuotaLog()
    {
        // Arrange
        using var stream = CreateValidPngStream(640, 480);
        var input = new FridgeDetectionInputDto
        {
            ImageStream = stream,
            FileName = "fridge.png",
            FileLength = stream.Length,
            ContentType = "image/png"
        };

        _mockAiClient.Setup(c => c.DetectFridgeIngredientsAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiFridgeDetectionResult
            {
                Success = true,
                Items = new List<AiDetectedItemDto>
                {
                    new() { Name = "Nấm rơm tươi", FreshnessStatus = "FRESH" }, // Khớp >= 80% với "Nấm rơm" (ID 12)
                    new() { Name = "Rau rừng lạ không có trong từ điển", FreshnessStatus = "NON-FRESH" } // Không khớp
                }
            });

        // Act
        var response = await _service.DetectIngredientsAsync(
            input,
            userId: 10,
            userRole: UserRoles.Member,
            guestUuid: null,
            CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Saved.Should().BeTrue();
        response.Detected.Should().HaveCount(2);

        // Mục khớp Nấm rơm: được lưu và có inventoryId, ingredientId = 12
        var matchedItem = response.Detected.First(d => d.Name == "Nấm rơm");
        matchedItem.InventoryId.Should().NotBeNull();
        matchedItem.IngredientId.Should().Be(12);
        matchedItem.FreshnessStatus.Should().Be("FRESH");

        // Mục không khớp: không được lưu, inventoryId = null, ingredientId = null
        var unmatchedItem = response.Detected.First(d => d.Name.Contains("Rau rừng lạ"));
        unmatchedItem.InventoryId.Should().NotBeNull();
        unmatchedItem.IngredientId.Should().NotBeNull();
        unmatchedItem.FreshnessStatus.Should().Be("NON-FRESH");

        // Kiểm tra trong DB: có FridgeSession, có FridgeInventory, có AiquotaLog
        var session = await _db.FridgeSessions.FirstOrDefaultAsync(s => s.UserId == 10);
        session.Should().NotBeNull();
        session!.DetectedItemsCount.Should().Be(2);

        var invItem = await _db.FridgeInventories.FirstOrDefaultAsync(f => f.UserId == 10 && f.IngredientId == 12);
        invItem.Should().NotBeNull();
        invItem!.FreshnessStatus.Should().Be("FRESH");

        var log = await _db.AiquotaLogs.FirstOrDefaultAsync(l => l.UserId == 10 && l.FeatureCode == "AI_FRIDGE");
        log.Should().NotBeNull();
        log!.RequestStatus.Should().Be("SUCCESS");

        // Hạn mức
        response.Quota.Used.Should().Be(1);
        response.Quota.Limit.Should().Be(10);
        response.Quota.Remaining.Should().Be(9);
    }

    [Fact]
    public async Task DetectIngredients_GuestSuccess_DoesNotSaveToDb_IncrementsRedis()
    {
        // Arrange
        var guestUuid = "guest-new-uuid";
        _mockRedis.Setup(r => r.GetAsync<long?>($"guest_quota:ai:{guestUuid}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        using var stream = CreateValidPngStream(640, 480);
        var input = new FridgeDetectionInputDto
        {
            ImageStream = stream,
            FileName = "fridge.png",
            FileLength = stream.Length,
            ContentType = "image/png"
        };

        _mockAiClient.Setup(c => c.DetectFridgeIngredientsAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiFridgeDetectionResult
            {
                Success = true,
                Items = new List<AiDetectedItemDto>
                {
                    new() { Name = "Cà chua", FreshnessStatus = "FRESH" }
                }
            });

        // Act
        var response = await _service.DetectIngredientsAsync(
            input,
            userId: null,
            userRole: UserRoles.Guest,
            guestUuid: guestUuid,
            CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Saved.Should().BeFalse();
        response.Detected.Should().HaveCount(1);
        response.Detected[0].InventoryId.Should().BeNull(); // Không lưu vào DB

        // Đảm bảo không ghi bảng SQL nào
        var sessionCount = await _db.FridgeSessions.CountAsync();
        sessionCount.Should().Be(0);
        var invCount = await _db.FridgeInventories.CountAsync();
        invCount.Should().Be(0);

        // Redis được gọi lưu lượt tăng lên 1
        _mockRedis.Verify(r => r.SetAsync(
            $"guest_quota:ai:{guestUuid}",
            1,
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        response.Quota.Used.Should().Be(1);
        response.Quota.Limit.Should().Be(3);
        response.Quota.Remaining.Should().Be(2);
    }

    [Fact]
    public async Task DetectIngredients_MeatOrForbiddenFoodDetected_GeneratesWarnings()
    {
        // Arrange
        using var stream = CreateValidPngStream(640, 480);
        var input = new FridgeDetectionInputDto
        {
            ImageStream = stream,
            FileName = "fridge.png",
            FileLength = stream.Length,
            ContentType = "image/png"
        };

        _mockAiClient.Setup(c => c.DetectFridgeIngredientsAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiFridgeDetectionResult
            {
                Success = true,
                Items = new List<AiDetectedItemDto>
                {
                    new() { Name = "Thịt bò nạc", FreshnessStatus = "FRESH", FoodGroup = "MEAT" },
                    new() { Name = "Cà chua", FreshnessStatus = "FRESH", FoodGroup = "VEGETABLE" }
                }
            });

        // Act
        var response = await _service.DetectIngredientsAsync(
            input,
            userId: 10,
            userRole: UserRoles.Member,
            guestUuid: null,
            CancellationToken.None);

        // Assert
        response.Warnings.Should().NotBeEmpty();
        response.Warnings.Any(w => w.Contains("Thịt bò nạc")).Should().BeTrue();
    }

    // =========================================================================
    // NHÓM 5: QUẢN LÝ NGUYÊN LIỆU TRONG TỦ LẠNH ẢO (FN-VIS-02)
    // =========================================================================

    [Fact]
    public async Task GetFridgeItems_OrdersNonFreshBeforeFresh()
    {
        // Arrange
        _db.FridgeInventories.AddRange(
            new FridgeInventory
            {
                InventoryId = 1,
                UserId = 10,
                IngredientId = 12,
                CustomItemName = "Nấm rơm tươi",
                FreshnessStatus = "FRESH"
            },
            new FridgeInventory
            {
                InventoryId = 2,
                UserId = 10,
                IngredientId = 13,
                CustomItemName = "Cà chua chín mềm",
                FreshnessStatus = "NON-FRESH"
            }
        );
        await _db.SaveChangesAsync();

        // Act
        var items = await _service.GetFridgeItemsAsync(10, null, CancellationToken.None);

        // Assert: NON-FRESH phải xếp trước FRESH
        items.Should().HaveCount(2);
        items[0].FreshnessStatus.Should().Be("NON-FRESH");
        items[0].Name.Should().Be("Cà chua chín mềm");
        items[1].FreshnessStatus.Should().Be("FRESH");
        items[1].Name.Should().Be("Nấm rơm tươi");
    }

    [Fact]
    public async Task GetFridgeItems_FilterByFreshness_ReturnsFilteredList()
    {
        // Arrange
        _db.FridgeInventories.AddRange(
            new FridgeInventory
            {
                InventoryId = 10,
                UserId = 10,
                IngredientId = 12,
                CustomItemName = "Nấm rơm",
                FreshnessStatus = "FRESH"
            },
            new FridgeInventory
            {
                InventoryId = 11,
                UserId = 10,
                IngredientId = 13,
                CustomItemName = "Cà chua",
                FreshnessStatus = "NON-FRESH"
            }
        );
        await _db.SaveChangesAsync();

        // Act
        var freshOnly = await _service.GetFridgeItemsAsync(10, "FRESH", CancellationToken.None);

        // Assert
        freshOnly.Should().HaveCount(1);
        freshOnly[0].FreshnessStatus.Should().Be("FRESH");
    }

    [Fact]
    public async Task AddFridgeItem_AlreadyExists_Throws409FridgeItemExists()
    {
        // Arrange
        _db.FridgeInventories.Add(new FridgeInventory
        {
            InventoryId = 1,
            UserId = 10,
            IngredientId = 12,
            CustomItemName = "Nấm rơm",
            FreshnessStatus = "FRESH"
        });
        await _db.SaveChangesAsync();

        var request = new CreateFridgeItemRequest { IngredientId = 12 };

        // Act & Assert
        var act = async () => await _service.AddFridgeItemAsync(10, request, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(409);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrFridgeItemExists);
    }

    [Fact]
    public async Task AddFridgeItem_ValidIngredient_Returns201Item()
    {
        // Arrange
        var request = new CreateFridgeItemRequest
        {
            IngredientId = 13,
            CustomItemName = "Cà chua bi"
        };

        // Act
        var result = await _service.AddFridgeItemAsync(10, request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IngredientId.Should().Be(13);
        result.Name.Should().Be("Cà chua bi");
        result.FreshnessStatus.Should().Be("FRESH");
        result.Quantity.Should().Be(1);
        result.Unit.Should().Be("quả");

        var inDb = await _db.FridgeInventories.FirstOrDefaultAsync(f => f.UserId == 10 && f.IngredientId == 13);
        inDb.Should().NotBeNull();
        inDb!.Quantity.Should().Be(1);
        inDb.Unit.Should().Be("quả");
    }

    [Fact]
    public async Task AddFridgeItem_WithQuantityAndUnit_ReturnsAndSavesExactValues()
    {
        // Arrange: Thêm nguyên liệu với số lượng (VD: 3) và đơn vị (VD: "quả")
        var request = new CreateFridgeItemRequest
        {
            IngredientId = 13, // Cà chua
            CustomItemName = "Cà chua bi vườn nhà",
            Quantity = 3,
            Unit = "quả"
        };

        // Act
        var result = await _service.AddFridgeItemAsync(11, request, CancellationToken.None);

        // Assert: API trả về chính xác số lượng và đơn vị
        result.Should().NotBeNull();
        result.Quantity.Should().Be(3);
        result.Unit.Should().Be("quả");

        var inDb = await _db.FridgeInventories.FirstOrDefaultAsync(f => f.UserId == 11 && f.IngredientId == 13);
        inDb.Should().NotBeNull();
        inDb!.Quantity.Should().Be(3);
        inDb.Unit.Should().Be("quả");
    }

    [Fact]
    public async Task AddFridgeItem_WithoutUnit_DefaultsToIngredientStandardUnit()
    {
        // Arrange: Thêm Nấm rơm (StandardUnit = "g") nhưng không truyền Unit
        var request = new CreateFridgeItemRequest
        {
            IngredientId = 12, // Nấm rơm
            Quantity = 250,
            Unit = null
        };

        // Act
        var result = await _service.AddFridgeItemAsync(12, request, CancellationToken.None);

        // Assert: Tự động dùng StandardUnit ("g") của nguyên liệu
        result.Should().NotBeNull();
        result.Quantity.Should().Be(250);
        result.Unit.Should().Be("g");

        var inDb = await _db.FridgeInventories.FirstOrDefaultAsync(f => f.UserId == 12 && f.IngredientId == 12);
        inDb.Should().NotBeNull();
        inDb!.Quantity.Should().Be(250);
        inDb.Unit.Should().Be("g");
    }

    [Fact]
    public async Task AddFridgeItem_NegativeQuantity_Throws400Validation()
    {
        // Arrange
        var request = new CreateFridgeItemRequest
        {
            IngredientId = 13,
            Quantity = -5
        };

        // Act & Assert
        var act = async () => await _service.AddFridgeItemAsync(13, request, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task UpdateFridgeItem_ValidInput_UpdatesFreshnessAndCustomName()
    {
        // Arrange
        var item = new FridgeInventory
        {
            InventoryId = 99,
            UserId = 10,
            IngredientId = 14,
            CustomItemName = "Đậu hũ non",
            FreshnessStatus = "FRESH",
            Quantity = 2,
            Unit = "miếng"
        };
        _db.FridgeInventories.Add(item);
        await _db.SaveChangesAsync();

        var request = new UpdateFridgeItemRequest
        {
            CustomItemName = "Đậu hũ non lướt ván",
            FreshnessStatus = "NON-FRESH",
            Quantity = 4,
            Unit = "bìa"
        };

        // Act
        var updated = await _service.UpdateFridgeItemAsync(10, 99, request, CancellationToken.None);

        // Assert
        updated.Should().NotBeNull();
        updated.Name.Should().Be("Đậu hũ non lướt ván");
        updated.FreshnessStatus.Should().Be("NON-FRESH");
        updated.Quantity.Should().Be(4);
        updated.Unit.Should().Be("bìa");

        var inDb = await _db.FridgeInventories.FindAsync(99);
        inDb!.FreshnessStatus.Should().Be("NON-FRESH");
        inDb.Quantity.Should().Be(4);
        inDb.Unit.Should().Be("bìa");
    }

    [Fact]
    public async Task UpdateFridgeItem_ZeroOrNegativeQuantity_Throws400Validation()
    {
        // Arrange
        var item = new FridgeInventory
        {
            InventoryId = 199,
            UserId = 10,
            IngredientId = 14,
            CustomItemName = "Đậu hũ non",
            FreshnessStatus = "FRESH",
            Quantity = 2,
            Unit = "miếng"
        };
        _db.FridgeInventories.Add(item);
        await _db.SaveChangesAsync();

        var request = new UpdateFridgeItemRequest
        {
            Quantity = 0
        };

        // Act & Assert
        var act = async () => await _service.UpdateFridgeItemAsync(10, 199, request, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task UpdateFridgeItem_ItemNotFound_Throws404NotFound()
    {
        // Arrange
        var request = new UpdateFridgeItemRequest { FreshnessStatus = "NON-FRESH" };

        // Act & Assert
        var act = async () => await _service.UpdateFridgeItemAsync(10, 9999, request, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrFridgeItemNotFound);
    }

    [Fact]
    public async Task DeleteFridgeItem_ExistingItem_RemovesFromDb()
    {
        // Arrange
        var item = new FridgeInventory
        {
            InventoryId = 77,
            UserId = 10,
            IngredientId = 12,
            CustomItemName = "Nấm rơm",
            FreshnessStatus = "FRESH"
        };
        _db.FridgeInventories.Add(item);
        await _db.SaveChangesAsync();

        // Act
        await _service.DeleteFridgeItemAsync(10, 77, CancellationToken.None);

        // Assert
        var inDb = await _db.FridgeInventories.FindAsync(77);
        inDb.Should().BeNull();
    }

    [Fact]
    public async Task DeleteFridgeItem_ItemNotFound_Throws404NotFound()
    {
        // Act & Assert
        var act = async () => await _service.DeleteFridgeItemAsync(10, 9999, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrFridgeItemNotFound);
    }

    // =========================================================================
    // FN-VIS-03: GỢI Ý CÔNG THỨC MÓN ĂN TỪ NGUYÊN LIỆU TRONG TỦ LẠNH ẢO
    // =========================================================================

    [Fact]
    public async Task GetRecipeSuggestions_EmptyFridge_Throws409Conflict_ErrFridgeEmpty()
    {
        // User 99 không có nguyên liệu nào trong tủ
        var act = () => _service.GetRecipeSuggestionsAsync(userId: 99, minSimilarity: 0.7, limit: 10, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(409);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrFridgeEmpty);
        ex.Which.Message.Should().Contain("Tủ lạnh ảo chưa có nguyên liệu.");
    }

    [Fact]
    public async Task GetRecipeSuggestions_ValidFridge_ReturnsMatchingRecipes_PrioritizingNonFresh()
    {
        // Arrange: User 10 có nguyên liệu trong tủ: Nấm rơm (12, NON-FRESH), Cà chua (13, FRESH)
        _db.FridgeInventories.AddRange(
            new FridgeInventory { UserId = 10, IngredientId = 12, CustomItemName = "Nấm rơm", FreshnessStatus = "NON-FRESH", Quantity = 500, Unit = "g" },
            new FridgeInventory { UserId = 10, IngredientId = 13, CustomItemName = "Cà chua", FreshnessStatus = "FRESH", Quantity = 10, Unit = "quả" }
        );
        await _db.SaveChangesAsync();

        // Tạo 2 công thức:
        // Recipe 1: dùng Nấm rơm (12) và Cà chua (13) -> 2/2 = 100%, dùng Non-fresh
        // Recipe 2: dùng Cà chua (13) -> 1/1 = 100%, không dùng Non-fresh
        var r1 = new Recipe
        {
            RecipeId = 101,
            CategoryId = 1,
            DietarySchoolId = 1,
            Title = "Canh nấm cà chua",
            Instructions = "Nấu canh",
            DifficultyLevel = "EASY",
            Status = "APPROVED_ACTIVE",
            Source = "OFFICIAL",
            CreatedAt = DateTime.UtcNow
        };
        var r2 = new Recipe
        {
            RecipeId = 102,
            CategoryId = 1,
            DietarySchoolId = 1,
            Title = "Salad cà chua",
            Instructions = "Trộn salad",
            DifficultyLevel = "EASY",
            Status = "APPROVED_ACTIVE",
            Source = "OFFICIAL",
            CreatedAt = DateTime.UtcNow
        };
        _db.Recipes.AddRange(r1, r2);
        await _db.SaveChangesAsync();

        _db.RecipeIngredients.AddRange(
            new RecipeIngredient { RecipeId = 101, IngredientId = 12, QuantityNeeded = 100, Unit = "g", IsOptional = false },
            new RecipeIngredient { RecipeId = 101, IngredientId = 13, QuantityNeeded = 2, Unit = "quả", IsOptional = false },
            new RecipeIngredient { RecipeId = 102, IngredientId = 13, QuantityNeeded = 3, Unit = "quả", IsOptional = false }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetRecipeSuggestionsAsync(userId: 10, minSimilarity: 0.7, limit: 10, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(2);

        // Recipe 101 ưu tiên xếp trước vì dùng Non-fresh
        result[0].RecipeId.Should().Be(101);
        result[0].UsesNonFreshItems.Should().BeTrue();
        result[0].Similarity.Should().Be(1.0);

        result[1].RecipeId.Should().Be(102);
        result[1].UsesNonFreshItems.Should().BeFalse();
        result[1].Similarity.Should().Be(1.0);
    }

    [Fact]
    public async Task GetRecipeSuggestions_RecipeWithAllergen_IsExcludedFromSuggestions()
    {
        // Arrange
        // Seed Allergen 1 (SOY) và gán cho ingredient 14 (Đậu hũ non)
        var allergen = new Allergen { AllergenId = 1, Code = "SOY", AllergenName = "Đậu nành" };
        _db.Allergens.Add(allergen);
        _db.IngredientAllergens.Add(new IngredientAllergen { IngredientId = 14, AllergenId = 1 });

        // User 20 bị dị ứng Đậu nành
        _db.UserAllergies.Add(new UserAllergy { UserId = 20, AllergenId = 1, CreatedAt = DateTime.UtcNow });

        // User 20 có nguyên liệu trong tủ: Nấm rơm (12) và Đậu hũ non (14)
        _db.FridgeInventories.AddRange(
            new FridgeInventory { UserId = 20, IngredientId = 12, CustomItemName = "Nấm rơm", FreshnessStatus = "FRESH", Quantity = 500, Unit = "g" },
            new FridgeInventory { UserId = 20, IngredientId = 14, CustomItemName = "Đậu hũ non", FreshnessStatus = "FRESH", Quantity = 2, Unit = "miếng" }
        );

        // Recipe 103: Canh nấm (dùng 12) -> Hợp lệ
        // Recipe 104: Đậu hũ sốt (dùng 14) -> Bị loại do dị ứng Đậu nành
        var r1 = new Recipe
        {
            RecipeId = 103,
            CategoryId = 1,
            Title = "Canh nấm an toàn",
            Instructions = "B1",
            DifficultyLevel = "EASY",
            Status = "APPROVED_ACTIVE",
            Source = "OFFICIAL",
            CreatedAt = DateTime.UtcNow
        };
        var r2 = new Recipe
        {
            RecipeId = 104,
            CategoryId = 1,
            Title = "Đậu hũ sốt chua cay",
            Instructions = "B1",
            DifficultyLevel = "EASY",
            Status = "APPROVED_ACTIVE",
            Source = "OFFICIAL",
            CreatedAt = DateTime.UtcNow
        };
        _db.Recipes.AddRange(r1, r2);
        await _db.SaveChangesAsync();

        _db.RecipeIngredients.AddRange(
            new RecipeIngredient { RecipeId = 103, IngredientId = 12, QuantityNeeded = 100, Unit = "g", IsOptional = false },
            new RecipeIngredient { RecipeId = 104, IngredientId = 14, QuantityNeeded = 1, Unit = "miếng", IsOptional = false }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetRecipeSuggestionsAsync(userId: 20, minSimilarity: 0.7, limit: 10, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainSingle();
        result[0].RecipeId.Should().Be(103);
    }

    [Fact]
    public async Task GetRecipeSuggestions_RecipeWithForbiddenFoodGroup_IsExcludedFromSuggestions()
    {
        // Arrange
        // Ingredient 90: Sữa bò (DAIRY)
        var milk = new Ingredient
        {
            IngredientId = 90,
            IngredientName = "Sữa bò",
            FoodGroup = "DAIRY",
            StandardUnit = "ml",
            DefaultStallType = "DAIRY",
            CaloriesPer100g = 60m
        };
        _db.Ingredients.Add(milk);

        // User 10 thuộc DietarySchool 1 (Thuần chay - cấm DAIRY)
        // Cho sữa vào tủ của User 10
        _db.FridgeInventories.Add(
            new FridgeInventory { UserId = 10, IngredientId = 90, CustomItemName = "Sữa bò", FreshnessStatus = "FRESH" }
        );

        var r = new Recipe
        {
            RecipeId = 105,
            CategoryId = 1,
            Title = "Súp sữa chay",
            Instructions = "B1",
            DifficultyLevel = "EASY",
            Status = "APPROVED_ACTIVE",
            Source = "OFFICIAL",
            CreatedAt = DateTime.UtcNow
        };
        _db.Recipes.Add(r);
        await _db.SaveChangesAsync();

        _db.RecipeIngredients.Add(
            new RecipeIngredient { RecipeId = 105, IngredientId = 90, QuantityNeeded = 200, Unit = "ml", IsOptional = false }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetRecipeSuggestionsAsync(userId: 10, minSimilarity: 0.7, limit: 10, CancellationToken.None);

        // Assert
        result.Should().NotContain(s => s.RecipeId == 105);
    }

    [Fact]
    public async Task GetRecipeSuggestions_RecipeWithAllium_ExcludedWhenUserRequiresAlliumFree()
    {
        // Arrange
        // User 10 có RequiresAlliumFree = true trong HealthProfile
        // Tủ của user có Hành tây (15, IsAllium = true)
        _db.FridgeInventories.Add(
            new FridgeInventory { UserId = 10, IngredientId = 15, CustomItemName = "Hành tây", FreshnessStatus = "FRESH" }
        );

        var r = new Recipe
        {
            RecipeId = 106,
            CategoryId = 1,
            Title = "Hành xào tương",
            Instructions = "B1",
            DifficultyLevel = "EASY",
            Status = "APPROVED_ACTIVE",
            Source = "OFFICIAL",
            CreatedAt = DateTime.UtcNow
        };
        _db.Recipes.Add(r);
        await _db.SaveChangesAsync();

        _db.RecipeIngredients.Add(
            new RecipeIngredient { RecipeId = 106, IngredientId = 15, QuantityNeeded = 1, Unit = "củ", IsOptional = false }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetRecipeSuggestionsAsync(userId: 10, minSimilarity: 0.7, limit: 10, CancellationToken.None);

        // Assert: Không được gợi ý công thức có ngũ vị tân
        result.Should().NotContain(s => s.RecipeId == 106);
    }

    [Fact]
    public async Task GetRecipeSuggestions_SimilarityBelowThreshold_IsExcludedFromSuggestions()
    {
        // Arrange: User 30 chỉ có 1 nguyên liệu (Nấm rơm 12)
        _db.FridgeInventories.Add(
            new FridgeInventory { UserId = 30, IngredientId = 12, CustomItemName = "Nấm rơm", FreshnessStatus = "FRESH" }
        );

        // Recipe 107 cần 4 nguyên liệu bắt buộc (12, 13, 14, 15). User chỉ có 12 -> 1/4 = 25% < 70% minSimilarity
        var r = new Recipe
        {
            RecipeId = 107,
            CategoryId = 1,
            Title = "Lẩu nấm thập cẩm 4 món",
            Instructions = "B1",
            DifficultyLevel = "HARD",
            Status = "APPROVED_ACTIVE",
            Source = "OFFICIAL",
            CreatedAt = DateTime.UtcNow
        };
        _db.Recipes.Add(r);
        await _db.SaveChangesAsync();

        _db.RecipeIngredients.AddRange(
            new RecipeIngredient { RecipeId = 107, IngredientId = 12, QuantityNeeded = 100, Unit = "g", IsOptional = false },
            new RecipeIngredient { RecipeId = 107, IngredientId = 13, QuantityNeeded = 2, Unit = "quả", IsOptional = false },
            new RecipeIngredient { RecipeId = 107, IngredientId = 14, QuantityNeeded = 1, Unit = "miếng", IsOptional = false },
            new RecipeIngredient { RecipeId = 107, IngredientId = 15, QuantityNeeded = 1, Unit = "củ", IsOptional = false }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetRecipeSuggestionsAsync(userId: 30, minSimilarity: 0.7, limit: 10, CancellationToken.None);

        // Assert: Không được gợi ý vì similarity 0.25 < 0.7
        result.Should().NotContain(s => s.RecipeId == 107);
    }

    [Fact]
    public async Task DetectIngredients_WithBoundingBox_ShouldPassThroughCoordinates()
    {
        // Arrange
        using var stream = CreateValidPngStream(800, 600);
        var input = new FridgeDetectionInputDto
        {
            ImageStream = stream,
            FileName = "fridge.png",
            FileLength = stream.Length,
            ContentType = "image/png"
        };

        _mockAiClient.Setup(c => c.DetectFridgeIngredientsAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiFridgeDetectionResult
            {
                Success = true,
                Items = new List<AiDetectedItemDto>
                {
                    new()
                    {
                        Name = "Nấm rơm",
                        FreshnessStatus = "FRESH",
                        FoodGroup = "MUSHROOM",
                        Box2D = new List<int> { 145, 230, 420, 510 }
                    },
                    new()
                    {
                        Name = "Rau dại kỳ lạ",
                        FreshnessStatus = "NON-FRESH",
                        FoodGroup = "VEGETABLE",
                        Box2D = new List<int> { 510, 120, 780, 390 }
                    },
                    new()
                    {
                        Name = "Cà rốt không có tọa độ",
                        FreshnessStatus = "FRESH",
                        FoodGroup = "VEGETABLE",
                        Box2D = null
                    }
                }
            });

        // Act
        var result = await _service.DetectIngredientsAsync(input, userId: 1, userRole: "MEMBER", guestUuid: null);

        // Assert
        result.Should().NotBeNull();
        result.Detected.Should().HaveCount(3);

        // Mục 1: Khớp với Nấm rơm (IngredientId 12), toạ độ Box2D được giữ nguyên
        result.Detected[0].Name.Should().Be("Nấm rơm");
        result.Detected[0].IngredientId.Should().Be(12);
        result.Detected[0].Box2D.Should().NotBeNull();
        result.Detected[0].Box2D.Should().Equal(new List<int> { 145, 230, 420, 510 });

        // Mục 2: Không khớp danh mục, toạ độ Box2D vẫn được giữ nguyên để FE crop
        result.Detected[1].Name.Should().Be("Rau dại kỳ lạ");
        result.Detected[1].IngredientId.Should().NotBeNull();
        result.Detected[1].Box2D.Should().NotBeNull();
        result.Detected[1].Box2D.Should().Equal(new List<int> { 510, 120, 780, 390 });

        // Mục 3: Không có toạ độ từ AI -> Box2D null
        result.Detected[2].Name.Should().Be("Cà rốt không có tọa độ");
        result.Detected[2].Box2D.Should().BeNull();
    }

    [Fact]
    public async Task DetectIngredients_WithIsAnimalProductFlag_ShouldTriggerWarning()
    {
        // Arrange
        using var stream = CreateValidPngStream(800, 600);
        var input = new FridgeDetectionInputDto
        {
            ImageStream = stream,
            FileName = "fridge.png",
            FileLength = stream.Length,
            ContentType = "image/png"
        };

        _mockAiClient.Setup(c => c.DetectFridgeIngredientsAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiFridgeDetectionResult
            {
                Success = true,
                Items = new List<AiDetectedItemDto>
                {
                    new()
                    {
                        Name = "Thực phẩm có nguồn gốc động vật",
                        FreshnessStatus = "FRESH",
                        IsAnimalProduct = true,
                        IsRedMeat = false,
                        Box2D = new List<int> { 100, 100, 200, 200 }
                    }
                }
            });

        // Act
        var result = await _service.DetectIngredientsAsync(input, userId: 1, userRole: "MEMBER", guestUuid: null);

        // Assert
        result.Should().NotBeNull();
        result.Warnings.Should().NotBeEmpty();
        result.Warnings.Any(w => w.Contains("nguồn gốc động vật")).Should().BeTrue();
    }

    // =========================================================================
    // HÀM BỔ TRỢ TẠO LUỒNG ẢNH HỢP LỆ VỚI HEADER CHUẨN
    // =========================================================================

    private static MemoryStream CreateValidPngStream(int width, int height)
    {
        var ms = new MemoryStream();
        // PNG Signature (8 bytes)
        ms.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        // Chunk Length (4 bytes big-endian: 13 bytes)
        ms.Write(new byte[] { 0x00, 0x00, 0x00, 0x0D });
        // Chunk Type: "IHDR"
        ms.Write(Encoding.ASCII.GetBytes("IHDR"));
        // Width (4 bytes big-endian)
        ms.WriteByte((byte)((width >> 24) & 0xFF));
        ms.WriteByte((byte)((width >> 16) & 0xFF));
        ms.WriteByte((byte)((width >> 8) & 0xFF));
        ms.WriteByte((byte)(width & 0xFF));
        // Height (4 bytes big-endian)
        ms.WriteByte((byte)((height >> 24) & 0xFF));
        ms.WriteByte((byte)((height >> 16) & 0xFF));
        ms.WriteByte((byte)((height >> 8) & 0xFF));
        ms.WriteByte((byte)(height & 0xFF));
        // Bit depth (1 byte), Color type (1 byte), Compression (1 byte), Filter (1 byte), Interlace (1 byte)
        ms.Write(new byte[] { 0x08, 0x06, 0x00, 0x00, 0x00 });
        ms.Position = 0;
        return ms;
    }
}
