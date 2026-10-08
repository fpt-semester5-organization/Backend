-- ============================================================================
-- VeggieLifeDb - Master Schema Creation Script (38 Tables)
-- Clusters:
--   - Quản trị Tài khoản, Sức khỏe & Hội viên (9 thực thể)
--   - Mạng xã hội Ẩm thực, Video, Kiểm duyệt & Tương tác (7 thực thể)
--   - Kho Công thức Chay, Thực đơn, Nguyên liệu & Đi chợ (10 thực thể)
--   - Tra cứu Phụ gia, Quét Mã vạch & An toàn (5 thực thể)
--   - Trợ lý Tư vấn AI, Tủ lạnh Ảo & Địa điểm (7 thực thể)
-- Target RDBMS: Microsoft SQL Server 2022
-- ============================================================================

USE [VeggieLifeDb];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
-- START: 01_DietarySchools.sql
-- ============================================================================
-- ============================================================
-- File: 01_DietarySchools.sql
-- Bảng Thực Thể: DietarySchools (3.2)
-- Cluster: Kho Công thức Chay, Thực đơn & Đi chợ / Master Data
-- Mục đích nghiệp vụ: Master Data tri thức chuẩn hóa 07 trường phái ăn chay hệ thống kèm cờ quy tắc kiểm soát thực phẩm (BR-USR-01.1, BR-ADM-MAS-001).
-- ============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DietarySchools]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DietarySchools] (
        [Dietary_School_ID]     INT IDENTITY(1,1) NOT NULL,
        [School_Name]           NVARCHAR(100) NOT NULL,
        [School_Code]           VARCHAR(20) NOT NULL,
        [Forbidden_Food_Rules]  NVARCHAR(MAX) NOT NULL,
        [Updated_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_DietarySchools_Updated_At] DEFAULT (SYSDATETIME()),
        [Created_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_DietarySchools_Created_At] DEFAULT (SYSDATETIME()),

        CONSTRAINT [PK_DietarySchools] PRIMARY KEY CLUSTERED ([Dietary_School_ID] ASC),
        CONSTRAINT [UQ_DietarySchools_School_Code] UNIQUE ([School_Code] ASC),
        CONSTRAINT [CK_DietarySchools_Code] CHECK ([School_Code] IN ('VEGAN', 'OVO', 'LACTO', 'LACTO_OVO', 'BUDDHIST', 'POLLOTARIAN', 'PESCATARIAN')),
        CONSTRAINT [CK_DietarySchools_Rules_JSON] CHECK (ISJSON([Forbidden_Food_Rules]) = 1)
    );
END;
GO
GO


-- ============================================================================
-- START: 02_Allergens.sql
-- ============================================================================
-- ============================================================
-- File: 02_Allergens.sql
-- Bảng Thực Thể: Allergens (3.7)
-- Mục đích nghiệp vụ: Từ điển chuẩn hóa danh mục các chất dễ gây dị ứng thực phẩm theo tiêu chuẩn FDA và Bộ Y tế.
-- ============================================================

CREATE TABLE dbo.Allergens (
    Allergen_ID INT IDENTITY(1,1) NOT NULL,
    Allergen_Name NVARCHAR(100) NOT NULL,
    Code NVARCHAR(50) NOT NULL,
    Description NVARCHAR(500) NULL,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),
    Updated_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_Allergens PRIMARY KEY CLUSTERED (Allergen_ID),
    CONSTRAINT UQ_Allergens_Name UNIQUE (Allergen_Name),
    CONSTRAINT UQ_Allergens_Code UNIQUE (Code)
);
GO


-- ============================================================================
-- START: 03_Users.sql
-- ============================================================================
-- ============================================================
-- File: 03_Users.sql
-- Bảng Thực Thể: Users (3.1)
-- Mục đích nghiệp vụ: Quản lý tài khoản, danh tính và phân quyền thành viên hệ thống.
-- ============================================================
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO


