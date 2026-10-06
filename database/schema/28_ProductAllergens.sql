-- ============================================================
-- File: 28_ProductAllergens.sql
-- Bảng Thực Thể: ProductAllergens (3.26) [Bảng Trung Gian Phân Rã M:N Products - Allergens]
-- Mục đích nghiệp vụ: Lưu trữ các chất gây dị ứng thực phẩm có mặt hoặc có nguy cơ nhiễm chéo trong sản phẩm (FDA/BYT).
-- ============================================================

CREATE TABLE dbo.ProductAllergens (
    Product_ID INT NOT NULL,
    Allergen_ID INT NOT NULL,
    Presence_Type VARCHAR(15) NOT NULL,

    CONSTRAINT PK_ProductAllergens PRIMARY KEY CLUSTERED (Product_ID, Allergen_ID),
    CONSTRAINT FK_ProductAllergens_Products FOREIGN KEY (Product_ID) 
        REFERENCES dbo.Products(Product_ID) ON DELETE CASCADE,
    CONSTRAINT FK_ProductAllergens_Allergens FOREIGN KEY (Allergen_ID) 
        REFERENCES dbo.Allergens(Allergen_ID) ON DELETE CASCADE,
    CONSTRAINT CK_ProductAllergens_Presence CHECK (Presence_Type IN ('CONTAINS', 'MAY_CONTAIN'))
);
GO
