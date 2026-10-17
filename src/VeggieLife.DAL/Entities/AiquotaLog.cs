using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class AiquotaLog
{
    public long LogId { get; set; }

    public int UserId { get; set; }

    public string FeatureCode { get; set; } = null!;

    public int TokensConsumed { get; set; }

    public decimal CostEstimateUsd { get; set; }

    public string RequestStatus { get; set; } = null!;

    public string? ErrorMessage { get; set; }

    public int ExecutionTimeMs { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