CREATE TABLE dbo.Users (
    User_ID INT IDENTITY(1,1) NOT NULL,
    Email VARCHAR(150) NOT NULL,
    Password_Hash NVARCHAR(MAX) NULL,
    Full_Name NVARCHAR(100) NOT NULL,
    Phone_Number VARCHAR(20) NULL,
    Avatar_Url VARCHAR(255) NULL,
    Role VARCHAR(20) NOT NULL DEFAULT 'MEMBER',
    Failed_Login_Count INT NOT NULL DEFAULT 0,
    Locked_Until DATETIME2(7) NULL,
    Last_Login_At DATETIME2(7) NULL,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),
    Updated_At DATETIME2(7) NULL,
    Google_ID VARCHAR(64) NULL,
    Account_Status VARCHAR(30) NOT NULL DEFAULT 'PENDING_ACTIVATION',
    Count_ViolationReports INT NOT NULL DEFAULT 0,

    CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (User_ID),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('MEMBER', 'VIP', 'ADMIN')),
    CONSTRAINT CK_Users_Account_Status CHECK (Account_Status IN (
        'PENDING_ACTIVATION', 'ACTIVE', 'QUARANTINED', 'SUSPENDED',
        'BANNED', 'SOFT_DELETED', 'ANONYMIZED_DELETED'
    ))
);
GO

-- Unique filtered index cho Phone_Number (áp dụng khi NOT NULL)
CREATE UNIQUE NONCLUSTERED INDEX UQ_Users_Phone_Number 
ON dbo.Users(Phone_Number) 
WHERE Phone_Number IS NOT NULL;
GO

-- Unique filtered index cho Google_ID (áp dụng khi NOT NULL - BR-USR-AUTH-02)
CREATE UNIQUE NONCLUSTERED INDEX UQ_Users_Google_ID 
ON dbo.Users(Google_ID) 
WHERE Google_ID IS NOT NULL;
GO
GO


-- ============================================================================
-- START: 04_HealthProfiles.sql
-- ============================================================================
-- ============================================================
-- File: 04_HealthProfiles.sql
-- Bảng Thực Thể: HealthProfiles (3.3)
-- Mục đích nghiệp vụ: Hồ sơ thể trạng, chỉ số sinh học dinh dưỡng cá nhân hóa (BMR/TDEE/BMI).
-- ============================================================

CREATE TABLE dbo.HealthProfiles (
    User_ID INT NOT NULL,
    Dietary_School_ID INT NOT NULL,
    Gender VARCHAR(10) NOT NULL,
    Date_Of_Birth DATE NOT NULL,
    Height_Cm DECIMAL(5,1) NOT NULL,
    Current_Weight_Kg DECIMAL(5,1) NOT NULL,
    Target_Weight_Kg DECIMAL(5,1) NULL,
    BMI_Value DECIMAL(4,1) NOT NULL,
    BMR_Kcal DECIMAL(6,1) NOT NULL,
    TDEE_Kcal DECIMAL(6,1) NOT NULL,
    Target_Daily_Calories DECIMAL(6,1) NOT NULL,
    Requires_Allium_Free BIT NOT NULL DEFAULT 0,
    Region_Preference VARCHAR(20) NOT NULL DEFAULT 'SOUTH',
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),
    Updated_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),
    Is_Stale_Metrics BIT NOT NULL DEFAULT 0,
    HealthGoal VARCHAR(30) NOT NULL DEFAULT 'MAINTAIN',

    CONSTRAINT PK_HealthProfiles PRIMARY KEY CLUSTERED (User_ID),
    CONSTRAINT FK_HealthProfiles_Users FOREIGN KEY (User_ID) 
        REFERENCES dbo.Users(User_ID),
    CONSTRAINT CK_HealthProfiles_Gender CHECK (Gender IN ('MALE', 'FEMALE')),
    CONSTRAINT CK_HealthProfiles_Height CHECK (Height_Cm BETWEEN 50 AND 250),
    CONSTRAINT CK_HealthProfiles_Current_Weight CHECK (Current_Weight_Kg BETWEEN 20 AND 300),
    CONSTRAINT CK_HealthProfiles_HealthGoal CHECK (HealthGoal IN ('LOSE_WEIGHT', 'MAINTAIN', 'GAIN_MUSCLE'))
);

