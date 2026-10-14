-- ============================================================
-- File: 10_Seed_VeganVenues_Goong.sql
-- Thư mục: database/seed/
-- Mục đích: Nạp sẵn danh sách quán ăn chay tại Hà Nội và TP.HCM từ nguồn Goong (FN-MAP-01).
-- Tính chất: Idempotent - MERGE an toàn chạy nhiều lần không trùng dữ liệu.
-- ============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'>> [Seed 10/10] Đang nạp dữ liệu quán ăn thuần chay Goong (VeganVenues)...';

SET NOCOUNT ON;

DECLARE @Now DATETIME2(7) = SYSDATETIME();

-- 1. Nạp danh sách quán chay tại Hà Nội và TP.HCM
MERGE INTO dbo.VeganVenues AS target
USING (
    VALUES
        -- === HÀ NỘI ===
        ('GOONG', 'GOONG_HN_01', N'Ưu Đàm Chay', N'34 Hàng Bài, Hoàn Kiếm, Hà Nội', 21.022700, 105.852400, 4.8, 'https://res.cloudinary.com/veggielife/venues/uudam.webp', 1, 1),
        ('GOONG', 'GOONG_HN_02', N'Cơm Chay Nàng Tấm', N'79A Trần Hưng Đạo, Hoàn Kiếm, Hà Nội', 21.023500, 105.845000, 4.5, 'https://res.cloudinary.com/veggielife/venues/nangtam.webp', 1, 1),
        ('GOONG', 'GOONG_HN_03', N'Nhà Hàng Chay Vị Lai', N'67 Lý Thường Kiệt, Hoàn Kiếm, Hà Nội', 21.024500, 105.848000, 4.9, 'https://res.cloudinary.com/veggielife/venues/vilai.webp', 1, 1),
        ('GOONG', 'GOONG_HN_04', N'Tịnh Thực Quán', N'10 Huỳnh Thúc Kháng, Đống Đa, Hà Nội', 21.018900, 105.811500, 4.6, 'https://res.cloudinary.com/veggielife/venues/tinhthuc.webp', 1, 1),
        ('GOONG', 'GOONG_HN_05', N'Cơm Chay An Lạc', N'109 Trần Hưng Đạo, Cửa Nam, Hoàn Kiếm, Hà Nội', 21.023000, 105.842000, 4.3, 'https://res.cloudinary.com/veggielife/venues/anlac.webp', 1, 1),
        ('GOONG', 'GOONG_HN_06', N'Cơm Chay Bồ Đề Tâm', N'34 Phạm Huy Thông, Ba Đình, Hà Nội', 21.030500, 105.814200, 4.2, 'https://res.cloudinary.com/veggielife/venues/bodetam.webp', 1, 1),
        ('GOONG', 'GOONG_HN_07', N'Quán Chay Hà Thành', N'116 Ngõ 166 Kim Mã, Ba Đình, Hà Nội', 21.033500, 105.823000, 4.1, 'https://res.cloudinary.com/veggielife/venues/hathanh.webp', 1, 1),
        ('GOONG', 'GOONG_HN_08', N'Quán Chay Dưỡng Sinh Cũ', N'Ngõ 40 Tôn Đức Thắng, Đống Đa, Hà Nội', 21.025000, 105.835000, 2.1, 'https://res.cloudinary.com/veggielife/venues/duongsinh.webp', 0, 1),

        -- === TP. HỒ CHÍ MINH ===
        ('GOONG', 'GOONG_SG_01', N'Cơm Chay Tâm An', N'12 Lê Lợi, Bến Nghé, Quận 1, TP.HCM', 10.772900, 106.698300, 4.6, 'https://res.cloudinary.com/veggielife/venues/taman.webp', 1, 1),
        ('GOONG', 'GOONG_SG_02', N'Tib Chay Huỳnh Thúc Kháng', N'11 Huỳnh Thúc Kháng, Bến Nghé, Quận 1, TP.HCM', 10.771500, 106.702200, 4.8, 'https://res.cloudinary.com/veggielife/venues/tibchay.webp', 1, 1),
        ('GOONG', 'GOONG_SG_03', N'Hum Vegetarian Lounge & Restaurant', N'32 Võ Văn Tần, Phường 6, Quận 3, TP.HCM', 10.778100, 106.691200, 4.9, 'https://res.cloudinary.com/veggielife/venues/hum.webp', 1, 1),
        ('GOONG', 'GOONG_SG_04', N'Quán Chay Mani', N'291/2 Võ Văn Tần, Phường 5, Quận 3, TP.HCM', 10.771200, 106.682100, 4.5, 'https://res.cloudinary.com/veggielife/venues/mani.webp', 1, 1),
        ('GOONG', 'GOONG_SG_05', N'Cơm Chay Pháp Hoa', N'175 Thích Quảng Đức, Phường 4, Phú Nhuận, TP.HCM', 10.803100, 106.689500, 4.4, 'https://res.cloudinary.com/veggielife/venues/phaphoa.webp', 1, 1),
        ('GOONG', 'GOONG_SG_06', N'Cơm Chay Đóa Sen Vàng', N'Thích Quảng Đức, Phường 5, Phú Nhuận, TP.HCM', 10.799500, 106.681100, 4.7, 'https://res.cloudinary.com/veggielife/venues/doasenvang.webp', 1, 1),
        ('GOONG', 'GOONG_SG_07', N'Cơm Chay Giác Đức', N'392 Nguyễn Đình Chiểu, Phường 4, Quận 3, TP.HCM', 10.773500, 106.683000, 4.3, 'https://res.cloudinary.com/veggielife/venues/giacduc.webp', 1, 1),
        ('GOONG', 'GOONG_SG_08', N'Quán Chay Vỉa Hè Cũ', N'Hẻm 18A Nguyễn Thị Minh Khai, Đa Kao, Quận 1, TP.HCM', 10.785000, 106.699000, 2.2, 'https://res.cloudinary.com/veggielife/venues/viahe.webp', 0, 1)
) AS source (External_Source, External_Place_ID, Venue_Name, Address, Latitude, Longitude, Rating, Thumbnail_Url, Is_Fully_Vegan, Is_Active)
ON (target.External_Place_ID = source.External_Place_ID)
WHEN MATCHED THEN
    UPDATE SET
        target.Venue_Name = source.Venue_Name,
        target.Address = source.Address,
        target.Latitude = source.Latitude,
        target.Longitude = source.Longitude,
        target.Rating = source.Rating,
        target.Thumbnail_Url = source.Thumbnail_Url,
        target.Is_Fully_Vegan = source.Is_Fully_Vegan,
        target.Is_Active = source.Is_Active
