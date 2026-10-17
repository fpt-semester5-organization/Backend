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
        [Custom_Instructions]   NVARCHAR(200) NULL,
        [Plan_Status]           VARCHAR(20) NOT NULL CONSTRAINT [DF_MealPlans_Status] DEFAULT ('DRAFT'),
        [Generated_By_AI]       BIT NOT NULL CONSTRAINT [DF_MealPlans_Generated_By_AI] DEFAULT (0),
        [Created_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_MealPlans_Created_At] DEFAULT (SYSDATETIME()),

        CONSTRAINT [PK_MealPlans] PRIMARY KEY CLUSTERED ([Plan_ID] ASC),
        CONSTRAINT [CK_MealPlans_Budget_Tier] CHECK ([Budget_Tier] IN ('ECONOMICAL', 'STANDARD', 'PREMIUM')),
        CONSTRAINT [CK_MealPlans_Status] CHECK ([Plan_Status] IN ('DRAFT', 'ACTIVE', 'ARCHIVED', 'TEMPLATE'))
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
