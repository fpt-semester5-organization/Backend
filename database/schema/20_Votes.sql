-- ============================================================
-- File: 20_Votes.sql
-- Bảng Thực Thể: Votes (3.37) [Bảng Trung Gian Phân Rã M:N Users - Posts]
-- Mục đích nghiệp vụ: Quản lý lượt tương tác thả tim / yêu thích của người dùng đối với bài viết cộng đồng (Posts).
-- Quy tắc nghiệp vụ BR-INT-LIKE-01: Mỗi tài khoản chỉ Like tối đa 1 lần/bài viết (Unlike nếu bấm lại). Tim cho công thức/video được tính qua Post đại diện.
-- ============================================================

CREATE TABLE dbo.Votes (
    Vote_ID BIGINT IDENTITY(1,1) NOT NULL,
    User_ID INT NOT NULL,
    Post_ID INT NOT NULL,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_Votes PRIMARY KEY CLUSTERED (Vote_ID),
    CONSTRAINT FK_Votes_Users FOREIGN KEY (User_ID) 
        REFERENCES dbo.Users(User_ID) ON DELETE CASCADE,
    CONSTRAINT UQ_Votes_User_Post UNIQUE NONCLUSTERED (User_ID, Post_ID)
);

CREATE NONCLUSTERED INDEX IX_Votes_Post_ID ON dbo.Votes(Post_ID);
GO

-- Khóa ngoại tới Posts nếu bảng đã tồn tại
IF OBJECT_ID('dbo.Posts', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Votes_Posts')
    BEGIN
        ALTER TABLE dbo.Votes
        ADD CONSTRAINT FK_Votes_Posts 
            FOREIGN KEY (Post_ID) REFERENCES dbo.Posts(Post_ID) ON DELETE CASCADE;
    END
END;
GO
