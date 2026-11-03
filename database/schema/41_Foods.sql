-- ============================================================================
-- File: 41_Foods.sql
-- Table: Foods (3.41)
-- Cluster: Kho Công thức Chay, Thực đơn & Đi chợ
-- Description: Danh mục món ăn khái niệm (Food entity). Một Food có thể có nhiều Recipe
--              với các biến thể dinh dưỡng khác nhau (tăng cân, giảm cân, duy trì...).
-- ============================================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Foods]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Foods] (
        [Food_ID]       INT IDENTITY(1,1) NOT NULL,
        [Category_ID]   INT NOT NULL,
        [Food_Name]     NVARCHAR(150) NOT NULL,
        [Image_URL]     NVARCHAR(500) NULL,
        [Created_At]    DATETIME2(7) NOT NULL CONSTRAINT [DF_Foods_Created_At] DEFAULT (SYSDATETIME()),
        [Updated_At]    DATETIME2(7) NULL,

        CONSTRAINT [PK_Foods] PRIMARY KEY CLUSTERED ([Food_ID] ASC),
        CONSTRAINT [UQ_Foods_Food_Name] UNIQUE ([Food_Name] ASC)
    );

    CREATE NONCLUSTERED INDEX [IX_Foods_Category_ID] ON [dbo].[Foods]([Category_ID] ASC);
END;
GO

IF OBJECT_ID(N'[dbo].[FoodCategories]', N'U') IS NOT NULL AND OBJECT_ID(N'[dbo].[FK_Foods_FoodCategories]', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[Foods] ADD CONSTRAINT [FK_Foods_FoodCategories]
        FOREIGN KEY ([Category_ID]) REFERENCES [dbo].[FoodCategories]([Category_ID]);
END;
GO
