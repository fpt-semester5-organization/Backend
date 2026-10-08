-- ============================================================================
-- Table: Ingredients
-- Cluster: Kho Công thức Chay, Thực đơn & Đi chợ
-- Description: Từ điển nguyên liệu thực phẩm chuẩn hóa, định mức calo cơ sở trên 100g.
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Ingredients]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Ingredients] (
        [Ingredient_ID]         INT IDENTITY(1,1) NOT NULL,
        [Ingredient_Name]       NVARCHAR(100) NOT NULL,
        [Standard_Unit]         NVARCHAR(20) NOT NULL CONSTRAINT [DF_Ingredients_Standard_Unit] DEFAULT (N'g'),
        [Default_Stall_Type]    VARCHAR(30) NOT NULL,
        [Calories_Per_100g]     DECIMAL(6,1) NOT NULL,
        [Is_Allium]             BIT NOT NULL CONSTRAINT [DF_Ingredients_Is_Allium] DEFAULT (0),
        [Food_Group]            VARCHAR(20) NOT NULL CONSTRAINT [DF_Ingredients_Food_Group] DEFAULT ('PLANT'),
        [Created_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_Ingredients_Created_At] DEFAULT (SYSDATETIME()),

        CONSTRAINT [PK_Ingredients] PRIMARY KEY CLUSTERED ([Ingredient_ID] ASC),
        CONSTRAINT [CK_Ingredients_Stall_Type] CHECK ([Default_Stall_Type] IN ('PRODUCE', 'CONDIMENTS', 'DRY_GOODS', 'PLANT_PROTEIN')),
        CONSTRAINT [CK_Ingredients_Food_Group] CHECK ([Food_Group] IN ('PLANT', 'EGG', 'DAIRY', 'POULTRY', 'SEAFOOD'))
    );

    CREATE UNIQUE NONCLUSTERED INDEX [UQ_Ingredients_Ingredient_Name] 
    ON [dbo].[Ingredients]([Ingredient_Name] ASC);
END;
GO
