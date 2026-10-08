-- ============================================================
-- File: 07_SubscriptionPlans.sql
-- Bảng Thực Thể: SubscriptionPlans (3.5)
-- Mục đích nghiệp vụ: Master Data lưu trữ danh mục các gói hội viên VIP trả phí, cấu hình giá, thời hạn và đặc quyền AI (BR-PAY-SUB-01).
-- ============================================================

CREATE TABLE dbo.SubscriptionPlans (
    Plan_ID INT IDENTITY(1,1) NOT NULL,
    Admin_ID INT NOT NULL,
    Plan_Name NVARCHAR(100) NOT NULL,
    Price DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    Duration_Days INT NOT NULL DEFAULT 30,
    Features_JSON NVARCHAR(MAX) NULL,
    Is_Active BIT NOT NULL DEFAULT 1,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),
    Updated_At DATETIME2(7) NULL,

    CONSTRAINT PK_SubscriptionPlans PRIMARY KEY CLUSTERED (Plan_ID),
    CONSTRAINT FK_SubscriptionPlans_Users FOREIGN KEY (Admin_ID) 
        REFERENCES dbo.Users(User_ID),
    CONSTRAINT UQ_SubscriptionPlans_Plan_Name UNIQUE (Plan_Name),
    CONSTRAINT CK_SubscriptionPlans_Price CHECK (Price >= 0.00),
    CONSTRAINT CK_SubscriptionPlans_Duration CHECK (Duration_Days > 0),
    CONSTRAINT CK_SubscriptionPlans_Features_JSON CHECK (Features_JSON IS NULL OR ISJSON(Features_JSON) = 1)
);
GO
