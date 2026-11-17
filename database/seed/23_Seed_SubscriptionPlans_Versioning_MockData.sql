-- ============================================================================
-- File: 23_Seed_SubscriptionPlans_Versioning_MockData.sql
-- Thư mục: database/seed/
-- Mục đích: Đồng bộ quyền lợi chuẩn cho toàn bộ Gói VIP và nạp dữ liệu Snapshot:
--   1. Hệ thống chỉ có 1 Role VIP duy nhất: Toàn bộ gói VIP (1 Tháng, 3 Tháng, 1 Năm)
--      đều mang CHUNG bộ quyền lợi chuẩn theo cấu hình hệ thống:
--      - Hạn mức Chat AI dinh dưỡng: 200 câu/ngày
--      - Hạn mức Tạo thực đơn tuần: 3 thực đơn/tuần
--      - Hạn mức Quét tủ lạnh AI: 30 lượt/ngày
--      - Đặc quyền: Không quảng cáo, Huy hiệu VIP, Ưu tiên AI, Video-to-Recipe STT.
--   2. Các gói chỉ khác nhau về Thời hạn (30, 90, 365 ngày) và Giá tiền (49k, 129k, 449k).
--   3. Tất cả các gói được đặt ở Version = 1 chuẩn mực.
--   4. Bảng [PaymentTransactions] đóng băng trọn vẹn bộ quyền lợi chuẩn này vào Snapshot_Features_JSON.
-- ============================================================================

USE [VeggieLifeDb];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'>> [Seed 23] Đang đồng bộ chuẩn quyền lợi các Gói VIP và dữ liệu Snapshot...';

-- Định nghĩa JSON quyền lợi VIP chuẩn mực theo cấu hình hệ thống
DECLARE @StandardVipFeatures NVARCHAR(MAX) = N'{"chatLimitDaily":200,"mealPlanLimitWeekly":3,"fridgeScanLimitDaily":30,"exclusiveBadge":true,"adFree":true,"priorityAiResponse":true,"vipDietitianAi":true,"videoToRecipeStt":true,"description":"Mở khóa toàn bộ đặc quyền hội viên VIP VeggieLife."}';

-- ============================================================================
-- 1. ĐỒNG BỘ 3 GÓI VIP TRONG BẢNG SubscriptionPlans VỀ VERSION 1 & 200 CHAT/NGÀY
-- ============================================================================

-- Gói 1: 1 Năm (Plan_ID = 1, Giá 449k, 365 ngày)
UPDATE dbo.SubscriptionPlans
SET 
    [Version] = 1,
    [Price] = 449000.00,
    [Duration_Days] = 365,
    [Features_JSON] = @StandardVipFeatures,
    [Is_Active] = 1,
    [Updated_At] = GETUTCDATE()
WHERE [Plan_ID] = 1;

-- Gói 2: 1 Tháng (Plan_ID = 2, Giá 49k, 30 ngày)
UPDATE dbo.SubscriptionPlans
SET 
    [Version] = 1,
    [Price] = 49000.00,
    [Duration_Days] = 30,
    [Features_JSON] = @StandardVipFeatures,
    [Is_Active] = 1,
    [Updated_At] = GETUTCDATE()
WHERE [Plan_ID] = 2;

-- Gói 3: 3 Tháng (Plan_ID = 3, Giá 129k, 90 ngày)
UPDATE dbo.SubscriptionPlans
SET 
    [Version] = 1,
    [Price] = 129000.00,
    [Duration_Days] = 90,
    [Features_JSON] = @StandardVipFeatures,
    [Is_Active] = 1,
    [Updated_At] = GETUTCDATE()
WHERE [Plan_ID] = 3;

PRINT N'   -> Đã đồng bộ cả 3 gói VIP về Version 1 với hạn mức chuẩn (200 chat/ngày, 3 thực đơn/tuần, 30 quét tủ lạnh).';

-- ============================================================================
-- 2. ĐÓNG BĂNG QUYỀN LỢI CHUẨN VÀO TOÀN BỘ GIAO DỊCH TRONG PaymentTransactions
-- ============================================================================
UPDATE pt
SET 
    pt.[Snapshot_Plan_Name] = sp.[Plan_Name],
    pt.[Snapshot_Features_JSON] = @StandardVipFeatures
FROM dbo.PaymentTransactions pt
JOIN dbo.SubscriptionPlans sp ON pt.[Plan_ID] = sp.[Plan_ID]
WHERE pt.[Snapshot_Features_JSON] IS NULL 
   OR pt.[Snapshot_Features_JSON] LIKE '%"chatLimitDaily":300%';

PRINT N'   -> Đã đóng băng quyền lợi chuẩn (200 chat/ngày) cho toàn bộ các giao dịch thanh toán.';

-- ============================================================================
-- 3. ĐẢM BẢO 2 GIAO DỊCH MẪU #36 VÀ #37 ĐỒNG NHẤT QUYỀN LỢI CHUẨN
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
        2,
        3, -- User 3 (Master Chef Hoàng Kim)
        'VIETQR',
        'VIETQR_VIP_20261115_036',
        49000.00,
        'SUCCESS',
        'sig_hash_vietqr_036_v1',
        DATEADD(DAY, -2, SYSDATETIME()),
        DATEADD(DAY, -2, SYSDATETIME()),
        'https://receipts.veggielife.vn/vietqr/036.pdf',
        DATEADD(DAY, -2, SYSDATETIME()),
        DATEADD(DAY, 28, SYSDATETIME()),
        N'Gói VIP 1 Tháng - Mở Khóa AI Dinh Dưỡng',
        @StandardVipFeatures
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
        2,
        10, -- User 10 (Phạm Đình Sống Xanh)
        'MOMO',
        'MOMO_VIP_20261116_037',
        49000.00,
        'SUCCESS',
        'sig_hash_momo_037_v1',
        DATEADD(DAY, -1, SYSDATETIME()),
        DATEADD(DAY, -1, SYSDATETIME()),
        'https://receipts.veggielife.vn/momo/037.pdf',
        DATEADD(DAY, -1, SYSDATETIME()),
        DATEADD(DAY, 29, SYSDATETIME()),
        N'Gói VIP 1 Tháng - Mở Khóa AI Dinh Dưỡng',
        @StandardVipFeatures
    );
END
ELSE
BEGIN
    UPDATE dbo.PaymentTransactions
    SET [Amount_VND] = 49000.00,
        [Snapshot_Features_JSON] = @StandardVipFeatures
    WHERE Partner_Transaction_Code = 'MOMO_VIP_20261116_037';
END;

PRINT N'>> Hoàn tất đồng bộ dữ liệu mẫu: Cả 3 gói VIP đều đồng nhất 200 chat/ngày theo cấu hình hệ thống!';
GO
