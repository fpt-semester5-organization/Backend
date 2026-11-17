-- ============================================================================
-- File: 23_Seed_SubscriptionPlans_Versioning_MockData.sql
-- Thư mục: database/seed/
-- Mục đích: Nạp dữ liệu mẫu kiểm thử bài toán Phân biệt quyền lợi Gói VIP:
--   1. Gói VIP 1 Tháng (Plan_ID = 1) được Admin nâng cấp từ Version 1 lên Version 2
--      (Giá tăng 49k -> 59k, Hạn mức AI tăng 200 -> 300 chat/ngày, 3 -> 5 thực đơn, thêm Video-to-Recipe STT).
--   2. Bảng [AuditLogs] có sẵn ghi vết toàn bộ lịch sử thay đổi của Admin (OldValue, NewValue, Note, CreatedAt).
--   3. Bảng [PaymentTransactions] thể hiện rõ 2 nhóm người dùng:
--      - Nhóm Đăng Ký TRƯỚC (trước ngày cập nhật): Snapshot quyền lợi Version 1 (200 chat/ngày, giá 49k).
--      - Nhóm Đăng Ký SAU (sau ngày cập nhật): Snapshot quyền lợi Version 2 (300 chat/ngày, giá 59k, Video-to-Recipe STT).
--   4. KHÔNG SỬ DỤNG BẢNG MỚI - Giữ trọn vẹn mô hình dữ liệu Conceptual & Logical.
-- ============================================================================

USE [VeggieLifeDb];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'>> [Seed 23] Đang nạp dữ liệu mẫu kiểm thử Phân biệt quyền lợi VIP (Tận dụng AuditLogs & Snapshot)...';

DECLARE @Now DATETIME2(7) = SYSDATETIME();
DECLARE @UpdateDate DATETIME2(7) = DATEADD(DAY, -4, SYSDATETIME()); -- Mốc Admin sửa gói cách đây 4 ngày
DECLARE @AdminId INT = (SELECT TOP 1 User_ID FROM dbo.Users WHERE Role = 'ADMIN' ORDER BY User_ID ASC);

IF @AdminId IS NULL SET @AdminId = 1;

-- Lấy thông tin Gói VIP 1 Tháng
DECLARE @PlanId INT = (SELECT TOP 1 Plan_ID FROM dbo.SubscriptionPlans WHERE Plan_Name LIKE N'%1 Tháng%' ORDER BY Plan_ID ASC);
IF @PlanId IS NULL SET @PlanId = 1;

-- Định nghĩa JSON quyền lợi Version 1 và Version 2
DECLARE @FeaturesV1 NVARCHAR(MAX) = N'{"chatLimitDaily":200,"mealPlanLimitWeekly":3,"fridgeScanLimitDaily":30,"exclusiveBadge":true,"adFree":true,"priorityAiResponse":true,"description":"Gói trải nghiệm linh hoạt 30 ngày (Version 1 cam kết đúng biên lai lúc mua)."}';
DECLARE @FeaturesV2 NVARCHAR(MAX) = N'{"chatLimitDaily":300,"mealPlanLimitWeekly":5,"fridgeScanLimitDaily":50,"exclusiveBadge":true,"adFree":true,"priorityAiResponse":true,"vipDietitianAi":true,"videoToRecipeStt":true,"description":"Gói VIP 1 Tháng nâng cấp Version 2: Tăng lên 300 lượt AI chat/ngày, 5 thực đơn tuần và mở khóa tính năng tách công thức từ video nấu ăn (Video-to-Recipe STT)."}';

-- ============================================================================
-- 1. CẬP NHẬT GÓI VIP 1 THÁNG LÊN VERSION 2 TRONG BẢNG SubscriptionPlans
-- ============================================================================
UPDATE dbo.SubscriptionPlans
SET 
    [Version] = 2,
    [Price] = 59000.00,
    [Features_JSON] = @FeaturesV2,
    [Updated_At] = @UpdateDate
WHERE [Plan_ID] = @PlanId;

PRINT N'   -> Đã cập nhật Gói VIP 1 Tháng lên Version 2 (Giá 59.000đ, 300 chat/ngày, Video-to-Recipe).';

