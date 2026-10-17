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
using VeggieLife.BLL.DTOs.Venues.Requests;
using VeggieLife.BLL.DTOs.Venues.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Venues;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Venues;

/// <summary>
/// Bộ kiểm thử đơn vị toàn diện cho VenueService (FN-MAP-01).
/// Kiểm tra:
/// 1. Tìm thử ở Hà Nội và TP.HCM cho kết quả đúng thứ tự khoảng cách.
/// 2. Khách: bán kính 5 km, tối đa 03 lần dùng thử (BR-GUEST-03).
/// 3. Thành viên: bán kính 1..10 km, không giới hạn lượt (BR-USR-11).
/// 4. Giới hạn lãnh thổ Việt Nam (BR-GUEST-06).
/// 5. Tìm theo tỉnh/thành khi từ chối GPS, lọc theo danh mục, phân trang.
/// 6. Cơ chế Cache Geohash trên Redis 24 giờ (BR-SYS-CACHE-01).
/// </summary>
public class VenueServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IRedisService> _mockRedis;
    private readonly Mock<ILogger<VenueService>> _mockLogger;
    private readonly VenueService _service;

    public VenueServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _mockRedis = new Mock<IRedisService>();
        _mockLogger = new Mock<ILogger<VenueService>>();

        _service = new VenueService(_db, _mockRedis.Object, _mockLogger.Object);

        SeedVenuesData();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private void SeedVenuesData()
    {
        var catRice = new FoodCategory { CategoryId = 1, CategoryName = "Món Cơm & Xôi Chay", IsCore = true, IsActive = true, CreatedAt = DateTime.UtcNow };
        var catHotpot = new FoodCategory { CategoryId = 9, CategoryName = "Món Lẩu Chay Gia Đình", IsCore = true, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.FoodCategories.AddRange(catRice, catHotpot);

        // Quán tại Hà Nội
        // 1. Ưu Đàm: 21.0227, 105.8524 (Cách Hồ Gươm ~0.7 km)
        var uDam = new VeganVenue
        {
            VenueId = 301,
            ExternalSource = "GOONG",
            ExternalPlaceId = "GOONG_HN_01",
            VenueName = "Ưu Đàm Chay",
            Address = "34 Hàng Bài, Hoàn Kiếm, Hà Nội",
            Latitude = 21.022700m,
            Longitude = 105.852400m,
            Rating = 4.8m,
            ThumbnailUrl = "https://res.cloudinary.com/veggielife/venues/uudam.webp",
            IsFullyVegan = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // 2. Nàng Tấm: 21.0235, 105.8450 (Cách Hồ Gươm ~1.1 km)
        var nangTam = new VeganVenue
        {
            VenueId = 302,
            ExternalSource = "GOONG",
            ExternalPlaceId = "GOONG_HN_02",
            VenueName = "Cơm Chay Nàng Tấm",
            Address = "79A Trần Hưng Đạo, Hoàn Kiếm, Hà Nội",
            Latitude = 21.023500m,
            Longitude = 105.845000m,
            Rating = 4.5m,
            ThumbnailUrl = "https://res.cloudinary.com/veggielife/venues/nangtam.webp",
            IsFullyVegan = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // 3. Vị Lai: 21.0245, 105.8480 (Cách Hồ Gươm ~0.8 km)
        var viLai = new VeganVenue
        {
            VenueId = 303,
            ExternalSource = "GOONG",
            ExternalPlaceId = "GOONG_HN_03",
            VenueName = "Nhà Hàng Chay Vị Lai",
            Address = "67 Lý Thường Kiệt, Hoàn Kiếm, Hà Nội",
            Latitude = 21.024500m,
            Longitude = 105.848000m,
            Rating = 4.9m,
            ThumbnailUrl = "https://res.cloudinary.com/veggielife/venues/vilai.webp",
            IsFullyVegan = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // 4. Tịnh Thực Quán: 21.0189, 105.8115 (Cách Hồ Gươm ~4.6 km)
        var tinhThuc = new VeganVenue
        {
            VenueId = 304,
            ExternalSource = "GOONG",
            ExternalPlaceId = "GOONG_HN_04",
            VenueName = "Tịnh Thực Quán",
            Address = "10 Huỳnh Thúc Kháng, Đống Đa, Hà Nội",
            Latitude = 21.018900m,
            Longitude = 105.811500m,
            Rating = 4.6m,
            ThumbnailUrl = "https://res.cloudinary.com/veggielife/venues/tinhthuc.webp",
            IsFullyVegan = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // 5. Quán Dưỡng Sinh Cũ (Rating 2.1 < 2.5): 21.0250, 105.8350 (Cách Hồ Gươm ~2.1 km)
        var duongSinhHn = new VeganVenue
        {
            VenueId = 305,
            ExternalSource = "GOONG",
            ExternalPlaceId = "GOONG_HN_08",
            VenueName = "Quán Chay Dưỡng Sinh Cũ",
            Address = "Ngõ 40 Tôn Đức Thắng, Đống Đa, Hà Nội",
            Latitude = 21.025000m,
            Longitude = 105.835000m,
            Rating = 2.1m,
            ThumbnailUrl = "https://res.cloudinary.com/veggielife/venues/duongsinh.webp",
            IsFullyVegan = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Quán tại TP.HCM
        // 6. Cơm Chay Tâm An: 10.7729, 106.6983 (Cách Phố Đi Bộ Nguyễn Huệ ~0.6 km)
        var tamAnSg = new VeganVenue
        {
            VenueId = 311,
            ExternalSource = "GOONG",
            ExternalPlaceId = "GOONG_SG_01",
            VenueName = "Cơm Chay Tâm An",
            Address = "12 Lê Lợi, Bến Nghé, Quận 1, TP.HCM",
            Latitude = 10.772900m,
            Longitude = 106.698300m,
            Rating = 4.6m,
            ThumbnailUrl = "https://res.cloudinary.com/veggielife/venues/taman.webp",
            IsFullyVegan = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // 7. Tib Chay: 10.7715, 106.7022 (Cách Phố Đi Bộ Nguyễn Huệ ~0.3 km)
        var tibChaySg = new VeganVenue
        {
            VenueId = 312,
            ExternalSource = "GOONG",
            ExternalPlaceId = "GOONG_SG_02",
            VenueName = "Tib Chay Huỳnh Thúc Kháng",
            Address = "11 Huỳnh Thúc Kháng, Bến Nghé, Quận 1, TP.HCM",
            Latitude = 10.771500m,
            Longitude = 106.702200m,
            Rating = 4.8m,
            ThumbnailUrl = "https://res.cloudinary.com/veggielife/venues/tibchay.webp",
            IsFullyVegan = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // 8. Hum Vegetarian: 10.7781, 106.6912 (Cách Phố Đi Bộ Nguyễn Huệ ~1.4 km)
        var humSg = new VeganVenue
        {
            VenueId = 313,
            ExternalSource = "GOONG",
            ExternalPlaceId = "GOONG_SG_03",
            VenueName = "Hum Vegetarian Lounge & Restaurant",
            Address = "32 Võ Văn Tần, Phường 6, Quận 3, TP.HCM",
            Latitude = 10.778100m,
            Longitude = 106.691200m,
            Rating = 4.9m,
            ThumbnailUrl = "https://res.cloudinary.com/veggielife/venues/hum.webp",
            IsFullyVegan = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // 9. Quán Vỉa Hè Cũ (Rating 2.2 < 2.5): 10.7850, 106.6990
        var viaHeSg = new VeganVenue
        {
            VenueId = 314,
            ExternalSource = "GOONG",
            ExternalPlaceId = "GOONG_SG_08",
            VenueName = "Quán Chay Vỉa Hè Cũ",
            Address = "Hẻm 18A Nguyễn Thị Minh Khai, Đa Kao, Quận 1, TP.HCM",
            Latitude = 10.785000m,
            Longitude = 106.699000m,
            Rating = 2.2m,
            ThumbnailUrl = "https://res.cloudinary.com/veggielife/venues/viahe.webp",
            IsFullyVegan = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.VeganVenues.AddRange(uDam, nangTam, viLai, tinhThuc, duongSinhHn, tamAnSg, tibChaySg, humSg, viaHeSg);

        // Gán danh mục món ăn
        var allVenues = new[] { uDam, nangTam, viLai, tinhThuc, duongSinhHn, tamAnSg, tibChaySg, humSg, viaHeSg };
        foreach (var v in allVenues)
        {
            _db.VenueCategories.Add(new VenueCategory { VenueId = v.VenueId, CategoryId = 1, IsPrimary = true, AssignedAt = DateTime.UtcNow });
        }
        // Thêm danh mục lẩu cho Vị Lai và Hum
        _db.VenueCategories.Add(new VenueCategory { VenueId = viLai.VenueId, CategoryId = 9, IsPrimary = false, AssignedAt = DateTime.UtcNow });
        _db.VenueCategories.Add(new VenueCategory { VenueId = humSg.VenueId, CategoryId = 9, IsPrimary = false, AssignedAt = DateTime.UtcNow });

        _db.SaveChanges();
    }

    [Fact]
    public async Task ListNearbyVenues_SearchHanoiCoordinates_ShouldReturnHanoiVenuesOrderedByDistanceAscending()
    {
        // ARRANGE: Tọa độ Hồ Gươm (21.0285, 105.8542)
        var request = new ListNearbyVenuesRequest
        {
            Lat = 21.0285,
            Lng = 105.8542,
            RadiusKm = 5.0
        };

        // ACT: Thành viên tìm kiếm
        var result = await _service.ListNearbyVenuesAsync(request, userId: 1, userRole: UserRoles.Member, guestUuid: null);

        // ASSERT: Kết quả trả về các quán ở Hà Nội, được sắp xếp theo khoảng cách tăng dần
        result.Should().NotBeNull();
        result.Venues.Should().NotBeEmpty();

        // 1. Quán gần nhất là Ưu Đàm (~0.7 km), tiếp theo là Vị Lai (~0.8 km), Nàng Tấm (~1.1 km), Tịnh Thực (~4.6 km)
        var venues = result.Venues;
        for (int i = 0; i < venues.Count - 1; i++)
        {
            venues[i].DistanceKm.Should().BeLessThanOrEqualTo(venues[i + 1].DistanceKm);
        }

        // 2. Không được chứa quán ở TP.HCM
        venues.Should().NotContain(v => v.Address.Contains("TP.HCM"));

        // 3. Không lọc theo rating (chỉ dùng Goong Places) nên Quán Dưỡng Sinh Cũ (2.1 sao) vẫn có mặt
        venues.Should().Contain(v => v.VenueId == 305);

        // 4. Kiểm tra meta và thông tin tọa độ / rating của quán
        venues[0].Latitude.Should().BeGreaterThan(0);
        venues[0].Longitude.Should().BeGreaterThan(0);
        venues[0].Rating.Should().BeGreaterThan(0);
        result.Meta.RadiusKm.Should().Be(5.0);
        result.Meta.Quota.Should().BeNull(); // Thành viên không có quota
    }

    [Fact]
    public async Task ListNearbyVenues_SearchHcmcCoordinates_ShouldReturnHcmcVenuesOrderedByDistanceAscending()
    {
        // ARRANGE: Tọa độ Nguyễn Huệ Quận 1 (10.7745, 106.7035)
        var request = new ListNearbyVenuesRequest
        {
            Lat = 10.7745,
            Lng = 106.7035,
            RadiusKm = 5.0
        };

        // ACT: Thành viên tìm kiếm
        var result = await _service.ListNearbyVenuesAsync(request, userId: 2, userRole: UserRoles.Vip, guestUuid: null);

        // ASSERT: Kết quả trả về các quán ở TP.HCM theo đúng thứ tự khoảng cách
        result.Should().NotBeNull();
        result.Venues.Should().NotBeEmpty();

        var venues = result.Venues;
        for (int i = 0; i < venues.Count - 1; i++)
        {
            venues[i].DistanceKm.Should().BeLessThanOrEqualTo(venues[i + 1].DistanceKm);
        }

        // Không chứa quán Hà Nội
        venues.Should().NotContain(v => v.Address.Contains("Hà Nội"));

        // Tib Chay (~0.3 km) đứng trước Tâm An (~0.6 km), Tâm An đứng trước Vỉa Hè Cũ (~1.3 km), Vỉa Hè Cũ đứng trước Hum (~1.5 km)
        venues[0].Name.Should().Be("Tib Chay Huỳnh Thúc Kháng");
        venues[1].Name.Should().Be("Cơm Chay Tâm An");
        venues[2].Name.Should().Be("Quán Chay Vỉa Hè Cũ");
        venues[3].Name.Should().Be("Hum Vegetarian Lounge & Restaurant");
    }

    [Fact]
    public async Task ListNearbyVenues_MemberAndGuest_ReturnsAllVenuesInRadius()
    {
        // ARRANGE: Hà Nội
        var request = new ListNearbyVenuesRequest
        {
            Lat = 21.0285,
            Lng = 105.8542,
            RadiusKm = 5.0
        };

        // ACT: Thành viên (MEMBER)
        var memberResult = await _service.ListNearbyVenuesAsync(request, userId: 1, userRole: UserRoles.Member, guestUuid: null);

        // ACT: Khách (GUEST)
        var guestResult = await _service.ListNearbyVenuesAsync(request, userId: null, userRole: UserRoles.Guest, guestUuid: "guest-test-1");

        // ASSERT: Cả Thành viên và Khách đều nhận được quán trong bán kính
        memberResult.Venues.Should().Contain(v => v.VenueId == 305);
        guestResult.Venues.Should().Contain(v => v.VenueId == 305);
    }

    [Fact]
    public async Task ListNearbyVenues_Guest_RadiusGreaterThanFive_ShouldThrowBadRequest()
    {
        // ARRANGE: Khách cố tình yêu cầu bán kính 10 km
        var request = new ListNearbyVenuesRequest
        {
            Lat = 21.0285,
            Lng = 105.8542,
            RadiusKm = 10.0
        };

        // ACT & ASSERT: Bị từ chối với 400 ERR_VALIDATION
        var act = () => _service.ListNearbyVenuesAsync(request, userId: null, userRole: UserRoles.Guest, guestUuid: "guest-test-radius");
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
        ex.Which.Message.Should().Contain("5 km");
    }

    [Fact]
    public async Task ListNearbyVenues_Member_RadiusBetweenOneAndTen_ShouldBeAllowed()
    {
        // ARRANGE: Thành viên yêu cầu bán kính 10 km
        var request = new ListNearbyVenuesRequest
        {
            Lat = 21.0285,
            Lng = 105.8542,
            RadiusKm = 10.0
        };

        // ACT
        var result = await _service.ListNearbyVenuesAsync(request, userId: 10, userRole: UserRoles.Member, guestUuid: null);

        // ASSERT: Thành công với bán kính 10 km
        result.Should().NotBeNull();
        result.Meta.RadiusKm.Should().Be(10.0);
    }

    [Fact]
    public async Task ListNearbyVenues_Guest_ExceedThreeSearches_ShouldThrowForbiddenErrQuotaMapExceeded()
    {
        // ARRANGE: Khách đã tìm đủ 3 lần trong Redis
        var guestId = "guest-exceeded-quota";
        _mockRedis.Setup(r => r.GetAsync<int?>($"guest_quota:{guestId}:map", It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var request = new ListNearbyVenuesRequest
        {
            Lat = 21.0285,
            Lng = 105.8542,
            RadiusKm = 5.0
        };

        // ACT & ASSERT: Lần thứ 4 bị chặn với HTTP 403 ERR_QUOTA_MAP_EXCEEDED (BR-GUEST-03)
        var act = () => _service.ListNearbyVenuesAsync(request, userId: null, userRole: UserRoles.Guest, guestUuid: guestId);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrQuotaMapExceeded);
        ex.Which.Message.Should().Contain("03 lần");
    }

    [Fact]
    public async Task ListNearbyVenues_Guest_SuccessfulSearch_ShouldIncrementQuotaAndReturnQuotaMeta()
    {
        // ARRANGE: Khách chưa dùng lượt nào (quota = 0)
        var guestId = "guest-new-user";
        _mockRedis.Setup(r => r.GetAsync<int?>($"guest_quota:{guestId}:map", It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mockRedis.Setup(r => r.IncrementAsync($"guest_quota:{guestId}:map", 1, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var request = new ListNearbyVenuesRequest
        {
            Lat = 21.0285,
            Lng = 105.8542,
            RadiusKm = 5.0
        };

        // ACT
        var result = await _service.ListNearbyVenuesAsync(request, userId: null, userRole: UserRoles.Guest, guestUuid: guestId);

        // ASSERT: Trừ 1 lượt dùng thử
        result.Should().NotBeNull();
        result.Meta.Quota.Should().NotBeNull();
        result.Meta.Quota!.Used.Should().Be(1);
        result.Meta.Quota.Limit.Should().Be(3);
        result.Meta.Quota.Remaining.Should().Be(2);

        _mockRedis.Verify(r => r.IncrementAsync($"guest_quota:{guestId}:map", 1, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListNearbyVenues_OutsideVietnamCoordinates_ShouldThrowForbiddenErrGuestRegionUnsupported()
    {
        // ARRANGE: Tọa độ New York (40.7128, -74.0060) nằm ngoài lãnh thổ Việt Nam
        var request = new ListNearbyVenuesRequest
        {
            Lat = 40.7128,
            Lng = -74.0060,
            RadiusKm = 5.0
        };

        // ACT & ASSERT: Bị từ chối với 403 ERR_GUEST_REGION_UNSUPPORTED (BR-GUEST-06)
        var act = () => _service.ListNearbyVenuesAsync(request, userId: 1, userRole: UserRoles.Member, guestUuid: null);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrGuestRegionUnsupported);
        ex.Which.Message.Should().Contain("Việt Nam");
    }

    [Fact]
    public async Task ListNearbyVenues_MissingCoordinatesAndProvince_ShouldThrowBadRequest()
    {
        // ARRANGE: Không truyền tọa độ cũng không truyền tỉnh/thành
        var request = new ListNearbyVenuesRequest
        {
            Lat = null,
            Lng = null,
            Province = null
        };

        // ACT & ASSERT: 400 ERR_VALIDATION
        var act = () => _service.ListNearbyVenuesAsync(request, userId: 1, userRole: UserRoles.Member, guestUuid: null);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task ListNearbyVenues_FilterByCategoryId_ShouldReturnOnlyVenuesInThatCategory()
    {
        // ARRANGE: Lọc theo CategoryId = 9 (Món Lẩu Chay)
        var request = new ListNearbyVenuesRequest
        {
            Lat = 21.0285,
            Lng = 105.8542,
            RadiusKm = 5.0,
            CategoryId = 9
        };

        // ACT
        var result = await _service.ListNearbyVenuesAsync(request, userId: 1, userRole: UserRoles.Member, guestUuid: null);

        // ASSERT: Chỉ có Nhà hàng chay Vị Lai có danh mục Lẩu Chay trong bán kính 5 km Hà Nội
        result.Venues.Should().NotBeEmpty();
        result.Venues.Should().Contain(v => v.Name == "Nhà Hàng Chay Vị Lai");
        result.Venues.Should().NotContain(v => v.Name == "Ưu Đàm Chay"); // Ưu Đàm chỉ gán Category 1 trong mock
    }

    [Fact]
    public async Task ListNearbyVenues_SearchByProvinceWhenGpsDenied_ShouldReturnVenuesInProvince()
    {
        // ARRANGE: Không có tọa độ GPS, tìm theo tỉnh/thành = "Hà Nội"
        var request = new ListNearbyVenuesRequest
        {
            Province = "Hà Nội"
        };

        // ACT
        var result = await _service.ListNearbyVenuesAsync(request, userId: 1, userRole: UserRoles.Member, guestUuid: null);

        // ASSERT: Trả về danh sách quán có địa chỉ ở Hà Nội
        result.Venues.Should().NotBeEmpty();
        result.Venues.Should().OnlyContain(v => v.Address.Contains("Hà Nội"));
    }

    [Fact]
    public async Task ListNearbyVenues_WhenGoongConfiguredAndSearchingInDaNang_ShouldFetchAndSaveFromGoong()
    {
        // ARRANGE: Người dùng ở Đà Nẵng (vị trí ban đầu chưa có trong seed data CSDL)
        var userLat = 16.0678;
        var userLng = 108.2208;

        var mockGoong = new Mock<VeggieLife.DAL.External.Goong.IGoongPlacesClient>();
        mockGoong.Setup(g => g.IsConfigured).Returns(true);
        mockGoong.Setup(g => g.SearchNearbyPlacesAsync(
                It.IsAny<string>(),
                userLat,
                userLng,
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VeggieLife.DAL.External.Goong.GoongPlacePredictionDto>
            {
                new()
                {
                    PlaceId = "GOONG_DN_01",
                    Description = "Quán Cơm Chay An Lạc, 123 Nguyễn Văn Linh, Đà Nẵng",
                    StructuredFormatting = new VeggieLife.DAL.External.Goong.GoongStructuredFormatting
                    {
                        MainText = "Cơm Chay An Lạc"
                    }
                }
            });

        mockGoong.Setup(g => g.GetPlaceDetailAsync("GOONG_DN_01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VeggieLife.DAL.External.Goong.GoongPlaceDetailDto
            {
                PlaceId = "GOONG_DN_01",
                Name = "Cơm Chay An Lạc",
                FormattedAddress = "123 Nguyễn Văn Linh, Hải Châu, Đà Nẵng",
                Geometry = new VeggieLife.DAL.External.Goong.GoongGeometry
                {
                    Location = new VeggieLife.DAL.External.Goong.GoongLocation
                    {
                        Lat = 16.0680,
                        Lng = 108.2210
                    }
                }
            });

        var serviceWithGoong = new VenueService(_db, _mockRedis.Object, _mockLogger.Object, mockGoong.Object);

        var request = new ListNearbyVenuesRequest
        {
            Lat = userLat,
            Lng = userLng,
            RadiusKm = 5.0
        };

        // ACT
        var result = await serviceWithGoong.ListNearbyVenuesAsync(request, userId: 1, userRole: UserRoles.Member, guestUuid: null);

        // ASSERT: Quán từ Goong được trả về và lưu vào CSDL
        result.Venues.Should().NotBeEmpty();
        result.Venues.Should().Contain(v => v.Name == "Cơm Chay An Lạc");
        result.Venues[0].DistanceKm.Should().BeLessThan(1.0);

        var savedDbItem = await _db.VeganVenues.FirstOrDefaultAsync(v => v.ExternalPlaceId == "GOONG_DN_01");
        savedDbItem.Should().NotBeNull();
        savedDbItem!.Address.Should().Contain("Đà Nẵng");
        savedDbItem.ExternalSource.Should().Be("GOONG");
    }

    [Fact]
    public async Task ListNearbyVenues_WhenGpsDenied_ShouldGeocodeAddressAndSearchVenues()
    {
        // ARRANGE: Người dùng từ chối GPS, nhập "TP. Hồ Chí Minh" (Bước 7b -> 7c)
        var request = new ListNearbyVenuesRequest
        {
            Province = "TP. Hồ Chí Minh",
            RadiusKm = 5.0
        };

        // ACT
        var result = await _service.ListNearbyVenuesAsync(request, userId: 1, userRole: UserRoles.Member, guestUuid: null);

        // ASSERT: Trả về các quán tại TP.HCM được tính khoảng cách từ tâm TP.HCM và sắp xếp tăng dần
        result.Should().NotBeNull();
        result.Venues.Should().NotBeEmpty();
        result.Venues.Should().OnlyContain(v => v.Address.Contains("TP.HCM"));

        var venues = result.Venues;
        for (int i = 0; i < venues.Count - 1; i++)
        {
            venues[i].DistanceKm.Should().BeLessThanOrEqualTo(venues[i + 1].DistanceKm);
        }
    }

    [Fact]
    public async Task ListNearbyVenues_WhenGpsDeniedAndAddressOutsideVietnam_ShouldThrowForbidden()
    {
        // ARRANGE: Goong Geocode trả về tọa độ ngoài lãnh thổ Việt Nam (Tokyo: 35.6762, 139.6503)
        var mockGoong = new Mock<VeggieLife.DAL.External.Goong.IGoongPlacesClient>();
        mockGoong.Setup(g => g.IsConfigured).Returns(true);
        mockGoong.Setup(g => g.GeocodeAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VeggieLife.DAL.External.Goong.GoongLocation { Lat = 35.6762, Lng = 139.6503 });

        var service = new VenueService(_db, _mockRedis.Object, _mockLogger.Object, mockGoong.Object);

        var request = new ListNearbyVenuesRequest
        {
            Province = "Tokyo Japan"
        };

        // ACT & ASSERT: Bị chặn với 403 ERR_GUEST_REGION_UNSUPPORTED (Bước 3 / 3a)
        var act = () => service.ListNearbyVenuesAsync(request, userId: 1, userRole: UserRoles.Member, guestUuid: null);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrGuestRegionUnsupported);
    }

    [Fact]
    public async Task ListNearbyVenues_WhenGpsDeniedAndAddressUnresolvable_ShouldThrowBadRequest()
    {
        // ARRANGE: Nhập địa chỉ lạ không thể geocode và không nằm trong từ điển 63 tỉnh thành
        var request = new ListNearbyVenuesRequest
        {
            Province = "UnknownCityXYZ12345"
        };

        // ACT & ASSERT: 400 ERR_VALIDATION
        var act = () => _service.ListNearbyVenuesAsync(request, userId: 1, userRole: UserRoles.Member, guestUuid: null);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
        ex.Which.Message.Should().Contain("Không xác định được tọa độ trung tâm");
    }

    [Fact]
    public async Task ListNearbyVenues_WhenGpsDeniedAndGoongConfigured_ShouldUseGoongGeocoding()
    {
        // ARRANGE: Goong Geocode thành công địa chỉ nhập tay
        var mockGoong = new Mock<VeggieLife.DAL.External.Goong.IGoongPlacesClient>();
        mockGoong.Setup(g => g.IsConfigured).Returns(true);
        mockGoong.Setup(g => g.GeocodeAddressAsync("Hoàn Kiếm, Hà Nội", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VeggieLife.DAL.External.Goong.GoongLocation { Lat = 21.0285, Lng = 105.8542 });

        var service = new VenueService(_db, _mockRedis.Object, _mockLogger.Object, mockGoong.Object);

        var request = new ListNearbyVenuesRequest
        {
            Province = "Hà Nội",
            District = "Hoàn Kiếm",
            RadiusKm = 5.0
        };

        // ACT
        var result = await service.ListNearbyVenuesAsync(request, userId: 1, userRole: UserRoles.Member, guestUuid: null);

        // ASSERT: Gọi Goong Geocode thành công và trả về danh sách quán Hà Nội
        mockGoong.Verify(g => g.GeocodeAddressAsync("Hoàn Kiếm, Hà Nội", It.IsAny<CancellationToken>()), Times.Once);
        result.Venues.Should().NotBeEmpty();
        result.Venues.Should().OnlyContain(v => v.Address.Contains("Hà Nội"));
    }

    #region FN-MAP-02: Xem chi tiết quán (GetVenueDetailAsync)

    [Fact]
    public async Task GetVenueDetail_ValidId_ReturnsVenueDetailWithCategories()
    {
        // ACT
        var result = await _service.GetVenueDetailAsync(301);

        // ASSERT
        result.Should().NotBeNull();
        result.VenueId.Should().Be(301);
        result.Name.Should().Be("Ưu Đàm Chay");
        result.Address.Should().Be("34 Hàng Bài, Hoàn Kiếm, Hà Nội");
        result.Categories.Should().Contain("Món Cơm & Xôi Chay");
        result.DirectionsUrl.Should().Contain("destination=21.0227,105.8524");
        result.Source.Should().Be("GOONG");

        // Xác nhận đã lưu cache với TTL 24h (BR-SYS-CACHE-01)
        _mockRedis.Verify(r => r.SetAsync("venue_detail:301", It.IsAny<VenueDetailDto>(), TimeSpan.FromHours(24), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetVenueDetail_WithCoordinates_ComputesDistanceKm()
    {
        // ARRANGE: Hồ Gươm (21.0285, 105.8542)
        double userLat = 21.0285;
        double userLng = 105.8542;

        // ACT
        var result = await _service.GetVenueDetailAsync(301, userLat, userLng);

        // ASSERT
        result.DistanceKm.Should().NotBeNull();
        result.DistanceKm.Value.Should().BeInRange(0.5, 1.0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetVenueDetail_InvalidId_ThrowsBadRequest(int invalidId)
    {
        // ACT & ASSERT
        var act = () => _service.GetVenueDetailAsync(invalidId);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    [Fact]
    public async Task GetVenueDetail_NonExistentVenue_ThrowsNotFound()
    {
        // ACT & ASSERT
        var act = () => _service.GetVenueDetailAsync(99999);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrVenueNotFound);
    }

    [Fact]
    public async Task GetVenueDetail_HiddenVenue_ThrowsNotFoundErrVenueNotFound()
    {
        // ARRANGE: Quán bị ẩn (Is_Active = 0, BR-ADM-MAP-001)
        var hiddenVenue = new VeganVenue
        {
            VenueId = 399,
            ExternalSource = "GOONG",
            VenueName = "Quán Bị Admin Ẩn",
            Address = "123 Phố Ẩn, Hà Nội",
            Latitude = 21.02m,
            Longitude = 105.85m,
            Rating = 4.0m,
            IsFullyVegan = true,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };
        _db.VeganVenues.Add(hiddenVenue);
        await _db.SaveChangesAsync();

        // ACT & ASSERT: Quán bị ẩn trả về 404 ERR_VENUE_NOT_FOUND
        var act = () => _service.GetVenueDetailAsync(399);
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrVenueNotFound);
    }

    [Fact]
    public async Task GetVenueDetail_CachedInRedis_ReturnsCachedDtoWithoutQueryingDb()
    {
        // ARRANGE
        var cachedDto = new VenueDetailDto
        {
            VenueId = 301,
            Name = "Ưu Đàm Chay (From Cache)",
            Address = "34 Hàng Bài, Hà Nội",
            DirectionsUrl = "https://www.google.com/maps/dir/?api=1&destination=21.0227,105.8524",
            Source = "GOONG"
        };
        _mockRedis.Setup(r => r.GetAsync<VenueDetailDto>("venue_detail:301", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedDto);

        // ACT
        var result = await _service.GetVenueDetailAsync(301, 21.0285, 105.8542);

        // ASSERT
        result.Name.Should().Be("Ưu Đàm Chay (From Cache)");
        result.DistanceKm.Should().NotBeNull();
        _mockRedis.Verify(r => r.GetAsync<VenueDetailDto>("venue_detail:301", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region FN-MAP-03: Gợi ý quán theo món (ListRecipeVenuesAsync)

    [Fact]
    public async Task ListRecipeVenues_ValidRecipe_ReturnsMatchingCategoryVenuesSortedByDistance()
    {
        // ARRANGE: Tạo công thức thuộc Category 9 (Món Lẩu Chay)
        var recipe = new Recipe
        {
            RecipeId = 501,
            CategoryId = 9,
            DietarySchoolId = 1,
            Title = "Lẩu Nấm Chay Thập Cẩm",
            Instructions = "Nấu nước dùng nấm, nhúng rau củ",
            DifficultyLevel = "EASY",
            Status = "APPROVED_ACTIVE",
            Source = "COMMUNITY",
            CreatedAt = DateTime.UtcNow
        };
        _db.Recipes.Add(recipe);
        await _db.SaveChangesAsync();

        // Tọa độ Hồ Gươm, Hà Nội (21.0285, 105.8542)
        double userLat = 21.0285;
        double userLng = 105.8542;

        // ACT: Member tìm quán gợi ý
        var results = await _service.ListRecipeVenuesAsync(
            recipeId: 501,
            lat: userLat,
            lng: userLng,
            limit: 5,
            userId: 1,
            userRole: UserRoles.Member,
            guestUuid: null);

        // ASSERT: Quán Vị Lai (Id 303) có Category 9 và cách ~0.8 km
        results.Should().NotBeEmpty();
        results.Should().Contain(v => v.VenueId == 303);
        results.First().DistanceKm.Should().BeLessThan(2.0);
    }

    [Fact]
    public async Task ListRecipeVenues_NonExistentRecipe_ThrowsNotFoundErrContentNotFound()
    {
        // ACT & ASSERT: Công thức không tồn tại -> 404 ERR_CONTENT_NOT_FOUND
        var act = () => _service.ListRecipeVenuesAsync(
            recipeId: 99999,
            lat: 21.0285,
            lng: 105.8542,
            limit: 5,
            userId: 1,
            userRole: UserRoles.Member,
            guestUuid: null);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrContentNotFound);
    }

    [Fact]
    public async Task ListRecipeVenues_UnapprovedRecipe_ThrowsNotFoundErrContentNotFound()
    {
        // ARRANGE: Công thức đang PENDING_REVIEW
        var recipe = new Recipe
        {
            RecipeId = 502,
            CategoryId = 9,
            DietarySchoolId = 1,
            Title = "Lẩu Chưa Duyệt",
            Instructions = "Chưa duyệt",
            DifficultyLevel = "EASY",
            Status = "PENDING_REVIEW",
            Source = "COMMUNITY",
            CreatedAt = DateTime.UtcNow
        };
        _db.Recipes.Add(recipe);
        await _db.SaveChangesAsync();

        // ACT & ASSERT
        var act = () => _service.ListRecipeVenuesAsync(
            recipeId: 502,
            lat: 21.0285,
            lng: 105.8542,
            limit: 5,
            userId: 1,
            userRole: UserRoles.Member,
            guestUuid: null);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrContentNotFound);
    }

    [Fact]
    public async Task ListRecipeVenues_OutsideVietnam_ThrowsForbiddenErrGuestRegionUnsupported()
    {
        // ARRANGE: Tọa độ Tokyo Nhật Bản (35.6762, 139.6503)
        var act = () => _service.ListRecipeVenuesAsync(
            recipeId: 1,
            lat: 35.6762,
            lng: 139.6503,
            limit: 5,
            userId: null,
            userRole: UserRoles.Guest,
            guestUuid: "guest-test");

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrGuestRegionUnsupported);
    }

    [Fact]
    public async Task ListRecipeVenues_GuestQuotaExceeded_ThrowsForbiddenErrQuotaMapExceeded()
    {
        // ARRANGE: Guest đã dùng 3 lượt
        var guestId = "guest-quota-test";
        _mockRedis.Setup(r => r.GetAsync<int?>($"guest_quota:{guestId}:map", It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        // ACT & ASSERT: 403 ERR_QUOTA_MAP_EXCEEDED
        var act = () => _service.ListRecipeVenuesAsync(
            recipeId: 1,
            lat: 21.0285,
            lng: 105.8542,
            limit: 5,
            userId: null,
            userRole: UserRoles.Guest,
            guestUuid: guestId);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(403);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ErrQuotaMapExceeded);
    }

    [Fact]
    public async Task ListRecipeVenues_GuestWithResults_DeductsQuota()
    {
        // ARRANGE
        var recipe = new Recipe
        {
            RecipeId = 503,
            CategoryId = 1,
            DietarySchoolId = 1,
            Title = "Cơm Chiên Dương Châu Chay",
            Instructions = "Chiên cơm với rau củ",
            DifficultyLevel = "EASY",
            Status = "APPROVED_ACTIVE",
            Source = "COMMUNITY",
            CreatedAt = DateTime.UtcNow
        };
        _db.Recipes.Add(recipe);
        await _db.SaveChangesAsync();

        var guestId = "guest-fresh";
        _mockRedis.Setup(r => r.GetAsync<int?>($"guest_quota:{guestId}:map", It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mockRedis.Setup(r => r.IncrementAsync($"guest_quota:{guestId}:map", 1, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // ACT
        var results = await _service.ListRecipeVenuesAsync(
            recipeId: 503,
            lat: 21.0285,
            lng: 105.8542,
            limit: 5,
            userId: null,
            userRole: UserRoles.Guest,
            guestUuid: guestId);

        // ASSERT: Trả về kết quả và tăng quota của khách
        results.Should().NotBeEmpty();
        _mockRedis.Verify(r => r.IncrementAsync($"guest_quota:{guestId}:map", 1, TimeSpan.FromHours(24), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListRecipeVenues_MemberRole_ReturnsMatchingCategoryVenues()
    {
        // ARRANGE
        var recipe = new Recipe
        {
            RecipeId = 504,
            CategoryId = 1,
            DietarySchoolId = 1,
            Title = "Cơm Trắng Chay",
            Instructions = "Nấu cơm",
            DifficultyLevel = "EASY",
            Status = "APPROVED_ACTIVE",
            Source = "COMMUNITY",
            CreatedAt = DateTime.UtcNow
        };
        _db.Recipes.Add(recipe);
        await _db.SaveChangesAsync();

        // ACT: Member tìm quán
        var results = await _service.ListRecipeVenuesAsync(
            recipeId: 504,
            lat: 21.0285,
            lng: 105.8542,
            limit: 5,
            userId: 10,
            userRole: UserRoles.Member,
            guestUuid: null);

        // ASSERT: Quán Dưỡng Sinh Cũ (VenueId 305) cùng danh mục vẫn được hiển thị
        results.Should().Contain(v => v.VenueId == 305);
    }

    #endregion
}

