-- ============================================================
-- File: 01_Seed_DietarySchools.sql
-- Thư mục: database/seed/
-- Mục đích: Nạp sẵn dữ liệu master 07 trường phái ăn chay hệ thống
--           kèm quy tắc cấm thực phẩm chuẩn hóa dạng JSON (BR-ADM-MAS-001, D109).
-- Tính chất: Idempotent - chạy nhiều lần không trùng dữ liệu (MERGE).
-- ============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'>> [Seed 01/08] Đang nạp dữ liệu Master 07 Trường Phái Ăn Chay (DietarySchools)...';

SET NOCOUNT ON;

DECLARE @Now DATETIME2(7) = SYSDATETIME();

MERGE INTO dbo.DietarySchools AS target
USING (
    VALUES
        (
            N'Thuần chay (Vegan)',
            'VEGAN',
            N'{"forbiddenFoodGroups":["EGG","DAIRY","POULTRY","SEAFOOD"],"forbidAllium":false,"description":"Kiêng hoàn toàn tất cả thịt động vật, gia cầm, cá, hải sản, trứng, sữa và các chế phẩm từ động vật."}'
        ),
        (
            N'Chay có trứng (Ovo-Vegetarian)',
            'OVO',
            N'{"forbiddenFoodGroups":["DAIRY","POULTRY","SEAFOOD"],"forbidAllium":false,"description":"Kiêng thịt, gia cầm, cá, hải sản và sữa; cho phép tiêu thụ trứng gia cầm."}'
        ),
        (
            N'Chay có sữa (Lacto-Vegetarian)',
            'LACTO',
            N'{"forbiddenFoodGroups":["EGG","POULTRY","SEAFOOD"],"forbidAllium":false,"description":"Kiêng thịt, gia cầm, cá, hải sản và trứng; cho phép tiêu thụ sữa và các chế phẩm từ sữa."}'
        ),
        (
            N'Chay có trứng và sữa (Lacto-Ovo Vegetarian)',
            'LACTO_OVO',
            N'{"forbiddenFoodGroups":["POULTRY","SEAFOOD"],"forbidAllium":false,"description":"Kiêng thịt gia súc, gia cầm và hải sản; cho phép tiêu thụ cả trứng và sữa."}'
        ),
        (
            N'Chay Phật giáo (Buddhist Vegetarian)',
            'BUDDHIST',
            N'{"forbiddenFoodGroups":["EGG","DAIRY","POULTRY","SEAFOOD"],"forbidAllium":true,"description":"Kiêng hoàn toàn thịt động vật, trứng, sữa và nghiêm cấm ngũ vị tân (hành, tỏi, kiệu, hẹ, nén/boa-rô)."}'
        ),
        (
            N'Ăn chay bán phần - Gia cầm (Pollotarian)',
            'POLLOTARIAN',
            N'{"forbiddenFoodGroups":["SEAFOOD"],"forbidAllium":false,"description":"Kiêng thịt đỏ và hải sản; cho phép tiêu thụ thịt gia cầm (gà, vịt) bên cạnh thực vật, trứng và sữa."}'
        ),
        (
            N'Ăn chay bán phần - Hải sản (Pescatarian)',
            'PESCATARIAN',
            N'{"forbiddenFoodGroups":["POULTRY"],"forbidAllium":false,"description":"Kiêng thịt gia súc và gia cầm; cho phép tiêu thụ cá, tôm, hải sản bên cạnh thực vật, trứng và sữa."}'
        )
) AS source (School_Name, School_Code, Forbidden_Food_Rules)
ON (target.School_Code = source.School_Code)
WHEN MATCHED THEN
    UPDATE SET 
        target.School_Name = source.School_Name,
        target.Forbidden_Food_Rules = source.Forbidden_Food_Rules,
        target.Updated_At = @Now
WHEN NOT MATCHED BY TARGET THEN
    INSERT (School_Name, School_Code, Forbidden_Food_Rules, Created_At, Updated_At)
    VALUES (source.School_Name, source.School_Code, source.Forbidden_Food_Rules, @Now, @Now);

PRINT N'   -> Hoàn tất nạp 07 Trường Phái Ăn Chay thành công!';
GO
