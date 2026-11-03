-- ============================================================================
-- File: 18_Seed_Member3_MealPlans_Shopping_Health.sql
-- Thư mục: database/seed/
-- Mục đích: Kịch bản nạp dữ liệu Seed Data cho phân hệ của THÀNH VIÊN 3:
--           "Thực Đơn Tuần, Đi Chợ & Hồ Sơ Sức Khỏe (Cá nhân hóa & Sức khỏe)"
-- Dải ID phân bổ: 301 – 399
--
-- Nội dung dữ liệu mẫu chuẩn theo phân công đồ án:
--   1. 02 Thực đơn tuần hoàn chỉnh (Plan_ID: 301, 302) lấy món của TV1 ghép vào
--      Sáng / Trưa / Tối từ T2-CN (21 slots/plan), đảm bảo tổng calo mỗi ngày ~ 1800-2000 kcal.
--   2. 01 Thực đơn tuần đã hủy (Plan_ID: 303, Plan_Status = 'CANCELLED') để test hoàn quota.
--   3. 01 Danh sách đi chợ thông minh tự động (ShoppingLists: 301 & 24 ShoppingListItems)
--      phân loại đầy đủ vào 4 quầy hàng tiện lợi: Rau củ quả, Đạm thực vật, Đồ khô, Gia vị.
--   4. Hồ sơ sức khỏe (HealthProfiles) & Nhập lịch sử cân nặng 4 tuần (UserWeightHistory)
--      để vẽ biểu đồ tiến độ theo dõi sức khỏe tại trang cá nhân ( /profile ).
--   5. Ghi nhận lịch sử đổi món mẫu (MealSlotSwapHistories: 301) để test tính năng swap.
--
-- Tính chất: IDEMPOTENT (Chạy nhiều lần không trùng dữ liệu, dùng MERGE / IF EXISTS).
-- ============================================================================

USE VeggieLifeDb;
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'============================================================================';
PRINT N'  >> [Seed 18] Đang nạp Dữ liệu Mẫu Thành viên 3 (ID Range: 301 - 399)...';
PRINT N'============================================================================';

DECLARE @Now DATETIME2(7) = SYSDATETIME();

-- ============================================================================
-- PHẦN 1: HỒ SƠ THỂ TRẠNG & SỨC KHỎE (HEALTHPROFILES)
-- ============================================================================
PRINT N'   [1/5] Nạp Hồ sơ thể trạng sức khỏe (HealthProfiles) cho các tài khoản người dùng...';

MERGE INTO dbo.HealthProfiles AS target
USING (
    VALUES
        (18, 1, 'FEMALE', CAST('1998-07-19' AS DATE), 170.0, 66.0, 63.0, 22.8, 1450.0, 2050.0, 1900.0, 0, 'SOUTH', 'LOSE_WEIGHT'),
        (1,  1, 'MALE',   CAST('1995-06-15' AS DATE), 172.0, 68.0, 64.0, 23.0, 1620.0, 2200.0, 1900.0, 0, 'SOUTH', 'LOSE_WEIGHT'),
        (106, 1, 'FEMALE', CAST('2000-03-12' AS DATE), 165.0, 62.0, 58.0, 22.8, 1450.0, 2000.0, 1850.0, 0, 'SOUTH', 'LOSE_WEIGHT')
) AS source (
    User_ID, Dietary_School_ID, Gender, Date_Of_Birth, Height_Cm, Current_Weight_Kg, Target_Weight_Kg,
    BMI_Value, BMR_Kcal, TDEE_Kcal, Target_Daily_Calories, Requires_Allium_Free, Region_Preference, HealthGoal
)
ON target.User_ID = source.User_ID
WHEN MATCHED THEN
    UPDATE SET
        target.Dietary_School_ID = source.Dietary_School_ID,
        target.Gender = source.Gender,
        target.Date_Of_Birth = source.Date_Of_Birth,
        target.Height_Cm = source.Height_Cm,
        target.Current_Weight_Kg = source.Current_Weight_Kg,
        target.Target_Weight_Kg = source.Target_Weight_Kg,
        target.BMI_Value = source.BMI_Value,
        target.BMR_Kcal = source.BMR_Kcal,
        target.TDEE_Kcal = source.TDEE_Kcal,
        target.Target_Daily_Calories = source.Target_Daily_Calories,
        target.Requires_Allium_Free = source.Requires_Allium_Free,
        target.Region_Preference = source.Region_Preference,
        target.HealthGoal = source.HealthGoal,
        target.Updated_At = @Now
