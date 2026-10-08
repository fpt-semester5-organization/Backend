-- ============================================================================
-- Table: FridgeInventory
-- Cluster: Trợ lý Tư vấn AI, Tủ lạnh Ảo & Địa điểm
-- Description: Quản lý thực phẩm trong tủ lạnh ảo phục vụ thuật toán Empty the Fridge.
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FridgeInventory]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[FridgeInventory] (
        [Inventory_ID]          INT IDENTITY(1,1) NOT NULL,
        [User_ID]               INT NOT NULL,
        [Ingredient_ID]         INT NOT NULL,
        [Session_ID]            INT NULL,
        [Custom_Item_Name]      NVARCHAR(100) NOT NULL,
        [Freshness_Status]      VARCHAR(15) NOT NULL CONSTRAINT [DF_FridgeInventory_Freshness] DEFAULT ('FRESH'),

        CONSTRAINT [PK_FridgeInventory] PRIMARY KEY CLUSTERED ([Inventory_ID] ASC),
        CONSTRAINT [FK_FridgeInventory_Ingredients] FOREIGN KEY ([Ingredient_ID]) REFERENCES [dbo].[Ingredients]([Ingredient_ID]),
        CONSTRAINT [CK_FridgeInventory_Freshness] CHECK ([Freshness_Status] IN ('FRESH', 'NON-FRESH'))
    );

    CREATE NONCLUSTERED INDEX [IX_FridgeInventory_User_ID] ON [dbo].[FridgeInventory]([User_ID] ASC);

    IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[FridgeInventory] ADD CONSTRAINT [FK_FridgeInventory_Users] FOREIGN KEY ([User_ID]) REFERENCES [dbo].[Users]([User_ID]);
    END;

    IF OBJECT_ID(N'[dbo].[FridgeSessions]', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[FridgeInventory] ADD CONSTRAINT [FK_FridgeInventory_FridgeSessions] FOREIGN KEY ([Session_ID]) REFERENCES [dbo].[FridgeSessions]([Session_ID]);
    END;
END;
GO
