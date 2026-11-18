-- ============================================================================
-- File: 22_Seed_AIQuotaLogs_2Months.sql
-- Thư mục: database/seed/
-- Mục đích: Nạp lịch sử sử dụng AI 60 ngày gần nhất (AIQuotaLogs) để màn hình
--           LLMOps / Phân tích chi phí (FN-ADM-16) có dữ liệu dày, có xu hướng tăng.
--
-- Quy tắc dữ liệu:
--   * Chỉ nạp từ 60 ngày trước đến HẾT HÔM QUA (giờ Việt Nam); dữ liệu test thật của hôm nay giữ nguyên.
--   * Chức năng khách hàng (AI_CHAT, MEAL_PLAN_GEN, AI_FRIDGE, AI_BARCODE, SUMMARIZE_VIDEO,
--     CONTENT_MODERATION) chỉ gán cho MEMBER/VIP đang ACTIVE; VIP dùng nhiều hơn (trọng số x3).
--   * AI_EMBEDDING (đồng bộ kho tri thức) là tác vụ hệ thống, gán cho tài khoản ADMIN.
--   * Lưu lượng tăng dần ~70 -> ~250 lượt/ngày, cuối tuần cao hơn, cao điểm 7-8h, 11-13h, 18-22h.
--   * Chi phí tính đúng công thức AiCostCalculator.CalculateFromTotal (70% prompt x 0.075 USD/1M,
--     30% completion x 0.300 USD/1M, làm tròn 4 chữ số, sàn 0.0001 USD).
--   * ~2% ERROR, ~2% QUOTA_EXCEEDED (0 token, 0 USD).
--   * Gán lại 6 log mẫu của DevDataSeeder đang thuộc Admin #1 (chức năng khách hàng) cho khách hàng.
--
-- Tính chất: chạy lại an toàn — bỏ qua nếu khoảng 60 ngày đã có >= 5.000 log.
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @todayVn DATE = CAST(DATEADD(HOUR, 7, SYSUTCDATETIME()) AS DATE);
DECLARE @fromVn  DATE = DATEADD(DAY, -60, @todayVn);
DECLARE @fromUtc DATETIME2 = DATEADD(HOUR, -7, CAST(@fromVn AS DATETIME2));
DECLARE @toUtc   DATETIME2 = DATEADD(HOUR, -7, CAST(@todayVn AS DATETIME2));

BEGIN TRANSACTION;

-- 0. Log mẫu của DevDataSeeder gán nhầm cho Admin #1 -> chuyển sang khách hàng
;WITH customers AS (
    SELECT User_ID, ROW_NUMBER() OVER (ORDER BY User_ID) AS rn, COUNT(*) OVER () AS cnt
    FROM dbo.Users WHERE Role IN ('MEMBER', 'VIP') AND Account_Status = 'ACTIVE'
)
UPDATE l
SET l.User_ID = c.User_ID
FROM dbo.AIQuotaLogs l
JOIN dbo.Users u ON u.User_ID = l.User_ID AND u.Role = 'ADMIN'
CROSS APPLY (SELECT TOP 1 User_ID FROM customers
             WHERE rn = 1 + (l.Log_ID * 37) % (SELECT MAX(cnt) FROM customers)) c
WHERE l.Feature_Code <> 'AI_EMBEDDING';
PRINT N'Gán lại log chức năng khách hàng khỏi Admin: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

IF (SELECT COUNT(*) FROM dbo.AIQuotaLogs WHERE Created_At >= @fromUtc AND Created_At < @toUtc) >= 5000
BEGIN
    PRINT N'Đã có đủ lịch sử 60 ngày, bỏ qua bước nạp.';
    COMMIT TRANSACTION;
    RETURN;
END;