WHEN NOT MATCHED THEN
    INSERT (External_Source, External_Place_ID, Venue_Name, Address, Latitude, Longitude, Rating, Thumbnail_Url, Is_Fully_Vegan, Is_Active, Created_At)
    VALUES (source.External_Source, source.External_Place_ID, source.Venue_Name, source.Address, source.Latitude, source.Longitude, source.Rating, source.Thumbnail_Url, source.Is_Fully_Vegan, source.Is_Active, @Now);

-- 2. Gán danh mục món ăn đại diện (VenueCategories)
MERGE INTO dbo.VenueCategories AS target
USING (
    SELECT v.Venue_ID, c.Category_ID, 1 AS Is_Primary, @Now AS Assigned_At
    FROM dbo.VeganVenues v
    CROSS JOIN dbo.FoodCategories c
    WHERE c.Category_Name = N'Món Cơm & Xôi Chay'
      AND v.External_Source = 'GOONG'
) AS source (Venue_ID, Category_ID, Is_Primary, Assigned_At)
ON (target.Venue_ID = source.Venue_ID AND target.Category_ID = source.Category_ID)
WHEN NOT MATCHED THEN
    INSERT (Category_ID, Venue_ID, Is_Primary, Assigned_At)
    VALUES (source.Category_ID, source.Venue_ID, source.Is_Primary, source.Assigned_At);

PRINT N'>> [Seed 10/10] Hoàn tất nạp dữ liệu quán chay Goong (Hà Nội & TP.HCM).';
GO
