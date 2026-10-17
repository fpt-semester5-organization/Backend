-- ============================================================================
-- Table: FoodCategories
-- Cluster: Kho Công thức Chay, Thực đơn & Đi chợ
-- Description: Danh mục phân loại trường phái ẩm thực và món ăn thuần chay.
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FoodCategories]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[FoodCategories] (
        [Category_ID]       INT IDENTITY(1,1) NOT NULL,
        [Category_Name]     NVARCHAR(100) NOT NULL,
        [Description]       NVARCHAR(500) NULL,
        [Is_Core]           BIT NOT NULL CONSTRAINT [DF_FoodCategories_Is_Core] DEFAULT (0),
        [Is_Active]         BIT NOT NULL CONSTRAINT [DF_FoodCategories_Is_Active] DEFAULT (1),
        [Created_At]        DATETIME2(7) NOT NULL CONSTRAINT [DF_FoodCategories_Created_At] DEFAULT (SYSDATETIME()),
        [Updated_At]        DATETIME2(7) NULL,

        CONSTRAINT [PK_FoodCategories] PRIMARY KEY CLUSTERED ([Category_ID] ASC)
    );

    CREATE UNIQUE NONCLUSTERED INDEX [UQ_FoodCategories_Category_Name] 
    ON [dbo].[FoodCategories]([Category_Name] ASC);
END;
GO
