-- ============================================================
-- File: 03_Users.sql
-- Bảng Thực Thể: Users (3.1)
-- Mục đích nghiệp vụ: Quản lý tài khoản, danh tính và phân quyền thành viên hệ thống.
-- ============================================================
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO


CREATE TABLE dbo.Users (
    User_ID INT IDENTITY(1,1) NOT NULL,
    Email VARCHAR(150) NOT NULL,
    Password_Hash NVARCHAR(MAX) NULL,
    Full_Name NVARCHAR(100) NOT NULL,
    Phone_Number VARCHAR(20) NULL,
    Avatar_Url VARCHAR(255) NULL,
    Role VARCHAR(20) NOT NULL DEFAULT 'MEMBER',
    Failed_Login_Count INT NOT NULL DEFAULT 0,
    Locked_Until DATETIME2(7) NULL,
    Last_Login_At DATETIME2(7) NULL,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),
    Updated_At DATETIME2(7) NULL,
    Google_ID VARCHAR(64) NULL,
    Account_Status VARCHAR(30) NOT NULL DEFAULT 'PENDING_ACTIVATION',
    Count_ViolationReports INT NOT NULL DEFAULT 0,

    CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (User_ID),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('MEMBER', 'VIP', 'ADMIN')),
    CONSTRAINT CK_Users_Account_Status CHECK (Account_Status IN (
        'PENDING_ACTIVATION', 'ACTIVE', 'QUARANTINED', 'SUSPENDED',
        'BANNED', 'SOFT_DELETED', 'ANONYMIZED_DELETED'
    ))
);
GO

-- Unique filtered index cho Phone_Number (áp dụng khi NOT NULL)
CREATE UNIQUE NONCLUSTERED INDEX UQ_Users_Phone_Number 
ON dbo.Users(Phone_Number) 
WHERE Phone_Number IS NOT NULL;
GO

-- Unique filtered index cho Google_ID (áp dụng khi NOT NULL - BR-USR-AUTH-02)
CREATE UNIQUE NONCLUSTERED INDEX UQ_Users_Google_ID 
ON dbo.Users(Google_ID) 
WHERE Google_ID IS NOT NULL;
GO
