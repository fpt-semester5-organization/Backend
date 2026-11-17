-- ============================================================================
-- File: 48_SubscriptionPlans_Versioning_And_Snapshots.sql
-- Table: SubscriptionPlans, PaymentTransactions, SubscriptionPlanVersions
-- Cluster: Quản trị Tài khoản, Sức khỏe & Hội viên (VIP Subscriptions)
-- Description:
--   Giải quyết bài toán phân biệt và bảo lưu quyền lợi gói VIP theo cam kết biên lai:
--   1. Bổ sung [Version] và [Effective_From] vào [SubscriptionPlans] để đánh dấu phiên bản gói.
--   2. Bổ sung các cột Snapshot vào [PaymentTransactions]:
--      - [Plan_Version]: Lưu phiên bản của gói tại thời điểm người dùng thanh toán.
--      - [Snapshot_Features_JSON]: Chụp lại toàn bộ quyền lợi (Quota AI, Meal, đặc quyền) cam kết đúng biên lai lúc mua.
--      - [Snapshot_Plan_Name]: Tên gói tại thời điểm mua.
--      - [Snapshot_Price]: Giá niêm yết tại thời điểm mua.
--      - [Snapshot_Duration_Days]: Thời hạn số ngày tại thời điểm mua.
--   3. Tạo bảng mới [SubscriptionPlanVersions] để lưu vết lịch sử mọi lần thay đổi gói của Admin (Audit & Quản lý phiên bản).
--   4. Tự động Backfill dữ liệu snapshot cho các giao dịch hiện có để đảm bảo tính toàn vẹn dữ liệu.
-- ============================================================================

USE [VeggieLifeDb];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
-- PHẦN 1: BỔ SUNG THUỘC TÍNH PHIÊN BẢN VÀO BẢNG SubscriptionPlans
-- ============================================================================

-- 1.1 Cột [Version]: Phiên bản hiện hành của gói (Mỗi lần Admin sửa gói -> Version tăng 1)
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

-- 1.2 Cột [Effective_From]: Thời điểm phiên bản hiện hành bắt đầu áp dụng
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[SubscriptionPlans]')
      AND name = 'Effective_From'
)
BEGIN
    ALTER TABLE [dbo].[SubscriptionPlans]
    ADD [Effective_From] DATETIME2(7) NOT NULL CONSTRAINT [DF_SubscriptionPlans_Effective_From] DEFAULT (SYSDATETIME());

    PRINT N'>> Da bo sung cot [Effective_From] vao bang dbo.SubscriptionPlans.';
END;
GO


-- ============================================================================
-- PHẦN 2: BỔ SUNG CÁC CỘT SNAPSHOT QUYỀN LỢI VÀO BẢNG PaymentTransactions
-- ============================================================================

-- 2.1 Cột [Plan_Version]: Lưu phiên bản gói mà người dùng đã mua
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]')
      AND name = 'Plan_Version'
)
BEGIN
    ALTER TABLE [dbo].[PaymentTransactions]
    ADD [Plan_Version] INT NOT NULL CONSTRAINT [DF_PaymentTransactions_Plan_Version] DEFAULT (1);

    PRINT N'>> Da bo sung cot [Plan_Version] vao bang dbo.PaymentTransactions.';
END;
GO

-- 2.2 Cột [Snapshot_Features_JSON]: Lưu trữ nguyên vẹn đặc quyền đã cam kết trên biên lai
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

-- 2.3 Cột [Snapshot_Plan_Name]: Lưu tên gói tại thời điểm mua
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]')
      AND name = 'Snapshot_Plan_Name'
)
BEGIN
    ALTER TABLE [dbo].[PaymentTransactions]
    ADD [Snapshot_Plan_Name] NVARCHAR(100) NULL;

    PRINT N'>> Da bo sung cot [Snapshot_Plan_Name] vao bang dbo.PaymentTransactions.';
END;
GO

-- 2.4 Cột [Snapshot_Price]: Lưu giá gốc niêm yết tại thời điểm mua
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]')
      AND name = 'Snapshot_Price'
)
BEGIN
    ALTER TABLE [dbo].[PaymentTransactions]
    ADD [Snapshot_Price] DECIMAL(18,2) NULL;

    PRINT N'>> Da bo sung cot [Snapshot_Price] vao bang dbo.PaymentTransactions.';
END;
GO

-- 2.5 Cột [Snapshot_Duration_Days]: Lưu số ngày thời hạn gói tại thời điểm mua
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]')
      AND name = 'Snapshot_Duration_Days'
)
BEGIN
    ALTER TABLE [dbo].[PaymentTransactions]
    ADD [Snapshot_Duration_Days] INT NULL;

    PRINT N'>> Da bo sung cot [Snapshot_Duration_Days] vao bang dbo.PaymentTransactions.';
END;
GO