-- Khóa ngoại tới DietarySchools (bảng 01_DietarySchools)
IF OBJECT_ID('dbo.DietarySchools', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HealthProfiles_DietarySchools')
    BEGIN
        ALTER TABLE dbo.HealthProfiles
        ADD CONSTRAINT FK_HealthProfiles_DietarySchools 
            FOREIGN KEY (Dietary_School_ID) REFERENCES dbo.DietarySchools(Dietary_School_ID);
    END
END;
GO


-- ============================================================================
-- START: 05_UserWeightHistory.sql
-- ============================================================================
-- ============================================================
-- File: 05_UserWeightHistory.sql
-- Bảng Thực Thể: UserWeightHistory (3.4)
-- Mục đích nghiệp vụ: Lưu trữ lịch sử theo dõi biến thiên cân nặng theo thời gian thực (BR-USR-02).
-- ============================================================

CREATE TABLE dbo.UserWeightHistory (
    Log_ID BIGINT IDENTITY(1,1) NOT NULL,
    User_ID INT NOT NULL,
    Weight_Kg DECIMAL(5,2) NOT NULL,
    Logged_Date DATE NOT NULL,
    Note NVARCHAR(255) NULL,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_UserWeightHistory PRIMARY KEY CLUSTERED (Log_ID),
    CONSTRAINT FK_UserWeightHistory_Users FOREIGN KEY (User_ID) 
        REFERENCES dbo.Users(User_ID)
);
GO


-- ============================================================================
-- START: 06_UserAllergies.sql
-- ============================================================================
-- ============================================================
-- File: 06_UserAllergies.sql
-- Bảng Thực Thể: UserAllergies (3.8) [Bảng Trung Gian Phân Rã M:N Users - Allergens]
-- Mục đích nghiệp vụ: Lưu trữ danh mục chất dị ứng thực phẩm mà người dùng bị mẫn cảm phục vụ loại trừ món ăn an toàn (BR-SCI-04, BR-USR-07).
-- ============================================================

CREATE TABLE dbo.UserAllergies (
    User_ID INT NOT NULL,
    Allergen_ID INT NOT NULL,
    Notes NVARCHAR(255) NULL,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),
    Updated_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_UserAllergies PRIMARY KEY CLUSTERED (User_ID, Allergen_ID),
    CONSTRAINT FK_UserAllergies_Users FOREIGN KEY (User_ID) 
        REFERENCES dbo.Users(User_ID) ON DELETE CASCADE,
    CONSTRAINT FK_UserAllergies_Allergens FOREIGN KEY (Allergen_ID) 
        REFERENCES dbo.Allergens(Allergen_ID) ON DELETE CASCADE
);
GO
GO


-- ============================================================================
-- START: 07_SubscriptionPlans.sql
-- ============================================================================
-- ============================================================
-- File: 07_SubscriptionPlans.sql
-- Bảng Thực Thể: SubscriptionPlans (3.5)
-- Mục đích nghiệp vụ: Master Data lưu trữ danh mục các gói hội viên VIP trả phí, cấu hình giá, thời hạn và đặc quyền AI (BR-PAY-SUB-01).
-- ============================================================

CREATE TABLE dbo.SubscriptionPlans (
    Plan_ID INT IDENTITY(1,1) NOT NULL,
    Admin_ID INT NOT NULL,
    Plan_Name NVARCHAR(100) NOT NULL,
    Price DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    Duration_Days INT NOT NULL DEFAULT 30,
    Features_JSON NVARCHAR(MAX) NULL,
    Is_Active BIT NOT NULL DEFAULT 1,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),
    Updated_At DATETIME2(7) NULL,

    CONSTRAINT PK_SubscriptionPlans PRIMARY KEY CLUSTERED (Plan_ID),
    CONSTRAINT FK_SubscriptionPlans_Users FOREIGN KEY (Admin_ID) 
        REFERENCES dbo.Users(User_ID),
    CONSTRAINT UQ_SubscriptionPlans_Plan_Name UNIQUE (Plan_Name),
    CONSTRAINT CK_SubscriptionPlans_Price CHECK (Price >= 0.00),
    CONSTRAINT CK_SubscriptionPlans_Duration CHECK (Duration_Days > 0),
    CONSTRAINT CK_SubscriptionPlans_Features_JSON CHECK (Features_JSON IS NULL OR ISJSON(Features_JSON) = 1)
);
GO
GO


-- ============================================================================
-- START: 08_PaymentTransactions.sql
-- ============================================================================
-- ============================================================
-- File: 08_PaymentTransactions.sql
-- Bảng Thực Thể: PaymentTransactions (3.6) [Bảng giao điểm Users – SubscriptionPlans]
-- Mục đích nghiệp vụ: Lưu trữ nhật ký giao dịch tài chính mua gói VIP qua MoMo, VietQR, quản lý vòng đời thanh toán và đối soát (BR-PAY-TXN-01).
-- ============================================================

