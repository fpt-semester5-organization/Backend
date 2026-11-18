-- ============================================================================
-- File: 47_MealPlans_Add_Personalization_Columns.sql
-- Table: MealPlans (Alteration)
-- Cluster: Kho Công thức Chay, Thực đơn & Đi chợ
-- Description: 
--   1. Bổ sung [Health_Goal] (Mục tiêu sức khỏe: LOSE_WEIGHT, MAINTAIN, GAIN_MUSCLE) vào bảng MealPlans.
--   2. Bổ sung [Region_Preference] (Khẩu vị vùng miền: NORTH, CENTRAL, SOUTH) vào bảng MealPlans.
--   3. Bổ sung [Use_Fridge] (Cờ tối ưu nguyên liệu tủ lạnh ảo: 1/0) vào bảng MealPlans.
--   4. Bổ sung [Requires_Allium_Free] (Cờ kiêng ngũ vị tân: 1/0) vào bảng MealPlans.
--   5. Mở rộng [Custom_Instructions] lên NVARCHAR(500) để đồng bộ với giới hạn DTO và giao diện người dùng.
-- ============================================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- 1. Bổ sung cột Health_Goal vào bảng MealPlans
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[MealPlans]')
      AND name = 'Health_Goal'
)
BEGIN
    ALTER TABLE [dbo].[MealPlans]
    ADD [Health_Goal] VARCHAR(30) NULL;

    ALTER TABLE [dbo].[MealPlans]
    ADD CONSTRAINT [CK_MealPlans_Health_Goal]
    CHECK ([Health_Goal] IS NULL OR [Health_Goal] IN ('LOSE_WEIGHT', 'MAINTAIN', 'GAIN_MUSCLE'));

    PRINT N'>> Da bo sung cot [Health_Goal] vao bang dbo.MealPlans.';
END;
GO

-- 2. Bổ sung cột Region_Preference vào bảng MealPlans
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[MealPlans]')
      AND name = 'Region_Preference'
)
BEGIN
    ALTER TABLE [dbo].[MealPlans]
    ADD [Region_Preference] VARCHAR(20) NULL;

    ALTER TABLE [dbo].[MealPlans]
    ADD CONSTRAINT [CK_MealPlans_Region_Preference]
    CHECK ([Region_Preference] IS NULL OR [Region_Preference] IN ('NORTH', 'CENTRAL', 'SOUTH'));

    PRINT N'>> Da bo sung cot [Region_Preference] vao bang dbo.MealPlans.';
END;
GO

-- 3. Bổ sung cột Use_Fridge vào bảng MealPlans
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[MealPlans]')
      AND name = 'Use_Fridge'
)
BEGIN
    ALTER TABLE [dbo].[MealPlans]
    ADD [Use_Fridge] BIT NOT NULL CONSTRAINT [DF_MealPlans_Use_Fridge] DEFAULT (0);

    PRINT N'>> Da bo sung cot [Use_Fridge] vao bang dbo.MealPlans.';
END;
GO

-- 4. Bổ sung cột Requires_Allium_Free vào bảng MealPlans
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[MealPlans]')
      AND name = 'Requires_Allium_Free'
)
BEGIN
    ALTER TABLE [dbo].[MealPlans]
    ADD [Requires_Allium_Free] BIT NOT NULL CONSTRAINT [DF_MealPlans_Requires_Allium_Free] DEFAULT (0);

    PRINT N'>> Da bo sung cot [Requires_Allium_Free] vao bang dbo.MealPlans.';
END;
GO

-- 5. Mở rộng Custom_Instructions lên NVARCHAR(500)
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[MealPlans]')
      AND name = 'Custom_Instructions'
      AND max_length < 1000
)
BEGIN
    ALTER TABLE [dbo].[MealPlans]
    ALTER COLUMN [Custom_Instructions] NVARCHAR(500) NULL;

    PRINT N'>> Da mo rong cot [Custom_Instructions] len NVARCHAR(500) trong dbo.MealPlans.';
END;
GO
