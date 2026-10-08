-- ============================================================================
-- VeggieLifeDb - Master Schema Creation Script (17 Tables)
-- Clusters:
--   - Mạng xã hội Ẩm thực, Video & Kiểm duyệt (5 thực thể)
--   - Kho Công thức Chay, Thực đơn & Đi chợ (8 thực thể)
--   - Trợ lý Tư vấn AI, Tủ lạnh Ảo & Địa điểm (4 thực thể)
-- Target RDBMS: Microsoft SQL Server 2022
-- ============================================================================

USE [VeggieLifeDb];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
-- START: 11_FoodCategories.sql
-- ============================================================================
-- ============================================================================
-- Table: FoodCategories
-- Cluster: Kho Công thức Chay, Thực đơn & Đi chợ
-- Description: Danh mục phân loại trường phái ẩm thực và món ăn thuần chay.
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FoodCategories]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[FoodCategories] (
        [Category_ID]       INT IDENTITY(1,1) NOT NULL,
        [Category_Name]     NVARCHAR(100) NOT NULL,
        [Description]       NVARCHAR(500) NULL,
        [Is_Core]           BIT NOT NULL CONSTRAINT [DF_FoodCategories_Is_Core] DEFAULT (0),
        [Is_Active]         BIT NOT NULL CONSTRAINT [DF_FoodCategories_Is_Active] DEFAULT (1),
        [Created_At]        DATETIME2(7) NOT NULL CONSTRAINT [DF_FoodCategories_Created_At] DEFAULT (SYSDATETIME()),
        [Updated_At]        DATETIME2(7) NULL,

        CONSTRAINT [PK_FoodCategories] PRIMARY KEY CLUSTERED ([Category_ID] ASC)
    );

    CREATE UNIQUE NONCLUSTERED INDEX [UQ_FoodCategories_Category_Name] 
    ON [dbo].[FoodCategories]([Category_Name] ASC);
END;
GO
GO

-- ============================================================================
-- START: 12_Ingredients.sql
-- ============================================================================
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
GO

-- ============================================================================
-- START: 14_Posts.sql
-- ============================================================================
-- ============================================================================
-- Table: Posts
-- Cluster: Mạng xã hội Ẩm thực, Video & Kiểm duyệt
-- Description: Không gian bài viết chia sẻ công thức, kinh nghiệm sống thuần chay.
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Posts]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Posts] (
        [Post_ID]               INT IDENTITY(1,1) NOT NULL,
        [User_ID]               INT NOT NULL,
        [Recipe_ID]             INT NULL,
        [Title]                 NVARCHAR(150) NOT NULL,
        [Content]               NVARCHAR(MAX) NOT NULL,
        [Image_Url]             VARCHAR(255) NULL,
        [Visibility]            VARCHAR(20) NOT NULL CONSTRAINT [DF_Posts_Visibility] DEFAULT ('PUBLIC'),
        [View_Count]            INT NOT NULL CONSTRAINT [DF_Posts_View_Count] DEFAULT (0),
        [Moderation_Status]     VARCHAR(20) NOT NULL CONSTRAINT [DF_Posts_Moderation_Status] DEFAULT ('PENDING_REVIEW'),
        [Created_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_Posts_Created_At] DEFAULT (SYSDATETIME()),
        [Updated_At]            DATETIME2(7) NULL,

        CONSTRAINT [PK_Posts] PRIMARY KEY CLUSTERED ([Post_ID] ASC),
        CONSTRAINT [CK_Posts_Visibility] CHECK ([Visibility] IN ('PUBLIC', 'FOLLOWERS', 'PRIVATE')),
        CONSTRAINT [CK_Posts_Moderation_Status] CHECK ([Moderation_Status] IN ('DRAFT', 'PENDING_REVIEW', 'APPROVED_ACTIVE', 'REJECTED', 'QUARANTINED_HIDDEN', 'DELETED'))
    );

    CREATE NONCLUSTERED INDEX [IX_Posts_User_ID] ON [dbo].[Posts]([User_ID] ASC);
    CREATE NONCLUSTERED INDEX [IX_Posts_Moderation_Status] ON [dbo].[Posts]([Moderation_Status] ASC);

    -- Khóa ngoại Users
    IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[Posts] ADD CONSTRAINT [FK_Posts_Users] FOREIGN KEY ([User_ID]) REFERENCES [dbo].[Users]([User_ID]);
    END;
