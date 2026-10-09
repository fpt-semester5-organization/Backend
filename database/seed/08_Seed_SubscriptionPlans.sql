-- ============================================================
-- File: 08_Seed_SubscriptionPlans.sql
-- Thư mục: database/seed/
-- Mục đích: Nạp sẵn 03 gói hội viên VIP trả phí (49.000đ, 129.000đ, 449.000đ)
--           kèm cấu hình thời hạn và đặc quyền AI Features_JSON (BR-PAY-SUB-01, D111).
-- Liên kết: Khóa ngoại Admin_ID tham chiếu tới tài khoản Admin đầu tiên.
-- Tính chất: Idempotent - chạy nhiều lần không trùng dữ liệu (MERGE).
-- ============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'>> [Seed 08/08] Đang nạp dữ liệu Master 03 Gói Hội Viên VIP (SubscriptionPlans)...';

SET NOCOUNT ON;

DECLARE @Now DATETIME2(7) = SYSDATETIME();

-- Lấy Admin_ID từ tài khoản Admin vừa khởi tạo
DECLARE @AdminId INT = (SELECT TOP 1 User_ID FROM dbo.Users WHERE Role = 'ADMIN' ORDER BY User_ID ASC);

IF @AdminId IS NULL
BEGIN
    RAISERROR(N'Không tìm thấy tài khoản Admin trong hệ thống để gán vào SubscriptionPlans!', 16, 1);
    RETURN;
END;

MERGE INTO dbo.SubscriptionPlans AS target
USING (
    VALUES
        (
            N'Gói VIP 1 Tháng - Mở Khóa AI Dinh Dưỡng',
            49000.00,
            30,
            N'{"chatLimitDaily":200,"mealPlanLimitWeekly":3,"fridgeScanLimitDaily":30,"exclusiveBadge":true,"adFree":true,"priorityAiResponse":true,"description":"Gói trải nghiệm linh hoạt 30 ngày mở khóa toàn diện các tiện ích AI dinh dưỡng."}',
            1
        ),
        (
            N'Gói VIP 3 Tháng - Lộ Trình Sống Khỏe',
            129000.00,
            90,
            N'{"chatLimitDaily":200,"mealPlanLimitWeekly":3,"fridgeScanLimitDaily":30,"exclusiveBadge":true,"adFree":true,"priorityAiResponse":true,"dietitianConsultation":true,"description":"Gói tiết kiệm 90 ngày (tiết kiệm 12%), đồng hành xây dựng thói quen ăn chay khoa học."}',
            1
        ),
        (
            N'Gói VIP 1 Năm - Thuần Chay Bền Vững',
            449000.00,
            365,
            N'{"chatLimitDaily":200,"mealPlanLimitWeekly":3,"fridgeScanLimitDaily":30,"exclusiveBadge":true,"adFree":true,"priorityAiResponse":true,"dietitianConsultation":true,"earlyFeatureAccess":true,"description":"Gói toàn diện 365 ngày (tiết kiệm gần 25%), đặc quyền cao cấp nhất dành cho hội viên thân thiết."}',
            1
        )
) AS source (Plan_Name, Price, Duration_Days, Features_JSON, Is_Active)
ON (target.Plan_Name = source.Plan_Name)
WHEN MATCHED THEN
    UPDATE SET 
        target.Price = source.Price,
        target.Duration_Days = source.Duration_Days,
        target.Features_JSON = source.Features_JSON,
        target.Is_Active = source.Is_Active,
        target.Admin_ID = @AdminId,
        target.Updated_At = @Now
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Admin_ID, Plan_Name, Price, Duration_Days, Features_JSON, Is_Active, Created_At, Updated_At)
    VALUES (@AdminId, source.Plan_Name, source.Price, source.Duration_Days, source.Features_JSON, source.Is_Active, @Now, @Now);

PRINT N'   -> Hoàn tất nạp 03 Gói Hội Viên VIP thành công!';
GO
