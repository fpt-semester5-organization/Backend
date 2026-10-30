-- ==============================================================================
-- File: 16_Seed_Foods.sql
-- Thư mục: database/seed/
-- Mục đích: Nạp sẵn danh mục các món ăn khái niệm (Food Master Data) phổ biến
--           giúp người dùng dễ dàng chọn lựa qua autocomplete khi đăng công thức.
-- Tính chất: Idempotent - MERGE theo Food_Name để tránh tạo trùng.
-- ==============================================================================

USE [VeggieLifeDb];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'>> [Seed 16] Đang nạp danh mục Món Ăn Khái Niệm (Foods)...';

SET NOCOUNT ON;

DECLARE @Now DATETIME2(7) = SYSDATETIME();

-- Nạp các món ăn khái niệm phổ biến theo tên danh mục chuẩn
MERGE INTO dbo.Foods AS target
USING (
    SELECT c.Category_ID, src.Food_Name
    FROM (
        VALUES
            -- Món Mì, Bún & Phở Chay
            (N'Món Mì, Bún & Phở Chay', N'Bún bò Huế chay'),
            (N'Món Mì, Bún & Phở Chay', N'Phở chay Hà Nội'),
            (N'Món Mì, Bún & Phở Chay', N'Bún riêu cua chay'),
            (N'Món Mì, Bún & Phở Chay', N'Hủ tiếu Nam Vang chay'),
            (N'Món Mì, Bún & Phở Chay', N'Mì Quảng chay'),
            (N'Món Mì, Bún & Phở Chay', N'Bún chả giò chay'),
            (N'Món Mì, Bún & Phở Chay', N'Bún mắm chay miền Tây'),
            (N'Món Mì, Bún & Phở Chay', N'Hủ tiếu sa tế chay'),

            -- Món Cơm & Xôi Chay
            (N'Món Cơm & Xôi Chay', N'Cơm tấm sườn bì chả chay'),
            (N'Món Cơm & Xôi Chay', N'Cơm chiên Dương Châu chay'),
            (N'Món Cơm & Xôi Chay', N'Cơm cháy kho quẹt chay'),
            (N'Món Cơm & Xôi Chay', N'Xôi gấc hạt sen chay'),
            (N'Món Cơm & Xôi Chay', N'Cơm chiên lá é chay'),

            -- Món Kho Chay Đậm Đà
            (N'Món Kho Chay Đậm Đà', N'Đậu hũ kho nấm rơm'),
            (N'Món Kho Chay Đậm Đà', N'Nấm kho tiêu hạt sen'),
            (N'Món Kho Chay Đậm Đà', N'Cà tím kho tộ chay'),
            (N'Món Kho Chay Đậm Đà', N'Mít non kho tiêu xanh'),
            (N'Món Kho Chay Đậm Đà', N'Sườn non chay rim mặn ngọt'),

            -- Món Xào Chay Thanh Vị
            (N'Món Xào Chay Thanh Vị', N'Rau củ thập cẩm xào chay'),
            (N'Món Xào Chay Thanh Vị', N'Mì xào giòn rau củ chay'),
            (N'Món Xào Chay Thanh Vị', N'Bông cải xanh xào nấm bào ngư'),
            (N'Món Xào Chay Thanh Vị', N'Miến xào rau củ chay'),

            -- Món Canh & Súp Bổ Dưỡng
            (N'Món Canh & Súp Bổ Dưỡng', N'Canh chua Nam Bộ chay'),
            (N'Món Canh & Súp Bổ Dưỡng', N'Canh nấm đậu phụ rong biển'),
            (N'Món Canh & Súp Bổ Dưỡng', N'Súp bí đỏ hạt sen dưỡng sinh'),
            (N'Món Canh & Súp Bổ Dưỡng', N'Canh khổ qua nhồi đậu hũ'),
            (N'Món Canh & Súp Bổ Dưỡng', N'Súp nấm tuyết hạt sen'),

            -- Món Lẩu Chay Gia Đình
            (N'Món Lẩu Chay Gia Đình', N'Lẩu nấm dưỡng sinh thảo mộc'),
            (N'Món Lẩu Chay Gia Đình', N'Lẩu Thái chay chua cay'),
            (N'Món Lẩu Chay Gia Đình', N'Lẩu chao môn thanh đạm'),
            (N'Món Lẩu Chay Gia Đình', N'Lẩu tiêu xanh chay'),

            -- Món Cuốn Chay
            (N'Món Cuốn Chay', N'Gỏi cuốn chay ngũ sắc'),
            (N'Món Cuốn Chay', N'Phở cuốn thanh vị chay'),
            (N'Món Cuốn Chay', N'Bì cuốn chay'),

            -- Món Chiên & Rán Giòn Rụm
            (N'Món Chiên & Rán Giòn Rụm', N'Chả giò chay giòn rụm'),
            (N'Món Chiên & Rán Giòn Rụm', N'Đậu hũ non chiên xù'),
            (N'Món Chiên & Rán Giòn Rụm', N'Bánh xèo chay giòn tan'),

            -- Món Gỏi & Salad Tươi Mát
            (N'Món Gỏi & Salad Tươi Mát', N'Gỏi ngó sen tôm chay'),
            (N'Món Gỏi & Salad Tươi Mát', N'Gỏi hoa chuối tai nấm')
    ) AS src (Category_Name, Food_Name)
    INNER JOIN dbo.FoodCategories c ON c.Category_Name = src.Category_Name
) AS source (Category_ID, Food_Name)
ON (target.Food_Name = source.Food_Name)
WHEN MATCHED THEN
    UPDATE SET
        target.Category_ID = source.Category_ID,
        target.Updated_At = @Now
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Category_ID, Food_Name, Created_At, Updated_At)
    VALUES (source.Category_ID, source.Food_Name, @Now, @Now);

