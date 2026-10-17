using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace VeggieLife.BLL.Common.Helpers;

/// <summary>
/// Tiện ích tính toán khoảng cách địa lý (Haversine), mã hóa Geohash và kiểm tra biên giới Việt Nam (FN-MAP-01).
/// </summary>
public static class GeoHelper
{
    private const double EarthRadiusKm = 6371.0;
    private const string Base32Chars = "0123456789bcdefghjkmnpqrstuvwxyz";

    // Biên độ tọa độ lãnh thổ Việt Nam (đất liền và vùng ven biển)
    public const double VietnamMinLat = 8.0;
    public const double VietnamMaxLat = 24.0;
    public const double VietnamMinLng = 102.0;
    public const double VietnamMaxLng = 110.0;

    /// <summary>
    /// Kiểm tra xem tọa độ GPS có nằm trong lãnh thổ Việt Nam hay không (BR-GUEST-06).
    /// </summary>
    public static bool IsInVietnam(double lat, double lng)
    {
        return lat >= VietnamMinLat && lat <= VietnamMaxLat &&
               lng >= VietnamMinLng && lng <= VietnamMaxLng;
    }

    /// <summary>
    /// Tính khoảng cách địa lý giữa 2 điểm tọa độ theo công thức Haversine (km), làm tròn 1 chữ số thập phân.
    /// </summary>
    public static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;
        var rLat1 = lat1 * Math.PI / 180.0;
        var rLat2 = lat2 * Math.PI / 180.0;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(rLat1) * Math.Cos(rLat2) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Math.Round(EarthRadiusKm * c, 1);
    }

    /// <summary>
    /// Mã hóa tọa độ (lat, lng) sang Geohash chuẩn base32 với độ chính xác chỉ định (mặc định 6 ký tự ~1.2 km).
    /// Dùng làm cache key Redis: venue_geo:{geohash6}:{radius} (BR-SYS-CACHE-01).
    /// </summary>
    public static string EncodeGeohash(double lat, double lon, int precision = 6)
    {
        double[] latRange = { -90.0, 90.0 };
        double[] lonRange = { -180.0, 180.0 };
        var sb = new StringBuilder(precision);
        bool isEven = true;
        int bit = 0;
        int ch = 0;

        while (sb.Length < precision)
        {
            if (isEven)
            {
                var mid = (lonRange[0] + lonRange[1]) / 2.0;
                if (lon >= mid)
                {
                    ch |= (1 << (4 - bit));
                    lonRange[0] = mid;
                }
                else
                {
                    lonRange[1] = mid;
                }
            }
            else
            {
                var mid = (latRange[0] + latRange[1]) / 2.0;
                if (lat >= mid)
                {
                    ch |= (1 << (4 - bit));
                    latRange[0] = mid;
                }
                else
                {
                    latRange[1] = mid;
                }
            }

            isEven = !isEven;
            if (bit < 4)
            {
                bit++;
            }
            else
            {
                sb.Append(Base32Chars[ch]);
                bit = 0;
                ch = 0;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Từ điển tọa độ trung tâm các tỉnh/thành phố và đô thị lớn tại Việt Nam phục vụ Bước 7c trong luồng Swimlane.
    /// </summary>
    private static readonly Dictionary<string, (double Lat, double Lng)> ProvinceCenters = new(StringComparer.OrdinalIgnoreCase)
    {
        { "ha noi", (21.0285, 105.8542) },
        { "ho chi minh", (10.7769, 106.7009) },
        { "tp hcm", (10.7769, 106.7009) },
        { "sai gon", (10.7769, 106.7009) },
        { "hcm", (10.7769, 106.7009) },
        { "da nang", (16.0544, 108.2022) },
        { "hai phong", (20.8449, 106.6881) },
        { "can tho", (10.0452, 105.7469) },
        { "an giang", (10.5216, 105.1259) },
        { "ba ria vung tau", (10.5417, 107.2430) },
        { "vung tau", (10.3460, 107.0843) },
        { "bac giang", (21.2731, 106.1946) },
        { "bac kan", (22.1470, 105.8348) },
        { "bac lieu", (9.2941, 105.7278) },
        { "bac ninh", (21.1861, 106.0763) },
        { "ben tre", (10.2415, 106.3759) },
        { "binh dinh", (14.1667, 108.9000) },
        { "quy nhon", (13.7830, 109.2197) },
        { "binh duong", (11.1604, 106.6565) },
        { "binh phuoc", (11.7512, 106.9048) },
        { "binh thuan", (11.1042, 108.1834) },
        { "phan thiet", (10.9289, 108.1021) },
        { "ca mau", (9.1769, 105.1524) },
        { "cao bang", (22.6667, 106.2500) },
        { "dak lak", (12.6667, 108.0500) },
        { "buon ma thuot", (12.6667, 108.0383) },
        { "dak nong", (12.0000, 107.6833) },
        { "dien bien", (21.3833, 103.0167) },
        { "dong nai", (11.0000, 107.1667) },
        { "bien hoa", (10.9575, 106.8427) },
        { "dong thap", (10.5000, 105.6667) },
        { "cao lanh", (10.4602, 105.6328) },
        { "gia lai", (13.9833, 108.0000) },
        { "pleiku", (13.9833, 108.0000) },
        { "ha giang", (22.8233, 104.9839) },
        { "ha nam", (20.5833, 105.9167) },
        { "phu ly", (20.5413, 105.9136) },
        { "ha tinh", (18.3333, 105.9000) },
        { "hai duong", (20.9333, 106.3167) },
        { "hau giang", (9.7833, 105.4667) },
        { "vi thanh", (9.7844, 105.4701) },
        { "hoa binh", (20.8167, 105.3333) },
        { "hung yen", (20.6500, 106.0500) },
        { "khanh hoa", (12.2500, 109.1833) },
        { "nha trang", (12.2388, 109.1967) },
        { "kien giang", (10.0167, 105.0833) },
        { "rach gia", (10.0125, 105.0809) },
        { "phu quoc", (10.2899, 103.9840) },
        { "kon tum", (14.3500, 108.0000) },
        { "lai chau", (22.3956, 103.4586) },
        { "lam dong", (11.9404, 108.4583) },
        { "da lat", (11.9404, 108.4583) },
        { "lang son", (21.8500, 106.7667) },
        { "lao cai", (22.4833, 103.9667) },
        { "sapa", (22.3364, 103.8438) },
        { "long an", (10.5333, 106.4000) },
        { "tan an", (10.5360, 106.4137) },
        { "nam dinh", (20.4333, 106.1833) },
        { "nghe an", (19.3333, 104.8333) },
        { "vinh", (18.6796, 105.6813) },
        { "ninh binh", (20.2500, 105.9667) },
        { "ninh thuan", (11.5667, 108.9833) },
        { "phan rang", (11.5667, 108.9833) },
        { "phu tho", (21.3167, 105.2167) },
        { "viet tri", (21.3228, 105.4019) },
        { "phu yen", (13.0833, 109.0833) },
        { "tuy hoa", (13.0882, 109.3136) },
        { "quang binh", (17.5000, 106.3333) },
        { "dong hoi", (17.4739, 106.6028) },
        { "quang nam", (15.5994, 108.0047) },
        { "hoi an", (15.8801, 108.3380) },
        { "tam ky", (15.5684, 108.4795) },
        { "quang ngai", (15.1167, 108.8000) },
        { "quang ninh", (21.2500, 107.3333) },
        { "ha long", (20.9505, 107.0734) },
        { "quang tri", (16.7500, 107.1833) },
        { "dong ha", (16.8167, 107.1000) },
        { "soc trang", (9.6000, 105.9833) },
        { "son la", (21.3333, 103.9167) },
        { "tay ninh", (11.3000, 106.1000) },
        { "thai binh", (20.4500, 106.3333) },
        { "thai nguyen", (21.5928, 105.8442) },
        { "thanh hoa", (19.8000, 105.7667) },
        { "thua thien hue", (16.4637, 107.5909) },
        { "hue", (16.4637, 107.5909) },
        { "tien giang", (10.4167, 106.3333) },
        { "my tho", (10.3544, 106.3653) },
        { "tra vinh", (9.9333, 106.3333) },
        { "tuyen quang", (21.8167, 105.2167) },
        { "vinh long", (10.2500, 105.9667) },
        { "vinh phuc", (21.3000, 105.6000) },
        { "yen bai", (21.7167, 104.8667) }
    };

    /// <summary>
    /// Thử tra cứu tọa độ trung tâm của tỉnh/thành phố khi người dùng nhập thủ công địa chỉ (Bước 7c trong luồng Swimlane).
    /// </summary>
    public static bool TryGetProvinceCenter(string province, out double lat, out double lng)
    {
        lat = 0;
        lng = 0;

        if (string.IsNullOrWhiteSpace(province))
        {
            return false;
        }

        var normalized = NormalizeVietnamese(province);

        if (ProvinceCenters.TryGetValue(normalized, out var coords))
        {
            lat = coords.Lat;
            lng = coords.Lng;
            return true;
        }

        // Tìm kiếm mềm (chứa tên)
        foreach (var kvp in ProvinceCenters)
        {
            if (normalized.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase) ||
                kvp.Key.Contains(normalized, StringComparison.OrdinalIgnoreCase))
            {
                lat = kvp.Value.Lat;
                lng = kvp.Value.Lng;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Chuẩn hóa chuỗi tiếng Việt: loại bỏ dấu thanh, chuyển chữ thường, loại bỏ tiền tố tỉnh/thành phố.
    /// </summary>
    public static string NormalizeVietnamese(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalizedString.Length);

        foreach (var c in normalizedString)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        var cleaned = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        cleaned = cleaned.Replace('đ', 'd').Replace('Đ', 'd');

        cleaned = cleaned.Trim();
        if (cleaned.StartsWith("thanh pho "))
        {
            cleaned = cleaned["thanh pho ".Length..].Trim();
        }
        else if (cleaned.StartsWith("tp. "))
        {
            cleaned = cleaned["tp. ".Length..].Trim();
        }
        else if (cleaned.StartsWith("tp "))
        {
            cleaned = cleaned["tp ".Length..].Trim();
        }
        else if (cleaned.StartsWith("tinh "))
        {
            cleaned = cleaned["tinh ".Length..].Trim();
        }

        return cleaned;
    }
}

