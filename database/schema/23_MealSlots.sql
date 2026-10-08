-- ============================================================================
-- Table: MealSlots (Junction Table)
-- Cluster: Kho Công thức Chay, Thực đơn & Đi chợ
-- Description: Phân bổ từng bữa ăn (Sáng, Trưa, Tối, Phụ) trong kế hoạch thực đơn.
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MealSlots]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MealSlots] (
        [Slot_ID]               INT IDENTITY(1,1) NOT NULL,
        [Plan_ID]               INT NOT NULL,
        [Recipe_ID]             INT NOT NULL,
        [Day_Of_Week]           VARCHAR(10) NOT NULL,
        [Meal_Type]             VARCHAR(20) NOT NULL,
        [Is_Completed]          BIT NOT NULL CONSTRAINT [DF_MealSlots_Is_Completed] DEFAULT (0),
        [Notes]                 NVARCHAR(200) NULL,

        CONSTRAINT [PK_MealSlots] PRIMARY KEY CLUSTERED ([Slot_ID] ASC),
        CONSTRAINT [FK_MealSlots_MealPlans] FOREIGN KEY ([Plan_ID]) REFERENCES [dbo].[MealPlans]([Plan_ID]) ON DELETE CASCADE,
        CONSTRAINT [FK_MealSlots_Recipes] FOREIGN KEY ([Recipe_ID]) REFERENCES [dbo].[Recipes]([Recipe_ID]),
        CONSTRAINT [CK_MealSlots_Day] CHECK ([Day_Of_Week] IN ('MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT', 'SUN')),
        CONSTRAINT [CK_MealSlots_Type] CHECK ([Meal_Type] IN ('BREAKFAST', 'LUNCH', 'DINNER', 'SNACK'))
    );

    CREATE NONCLUSTERED INDEX [IX_MealSlots_Plan_ID] ON [dbo].[MealSlots]([Plan_ID] ASC);
END;
GO