-- Cập nhật Food_ID cho các Recipe mẫu tương ứng
UPDATE dbo.Recipes SET Food_ID = 1  WHERE Recipe_ID IN (124) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 2  WHERE Recipe_ID IN (139) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 3  WHERE Recipe_ID IN (120) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 4  WHERE Recipe_ID IN (143) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 5  WHERE Recipe_ID IN (145) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 6  WHERE Recipe_ID IN (149) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 7  WHERE Recipe_ID IN (148) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 8  WHERE Recipe_ID IN (125) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 9  WHERE Recipe_ID IN (103) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 10 WHERE Recipe_ID IN (105) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 11 WHERE Recipe_ID IN (101) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 12 WHERE Recipe_ID IN (122) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 13 WHERE Recipe_ID IN (153) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 14 WHERE Recipe_ID IN (154) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 15 WHERE Recipe_ID IN (155) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 16 WHERE Recipe_ID IN (152) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 17 WHERE Recipe_ID IN (109, 111) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 18 WHERE Recipe_ID IN (123) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 19 WHERE Recipe_ID IN (136) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 20 WHERE Recipe_ID IN (138) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 21 WHERE Recipe_ID IN (137) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 22 WHERE Recipe_ID IN (146) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 23 WHERE Recipe_ID IN (150) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 24 WHERE Recipe_ID IN (158) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 25 WHERE Recipe_ID IN (157) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 26 WHERE Recipe_ID IN (159) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 27 WHERE Recipe_ID IN (160) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 28 WHERE Recipe_ID IN (147) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 29 WHERE Recipe_ID IN (115) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 30 WHERE Recipe_ID IN (119) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 31 WHERE Recipe_ID IN (110) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 32 WHERE Recipe_ID IN (108) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 33 WHERE Recipe_ID IN (144) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 34 WHERE Recipe_ID IN (140) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 35 WHERE Recipe_ID IN (117, 118) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 36 WHERE Recipe_ID IN (113) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 37 WHERE Recipe_ID IN (104) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 38 WHERE Recipe_ID IN (106) AND Food_ID IS NULL;
UPDATE dbo.Recipes SET Food_ID = 39 WHERE Recipe_ID IN (156) AND Food_ID IS NULL;

PRINT N'   -> Hoàn tất nạp danh mục Món Ăn Khái Niệm (Foods) thành công!';
GO
