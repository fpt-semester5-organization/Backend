-- ============================================================
-- File: 04_Seed_Ingredients.sql
-- Thư mục: database/seed/
-- Mục đích: Nạp sẵn từ điển 100 nguyên liệu thực phẩm chuẩn hóa kèm:
--           - Định mức calo cơ sở trên 100g (Calories_Per_100g)
--           - Gian hàng đi chợ (Default_Stall_Type: PRODUCE, CONDIMENTS, DRY_GOODS, PLANT_PROTEIN)
--           - Cờ ngũ vị tân Phật giáo (Is_Allium)
--           - Nhóm thực phẩm kiểm soát theo trường phái (Food_Group: PLANT, EGG, DAIRY) theo D109.
-- Tính chất: Idempotent - chạy nhiều lần không trùng dữ liệu (MERGE).
-- ============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'>> [Seed 04/08] Đang nạp dữ liệu Master 100 Nguyên Liệu Thực Phẩm (Ingredients)...';

SET NOCOUNT ON;

DECLARE @Now DATETIME2(7) = SYSDATETIME();

MERGE INTO dbo.Ingredients AS target
USING (
    VALUES
        -- ==========================================
        -- 1. ĐẠM THỰC VẬT & CÁC LOẠI ĐẬU (PLANT_PROTEIN)
        -- ==========================================
        (N'Đậu hũ trắng (Tofu)', N'g', 'PLANT_PROTEIN', 76.0, 0, 'PLANT'),
        (N'Đậu hũ chiên vàng', N'g', 'PLANT_PROTEIN', 271.0, 0, 'PLANT'),
        (N'Đậu hũ non (Silken Tofu)', N'g', 'PLANT_PROTEIN', 55.0, 0, 'PLANT'),
        (N'Tàu hũ ky lá tươi (Váng đậu tươi)', N'g', 'PLANT_PROTEIN', 150.0, 0, 'PLANT'),
        (N'Tàu hũ ky khô', N'g', 'PLANT_PROTEIN', 380.0, 0, 'PLANT'),
        (N'Tempeh (Đậu nành lên men Indonesia)', N'g', 'PLANT_PROTEIN', 192.0, 0, 'PLANT'),
        (N'Mì căn tươi (Seitan đạm lúa mì)', N'g', 'PLANT_PROTEIN', 120.0, 0, 'PLANT'),
        (N'Mì căn chiên giòn', N'g', 'PLANT_PROTEIN', 280.0, 0, 'PLANT'),
        (N'Chả lụa chay (từ đậu xanh & bột mì)', N'g', 'PLANT_PROTEIN', 145.0, 0, 'PLANT'),
        (N'Sườn non chay khô (Đạm đậu nành TVP)', N'g', 'PLANT_PROTEIN', 345.0, 0, 'PLANT'),
        (N'Bò lát chay khô (Đạm đậu nành TVP)', N'g', 'PLANT_PROTEIN', 320.0, 0, 'PLANT'),
        (N'Gà viên chay (Đạm thực vật)', N'g', 'PLANT_PROTEIN', 185.0, 0, 'PLANT'),
        (N'Đậu nành hạt khô', N'g', 'PLANT_PROTEIN', 446.0, 0, 'PLANT'),
        (N'Đậu xanh bóc vỏ', N'g', 'PLANT_PROTEIN', 347.0, 0, 'PLANT'),
        (N'Đậu đỏ hạt khô', N'g', 'PLANT_PROTEIN', 337.0, 0, 'PLANT'),
        (N'Đậu đen xanh lòng', N'g', 'PLANT_PROTEIN', 341.0, 0, 'PLANT'),
        (N'Đậu gà (Chickpeas)', N'g', 'PLANT_PROTEIN', 364.0, 0, 'PLANT'),
        (N'Đậu lăng đỏ (Red Lentils)', N'g', 'PLANT_PROTEIN', 353.0, 0, 'PLANT'),
        (N'Đậu trắng hạt khô', N'g', 'PLANT_PROTEIN', 333.0, 0, 'PLANT'),
        (N'Đậu Hà Lan hạt tươi', N'g', 'PRODUCE', 81.0, 0, 'PLANT'),

        -- ==========================================
        -- 2. CÁC LOẠI NẤM TƯƠI & NẤM KHÔ (PRODUCE / DRY_GOODS)
        -- ==========================================
        (N'Nấm rơm tươi', N'g', 'PRODUCE', 31.0, 0, 'PLANT'),
        (N'Nấm bào ngư trắng', N'g', 'PRODUCE', 33.0, 0, 'PLANT'),
        (N'Nấm bào ngư xám', N'g', 'PRODUCE', 35.0, 0, 'PLANT'),
        (N'Nấm đùi gà (King Oyster)', N'g', 'PRODUCE', 35.0, 0, 'PLANT'),
        (N'Nấm kim châm tươi', N'g', 'PRODUCE', 37.0, 0, 'PLANT'),
        (N'Nấm hương tươi (Shiitake)', N'g', 'PRODUCE', 34.0, 0, 'PLANT'),
        (N'Nấm hương khô', N'g', 'DRY_GOODS', 296.0, 0, 'PLANT'),
        (N'Nấm mộc nhĩ khô (Nấm mèo)', N'g', 'DRY_GOODS', 284.0, 0, 'PLANT'),
        (N'Nấm tuyết khô (Ngân nhĩ)', N'g', 'DRY_GOODS', 200.0, 0, 'PLANT'),
        (N'Nấm mỡ trắng (Champignon)', N'g', 'PRODUCE', 22.0, 0, 'PLANT'),
        (N'Nấm ngọc châm trắng (Hải sản)', N'g', 'PRODUCE', 36.0, 0, 'PLANT'),
        (N'Nấm linh chi nâu tươi', N'g', 'PRODUCE', 32.0, 0, 'PLANT'),

        -- ==========================================
        -- 3. CỦ & QUẢ DINH DƯỠNG (PRODUCE)
        -- ==========================================
        (N'Cà rốt củ tươi', N'g', 'PRODUCE', 41.0, 0, 'PLANT'),
        (N'Củ cải trắng tươi', N'g', 'PRODUCE', 16.0, 0, 'PLANT'),
        (N'Bí đỏ hạt đậu (Bí ngô)', N'g', 'PRODUCE', 26.0, 0, 'PLANT'),
        (N'Khoai tây tươi', N'g', 'PRODUCE', 77.0, 0, 'PLANT'),
        (N'Khoai lang mật', N'g', 'PRODUCE', 86.0, 0, 'PLANT'),
        (N'Khoai môn cao', N'g', 'PRODUCE', 112.0, 0, 'PLANT'),
        (N'Củ sen tươi', N'g', 'PRODUCE', 74.0, 0, 'PLANT'),
        (N'Củ sắn tươi (Củ đậu)', N'g', 'PRODUCE', 38.0, 0, 'PLANT'),
        (N'Bắp ngọt tươi (Ngô ngọt)', N'g', 'PRODUCE', 86.0, 0, 'PLANT'),
        (N'Bắp nếp tươi', N'g', 'PRODUCE', 95.0, 0, 'PLANT'),
        (N'Khổ qua (Mướp đắng)', N'g', 'PRODUCE', 17.0, 0, 'PLANT'),
        (N'Mướp hương tươi', N'g', 'PRODUCE', 18.0, 0, 'PLANT'),
        (N'Bầu sao quả tươi', N'g', 'PRODUCE', 14.0, 0, 'PLANT'),
        (N'Bí xanh (Bí đao)', N'g', 'PRODUCE', 12.0, 0, 'PLANT'),
        (N'Cà chua chín đỏ', N'g', 'PRODUCE', 18.0, 0, 'PLANT'),
        (N'Dưa leo tươi (Dưa chuột)', N'g', 'PRODUCE', 15.0, 0, 'PLANT'),
        (N'Cà tím dài', N'g', 'PRODUCE', 25.0, 0, 'PLANT'),
        (N'Su su quả tươi', N'g', 'PRODUCE', 19.0, 0, 'PLANT'),
        (N'Đậu que (Đậu cô ve)', N'g', 'PRODUCE', 31.0, 0, 'PLANT'),
        (N'Đậu rồng tươi', N'g', 'PRODUCE', 49.0, 0, 'PLANT'),
        (N'Đậu bắp tươi', N'g', 'PRODUCE', 33.0, 0, 'PLANT'),
        (N'Măng tươi luộc chín', N'g', 'PRODUCE', 27.0, 0, 'PLANT'),
        (N'Măng tây xanh tươi', N'g', 'PRODUCE', 20.0, 0, 'PLANT'),
        (N'Hoa chuối thái mỏng', N'g', 'PRODUCE', 20.0, 0, 'PLANT'),
        (N'Ngó sen tươi làm gỏi', N'g', 'PRODUCE', 44.0, 0, 'PLANT'),

        -- ==========================================
        -- 4. RAU LÁ XANH & RAU GIA VỊ LÀNH TÍNH (PRODUCE)
        -- ==========================================
        (N'Bông cải xanh (Broccoli)', N'g', 'PRODUCE', 34.0, 0, 'PLANT'),
        (N'Bông cải trắng (Cauliflower)', N'g', 'PRODUCE', 25.0, 0, 'PLANT'),
        (N'Bắp cải trắng', N'g', 'PRODUCE', 25.0, 0, 'PLANT'),
        (N'Bắp cải tím', N'g', 'PRODUCE', 31.0, 0, 'PLANT'),
        (N'Cải thìa tươi (Cải chíp)', N'g', 'PRODUCE', 13.0, 0, 'PLANT'),
        (N'Cải ngọt tươi', N'g', 'PRODUCE', 16.0, 0, 'PLANT'),
        (N'Cải xanh cay', N'g', 'PRODUCE', 18.0, 0, 'PLANT'),
        (N'Rau muống tươi', N'g', 'PRODUCE', 19.0, 0, 'PLANT'),
        (N'Rau ngót tươi', N'g', 'PRODUCE', 35.0, 0, 'PLANT'),
        (N'Rau mồng tơi tươi', N'g', 'PRODUCE', 19.0, 0, 'PLANT'),
        (N'Rau dền đỏ tươi', N'g', 'PRODUCE', 23.0, 0, 'PLANT'),
        (N'Xà lách lụa tươi', N'g', 'PRODUCE', 15.0, 0, 'PLANT'),
        (N'Cải xoăn (Kale)', N'g', 'PRODUCE', 49.0, 0, 'PLANT'),
        (N'Cần tây cọng tươi', N'g', 'PRODUCE', 16.0, 0, 'PLANT'),
        (N'Rau mùi tươi (Ngò rí)', N'g', 'PRODUCE', 23.0, 0, 'PLANT'),
        (N'Ngò gai tươi (Mùi tàu)', N'g', 'PRODUCE', 20.0, 0, 'PLANT'),
        (N'Rau răm tươi', N'g', 'PRODUCE', 30.0, 0, 'PLANT'),
        (N'Kinh giới lá tươi', N'g', 'PRODUCE', 25.0, 0, 'PLANT'),
        (N'Tía tô lá tươi', N'g', 'PRODUCE', 25.0, 0, 'PLANT'),

        -- ==========================================
        -- 5. NGŨ VỊ TÂN (ALLIUM - Is_Allium = 1)
        -- ==========================================
        (N'Hành lá tươi (Hành hoa)', N'g', 'PRODUCE', 32.0, 1, 'PLANT'),
        (N'Hành tím củ (Hành ta)', N'g', 'PRODUCE', 40.0, 1, 'PLANT'),
        (N'Hành tây củ trắng', N'g', 'PRODUCE', 40.0, 1, 'PLANT'),
        (N'Tỏi củ tươi', N'g', 'PRODUCE', 149.0, 1, 'PLANT'),
        (N'Hẹ lá tươi', N'g', 'PRODUCE', 30.0, 1, 'PLANT'),
        (N'Kiệu củ tươi', N'g', 'PRODUCE', 35.0, 1, 'PLANT'),
        (N'Hành Boa-rô (Tỏi tây / Leek)', N'g', 'PRODUCE', 61.0, 1, 'PLANT'),

        -- ==========================================
        -- 6. HẠT DINH DƯỠNG & NGŨ CỐC KHÔ (DRY_GOODS)
        -- ==========================================
        (N'Hạt sen tươi', N'g', 'PRODUCE', 89.0, 0, 'PLANT'),
        (N'Hạt sen khô', N'g', 'DRY_GOODS', 332.0, 0, 'PLANT'),
        (N'Đậu phộng hạt khô (Lạc)', N'g', 'DRY_GOODS', 567.0, 0, 'PLANT'),
        (N'Hạt điều rang mộc', N'g', 'DRY_GOODS', 553.0, 0, 'PLANT'),
        (N'Hạt hạnh nhân Mỹ', N'g', 'DRY_GOODS', 579.0, 0, 'PLANT'),
        (N'Hạt óc chó nhân', N'g', 'DRY_GOODS', 654.0, 0, 'PLANT'),
        (N'Hạt mè trắng (Vừng trắng)', N'g', 'DRY_GOODS', 573.0, 0, 'PLANT'),
        (N'Hạt mè đen (Vừng đen)', N'g', 'DRY_GOODS', 570.0, 0, 'PLANT'),
        (N'Gạo thơm lài miên', N'g', 'DRY_GOODS', 356.0, 0, 'PLANT'),
        (N'Gạo lứt đỏ huyết rồng', N'g', 'DRY_GOODS', 345.0, 0, 'PLANT'),
        (N'Yến mạch cán dẹt Úc', N'g', 'DRY_GOODS', 389.0, 0, 'PLANT'),
        (N'Hạt diêm mạch (Quinoa trắng)', N'g', 'DRY_GOODS', 368.0, 0, 'PLANT'),
        (N'Bún gạo khô', N'g', 'DRY_GOODS', 350.0, 0, 'PLANT'),
        (N'Bánh tráng gỏi cuốn', N'g', 'DRY_GOODS', 330.0, 0, 'PLANT'),
        (N'Bột mì đa dụng', N'g', 'DRY_GOODS', 364.0, 0, 'PLANT'),
        (N'Bột năng (Bột sắn)', N'g', 'DRY_GOODS', 381.0, 0, 'PLANT'),

        -- ==========================================
        -- 7. GIA VỊ & NƯỚC SỐT THUẦN CHAY (CONDIMENTS)
        -- ==========================================
        (N'Nước tương đậu nành (Xì dầu)', N'ml', 'CONDIMENTS', 53.0, 0, 'PLANT'),
        (N'Nước mắm chay (từ dứa & nấm rơm)', N'ml', 'CONDIMENTS', 45.0, 0, 'PLANT'),
        (N'Dầu hào chay nấm hương', N'g', 'CONDIMENTS', 80.0, 0, 'PLANT'),
        (N'Tương hột đậu nành lên men', N'g', 'CONDIMENTS', 120.0, 0, 'PLANT'),
        (N'Chao trắng béo ngậy', N'g', 'CONDIMENTS', 110.0, 0, 'PLANT'),
        (N'Chao đỏ nấu lẩu', N'g', 'CONDIMENTS', 125.0, 0, 'PLANT'),
        (N'Hạt nêm nấm chay bào ngư', N'g', 'CONDIMENTS', 180.0, 0, 'PLANT'),
        (N'Bột ngọt (Mì chính tinh khiết)', N'g', 'CONDIMENTS', 0.0, 0, 'PLANT'),
        (N'Dầu đậu nành nguyên chất', N'ml', 'CONDIMENTS', 884.0, 0, 'PLANT'),
        (N'Dầu mè thơm thượng hạng', N'ml', 'CONDIMENTS', 884.0, 0, 'PLANT'),
        (N'Tiêu đen xay nguyên chất', N'g', 'CONDIMENTS', 251.0, 0, 'PLANT'),
        (N'Gừng củ tươi', N'g', 'PRODUCE', 80.0, 0, 'PLANT'),
        (N'Nghệ củ tươi', N'g', 'PRODUCE', 65.0, 0, 'PLANT'),
        (N'Sả cây tươi', N'g', 'PRODUCE', 99.0, 0, 'PLANT'),
        (N'Ớt hiểm tươi cay', N'g', 'PRODUCE', 40.0, 0, 'PLANT'),

        -- ==========================================
        -- 8. TRỨNG VÀ SỮA (HỖ TRỢ OVO & LACTO)
        -- ==========================================
        (N'Trứng gà ta tươi', N'g', 'PRODUCE', 143.0, 0, 'EGG'),
        (N'Trứng cút tươi', N'g', 'PRODUCE', 158.0, 0, 'EGG'),
        (N'Sữa tươi tiệt trùng bò', N'ml', 'CONDIMENTS', 61.0, 0, 'DAIRY'),
        (N'Phô mai sợi Mozzarella', N'g', 'DRY_GOODS', 280.0, 0, 'DAIRY'),
        (N'Bơ lạt từ sữa bò', N'g', 'CONDIMENTS', 717.0, 0, 'DAIRY')
) AS source (Ingredient_Name, Standard_Unit, Default_Stall_Type, Calories_Per_100g, Is_Allium, Food_Group)
ON (target.Ingredient_Name = source.Ingredient_Name)
WHEN MATCHED THEN
    UPDATE SET 
        target.Standard_Unit = source.Standard_Unit,
        target.Default_Stall_Type = source.Default_Stall_Type,
        target.Calories_Per_100g = source.Calories_Per_100g,
        target.Is_Allium = source.Is_Allium,
        target.Food_Group = source.Food_Group
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Ingredient_Name, Standard_Unit, Default_Stall_Type, Calories_Per_100g, Is_Allium, Food_Group, Created_At)
    VALUES (source.Ingredient_Name, source.Standard_Unit, source.Default_Stall_Type, source.Calories_Per_100g, source.Is_Allium, source.Food_Group, @Now);

PRINT N'   -> Hoàn tất nạp 100 Nguyên Liệu Thực Phẩm thành công!';
GO
