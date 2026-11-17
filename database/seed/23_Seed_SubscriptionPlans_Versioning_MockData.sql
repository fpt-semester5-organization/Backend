-- ============================================================================
-- File: 23_Seed_SubscriptionPlans_Versioning_MockData.sql
-- Thư mục: database/seed/
-- Mục đích: Đồng bộ quyền lợi các Gói VIP bám sát 100% theo SystemSettings:
--   1. Hệ thống sử dụng SystemSettings làm nguồn cấu hình động toàn cục:
--      - Hạn mức Chat AI VIP: SystemSettings.CHAT_LIMIT_DAILY_VIP (Mặc định: 200 câu/ngày)
--      - Hạn mức Thực đơn VIP: SystemSettings.MEAL_PLAN_WEEKLY_LIMIT_VIP (Mặc định: 3 thực đơn/tuần)
--      - Hạn mức Quét tủ lạnh VIP: SystemSettings.FRIDGE_SCAN_LIMIT_DAILY_VIP (Mặc định: 30 lượt/ngày)
--   2. Toàn bộ các gói VIP (1 Tháng, 3 Tháng, 1 Năm) mang quyền lợi đồng nhất theo SystemSettings,
--      chỉ khác nhau về Thời hạn (30, 90, 365 ngày) và Giá tiền (49k, 129k, 449k).
--   3. Tất cả các gói ở Version 1 chuẩn mực.
--   4. Đóng băng quyền lợi chuẩn này vào Snapshot_Features_JSON của các giao dịch thanh toán.
-- ============================================================================

USE [VeggieLifeDb];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'>> [Seed 23] Đang đồng bộ chuẩn quyền lợi các Gói VIP theo SystemSettings (200 câu/ngày)...';

-- Định nghĩa JSON quyền lợi VIP chuẩn mực khớp 100% với SystemSettings
DECLARE @StandardVipFeatures NVARCHAR(MAX) = N'{"chatLimitDaily":200,"mealPlanLimitWeekly":3,"fridgeScanLimitDaily":30,"exclusiveBadge":true,"adFree":true,"priorityAiResponse":true,"vipDietitianAi":true,"videoToRecipeStt":true,"description":"Mở khóa toàn bộ đặc quyền hội viên VIP VeggieLife theo cấu hình hệ thống."}';

-- ============================================================================
-- 1. ĐỒNG BỘ CẢ 3 GÓI VIP TRONG BẢNG SubscriptionPlans VỀ CHUẨN 200 CÂU/NGÀY
-- ============================================================================

-- Gói 1: VIP 1 Năm (Plan_ID = 1, 449k, 365 ngày)
UPDATE dbo.SubscriptionPlans
SET 
    [Version] = 1,
    [Price] = 449000.00,
    [Duration_Days] = 365,
    [Features_JSON] = @StandardVipFeatures,
    [Is_Active] = 1,
    [Updated_At] = GETUTCDATE()
WHERE [Plan_ID] = 1;

-- Gói 2: VIP 1 Tháng (Plan_ID = 2, 49k, 30 ngày)
UPDATE dbo.SubscriptionPlans
SET 
    [Version] = 1,
    [Price] = 49000.00,
    [Duration_Days] = 30,
    [Features_JSON] = @StandardVipFeatures,
    [Is_Active] = 1,
    [Updated_At] = GETUTCDATE()
WHERE [Plan_ID] = 2;

-- Gói 3: VIP 3 Tháng (Plan_ID = 3, 129k, 90 ngày)
UPDATE dbo.SubscriptionPlans
SET 
    [Version] = 1,
    [Price] = 129000.00,
    [Duration_Days] = 90,
    [Features_JSON] = @StandardVipFeatures,
    [Is_Active] = 1,
    [Updated_At] = GETUTCDATE()
WHERE [Plan_ID] = 3;

PRINT N'   -> Đã đồng bộ 3 gói VIP về Version 1 với hạn mức chuẩn (200 chat/ngày, 3 thực đơn, 30 tủ lạnh).';

-- ============================================================================
-- 2. ĐÓNG BĂNG QUYỀN LỢI CHUẨN VÀO BẢNG PaymentTransactions
-- ============================================================================
UPDATE pt
SET 
    pt.[Snapshot_Plan_Name] = sp.[Plan_Name],
    pt.[Snapshot_Features_JSON] = @StandardVipFeatures
FROM dbo.PaymentTransactions pt
JOIN dbo.SubscriptionPlans sp ON pt.[Plan_ID] = sp.[Plan_ID];

PRINT N'   -> Đã đóng băng quyền lợi chuẩn (200 chat/ngày) cho toàn bộ các giao dịch thanh toán.';

-- ============================================================================
-- 3. ĐỒNG BỘ 2 GIAO DỊCH MẪU #36 VÀ #37 VỀ CHUẨN 200 CÂU/NGÀY
-- ============================================================================
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
        49000.00, 'SUCCESS', 'sig_hash_vietqr_036_v1',
        DATEADD(DAY, -2, SYSDATETIME()), DATEADD(DAY, -2, SYSDATETIME()),
        'https://receipts.veggielife.vn/vietqr/036.pdf',
        DATEADD(DAY, -2, SYSDATETIME()), DATEADD(DAY, 28, SYSDATETIME()),
        N'Gói VIP 1 Tháng - Mở Khóa AI Dinh Dưỡng', @StandardVipFeatures
    );
END
ELSE
BEGIN
    UPDATE dbo.PaymentTransactions
    SET [Amount_VND] = 49000.00,
        [Snapshot_Features_JSON] = @StandardVipFeatures
    WHERE Partner_Transaction_Code = 'VIETQR_VIP_20261115_036';
END;

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
        49000.00, 'SUCCESS', 'sig_hash_momo_037_v1',
        DATEADD(DAY, -1, SYSDATETIME()), DATEADD(DAY, -1, SYSDATETIME()),
        'https://receipts.veggielife.vn/momo/037.pdf',
        DATEADD(DAY, -1, SYSDATETIME()), DATEADD(DAY, 29, SYSDATETIME()),
        N'Gói VIP 1 Tháng - Mở Khóa AI Dinh Dưỡng', @StandardVipFeatures
    );
END
ELSE
BEGIN
    UPDATE dbo.PaymentTransactions
    SET [Amount_VND] = 49000.00,
        [Snapshot_Features_JSON] = @StandardVipFeatures
    WHERE Partner_Transaction_Code = 'MOMO_VIP_20261116_037';
END;

PRINT N'>> Hoàn tất đồng bộ: Toàn bộ hệ thống tuân thủ 100% SystemSettings (200 câu/ngày)!';
GO
