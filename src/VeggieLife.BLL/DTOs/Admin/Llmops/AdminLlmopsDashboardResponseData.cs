using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Admin.Llmops;

/// <summary>
/// Dữ liệu bảng điều khiển LLMOps / Giám sát chi phí Token AI dành cho Quản trị viên (FN-ADM-16).
/// Khớp chuẩn OpenAPI GetAdminLlmopsDashboardResponse và hỗ trợ đầy đủ ma trận dữ liệu cho giao diện AdminAiPage.
/// </summary>
public class AdminLlmopsDashboardResponseData
{
    /// <summary>
    /// Tổng chi phí AI phát sinh trong ngày hôm nay (USD).
    /// </summary>
    public decimal TodayCostUsd { get; set; }

    /// <summary>
    /// Hạn mức ngân sách tối đa trong ngày (USD). Mặc định $12.5 (MVP BR-ADM-AI-001).
    /// </summary>
    public decimal DailyBudgetUsd { get; set; } = 12.5m;

    /// <summary>
    /// Tỷ lệ phần trăm ngân sách đã sử dụng trong ngày (%).
    /// </summary>
    public double UsagePercent { get; set; }

    /// <summary>
    /// Cấp độ cảnh báo ngân sách: NONE | WARNING (>= 80%) | THROTTLED (>= 100%).
    /// </summary>
    public string AlertLevel { get; set; } = "NONE";

    /// <summary>
    /// Chuỗi dữ liệu chi phí và token theo từng điểm (ngày / tính năng).
    /// </summary>
    public List<CostPointDto> Series { get; set; } = new();

    /// <summary>
    /// Danh sách các ngày có dữ liệu (tăng dần YYYY-MM-DD).
    /// </summary>
    public List<string> Dates { get; set; } = new();

    /// <summary>
    /// Danh sách các tính năng AI có dữ liệu.
    /// </summary>
    public List<string> Features { get; set; } = new();

    /// <summary>
    /// Ma trận chi phí và token: cells[date][feature].
    /// </summary>
    public Dictionary<string, Dictionary<string, CostCellDto>> Cells { get; set; } = new();

    /// <summary>
    /// Tổng hợp chi phí và token theo từng ngày: dayTotals[date].
    /// </summary>
    public Dictionary<string, CostCellDto> DayTotals { get; set; } = new();

    /// <summary>
    /// Tổng hợp chi phí và token theo từng tính năng: featureTotals[feature].
    /// </summary>
    public Dictionary<string, CostCellDto> FeatureTotals { get; set; } = new();

    /// <summary>
    /// Tổng hợp chi phí và token toàn bộ kỳ lọc.
    /// </summary>
    public CostCellDto Total { get; set; } = new();

    /// <summary>
    /// Tổng số Prompt Tokens (Input / Ngữ cảnh RAG) trong kỳ.
    /// </summary>
    public long TotalPromptTokens { get; set; }

    /// <summary>
    /// Tổng số Completion Tokens (Output / Mô hình sinh) trong kỳ.
    /// </summary>
    public long TotalCompletionTokens { get; set; }

    /// <summary>
    /// Chi phí ước tính riêng cho Prompt Tokens (USD).
    /// </summary>
    public decimal PromptCostUsd { get; set; }

    /// <summary>
    /// Chi phí ước tính riêng cho Completion Tokens (USD).
    /// </summary>
    public decimal CompletionCostUsd { get; set; }

    /// <summary>
    /// Phân bổ tiêu thụ Token theo hạng hội viên (VIP, Member, System/Khách).
    /// </summary>
    public UserTierDistributionDto UserTierDistribution { get; set; } = new();

    /// <summary>
    /// Danh sách top 5 người dùng tiêu tốn nhiều token nhất trong kỳ lọc.
    /// </summary>
    public List<TopSpenderDto> TopSpenders { get; set; } = new();

    /// <summary>
    /// Chi phí cao nhất của một ngày trong kỳ (dùng để vẽ trục tọa độ biểu đồ).
    /// </summary>
    public decimal MaxDayCost { get; set; }
}
