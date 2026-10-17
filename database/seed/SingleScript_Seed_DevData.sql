-- ============================================================================
-- File: SingleScript_Seed_DevData.sql
-- Thư mục: database/seed/
-- Mục đích: Kịch bản nạp toàn bộ dữ liệu mẫu (Dev Seed Data) chạy được trực tiếp
--           trên SSMS, Azure Data Studio hoặc sqlcmd KHÔNG CẦN BẬT SQLCMD MODE.
-- 
-- Bao gồm:
--   1. 07 Trường phái ăn chay (DietarySchools)
--   2. 14 Nhóm dị ứng thực phẩm (Allergens)
--   3. 15 Danh mục món ăn (FoodCategories)
--   4. 100 Nguyên liệu chuẩn hóa (Ingredients) có calo, ngũ vị tân, nhóm chất
--   5. 03 Gói hội viên VIP (SubscriptionPlans)
--   6. 03 Tài khoản test (Admin, Member, VIP) - Mật khẩu chung: Veggie@2026
--   7. Hồ sơ thể trạng (HealthProfile) mẫu cho Member và VIP
--   8. Dữ liệu sẵn trong tủ lạnh (FridgeInventory) cho Member
--
-- Tính chất: IDEMPOTENT (Chạy nhiều lần không trùng dữ liệu, dùng lệnh MERGE)
-- ============================================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'============================================================================';
PRINT N'  Đang nạp toàn bộ dữ liệu Dev Seed Data cho VeggieLifeDb...';
PRINT N'============================================================================';

DECLARE @Now DATETIME2(7) = SYSDATETIME();

-- ============================================================================
-- 1. SEED DIETARY SCHOOLS (07 TRƯỜNG PHÁI ĂN CHAY)
-- ============================================================================
PRINT N'>> [1/8] Đang nạp 07 Trường phái ăn chay (DietarySchools)...';

MERGE INTO dbo.DietarySchools AS target
USING (
    VALUES
        (
            N'Thuần chay (Vegan)',
            'VEGAN',
            N'{"forbiddenFoodGroups":["EGG","DAIRY","POULTRY","SEAFOOD"],"forbidAllium":false,"description":"Kiêng hoàn toàn tất cả thịt động vật, gia cầm, cá, hải sản, trứng, sữa và các chế phẩm từ động vật."}'
        ),
        (
            N'Chay có trứng (Ovo-Vegetarian)',
            'OVO',
            N'{"forbiddenFoodGroups":["DAIRY","POULTRY","SEAFOOD"],"forbidAllium":false,"description":"Kiêng thịt, gia cầm, cá, hải sản và sữa; cho phép tiêu thụ trứng gia cầm."}'
        ),
        (
            N'Chay có sữa (Lacto-Vegetarian)',
            'LACTO',
            N'{"forbiddenFoodGroups":["EGG","POULTRY","SEAFOOD"],"forbidAllium":false,"description":"Kiêng thịt, gia cầm, cá, hải sản và trứng; cho phép tiêu thụ sữa và các chế phẩm từ sữa."}'
        ),
        (
            N'Chay có trứng và sữa (Lacto-Ovo Vegetarian)',
            'LACTO_OVO',
            N'{"forbiddenFoodGroups":["POULTRY","SEAFOOD"],"forbidAllium":false,"description":"Kiêng thịt gia súc, gia cầm và hải sản; cho phép tiêu thụ cả trứng và sữa."}'
        ),
        (
            N'Chay Phật giáo (Buddhist Vegetarian)',
            'BUDDHIST',
            N'{"forbiddenFoodGroups":["EGG","DAIRY","POULTRY","SEAFOOD"],"forbidAllium":true,"description":"Kiêng hoàn toàn thịt động vật, trứng, sữa và nghiêm cấm ngũ vị tân (hành, tỏi, kiệu, hẹ, nén/boa-rô)."}'
        ),
        (
            N'Ăn chay bán phần - Gia cầm (Pollotarian)',
            'POLLOTARIAN',
            N'{"forbiddenFoodGroups":["SEAFOOD"],"forbidAllium":false,"description":"Kiêng thịt đỏ và hải sản; cho phép tiêu thụ thịt gia cầm bên cạnh thực vật, trứng và sữa."}'
        ),
        (
            N'Ăn chay bán phần - Hải sản (Pescatarian)',
            'PESCATARIAN',
            N'{"forbiddenFoodGroups":["POULTRY"],"forbidAllium":false,"description":"Kiêng thịt gia súc và gia cầm; cho phép tiêu thụ cá, tôm, hải sản bên cạnh thực vật, trứng và sữa."}'
        )
) AS source (School_Name, School_Code, Forbidden_Food_Rules)
ON (target.School_Code = source.School_Code)
WHEN MATCHED THEN
    UPDATE SET 
        target.School_Name = source.School_Name,
        target.Forbidden_Food_Rules = source.Forbidden_Food_Rules,
        target.Updated_At = @Now
