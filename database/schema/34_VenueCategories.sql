-- ============================================================================
-- Table: VenueCategories (Junction Table M:N)
-- Cluster: Trợ lý Tư vấn AI, Tủ lạnh Ảo & Địa điểm
-- Description: Bảng nối phân loại ẩm thực của cơ sở (M:N với FoodCategories).
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[VenueCategories]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[VenueCategories] (
        [Category_ID]           INT NOT NULL,
        [Venue_ID]              INT NOT NULL,
        [Is_Primary]            BIT NOT NULL CONSTRAINT [DF_VenueCategories_Is_Primary] DEFAULT (0),
        [Assigned_At]           DATETIME2(7) NOT NULL CONSTRAINT [DF_VenueCategories_Assigned_At] DEFAULT (SYSDATETIME()),

        CONSTRAINT [PK_VenueCategories] PRIMARY KEY CLUSTERED ([Category_ID] ASC, [Venue_ID] ASC),
        CONSTRAINT [FK_VenueCategories_FoodCategories] FOREIGN KEY ([Category_ID]) REFERENCES [dbo].[FoodCategories]([Category_ID]),
        CONSTRAINT [FK_VenueCategories_VeganVenues] FOREIGN KEY ([Venue_ID]) REFERENCES [dbo].[VeganVenues]([Venue_ID]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_VenueCategories_Venue_ID] ON [dbo].[VenueCategories]([Venue_ID] ASC);
END;
GO
