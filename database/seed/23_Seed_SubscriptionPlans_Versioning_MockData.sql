-- ============================================================================
-- File: 23_Seed_SubscriptionPlans_Versioning_MockData.sql
-- Thư mục: database/seed/
-- Mục đích: Kịch bản giả lập Admin nâng cấp hạn mức VIP toàn hệ thống từ 200 lên 300 câu/ngày:
--   1. Admin nâng cấp cấu hình hệ thống (SystemSettings.CHAT_LIMIT_DAILY_VIP) từ 200 -> 300 câu/ngày.
--   2. Cả 3 Gói VIP (1 Tháng, 3 Tháng, 1 Năm) đều đồng loạt nâng lên Version 2 với hạn mức mới 300 câu/ngày.
--   3. Bảng [AuditLogs] lưu vết lịch sử Admin nâng cấp cấu hình và các gói VIP theo chuẩn BR-ADM-SYS-002.
--   4. Minh chứng tính năng Bảo lưu cam kết (Grandfathering):
--      - Nhóm mua TRƯỚC mốc nâng cấp: Vẫn được bảo lưu đúng hạn mức 200 câu/ngày trong Snapshot.
--      - Nhóm mua SAU mốc nâng cấp (Đơn #36, #37 và người dùng nạp mới): Nhận đúng hạn mức mới 300 câu/ngày.
-- ============================================================================

USE [VeggieLifeDb];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'>> [Seed 23] Đang nạp kịch bản giả lập Admin nâng cấp VIP toàn hệ thống (200 -> 300 câu/ngày)...';

DECLARE @Now DATETIME2(7) = SYSDATETIME();
DECLARE @UpdateDate DATETIME2(7) = DATEADD(DAY, -4, SYSDATETIME()); -- Mốc Admin sửa cách đây 4 ngày
DECLARE @AdminId INT = (SELECT TOP 1 User_ID FROM dbo.Users WHERE Role = 'ADMIN' ORDER BY User_ID ASC);
IF @AdminId IS NULL SET @AdminId = 1;

-- Định nghĩa JSON quyền lợi Version 1 (Cũ: 200 câu/ngày) và Version 2 (Mới: 300 câu/ngày)
DECLARE @FeaturesV1 NVARCHAR(MAX) = N'{"chatLimitDaily":200,"mealPlanLimitWeekly":3,"fridgeScanLimitDaily":30,"exclusiveBadge":true,"adFree":true,"priorityAiResponse":true,"vipDietitianAi":true,"videoToRecipeStt":true,"description":"Gói hội viên VIP Version 1 (Cam kết 200 lượt AI chat/ngày đúng biên lai lúc mua)."}';
DECLARE @FeaturesV2 NVARCHAR(MAX) = N'{"chatLimitDaily":300,"mealPlanLimitWeekly":3,"fridgeScanLimitDaily":30,"exclusiveBadge":true,"adFree":true,"priorityAiResponse":true,"vipDietitianAi":true,"videoToRecipeStt":true,"description":"Gói hội viên VIP Version 2: Tăng cường hạn mức lên 300 lượt AI chat/ngày cho toàn bộ thành viên VIP VeggieLife."}';

-- ============================================================================
-- 1. ADMIN NÂNG CẤP CẤU HÌNH HỆ THỐNG TRONG BẢNG SystemSettings (200 -> 300)
-- ============================================================================
UPDATE dbo.SystemSettings
SET 
    [Setting_Value] = N'300',
    [Updated_At] = @UpdateDate,
    [Updated_By] = @AdminId
WHERE [Setting_Key] = 'CHAT_LIMIT_DAILY_VIP';

-- Ghi vết AuditLogs cho SystemSettings
IF NOT EXISTS (
    SELECT 1 FROM dbo.AuditLogs 
    WHERE Target_Table = 'SystemSettings' 
      AND Target_ID = 'CHAT_LIMIT_DAILY_VIP'
)
BEGIN
    INSERT INTO dbo.AuditLogs (
        [Actor_User_ID], [Actor_Role], [Action], [Target_Table], [Target_ID],
        [Old_Value], [New_Value], [Note], [Created_At]
    )
    VALUES (
        @AdminId, 'ADMIN', 'UPDATE_SYSTEM_SETTING', 'SystemSettings', 'CHAT_LIMIT_DAILY_VIP',
        N'{"settingKey":"CHAT_LIMIT_DAILY_VIP","settingValue":"200"}',
        N'{"settingKey":"CHAT_LIMIT_DAILY_VIP","settingValue":"300"}',
        N'Admin nâng cấp hạn mức Chat AI của tài khoản VIP toàn hệ thống từ 200 lên 300 câu/ngày',
        @UpdateDate
    );
END;

PRINT N'   -> Đã cập nhật SystemSettings: CHAT_LIMIT_DAILY_VIP = 300 câu/ngày.';

-- ============================================================================
-- 2. CẢ 3 GÓI VIP ĐỀU ĐƯỢC NÂNG LÊN VERSION 2 VỚI HẠN MỨC 300 CÂU/NGÀY
-- ============================================================================

-- Gói 1: VIP 1 Năm (Plan_ID = 1, 449k, 365 ngày)
UPDATE dbo.SubscriptionPlans
SET [Version] = 2, [Features_JSON] = @FeaturesV2, [Updated_At] = @UpdateDate
WHERE [Plan_ID] = 1;

-- Gói 2: VIP 1 Tháng (Plan_ID = 2, 49k, 30 ngày)
UPDATE dbo.SubscriptionPlans
SET [Version] = 2, [Features_JSON] = @FeaturesV2, [Updated_At] = @UpdateDate
WHERE [Plan_ID] = 2;

-- Gói 3: VIP 3 Tháng (Plan_ID = 3, 129k, 90 ngày)
UPDATE dbo.SubscriptionPlans
SET [Version] = 2, [Features_JSON] = @FeaturesV2, [Updated_At] = @UpdateDate
WHERE [Plan_ID] = 3;

-- Ghi vết AuditLogs nâng cấp cả 3 gói VIP lên Version 2
IF NOT EXISTS (SELECT 1 FROM dbo.AuditLogs WHERE Target_Table = 'SubscriptionPlans' AND Target_ID = '2' AND Action = 'UPDATE_VIP_PRICE')
BEGIN
    INSERT INTO dbo.AuditLogs ([Actor_User_ID], [Actor_Role], [Action], [Target_Table], [Target_ID], [Old_Value], [New_Value], [Note], [Created_At])
    VALUES (
        @AdminId, 'ADMIN', 'UPDATE_VIP_PRICE', 'SubscriptionPlans', '2',
        N'{"version":1,"planName":"Gói VIP 1 Tháng - Mở Khóa AI Dinh Dưỡng","price":49000.00,"durationDays":30,"features":' + @FeaturesV1 + N'}',
        N'{"version":2,"planName":"Gói VIP 1 Tháng - Mở Khóa AI Dinh Dưỡng","price":49000.00,"durationDays":30,"features":' + @FeaturesV2 + N'}',
        N'Admin nâng cấp gói VIP 1 Tháng lên Version 2: Tăng hạn mức AI chat từ 200 lên 300 câu/ngày',
        @UpdateDate
    );
END;

PRINT N'   -> Đã nâng toàn bộ 3 gói VIP lên Version 2 với hạn mức mới (300 câu/ngày).';

-- ============================================================================
-- 3. BẢO TOÀN QUYỀN LỢI CHO NHÓM ĐĂNG KÝ TRƯỚC (VERSION 1: 200 CÂU/NGÀY)
-- ============================================================================
UPDATE pt
SET 
    pt.[Snapshot_Plan_Name] = sp.[Plan_Name],
    pt.[Snapshot_Features_JSON] = @FeaturesV1
FROM dbo.PaymentTransactions pt
JOIN dbo.SubscriptionPlans sp ON pt.[Plan_ID] = sp.[Plan_ID]
WHERE pt.[Created_At] < @UpdateDate;

PRINT N'   -> Đã đóng băng quyền lợi Version 1 (200 câu/ngày) cho tất cả các giao dịch mua trước ngày cập nhật.';

-- ============================================================================
-- 4. NHÓM ĐĂNG KÝ SAU KHI CẬP NHẬT (VERSION 2: 300 CÂU/NGÀY)
-- ============================================================================

-- Giao dịch #36: Thành viên Hoàng Kim (User 3) nạp qua VietQR sau ngày cập nhật
IF NOT EXISTS (SELECT 1 FROM dbo.PaymentTransactions WHERE Partner_Transaction_Code = 'VIETQR_VIP_20261115_036')
BEGIN
    INSERT INTO dbo.PaymentTransactions (
        [Plan_ID], [User_ID], [Gateway_Provider], [Partner_Transaction_Code],
        [Amount_VND], [Transaction_Status], [Signature_Hash],
        [Created_At], [Completed_At], [Receipt_URL],
        [Starts_At], [Ends_At],
        [Snapshot_Plan_Name], [Snapshot_Features_JSON]
    )
    VALUES (
        2, 3, 'VIETQR', 'VIETQR_VIP_20261115_036',
        49000.00, 'SUCCESS', 'sig_hash_vietqr_036_v2',
        DATEADD(DAY, -2, SYSDATETIME()), DATEADD(DAY, -2, SYSDATETIME()),
        'https://receipts.veggielife.vn/vietqr/036.pdf',
        DATEADD(DAY, -2, SYSDATETIME()), DATEADD(DAY, 28, SYSDATETIME()),
        N'Gói VIP 1 Tháng - Mở Khóa AI Dinh Dưỡng', @FeaturesV2
    );
END
ELSE
BEGIN
    UPDATE dbo.PaymentTransactions
    SET [Amount_VND] = 49000.00,
        [Snapshot_Features_JSON] = @FeaturesV2
    WHERE Partner_Transaction_Code = 'VIETQR_VIP_20261115_036';
END;

-- Giao dịch #37: Thành viên Phạm Đình (User 10) nạp qua MoMo sau ngày cập nhật
IF NOT EXISTS (SELECT 1 FROM dbo.PaymentTransactions WHERE Partner_Transaction_Code = 'MOMO_VIP_20261116_037')
BEGIN
    INSERT INTO dbo.PaymentTransactions (
        [Plan_ID], [User_ID], [Gateway_Provider], [Partner_Transaction_Code],
        [Amount_VND], [Transaction_Status], [Signature_Hash],
        [Created_At], [Completed_At], [Receipt_URL],
        [Starts_At], [Ends_At],
        [Snapshot_Plan_Name], [Snapshot_Features_JSON]
    )
    VALUES (
        2, 10, 'MOMO', 'MOMO_VIP_20261116_037',
        49000.00, 'SUCCESS', 'sig_hash_momo_037_v2',
        DATEADD(DAY, -1, SYSDATETIME()), DATEADD(DAY, -1, SYSDATETIME()),
        'https://receipts.veggielife.vn/momo/037.pdf',
        DATEADD(DAY, -1, SYSDATETIME()), DATEADD(DAY, 29, SYSDATETIME()),
        N'Gói VIP 1 Tháng - Mở Khóa AI Dinh Dưỡng', @FeaturesV2
    );
END
ELSE
BEGIN
    UPDATE dbo.PaymentTransactions
    SET [Amount_VND] = 49000.00,
        [Snapshot_Features_JSON] = @FeaturesV2
    WHERE Partner_Transaction_Code = 'MOMO_VIP_20261116_037';
END;

PRINT N'>> Hoàn tất nạp kịch bản: Hệ thống nâng cấp 300 câu/ngày, bảo lưu trọn vẹn 200 câu cho khách cũ!';
GO
