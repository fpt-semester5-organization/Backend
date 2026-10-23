using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.DTOs.Admin.Analytics;

namespace VeggieLife.BLL.Services.Admin;

/// <summary>
/// Giao diện dịch vụ phân tích & thống kê tổng quan nền tảng dành cho Quản trị viên (FN-ADM-18).
/// Tuân thủ BR-ADM-REP-01 (Chỉ Admin xem báo cáo tăng trưởng, không lộ PII), BR-ADM-AI-001 (Chi phí AI),
/// và cơ chế lưu tạm Redis Cache TTL 5 phút.
/// </summary>
public interface IAdminAnalyticsService
{
    /// <summary>
    /// Lấy chỉ số tổng quan nền tảng: người dùng, nội dung, gói VIP & doanh thu, hàng đợi duyệt và chi phí AI.
    /// Có cache phân tán 5 phút.
    /// </summary>
    Task<AdminAnalyticsOverviewResponseData> GetOverviewAsync(
        AdminAnalyticsOverviewFilterRequest filter,
        CancellationToken ct = default);
}
