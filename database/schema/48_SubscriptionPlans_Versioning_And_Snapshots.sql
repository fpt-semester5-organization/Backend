-- ============================================================================
-- File: 48_SubscriptionPlans_Versioning_And_Snapshots.sql
-- Table: SubscriptionPlans, PaymentTransactions, AuditLogs
-- Cluster: Quản trị Tài khoản, Sức khỏe & Hội viên (VIP Subscriptions)
-- Description:
--   Giải pháp TỐI GIẢN (Lean Architecture) bảo lưu quyền lợi gói VIP theo cam kết biên lai:
--   1. TẬN DỤNG bảng [AuditLogs] có sẵn để ghi vết toàn bộ lịch sử cập nhật của Admin (Old_Value, New_Value, Note, Created_At).
--   2. Bổ sung cột [Version] vào [SubscriptionPlans] để nhận diện phiên bản hiện hành.
--   3. Bổ sung [Snapshot_Features_JSON] và [Snapshot_Plan_Name] vào [PaymentTransactions]
--      để đóng băng (freeze) toàn bộ đặc quyền AI, thực đơn, cẩm nang đúng với biên lai lúc mua.
--   4. KHÔNG TẠO BẢNG MỚI -> Giữ nguyên 100% Conceptual ERD và Logical ERD.
--   5. Backfill an toàn dữ liệu snapshot cho các giao dịch hiện có.
-- ============================================================================

USE [VeggieLifeDb];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
-- PHẦN 0: DỌN DẸP BẢNG THỪA (NẾU ĐÃ TỪNG TẠO TRƯỚC ĐÓ)
-- ============================================================================
IF OBJECT_ID(N'[dbo].[SubscriptionPlanVersions]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[SubscriptionPlanVersions];
    PRINT N'>> Da xoa bo bang dbo.SubscriptionPlanVersions de tan dung bang dbo.AuditLogs co san.';
END;
GO

-- ============================================================================
-- PHẦN 1: BỔ SUNG CỘT PHIÊN BẢN VÀO BẢNG SubscriptionPlans
-- ============================================================================

-- 1.1 Cột [Version]: Đánh dấu số phiên bản (Mỗi lần Admin sửa gói -> Version tăng 1)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[SubscriptionPlans]')
      AND name = 'Version'
)
BEGIN
    ALTER TABLE [dbo].[SubscriptionPlans]
    ADD [Version] INT NOT NULL CONSTRAINT [DF_SubscriptionPlans_Version] DEFAULT (1);

    PRINT N'>> Da bo sung cot [Version] vao bang dbo.SubscriptionPlans.';
END;
GO

-- 1.2 Dọn dẹp cột thừa [Effective_From] nếu đã từng thêm trước đó
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[SubscriptionPlans]')
      AND name = 'Effective_From'
)
BEGIN
    DECLARE @dfEffFrom NVARCHAR(256);
    SELECT @dfEffFrom = d.name 
    FROM sys.default_constraints d 
    JOIN sys.columns c ON d.parent_column_id = c.column_id AND d.parent_object_id = c.object_id
    WHERE c.name = 'Effective_From' AND c.object_id = OBJECT_ID(N'[dbo].[SubscriptionPlans]');

    IF @dfEffFrom IS NOT NULL
        EXEC('ALTER TABLE [dbo].[SubscriptionPlans] DROP CONSTRAINT ' + @dfEffFrom);

    ALTER TABLE [dbo].[SubscriptionPlans] DROP COLUMN [Effective_From];
    PRINT N'>> Da don dep cot thua [Effective_From] tren dbo.SubscriptionPlans.';
END;
GO


-- ============================================================================
-- PHẦN 2: BỔ SUNG CỘT SNAPSHOT QUYỀN LỢI VÀO BẢNG PaymentTransactions
-- ============================================================================

