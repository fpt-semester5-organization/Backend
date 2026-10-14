using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Helpers;
using VeggieLife.BLL.DTOs.Venues.Requests;
using VeggieLife.BLL.DTOs.Venues.Responses;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using VeggieLife.DAL.External.Goong;

namespace VeggieLife.BLL.Services.Venues;

/// <summary>
/// Hiện thực nghiệp vụ tìm kiếm quán ăn chay lân cận (FN-MAP-01).
/// </summary>
public class VenueService : IVenueService
{
    private readonly AppDbContext _db;
    private readonly IRedisService _redis;
    private readonly ILogger<VenueService> _logger;
    private readonly IGoongPlacesClient? _goongClient;

    public VenueService(
        AppDbContext db,
        IRedisService redis,
        ILogger<VenueService> logger,
        IGoongPlacesClient? goongClient = null)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
        _goongClient = goongClient;
    }

    public async Task<ListNearbyVenuesResult> ListNearbyVenuesAsync(
        ListNearbyVenuesRequest request,
        int? userId,
        string userRole,
        string? guestUuid,
        CancellationToken ct = default)
    {
        // 1. Kiểm tra tham số đầu vào (BR-VALIDATION)
        ValidateRequest(request);

        // BƯỚC 4 & RẼ NHÁNH: Kiểm tra Role: Guest hay Member
        bool isGuest = string.Equals(userRole, UserRoles.Guest, StringComparison.OrdinalIgnoreCase) || !userId.HasValue;

        double effectiveRadius = 5.0;
        string? effectiveGuestId = null;
        int currentGuestUsage = 0;

        if (isGuest)
        {
            // BƯỚC 5a: Gán Bán kính mặc định 5km & đếm lượt tra cứu (BR-GUEST-03)
            if (request.RadiusKm > 5.0)
            {
                throw AppException.BadRequest(
                    ErrorCodes.ErrValidation,
                    "Khách vãng lai chỉ được tìm kiếm trong phạm vi bán kính tối đa 5 km.");
            }
            effectiveRadius = 5.0;

            effectiveGuestId = !string.IsNullOrWhiteSpace(guestUuid)
                ? guestUuid.Trim()
                : (userId.HasValue ? userId.Value.ToString() : "anonymous");

            var quotaUsed = await _redis.GetAsync<int?>($"guest_quota:{effectiveGuestId}:map", ct)
                         ?? await _redis.GetAsync<int?>($"guest_quota:map:{effectiveGuestId}", ct)
                         ?? 0;

            if (quotaUsed >= 3)
            {
                throw AppException.Forbidden(
                    ErrorCodes.ErrQuotaMapExceeded,
                    "Khách đã tìm đủ 03 lần (BR-GUEST-03). Vui lòng đăng ký tài khoản để tiếp tục tìm kiếm quán chay không giới hạn.");
            }

            currentGuestUsage = quotaUsed;
        }
        else
        {
            // BƯỚC 5b: Gán Bán kính 10km mở rộng & cho phép tùy chỉnh bộ lọc (BR-USR-11)
            if (request.RadiusKm < 1.0 || request.RadiusKm > 10.0)
            {
                throw AppException.BadRequest(
                    ErrorCodes.ErrValidation,
                    "Bán kính tìm kiếm của thành viên phải từ 1 đến 10 km.");
            }
            effectiveRadius = request.RadiusKm;
        }

        // BƯỚC 6, 7a, 7b, 7c: Quyền GPS và Xác định Tọa độ trung tâm
        double targetLat;
        double targetLng;

        if (request.Lat.HasValue && request.Lng.HasValue)
        {
            // Bước 7a: API lấy tọa độ hiện tại từ thiết bị
            targetLat = request.Lat.Value;
            targetLng = request.Lng.Value;

            // Bước 3: Kiểm tra toạ độ Việt Nam hay Nước ngoài
            if (!GeoHelper.IsInVietnam(targetLat, targetLng))
            {
                // Bước 3a: Báo lỗi: Chưa hỗ trợ dịch vụ ngoài lãnh thổ VN
                throw AppException.Forbidden(
                    ErrorCodes.ErrGuestRegionUnsupported,
                    "IP ngoài lãnh thổ Việt Nam — dịch vụ bản đồ chưa hỗ trợ khu vực này (BR-GUEST-06).");
            }
        }
        else
        {
            // Bước 7b: Nhập thủ công địa chỉ (Tỉnh/TP -> Quận/Huyện)
            var province = request.Province!.Trim();
            var district = request.District?.Trim();
            var fullAddress = string.IsNullOrWhiteSpace(district) ? province : $"{district}, {province}";

            // Bước 7c: Chuyển đổi địa chỉ nhập thành tọa độ trung tâm
            GoongLocation? geocodedLocation = null;
            if (_goongClient?.IsConfigured == true)
            {
                geocodedLocation = await _goongClient.GeocodeAddressAsync(fullAddress, ct);
            }

            if (geocodedLocation != null)
            {
                targetLat = geocodedLocation.Lat;
                targetLng = geocodedLocation.Lng;
            }
            else if (GeoHelper.TryGetProvinceCenter(province, out var centerLat, out var centerLng))
            {
                targetLat = centerLat;
                targetLng = centerLng;
            }
            else
            {
                throw AppException.BadRequest(
                    ErrorCodes.ErrValidation,
                    "Không xác định được tọa độ trung tâm cho địa chỉ đã nhập. Vui lòng kiểm tra lại tỉnh/thành phố.");
            }

            // Bước 3: Kiểm tra toạ độ Việt Nam hay Nước ngoài
            if (!GeoHelper.IsInVietnam(targetLat, targetLng))
            {
                // Bước 3a: Báo lỗi: Chưa hỗ trợ dịch vụ ngoài lãnh thổ VN
                throw AppException.Forbidden(
                    ErrorCodes.ErrGuestRegionUnsupported,
                    "Địa chỉ ngoài lãnh thổ Việt Nam — dịch vụ bản đồ chưa hỗ trợ khu vực này (BR-GUEST-06).");
            }
        }

        // BƯỚC 8: Tổng hợp Query: Tọa độ, Bán kính, Bộ lọc & Từ khóa món ăn
        string foodKeyword = "quán chay";
        if (request.CategoryId.HasValue)
        {
            var category = await _db.FoodCategories.FindAsync(new object[] { request.CategoryId.Value }, ct);
            if (category != null && !string.IsNullOrWhiteSpace(category.CategoryName))
            {
                var catName = category.CategoryName.ToLowerInvariant();
                if (catName.Contains("cơm") || catName.Contains("xôi"))
                {
                    foodKeyword = "cơm chay";
                }
                else if (catName.Contains("lẩu"))
                {
                    foodKeyword = "lẩu chay";
                }
                else if (catName.Contains("bún") || catName.Contains("phở"))
                {
                    foodKeyword = "bún phở chay";
                }
                else if (catName.Contains("bánh"))
                {
                    foodKeyword = "bánh chay";
                }
                else
                {
                    foodKeyword = $"{category.CategoryName} chay";
                }
            }
        }

        // Kiểm tra Cache Geohash trên Redis (TTL 24 giờ, BR-SYS-CACHE-01)
        var geohash6 = GeoHelper.EncodeGeohash(targetLat, targetLng, 6);
        var cacheKey = $"venue_geo:{geohash6}:{Math.Round(effectiveRadius, 1)}";
        if (request.CategoryId.HasValue)
        {
            cacheKey += $":{request.CategoryId.Value}";
        }

        var candidateVenues = await _redis.GetAsync<List<NearbyVenueItemDto>>(cacheKey, ct);

        if (candidateVenues == null)
        {
            // BƯỚC 9b: Goong Places API: Quét tìm địa điểm quán chay (tên quán, địa chỉ, tọa độ)
            // Đồng thời truy vấn CSDL địa phương bảng VeganVenues (chỉ quán Is_Active = 1, BR-ADM-MAP-001)
            var query = _db.VeganVenues
                .AsNoTracking()
                .Where(v => v.IsActive);

            if (request.CategoryId.HasValue)
            {
                query = query.Where(v => v.VenueCategories.Any(vc => vc.CategoryId == request.CategoryId.Value));
            }

            var venuesFromDb = await query
                .Include(v => v.VenueCategories)
                    .ThenInclude(vc => vc.Category)
                .ToListAsync(ct);

            candidateVenues = new List<NearbyVenueItemDto>();

            foreach (var v in venuesFromDb)
            {
                var distance = GeoHelper.CalculateDistanceKm(targetLat, targetLng, (double)v.Latitude, (double)v.Longitude);
                if (distance <= effectiveRadius)
                {
                    candidateVenues.Add(MapToDto(v, distance));
                }
            }

            // Gọi Goong Places API quét tìm thêm địa điểm quán chay quanh tọa độ trung tâm
            if (_goongClient?.IsConfigured == true)
            {
                await TryFetchAndSaveFromGoongAsync(
                    foodKeyword,
                    targetLat,
                    targetLng,
                    effectiveRadius,
                    venuesFromDb,
                    candidateVenues,
                    ct);
            }

            // Ghi vào Redis cache 24 giờ nếu có kết quả
            await _redis.SetAsync(cacheKey, candidateVenues, TimeSpan.FromHours(24), ct);
        }

        // BƯỚC: Danh mục quán chay (Data Object)
        // Lọc bỏ quán có đánh giá < 2.5 sao đối với Thành viên (BR-USR-11)
        var filteredList = candidateVenues.AsEnumerable();
        if (!isGuest)
        {
            filteredList = filteredList.Where(v => v.Rating >= 2.5m);
        }

        // Sắp xếp: Ưu tiên khoảng cách gần nhất, sau đó đến điểm đánh giá (Rating) cao nhất
        var sortedList = filteredList
            .OrderBy(v => v.DistanceKm)
            .ThenByDescending(v => v.Rating)
            .ToList();

        var total = sortedList.Count;

        // Phân trang
        var pagedItems = sortedList
            .Skip((request.Page - 1) * request.Size)
            .Take(request.Size)
            .ToList();

        // Trừ lượt Khách chỉ khi có >= 1 kết quả (BR-GUEST-03 Rule 8)
        GuestMapQuotaDto? quotaMeta = null;
        if (isGuest && effectiveGuestId != null)
        {
            int updatedUsage = currentGuestUsage;
            if (total >= 1)
            {
                var newCount = await _redis.IncrementAsync($"guest_quota:{effectiveGuestId}:map", 1, TimeSpan.FromHours(24), ct);
                await _redis.SetAsync($"guest_quota:map:{effectiveGuestId}", (int)newCount, TimeSpan.FromHours(24), ct);
                updatedUsage = (int)newCount;
            }

            quotaMeta = new GuestMapQuotaDto
            {
                Used = updatedUsage,
                Limit = 3,
                Remaining = Math.Max(0, 3 - updatedUsage)
            };
        }

        // Ghi nhật ký với tọa độ làm tròn 2 chữ số (BR-AUTH-002)
        _logger.LogInformation(
            "Tìm thấy {Total} quán chay trong bán kính {Radius} km quanh tọa độ ({Lat}, {Lng}) cho vai trò {Role}.",
            total,
            effectiveRadius,
            Math.Round(targetLat, 2),
            Math.Round(targetLng, 2),
            userRole);

        return new ListNearbyVenuesResult
        {
            Venues = pagedItems,
            Meta = new NearbyVenuesMeta(
                page: request.Page,
                size: request.Size,
                total: total,
                radiusKm: effectiveRadius,
                quota: quotaMeta)
        };
    }


    private static void ValidateRequest(ListNearbyVenuesRequest request)
    {
        if (request.Page < 1)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Số trang (page) phải lớn hơn hoặc bằng 1.");
        }

        if (request.Size < 1 || request.Size > 20)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Kích thước trang (size) phải từ 1 đến 20.");
        }

        bool hasLat = request.Lat.HasValue;
        bool hasLng = request.Lng.HasValue;

        if ((hasLat && !hasLng) || (!hasLat && hasLng))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Vĩ độ (lat) và kinh độ (lng) phải được cung cấp cùng nhau.");
        }

        bool hasCoordinates = hasLat && hasLng;
        bool hasProvince = !string.IsNullOrWhiteSpace(request.Province);

        if (!hasCoordinates && !hasProvince)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Cần cung cấp tọa độ (lat, lng) hoặc tỉnh/thành phố (province).");
        }

        if (hasCoordinates)
        {
            var lat = request.Lat!.Value;
            var lng = request.Lng!.Value;

            if (lat < -90.0 || lat > 90.0 || lng < -180.0 || lng > 180.0)
            {
                throw AppException.BadRequest(
                    ErrorCodes.ErrValidation,
                    "Tọa độ không hợp lệ. Vĩ độ từ -90 đến 90, kinh độ từ -180 đến 180.");
            }

            // Kiểm tra trong lãnh thổ Việt Nam (BR-GUEST-06)
            if (!GeoHelper.IsInVietnam(lat, lng))
            {
                throw AppException.Forbidden(
                    ErrorCodes.ErrGuestRegionUnsupported,
                    "IP ngoài lãnh thổ Việt Nam — dịch vụ bản đồ chưa hỗ trợ khu vực này (BR-GUEST-06).");
            }
        }

        if (hasProvince && request.Province!.Trim().Length > 50)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên tỉnh/thành phố không được vượt quá 50 ký tự.");
        }

        if (!string.IsNullOrWhiteSpace(request.District) && request.District.Trim().Length > 50)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Tên quận/huyện không được vượt quá 50 ký tự.");
        }

        if (request.CategoryId.HasValue && request.CategoryId.Value <= 0)
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã danh mục món ăn (categoryId) phải lớn hơn 0.");
        }
    }

    private static NearbyVenueItemDto MapToDto(VeganVenue v, double distanceKm)
    {
        return new NearbyVenueItemDto
        {
            VenueId = v.VenueId,
            Name = v.VenueName,
            Address = v.Address,
            DistanceKm = distanceKm,
            Rating = v.Rating,
            IsFullyVegan = v.IsFullyVegan,
            ThumbnailUrl = v.ThumbnailUrl,
            Categories = v.VenueCategories.Select(vc => vc.Category.CategoryName).ToList(),
            DirectionsUrl = $"https://www.google.com/maps/dir/?api=1&destination={v.Latitude.ToString("G29", CultureInfo.InvariantCulture)},{v.Longitude.ToString("G29", CultureInfo.InvariantCulture)}"
        };
    }


    private async Task TryFetchAndSaveFromGoongAsync(
        string keyword,
        double targetLat,
        double targetLng,
        double effectiveRadius,
        List<VeganVenue> venuesFromDb,
        List<NearbyVenueItemDto> candidateVenues,
        CancellationToken ct)
    {
        if (_goongClient == null)
        {
            return;
        }

        try
        {
            var predictions = await _goongClient.SearchNearbyPlacesAsync(
                keyword,
                targetLat,
                targetLng,
                (int)(effectiveRadius * 1000),
                10,
                ct);

            bool hasNewEntities = false;

            foreach (var pred in predictions)
            {
                if (string.IsNullOrWhiteSpace(pred.PlaceId))
                {
                    continue;
                }

                // Kiểm tra xem quán đã tồn tại trong CSDL hoặc danh sách ứng viên chưa
                bool exists = venuesFromDb.Any(v => v.ExternalPlaceId == pred.PlaceId)
                    || candidateVenues.Any(cv => !string.IsNullOrEmpty(pred.StructuredFormatting?.MainText) &&
                                                 cv.Name.Equals(pred.StructuredFormatting.MainText, StringComparison.OrdinalIgnoreCase));

                if (exists)
                {
                    continue;
                }

                var detail = await _goongClient.GetPlaceDetailAsync(pred.PlaceId, ct);
                if (detail?.Geometry?.Location != null && GeoHelper.IsInVietnam(detail.Geometry.Location.Lat, detail.Geometry.Location.Lng))
                {
                    var distance = GeoHelper.CalculateDistanceKm(targetLat, targetLng, detail.Geometry.Location.Lat, detail.Geometry.Location.Lng);
                    if (distance <= effectiveRadius)
                    {
                        var newVenue = new VeganVenue
                        {
                            VenueName = !string.IsNullOrWhiteSpace(detail.Name)
                                ? detail.Name
                                : (!string.IsNullOrWhiteSpace(pred.StructuredFormatting?.MainText) ? pred.StructuredFormatting.MainText : "Quán chay"),
                            Address = !string.IsNullOrWhiteSpace(detail.FormattedAddress) ? detail.FormattedAddress : pred.Description,
                            Latitude = (decimal)detail.Geometry.Location.Lat,
                            Longitude = (decimal)detail.Geometry.Location.Lng,
                            Rating = detail.Rating.HasValue && detail.Rating.Value > 0 ? detail.Rating.Value : 4.2m,
                            IsActive = true,
                            IsFullyVegan = true,
                            ExternalSource = "GOONG",
                            ExternalPlaceId = detail.PlaceId,
                            CreatedAt = DateTime.UtcNow,
                            ThumbnailUrl = "https://images.unsplash.com/photo-1540420773420-3366772f4999?w=600&auto=format&fit=crop"
                        };

                        _db.VeganVenues.Add(newVenue);
                        venuesFromDb.Add(newVenue);
                        candidateVenues.Add(MapToDto(newVenue, distance));
                        hasNewEntities = true;
                    }
                }
            }

            if (hasNewEntities)
            {
                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("Đã đồng bộ tự động các quán chay mới từ Goong Places API vào CSDL.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi xảy ra khi tự động đồng bộ quán từ Goong Places API quanh ({Lat}, {Lng}).", targetLat, targetLng);
        }
    }
}

