-- ============================================================
-- File: 05_UserWeightHistory.sql
-- Bảng Thực Thể: UserWeightHistory (3.4)
-- Mục đích nghiệp vụ: Lưu trữ lịch sử theo dõi biến thiên cân nặng theo thời gian thực (BR-USR-02).
-- ============================================================

CREATE TABLE dbo.UserWeightHistory (
    Log_ID BIGINT IDENTITY(1,1) NOT NULL,
    User_ID INT NOT NULL,
    Weight_Kg DECIMAL(5,2) NOT NULL,
    Logged_Date DATE NOT NULL,
    Note NVARCHAR(255) NULL,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_UserWeightHistory PRIMARY KEY CLUSTERED (Log_ID),
    CONSTRAINT FK_UserWeightHistory_Users FOREIGN KEY (User_ID) 
        REFERENCES dbo.Users(User_ID)
);