CREATE TABLE dbo.PaymentTransactions (
    Transaction_ID BIGINT IDENTITY(1,1) NOT NULL,
    Plan_ID INT NOT NULL,
    User_ID INT NOT NULL,
    Gateway_Provider VARCHAR(30) NOT NULL,
    Partner_Transaction_Code VARCHAR(100) NOT NULL,
    Amount_VND DECIMAL(12,2) NOT NULL,
    Transaction_Status VARCHAR(15) NOT NULL DEFAULT 'PENDING',
    Signature_Hash VARCHAR(256) NOT NULL,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),
    Completed_At DATETIME2(7) NULL,
    Receipt_URL VARCHAR(255) NULL,
    Starts_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),
    Ends_At DATETIME2(7) NOT NULL,

    CONSTRAINT PK_PaymentTransactions PRIMARY KEY CLUSTERED (Transaction_ID),
    CONSTRAINT FK_PaymentTransactions_SubscriptionPlans FOREIGN KEY (Plan_ID) 
        REFERENCES dbo.SubscriptionPlans(Plan_ID),
    CONSTRAINT FK_PaymentTransactions_Users FOREIGN KEY (User_ID) 
        REFERENCES dbo.Users(User_ID),
    CONSTRAINT UQ_PaymentTransactions_Partner_Code UNIQUE (Partner_Transaction_Code),
    CONSTRAINT CK_PaymentTransactions_Gateway CHECK (Gateway_Provider IN ('MOMO', 'VIETQR')),
    CONSTRAINT CK_PaymentTransactions_Amount CHECK (Amount_VND > 0),
    CONSTRAINT CK_PaymentTransactions_Status CHECK (Transaction_Status IN (
        'PENDING', 'SUCCESS', 'FAILED', 'CANCELLED', 'EXPIRED', 'REFUNDED'
    )),
    CONSTRAINT CK_PaymentTransactions_Period CHECK (Ends_At > Starts_At)
);
GO
GO


-- ============================================================================
-- START: 09_AIQuotaLogs.sql
-- ============================================================================
-- ============================================================
-- File: 09_AIQuotaLogs.sql
-- Bảng Thực Thể: AIQuotaLogs (3.9)
-- Mục đích nghiệp vụ: Kiểm toán và giám sát việc tiêu thụ định mức tài nguyên AI (Gemini Tokens), chi phí USD và độ trễ (BR-ADM-AI-001).
-- ============================================================

CREATE TABLE dbo.AIQuotaLogs (
    Log_ID BIGINT IDENTITY(1,1) NOT NULL,
    User_ID INT NOT NULL,
    Feature_Code VARCHAR(50) NOT NULL,
    Tokens_Consumed INT NOT NULL,
    Cost_Estimate_USD DECIMAL(8,4) NOT NULL DEFAULT 0.0000,
    Request_Status VARCHAR(20) NOT NULL,
    Error_Message NVARCHAR(500) NULL,
    Execution_Time_Ms INT NOT NULL,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_AIQuotaLogs PRIMARY KEY CLUSTERED (Log_ID),
    CONSTRAINT FK_AIQuotaLogs_Users FOREIGN KEY (User_ID) 
        REFERENCES dbo.Users(User_ID),
    CONSTRAINT CK_AIQuotaLogs_Feature_Code CHECK (Feature_Code IN (
        'AI_CHAT', 'MEAL_PLAN_GEN', 'AI_FRIDGE', 'AI_BARCODE',
        'SUMMARIZE_VIDEO', 'AI_EMBEDDING', 'CONTENT_MODERATION'
    )),
    CONSTRAINT CK_AIQuotaLogs_Request_Status CHECK (Request_Status IN (
        'SUCCESS', 'QUOTA_EXCEEDED', 'ERROR'
    ))
);
GO


