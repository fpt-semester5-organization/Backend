using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VeggieLife.API.Controllers;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.MasterData.Responses;
using VeggieLife.BLL.Services.MasterData;
using Xunit;

namespace VeggieLife.Tests.Unit.Controllers;

/// <summary>
/// Unit Tests kiểm thử các endpoints của MasterDataController (FN-PROF-08).
/// </summary>
public class MasterDataControllerTests
{
    private readonly Mock<IMasterDataService> _mockService;
    private readonly MasterDataController _controller;

    public MasterDataControllerTests()
    {
        _mockService = new Mock<IMasterDataService>();
        _controller = new MasterDataController(_mockService.Object);
    }

    [Fact]
    public async Task ListDietarySchools_ShouldReturnOkEnvelope()
    {
        var schools = new List<DietarySchoolDto>
        {
            new() { DietarySchoolId = 1, SchoolCode = "VEGAN", SchoolName = "Thuần chay" }
        };

        _mockService.Setup(s => s.GetDietarySchoolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(schools);

        var actionResult = await _controller.ListDietarySchools(CancellationToken.None);

        var okResult = actionResult as OkObjectResult;
        okResult.Should().NotBeNull();
        var envelope = okResult!.Value as ApiResponse<IReadOnlyList<DietarySchoolDto>>;
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(200);
        envelope.Message.Should().Be("OK");
        envelope.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task ListIngredients_ShouldReturnPagedResponseWithMeta()
    {
        var items = new List<IngredientDto>
        {
            new() { IngredientId = 1, Name = "Đậu phụ" }
        };
        var meta = new PagedMeta(1, 20, 1);

        _mockService.Setup(s => s.GetIngredientsAsync("đậu", 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, meta));

        var actionResult = await _controller.ListIngredients("đậu", 1, 20, CancellationToken.None);

        var okResult = actionResult as OkObjectResult;
        okResult.Should().NotBeNull();
        var envelope = okResult!.Value as ApiResponse<IReadOnlyList<IngredientDto>>;
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(200);
        envelope.Message.Should().Be("OK");
        envelope.Data.Should().HaveCount(1);
        envelope.Meta.Should().NotBeNull();
        envelope.Meta!.Total.Should().Be(1);
        envelope.Meta.Page.Should().Be(1);
        envelope.Meta.Size.Should().Be(20);
    }
}