WHEN NOT MATCHED THEN
    INSERT (
        User_ID, Dietary_School_ID, Gender, Date_Of_Birth, Height_Cm, Current_Weight_Kg, Target_Weight_Kg,
        BMI_Value, BMR_Kcal, TDEE_Kcal, Target_Daily_Calories, Requires_Allium_Free, Region_Preference,
        Created_At, Updated_At, Is_Stale_Metrics, HealthGoal
    )
    VALUES (
        source.User_ID, source.Dietary_School_ID, source.Gender, source.Date_Of_Birth, source.Height_Cm,
        source.Current_Weight_Kg, source.Target_Weight_Kg, source.BMI_Value, source.BMR_Kcal, source.TDEE_Kcal,
        source.Target_Daily_Calories, source.Requires_Allium_Free, source.Region_Preference,
        @Now, @Now, 0, source.HealthGoal
    );

-- ============================================================================
-- PHẦN 2: LỊCH SỬ CÂN NẶNG 4 TUẦN (USERWEIGHTHISTORY) VẼ BIỂU ĐỒ PROFILE
-- ============================================================================
PRINT N'   [2/5] Nạp Lịch sử cân nặng 4 tuần (UserWeightHistory) để vẽ biểu đồ theo dõi sức khỏe...';

DELETE FROM dbo.UserWeightHistory WHERE User_ID IN (1, 18, 106);

INSERT INTO dbo.UserWeightHistory (User_ID, Weight_Kg, Logged_Date, Note, Created_At)
VALUES
    -- User 18 (Tài khoản đang đăng nhập của TV3)
    (18, 68.5, CAST(DATEADD(DAY, -28, @Now) AS DATE), N'Bắt đầu theo thực đơn tuần VeggieLife', DATEADD(DAY, -28, @Now)),
    (18, 67.8, CAST(DATEADD(DAY, -21, @Now) AS DATE), N'Giảm 0.7kg sau tuần đầu, cơ thể nhẹ nhàng hơn', DATEADD(DAY, -21, @Now)),
    (18, 67.2, CAST(DATEADD(DAY, -14, @Now) AS DATE), N'Duy trì ăn đúng khẩu phần, tiêu hóa rất êm', DATEADD(DAY, -14, @Now)),
    (18, 66.6, CAST(DATEADD(DAY, -7, @Now) AS DATE),  N'Cảm thấy nhiều năng lượng và ngủ ngon giấc', DATEADD(DAY, -7, @Now)),
    (18, 66.0, CAST(@Now AS DATE),                    N'Đạt mốc 66.0kg, tiến gần mục tiêu 63kg!', @Now),

    -- User 1 (Admin)
    (1, 70.2, CAST(DATEADD(DAY, -28, @Now) AS DATE), N'Bắt đầu theo thực đơn tuần VeggieLife', DATEADD(DAY, -28, @Now)),
    (1, 69.5, CAST(DATEADD(DAY, -21, @Now) AS DATE), N'Giảm 0.7kg, cơ thể sảng khoái', DATEADD(DAY, -21, @Now)),
    (1, 68.8, CAST(DATEADD(DAY, -14, @Now) AS DATE), N'Duy trì chế độ ăn thực vật đủ đạm', DATEADD(DAY, -14, @Now)),
    (1, 68.3, CAST(DATEADD(DAY, -7, @Now) AS DATE),  N'Vóc dáng thon gọn, giảm mỡ vòng bụng', DATEADD(DAY, -7, @Now)),
    (1, 67.8, CAST(@Now AS DATE),                    N'Đạt mốc 67.8kg, sức khỏe rất tốt', @Now),

    -- User 106 (Thành viên thử nghiệm)
    (106, 64.5, CAST(DATEADD(DAY, -28, @Now) AS DATE), N'Bắt đầu liệu trình thực đơn tuần VeggieLife', DATEADD(DAY, -28, @Now)),
    (106, 63.8, CAST(DATEADD(DAY, -21, @Now) AS DATE), N'Giảm 0.7kg sau tuần đầu', DATEADD(DAY, -21, @Now)),
    (106, 63.1, CAST(DATEADD(DAY, -14, @Now) AS DATE), N'Ăn ngon miệng và đầy đủ dinh dưỡng', DATEADD(DAY, -14, @Now)),
    (106, 62.5, CAST(DATEADD(DAY, -7, @Now) AS DATE),  N'Giảm mỡ bụng rõ rệt, vóc dáng thon gọn', DATEADD(DAY, -7, @Now)),
    (106, 62.0, CAST(@Now AS DATE),                    N'Đạt mốc 62.0kg, sức khỏe dồi dào!', @Now);