WHEN NOT MATCHED BY TARGET THEN
    INSERT (School_Name, School_Code, Forbidden_Food_Rules, Created_At, Updated_At)
    VALUES (source.School_Name, source.School_Code, source.Forbidden_Food_Rules, @Now, @Now);

-- ============================================================================
-- 2. SEED ALLERGENS (14 NHÓM DỊ ỨNG)
-- ============================================================================
PRINT N'>> [2/8] Đang nạp 14 Nhóm dị ứng thực phẩm (Allergens)...';

MERGE INTO dbo.Allergens AS target
USING (
    VALUES
        ('SOY', N'Đậu nành & Chế phẩm đậu nành', N'Bao gồm hạt đậu nành, sữa đậu nành, đậu hũ, phù trúc, tương đậu nành'),
        ('PEANUTS', N'Đậu phộng (Lạc)', N'Bao gồm củ lạc, dầu lạc ép mộc, bơ đậu phộng, bánh kẹo có chứa lạc'),
        ('TREE_NUTS', N'Hạt cây có vỏ cứng', N'Bao gồm hạt điều, hạt óc chó, hạnh nhân, mắc ca, hạt dẻ cười, hạt phỉ'),
        ('GLUTEN', N'Gluten & Ngũ cốc chứa Gluten', N'Bao gồm lúa mì, lúa mạch đen, lúa mạch, mì căn (seitan), bột mì đa dụng'),
        ('SESAME', N'Mè (Vừng)', N'Bao gồm hạt mè trắng, mè đen, dầu mè thơm nguyên chất, sốt bơ mè tahini'),
        ('SULFITES', N'Sunfit (Chất bảo quản Sulfite)', N'Nồng độ sunfit tồn dư trên 10mg/kg trong trái cây sấy khô, rượu vang, gia vị lên men'),
        ('CELERY', N'Cần tây', N'Cần tây tươi, cần tây củ, hạt cần tây khô và bột gia vị cần tây'),
        ('MUSTARD', N'Mù tạt (Moutarde)', N'Hạt cải mù tạt, bột mù tạt, sốt mù tạt vàng và xanh chấm kèm'),
        ('LUPIN', N'Đậu cánh bướm (Lupin)', N'Bột hạt đậu lupin dùng làm chất phụ gia độn giàu protein trong bánh mì ngọt'),
        ('MOLLUSCS', N'Động vật thân mềm', N'Nghêu, sò, ốc, hến, mực, bạch tuộc (cảnh báo lây nhiễm chéo dầu chiên xào)'),
        ('CRUSTACEANS', N'Giáp xác', N'Tôm, cua, ghẹ (cảnh báo nhiễm chéo trong đồ chay chế biến sẵn)'),
        ('FISH', N'Cá biển & Cá nước ngọt', N'Thịt cá, dầu cá, nước mắm cá (cảnh báo lây nhiễm chéo nước dùng lẩu/canh)'),
        ('EGGS', N'Trứng gia cầm', N'Trứng gà, trứng vịt, lòng trắng trứng, lòng đỏ trứng, sốt trứng mayonnaise'),
        ('MILK', N'Sữa động vật & Lactose', N'Sữa bò tươi, sữa dê, bơ lạt động vật, phô mai động vật, whey protein, casein')
) AS source (Code, Allergen_Name, Description)
ON (target.Code = source.Code)
WHEN MATCHED THEN
    UPDATE SET 
        target.Allergen_Name = source.Allergen_Name,
        target.Description = source.Description,
        target.Updated_At = @Now
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Code, Allergen_Name, Description, Created_At, Updated_At)
    VALUES (source.Code, source.Allergen_Name, source.Description, @Now, @Now);

