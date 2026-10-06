-- ============================================================================
-- Table: RecipeIngredients (Junction Table M:N)
-- Cluster: Kho Công thức Chay, Thực đơn & Đi chợ
-- Description: Định lượng nguyên liệu chi tiết cho từng công thức món ăn.
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RecipeIngredients]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RecipeIngredients] (
        [Recipe_ID]             INT NOT NULL,
        [Ingredient_ID]         INT NOT NULL,
        [Quantity_Needed]       DECIMAL(8,2) NOT NULL,
        [Unit]                  NVARCHAR(20) NOT NULL,
        [Preparation_Note]      NVARCHAR(150) NULL,
        [Is_Optional]           BIT NOT NULL CONSTRAINT [DF_RecipeIngredients_Is_Optional] DEFAULT (0),

        CONSTRAINT [PK_RecipeIngredients] PRIMARY KEY CLUSTERED ([Recipe_ID] ASC, [Ingredient_ID] ASC),
        CONSTRAINT [FK_RecipeIngredients_Recipes] FOREIGN KEY ([Recipe_ID]) REFERENCES [dbo].[Recipes]([Recipe_ID]) ON DELETE CASCADE,
        CONSTRAINT [FK_RecipeIngredients_Ingredients] FOREIGN KEY ([Ingredient_ID]) REFERENCES [dbo].[Ingredients]([Ingredient_ID])
    );

    CREATE NONCLUSTERED INDEX [IX_RecipeIngredients_Ingredient_ID] ON [dbo].[RecipeIngredients]([Ingredient_ID] ASC);
END;
GO
