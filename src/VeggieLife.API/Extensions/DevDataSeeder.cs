using System;
using System.Collections.Generic;
using System.Linq;
using VeggieLife.BLL.Common.Constants;
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

    public static void SeedIfEmpty(AppDbContext db)
    {
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
                    FreshnessStatus = "FRESH"
                },
                new FridgeInventory
                {
                    UserId = 2,
                    IngredientId = 4, // Nấm rơm (sắp hết hạn cần dùng sớm)
                    CustomItemName = "Nấm rơm tươi",
                    FreshnessStatus = "NON-FRESH"
                },
                new FridgeInventory
                {
                    UserId = 2,
                    IngredientId = 7, // Cà chua
                    CustomItemName = "Cà chua Đà Lạt",
                    FreshnessStatus = "FRESH"
                }
            );

            // 6. Nạp Danh mục món ăn (FoodCategories)
            if (!db.FoodCategories.Any())
            {
                db.FoodCategories.AddRange(
                    new FoodCategory
                    {
                        CategoryId = 1,
                        CategoryName = "Món Canh / Súp",
                        Description = "Các món canh, súp thanh đạm bổ dưỡng",
                        IsCore = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new FoodCategory
                    {
                        CategoryId = 2,
                        CategoryName = "Món Xào / Kho",
                        Description = "Các món xào chay, kho tiêu, kho nấm",
                        IsCore = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
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
                    CategoryId = 1,
                    DietarySchoolId = 1, // Vegan
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
                    CategoryId = 2,
                    DietarySchoolId = 1, // Vegan
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
                    CategoryId = 1,
                    DietarySchoolId = 1, // Vegan
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

                db.Recipes.AddRange(r1, r2, r3);
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
    }
}

