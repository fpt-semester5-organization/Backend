-- ============================================================================
-- File: 50_SystemSettings_BarcodeScanLimits.sql
-- Table: SystemSettings, SubscriptionPlans, PaymentTransactions
-- Cluster: Quản trị Hệ thống, Tham số & Kiểm toán
-- Description (4. Barcode quota plan - GĐ1, D122):
--   Hạn mức quét mã vạch do Admin cấu hình trong "Chỉ số hệ thống" (Khách 3 / Free 20 / VIP 100 lượt/ngày).
--   1. Thêm 3 khóa BARCODE_SCAN_LIMIT_DAILY_GUEST / _FREE / _VIP vào SystemSettings (chỉ thêm khi chưa có).
--   2. Bổ sung khóa [barcodeScanLimitDaily] = Settings VIP vào Features_JSON của gói còn thiếu
--      (chỉ thêm khóa thiếu, không ghi đè, không tăng Version - giống migration 49).
--   3. Bổ sung [barcodeScanLimitDaily] = 100 vào Snapshot_Features_JSON của giao dịch còn thiếu:
--      người mua trước đây được quét không giới hạn, 100 là mức VIP mới gần nhất (D122).
--   4. In số gói / giao dịch SUCCESS còn thiếu khóa (kỳ vọng 0).
-- Không tạo bảng, không thêm cột. Idempotent: chạy lại không đổi kết quả.
-- ============================================================================

USE [VeggieLifeDb];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
-- PHẦN 1: KHÓA HẠN MỨC QUÉT MÃ VẠCH TRONG SystemSettings
-- ============================================================================
DECLARE @Now DATETIME2(7) = SYSUTCDATETIME();

INSERT INTO [dbo].[SystemSettings] ([Setting_Key], [Setting_Value], [Default_Value], [Data_Type], [Category], [Description], [Updated_At])
SELECT source.[Setting_Key], source.[Setting_Value], source.[Default_Value], 'INT', 'AI_QUOTA', source.[Description], @Now
FROM (VALUES
    ('BARCODE_SCAN_LIMIT_DAILY_GUEST', N'3', N'3', N'Số lần quét mã vạch tối đa mỗi ngày cho khách vãng lai (Guest)'),
    ('BARCODE_SCAN_LIMIT_DAILY_FREE', N'20', N'20', N'Số lần quét mã vạch tối đa mỗi ngày cho tài khoản Miễn phí'),
    ('BARCODE_SCAN_LIMIT_DAILY_VIP', N'100', N'100', N'Số lần quét mã vạch tối đa mỗi ngày cho tài khoản VIP')
) AS source ([Setting_Key], [Setting_Value], [Default_Value], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[SystemSettings] s WHERE s.[Setting_Key] = source.[Setting_Key]);

PRINT N'>> Them khoa han muc quet ma vach vao SystemSettings: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' dong.';
GO

-- ============================================================================
-- PHẦN 2: BỔ SUNG [barcodeScanLimitDaily] VÀO Features_JSON CỦA GÓI VIP
-- ============================================================================
DECLARE @VipBarcode INT = ISNULL((SELECT TRY_CAST([Setting_Value] AS INT) FROM [dbo].[SystemSettings] WHERE [Setting_Key] = 'BARCODE_SCAN_LIMIT_DAILY_VIP'), 100);

UPDATE [dbo].[SubscriptionPlans]
SET [Features_JSON] = JSON_MODIFY(ISNULL(NULLIF([Features_JSON], N''), N'{}'), '$.barcodeScanLimitDaily', @VipBarcode)
WHERE ([Features_JSON] IS NULL OR ISJSON([Features_JSON]) = 1)
  AND JSON_VALUE(ISNULL([Features_JSON], N'{}'), '$.barcodeScanLimitDaily') IS NULL
  AND JSON_VALUE(ISNULL([Features_JSON], N'{}'), '$.barcode_scan_limit_daily') IS NULL;

PRINT N'>> Bo sung barcodeScanLimitDaily = ' + CAST(@VipBarcode AS NVARCHAR(10)) + N' cho ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' goi VIP.';
GO

-- ============================================================================
-- PHẦN 3: BỔ SUNG [barcodeScanLimitDaily] = 100 VÀO SNAPSHOT GIAO DỊCH CŨ
-- ============================================================================
UPDATE [dbo].[PaymentTransactions]
SET [Snapshot_Features_JSON] = JSON_MODIFY([Snapshot_Features_JSON], '$.barcodeScanLimitDaily', 100)
WHERE [Snapshot_Features_JSON] IS NOT NULL
  AND ISJSON([Snapshot_Features_JSON]) = 1
  AND JSON_VALUE([Snapshot_Features_JSON], '$.barcodeScanLimitDaily') IS NULL
  AND JSON_VALUE([Snapshot_Features_JSON], '$.barcode_scan_limit_daily') IS NULL;

PRINT N'>> Bo sung barcodeScanLimitDaily = 100 cho ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' giao dich.';
GO

-- ============================================================================
-- PHẦN 4: KIỂM TRA SAU MIGRATION (kỳ vọng = 0)
-- ============================================================================
DECLARE @MissingPlans INT = (
    SELECT COUNT(*) FROM [dbo].[SubscriptionPlans]
    WHERE JSON_VALUE(ISNULL([Features_JSON], N'{}'), '$.barcodeScanLimitDaily') IS NULL
      AND JSON_VALUE(ISNULL([Features_JSON], N'{}'), '$.barcode_scan_limit_daily') IS NULL
);
DECLARE @MissingTransactions INT = (
    SELECT COUNT(*) FROM [dbo].[PaymentTransactions]
    WHERE [Transaction_Status] = 'SUCCESS'
      AND ([Snapshot_Features_JSON] IS NULL
        OR (JSON_VALUE([Snapshot_Features_JSON], '$.barcodeScanLimitDaily') IS NULL
            AND JSON_VALUE([Snapshot_Features_JSON], '$.barcode_scan_limit_daily') IS NULL))
);

PRINT N'>> Goi con thieu barcodeScanLimitDaily: ' + CAST(@MissingPlans AS NVARCHAR(10)) + N' (ky vong 0).';
PRINT N'>> Giao dich SUCCESS con thieu barcodeScanLimitDaily: ' + CAST(@MissingTransactions AS NVARCHAR(10)) + N' (ky vong 0).';
PRINT N'>> Hoan tat Migration 50: han muc quet ma vach cau hinh trong SystemSettings!';
GO
