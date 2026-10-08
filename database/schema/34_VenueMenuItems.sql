-- ============================================================================
-- Table: VenueMenuItems
-- Cluster: Trợ lý Tư vấn AI, Tủ lạnh Ảo & Địa điểm
-- Description: Món ăn trong thực đơn của cơ sở ẩm thực chay.
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[VenueMenuItems]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[VenueMenuItems] (
        [Item_ID]               INT IDENTITY(1,1) NOT NULL,
        [Venue_ID]              INT NOT NULL,
        [Dish_Name]             NVARCHAR(150) NOT NULL,
        [Price_VND]             DECIMAL(12,0) NULL,
        [Description]           NVARCHAR(500) NULL,
        [Image_Url]             VARCHAR(255) NULL,
        [Is_Signature]          BIT NOT NULL CONSTRAINT [DF_VenueMenuItems_Is_Signature] DEFAULT (0),

        CONSTRAINT [PK_VenueMenuItems] PRIMARY KEY CLUSTERED ([Item_ID] ASC),
        CONSTRAINT [FK_VenueMenuItems_VeganVenues] FOREIGN KEY ([Venue_ID]) REFERENCES [dbo].[VeganVenues]([Venue_ID]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_VenueMenuItems_Venue_ID] ON [dbo].[VenueMenuItems]([Venue_ID] ASC);
END;
GO