-- ============================================================================
-- PHẦN 3: THỰC ĐƠN TUẦN CỦA THÀNH VIÊN 3 (MEALPLANS & MEALSLOTS)
-- ============================================================================
PRINT N'   [3/5] Nạp 03 Thực đơn tuần Thành viên 3 (Plan 301, 302 hoàn chỉnh ~1860-1900 kcal, Plan 303 đã hủy)...';

-- Lưu trữ các thực đơn cũ sang ARCHIVED
UPDATE dbo.MealPlans SET Plan_Status = 'ARCHIVED' WHERE User_ID IN (18, 1, 106) AND Plan_ID NOT IN (301, 311, 321);

DELETE FROM dbo.ShoppingListItems WHERE List_ID IN (SELECT List_ID FROM dbo.ShoppingLists WHERE Plan_ID IN (301, 302, 303, 311, 312, 313, 321, 322, 323));
DELETE FROM dbo.ShoppingLists WHERE Plan_ID IN (301, 302, 303, 311, 312, 313, 321, 322, 323);
DELETE FROM dbo.MealSlotSwapHistories WHERE Slot_ID IN (SELECT Slot_ID FROM dbo.MealSlots WHERE Plan_ID IN (301, 302, 303, 311, 312, 313, 321, 322, 323));
DELETE FROM dbo.MealSlots WHERE Plan_ID IN (301, 302, 303, 311, 312, 313, 321, 322, 323);

SET IDENTITY_INSERT dbo.MealPlans ON;

