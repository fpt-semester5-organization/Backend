using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.API.Extensions;

/// <summary>
/// Khởi tạo dữ liệu mẫu tự động khi chạy chế độ In-Memory Database (Database:UseInMemory = true).
/// Đảm bảo môi trường phát triển local luôn có sẵn dữ liệu chuẩn để kiểm thử trên Swagger UI.
/// </summary>
public static class DevInMemoryDataSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.Recipes.Any())
        {
            return;
        }

        // 1. Trường phái ăn chay (DietarySchools)
        var veganSchool = new DietarySchool
        {
            DietarySchoolId = 7,
            SchoolCode = "VEGAN",
            SchoolName = "Thuần chay (Vegan)",
            ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"EGG\",\"DAIRY\",\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":false}"
        };
        var buddhistSchool = new DietarySchool
        {
            DietarySchoolId = 1,
            SchoolCode = "BUDDHIST",
            SchoolName = "Chay Phật giáo",
            ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"EGG\",\"DAIRY\",\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":true}"
        };
        var lactoSchool = new DietarySchool
        {
            DietarySchoolId = 2,
            SchoolCode = "LACTO",
            SchoolName = "Chay có sữa (Lacto-Vegetarian)",
            ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"EGG\",\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":false}"
        };
        var ovoSchool = new DietarySchool
        {
            DietarySchoolId = 3,
            SchoolCode = "OVO",
            SchoolName = "Chay có trứng (Ovo-Vegetarian)",
            ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"DAIRY\",\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":false}"
        };
        var lactoOvoSchool = new DietarySchool
        {
            DietarySchoolId = 4,
            SchoolCode = "LACTO_OVO",
            SchoolName = "Chay có trứng và sữa (Lacto-Ovo)",
            ForbiddenFoodRules = "{\"forbiddenFoodGroups\":[\"POULTRY\",\"SEAFOOD\"],\"forbidAllium\":false}"
        };
        db.DietarySchools.AddRange(veganSchool, buddhistSchool, lactoSchool, ovoSchool, lactoOvoSchool);

        // 2. Danh mục món ăn (FoodCategories)
        var soupCategory = new FoodCategory { CategoryId = 3, CategoryName = "Món Canh & Súp Bổ Dưỡng", Description = "Món canh", IsCore = true, IsActive = true };
        var braisedCategory = new FoodCategory { CategoryId = 9, CategoryName = "Món Kho Chay Đậm Đà", Description = "Món kho", IsCore = true, IsActive = true };
        var stirFryCategory = new FoodCategory { CategoryId = 14, CategoryName = "Món Xào Chay Thanh Vị", Description = "Món xào", IsCore = true, IsActive = true };
        var riceCategory = new FoodCategory { CategoryId = 1, CategoryName = "Món Cơm & Xôi Chay", Description = "Món cơm", IsCore = true, IsActive = true };
        var hiddenCategory = new FoodCategory { CategoryId = 99, CategoryName = "Danh Mục Đã Bị Ẩn", Description = "Danh mục ẩn", IsCore = false, IsActive = false };
        db.FoodCategories.AddRange(soupCategory, braisedCategory, stirFryCategory, riceCategory, hiddenCategory);

        // 3. Nguyên liệu thực phẩm (Ingredients)
        var tofuSilken = new Ingredient { IngredientId = 39, IngredientName = "Đậu hũ non", StandardUnit = "g", DefaultStallType = "PLANT_PROTEIN", FoodGroup = "PLANT", CaloriesPer100g = 76m };
        var mushroomStraw = new Ingredient { IngredientId = 90, IngredientName = "Nấm rơm tươi", StandardUnit = "g", DefaultStallType = "PRODUCE", FoodGroup = "PLANT", CaloriesPer100g = 32m };
        var mushroomShiitake = new Ingredient { IngredientId = 84, IngredientName = "Nấm hương tươi", StandardUnit = "g", DefaultStallType = "PRODUCE", FoodGroup = "PLANT", CaloriesPer100g = 34m };
        var mushroomKingOyster = new Ingredient { IngredientId = 82, IngredientName = "Nấm đùi gà", StandardUnit = "g", DefaultStallType = "PRODUCE", FoodGroup = "PLANT", CaloriesPer100g = 35m };
        var mushroomEnoki = new Ingredient { IngredientId = 85, IngredientName = "Nấm kim châm tươi", StandardUnit = "g", DefaultStallType = "PRODUCE", FoodGroup = "PLANT", CaloriesPer100g = 37m };
        var tofuFried = new Ingredient { IngredientId = 38, IngredientName = "Đậu hũ chiên vàng", StandardUnit = "g", DefaultStallType = "PLANT_PROTEIN", FoodGroup = "PLANT", CaloriesPer100g = 271m };
        var sweetCorn = new Ingredient { IngredientId = 5, IngredientName = "Bắp ngọt tươi", StandardUnit = "g", DefaultStallType = "PRODUCE", FoodGroup = "PLANT", CaloriesPer100g = 86m };
        var butterDairy = new Ingredient { IngredientId = 10, IngredientName = "Bơ lạt từ sữa bò", StandardUnit = "g", DefaultStallType = "DAIRY", FoodGroup = "DAIRY", CaloriesPer100g = 717m };
        db.Ingredients.AddRange(tofuSilken, mushroomStraw, mushroomShiitake, mushroomKingOyster, mushroomEnoki, tofuFried, sweetCorn, butterDairy);

        // 4. Người dùng mẫu (Users)
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
        var admin = new User
        {
            UserId = 1,
            Email = "admin@veggielife.vn",
            FullName = "Quản Trị Viên Hệ Thống",
            PasswordHash = passwordHash,
            Role = "ADMIN",
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };
        var member = new User
        {
            UserId = 2,
            Email = "member@veggielife.vn",
            FullName = "Nguyễn Văn Chay",
            PasswordHash = passwordHash,
            Role = "MEMBER",
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-20)
        };
        db.Users.AddRange(admin, member);

        // 5. Hồ sơ thể trạng (HealthProfiles)
        var memberProfile = new HealthProfile
        {
            UserId = 2,
            DietarySchoolId = 7, // VEGAN
            Gender = "OTHER",
            RegionPreference = "SOUTH",
            HealthGoal = "MAINTAIN",
            DateOfBirth = new DateOnly(1995, 1, 1),
            HeightCm = 170m,
            CurrentWeightKg = 65m,
            BmiValue = 22.5m,
            BmrKcal = 1500m,
            TdeeKcal = 2000m,
            TargetDailyCalories = 2000m,
            CreatedAt = DateTime.UtcNow.AddDays(-20),
            UpdatedAt = DateTime.UtcNow.AddDays(-20)
        };
        db.HealthProfiles.Add(memberProfile);

        // 6. Công thức nấu ăn (Recipes) - ID 101 đến 106 chuẩn theo 09_Seed_SampleRecipes.sql
        var r101 = new Recipe
        {
            RecipeId = 101,
            UserId = 1,
            CategoryId = 3,
            DietarySchoolId = 7, // VEGAN
            Title = "Canh nấm đậu phụ rong biển",
            Description = "Món canh thanh đạm bổ dưỡng mát lành giải nhiệt cơ thể.",
            Instructions = "[\"Bước 1: Rửa sạch nấm rơm và cắt miếng vừa ăn, cắt đậu hũ non thành khối vuông.\",\"Bước 2: Nấu sôi 800ml nước, cho nấm rơm vào đun trong 5 phút.\",\"Bước 3: Cho đậu hũ non và rong biển vào nấu thêm 3 phút, nêm nếm vừa ăn rồi tắt bếp.\"]",
            PrepTimeMinutes = 10,
            CookTimeMinutes = 25,
            Servings = 2,
            CaloriesKcal = 310.0m,
            DifficultyLevel = "EASY",
            IsVerified = true,
            ImageUrl = "https://res.cloudinary.com/veggielife/r/101.webp",
            Status = "APPROVED_ACTIVE",
            ViewCount = 150,
            Source = "USER",
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        var r102 = new Recipe
        {
            RecipeId = 102,
            UserId = 1,
            CategoryId = 9,
            DietarySchoolId = 1, // BUDDHIST
            Title = "Nấm kho tiêu hạt sen đậm đà",
            Description = "Nấm kho sánh quyện đậm đà đưa cơm chuẩn vị chay Phật giáo.",
            Instructions = "[\"Bước 1: Sơ chế nấm hương và nấm đùi gà.\",\"Bước 2: Ướp nấm với nước tương và tiêu.\",\"Bước 3: Kho trên lửa nhỏ trong 20 phút.\"]",
            PrepTimeMinutes = 15,
            CookTimeMinutes = 35,
            Servings = 4,
            CaloriesKcal = 240.0m,
            DifficultyLevel = "MEDIUM",
            IsVerified = true,
            ImageUrl = "https://res.cloudinary.com/veggielife/r/102.webp",
            Status = "APPROVED_ACTIVE",
            ViewCount = 280,
            Source = "USER",
            CreatedAt = DateTime.UtcNow.AddDays(-4)
        };

        var r103 = new Recipe
        {
            RecipeId = 103,
            UserId = 2,
            CategoryId = 14,
            DietarySchoolId = 7, // VEGAN
            Title = "Mì xào nấm rau củ giòn ngọt",
            Description = "Món mì xào giòn thơm kèm video hướng dẫn từng bước.",
            Instructions = "[\"Bước 1: Chần sơ mì qua nước sôi.\",\"Bước 2: Xào chín nấm kim châm và đậu hũ chiên.\",\"Bước 3: Cho mì vào đảo đều lửa lớn trong 3 phút.\"]",
            PrepTimeMinutes = 10,
            CookTimeMinutes = 15,
            Servings = 2,
            CaloriesKcal = 350.0m,
            DifficultyLevel = "EASY",
            IsVerified = false,
            ImageUrl = "https://res.cloudinary.com/veggielife/r/103.webp",
            Status = "APPROVED_ACTIVE",
            ViewCount = 95,
            Source = "USER",
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };

        var r104 = new Recipe
        {
            RecipeId = 104,
            UserId = 2,
            CategoryId = 3,
            DietarySchoolId = 2, // LACTO
            Title = "Súp ngô ngọt kem tươi dinh dưỡng",
            Description = "Súp thơm mùi bơ sữa và ngọt thanh tự nhiên từ bắp ngọt.",
            Instructions = "[\"Bước 1: Tách hạt ngô ngọt.\",\"Bước 2: Xào thơm ngô với bơ lạt.\",\"Bước 3: Nấu sôi hỗn hợp súp và khuấy đều.\"]",
            PrepTimeMinutes = 10,
            CookTimeMinutes = 20,
            Servings = 3,
            CaloriesKcal = 280.0m,
            DifficultyLevel = "EASY",
            IsVerified = true,
            ImageUrl = "https://res.cloudinary.com/veggielife/r/104.webp",
            Status = "APPROVED_ACTIVE",
            ViewCount = 120,
            Source = "USER",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var r105 = new Recipe
        {
            RecipeId = 105,
            UserId = 2,
            CategoryId = 3,
            DietarySchoolId = 7,
            Title = "Canh bí đỏ nháp đang soạn",
            Description = "Bản nháp chưa gửi duyệt.",
            Instructions = "[\"Bước 1: Gọt vỏ bí đỏ.\"]",
            PrepTimeMinutes = 5,
            CookTimeMinutes = 20,
            Servings = 2,
            CaloriesKcal = 190.0m,
            DifficultyLevel = "EASY",
            IsVerified = false,
            Status = "DRAFT",
            ViewCount = 0,
            Source = "USER",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var r106 = new Recipe
        {
            RecipeId = 106,
            UserId = 2,
            CategoryId = 9,
            DietarySchoolId = 7,
            Title = "Công thức vi phạm cách ly",
            Description = "Bị cách ly kiểm duyệt.",
            Instructions = "[\"Cách ly\"]",
            PrepTimeMinutes = 5,
            CookTimeMinutes = 30,
            Servings = 2,
            CaloriesKcal = 200.0m,
            DifficultyLevel = "HARD",
            IsVerified = false,
            Status = "QUARANTINED_HIDDEN",
            ViewCount = 0,
            Source = "USER",
            CreatedAt = DateTime.UtcNow
        };
        db.Recipes.AddRange(r101, r102, r103, r104, r105, r106);

        // 7. Định lượng nguyên liệu (RecipeIngredients)
        db.RecipeIngredients.AddRange(
            new RecipeIngredient { RecipeId = 101, IngredientId = 39, QuantityNeeded = 200, Unit = "g", PreparationNote = "Cắt khối vuông nhỏ" },
            new RecipeIngredient { RecipeId = 101, IngredientId = 90, QuantityNeeded = 150, Unit = "g", PreparationNote = "Cắt đôi" },
            new RecipeIngredient { RecipeId = 102, IngredientId = 84, QuantityNeeded = 250, Unit = "g", PreparationNote = "Ngâm nở, khía hoa" },
            new RecipeIngredient { RecipeId = 102, IngredientId = 82, QuantityNeeded = 150, Unit = "g", PreparationNote = "Cắt khoanh tròn" },
            new RecipeIngredient { RecipeId = 103, IngredientId = 85, QuantityNeeded = 100, Unit = "g", PreparationNote = "Bỏ gốc, tách sợi" },
            new RecipeIngredient { RecipeId = 103, IngredientId = 38, QuantityNeeded = 100, Unit = "g", PreparationNote = "Cắt sợi vừa ăn" },
            new RecipeIngredient { RecipeId = 104, IngredientId = 5, QuantityNeeded = 200, Unit = "g", PreparationNote = "Tách hạt" },
            new RecipeIngredient { RecipeId = 104, IngredientId = 10, QuantityNeeded = 30, Unit = "g", PreparationNote = "Để mềm nhiệt độ phòng" }
        );

        // 8. Bài viết cộng đồng (Posts)
        var p101 = new Post { PostId = 1001, UserId = 1, RecipeId = 101, Title = r101.Title, Content = "Chia sẻ công thức canh nấm ngon tuyệt thanh mát giải nhiệt.", Visibility = "PUBLIC", ModerationStatus = "APPROVED_ACTIVE", ViewCount = 150, CreatedAt = DateTime.UtcNow.AddDays(-5) };
        var p102 = new Post { PostId = 1002, UserId = 1, RecipeId = 102, Title = r102.Title, Content = "Món kho đậm đà chuẩn vị chay ăn kèm cơm nóng.", Visibility = "PUBLIC", ModerationStatus = "APPROVED_ACTIVE", ViewCount = 280, CreatedAt = DateTime.UtcNow.AddDays(-4) };
        var p103 = new Post { PostId = 1003, UserId = 2, RecipeId = 103, Title = r103.Title, Content = "Mì xào rau củ dễ làm tại nhà kèm video hướng dẫn chi tiết.", Visibility = "PUBLIC", ModerationStatus = "APPROVED_ACTIVE", ViewCount = 95, CreatedAt = DateTime.UtcNow.AddDays(-3) };
        var p104 = new Post { PostId = 1004, UserId = 2, RecipeId = 104, Title = r104.Title, Content = "Súp ngô ngọt dịu trẻ nhỏ và người lớn đều thích mê.", Visibility = "PUBLIC", ModerationStatus = "APPROVED_ACTIVE", ViewCount = 120, CreatedAt = DateTime.UtcNow.AddDays(-2) };
        var pExtra = new Post { PostId = 1005, UserId = 1, RecipeId = null, Title = "Bí quyết chọn nấm rơm và đậu phụ tươi ngon", Content = "Mẹo hay nhà bếp giúp bạn chọn nấm và đậu ngon an toàn.", Visibility = "PUBLIC", ModerationStatus = "APPROVED_ACTIVE", ViewCount = 80, CreatedAt = DateTime.UtcNow.AddDays(-1) };
        db.Posts.AddRange(p101, p102, p103, p104, pExtra);

        // 9. Video nấu ăn (Videos)
        var v103 = new Video
        {
            VideoId = 2001,
            PostId = 1003,
            Title = "Video hướng dẫn làm Mì xào nấm rau củ giòn ngọt",
            VideoUrl = "https://video.veggielife.vn/r103.mp4",
            ThumbnailUrl = "https://res.cloudinary.com/veggielife/r/103_thumb.webp",
            DurationSeconds = 120,
            ViewCount = 150,
            Status = "READY",
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };
        var v101 = new Video
        {
            VideoId = 2002,
            PostId = 1001,
            Title = "Video nấu canh nấm thanh mát đậu hũ non",
            VideoUrl = "https://video.veggielife.vn/r101.mp4",
            ThumbnailUrl = "https://res.cloudinary.com/veggielife/r/101_thumb.webp",
            DurationSeconds = 90,
            ViewCount = 200,
            Status = "READY",
            CreatedAt = DateTime.UtcNow.AddDays(-4)
        };
        db.Videos.AddRange(v103, v101);

        // 10. Lượt bình chọn (Votes)
        for (int i = 1; i <= 25; i++)
        {
            db.Votes.Add(new Vote { VoteId = 1000 + i, PostId = 1001, UserId = 1, CreatedAt = DateTime.UtcNow });
        }
        for (int i = 1; i <= 42; i++)
        {
            db.Votes.Add(new Vote { VoteId = 2000 + i, PostId = 1002, UserId = 1, CreatedAt = DateTime.UtcNow });
        }
        for (int i = 1; i <= 15; i++)
        {
            db.Votes.Add(new Vote { VoteId = 3000 + i, PostId = 1003, UserId = 2, CreatedAt = DateTime.UtcNow });
        }

        // 11. Bình luận (Comments)
        db.Comments.AddRange(
            new Comment { CommentId = 1, PostId = 1001, UserId = 1, Content = "Canh nấu rất thanh mát và dễ ăn!", Status = "APPROVED_ACTIVE", IsSoftDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new Comment { CommentId = 2, PostId = 1001, UserId = 2, Content = "Cảm ơn tác giả đã chia sẻ công thức tuyệt vời này.", Status = "APPROVED_ACTIVE", IsSoftDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new Comment { CommentId = 3, PostId = 1002, UserId = 1, Content = "Món kho này ăn cùng cơm nóng rất đưa cơm.", Status = "APPROVED_ACTIVE", IsSoftDeleted = false, CreatedAt = DateTime.UtcNow.AddDays(-1) }
        );

        // 12. Quán ăn chay Goong tại Hà Nội và TP.HCM (FN-MAP-01)
        if (!db.VeganVenues.Any())
        {
            var uDamChay = new VeganVenue
            {
                VenueId = 301,
                ExternalSource = "GOONG",
                ExternalPlaceId = "GOONG_HN_01",
                VenueName = "Ưu Đàm Chay",
                Address = "34 Hàng Bài, Hoàn Kiếm, Hà Nội",
                Latitude = 21.022700m,
                Longitude = 105.852400m,
                Rating = 4.8m,
                ThumbnailUrl = "https://res.cloudinary.com/veggielife/venues/uudam.webp",
                IsFullyVegan = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var nangTam = new VeganVenue
            {
                VenueId = 302,
                ExternalSource = "GOONG",
                ExternalPlaceId = "GOONG_HN_02",
                VenueName = "Cơm Chay Nàng Tấm",
                Address = "79A Trần Hưng Đạo, Hoàn Kiếm, Hà Nội",
                Latitude = 21.023500m,
                Longitude = 105.845000m,
                Rating = 4.5m,
                ThumbnailUrl = "https://res.cloudinary.com/veggielife/venues/nangtam.webp",
                IsFullyVegan = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var viLai = new VeganVenue
            {
                VenueId = 303,
                ExternalSource = "GOONG",
                ExternalPlaceId = "GOONG_HN_03",
                VenueName = "Nhà Hàng Chay Vị Lai",
                Address = "67 Lý Thường Kiệt, Hoàn Kiếm, Hà Nội",
                Latitude = 21.024500m,
                Longitude = 105.848000m,
                Rating = 4.9m,
                ThumbnailUrl = "https://res.cloudinary.com/veggielife/venues/vilai.webp",
                IsFullyVegan = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var tinhThuc = new VeganVenue
            {
                VenueId = 304,
                ExternalSource = "GOONG",
                ExternalPlaceId = "GOONG_HN_04",
                VenueName = "Tịnh Thực Quán",
                Address = "10 Huỳnh Thúc Kháng, Đống Đa, Hà Nội",
                Latitude = 21.018900m,
                Longitude = 105.811500m,
                Rating = 4.6m,
                ThumbnailUrl = "https://res.cloudinary.com/veggielife/venues/tinhthuc.webp",
                IsFullyVegan = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var anLacHn = new VeganVenue
            {
                VenueId = 305,
                ExternalSource = "GOONG",
                ExternalPlaceId = "GOONG_HN_05",
                VenueName = "Cơm Chay An Lạc",
                Address = "109 Trần Hưng Đạo, Cửa Nam, Hoàn Kiếm, Hà Nội",
                Latitude = 21.023000m,
                Longitude = 105.842000m,
                Rating = 4.3m,
                ThumbnailUrl = "https://res.cloudinary.com/veggielife/venues/anlac.webp",
                IsFullyVegan = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var duongSinhLowRatingHn = new VeganVenue
            {
                VenueId = 306,
                ExternalSource = "GOONG",
                ExternalPlaceId = "GOONG_HN_08",
                VenueName = "Quán Chay Dưỡng Sinh Cũ",
                Address = "Ngõ 40 Tôn Đức Thắng, Đống Đa, Hà Nội",
                Latitude = 21.025000m,
                Longitude = 105.835000m,
                Rating = 2.1m,
                ThumbnailUrl = "https://res.cloudinary.com/veggielife/venues/duongsinh.webp",
                IsFullyVegan = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // TP. Hồ Chí Minh
            var tamAnSg = new VeganVenue
            {
                VenueId = 311,
                ExternalSource = "GOONG",
                ExternalPlaceId = "GOONG_SG_01",
                VenueName = "Cơm Chay Tâm An",
                Address = "12 Lê Lợi, Bến Nghé, Quận 1, TP.HCM",
                Latitude = 10.772900m,
                Longitude = 106.698300m,
                Rating = 4.6m,
                ThumbnailUrl = "https://res.cloudinary.com/veggielife/venues/taman.webp",
                IsFullyVegan = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var tibChaySg = new VeganVenue
            {
                VenueId = 312,
                ExternalSource = "GOONG",
                ExternalPlaceId = "GOONG_SG_02",
                VenueName = "Tib Chay Huỳnh Thúc Kháng",
                Address = "11 Huỳnh Thúc Kháng, Bến Nghé, Quận 1, TP.HCM",
                Latitude = 10.771500m,
                Longitude = 106.702200m,
                Rating = 4.8m,
                ThumbnailUrl = "https://res.cloudinary.com/veggielife/venues/tibchay.webp",
                IsFullyVegan = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var humSg = new VeganVenue
            {
                VenueId = 313,
                ExternalSource = "GOONG",
                ExternalPlaceId = "GOONG_SG_03",
                VenueName = "Hum Vegetarian Lounge & Restaurant",
                Address = "32 Võ Văn Tần, Phường 6, Quận 3, TP.HCM",
                Latitude = 10.778100m,
                Longitude = 106.691200m,
                Rating = 4.9m,
                ThumbnailUrl = "https://res.cloudinary.com/veggielife/venues/hum.webp",
                IsFullyVegan = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var maniSg = new VeganVenue
            {
                VenueId = 314,
                ExternalSource = "GOONG",
                ExternalPlaceId = "GOONG_SG_04",
                VenueName = "Quán Chay Mani",
                Address = "291/2 Võ Văn Tần, Phường 5, Quận 3, TP.HCM",
                Latitude = 10.771200m,
                Longitude = 106.682100m,
                Rating = 4.5m,
                ThumbnailUrl = "https://res.cloudinary.com/veggielife/venues/mani.webp",
                IsFullyVegan = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var phapHoaSg = new VeganVenue
            {
                VenueId = 315,
                ExternalSource = "GOONG",
                ExternalPlaceId = "GOONG_SG_05",
                VenueName = "Cơm Chay Pháp Hoa",
                Address = "175 Thích Quảng Đức, Phường 4, Phú Nhuận, TP.HCM",
                Latitude = 10.803100m,
                Longitude = 106.689500m,
                Rating = 4.4m,
                ThumbnailUrl = "https://res.cloudinary.com/veggielife/venues/phaphoa.webp",
                IsFullyVegan = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var viaHeLowRatingSg = new VeganVenue
            {
                VenueId = 316,
                ExternalSource = "GOONG",
                ExternalPlaceId = "GOONG_SG_08",
                VenueName = "Quán Chay Vỉa Hè Cũ",
                Address = "Hẻm 18A Nguyễn Thị Minh Khai, Đa Kao, Quận 1, TP.HCM",
                Latitude = 10.785000m,
                Longitude = 106.699000m,
                Rating = 2.2m,
                ThumbnailUrl = "https://res.cloudinary.com/veggielife/venues/viahe.webp",
                IsFullyVegan = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            db.VeganVenues.AddRange(
                uDamChay, nangTam, viLai, tinhThuc, anLacHn, duongSinhLowRatingHn,
                tamAnSg, tibChaySg, humSg, maniSg, phapHoaSg, viaHeLowRatingSg);

            // Gán danh mục món ăn đại diện (Món Cơm & Xôi Chay: CategoryId = 1)
            var allVenues = new[] { uDamChay, nangTam, viLai, tinhThuc, anLacHn, duongSinhLowRatingHn, tamAnSg, tibChaySg, humSg, maniSg, phapHoaSg, viaHeLowRatingSg };
            foreach (var v in allVenues)
            {
                db.VenueCategories.Add(new VenueCategory
                {
                    VenueId = v.VenueId,
                    CategoryId = 1,
                    IsPrimary = true,
                    AssignedAt = DateTime.UtcNow
                });
            }
        }

        db.SaveChanges();
    }
}
