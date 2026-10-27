-- ============================================================
-- File: 39_SystemSettings.sql
-- Bảng Thực Thể: SystemSettings (3.39)
-- Mục đích nghiệp vụ: Lưu trữ các tham số cấu hình động toàn hệ thống (Hạn mức đổi món, hạn mức AI, ngưỡng ngân sách...)
-- Phục vụ quản trị tập trung linh hoạt từ Admin Dashboard theo chuẩn Enterprise.
-- ============================================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SystemSettings]') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.SystemSettings (
        Setting_Key VARCHAR(60) NOT NULL,
        Setting_Value NVARCHAR(500) NOT NULL,
        Default_Value NVARCHAR(500) NOT NULL,
        Data_Type VARCHAR(20) NOT NULL CONSTRAINT DF_SystemSettings_Data_Type DEFAULT 'INT',
        Category VARCHAR(30) NOT NULL,
        Description NVARCHAR(255) NULL,
        Updated_At DATETIME2(7) NOT NULL CONSTRAINT DF_SystemSettings_Updated_At DEFAULT SYSDATETIME(),
        Updated_By INT NULL,

        CONSTRAINT PK_SystemSettings PRIMARY KEY CLUSTERED (Setting_Key),
        CONSTRAINT FK_SystemSettings_Users FOREIGN KEY (Updated_By) 
            REFERENCES dbo.Users(User_ID) ON DELETE SET NULL,
        CONSTRAINT CK_SystemSettings_Data_Type CHECK (Data_Type IN ('INT', 'DECIMAL', 'BOOLEAN', 'STRING')),
        CONSTRAINT CK_SystemSettings_Category CHECK (Category IN ('MEAL_PLAN', 'AI_QUOTA', 'LLMOPS', 'COMMUNITY', 'GENERAL'))
    );
END;
