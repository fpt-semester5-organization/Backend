-- ============================================================================
-- Table: Recipes
-- Cluster: Kho Công thức Chay, Thực đơn & Đi chợ
-- Description: Kho công thức nấu ăn thuần chay chuẩn hóa, định lượng dinh dưỡng calo/macro.
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Recipes]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Recipes] (
        [Recipe_ID]             INT IDENTITY(1,1) NOT NULL,
        [User_ID]               INT NULL,
        [Category_ID]           INT NOT NULL,
        [Dietary_School_ID]     INT NOT NULL,
        [Title]                 NVARCHAR(150) NOT NULL,
        [Description]           NVARCHAR(1000) NULL,
        [Instructions]          NVARCHAR(MAX) NOT NULL,
        [Prep_Time_Minutes]     INT NOT NULL CONSTRAINT [DF_Recipes_Prep_Time] DEFAULT (15),
        [Cook_Time_Minutes]     INT NOT NULL CONSTRAINT [DF_Recipes_Cook_Time] DEFAULT (20),
        [Servings]              INT NOT NULL CONSTRAINT [DF_Recipes_Servings] DEFAULT (2),
        [Calories_Kcal]         DECIMAL(6,1) NOT NULL,
        [Difficulty_Level]      VARCHAR(10) NOT NULL CONSTRAINT [DF_Recipes_Difficulty] DEFAULT ('MEDIUM'),
        [Is_Verified]           BIT NOT NULL CONSTRAINT [DF_Recipes_Is_Verified] DEFAULT (0),
        [Image_Url]             VARCHAR(255) NULL,
        [Status]                VARCHAR(20) NOT NULL CONSTRAINT [DF_Recipes_Status] DEFAULT ('DRAFT'),
        [View_Count]            INT NOT NULL CONSTRAINT [DF_Recipes_View_Count] DEFAULT (0),
        [Source]                VARCHAR(10) NOT NULL CONSTRAINT [DF_Recipes_Source] DEFAULT ('USER'),
        [Created_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_Recipes_Created_At] DEFAULT (SYSDATETIME()),
        [Updated_At]            DATETIME2(7) NULL,

        CONSTRAINT [PK_Recipes] PRIMARY KEY CLUSTERED ([Recipe_ID] ASC),
        CONSTRAINT [FK_Recipes_FoodCategories] FOREIGN KEY ([Category_ID]) REFERENCES [dbo].[FoodCategories]([Category_ID]),
        CONSTRAINT [CK_Recipes_Difficulty_Level] CHECK ([Difficulty_Level] IN ('EASY', 'MEDIUM', 'HARD')),
        CONSTRAINT [CK_Recipes_Status] CHECK ([Status] IN ('DRAFT', 'PENDING_REVIEW', 'APPROVED_ACTIVE', 'REJECTED', 'QUARANTINED_HIDDEN', 'ARCHIVED', 'DELETED')),
        CONSTRAINT [CK_Recipes_Source] CHECK ([Source] IN ('USER', 'AI', 'SYSTEM'))
    );

    CREATE NONCLUSTERED INDEX [IX_Recipes_Category_ID] ON [dbo].[Recipes]([Category_ID] ASC);
    CREATE NONCLUSTERED INDEX [IX_Recipes_Dietary_School_ID] ON [dbo].[Recipes]([Dietary_School_ID] ASC);
    CREATE NONCLUSTERED INDEX [IX_Recipes_Status] ON [dbo].[Recipes]([Status] ASC);

    -- Foreign Key to Users if table exists
    IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[Recipes] ADD CONSTRAINT [FK_Recipes_Users] FOREIGN KEY ([User_ID]) REFERENCES [dbo].[Users]([User_ID]);
    END;

    -- Foreign Key to DietarySchools if table exists
    IF OBJECT_ID(N'[dbo].[DietarySchools]', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[Recipes] ADD CONSTRAINT [FK_Recipes_DietarySchools] FOREIGN KEY ([Dietary_School_ID]) REFERENCES [dbo].[DietarySchools]([Dietary_School_ID]);
    END;

    -- Foreign Key from Posts to Recipes
    IF OBJECT_ID(N'[dbo].[Posts]', N'U') IS NOT NULL AND OBJECT_ID(N'[dbo].[FK_Posts_Recipes]', N'F') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Posts] ADD CONSTRAINT [FK_Posts_Recipes] FOREIGN KEY ([Recipe_ID]) REFERENCES [dbo].[Recipes]([Recipe_ID]);
    END;
END;
GO
