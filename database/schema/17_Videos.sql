-- ============================================================================
-- Table: Videos
-- Cluster: Mạng xã hội Ẩm thực, Video & Kiểm duyệt
-- Description: Quản lý siêu dữ liệu video hướng dẫn nấu ăn thuần chay.
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Videos]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Videos] (
        [Video_ID]              INT IDENTITY(1,1) NOT NULL,
        [Post_ID]               INT NULL,
        [Recipe_ID]             INT NULL,
        [Title]                 NVARCHAR(150) NOT NULL,
        [Video_Url]             VARCHAR(255) NOT NULL,
        [Thumbnail_Url]         VARCHAR(255) NULL,
        [Duration_Seconds]      INT NOT NULL,
        [View_Count]            INT NOT NULL CONSTRAINT [DF_Videos_View_Count] DEFAULT (0),
        [Status]                VARCHAR(20) NOT NULL CONSTRAINT [DF_Videos_Status] DEFAULT ('PROCESSING'),
        [Created_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_Videos_Created_At] DEFAULT (SYSDATETIME()),
        [Updated_At]            DATETIME2(7) NULL,

        CONSTRAINT [PK_Videos] PRIMARY KEY CLUSTERED ([Video_ID] ASC),
        CONSTRAINT [FK_Videos_Posts] FOREIGN KEY ([Post_ID]) REFERENCES [dbo].[Posts]([Post_ID]),
        CONSTRAINT [FK_Videos_Recipes] FOREIGN KEY ([Recipe_ID]) REFERENCES [dbo].[Recipes]([Recipe_ID]),
        CONSTRAINT [CK_Videos_Target] CHECK ([Post_ID] IS NOT NULL OR [Recipe_ID] IS NOT NULL),
        CONSTRAINT [CK_Videos_Status] CHECK ([Status] IN ('PROCESSING', 'READY', 'FAILED', 'BANNED'))
    );

    CREATE NONCLUSTERED INDEX [IX_Videos_Post_ID] ON [dbo].[Videos]([Post_ID] ASC);
    CREATE NONCLUSTERED INDEX [IX_Videos_Recipe_ID] ON [dbo].[Videos]([Recipe_ID] ASC);
END;
GO
