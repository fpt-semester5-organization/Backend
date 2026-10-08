-- ============================================================
-- File: 30_UserScanHistory.sql
-- Bảng Thực Thể: UserScanHistory (3.27) [Lịch Sử Quét Mã Vạch Sản Phẩm]
-- Mục đích nghiệp vụ: Lưu trữ nhật ký quét mã vạch sản phẩm đóng gói, kết quả phát hiện nguyên liệu không thuần chay (hỗ trợ cả Member và Khách Guest).
-- ============================================================

CREATE TABLE dbo.UserScanHistory (
    Scan_ID BIGINT IDENTITY(1,1) NOT NULL,
    User_ID INT NULL,
    Guest_UUID VARCHAR(36) NULL,
    Product_ID INT NOT NULL,
    Scan_Method VARCHAR(25) NOT NULL,
    Is_Non_Vegan_Triggered BIT NOT NULL DEFAULT 0,
    Scanned_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_UserScanHistory PRIMARY KEY CLUSTERED (Scan_ID),
    CONSTRAINT FK_UserScanHistory_Users FOREIGN KEY (User_ID) 
        REFERENCES dbo.Users(User_ID) ON DELETE SET NULL,
    CONSTRAINT FK_UserScanHistory_Products FOREIGN KEY (Product_ID) 
        REFERENCES dbo.Products(Product_ID) ON DELETE CASCADE,
    CONSTRAINT CK_UserScanHistory_Scan_Method CHECK (Scan_Method IN ('CAMERA_BARCODE', 'MANUAL_INPUT')),
    CONSTRAINT CK_UserScanHistory_Owner CHECK (User_ID IS NOT NULL OR Guest_UUID IS NOT NULL)
);
GO
