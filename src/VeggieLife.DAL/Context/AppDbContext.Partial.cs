using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VeggieLife.DAL.Entities;

namespace VeggieLife.DAL.Context;

/// <summary>
/// Phần mở rộng của AppDbContext (Database-First).
/// Nơi chứa toàn bộ code cấu hình tùy biến của dự án:
/// - Global Query Filters (Soft Delete theo BR-ADM-USR-002, trạng thái hoạt động gói VIP...)
/// - Cấu hình chuyển đổi Enum / Value Converters (README.md mục 5)
/// - Custom Model Mapping (độ chính xác số thực, ràng buộc chuỗi, khóa ngoại)
/// - Tự động cập nhật Audit Timestamps (Updated_At)
/// File này KHÔNG bao giờ bị ghi đè khi chạy lệnh `dotnet ef dbcontext scaffold --force`.
/// </summary>
public partial class AppDbContext
{
    /// <summary>
    /// Hook partial method được gọi ở cuối OnModelCreating của AppDbContext (scaffold sinh).
    /// </summary>
    /// <param name="modelBuilder">ModelBuilder của EF Core.</param>
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        ConfigureGlobalFilters(modelBuilder);
        ConfigureEnumConverters(modelBuilder);
        ConfigureCustomMappings(modelBuilder);
    }

    /// <summary>
    /// Cấu hình Global Query Filters.
    /// Tự động loại trừ người dùng bị xóa mềm/ẩn danh hóa theo BR-ADM-USR-002 trong các truy vấn thường.
    /// (Khi cần truy vấn toàn bộ kể cả bản ghi đã xóa, gọi .IgnoreQueryFilters() trong Repository).
    /// </summary>
    private static void ConfigureGlobalFilters(ModelBuilder modelBuilder)
    {
        // 1. Soft Delete filter cho User: loại trừ SOFT_DELETED và ANONYMIZED_DELETED
        modelBuilder.Entity<User>()
            .HasQueryFilter(u => u.AccountStatus != "SOFT_DELETED" && u.AccountStatus != "ANONYMIZED_DELETED");

        // 2. Filter cho SubscriptionPlan: mặc định chỉ nạp các gói VIP đang mở bán
        modelBuilder.Entity<SubscriptionPlan>()
            .HasQueryFilter(p => p.IsActive);
    }

    /// <summary>
    /// Cấu hình Value Converters và định dạng dữ liệu chuỗi/enum theo quy ước kiến trúc và CHECK constraints.
    /// </summary>
    private static void ConfigureEnumConverters(ModelBuilder modelBuilder)
    {
        // Users
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.Role).HasMaxLength(20).IsUnicode(false);
            entity.Property(u => u.AccountStatus).HasMaxLength(30).IsUnicode(false);
        });

        // HealthProfiles
        modelBuilder.Entity<HealthProfile>(entity =>
        {
            entity.Property(h => h.Gender).HasMaxLength(10).IsUnicode(false);
            entity.Property(h => h.HealthGoal).HasMaxLength(30).IsUnicode(false);
            entity.Property(h => h.RegionPreference).HasMaxLength(20).IsUnicode(false);
        });

        // PaymentTransactions (Giao dịch VIP Momo / VietQR)
        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.Property(p => p.GatewayProvider).HasMaxLength(30).IsUnicode(false);
            entity.Property(p => p.TransactionStatus).HasMaxLength(15).IsUnicode(false);
            entity.Property(p => p.PartnerTransactionCode).HasMaxLength(100).IsUnicode(false);
            entity.Property(p => p.SignatureHash).HasMaxLength(256).IsUnicode(false);
            entity.Property(p => p.ReceiptUrl).HasMaxLength(255).IsUnicode(false);
        });

        // ProductAllergens & ProductEnumbers
        modelBuilder.Entity<ProductAllergen>(entity =>
        {
            entity.Property(pa => pa.PresenceType).HasMaxLength(15).IsUnicode(false);
        });

        modelBuilder.Entity<ProductEnumber>(entity =>
        {
            entity.Property(pe => pe.Code).HasMaxLength(15).IsUnicode(false);
        });

        // UserScanHistory
        modelBuilder.Entity<UserScanHistory>(entity =>
        {
            entity.Property(s => s.ScanMethod).HasMaxLength(25).IsUnicode(false);
            entity.Property(s => s.GuestUuid).HasMaxLength(36).IsUnicode(false);
        });

        // AIQuotaLogs
        modelBuilder.Entity<AiquotaLog>(entity =>
        {
            entity.Property(q => q.FeatureCode).HasMaxLength(50).IsUnicode(false);
            entity.Property(q => q.RequestStatus).HasMaxLength(20).IsUnicode(false);
        });

        // Products
        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.VeganVerdict).HasMaxLength(15).IsUnicode(false);
        });

        // ENumberDictionary
        modelBuilder.Entity<EnumberDictionary>(entity =>
        {
            entity.Property(e => e.VeganStatus).HasMaxLength(20).IsUnicode(false);
        });

        // ChatMessages & ChatSessions
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.Property(m => m.SenderType).HasMaxLength(20).IsUnicode(false);
        });

        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.Property(s => s.ContextType).HasMaxLength(30).IsUnicode(false);
            entity.Property(s => s.GuestUuid).HasMaxLength(36).IsUnicode(false);
        });
    }

    /// <summary>
    /// Cấu hình các ánh xạ tùy biến bổ sung (Precision số thực, kích thước trường ghi chú).
    /// </summary>
    private static void ConfigureCustomMappings(ModelBuilder modelBuilder)
    {
        // Độ chính xác số tiền thanh toán VNĐ
        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.Property(p => p.AmountVnd).HasPrecision(12, 2);
        });

        // Ghi chú tiền sử dị ứng
        modelBuilder.Entity<UserAllergy>(entity =>
        {
            entity.Property(ua => ua.Notes).HasMaxLength(255);
        });

        // Đồng bộ độ chính xác số thập phân cho các trường dinh dưỡng
        modelBuilder.Entity<HealthProfile>(entity =>
        {
            entity.Property(h => h.HeightCm).HasPrecision(5, 1);
            entity.Property(h => h.CurrentWeightKg).HasPrecision(5, 1);
            entity.Property(h => h.TargetWeightKg).HasPrecision(5, 1);
            entity.Property(h => h.BmiValue).HasPrecision(4, 1);
            entity.Property(h => h.BmrKcal).HasPrecision(6, 1);
            entity.Property(h => h.TdeeKcal).HasPrecision(6, 1);
            entity.Property(h => h.TargetDailyCalories).HasPrecision(6, 1);
        });

        // UserWeightHistory
        modelBuilder.Entity<UserWeightHistory>(entity =>
        {
            entity.Property(w => w.WeightKg).HasPrecision(5, 2);
        });

        // AIQuotaLogs
        modelBuilder.Entity<AiquotaLog>(entity =>
        {
            entity.Property(q => q.CostEstimateUsd).HasPrecision(8, 4);
        });
    }

    /// <summary>
    /// Ghi đè SaveChangesAsync để tự động cập nhật mốc thời gian Updated_At (theo UTC).
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Ghi đè SaveChanges đồng bộ để tự động cập nhật mốc thời gian Updated_At (theo UTC).
    /// </summary>
    public override int SaveChanges()
    {
        UpdateAuditTimestamps();
        return base.SaveChanges();
    }

    /// <summary>
    /// Tự động cập nhật thuộc tính Updated_At / UpdatedAt cho các thực thể bị chỉnh sửa.
    /// </summary>
    private void UpdateAuditTimestamps()
    {
        var entries = ChangeTracker.Entries();
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Modified)
            {
                if (entry.Metadata.FindProperty("UpdatedAt") != null)
                {
                    entry.Property("UpdatedAt").CurrentValue = now;
                }
            }
        }
    }
}