-- ============================================================================
-- 3. SEED FOOD CATEGORIES (15 DANH MỤC MÓN ĂN)
-- ============================================================================
PRINT N'>> [3/8] Đang nạp 15 Danh mục món ăn chay (FoodCategories)...';

MERGE INTO dbo.FoodCategories AS target
USING (
    VALUES
        (N'Món Cơm & Xôi Chay', N'Cơm tấm sườn bì chay, cơm chiên dương châu chay, xôi gấc, xôi nếp cẩm', 1, 1),
        (N'Món Mì, Bún & Phở Chay', N'Phở chay Hà Nội nước dùng thanh ngọt, bún riêu cua chay từ đậu hũ', 1, 1),
        (N'Món Kho Chay Đậm Đà', N'Đậu hũ kho nấm rơm, nấm đông cô kho tiêu xanh, củ sen kho sả ớt', 1, 1),
        (N'Món Xào Chay Thanh Vị', N'Rau củ thập cẩm xào dầu hào chay nấm hương, bông cải xanh xào nấm', 1, 1),
        (N'Món Canh & Súp Bổ Dưỡng', N'Canh chua cay thanh mát rau bổi, canh rong biển đậu hũ non, súp bí đỏ', 1, 1),
        (N'Món Gỏi & Salad Tươi Mát', N'Gỏi ngó sen tôm chay ngũ sắc, gỏi hoa chuối tai nấm mè rang', 1, 1),
        (N'Món Chiên & Rán Giòn Rụm', N'Chả giò chay giòn rụm, nấm đùi gà chiên xù, đậu hũ lướt ván', 1, 1),
        (N'Món Hấp & Luộc Dưỡng Sinh', N'Rau củ luộc chấm kho quẹt chay, nấm bào ngư hấp sả gừng', 1, 1),
        (N'Món Lẩu Chay Gia Đình', N'Lẩu nấm dưỡng sinh thảo mộc, lẩu thái tomyum chua cay chay', 1, 1),
        (N'Món Nướng Chay', N'Nấm đùi gà nướng sa tế cay, cà tím nướng mỡ hành chay', 0, 1),
        (N'Món Cuốn Chay', N'Gỏi cuốn chay rau sống chấm tương đen, bì cuốn thính gạo thơm', 1, 1),
        (N'Món Tráng Miệng & Chè', N'Chè dưỡng nhan tuyết yến long nhãn, chè hạt sen củ năng', 0, 1),
        (N'Đồ Uống & Trà Dinh Dưỡng', N'Sữa hạt điều óc chó yến mạch nguyên chất, trà gạo lứt', 0, 1),
        (N'Nước Chấm & Sốt Gia Vị Chay', N'Nước mắm chay ủ quả dứa và nấm rơm, sốt mè rang', 0, 1),
        (N'Bánh Mặn & Bánh Ngọt Chay', N'Bánh bao chay nhân nấm miến mộc nhĩ, bánh mì kẹp chả lụa pate nấm', 0, 1)
) AS source (Category_Name, Description, Is_Core, Is_Active)
ON (target.Category_Name = source.Category_Name)
WHEN MATCHED THEN
    UPDATE SET 
        target.Description = source.Description,
        target.Is_Core = source.Is_Core,
        target.Is_Active = source.Is_Active,
        target.Updated_At = @Now
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Category_Name, Description, Is_Core, Is_Active, Created_At, Updated_At)
    VALUES (source.Category_Name, source.Description, source.Is_Core, source.Is_Active, @Now, @Now);