END;
GO
GO

-- ============================================================================
-- START: 15_Recipes.sql
-- ============================================================================
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
GO

-- ============================================================================
-- START: 16_RecipeIngredients.sql
-- ============================================================================
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
GO

-- ============================================================================
-- START: 17_Videos.sql
-- ============================================================================
-- ============================================================================
-- Table: Videos
-- Cluster: Mạng xã hội Ẩm thực, Video & Kiểm duyệt
-- Description: Quản lý siêu dữ liệu video hướng dẫn nấu ăn thuần chay.
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Videos]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Videos] (
        [Video_ID]              INT IDENTITY(1,1) NOT NULL,
        [Post_ID]               INT NOT NULL,
        [Title]                 NVARCHAR(150) NOT NULL,
        [Video_Url]             VARCHAR(255) NOT NULL,
        [Thumbnail_Url]         VARCHAR(255) NULL,
        [Duration_Seconds]      INT NOT NULL,
        [View_Count]            INT NOT NULL CONSTRAINT [DF_Videos_View_Count] DEFAULT (0),
        [Status]                VARCHAR(20) NOT NULL CONSTRAINT [DF_Videos_Status] DEFAULT ('PROCESSING'),
        [Created_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_Videos_Created_At] DEFAULT (SYSDATETIME()),
        [Updated_At]            DATETIME2(7) NULL,

        CONSTRAINT [PK_Videos] PRIMARY KEY CLUSTERED ([Video_ID] ASC),
        CONSTRAINT [FK_Videos_Posts] FOREIGN KEY ([Post_ID]) REFERENCES [dbo].[Posts]([Post_ID]),
        CONSTRAINT [CK_Videos_Status] CHECK ([Status] IN ('PROCESSING', 'READY', 'FAILED', 'BANNED'))
    );

    CREATE NONCLUSTERED INDEX [IX_Videos_Post_ID] ON [dbo].[Videos]([Post_ID] ASC);
END;
GO
GO

-- ============================================================================
-- START: 18_VideoTranscripts.sql
-- ============================================================================
-- ============================================================================
-- Table: VideoTranscripts
-- Cluster: Mạng xã hội Ẩm thực, Video & Kiểm duyệt
-- Description: Bóc tách văn bản thoại từ video (STT) và JSON công thức trích xuất.
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[VideoTranscripts]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[VideoTranscripts] (
        [Transcript_ID]         INT IDENTITY(1,1) NOT NULL,
        [Video_ID]              INT NOT NULL,
        [Recipe_ID]             INT NULL,
        [Raw_Transcript]        NVARCHAR(MAX) NULL,
        [Extracted_Recipe_JSON] NVARCHAR(MAX) NULL,
        [STT_Model_Version]     VARCHAR(50) NULL,
        [Status]                VARCHAR(20) NOT NULL CONSTRAINT [DF_VideoTranscripts_Status] DEFAULT ('PENDING'),
        [Created_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_VideoTranscripts_Created_At] DEFAULT (SYSDATETIME()),
        [Completed_At]          DATETIME2(7) NULL,

        CONSTRAINT [PK_VideoTranscripts] PRIMARY KEY CLUSTERED ([Transcript_ID] ASC),
        CONSTRAINT [FK_VideoTranscripts_Videos] FOREIGN KEY ([Video_ID]) REFERENCES [dbo].[Videos]([Video_ID]) ON DELETE CASCADE,
        CONSTRAINT [FK_VideoTranscripts_Recipes] FOREIGN KEY ([Recipe_ID]) REFERENCES [dbo].[Recipes]([Recipe_ID]),
        CONSTRAINT [CK_VideoTranscripts_Status] CHECK ([Status] IN ('PENDING', 'COMPLETED', 'FAILED'))
    );

    CREATE UNIQUE NONCLUSTERED INDEX [UQ_VideoTranscripts_Video_ID] ON [dbo].[VideoTranscripts]([Video_ID] ASC);
    CREATE NONCLUSTERED INDEX [IX_VideoTranscripts_Recipe_ID] ON [dbo].[VideoTranscripts]([Recipe_ID] ASC);
END;
GO
GO

