-- ============================================================
-- File: 02_Seed_Allergens.sql
-- Thư mục: database/seed/
-- Mục đích: Nạp sẵn từ điển các nhóm chất gây dị ứng thực phẩm theo tiêu chuẩn FDA Top 9 & Bộ Y tế
--           (BR-USR-07, BR-SCI-04).
-- Tính chất: Idempotent - chạy nhiều lần không trùng dữ liệu (MERGE).
-- ============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'>> [Seed 02/08] Đang nạp dữ liệu Master Nhóm Dị Ứng (Allergens)...';

SET NOCOUNT ON;

DECLARE @Now DATETIME2(7) = SYSDATETIME();

MERGE INTO dbo.Allergens AS target
USING (
    VALUES
        (
            N'Đậu nành (Đỗ tương)',
            'SOY',
            N'Dị ứng protein đậu nành và các chế phẩm như đậu phụ, tương bần, chao, xì dầu, dầu đậu nành.'
        ),
        (
            N'Gluten (Lúa mì & Ngũ cốc)',
            'GLUTEN',
            N'Protein gluten có trong lúa mì, lúa mạch, bánh mì, mì căn (seitan), bột mì đa dụng.'
        ),
        (
            N'Đậu phộng (Lạc)',
            'PEANUT',
            N'Dị ứng protein đậu phộng và các chế phẩm như bơ đậu phộng, dầu lạc ép thủ công.'
        ),
        (
            N'Quả hạch (Hạt cây)',
            'TREE_NUT',
            N'Dị ứng các loại hạt vỏ cứng: hạt óc chó, hạt điều, hạnh nhân, hạt dẻ cười, mắc ca, hồ đào.'
        ),
        (
            N'Sữa & Chế phẩm sữa động vật',
            'DAIRY',
            N'Dị ứng đạm sữa bò (casein, whey) hoặc hội chứng bất dung nạp đường sữa lactose.'
        ),
        (
            N'Trứng gia cầm',
            'EGG',
            N'Dị ứng protein ovalbumin/ovomucoid trong lòng trắng và lòng đỏ trứng gà, trứng vịt, trứng cút.'
        ),
        (
            N'Mè (Vừng)',
            'SESAME',
            N'Dị ứng protein hạt mè đen, mè trắng, dầu mè thơm, sốt tahini mè rang.'
        ),
        (
            N'Động vật giáp xác (Tôm, Cua)',
            'CRUSTACEAN',
            N'Dị ứng protein tropomyosin trong tôm, cua, ghẹ (áp dụng kiểm soát trường phái ăn chay bán phần).'
        ),
        (
            N'Cá (Cá biển & Cá nước ngọt)',
            'FISH',
            N'Dị ứng protein parvalbumin trong các loại cá (áp dụng kiểm soát trường phái Pescatarian).'
        ),
        (
            N'Sunfit (Chất bảo quản Sulfites)',
            'SULFITES',
            N'Chất bảo quản lưu huỳnh có nồng độ > 10mg/kg trong hoa quả sấy khô, rượu vang, giấm ăn.'
        ),
        (
            N'Cần tây',
            'CELERY',
            N'Dị ứng cần tây củ, cần tây cọng lá tươi, hạt cần tây và bột nêm cần tây.'
        ),
        (
            N'Mù tạt (Mustard)',
            'MUSTARD',
            N'Dị ứng hạt mù tạt, bột mù tạt, sốt mù tạt vàng Dijon, mù tạt xanh wasabi.'
        ),
        (
            N'Đậu Lupin (Lupine)',
            'LUPIN',
            N'Protein họ đậu lupin thường dùng thay thế trong bột làm bánh mì, mì ống không gluten.'
        ),
        (
            N'Động vật thân mềm (Mực, Bạch tuộc, Ốc)',
            'MOLLUSCS',
            N'Dị ứng các loài thân mềm: mực, bạch tuộc, sò điệp, ngao, hến (dành cho trường phái bán phần).'
        )
) AS source (Allergen_Name, Code, Description)
ON (target.Code = source.Code)
WHEN MATCHED THEN
    UPDATE SET 
        target.Allergen_Name = source.Allergen_Name,
        target.Description = source.Description,
        target.Updated_At = @Now
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Allergen_Name, Code, Description, Created_At, Updated_At)
    VALUES (source.Allergen_Name, source.Code, source.Description, @Now, @Now);

PRINT N'   -> Hoàn tất nạp 14 nhóm Dị Ứng thành công!';
GO
