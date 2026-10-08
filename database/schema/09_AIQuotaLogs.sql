-- ============================================================
-- File: 09_AIQuotaLogs.sql
-- Bảng Thực Thể: AIQuotaLogs (3.9)
-- Mục đích nghiệp vụ: Kiểm toán và giám sát việc tiêu thụ định mức tài nguyên AI (Gemini Tokens), chi phí USD và độ trễ (BR-ADM-AI-001).
-- ============================================================

CREATE TABLE dbo.AIQuotaLogs (
    Log_ID BIGINT IDENTITY(1,1) NOT NULL,
    User_ID INT NOT NULL,
    Feature_Code VARCHAR(50) NOT NULL,
    Tokens_Consumed INT NOT NULL,
    Cost_Estimate_USD DECIMAL(8,4) NOT NULL DEFAULT 0.0000,
    Request_Status VARCHAR(20) NOT NULL,
    Error_Message NVARCHAR(500) NULL,
    Execution_Time_Ms INT NOT NULL,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_AIQuotaLogs PRIMARY KEY CLUSTERED (Log_ID),
    CONSTRAINT FK_AIQuotaLogs_Users FOREIGN KEY (User_ID) 
        REFERENCES dbo.Users(User_ID),
    CONSTRAINT CK_AIQuotaLogs_Feature_Code CHECK (Feature_Code IN (
        'AI_CHAT', 'MEAL_PLAN_GEN', 'AI_FRIDGE', 'AI_BARCODE',
        'SUMMARIZE_VIDEO', 'AI_EMBEDDING', 'CONTENT_MODERATION'
    )),
    CONSTRAINT CK_AIQuotaLogs_Request_Status CHECK (Request_Status IN (
        'SUCCESS', 'QUOTA_EXCEEDED', 'ERROR'
    ))
);
