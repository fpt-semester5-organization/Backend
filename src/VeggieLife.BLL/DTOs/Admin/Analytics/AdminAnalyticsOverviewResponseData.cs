using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Admin.Analytics;

/// <summary>
/// DTO chứa toàn bộ dữ liệu thống kê tổng quan nền tảng dành cho Quản trị viên (FN-ADM-18).
/// </summary>
public class AdminAnalyticsOverviewResponseData
{
    public UsersOverviewDto Users { get; set; } = new();

    public ContentOverviewDto Content { get; set; } = new();

    public VipOverviewDto Vip { get; set; } = new();

    public int ModerationQueue { get; set; }

    public double AiCostUsd { get; set; }
}

public class UsersOverviewDto
{
    public int Total { get; set; }

    public int NewInPeriod { get; set; }

    public Dictionary<string, int> ByStatus { get; set; } = new();
}

public class ContentOverviewDto
{
    public int Recipes { get; set; }

    public int Posts { get; set; }

    public int Videos { get; set; }
}

public class VipOverviewDto
{
    public int Active { get; set; }

    public long RevenueVnd { get; set; }
}
