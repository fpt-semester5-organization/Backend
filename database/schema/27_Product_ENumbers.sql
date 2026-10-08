-- ============================================================
-- File: 27_Product_ENumbers.sql
-- Bảng Thực Thể: Product_ENumbers (3.25) [Bảng Trung Gian Phân Rã M:N Products - ENumberDictionary]
-- Mục đích nghiệp vụ: Lưu trữ các phụ gia thực phẩm mã số E-number phát hiện trong sản phẩm đóng gói.
-- ============================================================

CREATE TABLE dbo.Product_ENumbers (
    Product_ID INT NOT NULL,
    Code VARCHAR(15) NOT NULL,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_Product_ENumbers PRIMARY KEY CLUSTERED (Product_ID, Code),
    CONSTRAINT FK_Product_ENumbers_Products FOREIGN KEY (Product_ID) 
        REFERENCES dbo.Products(Product_ID) ON DELETE CASCADE,
    CONSTRAINT FK_Product_ENumbers_ENumberDictionary FOREIGN KEY (Code) 
        REFERENCES dbo.ENumberDictionary(Code) ON DELETE CASCADE
);
GO