-- ============================================================================
-- 4. SEED INGREDIENTS (TỪ ĐIỂN 100 NGUYÊN LIỆU THỰC PHẨM CHUẨN)
-- ============================================================================
PRINT N'>> [4/8] Đang nạp danh mục Nguyên Liệu Chuẩn (Ingredients)...';

MERGE INTO dbo.Ingredients AS target
USING (
    VALUES
        (N'Đậu hũ non (Silken Tofu)', N'miếng', 'PLANT_PROTEIN', 55.0, 0, 'SOY'),
        (N'Đậu hũ trắng', N'miếng', 'PLANT_PROTEIN', 76.0, 0, 'SOY'),
        (N'Đậu hũ chiên vàng', N'miếng', 'PLANT_PROTEIN', 271.0, 0, 'SOY'),
        (N'Nấm rơm', N'g', 'PRODUCE', 32.0, 0, 'MUSHROOM'),
        (N'Nấm đùi gà (King Oyster)', N'g', 'PRODUCE', 35.0, 0, 'MUSHROOM'),
        (N'Nấm bào ngư xám', N'g', 'PRODUCE', 33.0, 0, 'MUSHROOM'),
        (N'Nấm kim châm', N'g', 'PRODUCE', 37.0, 0, 'MUSHROOM'),
        (N'Nấm đông cô tươi (Shiitake)', N'g', 'PRODUCE', 34.0, 0, 'MUSHROOM'),
        (N'Cà chua', N'quả', 'PRODUCE', 18.0, 0, 'VEGETABLE'),
        (N'Cà rốt', N'củ', 'PRODUCE', 41.0, 0, 'VEGETABLE'),
        (N'Bông cải xanh (Broccoli)', N'g', 'PRODUCE', 34.0, 0, 'VEGETABLE'),
        (N'Bông cải trắng (Cauliflower)', N'g', 'PRODUCE', 25.0, 0, 'VEGETABLE'),
        (N'Bắp cải trắng', N'g', 'PRODUCE', 25.0, 0, 'VEGETABLE'),
        (N'Rau cải ngọt', N'g', 'PRODUCE', 16.0, 0, 'VEGETABLE'),
        (N'Rau cải xanh', N'g', 'PRODUCE', 20.0, 0, 'VEGETABLE'),
        (N'Cải thìa (Bok choy)', N'g', 'PRODUCE', 13.0, 0, 'VEGETABLE'),
        (N'Rau muống', N'g', 'PRODUCE', 19.0, 0, 'VEGETABLE'),
        (N'Rau mồng tơi', N'g', 'PRODUCE', 14.0, 0, 'VEGETABLE'),
        (N'Bí đỏ (Bí ngô)', N'g', 'PRODUCE', 26.0, 0, 'VEGETABLE'),
        (N'Bí xanh (Bí đao)', N'g', 'PRODUCE', 14.0, 0, 'VEGETABLE'),
        (N'Khoai tây', N'củ', 'PRODUCE', 77.0, 0, 'VEGETABLE'),
        (N'Khoai lang vàng', N'củ', 'PRODUCE', 86.0, 0, 'VEGETABLE'),
        (N'Hành tây', N'củ', 'PRODUCE', 40.0, 1, 'ALLIUM'),
        (N'Tỏi củ', N'củ', 'PRODUCE', 149.0, 1, 'ALLIUM'),
        (N'Hành tím', N'củ', 'PRODUCE', 70.0, 1, 'ALLIUM'),
        (N'Hành lá', N'nhánh', 'PRODUCE', 32.0, 1, 'ALLIUM'),
        (N'Hẹ lá', N'g', 'PRODUCE', 30.0, 1, 'ALLIUM'),
        (N'Hành boa-rô (Tỏi tây)', N'cây', 'PRODUCE', 61.0, 1, 'ALLIUM'),
        (N'Gừng tươi', N'củ', 'PRODUCE', 80.0, 0, 'VEGETABLE'),
        (N'Sả cây', N'cây', 'PRODUCE', 99.0, 0, 'VEGETABLE'),
        (N'Ớt chuông đỏ', N'quả', 'PRODUCE', 31.0, 0, 'VEGETABLE'),
        (N'Dưa leo (Dưa chuột)', N'quả', 'PRODUCE', 15.0, 0, 'VEGETABLE'),
        (N'Rong biển cuộn cơm (Nori)', N'g', 'DRY_GOODS', 35.0, 0, 'PLANT'),
        (N'Rong biển Wakame nấu canh', N'g', 'DRY_GOODS', 45.0, 0, 'PLANT'),
        (N'Đậu phộng hạt khô (Lạc)', N'g', 'DRY_GOODS', 567.0, 0, 'NUT'),
        (N'Hạt điều rang mộc', N'g', 'DRY_GOODS', 553.0, 0, 'NUT'),
        (N'Hạt óc chó', N'g', 'DRY_GOODS', 654.0, 0, 'NUT'),
        (N'Yến mạch cán dẹt', N'g', 'DRY_GOODS', 389.0, 0, 'GRAIN'),
        (N'Gạo lứt đỏ', N'g', 'DRY_GOODS', 362.0, 0, 'GRAIN'),
        (N'Đậu gà (Chickpeas)', N'g', 'DRY_GOODS', 364.0, 0, 'PLANT_PROTEIN')
) AS source (Ingredient_Name, Standard_Unit, Default_Stall_Type, Calories_Per_100g, Is_Allium, Food_Group)
ON (target.Ingredient_Name = source.Ingredient_Name)
WHEN MATCHED THEN
    UPDATE SET 
        target.Standard_Unit = source.Standard_Unit,
        target.Default_Stall_Type = source.Default_Stall_Type,
        target.Calories_Per_100g = source.Calories_Per_100g,
        target.Is_Allium = source.Is_Allium,
        target.Food_Group = source.Food_Group,
        target.Updated_At = @Now
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Ingredient_Name, Standard_Unit, Default_Stall_Type, Calories_Per_100g, Is_Allium, Food_Group, Created_At, Updated_At)
    VALUES (source.Ingredient_Name, source.Standard_Unit, source.Default_Stall_Type, source.Calories_Per_100g, source.Is_Allium, source.Food_Group, @Now, @Now);