-- ============================================================================
-- START: 19_Comments.sql
-- ============================================================================
-- ============================================================================
-- Table: Comments
-- Cluster: Mạng xã hội Ẩm thực, Video & Kiểm duyệt
-- Description: Bình luận thảo luận đa cấp (Parent_Comment_ID) trên bài viết.
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Comments]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Comments] (
        [Comment_ID]            INT IDENTITY(1,1) NOT NULL,
        [User_ID]               INT NOT NULL,
        [Parent_Comment_ID]     INT NULL,
        [Post_ID]               INT NULL,
        [Content]               NVARCHAR(MAX) NOT NULL,
        [Is_Soft_Deleted]       BIT NOT NULL CONSTRAINT [DF_Comments_Is_Soft_Deleted] DEFAULT (0),
        [Status]                VARCHAR(20) NOT NULL CONSTRAINT [DF_Comments_Status] DEFAULT ('ACTIVE'),
        [Created_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_Comments_Created_At] DEFAULT (SYSDATETIME()),
        [Updated_At]            DATETIME2(7) NULL,

        CONSTRAINT [PK_Comments] PRIMARY KEY CLUSTERED ([Comment_ID] ASC),
        CONSTRAINT [FK_Comments_Parent] FOREIGN KEY ([Parent_Comment_ID]) REFERENCES [dbo].[Comments]([Comment_ID]),
        CONSTRAINT [FK_Comments_Posts] FOREIGN KEY ([Post_ID]) REFERENCES [dbo].[Posts]([Post_ID]),
        CONSTRAINT [CK_Comments_Status] CHECK ([Status] IN ('ACTIVE', 'FLAGGED', 'QUARANTINED_HIDDEN'))
    );

    CREATE NONCLUSTERED INDEX [IX_Comments_Post_ID] ON [dbo].[Comments]([Post_ID] ASC);
    CREATE NONCLUSTERED INDEX [IX_Comments_Parent_Comment_ID] ON [dbo].[Comments]([Parent_Comment_ID] ASC);

    IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[Comments] ADD CONSTRAINT [FK_Comments_Users] FOREIGN KEY ([User_ID]) REFERENCES [dbo].[Users]([User_ID]);
    END;
END;
GO
GO

-- ============================================================================
-- START: 21_ViolationReports.sql
-- ============================================================================
-- ============================================================================
-- Table: ViolationReports
-- Cluster: Mạng xã hội Ẩm thực, Video & Kiểm duyệt
-- Description: Phiếu báo cáo vi phạm nội dung (thịt mặn, ngôn từ xúc phạm, spam).
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ViolationReports]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ViolationReports] (
        [Report_ID]             INT IDENTITY(1,1) NOT NULL,
        [Reporter_User_ID]      INT NULL,
        [Admin_ID]              INT NULL,
        [Recipe_ID]             INT NULL,
        [Comment_ID]            INT NULL,
        [Post_ID]               INT NULL,
        [Video_ID]              INT NULL,
        [Violation_Type]        VARCHAR(30) NOT NULL,
        [Evidence_Summary]      NVARCHAR(500) NOT NULL,
        [Report_Source]         VARCHAR(20) NOT NULL CONSTRAINT [DF_ViolationReports_Source] DEFAULT ('USER_REPORT'),
        [Status]                VARCHAR(20) NOT NULL CONSTRAINT [DF_ViolationReports_Status] DEFAULT ('PENDING'),
        [Created_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_ViolationReports_Created_At] DEFAULT (SYSDATETIME()),

        CONSTRAINT [PK_ViolationReports] PRIMARY KEY CLUSTERED ([Report_ID] ASC),
        CONSTRAINT [FK_ViolationReports_Recipes] FOREIGN KEY ([Recipe_ID]) REFERENCES [dbo].[Recipes]([Recipe_ID]),
        CONSTRAINT [FK_ViolationReports_Comments] FOREIGN KEY ([Comment_ID]) REFERENCES [dbo].[Comments]([Comment_ID]),
        CONSTRAINT [FK_ViolationReports_Posts] FOREIGN KEY ([Post_ID]) REFERENCES [dbo].[Posts]([Post_ID]),
        CONSTRAINT [FK_ViolationReports_Videos] FOREIGN KEY ([Video_ID]) REFERENCES [dbo].[Videos]([Video_ID]),
        CONSTRAINT [CK_ViolationReports_Type] CHECK ([Violation_Type] IN ('VIO_MEAT', 'VIO_LABEL', 'VIO_HATE', 'VIO_SPAM')),
        CONSTRAINT [CK_ViolationReports_Source] CHECK ([Report_Source] IN ('AI_SCAN', 'USER_REPORT')),
        CONSTRAINT [CK_ViolationReports_Status] CHECK ([Status] IN ('PENDING', 'APPROVE', 'REJECT'))
    );

    CREATE NONCLUSTERED INDEX [IX_ViolationReports_Status] ON [dbo].[ViolationReports]([Status] ASC);

    IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[ViolationReports] ADD CONSTRAINT [FK_ViolationReports_Reporter] FOREIGN KEY ([Reporter_User_ID]) REFERENCES [dbo].[Users]([User_ID]);
        ALTER TABLE [dbo].[ViolationReports] ADD CONSTRAINT [FK_ViolationReports_Admin] FOREIGN KEY ([Admin_ID]) REFERENCES [dbo].[Users]([User_ID]);
    END;
