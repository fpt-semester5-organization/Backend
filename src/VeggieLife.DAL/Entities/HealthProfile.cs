using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class HealthProfile
{
    public int UserId { get; set; }

    public int DietarySchoolId { get; set; }

    public string Gender { get; set; } = null!;

    public DateOnly DateOfBirth { get; set; }

    public decimal HeightCm { get; set; }

    public decimal CurrentWeightKg { get; set; }

    public decimal? TargetWeightKg { get; set; }

    public decimal BmiValue { get; set; }

    public decimal BmrKcal { get; set; }

    public decimal TdeeKcal { get; set; }

    public decimal TargetDailyCalories { get; set; }

    public bool RequiresAlliumFree { get; set; }

    public string RegionPreference { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsStaleMetrics { get; set; }

    public string HealthGoal { get; set; } = null!;

    public virtual ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();

    public virtual DietarySchool DietarySchool { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