-- ============================================================================
-- 2. GHI VẾT LỊCH SỬ THAY ĐỔI CỦA ADMIN VÀO BẢNG AuditLogs (CÓ SẴN)
-- ============================================================================
IF NOT EXISTS (
    SELECT 1 FROM dbo.AuditLogs 
    WHERE Target_Table = 'SubscriptionPlans' 
      AND Target_ID = CAST(@PlanId AS NVARCHAR(50))
      AND Action = 'UPDATE_VIP_PLAN'
)
BEGIN
    INSERT INTO dbo.AuditLogs (
        [Actor_User_ID], [Actor_Role], [Action], [Target_Table], [Target_ID],
        [Old_Value], [New_Value], [Note], [Created_At]
    )
    VALUES (
        @AdminId,
        'ADMIN',
        'UPDATE_VIP_PLAN',
        'SubscriptionPlans',
        CAST(@PlanId AS NVARCHAR(50)),
        N'{"version":1,"planName":"Gói VIP 1 Tháng - Mở Khóa AI Dinh Dưỡng","price":49000.00,"durationDays":30,"features":' + @FeaturesV1 + N'}',
        N'{"version":2,"planName":"Gói VIP 1 Tháng - Mở Khóa AI Dinh Dưỡng","price":59000.00,"durationDays":30,"features":' + @FeaturesV2 + N'}',
        N'Admin nâng cấp gói VIP 1 Tháng lên Version 2: Tăng giá 49k -> 59k, hạn mức AI chat 200 -> 300/ngày, 3 -> 5 thực đơn tuần và mở khóa Video-to-Recipe STT',
        @UpdateDate
    );

    PRINT N'   -> Đã ghi vết lịch sử nâng cấp gói vào bảng dbo.AuditLogs.';
END;

-- ============================================================================
-- 3. BẢO TOÀN QUYỀN LỢI CHO NHÓM ĐĂNG KÝ TRƯỚC (VERSION 1)
-- ============================================================================
-- Cập nhật Snapshot quyền lợi Version 1 cho toàn bộ giao dịch trước mốc @UpdateDate
UPDATE pt
SET 
    pt.[Snapshot_Plan_Name] = N'Gói VIP 1 Tháng - Mở Khóa AI Dinh Dưỡng',
    pt.[Snapshot_Features_JSON] = @FeaturesV1
FROM dbo.PaymentTransactions pt
WHERE pt.[Plan_ID] = @PlanId 
  AND pt.[Created_At] < @UpdateDate;

PRINT N'   -> Đã đóng băng quyền lợi Version 1 (200 chat/ngày, 49k) cho người mua trước ngày cập nhật.';

-- ============================================================================
-- 4. THÊM GIAO DỊCH CỦA NHÓM ĐĂNG KÝ SAU KHI CẬP NHẬT GÓI (VERSION 2)
-- ============================================================================

-- Giao dịch #36: Thành viên Hoàng Kim (User 3) đăng ký mua gói mới (Version 2)
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
        @PlanId,
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
        N'Gói VIP 1 Tháng - Mở Khóa AI Dinh Dưỡng',
        @FeaturesV2
    );

    -- Ghi AuditLogs tương ứng cho giao dịch thanh toán thành công
    INSERT INTO dbo.AuditLogs (
        [Actor_User_ID], [Actor_Role], [Action], [Target_Table], [Target_ID],
        [New_Value], [Note], [Created_At]
    )
    VALUES (
        3,
        'VIP',
        'PAYMENT_SUCCESS',
        'PaymentTransactions',
        'VIETQR_VIP_20261115_036',
        N'{"planId":' + CAST(@PlanId AS NVARCHAR(10)) + N',"version":2,"amount":59000.00,"gateway":"VIETQR"}',
        N'Thanh toán thành công gói VIP 1 Tháng Version 2 qua VietQR',
        DATEADD(DAY, -2, SYSDATETIME())
    );

    PRINT N'   -> Đã chèn giao dịch mẫu #36 mua gói Version 2 (User 3 - Hoàng Kim - 300 chat/ngày, Video-to-Recipe).';
END;

-- Giao dịch #37: Thành viên Phạm Đình (User 10) đăng ký mua gói mới (Version 2)
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
        @PlanId,
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
        N'Gói VIP 1 Tháng - Mở Khóa AI Dinh Dưỡng',
        @FeaturesV2
    );

    INSERT INTO dbo.AuditLogs (
        [Actor_User_ID], [Actor_Role], [Action], [Target_Table], [Target_ID],
        [New_Value], [Note], [Created_At]
    )
    VALUES (
        10,
        'VIP',
        'PAYMENT_SUCCESS',
        'PaymentTransactions',
        'MOMO_VIP_20261116_037',
        N'{"planId":' + CAST(@PlanId AS NVARCHAR(10)) + N',"version":2,"amount":59000.00,"gateway":"MOMO"}',
        N'Thanh toán thành công gói VIP 1 Tháng Version 2 qua MoMo',
        DATEADD(DAY, -1, SYSDATETIME())
    );

    PRINT N'   -> Đã chèn giao dịch mẫu #37 mua gói Version 2 (User 10 - Phạm Đình - 300 chat/ngày, Video-to-Recipe).';
END;

PRINT N'>> Hoàn tất nạp dữ liệu mẫu: Phân biệt rõ người đăng ký trước (V1) và sau (V2) thành công!';
GO
