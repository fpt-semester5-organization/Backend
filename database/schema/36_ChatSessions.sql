-- ============================================================
-- File: 36_ChatSessions.sql
-- Bảng Thực Thể: ChatSessions (3.33)
-- Mục đích nghiệp vụ: Quản lý phiên hội thoại trợ lý AI tư vấn dinh dưỡng (hỗ trợ cả Member và Khách Guest).
-- ============================================================

CREATE TABLE dbo.ChatSessions (
    Chat_Session_ID BIGINT IDENTITY(1,1) NOT NULL,
    User_ID INT NULL,
    Profiles_ID INT NULL,
    Guest_UUID VARCHAR(36) NULL,
    Session_Title NVARCHAR(150) NOT NULL DEFAULT N'Hội thoại mới',
    Context_Type VARCHAR(30) NOT NULL DEFAULT 'NUTRITION_CONSULTATION',
    Started_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),
    Updated_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_ChatSessions PRIMARY KEY CLUSTERED (Chat_Session_ID),
    CONSTRAINT FK_ChatSessions_Users FOREIGN KEY (User_ID) 
        REFERENCES dbo.Users(User_ID) ON DELETE SET NULL,
    CONSTRAINT FK_ChatSessions_HealthProfiles FOREIGN KEY (Profiles_ID) 
        REFERENCES dbo.HealthProfiles(User_ID),
    CONSTRAINT CK_ChatSessions_Owner CHECK (User_ID IS NOT NULL OR Guest_UUID IS NOT NULL)
);
