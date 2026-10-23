namespace VeggieLife.BLL.DTOs.Admin.Analytics;

/// <summary>
/// DTO chứa tham số khoảng thời gian truy vấn thống kê tổng quan (FN-ADM-18).
/// </summary>
public class AdminAnalyticsOverviewFilterRequest
{
    public string? From { get; set; }

    public string? To { get; set; }
}
