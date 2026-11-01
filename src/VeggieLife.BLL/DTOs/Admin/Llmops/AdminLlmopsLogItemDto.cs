using System;

namespace VeggieLife.BLL.DTOs.Admin.Llmops;

/// <summary>
/// Chi tiết một bản ghi nhật ký tiêu thụ định mức AI (dbo.AIQuotaLogs).
/// </summary>
public class AdminLlmopsLogItemDto
{
    public long LogId { get; set; }
    public int UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public string FeatureCode { get; set; } = string.Empty;
    public int TokensConsumed { get; set; }
    public decimal CostEstimateUsd { get; set; }
    public string RequestStatus { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int ExecutionTimeMs { get; set; }
    public DateTime CreatedAt { get; set; }
}
