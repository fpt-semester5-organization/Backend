-- ============================================================
-- File: 31_FridgeSessions.sql
-- Bảng Thực Thể: FridgeSessions (3.28)
-- Cluster: Trợ lý Tư vấn AI, Tủ lạnh Ảo & Địa điểm
-- Mục đích nghiệp vụ: Quản lý phiên chụp và phân tích hình ảnh tủ lạnh bằng Vision AI (BR-AI-FRIDGE-01).
-- ============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FridgeSessions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[FridgeSessions] (
        [Session_ID]            INT IDENTITY(1,1) NOT NULL,
        [User_ID]               INT NOT NULL,
        [Image_CDN_URL]         VARCHAR(255) NOT NULL,
        [Detected_Items_Count]  INT NOT NULL CONSTRAINT [DF_FridgeSessions_Items_Count] DEFAULT (0),
        [Created_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_FridgeSessions_Created_At] DEFAULT (SYSDATETIME()),

        CONSTRAINT [PK_FridgeSessions] PRIMARY KEY CLUSTERED ([Session_ID] ASC)
    );

    CREATE NONCLUSTERED INDEX [IX_FridgeSessions_User_ID] ON [dbo].[FridgeSessions]([User_ID] ASC);

    IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[FridgeSessions] ADD CONSTRAINT [FK_FridgeSessions_Users] FOREIGN KEY ([User_ID]) REFERENCES [dbo].[Users]([User_ID]);
    END;
END;
GO
