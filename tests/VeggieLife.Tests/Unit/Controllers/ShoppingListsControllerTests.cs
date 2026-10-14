using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VeggieLife.API.Controllers;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.ShoppingLists.Requests;
using VeggieLife.BLL.DTOs.ShoppingLists.Responses;
using VeggieLife.BLL.Services.ShoppingLists;
using Xunit;

namespace VeggieLife.Tests.Unit.Controllers;

/// <summary>
/// Bộ kiểm thử đơn vị cho ShoppingListsController (FN-MEAL-06).
/// </summary>
public class ShoppingListsControllerTests
{
    private readonly Mock<IShoppingListService> _mockService;
    private readonly ShoppingListsController _controller;

    public ShoppingListsControllerTests()
    {
        _mockService = new Mock<IShoppingListService>();
        _controller = new ShoppingListsController(_mockService.Object);
    }

    private void SetUserContext(int userId, string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetShoppingList_AuthorizedUser_ShouldReturn200Ok()
    {
        // ARRANGE
        var userId = 50;
        SetUserContext(userId, UserRoles.Member);

        var responseData = new GetShoppingListResponseData
        {
            ListId = 1,
            Status = "PENDING",
            Progress = new ShoppingListProgressDto { Purchased = 2, Total = 5 }
        };

        _mockService.Setup(s => s.GetShoppingListByIdAsync(1, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseData);

        // ACT
        var actionResult = await _controller.GetShoppingList(1, CancellationToken.None);

        // ASSERT
        var objectResult = actionResult.Should().BeAssignableTo<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var apiResponse = objectResult.Value as ApiResponse<GetShoppingListResponseData>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Data!.ListId.Should().Be(1);
        apiResponse.Data.Progress.Purchased.Should().Be(2);
    }

    [Fact]
    public async Task PatchShoppingListItem_AuthorizedUser_ShouldReturn200Ok()
    {
        // ARRANGE
        var userId = 51;
        SetUserContext(userId, UserRoles.Member);

        var request = new PatchShoppingListItemRequest { IsPurchased = true };
        var responseData = new PatchShoppingListItemResponseData
        {
            ItemId = 101,
            IsPurchased = true,
            ListStatus = "COMPLETED"
        };

        _mockService.Setup(s => s.PatchShoppingListItemAsync(1, 101, userId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseData);

        // ACT
        var actionResult = await _controller.PatchShoppingListItem(1, 101, request, CancellationToken.None);

        // ASSERT
        var objectResult = actionResult.Should().BeAssignableTo<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var apiResponse = objectResult.Value as ApiResponse<PatchShoppingListItemResponseData>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Data!.IsPurchased.Should().BeTrue();
        apiResponse.Data.ListStatus.Should().Be("COMPLETED");
    }

    [Fact]
    public async Task PatchShoppingList_AuthorizedUser_ShouldReturn200Ok()
    {
        // ARRANGE
        var userId = 52;
        SetUserContext(userId, UserRoles.Member);

        var request = new PatchShoppingListRequest { Status = "CANCELLED" };
        var responseData = new PatchShoppingListResponseData
        {
            ListId = 1,
            Status = "CANCELLED"
        };

        _mockService.Setup(s => s.PatchShoppingListAsync(1, userId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseData);

        // ACT
        var actionResult = await _controller.PatchShoppingList(1, request, CancellationToken.None);

        // ASSERT
        var objectResult = actionResult.Should().BeAssignableTo<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var apiResponse = objectResult.Value as ApiResponse<PatchShoppingListResponseData>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Data!.Status.Should().Be("CANCELLED");
    }

    [Fact]
    public async Task ShoppingLists_GuestRole_ShouldThrowForbidden()
    {
        // ARRANGE: Khách vãng lai gọi tính năng cá nhân hóa
        SetUserContext(999, UserRoles.Guest);

        // ACT & ASSERT: Lỗi 403 ERR_GUEST_PERSONALIZATION_RESTRICTED
        var act = () => _controller.GetShoppingList(1, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrGuestPersonalizationRestricted);
    }
}
