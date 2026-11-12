-- ============================================================================
-- Bước 3 — Kế hoạch cải tiến nhận diện tủ lạnh (documents/KE_HOACH_CAI_TIEN_NHAN_DIEN_TU_LANH.md)
-- GỠ ảnh đại diện bị cắt sai cho các nguyên liệu ĐÃ ĐƯỢC LEADER DUYỆT.
--
-- QUY TRÌNH BẮT BUỘC
--   1. Chạy fridge_avatar_audit_list.sql, rà từng ảnh (cột Anh_Dai_Dien_Hien_Tai).
--   2. Leader chốt danh sách Ingredient_ID có ảnh SAI → điền vào bảng @Approved bên dưới.
--   3. Chạy script với @DryRun = 1 (mặc định): chỉ in ra những dòng SẼ bị gỡ rồi ROLLBACK.
--   4. Kiểm tra kết quả, đổi @DryRun = 0 và chạy lại để thực hiện thật.
--
-- An toàn:
--   - Chỉ gỡ ảnh của nguyên liệu Is_Locked = 0 VÀ ảnh là ảnh cắt tự động ("/upload/c_crop,x_").
--   - Trước khi gỡ, sao lưu (Ingredient_ID, Image_Url cũ) vào bảng dbo.Bak_IngredientAvatar_Step3.
--   - Toàn bộ chạy trong một transaction; lỗi bất kỳ → ROLLBACK.
--   - Khôi phục: xem đoạn KHÔI PHỤC cuối file.
-- Sau khi gỡ (Image_Url = NULL), lần quét tủ lạnh tiếp theo đạt điều kiện mới của Bước 2
-- (độ tin cậy ≥ 0.85, tên trùng hẳn) sẽ tự cắt lại ảnh đúng; Admin cũng có thể tải ảnh lên tay.
-- ============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @DryRun BIT = 1;   -- 1 = chạy thử (ROLLBACK) · 0 = thực hiện thật

DECLARE @Approved TABLE (Ingredient_ID INT PRIMARY KEY);
-- >>> ĐIỀN DANH SÁCH ĐÃ DUYỆT (ví dụ: INSERT INTO @Approved VALUES (101), (102), (117);)
-- INSERT INTO @Approved VALUES (0);
-- <<<

IF NOT EXISTS (SELECT 1 FROM @Approved)
BEGIN
    PRINT N'Chưa có Ingredient_ID nào trong @Approved — không làm gì.';
    RETURN;
END;

BEGIN TRANSACTION;

    -- Tạo bảng sao lưu trong transaction: chạy thử (ROLLBACK) sẽ không để lại bảng rỗng
    IF OBJECT_ID(N'dbo.Bak_IngredientAvatar_Step3', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Bak_IngredientAvatar_Step3 (
            Backup_ID       INT IDENTITY(1,1) PRIMARY KEY,
            Ingredient_ID   INT           NOT NULL,
            Ingredient_Name NVARCHAR(100) NOT NULL,
            Old_Image_Url   VARCHAR(500)  NOT NULL,
            Backed_Up_At    DATETIME2(7)  NOT NULL DEFAULT (SYSDATETIME())
        );
    END;

    DECLARE @Targets TABLE (Ingredient_ID INT PRIMARY KEY, Ingredient_Name NVARCHAR(100), Old_Image_Url VARCHAR(500));

    INSERT INTO @Targets (Ingredient_ID, Ingredient_Name, Old_Image_Url)
    SELECT i.Ingredient_ID, i.Ingredient_Name, i.Image_Url
    FROM dbo.Ingredients AS i
    JOIN @Approved AS a ON a.Ingredient_ID = i.Ingredient_ID
    WHERE i.Is_Locked = 0
      AND i.Image_Url LIKE '%/upload/c[_]crop,x[_]%';

    -- Báo các ID đã duyệt nhưng bị bỏ qua (đã khóa / không phải ảnh cắt tự động / không tồn tại)
    SELECT a.Ingredient_ID,
           i.Ingredient_Name,
           CASE WHEN i.Ingredient_ID IS NULL THEN N'Không tồn tại'
                WHEN i.Is_Locked = 1 THEN N'Đang khóa — không gỡ'
                ELSE N'Ảnh không phải ảnh cắt tự động — không gỡ' END AS Ly_Do_Bo_Qua
    FROM @Approved AS a
    LEFT JOIN dbo.Ingredients AS i ON i.Ingredient_ID = a.Ingredient_ID
    WHERE a.Ingredient_ID NOT IN (SELECT Ingredient_ID FROM @Targets);

    INSERT INTO dbo.Bak_IngredientAvatar_Step3 (Ingredient_ID, Ingredient_Name, Old_Image_Url)
    SELECT Ingredient_ID, Ingredient_Name, Old_Image_Url FROM @Targets;

    UPDATE i
    SET i.Image_Url = NULL
    FROM dbo.Ingredients AS i
    JOIN @Targets AS t ON t.Ingredient_ID = i.Ingredient_ID;

    SELECT t.Ingredient_ID, t.Ingredient_Name, t.Old_Image_Url AS Anh_Da_Go
    FROM @Targets AS t
    ORDER BY t.Ingredient_ID;

    DECLARE @n INT = (SELECT COUNT(*) FROM @Targets);

IF @DryRun = 1
BEGIN
    ROLLBACK TRANSACTION;
    PRINT CONCAT(N'[CHẠY THỬ] Sẽ gỡ ảnh của ', @n, N' nguyên liệu. Đã ROLLBACK, dữ liệu không đổi. Đặt @DryRun = 0 để thực hiện.');
END
ELSE
BEGIN
    COMMIT TRANSACTION;
    PRINT CONCAT(N'[ĐÃ THỰC HIỆN] Gỡ ảnh của ', @n, N' nguyên liệu. Bản sao lưu: dbo.Bak_IngredientAvatar_Step3.');
END;

-- ============================================================================
-- KHÔI PHỤC (chạy riêng khi cần; chỉ khôi phục ảnh nếu hiện vẫn đang NULL):
--
-- UPDATE i SET i.Image_Url = b.Old_Image_Url
-- FROM dbo.Ingredients AS i
-- JOIN (SELECT Ingredient_ID, Old_Image_Url,
--              ROW_NUMBER() OVER (PARTITION BY Ingredient_ID ORDER BY Backup_ID DESC) AS rn
--       FROM dbo.Bak_IngredientAvatar_Step3) AS b
--   ON b.Ingredient_ID = i.Ingredient_ID AND b.rn = 1
-- WHERE i.Image_Url IS NULL;
-- ============================================================================
