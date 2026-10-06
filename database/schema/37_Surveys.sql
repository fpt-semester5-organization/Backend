-- ============================================================================
-- Table: Surveys
-- Cluster: Trợ lý Tư vấn AI, Tủ lạnh Ảo & Địa điểm
-- Description: Khảo sát và đánh giá chất lượng phản hồi của AI (1 - 5 sao).
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Surveys]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Surveys] (
        [Survey_ID]             BIGINT IDENTITY(1,1) NOT NULL,
        [User_ID]               INT NOT NULL,
        [Session_ID]            BIGINT NULL,
        [Rating_Score]          INT NOT NULL,
        [Context_Question]      NVARCHAR(255) NOT NULL,
        [Feedback_Content]      NVARCHAR(MAX) NULL,
        [Created_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_Surveys_Created_At] DEFAULT (SYSDATETIME()),

        CONSTRAINT [PK_Surveys] PRIMARY KEY CLUSTERED ([Survey_ID] ASC),
        CONSTRAINT [FK_Surveys_ChatSessions] FOREIGN KEY ([Session_ID]) REFERENCES [dbo].[ChatSessions]([Chat_Session_ID]),
        CONSTRAINT [CK_Surveys_Rating_Score] CHECK ([Rating_Score] BETWEEN 1 AND 5)
    );

    CREATE NONCLUSTERED INDEX [IX_Surveys_User_ID] ON [dbo].[Surveys]([User_ID] ASC);

    IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[Surveys] ADD CONSTRAINT [FK_Surveys_Users] FOREIGN KEY ([User_ID]) REFERENCES [dbo].[Users]([User_ID]);
    END;
END;
GO