-- ============================================================================
-- START: 10_AuditLogs.sql
-- ============================================================================
-- ============================================================
-- File: 10_AuditLogs.sql
-- Bảng Thực Thể: AuditLogs (3.36)
-- Cluster: Mạng xã hội Ẩm thực, Video, Kiểm duyệt & Tương tác
-- Mục đích nghiệp vụ: Nhật ký kiểm toán thao tác quản trị viên và hệ thống (Admin & System audit trail).
-- ============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AuditLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AuditLogs] (
        [Audit_ID]        BIGINT IDENTITY(1,1) NOT NULL,
        [Actor_User_ID]   INT NULL,
        [Actor_Role]      VARCHAR(10) NOT NULL,
        [Action]          VARCHAR(50) NOT NULL,
        [Target_Table]    VARCHAR(50) NOT NULL,
        [Target_ID]       VARCHAR(50) NOT NULL,
        [Old_Value]       NVARCHAR(MAX) NULL,
        [New_Value]       NVARCHAR(MAX) NULL,
        [Note]            NVARCHAR(500) NULL,
        [Created_At]      DATETIME2(7) NOT NULL CONSTRAINT [DF_AuditLogs_Created_At] DEFAULT (SYSDATETIME()),

        CONSTRAINT [PK_AuditLogs] PRIMARY KEY CLUSTERED ([Audit_ID] ASC),
        CONSTRAINT [CK_AuditLogs_Actor_Role] CHECK ([Actor_Role] IN ('MEMBER', 'VIP', 'ADMIN', 'SYSTEM')),
        CONSTRAINT [CK_AuditLogs_Old_Value_JSON] CHECK ([Old_Value] IS NULL OR ISJSON([Old_Value]) = 1),
        CONSTRAINT [CK_AuditLogs_New_Value_JSON] CHECK ([New_Value] IS NULL OR ISJSON([New_Value]) = 1)
    );

    CREATE NONCLUSTERED INDEX [IX_AuditLogs_Actor_User_ID] ON [dbo].[AuditLogs]([Actor_User_ID] ASC);
    CREATE NONCLUSTERED INDEX [IX_AuditLogs_Target] ON [dbo].[AuditLogs]([Target_Table] ASC, [Target_ID] ASC);

    IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[AuditLogs] ADD CONSTRAINT [FK_AuditLogs_Users] FOREIGN KEY ([Actor_User_ID]) REFERENCES [dbo].[Users]([User_ID]);
    END;
END;
GO
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
-- START: 13_IngredientAllergens.sql
-- ============================================================================
-- ============================================================
-- File: 13_IngredientAllergens.sql
-- Bảng Thực Thể: IngredientAllergens (3.38) [Bảng Trung Gian Phân Rã M:N Ingredients - Allergens]
-- Mục đích nghiệp vụ: Quản lý mối quan hệ giữa nguyên liệu thực phẩm và nhóm dị ứng (BR-USR-07, BR-SCI-04).
-- ============================================================

CREATE TABLE dbo.IngredientAllergens (
    Ingredient_ID INT NOT NULL,
    Allergen_ID INT NOT NULL,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_IngredientAllergens PRIMARY KEY CLUSTERED (Ingredient_ID, Allergen_ID),
    CONSTRAINT FK_IngredientAllergens_Allergens FOREIGN KEY (Allergen_ID) 
        REFERENCES dbo.Allergens(Allergen_ID) ON DELETE CASCADE
);

CREATE NONCLUSTERED INDEX IX_IngredientAllergens_Allergen_ID ON dbo.IngredientAllergens(Allergen_ID);
GO

