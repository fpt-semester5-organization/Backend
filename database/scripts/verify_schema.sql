-- ============================================================================
-- VeggieLifeDb - Database Schema Verification Script (Sprint 5-6)
-- Mục tiêu: Kiểm tra đếm lại toàn bộ cơ sở dữ liệu sau khi sinh từ tài liệu
-- Tiêu chuẩn nghiệm thu: Đúng 41 bảng và đúng 349 cột (loại trừ sysdiagrams nếu có).
-- ============================================================================

USE [VeggieLifeDb];
GO

SET NOCOUNT ON;

PRINT N'============================================================================';
PRINT N'            KIỂM TRA TỰ ĐỘNG CƠ SỞ DỮ LIỆU VEGGIELIFE (41 BẢNG - 349 CỘT)';
PRINT N'============================================================================';

DECLARE @TotalTables INT;
DECLARE @TotalColumns INT;

SELECT 
    @TotalTables = COUNT(DISTINCT TABLE_NAME),
    @TotalColumns = COUNT(*)
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME <> 'sysdiagrams';

PRINT N'1. TỔNG HỢP TOÀN BỘ CƠ SỞ DỮ LIỆU:';
PRINT N'   - Số bảng thực tế: ' + CAST(@TotalTables AS NVARCHAR(10)) + N' / 41 bảng yêu cầu';
PRINT N'   - Số cột thực tế:  ' + CAST(@TotalColumns AS NVARCHAR(10)) + N' / 349 cột yêu cầu';

IF @TotalTables = 41 AND @TotalColumns = 349
BEGIN
    PRINT N'   >>> KẾT QUẢ NGHIỆM THU: [PASS] ĐẠT 100% TIÊU CHÍ (41 BẢNG, 349 CỘT)';
END
ELSE
BEGIN
    PRINT N'   >>> KẾT QUẢ NGHIỆM THU: [FAIL] KHÔNG KHỚP TIÊU CHÍ ĐẶC TẢ!';
END
PRINT N'----------------------------------------------------------------------------';

PRINT N'2. CHI TIẾT SỐ LƯỢNG CỘT TỪNG BẢNG (41 BẢNG CHUẨN 3NF):';

SELECT 
    ROW_NUMBER() OVER (ORDER BY TABLE_NAME) AS [STT],
    TABLE_NAME AS [Tên Bảng],
    COUNT(*) AS [Số Cột]
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME <> 'sysdiagrams'
GROUP BY TABLE_NAME
ORDER BY TABLE_NAME;
GO
