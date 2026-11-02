-- ============================================================================
-- File: 17_Seed_Member4_Venues_Products_Fridge.sql
-- Thư mục: database/seed/
-- Mục đích: Kịch bản nạp dữ liệu Seed Data cho phân hệ của THÀNH VIÊN 4:
--           "Quán Chay, Máy Quét E-Number & Tủ Lạnh Ảo (Công nghệ & Khám phá)"
-- Dải ID phân bổ: 401 - 499
--
-- Nội dung dữ liệu mẫu:
--   1. 08 Quán ăn chay thực tế tại VN (VeganVenues, VenueCategories) kèm tọa độ GPS
--   2. 40 Món ăn thực đơn chuẩn (VenueMenuItems) - 5 món/quán
--   3. 05 Sản phẩm quét mã vạch Barcode EAN-13 thực tế (Products, Product_ENumbers, ProductAllergens)
--   4. 01 Phiên nhận diện Tủ Lạnh thông minh (FridgeSessions, FridgeInventory)
--   5. 01 Phiên tư vấn trợ lý AI & Khảo sát (ChatSessions, ChatMessages, Surveys)
--
-- Tính chất: IDEMPOTENT (Chạy nhiều lần không trùng dữ liệu, dùng cú pháp MERGE)
-- Toàn vẹn tham chiếu: Kế thừa chuẩn khóa ngoại từ FoodCategories, Ingredients,
--                     ENumberDictionary, Allergens, Users, HealthProfiles.
-- ============================================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'============================================================================';
PRINT N'  >> [Seed 17] Đang nạp Dữ liệu Mẫu Thành viên 4 (ID Range: 401 - 499)...';
PRINT N'============================================================================';

DECLARE @Now DATETIME2(7) = SYSDATETIME();

-- ============================================================================
-- PHẦN 1: 08 QUÁN CHAY THỰC TẾ (VEGANVENUES & VENUECATEGORIES)
-- ============================================================================
PRINT N'   [1/5] Nạp 08 Quán chay thực tế tại Việt Nam (VeganVenues & VenueCategories)...';

SET IDENTITY_INSERT dbo.VeganVenues ON;

MERGE INTO dbo.VeganVenues AS target
USING (
    VALUES
        (
            401, 
            'GOONG', 
            'goong_vn_place_uudam_hanoi_401', 
            N'Nhà Hàng Chay Ưu Đàm Chay', 
            N'34 Hàng Bài, Phường Hàng Bài, Quận Hoàn Kiếm, Hà Nội', 
            21.022315, 
            105.852412, 
            4.8, 
            'https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=800&q=80', 
            1, 
            1, 
            N'{"phone":"0981349889","openingHours":"09:00 - 22:00","priceRange":"100.000đ - 350.000đ","amenities":["Wifi","Máy lạnh","Chỗ đỗ ô tô","Phòng riêng"]}', 
            DATEADD(DAY, -45, @Now)
        ),
        (
            402, 
            'GOONG', 
            'goong_vn_place_vilai_hanoi_402', 
            N'Nhà Hàng Chay Vị Lai', 
            N'67 Lý Thường Kiệt, Phường Trần Hưng Đạo, Quận Hoàn Kiếm, Hà Nội', 
            21.023456, 
            105.847895, 
            4.7, 
            'https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?auto=format&fit=crop&w=800&q=80', 
            1, 
            1, 
            N'{"phone":"0853535656","openingHours":"09:30 - 22:00","priceRange":"120.000đ - 400.000đ","amenities":["Không gian sân vườn","Thanh toán thẻ","Hồ cá Koi"]}', 
            DATEADD(DAY, -42, @Now)
        ),
        (
            403, 
            'GOONG', 
            'goong_vn_place_sadhu_hanoi_403', 
            N'Nhà Hàng Chay Sadhu', 
            N'87 Lý Thường Kiệt, Phường Cửa Nam, Quận Hoàn Kiếm, Hà Nội', 
            21.024128, 
            105.842318, 
            4.9, 
            'https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=800&q=80', 
            1, 
            1, 
            N'{"phone":"0981995588","openingHours":"10:30 - 22:00","priceRange":"289.000đ buffet","amenities":["Buffet gọi món tại bàn","Điều hòa","Giao hàng"]}', 
            DATEADD(DAY, -40, @Now)
        ),
        (
            404, 
            'GOONG', 
            'goong_vn_place_hum_saigon_404', 
            N'Nhà Hàng Chay Hum Vegetarian, Lounge & Restaurant', 
            N'32 Võ Văn Tần, Phường Võ Thị Sáu, Quận 3, TP. Hồ Chí Minh', 
            10.776518, 
            106.690822, 
            4.9, 
            'https://images.unsplash.com/photo-1505253758473-96b301b5c81d?auto=format&fit=crop&w=800&q=80', 
            1, 
            1, 
            N'{"phone":"02839301419","openingHours":"10:00 - 22:00","priceRange":"150.000đ - 500.000đ","amenities":["Cocktail bar thảo mộc","Nhạc acoustic","Không gian thiền"]}', 
            DATEADD(DAY, -38, @Now)
        ),
        (
            405, 
            'GOONG', 
            'goong_vn_place_pibistro_saigon_405', 
            N'Pi Vegetarian Bistro', 
            N'19 Võ Văn Tần, Phường Võ Thị Sáu, Quận 3, TP. Hồ Chí Minh', 
            10.778194, 
            106.691458, 
            4.6, 
            'https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=800&q=80', 
            1, 
            1, 
            N'{"phone":"0933411910","openingHours":"10:00 - 21:30","priceRange":"80.000đ - 220.000đ","amenities":["Ban công ngắm phố","Món chay Đông Nam Á"]}', 
            DATEADD(DAY, -35, @Now)
        ),
        (
            406, 
            'GOONG', 
            'goong_vn_place_rou_saigon_406', 
            N'Nhà Hàng Chay Rôu Vegetarian', 
            N'37B Cô Bắc, Phường Cầu Ông Lãnh, Quận 1, TP. Hồ Chí Minh', 
            10.763425, 
            106.694815, 
            4.7, 
            'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=800&q=80', 
            1, 
            1, 
            N'{"phone":"02838389666","openingHours":"09:00 - 21:30","priceRange":"60.000đ - 180.000đ","amenities":["Món chay hiện đại","Takeaway","Tráng miệng đặc biệt"]}', 
            DATEADD(DAY, -30, @Now)
        ),
        (
            407, 
            'GOONG', 
            'goong_vn_place_anlactam_danang_407', 
            N'Quán Chay An Lạc Tâm', 
            N'209 Nguyễn Hoàng, Phường Nam Dương, Quận Hải Châu, TP. Đà Nẵng', 
            16.061245, 
            108.213452, 
            4.8, 
            'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80', 
            1, 
            1, 
            N'{"phone":"0905123456","openingHours":"06:30 - 21:00","priceRange":"35.000đ - 120.000đ","amenities":["Ẩm thực chay miền Trung","Giá bình dân","Phục vụ nhanh"]}', 
            DATEADD(DAY, -28, @Now)
        ),
        (
            408, 
            'GOONG', 
            'goong_vn_place_thientam_hue_408', 
            N'Nhà Hàng Chay Thiền Tâm', 
            N'110A Lê Ngô Cát, Phường Thủy Xuân, TP. Huế, Thừa Thiên Huế', 
            16.438924, 
            107.576518, 
            4.9, 
            'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80', 
            1, 
            1, 
            N'{"phone":"02343898555","openingHours":"07:00 - 21:00","priceRange":"45.000đ - 200.000đ","amenities":["Cơm chay cung đình Huế","Không gian đồi thông","Trà sen ngự"]}', 
            DATEADD(DAY, -25, @Now)
        )
) AS source (Venue_ID, External_Source, External_Place_ID, Venue_Name, Address, Latitude, Longitude, Rating, Thumbnail_Url, Is_Fully_Vegan, Is_Active, Raw_Origin_JSON, Created_At)
ON target.Venue_ID = source.Venue_ID
WHEN MATCHED THEN
    UPDATE SET 
        target.Venue_Name = source.Venue_Name,
        target.Address = source.Address,
        target.Latitude = source.Latitude,
        target.Longitude = source.Longitude,
        target.Rating = source.Rating,
        target.Thumbnail_Url = source.Thumbnail_Url,
        target.Is_Fully_Vegan = source.Is_Fully_Vegan,
        target.Is_Active = source.Is_Active,
        target.Raw_Origin_JSON = source.Raw_Origin_JSON
