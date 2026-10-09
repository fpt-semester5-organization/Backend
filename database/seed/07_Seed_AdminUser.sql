-- ============================================================
-- File: 07_Seed_AdminUser.sql
-- Thư mục: database/seed/
-- Mục đích: Khởi tạo tài khoản Administrator đầu tiên của hệ thống (Role = 'ADMIN', Status = 'ACTIVE').
-- Bảo mật: Nhận thông tin Email và Password Hash từ biến môi trường / SQLCMD:
--          $(ADMIN_EMAIL) và $(ADMIN_PASSWORD_HASH).
--          Không ghi cứng mật khẩu plaintext.
-- Tính chất: Idempotent - chạy nhiều lần không trùng dữ liệu.
-- ============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'>> [Seed 07/08] Đang khởi tạo Tài Khoản Quản Trị Viên Đầu Tiên (Admin User)...';

-- Thiết lập giá trị mặc định cho sqlcmd scripting variables (có thể ghi đè qua tham số -v từ command line)
:setvar ADMIN_EMAIL "admin@veggielife.vn"
:setvar ADMIN_PASSWORD_HASH "$2a$11$O.xL1H4j8f1mQ0t5S.R8gOaX9G1kL7Q4dM6pS2nU5tW9vB3zE7h1y"
:setvar ADMIN_FULLNAME "System Administrator"

SET NOCOUNT ON;

DECLARE @AdminEmail VARCHAR(150) = '$(ADMIN_EMAIL)';
DECLARE @AdminFullName NVARCHAR(100) = N'$(ADMIN_FULLNAME)';
DECLARE @AdminPasswordHash NVARCHAR(MAX) = '$(ADMIN_PASSWORD_HASH)';
DECLARE @Now DATETIME2(7) = SYSDATETIME();

-- Kiểm tra xem đã tồn tại Admin với Email này chưa
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = @AdminEmail)
BEGIN
    INSERT INTO dbo.Users (
        Email,
        Password_Hash,
        Full_Name,
        Role,
        Account_Status,
        Failed_Login_Count,
        Created_At,
        Updated_At
    )
    VALUES (
        @AdminEmail,
        @AdminPasswordHash,
        @AdminFullName,
        'ADMIN',
        'ACTIVE',
        0,
        @Now,
        @Now
    );

    PRINT N'   -> Đã khởi tạo thành công tài khoản Admin: ' + @AdminEmail;
END
ELSE
BEGIN
    -- Nếu đã tồn tại tài khoản Admin, đảm bảo Role là ADMIN và Account_Status là ACTIVE
    UPDATE dbo.Users
    SET Role = 'ADMIN',
        Account_Status = 'ACTIVE',
        Updated_At = @Now
    WHERE Email = @AdminEmail;

    PRINT N'   -> Tài khoản Admin ' + @AdminEmail + N' đã tồn tại, đảm bảo trạng thái ACTIVE.';
END;
GO