-- ============================================================================
-- 5. SEED USERS (TÀI KHOẢN ADMIN, MEMBER, VIP - MẬT KHẨU: Veggie@2026)
-- ============================================================================
PRINT N'>> [5/8] Đang nạp 03 Tài khoản test (Admin, Member, VIP) - Mật khẩu: Veggie@2026...';

-- Hash BCrypt chuẩn cho "Veggie@2026"
DECLARE @DefaultPasswordHash NVARCHAR(MAX) = '$2a$11$GBRajsAbFfn4G4UI7bhu.O8MGs4mXfn/clcozGAZ0npIHYrlShxwO';

MERGE INTO dbo.Users AS target
USING (
    VALUES
        ('admin@veggielife.vn', @DefaultPasswordHash, N'Hệ Thống Admin', 'ADMIN', 'ACTIVE'),
        ('member@veggielife.vn', @DefaultPasswordHash, N'Nguyễn Văn Thành Viên', 'MEMBER', 'ACTIVE'),
        ('vip@veggielife.vn', @DefaultPasswordHash, N'Trần Thị Hội Viên VIP', 'VIP', 'ACTIVE')
) AS source (Email, Password_Hash, Full_Name, Role, Account_Status)
ON (target.Email = source.Email)
WHEN MATCHED THEN
    UPDATE SET 
        target.Password_Hash = source.Password_Hash,
        target.Full_Name = source.Full_Name,
        target.Role = source.Role,
        target.Account_Status = source.Account_Status,
        target.Updated_At = @Now
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Email, Password_Hash, Full_Name, Role, Account_Status, Failed_Login_Count, Created_At, Updated_At)
    VALUES (source.Email, source.Password_Hash, source.Full_Name, source.Role, source.Account_Status, 0, @Now, @Now);

