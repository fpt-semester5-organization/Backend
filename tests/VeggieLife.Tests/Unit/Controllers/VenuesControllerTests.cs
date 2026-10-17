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
using VeggieLife.BLL.DTOs.Venues.Requests;
using VeggieLife.BLL.DTOs.Venues.Responses;
using VeggieLife.BLL.Services.Venues;
using Xunit;

namespace VeggieLife.Tests.Unit.Controllers;

/// <summary>
/// Bộ kiểm thử đơn vị cho VenuesController (FN-MAP-01).
/// </summary>
public class VenuesControllerTests
{
    private readonly Mock<IVenueService> _mockVenueService;
    private readonly VenuesController _controller;

    public VenuesControllerTests()
    {
        _mockVenueService = new Mock<IVenueService>();
        _controller = new VenuesController(_mockVenueService.Object);
    }

    [Fact]
    public async Task ListNearbyVenues_AuthorizedMember_ShouldReturn200OkWithPagedResponse()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "101"),
            new(ClaimTypes.Role, UserRoles.Member)
        };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };

        var request = new ListNearbyVenuesRequest { Lat = 21.0285, Lng = 105.8542 };
        var sampleVenues = new List<NearbyVenueItemDto>
        {
            new()
            {
                VenueId = 301,
                Name = "Ưu Đàm Chay",
                Address = "34 Hàng Bài, Hà Nội",
                Latitude = 21.0227,
                Longitude = 105.8524,
                DistanceKm = 0.7,
                Rating = 4.8m,
                IsFullyVegan = true,
                DirectionsUrl = "https://www.google.com/maps/dir/?api=1&destination=21.0227,105.8524"
            }
        };
        var meta = new NearbyVenuesMeta(page: 1, size: 10, total: 1, radiusKm: 5.0);

        _mockVenueService.Setup(s => s.ListNearbyVenuesAsync(
                It.IsAny<ListNearbyVenuesRequest>(),
                101,
                UserRoles.Member,
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListNearbyVenuesResult { Venues = sampleVenues, Meta = meta });

        // Act
        var actionResult = await _controller.ListNearbyVenues(request, CancellationToken.None);

        // Assert
        var okResult = actionResult as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        var apiResponse = okResult.Value as ApiResponse<List<NearbyVenueItemDto>>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Message.Should().Be("Tìm thấy quán chay quanh bạn.");
        apiResponse.Data.Should().HaveCount(1);
        apiResponse.Data![0].Name.Should().Be("Ưu Đàm Chay");
        apiResponse.Data[0].Latitude.Should().Be(21.0227);
        apiResponse.Data[0].Longitude.Should().Be(105.8524);
        apiResponse.Data[0].Rating.Should().Be(4.8m);
        apiResponse.Data[0].IsFullyVegan.Should().BeTrue();
        apiResponse.Meta.Should().NotBeNull();
    }

    [Fact]
    public async Task ListNearbyVenues_GuestUser_ShouldReturn200OkWithQuotaInMeta()
    {
        // Arrange: Khách vãng lai với Anonymous JWT và Header X-Guest-UUID
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, UserRoles.Guest),
            new("Guest_UUID", "guest-uuid-12345")
        };
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
        };
        httpContext.Request.Headers["X-Guest-UUID"] = "guest-uuid-12345";

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var request = new ListNearbyVenuesRequest { Lat = 10.7729, Lng = 106.6983 };
        var sampleVenues = new List<NearbyVenueItemDto>
        {
            new()
            {
                VenueId = 311,
                Name = "Cơm Chay Tâm An",
                Address = "12 Lê Lợi, Quận 1, TP.HCM",
                DistanceKm = 0.1
            }
        };
        var quota = new GuestMapQuotaDto { Used = 1, Limit = 3, Remaining = 2 };
        var meta = new NearbyVenuesMeta(page: 1, size: 10, total: 1, radiusKm: 5.0, quota: quota);

        _mockVenueService.Setup(s => s.ListNearbyVenuesAsync(
                It.IsAny<ListNearbyVenuesRequest>(),
                null,
                UserRoles.Guest,
                "guest-uuid-12345",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListNearbyVenuesResult { Venues = sampleVenues, Meta = meta });

        // Act
        var actionResult = await _controller.ListNearbyVenues(request, CancellationToken.None);

        // Assert
        var okResult = actionResult as OkObjectResult;
        okResult.Should().NotBeNull();

        var apiResponse = okResult.Value as ApiResponse<List<NearbyVenueItemDto>>;
        apiResponse.Should().NotBeNull();
        var nearbyMeta = apiResponse!.Meta as NearbyVenuesMeta;
        nearbyMeta.Should().NotBeNull();
        nearbyMeta!.Quota.Should().NotBeNull();
        nearbyMeta.Quota!.Used.Should().Be(1);
        nearbyMeta.Quota.Remaining.Should().Be(2);
    }

    [Fact]
    public async Task ListNearbyVenues_Unauthenticated_ShouldThrowUnauthorized()
    {
        // Arrange: Người dùng chưa đăng nhập (không có token)
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var request = new ListNearbyVenuesRequest { Lat = 21.0285, Lng = 105.8542 };

        // Act & Assert
        var act = () => _controller.ListNearbyVenues(request, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(401);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrAuthInvalidToken);
    }

    [Fact]
    public async Task GetVenue_ValidId_Returns200OkWithVenueDetail()
    {
        // Arrange
        var venueId = 301;
        var detailDto = new VenueDetailDto
        {
            VenueId = venueId,
            Name = "Cơm Chay Tâm An",
            Address = "12 Lê Lợi, Quận 1, TP.HCM",
            Latitude = 10.7729,
            Longitude = 106.6983,
            Rating = 4.6m,
            IsFullyVegan = true,
            Categories = new List<string> { "Cơm chay" },
            DirectionsUrl = "https://www.google.com/maps/dir/?api=1&destination=10.7729,106.6983",
            Source = "HAPPYCOW"
        };

        _mockVenueService.Setup(s => s.GetVenueDetailAsync(venueId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detailDto);

        // Act
        var actionResult = await _controller.GetVenue(venueId, null, null, CancellationToken.None);

        // Assert
        var okResult = actionResult as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var apiResponse = okResult.Value as ApiResponse<VenueDetailDto>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Data.Should().NotBeNull();
        apiResponse.Data!.VenueId.Should().Be(venueId);
        apiResponse.Data.Name.Should().Be("Cơm Chay Tâm An");
        apiResponse.Data.Latitude.Should().Be(10.7729);
        apiResponse.Data.Longitude.Should().Be(106.6983);
        apiResponse.Data.Rating.Should().Be(4.6m);
        apiResponse.Data.IsFullyVegan.Should().BeTrue();
    }
}
