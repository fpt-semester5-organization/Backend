-- ============================================================================
-- File: 15_Seed_SystemSettings.sql
-- Thư mục: database/seed/
-- Mục đích: Khởi tạo giá trị mặc định cho toàn bộ các tham số cấu hình động hệ thống
--           (Hạn mức đổi món, hạn mức AI, ngưỡng chi phí LLMOps, kiểm duyệt cộng đồng)
-- Tính chất: IDEMPOTENT (Chạy nhiều lần không gây lỗi trùng lặp khóa chính)
-- ============================================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'--> Đang nạp cấu hình hệ thống động (SystemSettings)...';

MERGE INTO dbo.SystemSettings AS Target
USING (VALUES
    ('MEAL_SWAP_LIMIT_DRAFT', N'21', N'21', 'INT', 'MEAL_PLAN', N'Hạn mức số lần đổi món thực đơn trong giai đoạn dự thảo (DRAFT)'),
    ('MEAL_SWAP_LIMIT_ACTIVE', N'21', N'21', 'INT', 'MEAL_PLAN', N'Hạn mức số lần đổi món thực đơn trong giai đoạn đã kích hoạt (ACTIVE)'),
    ('MEAL_PLAN_WEEKLY_LIMIT_FREE', N'1', N'1', 'INT', 'MEAL_PLAN', N'Số lượng thực đơn tối đa được tạo mỗi tuần cho tài khoản Miễn phí'),
    ('MEAL_PLAN_WEEKLY_LIMIT_VIP', N'3', N'3', 'INT', 'MEAL_PLAN', N'Số lượng thực đơn tối đa được tạo mỗi tuần cho tài khoản VIP'),
    ('MEAL_SWAP_CALORIE_TOLERANCE_PERCENT', N'15', N'15', 'INT', 'MEAL_PLAN', N'Độ lệch calo tối đa (%) khi thuật toán tự động tìm món thay thế'),
    ('CHAT_LIMIT_DAILY_GUEST', N'3', N'3', 'INT', 'AI_QUOTA', N'Số câu hỏi AI Chatbot tối đa cho khách vãng lai (Guest) trước khi bắt buộc đăng ký tài khoản'),
    ('CHAT_LIMIT_DAILY_FREE', N'20', N'20', 'INT', 'AI_QUOTA', N'Số câu hỏi AI Chatbot tối đa mỗi ngày cho tài khoản Miễn phí'),
    ('CHAT_LIMIT_DAILY_VIP', N'200', N'200', 'INT', 'AI_QUOTA', N'Số câu hỏi AI Chatbot tối đa mỗi ngày cho tài khoản VIP'),
    ('FRIDGE_SCAN_LIMIT_DAILY_FREE', N'10', N'10', 'INT', 'AI_QUOTA', N'Số lần quét tủ lạnh AI tối đa mỗi ngày cho tài khoản Miễn phí'),
    ('FRIDGE_SCAN_LIMIT_DAILY_VIP', N'30', N'30', 'INT', 'AI_QUOTA', N'Số lần quét tủ lạnh AI tối đa mỗi ngày cho tài khoản VIP'),
    ('LLMOPS_DAILY_BUDGET_USD', N'12.5', N'12.5', 'DECIMAL', 'LLMOPS', N'Ngân sách tiêu dùng AI (Gemini) tối đa mỗi ngày của toàn hệ thống (USD)'),
    ('LLMOPS_ALERT_THRESHOLD_PERCENT', N'80', N'80', 'INT', 'LLMOPS', N'Ngưỡng cảnh báo chi phí khẩn cấp (%) trước khi kích hoạt quy trình cắt giảm'),
    ('COMMUNITY_REPORT_LIMIT', N'10', N'10', 'INT', 'COMMUNITY', N'Số lượng báo cáo vi phạm tích lũy để bài viết tự động bị tạm ẩn chờ kiểm duyệt')
) AS Source (Setting_Key, Setting_Value, Default_Value, Data_Type, Category, Description)
ON Target.Setting_Key = Source.Setting_Key
WHEN MATCHED THEN
    UPDATE SET 
        Default_Value = Source.Default_Value,
        Data_Type = Source.Data_Type,
        Category = Source.Category,
        Description = Source.Description
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Setting_Key, Setting_Value, Default_Value, Data_Type, Category, Description, Updated_At)
    VALUES (Source.Setting_Key, Source.Setting_Value, Source.Default_Value, Source.Data_Type, Source.Category, Source.Description, SYSDATETIME());

PRINT N'    Đã đồng bộ thành công Master Data cho SystemSettings.';
GO