-- 1. Nhóm người dùng: khách hàng (VIP x3) và Admin cho tác vụ hệ thống
IF OBJECT_ID('tempdb..#cust') IS NOT NULL DROP TABLE #cust;
SELECT ROW_NUMBER() OVER (ORDER BY NEWID()) AS idx, x.User_ID
INTO #cust
FROM (
    SELECT User_ID FROM dbo.Users WHERE Role = 'MEMBER' AND Account_Status = 'ACTIVE'
    UNION ALL
    SELECT u.User_ID FROM dbo.Users u CROSS JOIN (VALUES (1), (2), (3)) w(k)
    WHERE u.Role = 'VIP' AND u.Account_Status = 'ACTIVE'
) x;
DECLARE @custCount INT = (SELECT COUNT(*) FROM #cust);

IF OBJECT_ID('tempdb..#adm') IS NOT NULL DROP TABLE #adm;
SELECT ROW_NUMBER() OVER (ORDER BY User_ID) AS idx, User_ID INTO #adm
FROM dbo.Users WHERE Role = 'ADMIN' AND Account_Status = 'ACTIVE';
DECLARE @admCount INT = (SELECT COUNT(*) FROM #adm);

IF @custCount = 0
BEGIN
    PRINT N'Không có khách hàng ACTIVE, bỏ qua.';
    COMMIT TRANSACTION;
    RETURN;
END;

-- 2. Trọng số giờ trong ngày (giờ Việt Nam), tổng 100
IF OBJECT_ID('tempdb..#hours') IS NOT NULL DROP TABLE #hours;
CREATE TABLE #hours (h INT, w INT, lo INT, hi INT);
INSERT INTO #hours (h, w) VALUES
 (0,1),(1,1),(2,0),(3,0),(4,0),(5,1),(6,3),(7,6),(8,6),(9,5),(10,5),(11,7),
 (12,8),(13,6),(14,4),(15,4),(16,4),(17,5),(18,7),(19,8),(20,8),(21,7),(22,3),(23,1);
;WITH c AS (SELECT h, w, SUM(w) OVER (ORDER BY h ROWS UNBOUNDED PRECEDING) AS cum FROM #hours)
UPDATE hs SET lo = c.cum - c.w, hi = c.cum FROM #hours hs JOIN c ON c.h = hs.h;

-- 3. Số lượt mỗi ngày: tăng dần, cuối tuần x1.25, dao động +-20%
IF OBJECT_ID('tempdb..#days') IS NOT NULL DROP TABLE #days;
;WITH d AS (
    SELECT TOP (60) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS i FROM sys.all_objects
)
SELECT DATEADD(DAY, i, @fromVn) AS dayVn,
       CAST((70 + i * 3)
            * CASE WHEN DATEPART(WEEKDAY, DATEADD(DAY, i, @fromVn)) IN (1, 7) THEN 1.25 ELSE 1.0 END
            * (0.8 + (ABS(CHECKSUM(NEWID())) % 41) / 100.0) AS INT) AS vol
INTO #days
FROM d;

-- 4. Sinh lượt gọi với các số ngẫu nhiên được cố định trước
IF OBJECT_ID('tempdb..#gen') IS NOT NULL DROP TABLE #gen;
;WITH n AS (
    SELECT TOP (400) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS k FROM sys.all_objects a CROSS JOIN sys.all_objects b
)
SELECT d.dayVn,
       ABS(CHECKSUM(NEWID())) % 100   AS rFeat,
       ABS(CHECKSUM(NEWID())) % 100   AS rHour,
       ABS(CHECKSUM(NEWID())) % 3600  AS rSec,
       ABS(CHECKSUM(NEWID())) % 10000 AS rTok,
       ABS(CHECKSUM(NEWID())) % 100   AS rStatus,
       ABS(CHECKSUM(NEWID())) % 1000  AS rMs,
       ABS(CHECKSUM(NEWID()))         AS rUser
INTO #gen
FROM #days d
JOIN n ON n.k <= d.vol;

IF OBJECT_ID('tempdb..#rows') IS NOT NULL DROP TABLE #rows;
SELECT g.*,
       CASE
           WHEN g.rFeat < 42 THEN 'AI_CHAT'
           WHEN g.rFeat < 58 THEN 'AI_BARCODE'
           WHEN g.rFeat < 70 THEN 'AI_FRIDGE'
           WHEN g.rFeat < 78 THEN 'MEAL_PLAN_GEN'
           WHEN g.rFeat < 93 THEN 'CONTENT_MODERATION'
           WHEN g.rFeat < 98 THEN 'SUMMARIZE_VIDEO'
           ELSE 'AI_EMBEDDING'
       END AS feature,
       hs.h AS hourVn
INTO #rows
FROM #gen g
JOIN #hours hs ON g.rHour >= hs.lo AND g.rHour < hs.hi;

-- 5. Ghi vào AIQuotaLogs
INSERT INTO dbo.AIQuotaLogs (User_ID, Feature_Code, Tokens_Consumed, Cost_Estimate_USD, Request_Status, Error_Message, Execution_Time_Ms, Created_At)
SELECT
    CASE WHEN r.feature = 'AI_EMBEDDING' AND @admCount > 0
         THEN (SELECT User_ID FROM #adm WHERE idx = 1 + r.rUser % @admCount)
         ELSE (SELECT User_ID FROM #cust WHERE idx = 1 + r.rUser % @custCount) END,
    r.feature,
    t.tokens,
    CASE WHEN t.tokens <= 0 THEN 0.0000
         ELSE CASE WHEN c.cost < 0.0001 THEN 0.0001 ELSE c.cost END END,
    s.status,
    CASE s.status WHEN 'ERROR' THEN N'Hết thời gian chờ phản hồi từ AI service (timeout 30 giây).' END,
    CASE WHEN s.status = 'QUOTA_EXCEEDED' THEN 5 + r.rMs % 20
         ELSE ms.baseMs + r.rMs * ms.spanMs / 1000 END,
    DATEADD(HOUR, -7, DATEADD(SECOND, r.rSec, DATEADD(HOUR, r.hourVn, CAST(r.dayVn AS DATETIME2))))
FROM #rows r
CROSS APPLY (SELECT CASE
        WHEN r.rStatus < 2 THEN 'ERROR'
        WHEN r.rStatus < 4 AND r.feature IN ('AI_CHAT', 'AI_FRIDGE', 'MEAL_PLAN_GEN') THEN 'QUOTA_EXCEEDED'
        ELSE 'SUCCESS' END AS status) s
CROSS APPLY (SELECT CASE r.feature
        WHEN 'AI_CHAT'            THEN 1200 + r.rTok * 3600 / 10000
        WHEN 'MEAL_PLAN_GEN'      THEN 6000 + r.rTok * 8000 / 10000
        WHEN 'AI_FRIDGE'          THEN 220  + r.rTok * 730  / 10000
        WHEN 'AI_BARCODE'         THEN 850  + r.rTok * 750  / 10000
        WHEN 'SUMMARIZE_VIDEO'    THEN 2500 + r.rTok * 6500 / 10000
        WHEN 'CONTENT_MODERATION' THEN 700  + r.rTok * 1900 / 10000
        ELSE                           300  + r.rTok * 1200 / 10000 END AS fullTokens) ft
CROSS APPLY (SELECT CASE
        WHEN s.status = 'QUOTA_EXCEEDED' THEN 0
        WHEN s.status = 'ERROR' THEN ft.fullTokens / 3
        ELSE ft.fullTokens END AS tokens) t
CROSS APPLY (SELECT ROUND((ROUND(t.tokens * 0.7, 0) * 0.075 + (t.tokens - ROUND(t.tokens * 0.7, 0)) * 0.300) / 1000000.0, 4) AS cost) c
CROSS APPLY (SELECT
        CASE r.feature WHEN 'AI_CHAT' THEN 900 WHEN 'MEAL_PLAN_GEN' THEN 4000 WHEN 'AI_FRIDGE' THEN 1500
                       WHEN 'AI_BARCODE' THEN 600 WHEN 'SUMMARIZE_VIDEO' THEN 6000 WHEN 'CONTENT_MODERATION' THEN 400 ELSE 250 END AS baseMs,
        CASE r.feature WHEN 'AI_CHAT' THEN 2600 WHEN 'MEAL_PLAN_GEN' THEN 9000 WHEN 'AI_FRIDGE' THEN 2500
                       WHEN 'AI_BARCODE' THEN 1400 WHEN 'SUMMARIZE_VIDEO' THEN 18000 WHEN 'CONTENT_MODERATION' THEN 900 ELSE 600 END AS spanMs) ms;

PRINT N'Đã nạp lịch sử AI 60 ngày: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' lượt.';

COMMIT TRANSACTION;