-- 2.1 Cột [Snapshot_Features_JSON]: Đóng băng quyền lợi lúc thanh toán thành công
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]')
      AND name = 'Snapshot_Features_JSON'
)
BEGIN
    ALTER TABLE [dbo].[PaymentTransactions]
    ADD [Snapshot_Features_JSON] NVARCHAR(MAX) NULL
        CONSTRAINT [CK_PaymentTransactions_Snapshot_Features_JSON]
        CHECK ([Snapshot_Features_JSON] IS NULL OR ISJSON([Snapshot_Features_JSON]) = 1);

    PRINT N'>> Da bo sung cot [Snapshot_Features_JSON] vao bang dbo.PaymentTransactions.';
END;
GO

-- 2.2 Cột [Snapshot_Plan_Name]: Lưu tên gói lúc mua (để in biên lai chính xác dù sau này gói đổi tên)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]')
      AND name = 'Snapshot_Plan_Name'
)
BEGIN
    ALTER TABLE [dbo].[PaymentTransactions]
    ADD [Snapshot_Plan_Name] NVARCHAR(200) NULL;

    PRINT N'>> Da bo sung cot [Snapshot_Plan_Name] vao bang dbo.PaymentTransactions.';
END;
GO

-- 2.3 Dọn dẹp các cột snapshot dư thừa nếu đã từng tạo (vì Amount_VND, Starts_At, Ends_At đã có sẵn)
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]') AND name = 'Plan_Version')
BEGIN
    DECLARE @dfPlanVer NVARCHAR(256);
    SELECT @dfPlanVer = d.name 
    FROM sys.default_constraints d 
    JOIN sys.columns c ON d.parent_column_id = c.column_id AND d.parent_object_id = c.object_id
    WHERE c.name = 'Plan_Version' AND c.object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]');

    IF @dfPlanVer IS NOT NULL
        EXEC('ALTER TABLE [dbo].[PaymentTransactions] DROP CONSTRAINT ' + @dfPlanVer);

    ALTER TABLE [dbo].[PaymentTransactions] DROP COLUMN [Plan_Version];
    PRINT N'>> Da xoa cot thua [Plan_Version] tren dbo.PaymentTransactions.';
END;
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]') AND name = 'Snapshot_Price')
BEGIN
    ALTER TABLE [dbo].[PaymentTransactions] DROP COLUMN [Snapshot_Price];
    PRINT N'>> Da xoa cot thua [Snapshot_Price] tren dbo.PaymentTransactions.';
END;
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]') AND name = 'Snapshot_Duration_Days')
BEGIN
    ALTER TABLE [dbo].[PaymentTransactions] DROP COLUMN [Snapshot_Duration_Days];
    PRINT N'>> Da xoa cot thua [Snapshot_Duration_Days] tren dbo.PaymentTransactions.';
END;
GO


-- ============================================================================
-- PHẦN 3: BACKFILL DỮ LIỆU BAN ĐẦU (IDEMPOTENT DATA BACKFILL)
-- ============================================================================

-- Backfill quyền lợi và tên gói cho các giao dịch lịch sử hiện có nếu chưa có snapshot
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]')
      AND name = 'Snapshot_Features_JSON'
)
BEGIN
    EXEC sp_executesql N'
    UPDATE pt
    SET 
        pt.[Snapshot_Plan_Name] = ISNULL(pt.[Snapshot_Plan_Name], sp.[Plan_Name]),
        pt.[Snapshot_Features_JSON] = ISNULL(pt.[Snapshot_Features_JSON], sp.[Features_JSON])
    FROM [dbo].[PaymentTransactions] pt
    INNER JOIN [dbo].[SubscriptionPlans] sp ON pt.[Plan_ID] = sp.[Plan_ID]
    WHERE pt.[Snapshot_Features_JSON] IS NULL;';

    PRINT N'>> Da backfill Snapshot quyen loi cho cac giao dich cu thanh cong.';
END;
GO

PRINT N'>> Hoan tat Migration 48: Toi uu hoa luu tru quyen loi VIP qua AuditLogs va Snapshot columns!';
GO
