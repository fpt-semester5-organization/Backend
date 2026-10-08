-- ============================================================
-- File: 08_PaymentTransactions.sql
-- Bảng Thực Thể: PaymentTransactions (3.6) [Bảng giao điểm Users – SubscriptionPlans]
-- Mục đích nghiệp vụ: Lưu trữ nhật ký giao dịch tài chính mua gói VIP qua MoMo, VietQR, quản lý vòng đời thanh toán và đối soát (BR-PAY-TXN-01).
-- ============================================================

CREATE TABLE dbo.PaymentTransactions (
    Transaction_ID BIGINT IDENTITY(1,1) NOT NULL,
    Plan_ID INT NOT NULL,
    User_ID INT NOT NULL,
    Gateway_Provider VARCHAR(30) NOT NULL,
    Partner_Transaction_Code VARCHAR(100) NOT NULL,
    Amount_VND DECIMAL(12,2) NOT NULL,
    Transaction_Status VARCHAR(15) NOT NULL DEFAULT 'PENDING',
    Signature_Hash VARCHAR(256) NOT NULL,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),
    Completed_At DATETIME2(7) NULL,
    Receipt_URL VARCHAR(255) NULL,
    Starts_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),
    Ends_At DATETIME2(7) NOT NULL,

    CONSTRAINT PK_PaymentTransactions PRIMARY KEY CLUSTERED (Transaction_ID),
    CONSTRAINT FK_PaymentTransactions_SubscriptionPlans FOREIGN KEY (Plan_ID) 
        REFERENCES dbo.SubscriptionPlans(Plan_ID),
    CONSTRAINT FK_PaymentTransactions_Users FOREIGN KEY (User_ID) 
        REFERENCES dbo.Users(User_ID),
    CONSTRAINT UQ_PaymentTransactions_Partner_Code UNIQUE (Partner_Transaction_Code),
    CONSTRAINT CK_PaymentTransactions_Gateway CHECK (Gateway_Provider IN ('MOMO', 'VIETQR')),
    CONSTRAINT CK_PaymentTransactions_Amount CHECK (Amount_VND > 0),
    CONSTRAINT CK_PaymentTransactions_Status CHECK (Transaction_Status IN (
        'PENDING', 'SUCCESS', 'FAILED', 'CANCELLED', 'EXPIRED', 'REFUNDED'
    )),
    CONSTRAINT CK_PaymentTransactions_Period CHECK (Ends_At > Starts_At)
);
GO