-- Khóa ngoại tới Ingredients nếu bảng đã tồn tại
IF OBJECT_ID('dbo.Ingredients', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IngredientAllergens_Ingredients')
    BEGIN
        ALTER TABLE dbo.IngredientAllergens
        ADD CONSTRAINT FK_IngredientAllergens_Ingredients 
            FOREIGN KEY (Ingredient_ID) REFERENCES dbo.Ingredients(Ingredient_ID) ON DELETE CASCADE;
    END
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
-- START: 20_Votes.sql
-- ============================================================================
-- ============================================================
-- File: 20_Votes.sql
-- Bảng Thực Thể: Votes (3.37) [Bảng Trung Gian Phân Rã M:N Users - Posts]
-- Mục đích nghiệp vụ: Quản lý lượt tương tác thả tim / yêu thích của người dùng đối với bài viết cộng đồng (Posts).
-- Quy tắc nghiệp vụ BR-INT-LIKE-01: Mỗi tài khoản chỉ Like tối đa 1 lần/bài viết (Unlike nếu bấm lại). Tim cho công thức/video được tính qua Post đại diện.
-- ============================================================

CREATE TABLE dbo.Votes (
    Vote_ID BIGINT IDENTITY(1,1) NOT NULL,
    User_ID INT NOT NULL,
    Post_ID INT NOT NULL,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_Votes PRIMARY KEY CLUSTERED (Vote_ID),
    CONSTRAINT FK_Votes_Users FOREIGN KEY (User_ID) 
        REFERENCES dbo.Users(User_ID) ON DELETE CASCADE,
    CONSTRAINT UQ_Votes_User_Post UNIQUE NONCLUSTERED (User_ID, Post_ID)
);

CREATE NONCLUSTERED INDEX IX_Votes_Post_ID ON dbo.Votes(Post_ID);
GO

-- Khóa ngoại tới Posts nếu bảng đã tồn tại
IF OBJECT_ID('dbo.Posts', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Votes_Posts')
    BEGIN
        ALTER TABLE dbo.Votes
        ADD CONSTRAINT FK_Votes_Posts 
            FOREIGN KEY (Post_ID) REFERENCES dbo.Posts(Post_ID) ON DELETE CASCADE;
    END
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
-- START: 26_Products.sql
-- ============================================================================
-- ============================================================
-- File: 26_Products.sql
-- Bảng Thực Thể: Products (3.23)
-- Mục đích nghiệp vụ: Cơ sở dữ liệu sản phẩm đóng gói thương mại, mã vạch Barcode EAN-13/UPC và kết luận kiểm định chay.
-- ============================================================

CREATE TABLE dbo.Products (
    Product_ID INT IDENTITY(1,1) NOT NULL,
    Barcode VARCHAR(50) NOT NULL,
    Product_Name NVARCHAR(150) NOT NULL,
    Brand NVARCHAR(100) NULL,
    Ingredients_Text NVARCHAR(MAX) NULL,
    Vegan_Verdict VARCHAR(15) NOT NULL,
    Admin_Verified BIT NOT NULL DEFAULT 0,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),
    Last_Synced_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_Products PRIMARY KEY CLUSTERED (Product_ID),
    CONSTRAINT UQ_Products_Barcode UNIQUE (Barcode),
    CONSTRAINT CK_Products_Vegan_Verdict CHECK (Vegan_Verdict IN ('VEGAN', 'NON_VEGAN', 'DOUBTFUL'))
);
GO


-- ============================================================================
-- START: 27_ENumberDictionary.sql
-- ============================================================================
-- ============================================================
-- File: 27_ENumberDictionary.sql
-- Bảng Thực Thể: ENumberDictionary (3.24)
-- Mục đích nghiệp vụ: Từ điển bách khoa phụ gia thực phẩm mã hiệu E-Number quốc tế, nguồn gốc hóa học và mức độ phù hợp ăn chay.
-- ============================================================

CREATE TABLE dbo.ENumberDictionary (
    Code VARCHAR(15) NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    Category_Type VARCHAR(50) NULL,
    Origin_Source NVARCHAR(100) NOT NULL,
    Vegan_Status VARCHAR(20) NOT NULL,
    Health_Risk_Notes NVARCHAR(MAX) NULL,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),
    Updated_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_ENumberDictionary PRIMARY KEY CLUSTERED (Code),
    CONSTRAINT CK_ENumberDictionary_Vegan_Status CHECK (Vegan_Status IN ('VEGAN', 'NON_VEGAN', 'DOUBTFUL'))
);
GO


-- ============================================================================
-- START: 28_Product_ENumbers.sql
-- ============================================================================
-- ============================================================
-- File: 28_Product_ENumbers.sql
-- Bảng Thực Thể: Product_ENumbers (3.25) [Bảng Trung Gian Phân Rã M:N Products - ENumberDictionary]
-- Mục đích nghiệp vụ: Lưu trữ các phụ gia thực phẩm mã số E-number phát hiện trong sản phẩm đóng gói.
-- ============================================================

CREATE TABLE dbo.Product_ENumbers (
    Product_ID INT NOT NULL,
    Code VARCHAR(15) NOT NULL,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_Product_ENumbers PRIMARY KEY CLUSTERED (Product_ID, Code),
    CONSTRAINT FK_Product_ENumbers_Products FOREIGN KEY (Product_ID) 
        REFERENCES dbo.Products(Product_ID) ON DELETE CASCADE,
    CONSTRAINT FK_Product_ENumbers_ENumberDictionary FOREIGN KEY (Code) 
        REFERENCES dbo.ENumberDictionary(Code) ON DELETE CASCADE
);
GO
GO


