-- ============================================================================
-- File: 42_Recipes_Add_FoodId.sql
-- Table: Recipes (Alteration)
-- Cluster: Kho Công thức Chay, Thực đơn & Đi chợ
-- Description: Thêm khóa ngoại Food_ID vào bảng Recipes để liên kết công thức với món ăn khái niệm (Foods).
--              Nullable để tương thích ngược với dữ liệu công thức đã tồn tại.
-- ============================================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Recipes]')
      AND name = 'Food_ID'
)
BEGIN
    ALTER TABLE [dbo].[Recipes] ADD [Food_ID] INT NULL;

    ALTER TABLE [dbo].[Recipes] ADD CONSTRAINT [FK_Recipes_Foods]
        FOREIGN KEY ([Food_ID]) REFERENCES [dbo].[Foods]([Food_ID]);

    CREATE NONCLUSTERED INDEX [IX_Recipes_Food_ID] ON [dbo].[Recipes]([Food_ID] ASC);
END;
GO
