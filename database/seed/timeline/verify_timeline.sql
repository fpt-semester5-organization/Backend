-- ============================================================================
-- verify_timeline.sql - kiểm tra chéo dữ liệu SEED60 (chỉ đọc, không sửa dữ liệu)
-- Kết quả: mỗi check 1 dòng (Check_Name, Violations, Result). Mọi dòng phải PASS.
-- Chạy: sqlcmd -S <server> -d <db> -f 65001 -i verify_timeline.sql
-- ============================================================================
SET NOCOUNT ON;
DECLARE @Now DATETIME2(7) = GETUTCDATE();
DECLARE @From DATETIME2(7) = DATEADD(DAY, -60, @Now);
DECLARE @S INT = 10001;   -- dải ID seed
DECLARE @E INT = 99999;   -- hết dải seed; dữ liệu app tạo sau khi nạp có ID >= 100000
CREATE TABLE #r (Seq INT IDENTITY, Check_Name NVARCHAR(200), Violations INT);
DECLARE @n INT;

-- 1) FK mồ côi: duyệt toàn bộ foreign key (single-column) trên bảng con, chỉ xét dòng seed ------------
DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += N'INSERT #r(Check_Name, Violations) SELECT N''ORPHAN '
      + OBJECT_NAME(fk.parent_object_id) + N'.' + pc.name + N' -> ' + OBJECT_NAME(fk.referenced_object_id)
      + N''', COUNT(*) FROM dbo.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id)) + N' c WHERE c.' + QUOTENAME(pc.name) + N' IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.'
      + QUOTENAME(OBJECT_NAME(fk.referenced_object_id)) + N' p WHERE p.' + QUOTENAME(rc.name) + N' = c.' + QUOTENAME(pc.name) + N');' + CHAR(10)
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
JOIN sys.columns pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
JOIN sys.columns rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
WHERE fk.is_disabled = 0 AND fk.key_index_id IS NOT NULL
  AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id = fk.object_id) = 1;
EXEC sp_executesql @sql;

-- 2) Mốc thời gian nằm trong [@Now-60d, @Now] (trừ các mốc tương lai hợp lệ) -------------------------
INSERT #r SELECT N'TIME Users.Created_At ngoài 60 ngày', COUNT(*) FROM dbo.Users WHERE User_ID BETWEEN @S AND @E AND (Created_At < @From OR Created_At > @Now);
INSERT #r SELECT N'TIME Posts.Created_At ngoài 60 ngày', COUNT(*) FROM dbo.Posts WHERE Post_ID BETWEEN @S AND @E AND (Created_At < @From OR Created_At > @Now);
INSERT #r SELECT N'TIME Comments.Created_At ngoài 60 ngày', COUNT(*) FROM dbo.Comments WHERE Comment_ID BETWEEN @S AND @E AND (Created_At < @From OR Created_At > @Now);
INSERT #r SELECT N'TIME ChatMessages.Created_At ngoài 60 ngày', COUNT(*) FROM dbo.ChatMessages WHERE Message_ID BETWEEN @S AND @E AND (Created_At < @From OR Created_At > @Now);
INSERT #r SELECT N'TIME AIQuotaLogs.Created_At ngoài 60 ngày', COUNT(*) FROM dbo.AIQuotaLogs WHERE Log_ID BETWEEN @S AND @E AND (Created_At < @From OR Created_At > @Now);
INSERT #r SELECT N'TIME AuditLogs.Created_At ngoài 60 ngày', COUNT(*) FROM dbo.AuditLogs WHERE Audit_ID BETWEEN @S AND @E AND (Created_At < @From OR Created_At > @Now);
INSERT #r SELECT N'TIME PaymentTransactions.Created_At ngoài 60 ngày', COUNT(*) FROM dbo.PaymentTransactions WHERE Transaction_ID BETWEEN @S AND @E AND (Created_At < @From OR Created_At > @Now);
INSERT #r SELECT N'TIME UserScanHistory.Scanned_At ngoài 60 ngày', COUNT(*) FROM dbo.UserScanHistory WHERE Scan_ID BETWEEN @S AND @E AND (Scanned_At < @From OR Scanned_At > @Now);
INSERT #r SELECT N'TIME FridgeSessions.Created_At ngoài 60 ngày', COUNT(*) FROM dbo.FridgeSessions WHERE Session_ID BETWEEN @S AND @E AND (Created_At < @From OR Created_At > @Now);

-- 3) Thứ tự thời gian: Created_At <= Updated_At / Completed_At; Starts_At < Ends_At ----------------------
INSERT #r SELECT N'ORDER Users Updated_At < Created_At', COUNT(*) FROM dbo.Users WHERE User_ID BETWEEN @S AND @E AND Updated_At IS NOT NULL AND Updated_At < Created_At;
INSERT #r SELECT N'ORDER Users Last_Login_At < Created_At', COUNT(*) FROM dbo.Users WHERE User_ID BETWEEN @S AND @E AND Last_Login_At IS NOT NULL AND Last_Login_At < Created_At;
INSERT #r SELECT N'ORDER Posts Updated_At < Created_At', COUNT(*) FROM dbo.Posts WHERE Post_ID BETWEEN @S AND @E AND Updated_At IS NOT NULL AND Updated_At < Created_At;
INSERT #r SELECT N'ORDER Comments Updated_At < Created_At', COUNT(*) FROM dbo.Comments WHERE Comment_ID BETWEEN @S AND @E AND Updated_At IS NOT NULL AND Updated_At < Created_At;
INSERT #r SELECT N'ORDER Recipes Updated_At < Created_At', COUNT(*) FROM dbo.Recipes WHERE Recipe_ID BETWEEN @S AND @E AND Updated_At IS NOT NULL AND Updated_At < Created_At;
INSERT #r SELECT N'ORDER Videos Updated_At < Created_At', COUNT(*) FROM dbo.Videos WHERE Video_ID BETWEEN @S AND @E AND Updated_At IS NOT NULL AND Updated_At < Created_At;
INSERT #r SELECT N'ORDER VideoTranscripts Completed_At < Created_At', COUNT(*) FROM dbo.VideoTranscripts WHERE Transcript_ID BETWEEN @S AND @E AND Completed_At IS NOT NULL AND Completed_At < Created_At;
INSERT #r SELECT N'ORDER Payment Completed_At < Created_At', COUNT(*) FROM dbo.PaymentTransactions WHERE Transaction_ID BETWEEN @S AND @E AND Completed_At IS NOT NULL AND Completed_At < Created_At;
INSERT #r SELECT N'ORDER Payment Ends_At <= Starts_At', COUNT(*) FROM dbo.PaymentTransactions WHERE Transaction_ID BETWEEN @S AND @E AND Ends_At <= Starts_At;
INSERT #r SELECT N'ORDER ChatMessages trước Started_At của session', COUNT(*) FROM dbo.ChatMessages m JOIN dbo.ChatSessions s ON s.Chat_Session_ID = m.Chat_Session_ID WHERE m.Message_ID BETWEEN @S AND @E AND m.Created_At < s.Started_At;
INSERT #r SELECT N'ORDER Comment trước Post', COUNT(*) FROM dbo.Comments c JOIN dbo.Posts p ON p.Post_ID = c.Post_ID WHERE c.Comment_ID BETWEEN @S AND @E AND c.Created_At < p.Created_At;
INSERT #r SELECT N'ORDER Reply trước Comment cha', COUNT(*) FROM dbo.Comments c JOIN dbo.Comments p ON p.Comment_ID = c.Parent_Comment_ID WHERE c.Comment_ID BETWEEN @S AND @E AND c.Created_At < p.Created_At;
INSERT #r SELECT N'ORDER Vote trước Post', COUNT(*) FROM dbo.Votes v JOIN dbo.Posts p ON p.Post_ID = v.Post_ID WHERE v.Vote_ID BETWEEN @S AND @E AND v.Created_At < p.Created_At;
INSERT #r SELECT N'ORDER Report trước nội dung bị báo cáo (post)', COUNT(*) FROM dbo.ViolationReports r JOIN dbo.Posts p ON p.Post_ID = r.Post_ID WHERE r.Report_ID BETWEEN @S AND @E AND r.Created_At < p.Created_At;
INSERT #r SELECT N'ORDER Swap trước Plan', COUNT(*) FROM dbo.MealSlotSwapHistories h JOIN dbo.MealSlots s ON s.Slot_ID = h.Slot_ID JOIN dbo.MealPlans p ON p.Plan_ID = s.Plan_ID WHERE h.Swap_History_ID BETWEEN @S AND @E AND h.Swapped_At < p.Created_At;

-- 4) VIP <-> Payment ---------------------------------------------------------------------------------
INSERT #r SELECT N'VIP seed không có giao dịch SUCCESS còn hạn', COUNT(*) FROM dbo.Users u
  WHERE u.User_ID BETWEEN @S AND @E AND u.Role = 'VIP' AND NOT EXISTS (SELECT 1 FROM dbo.PaymentTransactions t WHERE t.User_ID = u.User_ID AND t.Transaction_Status = 'SUCCESS' AND t.Ends_At > @Now AND t.Starts_At <= @Now);
INSERT #r SELECT N'MEMBER/ADMIN còn giao dịch SUCCESS đang hiệu lực (VIP hết hạn chưa hạ cấp)', COUNT(*) FROM dbo.Users u
  WHERE u.User_ID BETWEEN @S AND @E AND u.Role = 'MEMBER' AND EXISTS (SELECT 1 FROM dbo.PaymentTransactions t WHERE t.User_ID = u.User_ID AND t.Transaction_Status = 'SUCCESS' AND t.Ends_At > @Now AND t.Starts_At <= @Now);
INSERT #r SELECT N'PAYMENT SUCCESS thiếu Completed_At', COUNT(*) FROM dbo.PaymentTransactions WHERE Transaction_ID BETWEEN @S AND @E AND Transaction_Status = 'SUCCESS' AND Completed_At IS NULL;
INSERT #r SELECT N'PAYMENT PENDING quá 5 phút', COUNT(*) FROM dbo.PaymentTransactions WHERE Transaction_ID BETWEEN @S AND @E AND Transaction_Status = 'PENDING' AND Created_At < DATEADD(MINUTE, -5, @Now);
INSERT #r SELECT N'PAYMENT gia hạn chồng chéo (SUCCESS cùng user giao nhau)', COUNT(*) FROM dbo.PaymentTransactions a JOIN dbo.PaymentTransactions b
  ON a.User_ID = b.User_ID AND a.Transaction_ID < b.Transaction_ID AND a.Transaction_Status = 'SUCCESS' AND b.Transaction_Status = 'SUCCESS' AND a.Starts_At < b.Ends_At AND b.Starts_At < a.Ends_At
  WHERE a.Transaction_ID BETWEEN @S AND @E AND b.Transaction_ID BETWEEN @S AND @E;
INSERT #r SELECT N'PAYMENT Partner_Transaction_Code trùng', COUNT(*) FROM (SELECT Partner_Transaction_Code FROM dbo.PaymentTransactions WHERE Transaction_ID BETWEEN @S AND @E GROUP BY Partner_Transaction_Code HAVING COUNT(*) > 1) x;

-- 5) ViolationReports <-> Count_ViolationReports <-> thang chế tài -------------------------------------
;WITH tgt AS (   -- chủ sở hữu nội dung bị báo cáo
  SELECT r.Report_ID, r.Status,
         COALESCE(rc.User_ID, cm.User_ID, po.User_ID, vp.User_ID) AS Owner_ID,
         (CASE WHEN r.Recipe_ID IS NULL THEN 0 ELSE 1 END) + (CASE WHEN r.Comment_ID IS NULL THEN 0 ELSE 1 END)
       + (CASE WHEN r.Post_ID IS NULL THEN 0 ELSE 1 END) + (CASE WHEN r.Video_ID IS NULL THEN 0 ELSE 1 END) AS Targets
  FROM dbo.ViolationReports r
  LEFT JOIN dbo.Recipes rc ON rc.Recipe_ID = r.Recipe_ID
  LEFT JOIN dbo.Comments cm ON cm.Comment_ID = r.Comment_ID
  LEFT JOIN dbo.Posts po ON po.Post_ID = r.Post_ID
  LEFT JOIN dbo.Videos vd ON vd.Video_ID = r.Video_ID
  LEFT JOIN dbo.Posts vp ON vp.Post_ID = vd.Post_ID
  WHERE r.Report_ID BETWEEN @S AND @E)
INSERT #r SELECT N'REPORT không đúng 1 đối tượng bị báo cáo', COUNT(*) FROM tgt WHERE Targets <> 1;
;WITH tgt AS (
  SELECT r.Status, COALESCE(rc.User_ID, cm.User_ID, po.User_ID, vp.User_ID) AS Owner_ID
  FROM dbo.ViolationReports r
  LEFT JOIN dbo.Recipes rc ON rc.Recipe_ID = r.Recipe_ID
  LEFT JOIN dbo.Comments cm ON cm.Comment_ID = r.Comment_ID
  LEFT JOIN dbo.Posts po ON po.Post_ID = r.Post_ID
  LEFT JOIN dbo.Videos vd ON vd.Video_ID = r.Video_ID
  LEFT JOIN dbo.Posts vp ON vp.Post_ID = vd.Post_ID
  WHERE r.Report_ID BETWEEN @S AND @E AND r.Status = 'APPROVE')
INSERT #r SELECT N'COUNT_ViolationReports lệch số report APPROVE', COUNT(*) FROM dbo.Users u
  WHERE u.User_ID BETWEEN @S AND @E AND u.Count_ViolationReports <> (SELECT COUNT(*) FROM tgt WHERE tgt.Owner_ID = u.User_ID);
INSERT #r SELECT N'LADDER BANNED nhưng Count < 13', COUNT(*) FROM dbo.Users WHERE User_ID BETWEEN @S AND @E AND Account_Status = 'BANNED' AND Count_ViolationReports < 13;
INSERT #r SELECT N'LADDER Count >= 13 nhưng không BANNED', COUNT(*) FROM dbo.Users WHERE User_ID BETWEEN @S AND @E AND Count_ViolationReports >= 13 AND Account_Status <> 'BANNED';
INSERT #r SELECT N'LADDER SUSPENDED nhưng Count ngoài 4..12', COUNT(*) FROM dbo.Users WHERE User_ID BETWEEN @S AND @E AND Account_Status = 'SUSPENDED' AND Count_ViolationReports NOT BETWEEN 4 AND 12;
INSERT #r SELECT N'LADDER SUSPENDED nhưng Locked_Until không ở tương lai', COUNT(*) FROM dbo.Users WHERE User_ID BETWEEN @S AND @E AND Account_Status = 'SUSPENDED' AND (Locked_Until IS NULL OR Locked_Until <= @Now);
INSERT #r SELECT N'LADDER Count 1..3 mà bị SUSPENDED/BANNED', COUNT(*) FROM dbo.Users WHERE User_ID BETWEEN @S AND @E AND Count_ViolationReports BETWEEN 1 AND 3 AND Account_Status IN ('SUSPENDED','BANNED');
INSERT #r SELECT N'LADDER thời hạn khóa vượt bậc thang (3/10/30 ngày)', COUNT(*) FROM dbo.Users WHERE User_ID BETWEEN @S AND @E AND Account_Status = 'SUSPENDED'
  AND Locked_Until > DATEADD(DAY, CASE WHEN Count_ViolationReports <= 6 THEN 3 WHEN Count_ViolationReports <= 9 THEN 10 ELSE 30 END, @Now);
INSERT #r SELECT N'REPORT REJECT/PENDING bị tính vào Count (user chỉ có report không APPROVE mà Count>0)', COUNT(*) FROM dbo.Users u
  WHERE u.User_ID BETWEEN @S AND @E AND u.Count_ViolationReports > 0 AND NOT EXISTS (SELECT 1 FROM dbo.ViolationReports r WHERE r.Report_ID BETWEEN @S AND @E AND r.Status = 'APPROVE');

-- 6) Email / định danh ---------------------------------------------------------------------------------
INSERT #r SELECT N'EMAIL seed sai miền @seed60.veggielife.vn', COUNT(*) FROM dbo.Users WHERE User_ID BETWEEN @S AND (SELECT MAX(User_ID) FROM dbo.Users WHERE Email LIKE '%@seed60.veggielife.vn') AND Email NOT LIKE '%@seed60.veggielife.vn';   -- tài khoản thật đăng ký sau dải user seed không tính
INSERT #r SELECT N'EMAIL seed60 nằm ngoài dải ID >= 10001', COUNT(*) FROM dbo.Users WHERE User_ID < @S AND Email LIKE '%@seed60.veggielife.vn';
INSERT #r SELECT N'ID dữ liệu cũ bị chạm (Users ID<10001 phải còn nguyên)', CASE WHEN EXISTS (SELECT 1 FROM dbo.Users WHERE User_ID < @S) THEN 0 ELSE 1 END;

-- 7) JSON hợp lệ ------------------------------------------------------------------------------------------
INSERT #r SELECT N'JSON AuditLogs.Old_Value', COUNT(*) FROM dbo.AuditLogs WHERE Audit_ID BETWEEN @S AND @E AND Old_Value IS NOT NULL AND ISJSON(Old_Value) = 0;
INSERT #r SELECT N'JSON AuditLogs.New_Value', COUNT(*) FROM dbo.AuditLogs WHERE Audit_ID BETWEEN @S AND @E AND New_Value IS NOT NULL AND ISJSON(New_Value) = 0;
INSERT #r SELECT N'JSON ChatMessages.Metadata_JSON', COUNT(*) FROM dbo.ChatMessages WHERE Message_ID BETWEEN @S AND @E AND Metadata_JSON IS NOT NULL AND ISJSON(Metadata_JSON) = 0;
INSERT #r SELECT N'JSON FridgeSessions.Detected_Data_JSON', COUNT(*) FROM dbo.FridgeSessions WHERE Session_ID BETWEEN @S AND @E AND ISJSON(Detected_Data_JSON) = 0;
INSERT #r SELECT N'JSON VideoTranscripts.Extracted_Recipe_JSON', COUNT(*) FROM dbo.VideoTranscripts WHERE Transcript_ID BETWEEN @S AND @E AND Extracted_Recipe_JSON IS NOT NULL AND ISJSON(Extracted_Recipe_JSON) = 0;

-- 8) Tủ lạnh / quota / công thức ---------------------------------------------------------------------------
INSERT #r SELECT N'FRIDGE Freshness_Status không thuộc FRESH/NON-FRESH', COUNT(*) FROM dbo.FridgeInventory WHERE Inventory_ID BETWEEN @S AND @E AND Freshness_Status NOT IN ('FRESH','NON-FRESH');
INSERT #r SELECT N'QUOTA chat vượt hạn mức/ngày (Free 20, VIP 200) (SUCCESS vượt hạn mức)', COUNT(*) FROM (
  SELECT l.User_ID, CAST(l.Created_At AS DATE) d, u.Role, COUNT(*) c
  FROM dbo.AIQuotaLogs l JOIN dbo.Users u ON u.User_ID = l.User_ID
  WHERE l.Log_ID BETWEEN @S AND @E AND l.Feature_Code = 'AI_CHAT' AND l.Request_Status = 'SUCCESS'
  GROUP BY l.User_ID, CAST(l.Created_At AS DATE), u.Role
  HAVING COUNT(*) > CASE WHEN u.Role = 'VIP' THEN 200 ELSE 20 END) x;
INSERT #r SELECT N'RECIPE calo ngoài khoảng hợp lý (50..1500 kcal/phần)', COUNT(*) FROM dbo.Recipes WHERE Recipe_ID BETWEEN @S AND @E AND (Calories_Kcal < 50 OR Calories_Kcal > 1500);
INSERT #r SELECT N'RECIPE không có nguyên liệu', COUNT(*) FROM dbo.Recipes r WHERE r.Recipe_ID BETWEEN @S AND @E AND NOT EXISTS (SELECT 1 FROM dbo.RecipeIngredients i WHERE i.Recipe_ID = r.Recipe_ID);
INSERT #r SELECT N'MEALSLOT trỏ tới công thức chưa APPROVED_ACTIVE', COUNT(*) FROM dbo.MealSlots s JOIN dbo.Recipes r ON r.Recipe_ID = s.Recipe_ID WHERE s.Slot_ID BETWEEN @S AND @E AND r.Status <> 'APPROVED_ACTIVE';
INSERT #r SELECT N'SWAP Swaps_Remaining > Max_Swaps_Allowed', COUNT(*) FROM dbo.MealPlans WHERE Plan_ID BETWEEN @S AND @E AND Swaps_Remaining > Max_Swaps_Allowed;

-- Kết quả -----------------------------------------------------------------------------------------------------
SELECT Check_Name, SUM(Violations) AS Violations, CASE WHEN SUM(Violations) = 0 THEN 'PASS' ELSE 'FAIL' END AS Result
FROM #r GROUP BY Check_Name, Seq HAVING 1 = 1 ORDER BY CASE WHEN SUM(Violations) = 0 THEN 1 ELSE 0 END, Seq;
SELECT COUNT(*) AS Total_Checks, SUM(CASE WHEN Violations = 0 THEN 1 ELSE 0 END) AS Passed, SUM(CASE WHEN Violations > 0 THEN 1 ELSE 0 END) AS Failed FROM #r;
DROP TABLE #r;
