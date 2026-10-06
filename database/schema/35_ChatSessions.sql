-- ============================================================================
-- Table: ChatSessions
-- Cluster: Trợ lý Tư vấn AI, Tủ lạnh Ảo & Địa điểm
-- Description: Phiên hội thoại tư vấn dinh dưỡng với Chatbot AI (User & Guest).
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ChatSessions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ChatSessions] (
        [Chat_Session_ID]       BIGINT IDENTITY(1,1) NOT NULL,
        [User_ID]               INT NULL,
        [Profiles_ID]           INT NULL,
        [Guest_UUID]            VARCHAR(36) NULL,
        [Session_Title]         NVARCHAR(150) NOT NULL CONSTRAINT [DF_ChatSessions_Title] DEFAULT (N'Hội thoại mới'),
        [Context_Type]          VARCHAR(30) NOT NULL CONSTRAINT [DF_ChatSessions_Context] DEFAULT ('NUTRITION_CONSULTATION'),
        [Started_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_ChatSessions_Started_At] DEFAULT (SYSDATETIME()),
        [Updated_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_ChatSessions_Updated_At] DEFAULT (SYSDATETIME()),

        CONSTRAINT [PK_ChatSessions] PRIMARY KEY CLUSTERED ([Chat_Session_ID] ASC),
        CONSTRAINT [CK_ChatSessions_User_Or_Guest] CHECK ([User_ID] IS NOT NULL OR [Guest_UUID] IS NOT NULL)
    );

    CREATE NONCLUSTERED INDEX [IX_ChatSessions_User_ID] ON [dbo].[ChatSessions]([User_ID] ASC);
    CREATE NONCLUSTERED INDEX [IX_ChatSessions_Guest_UUID] ON [dbo].[ChatSessions]([Guest_UUID] ASC) WHERE [Guest_UUID] IS NOT NULL;

    IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[ChatSessions] ADD CONSTRAINT [FK_ChatSessions_Users] FOREIGN KEY ([User_ID]) REFERENCES [dbo].[Users]([User_ID]);
    END;
END;
GO
