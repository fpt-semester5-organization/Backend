-- ============================================================================
-- File: 43_MealPlans_Add_DurationDays.sql
-- Table: MealPlans (Alteration)
-- Cluster: Kho Công thức Chay, Thực đơn & Đi chợ
-- Description: Thêm cột Duration_Days vào bảng MealPlans để lưu số ngày của thực đơn (1 - 30 ngày, mặc định 7 ngày).
-- ============================================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[MealPlans]')
      AND name = 'Duration_Days'
)
BEGIN
    ALTER TABLE [dbo].[MealPlans] ADD [Duration_Days] INT NOT NULL CONSTRAINT [DF_MealPlans_Duration_Days] DEFAULT (7);
END;
GO

