-- ============================================================================
-- File: 44_SystemSettings_Add_LegalDocuments.sql
-- Table: SystemSettings (Alteration & Data Seeding)
-- Cluster: Quản trị Hệ thống, Tham số & Kiểm toán (Cluster 6)
-- Description: Bổ sung thuộc tính Legal_Documents_Url và lưu trữ các đường dẫn
--              Cloudinary của 4 tài liệu pháp lý (Legal documents) vào bảng SystemSettings.
-- ============================================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- 1. Thêm thuộc tính (cột) Legal_Documents_Url vào bảng SystemSettings nếu chưa tồn tại
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[SystemSettings]')
      AND name = 'Legal_Documents_Url'
)
BEGIN
    ALTER TABLE [dbo].[SystemSettings]
    ADD [Legal_Documents_Url] NVARCHAR(MAX) NULL;
END;
GO

-- 2. Mở rộng độ dài Setting_Value và Default_Value lên NVARCHAR(2000) để chứa JSON cấu hình đầy đủ
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[SystemSettings]')
      AND name = 'Setting_Value'
      AND max_length = 1000 -- 500 characters in NVARCHAR = 1000 bytes
)
BEGIN
    ALTER TABLE [dbo].[SystemSettings] ALTER COLUMN [Setting_Value] NVARCHAR(2000) NOT NULL;
    ALTER TABLE [dbo].[SystemSettings] ALTER COLUMN [Default_Value] NVARCHAR(2000) NOT NULL;
END;
GO

-- 3. Nạp / Đồng bộ cấu hình URL tài liệu pháp lý (định dạng ảnh PNG chất lượng cao) vào bảng SystemSettings
MERGE INTO dbo.SystemSettings AS Target
USING (VALUES
    (
        'LEGAL_DOCUMENTS',
        N'{"user_agreement":"https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Ban_Cam_Ket_Va_Thoa_Thuan_Dang_Ky_Nguoi_Dung.png","privacy_policy":"https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Chinh_Sach_Bao_Mat_Du_Lieu_VeggieLife.png","terms_of_service":"https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Dieu_Khoan_Su_Dung_Dich_Vu_VeggieLife.png","vegan_disclaimer":"https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Quy_Chuan_Am_Thuc_Chay_Va_Mien_Tru_Y_Te_VeggieLife.png"}',
        N'{"user_agreement":"https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Ban_Cam_Ket_Va_Thoa_Thuan_Dang_Ky_Nguoi_Dung.png","privacy_policy":"https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Chinh_Sach_Bao_Mat_Du_Lieu_VeggieLife.png","terms_of_service":"https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Dieu_Khoan_Su_Dung_Dich_Vu_VeggieLife.png","vegan_disclaimer":"https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Quy_Chuan_Am_Thuc_Chay_Va_Mien_Tru_Y_Te_VeggieLife.png"}',
        'STRING',
        'GENERAL',
        N'Tài liệu pháp lý VeggieLife (Điều khoản, Chính sách bảo mật, Cam kết người dùng, Quy chuẩn ẩm thực chay)',
        N'{"user_agreement":"https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Ban_Cam_Ket_Va_Thoa_Thuan_Dang_Ky_Nguoi_Dung.png","privacy_policy":"https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Chinh_Sach_Bao_Mat_Du_Lieu_VeggieLife.png","terms_of_service":"https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Dieu_Khoan_Su_Dung_Dich_Vu_VeggieLife.png","vegan_disclaimer":"https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Quy_Chuan_Am_Thuc_Chay_Va_Mien_Tru_Y_Te_VeggieLife.png"}'
    ),
    (
        'LEGAL_USER_AGREEMENT_URL',
        N'https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Ban_Cam_Ket_Va_Thoa_Thuan_Dang_Ky_Nguoi_Dung.png',
        N'https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Ban_Cam_Ket_Va_Thoa_Thuan_Dang_Ky_Nguoi_Dung.png',
        'STRING',
        'GENERAL',
        N'Bản Cam Kết Và Thỏa Thuận Đăng Ký Người Dùng',
        N'https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Ban_Cam_Ket_Va_Thoa_Thuan_Dang_Ky_Nguoi_Dung.png'
    ),
    (
        'LEGAL_PRIVACY_POLICY_URL',
        N'https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Chinh_Sach_Bao_Mat_Du_Lieu_VeggieLife.png',
        N'https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Chinh_Sach_Bao_Mat_Du_Lieu_VeggieLife.png',
        'STRING',
        'GENERAL',
        N'Chính Sách Bảo Mật Dữ Liệu VeggieLife',
        N'https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Chinh_Sach_Bao_Mat_Du_Lieu_VeggieLife.png'
    ),
    (
        'LEGAL_TERMS_OF_SERVICE_URL',
        N'https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Dieu_Khoan_Su_Dung_Dich_Vu_VeggieLife.png',
        N'https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Dieu_Khoan_Su_Dung_Dich_Vu_VeggieLife.png',
        'STRING',
        'GENERAL',
        N'Điều Khoản Sử Dụng Dịch Vụ VeggieLife',
        N'https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Dieu_Khoan_Su_Dung_Dich_Vu_VeggieLife.png'
    ),
    (
        'LEGAL_VEGAN_DISCLAIMER_URL',
        N'https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Quy_Chuan_Am_Thuc_Chay_Va_Mien_Tru_Y_Te_VeggieLife.png',
        N'https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Quy_Chuan_Am_Thuc_Chay_Va_Mien_Tru_Y_Te_VeggieLife.png',
        'STRING',
        'GENERAL',
        N'Quy Chuẩn Ẩm Thực Chay Và Miễn Trừ Y Tế VeggieLife',
        N'https://res.cloudinary.com/swrsuenp/image/upload/veggielife/legal/Quy_Chuan_Am_Thuc_Chay_Va_Mien_Tru_Y_Te_VeggieLife.png'
    )
) AS Source (Setting_Key, Setting_Value, Default_Value, Data_Type, Category, Description, Legal_Documents_Url)
ON Target.Setting_Key = Source.Setting_Key
WHEN MATCHED THEN
    UPDATE SET 
        Setting_Value = Source.Setting_Value,
        Default_Value = Source.Default_Value,
        Data_Type = Source.Data_Type,
        Category = Source.Category,
        Description = Source.Description,
        Legal_Documents_Url = Source.Legal_Documents_Url,
        Updated_At = SYSDATETIME()
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Setting_Key, Setting_Value, Default_Value, Data_Type, Category, Description, Legal_Documents_Url, Updated_At)
    VALUES (Source.Setting_Key, Source.Setting_Value, Source.Default_Value, Source.Data_Type, Source.Category, Source.Description, Source.Legal_Documents_Url, SYSDATETIME());
GO
