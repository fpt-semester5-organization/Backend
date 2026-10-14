using System;
using System.Collections.Generic;
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

namespace VeggieLife.BLL.Services.Venues;

/// <summary>
/// Hiện thực nghiệp vụ tìm kiếm quán ăn chay lân cận (FN-MAP-01).
/// </summary>
public class VenueService : IVenueService
{
    private readonly AppDbContext _db;
    private readonly IRedisService _redis;
    private readonly ILogger<VenueService> _logger;

    public VenueService(
        AppDbContext db,
        IRedisService redis,
        ILogger<VenueService> logger)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
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

        // 2. Xác định vai trò người dùng (Khách hay Thành viên)
        bool isGuest = string.Equals(userRole, UserRoles.Guest, StringComparison.OrdinalIgnoreCase) || !userId.HasValue;

        double effectiveRadius = 5.0;
        if (isGuest)
        {
            // Khách: bán kính cố định 5 km (BR-GUEST-03)
            if (request.RadiusKm > 5.0)
            {
                throw AppException.BadRequest(
                    ErrorCodes.ErrValidation,
                    "Khách vãng lai chỉ được tìm kiếm trong phạm vi bán kính tối đa 5 km.");
            }
            effectiveRadius = 5.0;
        }
        else
        {
            // Thành viên: bán kính 1..10 km (mặc định 5)
            if (request.RadiusKm < 1.0 || request.RadiusKm > 10.0)
            {
                throw AppException.BadRequest(
                    ErrorCodes.ErrValidation,
                    "Bán kính tìm kiếm của thành viên phải từ 1 đến 10 km.");
            }
            effectiveRadius = request.RadiusKm;
        }

        // 3. Kiểm tra hạn mức dùng thử của Khách vãng lai qua Redis (BR-GUEST-03)
        string? effectiveGuestId = null;
        int currentGuestUsage = 0;
        if (isGuest)
        {
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

        // 4. Kiểm tra Cache Geohash trên Redis (TTL 24 giờ, BR-SYS-CACHE-01)
        List<NearbyVenueItemDto>? candidateVenues = null;
        string? cacheKey = null;

        if (request.Lat.HasValue && request.Lng.HasValue)
        {
            var geohash6 = GeoHelper.EncodeGeohash(request.Lat.Value, request.Lng.Value, 6);
            cacheKey = $"venue_geo:{geohash6}:{Math.Round(effectiveRadius, 1)}";
            if (request.CategoryId.HasValue)
            {
                cacheKey += $":{request.CategoryId.Value}";
            }

            candidateVenues = await _redis.GetAsync<List<NearbyVenueItemDto>>(cacheKey, ct);
        }

        if (candidateVenues == null)
        {
            // 5. Truy vấn CSDL bảng VeganVenues (chỉ quán Is_Active = 1, BR-ADM-MAP-001)
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

            if (request.Lat.HasValue && request.Lng.HasValue)
            {
                var userLat = request.Lat.Value;
                var userLng = request.Lng.Value;

                foreach (var v in venuesFromDb)
                {
                    var distance = GeoHelper.CalculateDistanceKm(userLat, userLng, (double)v.Latitude, (double)v.Longitude);
                    if (distance <= effectiveRadius)
                    {
                        candidateVenues.Add(MapToDto(v, distance));
                    }
                }
            }
            else if (!string.IsNullOrWhiteSpace(request.Province))
            {
                var prov = request.Province.Trim();
                var dist = request.District?.Trim();

                foreach (var v in venuesFromDb)
                {
                    bool matchProvince = v.Address.Contains(prov, StringComparison.OrdinalIgnoreCase);
                    bool matchDistrict = string.IsNullOrEmpty(dist) || v.Address.Contains(dist, StringComparison.OrdinalIgnoreCase);

                    if (matchProvince && matchDistrict)
                    {
                        candidateVenues.Add(MapToDto(v, 0.0));
                    }
                }
            }

            // Ghi vào Redis cache 24 giờ nếu có cache key
            if (!string.IsNullOrEmpty(cacheKey))
            {
                await _redis.SetAsync(cacheKey, candidateVenues, TimeSpan.FromHours(24), ct);
            }
        }

        // 6. Áp dụng quy tắc xếp hạng và loại bỏ quán < 2.5 sao đối với Thành viên (BR-USR-11)
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

        // 7. Phân trang
        var pagedItems = sortedList
            .Skip((request.Page - 1) * request.Size)
            .Take(request.Size)
            .ToList();

        // 8. Trừ lượt Khách chỉ khi có >= 1 kết quả (BR-GUEST-03 Rule 8)
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

        // 9. Ghi nhật ký với tọa độ làm tròn 2 chữ số (BR-AUTH-002)
        if (request.Lat.HasValue && request.Lng.HasValue)
        {
            _logger.LogInformation(
                "Tìm thấy {Total} quán chay trong bán kính {Radius} km quanh tọa độ ({Lat}, {Lng}) cho vai trò {Role}.",
                total,
                effectiveRadius,
                Math.Round(request.Lat.Value, 2),
                Math.Round(request.Lng.Value, 2),
                userRole);
        }
        else
        {
            _logger.LogInformation(
                "Tìm thấy {Total} quán chay theo địa bàn {Province} - {District} cho vai trò {Role}.",
                total,
                request.Province,
                request.District ?? "Tất cả",
                userRole);
        }

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
            DirectionsUrl = $"https://www.google.com/maps/dir/?api=1&destination={v.Latitude:G29},{v.Longitude:G29}"
        };
    }
}
