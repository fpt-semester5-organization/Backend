-- ============================================================
-- File: 02_Allergens.sql
-- Bảng Thực Thể: Allergens (3.7)
-- Mục đích nghiệp vụ: Từ điển chuẩn hóa danh mục các chất dễ gây dị ứng thực phẩm theo tiêu chuẩn FDA và Bộ Y tế.
-- ============================================================

CREATE TABLE dbo.Allergens (
    Allergen_ID INT IDENTITY(1,1) NOT NULL,
    Allergen_Name NVARCHAR(100) NOT NULL,
    Code NVARCHAR(50) NOT NULL,
    Description NVARCHAR(500) NULL,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),
    Updated_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_Allergens PRIMARY KEY CLUSTERED (Allergen_ID),
    CONSTRAINT UQ_Allergens_Name UNIQUE (Allergen_Name),
    CONSTRAINT UQ_Allergens_Code UNIQUE (Code)
);
