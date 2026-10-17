-- ============================================================================
-- Table: ShoppingListItems (Junction Table)
-- Cluster: Kho Công thức Chay, Thực đơn & Đi chợ
-- Description: Chi tiết từng món hàng cần mua sắm với số lượng cộng gộp.
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ShoppingListItems]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ShoppingListItems] (
        [Item_ID]               INT IDENTITY(1,1) NOT NULL,
        [List_ID]               INT NOT NULL,
        [Ingredient_ID]         INT NOT NULL,
        [Aggregated_Quantity]   DECIMAL(8,2) NOT NULL,
        [Unit]                  NVARCHAR(20) NOT NULL,
        [Is_Purchased]          BIT NOT NULL CONSTRAINT [DF_ShoppingListItems_Is_Purchased] DEFAULT (0),

        CONSTRAINT [PK_ShoppingListItems] PRIMARY KEY CLUSTERED ([Item_ID] ASC),
        CONSTRAINT [FK_ShoppingListItems_ShoppingLists] FOREIGN KEY ([List_ID]) REFERENCES [dbo].[ShoppingLists]([List_ID]) ON DELETE CASCADE,
        CONSTRAINT [FK_ShoppingListItems_Ingredients] FOREIGN KEY ([Ingredient_ID]) REFERENCES [dbo].[Ingredients]([Ingredient_ID])
    );

    CREATE NONCLUSTERED INDEX [IX_ShoppingListItems_List_ID] ON [dbo].[ShoppingListItems]([List_ID] ASC);
END;
GO