WHEN NOT MATCHED THEN
    INSERT (Venue_ID, External_Source, External_Place_ID, Venue_Name, Address, Latitude, Longitude, Rating, Thumbnail_Url, Is_Fully_Vegan, Is_Active, Raw_Origin_JSON, Created_At)
    VALUES (source.Venue_ID, source.External_Source, source.External_Place_ID, source.Venue_Name, source.Address, source.Latitude, source.Longitude, source.Rating, source.Thumbnail_Url, source.Is_Fully_Vegan, source.Is_Active, source.Raw_Origin_JSON, source.Created_At);

SET IDENTITY_INSERT dbo.VeganVenues OFF;

-- Ánh xạ Phân loại ẩm thực cho 8 quán (VenueCategories)
MERGE INTO dbo.VenueCategories AS target
USING (
    VALUES
        -- Quán 401 (Ưu Đàm Chay): Canh & Súp (5), Lẩu Chay (9)
        (5, 401, 1, @Now),
        (9, 401, 0, @Now),
        -- Quán 402 (Vị Lai): Gỏi & Salad (6), Món Xào (4)
        (6, 402, 1, @Now),
        (4, 402, 0, @Now),
        -- Quán 403 (Sadhu): Cơm & Xôi (1), Món Chiên & Rán (7)
        (1, 403, 1, @Now),
        (7, 403, 0, @Now),
        -- Quán 404 (Hum): Món Cuốn (11), Lẩu Chay (9)
        (11, 404, 1, @Now),
        (9, 404, 0, @Now),
        -- Quán 405 (Pi Bistro): Món Xào (4), Mì Bún Phở (2)
        (4, 405, 1, @Now),
        (2, 405, 0, @Now),
        -- Quán 406 (Rôu): Mì Bún Phở (2), Bánh Mặn & Ngọt (15)
        (2, 406, 1, @Now),
        (15, 406, 0, @Now),
        -- Quán 407 (An Lạc Tâm): Mì Bún Phở (2), Món Kho (3)
        (2, 407, 1, @Now),
        (3, 407, 0, @Now),
        -- Quán 408 (Thiền Tâm): Món Hấp & Luộc (8), Tráng Miệng & Chè (12)
        (8, 408, 1, @Now),
        (12, 408, 0, @Now)
) AS source (Category_ID, Venue_ID, Is_Primary, Assigned_At)
ON target.Category_ID = source.Category_ID AND target.Venue_ID = source.Venue_ID
WHEN MATCHED THEN
    UPDATE SET target.Is_Primary = source.Is_Primary
WHEN NOT MATCHED THEN
    INSERT (Category_ID, Venue_ID, Is_Primary, Assigned_At)
    VALUES (source.Category_ID, source.Venue_ID, source.Is_Primary, source.Assigned_At);

-- ============================================================================
-- PHẦN 2: THỰC ĐƠN 40 MÓN ĂN THỰC TẾ (VENUEMENUITEMS) - 5 MÓN/QUÁN
-- ============================================================================
PRINT N'   [2/5] Nạp 40 Món ăn thực đơn (VenueMenuItems) cho 08 Quán chay...';

SET IDENTITY_INSERT dbo.VenueMenuItems ON;

