-- ============================================================================
-- File: 49_PaymentTransactions_PlanVersion_And_Full_Snapshot.sql
-- Table: PaymentTransactions, SubscriptionPlans
-- Cluster: Quản trị Tài khoản, Sức khỏe & Hội viên (VIP Subscriptions)
-- Description (2.plan solving - GĐ1, D121 Q1-B/Q2/Q3):
--   Giao dịch phải tự mang đủ "phiên bản + quyền lợi lúc mua", không phụ thuộc trạng thái hiện tại của gói.
--   1. Bổ sung [Plan_Version] vào [PaymentTransactions] (migration 48 đã xóa cột này) và backfill một lần
--      bằng [SubscriptionPlans].[Version] tại thời điểm thêm cột.
--   2. Bổ sung khóa hạn mức còn thiếu trong [SubscriptionPlans].[Features_JSON] từ SystemSettings VIP hiện hành
--      (chỉ thêm khóa thiếu, không ghi đè khóa đã có, không tăng Version vì Backend vốn đã dùng SystemSettings cho khóa thiếu).
--   3. Snapshot của giao dịch: NULL -> lấy Features_JSON của gói; thiếu khóa -> bổ sung như bước 2.
--   4. In số giao dịch SUCCESS còn thiếu khóa (kỳ vọng = 0).
-- Idempotent: chạy lại nhiều lần không đổi kết quả (backfill Plan_Version chỉ chạy khi vừa thêm cột).
-- ============================================================================

USE [VeggieLifeDb];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
-- PHẦN 1: CỘT [Plan_Version] TRÊN PaymentTransactions
-- ============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]')
      AND name = 'Plan_Version'
)
BEGIN
    ALTER TABLE [dbo].[PaymentTransactions]
    ADD [Plan_Version] INT NOT NULL
        CONSTRAINT [DF_PaymentTransactions_Plan_Version] DEFAULT (1)
        CONSTRAINT [CK_PaymentTransactions_Plan_Version] CHECK ([Plan_Version] >= 1);

    PRINT N'>> Da bo sung cot [Plan_Version] vao bang dbo.PaymentTransactions.';

    -- Backfill một lần: giao dịch cũ nhận phiên bản hiện hành của gói (hiện tất cả gói đều ở Version 1)
    EXEC sp_executesql N'
    UPDATE pt
    SET pt.[Plan_Version] = sp.[Version]
    FROM [dbo].[PaymentTransactions] pt
    INNER JOIN [dbo].[SubscriptionPlans] sp ON pt.[Plan_ID] = sp.[Plan_ID];';

    PRINT N'>> Da backfill [Plan_Version] cho cac giao dich hien co.';
END;
GO

-- ============================================================================
-- PHẦN 2: BỔ SUNG KHÓA HẠN MỨC CÒN THIẾU TRONG SubscriptionPlans.Features_JSON
-- ============================================================================
DECLARE @VipChat INT = ISNULL((SELECT TRY_CAST([Setting_Value] AS INT) FROM [dbo].[SystemSettings] WHERE [Setting_Key] = 'CHAT_LIMIT_DAILY_VIP'), 200);
DECLARE @VipMeal INT = ISNULL((SELECT TRY_CAST([Setting_Value] AS INT) FROM [dbo].[SystemSettings] WHERE [Setting_Key] = 'MEAL_PLAN_WEEKLY_LIMIT_VIP'), 3);
DECLARE @VipFridge INT = ISNULL((SELECT TRY_CAST([Setting_Value] AS INT) FROM [dbo].[SystemSettings] WHERE [Setting_Key] = 'FRIDGE_SCAN_LIMIT_DAILY_VIP'), 30);

PRINT N'>> Gia tri bo sung cho khoa thieu (SystemSettings VIP): chat=' + CAST(@VipChat AS NVARCHAR(10))
    + N', meal=' + CAST(@VipMeal AS NVARCHAR(10)) + N', fridge=' + CAST(@VipFridge AS NVARCHAR(10));

UPDATE [dbo].[SubscriptionPlans]
SET [Features_JSON] = N'{}'
WHERE [Features_JSON] IS NULL OR ISJSON([Features_JSON]) = 0;

-- Backend đọc cả camelCase lẫn snake_case (UserBenefitService), nên chỉ coi là thiếu khi cả hai đều không có
UPDATE [dbo].[SubscriptionPlans]
SET [Features_JSON] = JSON_MODIFY([Features_JSON], '$.chatLimitDaily', @VipChat)
WHERE JSON_VALUE([Features_JSON], '$.chatLimitDaily') IS NULL
  AND JSON_VALUE([Features_JSON], '$.chat_limit_daily') IS NULL;

