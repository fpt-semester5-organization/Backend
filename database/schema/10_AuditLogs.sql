-- ============================================================
-- File: 10_AuditLogs.sql
-- Bảng Thực Thể: AuditLogs (3.36)
-- Cluster: Mạng xã hội Ẩm thực, Video, Kiểm duyệt & Tương tác
-- Mục đích nghiệp vụ: Nhật ký kiểm toán thao tác quản trị viên và hệ thống (Admin & System audit trail).
-- ============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AuditLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AuditLogs] (
        [Audit_ID]        BIGINT IDENTITY(1,1) NOT NULL,
        [Actor_User_ID]   INT NULL,
        [Actor_Role]      VARCHAR(10) NOT NULL,
        [Action]          VARCHAR(50) NOT NULL,
        [Target_Table]    VARCHAR(50) NOT NULL,
        [Target_ID]       VARCHAR(50) NOT NULL,
        [Old_Value]       NVARCHAR(MAX) NULL,
        [New_Value]       NVARCHAR(MAX) NULL,
        [Note]            NVARCHAR(500) NULL,
        [Created_At]      DATETIME2(7) NOT NULL CONSTRAINT [DF_AuditLogs_Created_At] DEFAULT (SYSDATETIME()),

        CONSTRAINT [PK_AuditLogs] PRIMARY KEY CLUSTERED ([Audit_ID] ASC),
        CONSTRAINT [CK_AuditLogs_Actor_Role] CHECK ([Actor_Role] IN ('MEMBER', 'VIP', 'ADMIN', 'SYSTEM')),
        CONSTRAINT [CK_AuditLogs_Old_Value_JSON] CHECK ([Old_Value] IS NULL OR ISJSON([Old_Value]) = 1),
        CONSTRAINT [CK_AuditLogs_New_Value_JSON] CHECK ([New_Value] IS NULL OR ISJSON([New_Value]) = 1)
    );

    CREATE NONCLUSTERED INDEX [IX_AuditLogs_Actor_User_ID] ON [dbo].[AuditLogs]([Actor_User_ID] ASC);
    CREATE NONCLUSTERED INDEX [IX_AuditLogs_Target] ON [dbo].[AuditLogs]([Target_Table] ASC, [Target_ID] ASC);

    IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[AuditLogs] ADD CONSTRAINT [FK_AuditLogs_Users] FOREIGN KEY ([Actor_User_ID]) REFERENCES [dbo].[Users]([User_ID]);
    END;
END;
GO
