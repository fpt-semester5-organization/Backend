-- ============================================================
-- File: 06_UserAllergies.sql
-- Bảng Thực Thể: UserAllergies (3.8) [Bảng Trung Gian Phân Rã M:N Users - Allergens]
-- Mục đích nghiệp vụ: Lưu trữ danh mục chất dị ứng thực phẩm mà người dùng bị mẫn cảm phục vụ loại trừ món ăn an toàn (BR-SCI-04, BR-USR-07).
-- ============================================================

CREATE TABLE dbo.UserAllergies (
    User_ID INT NOT NULL,
    Allergen_ID INT NOT NULL,
    Notes NVARCHAR(255) NULL,
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),
    Updated_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_UserAllergies PRIMARY KEY CLUSTERED (User_ID, Allergen_ID),
    CONSTRAINT FK_UserAllergies_Users FOREIGN KEY (User_ID) 
        REFERENCES dbo.Users(User_ID) ON DELETE CASCADE,
    CONSTRAINT FK_UserAllergies_Allergens FOREIGN KEY (Allergen_ID) 
        REFERENCES dbo.Allergens(Allergen_ID) ON DELETE CASCADE
);
GO
