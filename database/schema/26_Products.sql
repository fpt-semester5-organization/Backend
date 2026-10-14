-- ============================================================
-- File: 26_Products.sql
-- Bảng Thực Thể: Products (3.23)
-- Mục đích nghiệp vụ: Cơ sở dữ liệu sản phẩm đóng gói thương mại, mã vạch Barcode EAN-13/UPC và kết luận kiểm định chay.
-- ============================================================

CREATE TABLE dbo.Products (
    Product_ID INT IDENTITY(1,1) NOT NULL,
    Barcode VARCHAR(50) NOT NULL,
    Product_Name NVARCHAR(150) NOT NULL,
    Brand NVARCHAR(100) NULL,
    Ingredients_Text NVARCHAR(MAX) NULL,
    Image_Url VARCHAR(500) NULL,
    Vegan_Verdict VARCHAR(15) NOT NULL,
    Admin_Verified BIT NOT NULL DEFAULT 0,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),
    Last_Synced_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_Products PRIMARY KEY CLUSTERED (Product_ID),
    CONSTRAINT UQ_Products_Barcode UNIQUE (Barcode),
    CONSTRAINT CK_Products_Vegan_Verdict CHECK (Vegan_Verdict IN ('VEGAN', 'NON_VEGAN', 'DOUBTFUL'))
);
