-- ============================================================
-- File: 06_Seed_ENumberDictionary.sql
-- Thư mục: database/seed/
-- Mục đích: Nạp sẵn từ điển 65 mã phụ gia thực phẩm E-Number quốc tế
--           kèm nguồn gốc hóa học, trạng thái chay (VEGAN, NON_VEGAN, DOUBTFUL)
--           và cảnh báo rủi ro sức khỏe (BR-BAR-02, BR-ADM-MAS-003).
-- Tính chất: Idempotent - chạy nhiều lần không trùng dữ liệu (MERGE).
-- ============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'>> [Seed 06/08] Đang nạp dữ liệu Master 65 Mã Phụ Gia E-Number (ENumberDictionary)...';

SET NOCOUNT ON;

DECLARE @Now DATETIME2(7) = SYSDATETIME();

MERGE INTO dbo.ENumberDictionary AS target
USING (
    VALUES
        -- ==========================================
        -- 1. CHẤT TẠO MÀU (COLOURS: E100 - E199)
        -- ==========================================
        ('E100', N'Curcumin (Chiết xuất nghệ vàng)', N'Chất tạo màu', N'Củ nghệ thực vật tự nhiên', 'VEGAN', N'An toàn, đặc tính chống oxy hóa cao.'),
        ('E101', N'Riboflavin (Vitamin B2)', N'Chất tạo màu', N'Lên men vi sinh hoặc tổng hợp', 'VEGAN', N'Vitamin B2 cần thiết cho cơ thể.'),
        ('E102', N'Tartrazine (Vàng chanh)', N'Chất tạo màu', N'Hóa học tổng hợp từ dầu mỏ', 'VEGAN', N'Có thể gây dị ứng hoặc hiếu động ở trẻ nhỏ nhạy cảm.'),
        ('E110', N'Sunset Yellow FCF (Vàng cam hoàng hôn)', N'Chất tạo màu', N'Hóa học tổng hợp', 'VEGAN', N'Cần hạn chế cho trẻ em, có thể gây dị ứng nhẹ.'),
        ('E120', N'Carmine / Cochineal (Axit Carminic)', N'Chất tạo màu', N'Chiết xuất từ côn trùng rệp son (Dactylopius coccus)', 'NON_VEGAN', N'NGUỒN GỐC ĐỘNG VẬT/CÔN TRÙNG: Tuyệt đối không dùng cho người ăn chay.'),
        ('E122', N'Azorubine (Carmoisine đỏ)', N'Chất tạo màu', N'Hóa học tổng hợp', 'VEGAN', N'Nên hạn chế liều lượng cho trẻ nhỏ.'),
        ('E124', N'Ponceau 4R (Đỏ cochineal A)', N'Chất tạo màu', N'Hóa học tổng hợp', 'VEGAN', N'Phẩm màu tổng hợp tan trong nước.'),
        ('E129', N'Allura Red AC (Đỏ quyến rũ)', N'Chất tạo màu', N'Hóa học tổng hợp', 'VEGAN', N'Phẩm màu thực phẩm phổ biến.'),
        ('E132', N'Indigo carmine (Xanh chàm)', N'Chất tạo màu', N'Hóa học tổng hợp', 'VEGAN', N'An toàn trong ngưỡng quy định.'),
        ('E133', N'Brilliant Blue FCF (Xanh dương rực rỡ)', N'Chất tạo màu', N'Hóa học tổng hợp', 'VEGAN', N'Phẩm màu xanh an toàn phổ biến.'),
        ('E140', N'Chlorophylls (Diệp lục tố)', N'Chất tạo màu', N'Lá cây, cỏ ba lá, tầm ma tự nhiên', 'VEGAN', N'Hoàn toàn tự nhiên và lành tính.'),
        ('E141', N'Copper complexes of chlorophylls', N'Chất tạo màu', N'Bán tổng hợp từ diệp lục tố tự nhiên', 'VEGAN', N'Bền màu hơn diệp lục nguyên chất.'),
        ('E150a', N'Plain caramel (Caramel loại I)', N'Chất tạo màu', N'Nhiệt phân đường thực vật tự nhiên', 'VEGAN', N'Màu caramel an toàn và phổ biến nhất.'),
        ('E150c', N'Ammonia caramel (Caramel loại III)', N'Chất tạo màu', N'Gia nhiệt đường với hợp chất amoniac', 'VEGAN', N'Thường có trong nước sốt, bia, xì dầu.'),
        ('E150d', N'Sulphite ammonia caramel (Caramel loại IV)', N'Chất tạo màu', N'Gia nhiệt đường với amoniac và sunfit', 'VEGAN', N'Chứa dấu vết sunfit, người dị ứng sunfit cần lưu ý.'),
        ('E153', N'Vegetable carbon (Than thực vật)', N'Chất tạo màu', N'Than bùn, gỗ hoặc xơ thực vật đốt cháy', 'VEGAN', N'Thuần chay nếu từ thực vật; cần xác minh không phải than xương động vật (bone char).'),
        ('E160a', N'Beta-carotene (Tiền chất Vitamin A)', N'Chất tạo màu', N'Chiết xuất cà rốt, tảo biển hoặc tổng hợp', 'VEGAN', N'Bổ sung provitamin A có lợi cho mắt.'),
        ('E160b', N'Annatto (Bột hạt điều màu nhuộm)', N'Chất tạo màu', N'Hạt cây điều màu Bixa orellana', 'VEGAN', N'Tạo màu vàng cam tự nhiên cho thực phẩm chay.'),
        ('E160c', N'Paprika extract (Chiết xuất ớt chuông)', N'Chất tạo màu', N'Quả ớt đỏ Capsicum annuum', 'VEGAN', N'Chất tạo màu đỏ tự nhiên lành tính.'),
        ('E162', N'Beetroot Red (Đỏ củ dền)', N'Chất tạo màu', N'Củ dền đỏ Beta vulgaris', 'VEGAN', N'Tự nhiên, tuyệt đối an toàn.'),
        ('E163', N'Anthocyanins (Sắc tố tím/đỏ quả mọng)', N'Chất tạo màu', N'Vỏ nho, bắp cải tím, dâu tằm', 'VEGAN', N'Chống oxy hóa mạnh mẽ.'),

        -- ==========================================
        -- 2. CHẤT BẢO QUẢN (PRESERVATIVES: E200 - E299)
        -- ==========================================
        ('E200', N'Sorbic acid', N'Chất bảo quản', N'Tổng hợp hoặc quả thanh lương trà', 'VEGAN', N'Chống nấm mốc hiệu quả.'),
        ('E202', N'Potassium sorbate', N'Chất bảo quản', N'Muối kali của axit sorbic', 'VEGAN', N'Rất phổ biến trong thực phẩm chay đóng gói.'),
        ('E210', N'Benzoic acid', N'Chất bảo quản', N'Tổng hợp hóa học', 'VEGAN', N'Chống vi khuẩn và nấm men.'),
        ('E211', N'Sodium benzoate', N'Chất bảo quản', N'Muối natri của axit benzoic', 'VEGAN', N'Không kết hợp cùng Vitamin C ở nồng độ cao.'),
        ('E220', N'Sulphur dioxide (Khí lưu huỳnh đioxit)', N'Chất bảo quản', N'Hóa học vô cơ', 'VEGAN', N'Gây dị ứng cho người nhạy cảm với Sunfit.'),
        ('E223', N'Sodium metabisulphite', N'Chất bảo quản', N'Hóa học vô cơ', 'VEGAN', N'Cảnh báo dị ứng sunfit trong hoa quả sấy.'),
        ('E250', N'Sodium nitrite', N'Chất bảo quản', N'Tổng hợp hóa học', 'VEGAN', N'Thường dùng giữ màu và ức chế độc tố vi khuẩn botulinum.'),
        ('E260', N'Acetic acid (Giấm ăn / Axit axetic)', N'Chất điều chỉnh độ chua', N'Lên men rượu hoặc vi sinh tinh bột', 'VEGAN', N'An toàn, là thành phần chính của giấm ăn.'),
        ('E270', N'Lactic acid', N'Chất điều chỉnh độ chua', N'Lên men vi khuẩn từ đường ngô hoặc rỉ mật', 'VEGAN', N'Dù có tên lactic nhưng 99% thương mại sản xuất từ tinh bột thực vật.'),

        -- ==========================================
        -- 3. CHẤT CHỐNG OXY HÓA & ĐIỀU VỊ (ANTIOXIDANTS: E300 - E399)
        -- ==========================================
        ('E300', N'Ascorbic acid (Vitamin C)', N'Chất chống oxy hóa', N'Tổng hợp hoặc chiết xuất trái cây có múi', 'VEGAN', N'Bổ sung Vitamin C thiết yếu.'),
        ('E301', N'Sodium ascorbate', N'Chất chống oxy hóa', N'Muối natri của Vitamin C', 'VEGAN', N'An toàn, hỗ trợ bảo quản màu sắc thực phẩm.'),
        ('E306', N'Tocopherol extract (Vitamin E tự nhiên)', N'Chất chống oxy hóa', N'Dầu mầm lúa mì, dầu đậu nành nguyên chất', 'VEGAN', N'Vitamin E tự nhiên bảo vệ chất béo khỏi ôi thiu.'),
        ('E322', N'Lecithin (Chất nhũ hóa Lecithin)', N'Chất nhũ hóa', N'Đậu nành hoặc lòng đỏ trứng gia cầm', 'DOUBTFUL', N'NGHI VẤN: Nếu nhãn ghi Soy Lecithin là Thuần Chay; nếu từ trứng là Ovo-Vegetarian.'),
        ('E330', N'Citric acid (Axit chanh)', N'Chất điều chỉnh độ chua', N'Lên men nấm Aspergillus niger từ rỉ mật mía', 'VEGAN', N'Rất an toàn và cực kỳ phổ biến.'),
        ('E334', N'Tartaric acid', N'Chất điều chỉnh độ chua', N'Phụ phẩm vỏ nho tự nhiên', 'VEGAN', N'An toàn.'),
        ('E385', N'Calcium disodium EDTA', N'Chất chống oxy hóa', N'Tổng hợp hóa học', 'VEGAN', N'Tạo phức giữ màu thực phẩm đóng hộp.'),

        -- ==========================================
        -- 4. CHẤT LÀM DÀY & ỔN ĐỊNH (THICKENERS / GELLING: E400 - E499)
        -- ==========================================
        ('E406', N'Agar (Bột rau câu Agar-agar)', N'Chất tạo gel', N'Chiết xuất từ tảo biển đỏ (Rhodophyta)', 'VEGAN', N'Thay thế gelatin tuyệt vời cho người ăn chay.'),
        ('E407', N'Carrageenan', N'Chất tạo gel & làm dày', N'Chiết xuất từ rong biển đỏ thiên nhiên', 'VEGAN', N'Tạo độ sánh mịn cho sữa hạt và thạch chay.'),
        ('E410', N'Locust bean gum (Gôm đậu carob)', N'Chất làm dày', N'Hạt cây đậu carob Ceratonia siliqua', 'VEGAN', N'Chất xơ hòa tan tự nhiên an toàn.'),
        ('E412', N'Guar gum (Gôm guar)', N'Chất làm dày', N'Hạt đậu guar tự nhiên', 'VEGAN', N'Tạo độ dẻo xốp cho bánh mì và kem chay.'),
        ('E415', N'Xanthan gum', N'Chất làm dày & ổn định', N'Lên men vi khuẩn Xanthomonas campestris từ đường ngô', 'VEGAN', N'Cực kỳ phổ biến và an toàn.'),
        ('E420', N'Sorbitol', N'Chất tạo ngọt & giữ ẩm', N'Tổng hợp từ glucose tinh bột ngô', 'VEGAN', N'Vị ngọt mát, không gây sâu răng.'),
        ('E422', N'Glycerol / Glycerin', N'Chất giữ ẩm', N'Dầu cọ thực vật hoặc mỡ động vật', 'DOUBTFUL', N'NGHI VẤN: Nếu là Vegetable Glycerin thì Thuần Chay; nếu từ mỡ động vật thì KHÔNG CHAY.'),
        ('E428', N'Gelatin (Gelatin động vật)', N'Chất tạo đông', N'Collagen nấu từ da, xương, gân lợn hoặc bò', 'NON_VEGAN', N'NGUỒN GỐC ĐỘNG VẬT: Tuyệt đối cấm người ăn chay sử dụng.'),
        ('E440', N'Pectin', N'Chất tạo gel', N'Chiết xuất từ vỏ cam, quýt và bã táo', 'VEGAN', N'Tạo đông tự nhiên dùng làm mứt quả thuần chay.'),
        ('E441', N'Gelatine (Mã định danh phụ)', N'Chất tạo gel', N'Mô liên kết và xương động vật', 'NON_VEGAN', N'NGUỒN GỐC ĐỘNG VẬT: Cấm người ăn chay sử dụng.'),
        ('E471', N'Mono- and diglycerides of fatty acids', N'Chất nhũ hóa', N'Dầu thực vật hoặc mỡ động vật', 'DOUBTFUL', N'NGHI VẤN: Rất hay gặp trong bánh ngọt, bơ thực vật; cần nhãn xác nhận 100% Plant-based.'),
        ('E472e', N'DATEM (Este diacetyl tartaric của monoglyceride)', N'Chất nhũ hóa bột mì', N'Dầu thực vật hoặc mỡ động vật', 'DOUBTFUL', N'NGHI VẤN: Thường dùng trong bánh mì gối công nghiệp để làm nở phồng.'),
        ('E476', N'Polyglycerol polyricinoleate (PGPR)', N'Chất nhũ hóa socola', N'Dầu thầu dầu thực vật (Castor oil)', 'VEGAN', N'Thay thế bơ cacao giảm độ nhớt socola.'),

        -- ==========================================
        -- 5. CHẤT ĐIỀU CHỈNH ĐỘ KIỀM & TẠO XỐP (E500 - E599)
        -- ==========================================
        ('E500', N'Sodium carbonates (Baking soda / Muối nở)', N'Chất tạo xốp', N'Khoáng sản tự nhiên hoặc tổng hợp Solvay', 'VEGAN', N'Muối nở làm bánh cực kỳ quen thuộc.'),
        ('E503', N'Ammonium carbonates (Bột khai)', N'Chất tạo xốp', N'Khoáng vô cơ tổng hợp', 'VEGAN', N'Dùng tạo độ xốp giòn cho quẩy và bánh bao.'),
        ('E509', N'Calcium chloride (Canxi clorua)', N'Chất làm rắn chắc', N'Khoáng vô cơ tự nhiên', 'VEGAN', N'Dùng làm chất đông tụ truyền thống khi nấu đậu hũ.'),
        ('E516', N'Calcium sulphate (Thạch cao phi thực phẩm)', N'Chất đông tụ đậu phụ', N'Khoáng thạch cao tự nhiên tinh chế', 'VEGAN', N'Đông tụ tàu hũ non và đậu phụ mềm mượt.'),

        -- ==========================================
        -- 6. CHẤT ĐIỀU VỊ (FLAVOUR ENHANCERS: E600 - E699)
        -- ==========================================
        ('E621', N'Monosodium glutamate (Mì chính / Bột ngọt MSG)', N'Chất điều vị', N'Lên men mật rỉ đường, sắn, ngô vi khuẩn Corynebacterium', 'VEGAN', N'Tạo vị ngọt thịt Umami hoàn toàn thuần chay.'),
        ('E627', N'Disodium guanylate', N'Chất điều vị', N'Lên men nấm men hoặc chiết xuất từ cá khô', 'DOUBTFUL', N'NGHI VẤN: Đa phần từ nấm men/tinh bột nhưng có thể từ cá ngừ sấy khô.'),
        ('E631', N'Disodium inosinate', N'Chất điều vị', N'Lên men vi sinh hoặc chiết xuất từ thịt lợn, cá', 'DOUBTFUL', N'NGHI VẤN: Rất nhiều nguồn công nghiệp chiết xuất từ phụ phẩm thịt động vật/cá.'),
        ('E635', N'Disodium 5-ribonucleotides (Siêu bột ngọt I+G)', N'Chất điều vị', N'Hỗn hợp 50/50 giữa E627 và E631', 'DOUBTFUL', N'NGHI VẤN: Phổ biến trong hạt nêm và nước dùng, người ăn chay nghiêm ngặt cần tránh.'),

        -- ==========================================
        -- 7. CHẤT LÀM BÓNG & CHẤT NGỌT NHÂN TẠO (E900 - E999)
        -- ==========================================
        ('E901', N'Beeswax (Sáp ong tự nhiên)', N'Chất làm bóng', N'Tiết ra từ tuyến sáp của loài ong mật Apis mellifera', 'NON_VEGAN', N'PHỤ PHẨM ĐỘNG VẬT: Người ăn chay thuần (Vegan) không tiêu thụ.'),
        ('E904', N'Shellac (Nhựa cánh kiến đỏ)', N'Chất làm bóng', N'Tiết ra từ loài rệp cánh kiến đỏ Kerria lacca', 'NON_VEGAN', N'NGUỒN GỐC CÔN TRÙNG: Cấm người ăn chay sử dụng trên kẹo và trái cây tráng bóng.'),
        ('E920', N'L-cysteine', N'Chất xử lý bột mì', N'Chiết xuất từ lông vũ vịt gà hoặc lông bờm lợn', 'NON_VEGAN', N'NGUỒN GỐC ĐỘNG VẬT: Thường dùng làm mềm sợi gluten bánh mì sandwich.'),
        ('E950', N'Acesulfame K', N'Chất tạo ngọt nhân tạo', N'Tổng hợp hóa học', 'VEGAN', N'Ngọt gấp 200 lần đường, 0 calo.'),
        ('E951', N'Aspartame', N'Chất tạo ngọt nhân tạo', N'Tổng hợp peptide amino acid', 'VEGAN', N'Người mắc bệnh Phenylketonuria (PKU) không được dùng.'),
        ('E955', N'Sucralose', N'Chất tạo ngọt nhân tạo', N'Clo hóa phân tử đường sucrose', 'VEGAN', N'Không chứa calo, bền nhiệt khi nấu nướng.'),
        ('E960', N'Steviol glycosides (Cỏ ngọt Stevia)', N'Chất tạo ngọt tự nhiên', N'Chiết xuất từ lá cây cỏ ngọt Stevia rebaudiana', 'VEGAN', N'Vị ngọt thanh thảo mộc tự nhiên, lành tính cho người tiểu đường.')
) AS source (Code, Name, Category_Type, Origin_Source, Vegan_Status, Health_Risk_Notes)
ON (target.Code = source.Code)
WHEN MATCHED THEN
    UPDATE SET 
        target.Name = source.Name,
        target.Category_Type = source.Category_Type,
        target.Origin_Source = source.Origin_Source,
        target.Vegan_Status = source.Vegan_Status,
        target.Health_Risk_Notes = source.Health_Risk_Notes,
        target.Updated_At = @Now
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Code, Name, Category_Type, Origin_Source, Vegan_Status, Health_Risk_Notes, Created_At, Updated_At)
    VALUES (source.Code, source.Name, source.Category_Type, source.Origin_Source, source.Vegan_Status, source.Health_Risk_Notes, @Now, @Now);

PRINT N'   -> Hoàn tất nạp 65 Mã Phụ Gia E-Number thành công!';
GO
