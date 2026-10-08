using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using VeggieLife.DAL.Entities;

namespace VeggieLife.DAL.Context;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AiquotaLog> AiquotaLogs { get; set; }

    public virtual DbSet<Allergen> Allergens { get; set; }

    public virtual DbSet<ChatMessage> ChatMessages { get; set; }

    public virtual DbSet<ChatSession> ChatSessions { get; set; }

    public virtual DbSet<EnumberDictionary> EnumberDictionaries { get; set; }

    public virtual DbSet<HealthProfile> HealthProfiles { get; set; }

    public virtual DbSet<IngredientAllergen> IngredientAllergens { get; set; }

    public virtual DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductAllergen> ProductAllergens { get; set; }

    public virtual DbSet<ProductEnumber> ProductEnumbers { get; set; }

    public virtual DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

    public virtual DbSet<Survey> Surveys { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserAllergy> UserAllergies { get; set; }

    public virtual DbSet<UserScanHistory> UserScanHistories { get; set; }

    public virtual DbSet<UserWeightHistory> UserWeightHistories { get; set; }

    public virtual DbSet<Vote> Votes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiquotaLog>(entity =>
        {
            entity.HasKey(e => e.LogId);

            entity.ToTable("AIQuotaLogs");

            entity.Property(e => e.LogId).HasColumnName("Log_ID");
            entity.Property(e => e.CostEstimateUsd)
                .HasColumnType("decimal(8, 4)")
                .HasColumnName("Cost_Estimate_USD");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");
            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(500)
                .HasColumnName("Error_Message");
            entity.Property(e => e.ExecutionTimeMs).HasColumnName("Execution_Time_Ms");
            entity.Property(e => e.FeatureCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Feature_Code");
            entity.Property(e => e.RequestStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("Request_Status");
            entity.Property(e => e.TokensConsumed).HasColumnName("Tokens_Consumed");
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.User).WithMany(p => p.AiquotaLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AIQuotaLogs_Users");
        });

        modelBuilder.Entity<Allergen>(entity =>
        {
            entity.HasIndex(e => e.Code, "UQ_Allergens_Code").IsUnique();

            entity.HasIndex(e => e.AllergenName, "UQ_Allergens_Name").IsUnique();

            entity.Property(e => e.AllergenId).HasColumnName("Allergen_ID");
            entity.Property(e => e.AllergenName)
                .HasMaxLength(100)
                .HasColumnName("Allergen_Name");
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Updated_At");
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId);

            entity.Property(e => e.MessageId).HasColumnName("Message_ID");
            entity.Property(e => e.ChatSessionId).HasColumnName("Chat_Session_ID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");
            entity.Property(e => e.MetadataJson).HasColumnName("Metadata_JSON");
            entity.Property(e => e.SenderType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("Sender_Type");

            entity.HasOne(d => d.ChatSession).WithMany(p => p.ChatMessages)
                .HasForeignKey(d => d.ChatSessionId)
                .HasConstraintName("FK_ChatMessages_ChatSessions");
        });

        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.Property(e => e.ChatSessionId).HasColumnName("Chat_Session_ID");
            entity.Property(e => e.ContextType)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("NUTRITION_CONSULTATION")
                .HasColumnName("Context_Type");
            entity.Property(e => e.GuestUuid)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("Guest_UUID");
            entity.Property(e => e.ProfilesId).HasColumnName("Profiles_ID");
            entity.Property(e => e.SessionTitle)
                .HasMaxLength(150)
                .HasDefaultValue("Há»™i thoáº¡i má»›i")
                .HasColumnName("Session_Title");
            entity.Property(e => e.StartedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Started_At");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Updated_At");
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.Profiles).WithMany(p => p.ChatSessions)
                .HasForeignKey(d => d.ProfilesId)
                .HasConstraintName("FK_ChatSessions_HealthProfiles");

            entity.HasOne(d => d.User).WithMany(p => p.ChatSessions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_ChatSessions_Users");
        });

        modelBuilder.Entity<EnumberDictionary>(entity =>
        {
            entity.HasKey(e => e.Code);

            entity.ToTable("ENumberDictionary");

            entity.Property(e => e.Code)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.CategoryType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Category_Type");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");
            entity.Property(e => e.HealthRiskNotes).HasColumnName("Health_Risk_Notes");
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.OriginSource)
                .HasMaxLength(100)
                .HasColumnName("Origin_Source");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Updated_At");
            entity.Property(e => e.VeganStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("Vegan_Status");
        });

        modelBuilder.Entity<HealthProfile>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("User_ID");
            entity.Property(e => e.BmiValue)
                .HasColumnType("decimal(4, 1)")
                .HasColumnName("BMI_Value");
            entity.Property(e => e.BmrKcal)
                .HasColumnType("decimal(6, 1)")
                .HasColumnName("BMR_Kcal");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");
            entity.Property(e => e.CurrentWeightKg)
                .HasColumnType("decimal(5, 1)")
                .HasColumnName("Current_Weight_Kg");
            entity.Property(e => e.DateOfBirth).HasColumnName("Date_Of_Birth");
            entity.Property(e => e.DietarySchoolId).HasColumnName("Dietary_School_ID");
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.HealthGoal)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("MAINTAIN");
            entity.Property(e => e.HeightCm)
                .HasColumnType("decimal(5, 1)")
                .HasColumnName("Height_Cm");
            entity.Property(e => e.IsStaleMetrics).HasColumnName("Is_Stale_Metrics");
            entity.Property(e => e.RegionPreference)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("SOUTH")
                .HasColumnName("Region_Preference");
            entity.Property(e => e.RequiresAlliumFree).HasColumnName("Requires_Allium_Free");
            entity.Property(e => e.TargetDailyCalories)
                .HasColumnType("decimal(6, 1)")
                .HasColumnName("Target_Daily_Calories");
            entity.Property(e => e.TargetWeightKg)
                .HasColumnType("decimal(5, 1)")
                .HasColumnName("Target_Weight_Kg");
            entity.Property(e => e.TdeeKcal)
                .HasColumnType("decimal(6, 1)")
                .HasColumnName("TDEE_Kcal");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Updated_At");

            entity.HasOne(d => d.User).WithOne(p => p.HealthProfile)
                .HasForeignKey<HealthProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HealthProfiles_Users");
        });

        modelBuilder.Entity<IngredientAllergen>(entity =>
        {
            entity.HasKey(e => new { e.IngredientId, e.AllergenId });

            entity.HasIndex(e => e.AllergenId, "IX_IngredientAllergens_Allergen_ID");

            entity.Property(e => e.IngredientId).HasColumnName("Ingredient_ID");
            entity.Property(e => e.AllergenId).HasColumnName("Allergen_ID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");

            entity.HasOne(d => d.Allergen).WithMany(p => p.IngredientAllergens)
                .HasForeignKey(d => d.AllergenId)
                .HasConstraintName("FK_IngredientAllergens_Allergens");
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId);

            entity.HasIndex(e => e.PartnerTransactionCode, "UQ_PaymentTransactions_Partner_Code").IsUnique();

            entity.Property(e => e.TransactionId).HasColumnName("Transaction_ID");
            entity.Property(e => e.AmountVnd)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("Amount_VND");
            entity.Property(e => e.CompletedAt).HasColumnName("Completed_At");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");
            entity.Property(e => e.EndsAt).HasColumnName("Ends_At");
            entity.Property(e => e.GatewayProvider)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("Gateway_Provider");
            entity.Property(e => e.PartnerTransactionCode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Partner_Transaction_Code");
            entity.Property(e => e.PlanId).HasColumnName("Plan_ID");
            entity.Property(e => e.ReceiptUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Receipt_URL");
            entity.Property(e => e.SignatureHash)
                .HasMaxLength(256)
                .IsUnicode(false)
                .HasColumnName("Signature_Hash");
            entity.Property(e => e.StartsAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Starts_At");
            entity.Property(e => e.TransactionStatus)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasDefaultValue("PENDING")
                .HasColumnName("Transaction_Status");
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.Plan).WithMany(p => p.PaymentTransactions)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PaymentTransactions_SubscriptionPlans");

            entity.HasOne(d => d.User).WithMany(p => p.PaymentTransactions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PaymentTransactions_Users");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(e => e.Barcode, "UQ_Products_Barcode").IsUnique();

            entity.Property(e => e.ProductId).HasColumnName("Product_ID");
            entity.Property(e => e.AdminVerified).HasColumnName("Admin_Verified");
            entity.Property(e => e.Barcode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Brand).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");
            entity.Property(e => e.IngredientsText).HasColumnName("Ingredients_Text");
            entity.Property(e => e.LastSyncedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Last_Synced_At");
            entity.Property(e => e.ProductName)
                .HasMaxLength(150)
                .HasColumnName("Product_Name");
            entity.Property(e => e.VeganVerdict)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("Vegan_Verdict");
        });

        modelBuilder.Entity<ProductAllergen>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.AllergenId });

            entity.Property(e => e.ProductId).HasColumnName("Product_ID");
            entity.Property(e => e.AllergenId).HasColumnName("Allergen_ID");
            entity.Property(e => e.PresenceType)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("Presence_Type");

            entity.HasOne(d => d.Allergen).WithMany(p => p.ProductAllergens)
                .HasForeignKey(d => d.AllergenId)
                .HasConstraintName("FK_ProductAllergens_Allergens");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductAllergens)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_ProductAllergens_Products");
        });

        modelBuilder.Entity<ProductEnumber>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.Code });

            entity.ToTable("Product_ENumbers");

            entity.Property(e => e.ProductId).HasColumnName("Product_ID");
            entity.Property(e => e.Code)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");

            entity.HasOne(d => d.CodeNavigation).WithMany(p => p.ProductEnumbers)
                .HasForeignKey(d => d.Code)
                .HasConstraintName("FK_Product_ENumbers_ENumberDictionary");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductEnumbers)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_Product_ENumbers_Products");
        });

        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.HasKey(e => e.PlanId);

            entity.HasIndex(e => e.PlanName, "UQ_SubscriptionPlans_Plan_Name").IsUnique();

            entity.Property(e => e.PlanId).HasColumnName("Plan_ID");
            entity.Property(e => e.AdminId).HasColumnName("Admin_ID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");
            entity.Property(e => e.DurationDays)
                .HasDefaultValue(30)
                .HasColumnName("Duration_Days");
            entity.Property(e => e.FeaturesJson).HasColumnName("Features_JSON");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("Is_Active");
            entity.Property(e => e.PlanName)
                .HasMaxLength(100)
                .HasColumnName("Plan_Name");
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt).HasColumnName("Updated_At");

            entity.HasOne(d => d.Admin).WithMany(p => p.SubscriptionPlans)
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SubscriptionPlans_Users");
        });

        modelBuilder.Entity<Survey>(entity =>
        {
            entity.Property(e => e.SurveyId).HasColumnName("Survey_ID");
            entity.Property(e => e.ContextQuestion)
                .HasMaxLength(255)
                .HasColumnName("Context_Question");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");
            entity.Property(e => e.FeedbackContent).HasColumnName("Feedback_Content");
            entity.Property(e => e.RatingScore).HasColumnName("Rating_Score");
            entity.Property(e => e.SessionId).HasColumnName("Session_ID");
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.Session).WithMany(p => p.Surveys)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Surveys_ChatSessions");

            entity.HasOne(d => d.User).WithMany(p => p.Surveys)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Surveys_Users");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email, "UQ_Users_Email").IsUnique();

            entity.HasIndex(e => e.GoogleId, "UQ_Users_Google_ID")
                .IsUnique()
                .HasFilter("([Google_ID] IS NOT NULL)");

            entity.HasIndex(e => e.PhoneNumber, "UQ_Users_Phone_Number")
                .IsUnique()
                .HasFilter("([Phone_Number] IS NOT NULL)");

            entity.Property(e => e.UserId).HasColumnName("User_ID");
            entity.Property(e => e.AccountStatus)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("PENDING_ACTIVATION")
                .HasColumnName("Account_Status");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Avatar_Url");
            entity.Property(e => e.CountViolationReports).HasColumnName("Count_ViolationReports");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.FailedLoginCount).HasColumnName("Failed_Login_Count");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("Full_Name");
            entity.Property(e => e.GoogleId)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("Google_ID");
            entity.Property(e => e.LastLoginAt).HasColumnName("Last_Login_At");
            entity.Property(e => e.LockedUntil).HasColumnName("Locked_Until");
            entity.Property(e => e.PasswordHash).HasColumnName("Password_Hash");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("Phone_Number");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("MEMBER");
            entity.Property(e => e.UpdatedAt).HasColumnName("Updated_At");
        });

        modelBuilder.Entity<UserAllergy>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.AllergenId });

            entity.Property(e => e.UserId).HasColumnName("User_ID");
            entity.Property(e => e.AllergenId).HasColumnName("Allergen_ID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");
            entity.Property(e => e.Notes).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Updated_At");

            entity.HasOne(d => d.Allergen).WithMany(p => p.UserAllergies)
                .HasForeignKey(d => d.AllergenId)
                .HasConstraintName("FK_UserAllergies_Allergens");

            entity.HasOne(d => d.User).WithMany(p => p.UserAllergies)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_UserAllergies_Users");
        });

        modelBuilder.Entity<UserScanHistory>(entity =>
        {
            entity.HasKey(e => e.ScanId);

            entity.ToTable("UserScanHistory");

            entity.Property(e => e.ScanId).HasColumnName("Scan_ID");
            entity.Property(e => e.GuestUuid)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasColumnName("Guest_UUID");
            entity.Property(e => e.IsNonVeganTriggered).HasColumnName("Is_Non_Vegan_Triggered");
            entity.Property(e => e.ProductId).HasColumnName("Product_ID");
            entity.Property(e => e.ScanMethod)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("Scan_Method");
            entity.Property(e => e.ScannedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Scanned_At");
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.Product).WithMany(p => p.UserScanHistories)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_UserScanHistory_Products");

            entity.HasOne(d => d.User).WithMany(p => p.UserScanHistories)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_UserScanHistory_Users");
        });

        modelBuilder.Entity<UserWeightHistory>(entity =>
        {
            entity.HasKey(e => e.LogId);

            entity.ToTable("UserWeightHistory");

            entity.Property(e => e.LogId).HasColumnName("Log_ID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");
            entity.Property(e => e.LoggedDate).HasColumnName("Logged_Date");
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.UserId).HasColumnName("User_ID");
            entity.Property(e => e.WeightKg)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("Weight_Kg");

            entity.HasOne(d => d.User).WithMany(p => p.UserWeightHistories)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserWeightHistory_Users");
        });

        modelBuilder.Entity<Vote>(entity =>
        {
            entity.HasIndex(e => e.PostId, "IX_Votes_Post_ID");

            entity.HasIndex(e => new { e.UserId, e.PostId }, "UQ_Votes_User_Post").IsUnique();

            entity.Property(e => e.VoteId).HasColumnName("Vote_ID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");
            entity.Property(e => e.PostId).HasColumnName("Post_ID");
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.User).WithMany(p => p.Votes)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Votes_Users");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
