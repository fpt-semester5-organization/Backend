-- ============================================================
-- File: 37_ChatMessages.sql
-- Bảng Thực Thể: ChatMessages (3.34)
-- Mục đích nghiệp vụ: Chi tiết từng lượt tin nhắn trong phiên tư vấn AI (phân vai USER và ASSISTANT).
-- ============================================================

CREATE TABLE dbo.ChatMessages (
    Message_ID BIGINT IDENTITY(1,1) NOT NULL,
    Chat_Session_ID BIGINT NOT NULL,
    Sender_Type VARCHAR(20) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    Metadata_JSON NVARCHAR(MAX) NULL,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_ChatMessages PRIMARY KEY CLUSTERED (Message_ID),
    CONSTRAINT FK_ChatMessages_ChatSessions FOREIGN KEY (Chat_Session_ID) 
        REFERENCES dbo.ChatSessions(Chat_Session_ID) ON DELETE CASCADE,
    CONSTRAINT CK_ChatMessages_Sender_Type CHECK (Sender_Type IN ('USER', 'ASSISTANT')),
    CONSTRAINT CK_ChatMessages_Metadata_JSON CHECK (Metadata_JSON IS NULL OR ISJSON(Metadata_JSON) = 1)
);
