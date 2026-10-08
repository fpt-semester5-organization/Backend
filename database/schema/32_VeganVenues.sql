-- ============================================================================
-- Table: VeganVenues
-- Cluster: Trợ lý Tư vấn AI, Tủ lạnh Ảo & Địa điểm
-- Description: Bản đồ cơ sở ẩm thực chay (nhà hàng, quán ăn, buffet chay).
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[VeganVenues]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[VeganVenues] (
        [Venue_ID]              INT IDENTITY(1,1) NOT NULL,
        [External_Source]       VARCHAR(20) NOT NULL,
        [External_Place_ID]     VARCHAR(100) NULL,
        [Venue_Name]            NVARCHAR(150) NOT NULL,
        [Address]               NVARCHAR(255) NOT NULL,
        [Latitude]              DECIMAL(9,6) NOT NULL,
        [Longitude]             DECIMAL(9,6) NOT NULL,
        [Rating]                DECIMAL(2,1) NOT NULL CONSTRAINT [DF_VeganVenues_Rating] DEFAULT (0.0),
        [Thumbnail_Url]         VARCHAR(255) NULL,
        [Is_Fully_Vegan]        BIT NOT NULL CONSTRAINT [DF_VeganVenues_Is_Fully_Vegan] DEFAULT (1),
        [Is_Active]             BIT NOT NULL CONSTRAINT [DF_VeganVenues_Is_Active] DEFAULT (1),
        [Raw_Origin_JSON]       NVARCHAR(MAX) NULL,
        [Created_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_VeganVenues_Created_At] DEFAULT (SYSDATETIME()),

        CONSTRAINT [PK_VeganVenues] PRIMARY KEY CLUSTERED ([Venue_ID] ASC),
        CONSTRAINT [CK_VeganVenues_Source] CHECK ([External_Source] IN ('HAPPYCOW', 'GOOGLE_PLACES')),
        CONSTRAINT [CK_VeganVenues_Rating] CHECK ([Rating] BETWEEN 0.0 AND 5.0)
    );

    CREATE UNIQUE NONCLUSTERED INDEX [UQ_VeganVenues_External_Place_ID] 
    ON [dbo].[VeganVenues]([External_Place_ID] ASC) WHERE [External_Place_ID] IS NOT NULL;

    CREATE NONCLUSTERED INDEX [IX_VeganVenues_Coordinates] 
    ON [dbo].[VeganVenues]([Latitude] ASC, [Longitude] ASC);
END;
GO
