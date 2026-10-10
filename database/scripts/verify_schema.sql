-- ============================================================================
-- VeggieLifeDb - Database Schema Verification Script (Sprint 5: D1-D2)
-- Mục tiêu: Kiểm tra đếm lại toàn bộ cơ sở dữ liệu sau khi sinh từ tài liệu
-- Tiêu chuẩn nghiệm thu: Đúng 38 bảng và đúng 312 cột (loại trừ sysdiagrams nếu có).
-- ============================================================================

USE [VeggieLifeDb];
GO

SET NOCOUNT ON;

PRINT '============================================================================';
PRINT '            KIỂM TRA TỰ ĐỘNG CƠ SỞ DỮ LIỆU VEGGIELIFE (38 BẢNG - 312 CỘT)';
PRINT '============================================================================';

DECLARE @TotalTables INT;
DECLARE @TotalColumns INT;

SELECT 
    @TotalTables = COUNT(DISTINCT TABLE_NAME),
    @TotalColumns = COUNT(*)
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME <> 'sysdiagrams';

PRINT '1. TỔNG HỢP TOÀN BỘ CƠ SỞ DỮ LIỆU:';
PRINT '   - Số bảng thực tế: ' + CAST(@TotalTables AS VARCHAR(10)) + ' / 38 bảng yêu cầu';
PRINT '   - Số cột thực tế:  ' + CAST(@TotalColumns AS VARCHAR(10)) + ' / 312 cột yêu cầu';

IF @TotalTables = 38 AND @TotalColumns = 312
BEGIN
    PRINT '   >>> KẾT QUẢ NGHIỆM THU: [PASS] ĐẠT 100% TIÊU CHÍ (38 BẢNG, 312 CỘT)';
END
ELSE
BEGIN
    PRINT '   >>> KẾT QUẢ NGHIỆM THU: [FAIL] KHÔNG KHỚP TIÊU CHÍ ĐẶC TẢ!';
END
PRINT '----------------------------------------------------------------------------';

PRINT '2. CHI TIẾT SỐ LƯỢNG CỘT TỪNG BẢNG (38 BẢNG CHUẨN 3NF):';

SELECT 
    ROW_NUMBER() OVER (ORDER BY TABLE_NAME) AS [STT],
    TABLE_NAME AS [Tên Bảng],
    COUNT(*) AS [Số Cột]
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME <> 'sysdiagrams'
GROUP BY TABLE_NAME
ORDER BY TABLE_NAME;
GO