END;
GO
GO

-- ============================================================================
-- START: 22_MealPlans.sql
-- ============================================================================
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
GO

-- ============================================================================
-- START: 23_MealSlots.sql
-- ============================================================================
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
GO

-- ============================================================================
-- START: 24_ShoppingLists.sql
-- ============================================================================
-- ============================================================================
-- Table: ShoppingLists
-- Cluster: Kho Công thức Chay, Thực đơn & Đi chợ
-- Description: Danh sách đi chợ thông minh tổng hợp từ thực đơn tuần (Chế độ Online & PWA Offline Sync).
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ShoppingLists]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ShoppingLists] (
        [List_ID]               INT IDENTITY(1,1) NOT NULL,
        [Plan_ID]               INT NOT NULL,
        [List_Title]            NVARCHAR(150) NOT NULL,
        [Target_Shopping_Date]  DATE NOT NULL,
        [Status]                VARCHAR(20) NOT NULL CONSTRAINT [DF_ShoppingLists_Status] DEFAULT ('PENDING'),
        [Is_Offline_Synced]     BIT NOT NULL CONSTRAINT [DF_ShoppingLists_Is_Offline_Synced] DEFAULT (0),
        [Created_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_ShoppingLists_Created_At] DEFAULT (SYSDATETIME()),

        CONSTRAINT [PK_ShoppingLists] PRIMARY KEY CLUSTERED ([List_ID] ASC),
        CONSTRAINT [FK_ShoppingLists_MealPlans] FOREIGN KEY ([Plan_ID]) REFERENCES [dbo].[MealPlans]([Plan_ID]),
        CONSTRAINT [CK_ShoppingLists_Status] CHECK ([Status] IN ('PENDING', 'COMPLETED', 'CANCELLED'))
    );

    CREATE NONCLUSTERED INDEX [IX_ShoppingLists_Plan_ID] ON [dbo].[ShoppingLists]([Plan_ID] ASC);
END;
GO
GO

-- ============================================================================
-- START: 25_ShoppingListItems.sql
-- ============================================================================
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
GO

-- ============================================================================
-- START: 31_FridgeInventory.sql
-- ============================================================================
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
GO