MERGE INTO dbo.MealPlans AS target
USING (
    VALUES
        -- User 18 Plans
        (301, 18, 1, N'Thực đơn thuần chay 7 ngày tăng cường đề kháng (Chuẩn 1800 - 2000 kcal)', 
         CAST('2026-11-02' AS DATE), CAST('2026-11-08' AS DATE), 1900.0, 'STANDARD', 
         N'Giàu đạm thực vật, thanh đạm, ít dầu mỡ, ưu tiên rau củ luộc và nấm tươi', 'ACTIVE', 1, 21, 20, 7),

        (302, 18, 1, N'Thực đơn chay thực dưỡng 7 ngày thanh lọc & nhẹ bụng', 
         CAST('2026-11-09' AS DATE), CAST('2026-11-15' AS DATE), 1850.0, 'STANDARD', 
         N'Ít calo, nhiều rau củ luộc, thanh lọc gan thận, không ngũ vị tân', 'DRAFT', 1, 21, 21, 7),

        (303, 18, 1, N'Thực đơn chay Eat Clean giảm mỡ cấp tốc (Đã hủy để test hoàn quota)', 
         CAST('2026-10-26' AS DATE), CAST('2026-11-01' AS DATE), 1600.0, 'ECONOMICAL', 
         N'Eat clean, giảm calo tối đa', 'CANCELLED', 1, 21, 21, 7),

        -- User 1 (Admin) Plans
        (311, 1, 1, N'Thực đơn thuần chay 7 ngày tăng đề kháng (Admin - Chuẩn 1800 - 2000 kcal)', 
         CAST('2026-11-02' AS DATE), CAST('2026-11-08' AS DATE), 1900.0, 'STANDARD', 
         N'Giàu đạm thực vật, thanh đạm, ít dầu mỡ', 'ACTIVE', 1, 21, 20, 7),

        (312, 1, 1, N'Thực đơn chay thực dưỡng 7 ngày thanh lọc (Admin)', 
         CAST('2026-11-09' AS DATE), CAST('2026-11-15' AS DATE), 1850.0, 'STANDARD', 
         N'Ít calo, nhiều rau củ luộc', 'DRAFT', 1, 21, 21, 7),

        (313, 1, 1, N'Thực đơn chay Eat Clean giảm mỡ (Admin - Đã hủy để test hoàn quota)', 
         CAST('2026-10-26' AS DATE), CAST('2026-11-01' AS DATE), 1600.0, 'ECONOMICAL', 
         N'Eat clean, giảm calo tối đa', 'CANCELLED', 1, 21, 21, 7),

        -- User 106 Plans
        (321, 106, 1, N'Thực đơn thuần chay 7 ngày tăng đề kháng (TV3 - Chuẩn 1800 - 2000 kcal)', 
         CAST('2026-11-02' AS DATE), CAST('2026-11-08' AS DATE), 1900.0, 'STANDARD', 
         N'Giàu đạm thực vật, thanh đạm, ít dầu mỡ', 'ACTIVE', 1, 21, 20, 7),

        (322, 106, 1, N'Thực đơn chay thực dưỡng 7 ngày thanh lọc (TV3)', 
         CAST('2026-11-09' AS DATE), CAST('2026-11-15' AS DATE), 1850.0, 'STANDARD', 
         N'Ít calo, nhiều rau củ luộc', 'DRAFT', 1, 21, 21, 7),

        (323, 106, 1, N'Thực đơn chay Eat Clean giảm mỡ (TV3 - Đã hủy để test hoàn quota)', 
         CAST('2026-10-26' AS DATE), CAST('2026-11-01' AS DATE), 1600.0, 'ECONOMICAL', 
         N'Eat clean, giảm calo tối đa', 'CANCELLED', 1, 21, 21, 7)
) AS source (
    Plan_ID, User_ID, Dietary_School_ID, Plan_Name, Start_Date, End_Date, Target_Daily_Kcal,
    Budget_Tier, Custom_Instructions, Plan_Status, Generated_By_AI, Max_Swaps_Allowed, Swaps_Remaining, Duration_Days
)
ON target.Plan_ID = source.Plan_ID
WHEN MATCHED THEN
    UPDATE SET
        target.User_ID = source.User_ID,
        target.Dietary_School_ID = source.Dietary_School_ID,
        target.Plan_Name = source.Plan_Name,
        target.Start_Date = source.Start_Date,
        target.End_Date = source.End_Date,
        target.Target_Daily_Kcal = source.Target_Daily_Kcal,
        target.Budget_Tier = source.Budget_Tier,
        target.Custom_Instructions = source.Custom_Instructions,
        target.Plan_Status = source.Plan_Status,
        target.Generated_By_AI = source.Generated_By_AI,
        target.Max_Swaps_Allowed = source.Max_Swaps_Allowed,
        target.Swaps_Remaining = source.Swaps_Remaining,
        target.Duration_Days = source.Duration_Days
WHEN NOT MATCHED THEN
    INSERT (
        Plan_ID, User_ID, Dietary_School_ID, Plan_Name, Start_Date, End_Date, Target_Daily_Kcal,
        Budget_Tier, Custom_Instructions, Plan_Status, Generated_By_AI, Max_Swaps_Allowed, Swaps_Remaining, Duration_Days, Created_At
    )
    VALUES (
        source.Plan_ID, source.User_ID, source.Dietary_School_ID, source.Plan_Name, source.Start_Date, source.End_Date,
        source.Target_Daily_Kcal, source.Budget_Tier, source.Custom_Instructions, source.Plan_Status,
        source.Generated_By_AI, source.Max_Swaps_Allowed, source.Swaps_Remaining, source.Duration_Days, @Now
    );

SET IDENTITY_INSERT dbo.MealPlans OFF;

-- ============================================================================
-- PHÂN BỔ 21 BỮA ĂN (MEALSLOTS) CHO THỰC ĐƠN HOÀN CHỈNH
-- TỔNG CALO MỖI NGÀY: SÁNG (~510) + TRƯA (~750) + TỐI (~620) = ~ 1860 - 1900 KCAL
-- ============================================================================
PRINT N'   [3.1] Nạp 21 bữa ăn MealSlots cho Thực đơn #301, #311, #321...';

