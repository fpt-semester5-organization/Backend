using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class User
{
    public int UserId { get; set; }

    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string FullName { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? AvatarUrl { get; set; }

    public string Role { get; set; } = null!;

    public int FailedLoginCount { get; set; }

    public DateTime? LockedUntil { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? GoogleId { get; set; }

    public string AccountStatus { get; set; } = null!;

    public int CountViolationReports { get; set; }

    public virtual ICollection<AiquotaLog> AiquotaLogs { get; set; } = new List<AiquotaLog>();

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<FridgeInventory> FridgeInventories { get; set; } = new List<FridgeInventory>();

    public virtual ICollection<FridgeSession> FridgeSessions { get; set; } = new List<FridgeSession>();

    public virtual HealthProfile? HealthProfile { get; set; }

    public virtual ICollection<MealPlan> MealPlans { get; set; } = new List<MealPlan>();

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();

    public virtual ICollection<SubscriptionPlan> SubscriptionPlans { get; set; } = new List<SubscriptionPlan>();

    public virtual ICollection<Survey> Surveys { get; set; } = new List<Survey>();

    public virtual ICollection<UserAllergy> UserAllergies { get; set; } = new List<UserAllergy>();

    public virtual ICollection<UserScanHistory> UserScanHistories { get; set; } = new List<UserScanHistory>();

    public virtual ICollection<UserWeightHistory> UserWeightHistories { get; set; } = new List<UserWeightHistory>();

    public virtual ICollection<ViolationReport> ViolationReportAdmins { get; set; } = new List<ViolationReport>();

    public virtual ICollection<ViolationReport> ViolationReportReporterUsers { get; set; } = new List<ViolationReport>();

    public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();
}
