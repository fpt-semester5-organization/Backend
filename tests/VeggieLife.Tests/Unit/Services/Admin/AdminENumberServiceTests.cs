using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Admin.ENumbers;
using VeggieLife.BLL.Services.Admin;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Knowledge;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Admin;

/// <summary>
/// Bộ kiểm thử đơn vị cho AdminENumberService (FN-ADM-11, BR-ADM-MAS-002, BR-ADM-AI-005, BR-SYS-MAS-01).
/// </summary>
public class AdminENumberServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly Mock<IKnowledgeSyncQueue> _mockKnowledgeSyncQueue = new();
    private readonly AdminENumberService _service;
    private const int AdminId = 999;

    public AdminENumberServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("AdminENumberServiceTestsDb_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _service = new AdminENumberService(
            _db,
            _mockRedis.Object,
            _mockKnowledgeSyncQueue.Object,
            NullLogger<AdminENumberService>.Instance);

        SeedTestData();
    }

    private void SeedTestData()
    {
        _db.EnumberDictionaries.AddRange(
            new EnumberDictionary
            {
                Code = "E100",
                Name = "Curcumin (Nghệ)",
                CategoryType = "Chất tạo màu",
                OriginSource = "Củ nghệ tự nhiên",
                VeganStatus = "VEGAN",
                HealthRiskNotes = "Chống oxy hóa",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new EnumberDictionary
            {
                Code = "E120",
                Name = "Carmine",
                CategoryType = "Chất tạo màu",
                OriginSource = "Rệp son côn trùng",
                VeganStatus = "NON_VEGAN",
                HealthRiskNotes = "Chiết xuất côn trùng",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new EnumberDictionary
            {
                Code = "E471",
                Name = "Mono- and diglycerides of fatty acids",
                CategoryType = "Chất nhũ hóa",
                OriginSource = "Dầu thực vật hoặc mỡ động vật",
                VeganStatus = "DOUBTFUL",
                HealthRiskNotes = "Cần xác minh nguồn gốc nhà sản xuất",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );

        // Tạo 2 sản phẩm thử nghiệm việc tính lại VeganVerdict
        var unverifiedProduct = new Product
        {
            ProductId = 10,
            Barcode = "8931112223334",
            ProductName = "Bánh xốp nhũ hóa",
            VeganVerdict = "DOUBTFUL",
            AdminVerified = false,
            IngredientsText = "Bột mì, đường, E471, hương vani",
            CreatedAt = DateTime.UtcNow,
            LastSyncedAt = DateTime.UtcNow
        };
        _db.Products.Add(unverifiedProduct);

        var verifiedProduct = new Product
        {
            ProductId = 20,
            Barcode = "8935556667778",
            ProductName = "Sản phẩm đã thẩm định bởi Admin",
            VeganVerdict = "VEGAN", // Admin đã xác minh thủ công nhà sx dùng gốc thực vật
            AdminVerified = true,
            IngredientsText = "Bột gạo, E471 thực vật",
            CreatedAt = DateTime.UtcNow,
            LastSyncedAt = DateTime.UtcNow
        };
        _db.Products.Add(verifiedProduct);

        _db.ProductEnumbers.AddRange(
            new ProductEnumber { ProductId = 10, Code = "E471" },
            new ProductEnumber { ProductId = 20, Code = "E471" }
        );

        _db.SaveChanges();
    }

    [Fact]
    public async Task GetENumbersAsync_WithFilter_ShouldReturnCorrectPagedItems()
    {
        // Act: Lọc theo VeganStatus = VEGAN
        var (items, meta) = await _service.GetENumbersAsync(new AdminENumberFilterRequest
        {
            VeganStatus = "VEGAN",
            Page = 1,
            Size = 10
        });

        // Assert
        items.Should().HaveCount(1);
        items[0].Code.Should().Be("E100");
        meta.Total.Should().Be(1);

        // Act 2: Lọc theo từ khóa
        var (searchItems, searchMeta) = await _service.GetENumbersAsync(new AdminENumberFilterRequest
        {
            Keyword = "Carmine",
            Page = 1,
            Size = 10
        });
        searchItems.Should().HaveCount(1);
        searchItems[0].Code.Should().Be("E120");
    }

    [Fact]
    public async Task GetENumberByCodeAsync_ExistingCode_ShouldReturnDetails()
    {
        // Act
        var result = await _service.GetENumberByCodeAsync("e120");

        // Assert
        result.Code.Should().Be("E120");
        result.Name.Should().Be("Carmine");
        result.VeganStatus.Should().Be("NON_VEGAN");
    }

    [Fact]
    public async Task GetENumberByCodeAsync_NonExistentCode_ShouldThrowNotFound()
    {
        // Act & Assert
        var act = () => _service.GetENumberByCodeAsync("E999");
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrAdmNotFound);
    }

    [Fact]
    public async Task CreateENumberAsync_ValidRequest_ShouldInsertAndRecordAuditAndEnqueueKnowledgeSync()
    {
        // Arrange
        var request = new CreateAdminENumberRequest
        {
            Code = "E441",
            Name = "Gelatin",
            CategoryType = "Chất làm dày / tạo gel",
            OriginSource = "Xương và da động vật",
            VeganStatus = "NON_VEGAN",
            HealthRiskNotes = "Nguồn gốc động vật tuyệt đối không dùng cho người ăn chay"
        };

        // Act
        var result = await _service.CreateENumberAsync(request, AdminId);

        // Assert
        result.Code.Should().Be("E441");

        var entity = await _db.EnumberDictionaries.FindAsync("E441");
        entity.Should().NotBeNull();
        entity!.Name.Should().Be("Gelatin");

        // AuditLog
        var audit = await _db.AuditLogs.FirstOrDefaultAsync(a => a.Action == "CREATE_ENUMBER" && a.TargetId == "E441");
        audit.Should().NotBeNull();
        audit!.ActorUserId.Should().Be(AdminId);

        // Cache invalidated
        _mockRedis.Verify(r => r.RemoveAsync("enumber_cache", It.IsAny<CancellationToken>()), Times.Once);

        // Chatbot knowledge base synced
        _mockKnowledgeSyncQueue.Verify(q => q.EnqueueUpsertAsync("E_NUMBER", "E441", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateENumberAsync_DuplicateCode_ShouldThrowConflict()
    {
        // Arrange: E100 đã tồn tại trong DB
        var request = new CreateAdminENumberRequest
        {
            Code = "e100",
            Name = "Curcumin bản sao",
            CategoryType = "Màu",
            OriginSource = "Nghệ",
            VeganStatus = "VEGAN"
        };

        // Act & Assert
        var act = () => _service.CreateENumberAsync(request, AdminId);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(409);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrEnumberDuplicate);
    }

    [Fact]
    public async Task UpdateENumberAsync_ChangeVeganStatus_ShouldRecalculateUnverifiedProductsAndKeepVerified()
    {
        // Arrange: Đổi E471 từ DOUBTFUL sang NON_VEGAN
        var request = new UpdateAdminENumberRequest
        {
            VeganStatus = "NON_VEGAN",
            HealthRiskNotes = "Phát hiện lô sản xuất nguồn gốc mỡ heo"
        };

        // Act
        var result = await _service.UpdateENumberAsync("E471", request, AdminId);

        // Assert
        result.Code.Should().Be("E471");
        result.VeganStatus.Should().Be("NON_VEGAN");

        // Sản phẩm 10 (AdminVerified = false) phải được tự động tính lại thành NON_VEGAN
        var product10 = await _db.Products.FindAsync(10);
        product10!.VeganVerdict.Should().Be("NON_VEGAN");

        // Sản phẩm 20 (AdminVerified = true) được bảo lưu kết quả của Admin (BR-ADM-AI-005)
        var product20 = await _db.Products.FindAsync(20);
        product20!.VeganVerdict.Should().Be("VEGAN");

        // AuditLog
        var audit = await _db.AuditLogs.FirstOrDefaultAsync(a => a.Action == "UPDATE_ENUMBER" && a.TargetId == "E471");
        audit.Should().NotBeNull();

        // Chatbot knowledge synced
        _mockKnowledgeSyncQueue.Verify(q => q.EnqueueUpsertAsync("E_NUMBER", "E471", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateENumberAsync_NonExistentCode_ShouldThrowNotFound()
    {
        var request = new UpdateAdminENumberRequest
        {
            Name = "Tên mới"
        };

        var act = () => _service.UpdateENumberAsync("E999", request, AdminId);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrAdmNotFound);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