-- THỰC ĐƠN HOÀN CHỈNH #301, #311, #321 (7 ngày x 3 bữa = 21 slots)
DECLARE @PlanList TABLE (Plan_ID INT);
INSERT INTO @PlanList VALUES (301), (311), (321);

DECLARE @CurrentPlanId INT;
DECLARE plan_cursor CURSOR FOR SELECT Plan_ID FROM @PlanList;
OPEN plan_cursor;
FETCH NEXT FROM plan_cursor INTO @CurrentPlanId;

WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO dbo.MealSlots (Plan_ID, Recipe_ID, Day_Of_Week, Meal_Type, Is_Completed, Notes)
    VALUES
        -- Thứ Hai (Tổng: 520 + 760 + 620 = 1900 kcal)
        (@CurrentPlanId, 129, 'MON', 'BREAKFAST', 1, N'Bún bò Huế chay truyền thống (520 kcal)'),
        (@CurrentPlanId, 169, 'MON', 'LUNCH',     1, N'Cơm tấm sườn bì chả chay (760 kcal)'),
        (@CurrentPlanId, 181, 'MON', 'DINNER',    1, N'Đậu hũ kho nấm rơm tiêu đen (620 kcal)'),

        -- Thứ Ba (Tổng: 510 + 740 + 610 = 1860 kcal)
        (@CurrentPlanId, 133, 'TUE', 'BREAKFAST', 1, N'Phở chay Hà Nội nước dùng thanh ngọt (510 kcal)'),
        (@CurrentPlanId, 165, 'TUE', 'LUNCH',     1, N'Cơm chiên Dương Châu ngũ sắc (740 kcal)'),
        (@CurrentPlanId, 189, 'TUE', 'DINNER',    1, N'Cà tím kho tộ nước cốt dừa (610 kcal)'),

        -- Thứ Tư (Tổng: 500 + 750 + 630 = 1880 kcal - Hôm nay)
        (@CurrentPlanId, 141, 'WED', 'BREAKFAST', 1, N'Hủ tiếu Nam Vang chay (500 kcal)'),
        (@CurrentPlanId, 173, 'WED', 'LUNCH',     0, N'Cơm niêu nấm kho tộ cháy giòn (750 kcal)'),
        (@CurrentPlanId, 161, 'WED', 'DINNER',    0, N'Lẩu nấm dưỡng sinh thảo mộc (630 kcal)'),

        -- Thứ Năm (Tổng: 530 + 760 + 610 = 1900 kcal)
        (@CurrentPlanId, 145, 'THU', 'BREAKFAST', 0, N'Bánh canh nấm rơm bột gạo (530 kcal)'),
        (@CurrentPlanId, 169, 'THU', 'LUNCH',     0, N'Cơm tấm sườn bì chả chay (760 kcal)'),
        (@CurrentPlanId, 185, 'THU', 'DINNER',    0, N'Nấm đông cô kho tiêu hạt sen (610 kcal)'),

        -- Thứ Sáu (Tổng: 540 + 740 + 620 = 1900 kcal)
        (@CurrentPlanId, 149, 'FRI', 'BREAKFAST', 0, N'Mì Quảng chay sợi vàng đậm đà (540 kcal)'),
        (@CurrentPlanId, 165, 'FRI', 'LUNCH',     0, N'Cơm chiên Dương Châu ngũ sắc (740 kcal)'),
        (@CurrentPlanId, 181, 'FRI', 'DINNER',    0, N'Đậu hũ kho nấm rơm tiêu đen (620 kcal)'),

        -- Thứ Bảy (Tổng: 480 + 750 + 630 = 1860 kcal)
        (@CurrentPlanId, 205, 'SAT', 'BREAKFAST', 0, N'Sinh tố bơ chuối hạt chia giàu đạm (480 kcal)'),
        (@CurrentPlanId, 173, 'SAT', 'LUNCH',     0, N'Cơm niêu nấm kho tộ cháy giòn (750 kcal)'),
        (@CurrentPlanId, 161, 'SAT', 'DINNER',    0, N'Lẩu nấm dưỡng sinh thảo mộc (630 kcal)'),

        -- Chủ Nhật (Tổng: 550 + 760 + 590 = 1900 kcal)
        (@CurrentPlanId, 177, 'SUN', 'BREAKFAST', 0, N'Xôi gấc hạt sen chay dẻo thơm (550 kcal)'),
        (@CurrentPlanId, 169, 'SUN', 'LUNCH',     0, N'Cơm tấm sườn bì chả chay (760 kcal)'),
        (@CurrentPlanId, 193, 'SUN', 'DINNER',    0, N'Gỏi cuốn chay ngũ sắc sốt tương (590 kcal)');

    FETCH NEXT FROM plan_cursor INTO @CurrentPlanId;
