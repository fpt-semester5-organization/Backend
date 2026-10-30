-- ============================================================================
-- File: 15_Seed_SystemSettings.sql
-- Thư mục: database/seed/
-- Mục đích: Khởi tạo giá trị mặc định cho toàn bộ các tham số cấu hình động hệ thống
--           (Hạn mức đổi món, hạn mức AI, ngưỡng chi phí LLMOps, kiểm duyệt cộng đồng, URL tài liệu pháp lý)
-- Tính chất: IDEMPOTENT (Chạy nhiều lần không gây lỗi trùng lặp khóa chính)
-- ============================================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'--> Đang nạp cấu hình hệ thống động (SystemSettings)...';

MERGE INTO dbo.SystemSettings AS Target
USING (VALUES
    ('MEAL_SWAP_LIMIT_DRAFT', N'21', N'21', 'INT', 'MEAL_PLAN', N'Hạn mức số lần đổi món thực đơn trong giai đoạn dự thảo (DRAFT)', NULL),
    ('MEAL_SWAP_LIMIT_ACTIVE', N'21', N'21', 'INT', 'MEAL_PLAN', N'Hạn mức số lần đổi món thực đơn trong giai đoạn đã kích hoạt (ACTIVE)', NULL),
    ('MEAL_PLAN_WEEKLY_LIMIT_FREE', N'1', N'1', 'INT', 'MEAL_PLAN', N'Số lượng thực đơn tối đa được tạo mỗi tuần cho tài khoản Miễn phí', NULL),
    ('MEAL_PLAN_WEEKLY_LIMIT_VIP', N'3', N'3', 'INT', 'MEAL_PLAN', N'Số lượng thực đơn tối đa được tạo mỗi tuần cho tài khoản VIP', NULL),
    ('MEAL_SWAP_CALORIE_TOLERANCE_PERCENT', N'15', N'15', 'INT', 'MEAL_PLAN', N'Độ lệch calo tối đa (%) khi thuật toán tự động tìm món thay thế', NULL),
    ('CHAT_LIMIT_DAILY_GUEST', N'3', N'3', 'INT', 'AI_QUOTA', N'Số câu hỏi AI Chatbot tối đa cho khách vãng lai (Guest) trước khi bắt buộc đăng ký tài khoản', NULL),
    ('CHAT_LIMIT_DAILY_FREE', N'20', N'20', 'INT', 'AI_QUOTA', N'Số câu hỏi AI Chatbot tối đa mỗi ngày cho tài khoản Miễn phí', NULL),
    ('CHAT_LIMIT_DAILY_VIP', N'200', N'200', 'INT', 'AI_QUOTA', N'Số câu hỏi AI Chatbot tối đa mỗi ngày cho tài khoản VIP', NULL),
    ('FRIDGE_SCAN_LIMIT_DAILY_FREE', N'10', N'10', 'INT', 'AI_QUOTA', N'Số lần quét tủ lạnh AI tối đa mỗi ngày cho tài khoản Miễn phí', NULL),
    ('FRIDGE_SCAN_LIMIT_DAILY_VIP', N'30', N'30', 'INT', 'AI_QUOTA', N'Số lần quét tủ lạnh AI tối đa mỗi ngày cho tài khoản VIP', NULL),
    ('LLMOPS_DAILY_BUDGET_USD', N'12.5', N'12.5', 'DECIMAL', 'LLMOPS', N'Ngân sách tiêu dùng AI (Gemini) tối đa mỗi ngày của toàn hệ thống (USD)', NULL),
    ('LLMOPS_ALERT_THRESHOLD_PERCENT', N'80', N'80', 'INT', 'LLMOPS', N'Ngưỡng cảnh báo chi phí khẩn cấp (%) trước khi kích hoạt quy trình cắt giảm', NULL),
    ('COMMUNITY_REPORT_LIMIT', N'10', N'10', 'INT', 'COMMUNITY', N'Số lượng báo cáo vi phạm tích lũy để bài viết tự động bị tạm ẩn chờ kiểm duyệt', NULL),
    ('LEGAL_DOCUMENTS', N'{"user_agreement":"https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Ban_Cam_Ket_Va_Thoa_Thuan_Dang_Ky_Nguoi_Dung.png","privacy_policy":"https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Chinh_Sach_Bao_Mat_Du_Lieu_VeggieLife.png","terms_of_service":"https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Dieu_Khoan_Su_Dung_Dich_Vu_VeggieLife.png","vegan_disclaimer":"https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Quy_Chuan_Am_Thuc_Chay_Va_Mien_Tru_Y_Te_VeggieLife.png"}', N'{"user_agreement":"https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Ban_Cam_Ket_Va_Thoa_Thuan_Dang_Ky_Nguoi_Dung.png","privacy_policy":"https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Chinh_Sach_Bao_Mat_Du_Lieu_VeggieLife.png","terms_of_service":"https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Dieu_Khoan_Su_Dung_Dich_Vu_VeggieLife.png","vegan_disclaimer":"https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Quy_Chuan_Am_Thuc_Chay_Va_Mien_Tru_Y_Te_VeggieLife.png"}', 'STRING', 'GENERAL', N'Tài liệu pháp lý VeggieLife (Điều khoản, Chính sách bảo mật, Cam kết người dùng, Quy chuẩn ẩm thực chay)', N'{"user_agreement":"https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Ban_Cam_Ket_Va_Thoa_Thuan_Dang_Ky_Nguoi_Dung.png","privacy_policy":"https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Chinh_Sach_Bao_Mat_Du_Lieu_VeggieLife.png","terms_of_service":"https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Dieu_Khoan_Su_Dung_Dich_Vu_VeggieLife.png","vegan_disclaimer":"https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Quy_Chuan_Am_Thuc_Chay_Va_Mien_Tru_Y_Te_VeggieLife.png"}'),
    ('LEGAL_USER_AGREEMENT_URL', N'https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Ban_Cam_Ket_Va_Thoa_Thuan_Dang_Ky_Nguoi_Dung.png', N'https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Ban_Cam_Ket_Va_Thoa_Thuan_Dang_Ky_Nguoi_Dung.png', 'STRING', 'GENERAL', N'Bản Cam Kết Và Thỏa Thuận Đăng Ký Người Dùng', N'https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Ban_Cam_Ket_Va_Thoa_Thuan_Dang_Ky_Nguoi_Dung.png'),
    ('LEGAL_PRIVACY_POLICY_URL', N'https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Chinh_Sach_Bao_Mat_Du_Lieu_VeggieLife.png', N'https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Chinh_Sach_Bao_Mat_Du_Lieu_VeggieLife.png', 'STRING', 'GENERAL', N'Chính Sách Bảo Mật Dữ Liệu VeggieLife', N'https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Chinh_Sach_Bao_Mat_Du_Lieu_VeggieLife.png'),
    ('LEGAL_TERMS_OF_SERVICE_URL', N'https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Dieu_Khoan_Su_Dung_Dich_Vu_VeggieLife.png', N'https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Dieu_Khoan_Su_Dung_Dich_Vu_VeggieLife.png', 'STRING', 'GENERAL', N'Điều Khoản Sử Dụng Dịch Vụ VeggieLife', N'https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Dieu_Khoan_Su_Dung_Dich_Vu_VeggieLife.png'),
    ('LEGAL_VEGAN_DISCLAIMER_URL', N'https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Quy_Chuan_Am_Thuc_Chay_Va_Mien_Tru_Y_Te_VeggieLife.png', N'https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Quy_Chuan_Am_Thuc_Chay_Va_Mien_Tru_Y_Te_VeggieLife.png', 'STRING', 'GENERAL', N'Quy Chuẩn Ẩm Thực Chay Và Miễn Trừ Y Tế VeggieLife', N'https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Quy_Chuan_Am_Thuc_Chay_Va_Mien_Tru_Y_Te_VeggieLife.png')
) AS Source (Setting_Key, Setting_Value, Default_Value, Data_Type, Category, Description, Legal_Documents_Url)
ON Target.Setting_Key = Source.Setting_Key
WHEN MATCHED THEN
    UPDATE SET 
        Setting_Value = Source.Setting_Value,
        Default_Value = Source.Default_Value,
        Data_Type = Source.Data_Type,
        Category = Source.Category,
        Description = Source.Description,
        Legal_Documents_Url = Source.Legal_Documents_Url
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Setting_Key, Setting_Value, Default_Value, Data_Type, Category, Description, Legal_Documents_Url, Updated_At)
    VALUES (Source.Setting_Key, Source.Setting_Value, Source.Default_Value, Source.Data_Type, Source.Category, Source.Description, Source.Legal_Documents_Url, SYSDATETIME());

PRINT N'    Đã đồng bộ thành công Master Data cho SystemSettings.';
GO