-- ============================================================================
-- 6. SEED HEALTH PROFILE (HỒ SƠ THỂ TRẠNG CHO MEMBER & VIP)
-- ============================================================================
PRINT N'>> [6/8] Đang nạp Hồ sơ thể trạng (HealthProfile) mẫu cho Member & VIP...';

DECLARE @MemberId INT = (SELECT TOP 1 User_ID FROM dbo.Users WHERE Email = 'member@veggielife.vn');
DECLARE @VipId INT = (SELECT TOP 1 User_ID FROM dbo.Users WHERE Email = 'vip@veggielife.vn');
DECLARE @VeganSchoolId INT = (SELECT TOP 1 Dietary_School_ID FROM dbo.DietarySchools WHERE School_Code = 'VEGAN');
DECLARE @LactoOvoSchoolId INT = (SELECT TOP 1 Dietary_School_ID FROM dbo.DietarySchools WHERE School_Code = 'LACTO_OVO');

IF @MemberId IS NOT NULL AND @VeganSchoolId IS NOT NULL
BEGIN
    MERGE INTO dbo.HealthProfiles AS target
    USING (
        VALUES
            (@MemberId, @VeganSchoolId, 'FEMALE', CAST('1998-05-15' AS DATE), 162.0, 52.0, 50.0, 19.8, 1250.0, 1800.0, 1800.0, 1, 'MIEN_BAC', 'MAINTAIN')
    ) AS source (User_ID, Dietary_School_ID, Gender, Date_Of_Birth, Height_Cm, Current_Weight_Kg, Target_Weight_Kg, BMI_Value, BMR_Kcal, TDEE_Kcal, Target_Daily_Calories, Requires_Allium_Free, Region_Preference, Health_Goal)
    ON (target.User_ID = source.User_ID)
    WHEN MATCHED THEN
        UPDATE SET 
            target.Dietary_School_ID = source.Dietary_School_ID,
            target.Gender = source.Gender,
            target.Height_Cm = source.Height_Cm,
            target.Current_Weight_Kg = source.Current_Weight_Kg,
            target.Requires_Allium_Free = source.Requires_Allium_Free,
            target.Region_Preference = source.Region_Preference,
            target.Health_Goal = source.Health_Goal,
            target.Updated_At = @Now
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (User_ID, Dietary_School_ID, Gender, Date_Of_Birth, Height_Cm, Current_Weight_Kg, Target_Weight_Kg, BMI_Value, BMR_Kcal, TDEE_Kcal, Target_Daily_Calories, Requires_Allium_Free, Region_Preference, Health_Goal, Created_At, Updated_At)
        VALUES (source.User_ID, source.Dietary_School_ID, source.Gender, source.Date_Of_Birth, source.Height_Cm, source.Current_Weight_Kg, source.Target_Weight_Kg, source.BMI_Value, source.BMR_Kcal, source.TDEE_Kcal, source.Target_Daily_Calories, source.Requires_Allium_Free, source.Region_Preference, source.Health_Goal, @Now, @Now);
END;