END;
CLOSE plan_cursor;
DEALLOCATE plan_cursor;

-- THỰC ĐƠN TUẦN TIẾP THEO (DRAFT) #302, #312, #322 (~1880 kcal/ngày)
DECLARE @DraftPlanList TABLE (Plan_ID INT);
INSERT INTO @DraftPlanList VALUES (302), (312), (322);

DECLARE draft_cursor CURSOR FOR SELECT Plan_ID FROM @DraftPlanList;
OPEN draft_cursor;
FETCH NEXT FROM draft_cursor INTO @CurrentPlanId;

WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO dbo.MealSlots (Plan_ID, Recipe_ID, Day_Of_Week, Meal_Type, Is_Completed, Notes)
    VALUES
        (@CurrentPlanId, 133, 'MON', 'BREAKFAST', 0, N'Phở chay Hà Nội (510 kcal)'),
        (@CurrentPlanId, 165, 'MON', 'LUNCH',     0, N'Cơm chiên Dương Châu (740 kcal)'),
        (@CurrentPlanId, 181, 'MON', 'DINNER',    0, N'Đậu hũ kho nấm rơm (620 kcal)'),

        (@CurrentPlanId, 129, 'TUE', 'BREAKFAST', 0, N'Bún bò Huế chay (520 kcal)'),
        (@CurrentPlanId, 173, 'TUE', 'LUNCH',     0, N'Cơm niêu nấm kho tộ (750 kcal)'),
        (@CurrentPlanId, 189, 'TUE', 'DINNER',    0, N'Cà tím kho tộ (610 kcal)'),

        (@CurrentPlanId, 141, 'WED', 'BREAKFAST', 0, N'Hủ tiếu Nam Vang chay (500 kcal)'),
        (@CurrentPlanId, 169, 'WED', 'LUNCH',     0, N'Cơm tấm sườn bì chả (760 kcal)'),
        (@CurrentPlanId, 161, 'WED', 'DINNER',    0, N'Lẩu nấm dưỡng sinh (630 kcal)'),

        (@CurrentPlanId, 145, 'THU', 'BREAKFAST', 0, N'Bánh canh nấm rơm (530 kcal)'),
        (@CurrentPlanId, 165, 'THU', 'LUNCH',     0, N'Cơm chiên Dương Châu (740 kcal)'),
        (@CurrentPlanId, 185, 'THU', 'DINNER',    0, N'Nấm đông cô kho tiêu (610 kcal)'),

        (@CurrentPlanId, 149, 'FRI', 'BREAKFAST', 0, N'Mì Quảng chay (540 kcal)'),
        (@CurrentPlanId, 173, 'FRI', 'LUNCH',     0, N'Cơm niêu nấm kho tộ (750 kcal)'),
        (@CurrentPlanId, 181, 'FRI', 'DINNER',    0, N'Đậu hũ kho nấm rơm (620 kcal)'),

        (@CurrentPlanId, 205, 'SAT', 'BREAKFAST', 0, N'Sinh tố bơ chuối hạt chia (480 kcal)'),
        (@CurrentPlanId, 169, 'SAT', 'LUNCH',     0, N'Cơm tấm sườn bì chả (760 kcal)'),
        (@CurrentPlanId, 161, 'SAT', 'DINNER',    0, N'Lẩu nấm dưỡng sinh (630 kcal)'),

        (@CurrentPlanId, 177, 'SUN', 'BREAKFAST', 0, N'Xôi gấc hạt sen chay (550 kcal)'),
        (@CurrentPlanId, 165, 'SUN', 'LUNCH',     0, N'Cơm chiên ngũ sắc (740 kcal)'),
        (@CurrentPlanId, 193, 'SUN', 'DINNER',    0, N'Gỏi cuốn chay ngũ sắc (590 kcal)');

    FETCH NEXT FROM draft_cursor INTO @CurrentPlanId;