-- ============================================================================
-- START: 29_ProductAllergens.sql
-- ============================================================================
-- ============================================================
-- File: 29_ProductAllergens.sql
-- Bảng Thực Thể: ProductAllergens (3.26) [Bảng Trung Gian Phân Rã M:N Products - Allergens]
-- Mục đích nghiệp vụ: Lưu trữ các chất gây dị ứng thực phẩm có mặt hoặc có nguy cơ nhiễm chéo trong sản phẩm (FDA/BYT).
-- ============================================================

CREATE TABLE dbo.ProductAllergens (
    Product_ID INT NOT NULL,
    Allergen_ID INT NOT NULL,
    Presence_Type VARCHAR(15) NOT NULL,

    CONSTRAINT PK_ProductAllergens PRIMARY KEY CLUSTERED (Product_ID, Allergen_ID),
    CONSTRAINT FK_ProductAllergens_Products FOREIGN KEY (Product_ID) 
        REFERENCES dbo.Products(Product_ID) ON DELETE CASCADE,
    CONSTRAINT FK_ProductAllergens_Allergens FOREIGN KEY (Allergen_ID) 
        REFERENCES dbo.Allergens(Allergen_ID) ON DELETE CASCADE,
    CONSTRAINT CK_ProductAllergens_Presence CHECK (Presence_Type IN ('CONTAINS', 'MAY_CONTAIN'))
);
GO
GO


-- ============================================================================
-- START: 30_UserScanHistory.sql
-- ============================================================================
-- ============================================================
-- File: 30_UserScanHistory.sql
-- Bảng Thực Thể: UserScanHistory (3.27) [Lịch Sử Quét Mã Vạch Sản Phẩm]
-- Mục đích nghiệp vụ: Lưu trữ nhật ký quét mã vạch sản phẩm đóng gói, kết quả phát hiện nguyên liệu không thuần chay (hỗ trợ cả Member và Khách Guest).
-- ============================================================

CREATE TABLE dbo.UserScanHistory (
    Scan_ID BIGINT IDENTITY(1,1) NOT NULL,
    User_ID INT NULL,
    Guest_UUID VARCHAR(36) NULL,
    Product_ID INT NOT NULL,
    Scan_Method VARCHAR(25) NOT NULL,
    Is_Non_Vegan_Triggered BIT NOT NULL DEFAULT 0,
    Scanned_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_UserScanHistory PRIMARY KEY CLUSTERED (Scan_ID),
    CONSTRAINT FK_UserScanHistory_Users FOREIGN KEY (User_ID) 
        REFERENCES dbo.Users(User_ID) ON DELETE SET NULL,
    CONSTRAINT FK_UserScanHistory_Products FOREIGN KEY (Product_ID) 
        REFERENCES dbo.Products(Product_ID) ON DELETE CASCADE,
    CONSTRAINT CK_UserScanHistory_Scan_Method CHECK (Scan_Method IN ('CAMERA_BARCODE', 'MANUAL_INPUT')),
    CONSTRAINT CK_UserScanHistory_Owner CHECK (User_ID IS NOT NULL OR Guest_UUID IS NOT NULL)
);
GO
GO


-- ============================================================================
-- START: 31_FridgeSessions.sql
-- ============================================================================
-- ============================================================
-- File: 31_FridgeSessions.sql
-- Bảng Thực Thể: FridgeSessions (3.28)
-- Cluster: Trợ lý Tư vấn AI, Tủ lạnh Ảo & Địa điểm
-- Mục đích nghiệp vụ: Quản lý phiên chụp và phân tích hình ảnh tủ lạnh bằng Vision AI (BR-AI-FRIDGE-01).
-- ============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FridgeSessions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[FridgeSessions] (
        [Session_ID]            INT IDENTITY(1,1) NOT NULL,
        [User_ID]               INT NOT NULL,
        [Image_CDN_URL]         VARCHAR(255) NOT NULL,
        [Detected_Items_Count]  INT NOT NULL CONSTRAINT [DF_FridgeSessions_Items_Count] DEFAULT (0),
        [Created_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_FridgeSessions_Created_At] DEFAULT (SYSDATETIME()),

        CONSTRAINT [PK_FridgeSessions] PRIMARY KEY CLUSTERED ([Session_ID] ASC)
    );

    CREATE NONCLUSTERED INDEX [IX_FridgeSessions_User_ID] ON [dbo].[FridgeSessions]([User_ID] ASC);

    IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[FridgeSessions] ADD CONSTRAINT [FK_FridgeSessions_Users] FOREIGN KEY ([User_ID]) REFERENCES [dbo].[Users]([User_ID]);
    END;
