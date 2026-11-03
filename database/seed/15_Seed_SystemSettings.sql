-- 15_Seed_SystemSettings.sql: Seed bảng SystemSettings theo quy chuẩn VeggieLife
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

DECLARE @Now DATETIME2 = SYSUTCDATETIME();

-- Danh mục tài liệu pháp lý tổng hợp (JSON format)
MERGE dbo.SystemSettings AS Target
USING (SELECT 'LEGAL_DOCUMENTS' AS Setting_Key) AS Source
ON (Target.Setting_Key = Source.Setting_Key)
WHEN MATCHED THEN
    UPDATE SET 
        Setting_Value = '{"user_agreement":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718600/veggielife/legal/116_2025_QH15_666020.jpg","privacy_policy":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718345/veggielife/legal/13_2023_ND-CP_465185.jpg","terms_of_service":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718645/veggielife/legal/19_2023_QH15_500102.jpg","vegan_disclaimer":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718667/veggielife/legal/142_2026_ND-CP_696080.jpg","ai_regulation":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718667/veggielife/legal/142_2026_ND-CP_696080.jpg","info_security":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718553/veggielife/legal/85_2016_ND-CP_317475.jpg","fintech_sandbox":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718656/veggielife/legal/94_2025_ND-CP_584706.jpg"}',
        Default_Value = '{"user_agreement":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718600/veggielife/legal/116_2025_QH15_666020.jpg","privacy_policy":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718345/veggielife/legal/13_2023_ND-CP_465185.jpg","terms_of_service":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718645/veggielife/legal/19_2023_QH15_500102.jpg","vegan_disclaimer":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718667/veggielife/legal/142_2026_ND-CP_696080.jpg","ai_regulation":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718667/veggielife/legal/142_2026_ND-CP_696080.jpg","info_security":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718553/veggielife/legal/85_2016_ND-CP_317475.jpg","fintech_sandbox":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718656/veggielife/legal/94_2025_ND-CP_584706.jpg"}',
        Data_Type = 'STRING',
        Category = 'GENERAL',
        Description = N'Danh mục tài liệu pháp lý VeggieLife',
        Legal_Documents_Url = '{"user_agreement":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718600/veggielife/legal/116_2025_QH15_666020.jpg","privacy_policy":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718345/veggielife/legal/13_2023_ND-CP_465185.jpg","terms_of_service":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718645/veggielife/legal/19_2023_QH15_500102.jpg","vegan_disclaimer":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718667/veggielife/legal/142_2026_ND-CP_696080.jpg","ai_regulation":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718667/veggielife/legal/142_2026_ND-CP_696080.jpg","info_security":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718553/veggielife/legal/85_2016_ND-CP_317475.jpg","fintech_sandbox":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718656/veggielife/legal/94_2025_ND-CP_584706.jpg"}',
        Updated_At = @Now
WHEN NOT MATCHED THEN
    INSERT (Setting_Key, Setting_Value, Default_Value, Data_Type, Category, Description, Legal_Documents_Url, Updated_At)
    VALUES ('LEGAL_DOCUMENTS', '{"user_agreement":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718600/veggielife/legal/116_2025_QH15_666020.jpg","privacy_policy":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718345/veggielife/legal/13_2023_ND-CP_465185.jpg","terms_of_service":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718645/veggielife/legal/19_2023_QH15_500102.jpg","vegan_disclaimer":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718667/veggielife/legal/142_2026_ND-CP_696080.jpg","ai_regulation":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718667/veggielife/legal/142_2026_ND-CP_696080.jpg","info_security":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718553/veggielife/legal/85_2016_ND-CP_317475.jpg","fintech_sandbox":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718656/veggielife/legal/94_2025_ND-CP_584706.jpg"}', '{"user_agreement":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718600/veggielife/legal/116_2025_QH15_666020.jpg","privacy_policy":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718345/veggielife/legal/13_2023_ND-CP_465185.jpg","terms_of_service":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718645/veggielife/legal/19_2023_QH15_500102.jpg","vegan_disclaimer":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718667/veggielife/legal/142_2026_ND-CP_696080.jpg","ai_regulation":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718667/veggielife/legal/142_2026_ND-CP_696080.jpg","info_security":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718553/veggielife/legal/85_2016_ND-CP_317475.jpg","fintech_sandbox":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718656/veggielife/legal/94_2025_ND-CP_584706.jpg"}', 'STRING', 'GENERAL', N'Danh mục tài liệu pháp lý VeggieLife', '{"user_agreement":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718600/veggielife/legal/116_2025_QH15_666020.jpg","privacy_policy":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718345/veggielife/legal/13_2023_ND-CP_465185.jpg","terms_of_service":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718645/veggielife/legal/19_2023_QH15_500102.jpg","vegan_disclaimer":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718667/veggielife/legal/142_2026_ND-CP_696080.jpg","ai_regulation":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718667/veggielife/legal/142_2026_ND-CP_696080.jpg","info_security":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718553/veggielife/legal/85_2016_ND-CP_317475.jpg","fintech_sandbox":"https://res.cloudinary.com/swrsuenp/image/upload/v1793718656/veggielife/legal/94_2025_ND-CP_584706.jpg"}', @Now);

