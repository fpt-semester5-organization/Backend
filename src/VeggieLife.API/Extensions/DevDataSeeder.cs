using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Services.AI;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.API.Extensions;

/// <summary>
/// Tiện ích tự động nạp sẵn dữ liệu ban đầu (Dev Seed Data) khi chạy ứng dụng
/// ở chế độ InMemory hoặc database mới, phục vụ test ngay trên Swagger.
/// </summary>
public static class DevDataSeeder
{
    private const string DefaultPasswordHash = "$2a$11$GBRajsAbFfn4G4UI7bhu.O8MGs4mXfn/clcozGAZ0npIHYrlShxwO"; // Veggie@2026

    /// <summary>
    /// Vá các cột thiếu trong CSDL vật lý (nếu có) để tránh lỗi Invalid column name khi chạy với CSDL cũ chưa cập nhật script mới.
    /// </summary>
    public static void ApplySchemaPatches(AppDbContext db)
    {
        if (db.Database.ProviderName?.Contains("SqlServer") == true)
        {
            try
            {
                db.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Products' AND COLUMN_NAME = 'Image_Url')
                    BEGIN
                        ALTER TABLE dbo.Products ADD Image_Url VARCHAR(500) NULL;
                    END

                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'MealPlans' AND COLUMN_NAME = 'Max_Swaps_Allowed')
                    BEGIN
                        ALTER TABLE dbo.MealPlans ADD Max_Swaps_Allowed INT NOT NULL CONSTRAINT DF_MealPlans_Max_Swaps DEFAULT (21);
                    END

                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'MealPlans' AND COLUMN_NAME = 'Swaps_Remaining')
                    BEGIN
                        ALTER TABLE dbo.MealPlans ADD Swaps_Remaining INT NOT NULL CONSTRAINT DF_MealPlans_Swaps_Remaining DEFAULT (21);
                    END

                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'MealPlans' AND COLUMN_NAME = 'Duration_Days')
                    BEGIN
                        ALTER TABLE dbo.MealPlans ADD Duration_Days INT NOT NULL CONSTRAINT DF_MealPlans_Duration_Days DEFAULT (7);
                    END

                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MealSlotSwapHistories]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [dbo].[MealSlotSwapHistories] (
                            [Swap_History_ID]       INT IDENTITY(1,1) NOT NULL,
                            [Slot_ID]               INT NOT NULL,
                            [Old_Recipe_ID]         INT NOT NULL,
                            [New_Recipe_ID]         INT NOT NULL,
                            [Swap_Reason]           NVARCHAR(255) NULL,
                            [Calories_Delta]        DECIMAL(6,1) NOT NULL,
                            [Swapped_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_MealSlotSwapHistories_Swapped_At] DEFAULT (SYSDATETIME()),

                            CONSTRAINT [PK_MealSlotSwapHistories] PRIMARY KEY CLUSTERED ([Swap_History_ID] ASC),
                            CONSTRAINT [FK_MealSlotSwapHistories_MealSlots] FOREIGN KEY ([Slot_ID]) REFERENCES [dbo].[MealSlots]([Slot_ID]) ON DELETE CASCADE,
                            CONSTRAINT [FK_MealSlotSwapHistories_OldRecipe] FOREIGN KEY ([Old_Recipe_ID]) REFERENCES [dbo].[Recipes]([Recipe_ID]),
                            CONSTRAINT [FK_MealSlotSwapHistories_NewRecipe] FOREIGN KEY ([New_Recipe_ID]) REFERENCES [dbo].[Recipes]([Recipe_ID])
                        );

                        CREATE NONCLUSTERED INDEX [IX_MealSlotSwapHistories_Slot_ID] ON [dbo].[MealSlotSwapHistories]([Slot_ID] ASC);
                    END

                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'FridgeInventory' AND COLUMN_NAME = 'Quantity')
                    BEGIN
                        ALTER TABLE dbo.FridgeInventory ADD Quantity DECIMAL(8,2) NOT NULL CONSTRAINT DF_FridgeInventory_Quantity DEFAULT (1.00);
                    END

                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'FridgeInventory' AND COLUMN_NAME = 'Unit')
                    BEGIN
                        ALTER TABLE dbo.FridgeInventory ADD Unit NVARCHAR(30) NOT NULL CONSTRAINT DF_FridgeInventory_Unit DEFAULT (N'phần');
                    END

                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Foods]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [dbo].[Foods] (
                            [Food_ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [Category_ID] INT NOT NULL,
                            [Food_Name] NVARCHAR(150) NOT NULL UNIQUE,
                            [Created_At] DATETIME2(7) NOT NULL DEFAULT (SYSDATETIME()),
                            [Updated_At] DATETIME2(7) NULL
                        );
                    END

                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Recipes' AND COLUMN_NAME = 'Food_ID')
                    BEGIN
                        ALTER TABLE dbo.Recipes ADD Food_ID INT NULL;
                    END

                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SystemSettings' AND COLUMN_NAME = 'Legal_Documents_Url')
                    BEGIN
                        ALTER TABLE dbo.SystemSettings ADD Legal_Documents_Url NVARCHAR(MAX) NULL;
                    END

                    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SystemSettings' AND COLUMN_NAME = 'Setting_Value' AND CHARACTER_MAXIMUM_LENGTH = 500)
                    BEGIN
                        ALTER TABLE dbo.SystemSettings ALTER COLUMN Setting_Value NVARCHAR(2000) NOT NULL;
                        ALTER TABLE dbo.SystemSettings ALTER COLUMN Default_Value NVARCHAR(2000) NOT NULL;
                    END
                ");
            }
            catch
            {
                // Bỏ qua nếu không có quyền DDL hoặc cột đã tồn tại
            }
        }
    }

    public static void SeedIfEmpty(AppDbContext db)
    {
        ApplySchemaPatches(db);

        // 1. Nạp Trường phái ăn chay (DietarySchools)
        if (!db.DietarySchools.Any())
        {
            db.DietarySchools.AddRange(
                new DietarySchool
                {
                    DietarySchoolId = 1,
                    SchoolName = "Thuần chay (Vegan)",
                    SchoolCode = "VEGAN",
                    ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"EGG\",\"DAIRY\",\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":false}"
                },
                new DietarySchool
                {
                    DietarySchoolId = 2,
                    SchoolName = "Chay có trứng (Ovo)",
                    SchoolCode = "OVO",
                    ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"DAIRY\",\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":false}"
                },
                new DietarySchool
                {
                    DietarySchoolId = 3,
                    SchoolName = "Chay có sữa (Lacto)",
                    SchoolCode = "LACTO",
                    ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"EGG\",\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":false}"
                },
                new DietarySchool
                {
                    DietarySchoolId = 4,
                    SchoolName = "Chay có trứng và sữa (Lacto-Ovo)",
                    SchoolCode = "LACTO_OVO",
                    ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":false}"
                },
                new DietarySchool
                {
                    DietarySchoolId = 5,
                    SchoolName = "Chay Phật giáo (Buddhist)",
                    SchoolCode = "BUDDHIST",
                    ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"EGG\",\"DAIRY\",\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":true}"
                },
                new DietarySchool
                {
                    DietarySchoolId = 6,
                    SchoolName = "Chay bán phần gia cầm (Pollotarian)",
                    SchoolCode = "POLLOTARIAN",
                    ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"SEAFOOD\"],\"forbidAllium\":false}"
                },
                new DietarySchool
                {
                    DietarySchoolId = 7,
                    SchoolName = "Chay bán phần hải sản (Pescatarian)",
                    SchoolCode = "PESCATARIAN",
                    ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"POULTRY\"],\"forbidAllium\":false}"
                }
            );
            db.SaveChanges();
        }

        // 2. Nạp Danh mục nguyên liệu chuẩn (Ingredients)
        if (!db.Ingredients.Any())
        {
            db.Ingredients.AddRange(
                new Ingredient
                {
                    IngredientId = 1,
                    IngredientName = "Đậu hũ non (Silken Tofu)",
                    StandardUnit = "miếng",
                    DefaultStallType = "PLANT_PROTEIN",
                    CaloriesPer100g = 55.0m,
                    IsAllium = false,
                    FoodGroup = "SOY"
                },
                new Ingredient
                {
                    IngredientId = 2,
                    IngredientName = "Đậu hũ non",
                    StandardUnit = "miếng",
                    DefaultStallType = "PLANT_PROTEIN",
                    CaloriesPer100g = 55.0m,
                    IsAllium = false,
                    FoodGroup = "SOY"
                },
                new Ingredient
                {
                    IngredientId = 3,
                    IngredientName = "Đậu hũ trắng",
                    StandardUnit = "miếng",
                    DefaultStallType = "PLANT_PROTEIN",
                    CaloriesPer100g = 76.0m,
                    IsAllium = false,
                    FoodGroup = "SOY"
                },
                new Ingredient
                {
                    IngredientId = 4,
                    IngredientName = "Nấm rơm",
                    StandardUnit = "g",
                    DefaultStallType = "PRODUCE",
                    CaloriesPer100g = 32.0m,
                    IsAllium = false,
                    FoodGroup = "MUSHROOM"
                },
                new Ingredient
                {
                    IngredientId = 5,
                    IngredientName = "Nấm đùi gà",
                    StandardUnit = "g",
                    DefaultStallType = "PRODUCE",
                    CaloriesPer100g = 35.0m,
                    IsAllium = false,
                    FoodGroup = "MUSHROOM"
                },
                new Ingredient
                {
                    IngredientId = 6,
                    IngredientName = "Nấm kim châm",
                    StandardUnit = "g",
                    DefaultStallType = "PRODUCE",
                    CaloriesPer100g = 37.0m,
                    IsAllium = false,
                    FoodGroup = "MUSHROOM"
                },
                new Ingredient
                {
                    IngredientId = 7,
                    IngredientName = "Cà chua",
                    StandardUnit = "quả",
                    DefaultStallType = "PRODUCE",
                    CaloriesPer100g = 18.0m,
                    IsAllium = false,
                    FoodGroup = "VEGETABLE"
                },
                new Ingredient
                {
                    IngredientId = 8,
                    IngredientName = "Cà rốt",
                    StandardUnit = "củ",
                    DefaultStallType = "PRODUCE",
                    CaloriesPer100g = 41.0m,
                    IsAllium = false,
                    FoodGroup = "VEGETABLE"
                },
                new Ingredient
                {
                    IngredientId = 9,
                    IngredientName = "Rau cải xanh",
                    StandardUnit = "g",
                    DefaultStallType = "PRODUCE",
                    CaloriesPer100g = 20.0m,
                    IsAllium = false,
                    FoodGroup = "VEGETABLE"
                },
                new Ingredient
                {
                    IngredientId = 10,
                    IngredientName = "Cải thìa (Bok choy)",
                    StandardUnit = "g",
                    DefaultStallType = "PRODUCE",
                    CaloriesPer100g = 13.0m,
                    IsAllium = false,
                    FoodGroup = "VEGETABLE"
                },
                new Ingredient
                {
                    IngredientId = 11,
                    IngredientName = "Bắp cải trắng",
                    StandardUnit = "g",
                    DefaultStallType = "PRODUCE",
                    CaloriesPer100g = 25.0m,
                    IsAllium = false,
                    FoodGroup = "VEGETABLE"
                },
                new Ingredient
                {
                    IngredientId = 12,
                    IngredientName = "Bông cải xanh (Broccoli)",
                    StandardUnit = "g",
                    DefaultStallType = "PRODUCE",
                    CaloriesPer100g = 34.0m,
                    IsAllium = false,
                    FoodGroup = "VEGETABLE"
                },
                new Ingredient
                {
                    IngredientId = 13,
                    IngredientName = "Hành tây",
                    StandardUnit = "củ",
                    DefaultStallType = "PRODUCE",
                    CaloriesPer100g = 40.0m,
                    IsAllium = true,
                    FoodGroup = "ALLIUM"
                },
                new Ingredient
                {
                    IngredientId = 14,
                    IngredientName = "Tỏi củ",
                    StandardUnit = "củ",
                    DefaultStallType = "PRODUCE",
                    CaloriesPer100g = 149.0m,
                    IsAllium = true,
                    FoodGroup = "ALLIUM"
                },
                new Ingredient
                {
                    IngredientId = 15,
                    IngredientName = "Hành lá",
                    StandardUnit = "nhánh",
                    DefaultStallType = "PRODUCE",
                    CaloriesPer100g = 32.0m,
                    IsAllium = true,
                    FoodGroup = "ALLIUM"
                }
            );
            db.SaveChanges();
        }

        // 3. Nạp Người dùng (Users: Admin, Member, VIP)
        if (!db.Users.Any())
        {
            var admin = new User
            {
                UserId = 1,
                Email = "admin@veggielife.vn",
                FullName = "Hệ Thống Admin",
                PasswordHash = DefaultPasswordHash,
                Role = UserRoles.Admin,
                AccountStatus = AccountStatuses.Active,
                CreatedAt = DateTime.UtcNow
            };

            var member = new User
            {
                UserId = 2,
                Email = "member@veggielife.vn",
                FullName = "Nguyễn Văn Thành Viên",
                PasswordHash = DefaultPasswordHash,
                Role = UserRoles.Member,
                AccountStatus = AccountStatuses.Active,
                CreatedAt = DateTime.UtcNow
            };

            var vip = new User
            {
                UserId = 3,
                Email = "vip@veggielife.vn",
                FullName = "Trần Thị Hội Viên VIP",
                PasswordHash = DefaultPasswordHash,
                Role = UserRoles.Vip,
                AccountStatus = AccountStatuses.Active,
                CreatedAt = DateTime.UtcNow
            };

            db.Users.AddRange(admin, member, vip);
            db.SaveChanges();

            // 4. Nạp Hồ sơ thể trạng (HealthProfile)
            db.HealthProfiles.AddRange(
                new HealthProfile
                {
                    UserId = 2,
                    DietarySchoolId = 1, // VEGAN
                    Gender = "FEMALE",
                    DateOfBirth = new DateOnly(1998, 5, 15),
                    HeightCm = 162.0m,
                    CurrentWeightKg = 52.0m,
                    TargetWeightKg = 50.0m,
                    BmiValue = 19.8m,
                    BmrKcal = 1250.0m,
                    TdeeKcal = 1800.0m,
                    TargetDailyCalories = 1800.0m,
                    RequiresAlliumFree = true,
                    RegionPreference = "MIEN_BAC",
                    HealthGoal = "MAINTAIN",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new HealthProfile
                {
                    UserId = 3,
                    DietarySchoolId = 4, // LACTO_OVO
                    Gender = "MALE",
                    DateOfBirth = new DateOnly(1995, 10, 20),
                    HeightCm = 175.0m,
                    CurrentWeightKg = 68.0m,
                    TargetWeightKg = 65.0m,
                    BmiValue = 22.2m,
                    BmrKcal = 1600.0m,
                    TdeeKcal = 2200.0m,
                    TargetDailyCalories = 2200.0m,
                    RequiresAlliumFree = false,
                    RegionPreference = "MIEN_NAM",
                    HealthGoal = "MAINTAIN",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );

            // 5. Nạp sẵn 3 món mẫu trong tủ lạnh cho Member (User 2)
            db.FridgeInventories.AddRange(
                new FridgeInventory
                {
                    UserId = 2,
                    IngredientId = 2, // Đậu hũ non
                    CustomItemName = "Đậu hũ non hữu cơ",
                    FreshnessStatus = "FRESH",
                    Quantity = 2.0m,
                    Unit = "miếng"
                },
                new FridgeInventory
                {
                    UserId = 2,
                    IngredientId = 4, // Nấm rơm (sắp hết hạn cần dùng sớm)
                    CustomItemName = "Nấm rơm tươi",
                    FreshnessStatus = "NON-FRESH",
                    Quantity = 200.0m,
                    Unit = "g"
                },
                new FridgeInventory
                {
                    UserId = 2,
                    IngredientId = 7, // Cà chua
                    CustomItemName = "Cà chua Đà Lạt",
                    FreshnessStatus = "FRESH",
                    Quantity = 3.0m,
                    Unit = "quả"
                }
            );

            // 6. Nạp Danh mục món ăn (FoodCategories)
            if (!db.FoodCategories.Any())
            {
                db.FoodCategories.AddRange(
                    new FoodCategory
                    {
                        CategoryId = 1,
                        CategoryName = "Mì, Bún & Phở",
                        Description = "Các món bún nước, phở, mì sợi thanh đạm",
                        IsCore = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new FoodCategory
                    {
                        CategoryId = 2,
                        CategoryName = "Cơm & Xôi",
                        Description = "Các món cơm tấm, cơm chiên, xôi chay",
                        IsCore = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new FoodCategory
                    {
                        CategoryId = 3,
                        CategoryName = "Món Kho & Xào",
                        Description = "Các món xào chay, kho tiêu, kho nấm",
                        IsCore = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new FoodCategory
                    {
                        CategoryId = 4,
                        CategoryName = "Canh & Súp",
                        Description = "Các món canh, súp thanh đạm bổ dưỡng",
                        IsCore = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new FoodCategory
                    {
                        CategoryId = 5,
                        CategoryName = "Món Chiên & Rán",
                        Description = "Các món chiên giòn, ram bắp, chả giò chay",
                        IsCore = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new FoodCategory
                    {
                        CategoryId = 6,
                        CategoryName = "Món Cuốn",
                        Description = "Gỏi cuốn, bò bía chay, phở cuốn thanh mát",
                        IsCore = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new FoodCategory
                    {
                        CategoryId = 7,
                        CategoryName = "Món Lẩu",
                        Description = "Lẩu nấm, lẩu thái, lẩu riêu cua chay",
                        IsCore = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new FoodCategory
                    {
                        CategoryId = 8,
                        CategoryName = "Gỏi & Salad",
                        Description = "Gỏi ngó sen, salad rau mầm, nộm hoa chuối",
                        IsCore = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                );
                db.SaveChanges();
            }

            // 6.5 Nạp Món ăn khái niệm (Foods) phục vụ combobox và liên kết công thức
            if (!db.Foods.Any())
            {
                db.Foods.AddRange(
                    new Food { FoodId = 1, CategoryId = 4, FoodName = "Canh nấm đậu phụ cà chua", CreatedAt = DateTime.UtcNow },
                    new Food { FoodId = 2, CategoryId = 3, FoodName = "Nấm rơm xào đậu hũ", CreatedAt = DateTime.UtcNow },
                    new Food { FoodId = 3, CategoryId = 4, FoodName = "Canh chua nấm rau cải", CreatedAt = DateTime.UtcNow },
                    new Food { FoodId = 4, CategoryId = 1, FoodName = "Bún bò Huế chay", CreatedAt = DateTime.UtcNow },
                    new Food { FoodId = 5, CategoryId = 1, FoodName = "Phở chay Hà Nội", CreatedAt = DateTime.UtcNow },
                    new Food { FoodId = 6, CategoryId = 1, FoodName = "Bún riêu cua chay", CreatedAt = DateTime.UtcNow },
                    new Food { FoodId = 7, CategoryId = 3, FoodName = "Đậu hũ kho nấm rơm", CreatedAt = DateTime.UtcNow },
                    new Food { FoodId = 8, CategoryId = 2, FoodName = "Cơm tấm sườn bì chả chay", CreatedAt = DateTime.UtcNow },
                    new Food { FoodId = 9, CategoryId = 7, FoodName = "Lẩu nấm dưỡng sinh thảo mộc", CreatedAt = DateTime.UtcNow },
                    new Food { FoodId = 10, CategoryId = 6, FoodName = "Gỏi cuốn chay ngũ sắc", CreatedAt = DateTime.UtcNow },
                    new Food { FoodId = 11, CategoryId = 5, FoodName = "Chả giò chay rong biển", CreatedAt = DateTime.UtcNow }
                );
                db.SaveChanges();
            }

            // 7. Nạp Công thức món ăn chuẩn (Recipes & RecipeIngredients) phục vụ FN-VIS-03
            if (!db.Recipes.Any())
            {
                var r1 = new Recipe
                {
                    RecipeId = 1,
                    UserId = 1,
                    DietarySchoolId = 1, // Vegan
                    FoodId = 1,
                    Title = "Canh nấm đậu phụ cà chua",
                    Description = "Món canh chay thanh đạm giàu đạm thực vật từ đậu hũ non và nấm rơm.",
                    Instructions = "1. Cắt đậu hũ miếng vuông. 2. Xào sơ cà chua và nấm rơm. 3. Nấu sôi và nêm gia vị.",
                    PrepTimeMinutes = 10,
                    CookTimeMinutes = 15,
                    Servings = 2,
                    CaloriesKcal = 180m,
                    DifficultyLevel = "EASY",
                    IsVerified = true,
                    Status = "APPROVED_ACTIVE",
                    Source = "OFFICIAL",
                    CreatedAt = DateTime.UtcNow
                };

                var r2 = new Recipe
                {
                    RecipeId = 2,
                    UserId = 1,
                    DietarySchoolId = 1, // Vegan
                    FoodId = 2,
                    Title = "Nấm rơm xào đậu hũ",
                    Description = "Món xào thơm lừng từ nấm rơm tươi và đậu hũ non.",
                    Instructions = "1. Áp chảo đậu hũ vàng đều. 2. Cho nấm rơm vào xào chín. 3. Rắc tiêu và dùng nóng.",
                    PrepTimeMinutes = 10,
                    CookTimeMinutes = 10,
                    Servings = 2,
                    CaloriesKcal = 220m,
                    DifficultyLevel = "EASY",
                    IsVerified = true,
                    Status = "APPROVED_ACTIVE",
                    Source = "OFFICIAL",
                    CreatedAt = DateTime.UtcNow
                };

                var r3 = new Recipe
                {
                    RecipeId = 3,
                    UserId = 1,
                    DietarySchoolId = 1, // Vegan
                    FoodId = 3,
                    Title = "Canh chua nấm rau cải",
                    Description = "Món canh chua giải nhiệt từ nấm rơm, cà chua và cải thìa.",
                    Instructions = "1. Nấu nước sôi. 2. Cho cà chua, nấm và rau cải thìa vào. 3. Nêm nước cốt me và muối.",
                    PrepTimeMinutes = 10,
                    CookTimeMinutes = 15,
                    Servings = 3,
                    CaloriesKcal = 150m,
                    DifficultyLevel = "EASY",
                    IsVerified = true,
                    Status = "APPROVED_ACTIVE",
                    Source = "OFFICIAL",
                    CreatedAt = DateTime.UtcNow
                };

                                var r4 = new Recipe
                {
                    RecipeId = 4,
                    UserId = 1,
                    DietarySchoolId = 1,
                    FoodId = 4,
                    Title = "Bún bò Huế chay giàu đạm & hạt sen dinh dưỡng",
                    Description = "Bún bò Huế chay nấu với hạt sen tươi, nấm đùi gà, chả huế chay và đậu hũ non chiên giòn, cung cấp năng lượng cao cho người gầy cần tăng cân.",
                    Instructions = "1. Nấu nước dùng với sả cây và hạt sen. 2. Xào chả huế chay và nấm. 3. Trình bày bún cùng rau thơm.",
                    PrepTimeMinutes = 15,
                    CookTimeMinutes = 35,
                    Servings = 2,
                    CaloriesKcal = 680m,
                    DifficultyLevel = "MEDIUM",
                    IsVerified = true,
                    Status = "APPROVED_ACTIVE",
                    Source = "OFFICIAL",
                    ImageUrl = "https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?auto=format&fit=crop&w=800&q=85",
                    CreatedAt = DateTime.UtcNow
                };

                var r5 = new Recipe
                {
                    RecipeId = 5,
                    UserId = 1,
                    DietarySchoolId = 1,
                    FoodId = 4,
                    Title = "Bún bò Huế chay thanh ngọt không cay dịu dạ dày",
                    Description = "Nước dùng ngọt thanh từ củ cải trắng, bắp ngọt và táo đỏ, hoàn toàn không cay, êm dịu cho người đau dạ dày và trẻ nhỏ.",
                    Instructions = "1. Hầm nước dùng rau củ ngọt thanh không dùng ớt. 2. Trụng bún và đậu hũ non. 3. Thưởng thức nóng.",
                    PrepTimeMinutes = 10,
                    CookTimeMinutes = 30,
                    Servings = 2,
                    CaloriesKcal = 420m,
                    DifficultyLevel = "EASY",
                    IsVerified = true,
                    Status = "APPROVED_ACTIVE",
                    Source = "OFFICIAL",
                    ImageUrl = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=85",
                    CreatedAt = DateTime.UtcNow
                };

                var r6 = new Recipe
                {
                    RecipeId = 6,
                    UserId = 1,
                    DietarySchoolId = 1,
                    FoodId = 4,
                    Title = "Bún bò Huế chay chuẩn vị cố đô đậm đà",
                    Description = "Chuẩn vị truyền thống xứ Huế với hương thơm sả cây, dầu màu điều và nấm đông cô ninh kỹ.",
                    Instructions = "1. Phi dầu màu điều thơm ngát cùng sả băm. 2. Nấu nước dùng đậm đà nấm hương. 3. Chan bún nóng.",
                    PrepTimeMinutes = 15,
                    CookTimeMinutes = 40,
                    Servings = 2,
                    CaloriesKcal = 490m,
                    DifficultyLevel = "MEDIUM",
                    IsVerified = true,
                    Status = "APPROVED_ACTIVE",
                    Source = "OFFICIAL",
                    ImageUrl = "https://images.unsplash.com/photo-1569718212165-3a8278d5f624?auto=format&fit=crop&w=800&q=85",
                    CreatedAt = DateTime.UtcNow
                };

                var r7 = new Recipe
                {
                    RecipeId = 7,
                    UserId = 1,
                    DietarySchoolId = 2,
                    FoodId = 4,
                    Title = "Bún bò Huế chay thanh tịnh kiêng ngũ vị tân",
                    Description = "Thuần chay Phật giáo không sử dụng hành, tỏi, hẹ, kiệu; tạo hương thơm bằng sả tươi, boa-rô và ngò gai.",
                    Instructions = "1. Dùng sả tươi và boa-rô phi thơm dầu mè. 2. Nấu nước dùng nấm rơm thanh tịnh. 3. Dùng kèm rau muống chẻ.",
                    PrepTimeMinutes = 15,
                    CookTimeMinutes = 30,
                    Servings = 2,
                    CaloriesKcal = 450m,
                    DifficultyLevel = "MEDIUM",
                    IsVerified = true,
                    Status = "APPROVED_ACTIVE",
                    Source = "OFFICIAL",
                    ImageUrl = "https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?auto=format&fit=crop&w=800&q=85",
                    CreatedAt = DateTime.UtcNow
                };

                var r8 = new Recipe
                {
                    RecipeId = 8,
                    UserId = 1,
                    DietarySchoolId = 1,
                    FoodId = 5,
                    Title = "Phở nấm chay nước dùng hồi quế béo bùi hạt óc chó",
                    Description = "Nước dùng phở hồi quế ninh cùng hạt óc chó và nấm đùi gà, cung cấp lượng calo dồi dào cho người gầy.",
                    Instructions = "1. Nướng thơm gừng, hồi, quế. 2. Nấu nước dùng ngọt thanh từ lê và hạt óc chó. 3. Chan bánh phở tươi.",
                    PrepTimeMinutes = 15,
                    CookTimeMinutes = 35,
                    Servings = 2,
                    CaloriesKcal = 650m,
                    DifficultyLevel = "MEDIUM",
                    IsVerified = true,
                    Status = "APPROVED_ACTIVE",
                    Source = "OFFICIAL",
                    ImageUrl = "https://images.unsplash.com/photo-1576097449798-7c7f90e1248a?auto=format&fit=crop&w=800&q=85",
                    CreatedAt = DateTime.UtcNow
                };

                var r9 = new Recipe
                {
                    RecipeId = 9,
                    UserId = 1,
                    DietarySchoolId = 1,
                    FoodId = 5,
                    Title = "Phở chay Hà Nội thanh vị không cay",
                    Description = "Bánh phở mềm mượt, nước dùng thanh ngọt từ củ cải và mía lau, không cay, ngọt lành tự nhiên.",
                    Instructions = "1. Hầm mía lau và củ cải lấy nước ngọt. 2. Trụng bánh phở Hà Nội. 3. Thêm nấm hương và đậu hũ non.",
                    PrepTimeMinutes = 10,
                    CookTimeMinutes = 25,
                    Servings = 2,
                    CaloriesKcal = 380m,
                    DifficultyLevel = "EASY",
                    IsVerified = true,
                    Status = "APPROVED_ACTIVE",
                    Source = "OFFICIAL",
                    ImageUrl = "https://images.unsplash.com/photo-1594041680534-e8c8cdebd659?auto=format&fit=crop&w=800&q=85",
                    CreatedAt = DateTime.UtcNow
                };

                var r10 = new Recipe
                {
                    RecipeId = 10,
                    UserId = 1,
                    DietarySchoolId = 1,
                    FoodId = 8,
                    Title = "Cơm tấm sườn bì chả chay năng lượng cao hạt điều",
                    Description = "Cơm tấm hạt thơm ăn kèm chả trứng đậu hũ, sườn non chay nướng ngũ vị và hạt điều rang ngậy béo, bữa ăn giàu calo hỗ trợ tăng cân.",
                    Instructions = "1. Nấu cơm tấm chín dẻo. 2. Nướng sườn non chay thơm lừng. 3. Dùng kèm chả chay và đồ chua.",
                    PrepTimeMinutes = 20,
                    CookTimeMinutes = 40,
                    Servings = 2,
                    CaloriesKcal = 720m,
                    DifficultyLevel = "HARD",
                    IsVerified = true,
                    Status = "APPROVED_ACTIVE",
                    Source = "OFFICIAL",
                    ImageUrl = "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=85",
                    CreatedAt = DateTime.UtcNow
                };

                var r11 = new Recipe
                {
                    RecipeId = 11,
                    UserId = 1,
                    DietarySchoolId = 1,
                    FoodId = 8,
                    Title = "Cơm tấm chả hấp chay thanh nhẹ không cay",
                    Description = "Chả đậu hũ mộc nhĩ hấp thanh đạm, nước mắm chay chua ngọt dịu không ớt, thân thiện cho hệ tiêu hóa.",
                    Instructions = "1. Trộn đậu hũ non với mộc nhĩ rồi hấp chín. 2. Pha nước mắm chay không ớt. 3. Dùng kèm cơm tấm nóng.",
                    PrepTimeMinutes = 15,
                    CookTimeMinutes = 30,
                    Servings = 2,
                    CaloriesKcal = 480m,
                    DifficultyLevel = "MEDIUM",
                    IsVerified = true,
                    Status = "APPROVED_ACTIVE",
                    Source = "OFFICIAL",
                    ImageUrl = "https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=800&q=85",
                    CreatedAt = DateTime.UtcNow
                };

                var r12 = new Recipe
                {
                    RecipeId = 12,
                    UserId = 1,
                    DietarySchoolId = 1,
                    FoodId = 7,
                    Title = "Đậu hũ kho nấm rơm nước cốt dừa đậm béo",
                    Description = "Đậu hũ chiên vàng kho cạn cùng nấm rơm tươi và nước cốt dừa béo bùi, giàu dinh dưỡng cho người gầy.",
                    Instructions = "1. Chiên vàng đậu hũ. 2. Xào săn nấm rơm cùng gia vị. 3. Thêm nước cốt dừa kho liu riu cho keo lại.",
                    PrepTimeMinutes = 10,
                    CookTimeMinutes = 25,
                    Servings = 2,
                    CaloriesKcal = 560m,
                    DifficultyLevel = "MEDIUM",
                    IsVerified = true,
                    Status = "APPROVED_ACTIVE",
                    Source = "OFFICIAL",
                    ImageUrl = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=85",
                    CreatedAt = DateTime.UtcNow
                };

                var r13 = new Recipe
                {
                    RecipeId = 13,
                    UserId = 1,
                    DietarySchoolId = 1,
                    FoodId = 7,
                    Title = "Đậu hũ kho nấm rơm thanh vị không cay",
                    Description = "Món kho gia đình quen thuộc, nêm nước tương ngon và đường mía, không dùng tiêu ớt, thanh lành dễ ăn.",
                    Instructions = "1. Cắt đậu hũ vuông vừa ăn. 2. Kho nhỏ lửa với nấm rơm trong 15 phút. 3. Rắc ngò rí và dọn dùng nóng.",
                    PrepTimeMinutes = 10,
                    CookTimeMinutes = 20,
                    Servings = 2,
                    CaloriesKcal = 280m,
                    DifficultyLevel = "EASY",
                    IsVerified = true,
                    Status = "APPROVED_ACTIVE",
                    Source = "OFFICIAL",
                    ImageUrl = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=85",
                    CreatedAt = DateTime.UtcNow
                };

                var r14 = new Recipe
                {
                    RecipeId = 14,
                    UserId = 1,
                    DietarySchoolId = 1,
                    FoodId = 9,
                    Title = "Lẩu Thái chay chua cay chuẩn vị Tomyum",
                    Description = "Nước lẩu bốc khói ngào ngạt hương lá chúc, riềng non và sả cây, nhúng kèm nấm quý và rau xanh tươi non.",
                    Instructions = "1. Nấu nước dùng từ me, thơm và sả. 2. Nêm sa tế chay và lá chanh kaffir. 3. Nhúng nấm, đậu hũ và rau tươi.",
                    PrepTimeMinutes = 15,
                    CookTimeMinutes = 35,
                    Servings = 4,
                    CaloriesKcal = 420m,
                    DifficultyLevel = "MEDIUM",
                    IsVerified = true,
                    Status = "APPROVED_ACTIVE",
                    Source = "OFFICIAL",
                    ImageUrl = "https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=800&q=85",
                    CreatedAt = DateTime.UtcNow
                };

                var r15 = new Recipe
                {
                    RecipeId = 15,
                    UserId = 1,
                    DietarySchoolId = 1,
                    FoodId = 9,
                    Title = "Lẩu nấm dưỡng sinh thảo mộc không cay thanh mát",
                    Description = "Thay ớt sa tế bằng kỷ tử, táo đỏ và đẳng sâm; nước dùng ngọt lịm từ bắp ngọt và lê tươi, thanh lọc cơ thể.",
                    Instructions = "1. Hầm thảo mộc kỷ tử, táo đỏ lấy nước ngọt. 2. Bày các loại nấm đùi gà, kim châm, nấm hương. 3. Thưởng thức nhẹ bụng.",
                    PrepTimeMinutes = 15,
                    CookTimeMinutes = 30,
                    Servings = 4,
                    CaloriesKcal = 350m,
                    DifficultyLevel = "EASY",
                    IsVerified = true,
                    Status = "APPROVED_ACTIVE",
                    Source = "OFFICIAL",
                    ImageUrl = "https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=800&q=85",
                    CreatedAt = DateTime.UtcNow
                };

                                var r16 = new Recipe
                {
                    RecipeId = 16,
                    UserId = 1,
                    DietarySchoolId = 1,
                    FoodId = 10,
                    Title = "Gỏi cuốn bơ sáp & sốt bơ đậu phộng dinh dưỡng",
                    Description = "Cuốn gỏi bơ sáp béo ngậy, đậu hũ chiên vàng giòn và rau thơm thanh mát, chấm sốt đậu phộng giàu đạm và calo tốt cho người gầy.",
                    Instructions = "1. Cắt bơ sáp và đậu hũ non. 2. Làm ẩm bánh tráng và cuốn chặt tay cùng bún và rau sống. 3. Pha sốt tương đen bơ đậu phộng béo ngậy.",
                    PrepTimeMinutes = 15,
                    CookTimeMinutes = 10,
                    Servings = 2,
                    CaloriesKcal = 520m,
                    DifficultyLevel = "EASY",
                    IsVerified = true,
                    Status = "APPROVED_ACTIVE",
                    Source = "OFFICIAL",
                    ImageUrl = "https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?auto=format&fit=crop&w=800&q=85",
                    CreatedAt = DateTime.UtcNow
                };

                var r17 = new Recipe
                {
                    RecipeId = 17,
                    UserId = 1,
                    DietarySchoolId = 1,
                    FoodId = 10,
                    Title = "Gỏi cuốn nấm tươi sốt mè rang thanh đạm không cay",
                    Description = "Bánh tráng cuốn nấm đùi gà xào thơm, cà rốt giòn ngọt và xà lách tươi, chấm sốt mè rang chua ngọt không cay dịu nhẹ.",
                    Instructions = "1. Xào chín nấm đùi gà và cà rốt với chút dầu mè. 2. Cuốn cùng bánh tráng mềm và rau sống. 3. Dùng kèm sốt mè rang.",
                    PrepTimeMinutes = 15,
                    CookTimeMinutes = 10,
                    Servings = 2,
                    CaloriesKcal = 260m,
                    DifficultyLevel = "EASY",
                    IsVerified = true,
                    Status = "APPROVED_ACTIVE",
                    Source = "OFFICIAL",
                    ImageUrl = "https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?auto=format&fit=crop&w=800&q=85",
                    CreatedAt = DateTime.UtcNow
                };

                var r18 = new Recipe
                {
                    RecipeId = 18,
                    UserId = 1,
                    DietarySchoolId = 1,
                    FoodId = 10,
                    Title = "Gỏi cuốn chay rau củ chấm tương đậu phộng truyền thống",
                    Description = "Món gỏi cuốn Nam Bộ truyền thống với chả lụa chay, đậu hũ, bún tươi và hẹ lá, chấm tương hột băm nhuyễn chuẩn vị.",
                    Instructions = "1. Chuẩn bị rau sống và cắt sợi chả chay. 2. Trải bánh tráng cuốn chặt tay. 3. Nấu tương hột với nước cốt dừa và rắc đậu phộng rang.",
                    PrepTimeMinutes = 15,
                    CookTimeMinutes = 15,
                    Servings = 4,
                    CaloriesKcal = 310m,
                    DifficultyLevel = "MEDIUM",
                    IsVerified = true,
                    Status = "APPROVED_ACTIVE",
                    Source = "OFFICIAL",
                    ImageUrl = "https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?auto=format&fit=crop&w=800&q=85",
                    CreatedAt = DateTime.UtcNow
                };

                db.Recipes.AddRange(r1, r2, r3, r4, r5, r6, r7, r8, r9, r10, r11, r12, r13, r14, r15, r16, r17, r18);
                db.SaveChanges();

                db.RecipeIngredients.AddRange(
                    // Canh nấm đậu phụ cà chua: Đậu hũ non (2), Nấm rơm (4), Cà chua (7) -> Match 100% (3/3)
                    new RecipeIngredient { RecipeId = 1, IngredientId = 2, QuantityNeeded = 1, Unit = "miếng", IsOptional = false },
                    new RecipeIngredient { RecipeId = 1, IngredientId = 4, QuantityNeeded = 150, Unit = "g", IsOptional = false },
                    new RecipeIngredient { RecipeId = 1, IngredientId = 7, QuantityNeeded = 2, Unit = "quả", IsOptional = false },

                    // Nấm rơm xào đậu hũ: Đậu hũ non (2), Nấm rơm (4) -> Match 100% (2/2)
                    new RecipeIngredient { RecipeId = 2, IngredientId = 2, QuantityNeeded = 1, Unit = "miếng", IsOptional = false },
                    new RecipeIngredient { RecipeId = 2, IngredientId = 4, QuantityNeeded = 200, Unit = "g", IsOptional = false },

                    // Canh chua nấm rau cải: Nấm rơm (4), Cà chua (7), Cải thìa (10), Cà rốt (8, optional)
                    // Member có 4, 7 -> 2/3 = 67% (< 70%)
                    new RecipeIngredient { RecipeId = 3, IngredientId = 4, QuantityNeeded = 100, Unit = "g", IsOptional = false },
                    new RecipeIngredient { RecipeId = 3, IngredientId = 7, QuantityNeeded = 2, Unit = "quả", IsOptional = false },
                    new RecipeIngredient { RecipeId = 3, IngredientId = 10, QuantityNeeded = 200, Unit = "g", IsOptional = false },
                    new RecipeIngredient { RecipeId = 3, IngredientId = 8, QuantityNeeded = 1, Unit = "củ", IsOptional = true }
                );
            }

            db.SaveChanges();
        }

        SeedSystemSettings(db);
        SeedAiquotaLogs(db);
        DevCommunityDataSeeder.Seed(db);
    }

    private static void SeedAiquotaLogs(AppDbContext db)
    {
        // 7. Nạp dữ liệu mẫu AIQuotaLogs (FN-ADM-16)
        if (!db.AiquotaLogs.Any())
        {
            // Log chức năng khách hàng phải thuộc MEMBER/VIP, không gán cho Admin (Admin không dùng chức năng khách hàng)
            var customerId = db.Users
                .Where(u => u.Role == "MEMBER" || u.Role == "VIP")
                .OrderBy(u => u.UserId)
                .Select(u => u.UserId)
                .FirstOrDefault();
            if (customerId == 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            db.AiquotaLogs.AddRange(
                new AiquotaLog { UserId = customerId, FeatureCode = "AI_CHAT", TokensConsumed = 120000, CostEstimateUsd = AiCostCalculator.CalculateFromTotal(120000), RequestStatus = "SUCCESS", ExecutionTimeMs = 600, CreatedAt = now.AddDays(-2) },
                new AiquotaLog { UserId = customerId, FeatureCode = "MEAL_PLAN_GEN", TokensConsumed = 30000, CostEstimateUsd = AiCostCalculator.CalculateFromTotal(30000), RequestStatus = "SUCCESS", ExecutionTimeMs = 1200, CreatedAt = now.AddDays(-2) },
                new AiquotaLog { UserId = customerId, FeatureCode = "AI_FRIDGE", TokensConsumed = 45000, CostEstimateUsd = AiCostCalculator.CalculateFromTotal(45000), RequestStatus = "SUCCESS", ExecutionTimeMs = 800, CreatedAt = now.AddDays(-1) },
                new AiquotaLog { UserId = customerId, FeatureCode = "AI_BARCODE", TokensConsumed = 15000, CostEstimateUsd = AiCostCalculator.CalculateFromTotal(15000), RequestStatus = "SUCCESS", ExecutionTimeMs = 350, CreatedAt = now.AddDays(-1) },
                new AiquotaLog { UserId = customerId, FeatureCode = "AI_CHAT", TokensConsumed = 80000, CostEstimateUsd = AiCostCalculator.CalculateFromTotal(80000), RequestStatus = "SUCCESS", ExecutionTimeMs = 700, CreatedAt = now.AddHours(-3) },
                new AiquotaLog { UserId = customerId, FeatureCode = "CONTENT_MODERATION", TokensConsumed = 20000, CostEstimateUsd = AiCostCalculator.CalculateFromTotal(20000), RequestStatus = "SUCCESS", ExecutionTimeMs = 400, CreatedAt = now.AddHours(-1) }
            );
            db.SaveChanges();
        }
    }

    private static void SeedSystemSettings(AppDbContext db)
    {
        const string legalJson = "{\"user_agreement\":\"https://res.cloudinary.com/swrsuenp/image/upload/v1793718600/veggielife/legal/116_2025_QH15_666020.jpg\",\"privacy_policy\":\"https://res.cloudinary.com/swrsuenp/image/upload/v1793718345/veggielife/legal/13_2023_ND-CP_465185.jpg\",\"terms_of_service\":\"https://res.cloudinary.com/swrsuenp/image/upload/v1793718645/veggielife/legal/19_2023_QH15_500102.jpg\",\"vegan_disclaimer\":\"https://res.cloudinary.com/swrsuenp/image/upload/v1793718667/veggielife/legal/142_2026_ND-CP_696080.jpg\",\"ai_regulation\":\"https://res.cloudinary.com/swrsuenp/image/upload/v1793718667/veggielife/legal/142_2026_ND-CP_696080.jpg\",\"info_security\":\"https://res.cloudinary.com/swrsuenp/image/upload/v1793718553/veggielife/legal/85_2016_ND-CP_317475.jpg\",\"fintech_sandbox\":\"https://res.cloudinary.com/swrsuenp/image/upload/v1793718656/veggielife/legal/94_2025_ND-CP_584706.jpg\"}";

        var settings = new List<SystemSetting>
        {
            new() { SettingKey = "MEAL_SWAP_LIMIT_DRAFT", SettingValue = "21", DefaultValue = "21", DataType = "INT", Category = "MEAL_PLAN", Description = "Hạn mức số lần đổi món thực đơn trong giai đoạn dự thảo (DRAFT)", UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "MEAL_SWAP_LIMIT_ACTIVE", SettingValue = "21", DefaultValue = "21", DataType = "INT", Category = "MEAL_PLAN", Description = "Hạn mức số lần đổi món thực đơn trong giai đoạn đã kích hoạt (ACTIVE)", UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "MEAL_PLAN_WEEKLY_LIMIT_FREE", SettingValue = "1", DefaultValue = "1", DataType = "INT", Category = "MEAL_PLAN", Description = "Số lượng thực đơn tối đa được tạo mỗi tuần cho tài khoản Miễn phí", UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "MEAL_PLAN_WEEKLY_LIMIT_VIP", SettingValue = "3", DefaultValue = "3", DataType = "INT", Category = "MEAL_PLAN", Description = "Số lượng thực đơn tối đa được tạo mỗi tuần cho tài khoản VIP", UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "MEAL_SWAP_CALORIE_TOLERANCE_PERCENT", SettingValue = "15", DefaultValue = "15", DataType = "INT", Category = "MEAL_PLAN", Description = "Độ lệch calo tối đa (%) khi thuật toán tự động tìm món thay thế", UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "CHAT_LIMIT_DAILY_GUEST", SettingValue = "3", DefaultValue = "3", DataType = "INT", Category = "AI_QUOTA", Description = "Số câu hỏi AI Chatbot tối đa cho khách vãng lai (Guest)", UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "CHAT_LIMIT_DAILY_FREE", SettingValue = "20", DefaultValue = "20", DataType = "INT", Category = "AI_QUOTA", Description = "Số câu hỏi AI Chatbot tối đa mỗi ngày cho tài khoản Miễn phí", UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "CHAT_LIMIT_DAILY_VIP", SettingValue = "200", DefaultValue = "200", DataType = "INT", Category = "AI_QUOTA", Description = "Số câu hỏi AI Chatbot tối đa mỗi ngày cho tài khoản VIP", UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "FRIDGE_SCAN_LIMIT_DAILY_FREE", SettingValue = "10", DefaultValue = "10", DataType = "INT", Category = "AI_QUOTA", Description = "Số lần quét tủ lạnh AI tối đa mỗi ngày cho tài khoản Miễn phí", UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "FRIDGE_SCAN_LIMIT_DAILY_VIP", SettingValue = "30", DefaultValue = "30", DataType = "INT", Category = "AI_QUOTA", Description = "Số lần quét tủ lạnh AI tối đa mỗi ngày cho tài khoản VIP", UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "LLMOPS_DAILY_BUDGET_USD", SettingValue = "12.5", DefaultValue = "12.5", DataType = "DECIMAL", Category = "LLMOPS", Description = "Ngân sách tiêu dùng AI tối đa mỗi ngày của toàn hệ thống (USD)", UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "LLMOPS_ALERT_THRESHOLD_PERCENT", SettingValue = "80", DefaultValue = "80", DataType = "INT", Category = "LLMOPS", Description = "Ngưỡng cảnh báo chi phí khẩn cấp (%)", UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "COMMUNITY_REPORT_LIMIT", SettingValue = "10", DefaultValue = "10", DataType = "INT", Category = "COMMUNITY", Description = "Số lượng báo cáo vi phạm tích lũy để bài viết tự động bị tạm ẩn", UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "LEGAL_DOCUMENTS", SettingValue = legalJson, DefaultValue = legalJson, DataType = "STRING", Category = "GENERAL", Description = "Danh mục tài liệu pháp lý VeggieLife", LegalDocumentsUrl = legalJson, UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "LEGAL_USER_AGREEMENT_URL", SettingValue = "https://res.cloudinary.com/swrsuenp/image/upload/v1793718600/veggielife/legal/116_2025_QH15_666020.jpg", DefaultValue = "https://res.cloudinary.com/swrsuenp/image/upload/v1793718600/veggielife/legal/116_2025_QH15_666020.jpg", DataType = "STRING", Category = "GENERAL", Description = "Thỏa thuận người dùng (Luật 116/2025/QH15)", LegalDocumentsUrl = "https://res.cloudinary.com/swrsuenp/image/upload/v1793718600/veggielife/legal/116_2025_QH15_666020.jpg", UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "LEGAL_PRIVACY_POLICY_URL", SettingValue = "https://res.cloudinary.com/swrsuenp/image/upload/v1793718345/veggielife/legal/13_2023_ND-CP_465185.jpg", DefaultValue = "https://res.cloudinary.com/swrsuenp/image/upload/v1793718345/veggielife/legal/13_2023_ND-CP_465185.jpg", DataType = "STRING", Category = "GENERAL", Description = "Chính sách bảo mật dữ liệu (NĐ 13/2023/NĐ-CP)", LegalDocumentsUrl = "https://res.cloudinary.com/swrsuenp/image/upload/v1793718345/veggielife/legal/13_2023_ND-CP_465185.jpg", UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "LEGAL_TERMS_OF_SERVICE_URL", SettingValue = "https://res.cloudinary.com/swrsuenp/image/upload/v1793718645/veggielife/legal/19_2023_QH15_500102.jpg", DefaultValue = "https://res.cloudinary.com/swrsuenp/image/upload/v1793718645/veggielife/legal/19_2023_QH15_500102.jpg", DataType = "STRING", Category = "GENERAL", Description = "Điều khoản dịch vụ & quyền lợi NTD (Luật 19/2023/QH15)", LegalDocumentsUrl = "https://res.cloudinary.com/swrsuenp/image/upload/v1793718645/veggielife/legal/19_2023_QH15_500102.jpg", UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "LEGAL_VEGAN_DISCLAIMER_URL", SettingValue = "https://res.cloudinary.com/swrsuenp/image/upload/v1793718667/veggielife/legal/142_2026_ND-CP_696080.jpg", DefaultValue = "https://res.cloudinary.com/swrsuenp/image/upload/v1793718667/veggielife/legal/142_2026_ND-CP_696080.jpg", DataType = "STRING", Category = "GENERAL", Description = "Quy chuẩn Trí tuệ nhân tạo và Miễn trừ (NĐ 142/2026/NĐ-CP)", LegalDocumentsUrl = "https://res.cloudinary.com/swrsuenp/image/upload/v1793718667/veggielife/legal/142_2026_ND-CP_696080.jpg", UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "LEGAL_INFO_SECURITY_URL", SettingValue = "https://res.cloudinary.com/swrsuenp/image/upload/v1793718553/veggielife/legal/85_2016_ND-CP_317475.jpg", DefaultValue = "https://res.cloudinary.com/swrsuenp/image/upload/v1793718553/veggielife/legal/85_2016_ND-CP_317475.jpg", DataType = "STRING", Category = "GENERAL", Description = "An toàn hệ thống thông tin theo cấp độ (NĐ 85/2016/NĐ-CP)", LegalDocumentsUrl = "https://res.cloudinary.com/swrsuenp/image/upload/v1793718553/veggielife/legal/85_2016_ND-CP_317475.jpg", UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "LEGAL_FINTECH_SANDBOX_URL", SettingValue = "https://res.cloudinary.com/swrsuenp/image/upload/v1793718656/veggielife/legal/94_2025_ND-CP_584706.jpg", DefaultValue = "https://res.cloudinary.com/swrsuenp/image/upload/v1793718656/veggielife/legal/94_2025_ND-CP_584706.jpg", DataType = "STRING", Category = "GENERAL", Description = "Thử nghiệm có kiểm soát ngân hàng (NĐ 94/2025/NĐ-CP)", LegalDocumentsUrl = "https://res.cloudinary.com/swrsuenp/image/upload/v1793718656/veggielife/legal/94_2025_ND-CP_584706.jpg", UpdatedAt = DateTime.UtcNow }
        };

        foreach (var item in settings)
        {
            var existing = db.SystemSettings.Find(item.SettingKey);
            if (existing == null)
            {
                db.SystemSettings.Add(item);
            }
            else
            {
                existing.LegalDocumentsUrl = item.LegalDocumentsUrl;
                if (item.SettingKey.StartsWith("LEGAL_"))
                {
                    existing.SettingValue = item.SettingValue;
                    existing.DefaultValue = item.DefaultValue;
                    existing.Description = item.Description;
                }
            }
        }

        db.SaveChanges();
    }
}

