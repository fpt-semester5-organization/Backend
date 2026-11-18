-- ============================================================================
-- Table: MealPlans
-- Cluster: Kho Công thức Chay, Thực đơn & Đi chợ
-- Description: Kế hoạch thực đơn dinh dưỡng 7 ngày được cá nhân hóa qua Gemini AI.
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MealPlans]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MealPlans] (
        [Plan_ID]               INT IDENTITY(1,1) NOT NULL,
        [User_ID]               INT NOT NULL,
        [Dietary_School_ID]     INT NOT NULL,
        [Plan_Name]             NVARCHAR(100) NOT NULL,
        [Start_Date]            DATE NOT NULL,
        [End_Date]              DATE NOT NULL,
        [Target_Daily_Kcal]     DECIMAL(6,1) NOT NULL,
        [Budget_Tier]           VARCHAR(12) NOT NULL CONSTRAINT [DF_MealPlans_Budget_Tier] DEFAULT ('STANDARD'),
        [Custom_Instructions]   NVARCHAR(500) NULL,
        [Health_Goal]           VARCHAR(30) NULL,
        [Region_Preference]     VARCHAR(20) NULL,
        [Use_Fridge]            BIT NOT NULL CONSTRAINT [DF_MealPlans_Use_Fridge] DEFAULT (0),
        [Requires_Allium_Free]  BIT NOT NULL CONSTRAINT [DF_MealPlans_Requires_Allium_Free] DEFAULT (0),
        [Plan_Status]           VARCHAR(20) NOT NULL CONSTRAINT [DF_MealPlans_Status] DEFAULT ('DRAFT'),
        [Generated_By_AI]       BIT NOT NULL CONSTRAINT [DF_MealPlans_Generated_By_AI] DEFAULT (0),
        [Max_Swaps_Allowed]     INT NOT NULL CONSTRAINT [DF_MealPlans_Max_Swaps] DEFAULT (21),
        [Swaps_Remaining]       INT NOT NULL CONSTRAINT [DF_MealPlans_Swaps_Remaining] DEFAULT (21),
        [Duration_Days]         INT NOT NULL CONSTRAINT [DF_MealPlans_Duration_Days] DEFAULT (7),
        [Created_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_MealPlans_Created_At] DEFAULT (SYSDATETIME()),

        CONSTRAINT [PK_MealPlans] PRIMARY KEY CLUSTERED ([Plan_ID] ASC),
        CONSTRAINT [CK_MealPlans_Budget_Tier] CHECK ([Budget_Tier] IN ('ECONOMICAL', 'STANDARD', 'PREMIUM')),
        CONSTRAINT [CK_MealPlans_Status] CHECK ([Plan_Status] IN ('DRAFT', 'ACTIVE', 'ARCHIVED', 'TEMPLATE', 'CANCELLED')),
        CONSTRAINT [CK_MealPlans_Health_Goal] CHECK ([Health_Goal] IS NULL OR [Health_Goal] IN ('LOSE_WEIGHT', 'MAINTAIN', 'GAIN_MUSCLE')),
        CONSTRAINT [CK_MealPlans_Region_Preference] CHECK ([Region_Preference] IS NULL OR [Region_Preference] IN ('NORTH', 'CENTRAL', 'SOUTH'))
    );

    CREATE NONCLUSTERED INDEX [IX_MealPlans_User_ID] ON [dbo].[MealPlans]([User_ID] ASC);

    IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[MealPlans] ADD CONSTRAINT [FK_MealPlans_Users] FOREIGN KEY ([User_ID]) REFERENCES [dbo].[Users]([User_ID]);
    END;

    IF OBJECT_ID(N'[dbo].[DietarySchools]', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[MealPlans] ADD CONSTRAINT [FK_MealPlans_DietarySchools] FOREIGN KEY ([Dietary_School_ID]) REFERENCES [dbo].[DietarySchools]([Dietary_School_ID]);
    END;
END;
GO
