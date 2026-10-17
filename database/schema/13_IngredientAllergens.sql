-- ============================================================
-- File: 13_IngredientAllergens.sql
-- Bảng Thực Thể: IngredientAllergens (3.38) [Bảng Trung Gian Phân Rã M:N Ingredients - Allergens]
-- Mục đích nghiệp vụ: Quản lý mối quan hệ giữa nguyên liệu thực phẩm và nhóm dị ứng (BR-USR-07, BR-SCI-04).
-- ============================================================

CREATE TABLE dbo.IngredientAllergens (
    Ingredient_ID INT NOT NULL,
    Allergen_ID INT NOT NULL,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_IngredientAllergens PRIMARY KEY CLUSTERED (Ingredient_ID, Allergen_ID),
    CONSTRAINT FK_IngredientAllergens_Allergens FOREIGN KEY (Allergen_ID) 
        REFERENCES dbo.Allergens(Allergen_ID) ON DELETE CASCADE
);

CREATE NONCLUSTERED INDEX IX_IngredientAllergens_Allergen_ID ON dbo.IngredientAllergens(Allergen_ID);
GO

-- Khóa ngoại tới Ingredients nếu bảng đã tồn tại
IF OBJECT_ID('dbo.Ingredients', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IngredientAllergens_Ingredients')
    BEGIN
        ALTER TABLE dbo.IngredientAllergens
        ADD CONSTRAINT FK_IngredientAllergens_Ingredients 
            FOREIGN KEY (Ingredient_ID) REFERENCES dbo.Ingredients(Ingredient_ID) ON DELETE CASCADE;
    END
END;
GO
