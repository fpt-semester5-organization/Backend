using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.ShoppingLists.Requests;
using VeggieLife.BLL.Services.ShoppingLists;
using VeggieLife.DAL.Context;
using Xunit;

namespace VeggieLife.Tests.Unit.Security;

/// <summary>
/// BỘ KIỂM THỬ BẢO MẬT & GIỚI HẠN DỮ LIỆU ĐẦU VÀO (SECURITY TESTING)
/// </summary>
public class SystemSecurityAndAuthTestSuite : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ShoppingListService _shoppingListService;

    public SystemSecurityAndAuthTestSuite()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("SecurityTestDb_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _shoppingListService = new ShoppingListService(_db);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.5)]
    [InlineData(11.0)]
    [InlineData(999.0)]
    public async Task Security_RejectInvalidServingsMultiplier_BoundaryProtection(decimal invalidMultiplier)
    {
        // ARRANGE & ACT: Gửi hệ số nhân vượt ra ngoài ngưỡng cho phép (1.0 - 10.0)
        var request = new CreateMealPlanShoppingListRequest { ServingsMultiplier = invalidMultiplier };
        var act = () => _shoppingListService.CreateMealPlanShoppingListAsync(1, 100, request);

        // ASSERT: Hệ thống phải lập tức ném ngoại lệ 400 Validation, không để tính toán tràn số
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public void Security_StandardizedErrorCodes_MustContainRequiredSecurityConstants()
    {
        // ASSERT: Rà soát các mã lỗi bảo mật chuẩn toàn hệ thống
        ErrorCodes.ErrUnauthorized.Should().Be("ERR_UNAUTHORIZED");
        ErrorCodes.ErrForbidden.Should().Be("ERR_FORBIDDEN");
        ErrorCodes.ErrValidation.Should().Be("ERR_VALIDATION");
    }
}