END;
GO
GO


-- ============================================================================
-- START: 32_FridgeInventory.sql
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
-- START: 33_VeganVenues.sql
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
-- START: 34_VenueMenuItems.sql
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
-- START: 35_VenueCategories.sql
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


-- ============================================================================
-- START: 36_ChatSessions.sql
-- ============================================================================
-- ============================================================
-- File: 36_ChatSessions.sql
-- Bảng Thực Thể: ChatSessions (3.33)
-- Mục đích nghiệp vụ: Quản lý phiên hội thoại trợ lý AI tư vấn dinh dưỡng (hỗ trợ cả Member và Khách Guest).
-- ============================================================

CREATE TABLE dbo.ChatSessions (
    Chat_Session_ID BIGINT IDENTITY(1,1) NOT NULL,
    User_ID INT NULL,
    Profiles_ID INT NULL,
    Guest_UUID VARCHAR(36) NULL,
    Session_Title NVARCHAR(150) NOT NULL DEFAULT N'Hội thoại mới',
    Context_Type VARCHAR(30) NOT NULL DEFAULT 'NUTRITION_CONSULTATION',
    Started_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),
    Updated_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_ChatSessions PRIMARY KEY CLUSTERED (Chat_Session_ID),
    CONSTRAINT FK_ChatSessions_Users FOREIGN KEY (User_ID) 
        REFERENCES dbo.Users(User_ID) ON DELETE SET NULL,
    CONSTRAINT FK_ChatSessions_HealthProfiles FOREIGN KEY (Profiles_ID) 
        REFERENCES dbo.HealthProfiles(User_ID),
    CONSTRAINT CK_ChatSessions_Owner CHECK (User_ID IS NOT NULL OR Guest_UUID IS NOT NULL)
);
GO


-- ============================================================================
-- START: 37_ChatMessages.sql
-- ============================================================================
-- ============================================================
-- File: 37_ChatMessages.sql
-- Bảng Thực Thể: ChatMessages (3.34)
-- Mục đích nghiệp vụ: Chi tiết từng lượt tin nhắn trong phiên tư vấn AI (phân vai USER và ASSISTANT).
-- ============================================================

CREATE TABLE dbo.ChatMessages (
    Message_ID BIGINT IDENTITY(1,1) NOT NULL,
    Chat_Session_ID BIGINT NOT NULL,
    Sender_Type VARCHAR(20) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    Metadata_JSON NVARCHAR(MAX) NULL,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_ChatMessages PRIMARY KEY CLUSTERED (Message_ID),
    CONSTRAINT FK_ChatMessages_ChatSessions FOREIGN KEY (Chat_Session_ID) 
        REFERENCES dbo.ChatSessions(Chat_Session_ID) ON DELETE CASCADE,
    CONSTRAINT CK_ChatMessages_Sender_Type CHECK (Sender_Type IN ('USER', 'ASSISTANT')),
    CONSTRAINT CK_ChatMessages_Metadata_JSON CHECK (Metadata_JSON IS NULL OR ISJSON(Metadata_JSON) = 1)
);
GO


-- ============================================================================
-- START: 38_Surveys.sql
-- ============================================================================
-- ============================================================
-- File: 38_Surveys.sql
-- Bảng Thực Thể: Surveys (3.35)
-- Mục đích nghiệp vụ: Phiếu khảo sát / đánh giá chất lượng phản hồi AI chatbot từ người dùng (BR-ADM-AI-002).
-- ============================================================

CREATE TABLE dbo.Surveys (
    Survey_ID BIGINT IDENTITY(1,1) NOT NULL,
    User_ID INT NOT NULL,
    Session_ID BIGINT NULL,
    Rating_Score INT NOT NULL,
    Context_Question NVARCHAR(255) NOT NULL,
    Feedback_Content NVARCHAR(MAX) NULL,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_Surveys PRIMARY KEY CLUSTERED (Survey_ID),
    CONSTRAINT FK_Surveys_Users FOREIGN KEY (User_ID) 
        REFERENCES dbo.Users(User_ID),
    CONSTRAINT FK_Surveys_ChatSessions FOREIGN KEY (Session_ID) 
        REFERENCES dbo.ChatSessions(Chat_Session_ID) ON DELETE SET NULL,
    CONSTRAINT CK_Surveys_Rating_Score CHECK (Rating_Score BETWEEN 1 AND 5)
);
GO
