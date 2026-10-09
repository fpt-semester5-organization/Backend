-- ============================================================
-- File: 05_Seed_IngredientAllergens.sql
-- Thư mục: database/seed/
-- Mục đích: Ánh xạ nguyên liệu thực phẩm với các nhóm chất gây dị ứng (BR-USR-07, BR-SCI-04).
-- Tính chất: Idempotent - chạy nhiều lần không trùng dữ liệu (MERGE).
-- ============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'>> [Seed 05/08] Đang nạp liên kết Nguyên Liệu - Nhóm Dị Ứng (IngredientAllergens)...';

SET NOCOUNT ON;

DECLARE @Now DATETIME2(7) = SYSDATETIME();

-- Tạo bảng tạm ánh xạ theo Tên Nguyên Liệu và Mã Dị Ứng
DECLARE @Mappings TABLE (
    IngredientName NVARCHAR(100),
    AllergenCode VARCHAR(50)
);

INSERT INTO @Mappings (IngredientName, AllergenCode)
VALUES
    -- Dị ứng Đậu nành (SOY)
    (N'Đậu hũ trắng (Tofu)', 'SOY'),
    (N'Đậu hũ chiên vàng', 'SOY'),
    (N'Đậu hũ non (Silken Tofu)', 'SOY'),
    (N'Tàu hũ ky lá tươi (Váng đậu tươi)', 'SOY'),
    (N'Tàu hũ ky khô', 'SOY'),
    (N'Tempeh (Đậu nành lên men Indonesia)', 'SOY'),
    (N'Sườn non chay khô (Đạm đậu nành TVP)', 'SOY'),
    (N'Bò lát chay khô (Đạm đậu nành TVP)', 'SOY'),
    (N'Đậu nành hạt khô', 'SOY'),
    (N'Nước tương đậu nành (Xì dầu)', 'SOY'),
    (N'Tương hột đậu nành lên men', 'SOY'),
    (N'Chao trắng béo ngậy', 'SOY'),
    (N'Chao đỏ nấu lẩu', 'SOY'),
    (N'Dầu đậu nành nguyên chất', 'SOY'),

    -- Dị ứng Gluten (GLUTEN)
    (N'Mì căn tươi (Seitan đạm lúa mì)', 'GLUTEN'),
    (N'Mì căn chiên giòn', 'GLUTEN'),
    (N'Chả lụa chay (từ đậu xanh & bột mì)', 'GLUTEN'),
    (N'Bột mì đa dụng', 'GLUTEN'),
    (N'Sườn non chay khô (Đạm đậu nành TVP)', 'GLUTEN'),

    -- Dị ứng Đậu phộng (PEANUT)
    (N'Đậu phộng hạt khô (Lạc)', 'PEANUT'),

    -- Dị ứng Quả hạch (TREE_NUT)
    (N'Hạt điều rang mộc', 'TREE_NUT'),
    (N'Hạt hạnh nhân Mỹ', 'TREE_NUT'),
    (N'Hạt óc chó nhân', 'TREE_NUT'),

    -- Dị ứng Mè / Vừng (SESAME)
    (N'Hạt mè trắng (Vừng trắng)', 'SESAME'),
    (N'Hạt mè đen (Vừng đen)', 'SESAME'),
    (N'Dầu mè thơm thượng hạng', 'SESAME'),

    -- Dị ứng Cần tây (CELERY)
    (N'Cần tây cọng tươi', 'CELERY'),

    -- Dị ứng Trứng (EGG)
    (N'Trứng gà ta tươi', 'EGG'),
    (N'Trứng cút tươi', 'EGG'),

    -- Dị ứng Sữa (DAIRY)
    (N'Sữa tươi tiệt trùng bò', 'DAIRY'),
    (N'Phô mai sợi Mozzarella', 'DAIRY'),
    (N'Bơ lạt từ sữa bò', 'DAIRY');

MERGE INTO dbo.IngredientAllergens AS target
USING (
    SELECT 
        i.Ingredient_ID,
        a.Allergen_ID
    FROM @Mappings m
    INNER JOIN dbo.Ingredients i ON i.Ingredient_Name = m.IngredientName
    INNER JOIN dbo.Allergens a ON a.Code = m.AllergenCode
) AS source (Ingredient_ID, Allergen_ID)
ON (target.Ingredient_ID = source.Ingredient_ID AND target.Allergen_ID = source.Allergen_ID)
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Ingredient_ID, Allergen_ID, Created_At)
    VALUES (source.Ingredient_ID, source.Allergen_ID, @Now);

PRINT N'   -> Hoàn tất nạp liên kết Nguyên Liệu - Dị Ứng thành công!';
GO
