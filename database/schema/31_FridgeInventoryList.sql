-- ============================================================================
-- Table: FridgeInventoryList (Fridge_Inventory)
-- Cluster: Trợ lý Tư vấn AI, Tủ lạnh Ảo & Địa điểm
-- Description: Quản lý thực phẩm trong tủ lạnh ảo phục vụ thuật toán Empty the Fridge.
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FridgeInventoryList]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[FridgeInventoryList] (
        [Inventory_ID]          INT IDENTITY(1,1) NOT NULL,
        [User_ID]               INT NOT NULL,
        [Ingredient_ID]         INT NOT NULL,
        [Session_ID]            INT NULL,
        [Custom_Item_Name]      NVARCHAR(100) NOT NULL,
        [Freshness_Status]      VARCHAR(20) NOT NULL CONSTRAINT [DF_FridgeInventoryList_Freshness] DEFAULT ('FRESH'),
        [Created_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_FridgeInventoryList_Created_At] DEFAULT (SYSDATETIME()),

        CONSTRAINT [PK_FridgeInventoryList] PRIMARY KEY CLUSTERED ([Inventory_ID] ASC),
        CONSTRAINT [FK_FridgeInventoryList_Ingredients] FOREIGN KEY ([Ingredient_ID]) REFERENCES [dbo].[Ingredients]([Ingredient_ID]),
        CONSTRAINT [CK_FridgeInventoryList_Freshness] CHECK ([Freshness_Status] IN ('FRESH', 'NON-FRESH'))
    );

    CREATE NONCLUSTERED INDEX [IX_FridgeInventoryList_User_ID] ON [dbo].[FridgeInventoryList]([User_ID] ASC);

    IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[FridgeInventoryList] ADD CONSTRAINT [FK_FridgeInventoryList_Users] FOREIGN KEY ([User_ID]) REFERENCES [dbo].[Users]([User_ID]);
    END;
END;
GO
