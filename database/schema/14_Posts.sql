-- ============================================================================
-- Table: Posts
-- Cluster: Mạng xã hội Ẩm thực, Video & Kiểm duyệt
-- Description: Không gian bài viết chia sẻ công thức, kinh nghiệm sống thuần chay.
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Posts]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Posts] (
        [Post_ID]               INT IDENTITY(1,1) NOT NULL,
        [User_ID]               INT NOT NULL,
        [Recipe_ID]             INT NULL,
        [Title]                 NVARCHAR(150) NOT NULL,
        [Content]               NVARCHAR(MAX) NOT NULL,
        [Image_Url]             VARCHAR(255) NULL,
        [Visibility]            VARCHAR(20) NOT NULL CONSTRAINT [DF_Posts_Visibility] DEFAULT ('PUBLIC'),
        [View_Count]            INT NOT NULL CONSTRAINT [DF_Posts_View_Count] DEFAULT (0),
        [Moderation_Status]     VARCHAR(20) NOT NULL CONSTRAINT [DF_Posts_Moderation_Status] DEFAULT ('PENDING_REVIEW'),
        [Created_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_Posts_Created_At] DEFAULT (SYSDATETIME()),
        [Updated_At]            DATETIME2(7) NULL,

        CONSTRAINT [PK_Posts] PRIMARY KEY CLUSTERED ([Post_ID] ASC),
        CONSTRAINT [CK_Posts_Visibility] CHECK ([Visibility] IN ('PUBLIC', 'FOLLOWERS', 'PRIVATE')),
        CONSTRAINT [CK_Posts_Moderation_Status] CHECK ([Moderation_Status] IN ('DRAFT', 'PENDING_REVIEW', 'APPROVED_ACTIVE', 'REJECTED', 'QUARANTINED_HIDDEN', 'SOFT_DELETED'))
    );

    CREATE NONCLUSTERED INDEX [IX_Posts_User_ID] ON [dbo].[Posts]([User_ID] ASC);
    CREATE NONCLUSTERED INDEX [IX_Posts_Moderation_Status] ON [dbo].[Posts]([Moderation_Status] ASC);

    -- Khóa ngoại Users
    IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[Posts] ADD CONSTRAINT [FK_Posts_Users] FOREIGN KEY ([User_ID]) REFERENCES [dbo].[Users]([User_ID]);
    END;
END;
GO