-- 1. Thỏa thuận người dùng (Luật An ninh mạng 116/2025/QH15)
MERGE dbo.SystemSettings AS Target
USING (SELECT 'LEGAL_USER_AGREEMENT_URL' AS Setting_Key) AS Source
ON (Target.Setting_Key = Source.Setting_Key)
WHEN MATCHED THEN
    UPDATE SET 
        Setting_Value = 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718600/veggielife/legal/116_2025_QH15_666020.jpg',
        Default_Value = 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718600/veggielife/legal/116_2025_QH15_666020.jpg',
        Data_Type = 'STRING',
        Category = 'GENERAL',
        Description = N'Thỏa thuận người dùng (Luật 116/2025/QH15)',
        Legal_Documents_Url = 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718600/veggielife/legal/116_2025_QH15_666020.jpg',
        Updated_At = @Now
WHEN NOT MATCHED THEN
    INSERT (Setting_Key, Setting_Value, Default_Value, Data_Type, Category, Description, Legal_Documents_Url, Updated_At)
    VALUES ('LEGAL_USER_AGREEMENT_URL', 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718600/veggielife/legal/116_2025_QH15_666020.jpg', 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718600/veggielife/legal/116_2025_QH15_666020.jpg', 'STRING', 'GENERAL', N'Thỏa thuận người dùng (Luật 116/2025/QH15)', 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718600/veggielife/legal/116_2025_QH15_666020.jpg', @Now);

-- 2. Chính sách bảo mật (Nghị định 13/2023/NĐ-CP)
MERGE dbo.SystemSettings AS Target
USING (SELECT 'LEGAL_PRIVACY_POLICY_URL' AS Setting_Key) AS Source
ON (Target.Setting_Key = Source.Setting_Key)
WHEN MATCHED THEN
    UPDATE SET 
        Setting_Value = 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718345/veggielife/legal/13_2023_ND-CP_465185.jpg',
        Default_Value = 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718345/veggielife/legal/13_2023_ND-CP_465185.jpg',
        Data_Type = 'STRING',
        Category = 'GENERAL',
        Description = N'Chính sách bảo mật dữ liệu (NĐ 13/2023/NĐ-CP)',
        Legal_Documents_Url = 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718345/veggielife/legal/13_2023_ND-CP_465185.jpg',
        Updated_At = @Now
WHEN NOT MATCHED THEN
    INSERT (Setting_Key, Setting_Value, Default_Value, Data_Type, Category, Description, Legal_Documents_Url, Updated_At)
    VALUES ('LEGAL_PRIVACY_POLICY_URL', 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718345/veggielife/legal/13_2023_ND-CP_465185.jpg', 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718345/veggielife/legal/13_2023_ND-CP_465185.jpg', 'STRING', 'GENERAL', N'Chính sách bảo mật dữ liệu (NĐ 13/2023/NĐ-CP)', 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718345/veggielife/legal/13_2023_ND-CP_465185.jpg', @Now);

-- 3. Điều khoản dịch vụ (Luật 19/2023/QH15)
MERGE dbo.SystemSettings AS Target
USING (SELECT 'LEGAL_TERMS_OF_SERVICE_URL' AS Setting_Key) AS Source
ON (Target.Setting_Key = Source.Setting_Key)
WHEN MATCHED THEN
    UPDATE SET 
        Setting_Value = 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718645/veggielife/legal/19_2023_QH15_500102.jpg',
        Default_Value = 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718645/veggielife/legal/19_2023_QH15_500102.jpg',
        Data_Type = 'STRING',
        Category = 'GENERAL',
        Description = N'Điều khoản dịch vụ & quyền lợi NTD (Luật 19/2023/QH15)',
        Legal_Documents_Url = 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718645/veggielife/legal/19_2023_QH15_500102.jpg',
        Updated_At = @Now
WHEN NOT MATCHED THEN
    INSERT (Setting_Key, Setting_Value, Default_Value, Data_Type, Category, Description, Legal_Documents_Url, Updated_At)
    VALUES ('LEGAL_TERMS_OF_SERVICE_URL', 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718645/veggielife/legal/19_2023_QH15_500102.jpg', 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718645/veggielife/legal/19_2023_QH15_500102.jpg', 'STRING', 'GENERAL', N'Điều khoản dịch vụ & quyền lợi NTD (Luật 19/2023/QH15)', 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718645/veggielife/legal/19_2023_QH15_500102.jpg', @Now);

-- 4. Quy chuẩn Trí tuệ nhân tạo (Nghị định 142/2026/NĐ-CP)
MERGE dbo.SystemSettings AS Target
USING (SELECT 'LEGAL_VEGAN_DISCLAIMER_URL' AS Setting_Key) AS Source
ON (Target.Setting_Key = Source.Setting_Key)
WHEN MATCHED THEN
    UPDATE SET 
        Setting_Value = 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718667/veggielife/legal/142_2026_ND-CP_696080.jpg',
        Default_Value = 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718667/veggielife/legal/142_2026_ND-CP_696080.jpg',
        Data_Type = 'STRING',
        Category = 'GENERAL',
        Description = N'Quy chuẩn Trí tuệ nhân tạo và Miễn trừ (NĐ 142/2026/NĐ-CP)',
        Legal_Documents_Url = 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718667/veggielife/legal/142_2026_ND-CP_696080.jpg',
        Updated_At = @Now
WHEN NOT MATCHED THEN
    INSERT (Setting_Key, Setting_Value, Default_Value, Data_Type, Category, Description, Legal_Documents_Url, Updated_At)
    VALUES ('LEGAL_VEGAN_DISCLAIMER_URL', 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718667/veggielife/legal/142_2026_ND-CP_696080.jpg', 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718667/veggielife/legal/142_2026_ND-CP_696080.jpg', 'STRING', 'GENERAL', N'Quy chuẩn Trí tuệ nhân tạo và Miễn trừ (NĐ 142/2026/NĐ-CP)', 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718667/veggielife/legal/142_2026_ND-CP_696080.jpg', @Now);

-- 5. An toàn thông tin (Nghị định 85/2016/NĐ-CP)
MERGE dbo.SystemSettings AS Target
USING (SELECT 'LEGAL_INFO_SECURITY_URL' AS Setting_Key) AS Source
ON (Target.Setting_Key = Source.Setting_Key)
WHEN MATCHED THEN
    UPDATE SET 
        Setting_Value = 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718553/veggielife/legal/85_2016_ND-CP_317475.jpg',
        Default_Value = 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718553/veggielife/legal/85_2016_ND-CP_317475.jpg',
        Data_Type = 'STRING',
        Category = 'GENERAL',
        Description = N'An toàn hệ thống thông tin theo cấp độ (NĐ 85/2016/NĐ-CP)',
        Legal_Documents_Url = 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718553/veggielife/legal/85_2016_ND-CP_317475.jpg',
        Updated_At = @Now
WHEN NOT MATCHED THEN
    INSERT (Setting_Key, Setting_Value, Default_Value, Data_Type, Category, Description, Legal_Documents_Url, Updated_At)
    VALUES ('LEGAL_INFO_SECURITY_URL', 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718553/veggielife/legal/85_2016_ND-CP_317475.jpg', 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718553/veggielife/legal/85_2016_ND-CP_317475.jpg', 'STRING', 'GENERAL', N'An toàn hệ thống thông tin theo cấp độ (NĐ 85/2016/NĐ-CP)', 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718553/veggielife/legal/85_2016_ND-CP_317475.jpg', @Now);

-- 6. Fintech Sandbox (Nghị định 94/2025/NĐ-CP)
MERGE dbo.SystemSettings AS Target
USING (SELECT 'LEGAL_FINTECH_SANDBOX_URL' AS Setting_Key) AS Source
ON (Target.Setting_Key = Source.Setting_Key)
WHEN MATCHED THEN
    UPDATE SET 
        Setting_Value = 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718656/veggielife/legal/94_2025_ND-CP_584706.jpg',
        Default_Value = 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718656/veggielife/legal/94_2025_ND-CP_584706.jpg',
        Data_Type = 'STRING',
        Category = 'GENERAL',
        Description = N'Thử nghiệm có kiểm soát ngân hàng (NĐ 94/2025/NĐ-CP)',
        Legal_Documents_Url = 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718656/veggielife/legal/94_2025_ND-CP_584706.jpg',
        Updated_At = @Now
WHEN NOT MATCHED THEN
    INSERT (Setting_Key, Setting_Value, Default_Value, Data_Type, Category, Description, Legal_Documents_Url, Updated_At)
    VALUES ('LEGAL_FINTECH_SANDBOX_URL', 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718656/veggielife/legal/94_2025_ND-CP_584706.jpg', 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718656/veggielife/legal/94_2025_ND-CP_584706.jpg', 'STRING', 'GENERAL', N'Thử nghiệm có kiểm soát ngân hàng (NĐ 94/2025/NĐ-CP)', 'https://res.cloudinary.com/swrsuenp/image/upload/v1793718656/veggielife/legal/94_2025_ND-CP_584706.jpg', @Now);
