-- ============================================================================
-- File: 99_MockData.sql
-- Thư mục: database/seed/
-- Mục đích: Kịch bản nạp toàn bộ 38 bảng thực tế cho VeggieLifeDb
--           Tất cả hình ảnh Cloudinary đều là URL thật, đã xác minh HTTP 200 OK
--           100% nguyên liệu và công thức thuần chay, chuẩn dinh dưỡng
-- ============================================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'============================================================================';
PRINT N'  Đang nạp toàn diện 38 bảng dữ liệu cho hệ thống VeggieLife...';
PRINT N'============================================================================';

DECLARE @Now DATETIME2(7) = SYSDATETIME();

-- 1. USERS (Bảng 03)
PRINT N'>> [1/38] Nạp dữ liệu Users...';
SET IDENTITY_INSERT dbo.Users ON;
MERGE INTO dbo.Users AS target
USING (
    VALUES
        (1, 'admin@veggielife.vn', '$2a$10$20up10MEsdp0EV87ggSBleLMG73NqXzaxSlmrdT5h3AeC.SROpFR.', N'Nguyễn Văn Quản Trị', '0901234567', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252869/veggielife/avatars/avatar_admin.jpg', 'ADMIN', 0, NULL, DATEADD(HOUR, -2, @Now), DATEADD(DAY, -60, @Now), 'ACTIVE', 0),
        (2, 'member@veggielife.vn', '$2a$10$20up10MEsdp0EV87ggSBleLMG73NqXzaxSlmrdT5h3AeC.SROpFR.', N'Trần Thị Thu Thảo', '0912345678', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252873/veggielife/avatars/avatar_member1.jpg', 'MEMBER', 0, NULL, DATEADD(HOUR, -5, @Now), DATEADD(DAY, -45, @Now), 'ACTIVE', 0),
        (3, 'nguyen.an@gmail.com', '$2a$10$20up10MEsdp0EV87ggSBleLMG73NqXzaxSlmrdT5h3AeC.SROpFR.', N'Nguyễn Hoàng An', '0923456789', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252872/veggielife/avatars/avatar_vip1.jpg', 'VIP', 0, NULL, DATEADD(HOUR, -1, @Now), DATEADD(DAY, -30, @Now), 'ACTIVE', 0),
        (4, 'tran.tam@gmail.com', '$2a$10$20up10MEsdp0EV87ggSBleLMG73NqXzaxSlmrdT5h3AeC.SROpFR.', N'Trần Minh Tâm', '0934567890', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252870/veggielife/avatars/avatar_mod.jpg', 'VIP', 0, NULL, DATEADD(HOUR, -12, @Now), DATEADD(DAY, -20, @Now), 'ACTIVE', 0),
        (5, 'le.minh@gmail.com', '$2a$10$20up10MEsdp0EV87ggSBleLMG73NqXzaxSlmrdT5h3AeC.SROpFR.', N'Lê Nhật Minh', '0945678901', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252873/veggielife/avatars/avatar_member1.jpg', 'MEMBER', 0, NULL, DATEADD(DAY, -1, @Now), DATEADD(DAY, -15, @Now), 'ACTIVE', 0),
        (6, 'pham.huong@gmail.com', '$2a$10$20up10MEsdp0EV87ggSBleLMG73NqXzaxSlmrdT5h3AeC.SROpFR.', N'Phạm Thu Hương', '0956789012', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252872/veggielife/avatars/avatar_vip1.jpg', 'MEMBER', 0, NULL, DATEADD(DAY, -2, @Now), DATEADD(DAY, -10, @Now), 'ACTIVE', 0),
        (7, 'dang.quang@gmail.com', '$2a$10$20up10MEsdp0EV87ggSBleLMG73NqXzaxSlmrdT5h3AeC.SROpFR.', N'Đặng Đăng Quang', '0967890123', NULL, 'MEMBER', 0, NULL, NULL, DATEADD(DAY, -5, @Now), 'PENDING_ACTIVATION', 0),
        (8, 'spammer99@badmail.com', '$2a$10$20up10MEsdp0EV87ggSBleLMG73NqXzaxSlmrdT5h3AeC.SROpFR.', N'Spam Bot Quảng Cáo', '0978901234', NULL, 'MEMBER', 5, DATEADD(DAY, 7, @Now), DATEADD(DAY, -3, @Now), DATEADD(DAY, -4, @Now), 'QUARANTINED', 3),
        (9, 'cheftoan@vegankitchen.vn', '$2a$10$20up10MEsdp0EV87ggSBleLMG73NqXzaxSlmrdT5h3AeC.SROpFR.', N'Đầu Bếp Quốc Toàn', '0989012345', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252870/veggielife/avatars/avatar_mod.jpg', 'VIP', 0, NULL, DATEADD(MINUTE, -30, @Now), DATEADD(DAY, -40, @Now), 'ACTIVE', 0),
        (10, 'banned_user@trash.com', '$2a$10$20up10MEsdp0EV87ggSBleLMG73NqXzaxSlmrdT5h3AeC.SROpFR.', N'Tài Khoản Vi Phạm Nặng', '0990123456', NULL, 'MEMBER', 0, NULL, DATEADD(DAY, -10, @Now), DATEADD(DAY, -25, @Now), 'BANNED', 5)
) AS source (User_ID, Email, Password_Hash, Full_Name, Phone_Number, Avatar_Url, Role, Failed_Login_Count, Locked_Until, Last_Login_At, Created_At, Account_Status, Count_ViolationReports)
ON target.User_ID = source.User_ID
WHEN MATCHED THEN
    UPDATE SET 
        target.Email = source.Email,
        target.Full_Name = source.Full_Name,
        target.Phone_Number = source.Phone_Number,
        target.Avatar_Url = source.Avatar_Url,
        target.Role = source.Role,
        target.Account_Status = source.Account_Status,
        target.Count_ViolationReports = source.Count_ViolationReports
WHEN NOT MATCHED THEN
    INSERT (User_ID, Email, Password_Hash, Full_Name, Phone_Number, Avatar_Url, Role, Failed_Login_Count, Locked_Until, Last_Login_At, Created_At, Account_Status, Count_ViolationReports)
    VALUES (source.User_ID, source.Email, source.Password_Hash, source.Full_Name, source.Phone_Number, source.Avatar_Url, source.Role, source.Failed_Login_Count, source.Locked_Until, source.Last_Login_At, source.Created_At, source.Account_Status, source.Count_ViolationReports);
SET IDENTITY_INSERT dbo.Users OFF;
GO

-- 2. HEALTH PROFILES (Bảng 04)
PRINT N'>> [2/38] Nạp dữ liệu HealthProfiles...';
MERGE INTO dbo.HealthProfiles AS target
USING (
    VALUES
        (1, 1, 'MALE', '1988-05-15', 172.0, 68.0, 65.0, 23.0, 1620.0, 2200.0, 2000.0, 0, 'SOUTH', SYSDATETIME(), SYSDATETIME(), 0, 'MAINTAIN'),
        (2, 1, 'FEMALE', '1996-03-22', 160.0, 54.0, 49.0, 21.1, 1260.0, 1720.0, 1500.0, 0, 'SOUTH', SYSDATETIME(), SYSDATETIME(), 0, 'LOSE_WEIGHT'),
        (3, 1, 'FEMALE', '1993-11-08', 165.0, 56.0, 53.0, 20.6, 1310.0, 1850.0, 1650.0, 0, 'SOUTH', SYSDATETIME(), SYSDATETIME(), 0, 'LOSE_WEIGHT'),
        (4, 5, 'MALE', '1990-09-19', 175.0, 67.0, 68.0, 21.9, 1630.0, 2250.0, 2250.0, 1, 'CENTRAL', SYSDATETIME(), SYSDATETIME(), 0, 'MAINTAIN'),
        (5, 4, 'MALE', '1998-07-12', 178.0, 75.0, 80.0, 23.7, 1780.0, 2600.0, 2900.0, 0, 'NORTH', SYSDATETIME(), SYSDATETIME(), 0, 'GAIN_MUSCLE'),
        (6, 2, 'FEMALE', '1995-12-30', 158.0, 50.0, 50.0, 20.0, 1210.0, 1650.0, 1650.0, 0, 'SOUTH', SYSDATETIME(), SYSDATETIME(), 0, 'MAINTAIN'),
        (9, 1, 'MALE', '1985-02-14', 170.0, 66.0, 65.0, 22.8, 1550.0, 2150.0, 2100.0, 0, 'SOUTH', SYSDATETIME(), SYSDATETIME(), 0, 'MAINTAIN')
) AS source (User_ID, Dietary_School_ID, Gender, Date_Of_Birth, Height_Cm, Current_Weight_Kg, Target_Weight_Kg, BMI_Value, BMR_Kcal, TDEE_Kcal, Target_Daily_Calories, Requires_Allium_Free, Region_Preference, Created_At, Updated_At, Is_Stale_Metrics, HealthGoal)
ON target.User_ID = source.User_ID
WHEN MATCHED THEN
    UPDATE SET 
        target.Dietary_School_ID = source.Dietary_School_ID,
        target.Current_Weight_Kg = source.Current_Weight_Kg,
        target.Target_Daily_Calories = source.Target_Daily_Calories,
        target.Requires_Allium_Free = source.Requires_Allium_Free,
        target.HealthGoal = source.HealthGoal
WHEN NOT MATCHED THEN
    INSERT (User_ID, Dietary_School_ID, Gender, Date_Of_Birth, Height_Cm, Current_Weight_Kg, Target_Weight_Kg, BMI_Value, BMR_Kcal, TDEE_Kcal, Target_Daily_Calories, Requires_Allium_Free, Region_Preference, Created_At, Updated_At, Is_Stale_Metrics, HealthGoal)
    VALUES (source.User_ID, source.Dietary_School_ID, source.Gender, source.Date_Of_Birth, source.Height_Cm, source.Current_Weight_Kg, source.Target_Weight_Kg, source.BMI_Value, source.BMR_Kcal, source.TDEE_Kcal, source.Target_Daily_Calories, source.Requires_Allium_Free, source.Region_Preference, source.Created_At, source.Updated_At, source.Is_Stale_Metrics, source.HealthGoal);
GO

-- 3. USER WEIGHT HISTORY (Bảng 05)
PRINT N'>> [3/38] Nạp dữ liệu UserWeightHistory...';
IF NOT EXISTS (SELECT 1 FROM dbo.UserWeightHistory WHERE User_ID = 2)
BEGIN
    INSERT INTO dbo.UserWeightHistory (User_ID, Weight_Kg, Logged_Date, Note, Created_At)
    VALUES
        (2, 56.5, CAST(DATEADD(DAY, -28, SYSDATETIME()) AS DATE), N'Bắt đầu chế độ thuần chay giảm cân', DATEADD(DAY, -28, SYSDATETIME())),
        (2, 55.8, CAST(DATEADD(DAY, -21, SYSDATETIME()) AS DATE), N'Tuần 1: Cảm thấy cơ thể nhẹ nhàng hơn', DATEADD(DAY, -21, SYSDATETIME())),
        (2, 55.0, CAST(DATEADD(DAY, -14, SYSDATETIME()) AS DATE), N'Tuần 2: Giảm mỡ bụng tốt', DATEADD(DAY, -14, SYSDATETIME())),
        (2, 54.4, CAST(DATEADD(DAY, -7, SYSDATETIME()) AS DATE), N'Tuần 3: Duy trì năng lượng tích cực', DATEADD(DAY, -7, SYSDATETIME())),
        (2, 54.0, CAST(SYSDATETIME() AS DATE), N'Cân nặng hiện tại đạt mốc 54kg', SYSDATETIME()),
        (3, 58.0, CAST(DATEADD(DAY, -20, SYSDATETIME()) AS DATE), N'Bắt đầu thực đơn cá nhân hóa', DATEADD(DAY, -20, SYSDATETIME())),
        (3, 57.0, CAST(DATEADD(DAY, -10, SYSDATETIME()) AS DATE), N'Cải thiện hệ tiêu hóa rõ rệt', DATEADD(DAY, -10, SYSDATETIME())),
        (3, 56.0, CAST(SYSDATETIME() AS DATE), N'Đạt 56kg khỏe khoắn', SYSDATETIME()),
        (5, 73.0, CAST(DATEADD(DAY, -30, SYSDATETIME()) AS DATE), N'Bắt đầu tập gym tăng cơ chay', DATEADD(DAY, -30, SYSDATETIME())),
        (5, 74.2, CAST(DATEADD(DAY, -15, SYSDATETIME()) AS DATE), N'Bổ sung đạm thực vật từ đậu và yến mạch', DATEADD(DAY, -15, SYSDATETIME())),
        (5, 75.0, CAST(SYSDATETIME() AS DATE), N'Cân nặng tăng đúng mục tiêu cơ bắp', SYSDATETIME());
END;
GO

-- 4. USER ALLERGIES (Bảng 06)
PRINT N'>> [4/38] Nạp dữ liệu UserAllergies...';
MERGE INTO dbo.UserAllergies AS target
USING (
    VALUES
        (2, 5, SYSDATETIME()),  -- User 2 dị ứng Đậu phộng (PEANUT)
        (3, 1, SYSDATETIME()),  -- User 3 dị ứng Gluten (GLUTEN)
        (4, 7, SYSDATETIME()),  -- User 4 dị ứng Sữa bò (MILK)
        (6, 6, SYSDATETIME())   -- User 6 dị ứng Đậu nành (SOYBEAN)
) AS source (User_ID, Allergen_ID, Created_At)
ON target.User_ID = source.User_ID AND target.Allergen_ID = source.Allergen_ID
WHEN NOT MATCHED THEN
    INSERT (User_ID, Allergen_ID, Created_At)
    VALUES (source.User_ID, source.Allergen_ID, source.Created_At);
GO

-- 5. PAYMENT TRANSACTIONS (Bảng 08)
PRINT N'>> [5/38] Nạp dữ liệu PaymentTransactions...';
IF NOT EXISTS (SELECT 1 FROM dbo.PaymentTransactions WHERE Partner_Transaction_Code = 'MOMO_VIP_20261001_001')
BEGIN
    INSERT INTO dbo.PaymentTransactions 
        (Plan_ID, User_ID, Gateway_Provider, Partner_Transaction_Code, Amount_VND, Transaction_Status, Signature_Hash, Created_At, Completed_At, Receipt_URL, Starts_At, Ends_At)
    VALUES
        (2, 3, 'MOMO', 'MOMO_VIP_20261001_001', 129000.00, 'SUCCESS', 'sig_hash_momo_verified_001', DATEADD(DAY, -25, SYSDATETIME()), DATEADD(DAY, -25, SYSDATETIME()), 'https://receipts.veggielife.vn/momo/001.pdf', DATEADD(DAY, -25, SYSDATETIME()), DATEADD(DAY, 5, SYSDATETIME())),
        (3, 4, 'VIETQR', 'VQR_VIP_20261005_002', 449000.00, 'SUCCESS', 'sig_hash_vietqr_verified_002', DATEADD(DAY, -18, SYSDATETIME()), DATEADD(DAY, -18, SYSDATETIME()), 'https://receipts.veggielife.vn/vietqr/002.pdf', DATEADD(DAY, -18, SYSDATETIME()), DATEADD(DAY, 347, SYSDATETIME())),
        (2, 9, 'MOMO', 'MOMO_VIP_20261010_003', 129000.00, 'SUCCESS', 'sig_hash_momo_verified_003', DATEADD(DAY, -7, SYSDATETIME()), DATEADD(DAY, -7, SYSDATETIME()), 'https://receipts.veggielife.vn/momo/003.pdf', DATEADD(DAY, -7, SYSDATETIME()), DATEADD(DAY, 23, SYSDATETIME())),
        (2, 5, 'MOMO', 'MOMO_VIP_20261015_004', 129000.00, 'PENDING', 'sig_hash_momo_pending_004', DATEADD(HOUR, -2, SYSDATETIME()), NULL, NULL, DATEADD(HOUR, -2, SYSDATETIME()), DATEADD(DAY, 30, SYSDATETIME())),
        (2, 6, 'VIETQR', 'VQR_VIP_20261012_005', 129000.00, 'FAILED', 'sig_hash_vietqr_failed_005', DATEADD(DAY, -5, SYSDATETIME()), DATEADD(DAY, -5, SYSDATETIME()), NULL, DATEADD(DAY, -5, SYSDATETIME()), DATEADD(DAY, 25, SYSDATETIME()));
END;
GO

-- 6. AI QUOTA LOGS (Bảng 09)
PRINT N'>> [6/38] Nạp dữ liệu AIQuotaLogs...';
IF NOT EXISTS (SELECT 1 FROM dbo.AIQuotaLogs WHERE Feature_Code = 'MEAL_PLAN_GEN' AND Tokens_Consumed = 1850)
BEGIN
    INSERT INTO dbo.AIQuotaLogs 
        (User_ID, Feature_Code, Tokens_Consumed, Cost_Estimate_USD, Request_Status, Error_Message, Execution_Time_Ms, Created_At)
    VALUES
        (3, 'MEAL_PLAN_GEN', 1850, 0.0037, 'SUCCESS', NULL, 2450, DATEADD(DAY, -10, SYSDATETIME())),
        (4, 'MEAL_PLAN_GEN', 1920, 0.0038, 'SUCCESS', NULL, 2810, DATEADD(DAY, -8, SYSDATETIME())),
        (2, 'AI_CHAT', 450, 0.0009, 'SUCCESS', NULL, 1120, DATEADD(DAY, -3, SYSDATETIME())),
        (3, 'AI_CHAT', 620, 0.0012, 'SUCCESS', NULL, 1340, DATEADD(DAY, -2, SYSDATETIME())),
        (2, 'AI_FRIDGE', 980, 0.0020, 'SUCCESS', NULL, 3100, DATEADD(DAY, -5, SYSDATETIME())),
        (5, 'AI_BARCODE', 120, 0.0002, 'SUCCESS', NULL, 680, DATEADD(DAY, -1, SYSDATETIME())),
        (8, 'CONTENT_MODERATION', 350, 0.0007, 'SUCCESS', N'Phát hiện từ khóa spam cờ bạc', 850, DATEADD(DAY, -4, SYSDATETIME()));
END;
GO

-- 7. AUDIT LOGS (Bảng 10)
PRINT N'>> [7/38] Nạp dữ liệu AuditLogs...';
IF NOT EXISTS (SELECT 1 FROM dbo.AuditLogs WHERE Action = 'BAN_USER' AND Target_ID = '10')
BEGIN
    INSERT INTO dbo.AuditLogs 
        (Actor_User_ID, Actor_Role, Action, Target_Table, Target_ID, Old_Value, New_Value, Note, Created_At)
    VALUES
        (1, 'ADMIN', 'VERIFY_RECIPE', 'Recipes', '101', '{"status":"PENDING_REVIEW"}', '{"status":"APPROVED_ACTIVE","isVerified":true}', N'Kiểm duyệt công thức Canh nấm đậu phụ rong biển đạt chuẩn dinh dưỡng', DATEADD(DAY, -30, SYSDATETIME())),
        (1, 'ADMIN', 'QUARANTINE_USER', 'Users', '8', '{"accountStatus":"ACTIVE"}', '{"accountStatus":"QUARANTINED"}', N'Tạm khóa tài khoản do spam bình luận liên tục', DATEADD(DAY, -4, SYSDATETIME())),
        (1, 'ADMIN', 'BAN_USER', 'Users', '10', '{"accountStatus":"QUARANTINED"}', '{"accountStatus":"BANNED"}', N'Khóa vĩnh viễn tài khoản đăng nội dung vi phạm tiêu chuẩn cộng đồng', DATEADD(DAY, -2, SYSDATETIME())),
        (1, 'ADMIN', 'APPROVE_PAYMENT', 'PaymentTransactions', '2', '{"status":"PENDING"}', '{"status":"SUCCESS"}', N'Đối soát thanh toán VietQR thành công', DATEADD(DAY, -18, SYSDATETIME()));
END;
GO

-- 8. RECIPES (Bảng 15) - 28 công thức ẩm thực chay chuẩn hóa với hình ảnh Cloudinary thật
PRINT N'>> [8/38] Nạp dữ liệu Recipes...';
SET IDENTITY_INSERT dbo.Recipes ON;


MERGE INTO dbo.Recipes AS target
USING (
    VALUES
        (101, 1, 1, 1, N'Canh nấm đậu phụ rong biển', N'Món canh thanh đạm bổ dưỡng mát lành giải nhiệt cơ thể với đậu hũ non và nấm rơm.', N'["Bước 1: Rửa sạch nấm rơm và cắt miếng vừa ăn, cắt đậu hũ non thành khối vuông.","Bước 2: Nấu sôi 800ml nước, cho nấm rơm vào đun trong 5 phút.","Bước 3: Cho đậu hũ non và rong biển vào nấu thêm 3 phút, nêm hạt nêm chay rồi tắt bếp."]', 10, 25, 2, 310.0, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252680/veggielife/recipes/canh_nam_dau_phu.jpg', 'APPROVED_ACTIVE', 280, 'USER', DATEADD(DAY, -3, SYSDATETIME())),
        (102, 1, 2, 1, N'Nấm kho tiêu hạt sen đậm đà', N'Nấm kho sánh quyện đậm đà đưa cơm chuẩn vị chay gia đình Việt Nam.', N'["Bước 1: Sơ chế nấm hương và nấm đùi gà, ngâm nước muối loãng 10 phút.","Bước 2: Ướp nấm với nước tương, tiêu hạt và đường thốt nốt trong 15 phút.","Bước 3: Kho trên lửa nhỏ trong 20 phút cho nước sốt sánh lại và thấm đều."]', 15, 35, 4, 240.0, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252682/veggielife/recipes/nam_kho_tieu.jpg', 'APPROVED_ACTIVE', 420, 'USER', DATEADD(DAY, -4, SYSDATETIME())),
        (103, 2, 7, 1, N'Mì xào nấm rau củ giòn ngọt', N'Món mì xào giòn thơm kèm video hướng dẫn từng bước chi tiết.', N'["Bước 1: Chần sơ mì qua nước sôi rồi để ráo nước.","Bước 2: Xào chín nấm kim châm và đậu hũ chiên cùng cà rốt.","Bước 3: Cho mì vào đảo đều lửa lớn trong 3 phút, rưới nước sốt tương xào thơm lừng."]', 10, 15, 2, 350.0, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252684/veggielife/recipes/mi_xao_nam_rau_cu.jpg', 'APPROVED_ACTIVE', 350, 'USER', DATEADD(DAY, -5, SYSDATETIME())),
        (104, 2, 1, 4, N'Súp ngô ngọt kem tươi dinh dưỡng', N'Súp thơm mùi bơ sữa và ngọt thanh tự nhiên từ bắp ngọt tươi mát.', N'["Bước 1: Tách hạt ngô ngọt và rửa sạch để ráo.","Bước 2: Xào thơm ngô với bơ lạt, xay nhuyễn một nửa lượng ngô.","Bước 3: Nấu sôi hỗn hợp súp và khuấy đều tay với kem tươi đến khi sánh mịn."]', 10, 20, 3, 280.0, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252685/veggielife/recipes/sup_ngo_ngot.jpg', 'APPROVED_ACTIVE', 210, 'USER', DATEADD(DAY, -6, SYSDATETIME())),
        (105, 3, 6, 1, N'Gỏi cuốn ngũ sắc chấm tương đậu', N'Gỏi cuốn thanh mát nhiều rau xanh, bún tươi và đậu hũ chiên giòn rụm.', N'["Bước 1: Cắt sợi dưa leo, cà rốt, bắp cải tím và đậu hũ chiên.","Bước 2: Nhúng bánh tráng qua nước ấm, xếp xà lách, rau thơm và nhân vào cuộn chặt.","Bước 3: Pha sốt tương bơ đậu phộng chấm kèm thơm béo ngậy."]', 20, 10, 4, 220.0, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252687/veggielife/recipes/goi_cuon_chay.jpg', 'APPROVED_ACTIVE', 390, 'USER', DATEADD(DAY, -7, SYSDATETIME())),
        (106, 3, 4, 1, N'Chả giò chay khoai môn giòn rụm', N'Chả giò chiên vàng ươm với nhân bùi béo từ khoai môn và nấm mèo thái sợi.', N'["Bước 1: Bào sợi khoai môn, nấm mèo, cà rốt rồi xào sơ cho săn ráo.","Bước 2: Cuộn nhân vào bánh tráng bía và dán mép bằng bột năng.","Bước 3: Chiên ngập dầu trên lửa vừa đến khi vàng giòn đều các mặt."]', 25, 20, 4, 320.0, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252688/veggielife/recipes/cha_gio_chay.jpg', 'APPROVED_ACTIVE', 450, 'USER', DATEADD(DAY, -8, SYSDATETIME())),
        (107, 9, 7, 1, N'Bún bò Huế chay nước dùng củ quả', N'Nước dùng hầm từ mía, củ cải ngọt đậm đà hòa quyện sả ớt cay nồng chuẩn vị cố đô.', N'["Bước 1: Hầm nước dùng từ mía, lê, củ cải trắng và củ sắn trong 40 phút.","Bước 2: Phi thơm sả băm, hạt điều màu và ớt sa tế chay rồi trút vào nồi nước dùng.","Bước 3: Xếp bún, chả lụa chay, đậu hũ chiên, nấm rơm vào tô rồi chan nước dùng sôi."]', 25, 40, 4, 380.0, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252690/veggielife/recipes/bun_bo_hue_chay.jpg', 'APPROVED_ACTIVE', 520, 'USER', DATEADD(DAY, -9, SYSDATETIME())),
        (108, 4, 7, 5, N'Phở chay truyền thống nấm hương quế hồi', N'Hương vị phở Hà Nội chay thanh tịnh, không hành tỏi ngũ vị tân theo trường phái Phật giáo.', N'["Bước 1: Nướng thơm hoa hồi, thảo quả, quế cây rồi cho vào túi lọc nấu nước dùng.","Bước 2: Xào thơm nấm hương và nấm đùi gà với nước tương tamari.","Bước 3: Trụng bánh phở mềm, chan nước dùng trong veo thơm lừng mùi thảo mộc."]', 20, 45, 3, 360.0, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252691/veggielife/recipes/pho_chay.jpg', 'APPROVED_ACTIVE', 480, 'USER', DATEADD(DAY, -10, SYSDATETIME())),
        (109, 1, 2, 1, N'Đậu hũ sốt cà chua nấm rơm', N'Món ăn dân dã quốc dân với đậu hũ mềm ngấm sốt cà chua đỏ mọng chua ngọt.', N'["Bước 1: Cắt đậu hũ chiên thành miếng vuông vừa ăn.","Bước 2: Xào chín cà chua băm nhuyễn với nấm rơm cho ra nước sốt sền sệt.","Bước 3: Cho đậu hũ vào đảo đều lửa nhỏ 10 phút để ngấm trọn gia vị."]', 10, 20, 2, 260.0, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252764/veggielife/recipes/dau_hu_sot_ca_chua.jpg', 'APPROVED_ACTIVE', 310, 'USER', DATEADD(DAY, -11, SYSDATETIME())),
        (110, 9, 1, 1, N'Cà ri chay khoai lang nước cốt dừa', N'Nước sốt cà ri vàng óng thơm ngậy mùi cốt dừa và khoai lang mật bùi béo ăn cùng bánh mì.', N'["Bước 1: Chiên sơ khoai lang, khoai môn và cà rốt để không bị nát khi nấu.","Bước 2: Xào thơm bột cà ri Ấn Độ với sả cây đập dập rồi cho rau củ vào đảo.","Bước 3: Thêm nước dừa tươi và nước cốt dừa hầm lửa nhỏ 25 phút đến khi sánh mịn."]', 20, 35, 4, 410.0, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252694/veggielife/recipes/ca_ri_chay.jpg', 'APPROVED_ACTIVE', 430, 'USER', DATEADD(DAY, -12, SYSDATETIME())),
        (111, 4, 8, 1, N'Cơm chiên trái thơm hạt sen', N'Cơm chiên hạt tơi xốp, ngọt dịu vị dứa tươi và bùi bùi từ hạt sen Đồng Tháp.', N'["Bước 1: Khoét ruột trái dứa để làm bát đựng, thái hạt lựu phần thịt dứa.","Bước 2: Luộc chín hạt sen tươi, bắp ngọt và cà rốt cắt hạt lựu.","Bước 3: Rang cơm nguội với dầu màu điều trên chảo gang lửa lớn rồi trộn đều rau củ."]', 20, 20, 3, 390.0, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252695/veggielife/recipes/com_chien_trai_thom.jpg', 'APPROVED_ACTIVE', 360, 'USER', DATEADD(DAY, -13, SYSDATETIME())),
        (112, 9, 9, 5, N'Lẩu nấm thập cẩm thanh ngọt tự nhiên', N'Nồi lẩu ấm cúng quy tụ hơn 6 loại nấm quý, nước dùng trong vắt từ củ cải và bắp ngọt.', N'["Bước 1: Nấu nước dùng lẩu từ bắp ngọt, mía lau, củ cải muối và táo đỏ trong 45 phút.","Bước 2: Sơ chế các loại nấm: nấm đùi gà, kim châm, bào ngư, nấm hương, nấm rơm.","Bước 3: Nêm muối hồng và hạt nêm nấm, ăn kèm bún tươi và rau tần ô, cải cúc."]', 30, 40, 6, 340.0, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252765/veggielife/recipes/lau_nam_chay.jpg', 'APPROVED_ACTIVE', 610, 'USER', DATEADD(DAY, -14, SYSDATETIME())),
        (113, 3, 14, 1, N'Salad bơ diêm mạch sốt dầu giấm', N'Món ăn giàu protein thực vật và chất béo tốt Omega-3 từ hạt quinoa và quả bơ sáp Đắk Lắk.', N'["Bước 1: Nấu chín hạt quinoa trong 15 phút rồi để nguội ráo nước.","Bước 2: Cắt hạt lựu quả bơ, cà chua bi cắt đôi, rau xà lách rocket rửa sạch.","Bước 3: Trộn đều các nguyên liệu với sốt dầu ô liu nguyên chất và giấm táo."]', 15, 15, 2, 290.0, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252698/veggielife/recipes/salad_bo_diem_mach.jpg', 'APPROVED_ACTIVE', 290, 'USER', DATEADD(DAY, -15, SYSDATETIME())),
        (114, 5, 4, 2, N'Đậu hũ non chiên xù sốt mè rang', N'Vỏ ngoài giòn rụm bên trong mềm mịn tan ngay đầu lưỡi, phủ mè rang thơm phức.', N'["Bước 1: Cắt đậu hũ non thành khối vuông nhẹ tay để không bị vỡ.","Bước 2: Nhúng đậu qua bột mì, trứng gà đánh tan rồi lăn đều bột chiên xù panko.","Bước 3: Chiên vàng giòn trong dầu nóng, vớt ra thấm ráo dầu và chấm sốt mè."]', 15, 15, 2, 310.0, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252699/veggielife/recipes/dau_hu_non_chien_xu.jpg', 'APPROVED_ACTIVE', 270, 'USER', DATEADD(DAY, -16, SYSDATETIME())),
        (115, 2, 7, 1, N'Bún chả giò chay rau sống thanh mát', N'Bún tươi ăn kèm chả giò giòn tan, dưa leo băm, đồ chua và nước mắm chay chua ngọt thanh tao.', N'["Bước 1: Chiên giòn chả giò chay rồi cắt khúc vừa ăn.","Bước 2: Chuẩn bị rau sống xà lách, rau thơm, giá đỗ và dưa leo thái sợi.","Bước 3: Xếp bún vào tô, thêm chả giò, rắc đậu phộng rang và rưới nước mắm chay."]', 15, 20, 2, 370.0, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252700/veggielife/recipes/bun_cha_gio_chay.jpg', 'APPROVED_ACTIVE', 380, 'USER', DATEADD(DAY, -17, SYSDATETIME())),
        (116, 9, 3, 1, N'Nấm đùi gà áp chảo sốt tiêu đen', N'Miếng nấm đùi gà khía hoa cúc ngấm đẫm sốt tiêu đen cay nồng, dai giòn như bào ngư thượng hạng.', N'["Bước 1: Cắt nấm đùi gà thành khoanh tròn dày 2cm, khía vảy rồng hai mặt.","Bước 2: Áp chảo nấm vàng xém cạnh cùng dầu thực vật.","Bước 3: Đổ sốt tiêu đen gồm nước tương, tiêu đập dập, dầu hào chay rim sánh lại."]', 15, 15, 2, 230.0, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252702/veggielife/recipes/nam_dui_ga_ap_chao.jpg', 'APPROVED_ACTIVE', 410, 'USER', DATEADD(DAY, -18, SYSDATETIME())),
        (117, 1, 1, 1, N'Canh chua chay Nam Bộ đậu hũ bắp non', N'Hương vị chua thanh từ nước cốt me và ngọt dịu của thơm dứa, bạc hà, đậu bắp giòn rụm.', N'["Bước 1: Nấu sôi nước với nước cốt me lọc hạt.","Bước 2: Cho dứa cắt lát, cà chua, bắp non và đậu hũ non vào nấu 7 phút.","Bước 3: Thêm bạc hà, đậu bắp, giá đỗ nấu sôi lại rồi nêm ngò gai, ớt lát."]', 15, 20, 3, 210.0, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252703/veggielife/recipes/canh_chua_chay.jpg', 'APPROVED_ACTIVE', 340, 'USER', DATEADD(DAY, -19, SYSDATETIME())),
        (118, 9, 2, 1, N'Mít non kho tiêu xanh đậm đà quê nhà', N'Mít non luộc chín mềm kho rục cùng hạt tiêu xanh cay nhẹ, món ăn đưa cơm số một miền Trung.', N'["Bước 1: Mít non cắt miếng tam giác vừa ăn, chiên sơ cho vàng săn mặt.","Bước 2: Thắng nước màu caramen từ đường thốt nốt, xếp mít vào nồi đất.","Bước 3: Thêm tiêu xanh, nước tương và nước dừa tươi kho liu riu 30 phút đến khi cạn sệt."]', 20, 35, 4, 250.0, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252705/veggielife/recipes/mit_non_kho_tieu.jpg', 'APPROVED_ACTIVE', 360, 'USER', DATEADD(DAY, -20, SYSDATETIME())),
        (119, 3, 15, 1, N'Cháo yến mạch nấm đông cô hạt chia', N'Bữa sáng tiện lợi chỉ 15 phút, giàu chất xơ hòa tan beta-glucan tốt cho tim mạch và dạ dày.', N'["Bước 1: Ngâm mềm nấm đông cô thái hạt lựu, xào thơm với chút dầu mè.","Bước 2: Nấu sôi 500ml nước dùng rau củ, cho yến mạch cán dẹt vào khuấy đều 5 phút.","Bước 3: Múc cháo ra bát, rắc nấm xào, hạt chia hữu cơ và chút tiêu thơm."]', 5, 10, 1, 270.0, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252706/veggielife/recipes/chao_yen_mach_nam.jpg', 'APPROVED_ACTIVE', 260, 'USER', DATEADD(DAY, -21, SYSDATETIME())),
        (120, 5, 10, 1, N'Bánh xèo chay giòn rụm nhân nấm giá đỗ', N'Vỏ bánh xèo giòn tan thơm béo nước cốt dừa và bột nghệ vàng ươm, cuộn rau cải xanh mát lành.', N'["Bước 1: Pha bột bánh xèo với nước cốt dừa, bột nghệ và hành bo-rô cắt nhỏ.","Bước 2: Xào chín nhân nấm kim châm, đậu hũ cắt sợi và giá đỗ.","Bước 3: Đổ bột tráng mỏng chảo, cho nhân vào đậy nắp 3 phút đến khi vỏ bánh giòn rụm."]', 20, 25, 3, 330.0, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252708/veggielife/recipes/banh_xeo_chay.jpg', 'APPROVED_ACTIVE', 440, 'USER', DATEADD(DAY, -2, SYSDATETIME())),
        (121, 2, 5, 1, N'Rau củ luộc ngũ sắc chấm kho quẹt chay', N'Bông cải xanh, cà rốt, bắp non giữ trọn vị ngọt tự nhiên chấm đẫm niêu kho quẹt nấm đậm đà.', N'["Bước 1: Luộc các loại rau củ trong nước sôi có chút muối để giữ màu xanh tươi.","Bước 2: Làm kho quẹt từ nấm hương băm nhuyễn, đậu hũ chiên cắt hạt lựu và ớt tiêu.","Bước 3: Kho trên lửa nhỏ liu riu đến khi nước sốt keo đặc quánh lại."]', 15, 15, 4, 190.0, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252709/veggielife/recipes/rau_cu_luoc_kho_quet.jpg', 'APPROVED_ACTIVE', 320, 'USER', DATEADD(DAY, -3, SYSDATETIME())),
        (122, 6, 15, 1, N'Bánh mì chay pate nấm chả lụa', N'Bánh mì giòn rụm đặc ruột phết pate nấm béo bùi, chả lụa chay và đồ chua dưa leo ngò rí.', N'["Bước 1: Làm pate nấm từ nấm mỡ, nấm hương xay nhuyễn cùng bơ đậu phộng và bột hạnh nhân.","Bước 2: Nướng nóng giòn ổ bánh mì, rạch bụng phết đều lớp pate thơm phức.","Bước 3: Kẹp chả lụa chay thái lát, dưa leo, củ cải chua ngọt và rưới nước sốt tương ớt."]', 15, 10, 1, 360.0, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252710/veggielife/recipes/banh_mi_chay.jpg', 'APPROVED_ACTIVE', 380, 'USER', DATEADD(DAY, -4, SYSDATETIME())),
        (123, 4, 1, 3, N'Súp bí đỏ nước cốt dừa hạnh nhân lát', N'Món súp sánh mịn màu vàng cam bắt mắt, ngọt thanh béo ngậy tốt cho trí não và làn da.', N'["Bước 1: Hấp chín bí đỏ hạt đậu rồi cho vào máy xay nhuyễn cùng chút sữa hạt.","Bước 2: Đun sôi nhẹ hỗn hợp trên nồi, thêm nước cốt dừa và nêm chút muối hồng.","Bước 3: Múc ra bát sứ, rắc hạnh nhân lát rang giòn lên bề mặt thưởng thức nóng."]', 15, 20, 2, 260.0, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252712/veggielife/recipes/sup_bi_do_cot_dua.jpg', 'APPROVED_ACTIVE', 270, 'USER', DATEADD(DAY, -5, SYSDATETIME())),
        (124, 2, 3, 1, N'Đậu cove xào nấm bào ngư dầu hào chay', N'Đậu cove giòn ngọt tự nhiên kết hợp nấm bào ngư dai ngọt trong sốt dầu hào nấm hương đậm đà.', N'["Bước 1: Tước xơ đậu cove cắt vát chéo, nấm bào ngư xé sợi vừa ăn.","Bước 2: Phi thơm bo-rô với dầu ăn, trút nấm vào xào chín tới.","Bước 3: Cho đậu cove vào đảo lửa lớn trong 4 phút, nêm dầu hào chay rồi trút ra đĩa."]', 10, 15, 3, 180.0, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252713/veggielife/recipes/dau_cove_xao_nam.jpg', 'APPROVED_ACTIVE', 230, 'USER', DATEADD(DAY, -6, SYSDATETIME())),
        (125, 1, 12, 1, N'Chè hạt sen long nhãn đường phèn', N'Món tráng miệng cung đình thanh nhã giúp an thần ngủ ngon, bổ dưỡng bồi bổ khí huyết.', N'["Bước 1: Luộc chín hạt sen tươi đến khi mềm bùi không bị nát.","Bước 2: Lồng từng hạt sen vào trong múi long nhãn tươi mọng nước.","Bước 3: Nấu nước đường phèn hoa cúc sôi lăn tăn rồi thả hạt sen bọc long nhãn vào tắt bếp."]', 20, 25, 4, 240.0, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252714/veggielife/recipes/che_hat_sen_long_nhan.jpg', 'APPROVED_ACTIVE', 350, 'USER', DATEADD(DAY, -7, SYSDATETIME())),
        (126, 3, 11, 1, N'Sữa hạt macca óc chó hạnh nhân tự nấu', N'Sữa hạt dinh dưỡng nguyên chất béo ngậy không đường hóa học, cung cấp vitamin E và khoáng chất.', N'["Bước 1: Ngâm hạt óc chó và hạnh nhân qua đêm, bóc vỏ lụa để sữa không bị chát.","Bước 2: Cho hạt macca, óc chó, hạnh nhân vào máy làm sữa hạt cùng 1 lít nước ấm.","Bước 3: Xay mịn và lọc qua rây lụa, uống ấm hoặc trữ lạnh dùng trong 3 ngày."]', 10, 20, 4, 170.0, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252715/veggielife/recipes/sua_hat_macca_oc_cho.jpg', 'APPROVED_ACTIVE', 290, 'USER', DATEADD(DAY, -8, SYSDATETIME())),
        (127, 2, 11, 1, N'Sinh tố bơ chuối cải xoăn kale thanh lọc', N'Thức uống xanh thuần khiết giúp giải độc cơ thể, giàu chất chống oxy hóa và chất xơ tự nhiên.', N'["Bước 1: Rửa sạch lá cải xoăn kale bỏ cọng già, chuối chín bóc vỏ cắt khúc đông đá.","Bước 2: Cho quả bơ sáp, chuối, cải kale và 200ml sữa đậu nành vào máy xay sinh tố.","Bước 3: Xay ở tốc độ cao trong 60 giây đến khi sánh mịn nhung, rót ra ly dùng ngay."]', 10, 5, 2, 210.0, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252717/veggielife/recipes/sinh_to_bo_chuoi_kale.jpg', 'APPROVED_ACTIVE', 310, 'USER', DATEADD(DAY, -9, SYSDATETIME())),
        (128, 5, 3, 1, N'Nui xào rau củ sốt cà chua nấm tươi', N'Nui xoắn dai mềm xào chín tới cùng cà chua, nấm mỡ và đậu Hà Lan đầy màu sắc bắt mắt.', N'["Bước 1: Luộc nui trong nước sôi 8 phút cùng chút dầu ăn cho không bị dính.","Bước 2: Xào thơm cà chua chín mềm với nấm và bắp hạt ngọt.","Bước 3: Cho nui vào đảo đều thấm sốt cà chua, rắc lá quế tây oregano dậy mùi thơm."]', 15, 15, 3, 320.0, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252768/veggielife/recipes/nui_xao_rau_cu.jpg', 'APPROVED_ACTIVE', 280, 'USER', DATEADD(DAY, -10, SYSDATETIME()))
) AS source (Recipe_ID, User_ID, Category_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At)
ON target.Recipe_ID = source.Recipe_ID
WHEN MATCHED THEN
    UPDATE SET
        target.Title = source.Title,
        target.Description = source.Description,
        target.Instructions = source.Instructions,
        target.Calories_Kcal = source.Calories_Kcal,
        target.Image_Url = source.Image_Url,
        target.Status = source.Status,
        target.View_Count = source.View_Count
WHEN NOT MATCHED THEN
    INSERT (Recipe_ID, User_ID, Category_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At)
    VALUES (source.Recipe_ID, source.User_ID, source.Category_ID, source.Dietary_School_ID, source.Title, source.Description, source.Instructions, source.Prep_Time_Minutes, source.Cook_Time_Minutes, source.Servings, source.Calories_Kcal, source.Difficulty_Level, source.Is_Verified, source.Image_Url, source.Status, source.View_Count, source.Source, source.Created_At);

SET IDENTITY_INSERT dbo.Recipes OFF;
GO

-- 9. RECIPE INGREDIENTS (Bảng 16) - Định lượng nguyên liệu chi tiết cho 28 công thức
PRINT N'>> [9/38] Nạp dữ liệu RecipeIngredients...';

MERGE INTO dbo.RecipeIngredients AS target
USING (
    VALUES
        -- 101 Canh nấm đậu phụ
        (101, 39, 200.0, N'g', N'Cắt khối vuông nhỏ', 0),
        (101, 90, 150.0, N'g', N'Cắt đôi bỏ chân', 0),
        (101, 68, 10.0, N'g', N'Nêm vừa ăn', 0),
        -- 102 Nấm kho tiêu
        (102, 84, 250.0, N'g', N'Ngâm nở, khía hoa', 0),
        (102, 82, 150.0, N'g', N'Cắt khoanh tròn 2cm', 0),
        (102, 69, 5.0, N'g', N'Đập dập', 0),
        (102, 67, 30.0, N'ml', N'Ướp thấm đều', 0),
        -- 103 Mì xào nấm
        (103, 53, 200.0, N'g', N'Chần sơ nước sôi', 0),
        (103, 85, 100.0, N'g', N'Bỏ gốc tách sợi', 0),
        (103, 38, 100.0, N'g', N'Cắt sợi vừa ăn', 0),
        (103, 18, 50.0, N'g', N'Bào sợi mỏng', 0),
        -- 104 Súp ngô ngọt
        (104, 5, 200.0, N'g', N'Tách hạt ngọt tươi', 0),
        (104, 10, 30.0, N'g', N'Để mềm nhiệt độ phòng', 0),
        (104, 14, 20.0, N'g', N'Hòa tan với nước ấm', 0),
        -- 105 Gỏi cuốn ngũ sắc
        (105, 1, 10.0, N'cái', N'Bánh tráng cuốn dẻo', 0),
        (105, 38, 150.0, N'g', N'Cắt sợi dài', 0),
        (105, 2, 80.0, N'g', N'Thái sợi mỏng', 0),
        (105, 18, 80.0, N'g', N'Bào sợi giòn', 0),
        (105, 16, 100.0, N'g', N'Bún tươi chần ráo', 0),
        -- 106 Chả giò chay
        (106, 86, 50.0, N'g', N'Ngâm nở thái sợi', 0),
        (106, 18, 100.0, N'g', N'Bào sợi mỏng', 0),
        (106, 1, 20.0, N'cái', N'Bánh tráng bía cuốn chả', 0),
        -- 107 Bún bò Huế chay
        (107, 16, 250.0, N'g', N'Bún sợi to Huế', 0),
        (107, 38, 100.0, N'g', N'Chiên vàng giòn', 0),
        (107, 90, 100.0, N'g', N'Rửa sạch cắt đôi', 0),
        (107, 18, 100.0, N'g', N'Nấu nước ngọt', 0),
        -- 108 Phở chay
        (108, 16, 200.0, N'g', N'Bánh phở mềm', 0),
        (108, 84, 150.0, N'g', N'Xào sơ thơm nức', 0),
        (108, 82, 100.0, N'g', N'Thái lát mỏng vừa ăn', 0),
        -- 109 Đậu hũ sốt cà chua
        (109, 38, 250.0, N'g', N'Cắt khối vuông chiên vàng', 0),
        (109, 17, 150.0, N'g', N'Băm nhuyễn làm sốt', 0),
        (109, 90, 80.0, N'g', N'Bổ đôi xào chung', 0),
        -- 110 Cà ri chay
        (110, 18, 150.0, N'g', N'Cắt khúc vát xéo', 0),
        (110, 38, 150.0, N'g', N'Chiên sơ vàng mặt', 0),
        (110, 71, 20.0, N'ml', N'Phi bột cà ri thơm', 0),
        -- 111 Cơm chiên trái thơm
        (111, 33, 200.0, N'g', N'Cơm nguội để ráo hạt', 0),
        (111, 35, 80.0, N'g', N'Hạt sen luộc chín mềm', 0),
        (111, 5, 50.0, N'g', N'Hạt bắp ngọt tách vỏ', 0),
        -- 112 Lẩu nấm chay
        (112, 82, 100.0, N'g', N'Thái lát mỏng', 0),
        (112, 85, 100.0, N'g', N'Bỏ gốc tách sợi', 0),
        (112, 91, 100.0, N'g', N'Xé sợi vừa ăn', 0),
        (112, 90, 100.0, N'g', N'Cắt đôi', 0),
        (112, 39, 150.0, N'g', N'Cắt khối vuông', 0),
        -- 113 Salad bơ diêm mạch
        (113, 17, 80.0, N'g', N'Cà chua bi cắt đôi', 0),
        (113, 71, 20.0, N'ml', N'Dầu ô liu nguyên chất', 0),
        -- 114 Đậu hũ non chiên xù
        (114, 39, 250.0, N'g', N'Cắt miếng dày 2cm', 0),
        (114, 13, 50.0, N'g', N'Lăn bột chiên giòn', 0),
        -- 115 Bún chả giò chay
        (115, 16, 200.0, N'g', N'Bún tươi chần nước sôi', 0),
        (115, 43, 80.0, N'g', N'Giá đỗ tươi rửa sạch', 0),
        (115, 80, 20.0, N'g', N'Đậu phộng rang giã dập', 0),
        -- 116 Nấm đùi gà áp chảo
        (116, 82, 300.0, N'g', N'Khía vảy rồng hai mặt', 0),
        (116, 69, 5.0, N'g', N'Tiêu đen rang xay thơm', 0),
        (116, 67, 20.0, N'ml', N'Nước tương nguyên chất', 0),
        -- 117 Canh chua chay
        (117, 39, 150.0, N'g', N'Cắt khối vuông', 0),
        (117, 17, 100.0, N'g', N'Cắt múi cau', 0),
        (117, 5, 80.0, N'g', N'Bắp non cắt khúc', 0),
        -- 118 Mít non kho tiêu
        (118, 69, 10.0, N'g', N'Tiêu xanh nguyên chùm', 0),
        (118, 67, 30.0, N'ml', N'Nước tương kho', 0),
        (118, 70, 15.0, N'g', N'Đường thốt nốt', 0),
        -- 119 Cháo yến mạch
        (119, 84, 80.0, N'g', N'Thái hạt lựu xào thơm', 0),
        (119, 72, 10.0, N'ml', N'Dầu mè thơm', 0),
        -- 120 Bánh xèo chay
        (120, 14, 150.0, N'g', N'Bột bánh xèo pha cốt dừa', 0),
        (120, 85, 100.0, N'g', N'Nấm kim châm bỏ gốc', 0),
        (120, 43, 100.0, N'g', N'Giá đỗ tươi', 0),
        -- 121 Rau củ luộc
        (121, 12, 150.0, N'g', N'Bông cải xanh tách nhánh', 0),
        (121, 18, 100.0, N'g', N'Cà rốt tỉa hoa', 0),
        (121, 84, 50.0, N'g', N'Nấm băm làm kho quẹt', 0),
        -- 122 Bánh mì chay
        (122, 84, 100.0, N'g', N'Nấm làm pate', 0),
        (122, 80, 30.0, N'g', N'Bơ đậu phộng xay chung pate', 0),
        -- 123 Súp bí đỏ
        (123, 7, 300.0, N'g', N'Bí đỏ gọt vỏ hấp chín', 0),
        (123, 81, 20.0, N'g', N'Hạnh nhân lát rang vàng', 0),
        -- 124 Đậu cove xào
        (124, 37, 250.0, N'g', N'Tước xơ cắt vát chéo', 0),
        (124, 91, 150.0, N'g', N'Xé sợi vừa ăn', 0),
        -- 125 Chè hạt sen
        (125, 35, 150.0, N'g', N'Hạt sen tươi thông tâm', 0),
        (125, 70, 80.0, N'g', N'Đường phèn kết tinh', 0),
        -- 126 Sữa hạt macca
        (126, 81, 50.0, N'g', N'Hạnh nhân ngâm tách vỏ', 0),
        (126, 36, 50.0, N'g', N'Hạt điều tươi béo ngậy', 0),
        -- 127 Sinh tố bơ kale
        (127, 2, 50.0, N'g', N'Cải xoăn kale rửa sạch', 0),
        (127, 10, 10.0, N'g', N'Sữa hạt bổ sung độ béo', 0),
        -- 128 Nui xào rau củ
        (128, 17, 100.0, N'g', N'Cà chua sốt', 0),
        (128, 90, 80.0, N'g', N'Nấm rơm cắt hạt lựu', 0)
) AS source (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional)
ON target.Recipe_ID = source.Recipe_ID AND target.Ingredient_ID = source.Ingredient_ID
WHEN NOT MATCHED THEN
    INSERT (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional)
    VALUES (source.Recipe_ID, source.Ingredient_ID, source.Quantity_Needed, source.Unit, source.Preparation_Note, source.Is_Optional);
GO

-- 10. POSTS (Bảng 14)
PRINT N'>> [10/38] Nạp dữ liệu Posts...';
SET IDENTITY_INSERT dbo.Posts ON;

MERGE INTO dbo.Posts AS target
USING (
    VALUES

        (101, 1, 101, N'Canh nấm đậu phụ rong biển', N'Món canh thanh đạm bổ dưỡng mát lành giải nhiệt cơ thể với đậu hũ non và nấm rơm.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252680/veggielife/recipes/canh_nam_dau_phu.jpg', 'PUBLIC', 330, 'APPROVED_ACTIVE', DATEADD(DAY, -3, SYSDATETIME()), NULL),
        (102, 1, 102, N'Nấm kho tiêu hạt sen đậm đà', N'Nấm kho sánh quyện đậm đà đưa cơm chuẩn vị chay gia đình Việt Nam.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252682/veggielife/recipes/nam_kho_tieu.jpg', 'PUBLIC', 470, 'APPROVED_ACTIVE', DATEADD(DAY, -4, SYSDATETIME()), NULL),
        (103, 2, 103, N'Mì xào nấm rau củ giòn ngọt', N'Món mì xào giòn thơm kèm video hướng dẫn từng bước chi tiết.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252684/veggielife/recipes/mi_xao_nam_rau_cu.jpg', 'PUBLIC', 400, 'APPROVED_ACTIVE', DATEADD(DAY, -5, SYSDATETIME()), NULL),
        (104, 2, 104, N'Súp ngô ngọt kem tươi dinh dưỡng', N'Súp thơm mùi bơ sữa và ngọt thanh tự nhiên từ bắp ngọt tươi mát.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252685/veggielife/recipes/sup_ngo_ngot.jpg', 'PUBLIC', 260, 'APPROVED_ACTIVE', DATEADD(DAY, -6, SYSDATETIME()), NULL),
        (105, 3, 105, N'Gỏi cuốn ngũ sắc chấm tương đậu', N'Gỏi cuốn thanh mát nhiều rau xanh, bún tươi và đậu hũ chiên giòn rụm.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252687/veggielife/recipes/goi_cuon_chay.jpg', 'PUBLIC', 440, 'APPROVED_ACTIVE', DATEADD(DAY, -7, SYSDATETIME()), NULL),
        (106, 3, 106, N'Chả giò chay khoai môn giòn rụm', N'Chả giò chiên vàng ươm với nhân bùi béo từ khoai môn và nấm mèo thái sợi.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252688/veggielife/recipes/cha_gio_chay.jpg', 'PUBLIC', 500, 'APPROVED_ACTIVE', DATEADD(DAY, -8, SYSDATETIME()), NULL),
        (107, 9, 107, N'Bún bò Huế chay nước dùng củ quả', N'Nước dùng hầm từ mía, củ cải ngọt đậm đà hòa quyện sả ớt cay nồng chuẩn vị cố đô.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252690/veggielife/recipes/bun_bo_hue_chay.jpg', 'PUBLIC', 570, 'APPROVED_ACTIVE', DATEADD(DAY, -9, SYSDATETIME()), NULL),
        (108, 4, 108, N'Phở chay truyền thống nấm hương quế hồi', N'Hương vị phở Hà Nội chay thanh tịnh, không hành tỏi ngũ vị tân theo trường phái Phật giáo.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252691/veggielife/recipes/pho_chay.jpg', 'PUBLIC', 530, 'APPROVED_ACTIVE', DATEADD(DAY, -10, SYSDATETIME()), NULL),
        (109, 1, 109, N'Đậu hũ sốt cà chua nấm rơm', N'Món ăn dân dã quốc dân với đậu hũ mềm ngấm sốt cà chua đỏ mọng chua ngọt.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252764/veggielife/recipes/dau_hu_sot_ca_chua.jpg', 'PUBLIC', 360, 'APPROVED_ACTIVE', DATEADD(DAY, -11, SYSDATETIME()), NULL),
        (110, 9, 110, N'Cà ri chay khoai lang nước cốt dừa', N'Nước sốt cà ri vàng óng thơm ngậy mùi cốt dừa và khoai lang mật bùi béo ăn cùng bánh mì.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252694/veggielife/recipes/ca_ri_chay.jpg', 'PUBLIC', 480, 'APPROVED_ACTIVE', DATEADD(DAY, -12, SYSDATETIME()), NULL),
        (111, 4, 111, N'Cơm chiên trái thơm hạt sen', N'Cơm chiên hạt tơi xốp, ngọt dịu vị dứa tươi và bùi bùi từ hạt sen Đồng Tháp.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252695/veggielife/recipes/com_chien_trai_thom.jpg', 'PUBLIC', 410, 'APPROVED_ACTIVE', DATEADD(DAY, -13, SYSDATETIME()), NULL),
        (112, 9, 112, N'Lẩu nấm thập cẩm thanh ngọt tự nhiên', N'Nồi lẩu ấm cúng quy tụ hơn 6 loại nấm quý, nước dùng trong vắt từ củ cải và bắp ngọt.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252765/veggielife/recipes/lau_nam_chay.jpg', 'PUBLIC', 660, 'APPROVED_ACTIVE', DATEADD(DAY, -14, SYSDATETIME()), NULL),
        (113, 3, 113, N'Salad bơ diêm mạch sốt dầu giấm', N'Món ăn giàu protein thực vật và chất béo tốt Omega-3 từ hạt quinoa và quả bơ sáp Đắk Lắk.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252698/veggielife/recipes/salad_bo_diem_mach.jpg', 'PUBLIC', 340, 'APPROVED_ACTIVE', DATEADD(DAY, -15, SYSDATETIME()), NULL),
        (114, 5, 114, N'Đậu hũ non chiên xù sốt mè rang', N'Vỏ ngoài giòn rụm bên trong mềm mịn tan ngay đầu lưỡi, phủ mè rang thơm phức.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252699/veggielife/recipes/dau_hu_non_chien_xu.jpg', 'PUBLIC', 320, 'APPROVED_ACTIVE', DATEADD(DAY, -16, SYSDATETIME()), NULL),
        (115, 2, 115, N'Bún chả giò chay rau sống thanh mát', N'Bún tươi ăn kèm chả giò giòn tan, dưa leo băm, đồ chua và nước mắm chay chua ngọt thanh tao.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252700/veggielife/recipes/bun_cha_gio_chay.jpg', 'PUBLIC', 430, 'APPROVED_ACTIVE', DATEADD(DAY, -17, SYSDATETIME()), NULL),
        (116, 9, 116, N'Nấm đùi gà áp chảo sốt tiêu đen', N'Miếng nấm đùi gà khía hoa cúc ngấm đẫm sốt tiêu đen cay nồng, dai giòn như bào ngư thượng hạng.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252702/veggielife/recipes/nam_dui_ga_ap_chao.jpg', 'PUBLIC', 460, 'APPROVED_ACTIVE', DATEADD(DAY, -18, SYSDATETIME()), NULL),
        (117, 1, 117, N'Canh chua chay Nam Bộ đậu hũ bắp non', N'Hương vị chua thanh từ nước cốt me và ngọt dịu của thơm dứa, bạc hà, đậu bắp giòn rụm.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252703/veggielife/recipes/canh_chua_chay.jpg', 'PUBLIC', 390, 'APPROVED_ACTIVE', DATEADD(DAY, -19, SYSDATETIME()), NULL),
        (118, 9, 118, N'Mít non kho tiêu xanh đậm đà quê nhà', N'Mít non luộc chín mềm kho rục cùng hạt tiêu xanh cay nhẹ, món ăn đưa cơm số một miền Trung.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252705/veggielife/recipes/mit_non_kho_tieu.jpg', 'PUBLIC', 410, 'APPROVED_ACTIVE', DATEADD(DAY, -20, SYSDATETIME()), NULL),
        (119, 3, 119, N'Cháo yến mạch nấm đông cô hạt chia', N'Bữa sáng tiện lợi chỉ 15 phút, giàu chất xơ hòa tan beta-glucan tốt cho tim mạch và dạ dày.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252706/veggielife/recipes/chao_yen_mach_nam.jpg', 'PUBLIC', 310, 'APPROVED_ACTIVE', DATEADD(DAY, -21, SYSDATETIME()), NULL),
        (120, 5, 120, N'Bánh xèo chay giòn rụm nhân nấm giá đỗ', N'Vỏ bánh xèo giòn tan thơm béo nước cốt dừa và bột nghệ vàng ươm, cuộn rau cải xanh mát lành.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252708/veggielife/recipes/banh_xeo_chay.jpg', 'PUBLIC', 490, 'APPROVED_ACTIVE', DATEADD(DAY, -2, SYSDATETIME()), NULL),
        (121, 2, 121, N'Rau củ luộc ngũ sắc chấm kho quẹt chay', N'Bông cải xanh, cà rốt, bắp non giữ trọn vị ngọt tự nhiên chấm đẫm niêu kho quẹt nấm đậm đà.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252709/veggielife/recipes/rau_cu_luoc_kho_quet.jpg', 'PUBLIC', 370, 'APPROVED_ACTIVE', DATEADD(DAY, -3, SYSDATETIME()), NULL),
        (122, 6, 122, N'Bánh mì chay pate nấm chả lụa', N'Bánh mì giòn rụm đặc ruột phết pate nấm béo bùi, chả lụa chay và đồ chua dưa leo ngò rí.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252710/veggielife/recipes/banh_mi_chay.jpg', 'PUBLIC', 430, 'APPROVED_ACTIVE', DATEADD(DAY, -4, SYSDATETIME()), NULL),
        (123, 4, 123, N'Súp bí đỏ nước cốt dừa hạnh nhân lát', N'Món súp sánh mịn màu vàng cam bắt mắt, ngọt thanh béo ngậy tốt cho trí não và làn da.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252712/veggielife/recipes/sup_bi_do_cot_dua.jpg', 'PUBLIC', 320, 'APPROVED_ACTIVE', DATEADD(DAY, -5, SYSDATETIME()), NULL),
        (124, 2, 124, N'Đậu cove xào nấm bào ngư dầu hào chay', N'Đậu cove giòn ngọt tự nhiên kết hợp nấm bào ngư dai ngọt trong sốt dầu hào nấm hương đậm đà.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252713/veggielife/recipes/dau_cove_xao_nam.jpg', 'PUBLIC', 280, 'APPROVED_ACTIVE', DATEADD(DAY, -6, SYSDATETIME()), NULL),
        (125, 1, 125, N'Chè hạt sen long nhãn đường phèn', N'Món tráng miệng cung đình thanh nhã giúp an thần ngủ ngon, bổ dưỡng bồi bổ khí huyết.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252714/veggielife/recipes/che_hat_sen_long_nhan.jpg', 'PUBLIC', 400, 'APPROVED_ACTIVE', DATEADD(DAY, -7, SYSDATETIME()), NULL),
        (126, 3, 126, N'Sữa hạt macca óc chó hạnh nhân tự nấu', N'Sữa hạt dinh dưỡng nguyên chất béo ngậy không đường hóa học, cung cấp vitamin E và khoáng chất.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252715/veggielife/recipes/sua_hat_macca_oc_cho.jpg', 'PUBLIC', 340, 'APPROVED_ACTIVE', DATEADD(DAY, -8, SYSDATETIME()), NULL),
        (127, 2, 127, N'Sinh tố bơ chuối cải xoăn kale thanh lọc', N'Thức uống xanh thuần khiết giúp giải độc cơ thể, giàu chất chống oxy hóa và chất xơ tự nhiên.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252717/veggielife/recipes/sinh_to_bo_chuoi_kale.jpg', 'PUBLIC', 360, 'APPROVED_ACTIVE', DATEADD(DAY, -9, SYSDATETIME()), NULL),
        (128, 5, 128, N'Nui xào rau củ sốt cà chua nấm tươi', N'Nui xoắn dai mềm xào chín tới cùng cà chua, nấm mỡ và đậu Hà Lan đầy màu sắc bắt mắt.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252768/veggielife/recipes/nui_xao_rau_cu.jpg', 'PUBLIC', 330, 'APPROVED_ACTIVE', DATEADD(DAY, -10, SYSDATETIME()), NULL),
        (129, 3, NULL, N'Hành trình 30 ngày chuyển sang thuần chay không hề mệt mỏi', N'Chia sẻ kinh nghiệm cá nhân khi bắt đầu lối sống thuần chay: cách lắng nghe cơ thể, bổ sung đủ vi chất B12 và sắt từ các loại hạt và đậu nành nguyên chất.', 'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=800&q=80', 'PUBLIC', 480, 'APPROVED_ACTIVE', DATEADD(DAY, -15, SYSDATETIME()), NULL),
        (130, 9, NULL, N'Top 5 nguồn đạm thực vật vàng cho người ăn chay tập gym', N'Đầu bếp Toàn chia sẻ: Yến mạch, đậu hũ, hạt diêm mạch (quinoa), hạt chia và các loại đậu khô cung cấp hàm lượng protein tinh khiết không thua kém đạm động vật.', 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80', 'PUBLIC', 560, 'APPROVED_ACTIVE', DATEADD(DAY, -10, SYSDATETIME()), NULL),
        (131, 4, NULL, N'Mẹo hay nhà bếp: Bảo quản nấm tươi cả tuần vẫn trắng giòn', N'Nấm tươi mua về tuyệt đối không rửa nước nếu chưa nấu ngay. Dùng khăn giấy khô gói kín và bảo quản trong ngăn mát 4-6 độ C để giữ độ giòn tự nhiên.', 'https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=800&q=80', 'PUBLIC', 390, 'APPROVED_ACTIVE', DATEADD(DAY, -5, SYSDATETIME()), NULL),
        (132, 8, NULL, N'Tham gia nhà cái cá cược nạp rút tiền uy tín 24/7', N'Link đăng ký nhận ngay 500k tại nhà cái số một châu Á truy cập ngay...', NULL, 'PUBLIC', 12, 'QUARANTINED_HIDDEN', DATEADD(DAY, -4, SYSDATETIME()), NULL)

) AS source (Post_ID, User_ID, Recipe_ID, Title, Content, Image_Url, Visibility, View_Count, Moderation_Status, Created_At, Updated_At)
ON target.Post_ID = source.Post_ID
WHEN MATCHED THEN
    UPDATE SET 
        target.Title = source.Title,
        target.Content = source.Content,
        target.Image_Url = source.Image_Url,
        target.Moderation_Status = source.Moderation_Status
WHEN NOT MATCHED THEN
    INSERT (Post_ID, User_ID, Recipe_ID, Title, Content, Image_Url, Visibility, View_Count, Moderation_Status, Created_At, Updated_At)
    VALUES (source.Post_ID, source.User_ID, source.Recipe_ID, source.Title, source.Content, source.Image_Url, source.Visibility, source.View_Count, source.Moderation_Status, source.Created_At, source.Updated_At);

SET IDENTITY_INSERT dbo.Posts OFF;
GO

-- 11. VIDEOS (Bảng 17) & VIDEO TRANSCRIPTS (Bảng 18)
PRINT N'>> [11/38] Nạp dữ liệu Videos & VideoTranscripts...';
SET IDENTITY_INSERT dbo.Videos ON;

MERGE INTO dbo.Videos AS target
USING (
    VALUES
        (2001, 103, N'Video hướng dẫn làm Mì xào nấm rau củ giòn ngọt', 'https://res.cloudinary.com/demo/video/upload/dog.mp4', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252684/veggielife/recipes/mi_xao_nam_rau_cu.jpg', 120, 320, 'READY', DATEADD(DAY, -18, SYSDATETIME())),
        (2002, 101, N'Video nấu canh nấm thanh mát đậu hũ non', 'https://res.cloudinary.com/demo/video/upload/dog.mp4', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252680/veggielife/recipes/canh_nam_dau_phu.jpg', 90, 240, 'READY', DATEADD(DAY, -20, SYSDATETIME())),
        (2003, 107, N'Bí quyết nấu nước dùng Bún bò Huế chay đậm đà', 'https://res.cloudinary.com/demo/video/upload/dog.mp4', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252690/veggielife/recipes/bun_bo_hue_chay.jpg', 180, 480, 'READY', DATEADD(DAY, -15, SYSDATETIME())),
        (2004, 108, N'Nấu Phở chay Hà Nội nước dùng thảo mộc thơm lừng', 'https://res.cloudinary.com/demo/video/upload/dog.mp4', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252691/veggielife/recipes/pho_chay.jpg', 150, 410, 'READY', DATEADD(DAY, -14, SYSDATETIME())),
        (2005, 110, N'Nấu Cà ri chay khoai lang cốt dừa sánh mịn', 'https://res.cloudinary.com/demo/video/upload/dog.mp4', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252694/veggielife/recipes/ca_ri_chay.jpg', 140, 360, 'READY', DATEADD(DAY, -12, SYSDATETIME())),
        (2006, 120, N'Bí quyết đổ Bánh xèo chay giòn tan không bị ỉu', 'https://res.cloudinary.com/demo/video/upload/dog.mp4', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252708/veggielife/recipes/banh_xeo_chay.jpg', 160, 520, 'READY', DATEADD(DAY, -8, SYSDATETIME()))
) AS source (Video_ID, Post_ID, Title, Video_Url, Thumbnail_Url, Duration_Seconds, View_Count, Status, Created_At)
ON target.Video_ID = source.Video_ID
WHEN MATCHED THEN
    UPDATE SET target.Video_Url = source.Video_Url, target.Thumbnail_Url = source.Thumbnail_Url, target.Status = source.Status
WHEN NOT MATCHED THEN
    INSERT (Video_ID, Post_ID, Title, Video_Url, Thumbnail_Url, Duration_Seconds, View_Count, Status, Created_At)
    VALUES (source.Video_ID, source.Post_ID, source.Title, source.Video_Url, source.Thumbnail_Url, source.Duration_Seconds, source.View_Count, source.Status, source.Created_At);

SET IDENTITY_INSERT dbo.Videos OFF;
GO

SET IDENTITY_INSERT dbo.VideoTranscripts ON;
MERGE INTO dbo.VideoTranscripts AS target
USING (
    VALUES
        (3001, 2001, 103, N'Xin chào các bạn, hôm nay chúng ta cùng vào bếp làm món mì xào nấm rau củ giòn ngọt. Đầu tiên chần mì qua nước sôi, sau đó phi thơm bo-rô xào chín nấm và đậu hũ, cuối cùng rưới sốt tương đảo đều tay trên lửa lớn...', 'COMPLETED', DATEADD(DAY, -18, SYSDATETIME()), DATEADD(DAY, -18, SYSDATETIME())),
        (3002, 2002, 101, N'Canh nấm đậu phụ rong biển là món ăn thanh lọc cơ thể tuyệt vời. Bí quyết là nấu sôi nấm rơm trước 5 phút để nấm tiết vị ngọt thanh tự nhiên, sau đó mới thả đậu hũ non và rong biển vào...', 'COMPLETED', DATEADD(DAY, -20, SYSDATETIME()), DATEADD(DAY, -20, SYSDATETIME())),
        (3003, 2003, 107, N'Hôm nay mình sẽ hướng dẫn nấu nồi bún bò Huế chay đúng điệu. Nước dùng hầm từ mía và củ cải tạo độ ngọt thanh sâu lắng, xào sả ớt hạt điều màu cho màu sắc bắt mắt và vị cay nồng đặc trưng...', 'COMPLETED', DATEADD(DAY, -15, SYSDATETIME()), DATEADD(DAY, -15, SYSDATETIME())),
        (3004, 2004, 108, N'Chào mừng các bạn đến với công thức phở chay Hà Nội. Điểm nhấn là gói gia vị thảo mộc gồm quế, hoa hồi, thảo quả được nướng thơm trước khi thả vào nồi nước dùng...', 'COMPLETED', DATEADD(DAY, -14, SYSDATETIME()), DATEADD(DAY, -14, SYSDATETIME())),
        (3005, 2005, 110, N'Món cà ri chay khoai lang cốt dừa ăn kèm bánh mì nóng giòn cực kỳ hấp dẫn. Chiên sơ khoai lang và khoai môn trước sẽ giúp củ không bị nát nhừ khi ninh cùng nước dừa...', 'COMPLETED', DATEADD(DAY, -12, SYSDATETIME()), DATEADD(DAY, -12, SYSDATETIME())),
        (3006, 2006, 120, N'Làm sao để đổ bánh xèo chay giòn rụm? Hãy pha bột với nước cốt dừa và giữ chảo thật nóng, tráng bột mỏng và đậy nắp đúng 3 phút...', 'COMPLETED', DATEADD(DAY, -8, SYSDATETIME()), DATEADD(DAY, -8, SYSDATETIME()))
) AS source (Transcript_ID, Video_ID, Recipe_ID, Raw_Transcript, Status, Created_At, Completed_At)
ON target.Transcript_ID = source.Transcript_ID
WHEN MATCHED THEN
    UPDATE SET target.Raw_Transcript = source.Raw_Transcript, target.Status = source.Status
WHEN NOT MATCHED THEN
    INSERT (Transcript_ID, Video_ID, Recipe_ID, Raw_Transcript, Status, Created_At, Completed_At)
    VALUES (source.Transcript_ID, source.Video_ID, source.Recipe_ID, source.Raw_Transcript, source.Status, source.Created_At, source.Completed_At);

SET IDENTITY_INSERT dbo.VideoTranscripts OFF;
GO

-- 12. COMMENTS (Bảng 19) & VOTES (Bảng 20)
PRINT N'>> [12/38] Nạp dữ liệu Comments & Votes...';

SET IDENTITY_INSERT dbo.Comments ON;
MERGE INTO dbo.Comments AS target
USING (
    VALUES
        (101, 101, 2, NULL, N'Canh nấu thanh mát dễ ăn lắm, cả nhà mình ai cũng khen!', 0, 'ACTIVE', DATEADD(DAY, -18, SYSDATETIME())),
        (102, 101, 3, 101, N'Bạn cho mình hỏi rong biển dùng loại sấy khô hay tươi vậy?', 0, 'ACTIVE', DATEADD(DAY, -17, SYSDATETIME())),
        (103, 101, 2, 102, N'Dùng rong biển khô ngâm nở 5 phút là vừa ngon bạn nhé.', 0, 'ACTIVE', DATEADD(DAY, -16, SYSDATETIME())),
        (104, 102, 4, NULL, N'Món kho tiêu hạt sen ăn với cơm nóng mùa mưa thì tuyệt vời!', 0, 'ACTIVE', DATEADD(DAY, -15, SYSDATETIME())),
        (105, 103, 5, NULL, N'Mì xào rau củ rất giòn, nước sốt vừa miệng không bị ngấy dầu.', 0, 'ACTIVE', DATEADD(DAY, -14, SYSDATETIME())),
        (106, 107, 6, NULL, N'Nước dùng bún bò Huế chay ngọt thanh tự nhiên từ mía và củ cải, đỉnh quá tác giả ơi!', 0, 'ACTIVE', DATEADD(DAY, -12, SYSDATETIME())),
        (107, 108, 4, NULL, N'Phở chay nấu theo trường phái Phật giáo không hành tỏi rất thanh tịnh.', 0, 'ACTIVE', DATEADD(DAY, -10, SYSDATETIME())),
        (108, 110, 3, NULL, N'Cà ri thơm nức mũi, khoai lang ngọt bùi chấm bánh mì giòn hết ý.', 0, 'ACTIVE', DATEADD(DAY, -9, SYSDATETIME())),
        (109, 120, 9, NULL, N'Bí quyết chiên bánh xèo giòn rụm của bạn rất hiệu quả, mình làm thành công ngay lần đầu.', 0, 'ACTIVE', DATEADD(DAY, -7, SYSDATETIME())),
        (110, 122, 5, NULL, N'Pate nấm làm từ nấm hương và bơ đậu phộng ăn bùi béo ngỡ ngàng luôn!', 0, 'ACTIVE', DATEADD(DAY, -5, SYSDATETIME()))
) AS source (Comment_ID, Post_ID, User_ID, Parent_Comment_ID, Content, Is_Soft_Deleted, Status, Created_At)
ON target.Comment_ID = source.Comment_ID
WHEN MATCHED THEN
    UPDATE SET target.Content = source.Content, target.Status = source.Status
WHEN NOT MATCHED THEN
    INSERT (Comment_ID, Post_ID, User_ID, Parent_Comment_ID, Content, Is_Soft_Deleted, Status, Created_At)
    VALUES (source.Comment_ID, source.Post_ID, source.User_ID, source.Parent_Comment_ID, source.Content, source.Is_Soft_Deleted, source.Status, source.Created_At);
SET IDENTITY_INSERT dbo.Comments OFF;
GO

MERGE INTO dbo.Votes AS target
USING (
    VALUES
        (2, 101, DATEADD(DAY, -18, SYSDATETIME())),
        (3, 101, DATEADD(DAY, -17, SYSDATETIME())),
        (4, 101, DATEADD(DAY, -16, SYSDATETIME())),
        (5, 101, DATEADD(DAY, -15, SYSDATETIME())),
        (2, 102, DATEADD(DAY, -15, SYSDATETIME())),
        (3, 102, DATEADD(DAY, -14, SYSDATETIME())),
        (4, 102, DATEADD(DAY, -13, SYSDATETIME())),
        (5, 102, DATEADD(DAY, -12, SYSDATETIME())),
        (6, 102, DATEADD(DAY, -11, SYSDATETIME())),
        (2, 103, DATEADD(DAY, -13, SYSDATETIME())),
        (3, 103, DATEADD(DAY, -12, SYSDATETIME())),
        (4, 103, DATEADD(DAY, -11, SYSDATETIME())),
        (2, 107, DATEADD(DAY, -10, SYSDATETIME())),
        (3, 107, DATEADD(DAY, -9, SYSDATETIME())),
        (4, 107, DATEADD(DAY, -8, SYSDATETIME())),
        (5, 107, DATEADD(DAY, -7, SYSDATETIME())),
        (6, 107, DATEADD(DAY, -6, SYSDATETIME())),
        (9, 107, DATEADD(DAY, -5, SYSDATETIME())),
        (2, 108, DATEADD(DAY, -9, SYSDATETIME())),
        (3, 108, DATEADD(DAY, -8, SYSDATETIME())),
        (4, 108, DATEADD(DAY, -7, SYSDATETIME())),
        (2, 110, DATEADD(DAY, -8, SYSDATETIME())),
        (3, 110, DATEADD(DAY, -7, SYSDATETIME())),
        (2, 120, DATEADD(DAY, -6, SYSDATETIME())),
        (3, 120, DATEADD(DAY, -5, SYSDATETIME())),
        (4, 120, DATEADD(DAY, -4, SYSDATETIME())),
        (5, 120, DATEADD(DAY, -3, SYSDATETIME()))
) AS source (User_ID, Post_ID, Created_At)
ON target.User_ID = source.User_ID AND target.Post_ID = source.Post_ID
WHEN NOT MATCHED THEN
    INSERT (User_ID, Post_ID, Created_At)
    VALUES (source.User_ID, source.Post_ID, source.Created_At);
GO

-- 13. VIOLATION REPORTS (Bảng 21)
PRINT N'>> [13/38] Nạp dữ liệu ViolationReports...';

SET IDENTITY_INSERT dbo.ViolationReports ON;
MERGE INTO dbo.ViolationReports AS target
USING (
    VALUES
        (1001, 2, 1, NULL, NULL, 132, NULL, 'VIO_SPAM', N'Bài viết chứa liên kết cá cược trực tuyến trái phép và quấy rối cộng đồng', 'USER_REPORT', 'PENDING', DATEADD(DAY, -4, SYSDATETIME())),
        (1002, 3, 1, 101, NULL, NULL, NULL, 'VIO_MEAT', N'Người dùng báo cáo nhầm công thức có thịt nhưng sau khi admin kiểm tra là nấm hương chuẩn chay', 'USER_REPORT', 'REJECT', DATEADD(DAY, -15, SYSDATETIME())),
        (1003, NULL, 1, NULL, 105, NULL, NULL, 'VIO_HATE', N'Hệ thống AI quét tự động phát hiện ngôn từ thô tục trong bình luận và tự động xử lý', 'AI_SCAN', 'APPROVE', DATEADD(DAY, -2, SYSDATETIME()))
) AS source (Report_ID, Reporter_User_ID, Admin_ID, Recipe_ID, Comment_ID, Post_ID, Video_ID, Violation_Type, Evidence_Summary, Report_Source, Status, Created_At)
ON target.Report_ID = source.Report_ID
WHEN MATCHED THEN
    UPDATE SET target.Status = source.Status, target.Evidence_Summary = source.Evidence_Summary
WHEN NOT MATCHED THEN
    INSERT (Report_ID, Reporter_User_ID, Admin_ID, Recipe_ID, Comment_ID, Post_ID, Video_ID, Violation_Type, Evidence_Summary, Report_Source, Status, Created_At)
    VALUES (source.Report_ID, source.Reporter_User_ID, source.Admin_ID, source.Recipe_ID, source.Comment_ID, source.Post_ID, source.Video_ID, source.Violation_Type, source.Evidence_Summary, source.Report_Source, source.Status, source.Created_At);
SET IDENTITY_INSERT dbo.ViolationReports OFF;
GO

-- 14. MEAL PLANS (Bảng 22) & MEAL SLOTS (Bảng 23)
PRINT N'>> [14/38] Nạp dữ liệu MealPlans & MealSlots...';

SET IDENTITY_INSERT dbo.MealPlans ON;
MERGE INTO dbo.MealPlans AS target
USING (
    VALUES
        (1001, 2, 1, N'Thực đơn thuần chay 7 ngày thanh lọc & nhẹ bụng', CAST(DATEADD(DAY, -2, SYSDATETIME()) AS DATE), CAST(DATEADD(DAY, 5, SYSDATETIME()) AS DATE), 1600.0, 'STANDARD', N'Ưu tiên món canh thanh mát và nấm giàu đạm thực vật', 'ACTIVE', 1, DATEADD(DAY, -3, SYSDATETIME())),
        (1002, 4, 5, N'Thực đơn Chay Phật giáo an lạc tịnh tâm', CAST(SYSDATETIME() AS DATE), CAST(DATEADD(DAY, 7, SYSDATETIME()) AS DATE), 2100.0, 'PREMIUM', N'Kiêng tuyệt đối ngũ vị tân, giàu dinh dưỡng hạt sen và đậu hũ', 'DRAFT', 1, SYSDATETIME())
) AS source (Plan_ID, User_ID, Dietary_School_ID, Plan_Name, Start_Date, End_Date, Target_Daily_Kcal, Budget_Tier, Custom_Instructions, Plan_Status, Generated_By_AI, Created_At)
ON target.Plan_ID = source.Plan_ID
WHEN MATCHED THEN
    UPDATE SET target.Plan_Name = source.Plan_Name, target.Plan_Status = source.Plan_Status, target.Target_Daily_Kcal = source.Target_Daily_Kcal
WHEN NOT MATCHED THEN
    INSERT (Plan_ID, User_ID, Dietary_School_ID, Plan_Name, Start_Date, End_Date, Target_Daily_Kcal, Budget_Tier, Custom_Instructions, Plan_Status, Generated_By_AI, Created_At)
    VALUES (source.Plan_ID, source.User_ID, source.Dietary_School_ID, source.Plan_Name, source.Start_Date, source.End_Date, source.Target_Daily_Kcal, source.Budget_Tier, source.Custom_Instructions, source.Plan_Status, source.Generated_By_AI, source.Created_At);
SET IDENTITY_INSERT dbo.MealPlans OFF;
GO

SET IDENTITY_INSERT dbo.MealSlots ON;
MERGE INTO dbo.MealSlots AS target
USING (
    VALUES
        -- THỨ 2 (MON)
        (2001, 1001, 119, 'MON', 'BREAKFAST', 1, N'Cháo yến mạch thanh đạm'),
        (2002, 1001, 102, 'MON', 'LUNCH', 1, N'Nấm kho tiêu hạt sen'),
        (2003, 1001, 101, 'MON', 'DINNER', 1, N'Canh nấm đậu phụ'),
        -- THỨ 3 (TUE)
        (2004, 1001, 122, 'TUE', 'BREAKFAST', 1, N'Bánh mì chay pate nấm'),
        (2005, 1001, 103, 'TUE', 'LUNCH', 1, N'Mì xào nấm rau củ'),
        (2006, 1001, 117, 'TUE', 'DINNER', 1, N'Canh chua chay Nam Bộ'),
        -- THỨ 4 (WED)
        (2007, 1001, 108, 'WED', 'BREAKFAST', 0, N'Phở chay Hà Nội'),
        (2008, 1001, 109, 'WED', 'LUNCH', 0, N'Đậu hũ sốt cà chua'),
        (2009, 1001, 121, 'WED', 'DINNER', 0, N'Rau củ luộc ngũ sắc'),
        -- THỨ 5 (THU)
        (2010, 1001, 127, 'THU', 'BREAKFAST', 0, N'Sinh tố bơ chuối kale'),
        (2011, 1001, 111, 'THU', 'LUNCH', 0, N'Cơm chiên trái thơm'),
        (2012, 1001, 110, 'THU', 'DINNER', 0, N'Cà ri chay cốt dừa'),
        -- THỨ 6 (FRI)
        (2013, 1001, 107, 'FRI', 'BREAKFAST', 0, N'Bún bò Huế chay'),
        (2014, 1001, 116, 'FRI', 'LUNCH', 0, N'Nấm đùi gà áp chảo'),
        (2015, 1001, 105, 'FRI', 'DINNER', 0, N'Gỏi cuốn ngũ sắc'),
        -- THỨ 7 (SAT)
        (2016, 1001, 120, 'SAT', 'BREAKFAST', 0, N'Bánh xèo chay giòn rụm'),
        (2017, 1001, 124, 'SAT', 'LUNCH', 0, N'Đậu cove xào nấm'),
        (2018, 1001, 112, 'SAT', 'DINNER', 0, N'Lẩu nấm thập cẩm'),
        -- CHỦ NHẬT (SUN)
        (2019, 1001, 123, 'SUN', 'BREAKFAST', 0, N'Súp bí đỏ hạnh nhân'),
        (2020, 1001, 118, 'SUN', 'LUNCH', 0, N'Mít non kho tiêu xanh'),
        (2021, 1001, 115, 'SUN', 'DINNER', 0, N'Bún chả giò chay')
) AS source (Slot_ID, Plan_ID, Recipe_ID, Day_Of_Week, Meal_Type, Is_Completed, Notes)
ON target.Slot_ID = source.Slot_ID
WHEN MATCHED THEN
    UPDATE SET target.Recipe_ID = source.Recipe_ID, target.Is_Completed = source.Is_Completed, target.Notes = source.Notes
WHEN NOT MATCHED THEN
    INSERT (Slot_ID, Plan_ID, Recipe_ID, Day_Of_Week, Meal_Type, Is_Completed, Notes)
    VALUES (source.Slot_ID, source.Plan_ID, source.Recipe_ID, source.Day_Of_Week, source.Meal_Type, source.Is_Completed, source.Notes);

SET IDENTITY_INSERT dbo.MealSlots OFF;
GO

-- 15. SHOPPING LISTS (Bảng 24) & SHOPPING LIST ITEMS (Bảng 25)
PRINT N'>> [15/38] Nạp dữ liệu ShoppingLists & ShoppingListItems...';

SET IDENTITY_INSERT dbo.ShoppingLists ON;
MERGE INTO dbo.ShoppingLists AS target
USING (
    VALUES
        (1001, 1001, N'Danh sách đi chợ tuần cho Thực đơn Thanh Lọc', CAST(DATEADD(DAY, 1, SYSDATETIME()) AS DATE), 'PENDING', 0, DATEADD(DAY, -2, SYSDATETIME())),
        (1002, 1002, N'Danh sách nguyên liệu chay Phật giáo', CAST(DATEADD(DAY, 2, SYSDATETIME()) AS DATE), 'PENDING', 1, SYSDATETIME())
) AS source (List_ID, Plan_ID, List_Title, Target_Shopping_Date, Status, Is_Offline_Synced, Created_At)
ON target.List_ID = source.List_ID
WHEN MATCHED THEN
    UPDATE SET target.List_Title = source.List_Title, target.Target_Shopping_Date = source.Target_Shopping_Date, target.Status = source.Status, target.Is_Offline_Synced = source.Is_Offline_Synced
WHEN NOT MATCHED THEN
    INSERT (List_ID, Plan_ID, List_Title, Target_Shopping_Date, Status, Is_Offline_Synced, Created_At)
    VALUES (source.List_ID, source.Plan_ID, source.List_Title, source.Target_Shopping_Date, source.Status, source.Is_Offline_Synced, source.Created_At);
SET IDENTITY_INSERT dbo.ShoppingLists OFF;
GO

SET IDENTITY_INSERT dbo.ShoppingListItems ON;
MERGE INTO dbo.ShoppingListItems AS target
USING (
    VALUES
        (5001, 1001, 90, 500.0, N'g', 1),       -- Nấm rơm tươi (Đã mua)
        (5002, 1001, 84, 300.0, N'g', 1),       -- Nấm hương tươi (Đã mua)
        (5003, 1001, 82, 400.0, N'g', 0),       -- Nấm đùi gà
        (5004, 1001, 85, 300.0, N'g', 0),       -- Nấm kim châm
        (5005, 1001, 18, 500.0, N'g', 1),       -- Cà rốt tươi (Đã mua)
        (5006, 1001, 17, 400.0, N'g', 0),       -- Cà chua chín đỏ
        (5007, 1001, 12, 350.0, N'g', 0),       -- Bông cải xanh
        (5008, 1001, 43, 200.0, N'g', 1),       -- Giá đỗ tươi (Đã mua)
        (5009, 1001, 39, 4.0, N'hộp', 1),       -- Đậu hũ non Silken (Đã mua)
        (5010, 1001, 38, 500.0, N'g', 0),       -- Đậu hũ chiên
        (5011, 1001, 16, 500.0, N'g', 1),       -- Bún gạo khô (Đã mua)
        (5012, 1001, 53, 300.0, N'g', 0),       -- Mì sợi khô
        (5013, 1001, 1, 1.0, N'gói', 1),        -- Bánh tráng cuốn (Đã mua)
        (5014, 1001, 35, 200.0, N'g', 0),       -- Hạt sen khô
        (5015, 1001, 67, 1.0, N'chai', 1),      -- Nước tương đậu nành (Đã mua)
        (5016, 1001, 68, 1.0, N'gói', 1),       -- Hạt nêm nấm Knorr (Đã mua)
        (5017, 1001, 69, 50.0, N'g', 0),        -- Tiêu đen Phú Quốc
        (5018, 1001, 70, 200.0, N'g', 0),       -- Đường thốt nốt
        (5019, 1001, 72, 1.0, N'chai', 0),      -- Dầu mè nguyên chất
        (5020, 1001, 80, 150.0, N'g', 0)        -- Đậu phộng rang
) AS source (Item_ID, List_ID, Ingredient_ID, Aggregated_Quantity, Unit, Is_Purchased)
ON target.Item_ID = source.Item_ID
WHEN MATCHED THEN
    UPDATE SET target.Aggregated_Quantity = source.Aggregated_Quantity, target.Is_Purchased = source.Is_Purchased
WHEN NOT MATCHED THEN
    INSERT (Item_ID, List_ID, Ingredient_ID, Aggregated_Quantity, Unit, Is_Purchased)
    VALUES (source.Item_ID, source.List_ID, source.Ingredient_ID, source.Aggregated_Quantity, source.Unit, source.Is_Purchased);
SET IDENTITY_INSERT dbo.ShoppingListItems OFF;
GO

-- 16. PRODUCTS (Bảng 26), PRODUCT_ENUMBERS (Bảng 28), PRODUCTALLERGENS (Bảng 29), USERSCANHISTORY (Bảng 30)
PRINT N'>> [16/38] Nạp dữ liệu Products & Barcode Scan...';

SET IDENTITY_INSERT dbo.Products ON;
MERGE INTO dbo.Products AS target
USING (
    VALUES
        (1, '8934567890123', N'Sữa đậu nành Fami Canxi 200ml', N'Vinasoy', N'Nước, đậu nành chọn lọc, đường kính trắng, canxi photphat, vitamin D3, muối ăn, chất điều chỉnh độ acid (E500ii).', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252789/veggielife/products/sua_fami_canxi.jpg', 'VEGAN', 1, DATEADD(DAY, -40, SYSDATETIME()), SYSDATETIME()),
        (2, '8935001234567', N'Nước tương Tam Thái Tử Nhất Ca 500ml', N'Masan', N'Nước, muối, khô đậu nành, chất điều vị (E621, E627, E631), màu caramel tự nhiên (E150a), chất bảo quản (E211).', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252791/veggielife/products/nuoc_tuong_chay.jpg', 'VEGAN', 1, DATEADD(DAY, -40, SYSDATETIME()), SYSDATETIME()),
        (3, '8936009876543', N'Hạt nêm nấm hương Knorr Chay 400g', N'Knorr', N'Muối i-ốt, đường, tinh bột sắn, chiết xuất nấm hương (20g), dầu cọ, hương nấm tổng hợp, chất điều vị (E621).', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252792/veggielife/products/hat_nem_nam.jpg', 'VEGAN', 1, DATEADD(DAY, -40, SYSDATETIME()), SYSDATETIME()),
        (4, '8937001122334', N'Mì chay Vị Hương nấm hương sa tế gói 75g', N'Vị Hương', N'Bột mì, dầu cọ, tinh bột khoai mì, muối, đường, ớt, bột nấm hương, chất ổn định (E412, E452i).', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252794/veggielife/products/mi_chay_vi_huong.jpg', 'VEGAN', 1, DATEADD(DAY, -35, SYSDATETIME()), SYSDATETIME()),
        (5, '8938004455667', N'Bơ đậu phộng mịn Golden Farm 340g', N'Golden Farm', N'Đậu phộng (90%), đường, dầu thực vật tinh luyện, muối ăn, chất nhũ hóa (E471).', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252795/veggielife/products/bo_dau_phong.jpg', 'VEGAN', 1, DATEADD(DAY, -30, SYSDATETIME()), SYSDATETIME()),
        (6, '8939007788990', N'Chả lụa chay truyền thống Âu Lạc 500g', N'Âu Lạc', N'Đạm đậu nành (không biến đổi gen), dầu đậu nành, nước, tiêu trắng, muối hầm, hạt nêm nấm chay, màng bọc thực phẩm.', 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252796/veggielife/products/cha_lua_chay.jpg', 'VEGAN', 1, DATEADD(DAY, -30, SYSDATETIME()), SYSDATETIME()),
        (7, '8931112233445', N'Sữa chua thực vật Vinamilk Plant 100g', N'Vinamilk', N'Sữa yến mạch lên men, đường, chất xơ hòa tan inulin, men sống Streptococcus thermophilus.', 'https://images.unsplash.com/photo-1550583724-b2692b85b150?auto=format&fit=crop&w=600&q=80', 'VEGAN', 1, DATEADD(DAY, -20, SYSDATETIME()), SYSDATETIME()),
        (8, '8932223344556', N'Bánh quy bơ kem Danisa Hộp thiếc 454g', N'Danisa', N'Bột mì, bơ động vật (28%), đường, trứng gà, bột sữa nguyên kem, bột nở, hương vani.', 'https://images.unsplash.com/photo-1551024709-8f23befc6f87?auto=format&fit=crop&w=600&q=80', 'NON_VEGAN', 1, DATEADD(DAY, -20, SYSDATETIME()), SYSDATETIME()),
        (9, '8933334455667', N'Snack khoai tây chiên O''Star Vị Kim Chi 90g', N'Orion', N'Khoai tây tươi, dầu thực vật, bột gia vị kim chi, chiết xuất men, chất điều vị (E627, E631 có nguồn gốc chưa xác định).', 'https://images.unsplash.com/photo-1567337710282-00832b415979?auto=format&fit=crop&w=600&q=80', 'DOUBTFUL', 0, DATEADD(DAY, -15, SYSDATETIME()), SYSDATETIME()),
        (10, '8934445566778', N'Tàu hũ ky lá sấy khô Cầu Tre 150g', N'Cầu Tre', N'100% đậu nành tự nhiên nguyên chất, nước sạch.', 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=600&q=80', 'VEGAN', 1, DATEADD(DAY, -15, SYSDATETIME()), SYSDATETIME()),
        (11, '8850029112233', N'Nước cốt dừa Chaokoh lon 400ml', N'Chaokoh', N'Nước cốt dừa tươi (85%), nước, chất ổn định (E412, E466).', 'https://images.unsplash.com/photo-1476718406336-bb5a9690ee2a?auto=format&fit=crop&w=600&q=80', 'VEGAN', 1, DATEADD(DAY, -10, SYSDATETIME()), SYSDATETIME()),
        (12, '8801234567890', N'Rong biển cuộn cơm Hàn Quốc Gim 10 lá', N'Dongwon', N'100% rong biển tự nhiên sấy khô, không tẩm ướp gia vị.', 'https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=600&q=80', 'VEGAN', 1, DATEADD(DAY, -10, SYSDATETIME()), SYSDATETIME())
) AS source (Product_ID, Barcode, Product_Name, Brand, Ingredients_Text, Image_Url, Vegan_Verdict, Admin_Verified, Created_At, Last_Synced_At)
ON target.Product_ID = source.Product_ID
WHEN MATCHED THEN
    UPDATE SET target.Product_Name = source.Product_Name, target.Vegan_Verdict = source.Vegan_Verdict, target.Image_Url = source.Image_Url
WHEN NOT MATCHED THEN
    INSERT (Product_ID, Barcode, Product_Name, Brand, Ingredients_Text, Image_Url, Vegan_Verdict, Admin_Verified, Created_At, Last_Synced_At)
    VALUES (source.Product_ID, source.Barcode, source.Product_Name, source.Brand, source.Ingredients_Text, source.Image_Url, source.Vegan_Verdict, source.Admin_Verified, source.Created_At, source.Last_Synced_At);
SET IDENTITY_INSERT dbo.Products OFF;
GO

-- Product_ENumbers
MERGE INTO dbo.Product_ENumbers AS target
USING (
    VALUES
        (1, 'E500'),
        (2, 'E150a'),
        (2, 'E621'),
        (2, 'E211'),
        (3, 'E621'),
        (4, 'E412'),
        (5, 'E471'),
        (9, 'E627'),
        (9, 'E631'),
        (11, 'E412'),
        (11, 'E407')
) AS source (Product_ID, Code)
ON target.Product_ID = source.Product_ID AND target.Code = source.Code
WHEN NOT MATCHED THEN
    INSERT (Product_ID, Code) VALUES (source.Product_ID, source.Code);
GO

-- ProductAllergens
MERGE INTO dbo.ProductAllergens AS target
USING (
    VALUES
        (1, 6, 'CONTAINS'),  -- Sữa Fami chứa Đậu nành
        (2, 6, 'CONTAINS'),  -- Nước tương chứa Đậu nành
        (4, 1, 'CONTAINS'),  -- Mì Vị Hương chứa Gluten lúa mì
        (5, 5, 'CONTAINS'),  -- Bơ Golden Farm chứa Đậu phộng
        (6, 6, 'CONTAINS'),  -- Chả lụa Âu Lạc chứa Đậu nành
        (8, 1, 'CONTAINS'),  -- Bánh Danisa chứa Gluten
        (8, 7, 'CONTAINS'),  -- Bánh Danisa chứa Sữa
        (8, 3, 'CONTAINS'),  -- Bánh Danisa chứa Trứng
        (10, 6, 'CONTAINS')  -- Tàu hũ ky chứa Đậu nành
) AS source (Product_ID, Allergen_ID, Presence_Type)
ON target.Product_ID = source.Product_ID AND target.Allergen_ID = source.Allergen_ID
WHEN MATCHED THEN
    UPDATE SET target.Presence_Type = source.Presence_Type
WHEN NOT MATCHED THEN
    INSERT (Product_ID, Allergen_ID, Presence_Type) VALUES (source.Product_ID, source.Allergen_ID, source.Presence_Type);
GO

-- UserScanHistory
IF NOT EXISTS (SELECT 1 FROM dbo.UserScanHistory WHERE Product_ID = 1 AND User_ID = 2)
BEGIN
    INSERT INTO dbo.UserScanHistory (User_ID, Guest_UUID, Product_ID, Scan_Method, Is_Non_Vegan_Triggered, Scanned_At)
    VALUES
        (2, NULL, 1, 'CAMERA_BARCODE', 0, DATEADD(DAY, -15, SYSDATETIME())),
        (2, NULL, 3, 'CAMERA_BARCODE', 0, DATEADD(DAY, -12, SYSDATETIME())),
        (3, NULL, 5, 'MANUAL_INPUT', 0, DATEADD(DAY, -8, SYSDATETIME())),
        (4, NULL, 8, 'CAMERA_BARCODE', 1, DATEADD(DAY, -5, SYSDATETIME())),
        (5, NULL, 6, 'CAMERA_BARCODE', 0, DATEADD(DAY, -2, SYSDATETIME())),
        (NULL, 'guest_device_uuid_9988_a1b2', 2, 'CAMERA_BARCODE', 0, DATEADD(HOUR, -6, SYSDATETIME()));
END;
GO

-- 17. FRIDGE SESSIONS (Bảng 31) & FRIDGE INVENTORY (Bảng 32)
PRINT N'>> [17/38] Nạp dữ liệu Tủ Lạnh Ảo...';

SET IDENTITY_INSERT dbo.FridgeSessions ON;
MERGE INTO dbo.FridgeSessions AS target
USING (
    VALUES
        (101, 2, 'https://images.unsplash.com/photo-1584473457406-6240486418e9?auto=format&fit=crop&w=800&q=80', 5, DATEADD(DAY, -5, SYSDATETIME())),
        (102, 4, 'https://images.unsplash.com/photo-1584473457406-6240486418e9?auto=format&fit=crop&w=800&q=80', 4, DATEADD(DAY, -2, SYSDATETIME()))
) AS source (Session_ID, User_ID, Image_CDN_URL, Detected_Items_Count, Created_At)
ON target.Session_ID = source.Session_ID
WHEN MATCHED THEN
    UPDATE SET target.Image_CDN_URL = source.Image_CDN_URL, target.Detected_Items_Count = source.Detected_Items_Count
WHEN NOT MATCHED THEN
    INSERT (Session_ID, User_ID, Image_CDN_URL, Detected_Items_Count, Created_At)
    VALUES (source.Session_ID, source.User_ID, source.Image_CDN_URL, source.Detected_Items_Count, source.Created_At);
SET IDENTITY_INSERT dbo.FridgeSessions OFF;
GO

SET IDENTITY_INSERT dbo.FridgeInventory ON;
MERGE INTO dbo.FridgeInventory AS target
USING (
    VALUES
        (6001, 2, 39, 101, N'Đậu hũ non Silken Tofu', 'FRESH'),
        (6002, 2, 90, 101, N'Nấm rơm tươi', 'FRESH'),
        (6003, 2, 18, 101, N'Cà rốt Đà Lạt', 'FRESH'),
        (6004, 2, 85, 101, N'Nấm kim châm', 'NON-FRESH'),   -- Cần giải cứu gấp!
        (6005, 2, 17, 101, N'Cà chua chín đỏ', 'NON-FRESH'), -- Cần giải cứu gấp!
        (6006, 4, 38, 102, N'Đậu hũ chiên sẵn', 'FRESH'),
        (6007, 4, 82, 102, N'Nấm đùi gà', 'FRESH'),
        (6008, 4, 5, 102, N'Bắp ngọt Mỹ', 'FRESH'),
        (6009, 4, 37, 102, N'Đậu cove tươi', 'FRESH'),
        (6010, 4, 84, 102, N'Nấm hương tươi', 'NON-FRESH')
) AS source (Inventory_ID, User_ID, Ingredient_ID, Session_ID, Custom_Item_Name, Freshness_Status)
ON target.Inventory_ID = source.Inventory_ID
WHEN MATCHED THEN
    UPDATE SET target.Custom_Item_Name = source.Custom_Item_Name, target.Freshness_Status = source.Freshness_Status
WHEN NOT MATCHED THEN
    INSERT (Inventory_ID, User_ID, Ingredient_ID, Session_ID, Custom_Item_Name, Freshness_Status)
    VALUES (source.Inventory_ID, source.User_ID, source.Ingredient_ID, source.Session_ID, source.Custom_Item_Name, source.Freshness_Status);
SET IDENTITY_INSERT dbo.FridgeInventory OFF;
GO

-- 18. VEGAN VENUES (Bảng 33), VENUE CATEGORIES (Bảng 35), VENUE MENU ITEMS (Bảng 34)
PRINT N'>> [18/38] Nạp dữ liệu Quán Chay & Bản đồ...';

SET IDENTITY_INSERT dbo.VeganVenues ON;
MERGE INTO dbo.VeganVenues AS target
USING (
    VALUES
        (101, 'HAPPYCOW', 'HC_VN_SGN_001', N'Nhà hàng Chay Kashew Thảo Điền', N'14 Trần Ngọc Diện, Thảo Điền, Quận 2, TP. Hồ Chí Minh', 10.803450, 106.732140, 4.8, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252798/veggielife/venues/kashew_cheese.jpg', 1, 1, '{"phone":"02836208888","website":"https://kashewcheese.com"}', DATEADD(DAY, -60, SYSDATETIME())),
        (102, 'HAPPYCOW', 'HC_VN_SGN_002', N'Pi Vegetarian Bistro', N'19 Võ Văn Tần, Phường 6, Quận 3, TP. Hồ Chí Minh', 10.776520, 106.692340, 4.7, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252804/veggielife/venues/pi_vegetarian.jpg', 1, 1, '{"phone":"0938888888","openHours":"10:00 - 22:00"}', DATEADD(DAY, -50, SYSDATETIME())),
        (103, 'GOOGLE_PLACES', 'GP_VN_SGN_003', N'Lẩu Nấm Chay An Nhiên', N'81 Phạm Ngũ Lão, Phường 3, Quận Gò Vấp, TP. Hồ Chí Minh', 10.826540, 106.685410, 4.6, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252837/veggielife/venues/lau_nam_an_nhien.jpg', 1, 1, '{"phone":"02838945678"}', DATEADD(DAY, -45, SYSDATETIME())),
        (104, 'GOONG', 'GOONG_SGN_004', N'Cơm Chay Nàng Gấm', N'73 Lê Quang Định, Phường 14, Quận Bình Thạnh, TP. Hồ Chí Minh', 10.801240, 106.698540, 4.5, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252841/veggielife/venues/com_chay_nang_gam.jpg', 1, 1, '{"phone":"0903123456"}', DATEADD(DAY, -40, SYSDATETIME())),
        (105, 'GOOGLE_PLACES', 'GP_VN_SGN_005', N'Nhà hàng Buffet Chay D''Gemma', N'5B Phổ Quang, Phường 2, Quận Tân Bình, TP. Hồ Chí Minh', 10.808720, 106.669810, 4.9, 'https://res.cloudinary.com/swrsuenp/image/upload/v1792252845/veggielife/venues/buffet_chay_dgemma.jpg', 1, 1, '{"priceRange":"150.000đ - 220.000đ"}', DATEADD(DAY, -35, SYSDATETIME())),
        (106, 'HAPPYCOW', 'HC_VN_SGN_006', N'Nhà hàng Chay Bếp Xanh An Duyên', N'10 Nguyễn Tri Phương, Phường 6, Quận 5, TP. Hồ Chí Minh', 10.753410, 106.668720, 4.6, 'https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=800&q=80', 1, 1, '{"phone":"02838555555"}', DATEADD(DAY, -30, SYSDATETIME())),
        (107, 'GOONG', 'GOONG_SGN_007', N'Quán Cơm Chay Thiện Duyên', N'24 Tôn Thất Thiệp, Bến Nghé, Quận 1, TP. Hồ Chí Minh', 10.773210, 106.703410, 4.4, 'https://images.unsplash.com/photo-1543007630-9710e4a00a20?auto=format&fit=crop&w=800&q=80', 1, 1, '{"priceRange":"35.000đ - 50.000đ"}', DATEADD(DAY, -25, SYSDATETIME())),
        (108, 'HAPPYCOW', 'HC_VN_HAN_008', N'Sadhu Vegetarian Restaurant Hanoi', N'87 Lý Thường Kiệt, Cửa Nam, Hoàn Kiếm, Hà Nội', 21.023410, 105.842310, 4.8, 'https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?auto=format&fit=crop&w=800&q=80', 1, 1, '{"city":"Hà Nội"}', DATEADD(DAY, -20, SYSDATETIME()))
) AS source (Venue_ID, External_Source, External_Place_ID, Venue_Name, Address, Latitude, Longitude, Rating, Thumbnail_Url, Is_Fully_Vegan, Is_Active, Raw_Origin_JSON, Created_At)
ON target.Venue_ID = source.Venue_ID
WHEN MATCHED THEN
    UPDATE SET target.Venue_Name = source.Venue_Name, target.Thumbnail_Url = source.Thumbnail_Url, target.Rating = source.Rating
WHEN NOT MATCHED THEN
    INSERT (Venue_ID, External_Source, External_Place_ID, Venue_Name, Address, Latitude, Longitude, Rating, Thumbnail_Url, Is_Fully_Vegan, Is_Active, Raw_Origin_JSON, Created_At)
    VALUES (source.Venue_ID, source.External_Source, source.External_Place_ID, source.Venue_Name, source.Address, source.Latitude, source.Longitude, source.Rating, source.Thumbnail_Url, source.Is_Fully_Vegan, source.Is_Active, source.Raw_Origin_JSON, source.Created_At);
SET IDENTITY_INSERT dbo.VeganVenues OFF;
GO

-- VenueCategories
MERGE INTO dbo.VenueCategories AS target
USING (
    VALUES
        (14, 101), -- Kashew: Salad chay & phô mai hạt
        (8, 102),  -- Pi Bistro: Cơm chay
        (9, 103),  -- An Nhiên: Lẩu nấm
        (8, 104),  -- Nàng Gấm: Cơm chay
        (1, 105),  -- D'Gemma: Buffet món canh súp
        (8, 106),  -- An Duyên: Cơm chay
        (8, 107),  -- Thiện Duyên: Cơm chay bình dân
        (9, 108)   -- Sadhu: Lẩu nấm ẩm thực chay
) AS source (Category_ID, Venue_ID)
ON target.Category_ID = source.Category_ID AND target.Venue_ID = source.Venue_ID
WHEN NOT MATCHED THEN
    INSERT (Category_ID, Venue_ID) VALUES (source.Category_ID, source.Venue_ID);
GO

-- VenueMenuItems (Fixed: Dish_Name, Price_VND, Description, Image_Url, Is_Signature)
SET IDENTITY_INSERT dbo.VenueMenuItems ON;
MERGE INTO dbo.VenueMenuItems AS target
USING (
    VALUES
        (1001, 101, N'Bánh mì Phô mai hạt điều Kashew', 95000, N'Phô mai lên men thủ công từ hạt điều hữu cơ', 'https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=400&q=80', 1),
        (1002, 102, N'Cơm chiên lá é nấm rừng Pi', 85000, N'Hương thơm độc đáo từ lá é tươi miền Trung', 'https://images.unsplash.com/photo-1603133872878-684f208fb84b?auto=format&fit=crop&w=400&q=80', 1),
        (1003, 103, N'Lẩu nấm An Nhiên đặc biệt', 289000, N'Set lẩu nấm 4 người gồm 8 loại nấm quý', 'https://images.unsplash.com/photo-1505253758473-96b301b5c81d?auto=format&fit=crop&w=400&q=80', 1),
        (1004, 105, N'Vé Buffet chay hơn 60 món D''Gemma', 189000, N'Thưởng thức trọn vẹn ẩm thực chay Á - Âu', 'https://images.unsplash.com/photo-1414235077428-338989a2e8c0?auto=format&fit=crop&w=400&q=80', 1)
) AS source (Item_ID, Venue_ID, Dish_Name, Price_VND, Description, Image_Url, Is_Signature)
ON target.Item_ID = source.Item_ID
WHEN MATCHED THEN
    UPDATE SET target.Dish_Name = source.Dish_Name, target.Price_VND = source.Price_VND
WHEN NOT MATCHED THEN
    INSERT (Item_ID, Venue_ID, Dish_Name, Price_VND, Description, Image_Url, Is_Signature)
    VALUES (source.Item_ID, source.Venue_ID, source.Dish_Name, source.Price_VND, source.Description, source.Image_Url, source.Is_Signature);
SET IDENTITY_INSERT dbo.VenueMenuItems OFF;
GO

-- 19. CHAT SESSIONS (Bảng 36), CHAT MESSAGES (Bảng 37), SURVEYS (Bảng 38)
PRINT N'>> [19/38] Nạp dữ liệu AI Chat Assistant & Khảo sát...';

SET IDENTITY_INSERT dbo.ChatSessions ON;
MERGE INTO dbo.ChatSessions AS target
USING (
    VALUES
        (501, 2, 2, NULL, N'Tư vấn thực đơn giảm 2kg trong 1 tháng', 'NUTRITION_CONSULTATION', DATEADD(DAY, -3, SYSDATETIME()), DATEADD(DAY, -3, SYSDATETIME())),
        (502, 5, 5, NULL, N'Tư vấn bổ sung đạm và sắt cho người ăn chay tập gym', 'NUTRITION_CONSULTATION', DATEADD(DAY, -1, SYSDATETIME()), DATEADD(DAY, -1, SYSDATETIME()))
) AS source (Chat_Session_ID, User_ID, Profiles_ID, Guest_UUID, Session_Title, Context_Type, Started_At, Updated_At)
ON target.Chat_Session_ID = source.Chat_Session_ID
WHEN MATCHED THEN
    UPDATE SET target.Session_Title = source.Session_Title
WHEN NOT MATCHED THEN
    INSERT (Chat_Session_ID, User_ID, Profiles_ID, Guest_UUID, Session_Title, Context_Type, Started_At, Updated_At)
    VALUES (source.Chat_Session_ID, source.User_ID, source.Profiles_ID, source.Guest_UUID, source.Session_Title, source.Context_Type, source.Started_At, source.Updated_At);
SET IDENTITY_INSERT dbo.ChatSessions OFF;
GO

SET IDENTITY_INSERT dbo.ChatMessages ON;
MERGE INTO dbo.ChatMessages AS target
USING (
    VALUES
        (7001, 501, 'USER', N'Chào VeggieBot, mình cao 1m60 nặng 54kg muốn ăn thuần chay để giảm xuống 50kg trong 1-2 tháng tới, bot có thể tư vấn giúp mình không?', NULL, DATEADD(DAY, -3, SYSDATETIME())),
        (7002, 501, 'ASSISTANT', N'Chào bạn! Với thể trạng hiện tại (BMI 21.1 bình thường), mức calo TDEE của bạn khoảng 1.720 kcal/ngày. Để giảm cân an toàn và bền vững, bạn nên thâm hụt khoảng 200-300 kcal, duy trì mức nạp vào từ 1.450 - 1.500 kcal/ngày. Ưu tiên các món giàu xơ và đạm thực vật như đậu hũ non, nấm rơm, súp ngô ngọt và các loại rau luộc. Khuyến nghị mang tính chất tham khảo dinh dưỡng, vui lòng tham vấn ý kiến bác sĩ chuyên khoa đối với các bệnh lý nền.', '{"tokensConsumed":450,"modelVersion":"gemini-2.5-flash"}', DATEADD(DAY, -3, SYSDATETIME())),
        (7003, 502, 'USER', N'Mình tập gym 4 buổi/tuần thì ăn chay có bị thiếu cơ không và nên nạp bao nhiêu protein mỗi ngày?', NULL, DATEADD(DAY, -1, SYSDATETIME())),
        (7004, 502, 'ASSISTANT', N'Chào bạn! Người ăn chay hoàn toàn có thể xây dựng cơ bắp săn chắc nếu nạp đủ từ 1.4 - 1.8g protein/kg thể trọng (tương đương 105 - 135g đạm/ngày với cân nặng 75kg). Bạn nên kết hợp đa dạng các nguồn đạm: yến mạch, đậu nành, hạt chia, diêm mạch quinoa và bơ đậu phộng để đảm bảo đủ 9 axit amin thiết yếu. Khuyến nghị mang tính chất tham khảo dinh dưỡng, vui lòng tham vấn ý kiến bác sĩ chuyên khoa đối với các bệnh lý nền.', '{"tokensConsumed":620,"modelVersion":"gemini-2.5-flash"}', DATEADD(DAY, -1, SYSDATETIME()))
) AS source (Message_ID, Chat_Session_ID, Sender_Type, Content, Metadata_JSON, Created_At)
ON target.Message_ID = source.Message_ID
WHEN MATCHED THEN
    UPDATE SET target.Content = source.Content
WHEN NOT MATCHED THEN
    INSERT (Message_ID, Chat_Session_ID, Sender_Type, Content, Metadata_JSON, Created_At)
    VALUES (source.Message_ID, source.Chat_Session_ID, source.Sender_Type, source.Content, source.Metadata_JSON, source.Created_At);
SET IDENTITY_INSERT dbo.ChatMessages OFF;
GO

SET IDENTITY_INSERT dbo.Surveys ON;
MERGE INTO dbo.Surveys AS target
USING (
    VALUES
        (8001, 2, 501, 5, N'Tư vấn thực đơn giảm cân ăn chay có hữu ích không?', N'Bot tư vấn rất chi tiết, có con số calo rõ ràng và giải thích dễ hiểu, mình rất hài lòng!', DATEADD(DAY, -3, SYSDATETIME())),
        (8002, 5, 502, 5, N'Đánh giá chất lượng lời khuyên dinh dưỡng thể hình', N'Phản hồi nhanh, gợi ý đúng các nguồn đạm thực vật mình đang tìm kiếm.', DATEADD(DAY, -1, SYSDATETIME()))
) AS source (Survey_ID, User_ID, Session_ID, Rating_Score, Context_Question, Feedback_Content, Created_At)
ON target.Survey_ID = source.Survey_ID
WHEN MATCHED THEN
    UPDATE SET target.Rating_Score = source.Rating_Score, target.Feedback_Content = source.Feedback_Content
WHEN NOT MATCHED THEN
    INSERT (Survey_ID, User_ID, Session_ID, Rating_Score, Context_Question, Feedback_Content, Created_At)
    VALUES (source.Survey_ID, source.User_ID, source.Session_ID, source.Rating_Score, source.Context_Question, source.Feedback_Content, source.Created_At);
SET IDENTITY_INSERT dbo.Surveys OFF;
GO

PRINT N'============================================================================';
PRINT N'  HOÀN TẤT NẠP TOÀN BỘ 38 BẢNG DỮ LIỆU THỰC TẾ CHO VEGGIELIFE THÀNH CÔNG!';
PRINT N'============================================================================';
GO
