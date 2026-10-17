-- ============================================================================
-- Table: ViolationReports
-- Cluster: Mạng xã hội Ẩm thực, Video & Kiểm duyệt
-- Description: Phiếu báo cáo vi phạm nội dung (thịt mặn, ngôn từ xúc phạm, spam).
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ViolationReports]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ViolationReports] (
        [Report_ID]             INT IDENTITY(1,1) NOT NULL,
        [Reporter_User_ID]      INT NULL,
        [Admin_ID]              INT NULL,
        [Recipe_ID]             INT NULL,
        [Comment_ID]            INT NULL,
        [Post_ID]               INT NULL,
        [Video_ID]              INT NULL,
        [Violation_Type]        VARCHAR(30) NOT NULL,
        [Evidence_Summary]      NVARCHAR(500) NOT NULL,
        [Report_Source]         VARCHAR(20) NOT NULL CONSTRAINT [DF_ViolationReports_Source] DEFAULT ('USER_REPORT'),
        [Status]                VARCHAR(20) NOT NULL CONSTRAINT [DF_ViolationReports_Status] DEFAULT ('PENDING'),
        [Created_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_ViolationReports_Created_At] DEFAULT (SYSDATETIME()),

        CONSTRAINT [PK_ViolationReports] PRIMARY KEY CLUSTERED ([Report_ID] ASC),
        CONSTRAINT [FK_ViolationReports_Recipes] FOREIGN KEY ([Recipe_ID]) REFERENCES [dbo].[Recipes]([Recipe_ID]),
        CONSTRAINT [FK_ViolationReports_Comments] FOREIGN KEY ([Comment_ID]) REFERENCES [dbo].[Comments]([Comment_ID]),
        CONSTRAINT [FK_ViolationReports_Posts] FOREIGN KEY ([Post_ID]) REFERENCES [dbo].[Posts]([Post_ID]),
        CONSTRAINT [FK_ViolationReports_Videos] FOREIGN KEY ([Video_ID]) REFERENCES [dbo].[Videos]([Video_ID]),
        CONSTRAINT [CK_ViolationReports_Type] CHECK ([Violation_Type] IN ('VIO_MEAT', 'VIO_LABEL', 'VIO_HATE', 'VIO_SPAM')),
        CONSTRAINT [CK_ViolationReports_Source] CHECK ([Report_Source] IN ('AI_SCAN', 'USER_REPORT')),
        CONSTRAINT [CK_ViolationReports_Status] CHECK ([Status] IN ('PENDING', 'APPROVE', 'REJECT'))
    );

    CREATE NONCLUSTERED INDEX [IX_ViolationReports_Status] ON [dbo].[ViolationReports]([Status] ASC);

    IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[ViolationReports] ADD CONSTRAINT [FK_ViolationReports_Reporter] FOREIGN KEY ([Reporter_User_ID]) REFERENCES [dbo].[Users]([User_ID]);
        ALTER TABLE [dbo].[ViolationReports] ADD CONSTRAINT [FK_ViolationReports_Admin] FOREIGN KEY ([Admin_ID]) REFERENCES [dbo].[Users]([User_ID]);
    END;
END;
GO