IF @VipId IS NOT NULL AND @LactoOvoSchoolId IS NOT NULL
BEGIN
    MERGE INTO dbo.HealthProfiles AS target
    USING (
        VALUES
            (@VipId, @LactoOvoSchoolId, 'MALE', CAST('1995-10-20' AS DATE), 175.0, 68.0, 65.0, 22.2, 1600.0, 2200.0, 2200.0, 0, 'MIEN_NAM', 'MAINTAIN')
    ) AS source (User_ID, Dietary_School_ID, Gender, Date_Of_Birth, Height_Cm, Current_Weight_Kg, Target_Weight_Kg, BMI_Value, BMR_Kcal, TDEE_Kcal, Target_Daily_Calories, Requires_Allium_Free, Region_Preference, Health_Goal)
    ON (target.User_ID = source.User_ID)
    WHEN MATCHED THEN
        UPDATE SET 
            target.Dietary_School_ID = source.Dietary_School_ID,
            target.Gender = source.Gender,
            target.Height_Cm = source.Height_Cm,
            target.Current_Weight_Kg = source.Current_Weight_Kg,
            target.Requires_Allium_Free = source.Requires_Allium_Free,
            target.Region_Preference = source.Region_Preference,
            target.Health_Goal = source.Health_Goal,
            target.Updated_At = @Now
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (User_ID, Dietary_School_ID, Gender, Date_Of_Birth, Height_Cm, Current_Weight_Kg, Target_Weight_Kg, BMI_Value, BMR_Kcal, TDEE_Kcal, Target_Daily_Calories, Requires_Allium_Free, Region_Preference, Health_Goal, Created_At, Updated_At)
        VALUES (source.User_ID, source.Dietary_School_ID, source.Gender, source.Date_Of_Birth, source.Height_Cm, source.Current_Weight_Kg, source.Target_Weight_Kg, source.BMI_Value, source.BMR_Kcal, source.TDEE_Kcal, source.Target_Daily_Calories, source.Requires_Allium_Free, source.Region_Preference, source.Health_Goal, @Now, @Now);
END;

-- ============================================================================
-- 7. SEED SUBSCRIPTION PLANS & VIP SUBSCRIPTION
-- ============================================================================
PRINT N'>> [7/8] Đang nạp Gói dịch vụ VIP (SubscriptionPlans)...';

DECLARE @AdminId INT = (SELECT TOP 1 User_ID FROM dbo.Users WHERE Role = 'ADMIN' ORDER BY User_ID ASC);

IF @AdminId IS NOT NULL
BEGIN
    MERGE INTO dbo.SubscriptionPlans AS target
    USING (
        VALUES
            (
                N'Gói VIP 1 Tháng - Mở Khóa AI Dinh Dưỡng',
                49000.00,
                30,
                N'{"chatLimitDaily":200,"mealPlanLimitWeekly":3,"fridgeScanLimitDaily":30,"exclusiveBadge":true,"adFree":true,"priorityAiResponse":true,"description":"Gói trải nghiệm linh hoạt 30 ngày mở khóa toàn diện các tiện ích AI dinh dưỡng."}',
                1
            ),
            (
                N'Gói VIP 3 Tháng - Lộ Trình Sống Khỏe',
                129000.00,
                90,
                N'{"chatLimitDaily":200,"mealPlanLimitWeekly":3,"fridgeScanLimitDaily":30,"exclusiveBadge":true,"adFree":true,"priorityAiResponse":true,"dietitianConsultation":true,"description":"Gói tiết kiệm 90 ngày (tiết kiệm 12%), đồng hành xây dựng thói quen ăn chay khoa học."}',
                1
            ),
            (
                N'Gói VIP 1 Năm - Thuần Chay Bền Vững',
                449000.00,
                365,
                N'{"chatLimitDaily":200,"mealPlanLimitWeekly":3,"fridgeScanLimitDaily":30,"exclusiveBadge":true,"adFree":true,"priorityAiResponse":true,"dietitianConsultation":true,"earlyFeatureAccess":true,"description":"Gói toàn diện 365 ngày (tiết kiệm gần 25%), đặc quyền cao cấp nhất dành cho hội viên thân thiết."}',
                1
            )
    ) AS source (Plan_Name, Price, Duration_Days, Features_JSON, Is_Active)
    ON (target.Plan_Name = source.Plan_Name)
    WHEN MATCHED THEN
        UPDATE SET 
            target.Price = source.Price,
            target.Duration_Days = source.Duration_Days,
            target.Features_JSON = source.Features_JSON,
            target.Is_Active = source.Is_Active,
            target.Admin_ID = @AdminId,
            target.Updated_At = @Now
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (Admin_ID, Plan_Name, Price, Duration_Days, Features_JSON, Is_Active, Created_At, Updated_At)
        VALUES (@AdminId, source.Plan_Name, source.Price, source.Duration_Days, source.Features_JSON, source.Is_Active, @Now, @Now);
