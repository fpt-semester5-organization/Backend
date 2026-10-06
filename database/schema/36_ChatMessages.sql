-- ============================================================================
-- Table: ChatMessages
-- Cluster: Trợ lý Tư vấn AI, Tủ lạnh Ảo & Địa điểm
-- Description: Tin nhắn hội thoại trong phiên tư vấn AI (Sender: USER | ASSISTANT).
-- ============================================================================


SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ChatMessages]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ChatMessages] (
        [Message_ID]            BIGINT IDENTITY(1,1) NOT NULL,
        [Chat_Session_ID]       BIGINT NOT NULL,
        [Sender_Type]           VARCHAR(20) NOT NULL,
        [Content]               NVARCHAR(MAX) NOT NULL,
        [Created_At]            DATETIME2(7) NOT NULL CONSTRAINT [DF_ChatMessages_Created_At] DEFAULT (SYSDATETIME()),

        CONSTRAINT [PK_ChatMessages] PRIMARY KEY CLUSTERED ([Message_ID] ASC),
        CONSTRAINT [FK_ChatMessages_ChatSessions] FOREIGN KEY ([Chat_Session_ID]) REFERENCES [dbo].[ChatSessions]([Chat_Session_ID]) ON DELETE CASCADE,
        CONSTRAINT [CK_ChatMessages_Sender_Type] CHECK ([Sender_Type] IN ('USER', 'ASSISTANT'))
    );

    CREATE NONCLUSTERED INDEX [IX_ChatMessages_Session_ID] ON [dbo].[ChatMessages]([Chat_Session_ID] ASC);
END;
GO
