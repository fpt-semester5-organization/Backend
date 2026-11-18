-- ============================================================================
-- File: 51_FridgeInventory_Unique_User_Ingredient.sql
-- Table: FridgeInventory
-- Cluster: Trợ lý Tư vấn AI, Tủ lạnh Ảo & Địa điểm
-- Description (D123 - lưu hàng loạt kết quả quét tủ lạnh):
--   BR-USR-08 / FN-VIS-02: mỗi nguyên liệu chỉ một dòng cho mỗi người dùng. Trước đây quy tắc chỉ kiểm
--   trong code nên các lệnh lưu song song đã tạo dòng trùng (User_ID, Ingredient_ID).
--   1. In số nhóm trùng hiện có.
--   2. Giữ dòng mới nhất (Inventory_ID lớn nhất) của mỗi nhóm, xóa các dòng còn lại.
--   3. Tạo unique index UQ_FridgeInventory_User_Ingredient (User_ID, Ingredient_ID).
-- Không tạo bảng, không thêm cột. Idempotent: chạy lại không đổi kết quả.
-- ============================================================================

USE [VeggieLifeDb];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[dbo].[FridgeInventory]', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_FridgeInventory_User_Ingredient' AND object_id = OBJECT_ID(N'[dbo].[FridgeInventory]'))
BEGIN
    -- PHẦN 1: Thống kê nhóm trùng
    SELECT [User_ID], [Ingredient_ID], COUNT(*) AS [Row_Count]
    FROM [dbo].[FridgeInventory]
    GROUP BY [User_ID], [Ingredient_ID]
    HAVING COUNT(*) > 1;

    -- PHẦN 2: Giữ dòng mới nhất, xóa phần trùng
    ;WITH Ranked AS (
        SELECT [Inventory_ID],
               ROW_NUMBER() OVER (PARTITION BY [User_ID], [Ingredient_ID] ORDER BY [Inventory_ID] DESC) AS [Rn]
        FROM [dbo].[FridgeInventory]
    )
    DELETE FROM Ranked WHERE [Rn] > 1;

    PRINT CONCAT(N'Đã xóa ', @@ROWCOUNT, N' dòng FridgeInventory trùng nguyên liệu.');

    -- PHẦN 3: Unique index
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_FridgeInventory_User_Ingredient]
        ON [dbo].[FridgeInventory]([User_ID] ASC, [Ingredient_ID] ASC);

    PRINT N'Đã tạo UQ_FridgeInventory_User_Ingredient.';
END
ELSE
BEGIN
    PRINT N'UQ_FridgeInventory_User_Ingredient đã tồn tại - bỏ qua.';
END;
GO
