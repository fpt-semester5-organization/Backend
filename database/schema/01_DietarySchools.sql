-- ============================================================
-- File: 01_DietarySchools.sql
-- Bảng Thực Thể: DietarySchools (3.2)
-- Cluster: Kho Công thức Chay, Thực đơn & Đi chợ / Master Data
-- Mục đích nghiệp vụ: Master Data tri thức chuẩn hóa 07 trường phái ăn chay hệ thống kèm cờ quy tắc kiểm soát thực phẩm (BR-USR-01.1, BR-ADM-MAS-001).
-- ============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DietarySchools]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DietarySchools] (
        [Dietary_School_ID]     INT IDENTITY(1,1) NOT NULL,
        [School_Name]           NVARCHAR(100) NOT NULL,
        [School_Code]           VARCHAR(20) NOT NULL,
        [Forbidden_Food_Rules]  NVARCHAR(MAX) NOT NULL,
        [Updated_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_DietarySchools_Updated_At] DEFAULT (SYSDATETIME()),
        [Created_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_DietarySchools_Created_At] DEFAULT (SYSDATETIME()),

        CONSTRAINT [PK_DietarySchools] PRIMARY KEY CLUSTERED ([Dietary_School_ID] ASC),
        CONSTRAINT [UQ_DietarySchools_School_Code] UNIQUE ([School_Code] ASC),
        CONSTRAINT [CK_DietarySchools_Code] CHECK ([School_Code] IN ('VEGAN', 'OVO', 'LACTO', 'LACTO_OVO', 'BUDDHIST', 'POLLOTARIAN', 'PESCATARIAN')),
        CONSTRAINT [CK_DietarySchools_Rules_JSON] CHECK (ISJSON([Forbidden_Food_Rules]) = 1)
    );
END;
GO