-- ============================================================================
-- START: 32_VeganVenues.sql
-- ============================================================================
-- ============================================================================
-- Table: VeganVenues
-- Cluster: Trợ lý Tư vấn AI, Tủ lạnh Ảo & Địa điểm
-- Description: Bản đồ cơ sở ẩm thực chay (nhà hàng, quán ăn, buffet chay).
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[VeganVenues]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[VeganVenues] (
        [Venue_ID]              INT IDENTITY(1,1) NOT NULL,
        [External_Source]       VARCHAR(20) NOT NULL,
        [External_Place_ID]     VARCHAR(100) NULL,
        [Venue_Name]            NVARCHAR(150) NOT NULL,
        [Address]               NVARCHAR(255) NOT NULL,
        [Latitude]              DECIMAL(9,6) NOT NULL,
        [Longitude]             DECIMAL(9,6) NOT NULL,
        [Rating]                DECIMAL(2,1) NOT NULL CONSTRAINT [DF_VeganVenues_Rating] DEFAULT (0.0),
        [Thumbnail_Url]         VARCHAR(255) NULL,
        [Is_Fully_Vegan]        BIT NOT NULL CONSTRAINT [DF_VeganVenues_Is_Fully_Vegan] DEFAULT (1),
        [Is_Active]             BIT NOT NULL CONSTRAINT [DF_VeganVenues_Is_Active] DEFAULT (1),
        [Raw_Origin_JSON]       NVARCHAR(MAX) NULL,
        [Created_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_VeganVenues_Created_At] DEFAULT (SYSDATETIME()),

        CONSTRAINT [PK_VeganVenues] PRIMARY KEY CLUSTERED ([Venue_ID] ASC),
        CONSTRAINT [CK_VeganVenues_Source] CHECK ([External_Source] IN ('HAPPYCOW', 'GOOGLE_PLACES')),
        CONSTRAINT [CK_VeganVenues_Rating] CHECK ([Rating] BETWEEN 0.0 AND 5.0)
    );

    CREATE UNIQUE NONCLUSTERED INDEX [UQ_VeganVenues_External_Place_ID] 
    ON [dbo].[VeganVenues]([External_Place_ID] ASC) WHERE [External_Place_ID] IS NOT NULL;

    CREATE NONCLUSTERED INDEX [IX_VeganVenues_Coordinates] 
    ON [dbo].[VeganVenues]([Latitude] ASC, [Longitude] ASC);
END;
GO
GO

-- ============================================================================
-- START: 33_VenueMenuItems.sql
-- ============================================================================
-- ============================================================================
-- Table: VenueMenuItems
-- Cluster: Trợ lý Tư vấn AI, Tủ lạnh Ảo & Địa điểm
-- Description: Món ăn trong thực đơn của cơ sở ẩm thực chay.
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[VenueMenuItems]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[VenueMenuItems] (
        [Item_ID]               INT IDENTITY(1,1) NOT NULL,
        [Venue_ID]              INT NOT NULL,
        [Dish_Name]             NVARCHAR(150) NOT NULL,
        [Price_VND]             DECIMAL(12,0) NULL,
        [Description]           NVARCHAR(500) NULL,
        [Image_Url]             VARCHAR(255) NULL,
        [Is_Signature]          BIT NOT NULL CONSTRAINT [DF_VenueMenuItems_Is_Signature] DEFAULT (0),

        CONSTRAINT [PK_VenueMenuItems] PRIMARY KEY CLUSTERED ([Item_ID] ASC),
        CONSTRAINT [FK_VenueMenuItems_VeganVenues] FOREIGN KEY ([Venue_ID]) REFERENCES [dbo].[VeganVenues]([Venue_ID]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_VenueMenuItems_Venue_ID] ON [dbo].[VenueMenuItems]([Venue_ID] ASC);
END;
GO
GO

-- ============================================================================
-- START: 34_VenueCategories.sql
-- ============================================================================
-- ============================================================================
-- Table: VenueCategories (Junction Table M:N)
-- Cluster: Trợ lý Tư vấn AI, Tủ lạnh Ảo & Địa điểm
-- Description: Bảng nối phân loại ẩm thực của cơ sở (M:N với FoodCategories).
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[VenueCategories]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[VenueCategories] (
        [Category_ID]           INT NOT NULL,
        [Venue_ID]              INT NOT NULL,
        [Is_Primary]            BIT NOT NULL CONSTRAINT [DF_VenueCategories_Is_Primary] DEFAULT (0),
        [Assigned_At]           DATETIME2(7) NOT NULL CONSTRAINT [DF_VenueCategories_Assigned_At] DEFAULT (SYSDATETIME()),

        CONSTRAINT [PK_VenueCategories] PRIMARY KEY CLUSTERED ([Category_ID] ASC, [Venue_ID] ASC),
        CONSTRAINT [FK_VenueCategories_FoodCategories] FOREIGN KEY ([Category_ID]) REFERENCES [dbo].[FoodCategories]([Category_ID]),
        CONSTRAINT [FK_VenueCategories_VeganVenues] FOREIGN KEY ([Venue_ID]) REFERENCES [dbo].[VeganVenues]([Venue_ID]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_VenueCategories_Venue_ID] ON [dbo].[VenueCategories]([Venue_ID] ASC);
END;
GO
GO
