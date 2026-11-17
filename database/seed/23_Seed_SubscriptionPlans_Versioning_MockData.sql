-- ============================================================================
-- File: 23_Seed_SubscriptionPlans_Versioning_MockData.sql
-- Thư mục: database/seed/
-- Mục đích: Nạp dữ liệu mẫu thực tế kiểm thử bài toán Phân biệt quyền lợi Gói VIP:
--   1. Gói VIP 1 Tháng (Plan_ID = 2) được Admin nâng cấp từ Version 1 lên Version 2
--      (Giá tăng 49k -> 59k, Hạn mức AI tăng 200 -> 300 chat/ngày, 3 -> 5 thực đơn, thêm Video-to-Recipe STT).
--   2. Bảng SubscriptionPlanVersions ghi nhận 2 mốc phiên bản (V1 trước đó, V2 hiện hành).
--   3. Bảng PaymentTransactions thể hiện rõ 2 nhóm người dùng:
--      - Nhóm Đăng Ký TRƯỚC (Version 1): Mang Plan_Version = 1, Snapshot giá 49k, Chat 200 lượt/ngày.
--      - Nhóm Đăng Ký SAU (Version 2): Mang Plan_Version = 2, Snapshot giá 59k, Chat 300 lượt/ngày, Video-to-Recipe.
-- ============================================================================

USE [VeggieLifeDb];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'>> [Seed 23] Đang nạp dữ liệu mẫu kiểm thử Versioning & Snapshot quyền lợi gói VIP...';

DECLARE @Now DATETIME2(7) = SYSDATETIME();
DECLARE @UpdateDate DATETIME2(7) = DATEADD(DAY, -4, SYSDATETIME()); -- Admin cập nhật gói cách đây 4 ngày
DECLARE @AdminId INT = (SELECT TOP 1 User_ID FROM dbo.Users WHERE Role = 'ADMIN' ORDER BY User_ID ASC);

IF @AdminId IS NULL SET @AdminId = 1;

-- ============================================================================
-- 1. CẬP NHẬT GÓI VIP 1 THÁNG (Plan_ID = 2) LÊN VERSION 2
-- ============================================================================

DECLARE @FeaturesV2 NVARCHAR(MAX) = N'{"chatLimitDaily":300,"mealPlanLimitWeekly":5,"fridgeScanLimitDaily":50,"exclusiveBadge":true,"adFree":true,"priorityAiResponse":true,"vipDietitianAi":true,"videoToRecipeStt":true,"description":"Gói VIP 1 Tháng nâng cấp Version 2: Tăng lên 300 lượt AI chat/ngày, 5 thực đơn tuần và mở khóa tính năng tách công thức từ video nấu ăn (Video-to-Recipe STT)."}';

-- Cập nhật bảng SubscriptionPlans
UPDATE dbo.SubscriptionPlans
SET 
    [Version] = 2,
    [Price] = 59000.00,
    [Features_JSON] = @FeaturesV2,
    [Effective_From] = @UpdateDate,
    [Updated_At] = @UpdateDate
WHERE [Plan_ID] = 2;

-- Cập nhật Effective_To cho Version 1 trong bảng SubscriptionPlanVersions
UPDATE dbo.SubscriptionPlanVersions
SET [Effective_To] = @UpdateDate
WHERE [Plan_ID] = 2 AND [Version] = 1;

-- Chèn bản ghi Version 2 vào SubscriptionPlanVersions nếu chưa có
IF NOT EXISTS (
    SELECT 1 FROM dbo.SubscriptionPlanVersions
    WHERE [Plan_ID] = 2 AND [Version] = 2
)
BEGIN
    INSERT INTO dbo.SubscriptionPlanVersions (
        [Plan_ID], [Version], [Plan_Name], [Price], [Duration_Days], [Features_JSON],
        [Effective_From], [Effective_To], [Updated_By_Admin_ID], [Change_Reason], [Created_At]
    )
    VALUES (
        2,
        2,
        N'Gói VIP 1 Tháng - Mở Khóa AI Dinh Dưỡng',
        59000.00,
        30,
        @FeaturesV2,
        @UpdateDate,
        NULL,
        @AdminId,
        N'Nâng cấp hạn mức AI lên 300 chat/ngày, 5 thực đơn tuần và mở khóa đặc quyền Video-to-Recipe STT',
        @UpdateDate
    );

    PRINT N'   -> Đã tạo phiên bản Version 2 cho Gói VIP 1 Tháng trong SubscriptionPlanVersions.';
END;

-- ============================================================================
-- 2. ĐẢM BẢO CÁC GIAO DỊCH CŨ (TRƯỚC MỐC CẬP NHẬT) MANG ĐẦY ĐỦ SNAPSHOT V1
-- ============================================================================