END;
CLOSE draft_cursor;
DEALLOCATE draft_cursor;

-- THỰC ĐƠN ĐÃ HỦY (CANCELLED) #303, #313, #323
INSERT INTO dbo.MealSlots (Plan_ID, Recipe_ID, Day_Of_Week, Meal_Type, Is_Completed, Notes)
SELECT 303, Recipe_ID, Day_Of_Week, Meal_Type, 0, Notes FROM dbo.MealSlots WHERE Plan_ID = 301 AND Slot_ID % 2 = 0;
INSERT INTO dbo.MealSlots (Plan_ID, Recipe_ID, Day_Of_Week, Meal_Type, Is_Completed, Notes)
SELECT 313, Recipe_ID, Day_Of_Week, Meal_Type, 0, Notes FROM dbo.MealSlots WHERE Plan_ID = 311 AND Slot_ID % 2 = 0;
INSERT INTO dbo.MealSlots (Plan_ID, Recipe_ID, Day_Of_Week, Meal_Type, Is_Completed, Notes)
SELECT 323, Recipe_ID, Day_Of_Week, Meal_Type, 0, Notes FROM dbo.MealSlots WHERE Plan_ID = 321 AND Slot_ID % 2 = 0;

-- ============================================================================
-- PHẦN 4: DANH SÁCH ĐI CHỢ TỰ ĐỘNG (SHOPPINGLISTS & SHOPPINGLISTITEMS)
-- ============================================================================
PRINT N'   [4/5] Nạp Danh sách đi chợ tự động #301, #311, #321 phân theo 4 quầy tiện lợi...';

SET IDENTITY_INSERT dbo.ShoppingLists ON;

MERGE INTO dbo.ShoppingLists AS target
USING (
    VALUES
        (301, 301, N'Danh sách đi chợ thực đơn tuần tăng đề kháng (Kế hoạch #301)', CAST('2026-11-02' AS DATE), 'PENDING', 0),
        (311, 311, N'Danh sách đi chợ thực đơn tuần tăng đề kháng (Kế hoạch #311)', CAST('2026-11-02' AS DATE), 'PENDING', 0),
        (321, 321, N'Danh sách đi chợ thực đơn tuần tăng đề kháng (Kế hoạch #321)', CAST('2026-11-02' AS DATE), 'PENDING', 0)
) AS source (List_ID, Plan_ID, List_Title, Target_Shopping_Date, Status, Is_Offline_Synced)
ON target.List_ID = source.List_ID
WHEN MATCHED THEN
    UPDATE SET
        target.Plan_ID = source.Plan_ID,
        target.List_Title = source.List_Title,
        target.Target_Shopping_Date = source.Target_Shopping_Date,
        target.Status = source.Status,
        target.Is_Offline_Synced = source.Is_Offline_Synced
WHEN NOT MATCHED THEN
    INSERT (List_ID, Plan_ID, List_Title, Target_Shopping_Date, Status, Is_Offline_Synced, Created_At)
    VALUES (source.List_ID, source.Plan_ID, source.List_Title, source.Target_Shopping_Date, source.Status, source.Is_Offline_Synced, @Now);

SET IDENTITY_INSERT dbo.ShoppingLists OFF;

-- Nạp các mặt hàng nguyên liệu theo 4 quầy chuẩn của VeggieLife
DECLARE @ListIdTable TABLE (List_ID INT);
INSERT INTO @ListIdTable VALUES (301), (311), (321);

DECLARE @CurListId INT;
DECLARE list_cursor CURSOR FOR SELECT List_ID FROM @ListIdTable;
OPEN list_cursor;
FETCH NEXT FROM list_cursor INTO @CurListId;

WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO dbo.ShoppingListItems (List_ID, Ingredient_ID, Aggregated_Quantity, Unit, Is_Purchased)
    VALUES
        -- 1. QUẦY RAU CỦ QUẢ (PRODUCE)
        (@CurListId, 12, 600.0, 'g', 1),  -- Bông cải xanh
        (@CurListId, 18, 500.0, 'g', 1),  -- Cà rốt tươi
        (@CurListId, 17, 800.0, 'g', 0),  -- Cà chua chín đỏ
        (@CurListId, 19, 600.0, 'g', 0),  -- Cà tím dài
        (@CurListId, 82, 450.0, 'g', 1),  -- Nấm đùi gà
        (@CurListId, 90, 500.0, 'g', 0),  -- Nấm rơm tươi
        (@CurListId, 5,  400.0, 'g', 0),  -- Bắp ngọt tươi
        (@CurListId, 31, 300.0, 'g', 1),  -- Đậu bắp tươi
        (@CurListId, 24, 300.0, 'g', 0),  -- Cần tây cọng tươi
        (@CurListId, 2,  400.0, 'g', 0),  -- Bắp cải tím

        -- 2. QUẦY ĐẬU HẠT & ĐẠM THỰC VẬT (PLANT_PROTEIN)
        (@CurListId, 39, 4.0,   N'miếng', 1), -- Đậu hũ non
        (@CurListId, 38, 6.0,   N'miếng', 1), -- Đậu hũ chiên
        (@CurListId, 25, 300.0, 'g',       0), -- Chả lụa chay
        (@CurListId, 9,  250.0, 'g',       1), -- Bò lát chay khô (TVP)
        (@CurListId, 66, 200.0, 'g',       0), -- Hạt sen tươi
        (@CurListId, 40, 4.0,   N'miếng', 1), -- Đậu hũ trắng

        -- 3. QUẦY ĐỒ KHÔ (DRY_GOODS)
        (@CurListId, 52, 2.0,   'kg', 1), -- Gạo thơm
        (@CurListId, 16, 500.0, 'g',  1), -- Bún gạo khô
        (@CurListId, 1,  200.0, 'g',  0), -- Bánh tráng gỏi cuốn
        (@CurListId, 44, 150.0, 'g',  1), -- Đậu phộng hạt khô

        -- 4. QUẦY GIA VỊ (CONDIMENTS)
        (@CurListId, 96, 1.0,   N'chai', 1), -- Nước tương đậu nành
        (@CurListId, 42, 1.0,   N'chai', 0), -- Dầu mè thơm thượng hạng
        (@CurListId, 37, 1.0,   N'chai', 1), -- Dầu hào chay nấm hương
        (@CurListId, 113, 100.0, 'g',    0); -- Tiêu đen xay nguyên chất

    FETCH NEXT FROM list_cursor INTO @CurListId;
END;
CLOSE list_cursor;
DEALLOCATE list_cursor;

-- ============================================================================
-- PHẦN 5: LỊCH SỬ ĐỔI MÓN (MEALSLOTSWAPHISTORIES)
-- ============================================================================
PRINT N'   [5/5] Nạp Lịch sử đổi món mẫu (MealSlotSwapHistories)...';

DECLARE @SampleSlotId INT = (SELECT TOP 1 Slot_ID FROM dbo.MealSlots WHERE Plan_ID = 301 ORDER BY Slot_ID ASC);
IF @SampleSlotId IS NOT NULL
BEGIN
    DELETE FROM dbo.MealSlotSwapHistories WHERE Slot_ID = @SampleSlotId;
    INSERT INTO dbo.MealSlotSwapHistories (Slot_ID, Old_Recipe_ID, New_Recipe_ID, Swap_Reason, Calories_Delta, Swapped_At)
    VALUES (@SampleSlotId, 101, 129, N'Muốn đổi sang món nước ấm nóng, nhiều rau thơm', 30.0, DATEADD(HOUR, -2, @Now));
END;

PRINT N'============================================================================';
PRINT N'  >> HOÀN TẤT NẠP DỮ LIỆU SEED CHO THÀNH VIÊN 3 (ID RANGE: 301 - 399)!';
PRINT N'============================================================================';
GO
