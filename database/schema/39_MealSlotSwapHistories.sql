-- ============================================================================
-- Table: MealSlotSwapHistories
-- Cluster: Kho Công thức Chay, Thực đơn & Đi chợ
-- Description: Lưu vết lịch sử đổi món từng bữa ăn trong kế hoạch thực đơn (SlotId, OldRecipeId, NewRecipeId, SwapReason, CaloriesDelta, SwappedAt).
-- ============================================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MealSlotSwapHistories]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MealSlotSwapHistories] (
        [Swap_History_ID]       INT IDENTITY(1,1) NOT NULL,
        [Slot_ID]               INT NOT NULL,
        [Old_Recipe_ID]         INT NOT NULL,
        [New_Recipe_ID]         INT NOT NULL,
        [Swap_Reason]           NVARCHAR(255) NULL,
        [Calories_Delta]        DECIMAL(6,1) NOT NULL,
        [Swapped_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_MealSlotSwapHistories_Swapped_At] DEFAULT (SYSDATETIME()),

        CONSTRAINT [PK_MealSlotSwapHistories] PRIMARY KEY CLUSTERED ([Swap_History_ID] ASC),
        CONSTRAINT [FK_MealSlotSwapHistories_MealSlots] FOREIGN KEY ([Slot_ID]) REFERENCES [dbo].[MealSlots]([Slot_ID]) ON DELETE CASCADE,
        CONSTRAINT [FK_MealSlotSwapHistories_OldRecipe] FOREIGN KEY ([Old_Recipe_ID]) REFERENCES [dbo].[Recipes]([Recipe_ID]),
        CONSTRAINT [FK_MealSlotSwapHistories_NewRecipe] FOREIGN KEY ([New_Recipe_ID]) REFERENCES [dbo].[Recipes]([Recipe_ID])
    );

    CREATE NONCLUSTERED INDEX [IX_MealSlotSwapHistories_Slot_ID] ON [dbo].[MealSlotSwapHistories]([Slot_ID] ASC);
END;
GO
