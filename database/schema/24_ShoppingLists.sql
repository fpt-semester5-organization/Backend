-- ============================================================================
-- Table: ShoppingLists
-- Cluster: Kho Công thức Chay, Thực đơn & Đi chợ
-- Description: Danh sách đi chợ thông minh tổng hợp từ thực đơn tuần (Chế độ Online).
-- Note: TUYỆT ĐỐI KHÔNG tạo cột Is_Offline_Synced theo quyết định 05/10.
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
        [Created_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_ShoppingLists_Created_At] DEFAULT (SYSDATETIME()),

        CONSTRAINT [PK_ShoppingLists] PRIMARY KEY CLUSTERED ([List_ID] ASC),
        CONSTRAINT [FK_ShoppingLists_MealPlans] FOREIGN KEY ([Plan_ID]) REFERENCES [dbo].[MealPlans]([Plan_ID]),
        CONSTRAINT [CK_ShoppingLists_Status] CHECK ([Status] IN ('PENDING', 'COMPLETED', 'CANCELLED'))
    );

    CREATE NONCLUSTERED INDEX [IX_ShoppingLists_Plan_ID] ON [dbo].[ShoppingLists]([Plan_ID] ASC);
END;
GO
