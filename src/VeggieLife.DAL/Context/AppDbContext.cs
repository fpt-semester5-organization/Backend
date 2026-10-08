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

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<ChatMessage> ChatMessages { get; set; }

    public virtual DbSet<ChatSession> ChatSessions { get; set; }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<DietarySchool> DietarySchools { get; set; }

    public virtual DbSet<EnumberDictionary> EnumberDictionaries { get; set; }

    public virtual DbSet<FoodCategory> FoodCategories { get; set; }

    public virtual DbSet<FridgeInventory> FridgeInventories { get; set; }

    public virtual DbSet<FridgeSession> FridgeSessions { get; set; }

    public virtual DbSet<HealthProfile> HealthProfiles { get; set; }

    public virtual DbSet<Ingredient> Ingredients { get; set; }

    public virtual DbSet<IngredientAllergen> IngredientAllergens { get; set; }

    public virtual DbSet<MealPlan> MealPlans { get; set; }

    public virtual DbSet<MealSlot> MealSlots { get; set; }

    public virtual DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    public virtual DbSet<Post> Posts { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductAllergen> ProductAllergens { get; set; }

    public virtual DbSet<ProductEnumber> ProductEnumbers { get; set; }

    public virtual DbSet<Recipe> Recipes { get; set; }

    public virtual DbSet<RecipeIngredient> RecipeIngredients { get; set; }

    public virtual DbSet<ShoppingList> ShoppingLists { get; set; }

    public virtual DbSet<ShoppingListItem> ShoppingListItems { get; set; }

    public virtual DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

    public virtual DbSet<Survey> Surveys { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserAllergy> UserAllergies { get; set; }

    public virtual DbSet<UserScanHistory> UserScanHistories { get; set; }

    public virtual DbSet<UserWeightHistory> UserWeightHistories { get; set; }

    public virtual DbSet<VeganVenue> VeganVenues { get; set; }

    public virtual DbSet<VenueCategory> VenueCategories { get; set; }

    public virtual DbSet<VenueMenuItem> VenueMenuItems { get; set; }

    public virtual DbSet<Video> Videos { get; set; }

    public virtual DbSet<VideoTranscript> VideoTranscripts { get; set; }

    public virtual DbSet<ViolationReport> ViolationReports { get; set; }

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

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditId);

            entity.HasIndex(e => e.ActorUserId, "IX_AuditLogs_Actor_User_ID");

            entity.HasIndex(e => new { e.TargetTable, e.TargetId }, "IX_AuditLogs_Target");

            entity.Property(e => e.AuditId).HasColumnName("Audit_ID");
            entity.Property(e => e.Action)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ActorRole)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("Actor_Role");
            entity.Property(e => e.ActorUserId).HasColumnName("Actor_User_ID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");
            entity.Property(e => e.NewValue).HasColumnName("New_Value");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.OldValue).HasColumnName("Old_Value");
            entity.Property(e => e.TargetId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Target_ID");
            entity.Property(e => e.TargetTable)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Target_Table");

            entity.HasOne(d => d.ActorUser).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.ActorUserId)
                .HasConstraintName("FK_AuditLogs_Users");
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

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasIndex(e => e.ParentCommentId, "IX_Comments_Parent_Comment_ID");

            entity.HasIndex(e => e.PostId, "IX_Comments_Post_ID");

            entity.Property(e => e.CommentId).HasColumnName("Comment_ID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");
            entity.Property(e => e.IsSoftDeleted).HasColumnName("Is_Soft_Deleted");
            entity.Property(e => e.ParentCommentId).HasColumnName("Parent_Comment_ID");
            entity.Property(e => e.PostId).HasColumnName("Post_ID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVE");
            entity.Property(e => e.UpdatedAt).HasColumnName("Updated_At");
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.ParentComment).WithMany(p => p.InverseParentComment)
                .HasForeignKey(d => d.ParentCommentId)
                .HasConstraintName("FK_Comments_Parent");

            entity.HasOne(d => d.Post).WithMany(p => p.Comments)
                .HasForeignKey(d => d.PostId)
                .HasConstraintName("FK_Comments_Posts");

            entity.HasOne(d => d.User).WithMany(p => p.Comments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Comments_Users");
        });

        modelBuilder.Entity<DietarySchool>(entity =>
        {
            entity.HasIndex(e => e.SchoolCode, "UQ_DietarySchools_School_Code").IsUnique();

            entity.Property(e => e.DietarySchoolId).HasColumnName("Dietary_School_ID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");
            entity.Property(e => e.ForbiddenFoodRules).HasColumnName("Forbidden_Food_Rules");
            entity.Property(e => e.SchoolCode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("School_Code");
            entity.Property(e => e.SchoolName)
                .HasMaxLength(100)
                .HasColumnName("School_Name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Updated_At");
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

        modelBuilder.Entity<FoodCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId);

            entity.HasIndex(e => e.CategoryName, "UQ_FoodCategories_Category_Name").IsUnique();

            entity.Property(e => e.CategoryId).HasColumnName("Category_ID");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(100)
                .HasColumnName("Category_Name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("Is_Active");
            entity.Property(e => e.IsCore).HasColumnName("Is_Core");
            entity.Property(e => e.UpdatedAt).HasColumnName("Updated_At");
        });

        modelBuilder.Entity<FridgeInventory>(entity =>
        {
            entity.HasKey(e => e.InventoryId);

            entity.ToTable("FridgeInventory");

            entity.HasIndex(e => e.UserId, "IX_FridgeInventory_User_ID");

            entity.Property(e => e.InventoryId).HasColumnName("Inventory_ID");
            entity.Property(e => e.CustomItemName)
                .HasMaxLength(100)
                .HasColumnName("Custom_Item_Name");
            entity.Property(e => e.FreshnessStatus)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasDefaultValue("FRESH")
                .HasColumnName("Freshness_Status");
            entity.Property(e => e.IngredientId).HasColumnName("Ingredient_ID");
            entity.Property(e => e.SessionId).HasColumnName("Session_ID");
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.Ingredient).WithMany(p => p.FridgeInventories)
                .HasForeignKey(d => d.IngredientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FridgeInventory_Ingredients");

            entity.HasOne(d => d.Session).WithMany(p => p.FridgeInventories)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK_FridgeInventory_FridgeSessions");

            entity.HasOne(d => d.User).WithMany(p => p.FridgeInventories)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FridgeInventory_Users");
        });

        modelBuilder.Entity<FridgeSession>(entity =>
        {
            entity.HasKey(e => e.SessionId);

            entity.HasIndex(e => e.UserId, "IX_FridgeSessions_User_ID");

            entity.Property(e => e.SessionId).HasColumnName("Session_ID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");
            entity.Property(e => e.DetectedItemsCount).HasColumnName("Detected_Items_Count");
            entity.Property(e => e.ImageCdnUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Image_CDN_URL");
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.User).WithMany(p => p.FridgeSessions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FridgeSessions_Users");
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

            entity.HasOne(d => d.DietarySchool).WithMany(p => p.HealthProfiles)
                .HasForeignKey(d => d.DietarySchoolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HealthProfiles_DietarySchools");

            entity.HasOne(d => d.User).WithOne(p => p.HealthProfile)
                .HasForeignKey<HealthProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HealthProfiles_Users");
        });

        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.HasIndex(e => e.IngredientName, "UQ_Ingredients_Ingredient_Name").IsUnique();

            entity.Property(e => e.IngredientId).HasColumnName("Ingredient_ID");
            entity.Property(e => e.CaloriesPer100g)
                .HasColumnType("decimal(6, 1)")
                .HasColumnName("Calories_Per_100g");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");
            entity.Property(e => e.DefaultStallType)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("Default_Stall_Type");
            entity.Property(e => e.FoodGroup)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("PLANT")
                .HasColumnName("Food_Group");
            entity.Property(e => e.IngredientName)
                .HasMaxLength(100)
                .HasColumnName("Ingredient_Name");
            entity.Property(e => e.IsAllium).HasColumnName("Is_Allium");
            entity.Property(e => e.StandardUnit)
                .HasMaxLength(20)
                .HasDefaultValue("g")
                .HasColumnName("Standard_Unit");
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

            entity.HasOne(d => d.Ingredient).WithMany(p => p.IngredientAllergens)
                .HasForeignKey(d => d.IngredientId)
                .HasConstraintName("FK_IngredientAllergens_Ingredients");
        });

        modelBuilder.Entity<MealPlan>(entity =>
        {
            entity.HasKey(e => e.PlanId);

            entity.HasIndex(e => e.UserId, "IX_MealPlans_User_ID");

            entity.Property(e => e.PlanId).HasColumnName("Plan_ID");
            entity.Property(e => e.BudgetTier)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasDefaultValue("STANDARD")
                .HasColumnName("Budget_Tier");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");
            entity.Property(e => e.CustomInstructions)
                .HasMaxLength(200)
                .HasColumnName("Custom_Instructions");
            entity.Property(e => e.DietarySchoolId).HasColumnName("Dietary_School_ID");
            entity.Property(e => e.EndDate).HasColumnName("End_Date");
            entity.Property(e => e.GeneratedByAi).HasColumnName("Generated_By_AI");
            entity.Property(e => e.PlanName)
                .HasMaxLength(100)
                .HasColumnName("Plan_Name");
            entity.Property(e => e.PlanStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("DRAFT")
                .HasColumnName("Plan_Status");
            entity.Property(e => e.StartDate).HasColumnName("Start_Date");
            entity.Property(e => e.TargetDailyKcal)
                .HasColumnType("decimal(6, 1)")
                .HasColumnName("Target_Daily_Kcal");
            entity.Property(e => e.UserId).HasColumnName("User_ID");

            entity.HasOne(d => d.DietarySchool).WithMany(p => p.MealPlans)
                .HasForeignKey(d => d.DietarySchoolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MealPlans_DietarySchools");

            entity.HasOne(d => d.User).WithMany(p => p.MealPlans)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MealPlans_Users");
        });

        modelBuilder.Entity<MealSlot>(entity =>
        {
            entity.HasKey(e => e.SlotId);

            entity.HasIndex(e => e.PlanId, "IX_MealSlots_Plan_ID");

            entity.Property(e => e.SlotId).HasColumnName("Slot_ID");
            entity.Property(e => e.DayOfWeek)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("Day_Of_Week");
            entity.Property(e => e.IsCompleted).HasColumnName("Is_Completed");
            entity.Property(e => e.MealType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("Meal_Type");
            entity.Property(e => e.Notes).HasMaxLength(200);
            entity.Property(e => e.PlanId).HasColumnName("Plan_ID");
            entity.Property(e => e.RecipeId).HasColumnName("Recipe_ID");

            entity.HasOne(d => d.Plan).WithMany(p => p.MealSlots)
                .HasForeignKey(d => d.PlanId)
                .HasConstraintName("FK_MealSlots_MealPlans");

            entity.HasOne(d => d.Recipe).WithMany(p => p.MealSlots)
                .HasForeignKey(d => d.RecipeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MealSlots_Recipes");
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

        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasIndex(e => e.ModerationStatus, "IX_Posts_Moderation_Status");

            entity.HasIndex(e => e.UserId, "IX_Posts_User_ID");

            entity.Property(e => e.PostId).HasColumnName("Post_ID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Image_Url");
            entity.Property(e => e.ModerationStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("PENDING_REVIEW")
                .HasColumnName("Moderation_Status");
            entity.Property(e => e.RecipeId).HasColumnName("Recipe_ID");
            entity.Property(e => e.Title).HasMaxLength(150);
            entity.Property(e => e.UpdatedAt).HasColumnName("Updated_At");
            entity.Property(e => e.UserId).HasColumnName("User_ID");
            entity.Property(e => e.ViewCount).HasColumnName("View_Count");
            entity.Property(e => e.Visibility)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("PUBLIC");

            entity.HasOne(d => d.Recipe).WithMany(p => p.Posts)
                .HasForeignKey(d => d.RecipeId)
                .HasConstraintName("FK_Posts_Recipes");

            entity.HasOne(d => d.User).WithMany(p => p.Posts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Posts_Users");
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

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.HasIndex(e => e.CategoryId, "IX_Recipes_Category_ID");

            entity.HasIndex(e => e.DietarySchoolId, "IX_Recipes_Dietary_School_ID");

            entity.HasIndex(e => e.Status, "IX_Recipes_Status");

            entity.Property(e => e.RecipeId).HasColumnName("Recipe_ID");
            entity.Property(e => e.CaloriesKcal)
                .HasColumnType("decimal(6, 1)")
                .HasColumnName("Calories_Kcal");
            entity.Property(e => e.CategoryId).HasColumnName("Category_ID");
            entity.Property(e => e.CookTimeMinutes)
                .HasDefaultValue(20)
                .HasColumnName("Cook_Time_Minutes");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.DietarySchoolId).HasColumnName("Dietary_School_ID");
            entity.Property(e => e.DifficultyLevel)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("MEDIUM")
                .HasColumnName("Difficulty_Level");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Image_Url");
            entity.Property(e => e.IsVerified).HasColumnName("Is_Verified");
            entity.Property(e => e.PrepTimeMinutes)
                .HasDefaultValue(15)
                .HasColumnName("Prep_Time_Minutes");
            entity.Property(e => e.Servings).HasDefaultValue(2);
            entity.Property(e => e.Source)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("USER");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("DRAFT");
            entity.Property(e => e.Title).HasMaxLength(150);
            entity.Property(e => e.UpdatedAt).HasColumnName("Updated_At");
            entity.Property(e => e.UserId).HasColumnName("User_ID");
            entity.Property(e => e.ViewCount).HasColumnName("View_Count");

            entity.HasOne(d => d.Category).WithMany(p => p.Recipes)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Recipes_FoodCategories");

            entity.HasOne(d => d.DietarySchool).WithMany(p => p.Recipes)
                .HasForeignKey(d => d.DietarySchoolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Recipes_DietarySchools");

            entity.HasOne(d => d.User).WithMany(p => p.Recipes)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Recipes_Users");
        });

        modelBuilder.Entity<RecipeIngredient>(entity =>
        {
            entity.HasKey(e => new { e.RecipeId, e.IngredientId });

            entity.HasIndex(e => e.IngredientId, "IX_RecipeIngredients_Ingredient_ID");

            entity.Property(e => e.RecipeId).HasColumnName("Recipe_ID");
            entity.Property(e => e.IngredientId).HasColumnName("Ingredient_ID");
            entity.Property(e => e.IsOptional).HasColumnName("Is_Optional");
            entity.Property(e => e.PreparationNote)
                .HasMaxLength(150)
                .HasColumnName("Preparation_Note");
            entity.Property(e => e.QuantityNeeded)
                .HasColumnType("decimal(8, 2)")
                .HasColumnName("Quantity_Needed");
            entity.Property(e => e.Unit).HasMaxLength(20);

            entity.HasOne(d => d.Ingredient).WithMany(p => p.RecipeIngredients)
                .HasForeignKey(d => d.IngredientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RecipeIngredients_Ingredients");

            entity.HasOne(d => d.Recipe).WithMany(p => p.RecipeIngredients)
                .HasForeignKey(d => d.RecipeId)
                .HasConstraintName("FK_RecipeIngredients_Recipes");
        });

        modelBuilder.Entity<ShoppingList>(entity =>
        {
            entity.HasKey(e => e.ListId);

            entity.HasIndex(e => e.PlanId, "IX_ShoppingLists_Plan_ID");

            entity.Property(e => e.ListId).HasColumnName("List_ID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");
            entity.Property(e => e.IsOfflineSynced).HasColumnName("Is_Offline_Synced");
            entity.Property(e => e.ListTitle)
                .HasMaxLength(150)
                .HasColumnName("List_Title");
            entity.Property(e => e.PlanId).HasColumnName("Plan_ID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.TargetShoppingDate).HasColumnName("Target_Shopping_Date");

            entity.HasOne(d => d.Plan).WithMany(p => p.ShoppingLists)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShoppingLists_MealPlans");
        });

        modelBuilder.Entity<ShoppingListItem>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity.HasIndex(e => e.ListId, "IX_ShoppingListItems_List_ID");

            entity.Property(e => e.ItemId).HasColumnName("Item_ID");
            entity.Property(e => e.AggregatedQuantity)
                .HasColumnType("decimal(8, 2)")
                .HasColumnName("Aggregated_Quantity");
            entity.Property(e => e.IngredientId).HasColumnName("Ingredient_ID");
            entity.Property(e => e.IsPurchased).HasColumnName("Is_Purchased");
            entity.Property(e => e.ListId).HasColumnName("List_ID");
            entity.Property(e => e.Unit).HasMaxLength(20);

            entity.HasOne(d => d.Ingredient).WithMany(p => p.ShoppingListItems)
                .HasForeignKey(d => d.IngredientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShoppingListItems_Ingredients");

            entity.HasOne(d => d.List).WithMany(p => p.ShoppingListItems)
                .HasForeignKey(d => d.ListId)
                .HasConstraintName("FK_ShoppingListItems_ShoppingLists");
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

        modelBuilder.Entity<VeganVenue>(entity =>
        {
            entity.HasKey(e => e.VenueId);

            entity.HasIndex(e => new { e.Latitude, e.Longitude }, "IX_VeganVenues_Coordinates");

            entity.HasIndex(e => e.ExternalPlaceId, "UQ_VeganVenues_External_Place_ID")
                .IsUnique()
                .HasFilter("([External_Place_ID] IS NOT NULL)");

            entity.Property(e => e.VenueId).HasColumnName("Venue_ID");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");
            entity.Property(e => e.ExternalPlaceId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("External_Place_ID");
            entity.Property(e => e.ExternalSource)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("External_Source");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("Is_Active");
            entity.Property(e => e.IsFullyVegan)
                .HasDefaultValue(true)
                .HasColumnName("Is_Fully_Vegan");
            entity.Property(e => e.Latitude).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.Rating).HasColumnType("decimal(2, 1)");
            entity.Property(e => e.RawOriginJson).HasColumnName("Raw_Origin_JSON");
            entity.Property(e => e.ThumbnailUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Thumbnail_Url");
            entity.Property(e => e.VenueName)
                .HasMaxLength(150)
                .HasColumnName("Venue_Name");
        });

        modelBuilder.Entity<VenueCategory>(entity =>
        {
            entity.HasKey(e => new { e.CategoryId, e.VenueId });

            entity.HasIndex(e => e.VenueId, "IX_VenueCategories_Venue_ID");

            entity.Property(e => e.CategoryId).HasColumnName("Category_ID");
            entity.Property(e => e.VenueId).HasColumnName("Venue_ID");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Assigned_At");
            entity.Property(e => e.IsPrimary).HasColumnName("Is_Primary");

            entity.HasOne(d => d.Category).WithMany(p => p.VenueCategories)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VenueCategories_FoodCategories");

            entity.HasOne(d => d.Venue).WithMany(p => p.VenueCategories)
                .HasForeignKey(d => d.VenueId)
                .HasConstraintName("FK_VenueCategories_VeganVenues");
        });

        modelBuilder.Entity<VenueMenuItem>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity.HasIndex(e => e.VenueId, "IX_VenueMenuItems_Venue_ID");

            entity.Property(e => e.ItemId).HasColumnName("Item_ID");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DishName)
                .HasMaxLength(150)
                .HasColumnName("Dish_Name");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Image_Url");
            entity.Property(e => e.IsSignature).HasColumnName("Is_Signature");
            entity.Property(e => e.PriceVnd)
                .HasColumnType("decimal(12, 0)")
                .HasColumnName("Price_VND");
            entity.Property(e => e.VenueId).HasColumnName("Venue_ID");

            entity.HasOne(d => d.Venue).WithMany(p => p.VenueMenuItems)
                .HasForeignKey(d => d.VenueId)
                .HasConstraintName("FK_VenueMenuItems_VeganVenues");
        });

        modelBuilder.Entity<Video>(entity =>
        {
            entity.HasIndex(e => e.PostId, "IX_Videos_Post_ID");

            entity.Property(e => e.VideoId).HasColumnName("Video_ID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");
            entity.Property(e => e.DurationSeconds).HasColumnName("Duration_Seconds");
            entity.Property(e => e.PostId).HasColumnName("Post_ID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("PROCESSING");
            entity.Property(e => e.ThumbnailUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Thumbnail_Url");
            entity.Property(e => e.Title).HasMaxLength(150);
            entity.Property(e => e.UpdatedAt).HasColumnName("Updated_At");
            entity.Property(e => e.VideoUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Video_Url");
            entity.Property(e => e.ViewCount).HasColumnName("View_Count");

            entity.HasOne(d => d.Post).WithMany(p => p.Videos)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Videos_Posts");
        });

        modelBuilder.Entity<VideoTranscript>(entity =>
        {
            entity.HasKey(e => e.TranscriptId);

            entity.HasIndex(e => e.RecipeId, "IX_VideoTranscripts_Recipe_ID");

            entity.HasIndex(e => e.VideoId, "UQ_VideoTranscripts_Video_ID").IsUnique();

            entity.Property(e => e.TranscriptId).HasColumnName("Transcript_ID");
            entity.Property(e => e.CompletedAt).HasColumnName("Completed_At");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");
            entity.Property(e => e.ExtractedRecipeJson).HasColumnName("Extracted_Recipe_JSON");
            entity.Property(e => e.RawTranscript).HasColumnName("Raw_Transcript");
            entity.Property(e => e.RecipeId).HasColumnName("Recipe_ID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.SttModelVersion)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("STT_Model_Version");
            entity.Property(e => e.VideoId).HasColumnName("Video_ID");

            entity.HasOne(d => d.Recipe).WithMany(p => p.VideoTranscripts)
                .HasForeignKey(d => d.RecipeId)
                .HasConstraintName("FK_VideoTranscripts_Recipes");

            entity.HasOne(d => d.Video).WithOne(p => p.VideoTranscript)
                .HasForeignKey<VideoTranscript>(d => d.VideoId)
                .HasConstraintName("FK_VideoTranscripts_Videos");
        });

        modelBuilder.Entity<ViolationReport>(entity =>
        {
            entity.HasKey(e => e.ReportId);

            entity.HasIndex(e => e.Status, "IX_ViolationReports_Status");

            entity.Property(e => e.ReportId).HasColumnName("Report_ID");
            entity.Property(e => e.AdminId).HasColumnName("Admin_ID");
            entity.Property(e => e.CommentId).HasColumnName("Comment_ID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("Created_At");
            entity.Property(e => e.EvidenceSummary)
                .HasMaxLength(500)
                .HasColumnName("Evidence_Summary");
            entity.Property(e => e.PostId).HasColumnName("Post_ID");
            entity.Property(e => e.RecipeId).HasColumnName("Recipe_ID");
            entity.Property(e => e.ReportSource)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("USER_REPORT")
                .HasColumnName("Report_Source");
            entity.Property(e => e.ReporterUserId).HasColumnName("Reporter_User_ID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.VideoId).HasColumnName("Video_ID");
            entity.Property(e => e.ViolationType)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("Violation_Type");

            entity.HasOne(d => d.Admin).WithMany(p => p.ViolationReportAdmins)
                .HasForeignKey(d => d.AdminId)
                .HasConstraintName("FK_ViolationReports_Admin");

            entity.HasOne(d => d.Comment).WithMany(p => p.ViolationReports)
                .HasForeignKey(d => d.CommentId)
                .HasConstraintName("FK_ViolationReports_Comments");

            entity.HasOne(d => d.Post).WithMany(p => p.ViolationReports)
                .HasForeignKey(d => d.PostId)
                .HasConstraintName("FK_ViolationReports_Posts");

            entity.HasOne(d => d.Recipe).WithMany(p => p.ViolationReports)
                .HasForeignKey(d => d.RecipeId)
                .HasConstraintName("FK_ViolationReports_Recipes");

            entity.HasOne(d => d.ReporterUser).WithMany(p => p.ViolationReportReporterUsers)
                .HasForeignKey(d => d.ReporterUserId)
                .HasConstraintName("FK_ViolationReports_Reporter");

            entity.HasOne(d => d.Video).WithMany(p => p.ViolationReports)
                .HasForeignKey(d => d.VideoId)
                .HasConstraintName("FK_ViolationReports_Videos");
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

            entity.HasOne(d => d.Post).WithMany(p => p.Votes)
                .HasForeignKey(d => d.PostId)
                .HasConstraintName("FK_Votes_Posts");

            entity.HasOne(d => d.User).WithMany(p => p.Votes)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Votes_Users");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
