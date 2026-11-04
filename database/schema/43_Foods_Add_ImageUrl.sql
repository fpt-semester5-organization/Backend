-- ============================================================================
-- File: 43_Foods_Add_ImageUrl.sql
-- Table: Foods (Alteration)
-- Cluster: Kho Công thức Chay, Thực đơn & Đi chợ
-- Description: Thêm thuộc tính Image_Url vào bảng Foods để lưu trữ ảnh đại diện của món ăn khái niệm.
-- ============================================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Foods]')
      AND name = 'Image_Url'
)
BEGIN
    ALTER TABLE [dbo].[Foods] ADD [Image_Url] VARCHAR(1000) NULL;
END;
GO

