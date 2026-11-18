-- ============================================================================
-- Bước 3 — Kế hoạch cải tiến nhận diện tủ lạnh (documents/KE_HOACH_CAI_TIEN_NHAN_DIEN_TU_LANH.md)
-- LIỆT KÊ (CHỈ ĐỌC) các nguyên liệu có ảnh đại diện được cắt tự động từ ảnh tủ lạnh.
--
-- Bối cảnh: trước Bước 1–2, backend
--   (1) đổi tên AI sang tên danh mục cụ thể hơn ("Chuối" → "Hoa chuối thái mỏng"), và
--   (2) gán cứng Confidence = 0.95 ≥ ngưỡng 0.80
-- nên khung nhận diện của món KHÁC có thể đã bị cắt làm ảnh đại diện cho nguyên liệu trong danh mục.
-- Code hiện tại không bao giờ ghi đè ảnh đã có, nên ảnh sai không tự hết.
--
-- Nhận diện ảnh cắt tự động: Image_Url chứa phép biến đổi Cloudinary "c_crop,x_..." do
-- FridgeService.BuildCloudinaryCropUrl tạo ra.
--
-- Cách chạy:  sqlcmd -S <server> -d VeggieLife -U <user> -P <mật khẩu> -C -I -i fridge_avatar_audit_list.sql
--             hoặc mở trong Azure Data Studio / SSMS và chạy.
-- Script KHÔNG thay đổi dữ liệu.
-- ============================================================================
SET NOCOUNT ON;

;WITH Cropped AS (
    SELECT
        i.Ingredient_ID,
        i.Ingredient_Name,
        i.Food_Group,
        i.Is_Locked,
        i.Image_Url,
        i.Created_At AS Ingredient_Created_At,
        -- Bỏ đoạn "c_crop,.../" ngay sau "/upload/" để lấy lại URL ảnh tủ lạnh gốc
        CASE
            WHEN CHARINDEX('/upload/c_crop,', i.Image_Url) > 0
            THEN STUFF(
                    i.Image_Url,
                    CHARINDEX('/upload/c_crop,', i.Image_Url) + LEN('/upload/'),
                    CHARINDEX('/', i.Image_Url, CHARINDEX('/upload/c_crop,', i.Image_Url) + LEN('/upload/'))
                        - (CHARINDEX('/upload/c_crop,', i.Image_Url) + LEN('/upload/')) + 1,
                    '')
        END AS Source_Image_Url
    FROM dbo.Ingredients AS i
    WHERE i.Image_Url LIKE '%/upload/c[_]crop,x[_]%'
),
WithSession AS (
    SELECT
        c.*,
        s.Session_ID,
        s.Created_At AS Session_Created_At,
        -- JSON hỏng → NULL để OPENJSON không báo lỗi
        CASE WHEN ISJSON(s.Detected_Data_JSON) = 1 THEN s.Detected_Data_JSON END AS Session_Json
    FROM Cropped AS c
    LEFT JOIN dbo.FridgeSessions AS s
           ON s.Image_CDN_URL = c.Source_Image_Url
)
SELECT
    c.Ingredient_ID,
    c.Ingredient_Name,
    c.Food_Group,
    c.Is_Locked,
    c.Image_Url                 AS Anh_Dai_Dien_Hien_Tai,      -- mở URL này để xem ảnh đã cắt
    c.Session_ID                AS Phien_Quet,
    c.Session_Created_At        AS Thoi_Gian_Quet,
    c.Source_Image_Url          AS Anh_Tu_Lanh_Goc,
    -- Mục nhận diện tương ứng trong phiên (tên đã lưu, độ tin cậy, khung) — để đối chiếu khi rà
    j.[Name]                    AS Ten_Da_Luu_Trong_Phien,
    j.Confidence                AS Do_Tin_Cay_Da_Luu,
    j.box2d                     AS Khung_box2d,
    (SELECT COUNT(*) FROM dbo.FridgeInventory AS f WHERE f.Ingredient_ID = c.Ingredient_ID) AS So_Muc_Tu_Lanh_Dang_Dung,
    -- Gợi ý rà: tên danh mục nhiều chữ hơn hẳn tên chung → nhiều khả năng bị đổi tên sai trước Bước 1
    CASE WHEN LEN(c.Ingredient_Name) - LEN(REPLACE(c.Ingredient_Name, ' ', '')) >= 2
         THEN N'Nên xem kỹ (tên cụ thể)' ELSE N'' END AS Goi_Y
FROM WithSession AS c
OUTER APPLY (
    SELECT TOP (1) d.[Name], d.Confidence, d.box2d
    FROM OPENJSON(c.Session_Json)
         WITH (
             IngredientId INT            '$.IngredientId',
             [Name]       NVARCHAR(200)  '$.Name',
             Confidence   FLOAT          '$.Confidence',
             box2d        NVARCHAR(MAX)  '$.box2d' AS JSON
         ) AS d
    WHERE d.IngredientId = c.Ingredient_ID
) AS j
ORDER BY c.Is_Locked ASC, c.Session_Created_At ASC, c.Ingredient_ID ASC;

-- Tổng hợp nhanh
SELECT
    COUNT(*)                                            AS Tong_Nguyen_Lieu_Co_Anh_Cat_Tu_Dong,
    SUM(CASE WHEN Is_Locked = 1 THEN 1 ELSE 0 END)      AS Dang_Bi_Khoa,
    SUM(CASE WHEN Is_Locked = 0 THEN 1 ELSE 0 END)      AS Co_The_Go_Anh
FROM dbo.Ingredients
WHERE Image_Url LIKE '%/upload/c[_]crop,x[_]%';
