-- ============================================================================
-- File: 46_Ingredients_Add_ImageUrl_IsLocked.sql
-- Table: Ingredients
-- Cluster: Quản trị Nguyên liệu & Ảnh đại diện chuẩn
-- Description: 
--   1. Bổ sung [Image_Url] (đường dẫn ảnh đại diện chuẩn duy nhất) vào bảng Ingredients.
--   2. Bổ sung [Is_Locked] (cờ khóa bảo vệ của Admin: không cho tự động ghi đè) vào bảng Ingredients.
-- ============================================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- 1. Bổ sung cột Image_Url vào bảng Ingredients nếu chưa tồn tại
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Ingredients]') AND name = 'Image_Url')
BEGIN
    ALTER TABLE [dbo].[Ingredients]
    ADD [Image_Url] VARCHAR(500) NULL;
    PRINT N'>> Da bo sung cot [Image_Url] vao bang dbo.Ingredients.';
END;
GO

-- 2. Bổ sung cột Is_Locked vào bảng Ingredients nếu chưa tồn tại
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Ingredients]') AND name = 'Is_Locked')
BEGIN
    ALTER TABLE [dbo].[Ingredients]
    ADD [Is_Locked] BIT NOT NULL CONSTRAINT [DF_Ingredients_Is_Locked] DEFAULT (0);
    PRINT N'>> Da bo sung cot [Is_Locked] vao bang dbo.Ingredients.';
END;
GO

-- 3. Tạo chỉ mục tìm kiếm nhanh theo trạng thái khóa
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Ingredients_Is_Locked' AND object_id = OBJECT_ID(N'[dbo].[Ingredients]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Ingredients_Is_Locked] ON [dbo].[Ingredients]([Is_Locked] ASC);
    PRINT N'>> Da tao chi muc IX_Ingredients_Is_Locked.';
END;
GO
