-- Vá schema: DB dựng từ create_all_tables.sql cũ có thể thiếu cột này (entity FridgeSession.DetectedDataJson cần nó)
IF COL_LENGTH(N'dbo.FridgeSessions', N'Detected_Data_JSON') IS NULL
    ALTER TABLE dbo.FridgeSessions ADD Detected_Data_JSON NVARCHAR(MAX) NULL;
IF COL_LENGTH(N'dbo.FridgeInventory', N'Thumbnail_URL') IS NULL
    ALTER TABLE dbo.FridgeInventory ADD Thumbnail_URL NVARCHAR(MAX) NULL;
GO
-- ============================================================================
-- SEED60 - bộ dữ liệu mock 60 ngày cho VeggieLife (SQL Server). SINH TỰ ĐỘNG bởi generate_seed60.py (seed=60)
-- Dải ID 10001+; email @seed60.veggielife.vn; chạy lại nhiều lần an toàn (xóa theo dải ID rồi nạp lại).
-- LƯU Ý: dọn dẹp xóa MỌI dòng có ID >= 10001 ở các bảng trên (kể cả dòng app tạo sau khi nạp seed).
-- File lưu UTF-8: chạy bằng sqlcmd cần cờ -f 65001.
-- ============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
DECLARE @Now DATETIME2(7) = GETUTCDATE();
DECLARE @Dow0 INT = DATEDIFF(DAY, '19000101', CAST(@Now AS DATE)) % 7;   -- 0 = thứ Hai
PRINT N'>> SEED60 bắt đầu lúc ' + CONVERT(NVARCHAR(30), @Now, 121) + N' (UTC)';
BEGIN TRANSACTION;
