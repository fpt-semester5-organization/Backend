-- ============================================================
-- File: 36_Surveys.sql
-- Bảng Thực Thể: Surveys (3.35)
-- Mục đích nghiệp vụ: Phiếu khảo sát / đánh giá chất lượng phản hồi AI chatbot từ người dùng (BR-ADM-AI-002).
-- ============================================================

CREATE TABLE dbo.Surveys (
    Survey_ID BIGINT IDENTITY(1,1) NOT NULL,
    User_ID INT NOT NULL,
    Session_ID BIGINT NULL,
    Rating_Score INT NOT NULL,
    Context_Question NVARCHAR(255) NOT NULL,
    Feedback_Content NVARCHAR(MAX) NULL,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_Surveys PRIMARY KEY CLUSTERED (Survey_ID),
    CONSTRAINT FK_Surveys_Users FOREIGN KEY (User_ID) 
        REFERENCES dbo.Users(User_ID),
    CONSTRAINT FK_Surveys_ChatSessions FOREIGN KEY (Session_ID) 
        REFERENCES dbo.ChatSessions(Chat_Session_ID) ON DELETE SET NULL,
    CONSTRAINT CK_Surveys_Rating_Score CHECK (Rating_Score BETWEEN 1 AND 5)
);