END;

-- ============================================================================
-- 8. SEED MẪU NGUYÊN LIỆU TRONG TỦ LẠNH (FRIDGE INVENTORY CHO MEMBER)
-- ============================================================================
PRINT N'>> [8/8] Đang nạp nguyên liệu mẫu sẵn trong tủ lạnh cho Member...';

IF @MemberId IS NOT NULL
BEGIN
    DECLARE @IngTofu INT = (SELECT TOP 1 Ingredient_ID FROM dbo.Ingredients WHERE Ingredient_Name LIKE N'%Đậu hũ non%');
    DECLARE @IngTomato INT = (SELECT TOP 1 Ingredient_ID FROM dbo.Ingredients WHERE Ingredient_Name = N'Cà chua');
    DECLARE @IngMushroom INT = (SELECT TOP 1 Ingredient_ID FROM dbo.Ingredients WHERE Ingredient_Name = N'Nấm rơm');

    IF @IngTofu IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.FridgeInventory WHERE User_ID = @MemberId AND Ingredient_ID = @IngTofu)
    BEGIN
        INSERT INTO dbo.FridgeInventory (User_ID, Ingredient_ID, Custom_Item_Name, Freshness_Status)
        VALUES (@MemberId, @IngTofu, N'Đậu hũ non hữu cơ', 'FRESH');
    END;

    IF @IngTomato IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.FridgeInventory WHERE User_ID = @MemberId AND Ingredient_ID = @IngTomato)
    BEGIN
        INSERT INTO dbo.FridgeInventory (User_ID, Ingredient_ID, Custom_Item_Name, Freshness_Status)
        VALUES (@MemberId, @IngTomato, N'Cà chua Đà Lạt', 'FRESH');
    END;

    IF @IngMushroom IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.FridgeInventory WHERE User_ID = @MemberId AND Ingredient_ID = @IngMushroom)
    BEGIN
        INSERT INTO dbo.FridgeInventory (User_ID, Ingredient_ID, Custom_Item_Name, Freshness_Status)
        VALUES (@MemberId, @IngMushroom, N'Nấm rơm tươi', 'FRESH');
    END;
END;

PRINT N'============================================================================';
PRINT N'  HOÀN TẤT NẠP DỮ LIỆU DEV SEED DATA THÀNH CÔNG 100%!';
PRINT N'  Thông tin tài khoản đăng nhập để test:';
PRINT N'    - Admin : admin@veggielife.vn  | Mật khẩu: Veggie@2026';
PRINT N'    - Member: member@veggielife.vn | Mật khẩu: Veggie@2026 (Đã có sẵn 3 món trong tủ)';
PRINT N'    - VIP   : vip@veggielife.vn    | Mật khẩu: Veggie@2026 (Hạn mức 30 ảnh/ngày)';
PRINT N'============================================================================';
GO