UPDATE pt
SET 
    pt.[Plan_Version] = 1,
    pt.[Snapshot_Plan_Name] = sp.[Plan_Name],
    pt.[Snapshot_Price] = 49000.00,
    pt.[Snapshot_Duration_Days] = 30,
    pt.[Snapshot_Features_JSON] = N'{"chatLimitDaily":200,"mealPlanLimitWeekly":3,"fridgeScanLimitDaily":30,"exclusiveBadge":true,"adFree":true,"priorityAiResponse":true,"description":"Gói trải nghiệm linh hoạt 30 ngày mở khóa toàn diện các tiện ích AI dinh dưỡng (Version 1 cam kết đúng biên lai)."}'
FROM dbo.PaymentTransactions pt
INNER JOIN dbo.SubscriptionPlans sp ON pt.[Plan_ID] = sp.[Plan_ID]
WHERE pt.[Plan_ID] = 2 AND pt.[Created_At] < @UpdateDate;

-- ============================================================================
-- 3. THÊM CÁC GIAO DỊCH MỚI CỦA NGƯỜI ĐĂNG KÝ SAU KHI GÓI ĐÃ CẬP NHẬT (VERSION 2)
-- ============================================================================

-- Giao dịch 36: Thành viên Hoàng Kim (User 3) gia hạn mua gói mới (Version 2)
IF NOT EXISTS (SELECT 1 FROM dbo.PaymentTransactions WHERE Partner_Transaction_Code = 'VIETQR_VIP_20261115_036')
BEGIN
    INSERT INTO dbo.PaymentTransactions (
        [Plan_ID], [User_ID], [Gateway_Provider], [Partner_Transaction_Code],
        [Amount_VND], [Transaction_Status], [Signature_Hash],
        [Created_At], [Completed_At], [Receipt_URL],
        [Starts_At], [Ends_At],
        [Plan_Version], [Snapshot_Plan_Name], [Snapshot_Price], [Snapshot_Duration_Days], [Snapshot_Features_JSON]
    )
    VALUES (
        2,
        3, -- User 3
        'VIETQR',
        'VIETQR_VIP_20261115_036',
        59000.00,
        'SUCCESS',
        'sig_hash_vietqr_036_v2',
        DATEADD(DAY, -2, SYSDATETIME()),
        DATEADD(DAY, -2, SYSDATETIME()),
        'https://receipts.veggielife.vn/vietqr/036_v2.pdf',
        DATEADD(DAY, -2, SYSDATETIME()),
        DATEADD(DAY, 28, SYSDATETIME()),
        2, -- Plan_Version 2
        N'Gói VIP 1 Tháng - Mở Khóa AI Dinh Dưỡng',
        59000.00,
        30,
        @FeaturesV2
    );

    PRINT N'   -> Đã chèn giao dịch mẫu #36 mua gói Version 2 (User 3 - Master Chef Hoàng Kim).';
END;

-- Giao dịch 37: Thành viên Phạm Đình (User 10) đăng ký mới gói Version 2 qua MoMo
IF NOT EXISTS (SELECT 1 FROM dbo.PaymentTransactions WHERE Partner_Transaction_Code = 'MOMO_VIP_20261116_037')
BEGIN
    INSERT INTO dbo.PaymentTransactions (
        [Plan_ID], [User_ID], [Gateway_Provider], [Partner_Transaction_Code],
        [Amount_VND], [Transaction_Status], [Signature_Hash],
        [Created_At], [Completed_At], [Receipt_URL],
        [Starts_At], [Ends_At],
        [Plan_Version], [Snapshot_Plan_Name], [Snapshot_Price], [Snapshot_Duration_Days], [Snapshot_Features_JSON]
    )
    VALUES (
        2,
        10, -- User 10
        'MOMO',
        'MOMO_VIP_20261116_037',
        59000.00,
        'SUCCESS',
        'sig_hash_momo_037_v2',
        DATEADD(DAY, -1, SYSDATETIME()),
        DATEADD(DAY, -1, SYSDATETIME()),
        'https://receipts.veggielife.vn/momo/037_v2.pdf',
        DATEADD(DAY, -1, SYSDATETIME()),
        DATEADD(DAY, 29, SYSDATETIME()),
        2, -- Plan_Version 2
        N'Gói VIP 1 Tháng - Mở Khóa AI Dinh Dưỡng',
        59000.00,
        30,
        @FeaturesV2
    );

    PRINT N'   -> Đã chèn giao dịch mẫu #37 mua gói Version 2 (User 10 - Phạm Đình Sống Xanh).';
END;

PRINT N'>> Hoàn tất nạp dữ liệu mẫu Versioning & Snapshot quyền lợi gói VIP thành công!';
GO