MERGE INTO dbo.VenueMenuItems AS target
USING (
    VALUES
        -- QUÁN 401: ƯU ĐÀM CHAY (5 MÓN)
        (401, 401, N'Súp nấm tuyết hạt sen Ưu Đàm', 95000, N'Hạt sen tươi Huế ninh nhừ cùng nấm tuyết giòn mát và nước dùng củ quả thanh khiết.', 'https://images.unsplash.com/photo-1547592166-23ac45744acd?auto=format&fit=crop&w=400&q=80', 1),
        (402, 401, N'Nem vuông nấm tam giác mạch', 125000, N'Vỏ ram giòn rụm cuộn nấm đông cô, mộc nhĩ và hạt tam giác mạch thơm bùi đặc trưng vùng cao.', 'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=400&q=80', 0),
        (403, 401, N'Lẩu dưỡng sinh thảo mộc cung đình', 380000, N'Nước lẩu ninh từ kỳ tử, táo đỏ, đảng sâm kết hợp 6 loại nấm tươi quý và rau xanh hữu cơ.', 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=400&q=80', 1),
        (404, 401, N'Cơm sen gạo lứt gói lá sen', 110000, N'Gạo lứt đỏ dẻo thơm hấp cùng hạt sen, đậu ngự và nấm rơm trong lá sen tươi ngát hương.', 'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=400&q=80', 0),
        (405, 401, N'Chè tuyết nhĩ táo đỏ long nhãn', 65000, N'Món tráng miệng dưỡng nhan thanh mát nấu cùng đường phèn Quảng Ngãi nguyên chất.', 'https://images.unsplash.com/photo-1551024709-8f23befc6f87?auto=format&fit=crop&w=400&q=80', 0),

        -- QUÁN 402: QUÁN CHAY VỊ LAI (5 MÓN)
        (406, 402, N'Salad hoa quả nhiệt đới sốt chanh leo', 115000, N'Bơ sáp Đắk Lắk, thanh long đỏ, xoài cát hòa quyện sốt chanh leo tươi chua thanh kích thích vị giác.', 'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=400&q=80', 1),
        (407, 402, N'Nấm mỡ đút lò phô mai hạt điều', 145000, N'Nấm mỡ nguyên tai nướng béo ngậy với sốt phô mai hạt điều thuần thực vật thơm lừng.', 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=400&q=80', 0),
        (408, 402, N'Đậu hũ Vị Lai sốt nấm tùng nhung', 135000, N'Đậu hũ non hữu cơ làm thủ công áp chảo phủ sốt nấm tùng nhung đậm đà thượng hạng.', 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=400&q=80', 1),
        (409, 402, N'Canh hồng đài sen ngọc bích', 98000, N'Nước canh hoa atiso đỏ ngọt thanh, củ năng giòn và hạt sen bổ dưỡng.', 'https://images.unsplash.com/photo-1547592166-23ac45744acd?auto=format&fit=crop&w=400&q=80', 0),
        (410, 402, N'Kem bơ hạt dẻ cười thuần chay', 75000, N'Chế biến từ bơ chín tự nhiên và cốt dừa béo bùi, rắc hạt dẻ cười rang giòn.', 'https://images.unsplash.com/photo-1551024709-8f23befc6f87?auto=format&fit=crop&w=400&q=80', 0),

        -- QUÁN 403: NHÀ HÀNG CHAY SADHU (5 MÓN)
        (411, 403, N'Vé Buffet chay thượng hạng Sadhu', 289000, N'Trải nghiệm hơn 80 món ăn mỹ vị chay phục vụ tận bàn theo phong cách ẩm thực Á Đông.', 'https://images.unsplash.com/photo-1414235077428-338989a2e8c0?auto=format&fit=crop&w=400&q=80', 1),
        (412, 403, N'Bánh xèo miền Tây nấm hoàng kim', 85000, N'Vỏ bánh vàng ươm giòn tan nhân nấm bào ngư, nấm hoàng kim, củ hũ dừa và giá đỗ.', 'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=400&q=80', 0),
        (413, 403, N'Nem lụi Sadhu cuộn rau rừng Tây Ninh', 110000, N'Nem lụi làm từ đậu xanh và nấm hương nướng than hoa, cuốn bánh tráng phơi sương chấm tương nếp.', 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=400&q=80', 1),
        (414, 403, N'Súp cốm ngọc làng Vòng thanh vị', 68000, N'Cốm non mùa thu Hà Nội nấu cùng nấm tuyết và bắp ngọt tạo độ dẻo mềm thơm mát.', 'https://images.unsplash.com/photo-1547592166-23ac45744acd?auto=format&fit=crop&w=400&q=80', 0),
        (415, 403, N'Chè sương sa hạt lựu cốt dừa Sadhu', 45000, N'Hạt lựu củ năng giòn bọc bột lọc kèm thạch sương sa và nước cốt dừa nguyên chất.', 'https://images.unsplash.com/photo-1551024709-8f23befc6f87?auto=format&fit=crop&w=400&q=80', 0),

        -- QUÁN 404: HUM VEGETARIAN, LOUNGE & RESTAURANT (5 MÓN)
        (416, 404, N'Cuốn diếp chay sốt tương bần mè rang', 115000, N'Lá cải cay cuộn bún tươi, nấm đùi gà áp chảo và rau thơm chấm tương bần ngậy béo.', 'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=400&q=80', 1),
        (417, 404, N'Cơm chiên trái thơm hạt điều Hum', 145000, N'Hạt cơm tơi vàng xào cùng dứa tươi ngọt dịu, đậu cô ve, cà rốt và hạt điều Bình Phước bùi béo.', 'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=400&q=80', 0),
        (418, 404, N'Lẩu nấm sa tế lá chúc An Giang', 395000, N'Nước lẩu cay nồng dậy mùi tinh dầu lá chúc miền Tây kết hợp hơn 7 loại nấm tươi.', 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=400&q=80', 1),
        (419, 404, N'Đậu hũ nướng giấy bạc sốt tiêu xanh', 135000, N'Đậu hũ mềm mướt ướp tiêu xanh Phú Quốc nướng thơm phức bốc khói nghi ngút.', 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=400&q=80', 0),
        (420, 404, N'Nước ép dưa hấu bạc hà thanh lọc', 75000, N'Thức uống giải nhiệt nguyên chất không thêm đường với hương bạc hà the mát.', 'https://images.unsplash.com/photo-1550583724-b2692b85b150?auto=format&fit=crop&w=400&q=80', 0),

        -- QUÁN 405: PI VEGETARIAN BISTRO (5 MÓN)
        (421, 405, N'Đậu hũ xào sả ớt nghệ tươi Pi Bistro', 95000, N'Đậu hũ chiên sơ xào sả băm giòn thơm lựng cùng nghệ vàng xứ Quảng đậm vị cơm nhà.', 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=400&q=80', 1),
        (422, 405, N'Cà tím kho tộ sốt tiêu đen cốt dừa', 105000, N'Cà tím om trong niêu đất với nước sốt tiêu sánh mịn béo ngậy thơm ngon hao cơm.', 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=400&q=80', 0),
        (423, 405, N'Bún riêu chay cua đồng đậu hũ nấm rơm', 85000, N'Riêu cua làm từ váng sữa đậu nành và đậu xanh xốp mềm, nước dùng chua thanh từ cà chua và giấm bỗng.', 'https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=400&q=80', 1),
        (424, 405, N'Gỏi mít non đậu phộng rang thính gạo', 95000, N'Mít non luộc chín xé sợi trộn rau răm, ớt chuông, rắc đậu phộng thơm ăn cùng bánh tráng mè nướng.', 'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=400&q=80', 0),
        (425, 405, N'Trà hoa đậu biếc sả chanh hạt chia', 55000, N'Trà thảo mộc màu tím lam quyến rũ giàu chất chống oxy hóa anthocyanin dưỡng da.', 'https://images.unsplash.com/photo-1551024709-8f23befc6f87?auto=format&fit=crop&w=400&q=80', 0),

        -- QUÁN 406: NHÀ HÀNG CHAY RÔU VEGETARIAN (5 MÓN)
        (426, 406, N'Phở khô Gia Lai sốt tương chay Rôu', 75000, N'Bánh phở khô dai thấm sốt tương phở đặc chế, ăn kèm tô nước súp ngọt nấm thanh thao.', 'https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=400&q=80', 1),
        (427, 406, N'Bánh bao nướng nhân nấm sốt BBQ', 45000, N'Vỏ bánh nướng vàng ruộm, nhân nấm đùi gà xé sợi tẩm sốt BBQ nấm khói đậm đà.', 'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=400&q=80', 0),
        (428, 406, N'Canh khổ qua nhồi thịt chay nấm rơm', 65000, N'Khổ qua tươi nhồi đạm đậu nành và mộc nhĩ giòn ngọt mát thanh giải nhiệt cơ thể.', 'https://images.unsplash.com/photo-1547592166-23ac45744acd?auto=format&fit=crop&w=400&q=80', 0),
        (429, 406, N'Cơm tấm sườn nướng chay chả bì', 70000, N'Sườn non chay ướp sốt sả nướng xém cạnh, chả trứng hấp từ đậu hũ và bì miến rang thính.', 'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=400&q=80', 1),
        (430, 406, N'Chè chuối nướng nước cốt dừa Nam Bộ', 38000, N'Chuối sứ bọc nếp nướng thơm phức trong lá chuối, chan nước cốt dừa béo ngậy rắc mè rang.', 'https://images.unsplash.com/photo-1551024709-8f23befc6f87?auto=format&fit=crop&w=400&q=80', 0),

        -- QUÁN 407: QUÁN CHAY AN LẠC TÂM (5 MÓN)
        (431, 407, N'Mì Quảng chay An Lạc Tâm đặc biệt', 45000, N'Sợi mì vàng óng nghệ, nhân đậu hũ ram, nấm đông cô, nước nhưn sánh đậm đà kèm bánh tráng mè nướng.', 'https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=400&q=80', 1),
        (432, 407, N'Bún nghệ xào lòng chay củ nén', 40000, N'Món ăn dân dã Quảng Nam với bún gạo xào nghệ tươi, mì căn dai dai và củ nén phi thơm ngào ngạt.', 'https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=400&q=80', 1),
        (433, 407, N'Cơm chiên ngọc bích hạt sen tươi', 50000, N'Cơm tơi xốp quyện màu xanh cải bó xôi tự nhiên, hạt sen mềm bùi cùng bắp ngọt và cà rốt.', 'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=400&q=80', 0),
        (434, 407, N'Nấm rơm kho tiêu củ kiệu đượm vị', 55000, N'Nấm rơm búp tươi giòn kho keo với tiêu sọ, nước dừa xiêm và củ kiệu giòn cay dịu.', 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=400&q=80', 0),
        (435, 407, N'Sữa bắp nếp non tự nấu An Lạc', 20000, N'Bắp non thu hoạch tại Hội An xay sánh mịn, thơm ngọt thanh lành tự nhiên.', 'https://images.unsplash.com/photo-1550583724-b2692b85b150?auto=format&fit=crop&w=400&q=80', 0),

        -- QUÁN 408: NHÀ HÀNG CHAY THIỀN TÂM (5 MÓN)
        (436, 408, N'Mẹt bánh Huế chay (Bánh bèo, nậm, lọc)', 65000, N'Bộ ba đặc sản cố đô làm từ bột gạo, bột lọc với nhân nấm mèo, cà rốt và đậu xanh chấm nước mắm chay.', 'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=400&q=80', 1),
        (437, 408, N'Cơm sen cung đình chay ngũ sắc', 95000, N'Món ăn tiến vua với hạt sen Tịnh Tâm, nấm hương, chả lụa chay thái hạt lựu ướp gia vị tinh tế.', 'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=400&q=80', 1),
        (438, 408, N'Canh vả nấu sườn non chay lá lốt', 60000, N'Quả vả đồi Thủy Xuân thái lát mỏng nấu sườn chay, tỏa hương thơm ngát của lá lốt vườn nhà.', 'https://images.unsplash.com/photo-1547592166-23ac45744acd?auto=format&fit=crop&w=400&q=80', 0),
        (439, 408, N'Vả trộn mè xúc bánh tráng nướng', 55000, N'Vả luộc thái mỏng bóp chua ngọt cùng rau răm, mè rang vàng, xúc cùng bánh tráng mè nướng giòn rụm.', 'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=400&q=80', 0),
        (440, 408, N'Chè hạt sen long nhãn bọc bột lọc', 35000, N'Hạt sen chín bở lồng khéo léo trong cùi nhãn ngọt thơm, nước đường phèn hoa bưởi thanh khiết.', 'https://images.unsplash.com/photo-1551024709-8f23befc6f87?auto=format&fit=crop&w=400&q=80', 0)
) AS source (Item_ID, Venue_ID, Dish_Name, Price_VND, Description, Image_Url, Is_Signature)
ON target.Item_ID = source.Item_ID
WHEN MATCHED THEN
    UPDATE SET 
        target.Venue_ID = source.Venue_ID,
        target.Dish_Name = source.Dish_Name,
        target.Price_VND = source.Price_VND,
        target.Description = source.Description,
        target.Image_Url = source.Image_Url,
        target.Is_Signature = source.Is_Signature
WHEN NOT MATCHED THEN
    INSERT (Item_ID, Venue_ID, Dish_Name, Price_VND, Description, Image_Url, Is_Signature)
    VALUES (source.Item_ID, source.Venue_ID, source.Dish_Name, source.Price_VND, source.Description, source.Image_Url, source.Is_Signature);

SET IDENTITY_INSERT dbo.VenueMenuItems OFF;

-- ============================================================================
-- PHẦN 3: 05 SẢN PHẨM QUÉT MÃ VẠCH EAN-13 (PRODUCTS, E-NUMBERS, ALLERGENS)
-- ============================================================================
PRINT N'   [3/5] Nạp 05 Sản phẩm quét mã Barcode EAN-13 (Products, E-Numbers, Allergens)...';

SET IDENTITY_INSERT dbo.Products ON;

MERGE INTO dbo.Products AS target
USING (
    VALUES
        (
            401, 
            '8934563138175', 
            N'Mì chay Vifon Lẩu Thái chay 65g', 
            N'Vifon', 
            N'Vắt mì: Bột mì, dầu cọ tinh luyện, tinh bột khoai mì, muối ăn, đường kính, chất ổn định (E412), chất tạo xốp (E500). Gói gia vị: Muối, dầu cọ, đường, ớt, sả, riềng, chất điều vị (E621), chất điều chỉnh độ acid (E330), phẩm màu tự nhiên (E150d).', 
            'https://res.cloudinary.com/swrsuenp/image/upload/v1792252794/veggielife/products/mi_chay_vifon_lauthai.jpg', 
            'VEGAN', 
            1, 
            DATEADD(DAY, -20, @Now), 
            @Now
        ),
        (
            402, 
            '8936036021442', 
            N'Hạt nêm nấm chay Maggi 450g', 
            N'Maggi', 
            N'Muối i-ốt, chất điều vị (E621, E627, E631), đường kính, tinh bột sắn biến tính, dầu thực vật tinh luyện, hương nấm tổng hợp, bột nấm đông cô chiết xuất tự nhiên (1.5%), màu caramel tự nhiên (E150a).', 
            'https://res.cloudinary.com/swrsuenp/image/upload/v1792252792/veggielife/products/hat_nem_nam_maggi.jpg', 
            'VEGAN', 
            1, 
            DATEADD(DAY, -18, @Now), 
            @Now
        ),
        (
            403, 
            '8934680031522', 
            N'Bánh quy mặn AFC Dinh Dưỡng Vị Rau Cải 172g', 
            N'Kinh Đô AFC', 
            N'Bột mì, dầu cọ tinh luyện, đường kính trắng, vảy rau cải sấy khô (2%), bơ động vật từ sữa bò (3%), bột whey sữa nguyên kem, muối ăn, chất nhũ hóa (E322), chất tạo xốp (E500, E503).', 
            'https://res.cloudinary.com/swrsuenp/image/upload/v1792252795/veggielife/products/banh_afc_raucai.jpg', 
            'NON_VEGAN', 
            1, 
            DATEADD(DAY, -15, @Now), 
            @Now
        ),
        (
            404, 
            '8934673592184', 
            N'Sữa hạt đậu nành hữu cơ Vinamilk Organic 180ml', 
            N'Vinamilk', 
            N'Nước, hạt đậu nành hữu cơ 100% Non-GMO (chứng nhận hữu cơ châu Âu EU Organic), đường mía hữu cơ nguyên chất, muối biển tinh khiết, chất làm dày & ổn định nguồn gốc thực vật (E415, E407).', 
            'https://res.cloudinary.com/swrsuenp/image/upload/v1792252789/veggielife/products/sua_dau_nanh_vinamilk_organic.jpg', 
            'VEGAN', 
            1, 
            DATEADD(DAY, -12, @Now), 
            @Now
        ),
        (
            405, 
            '8850718801243', 
            N'Snack Rong Biển Nướng Vị Cay Big Roll Tao Kae Noi 3.6g', 
            N'Tao Kae Noi', 
            N'Rong biển tự nhiên sấy nướng (95%), đường kính, bột ớt cay hun khói, bột gia vị nướng, chất điều vị (E627, E631 nguồn gốc phụ gia chưa xác minh xuất xứ vi sinh hay động vật), muối ăn.', 
            'https://res.cloudinary.com/swrsuenp/image/upload/v1792252796/veggielife/products/snack_rongbien_taokaenoi.jpg', 
            'DOUBTFUL', 
            0, 
            DATEADD(DAY, -10, @Now), 
            @Now
        )
) AS source (Product_ID, Barcode, Product_Name, Brand, Ingredients_Text, Image_Url, Vegan_Verdict, Admin_Verified, Created_At, Last_Synced_At)
ON target.Product_ID = source.Product_ID
WHEN MATCHED THEN
    UPDATE SET 
        target.Barcode = source.Barcode,
        target.Product_Name = source.Product_Name,
        target.Brand = source.Brand,
        target.Ingredients_Text = source.Ingredients_Text,
        target.Image_Url = source.Image_Url,
        target.Vegan_Verdict = source.Vegan_Verdict,
        target.Admin_Verified = source.Admin_Verified,
        target.Last_Synced_At = source.Last_Synced_At
WHEN NOT MATCHED THEN
    INSERT (Product_ID, Barcode, Product_Name, Brand, Ingredients_Text, Image_Url, Vegan_Verdict, Admin_Verified, Created_At, Last_Synced_At)
    VALUES (source.Product_ID, source.Barcode, source.Product_Name, source.Brand, source.Ingredients_Text, source.Image_Url, source.Vegan_Verdict, source.Admin_Verified, source.Created_At, source.Last_Synced_At);

SET IDENTITY_INSERT dbo.Products OFF;

-- Ánh xạ Phụ gia thực phẩm E-Number cho 5 sản phẩm (Product_ENumbers)
MERGE INTO dbo.Product_ENumbers AS target
USING (
    VALUES
        -- 401: Mì Vifon (E621, E330, E150d, E412, E500)
        (401, 'E621', @Now),
        (401, 'E330', @Now),
        (401, 'E150d', @Now),
        (401, 'E412', @Now),
        (401, 'E500', @Now),
        -- 402: Hạt nêm Maggi (E621, E627, E631, E150a)
        (402, 'E621', @Now),
        (402, 'E627', @Now),
        (402, 'E631', @Now),
        (402, 'E150a', @Now),
        -- 403: Bánh AFC (E322, E500, E503)
        (403, 'E322', @Now),
        (403, 'E500', @Now),
        (403, 'E503', @Now),
        -- 404: Sữa đậu nành Organic (E415, E407)
        (404, 'E415', @Now),
        (404, 'E407', @Now),
        -- 405: Snack rong biển (E627, E631)
        (405, 'E627', @Now),
        (405, 'E631', @Now)
) AS source (Product_ID, Code, Created_At)
ON target.Product_ID = source.Product_ID AND target.Code = source.Code
WHEN NOT MATCHED THEN
    INSERT (Product_ID, Code, Created_At) VALUES (source.Product_ID, source.Code, source.Created_At);

-- Ánh xạ Nhóm chất dị ứng thực phẩm cho 5 sản phẩm (ProductAllergens)
-- Kế thừa ID an toàn từ bảng master Allergens qua Code
MERGE INTO dbo.ProductAllergens AS target
USING (
    SELECT p.Product_ID, a.Allergen_ID, map.Presence_Type
    FROM (
        VALUES
            (401, 'GLUTEN', 'CONTAINS'),
            (401, 'SOY', 'MAY_CONTAIN'),
            (402, 'SOY', 'MAY_CONTAIN'),
            (402, 'GLUTEN', 'MAY_CONTAIN'),
            (403, 'DAIRY', 'CONTAINS'),
            (403, 'GLUTEN', 'CONTAINS'),
            (403, 'SOY', 'CONTAINS'),
            (404, 'SOY', 'CONTAINS'),
            (405, 'SOY', 'CONTAINS'),
            (405, 'GLUTEN', 'MAY_CONTAIN')
    ) AS map (Product_ID, Allergen_Code, Presence_Type)
    JOIN dbo.Products p ON p.Product_ID = map.Product_ID
    JOIN dbo.Allergens a ON a.Code = map.Allergen_Code
) AS source (Product_ID, Allergen_ID, Presence_Type)
ON target.Product_ID = source.Product_ID AND target.Allergen_ID = source.Allergen_ID
WHEN MATCHED THEN
    UPDATE SET target.Presence_Type = source.Presence_Type
WHEN NOT MATCHED THEN
    INSERT (Product_ID, Allergen_ID, Presence_Type)
    VALUES (source.Product_ID, source.Allergen_ID, source.Presence_Type);

-- ============================================================================
-- PHẦN 4: 01 PHIÊN TỦ LẠNH THÔNG MINH (FRIDGESESSIONS & FRIDGEINVENTORY)
-- ============================================================================
PRINT N'   [4/5] Nạp 01 Phiên Tủ Lạnh thông minh (FridgeSessions, FridgeInventory)...';

-- Xác định User_ID an toàn (User 2: nguyenvana@veggielife.vn, fallback User 1)
DECLARE @TargetUserID INT = (SELECT TOP 1 User_ID FROM dbo.Users WHERE Email = 'nguyenvana@veggielife.vn');
IF @TargetUserID IS NULL
    SET @TargetUserID = (SELECT TOP 1 User_ID FROM dbo.Users WHERE Role IN ('MEMBER', 'VIP'));
IF @TargetUserID IS NULL
    SET @TargetUserID = (SELECT TOP 1 User_ID FROM dbo.Users);

SET IDENTITY_INSERT dbo.FridgeSessions ON;

MERGE INTO dbo.FridgeSessions AS target
USING (
    VALUES
        (
            401, 
            @TargetUserID, 
            'https://images.unsplash.com/photo-1584473457406-6240486418e9?auto=format&fit=crop&w=800&q=80', 
            5, 
            DATEADD(HOUR, -3, @Now)
        )
) AS source (Session_ID, User_ID, Image_CDN_URL, Detected_Items_Count, Created_At)
ON target.Session_ID = source.Session_ID
WHEN MATCHED THEN
    UPDATE SET 
        target.User_ID = source.User_ID,
        target.Image_CDN_URL = source.Image_CDN_URL,
        target.Detected_Items_Count = source.Detected_Items_Count
WHEN NOT MATCHED THEN
    INSERT (Session_ID, User_ID, Image_CDN_URL, Detected_Items_Count, Created_At)
    VALUES (source.Session_ID, source.User_ID, source.Image_CDN_URL, source.Detected_Items_Count, source.Created_At);

SET IDENTITY_INSERT dbo.FridgeSessions OFF;

-- Nạp 5 nguyên liệu phát hiện được trong tủ lạnh (FridgeInventory)
-- Tra cứu linh hoạt Ingredient_ID từ dbo.Ingredients theo từ khóa
SET IDENTITY_INSERT dbo.FridgeInventory ON;

MERGE INTO dbo.FridgeInventory AS target
USING (
    SELECT 
        raw.Inventory_ID,
        @TargetUserID AS User_ID,
        COALESCE(ing.Ingredient_ID, raw.Fallback_Ing_ID) AS Ingredient_ID,
        401 AS Session_ID,
        raw.Custom_Item_Name,
        raw.Freshness_Status,
        raw.Quantity,
        raw.Unit
    FROM (
        VALUES
            (401, N'%Nấm đùi gà%', 56, N'Nấm đùi gà tươi', 'FRESH', CAST(200.00 AS DECIMAL(8,2)), N'g'),
            (402, N'%Đậu hũ non%', 31, N'Đậu hũ non Silken Tofu', 'FRESH', CAST(300.00 AS DECIMAL(8,2)), N'g'),
            (403, N'%Bông cải xanh%', 98, N'Bông cải xanh (Broccoli)', 'FRESH', CAST(250.00 AS DECIMAL(8,2)), N'g'),
            (404, N'%Cà chua chín%', 83, N'Cà chua chín đỏ (cần dùng sớm)', 'NON-FRESH', CAST(300.00 AS DECIMAL(8,2)), N'g'),
            (405, N'%Nấm kim châm%', 57, N'Nấm kim châm tươi', 'FRESH', CAST(150.00 AS DECIMAL(8,2)), N'g')
    ) AS raw (Inventory_ID, Keyword, Fallback_Ing_ID, Custom_Item_Name, Freshness_Status, Quantity, Unit)
    OUTER APPLY (
        SELECT TOP 1 i.Ingredient_ID 
        FROM dbo.Ingredients i 
        WHERE i.Ingredient_Name LIKE raw.Keyword
        ORDER BY i.Ingredient_ID ASC
    ) ing
) AS source (Inventory_ID, User_ID, Ingredient_ID, Session_ID, Custom_Item_Name, Freshness_Status, Quantity, Unit)
ON target.Inventory_ID = source.Inventory_ID
WHEN MATCHED THEN
    UPDATE SET 
        target.User_ID = source.User_ID,
        target.Ingredient_ID = source.Ingredient_ID,
        target.Session_ID = source.Session_ID,
        target.Custom_Item_Name = source.Custom_Item_Name,
        target.Freshness_Status = source.Freshness_Status,
        target.Quantity = source.Quantity,
        target.Unit = source.Unit
WHEN NOT MATCHED THEN
    INSERT (Inventory_ID, User_ID, Ingredient_ID, Session_ID, Custom_Item_Name, Freshness_Status, Quantity, Unit)
    VALUES (source.Inventory_ID, source.User_ID, source.Ingredient_ID, source.Session_ID, source.Custom_Item_Name, source.Freshness_Status, source.Quantity, source.Unit);

SET IDENTITY_INSERT dbo.FridgeInventory OFF;

-- ============================================================================
-- PHẦN 5: 01 PHIÊN TƯ VẤN AI CHATBOT & KHẢO SÁT (CHATSESSIONS, CHATMESSAGES, SURVEYS)
-- ============================================================================
PRINT N'   [5/5] Nạp 01 Phiên AI Chat Assistant & Khảo sát (ChatSessions, ChatMessages, Surveys)...';

DECLARE @TargetProfileID INT = (SELECT TOP 1 User_ID FROM dbo.HealthProfiles WHERE User_ID = @TargetUserID);

SET IDENTITY_INSERT dbo.ChatSessions ON;

MERGE INTO dbo.ChatSessions AS target
USING (
    VALUES
        (
            401, 
            @TargetUserID, 
            @TargetProfileID, 
            NULL, 
            N'Gợi ý thực đơn vét tủ lạnh Empty the Fridge', 
            'NUTRITION_CONSULTATION', 
            DATEADD(MINUTE, -135, @Now), 
            DATEADD(MINUTE, -120, @Now)
        )
) AS source (Chat_Session_ID, User_ID, Profiles_ID, Guest_UUID, Session_Title, Context_Type, Started_At, Updated_At)
ON target.Chat_Session_ID = source.Chat_Session_ID
WHEN MATCHED THEN
    UPDATE SET 
        target.User_ID = source.User_ID,
        target.Profiles_ID = source.Profiles_ID,
        target.Session_Title = source.Session_Title,
        target.Context_Type = source.Context_Type,
        target.Updated_At = source.Updated_At
WHEN NOT MATCHED THEN
    INSERT (Chat_Session_ID, User_ID, Profiles_ID, Guest_UUID, Session_Title, Context_Type, Started_At, Updated_At)
    VALUES (source.Chat_Session_ID, source.User_ID, source.Profiles_ID, source.Guest_UUID, source.Session_Title, source.Context_Type, source.Started_At, source.Updated_At);

SET IDENTITY_INSERT dbo.ChatSessions OFF;

-- Nạp 4 tin nhắn đối thoại người dùng và AI Trợ lý dinh dưỡng (ChatMessages)
SET IDENTITY_INSERT dbo.ChatMessages ON;

MERGE INTO dbo.ChatMessages AS target
USING (
    VALUES
        (
            401, 
            401, 
            'USER', 
            N'Chào VeggieBot! Tủ lạnh nhà mình hiện còn nấm đùi gà, đậu hũ non, bông cải xanh và cà chua chín sắp mềm. Bạn gợi ý giúp mình một bữa tối chay thanh đạm, nấu nhanh dưới 20 phút tận dụng hết các nguyên liệu này được không?', 
            NULL, 
            DATEADD(MINUTE, -134, @Now)
        ),
        (
            402, 
            401, 
            'ASSISTANT', 
            N'Chào bạn! VeggieBot rất vui được đồng hành cùng bạn giải cứu thực phẩm hôm nay. Với tính năng "Empty the Fridge", bạn đang có sự kết hợp nguyên liệu tuyệt vời: Đậu hũ non (giàu protein thực vật mềm mịn), Nấm đùi gà (kết cấu dai giòn ngọt tự nhiên), Bông cải xanh (giàu chất xơ và Vitamin C), cùng Cà chua chín đỏ (chứa lycopene chống oxy hóa, cần ưu tiên nấu trước để tránh ủng hỏng).

VeggieBot gợi ý thực đơn 2 món hoàn thiện trong 18 phút:
1. Canh Đậu Hũ Non Nấu Cà Chua & Nấm Kim Châm:
   - Vị chua dịu tự nhiên từ cà chua xào thơm, quyện cùng nước dùng nấm ngọt lịm và từng lát đậu hũ non béo mềm.
   - Thời gian nấu: 10 phút.
2. Nấm Đùi Gà Áp Chảo Xào Bông Cải Xanh Dầu Hào Chay:
   - Nấm đùi gà thái lát chéo áp chảo xém vàng thơm lừng, xào lửa lớn cùng bông cải xanh giòn sần sật, nêm 1 thìa dầu hào chay và rắc chút tiêu đen.
   - Thời gian nấu: 8 phút.

Tổng lượng calo ước tính cho bữa tối: ~385 kcal, Protein: 22g, Chất xơ: 8.5g – rất cân đối và phù hợp với mục tiêu duy trì thể trạng năng động của bạn!

Khuyến nghị mang tính chất tham khảo dinh dưỡng, vui lòng tham vấn ý kiến bác sĩ chuyên khoa đối với các bệnh lý nền.', 
            N'{"tokensConsumed":520,"modelVersion":"gemini-2.5-flash","promptType":"EMPTY_THE_FRIDGE","hasMedicalDisclaimer":true,"suggestedCookingTimeMinutes":18}', 
            DATEADD(MINUTE, -132, @Now)
        ),
        (
            403, 
            401, 
            'USER', 
            N'Cảm ơn bot nhiều! Nếu mình muốn thêm chút vị cay nhẹ vào món nấm đùi gà thì nên nêm gì để không phá vị ngọt tự nhiên của nấm?', 
            NULL, 
            DATEADD(MINUTE, -128, @Now)
        ),
        (
            404, 
            401, 
            'ASSISTANT', 
            N'Để tạo vị cay ấm nồng hài hòa mà vẫn tôn trọn vị ngọt thanh của nấm đùi gà, VeggieBot gợi ý 2 lựa chọn tuyệt hảo:
1. Tiêu đen Phú Quốc đập dập hoặc tiêu xanh tươi: Rắc vào chảo ở 30 giây cuối cùng trước khi tắt bếp. Tinh dầu piperine trong tiêu tạo độ cay ấm nhẹ nhàng kích thích vị giác và khử tính hàn của nấm cực tốt.
2. Ớt sừng băm nhuyễn bỏ hạt: Xào thơm cùng chút boa-rô trước khi cho nấm vào. Ớt sừng cho màu đỏ rực rỡ bắt mắt nhưng độ cay chỉ ở mức êm dịu, không nồng gắt như ớt hiểm.

Chúc bạn có một bữa tối chay ấm cúng, trọn vị và tràn đầy năng lượng an lành!', 
            N'{"tokensConsumed":380,"modelVersion":"gemini-2.5-flash","promptType":"RECIPE_TUNING","hasMedicalDisclaimer":false}', 
            DATEADD(MINUTE, -125, @Now)
        )
) AS source (Message_ID, Chat_Session_ID, Sender_Type, Content, Metadata_JSON, Created_At)
ON target.Message_ID = source.Message_ID
WHEN MATCHED THEN
    UPDATE SET 
        target.Chat_Session_ID = source.Chat_Session_ID,
        target.Sender_Type = source.Sender_Type,
        target.Content = source.Content,
        target.Metadata_JSON = source.Metadata_JSON
WHEN NOT MATCHED THEN
    INSERT (Message_ID, Chat_Session_ID, Sender_Type, Content, Metadata_JSON, Created_At)
    VALUES (source.Message_ID, source.Chat_Session_ID, source.Sender_Type, source.Content, source.Metadata_JSON, source.Created_At);

SET IDENTITY_INSERT dbo.ChatMessages OFF;

-- Nạp 01 Phiếu khảo sát đánh giá chất lượng phản hồi AI (Surveys)
SET IDENTITY_INSERT dbo.Surveys ON;

MERGE INTO dbo.Surveys AS target
USING (
    VALUES
        (
            401, 
            @TargetUserID, 
            401, 
            5, 
            N'Chất lượng gợi ý món ăn từ tủ lạnh và tính hữu ích của trợ lý dinh dưỡng AI', 
            N'Gợi ý cực kỳ thực tế và nhanh chóng! Món canh cà chua đậu hũ non và nấm xào bông cải nấu đúng 18 phút, vị vừa vặn, nguyên liệu giải quyết đúng mớ đồ sắp hỏng trong tủ lạnh mà không bị bỏ phí.', 
            DATEADD(MINUTE, -120, @Now)
        )
) AS source (Survey_ID, User_ID, Session_ID, Rating_Score, Context_Question, Feedback_Content, Created_At)
ON target.Survey_ID = source.Survey_ID
WHEN MATCHED THEN
    UPDATE SET 
        target.User_ID = source.User_ID,
        target.Session_ID = source.Session_ID,
        target.Rating_Score = source.Rating_Score,
        target.Context_Question = source.Context_Question,
        target.Feedback_Content = source.Feedback_Content
WHEN NOT MATCHED THEN
    INSERT (Survey_ID, User_ID, Session_ID, Rating_Score, Context_Question, Feedback_Content, Created_At)
    VALUES (source.Survey_ID, source.User_ID, source.Session_ID, source.Rating_Score, source.Context_Question, source.Feedback_Content, source.Created_At);

SET IDENTITY_INSERT dbo.Surveys OFF;

PRINT N'============================================================================';
PRINT N'  HOÀN TẤT NẠP TOÀN BỘ SEED DATA THÀNH VIÊN 4 (ID: 401 - 499) THÀNH CÔNG!';
PRINT N'============================================================================';
GO