UPDATE [dbo].[SubscriptionPlans]
SET [Features_JSON] = JSON_MODIFY([Features_JSON], '$.mealPlanLimitWeekly', @VipMeal)
WHERE JSON_VALUE([Features_JSON], '$.mealPlanLimitWeekly') IS NULL
  AND JSON_VALUE([Features_JSON], '$.meal_plan_limit_weekly') IS NULL;

UPDATE [dbo].[SubscriptionPlans]
SET [Features_JSON] = JSON_MODIFY([Features_JSON], '$.fridgeScanLimitDaily', @VipFridge)
WHERE JSON_VALUE([Features_JSON], '$.fridgeScanLimitDaily') IS NULL
  AND JSON_VALUE([Features_JSON], '$.fridge_scan_limit_daily') IS NULL;

PRINT N'>> Da chuan hoa Features_JSON cua cac goi VIP (du 3 khoa han muc).';

-- ============================================================================
-- PHẦN 3: CHUẨN HÓA SNAPSHOT CỦA GIAO DỊCH
-- ============================================================================
UPDATE pt
SET pt.[Snapshot_Features_JSON] = sp.[Features_JSON],
    pt.[Snapshot_Plan_Name] = ISNULL(pt.[Snapshot_Plan_Name], sp.[Plan_Name])
FROM [dbo].[PaymentTransactions] pt
INNER JOIN [dbo].[SubscriptionPlans] sp ON pt.[Plan_ID] = sp.[Plan_ID]
WHERE pt.[Snapshot_Features_JSON] IS NULL OR ISJSON(pt.[Snapshot_Features_JSON]) = 0;

UPDATE [dbo].[PaymentTransactions]
SET [Snapshot_Features_JSON] = JSON_MODIFY([Snapshot_Features_JSON], '$.chatLimitDaily', @VipChat)
WHERE JSON_VALUE([Snapshot_Features_JSON], '$.chatLimitDaily') IS NULL
  AND JSON_VALUE([Snapshot_Features_JSON], '$.chat_limit_daily') IS NULL;

UPDATE [dbo].[PaymentTransactions]
SET [Snapshot_Features_JSON] = JSON_MODIFY([Snapshot_Features_JSON], '$.mealPlanLimitWeekly', @VipMeal)
WHERE JSON_VALUE([Snapshot_Features_JSON], '$.mealPlanLimitWeekly') IS NULL
  AND JSON_VALUE([Snapshot_Features_JSON], '$.meal_plan_limit_weekly') IS NULL;

UPDATE [dbo].[PaymentTransactions]
SET [Snapshot_Features_JSON] = JSON_MODIFY([Snapshot_Features_JSON], '$.fridgeScanLimitDaily', @VipFridge)
WHERE JSON_VALUE([Snapshot_Features_JSON], '$.fridgeScanLimitDaily') IS NULL
  AND JSON_VALUE([Snapshot_Features_JSON], '$.fridge_scan_limit_daily') IS NULL;

PRINT N'>> Da chuan hoa Snapshot_Features_JSON cua cac giao dich (du 3 khoa han muc).';
GO

-- ============================================================================
-- PHẦN 4: KIỂM TRA SAU MIGRATION (kỳ vọng = 0)
-- ============================================================================
DECLARE @MissingSnapshot INT = (
    SELECT COUNT(*)
    FROM [dbo].[PaymentTransactions]
    WHERE [Transaction_Status] = 'SUCCESS'
      AND (
            [Snapshot_Features_JSON] IS NULL
         OR (JSON_VALUE([Snapshot_Features_JSON], '$.chatLimitDaily') IS NULL AND JSON_VALUE([Snapshot_Features_JSON], '$.chat_limit_daily') IS NULL)
         OR (JSON_VALUE([Snapshot_Features_JSON], '$.mealPlanLimitWeekly') IS NULL AND JSON_VALUE([Snapshot_Features_JSON], '$.meal_plan_limit_weekly') IS NULL)
         OR (JSON_VALUE([Snapshot_Features_JSON], '$.fridgeScanLimitDaily') IS NULL AND JSON_VALUE([Snapshot_Features_JSON], '$.fridge_scan_limit_daily') IS NULL)
      )
);

PRINT N'>> Giao dich SUCCESS con thieu khoa han muc trong snapshot: ' + CAST(@MissingSnapshot AS NVARCHAR(10)) + N' (ky vong 0).';
PRINT N'>> Hoan tat Migration 49: Plan_Version + snapshot quyen loi day du tren PaymentTransactions!';
GO
