-- ============================================================================
-- Table: Comments
-- Cluster: Mạng xã hội Ẩm thực, Video & Kiểm duyệt
-- Description: Bình luận thảo luận đa cấp (Parent_Comment_ID) trên bài viết.
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Comments]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Comments] (
        [Comment_ID]            INT IDENTITY(1,1) NOT NULL,
        [User_ID]               INT NOT NULL,
        [Parent_Comment_ID]     INT NULL,
        [Post_ID]               INT NULL,
        [Content]               NVARCHAR(MAX) NOT NULL,
        [Is_Soft_Deleted]       BIT NOT NULL CONSTRAINT [DF_Comments_Is_Soft_Deleted] DEFAULT (0),
        [Status]                VARCHAR(20) NOT NULL CONSTRAINT [DF_Comments_Status] DEFAULT ('ACTIVE'),
        [Created_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_Comments_Created_At] DEFAULT (SYSDATETIME()),
        [Updated_At]            DATETIME2(7) NULL,

        CONSTRAINT [PK_Comments] PRIMARY KEY CLUSTERED ([Comment_ID] ASC),
        CONSTRAINT [FK_Comments_Parent] FOREIGN KEY ([Parent_Comment_ID]) REFERENCES [dbo].[Comments]([Comment_ID]),
        CONSTRAINT [FK_Comments_Posts] FOREIGN KEY ([Post_ID]) REFERENCES [dbo].[Posts]([Post_ID]),
        CONSTRAINT [CK_Comments_Status] CHECK ([Status] IN ('ACTIVE', 'FLAGGED', 'QUARANTINED_HIDDEN'))
    );

    CREATE NONCLUSTERED INDEX [IX_Comments_Post_ID] ON [dbo].[Comments]([Post_ID] ASC);
    CREATE NONCLUSTERED INDEX [IX_Comments_Parent_Comment_ID] ON [dbo].[Comments]([Parent_Comment_ID] ASC);

    IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[Comments] ADD CONSTRAINT [FK_Comments_Users] FOREIGN KEY ([User_ID]) REFERENCES [dbo].[Users]([User_ID]);
    END;
END;
GO
