using System;
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
}