-- ============================================================================
-- PHẦN 3: TẠO BẢNG LỊCH SỬ PHIÊN BẢN GÓI [SubscriptionPlanVersions]
-- (Lưu vết mọi thay đổi của Admin, phục vụ đối soát và quản lý quyền lợi theo thời gian)
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SubscriptionPlanVersions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SubscriptionPlanVersions] (
        [Version_ID]            INT IDENTITY(1,1) NOT NULL,
        [Plan_ID]               INT NOT NULL,
        [Version]               INT NOT NULL,
        [Plan_Name]             NVARCHAR(100) NOT NULL,
        [Price]                 DECIMAL(18,2) NOT NULL,
        [Duration_Days]         INT NOT NULL,
        [Features_JSON]         NVARCHAR(MAX) NULL,
        [Effective_From]        DATETIME2(7) NOT NULL CONSTRAINT [DF_SubPlanVersions_Effective_From] DEFAULT (SYSDATETIME()),
        [Effective_To]          DATETIME2(7) NULL,
        [Updated_By_Admin_ID]   INT NOT NULL,
        [Change_Reason]         NVARCHAR(255) NULL,
        [Created_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_SubPlanVersions_Created_At] DEFAULT (SYSDATETIME()),

        CONSTRAINT [PK_SubscriptionPlanVersions] PRIMARY KEY CLUSTERED ([Version_ID] ASC),
        CONSTRAINT [FK_SubPlanVersions_SubscriptionPlans] FOREIGN KEY ([Plan_ID])
            REFERENCES [dbo].[SubscriptionPlans] ([Plan_ID]),
        CONSTRAINT [FK_SubPlanVersions_Users] FOREIGN KEY ([Updated_By_Admin_ID])
            REFERENCES [dbo].[Users] ([User_ID]),
        CONSTRAINT [UQ_SubPlanVersions_Plan_Version] UNIQUE ([Plan_ID], [Version]),
        CONSTRAINT [CK_SubPlanVersions_Price] CHECK ([Price] >= 0.00),
        CONSTRAINT [CK_SubPlanVersions_Duration] CHECK ([Duration_Days] > 0),
        CONSTRAINT [CK_SubPlanVersions_Features_JSON] CHECK ([Features_JSON] IS NULL OR ISJSON([Features_JSON]) = 1)
    );

    PRINT N'>> Da khoi tao bang dbo.SubscriptionPlanVersions thanh cong.';
END;
GO


-- ============================================================================
-- PHẦN 4: BACKFILL DỮ LIỆU BAN ĐẦU (IDEMPOTENT DATA BACKFILL)
-- ============================================================================

-- 4.1 Khởi tạo bản ghi phiên bản 1 (Initial Version) vào [SubscriptionPlanVersions] cho các gói hiện có
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SubscriptionPlanVersions')
BEGIN
    EXEC sp_executesql N'
    INSERT INTO [dbo].[SubscriptionPlanVersions] (
        [Plan_ID], [Version], [Plan_Name], [Price], [Duration_Days], [Features_JSON],
        [Effective_From], [Effective_To], [Updated_By_Admin_ID], [Change_Reason]
    )
    SELECT 
        sp.[Plan_ID],
        sp.[Version],
        sp.[Plan_Name],
        sp.[Price],
        sp.[Duration_Days],
        sp.[Features_JSON],
        sp.[Created_At],
        NULL,
        sp.[Admin_ID],
        N''Phiên bản ban đầu khi khởi tạo hệ thống (Initial Release)''
    FROM [dbo].[SubscriptionPlans] sp
    WHERE NOT EXISTS (
        SELECT 1 FROM [dbo].[SubscriptionPlanVersions] spv
        WHERE spv.[Plan_ID] = sp.[Plan_ID] AND spv.[Version] = sp.[Version]
    );';
END;
GO

-- 4.2 Cập nhật Snapshot quyền lợi cho toàn bộ các giao dịch cũ trong PaymentTransactions (nếu chưa có)
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]')
      AND name = 'Snapshot_Features_JSON'
)
BEGIN
    EXEC sp_executesql N'
    UPDATE pt
    SET 
        pt.[Plan_Version] = ISNULL(sp.[Version], 1),
        pt.[Snapshot_Plan_Name] = sp.[Plan_Name],
        pt.[Snapshot_Price] = sp.[Price],
        pt.[Snapshot_Duration_Days] = sp.[Duration_Days],
        pt.[Snapshot_Features_JSON] = sp.[Features_JSON]
    FROM [dbo].[PaymentTransactions] pt
    INNER JOIN [dbo].[SubscriptionPlans] sp ON pt.[Plan_ID] = sp.[Plan_ID]
    WHERE pt.[Snapshot_Features_JSON] IS NULL;';
END;
GO

PRINT N'>> Hoan tat chay Migration 48_SubscriptionPlans_Versioning_And_Snapshots thanh cong!';
GO
