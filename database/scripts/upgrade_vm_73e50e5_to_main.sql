-- ============================================================================
-- Nâng cấp CSDL VeggieLifeDb đã dựng từ create_all_tables.sql cũ (backend 73e50e5, 38 bảng)
-- lên schema của nhánh main (41 bảng). IDEMPOTENT: chạy lại nhiều lần không lỗi.
--
-- Chạy SAU các file schema/39..46 (bảng mới và cột mới của Foods, MealPlans, Ingredients...):
--   Phần này chỉ bù những chỗ lệch mà 39..46 không bao phủ (kiểu cột, ràng buộc, cột còn thiếu, chỉ mục).
-- Không dùng USE: chạy bằng sqlcmd -d VeggieLifeDb.
-- ============================================================================
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- 1. Recipes.Category_ID: danh mục nay thuộc Foods, Backend mới không ghi cột này -> cho phép NULL (giữ dữ liệu cũ)
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Recipes') AND name = 'Category_ID' AND is_nullable = 0)
    ALTER TABLE dbo.Recipes ALTER COLUMN Category_ID INT NULL;
GO

-- 2. Mở rộng kiểu cột
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ENumberDictionary') AND name = 'Category_Type' AND system_type_id = 167 AND max_length < 100)
    ALTER TABLE dbo.ENumberDictionary ALTER COLUMN Category_Type NVARCHAR(100) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Recipes') AND name = 'Image_Url' AND max_length < 1000 AND max_length <> -1)
    ALTER TABLE dbo.Recipes ALTER COLUMN Image_Url VARCHAR(1000) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.VeganVenues') AND name = 'External_Place_ID' AND max_length < 255 AND max_length <> -1)
BEGIN
    -- Chỉ mục duy nhất (có lọc) phụ thuộc cột này: gỡ, đổi kiểu rồi tạo lại đúng như schema main
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_VeganVenues_External_Place_ID' AND object_id = OBJECT_ID(N'dbo.VeganVenues'))
        DROP INDEX UQ_VeganVenues_External_Place_ID ON dbo.VeganVenues;
    ALTER TABLE dbo.VeganVenues ALTER COLUMN External_Place_ID VARCHAR(255) NULL;
    CREATE UNIQUE NONCLUSTERED INDEX UQ_VeganVenues_External_Place_ID ON dbo.VeganVenues(External_Place_ID ASC) WHERE External_Place_ID IS NOT NULL;
END
GO

-- 3. Videos.Status cho phép DELETED (xóa mềm video)
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.Videos') AND name = 'CK_Videos_Status' AND definition NOT LIKE '%DELETED%')
BEGIN
    ALTER TABLE dbo.Videos DROP CONSTRAINT CK_Videos_Status;
    ALTER TABLE dbo.Videos ADD CONSTRAINT CK_Videos_Status CHECK ([Status] IN ('PROCESSING', 'READY', 'FAILED', 'BANNED', 'DELETED'));
END
GO

-- 4. Cột còn thiếu
IF COL_LENGTH(N'dbo.MealPlans', N'Max_Swaps_Allowed') IS NULL
    ALTER TABLE dbo.MealPlans ADD Max_Swaps_Allowed INT NOT NULL CONSTRAINT DF_MealPlans_Max_Swaps DEFAULT (21);
GO
IF COL_LENGTH(N'dbo.MealPlans', N'Swaps_Remaining') IS NULL
    ALTER TABLE dbo.MealPlans ADD Swaps_Remaining INT NOT NULL CONSTRAINT DF_MealPlans_Swaps_Remaining DEFAULT (21);
GO
IF COL_LENGTH(N'dbo.FridgeInventory', N'Quantity') IS NULL
    ALTER TABLE dbo.FridgeInventory ADD Quantity DECIMAL(8,2) NOT NULL CONSTRAINT DF_FridgeInventory_Quantity DEFAULT (1.00);
GO
IF COL_LENGTH(N'dbo.FridgeInventory', N'Unit') IS NULL
    ALTER TABLE dbo.FridgeInventory ADD Unit NVARCHAR(30) NOT NULL CONSTRAINT DF_FridgeInventory_Unit DEFAULT (N'phần');
GO
IF COL_LENGTH(N'dbo.FridgeInventory', N'Thumbnail_URL') IS NULL
    ALTER TABLE dbo.FridgeInventory ADD Thumbnail_URL NVARCHAR(MAX) NULL;
GO
IF COL_LENGTH(N'dbo.FridgeSessions', N'Detected_Data_JSON') IS NULL
    ALTER TABLE dbo.FridgeSessions ADD Detected_Data_JSON NVARCHAR(MAX) NULL;
GO
IF COL_LENGTH(N'dbo.VideoTranscripts', N'AI_Summary') IS NULL
    ALTER TABLE dbo.VideoTranscripts ADD AI_Summary NVARCHAR(MAX) NULL;
GO

-- 5. Chỉ mục (hiệu năng)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditLogs_Created_At' AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
    CREATE NONCLUSTERED INDEX IX_AuditLogs_Created_At ON dbo.AuditLogs(Created_At DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditLogs_Action' AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
    CREATE NONCLUSTERED INDEX IX_AuditLogs_Action ON dbo.AuditLogs(Action ASC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Comments_Parent_SoftDeleted' AND object_id = OBJECT_ID(N'dbo.Comments'))
    CREATE NONCLUSTERED INDEX IX_Comments_Parent_SoftDeleted ON dbo.Comments(Parent_Comment_ID ASC, Is_Soft_Deleted ASC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Ingredients_Food_Group' AND object_id = OBJECT_ID(N'dbo.Ingredients'))
    CREATE NONCLUSTERED INDEX IX_Ingredients_Food_Group ON dbo.Ingredients(Food_Group ASC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Ingredients_Is_Allium' AND object_id = OBJECT_ID(N'dbo.Ingredients'))
    CREATE NONCLUSTERED INDEX IX_Ingredients_Is_Allium ON dbo.Ingredients(Is_Allium ASC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_VeganVenues_External_Source' AND object_id = OBJECT_ID(N'dbo.VeganVenues'))
    CREATE NONCLUSTERED INDEX IX_VeganVenues_External_Source ON dbo.VeganVenues(External_Source ASC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_VeganVenues_Is_Active' AND object_id = OBJECT_ID(N'dbo.VeganVenues'))
    CREATE NONCLUSTERED INDEX IX_VeganVenues_Is_Active ON dbo.VeganVenues(Is_Active ASC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ViolationReports_Status_CreatedAt' AND object_id = OBJECT_ID(N'dbo.ViolationReports'))
    CREATE NONCLUSTERED INDEX IX_ViolationReports_Status_CreatedAt ON dbo.ViolationReports(Status ASC, Created_At DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ViolationReports_Status_Source_Type' AND object_id = OBJECT_ID(N'dbo.ViolationReports'))
    CREATE NONCLUSTERED INDEX IX_ViolationReports_Status_Source_Type ON dbo.ViolationReports(Status ASC, Report_Source ASC, Violation_Type ASC);
GO

-- Ghi chú: CK_VeganVenues_Source ở main chỉ còn 'GOONG'; script này GIỮ ràng buộc cũ (rộng hơn) để không vỡ dòng dữ liệu cũ.
PRINT N'>> Hoàn tất nâng cấp schema lên main.';
GO
