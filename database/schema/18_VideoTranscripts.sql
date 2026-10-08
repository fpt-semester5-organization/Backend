-- ============================================================================
-- Table: VideoTranscripts
-- Cluster: Mạng xã hội Ẩm thực, Video & Kiểm duyệt
-- Description: Bóc tách văn bản thoại từ video (STT) và JSON công thức trích xuất.
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[VideoTranscripts]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[VideoTranscripts] (
        [Transcript_ID]         INT IDENTITY(1,1) NOT NULL,
        [Video_ID]              INT NOT NULL,
        [Recipe_ID]             INT NULL,
        [Raw_Transcript]        NVARCHAR(MAX) NULL,
        [Extracted_Recipe_JSON] NVARCHAR(MAX) NULL,
        [STT_Model_Version]     VARCHAR(50) NULL,
        [Status]                VARCHAR(20) NOT NULL CONSTRAINT [DF_VideoTranscripts_Status] DEFAULT ('PENDING'),
        [Created_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_VideoTranscripts_Created_At] DEFAULT (SYSDATETIME()),
        [Completed_At]          DATETIME2(7) NULL,

        CONSTRAINT [PK_VideoTranscripts] PRIMARY KEY CLUSTERED ([Transcript_ID] ASC),
        CONSTRAINT [FK_VideoTranscripts_Videos] FOREIGN KEY ([Video_ID]) REFERENCES [dbo].[Videos]([Video_ID]) ON DELETE CASCADE,
        CONSTRAINT [FK_VideoTranscripts_Recipes] FOREIGN KEY ([Recipe_ID]) REFERENCES [dbo].[Recipes]([Recipe_ID]),
        CONSTRAINT [CK_VideoTranscripts_Status] CHECK ([Status] IN ('PENDING', 'COMPLETED', 'FAILED'))
    );

    CREATE UNIQUE NONCLUSTERED INDEX [UQ_VideoTranscripts_Video_ID] ON [dbo].[VideoTranscripts]([Video_ID] ASC);
    CREATE NONCLUSTERED INDEX [IX_VideoTranscripts_Recipe_ID] ON [dbo].[VideoTranscripts]([Recipe_ID] ASC);
END;
GO
