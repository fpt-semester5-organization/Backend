-- ============================================================================
-- File: 45_MealPlans_Allow_Cancelled_Status.sql
-- Table: MealPlans (Alteration)
-- Cluster: Kho Công thức Chay, Thực đơn & Đi chợ
-- Description: Bổ sung giá trị CANCELLED vào check constraint CK_MealPlans_Status để hỗ trợ tính năng Hủy thực đơn.
-- ============================================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'[dbo].[MealPlans]')
      AND name = 'CK_MealPlans_Status'
)
BEGIN
    ALTER TABLE [dbo].[MealPlans] DROP CONSTRAINT [CK_MealPlans_Status];
END;
GO

ALTER TABLE [dbo].[MealPlans] ADD CONSTRAINT [CK_MealPlans_Status]
    CHECK ([Plan_Status] IN ('DRAFT', 'ACTIVE', 'ARCHIVED', 'TEMPLATE', 'CANCELLED'));
GO
