-- ============================================================
-- File: 27_ENumberDictionary.sql
-- Bảng Thực Thể: ENumberDictionary (3.24)
-- Mục đích nghiệp vụ: Từ điển bách khoa phụ gia thực phẩm mã hiệu E-Number quốc tế, nguồn gốc hóa học và mức độ phù hợp ăn chay.
-- ============================================================

CREATE TABLE dbo.ENumberDictionary (
    Code VARCHAR(15) NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    Category_Type NVARCHAR(100) NULL,
    Origin_Source NVARCHAR(100) NOT NULL,
    Vegan_Status VARCHAR(20) NOT NULL,
    Health_Risk_Notes NVARCHAR(MAX) NULL,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),
    Updated_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_ENumberDictionary PRIMARY KEY CLUSTERED (Code),
    CONSTRAINT CK_ENumberDictionary_Vegan_Status CHECK (Vegan_Status IN ('VEGAN', 'NON_VEGAN', 'DOUBTFUL'))
);
