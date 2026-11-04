SET IDENTITY_INSERT dbo.Foods ON;
INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Created_At)
SELECT x.id, c.Category_ID, x.nm, x.cr FROM (VALUES
(10001, N'Món Mì, Bún & Phở Chay', N'Bún Thái Chua Cay Chay', DATEADD(MINUTE,-86000,@Now)),
(10002, N'Món Mì, Bún & Phở Chay', N'Mì Xào La Hán', DATEADD(MINUTE,-86000,@Now)),
(10003, N'Món Cuốn Chay', N'Nem Rán Ngô Chay', DATEADD(MINUTE,-86000,@Now)),
(10004, N'Món Xào Chay Thanh Vị', N'Đậu Phụ Chua Ngọt', DATEADD(MINUTE,-86000,@Now)),
(10005, N'Món Xào Chay Thanh Vị', N'Đậu Hũ Non Sốt Nấm Đông Cô', DATEADD(MINUTE,-86000,@Now)),
(10006, N'Món Cuốn Chay', N'Nem Chay Phù Trúc', DATEADD(MINUTE,-86000,@Now)),
(10007, N'Món Mì, Bún & Phở Chay', N'Bún Bò Huế Chay', DATEADD(MINUTE,-86000,@Now)),
(10008, N'Món Xào Chay Thanh Vị', N'Đậu Xào Rau Củ', DATEADD(MINUTE,-86000,@Now)),
(10009, N'Món Cuốn Chay', N'Nem Phù Trúc Chay', DATEADD(MINUTE,-86000,@Now)),
(10010, N'Món Kho Chay Đậm Đà', N'Rau Củ Kho Chay', DATEADD(MINUTE,-86000,@Now)),
(10011, N'Món Chiên & Rán Giòn Rụm', N'Chả Lá Lốt Chay Đơn Giản', DATEADD(MINUTE,-86000,@Now)),
(10012, N'Món Kho Chay Đậm Đà', N'Nấm Kho Carot', DATEADD(MINUTE,-86000,@Now)),
(10013, N'Món Mì, Bún & Phở Chay', N'Bún Huế Chay', DATEADD(MINUTE,-86000,@Now)),
(10014, N'Món Hấp & Luộc Dưỡng Sinh', N'Bao Bố Đậu Phụ Chay', DATEADD(MINUTE,-86000,@Now)),
(10015, N'Món Cuốn Chay', N'Bò Bía Chay', DATEADD(MINUTE,-86000,@Now)),
(10016, N'Món Canh & Súp Bổ Dưỡng', N'Canh Khoai Nấm', DATEADD(MINUTE,-86000,@Now)),
(10017, N'Món Xào Chay Thanh Vị', N'Nấm Rơm Xào Đậu Phụ', DATEADD(MINUTE,-86000,@Now)),
(10018, N'Món Xào Chay Thanh Vị', N'Rau Xào Thập Cẩm', DATEADD(MINUTE,-86000,@Now)),
(10019, N'Món Cuốn Chay', N'Bắp Cải Cuốn', DATEADD(MINUTE,-86000,@Now)),
(10020, N'Món Xào Chay Thanh Vị', N'Rau Củ Xào Thập Cẩm', DATEADD(MINUTE,-86000,@Now)),
(10021, N'Món Mì, Bún & Phở Chay', N'Bún Trộn Rau Cải', DATEADD(MINUTE,-86000,@Now)),
(10022, N'Món Xào Chay Thanh Vị', N'Đậu Hủ Sốt Cay', DATEADD(MINUTE,-86000,@Now)),
(10023, N'Món Mì, Bún & Phở Chay', N'Nui Gạo Chay', DATEADD(MINUTE,-86000,@Now)),
(10024, N'Món Cơm & Xôi Chay', N'Cơm Sườn Chay', DATEADD(MINUTE,-86000,@Now)),
(10025, N'Món Mì, Bún & Phở Chay', N'Miến Xào Với Mít Non', DATEADD(MINUTE,-86000,@Now)),
(10026, N'Món Hấp & Luộc Dưỡng Sinh', N'Đậu Hũ La Hán', DATEADD(MINUTE,-86000,@Now)),
(10027, N'Món Mì, Bún & Phở Chay', N'Bún Trộn Dưa Leo', DATEADD(MINUTE,-86000,@Now)),
(10028, N'Món Xào Chay Thanh Vị', N'Đậu Hũ Nhồi Sốt Me', DATEADD(MINUTE,-86000,@Now)),
(10029, N'Món Xào Chay Thanh Vị', N'Đậu Hủ Nhồi Chay', DATEADD(MINUTE,-86000,@Now)),
(10030, N'Món Cuốn Chay', N'Chả Giò Bắp Chay', DATEADD(MINUTE,-86000,@Now)),
(10031, N'Món Xào Chay Thanh Vị', N'Đậu Hũ Sốt Chay', DATEADD(MINUTE,-86000,@Now)),
(10032, N'Món Hấp & Luộc Dưỡng Sinh', N'Rau Củ Luộc Chấm Nấm Đông Cô Kho Quẹt Chay', DATEADD(MINUTE,-86000,@Now)),
(10033, N'Món Xào Chay Thanh Vị', N'Váng Đậu Xào Chay', DATEADD(MINUTE,-86000,@Now)),
(10034, N'Món Mì, Bún & Phở Chay', N'Bún Chay Huế', DATEADD(MINUTE,-86000,@Now)),
(10035, N'Món Hấp & Luộc Dưỡng Sinh', N'Rau Luộc Kho Quẹt Chay', DATEADD(MINUTE,-86000,@Now)),
(10036, N'Món Mì, Bún & Phở Chay', N'Mì Quảng Chay', DATEADD(MINUTE,-86000,@Now)),
(10037, N'Món Cơm & Xôi Chay', N'Cháo Thập Cẩm', DATEADD(MINUTE,-86000,@Now)),
(10038, N'Món Mì, Bún & Phở Chay', N'Bún Gạo Xào Chay', DATEADD(MINUTE,-86000,@Now)),
(10039, N'Món Xào Chay Thanh Vị', N'Đậu Phụ Cay Sốt Nấm', DATEADD(MINUTE,-86000,@Now)),
(10040, N'Món Kho Chay Đậm Đà', N'Ragu Chay', DATEADD(MINUTE,-86000,@Now)),
(10041, N'Món Cơm & Xôi Chay', N'Cơm Cuộn Chay', DATEADD(MINUTE,-86000,@Now)),
(10042, N'Món Kho Chay Đậm Đà', N'Củ Hủ Dừa Kho Nấm', DATEADD(MINUTE,-86000,@Now)),
(10043, N'Món Cuốn Chay', N'Cuốn Kho Chay Rong Biển', DATEADD(MINUTE,-86000,@Now)),
(10044, N'Món Xào Chay Thanh Vị', N'Cách Làm Đậu Phụ Sốt Chay', DATEADD(MINUTE,-86000,@Now)),
(10045, N'Món Mì, Bún & Phở Chay', N'Bún Xào Chay', DATEADD(MINUTE,-86000,@Now)),
(10046, N'Món Xào Chay Thanh Vị', N'Đậu Phụ Sốt Cay Tứ Xuyên', DATEADD(MINUTE,-86000,@Now)),
(10047, N'Món Xào Chay Thanh Vị', N'Đậu Phụ Nhồi Chay', DATEADD(MINUTE,-86000,@Now)),
(10048, N'Món Kho Chay Đậm Đà', N'Đậu Bắp Kho Nước Tương', DATEADD(MINUTE,-86000,@Now)),
(10049, N'Món Kho Chay Đậm Đà', N'Cà Tím Kho Đậu Hủ', DATEADD(MINUTE,-86000,@Now)),
(10050, N'Món Mì, Bún & Phở Chay', N'Mì Quảng Xào Chay', DATEADD(MINUTE,-86000,@Now)),
(10051, N'Món Cuốn Chay', N'Bánh Cuốn Chay', DATEADD(MINUTE,-86000,@Now)),
(10052, N'Món Canh & Súp Bổ Dưỡng', N'Canh Chua Chay', DATEADD(MINUTE,-86000,@Now)),
(10053, N'Món Kho Chay Đậm Đà', N'Khổ Qua Kho Chay', DATEADD(MINUTE,-86000,@Now)),
(10054, N'Món Mì, Bún & Phở Chay', N'Bún Trộn Dưa Leo Giòn Mát Ngày Hè', DATEADD(MINUTE,-86000,@Now)),
(10055, N'Món Canh & Súp Bổ Dưỡng', N'Canh Nấm Đậu Hũ', DATEADD(MINUTE,-86000,@Now)),
(10056, N'Món Xào Chay Thanh Vị', N'Đậu Phụ Sốt Nấm Kim Châm', DATEADD(MINUTE,-86000,@Now)),
(10057, N'Món Chiên & Rán Giòn Rụm', N'Há Cảo Chay Chiên', DATEADD(MINUTE,-86000,@Now)),
(10058, N'Món Mì, Bún & Phở Chay', N'Mì Xào Chay', DATEADD(MINUTE,-86000,@Now)),
(10059, N'Món Xào Chay Thanh Vị', N'Đậu Phụ Nhồi Nấm Chay', DATEADD(MINUTE,-86000,@Now)),
(10060, N'Món Mì, Bún & Phở Chay', N'Hủ Tíu Xào Chay Vị Me', DATEADD(MINUTE,-86000,@Now)),
(10061, N'Món Cuốn Chay', N'Chả Giò Chay', DATEADD(MINUTE,-86000,@Now)),
(10062, N'Món Hấp & Luộc Dưỡng Sinh', N'Đậu Phụ Hấp La Hán', DATEADD(MINUTE,-86000,@Now)),
(10063, N'Món Hấp & Luộc Dưỡng Sinh', N'Bánh Bột Lọc Trần Nhân Đậu Xanh', DATEADD(MINUTE,-86000,@Now)),
(10064, N'Món Cuốn Chay', N'Cuốn Chay', DATEADD(MINUTE,-86000,@Now)),
(10065, N'Món Xào Chay Thanh Vị', N'Đậu Hủ Dồn Nhân Rau Quả', DATEADD(MINUTE,-86000,@Now)),
(10066, N'Món Xào Chay Thanh Vị', N'Nấm Đông Cô Cải Thìa Xốt Rau Củ', DATEADD(MINUTE,-86000,@Now)),
(10067, N'Món Kho Chay Đậm Đà', N'Mướp Đắng Kho Chay', DATEADD(MINUTE,-86000,@Now)),
(10068, N'Món Cuốn Chay', N'Nem Chay', DATEADD(MINUTE,-86000,@Now)),
(10069, N'Món Hấp & Luộc Dưỡng Sinh', N'Bánh Bột Lọc Chay', DATEADD(MINUTE,-86000,@Now))
) AS x(id, catnm, nm, cr)
JOIN #cat c ON c.Category_Name = x.catnm
WHERE NOT EXISTS (SELECT 1 FROM dbo.Foods e WHERE e.Food_Name = x.nm);
SET IDENTITY_INSERT dbo.Foods OFF;
SELECT Food_ID, Food_Name INTO #food FROM dbo.Foods;
PRINT N'   + Foods (món ăn khái niệm từ sách): 69';
SET IDENTITY_INSERT dbo.Recipes ON;
INSERT INTO dbo.Recipes (Recipe_ID, Food_ID, User_ID, Category_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At)
SELECT v.rid, f.Food_ID, v.uid, c.Category_ID, s.Dietary_School_ID, v.ti, v.de, v.ins, v.pr, v.ck, v.sv, v.kc, v.df, v.vf, v.im, v.st, v.vw, v.sr, v.cr, v.up
FROM (VALUES
(10001, N'Bún Thái Chua Cay Chay', 10001, N'Món Mì, Bún & Phở Chay', 'BUDDHIST', N'Bún Thái Chua Cay Chay – bản giảm cân', N'Bún Thái Chua Cay Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-081), phù hợp người giảm cân, khoảng 374.5 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Ngâm me trong một ít nước ấm khoảng 10 phút, dùng muỗng tán nhẹ để me nhả vị chua. Lược lấy phần nước, bỏ hạt.
2. Thái lát mỏng dứa. Rửa sạch, thái múi cau nhỏ cà chua. Gọt bỏ phần dơ, rửa sạch, cắt nhỏ nấm rơm.
3. Đặt một chiếc nồi lớn lên bếp, cho một ít dầu ăn và sả băm vào, mở lửa vừa. Khi sả tỏa mùi thơm, thả cà chua vào đảo đều.
4. Rửa sạch, đập dập sả cây. Khi cà chua mềm và ra màu, cho 1.2 lít nước sạch cùng sả cây, thơm vào. Rửa sạch lá chanh, dùng tay vò nhẹ lá chanh và thả vào nồi. Nêm thêm 15g muối, 40g đường, 60g bột nêm chay, nấu khoảng 10 phút.
5. Rửa sạch, cắt nhỏ ớt. Thả tàu hũ chiên, nấm rơm, ớt vào nồi, nấu thêm 5 phút nữa rồi tắt bếp.
6. Xếp bún, rau sống ra tô. Múc tàu hũ, nấm rơm và nước dùng vào tô. Ăn ngay khi còn nóng.
7. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 22, 25, 2, 374.5, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790962/veggielife/seed60/cookbook500/c500_081_bun_thai_chua_cay_chay.jpg', 'APPROVED_ACTIVE', 1442, 'SYSTEM', DATEADD(MINUTE,-78806,@Now), DATEADD(MINUTE,-78201,@Now)),
(10002, N'Bún Thái Chua Cay Chay', 10001, N'Món Mì, Bún & Phở Chay', 'BUDDHIST', N'Bún Thái Chua Cay Chay – bản giữ cân', N'Bún Thái Chua Cay Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-081), phù hợp người giữ cân, khoảng 477.3 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Ngâm me trong một ít nước ấm khoảng 10 phút, dùng muỗng tán nhẹ để me nhả vị chua. Lược lấy phần nước, bỏ hạt.
2. Thái lát mỏng dứa. Rửa sạch, thái múi cau nhỏ cà chua. Gọt bỏ phần dơ, rửa sạch, cắt nhỏ nấm rơm.
3. Đặt một chiếc nồi lớn lên bếp, cho một ít dầu ăn và sả băm vào, mở lửa vừa. Khi sả tỏa mùi thơm, thả cà chua vào đảo đều.
4. Rửa sạch, đập dập sả cây. Khi cà chua mềm và ra màu, cho 1.2 lít nước sạch cùng sả cây, thơm vào. Rửa sạch lá chanh, dùng tay vò nhẹ lá chanh và thả vào nồi. Nêm thêm 15g muối, 40g đường, 60g bột nêm chay, nấu khoảng 10 phút.
5. Rửa sạch, cắt nhỏ ớt. Thả tàu hũ chiên, nấm rơm, ớt vào nồi, nấu thêm 5 phút nữa rồi tắt bếp.
6. Xếp bún, rau sống ra tô. Múc tàu hũ, nấm rơm và nước dùng vào tô. Ăn ngay khi còn nóng.
7. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 22, 25, 2, 477.3, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790962/veggielife/seed60/cookbook500/c500_081_bun_thai_chua_cay_chay.jpg', 'APPROVED_ACTIVE', 1087, 'SYSTEM', DATEADD(MINUTE,-76639,@Now), DATEADD(MINUTE,-74055,@Now)),
(10003, N'Bún Thái Chua Cay Chay', 10114, N'Món Mì, Bún & Phở Chay', 'BUDDHIST', N'Bún Thái Chua Cay Chay – bản giảm cân (không cay)', N'Bún Thái Chua Cay Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-081), phù hợp người giảm cân, khoảng 359.2 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Ngâm me trong một ít nước ấm khoảng 10 phút, dùng muỗng tán nhẹ để me nhả vị chua. Lược lấy phần nước, bỏ hạt.
2. Thái lát mỏng dứa. Rửa sạch, thái múi cau nhỏ cà chua. Gọt bỏ phần dơ, rửa sạch, cắt nhỏ nấm rơm.
3. Đặt một chiếc nồi lớn lên bếp, cho một ít dầu ăn và sả băm vào, mở lửa vừa. Khi sả tỏa mùi thơm, thả cà chua vào đảo đều.
4. Rửa sạch, đập dập sả cây. Khi cà chua mềm và ra màu, cho 1.2 lít nước sạch cùng sả cây, thơm vào. Rửa sạch lá chanh, dùng tay vò nhẹ lá chanh và thả vào nồi. Nêm thêm 15g muối, 40g đường, 60g bột nêm chay, nấu khoảng 10 phút.
5. Rửa sạch, cắt nhỏ ớt. Thả tàu hũ chiên, nấm rơm, ớt vào nồi, nấu thêm 5 phút nữa rồi tắt bếp.
6. Xếp bún, rau sống ra tô. Múc tàu hũ, nấm rơm và nước dùng vào tô. Ăn ngay khi còn nóng.
7. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ. Bản không cay: bỏ ớt, tương ớt và các gia vị cay khi nấu.', 22, 25, 2, 359.2, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790962/veggielife/seed60/cookbook500/c500_081_bun_thai_chua_cay_chay.jpg', 'APPROVED_ACTIVE', 295, 'USER', DATEADD(MINUTE,-1594,@Now), DATEADD(MINUTE,-1,@Now)),
(10004, N'Mì Xào La Hán', 10001, N'Món Mì, Bún & Phở Chay', 'OVO', N'Mì Xào La Hán – bản giữ cân', N'Mì Xào La Hán – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-046), phù hợp người giữ cân, khoảng 232.0 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Mì trứng trụng nước sôi để ráo. Đậu phụ chiên vàng, xắt miếng nhỏ. Nấm đông cô, nấm rơm, nấm mèo cắt bỏ chân, rửa sạch, xắt nhỏ. Cải thảo và cải ngọt nhặt bỏ lá sâu, rửa sạch, xắt miếng vừa ăn. Bắc chảo nóng, cho cải và nấm vào xào, nêm muối, đường, nước tương, cho vào một ít nước khi xào. Khi rau và nấm chín tới, cho mì vào xào áp chảo, đảo đều tay. Cho mì ra đĩa, trang trí thêm ngò tây, dùng nóng.
2. Mách nhỏ**
3. Mì trứng trụng chín tới, không trụng quá kĩ sẽ bị nhão. Sau khi trụng cho ngay vào nước lạnh để mì được tơi và giòn. Xào nhanh mì ở lửa lớn để mì được thơm ngon.
4. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 19, 20, 2, 232.0, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790892/veggielife/seed60/cookbook500/c500_046_mi_xao_la_han.jpg', 'APPROVED_ACTIVE', 355, 'SYSTEM', DATEADD(MINUTE,-79928,@Now), DATEADD(MINUTE,-79822,@Now)),
(10005, N'Mì Xào La Hán', 10001, N'Món Mì, Bún & Phở Chay', 'OVO', N'Mì Xào La Hán – bản tăng cân', N'Mì Xào La Hán – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-046), phù hợp người tăng cân, khoảng 784.4 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Mì trứng trụng nước sôi để ráo. Đậu phụ chiên vàng, xắt miếng nhỏ. Nấm đông cô, nấm rơm, nấm mèo cắt bỏ chân, rửa sạch, xắt nhỏ. Cải thảo và cải ngọt nhặt bỏ lá sâu, rửa sạch, xắt miếng vừa ăn. Bắc chảo nóng, cho cải và nấm vào xào, nêm muối, đường, nước tương, cho vào một ít nước khi xào. Khi rau và nấm chín tới, cho mì vào xào áp chảo, đảo đều tay. Cho mì ra đĩa, trang trí thêm ngò tây, dùng nóng.
2. Mách nhỏ**
3. Mì trứng trụng chín tới, không trụng quá kĩ sẽ bị nhão. Sau khi trụng cho ngay vào nước lạnh để mì được tơi và giòn. Xào nhanh mì ở lửa lớn để mì được thơm ngon.
4. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 19, 20, 2, 784.4, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790892/veggielife/seed60/cookbook500/c500_046_mi_xao_la_han.jpg', 'APPROVED_ACTIVE', 1388, 'SYSTEM', DATEADD(MINUTE,-74736,@Now), DATEADD(MINUTE,-73876,@Now)),
(10006, N'Nem Rán Ngô Chay', 10001, N'Món Cuốn Chay', 'BUDDHIST', N'Nem Rán Ngô Chay – bản tăng cân', N'Nem Rán Ngô Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-130), phù hợp người tăng cân, khoảng 788.5 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Ngô lột vỏ, bỏ râu, rửa sạch rồi bào nhỏ.
2. Bước 2: Váng đậu tươi cắt nhỏ.
3. Bước 3: Nấm mèo ngâm với nước nóng khoảng 5 phút cho nở rồi rửa sạch và băm nhỏ.
4. Bước 4: Hành boa rô, rau mùi rửa sạch rồi cắt nhỏ.
5. Bước 5: Cho nấm mèo, váng đậu, boa rô, rau mùi vào cùng ngô đã bào nhỏ. Nêm hạt nêm và hạt tiêu.
6. Bước 6: Tất cả trộn đều cho vừa khẩu vị (Không nên cho thêm đường vì ngô đã có vị ngọt).
7. Bước 7: Vỏ bánh tráng để cuốn dùng loại có màu vàng hay dùng bánh đa nem thì khi chiên sẽ có màu vàng đẹp hơn.
8. Bước 8: Múc nhân cho lên vỏ bánh tráng và cuốn như cuốn nem bình thường.
9. Bước 9: Bắt chảo dầu lên bếp cho nóng và cho nem vào chiên (Lúc mới đầu cho nem vào chiên thì không để dầu sôi quá).
10. Bước 10: Nem vàng thì mở lửa to rồi sau đó gắp ra để không hút dầu nhiều. Vớt ra rổ có lót giấy thấm bớt dầu và xếp đứng để nem rán rỏ dầu xuống và không bị chèn lên nhau như khi nằm lộn xộn. Khi ăn thì có thể ăn nem rán cùng rau sống, xà lách và chấm nước mắm chay chua ngọt (hoặc xì dầu).
11. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 16, 10, 2, 788.5, 'HARD', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791074/veggielife/seed60/cookbook500/c500_130_nem_ran_ngo_chay.jpg', 'APPROVED_ACTIVE', 68, 'SYSTEM', DATEADD(MINUTE,-77432,@Now), DATEADD(MINUTE,-75780,@Now)),
(10007, N'Đậu Phụ Chua Ngọt', 10001, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Đậu Phụ Chua Ngọt – bản giữ cân', N'Đậu Phụ Chua Ngọt – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-010), phù hợp người giữ cân, khoảng 132.9 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1. Ướp thịt chay băm với 1/2 thìa cà phê xì dầu và 1/2 thìa cà phê bột bắp trong khoảng nửa giờ.
2. 2. Đậu phụ rửa sạch nhẹ nhàng, dùng giấy thấm thấm khô. Sau đó cắt thành hình chữ nhật.
3. 3. Đổ dầu vào chảo đun nóng, rán vàng đậu phụ, rồi cho ra đĩa.
4. 4. Tiếp tục đổ thêm dầu vào chảo, phi thơm bơ rô, cho thịt chay vào đảo đều.
5. 5. Trộn đều tất cả gia vị và đậu phụ với nhau. Đổ tất cả vào thịt chay đã xào, đậy nắp, đun nhỏ lửa. Khuấy đều và nấu cho tới khi nước xốt đậm đặc.
6. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 16, 20, 2, 132.9, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790816/veggielife/seed60/cookbook500/c500_010_dau_phu_chua_ngot.jpg', 'APPROVED_ACTIVE', 1165, 'SYSTEM', DATEADD(MINUTE,-79489,@Now), DATEADD(MINUTE,-77654,@Now)),
(10008, N'Đậu Hũ Non Sốt Nấm Đông Cô', 10001, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Đậu Hũ Non Sốt Nấm Đông Cô – bản giữ cân', N'Đậu Hũ Non Sốt Nấm Đông Cô – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-003), phù hợp người giữ cân, khoảng 121.6 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Nấm đông cô cắt phần chân già, rửa sạch, ngâm trong nước muối loãng khoảng 15’ cho sạch. Đậu hũ non rửa sạch, thái miếng xếp ra đĩa. Dùng màng thực phẩm bao lại, làm chín trong lò vi song khoảng 2 phút. Hoặc bạn có thể làm chín đậu hũ non bằng cách trần qua nước sôi thêm chút muối.
2. Nấm sau khi ngâm, vớt ra để ráo, thái lát mỏng. Hành lá, rau mùi bỏ rễ, rửa sạch, xắt nhỏ. Chú ý phần gốc hành xắt nhỏ để riêng. Bắc chảo lên bếp, cho 1 thìa dầu mè. Khi dầu nóng cho phần đầu hành ở trên vào phi thơm. Tiếp đó cho nấm vào xào chín, thêm chút dầu hào chay, nước tương vào đảo kỹ. Hòa 1 thìa bột năng với chút nước, cho vào chảo nấm đảo đều, nêm nếm cho vừa ăn.
3. Nấm chín, tắt bếp, cho hành lá, mùi xắt nhỏ ở trên vào đảo đều. Cho nấm cùng với nước sốt ở trên vào đĩa đậu hũ non đã làm chín. Dùng thìa dưới đều phần nước sốt lên các miếng đậu để đậu hũ được ngấm đều.
4. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 19, 17, 2, 121.6, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790803/veggielife/seed60/cookbook500/c500_003_dau_hu_non_sot_nam_dong_co.jpg', 'APPROVED_ACTIVE', 1216, 'SYSTEM', DATEADD(MINUTE,-76773,@Now), DATEADD(MINUTE,-74745,@Now)),
(10009, N'Đậu Hũ Non Sốt Nấm Đông Cô', 10001, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Đậu Hũ Non Sốt Nấm Đông Cô – bản tăng cân', N'Đậu Hũ Non Sốt Nấm Đông Cô – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-003), phù hợp người tăng cân, khoảng 357.3 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Nấm đông cô cắt phần chân già, rửa sạch, ngâm trong nước muối loãng khoảng 15’ cho sạch. Đậu hũ non rửa sạch, thái miếng xếp ra đĩa. Dùng màng thực phẩm bao lại, làm chín trong lò vi song khoảng 2 phút. Hoặc bạn có thể làm chín đậu hũ non bằng cách trần qua nước sôi thêm chút muối.
2. Nấm sau khi ngâm, vớt ra để ráo, thái lát mỏng. Hành lá, rau mùi bỏ rễ, rửa sạch, xắt nhỏ. Chú ý phần gốc hành xắt nhỏ để riêng. Bắc chảo lên bếp, cho 1 thìa dầu mè. Khi dầu nóng cho phần đầu hành ở trên vào phi thơm. Tiếp đó cho nấm vào xào chín, thêm chút dầu hào chay, nước tương vào đảo kỹ. Hòa 1 thìa bột năng với chút nước, cho vào chảo nấm đảo đều, nêm nếm cho vừa ăn.
3. Nấm chín, tắt bếp, cho hành lá, mùi xắt nhỏ ở trên vào đảo đều. Cho nấm cùng với nước sốt ở trên vào đĩa đậu hũ non đã làm chín. Dùng thìa dưới đều phần nước sốt lên các miếng đậu để đậu hũ được ngấm đều.
4. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 19, 17, 2, 357.3, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790803/veggielife/seed60/cookbook500/c500_003_dau_hu_non_sot_nam_dong_co.jpg', 'APPROVED_ACTIVE', 1123, 'SYSTEM', DATEADD(MINUTE,-71995,@Now), DATEADD(MINUTE,-69065,@Now)),
(10010, N'Nem Chay Phù Trúc', 10035, N'Món Cuốn Chay', 'BUDDHIST', N'Nem Chay Phù Trúc – bản tăng cân', N'Nem Chay Phù Trúc – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-209), phù hợp người tăng cân, khoảng 794.9 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Dùng thìa nghiền nát đậu phụ.
2. Trộn đều hỗn hợp các nguyên liệu: Miến, đậu phụ, nấm hương, mộc nhĩ, cả rốt, củ đậu vào 1 bát to, thêm gia vị: muối, mắm chay và bột ngọt. Trộn đều để các nguyên liệu quyện vào nhau.
3. Trải phù trúc ra mâm, cho nhân vào 1 đầu của miếng phù trúc rồi gói lại như gói nem thông thường.
4. Đun sôi dầu ăn trong chảo, cho từng cuốn nem phù trúc vào rán vàng các mặt là được. ⚠Lưu ý: phù trúc rất ngấm dầu ăn vì thế, khi rán xong, chị em có thể cho nem lên một lớp giấy thấm cho ráo dầu. Với cách làm như vậy món ăn sẽ bớt ngán và tốt cho sức khỏe hơn. Vỏ phù trúc bên ngoài vàng giòn đẹp mắt, phần nhân bên trong mềm ngậy, chấm với mắm chay tỏi ớt thì hết xảy.
5. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 22, 20, 2, 794.9, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791222/veggielife/seed60/cookbook500/c500_209_nem_chay_phu_truc.jpg', 'DRAFT', 0, 'AI', DATEADD(MINUTE,-8576,@Now), CAST(NULL AS DATETIME2(7))),
(10011, N'Bún Bò Huế Chay', 10121, N'Món Mì, Bún & Phở Chay', 'BUDDHIST', N'Bún Bò Huế Chay – bản giữ cân', N'Bún Bò Huế Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-174), phù hợp người giữ cân, khoảng 466.1 kcal mỗi khẩu phần (2 khẩu phần).', N'1. làm bún bò chay Huế:
2. Bắc soong, cho dầu ăn vào, dầu nóng cho đậu hũ, măng vào xào, nêm cho vừa ăn, cho nước dừa và 3 lon nước + xả vào đun sôi, để lửa vừa phải, nêm lại cho vừa ăn.
3. Lấy chảo phi ớt màu, đổ vào nồi nước lèo, bỏ lá chanh vào (nếu không có lá chanh bỏ lá thơm vào) đậy nắp, để lửa riu riu.
4. Bún trụng nước sôi vớt ra để ráo nước, cho vào tô, múc nước lèo đổ lên, dùng nóng với rau sống và các thứ rau thơm.
5. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 22, 20, 2, 466.1, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791160/veggielife/seed60/cookbook500/c500_174_bun_bo_hue_chay.jpg', 'PENDING_REVIEW', 10, 'AI', DATEADD(MINUTE,-854,@Now), DATEADD(MINUTE,-1,@Now)),
(10012, N'Bún Bò Huế Chay', 10001, N'Món Mì, Bún & Phở Chay', 'BUDDHIST', N'Bún Bò Huế Chay – bản giảm cân (không cay)', N'Bún Bò Huế Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-174), phù hợp người giảm cân, khoảng 366.4 kcal mỗi khẩu phần (2 khẩu phần).', N'1. làm bún bò chay Huế:
2. Bắc soong, cho dầu ăn vào, dầu nóng cho đậu hũ, măng vào xào, nêm cho vừa ăn, cho nước dừa và 3 lon nước + xả vào đun sôi, để lửa vừa phải, nêm lại cho vừa ăn.
3. Lấy chảo phi ớt màu, đổ vào nồi nước lèo, bỏ lá chanh vào (nếu không có lá chanh bỏ lá thơm vào) đậy nắp, để lửa riu riu.
4. Bún trụng nước sôi vớt ra để ráo nước, cho vào tô, múc nước lèo đổ lên, dùng nóng với rau sống và các thứ rau thơm.
5. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ. Bản không cay: bỏ ớt, tương ớt và các gia vị cay khi nấu.', 22, 20, 2, 366.4, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791160/veggielife/seed60/cookbook500/c500_174_bun_bo_hue_chay.jpg', 'APPROVED_ACTIVE', 1376, 'SYSTEM', DATEADD(MINUTE,-67570,@Now), DATEADD(MINUTE,-67162,@Now)),
(10013, N'Bún Bò Huế Chay', 10001, N'Món Mì, Bún & Phở Chay', 'BUDDHIST', N'Bún Bò Huế Chay – bản tăng cân', N'Bún Bò Huế Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-174), phù hợp người tăng cân, khoảng 779.4 kcal mỗi khẩu phần (2 khẩu phần).', N'1. làm bún bò chay Huế:
2. Bắc soong, cho dầu ăn vào, dầu nóng cho đậu hũ, măng vào xào, nêm cho vừa ăn, cho nước dừa và 3 lon nước + xả vào đun sôi, để lửa vừa phải, nêm lại cho vừa ăn.
3. Lấy chảo phi ớt màu, đổ vào nồi nước lèo, bỏ lá chanh vào (nếu không có lá chanh bỏ lá thơm vào) đậy nắp, để lửa riu riu.
4. Bún trụng nước sôi vớt ra để ráo nước, cho vào tô, múc nước lèo đổ lên, dùng nóng với rau sống và các thứ rau thơm.
5. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 22, 20, 2, 779.4, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791160/veggielife/seed60/cookbook500/c500_174_bun_bo_hue_chay.jpg', 'APPROVED_ACTIVE', 1449, 'SYSTEM', DATEADD(MINUTE,-81570,@Now), DATEADD(MINUTE,-80565,@Now)),
(10014, N'Đậu Xào Rau Củ', 10001, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Đậu Xào Rau Củ – bản giữ cân', N'Đậu Xào Rau Củ – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-017), phù hợp người giữ cân, khoảng 102.4 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Thấm khô nước từng miếng đậu, cắt thành khối vuông. Ngâm rửa sạch hành lá và ớt chuông rồi cắt ớt chuông thành miếng nhỏ, xắt hành lá thành khúc dài 2-3cm.
2. Ngâm nấm hương trong nước ấm cho mềm ra, chia làm 3 phần bằng nhau rồi lấy 1 phần ra cắt miếng vừa ăn.
3. Rán vàng đậu rồi gắp ra để ráo dầu. Cho nấm hương vào chảo đảo qua, sau đó đến ớt chuông và đậu phụ đã rán. Nêm nếm với xì dầu và gia vị sao cho vừa miệng. Cuối cùng, bạn cho nốt hành lá vào, đảo qua rồi tắt bếp.
4. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 13, 20, 2, 102.4, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790832/veggielife/seed60/cookbook500/c500_017_dau_xao_rau_cu.jpg', 'APPROVED_ACTIVE', 449, 'SYSTEM', DATEADD(MINUTE,-76631,@Now), DATEADD(MINUTE,-75450,@Now)),
(10015, N'Đậu Xào Rau Củ', 10001, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Đậu Xào Rau Củ – bản tăng cân', N'Đậu Xào Rau Củ – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-017), phù hợp người tăng cân, khoảng 733.6 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Thấm khô nước từng miếng đậu, cắt thành khối vuông. Ngâm rửa sạch hành lá và ớt chuông rồi cắt ớt chuông thành miếng nhỏ, xắt hành lá thành khúc dài 2-3cm.
2. Ngâm nấm hương trong nước ấm cho mềm ra, chia làm 3 phần bằng nhau rồi lấy 1 phần ra cắt miếng vừa ăn.
3. Rán vàng đậu rồi gắp ra để ráo dầu. Cho nấm hương vào chảo đảo qua, sau đó đến ớt chuông và đậu phụ đã rán. Nêm nếm với xì dầu và gia vị sao cho vừa miệng. Cuối cùng, bạn cho nốt hành lá vào, đảo qua rồi tắt bếp.
4. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 13, 20, 2, 733.6, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790832/veggielife/seed60/cookbook500/c500_017_dau_xao_rau_cu.jpg', 'APPROVED_ACTIVE', 1643, 'SYSTEM', DATEADD(MINUTE,-78961,@Now), DATEADD(MINUTE,-78479,@Now)),
(10016, N'Đậu Xào Rau Củ', 10001, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Đậu Xào Rau Củ – bản giảm cân', N'Đậu Xào Rau Củ – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-017), phù hợp người giảm cân, khoảng 359.0 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Thấm khô nước từng miếng đậu, cắt thành khối vuông. Ngâm rửa sạch hành lá và ớt chuông rồi cắt ớt chuông thành miếng nhỏ, xắt hành lá thành khúc dài 2-3cm.
2. Ngâm nấm hương trong nước ấm cho mềm ra, chia làm 3 phần bằng nhau rồi lấy 1 phần ra cắt miếng vừa ăn.
3. Rán vàng đậu rồi gắp ra để ráo dầu. Cho nấm hương vào chảo đảo qua, sau đó đến ớt chuông và đậu phụ đã rán. Nêm nếm với xì dầu và gia vị sao cho vừa miệng. Cuối cùng, bạn cho nốt hành lá vào, đảo qua rồi tắt bếp.
4. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 13, 20, 2, 359.0, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790832/veggielife/seed60/cookbook500/c500_017_dau_xao_rau_cu.jpg', 'APPROVED_ACTIVE', 811, 'SYSTEM', DATEADD(MINUTE,-81085,@Now), DATEADD(MINUTE,-79072,@Now)),
(10017, N'Nem Phù Trúc Chay', 10011, N'Món Cuốn Chay', 'BUDDHIST', N'Nem Phù Trúc Chay – bản giữ cân', N'Nem Phù Trúc Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-090), phù hợp người giữ cân, khoảng 446.0 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Bước 1: Cà rốt, củ đậu gọt bỏ vỏ, xay thô (hoặc nạo nhỏ).
2. Bước 2: Đậu phụ dùng thìa dằm nát.
3. Bước 3: Phù trúc, miến, nấm hương, mộc nhĩ ngâm nước cho nở mềm rồi rửa sạch. Miến cắt khúc ngắn, phù trúc cắt miếng vuông đủ để gói thành 1 cái nem, mộc nhĩ và nấm hương thái nhỏ.
4. Bước 4: Giá và rau mùi ngâm rửa sạch, thái nhỏ.
5. Bước 5: Cho tất cả các nguyên liệu vào 1 bát to, thêm muối, mắm chay và mì chính. Trộn đều để các nguyên liệu quyện vào nhau.
6. Bước 6: Trải phù trúc ra mâm, cho nhân vào 1 đầu của miếng phù trúc rồi gói lại như gói nem thông thường.
7. Bước 7: Đun sôi dầu ăn trong chảo, cho từng cuốn nem phù trúc vào rán vàng các mặt là được. Chị em lưu ý là, phù trúc rất ngấm dầu ăn vì thế, khi rán xong, chị em có thể cho nem lên một lớp giấy thấm cho ráo dầu. Với cách làm như vậy món ăn sẽ bớt ngán và tốt cho sức khỏe hơn. Khi ăn chấm nem phù trúc chay với nước mắm chay bạn nhé! Chúc các bạn thành công và ngon miệng với nem phù trúc chay!
8. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 19, 20, 2, 446.0, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790980/veggielife/seed60/cookbook500/c500_090_nem_phu_truc_chay.jpg', 'ARCHIVED', 18, 'USER', DATEADD(MINUTE,-25439,@Now), DATEADD(MINUTE,-24869,@Now)),
(10018, N'Nem Phù Trúc Chay', 10001, N'Món Cuốn Chay', 'BUDDHIST', N'Nem Phù Trúc Chay – bản tăng cân', N'Nem Phù Trúc Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-090), phù hợp người tăng cân, khoảng 782.5 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Bước 1: Cà rốt, củ đậu gọt bỏ vỏ, xay thô (hoặc nạo nhỏ).
2. Bước 2: Đậu phụ dùng thìa dằm nát.
3. Bước 3: Phù trúc, miến, nấm hương, mộc nhĩ ngâm nước cho nở mềm rồi rửa sạch. Miến cắt khúc ngắn, phù trúc cắt miếng vuông đủ để gói thành 1 cái nem, mộc nhĩ và nấm hương thái nhỏ.
4. Bước 4: Giá và rau mùi ngâm rửa sạch, thái nhỏ.
5. Bước 5: Cho tất cả các nguyên liệu vào 1 bát to, thêm muối, mắm chay và mì chính. Trộn đều để các nguyên liệu quyện vào nhau.
6. Bước 6: Trải phù trúc ra mâm, cho nhân vào 1 đầu của miếng phù trúc rồi gói lại như gói nem thông thường.
7. Bước 7: Đun sôi dầu ăn trong chảo, cho từng cuốn nem phù trúc vào rán vàng các mặt là được. Chị em lưu ý là, phù trúc rất ngấm dầu ăn vì thế, khi rán xong, chị em có thể cho nem lên một lớp giấy thấm cho ráo dầu. Với cách làm như vậy món ăn sẽ bớt ngán và tốt cho sức khỏe hơn. Khi ăn chấm nem phù trúc chay với nước mắm chay bạn nhé! Chúc các bạn thành công và ngon miệng với nem phù trúc chay!
8. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 19, 20, 2, 782.5, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790980/veggielife/seed60/cookbook500/c500_090_nem_phu_truc_chay.jpg', 'APPROVED_ACTIVE', 1192, 'SYSTEM', DATEADD(MINUTE,-77900,@Now), DATEADD(MINUTE,-77039,@Now)),
(10019, N'Nem Phù Trúc Chay', 10001, N'Món Cuốn Chay', 'BUDDHIST', N'Nem Phù Trúc Chay – bản giảm cân', N'Nem Phù Trúc Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-090), phù hợp người giảm cân, khoảng 366.1 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Bước 1: Cà rốt, củ đậu gọt bỏ vỏ, xay thô (hoặc nạo nhỏ).
2. Bước 2: Đậu phụ dùng thìa dằm nát.
3. Bước 3: Phù trúc, miến, nấm hương, mộc nhĩ ngâm nước cho nở mềm rồi rửa sạch. Miến cắt khúc ngắn, phù trúc cắt miếng vuông đủ để gói thành 1 cái nem, mộc nhĩ và nấm hương thái nhỏ.
4. Bước 4: Giá và rau mùi ngâm rửa sạch, thái nhỏ.
5. Bước 5: Cho tất cả các nguyên liệu vào 1 bát to, thêm muối, mắm chay và mì chính. Trộn đều để các nguyên liệu quyện vào nhau.
6. Bước 6: Trải phù trúc ra mâm, cho nhân vào 1 đầu của miếng phù trúc rồi gói lại như gói nem thông thường.
7. Bước 7: Đun sôi dầu ăn trong chảo, cho từng cuốn nem phù trúc vào rán vàng các mặt là được. Chị em lưu ý là, phù trúc rất ngấm dầu ăn vì thế, khi rán xong, chị em có thể cho nem lên một lớp giấy thấm cho ráo dầu. Với cách làm như vậy món ăn sẽ bớt ngán và tốt cho sức khỏe hơn. Khi ăn chấm nem phù trúc chay với nước mắm chay bạn nhé! Chúc các bạn thành công và ngon miệng với nem phù trúc chay!
8. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 19, 20, 2, 366.1, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790980/veggielife/seed60/cookbook500/c500_090_nem_phu_truc_chay.jpg', 'APPROVED_ACTIVE', 1266, 'SYSTEM', DATEADD(MINUTE,-80673,@Now), DATEADD(MINUTE,-80396,@Now)),
(10020, N'Rau Củ Kho Chay', 10001, N'Món Kho Chay Đậm Đà', 'VEGAN', N'Rau Củ Kho Chay – bản giữ cân', N'Rau Củ Kho Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-009), phù hợp người giữ cân, khoảng 98.7 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Rửa sạch các loại củ, nấm Cà rốt, cải trắng, cà tím cắt khúc vừa ăn Đậu hủ chiên cắt khúc vừa ăn
2. Để rau củ có vị thơm hơn, bạn có thể chiên sơ qua tất cả, Tuy nhiên để món ăn thanh đạm ít dầu mở thì với món rau củ kho chay mình thường không chiên. Cho 2 muỗng dầu ăn vào chảo cho nóng, cho BOARO vào phi thơm, cho tất cả rau củ vào xào sơ, cho ít gia vị, nước tương vào vừa ăn. Để lửa nhỏ cho nước tương rút vào trong rau củ, cho tiêu xay rồi tắt bếp, chừa chút nước sệt để dùng với cơm. Các bạn có thể tự do sáng tạo thêm món rau củ kho chay với dứa (thơm), đậu cove, khổ qua, bí xanh…
3. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 13, 20, 2, 98.7, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790814/veggielife/seed60/cookbook500/c500_009_rau_cu_kho_chay.jpg', 'APPROVED_ACTIVE', 82, 'SYSTEM', DATEADD(MINUTE,-76124,@Now), DATEADD(MINUTE,-73791,@Now)),
(10021, N'Rau Củ Kho Chay', 10001, N'Món Kho Chay Đậm Đà', 'VEGAN', N'Rau Củ Kho Chay – bản giảm cân', N'Rau Củ Kho Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-009), phù hợp người giảm cân, khoảng 348.8 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Rửa sạch các loại củ, nấm Cà rốt, cải trắng, cà tím cắt khúc vừa ăn Đậu hủ chiên cắt khúc vừa ăn
2. Để rau củ có vị thơm hơn, bạn có thể chiên sơ qua tất cả, Tuy nhiên để món ăn thanh đạm ít dầu mở thì với món rau củ kho chay mình thường không chiên. Cho 2 muỗng dầu ăn vào chảo cho nóng, cho BOARO vào phi thơm, cho tất cả rau củ vào xào sơ, cho ít gia vị, nước tương vào vừa ăn. Để lửa nhỏ cho nước tương rút vào trong rau củ, cho tiêu xay rồi tắt bếp, chừa chút nước sệt để dùng với cơm. Các bạn có thể tự do sáng tạo thêm món rau củ kho chay với dứa (thơm), đậu cove, khổ qua, bí xanh…
3. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 13, 20, 2, 348.8, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790814/veggielife/seed60/cookbook500/c500_009_rau_cu_kho_chay.jpg', 'APPROVED_ACTIVE', 1071, 'SYSTEM', DATEADD(MINUTE,-68594,@Now), DATEADD(MINUTE,-66797,@Now)),
(10022, N'Chả Lá Lốt Chay Đơn Giản', 10001, N'Món Chiên & Rán Giòn Rụm', 'VEGAN', N'Chả Lá Lốt Chay Đơn Giản – bản giữ cân', N'Chả Lá Lốt Chay Đơn Giản – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-048), phù hợp người giữ cân, khoảng 569.0 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đầu tiên, bạn ngâm mộc nhĩ vào nước nóng cho nở, rồi cắt chân rửa sạch, thái sợi.
2. Sau đó, bạn rửa sạch hành hoa rồi thái nhỏ Tiếp theo, bạn cho 2 bìa đậu vào bát tô đánh nát rồi trộn cùng với hành hoa, mộc nhĩ, đậu Hà Lan, ngô ngọt cùng 1 thìa hạt nêm nhỏ Sau phi trộn xong phần nhân, bạn mang lá lốt rửa sạch rồi để ráo.
3. Lưu ý, cần để lá lốt thật ráo nước, kẻo khi rán sẽ bị bắn dầu.
4. Giờ bạn cho phần nhân đã trộn vào bên trong lá lốt rồi cuộn lại.
5. Bạn làm như thế đến khi hết nhân thì thôi.
6. Bắc chảo lên bếp, cho dầu ăn vào đun sôi, sau đó, thả từng cái chả vào rán ở lửa nhỏ.
7. Không nên để lửa to kẻo chả sẽ cháy.
8. Khi nào thấy bề mặt lá lốt ngả màu xanh đậm là chả đã chín.
9. Cuối cùng, chỉ cần gắp chả lá lốt chay ra đĩa ròi bày lên mâm cỗ là xong.
10. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 16, 20, 2, 569.0, 'HARD', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790896/veggielife/seed60/cookbook500/c500_048_cha_la_lot_chay_don_gian.jpg', 'APPROVED_ACTIVE', 309, 'SYSTEM', DATEADD(MINUTE,-67675,@Now), DATEADD(MINUTE,-64930,@Now)),
(10023, N'Chả Lá Lốt Chay Đơn Giản', 10001, N'Món Chiên & Rán Giòn Rụm', 'VEGAN', N'Chả Lá Lốt Chay Đơn Giản – bản giảm cân', N'Chả Lá Lốt Chay Đơn Giản – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-048), phù hợp người giảm cân, khoảng 383.4 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đầu tiên, bạn ngâm mộc nhĩ vào nước nóng cho nở, rồi cắt chân rửa sạch, thái sợi.
2. Sau đó, bạn rửa sạch hành hoa rồi thái nhỏ Tiếp theo, bạn cho 2 bìa đậu vào bát tô đánh nát rồi trộn cùng với hành hoa, mộc nhĩ, đậu Hà Lan, ngô ngọt cùng 1 thìa hạt nêm nhỏ Sau phi trộn xong phần nhân, bạn mang lá lốt rửa sạch rồi để ráo.
3. Lưu ý, cần để lá lốt thật ráo nước, kẻo khi rán sẽ bị bắn dầu.
4. Giờ bạn cho phần nhân đã trộn vào bên trong lá lốt rồi cuộn lại.
5. Bạn làm như thế đến khi hết nhân thì thôi.
6. Bắc chảo lên bếp, cho dầu ăn vào đun sôi, sau đó, thả từng cái chả vào rán ở lửa nhỏ.
7. Không nên để lửa to kẻo chả sẽ cháy.
8. Khi nào thấy bề mặt lá lốt ngả màu xanh đậm là chả đã chín.
9. Cuối cùng, chỉ cần gắp chả lá lốt chay ra đĩa ròi bày lên mâm cỗ là xong.
10. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 16, 20, 2, 383.4, 'HARD', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790896/veggielife/seed60/cookbook500/c500_048_cha_la_lot_chay_don_gian.jpg', 'APPROVED_ACTIVE', 772, 'SYSTEM', DATEADD(MINUTE,-67708,@Now), DATEADD(MINUTE,-66765,@Now)),
(10024, N'Nấm Kho Carot', 10081, N'Món Kho Chay Đậm Đà', 'LACTO', N'Nấm Kho Carot – bản giữ cân', N'Nấm Kho Carot – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-006), phù hợp người giữ cân, khoảng 243.3 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Cho dầu ăn vào chảo, dầu nóng cho bơ vào nấu cho tan chảy.
2. Thêm hành boa-rô vào đảo đều, sau đó thêm cà rốt, nấm vào nấu khoảng 5 phút cho mềm.
3. Thêm cà chua paste, bột mì vào trộn đều trong 1 phút.
4. Thêm rượu trắng vào đun lửa nhỏ trong vài phút cho sốt sền sệt là được.
5. Nêm nước tương, ít muối tiêu vừa ăn.
6. Cho ngò rí cắt nhỏ vào, trộn đều, tắt bếp.
7. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 19, 10, 2, 243.3, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790809/veggielife/seed60/cookbook500/c500_006_nam_kho_carot.jpg', 'APPROVED_ACTIVE', 188, 'USER', DATEADD(MINUTE,-8683,@Now), DATEADD(MINUTE,-6432,@Now)),
(10025, N'Nấm Kho Carot', 10001, N'Món Kho Chay Đậm Đà', 'LACTO', N'Nấm Kho Carot – bản tăng cân', N'Nấm Kho Carot – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-006), phù hợp người tăng cân, khoảng 782.9 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Cho dầu ăn vào chảo, dầu nóng cho bơ vào nấu cho tan chảy.
2. Thêm hành boa-rô vào đảo đều, sau đó thêm cà rốt, nấm vào nấu khoảng 5 phút cho mềm.
3. Thêm cà chua paste, bột mì vào trộn đều trong 1 phút.
4. Thêm rượu trắng vào đun lửa nhỏ trong vài phút cho sốt sền sệt là được.
5. Nêm nước tương, ít muối tiêu vừa ăn.
6. Cho ngò rí cắt nhỏ vào, trộn đều, tắt bếp.
7. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 19, 10, 2, 782.9, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790809/veggielife/seed60/cookbook500/c500_006_nam_kho_carot.jpg', 'APPROVED_ACTIVE', 326, 'SYSTEM', DATEADD(MINUTE,-71958,@Now), DATEADD(MINUTE,-69730,@Now)),
(10026, N'Bún Huế Chay', 10001, N'Món Mì, Bún & Phở Chay', 'VEGAN', N'Bún Huế Chay – bản giữ cân', N'Bún Huế Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-053), phù hợp người giữ cân, khoảng 610.2 kcal mỗi khẩu phần (2 khẩu phần).', N'1. làm bún huế chay ngon hấp dãn :
2. Bước 1 : Rau củ quả bạn đem rửa sạch, sau đó đem cắt khúc. Lá hành boa rô đem rửa sạch rồi cũng cắt khúc
3. Cho rau củ quả vào nồi, rồi sau đó đem cho thêm nước, cùng với một ít muối vào đem hầm cho ra nước ngọt, sau đó đem hầm cho đến khi hỗn hợp mềm nhé
4. Sau dó đem bỏ lấy nước dùng chứ không lấy cái nhé ( chỉ sử dụng lại củ cải )
5. Bước 2 : Bắc nồi nước dùng lên trên bếp ( trong nồi còn củ cải ). Sau đó bạn đem cho thêm dứa thái miếng, sả đem rửa sạch đập dập, ít cọng đầu hành boa rô sau đó đem cắt khúc và đem nấu với lửa nhỏ cho thơm mềm rồi đem nêm nếm thêm gia vị. Mình sử hay sử dụng gia vị bột nêm chay, bột ngọt, đường phèn ( tùy theo khẩu vị gia đình bạn nhé ) và cùng với một ít màu dầu điều vào để cho món ăn ngon hấp dẫn hơn
6. Bước 3 : Đậu hũ bạn đem cắt nhỏ, sau đó đem chiên vàng khi nào ăn thì cho vào nồi
7. Bước 4 : Làm sốt nấm : Đem cho thêm ít dầu ăn vào nồi, sau đó đem đun nóng. Khi mà dầu nóng thì bạn đem cho đầu hành boa rô vào đem phi thơm rồi đem chocà chua vào băm nhỏ vào xào, sau đó đem cho thêm nấm rơm và ít nước vào. Sau đó nêm gia vị hơi đậm đà, rồi đun với lửa liu riu cho thấm đến khi mà nước còn sền sệt. Rồi sau đó bạn đem múc sốt nấm ra tô
8. Bạn Chuẩn bị bún ngon (dùng bún cọng lớn) cùng với các nguyên liệu ăn kèm như sa tế, rau thơm (bắp chuối, rau muống bào, giá, rau thơm).
9. Bún chần nóng, cho vào tô, vài miếng dứa, đậu hũ, củ cải trắng rồi chan nước dùng lên tô bún. Sau đó mới cho sốt nấm lên trên cùng rau ngò gai, hành boa-rô cắt nhỏ lên. Món bún Huế chay thơm ngon, hấp dẫn này đảm bảo cả nhà sẽ thích mê cho mà xem.Bạn hãy chia sẻ món ăn chay này cho mọi người cùng biết nhé. Chúc bạn thành công
10. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 22, 20, 2, 610.2, 'HARD', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790908/veggielife/seed60/cookbook500/c500_053b_bun_hue_chay.jpg', 'APPROVED_ACTIVE', 1288, 'SYSTEM', DATEADD(MINUTE,-71191,@Now), DATEADD(MINUTE,-69781,@Now)),
(10027, N'Bún Huế Chay', 10008, N'Món Mì, Bún & Phở Chay', 'VEGAN', N'Bún Huế Chay – bản tăng cân', N'Bún Huế Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-053), phù hợp người tăng cân, khoảng 729.1 kcal mỗi khẩu phần (2 khẩu phần).', N'1. làm bún huế chay ngon hấp dãn :
2. Bước 1 : Rau củ quả bạn đem rửa sạch, sau đó đem cắt khúc. Lá hành boa rô đem rửa sạch rồi cũng cắt khúc
3. Cho rau củ quả vào nồi, rồi sau đó đem cho thêm nước, cùng với một ít muối vào đem hầm cho ra nước ngọt, sau đó đem hầm cho đến khi hỗn hợp mềm nhé
4. Sau dó đem bỏ lấy nước dùng chứ không lấy cái nhé ( chỉ sử dụng lại củ cải )
5. Bước 2 : Bắc nồi nước dùng lên trên bếp ( trong nồi còn củ cải ). Sau đó bạn đem cho thêm dứa thái miếng, sả đem rửa sạch đập dập, ít cọng đầu hành boa rô sau đó đem cắt khúc và đem nấu với lửa nhỏ cho thơm mềm rồi đem nêm nếm thêm gia vị. Mình sử hay sử dụng gia vị bột nêm chay, bột ngọt, đường phèn ( tùy theo khẩu vị gia đình bạn nhé ) và cùng với một ít màu dầu điều vào để cho món ăn ngon hấp dẫn hơn
6. Bước 3 : Đậu hũ bạn đem cắt nhỏ, sau đó đem chiên vàng khi nào ăn thì cho vào nồi
7. Bước 4 : Làm sốt nấm : Đem cho thêm ít dầu ăn vào nồi, sau đó đem đun nóng. Khi mà dầu nóng thì bạn đem cho đầu hành boa rô vào đem phi thơm rồi đem chocà chua vào băm nhỏ vào xào, sau đó đem cho thêm nấm rơm và ít nước vào. Sau đó nêm gia vị hơi đậm đà, rồi đun với lửa liu riu cho thấm đến khi mà nước còn sền sệt. Rồi sau đó bạn đem múc sốt nấm ra tô
8. Bạn Chuẩn bị bún ngon (dùng bún cọng lớn) cùng với các nguyên liệu ăn kèm như sa tế, rau thơm (bắp chuối, rau muống bào, giá, rau thơm).
9. Bún chần nóng, cho vào tô, vài miếng dứa, đậu hũ, củ cải trắng rồi chan nước dùng lên tô bún. Sau đó mới cho sốt nấm lên trên cùng rau ngò gai, hành boa-rô cắt nhỏ lên. Món bún Huế chay thơm ngon, hấp dẫn này đảm bảo cả nhà sẽ thích mê cho mà xem.Bạn hãy chia sẻ món ăn chay này cho mọi người cùng biết nhé. Chúc bạn thành công
10. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 22, 20, 2, 729.1, 'HARD', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790908/veggielife/seed60/cookbook500/c500_053b_bun_hue_chay.jpg', 'QUARANTINED_HIDDEN', 2, 'USER', DATEADD(MINUTE,-48597,@Now), DATEADD(MINUTE,-47259,@Now)),
(10028, N'Bún Huế Chay', 10049, N'Món Mì, Bún & Phở Chay', 'VEGAN', N'Bún Huế Chay – bản giảm cân', N'Bún Huế Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-053), phù hợp người giảm cân, khoảng 392.9 kcal mỗi khẩu phần (2 khẩu phần).', N'1. làm bún huế chay ngon hấp dãn :
2. Bước 1 : Rau củ quả bạn đem rửa sạch, sau đó đem cắt khúc. Lá hành boa rô đem rửa sạch rồi cũng cắt khúc
3. Cho rau củ quả vào nồi, rồi sau đó đem cho thêm nước, cùng với một ít muối vào đem hầm cho ra nước ngọt, sau đó đem hầm cho đến khi hỗn hợp mềm nhé
4. Sau dó đem bỏ lấy nước dùng chứ không lấy cái nhé ( chỉ sử dụng lại củ cải )
5. Bước 2 : Bắc nồi nước dùng lên trên bếp ( trong nồi còn củ cải ). Sau đó bạn đem cho thêm dứa thái miếng, sả đem rửa sạch đập dập, ít cọng đầu hành boa rô sau đó đem cắt khúc và đem nấu với lửa nhỏ cho thơm mềm rồi đem nêm nếm thêm gia vị. Mình sử hay sử dụng gia vị bột nêm chay, bột ngọt, đường phèn ( tùy theo khẩu vị gia đình bạn nhé ) và cùng với một ít màu dầu điều vào để cho món ăn ngon hấp dẫn hơn
6. Bước 3 : Đậu hũ bạn đem cắt nhỏ, sau đó đem chiên vàng khi nào ăn thì cho vào nồi
7. Bước 4 : Làm sốt nấm : Đem cho thêm ít dầu ăn vào nồi, sau đó đem đun nóng. Khi mà dầu nóng thì bạn đem cho đầu hành boa rô vào đem phi thơm rồi đem chocà chua vào băm nhỏ vào xào, sau đó đem cho thêm nấm rơm và ít nước vào. Sau đó nêm gia vị hơi đậm đà, rồi đun với lửa liu riu cho thấm đến khi mà nước còn sền sệt. Rồi sau đó bạn đem múc sốt nấm ra tô
8. Bạn Chuẩn bị bún ngon (dùng bún cọng lớn) cùng với các nguyên liệu ăn kèm như sa tế, rau thơm (bắp chuối, rau muống bào, giá, rau thơm).
9. Bún chần nóng, cho vào tô, vài miếng dứa, đậu hũ, củ cải trắng rồi chan nước dùng lên tô bún. Sau đó mới cho sốt nấm lên trên cùng rau ngò gai, hành boa-rô cắt nhỏ lên. Món bún Huế chay thơm ngon, hấp dẫn này đảm bảo cả nhà sẽ thích mê cho mà xem.Bạn hãy chia sẻ món ăn chay này cho mọi người cùng biết nhé. Chúc bạn thành công
10. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 22, 20, 2, 392.9, 'HARD', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790908/veggielife/seed60/cookbook500/c500_053b_bun_hue_chay.jpg', 'PENDING_REVIEW', 10, 'AI', DATEADD(MINUTE,-36328,@Now), DATEADD(MINUTE,-35553,@Now)),
(10029, N'Bao Bố Đậu Phụ Chay', 10121, N'Món Hấp & Luộc Dưỡng Sinh', 'VEGAN', N'Bao Bố Đậu Phụ Chay – bản giảm cân', N'Bao Bố Đậu Phụ Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-120), phù hợp người giảm cân, khoảng 251.2 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Đậu phụ chiên rọc đường nhỏ phía cạnh rồi dùng muỗng múc hết phần ruột trắng bên trong.
2. Bước 2: Cho Nấm, củ năng, cà rốt, bắp cải xào với phân nửa gia vị thành nhân, trộn đều, cho vào các chiếc túi, dùng cọng hẹ cột ngang.
3. Bước 3: Cho vào nồi hấp thêm 5 phút.
4. Bước 4: Lấy nước dùng cho chút bột bắp vào khuấy đều, nêm phân nửa gia vị còn lại là thành sốt rưới lên bao bố khi ăn.
5. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 19, 10, 2, 251.2, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791046/veggielife/seed60/cookbook500/c500_120_bao_bo_dau_phu_chay.jpg', 'APPROVED_ACTIVE', 445, 'AI', DATEADD(MINUTE,-26247,@Now), DATEADD(MINUTE,-25058,@Now)),
(10030, N'Bao Bố Đậu Phụ Chay', 10001, N'Món Hấp & Luộc Dưỡng Sinh', 'VEGAN', N'Bao Bố Đậu Phụ Chay – bản tăng cân', N'Bao Bố Đậu Phụ Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-120), phù hợp người tăng cân, khoảng 726.9 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Đậu phụ chiên rọc đường nhỏ phía cạnh rồi dùng muỗng múc hết phần ruột trắng bên trong.
2. Bước 2: Cho Nấm, củ năng, cà rốt, bắp cải xào với phân nửa gia vị thành nhân, trộn đều, cho vào các chiếc túi, dùng cọng hẹ cột ngang.
3. Bước 3: Cho vào nồi hấp thêm 5 phút.
4. Bước 4: Lấy nước dùng cho chút bột bắp vào khuấy đều, nêm phân nửa gia vị còn lại là thành sốt rưới lên bao bố khi ăn.
5. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 19, 10, 2, 726.9, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791046/veggielife/seed60/cookbook500/c500_120_bao_bo_dau_phu_chay.jpg', 'APPROVED_ACTIVE', 693, 'SYSTEM', DATEADD(MINUTE,-83023,@Now), DATEADD(MINUTE,-82540,@Now)),
(10031, N'Bao Bố Đậu Phụ Chay', 10001, N'Món Hấp & Luộc Dưỡng Sinh', 'VEGAN', N'Bao Bố Đậu Phụ Chay – bản giữ cân', N'Bao Bố Đậu Phụ Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-120), phù hợp người giữ cân, khoảng 499.2 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Đậu phụ chiên rọc đường nhỏ phía cạnh rồi dùng muỗng múc hết phần ruột trắng bên trong.
2. Bước 2: Cho Nấm, củ năng, cà rốt, bắp cải xào với phân nửa gia vị thành nhân, trộn đều, cho vào các chiếc túi, dùng cọng hẹ cột ngang.
3. Bước 3: Cho vào nồi hấp thêm 5 phút.
4. Bước 4: Lấy nước dùng cho chút bột bắp vào khuấy đều, nêm phân nửa gia vị còn lại là thành sốt rưới lên bao bố khi ăn.
5. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 19, 10, 2, 499.2, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791046/veggielife/seed60/cookbook500/c500_120_bao_bo_dau_phu_chay.jpg', 'APPROVED_ACTIVE', 1228, 'SYSTEM', DATEADD(MINUTE,-84887,@Now), DATEADD(MINUTE,-82877,@Now)),
(10032, N'Bò Bía Chay', 10001, N'Món Cuốn Chay', 'BUDDHIST', N'Bò Bía Chay – bản giảm cân', N'Bò Bía Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-042), phù hợp người giảm cân, khoảng 258.7 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Cho phần cuốn bánh:
2. Bước 1: Ướp ham chay với bột xá xíu trong khoảng 15 phút và đem chiên vàng.
3. Bước 2: Đem xào sắn và cà rốt với ít muối, bột ngọt và hạt nêm.
4. Bước 3: Xào sơ mì căn cho thơm và săn lại.
5. Cho nước tương chấm: Cho nước cốt dừa vào đun sôi. Sau đó, cho thêm tương đen và bơ đậu phộng vào khuấy đều. Nêm lại với ít muối và đường cho vừa miệng.
6. Cho dưa chua: Đem trộn các nguyên liệu làm dưa chua đã chuẩn bị vào với nhau và để trong khoảng 8 tiếng (Phần này bạn nên làm trước để tiết kiệm thời gian) Khi dùng, bạn dọn bánh tráng, rau sống, các nguyên liệu mì căn, ham chay, cà rốt xào với bát nước chấm đã cho thêm dưa chua và ít ớt bằm. Cách ăn giống như bánh tráng cuốn. Món ăn vặt này rất ngon và lạ miệng.
7. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 16, 15, 2, 258.7, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790885/veggielife/seed60/cookbook500/c500_042_bo_bia_chay.jpg', 'APPROVED_ACTIVE', 448, 'SYSTEM', DATEADD(MINUTE,-77454,@Now), DATEADD(MINUTE,-75700,@Now)),
(10033, N'Bò Bía Chay', 10137, N'Món Cuốn Chay', 'BUDDHIST', N'Bò Bía Chay – bản tăng cân', N'Bò Bía Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-042), phù hợp người tăng cân, khoảng 783.5 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Cho phần cuốn bánh:
2. Bước 1: Ướp ham chay với bột xá xíu trong khoảng 15 phút và đem chiên vàng.
3. Bước 2: Đem xào sắn và cà rốt với ít muối, bột ngọt và hạt nêm.
4. Bước 3: Xào sơ mì căn cho thơm và săn lại.
5. Cho nước tương chấm: Cho nước cốt dừa vào đun sôi. Sau đó, cho thêm tương đen và bơ đậu phộng vào khuấy đều. Nêm lại với ít muối và đường cho vừa miệng.
6. Cho dưa chua: Đem trộn các nguyên liệu làm dưa chua đã chuẩn bị vào với nhau và để trong khoảng 8 tiếng (Phần này bạn nên làm trước để tiết kiệm thời gian) Khi dùng, bạn dọn bánh tráng, rau sống, các nguyên liệu mì căn, ham chay, cà rốt xào với bát nước chấm đã cho thêm dưa chua và ít ớt bằm. Cách ăn giống như bánh tráng cuốn. Món ăn vặt này rất ngon và lạ miệng.
7. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 16, 15, 2, 783.5, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790885/veggielife/seed60/cookbook500/c500_042_bo_bia_chay.jpg', 'PENDING_REVIEW', 20, 'USER', DATEADD(MINUTE,-25641,@Now), DATEADD(MINUTE,-23110,@Now)),
(10034, N'Bò Bía Chay', 10001, N'Món Cuốn Chay', 'BUDDHIST', N'Bò Bía Chay – bản giữ cân', N'Bò Bía Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-042), phù hợp người giữ cân, khoảng 526.6 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Cho phần cuốn bánh:
2. Bước 1: Ướp ham chay với bột xá xíu trong khoảng 15 phút và đem chiên vàng.
3. Bước 2: Đem xào sắn và cà rốt với ít muối, bột ngọt và hạt nêm.
4. Bước 3: Xào sơ mì căn cho thơm và săn lại.
5. Cho nước tương chấm: Cho nước cốt dừa vào đun sôi. Sau đó, cho thêm tương đen và bơ đậu phộng vào khuấy đều. Nêm lại với ít muối và đường cho vừa miệng.
6. Cho dưa chua: Đem trộn các nguyên liệu làm dưa chua đã chuẩn bị vào với nhau và để trong khoảng 8 tiếng (Phần này bạn nên làm trước để tiết kiệm thời gian) Khi dùng, bạn dọn bánh tráng, rau sống, các nguyên liệu mì căn, ham chay, cà rốt xào với bát nước chấm đã cho thêm dưa chua và ít ớt bằm. Cách ăn giống như bánh tráng cuốn. Món ăn vặt này rất ngon và lạ miệng.
7. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 16, 15, 2, 526.6, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790885/veggielife/seed60/cookbook500/c500_042_bo_bia_chay.jpg', 'APPROVED_ACTIVE', 143, 'SYSTEM', DATEADD(MINUTE,-81067,@Now), DATEADD(MINUTE,-78826,@Now)),
(10035, N'Canh Khoai Nấm', 10001, N'Món Canh & Súp Bổ Dưỡng', 'BUDDHIST', N'Canh Khoai Nấm – bản giữ cân', N'Canh Khoai Nấm – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-028), phù hợp người giữ cân, khoảng 203.8 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Cà rốt bạn gọt vỏ, rửa sạch,cắt miếng vừa ăn.
2. Nên tỉa hoa cho món ăn thêm đẹp nhé.
3. Khoai tây bạn cũng gọt vỏ, cắt miếng, rồi ngâm với nước muối loãng.
4. Nấm rơm bạn cắt bỏ gốc, rửa sạch, cắt miếng.
5. Nấm hương khô bạn rửa sạch, ngâm nước cho nở mềm và nhớ giữ lại phần nước ngâm để nấu cho canh ngon ngọt hơn Cho cà rốt khoai tây vào nước ngâm nấm hầm lên cho chín mềm sau đó bạn cho nấm hương và nấm rơm vào nấu thêm khoảng 5 phút ,nêm lại cho vừa ăn rồi bạn tắt bếp cho ngò lên trên.
6. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 13, 10, 2, 203.8, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790854/veggielife/seed60/cookbook500/c500_028_canh_khoai_nam.jpg', 'APPROVED_ACTIVE', 1215, 'SYSTEM', DATEADD(MINUTE,-66398,@Now), DATEADD(MINUTE,-64877,@Now)),
(10036, N'Canh Khoai Nấm', 10001, N'Món Canh & Súp Bổ Dưỡng', 'BUDDHIST', N'Canh Khoai Nấm – bản tăng cân', N'Canh Khoai Nấm – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-028), phù hợp người tăng cân, khoảng 790.9 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Cà rốt bạn gọt vỏ, rửa sạch,cắt miếng vừa ăn.
2. Nên tỉa hoa cho món ăn thêm đẹp nhé.
3. Khoai tây bạn cũng gọt vỏ, cắt miếng, rồi ngâm với nước muối loãng.
4. Nấm rơm bạn cắt bỏ gốc, rửa sạch, cắt miếng.
5. Nấm hương khô bạn rửa sạch, ngâm nước cho nở mềm và nhớ giữ lại phần nước ngâm để nấu cho canh ngon ngọt hơn Cho cà rốt khoai tây vào nước ngâm nấm hầm lên cho chín mềm sau đó bạn cho nấm hương và nấm rơm vào nấu thêm khoảng 5 phút ,nêm lại cho vừa ăn rồi bạn tắt bếp cho ngò lên trên.
6. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 13, 10, 2, 790.9, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790854/veggielife/seed60/cookbook500/c500_028_canh_khoai_nam.jpg', 'APPROVED_ACTIVE', 1665, 'SYSTEM', DATEADD(MINUTE,-72006,@Now), DATEADD(MINUTE,-70537,@Now)),
(10037, N'Canh Khoai Nấm', 10001, N'Món Canh & Súp Bổ Dưỡng', 'BUDDHIST', N'Canh Khoai Nấm – bản giảm cân', N'Canh Khoai Nấm – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-028), phù hợp người giảm cân, khoảng 358.8 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Cà rốt bạn gọt vỏ, rửa sạch,cắt miếng vừa ăn.
2. Nên tỉa hoa cho món ăn thêm đẹp nhé.
3. Khoai tây bạn cũng gọt vỏ, cắt miếng, rồi ngâm với nước muối loãng.
4. Nấm rơm bạn cắt bỏ gốc, rửa sạch, cắt miếng.
5. Nấm hương khô bạn rửa sạch, ngâm nước cho nở mềm và nhớ giữ lại phần nước ngâm để nấu cho canh ngon ngọt hơn Cho cà rốt khoai tây vào nước ngâm nấm hầm lên cho chín mềm sau đó bạn cho nấm hương và nấm rơm vào nấu thêm khoảng 5 phút ,nêm lại cho vừa ăn rồi bạn tắt bếp cho ngò lên trên.
6. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 13, 10, 2, 358.8, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790854/veggielife/seed60/cookbook500/c500_028_canh_khoai_nam.jpg', 'APPROVED_ACTIVE', 985, 'SYSTEM', DATEADD(MINUTE,-76480,@Now), DATEADD(MINUTE,-75135,@Now)),
(10038, N'Nấm Rơm Xào Đậu Phụ', 10001, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Nấm Rơm Xào Đậu Phụ – bản giữ cân', N'Nấm Rơm Xào Đậu Phụ – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-099), phù hợp người giữ cân, khoảng 121.5 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Trước tiên, rửa sạch nấm rơm nè, rồi cắt làm đôi. Ướp nấm với đường, tiêu, hành tím băm nhỏ nhé.
2. Đậu phụ ấy cắt khúc hình chữ nhật, đem chiên sơ cho vàng. Đợi nguội ta cắt thành hình vuông nhỏ nhỏ.
3. Cà chua rửa sạch cắt lát mỏng. sau đó xay cho thật nát
4. Phi hành thơm rồi mình xào sơ nấm rơm cho chín
5. Tiếp tục, bạn bắc chảo dầu, bỏ hành vào phi vàng. Trút cà chua xay vào, nêm đường cát và ít nước lọc vào, đảo đều
6. Hỗn hợp cà chua sôi lên cho đậu khuôn vào tiếp tục cho nấm đã chín vào luôn trộn đều lên và dùng được rồi đó.
7. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 13, 20, 2, 121.5, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790997/veggielife/seed60/cookbook500/c500_099_nam_rom_xao_dau_phu.jpg', 'APPROVED_ACTIVE', 216, 'SYSTEM', DATEADD(MINUTE,-83735,@Now), DATEADD(MINUTE,-81431,@Now)),
(10039, N'Nấm Rơm Xào Đậu Phụ', 10090, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Nấm Rơm Xào Đậu Phụ – bản tăng cân', N'Nấm Rơm Xào Đậu Phụ – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-099), phù hợp người tăng cân, khoảng 786.9 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Trước tiên, rửa sạch nấm rơm nè, rồi cắt làm đôi. Ướp nấm với đường, tiêu, hành tím băm nhỏ nhé.
2. Đậu phụ ấy cắt khúc hình chữ nhật, đem chiên sơ cho vàng. Đợi nguội ta cắt thành hình vuông nhỏ nhỏ.
3. Cà chua rửa sạch cắt lát mỏng. sau đó xay cho thật nát
4. Phi hành thơm rồi mình xào sơ nấm rơm cho chín
5. Tiếp tục, bạn bắc chảo dầu, bỏ hành vào phi vàng. Trút cà chua xay vào, nêm đường cát và ít nước lọc vào, đảo đều
6. Hỗn hợp cà chua sôi lên cho đậu khuôn vào tiếp tục cho nấm đã chín vào luôn trộn đều lên và dùng được rồi đó.
7. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 13, 20, 2, 786.9, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790997/veggielife/seed60/cookbook500/c500_099_nam_rom_xao_dau_phu.jpg', 'APPROVED_ACTIVE', 140, 'USER', DATEADD(MINUTE,-28951,@Now), DATEADD(MINUTE,-26421,@Now)),
(10040, N'Rau Xào Thập Cẩm', 10001, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Rau Xào Thập Cẩm – bản giữ cân', N'Rau Xào Thập Cẩm – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-083), phù hợp người giữ cân, khoảng 241.5 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đầu tiên, bạn làm nóng dầu ô liu trong một chảo trên ngọn lửa vừa.
2. Sau đó, thêm đậu Hà Lan, cà rốt, hành và súp lơ.
3. Xào trong 3-5 phút, sau đó thêm tỏi vào rồi xào thêm khoảng 2-3 phút.
4. Sau đó, bạn cho thêm chút nước lọc vào rồi đun sôi và nêm mắm muối vừa ăn.
5. Đun tiếp đến khi rau chín và nước cạn bớt là được.
6. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 16, 10, 2, 241.5, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790966/veggielife/seed60/cookbook500/c500_083_rau_xao_thap_cam.jpg', 'APPROVED_ACTIVE', 977, 'SYSTEM', DATEADD(MINUTE,-65523,@Now), DATEADD(MINUTE,-62627,@Now)),
(10041, N'Rau Xào Thập Cẩm', 10017, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Rau Xào Thập Cẩm – bản tăng cân', N'Rau Xào Thập Cẩm – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-083), phù hợp người tăng cân, khoảng 785.3 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đầu tiên, bạn làm nóng dầu ô liu trong một chảo trên ngọn lửa vừa.
2. Sau đó, thêm đậu Hà Lan, cà rốt, hành và súp lơ.
3. Xào trong 3-5 phút, sau đó thêm tỏi vào rồi xào thêm khoảng 2-3 phút.
4. Sau đó, bạn cho thêm chút nước lọc vào rồi đun sôi và nêm mắm muối vừa ăn.
5. Đun tiếp đến khi rau chín và nước cạn bớt là được.
6. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 16, 10, 2, 785.3, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790966/veggielife/seed60/cookbook500/c500_083_rau_xao_thap_cam.jpg', 'APPROVED_ACTIVE', 412, 'USER', DATEADD(MINUTE,-28515,@Now), DATEADD(MINUTE,-26353,@Now)),
(10042, N'Rau Xào Thập Cẩm', 10001, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Rau Xào Thập Cẩm – bản giảm cân', N'Rau Xào Thập Cẩm – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-083), phù hợp người giảm cân, khoảng 328.1 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đầu tiên, bạn làm nóng dầu ô liu trong một chảo trên ngọn lửa vừa.
2. Sau đó, thêm đậu Hà Lan, cà rốt, hành và súp lơ.
3. Xào trong 3-5 phút, sau đó thêm tỏi vào rồi xào thêm khoảng 2-3 phút.
4. Sau đó, bạn cho thêm chút nước lọc vào rồi đun sôi và nêm mắm muối vừa ăn.
5. Đun tiếp đến khi rau chín và nước cạn bớt là được.
6. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 16, 10, 2, 328.1, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790966/veggielife/seed60/cookbook500/c500_083_rau_xao_thap_cam.jpg', 'APPROVED_ACTIVE', 1420, 'SYSTEM', DATEADD(MINUTE,-80552,@Now), DATEADD(MINUTE,-78572,@Now)),
(10043, N'Bắp Cải Cuốn', 10104, N'Món Cuốn Chay', 'BUDDHIST', N'Bắp Cải Cuốn – bản giữ cân', N'Bắp Cải Cuốn – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-043), phù hợp người giữ cân, khoảng 194.4 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Tách từng lá bắp cải ra, rửa sạch
2. Dùng dao nạo nạo những lát cà rốt mỏng và dài
3. Đun 1 nồi nước sôi, cho lá bắp cải vài chần trong nửa phút. Khi nào bắp cải chín, màu lá trở nên xanh mướt là được.
4. Ngâm lá bắp cải vừa chần vào nước lạnh trong nửa phút nữa. Cắt lá bắp cải xanh đã chần làm 2 phần, để 2 phần chồng lên nhau rồi cuộn chúng lại. Làm lần lượt đến hết.
5. Dùng các “dây” cà rốt đã chuẩn bị buộc các cuộn bắp cải để chúng không bung ra.
6. Thái nhỏ các lá bắp cải tím rồi đặt lên đĩa, xếp các miếng bắp cải xanh lên trên, rắc vừng rang.
7. Trộn xì dầu, dấm, 1 chút đường và dầu vừng trong một cái bát nhỏ. Đây là nước chấm tuyệt vời cho món rau thanh mát. Các bạn có thể chuẩn bị món này trong bữa cơm chay ngày mồng 1, ngày rằm hoặc như một món rau ăn đổi bữa hàng ngày đều được.
8. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 16, 20, 2, 194.4, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790887/veggielife/seed60/cookbook500/c500_043_bap_cai_cuon.jpg', 'APPROVED_ACTIVE', 26, 'USER', DATEADD(MINUTE,-1756,@Now), DATEADD(MINUTE,-1,@Now)),
(10044, N'Rau Củ Xào Thập Cẩm', 10001, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Rau Củ Xào Thập Cẩm – bản giảm cân', N'Rau Củ Xào Thập Cẩm – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-008), phù hợp người giảm cân, khoảng 265.3 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đun nóng dầu trong chảo, dầu nóng thêm hành tây, cà rốt vào đảo đều trong 2 phút.
2. Thêm đậu Hà Lan, bông cải, ớt chuông vào xào thêm 5-7 phút cho đến khi rau củ vừa chín.
3. Nêm nước tương, bột tỏi, ít muối vừa ăn.
4. Rắc hạt mè lên, tắt bếp.
5. Dọn rau củ xào thập cẩm ăn kèm cơm trắng.
6. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 19, 10, 2, 265.3, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790813/veggielife/seed60/cookbook500/c500_008_rau_cu_xao_thap_cam.jpg', 'APPROVED_ACTIVE', 1427, 'SYSTEM', DATEADD(MINUTE,-84389,@Now), DATEADD(MINUTE,-82361,@Now)),
(10045, N'Rau Củ Xào Thập Cẩm', 10001, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Rau Củ Xào Thập Cẩm – bản giữ cân', N'Rau Củ Xào Thập Cẩm – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-008), phù hợp người giữ cân, khoảng 479.9 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đun nóng dầu trong chảo, dầu nóng thêm hành tây, cà rốt vào đảo đều trong 2 phút.
2. Thêm đậu Hà Lan, bông cải, ớt chuông vào xào thêm 5-7 phút cho đến khi rau củ vừa chín.
3. Nêm nước tương, bột tỏi, ít muối vừa ăn.
4. Rắc hạt mè lên, tắt bếp.
5. Dọn rau củ xào thập cẩm ăn kèm cơm trắng.
6. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 19, 10, 2, 479.9, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790813/veggielife/seed60/cookbook500/c500_008_rau_cu_xao_thap_cam.jpg', 'APPROVED_ACTIVE', 563, 'SYSTEM', DATEADD(MINUTE,-65254,@Now), DATEADD(MINUTE,-64683,@Now)),
(10046, N'Bún Trộn Rau Cải', 10110, N'Món Mì, Bún & Phở Chay', 'VEGAN', N'Bún Trộn Rau Cải – bản giữ cân', N'Bún Trộn Rau Cải – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-019), phù hợp người giữ cân, khoảng 529.1 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Xào thịt bò chay: Thịt bò chay sau khi đã sơ chế (mì căng), xé hoặc thái miếng vừa ăn rồi tẩm ướp bằng nước tương,chút đường thô, tiêu, sả bằm, gừng băm nhỏ, tiêu, chút xíu dầu ăn thực vật. Trộn đều rồi để trong khoảng 15 phút. Thái hành tây dọc theo củ thành miếng dài. Cho hành tây vào chảo có chút dầu nóng xào trước cho hơi trong, có mùi thơm thì cho thịt bò chay đã ướp vào xào cho chín mềm. Có thể cho chút nước nếu cần. Bắc ra để cho âm ấm.
2. Chuẩn bị rau sống: Giá rửa sạch, để ráo Các loại rau rửa sạch, để ráo, rồi thái sợi. Trước khi ăn: Cho bún vào bát. Xếp rau sống, giá, thịt bò chay xào lên trên. Rắc ít lạc rang giã dập, rồi đến hành khô phi lên trên cùng. Lúc ăn, rưới nước chấm bò bún Nên dùng nước lọc ấm để pha nước chấm.
3. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 22, 15, 2, 529.1, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790836/veggielife/seed60/cookbook500/c500_019_bun_tron_rau_cai.jpg', 'APPROVED_ACTIVE', 84, 'USER', DATEADD(MINUTE,-15760,@Now), DATEADD(MINUTE,-14346,@Now)),
(10047, N'Bún Trộn Rau Cải', 10121, N'Món Mì, Bún & Phở Chay', 'VEGAN', N'Bún Trộn Rau Cải – bản tăng cân', N'Bún Trộn Rau Cải – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-019), phù hợp người tăng cân, khoảng 690.7 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Xào thịt bò chay: Thịt bò chay sau khi đã sơ chế (mì căng), xé hoặc thái miếng vừa ăn rồi tẩm ướp bằng nước tương,chút đường thô, tiêu, sả bằm, gừng băm nhỏ, tiêu, chút xíu dầu ăn thực vật. Trộn đều rồi để trong khoảng 15 phút. Thái hành tây dọc theo củ thành miếng dài. Cho hành tây vào chảo có chút dầu nóng xào trước cho hơi trong, có mùi thơm thì cho thịt bò chay đã ướp vào xào cho chín mềm. Có thể cho chút nước nếu cần. Bắc ra để cho âm ấm.
2. Chuẩn bị rau sống: Giá rửa sạch, để ráo Các loại rau rửa sạch, để ráo, rồi thái sợi. Trước khi ăn: Cho bún vào bát. Xếp rau sống, giá, thịt bò chay xào lên trên. Rắc ít lạc rang giã dập, rồi đến hành khô phi lên trên cùng. Lúc ăn, rưới nước chấm bò bún Nên dùng nước lọc ấm để pha nước chấm.
3. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 22, 15, 2, 690.7, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790836/veggielife/seed60/cookbook500/c500_019_bun_tron_rau_cai.jpg', 'PENDING_REVIEW', 11, 'USER', DATEADD(MINUTE,-53143,@Now), DATEADD(MINUTE,-52203,@Now)),
(10048, N'Đậu Hủ Sốt Cay', 10001, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Đậu Hủ Sốt Cay – bản giữ cân', N'Đậu Hủ Sốt Cay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-159), phù hợp người giữ cân, khoảng 519.4 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đậu hũ rửa sạch cắt miếng vuông mỏng. Bột ngô bỏ ra đĩa, gắp từng miếng đậu tẩm qua bột rồi cho vào chảo dầu nóng rán vàng, bột chỉ cần bám sơ quanh miếng đậu là được.
2. Khi rán đậu chú ý để lửa nhỏ để tránh bột bị cháy. Đậu rán chín vàng hai mặt thì cho ra đĩa, để sang một bên.
3. Đặt chảo trở lại bếp, dầu ăn cho láng chảo, phi tỏi thơm. Cho đầu hành thái nhỏ vào phi cùng, tiếp đến cho ớt thái lát, cà chua thái hạt lựu vào đun. Cho một chút nước nếu chảo quá khô và để tránh cà chua bị cháy.
4. Sau khi cà chua đã nhừ thành nước sốt thì cho nấm vào xào, nêm 1 thìa bột canh, ½ thìa đường.
5. Tiếp theo cho đậu rán vào chảo, thêm nửa chén nước, rồi mở lửa to đun sôi, nêm nếm lại bột canh cho vừa ăn. Nhỏ lửa âm ỉ cho đậu ngấm gia vị.
6. Đun đến khi nước cạn bớt là được. Khi đun tránh dùng đũa đảo nhiều, đậu sẽ bị vỡ. Trước khi tắt bếp rắc chút hành xanh thái nhỏ. Xúc đậu ra bát to, rắc hạt tiêu lên trên, món chay ăn nóng với cơm.
7. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 19, 20, 2, 519.4, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791131/veggielife/seed60/cookbook500/c500_159_dau_hu_sot_cay.jpg', 'APPROVED_ACTIVE', 193, 'SYSTEM', DATEADD(MINUTE,-68180,@Now), DATEADD(MINUTE,-65504,@Now)),
(10049, N'Đậu Hủ Sốt Cay', 10001, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Đậu Hủ Sốt Cay – bản tăng cân', N'Đậu Hủ Sốt Cay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-159), phù hợp người tăng cân, khoảng 785.1 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đậu hũ rửa sạch cắt miếng vuông mỏng. Bột ngô bỏ ra đĩa, gắp từng miếng đậu tẩm qua bột rồi cho vào chảo dầu nóng rán vàng, bột chỉ cần bám sơ quanh miếng đậu là được.
2. Khi rán đậu chú ý để lửa nhỏ để tránh bột bị cháy. Đậu rán chín vàng hai mặt thì cho ra đĩa, để sang một bên.
3. Đặt chảo trở lại bếp, dầu ăn cho láng chảo, phi tỏi thơm. Cho đầu hành thái nhỏ vào phi cùng, tiếp đến cho ớt thái lát, cà chua thái hạt lựu vào đun. Cho một chút nước nếu chảo quá khô và để tránh cà chua bị cháy.
4. Sau khi cà chua đã nhừ thành nước sốt thì cho nấm vào xào, nêm 1 thìa bột canh, ½ thìa đường.
5. Tiếp theo cho đậu rán vào chảo, thêm nửa chén nước, rồi mở lửa to đun sôi, nêm nếm lại bột canh cho vừa ăn. Nhỏ lửa âm ỉ cho đậu ngấm gia vị.
6. Đun đến khi nước cạn bớt là được. Khi đun tránh dùng đũa đảo nhiều, đậu sẽ bị vỡ. Trước khi tắt bếp rắc chút hành xanh thái nhỏ. Xúc đậu ra bát to, rắc hạt tiêu lên trên, món chay ăn nóng với cơm.
7. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 19, 20, 2, 785.1, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791131/veggielife/seed60/cookbook500/c500_159_dau_hu_sot_cay.jpg', 'APPROVED_ACTIVE', 667, 'SYSTEM', DATEADD(MINUTE,-82227,@Now), DATEADD(MINUTE,-81819,@Now)),
(10050, N'Đậu Hủ Sốt Cay', 10132, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Đậu Hủ Sốt Cay – bản giảm cân', N'Đậu Hủ Sốt Cay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-159), phù hợp người giảm cân, khoảng 399.4 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đậu hũ rửa sạch cắt miếng vuông mỏng. Bột ngô bỏ ra đĩa, gắp từng miếng đậu tẩm qua bột rồi cho vào chảo dầu nóng rán vàng, bột chỉ cần bám sơ quanh miếng đậu là được.
2. Khi rán đậu chú ý để lửa nhỏ để tránh bột bị cháy. Đậu rán chín vàng hai mặt thì cho ra đĩa, để sang một bên.
3. Đặt chảo trở lại bếp, dầu ăn cho láng chảo, phi tỏi thơm. Cho đầu hành thái nhỏ vào phi cùng, tiếp đến cho ớt thái lát, cà chua thái hạt lựu vào đun. Cho một chút nước nếu chảo quá khô và để tránh cà chua bị cháy.
4. Sau khi cà chua đã nhừ thành nước sốt thì cho nấm vào xào, nêm 1 thìa bột canh, ½ thìa đường.
5. Tiếp theo cho đậu rán vào chảo, thêm nửa chén nước, rồi mở lửa to đun sôi, nêm nếm lại bột canh cho vừa ăn. Nhỏ lửa âm ỉ cho đậu ngấm gia vị.
6. Đun đến khi nước cạn bớt là được. Khi đun tránh dùng đũa đảo nhiều, đậu sẽ bị vỡ. Trước khi tắt bếp rắc chút hành xanh thái nhỏ. Xúc đậu ra bát to, rắc hạt tiêu lên trên, món chay ăn nóng với cơm.
7. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 19, 20, 2, 399.4, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791131/veggielife/seed60/cookbook500/c500_159_dau_hu_sot_cay.jpg', 'DRAFT', 15, 'USER', DATEADD(MINUTE,-27860,@Now), CAST(NULL AS DATETIME2(7))),
(10051, N'Nui Gạo Chay', 10001, N'Món Mì, Bún & Phở Chay', 'VEGAN', N'Nui Gạo Chay – bản tăng cân', N'Nui Gạo Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-073), phù hợp người tăng cân, khoảng 749.6 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Gọt cà rốt, khoai tây, khoai lang, su su, nấm cắt bỏ chân, ngâm chung vào âu nước muỗi pha loãng.
2. Chả chay cắt lát mỏng, rán sơ.
3. Nấm sau khi rửa sạch, để ráo, cắt lát mỏng. Đun nóng dầu ăn, phi tỏi tây thơm, cho nấm vào xào, thêm vào hai thìa canh xì dầu, một ít muối, đường, một ít hat tiêu, đậy kín nắp khoảng 6 - 8 phút.
4. Cho đậu phụ, chả chay vào xào cùng với nấm, tiếp tục đậy kín nắp, đun lửa nhỏ để
5. nguyên liệu thấm gia vị. Đun khoảng 10 phút thì tắt bếp.
6. Cà rốt, khoai tây, khoai lang, su su sau khi rửa sạch, để ráo, cắt khúc vừa ăn. Cho tất cả rau củ vào nồi, châm nước lạnh ngập mặt, đặt lên bếp đun sôi, thêm đường phèn, một ít muối. Đun đến khi dùng đũa xiên nhẹ qua cà rốt mềm.
7. Cho đậu phụ, nấm đã xào ở bước 4 vào nồi, tiếp tục đun sôi khoảng 5 phút nữa thì nêm nếm lại gia vị cho vừa ăn, tắt bếp.
8. Dùng chảo nhỏ, cho một ít dầu điều, phi tỏi tây thơm, tiếp theo đổ phần tỏi tây xào thơm lên bề mặt của nồi súp ở bước 6, đun sôi lại nồi nước súp khoảng 2 phút nữa thì tắt bếp.
9. Đun nồi nước sôi, cho nui gạo vào nồi, luộc chín, đổ ra rổ cho ráo nước.
10. Khi dùng múc một ít nui gạo vào bát, thêm nước súp, rau củ và đậu phụ, nấm, chả chay, dùng nóng.
11. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 22, 25, 2, 749.6, 'HARD', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790948/veggielife/seed60/cookbook500/c500_073_nui_gao_chay.jpg', 'APPROVED_ACTIVE', 765, 'SYSTEM', DATEADD(MINUTE,-77086,@Now), DATEADD(MINUTE,-76212,@Now)),
(10052, N'Nui Gạo Chay', 10001, N'Món Mì, Bún & Phở Chay', 'VEGAN', N'Nui Gạo Chay – bản giảm cân', N'Nui Gạo Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-073), phù hợp người giảm cân, khoảng 373.9 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Gọt cà rốt, khoai tây, khoai lang, su su, nấm cắt bỏ chân, ngâm chung vào âu nước muỗi pha loãng.
2. Chả chay cắt lát mỏng, rán sơ.
3. Nấm sau khi rửa sạch, để ráo, cắt lát mỏng. Đun nóng dầu ăn, phi tỏi tây thơm, cho nấm vào xào, thêm vào hai thìa canh xì dầu, một ít muối, đường, một ít hat tiêu, đậy kín nắp khoảng 6 - 8 phút.
4. Cho đậu phụ, chả chay vào xào cùng với nấm, tiếp tục đậy kín nắp, đun lửa nhỏ để
5. nguyên liệu thấm gia vị. Đun khoảng 10 phút thì tắt bếp.
6. Cà rốt, khoai tây, khoai lang, su su sau khi rửa sạch, để ráo, cắt khúc vừa ăn. Cho tất cả rau củ vào nồi, châm nước lạnh ngập mặt, đặt lên bếp đun sôi, thêm đường phèn, một ít muối. Đun đến khi dùng đũa xiên nhẹ qua cà rốt mềm.
7. Cho đậu phụ, nấm đã xào ở bước 4 vào nồi, tiếp tục đun sôi khoảng 5 phút nữa thì nêm nếm lại gia vị cho vừa ăn, tắt bếp.
8. Dùng chảo nhỏ, cho một ít dầu điều, phi tỏi tây thơm, tiếp theo đổ phần tỏi tây xào thơm lên bề mặt của nồi súp ở bước 6, đun sôi lại nồi nước súp khoảng 2 phút nữa thì tắt bếp.
9. Đun nồi nước sôi, cho nui gạo vào nồi, luộc chín, đổ ra rổ cho ráo nước.
10. Khi dùng múc một ít nui gạo vào bát, thêm nước súp, rau củ và đậu phụ, nấm, chả chay, dùng nóng.
11. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 22, 25, 2, 373.9, 'HARD', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790948/veggielife/seed60/cookbook500/c500_073_nui_gao_chay.jpg', 'APPROVED_ACTIVE', 1537, 'SYSTEM', DATEADD(MINUTE,-69901,@Now), DATEADD(MINUTE,-67874,@Now)),
(10053, N'Cơm Sườn Chay', 10001, N'Món Cơm & Xôi Chay', 'BUDDHIST', N'Cơm Sườn Chay – bản tăng cân', N'Cơm Sườn Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-014), phù hợp người tăng cân, khoảng 669.4 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Sườn non chay chọn mua thương hiệu của Âu Lạc , đóng gói sẵn loại 0,5kg hoặc 1kg, mua về chế biến ko hết thì để dành loại này ăn đảm bảo chất lượng hơn hàng mua lẻ ở chợ.
2. Sườn đem ngâm nước khoảng 5 phút cho mềm, vớt ra ép ráo nước, ướp với các gia vị sau: sả băm nhỏ, 1 muỗng hạt nem chay, 1 muỗng muối, 1 muỗng xì dầu, 1 muỗng đường.
3. Để ít nhất 10 phút cho sườn thấm.
4. Cho chảo dầu nóng lên bếp rồi chiên sườn, chiên lửa nhỏ đến khi thấy sườn vàng đều 2 mặt là được.
5. Cho cơm ra đĩa, trang trí thêm dưa leo, xà lách, cà chua vào, cho sườn đã chiên vàng lên, làm thêm chém xì dầu ớt và chén canh mồng tơi nấu đậu hủ nữa là xong bữa.
6. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 19, 15, 2, 669.4, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790826/veggielife/seed60/cookbook500/c500_014_com_suon_chay.jpg', 'APPROVED_ACTIVE', 821, 'SYSTEM', DATEADD(MINUTE,-74576,@Now), DATEADD(MINUTE,-73673,@Now)),
(10054, N'Cơm Sườn Chay', 10001, N'Món Cơm & Xôi Chay', 'BUDDHIST', N'Cơm Sườn Chay – bản giảm cân', N'Cơm Sườn Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-014), phù hợp người giảm cân, khoảng 393.8 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Sườn non chay chọn mua thương hiệu của Âu Lạc , đóng gói sẵn loại 0,5kg hoặc 1kg, mua về chế biến ko hết thì để dành loại này ăn đảm bảo chất lượng hơn hàng mua lẻ ở chợ.
2. Sườn đem ngâm nước khoảng 5 phút cho mềm, vớt ra ép ráo nước, ướp với các gia vị sau: sả băm nhỏ, 1 muỗng hạt nem chay, 1 muỗng muối, 1 muỗng xì dầu, 1 muỗng đường.
3. Để ít nhất 10 phút cho sườn thấm.
4. Cho chảo dầu nóng lên bếp rồi chiên sườn, chiên lửa nhỏ đến khi thấy sườn vàng đều 2 mặt là được.
5. Cho cơm ra đĩa, trang trí thêm dưa leo, xà lách, cà chua vào, cho sườn đã chiên vàng lên, làm thêm chém xì dầu ớt và chén canh mồng tơi nấu đậu hủ nữa là xong bữa.
6. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 19, 15, 2, 393.8, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790826/veggielife/seed60/cookbook500/c500_014_com_suon_chay.jpg', 'APPROVED_ACTIVE', 1179, 'SYSTEM', DATEADD(MINUTE,-70403,@Now), DATEADD(MINUTE,-68462,@Now)),
(10055, N'Miến Xào Với Mít Non', 10001, N'Món Mì, Bún & Phở Chay', 'VEGAN', N'Miến Xào Với Mít Non – bản giữ cân', N'Miến Xào Với Mít Non – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-032), phù hợp người giữ cân, khoảng 240.9 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Mít non rửa sạch nhựa, chần sơ qua nồi nước sôi sau đó để nguội dùng tay xé nhỏ.
2. Miến ngâm nở, cắt làm đôi, chần sơ miến qua nồi nước sôi, xả lại dưới vòi nước lạnh để miến không bị dính.
3. Mộc nhĩ ngâm nở, rửa sạch, cắt bỏ chân, thái sợi.
4. Đun nóng hai thìa nhỏ dầu ăn, phi boaro cho thơm, cho mít vào xào thơm khoảng 5 phút, thêm một thìa nhỏ nước mắm chay, nửa thìa nhỏ hạt nêm chay, ớt bột, nửa thìa nhỏ muối, đảo đều.
5. Cho mộc nhĩ vào đảo cùng, đều tay.
6. Cuối cùng cho miến vào xào cùng, đảo nhẹ tay để sợi miến không bị nát, thêm gia vị cho vừa miệng, xào khoảng 5 phút.
7. Tắt bếp, múc ra đĩa dùng nóng.
8. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 16, 10, 2, 240.9, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790863/veggielife/seed60/cookbook500/c500_032_mien_xao_voi_mit_non.jpg', 'APPROVED_ACTIVE', 196, 'SYSTEM', DATEADD(MINUTE,-79599,@Now), DATEADD(MINUTE,-77607,@Now)),
(10056, N'Miến Xào Với Mít Non', 10091, N'Món Mì, Bún & Phở Chay', 'VEGAN', N'Miến Xào Với Mít Non – bản giảm cân', N'Miến Xào Với Mít Non – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-032), phù hợp người giảm cân, khoảng 333.6 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Mít non rửa sạch nhựa, chần sơ qua nồi nước sôi sau đó để nguội dùng tay xé nhỏ.
2. Miến ngâm nở, cắt làm đôi, chần sơ miến qua nồi nước sôi, xả lại dưới vòi nước lạnh để miến không bị dính.
3. Mộc nhĩ ngâm nở, rửa sạch, cắt bỏ chân, thái sợi.
4. Đun nóng hai thìa nhỏ dầu ăn, phi boaro cho thơm, cho mít vào xào thơm khoảng 5 phút, thêm một thìa nhỏ nước mắm chay, nửa thìa nhỏ hạt nêm chay, ớt bột, nửa thìa nhỏ muối, đảo đều.
5. Cho mộc nhĩ vào đảo cùng, đều tay.
6. Cuối cùng cho miến vào xào cùng, đảo nhẹ tay để sợi miến không bị nát, thêm gia vị cho vừa miệng, xào khoảng 5 phút.
7. Tắt bếp, múc ra đĩa dùng nóng.
8. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 16, 10, 2, 333.6, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790863/veggielife/seed60/cookbook500/c500_032_mien_xao_voi_mit_non.jpg', 'REJECTED', 8, 'USER', DATEADD(MINUTE,-22235,@Now), DATEADD(MINUTE,-19870,@Now)),
(10057, N'Đậu Hũ La Hán', 10001, N'Món Hấp & Luộc Dưỡng Sinh', 'BUDDHIST', N'Đậu Hũ La Hán – bản giảm cân', N'Đậu Hũ La Hán – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-064), phù hợp người giảm cân, khoảng 371.7 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Cắt miếng dăm bông chay ra thành các lát mỏng,
2. Tiếp theo, xắt miếng đậu hũ non thành các miếng hình chữ nhật. Khi làm món ăn này thì các bạn nên chọn mua đậu hũ non để chế biến món ăn, vì đậu hũ non rất mềm, dễ ăn, dễ chế biến – phù hợp với món chay.
3. Sau khi cải chín thì vớt cải ra một tô nước lạnh, nồi nước luộc đang sôi, cho tiếp nấm đông cô và trần sơ qua rồi cũng đổ nấm vào tô nước lạnh đang ngâm cải.
4. Nấm đông cô tươi mua về chỉ nên cắt bỏ một chút châm nấm ở dưới thân rồi ngâm với nước ấm có cho 1 thìa café muối nhỏ. Bạn có thể khía các đường đồng tâm trên đỉnh nấm để tạo hoa đẹp mắt.
5. Cải thìa nhặt và rửa sạch lá úa, lá bẩn. Chuẩn bị một nồi nước để luộc cải thìa nguyên cây. Bí quyết luộc cải ngon và đẹp mắt là bạn cho 1 thìa café dầu ăn và ½ thìa café muối vào nồi nước luộc. Khi cải chín vớt ra sẽ xanh hơn, nhìn ngọn cải mướt và rất bóng mượt, đẹp mắt.
6. Vớt rau và nấm ra để ráo. Cải thìa dùng dao chẻ đôi dọc theo thân cây để vừa ăn. Gừng cạo vỏ và rửa sạch, để ráo nước, thái lát. Gừng này giúp món ăn chay của bạn giảm bớt tính hàn, cân bằng tính hàn- nhiệt của món ăn và giúp món ăn thơm ngon hơn.
7. Sau khi sơ chế xong nguyên liệu. Chuẩn bị một nồi nước để hấp. Sắp cải đều lên ½ diện tích của đĩa, ½ đĩa còn lại đặt một miếng đậu, xen kẽ một miếng dăm bông thịt nguội, xen kẽ một cây nấm, xen kẽ một lát gừng.
8. Lấy một chiếc bát to để pha nước sốt với công thức: 1,5 muỗng canh dầu hào chay, ½ muỗng canh nước tương, 1/3 muỗng canh tiêu, 1 muỗng canh đường, 1 muỗng café đường, 1 chút tiêu, ½ muỗng dầu mè, 1,5 muỗng nước lọc, 1,5 muỗng café bột năng. Trộn đều cho đến khi các gia vị tan ra và hòa đều vào nhau.
9. Rưới nước sốt đều lên trên mặt đĩa đậu phụ đã sắp, rắc cùng với một chút kì tử. Cho vào nồi nước sôi để hấp cách thủy. Sau mười phút là món ăn của bạn hoàn thành rồi đấy.
10. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 22, 20, 2, 371.7, 'HARD', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790929/veggielife/seed60/cookbook500/c500_064_dau_hu_la_han.jpg', 'APPROVED_ACTIVE', 226, 'SYSTEM', DATEADD(MINUTE,-77012,@Now), DATEADD(MINUTE,-74273,@Now)),
(10058, N'Đậu Hũ La Hán', 10001, N'Món Hấp & Luộc Dưỡng Sinh', 'BUDDHIST', N'Đậu Hũ La Hán – bản giữ cân', N'Đậu Hũ La Hán – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-064), phù hợp người giữ cân, khoảng 514.3 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Cắt miếng dăm bông chay ra thành các lát mỏng,
2. Tiếp theo, xắt miếng đậu hũ non thành các miếng hình chữ nhật. Khi làm món ăn này thì các bạn nên chọn mua đậu hũ non để chế biến món ăn, vì đậu hũ non rất mềm, dễ ăn, dễ chế biến – phù hợp với món chay.
3. Sau khi cải chín thì vớt cải ra một tô nước lạnh, nồi nước luộc đang sôi, cho tiếp nấm đông cô và trần sơ qua rồi cũng đổ nấm vào tô nước lạnh đang ngâm cải.
4. Nấm đông cô tươi mua về chỉ nên cắt bỏ một chút châm nấm ở dưới thân rồi ngâm với nước ấm có cho 1 thìa café muối nhỏ. Bạn có thể khía các đường đồng tâm trên đỉnh nấm để tạo hoa đẹp mắt.
5. Cải thìa nhặt và rửa sạch lá úa, lá bẩn. Chuẩn bị một nồi nước để luộc cải thìa nguyên cây. Bí quyết luộc cải ngon và đẹp mắt là bạn cho 1 thìa café dầu ăn và ½ thìa café muối vào nồi nước luộc. Khi cải chín vớt ra sẽ xanh hơn, nhìn ngọn cải mướt và rất bóng mượt, đẹp mắt.
6. Vớt rau và nấm ra để ráo. Cải thìa dùng dao chẻ đôi dọc theo thân cây để vừa ăn. Gừng cạo vỏ và rửa sạch, để ráo nước, thái lát. Gừng này giúp món ăn chay của bạn giảm bớt tính hàn, cân bằng tính hàn- nhiệt của món ăn và giúp món ăn thơm ngon hơn.
7. Sau khi sơ chế xong nguyên liệu. Chuẩn bị một nồi nước để hấp. Sắp cải đều lên ½ diện tích của đĩa, ½ đĩa còn lại đặt một miếng đậu, xen kẽ một miếng dăm bông thịt nguội, xen kẽ một cây nấm, xen kẽ một lát gừng.
8. Lấy một chiếc bát to để pha nước sốt với công thức: 1,5 muỗng canh dầu hào chay, ½ muỗng canh nước tương, 1/3 muỗng canh tiêu, 1 muỗng canh đường, 1 muỗng café đường, 1 chút tiêu, ½ muỗng dầu mè, 1,5 muỗng nước lọc, 1,5 muỗng café bột năng. Trộn đều cho đến khi các gia vị tan ra và hòa đều vào nhau.
9. Rưới nước sốt đều lên trên mặt đĩa đậu phụ đã sắp, rắc cùng với một chút kì tử. Cho vào nồi nước sôi để hấp cách thủy. Sau mười phút là món ăn của bạn hoàn thành rồi đấy.
10. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 22, 20, 2, 514.3, 'HARD', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790929/veggielife/seed60/cookbook500/c500_064_dau_hu_la_han.jpg', 'APPROVED_ACTIVE', 1691, 'SYSTEM', DATEADD(MINUTE,-79616,@Now), DATEADD(MINUTE,-76993,@Now)),
(10059, N'Đậu Hũ La Hán', 10065, N'Món Hấp & Luộc Dưỡng Sinh', 'BUDDHIST', N'Đậu Hũ La Hán – bản tăng cân', N'Đậu Hũ La Hán – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-064), phù hợp người tăng cân, khoảng 741.8 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Cắt miếng dăm bông chay ra thành các lát mỏng,
2. Tiếp theo, xắt miếng đậu hũ non thành các miếng hình chữ nhật. Khi làm món ăn này thì các bạn nên chọn mua đậu hũ non để chế biến món ăn, vì đậu hũ non rất mềm, dễ ăn, dễ chế biến – phù hợp với món chay.
3. Sau khi cải chín thì vớt cải ra một tô nước lạnh, nồi nước luộc đang sôi, cho tiếp nấm đông cô và trần sơ qua rồi cũng đổ nấm vào tô nước lạnh đang ngâm cải.
4. Nấm đông cô tươi mua về chỉ nên cắt bỏ một chút châm nấm ở dưới thân rồi ngâm với nước ấm có cho 1 thìa café muối nhỏ. Bạn có thể khía các đường đồng tâm trên đỉnh nấm để tạo hoa đẹp mắt.
5. Cải thìa nhặt và rửa sạch lá úa, lá bẩn. Chuẩn bị một nồi nước để luộc cải thìa nguyên cây. Bí quyết luộc cải ngon và đẹp mắt là bạn cho 1 thìa café dầu ăn và ½ thìa café muối vào nồi nước luộc. Khi cải chín vớt ra sẽ xanh hơn, nhìn ngọn cải mướt và rất bóng mượt, đẹp mắt.
6. Vớt rau và nấm ra để ráo. Cải thìa dùng dao chẻ đôi dọc theo thân cây để vừa ăn. Gừng cạo vỏ và rửa sạch, để ráo nước, thái lát. Gừng này giúp món ăn chay của bạn giảm bớt tính hàn, cân bằng tính hàn- nhiệt của món ăn và giúp món ăn thơm ngon hơn.
7. Sau khi sơ chế xong nguyên liệu. Chuẩn bị một nồi nước để hấp. Sắp cải đều lên ½ diện tích của đĩa, ½ đĩa còn lại đặt một miếng đậu, xen kẽ một miếng dăm bông thịt nguội, xen kẽ một cây nấm, xen kẽ một lát gừng.
8. Lấy một chiếc bát to để pha nước sốt với công thức: 1,5 muỗng canh dầu hào chay, ½ muỗng canh nước tương, 1/3 muỗng canh tiêu, 1 muỗng canh đường, 1 muỗng café đường, 1 chút tiêu, ½ muỗng dầu mè, 1,5 muỗng nước lọc, 1,5 muỗng café bột năng. Trộn đều cho đến khi các gia vị tan ra và hòa đều vào nhau.
9. Rưới nước sốt đều lên trên mặt đĩa đậu phụ đã sắp, rắc cùng với một chút kì tử. Cho vào nồi nước sôi để hấp cách thủy. Sau mười phút là món ăn của bạn hoàn thành rồi đấy.
10. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 22, 20, 2, 741.8, 'HARD', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790929/veggielife/seed60/cookbook500/c500_064_dau_hu_la_han.jpg', 'APPROVED_ACTIVE', 109, 'AI', DATEADD(MINUTE,-2515,@Now), DATEADD(MINUTE,-157,@Now)),
(10060, N'Bún Trộn Dưa Leo', 10001, N'Món Mì, Bún & Phở Chay', 'VEGAN', N'Bún Trộn Dưa Leo – bản giữ cân', N'Bún Trộn Dưa Leo – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-112), phù hợp người giữ cân, khoảng 594.6 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Bún rửa sạch, ngâm nước cho bún khô mềm ra.
2. Bước 2: Dưa chuột rửa sạch, bỏ ruột, bào sợi mảnh.
3. Bước 3: Đun nước sôi, sau đó thả bún vào trần cho chín. Khi bún đã chín vừa tới thì vớt ra cho vào nước lạnh và để ráo.
4. Bước 4: Gừng, tỏi băm nhỏ, cho vào tô trộn cùng giấm.
5. Bước 5: Thêm dầu mè, đường, nước mắm chay, ớt vào trộn đều rồi đặt lên bếp đun sôi sao cho hỗn hợp sốt vừa miệng.
6. Bước 6: Trộn bún cùng với dưa leo thái sợi.
7. Bước 7: Đổ phần nước sốt vào trộn đều lên và thưởng thức.
8. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 19, 20, 2, 594.6, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791028/veggielife/seed60/cookbook500/c500_112_bun_tron_dua_leo.jpg', 'APPROVED_ACTIVE', 1559, 'SYSTEM', DATEADD(MINUTE,-68302,@Now), DATEADD(MINUTE,-66109,@Now))
) AS v(rid, fn, uid, cat, sc, ti, de, ins, pr, ck, sv, kc, df, vf, im, st, vw, sr, cr, up)
JOIN #food f ON f.Food_Name = v.fn
JOIN #cat c ON c.Category_Name = v.cat
JOIN #sch s ON s.School_Code = v.sc;
IF @@ROWCOUNT <> 60 THROW 50001, N'Recipes: tra cứu dữ liệu master thất bại (thiếu tên/mã)', 1;
INSERT INTO dbo.Recipes (Recipe_ID, Food_ID, User_ID, Category_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At)
SELECT v.rid, f.Food_ID, v.uid, c.Category_ID, s.Dietary_School_ID, v.ti, v.de, v.ins, v.pr, v.ck, v.sv, v.kc, v.df, v.vf, v.im, v.st, v.vw, v.sr, v.cr, v.up
FROM (VALUES
(10061, N'Bún Trộn Dưa Leo', 10006, N'Món Mì, Bún & Phở Chay', 'VEGAN', N'Bún Trộn Dưa Leo – bản tăng cân', N'Bún Trộn Dưa Leo – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-112), phù hợp người tăng cân, khoảng 723.9 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Bún rửa sạch, ngâm nước cho bún khô mềm ra.
2. Bước 2: Dưa chuột rửa sạch, bỏ ruột, bào sợi mảnh.
3. Bước 3: Đun nước sôi, sau đó thả bún vào trần cho chín. Khi bún đã chín vừa tới thì vớt ra cho vào nước lạnh và để ráo.
4. Bước 4: Gừng, tỏi băm nhỏ, cho vào tô trộn cùng giấm.
5. Bước 5: Thêm dầu mè, đường, nước mắm chay, ớt vào trộn đều rồi đặt lên bếp đun sôi sao cho hỗn hợp sốt vừa miệng.
6. Bước 6: Trộn bún cùng với dưa leo thái sợi.
7. Bước 7: Đổ phần nước sốt vào trộn đều lên và thưởng thức.
8. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 19, 20, 2, 723.9, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791028/veggielife/seed60/cookbook500/c500_112_bun_tron_dua_leo.jpg', 'QUARANTINED_HIDDEN', 16, 'USER', DATEADD(MINUTE,-31062,@Now), DATEADD(MINUTE,-30545,@Now)),
(10062, N'Đậu Hũ Nhồi Sốt Me', 10001, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Đậu Hũ Nhồi Sốt Me – bản giữ cân', N'Đậu Hũ Nhồi Sốt Me – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-061), phù hợp người giữ cân, khoảng 1009.6 kcal mỗi khẩu phần (2 khẩu phần).', N'1. làm đậu hũ nhồi sốt me
2. Nấm mèo, nấm hương ngâm nở, cắt sợi. Bún tàu ngâm mềm, cắt khúc ngắn. Hành boaro cắt nhỏ. Ngò rí cắt nhỏ. Ơt sừng cắt khúc. Đậu hũ cắt đôi, lấy bớt ruột bên trong để riêng. Trộn đều ruột đậu hũ với nấm mèo, nấm hương, bún tàu, ngò rí, 2M bột năng, nêm 2/3m muối, ½ m bột ngọt và ít tiêu. Nhồi nhân vào từng miếng đậu hũ.
3. Phi thơm hành boaro, thêm 1 chén nước và 1 chén nước me, nêm 2/3m muối, 2/3 chén đường, 1m nước tương Lisa, đun sôi sau đó cho đậu hũ vào rim nhỏ lửa đến khi nước me hơi sánh lại là được.
4. Cho đậu hũ ra dĩa, rắc thêm đậu phộng rang, ớt băm và lá boaro. Dùng nóng với cơm trắng. Hướng dẫn cách làm món đậu hũ nhồi xốt me thơm ngon hấp dẫn tốt cho sức khỏe cả nhà phần 2 Hướng dẫn cách làm món đậu hũ nhồi xốt me thơm ngon hấp dẫn tốt cho sức khỏe cả nhà mà chúng tôi vừa trình bày trên đây chắc chắn sẽ giúp các chị em nội trợ thực hiện thành công món này! Chúc các bạn ngon miệng!
5. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 25, 20, 2, 1009.6, 'HARD', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790923/veggielife/seed60/cookbook500/c500_061_dau_hu_nhoi_sot_me.jpg', 'APPROVED_ACTIVE', 76, 'SYSTEM', DATEADD(MINUTE,-70501,@Now), DATEADD(MINUTE,-68846,@Now)),
(10063, N'Đậu Hũ Nhồi Sốt Me', 10001, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Đậu Hũ Nhồi Sốt Me – bản tăng cân (không cay)', N'Đậu Hũ Nhồi Sốt Me – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-061), phù hợp người tăng cân, khoảng 836.1 kcal mỗi khẩu phần (2 khẩu phần).', N'1. làm đậu hũ nhồi sốt me
2. Nấm mèo, nấm hương ngâm nở, cắt sợi. Bún tàu ngâm mềm, cắt khúc ngắn. Hành boaro cắt nhỏ. Ngò rí cắt nhỏ. Ơt sừng cắt khúc. Đậu hũ cắt đôi, lấy bớt ruột bên trong để riêng. Trộn đều ruột đậu hũ với nấm mèo, nấm hương, bún tàu, ngò rí, 2M bột năng, nêm 2/3m muối, ½ m bột ngọt và ít tiêu. Nhồi nhân vào từng miếng đậu hũ.
3. Phi thơm hành boaro, thêm 1 chén nước và 1 chén nước me, nêm 2/3m muối, 2/3 chén đường, 1m nước tương Lisa, đun sôi sau đó cho đậu hũ vào rim nhỏ lửa đến khi nước me hơi sánh lại là được.
4. Cho đậu hũ ra dĩa, rắc thêm đậu phộng rang, ớt băm và lá boaro. Dùng nóng với cơm trắng. Hướng dẫn cách làm món đậu hũ nhồi xốt me thơm ngon hấp dẫn tốt cho sức khỏe cả nhà phần 2 Hướng dẫn cách làm món đậu hũ nhồi xốt me thơm ngon hấp dẫn tốt cho sức khỏe cả nhà mà chúng tôi vừa trình bày trên đây chắc chắn sẽ giúp các chị em nội trợ thực hiện thành công món này! Chúc các bạn ngon miệng!
5. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang. Bản không cay: bỏ ớt, tương ớt và các gia vị cay khi nấu.', 25, 20, 2, 836.1, 'HARD', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790923/veggielife/seed60/cookbook500/c500_061_dau_hu_nhoi_sot_me.jpg', 'APPROVED_ACTIVE', 1002, 'SYSTEM', DATEADD(MINUTE,-75700,@Now), DATEADD(MINUTE,-75439,@Now)),
(10064, N'Đậu Hũ Nhồi Sốt Me', 10055, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Đậu Hũ Nhồi Sốt Me – bản tăng cân', N'Đậu Hũ Nhồi Sốt Me – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-061), phù hợp người tăng cân, khoảng 838.1 kcal mỗi khẩu phần (2 khẩu phần).', N'1. làm đậu hũ nhồi sốt me
2. Nấm mèo, nấm hương ngâm nở, cắt sợi. Bún tàu ngâm mềm, cắt khúc ngắn. Hành boaro cắt nhỏ. Ngò rí cắt nhỏ. Ơt sừng cắt khúc. Đậu hũ cắt đôi, lấy bớt ruột bên trong để riêng. Trộn đều ruột đậu hũ với nấm mèo, nấm hương, bún tàu, ngò rí, 2M bột năng, nêm 2/3m muối, ½ m bột ngọt và ít tiêu. Nhồi nhân vào từng miếng đậu hũ.
3. Phi thơm hành boaro, thêm 1 chén nước và 1 chén nước me, nêm 2/3m muối, 2/3 chén đường, 1m nước tương Lisa, đun sôi sau đó cho đậu hũ vào rim nhỏ lửa đến khi nước me hơi sánh lại là được.
4. Cho đậu hũ ra dĩa, rắc thêm đậu phộng rang, ớt băm và lá boaro. Dùng nóng với cơm trắng. Hướng dẫn cách làm món đậu hũ nhồi xốt me thơm ngon hấp dẫn tốt cho sức khỏe cả nhà phần 2 Hướng dẫn cách làm món đậu hũ nhồi xốt me thơm ngon hấp dẫn tốt cho sức khỏe cả nhà mà chúng tôi vừa trình bày trên đây chắc chắn sẽ giúp các chị em nội trợ thực hiện thành công món này! Chúc các bạn ngon miệng!
5. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 25, 20, 2, 838.1, 'HARD', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790923/veggielife/seed60/cookbook500/c500_061_dau_hu_nhoi_sot_me.jpg', 'PENDING_REVIEW', 11, 'USER', DATEADD(MINUTE,-10645,@Now), DATEADD(MINUTE,-9702,@Now)),
(10065, N'Đậu Hủ Nhồi Chay', 10031, N'Món Xào Chay Thanh Vị', 'BUDDHIST', N'Đậu Hủ Nhồi Chay – bản tăng cân', N'Đậu Hủ Nhồi Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-050), phù hợp người tăng cân, khoảng 715.6 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đậu hủ rọc đường nhỏ phía trên rồi dùng muỗng múc phần ruột trắng bên trong.
2. Nấm mèo & nấm đông cô ngâm nở cắt nhỏ.
3. Boa rô bằm nhỏ.
4. Tất cả trộn đều cùng hạt nêm, dầu hào chay & tiêu.
5. Sau đó nhồi lại vào phần vỏ đậu hủ rồi dùng lá chuối tước nhỏ trụng nước sôi cột lại rồi đem hấp 10 phút. (Không kiêng ngũ vị tân thì dùng bông hẹ để cột).
6. Phần nhân dư thì vo viên tròn rồi cho vào hấp cùng.
7. Cho 1 chén nước, dầu hào chay, hạt nêm, nước tương và đường cùng ít tương ớt bắt lên bếp nấu sôi.
8. Sau đó cho thêm 1 muỗng dầu mè vào.
9. Nêm gia vị cho vừa ăn rồi cho ít bột năng pha nước vào để phần sốt sánh lại rồi cho đậu hủ vào cho thấm sốt thì tắt bếp.
10. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 22, 10, 2, 715.6, 'HARD', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790900/veggielife/seed60/cookbook500/c500_050_dau_hu_nhoi_chay.jpg', 'APPROVED_ACTIVE', 341, 'USER', DATEADD(MINUTE,-155,@Now), DATEADD(MINUTE,-1,@Now)),
(10066, N'Đậu Hủ Nhồi Chay', 10089, N'Món Xào Chay Thanh Vị', 'BUDDHIST', N'Đậu Hủ Nhồi Chay – bản giữ cân', N'Đậu Hủ Nhồi Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-050), phù hợp người giữ cân, khoảng 541.0 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đậu hủ rọc đường nhỏ phía trên rồi dùng muỗng múc phần ruột trắng bên trong.
2. Nấm mèo & nấm đông cô ngâm nở cắt nhỏ.
3. Boa rô bằm nhỏ.
4. Tất cả trộn đều cùng hạt nêm, dầu hào chay & tiêu.
5. Sau đó nhồi lại vào phần vỏ đậu hủ rồi dùng lá chuối tước nhỏ trụng nước sôi cột lại rồi đem hấp 10 phút. (Không kiêng ngũ vị tân thì dùng bông hẹ để cột).
6. Phần nhân dư thì vo viên tròn rồi cho vào hấp cùng.
7. Cho 1 chén nước, dầu hào chay, hạt nêm, nước tương và đường cùng ít tương ớt bắt lên bếp nấu sôi.
8. Sau đó cho thêm 1 muỗng dầu mè vào.
9. Nêm gia vị cho vừa ăn rồi cho ít bột năng pha nước vào để phần sốt sánh lại rồi cho đậu hủ vào cho thấm sốt thì tắt bếp.
10. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 22, 10, 2, 541.0, 'HARD', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790900/veggielife/seed60/cookbook500/c500_050_dau_hu_nhoi_chay.jpg', 'DRAFT', 14, 'USER', DATEADD(MINUTE,-44512,@Now), CAST(NULL AS DATETIME2(7))),
(10067, N'Chả Giò Bắp Chay', 10029, N'Món Cuốn Chay', 'BUDDHIST', N'Chả Giò Bắp Chay – bản giữ cân', N'Chả Giò Bắp Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-105), phù hợp người giữ cân, khoảng 957.7 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Bắp lột vỏ, bỏ râu sau đó đem bào.
2. Tàu hủ ky tươi cắt nhỏ.
3. Nấm mèo ngâm nở, rửa sạch và cắt nhỏ.
4. Boa rô, ngò rí rửa sạch rồi cắt nhỏ.
5. Cho nấm mèo, tàu hủ ky, boa rô, ngò rí vào cùng bắp.
6. Nêm hạt nêm và tiêu.
7. Tất cả trộn đều cho vừa khẩu vị.
8. Không cho thêm đường vì bắp đã có vị ngọt.
9. Vỏ bánh tráng để cuốn dùng loại có màu vàng hay dùng bánh đa nem thì khi chiên sẽ có màu vàng đẹp.
10. Múc nhân cho lên vỏ bánh tráng và cuốn như bình thường hay cuốn chả giò.
11. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 16, 20, 2, 957.7, 'HARD', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791010/veggielife/seed60/cookbook500/c500_105_cha_gio_bap_chay.jpg', 'APPROVED_ACTIVE', 341, 'AI', DATEADD(MINUTE,-7411,@Now), DATEADD(MINUTE,-6368,@Now)),
(10068, N'Chả Giò Bắp Chay', 10001, N'Món Cuốn Chay', 'BUDDHIST', N'Chả Giò Bắp Chay – bản tăng cân', N'Chả Giò Bắp Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-105), phù hợp người tăng cân, khoảng 788.4 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Bắp lột vỏ, bỏ râu sau đó đem bào.
2. Tàu hủ ky tươi cắt nhỏ.
3. Nấm mèo ngâm nở, rửa sạch và cắt nhỏ.
4. Boa rô, ngò rí rửa sạch rồi cắt nhỏ.
5. Cho nấm mèo, tàu hủ ky, boa rô, ngò rí vào cùng bắp.
6. Nêm hạt nêm và tiêu.
7. Tất cả trộn đều cho vừa khẩu vị.
8. Không cho thêm đường vì bắp đã có vị ngọt.
9. Vỏ bánh tráng để cuốn dùng loại có màu vàng hay dùng bánh đa nem thì khi chiên sẽ có màu vàng đẹp.
10. Múc nhân cho lên vỏ bánh tráng và cuốn như bình thường hay cuốn chả giò.
11. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 16, 20, 2, 788.4, 'HARD', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791010/veggielife/seed60/cookbook500/c500_105_cha_gio_bap_chay.jpg', 'APPROVED_ACTIVE', 501, 'SYSTEM', DATEADD(MINUTE,-73696,@Now), DATEADD(MINUTE,-72760,@Now)),
(10069, N'Đậu Hũ Sốt Chay', 10001, N'Món Xào Chay Thanh Vị', 'BUDDHIST', N'Đậu Hũ Sốt Chay – bản giữ cân', N'Đậu Hũ Sốt Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-183), phù hợp người giữ cân, khoảng 86.0 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Nấm hương rửa sạch, ngâm nước cho nở.
2. Bước 2: Thái nhỏ hoặc băm nhỏ nấm.
3. Bước 3: Đậu phụ bỏ ra khỏi hộp, xắt miếng nhỏ vừa ăn.
4. Bước 4: Bắc chảo lên bếp, cho dầu ăn vào và xào nấm hương chín.
5. Bước 5: Tiếp tục cho sa tế chay và ớt bột vào đảo đều, lấy 2 thìa canh nước ngâm nấm hương thêm vào chảo, để sôi, nêm nếm muối, đường cho vừa miệng.
6. Bước 6: Cho đậu non vào chảo, thi thoảng lắc nhẹ chảo để đậu ngấm gia vị, không đảo bằng đũa để tránh miếng đậu bị nát.
7. Bước 7: Khi đậu non sôi, ngấm đủ gia vị thì tắt bếp, dùng nóng sẽ ngon hơn.
8. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 16, 20, 2, 86.0, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791177/veggielife/seed60/cookbook500/c500_183_dau_hu_sot_chay.jpg', 'APPROVED_ACTIVE', 96, 'SYSTEM', DATEADD(MINUTE,-80681,@Now), DATEADD(MINUTE,-79816,@Now)),
(10070, N'Đậu Hũ Sốt Chay', 10001, N'Món Xào Chay Thanh Vị', 'BUDDHIST', N'Đậu Hũ Sốt Chay – bản giảm cân', N'Đậu Hũ Sốt Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-183), phù hợp người giảm cân, khoảng 342.1 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Nấm hương rửa sạch, ngâm nước cho nở.
2. Bước 2: Thái nhỏ hoặc băm nhỏ nấm.
3. Bước 3: Đậu phụ bỏ ra khỏi hộp, xắt miếng nhỏ vừa ăn.
4. Bước 4: Bắc chảo lên bếp, cho dầu ăn vào và xào nấm hương chín.
5. Bước 5: Tiếp tục cho sa tế chay và ớt bột vào đảo đều, lấy 2 thìa canh nước ngâm nấm hương thêm vào chảo, để sôi, nêm nếm muối, đường cho vừa miệng.
6. Bước 6: Cho đậu non vào chảo, thi thoảng lắc nhẹ chảo để đậu ngấm gia vị, không đảo bằng đũa để tránh miếng đậu bị nát.
7. Bước 7: Khi đậu non sôi, ngấm đủ gia vị thì tắt bếp, dùng nóng sẽ ngon hơn.
8. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 16, 20, 2, 342.1, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791177/veggielife/seed60/cookbook500/c500_183_dau_hu_sot_chay.jpg', 'APPROVED_ACTIVE', 746, 'SYSTEM', DATEADD(MINUTE,-76673,@Now), DATEADD(MINUTE,-74265,@Now)),
(10071, N'Đậu Hũ Sốt Chay', 10001, N'Món Xào Chay Thanh Vị', 'BUDDHIST', N'Đậu Hũ Sốt Chay – bản tăng cân (không cay)', N'Đậu Hũ Sốt Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-183), phù hợp người tăng cân, khoảng 775.3 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Nấm hương rửa sạch, ngâm nước cho nở.
2. Bước 2: Thái nhỏ hoặc băm nhỏ nấm.
3. Bước 3: Đậu phụ bỏ ra khỏi hộp, xắt miếng nhỏ vừa ăn.
4. Bước 4: Bắc chảo lên bếp, cho dầu ăn vào và xào nấm hương chín.
5. Bước 5: Tiếp tục cho sa tế chay và ớt bột vào đảo đều, lấy 2 thìa canh nước ngâm nấm hương thêm vào chảo, để sôi, nêm nếm muối, đường cho vừa miệng.
6. Bước 6: Cho đậu non vào chảo, thi thoảng lắc nhẹ chảo để đậu ngấm gia vị, không đảo bằng đũa để tránh miếng đậu bị nát.
7. Bước 7: Khi đậu non sôi, ngấm đủ gia vị thì tắt bếp, dùng nóng sẽ ngon hơn.
8. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang. Bản không cay: bỏ ớt, tương ớt và các gia vị cay khi nấu.', 16, 20, 2, 775.3, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791177/veggielife/seed60/cookbook500/c500_183_dau_hu_sot_chay.jpg', 'APPROVED_ACTIVE', 730, 'SYSTEM', DATEADD(MINUTE,-71558,@Now), DATEADD(MINUTE,-69893,@Now)),
(10072, N'Rau Củ Luộc Chấm Nấm Đông Cô Kho Quẹt Chay', 10066, N'Món Hấp & Luộc Dưỡng Sinh', 'BUDDHIST', N'Rau Củ Luộc Chấm Nấm Đông Cô Kho Quẹt Chay – bản giữ cân', N'Rau Củ Luộc Chấm Nấm Đông Cô Kho Quẹt Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-051), phù hợp người giữ cân, khoảng 84.6 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Nấm đông cô ngâm nở cắt nhỏ.
2. Boa rô cắt nhỏ.
3. Phi thơm boa rô, cho nấm đông cô vào xào.
4. Cho nước mắm chay, đường và tiêu vào cho vừa khẩu vị.
5. Vì đây là món chấm nên sẽ có vị hơi mặn tí.
6. Đậu bắp + đậu cô ve + bầu luộc chín.
7. Khi ăn múc chén nhỏ nấm đông cô kho kèm rau ăn cùng cơm trắng.
8. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 16, 20, 2, 84.6, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790902/veggielife/seed60/cookbook500/c500_051_rau_cu_luoc_cham_nam_dong_co_kho_quet_chay.jpg', 'DRAFT', 12, 'USER', DATEADD(MINUTE,-3675,@Now), CAST(NULL AS DATETIME2(7))),
(10073, N'Rau Củ Luộc Chấm Nấm Đông Cô Kho Quẹt Chay', 10105, N'Món Hấp & Luộc Dưỡng Sinh', 'BUDDHIST', N'Rau Củ Luộc Chấm Nấm Đông Cô Kho Quẹt Chay – bản tăng cân', N'Rau Củ Luộc Chấm Nấm Đông Cô Kho Quẹt Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-051), phù hợp người tăng cân, khoảng 768.3 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Nấm đông cô ngâm nở cắt nhỏ.
2. Boa rô cắt nhỏ.
3. Phi thơm boa rô, cho nấm đông cô vào xào.
4. Cho nước mắm chay, đường và tiêu vào cho vừa khẩu vị.
5. Vì đây là món chấm nên sẽ có vị hơi mặn tí.
6. Đậu bắp + đậu cô ve + bầu luộc chín.
7. Khi ăn múc chén nhỏ nấm đông cô kho kèm rau ăn cùng cơm trắng.
8. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 16, 20, 2, 768.3, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790902/veggielife/seed60/cookbook500/c500_051_rau_cu_luoc_cham_nam_dong_co_kho_quet_chay.jpg', 'DRAFT', 15, 'AI', DATEADD(MINUTE,-16748,@Now), CAST(NULL AS DATETIME2(7))),
(10074, N'Rau Củ Luộc Chấm Nấm Đông Cô Kho Quẹt Chay', 10001, N'Món Hấp & Luộc Dưỡng Sinh', 'BUDDHIST', N'Rau Củ Luộc Chấm Nấm Đông Cô Kho Quẹt Chay – bản giảm cân', N'Rau Củ Luộc Chấm Nấm Đông Cô Kho Quẹt Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-051), phù hợp người giảm cân, khoảng 349.2 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Nấm đông cô ngâm nở cắt nhỏ.
2. Boa rô cắt nhỏ.
3. Phi thơm boa rô, cho nấm đông cô vào xào.
4. Cho nước mắm chay, đường và tiêu vào cho vừa khẩu vị.
5. Vì đây là món chấm nên sẽ có vị hơi mặn tí.
6. Đậu bắp + đậu cô ve + bầu luộc chín.
7. Khi ăn múc chén nhỏ nấm đông cô kho kèm rau ăn cùng cơm trắng.
8. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 16, 20, 2, 349.2, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790902/veggielife/seed60/cookbook500/c500_051_rau_cu_luoc_cham_nam_dong_co_kho_quet_chay.jpg', 'APPROVED_ACTIVE', 436, 'SYSTEM', DATEADD(MINUTE,-82610,@Now), DATEADD(MINUTE,-81039,@Now)),
(10075, N'Váng Đậu Xào Chay', 10001, N'Món Xào Chay Thanh Vị', 'BUDDHIST', N'Váng Đậu Xào Chay – bản giữ cân', N'Váng Đậu Xào Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-030), phù hợp người giữ cân, khoảng 596.2 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Sơ chế các nguyên liệu Tàu hũ ky chúng ta mang ngâm nước cho nở mềm, sau đó cắt khúc vừa ăn. Mang ngâm mục nhĩ và nấm hương cho nở ra sau đó rửa thật sạch. Mục nhĩ chúng ra cắt sợi, còn nấm hương thì để nguyên cái Su hào và cà rốt gọt sạch vỏ , rửa sạch thái miếng mỏng, chúng ta có thể dùng dao tạo hình để cắt cho đẹp Hướng dẫn Cắt su hào và cà rốt cho đẹp mắt Làm sạch đậu Hà Lan, bóc sạch 2 đường xơ bên mép quả. Rau mùi cũng làm sạch. Chúng ta ngâm đậu và rau mùi vào nước muối pha loãng khoảng 15 phút. Sau đó vớt ra xả lại nước sạch và để ráo.
2. Chúng ta cho chảo lên bếp, cho chút dầu ăn đun nóng Đầu tiên ta cho đậu hà lan vào đảo qua, sau đó cho cà rốt và su hào , tàu hũ ky vào xào, sau đó nêm chút muối, vẫn đảo đều tay để rau chín tới. Tiếp tục cho mục nhĩ và nấm hương vào đảo khoảng 2 phút, thì cho rau mùi vào đảo sơ qua. Cho nấm hương và mục nhĩ vào đảo cùng Nêm thêm mì chính và hạt tiêu vào cho vừa miệng sau đó tắt bếp và bày ra đĩa là các bạn đã hoàn thành xong.
3. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 19, 17, 2, 596.2, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790859/veggielife/seed60/cookbook500/c500_030_vang_dau_xao_chay.jpg', 'APPROVED_ACTIVE', 550, 'SYSTEM', DATEADD(MINUTE,-66316,@Now), DATEADD(MINUTE,-64774,@Now)),
(10076, N'Váng Đậu Xào Chay', 10111, N'Món Xào Chay Thanh Vị', 'BUDDHIST', N'Váng Đậu Xào Chay – bản tăng cân', N'Váng Đậu Xào Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-030), phù hợp người tăng cân, khoảng 780.6 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Sơ chế các nguyên liệu Tàu hũ ky chúng ta mang ngâm nước cho nở mềm, sau đó cắt khúc vừa ăn. Mang ngâm mục nhĩ và nấm hương cho nở ra sau đó rửa thật sạch. Mục nhĩ chúng ra cắt sợi, còn nấm hương thì để nguyên cái Su hào và cà rốt gọt sạch vỏ , rửa sạch thái miếng mỏng, chúng ta có thể dùng dao tạo hình để cắt cho đẹp Hướng dẫn Cắt su hào và cà rốt cho đẹp mắt Làm sạch đậu Hà Lan, bóc sạch 2 đường xơ bên mép quả. Rau mùi cũng làm sạch. Chúng ta ngâm đậu và rau mùi vào nước muối pha loãng khoảng 15 phút. Sau đó vớt ra xả lại nước sạch và để ráo.
2. Chúng ta cho chảo lên bếp, cho chút dầu ăn đun nóng Đầu tiên ta cho đậu hà lan vào đảo qua, sau đó cho cà rốt và su hào , tàu hũ ky vào xào, sau đó nêm chút muối, vẫn đảo đều tay để rau chín tới. Tiếp tục cho mục nhĩ và nấm hương vào đảo khoảng 2 phút, thì cho rau mùi vào đảo sơ qua. Cho nấm hương và mục nhĩ vào đảo cùng Nêm thêm mì chính và hạt tiêu vào cho vừa miệng sau đó tắt bếp và bày ra đĩa là các bạn đã hoàn thành xong.
3. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 19, 17, 2, 780.6, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790859/veggielife/seed60/cookbook500/c500_030_vang_dau_xao_chay.jpg', 'DRAFT', 25, 'USER', DATEADD(MINUTE,-25630,@Now), CAST(NULL AS DATETIME2(7))),
(10077, N'Váng Đậu Xào Chay', 10001, N'Món Xào Chay Thanh Vị', 'BUDDHIST', N'Váng Đậu Xào Chay – bản giảm cân', N'Váng Đậu Xào Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-030), phù hợp người giảm cân, khoảng 377.3 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Sơ chế các nguyên liệu Tàu hũ ky chúng ta mang ngâm nước cho nở mềm, sau đó cắt khúc vừa ăn. Mang ngâm mục nhĩ và nấm hương cho nở ra sau đó rửa thật sạch. Mục nhĩ chúng ra cắt sợi, còn nấm hương thì để nguyên cái Su hào và cà rốt gọt sạch vỏ , rửa sạch thái miếng mỏng, chúng ta có thể dùng dao tạo hình để cắt cho đẹp Hướng dẫn Cắt su hào và cà rốt cho đẹp mắt Làm sạch đậu Hà Lan, bóc sạch 2 đường xơ bên mép quả. Rau mùi cũng làm sạch. Chúng ta ngâm đậu và rau mùi vào nước muối pha loãng khoảng 15 phút. Sau đó vớt ra xả lại nước sạch và để ráo.
2. Chúng ta cho chảo lên bếp, cho chút dầu ăn đun nóng Đầu tiên ta cho đậu hà lan vào đảo qua, sau đó cho cà rốt và su hào , tàu hũ ky vào xào, sau đó nêm chút muối, vẫn đảo đều tay để rau chín tới. Tiếp tục cho mục nhĩ và nấm hương vào đảo khoảng 2 phút, thì cho rau mùi vào đảo sơ qua. Cho nấm hương và mục nhĩ vào đảo cùng Nêm thêm mì chính và hạt tiêu vào cho vừa miệng sau đó tắt bếp và bày ra đĩa là các bạn đã hoàn thành xong.
3. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 19, 17, 2, 377.3, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790859/veggielife/seed60/cookbook500/c500_030_vang_dau_xao_chay.jpg', 'APPROVED_ACTIVE', 923, 'SYSTEM', DATEADD(MINUTE,-65934,@Now), DATEADD(MINUTE,-65693,@Now)),
(10078, N'Bún Chay Huế', 10101, N'Món Mì, Bún & Phở Chay', 'VEGAN', N'Bún Chay Huế – bản giảm cân', N'Bún Chay Huế – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-013), phù hợp người giảm cân, khoảng 347.7 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Bước 1: Rau củ quả rửa sạch, cắt khúc. Lá boa rô cắt khúc. Cho rau củ quả vào nồi, rồi thêm nước, ít muối vào hầm cho ra nước ngọt, hầm đến khi hỗn hợp mềm. Sau đó bỏ lấy nước dùng không lấy cái (chỉ sử dụng lại củ cải).
2. Bước 2: Cho nồi nước dùng lên bếp (trong nồi còn củ cải). Thêm dứa cắt miếng, sả đập dập, ít cọng đầu boa-rô cắt khúc vào nấu với lửa nhỏ cho thơm mềm rồi nêm nếm gia vị. Mình sử dụng gia vị bột nêm chay, bột ngọt, đường phèn (tuỳ khẩu vị) và ít màu dầu điều vào.
3. Bước 3: Đậu hũ cắt nhỏ, chiên vàng, khi ăn cho vào nồi nước dùng.
4. Bước 4: Làm sốt nấm: Cho ít dầu ăn vào nồi, đun nóng. Khi dầu nóng, cho đầu boa-rô vào phi thơm rồi cho cà chua băm nhỏ vào xào, sau đó cho nấm rơm và ít nước vào. Nêm gia vị hơi đậm đà, đun liu riu cho thấm đến khi nước còn sền sệt. Múc sốt nấm ra tô.
5. Chuẩn bị bún ngon (dùng bún cọng lớn) cùng với các nguyên liệu ăn kèm như sa tế, rau thơm (bắp chuối, rau muống bào, giá, rau thơm). Bún chần nóng, cho vào tô, vài miếng dứa, đậu hũ, củ cải trắng rồi chan nước dùng lên tô bún. Sau đó mới cho sốt nấm lên trên cùng rau ngò gai, boa-rô cắt nhỏ lên.
6. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 22, 20, 2, 347.7, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790824/veggielife/seed60/cookbook500/c500_013_bun_chay_hue.jpg', 'APPROVED_ACTIVE', 135, 'USER', DATEADD(MINUTE,-5036,@Now), DATEADD(MINUTE,-3622,@Now)),
(10079, N'Bún Chay Huế', 10001, N'Món Mì, Bún & Phở Chay', 'VEGAN', N'Bún Chay Huế – bản giữ cân', N'Bún Chay Huế – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-013), phù hợp người giữ cân, khoảng 498.7 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Bước 1: Rau củ quả rửa sạch, cắt khúc. Lá boa rô cắt khúc. Cho rau củ quả vào nồi, rồi thêm nước, ít muối vào hầm cho ra nước ngọt, hầm đến khi hỗn hợp mềm. Sau đó bỏ lấy nước dùng không lấy cái (chỉ sử dụng lại củ cải).
2. Bước 2: Cho nồi nước dùng lên bếp (trong nồi còn củ cải). Thêm dứa cắt miếng, sả đập dập, ít cọng đầu boa-rô cắt khúc vào nấu với lửa nhỏ cho thơm mềm rồi nêm nếm gia vị. Mình sử dụng gia vị bột nêm chay, bột ngọt, đường phèn (tuỳ khẩu vị) và ít màu dầu điều vào.
3. Bước 3: Đậu hũ cắt nhỏ, chiên vàng, khi ăn cho vào nồi nước dùng.
4. Bước 4: Làm sốt nấm: Cho ít dầu ăn vào nồi, đun nóng. Khi dầu nóng, cho đầu boa-rô vào phi thơm rồi cho cà chua băm nhỏ vào xào, sau đó cho nấm rơm và ít nước vào. Nêm gia vị hơi đậm đà, đun liu riu cho thấm đến khi nước còn sền sệt. Múc sốt nấm ra tô.
5. Chuẩn bị bún ngon (dùng bún cọng lớn) cùng với các nguyên liệu ăn kèm như sa tế, rau thơm (bắp chuối, rau muống bào, giá, rau thơm). Bún chần nóng, cho vào tô, vài miếng dứa, đậu hũ, củ cải trắng rồi chan nước dùng lên tô bún. Sau đó mới cho sốt nấm lên trên cùng rau ngò gai, boa-rô cắt nhỏ lên.
6. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 22, 20, 2, 498.7, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790824/veggielife/seed60/cookbook500/c500_013_bun_chay_hue.jpg', 'APPROVED_ACTIVE', 218, 'SYSTEM', DATEADD(MINUTE,-80373,@Now), DATEADD(MINUTE,-78122,@Now)),
(10080, N'Rau Luộc Kho Quẹt Chay', 10001, N'Món Hấp & Luộc Dưỡng Sinh', 'VEGAN', N'Rau Luộc Kho Quẹt Chay – bản giữ cân', N'Rau Luộc Kho Quẹt Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-062), phù hợp người giữ cân, khoảng 124.9 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đậu hủ cắt nhỏ bằng đầu ngón tay, cho vào chảo dầu nóng, chiên vàng Nấm rơm cắt hạt lựu nhỏ Để kho quẹt ngon bạn nên dùng nồi đất để tạo nhiệt độ nóng đều.
2. Cho 2 muỗng dầu ăn vào nồi đất đun nóng, cho boaro phi thơm, nấm rơm, nước tương vào, cho đường, bột nêm.
3. Vì món kho quẹt nên ngọt mặn bạn nên cho nhiều đường tùy vào sở thích.
4. Để tạo độ sánh cho món kho quẹt chay, bạn cho một ít bột năng hòa vào nước loãng, cho 1 muỗng nước bột năng vào kho quẹt, để sánh lại.
5. Bạn cho đầu hủ chiên vào, đảo đều , cho rau mùi vào tắt bếp.
6. Để trang trí bạn có thể cho vài lát ớt sừng.
7. Kho quẹt chay sẽ đi ăn kèm với các loại rau củ luộc như đậu que, cà rốt, củ cải, su su, su hào, bí xanh, bầu, bắp cải…
8. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 13, 20, 2, 124.9, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790925/veggielife/seed60/cookbook500/c500_062_rau_luoc_kho_quet_chay.jpg', 'APPROVED_ACTIVE', 372, 'SYSTEM', DATEADD(MINUTE,-66427,@Now), DATEADD(MINUTE,-66071,@Now)),
(10081, N'Rau Luộc Kho Quẹt Chay', 10001, N'Món Hấp & Luộc Dưỡng Sinh', 'VEGAN', N'Rau Luộc Kho Quẹt Chay – bản giảm cân', N'Rau Luộc Kho Quẹt Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-062), phù hợp người giảm cân, khoảng 358.2 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đậu hủ cắt nhỏ bằng đầu ngón tay, cho vào chảo dầu nóng, chiên vàng Nấm rơm cắt hạt lựu nhỏ Để kho quẹt ngon bạn nên dùng nồi đất để tạo nhiệt độ nóng đều.
2. Cho 2 muỗng dầu ăn vào nồi đất đun nóng, cho boaro phi thơm, nấm rơm, nước tương vào, cho đường, bột nêm.
3. Vì món kho quẹt nên ngọt mặn bạn nên cho nhiều đường tùy vào sở thích.
4. Để tạo độ sánh cho món kho quẹt chay, bạn cho một ít bột năng hòa vào nước loãng, cho 1 muỗng nước bột năng vào kho quẹt, để sánh lại.
5. Bạn cho đầu hủ chiên vào, đảo đều , cho rau mùi vào tắt bếp.
6. Để trang trí bạn có thể cho vài lát ớt sừng.
7. Kho quẹt chay sẽ đi ăn kèm với các loại rau củ luộc như đậu que, cà rốt, củ cải, su su, su hào, bí xanh, bầu, bắp cải…
8. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 13, 20, 2, 358.2, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790925/veggielife/seed60/cookbook500/c500_062_rau_luoc_kho_quet_chay.jpg', 'APPROVED_ACTIVE', 1471, 'SYSTEM', DATEADD(MINUTE,-82982,@Now), DATEADD(MINUTE,-81304,@Now)),
(10082, N'Rau Luộc Kho Quẹt Chay', 10001, N'Món Hấp & Luộc Dưỡng Sinh', 'VEGAN', N'Rau Luộc Kho Quẹt Chay – bản tăng cân', N'Rau Luộc Kho Quẹt Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-062), phù hợp người tăng cân, khoảng 789.1 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đậu hủ cắt nhỏ bằng đầu ngón tay, cho vào chảo dầu nóng, chiên vàng Nấm rơm cắt hạt lựu nhỏ Để kho quẹt ngon bạn nên dùng nồi đất để tạo nhiệt độ nóng đều.
2. Cho 2 muỗng dầu ăn vào nồi đất đun nóng, cho boaro phi thơm, nấm rơm, nước tương vào, cho đường, bột nêm.
3. Vì món kho quẹt nên ngọt mặn bạn nên cho nhiều đường tùy vào sở thích.
4. Để tạo độ sánh cho món kho quẹt chay, bạn cho một ít bột năng hòa vào nước loãng, cho 1 muỗng nước bột năng vào kho quẹt, để sánh lại.
5. Bạn cho đầu hủ chiên vào, đảo đều , cho rau mùi vào tắt bếp.
6. Để trang trí bạn có thể cho vài lát ớt sừng.
7. Kho quẹt chay sẽ đi ăn kèm với các loại rau củ luộc như đậu que, cà rốt, củ cải, su su, su hào, bí xanh, bầu, bắp cải…
8. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 13, 20, 2, 789.1, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790925/veggielife/seed60/cookbook500/c500_062_rau_luoc_kho_quet_chay.jpg', 'APPROVED_ACTIVE', 146, 'SYSTEM', DATEADD(MINUTE,-79210,@Now), DATEADD(MINUTE,-77608,@Now)),
(10083, N'Mì Quảng Chay', 10001, N'Món Mì, Bún & Phở Chay', 'VEGAN', N'Mì Quảng Chay – bản tăng cân', N'Mì Quảng Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-031), phù hợp người tăng cân, khoảng 660.4 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Gọt vỏ các loại rau củ cắt miếng vuông cho vào nồi nước hầm với ít muối và đường phèn. Cho tất cả vào nồi ủ hầm lấy nước dùng.
2. Mì căn cắt xéo miếng bản lớn và dày ( để khi rim kg bị nát ) ướp mì căn với hạt nêm + đường + nước tương + bột nghệ + tiêu để qua 1 bên cho thấm gia vị và đi chuẩn bị các thứ khác.
3. Đậu phộng sống cho vào máy xay , xay nhuyễn.
4. Cà chua cho vào máy xay or bằm tuỳ thích .
5. Nấm mèo ngâm mềm , tai nấm nào lớn thì cắt đôi còn không thì để nguyên.
6. Đậu hũ cắt miếng vuông chiên vàng
7. Ham cắt lát chiên sơ 2 mặt, hoà nước tương + đường hạt nêm cho tất cả vào chảo ham đã chiên và rim cho ham thấm gia vị .
8. Bắt chảo nóng phi dầu phộng cho mì căn đã ướp vào xào cho thơm , cho thêm tí nước dừa ( or nước lạnh ) vào và rim cho đến khi mì căn khô lại và nêm nếm cho hơi đậm 1 chút.
9. Bắt chảo nóng cho dầu phộng hơi nhiều tí tiếp đến cho cà chua vào xào cùng tí muối và đường để tạo màu. Kế đến cho đậu phộng sống đã xay vào nấu cùng với nấm mèo và ham chiên , đậu chiên. Cho nước hầm rau củ vào nấu cùng, sao cho lượng nước vừa đủ hơi sánh lại và nêm nếm lại cho nồi nước lèo hơi đậm đà 1 chút . Về phần nhân cơ bản là như vậy nhưng mọi người có thể thay đổi nguyên liệu sao cho dễ kiếm và phù hợp với khẩu vị nhà mình là được.
10. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 28, 20, 2, 660.4, 'HARD', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790861/veggielife/seed60/cookbook500/c500_031_mi_quang_chay.jpg', 'APPROVED_ACTIVE', 1619, 'SYSTEM', DATEADD(MINUTE,-68696,@Now), DATEADD(MINUTE,-67331,@Now)),
(10084, N'Mì Quảng Chay', 10001, N'Món Mì, Bún & Phở Chay', 'VEGAN', N'Mì Quảng Chay – bản giữ cân', N'Mì Quảng Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-031), phù hợp người giữ cân, khoảng 538.2 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Gọt vỏ các loại rau củ cắt miếng vuông cho vào nồi nước hầm với ít muối và đường phèn. Cho tất cả vào nồi ủ hầm lấy nước dùng.
2. Mì căn cắt xéo miếng bản lớn và dày ( để khi rim kg bị nát ) ướp mì căn với hạt nêm + đường + nước tương + bột nghệ + tiêu để qua 1 bên cho thấm gia vị và đi chuẩn bị các thứ khác.
3. Đậu phộng sống cho vào máy xay , xay nhuyễn.
4. Cà chua cho vào máy xay or bằm tuỳ thích .
5. Nấm mèo ngâm mềm , tai nấm nào lớn thì cắt đôi còn không thì để nguyên.
6. Đậu hũ cắt miếng vuông chiên vàng
7. Ham cắt lát chiên sơ 2 mặt, hoà nước tương + đường hạt nêm cho tất cả vào chảo ham đã chiên và rim cho ham thấm gia vị .
8. Bắt chảo nóng phi dầu phộng cho mì căn đã ướp vào xào cho thơm , cho thêm tí nước dừa ( or nước lạnh ) vào và rim cho đến khi mì căn khô lại và nêm nếm cho hơi đậm 1 chút.
9. Bắt chảo nóng cho dầu phộng hơi nhiều tí tiếp đến cho cà chua vào xào cùng tí muối và đường để tạo màu. Kế đến cho đậu phộng sống đã xay vào nấu cùng với nấm mèo và ham chiên , đậu chiên. Cho nước hầm rau củ vào nấu cùng, sao cho lượng nước vừa đủ hơi sánh lại và nêm nếm lại cho nồi nước lèo hơi đậm đà 1 chút . Về phần nhân cơ bản là như vậy nhưng mọi người có thể thay đổi nguyên liệu sao cho dễ kiếm và phù hợp với khẩu vị nhà mình là được.
10. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 28, 20, 2, 538.2, 'HARD', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790861/veggielife/seed60/cookbook500/c500_031_mi_quang_chay.jpg', 'APPROVED_ACTIVE', 306, 'SYSTEM', DATEADD(MINUTE,-66846,@Now), DATEADD(MINUTE,-64973,@Now)),
(10085, N'Cháo Thập Cẩm', 10001, N'Món Cơm & Xôi Chay', 'BUDDHIST', N'Cháo Thập Cẩm – bản tăng cân', N'Cháo Thập Cẩm – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-001), phù hợp người tăng cân, khoảng 808.0 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Gạo và đậu xanh vo sạch, nấu cháo với 1.5 lít nước.
2. Khoai lang, bí đỏ, khoai môn cao cắt hạt lựu.
3. Ngò rí cắt nhỏ.
4. Hành boaro cắt lát xéo.
5. Phi thơm hành boaro, xào sơ nấm linh chi nâu và nấm linh chi trắng.
6. Cháo nấu sắp chín, cho các loại củ vào, sau đó cho nấm linh chi đã xào vào.
7. Nêm nếm gia vị, nước cốt dừa cho vừa ăn.
8. Múc cháo ra tô, thêm ngò, dùng nóng.
9. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 19, 20, 2, 808.0, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790800/veggielife/seed60/cookbook500/c500_001_chao_thap_cam.jpg', 'APPROVED_ACTIVE', 21, 'SYSTEM', DATEADD(MINUTE,-78363,@Now), DATEADD(MINUTE,-76605,@Now)),
(10086, N'Cháo Thập Cẩm', 10105, N'Món Cơm & Xôi Chay', 'BUDDHIST', N'Cháo Thập Cẩm – bản giảm cân', N'Cháo Thập Cẩm – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-001), phù hợp người giảm cân, khoảng 377.8 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Gạo và đậu xanh vo sạch, nấu cháo với 1.5 lít nước.
2. Khoai lang, bí đỏ, khoai môn cao cắt hạt lựu.
3. Ngò rí cắt nhỏ.
4. Hành boaro cắt lát xéo.
5. Phi thơm hành boaro, xào sơ nấm linh chi nâu và nấm linh chi trắng.
6. Cháo nấu sắp chín, cho các loại củ vào, sau đó cho nấm linh chi đã xào vào.
7. Nêm nếm gia vị, nước cốt dừa cho vừa ăn.
8. Múc cháo ra tô, thêm ngò, dùng nóng.
9. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 19, 20, 2, 377.8, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790800/veggielife/seed60/cookbook500/c500_001_chao_thap_cam.jpg', 'PENDING_REVIEW', 0, 'USER', DATEADD(MINUTE,-24486,@Now), DATEADD(MINUTE,-22544,@Now)),
(10087, N'Cháo Thập Cẩm', 10001, N'Món Cơm & Xôi Chay', 'BUDDHIST', N'Cháo Thập Cẩm – bản giữ cân', N'Cháo Thập Cẩm – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-001), phù hợp người giữ cân, khoảng 531.2 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Gạo và đậu xanh vo sạch, nấu cháo với 1.5 lít nước.
2. Khoai lang, bí đỏ, khoai môn cao cắt hạt lựu.
3. Ngò rí cắt nhỏ.
4. Hành boaro cắt lát xéo.
5. Phi thơm hành boaro, xào sơ nấm linh chi nâu và nấm linh chi trắng.
6. Cháo nấu sắp chín, cho các loại củ vào, sau đó cho nấm linh chi đã xào vào.
7. Nêm nếm gia vị, nước cốt dừa cho vừa ăn.
8. Múc cháo ra tô, thêm ngò, dùng nóng.
9. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 19, 20, 2, 531.2, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790800/veggielife/seed60/cookbook500/c500_001_chao_thap_cam.jpg', 'APPROVED_ACTIVE', 1258, 'SYSTEM', DATEADD(MINUTE,-82443,@Now), DATEADD(MINUTE,-80531,@Now)),
(10088, N'Bún Gạo Xào Chay', 10001, N'Món Mì, Bún & Phở Chay', 'VEGAN', N'Bún Gạo Xào Chay – bản tăng cân', N'Bún Gạo Xào Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-188), phù hợp người tăng cân, khoảng 681.0 kcal mỗi khẩu phần (4 khẩu phần).', N'1. Cà rốt, đậu phụ và nấm mèo thái sợi. Hẹ, hành lá thái khúc
2. Cần tây thái lát. Hành tây thái theo múi cau.
3. Bún gạo ngâm với nước ấm khoảng 30 phút rồi vớt ra, để ráo.
4. Làm nóng dầu, phi thơm hành. Cho cà rốt, nấm mèo, cần tây, hành tây vào xào vài phút.
5. Tiếp đến cho đậu phụ vào. Nêm ít hạt nêm nấm, đường, muối và nước tương. Các nguyên liệu như hẹ, hành lá được cho vào xào chung rồi cho ra đĩa để riêng.
6. Sử dụng lại cái chảo đó, phi thơm dầu với tỏi băm rồi cho bún gạo vào đảo sơ. Nêm ít đường, muối, hạt nêm nấm, nước tương cùng ít nước lọc.
7. Đảo đều đến khi bún mềm thì cho hỗn hợp rau đã xào vào trộn đều. Tắt bếp, rắc ít tiêu nếu thích.
8. Món này dùng ngay lúc còn nóng, bạn có thể làm thêm chén nước tương tỏi ớt để ăn kèm.
9. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 22, 30, 4, 681.0, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791186/veggielife/seed60/cookbook500/c500_188_bun_gao_xao_chay.jpg', 'APPROVED_ACTIVE', 1179, 'SYSTEM', DATEADD(MINUTE,-78263,@Now), DATEADD(MINUTE,-75635,@Now)),
(10089, N'Bún Gạo Xào Chay', 10001, N'Món Mì, Bún & Phở Chay', 'VEGAN', N'Bún Gạo Xào Chay – bản giảm cân', N'Bún Gạo Xào Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-188), phù hợp người giảm cân, khoảng 372.9 kcal mỗi khẩu phần (4 khẩu phần).', N'1. Cà rốt, đậu phụ và nấm mèo thái sợi. Hẹ, hành lá thái khúc
2. Cần tây thái lát. Hành tây thái theo múi cau.
3. Bún gạo ngâm với nước ấm khoảng 30 phút rồi vớt ra, để ráo.
4. Làm nóng dầu, phi thơm hành. Cho cà rốt, nấm mèo, cần tây, hành tây vào xào vài phút.
5. Tiếp đến cho đậu phụ vào. Nêm ít hạt nêm nấm, đường, muối và nước tương. Các nguyên liệu như hẹ, hành lá được cho vào xào chung rồi cho ra đĩa để riêng.
6. Sử dụng lại cái chảo đó, phi thơm dầu với tỏi băm rồi cho bún gạo vào đảo sơ. Nêm ít đường, muối, hạt nêm nấm, nước tương cùng ít nước lọc.
7. Đảo đều đến khi bún mềm thì cho hỗn hợp rau đã xào vào trộn đều. Tắt bếp, rắc ít tiêu nếu thích.
8. Món này dùng ngay lúc còn nóng, bạn có thể làm thêm chén nước tương tỏi ớt để ăn kèm.
9. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 22, 30, 4, 372.9, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791186/veggielife/seed60/cookbook500/c500_188_bun_gao_xao_chay.jpg', 'APPROVED_ACTIVE', 792, 'SYSTEM', DATEADD(MINUTE,-83737,@Now), DATEADD(MINUTE,-82470,@Now)),
(10090, N'Đậu Phụ Cay Sốt Nấm', 10138, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Đậu Phụ Cay Sốt Nấm – bản giữ cân', N'Đậu Phụ Cay Sốt Nấm – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-117), phù hợp người giữ cân, khoảng 508.3 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Đậu phụ rửa sạch cắt miếng vuông mỏng. Bột ngô bỏ ra đĩa, gắp từng miếng đậu tẩm qua bột rồi cho vào chảo dầu nóng rán vàng, bột chỉ cần bám sơ quanh miếng đậu là được (Khi rán đậu chú ý để lửa nhỏ để tránh bột bị cháy). Đậu rán chín vàng hai mặt thì cho ra đĩa, để sang một bên.
2. Bước 2: Đặt chảo trở lại bếp, cho dầu và phi tỏi thơm. Cho hành thái nhỏ vào phi cùng, tiếp đến cho ớt thái lát, cà chua thái hạt lựu vào đun. Cho một chút nước nếu chảo quá khô và để tránh cà chua bị cháy.
3. Bước 3: Sau khi cà chua đã nhừ thành nước sốt thì cho nấm vào xào, nêm 1 thìa bột canh, ½ thìa đường.
4. Bước 4: Tiếp theo cho đậu rán vào chảo, thêm nửa chén nước, rồi mở lửa to đun sôi, nêm nếm lại bột canh cho vừa ăn. Vặn nhỏ lửa âm ỉ cho đậu ngấm gia vị.
5. Bước 5: Đun đến khi nước cạn bớt là được. Khi đun tránh dùng đũa đảo nhiều, đậu sẽ bị vỡ. Trước khi tắt bếp rắc chút hành xanh thái nhỏ. Xúc đậu ra bát to, rắc hạt tiêu lên trên là đã hoàn thành món ăn rồi!
6. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 19, 20, 2, 508.3, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791040/veggielife/seed60/cookbook500/c500_117_dau_phu_cay_sot_nam.jpg', 'PENDING_REVIEW', 1, 'USER', DATEADD(MINUTE,-17840,@Now), DATEADD(MINUTE,-17487,@Now)),
(10091, N'Đậu Phụ Cay Sốt Nấm', 10049, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Đậu Phụ Cay Sốt Nấm – bản tăng cân', N'Đậu Phụ Cay Sốt Nấm – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-117), phù hợp người tăng cân, khoảng 775.4 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Đậu phụ rửa sạch cắt miếng vuông mỏng. Bột ngô bỏ ra đĩa, gắp từng miếng đậu tẩm qua bột rồi cho vào chảo dầu nóng rán vàng, bột chỉ cần bám sơ quanh miếng đậu là được (Khi rán đậu chú ý để lửa nhỏ để tránh bột bị cháy). Đậu rán chín vàng hai mặt thì cho ra đĩa, để sang một bên.
2. Bước 2: Đặt chảo trở lại bếp, cho dầu và phi tỏi thơm. Cho hành thái nhỏ vào phi cùng, tiếp đến cho ớt thái lát, cà chua thái hạt lựu vào đun. Cho một chút nước nếu chảo quá khô và để tránh cà chua bị cháy.
3. Bước 3: Sau khi cà chua đã nhừ thành nước sốt thì cho nấm vào xào, nêm 1 thìa bột canh, ½ thìa đường.
4. Bước 4: Tiếp theo cho đậu rán vào chảo, thêm nửa chén nước, rồi mở lửa to đun sôi, nêm nếm lại bột canh cho vừa ăn. Vặn nhỏ lửa âm ỉ cho đậu ngấm gia vị.
5. Bước 5: Đun đến khi nước cạn bớt là được. Khi đun tránh dùng đũa đảo nhiều, đậu sẽ bị vỡ. Trước khi tắt bếp rắc chút hành xanh thái nhỏ. Xúc đậu ra bát to, rắc hạt tiêu lên trên là đã hoàn thành món ăn rồi!
6. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 19, 20, 2, 775.4, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791040/veggielife/seed60/cookbook500/c500_117_dau_phu_cay_sot_nam.jpg', 'APPROVED_ACTIVE', 187, 'USER', DATEADD(MINUTE,-12814,@Now), DATEADD(MINUTE,-12099,@Now)),
(10092, N'Ragu Chay', 10125, N'Món Kho Chay Đậm Đà', 'LACTO', N'Ragu Chay – bản giữ cân', N'Ragu Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-132), phù hợp người giữ cân, khoảng 620.8 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Khoai tây, cà rốt rửa sạch, gọt vỏ. Khoai tây cắt làm tư, cà rốt cắt khúc vừa ăn. Đậu phụ, mít non cắt miếng vuông khoảng 2cm. Đậu phụ đem chiên sơ cho vàng hai mặt. Hành boa-rô cắt nhuyễn.
2. Bước 2: Đun nóng nồi cỡ vừa, cho 2 muỗng dầu ăn. Khi dầu nóng, cho hành củ băm nhuyễn vào xào thơm. Lần lượt cho khoai tây, cà rốt vào xào chung, nêm vào ít muối, hạt nêm.
3. Bước 3 : Cho khoảng 3 chén nước lọc, sữa tươi (nếu không dùng sữa tươi thì có thể tăng thêm nước lọc hoặc cốt dừa) vào nồi. Đun cho hỗn hợp khoai gần chín, cho tiếp đậu phụ, mít luộc vào nấu vừa chín. Rưới phần nước cốt dừa và cho bột ragu, bột ớt, 1/2 muỗng cà phê hạt nêm, ít đường, nấu sôi, tắt bếp. Cho Ragu chay ra tô, trang trí ngò rí, tiêu. Dùng nóng với bánh mì, bún hoặc cơm trắng.
4. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 19, 20, 2, 620.8, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791078/veggielife/seed60/cookbook500/c500_132_ragu_chay.jpg', 'PENDING_REVIEW', 15, 'AI', DATEADD(MINUTE,-12283,@Now), DATEADD(MINUTE,-12039,@Now)),
(10093, N'Ragu Chay', 10057, N'Món Kho Chay Đậm Đà', 'LACTO', N'Ragu Chay – bản tăng cân', N'Ragu Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-132), phù hợp người tăng cân, khoảng 810.7 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Khoai tây, cà rốt rửa sạch, gọt vỏ. Khoai tây cắt làm tư, cà rốt cắt khúc vừa ăn. Đậu phụ, mít non cắt miếng vuông khoảng 2cm. Đậu phụ đem chiên sơ cho vàng hai mặt. Hành boa-rô cắt nhuyễn.
2. Bước 2: Đun nóng nồi cỡ vừa, cho 2 muỗng dầu ăn. Khi dầu nóng, cho hành củ băm nhuyễn vào xào thơm. Lần lượt cho khoai tây, cà rốt vào xào chung, nêm vào ít muối, hạt nêm.
3. Bước 3 : Cho khoảng 3 chén nước lọc, sữa tươi (nếu không dùng sữa tươi thì có thể tăng thêm nước lọc hoặc cốt dừa) vào nồi. Đun cho hỗn hợp khoai gần chín, cho tiếp đậu phụ, mít luộc vào nấu vừa chín. Rưới phần nước cốt dừa và cho bột ragu, bột ớt, 1/2 muỗng cà phê hạt nêm, ít đường, nấu sôi, tắt bếp. Cho Ragu chay ra tô, trang trí ngò rí, tiêu. Dùng nóng với bánh mì, bún hoặc cơm trắng.
4. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 19, 20, 2, 810.7, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791078/veggielife/seed60/cookbook500/c500_132_ragu_chay.jpg', 'APPROVED_ACTIVE', 285, 'USER', DATEADD(MINUTE,-3538,@Now), DATEADD(MINUTE,-2663,@Now)),
(10094, N'Cơm Cuộn Chay', 10001, N'Món Cơm & Xôi Chay', 'OVO', N'Cơm Cuộn Chay – bản tăng cân', N'Cơm Cuộn Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-097), phù hợp người tăng cân, khoảng 746.3 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Bước 1. Nấm hương, cà rốt, măng sau khi sơ chế sạch đem cắt hạt lựu. Nếu dùng nấm khô, bạn ngâm vào nước ấm khoảng 30 phút trước khi làm cho nấm mềm. Làm nóng chảo với chút dầu láng mặt, chờ dầu nóng thì trút nấm, cà rốt, măng vào xào sơ, nêm 1 xíu muối, đảo lại trong giây lát rồi tắt lửa.
2. Bước 2. Cơm trắng cho vào tô lớn, thêm 1,5 muỗng cà phê dầu mè, 1 muỗng cà phê muối, ½ muỗng cà phê hạt tiêu trắng vào trộn đều. Kế đó cho nấm, cà rốt và măng vào trộn đều.
3. Bước 3. Trứng đập ra bát, đánh tan. Trải tấm phù trúc lên mặt thớt rộng, dùng cọ quết một lớp trứng mỏng đều lên tấm phù trúc. Múc cơm vào giữa tấm phù trúc rồi dàn thành một lớp thật đều dày chừng 0.5cm, nhưng không dàn cơm khắp mặt tấm phù trúc mà chừa lại chừng 5cm ở phần mép ngoài để giúp cuộn cơm dễ dàng và kết dính tốt hơn. Đặt mấy quả đậu cô ve đã luộc chín vào vị trí 1/3 của cuộn cơm.
4. Bước 4. Nhấc mép phù trúc, từ từ cuốn lại như thao tác gói nem, lực cuốn vừa tay không lỏng cũng không chặt quá. Ở đoạn phù trúc cuối cùng mà bạn để thừa lại lúc trước, bạn dùng cọ quết một lớp trứng mỏng lên rồi mới cuộn lại để giúp cố định mép cuốn. Sau cùng, tiếp tục dùng cọ quết một lớp trứng mỏng lên khắp cuộn cơm.
5. Bước 5. Lăn cuốn cơm qua bột xù. Lưu ý, nên bóp vụn phần bột xù để giúp bột bám dính tốt hơn. Thêm vào đó không chỉ lăn cuốn cơm 1 lần mà nên 2,3 lần qua trứng và bột xù để tạo thành một lớp bột dày hơn xung quanh cuốn thịt. Trong quá trình cuốn, nhớ dùng lực ấn nhẹ để bột bám chắc lên bề mặt. Đặt cuốn cơm vào giữa tấm giấy bạc, gói kín lại. Làm nóng lò trước đến 180 độ rồi mới đặt cuộn cơm vào, nướng trong khoảng 12 phút. Cuộn cơm lấy ra cắt thành từng khoanh dày chừng 1.5cm, xếp lên đĩa.
6. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 22, 42, 2, 746.3, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790994/veggielife/seed60/cookbook500/c500_097_com_cuon_chay.jpg', 'APPROVED_ACTIVE', 316, 'SYSTEM', DATEADD(MINUTE,-78134,@Now), DATEADD(MINUTE,-75252,@Now)),
(10095, N'Cơm Cuộn Chay', 10069, N'Món Cơm & Xôi Chay', 'OVO', N'Cơm Cuộn Chay – bản giảm cân', N'Cơm Cuộn Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-097), phù hợp người giảm cân, khoảng 394.4 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Bước 1. Nấm hương, cà rốt, măng sau khi sơ chế sạch đem cắt hạt lựu. Nếu dùng nấm khô, bạn ngâm vào nước ấm khoảng 30 phút trước khi làm cho nấm mềm. Làm nóng chảo với chút dầu láng mặt, chờ dầu nóng thì trút nấm, cà rốt, măng vào xào sơ, nêm 1 xíu muối, đảo lại trong giây lát rồi tắt lửa.
2. Bước 2. Cơm trắng cho vào tô lớn, thêm 1,5 muỗng cà phê dầu mè, 1 muỗng cà phê muối, ½ muỗng cà phê hạt tiêu trắng vào trộn đều. Kế đó cho nấm, cà rốt và măng vào trộn đều.
3. Bước 3. Trứng đập ra bát, đánh tan. Trải tấm phù trúc lên mặt thớt rộng, dùng cọ quết một lớp trứng mỏng đều lên tấm phù trúc. Múc cơm vào giữa tấm phù trúc rồi dàn thành một lớp thật đều dày chừng 0.5cm, nhưng không dàn cơm khắp mặt tấm phù trúc mà chừa lại chừng 5cm ở phần mép ngoài để giúp cuộn cơm dễ dàng và kết dính tốt hơn. Đặt mấy quả đậu cô ve đã luộc chín vào vị trí 1/3 của cuộn cơm.
4. Bước 4. Nhấc mép phù trúc, từ từ cuốn lại như thao tác gói nem, lực cuốn vừa tay không lỏng cũng không chặt quá. Ở đoạn phù trúc cuối cùng mà bạn để thừa lại lúc trước, bạn dùng cọ quết một lớp trứng mỏng lên rồi mới cuộn lại để giúp cố định mép cuốn. Sau cùng, tiếp tục dùng cọ quết một lớp trứng mỏng lên khắp cuộn cơm.
5. Bước 5. Lăn cuốn cơm qua bột xù. Lưu ý, nên bóp vụn phần bột xù để giúp bột bám dính tốt hơn. Thêm vào đó không chỉ lăn cuốn cơm 1 lần mà nên 2,3 lần qua trứng và bột xù để tạo thành một lớp bột dày hơn xung quanh cuốn thịt. Trong quá trình cuốn, nhớ dùng lực ấn nhẹ để bột bám chắc lên bề mặt. Đặt cuốn cơm vào giữa tấm giấy bạc, gói kín lại. Làm nóng lò trước đến 180 độ rồi mới đặt cuộn cơm vào, nướng trong khoảng 12 phút. Cuộn cơm lấy ra cắt thành từng khoanh dày chừng 1.5cm, xếp lên đĩa.
6. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 22, 42, 2, 394.4, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790994/veggielife/seed60/cookbook500/c500_097_com_cuon_chay.jpg', 'PENDING_REVIEW', 1, 'AI', DATEADD(MINUTE,-28272,@Now), DATEADD(MINUTE,-27421,@Now)),
(10096, N'Cơm Cuộn Chay', 10080, N'Món Cơm & Xôi Chay', 'OVO', N'Cơm Cuộn Chay – bản giữ cân', N'Cơm Cuộn Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-097), phù hợp người giữ cân, khoảng 546.3 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Bước 1. Nấm hương, cà rốt, măng sau khi sơ chế sạch đem cắt hạt lựu. Nếu dùng nấm khô, bạn ngâm vào nước ấm khoảng 30 phút trước khi làm cho nấm mềm. Làm nóng chảo với chút dầu láng mặt, chờ dầu nóng thì trút nấm, cà rốt, măng vào xào sơ, nêm 1 xíu muối, đảo lại trong giây lát rồi tắt lửa.
2. Bước 2. Cơm trắng cho vào tô lớn, thêm 1,5 muỗng cà phê dầu mè, 1 muỗng cà phê muối, ½ muỗng cà phê hạt tiêu trắng vào trộn đều. Kế đó cho nấm, cà rốt và măng vào trộn đều.
3. Bước 3. Trứng đập ra bát, đánh tan. Trải tấm phù trúc lên mặt thớt rộng, dùng cọ quết một lớp trứng mỏng đều lên tấm phù trúc. Múc cơm vào giữa tấm phù trúc rồi dàn thành một lớp thật đều dày chừng 0.5cm, nhưng không dàn cơm khắp mặt tấm phù trúc mà chừa lại chừng 5cm ở phần mép ngoài để giúp cuộn cơm dễ dàng và kết dính tốt hơn. Đặt mấy quả đậu cô ve đã luộc chín vào vị trí 1/3 của cuộn cơm.
4. Bước 4. Nhấc mép phù trúc, từ từ cuốn lại như thao tác gói nem, lực cuốn vừa tay không lỏng cũng không chặt quá. Ở đoạn phù trúc cuối cùng mà bạn để thừa lại lúc trước, bạn dùng cọ quết một lớp trứng mỏng lên rồi mới cuộn lại để giúp cố định mép cuốn. Sau cùng, tiếp tục dùng cọ quết một lớp trứng mỏng lên khắp cuộn cơm.
5. Bước 5. Lăn cuốn cơm qua bột xù. Lưu ý, nên bóp vụn phần bột xù để giúp bột bám dính tốt hơn. Thêm vào đó không chỉ lăn cuốn cơm 1 lần mà nên 2,3 lần qua trứng và bột xù để tạo thành một lớp bột dày hơn xung quanh cuốn thịt. Trong quá trình cuốn, nhớ dùng lực ấn nhẹ để bột bám chắc lên bề mặt. Đặt cuốn cơm vào giữa tấm giấy bạc, gói kín lại. Làm nóng lò trước đến 180 độ rồi mới đặt cuộn cơm vào, nướng trong khoảng 12 phút. Cuộn cơm lấy ra cắt thành từng khoanh dày chừng 1.5cm, xếp lên đĩa.
6. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 22, 42, 2, 546.3, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790994/veggielife/seed60/cookbook500/c500_097_com_cuon_chay.jpg', 'APPROVED_ACTIVE', 497, 'AI', DATEADD(MINUTE,-14774,@Now), DATEADD(MINUTE,-14556,@Now)),
(10097, N'Củ Hủ Dừa Kho Nấm', 10001, N'Món Kho Chay Đậm Đà', 'BUDDHIST', N'Củ Hủ Dừa Kho Nấm – bản giữ cân', N'Củ Hủ Dừa Kho Nấm – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-023), phù hợp người giữ cân, khoảng 591.5 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1. Sơ chế
2. Củ hũ dừa cắt miếng chữ nhật dày 1cm kích thước 4cmx2,5cm.
3. Đậu bắp cắt khúc như củ hũ dừa.
4. Nấm rơm rửa sạch, cắt hình chữ thập ở đầu nấm.
5. Ngò rí cắt khúc, ớt hiểm đập dập.
6. 2. Thực hiện
7. Phi thơm sả băm và poiro, cho lần lượt củ hũ dừa, nấm rơm vào xào. Cho thêm 2M Nước tương, 1m Hạt nêm , 1m đường, 1/2 chén nước dừa.
8. Cho đậu hũ và ớt hiểm vào kho với lửa nhỏ khoảng 10 phút, rồi cho đậu bắp vào thêm khoảng 3 phút. Nấu đến khi nước hơi sánh thì tắt lửa.
9. 3. Cách dùng
10. Cho ra đĩa, trang trí với ngò, rắc thêm tiêu, dùng nóng với cơm.
11. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 22, 13, 2, 591.5, 'HARD', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790845/veggielife/seed60/cookbook500/c500_023_cu_hu_dua_kho_nam.jpg', 'APPROVED_ACTIVE', 428, 'SYSTEM', DATEADD(MINUTE,-70100,@Now), DATEADD(MINUTE,-69604,@Now)),
(10098, N'Cuốn Kho Chay Rong Biển', 10001, N'Món Cuốn Chay', 'BUDDHIST', N'Cuốn Kho Chay Rong Biển – bản giữ cân', N'Cuốn Kho Chay Rong Biển – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-194), phù hợp người giữ cân, khoảng 166.6 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đậu phụ bóp nát , vắt bớt nước, cho ráo bớt nước chua. Nếu không thích đậu hủ ta có thể thay thế bằng phù trúc (tàu hủ ky). Đem rửa sơ, bắc nước sôi và trụng cho mềm (đừng để quá mềm), vớt ra và cho ít muối vào để cho rõ nước (khâu này giống khâu sơ chế khi làm chả chay)
2. Nêm gia vị vào đậu phụ đã bóp nát hoặc phù trúc đã trụng,
3. Trải miếng rong biển ra (tuỳ theo ý thích làm miếng to hay nhỏ mà cắt miếng rong biển ra)
4. Lấy phần đậu hủ (phù trúc đã sơ chế trên trải theo chiều ngang của miếng rong biển và cuốn lại
5. Sau đó bắc dầu lên bếp và chiên sơ
6. Đun nóng dầu ăn và bỏ riềng ớt vào xào, xào một lúc cho thơm rồi bỏ các cuốn đã chiên vào đảo nhẹ tay. Nêm nước mắm chay hoặc nước tương theo khẩu vị của gia đình, Thêm một ít nước sôi ( nếu muốn có thể sử dụng nước dừa) đun nhỏ lửa khoảng 15-20 phút để gia vị thấm đều vào riềng và cuốn đậu hũ.
7. Tắt bếp, rắc tiêu, dùng nóng với cơm và rau luộc rất ngon.
8. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 16, 20, 2, 166.6, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791195/veggielife/seed60/cookbook500/c500_194_cuon_kho_chay_rong_bien.jpg', 'APPROVED_ACTIVE', 1202, 'SYSTEM', DATEADD(MINUTE,-65533,@Now), DATEADD(MINUTE,-63150,@Now)),
(10099, N'Cách Làm Đậu Phụ Sốt Chay', 10057, N'Món Xào Chay Thanh Vị', 'BUDDHIST', N'Cách Làm Đậu Phụ Sốt Chay – bản giữ cân', N'Cách Làm Đậu Phụ Sốt Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-180), phù hợp người giữ cân, khoảng 86.0 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Nấm hương ngâm nước cho nở rồi Thái nhỏ hoặc băm nhỏ nấm.
2. Bắc chảo lên bếp, cho dầu ăn vào và xào nấm hương chín.
3. Tiếp tục cho sa tế chay và ớt bột vào đảo đều, lấy 2 thìa canh nước ngâm nấm hương thêm vào chảo, để sôi, nêm nếm muối, đường cho vừa miệng.
4. Cho đậu non vào chảo, thi thoảng lắc nhẹ chảo để đậu ngấm gia vị, không đảo bằng đũa để tránh miếng đậu bị nát.
5. Khi đậu non sôi, ngấm đủ gia vị thì tắt bếp, dùng nóng sẽ ngon hơn.
6. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 16, 20, 2, 86.0, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791171/veggielife/seed60/cookbook500/c500_180_dau_phu_sot_chay.jpg', 'DRAFT', 15, 'USER', DATEADD(MINUTE,-7090,@Now), CAST(NULL AS DATETIME2(7))),
(10100, N'Cách Làm Đậu Phụ Sốt Chay', 10001, N'Món Xào Chay Thanh Vị', 'BUDDHIST', N'Cách Làm Đậu Phụ Sốt Chay – bản tăng cân', N'Cách Làm Đậu Phụ Sốt Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-180), phù hợp người tăng cân, khoảng 776.3 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Nấm hương ngâm nước cho nở rồi Thái nhỏ hoặc băm nhỏ nấm.
2. Bắc chảo lên bếp, cho dầu ăn vào và xào nấm hương chín.
3. Tiếp tục cho sa tế chay và ớt bột vào đảo đều, lấy 2 thìa canh nước ngâm nấm hương thêm vào chảo, để sôi, nêm nếm muối, đường cho vừa miệng.
4. Cho đậu non vào chảo, thi thoảng lắc nhẹ chảo để đậu ngấm gia vị, không đảo bằng đũa để tránh miếng đậu bị nát.
5. Khi đậu non sôi, ngấm đủ gia vị thì tắt bếp, dùng nóng sẽ ngon hơn.
6. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 16, 20, 2, 776.3, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791171/veggielife/seed60/cookbook500/c500_180_dau_phu_sot_chay.jpg', 'APPROVED_ACTIVE', 1184, 'SYSTEM', DATEADD(MINUTE,-76552,@Now), DATEADD(MINUTE,-76011,@Now)),
(10101, N'Bún Xào Chay', 10001, N'Món Mì, Bún & Phở Chay', 'BUDDHIST', N'Bún Xào Chay – bản giữ cân', N'Bún Xào Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-004), phù hợp người giữ cân, khoảng 502.9 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Bước 1: Bắp cải tím, cà rốt, ớt, nấm rửa sạch, cắt sợi.
2. Bún chần sơ qua nước sôi rồi đổ ra rổ cho ráo nước, sau đó rưới chút dầu olive vào bún, trộn đều.
3. Chuẩn bị một chảo nóng với một ít dầu, cho bún vào xào nhanh tay. Xào đến khi bún hơi vàng thì bạn cho ra đĩa, để riêng.
4. Bước 3: Làm nóng chút dầu trong chảo rồi cho tất cả các nguyên liệu bạ đã xắt sợi vào.
5. Bước 4: Xào đến khi các loại rau củ chín khoảng 80% thì cho bún vào xào nhanh tay. Nêm nếm lại với nước tương, dầu mè, hạt nêm cho vừa miệng.
6. Bước 5: Xào thêm 2 – 3 phút cho các nguyên liệu thấm gia vị thì tắt bếp, lấy bún xào chay ra tô, dùng nóng. Như vậy chúng ta đã học được cách làm bún xào chay ngũ sắc ngon và đẹp mắt rối đấy.
7. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 19, 10, 2, 502.9, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790805/veggielife/seed60/cookbook500/c500_004_bun_xao_chay.jpg', 'APPROVED_ACTIVE', 1539, 'SYSTEM', DATEADD(MINUTE,-83741,@Now), DATEADD(MINUTE,-82787,@Now)),
(10102, N'Bún Xào Chay', 10001, N'Món Mì, Bún & Phở Chay', 'BUDDHIST', N'Bún Xào Chay – bản giảm cân', N'Bún Xào Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-004), phù hợp người giảm cân, khoảng 365.0 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Bước 1: Bắp cải tím, cà rốt, ớt, nấm rửa sạch, cắt sợi.
2. Bún chần sơ qua nước sôi rồi đổ ra rổ cho ráo nước, sau đó rưới chút dầu olive vào bún, trộn đều.
3. Chuẩn bị một chảo nóng với một ít dầu, cho bún vào xào nhanh tay. Xào đến khi bún hơi vàng thì bạn cho ra đĩa, để riêng.
4. Bước 3: Làm nóng chút dầu trong chảo rồi cho tất cả các nguyên liệu bạ đã xắt sợi vào.
5. Bước 4: Xào đến khi các loại rau củ chín khoảng 80% thì cho bún vào xào nhanh tay. Nêm nếm lại với nước tương, dầu mè, hạt nêm cho vừa miệng.
6. Bước 5: Xào thêm 2 – 3 phút cho các nguyên liệu thấm gia vị thì tắt bếp, lấy bún xào chay ra tô, dùng nóng. Như vậy chúng ta đã học được cách làm bún xào chay ngũ sắc ngon và đẹp mắt rối đấy.
7. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 19, 10, 2, 365.0, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790805/veggielife/seed60/cookbook500/c500_004_bun_xao_chay.jpg', 'APPROVED_ACTIVE', 299, 'SYSTEM', DATEADD(MINUTE,-72926,@Now), DATEADD(MINUTE,-70338,@Now)),
(10103, N'Đậu Phụ Sốt Cay Tứ Xuyên', 10073, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Đậu Phụ Sốt Cay Tứ Xuyên – bản giảm cân', N'Đậu Phụ Sốt Cay Tứ Xuyên – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-111), phù hợp người giảm cân, khoảng 252.6 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Cắt đậu phụ thành những miếng hình vuông dài khoảng 3x3cm. Sau đó bạn đem cho đậu phụ vào nước muối ngâm trong vòng 15 phút rồi vớt ra để cho ráo nước.
2. Bước 2: Ngâm nấm hương trong nước nóng để khoảng 5 - 10 phút. Sau đó vớt ra rửa sạch rồi đem thái mỏng, còn phần nước ngâm bạn để riêng để cho vào nấu.
3. Bước 3: Hành khô, tỏi bóc vỏ đem băm nhỏ. Gừng đem thái mỏng.
4. Bước 4 Pha nước sốt gồm : 1 thìa nước tương, 20ml nước, 1 thìa sa tế chay, 1 thìa đường và 1 thìa muối.
5. Bước 5: Đặt chảo lên bếp, cho dầu ăn vào, lấy hành khô, tỏi và gừng cho vào xào cho đến khi dậy mùi thơm.
6. Bước 6: Cho nấm hương vào xào tiếp cho đến khi nấm hương chín mềm.
7. Bước 7: Đổ nước sốt đã pha và nước ngâm nấm hương vào chảo. Chờ đến khi nước sốt bắt đầu sôi lăn tăn thì cho đậu vào, đun lửa nhỏ khoảng 5 phút thì bắc ra. Đậu phụ sốt Tứ Xuyên rất phù hợp để ăn với cơm nóng nhé!
8. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 19, 30, 2, 252.6, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791023/veggielife/seed60/cookbook500/c500_111_dau_phu_sot_cay_tu_xuyen.jpg', 'APPROVED_ACTIVE', 383, 'USER', DATEADD(MINUTE,-39417,@Now), DATEADD(MINUTE,-39068,@Now)),
(10104, N'Đậu Phụ Sốt Cay Tứ Xuyên', 10001, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Đậu Phụ Sốt Cay Tứ Xuyên – bản tăng cân', N'Đậu Phụ Sốt Cay Tứ Xuyên – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-111), phù hợp người tăng cân, khoảng 760.6 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Cắt đậu phụ thành những miếng hình vuông dài khoảng 3x3cm. Sau đó bạn đem cho đậu phụ vào nước muối ngâm trong vòng 15 phút rồi vớt ra để cho ráo nước.
2. Bước 2: Ngâm nấm hương trong nước nóng để khoảng 5 - 10 phút. Sau đó vớt ra rửa sạch rồi đem thái mỏng, còn phần nước ngâm bạn để riêng để cho vào nấu.
3. Bước 3: Hành khô, tỏi bóc vỏ đem băm nhỏ. Gừng đem thái mỏng.
4. Bước 4 Pha nước sốt gồm : 1 thìa nước tương, 20ml nước, 1 thìa sa tế chay, 1 thìa đường và 1 thìa muối.
5. Bước 5: Đặt chảo lên bếp, cho dầu ăn vào, lấy hành khô, tỏi và gừng cho vào xào cho đến khi dậy mùi thơm.
6. Bước 6: Cho nấm hương vào xào tiếp cho đến khi nấm hương chín mềm.
7. Bước 7: Đổ nước sốt đã pha và nước ngâm nấm hương vào chảo. Chờ đến khi nước sốt bắt đầu sôi lăn tăn thì cho đậu vào, đun lửa nhỏ khoảng 5 phút thì bắc ra. Đậu phụ sốt Tứ Xuyên rất phù hợp để ăn với cơm nóng nhé!
8. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 19, 30, 2, 760.6, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791023/veggielife/seed60/cookbook500/c500_111_dau_phu_sot_cay_tu_xuyen.jpg', 'APPROVED_ACTIVE', 221, 'SYSTEM', DATEADD(MINUTE,-82683,@Now), DATEADD(MINUTE,-82453,@Now)),
(10105, N'Đậu Phụ Sốt Cay Tứ Xuyên', 10001, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Đậu Phụ Sốt Cay Tứ Xuyên – bản giữ cân', N'Đậu Phụ Sốt Cay Tứ Xuyên – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-111), phù hợp người giữ cân, khoảng 513.2 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Cắt đậu phụ thành những miếng hình vuông dài khoảng 3x3cm. Sau đó bạn đem cho đậu phụ vào nước muối ngâm trong vòng 15 phút rồi vớt ra để cho ráo nước.
2. Bước 2: Ngâm nấm hương trong nước nóng để khoảng 5 - 10 phút. Sau đó vớt ra rửa sạch rồi đem thái mỏng, còn phần nước ngâm bạn để riêng để cho vào nấu.
3. Bước 3: Hành khô, tỏi bóc vỏ đem băm nhỏ. Gừng đem thái mỏng.
4. Bước 4 Pha nước sốt gồm : 1 thìa nước tương, 20ml nước, 1 thìa sa tế chay, 1 thìa đường và 1 thìa muối.
5. Bước 5: Đặt chảo lên bếp, cho dầu ăn vào, lấy hành khô, tỏi và gừng cho vào xào cho đến khi dậy mùi thơm.
6. Bước 6: Cho nấm hương vào xào tiếp cho đến khi nấm hương chín mềm.
7. Bước 7: Đổ nước sốt đã pha và nước ngâm nấm hương vào chảo. Chờ đến khi nước sốt bắt đầu sôi lăn tăn thì cho đậu vào, đun lửa nhỏ khoảng 5 phút thì bắc ra. Đậu phụ sốt Tứ Xuyên rất phù hợp để ăn với cơm nóng nhé!
8. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 19, 30, 2, 513.2, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791023/veggielife/seed60/cookbook500/c500_111_dau_phu_sot_cay_tu_xuyen.jpg', 'APPROVED_ACTIVE', 928, 'SYSTEM', DATEADD(MINUTE,-72769,@Now), DATEADD(MINUTE,-70697,@Now)),
(10106, N'Đậu Phụ Nhồi Chay', 10001, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Đậu Phụ Nhồi Chay – bản tăng cân', N'Đậu Phụ Nhồi Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-121), phù hợp người tăng cân, khoảng 724.8 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Đậu phụ chiên rọc đường nhỏ phía cạnh rồi dùng muỗng múc hết phần ruột trắng bên trong.
2. Bước 2: Nấm mèo & nấm hương ngâm nở cắt nhỏ. Hành boa rô băm nhỏ.
3. Bước 3: Tất cả trộn đều cùng hạt nêm, dầu hào chay và hạt tiêu. Sau đó nhồi lại vào phần vỏ đậu phụ rồi dùng lá chuối tước nhỏ chần nhanh qua nước sôi buộc lại miếng đậu phụ và đem hấp 10 phút. Phần nhân dư thì bạn có thể vo viên tròn rồi cho vào hấp cùng.
4. Bước 4: Bắc chảo lên bếp cho nóng. Cho 1 bát nước, dầu hào chay, hạt nêm, nước tương và đường cùng ít tương ớt nấu sôi. Sau đó cho thêm 1 muỗng dầu mè vào.
5. Bước 5: Muốn phần sốt thêm sánh thì bạn cho thêm 1 bát nước nhỏ pha với bột năng vào chảo đang đun nhé. Nêm gia vị cho vừa ăn rồi đun thêm một lúc cho ngấm thì tắt bếp.
6. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 22, 10, 2, 724.8, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791048/veggielife/seed60/cookbook500/c500_121_dau_phu_nhoi_chay.jpg', 'APPROVED_ACTIVE', 1114, 'SYSTEM', DATEADD(MINUTE,-73086,@Now), DATEADD(MINUTE,-72387,@Now)),
(10107, N'Đậu Bắp Kho Nước Tương', 10001, N'Món Kho Chay Đậm Đà', 'VEGAN', N'Đậu Bắp Kho Nước Tương – bản giữ cân', N'Đậu Bắp Kho Nước Tương – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-119), phù hợp người giữ cân, khoảng 484.2 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Bước 1: Phi thơm boaro với 2 muỗng dầu ăn, vớt ra để riêng.
2. Bước 2: Chiên đậu bắp với lửa vừa, thêm ngô bao tử vào xào khoảng 2 phút, cho tiếp sả đập dập và 1/2 chén nước dừa.
3. Bước 3: Nêm 1 muỗng đường, 2 muỗng nước tương , 1 muỗng hạt nêm từ nấm hương và hạt sen, ⅓ muỗng bột ngọt , thêm ớt sừng và chả lụa vào đun lửa riu riu đến khi nước cạn, có độ sánh, rau củ và chả lụa thấm gia vị thì tắt bếp. Cho món ăn ra dĩa, rắc thêm tiêu, boaro phi và rau mùi. Dùng nóng với cơm.
4. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 22, 10, 2, 484.2, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791044/veggielife/seed60/cookbook500/c500_119_dau_bap_kho_nuoc_tuong.jpg', 'APPROVED_ACTIVE', 466, 'SYSTEM', DATEADD(MINUTE,-76137,@Now), DATEADD(MINUTE,-75428,@Now)),
(10108, N'Đậu Bắp Kho Nước Tương', 10031, N'Món Kho Chay Đậm Đà', 'VEGAN', N'Đậu Bắp Kho Nước Tương – bản giữ cân (không cay)', N'Đậu Bắp Kho Nước Tương – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-119), phù hợp người giữ cân, khoảng 482.2 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Bước 1: Phi thơm boaro với 2 muỗng dầu ăn, vớt ra để riêng.
2. Bước 2: Chiên đậu bắp với lửa vừa, thêm ngô bao tử vào xào khoảng 2 phút, cho tiếp sả đập dập và 1/2 chén nước dừa.
3. Bước 3: Nêm 1 muỗng đường, 2 muỗng nước tương , 1 muỗng hạt nêm từ nấm hương và hạt sen, ⅓ muỗng bột ngọt , thêm ớt sừng và chả lụa vào đun lửa riu riu đến khi nước cạn, có độ sánh, rau củ và chả lụa thấm gia vị thì tắt bếp. Cho món ăn ra dĩa, rắc thêm tiêu, boaro phi và rau mùi. Dùng nóng với cơm.
4. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ. Bản không cay: bỏ ớt, tương ớt và các gia vị cay khi nấu.', 22, 10, 2, 482.2, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791044/veggielife/seed60/cookbook500/c500_119_dau_bap_kho_nuoc_tuong.jpg', 'DRAFT', 17, 'USER', DATEADD(MINUTE,-4718,@Now), CAST(NULL AS DATETIME2(7))),
(10109, N'Đậu Bắp Kho Nước Tương', 10001, N'Món Kho Chay Đậm Đà', 'VEGAN', N'Đậu Bắp Kho Nước Tương – bản tăng cân', N'Đậu Bắp Kho Nước Tương – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-119), phù hợp người tăng cân, khoảng 758.2 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Bước 1: Phi thơm boaro với 2 muỗng dầu ăn, vớt ra để riêng.
2. Bước 2: Chiên đậu bắp với lửa vừa, thêm ngô bao tử vào xào khoảng 2 phút, cho tiếp sả đập dập và 1/2 chén nước dừa.
3. Bước 3: Nêm 1 muỗng đường, 2 muỗng nước tương , 1 muỗng hạt nêm từ nấm hương và hạt sen, ⅓ muỗng bột ngọt , thêm ớt sừng và chả lụa vào đun lửa riu riu đến khi nước cạn, có độ sánh, rau củ và chả lụa thấm gia vị thì tắt bếp. Cho món ăn ra dĩa, rắc thêm tiêu, boaro phi và rau mùi. Dùng nóng với cơm.
4. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 22, 10, 2, 758.2, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791044/veggielife/seed60/cookbook500/c500_119_dau_bap_kho_nuoc_tuong.jpg', 'APPROVED_ACTIVE', 1344, 'SYSTEM', DATEADD(MINUTE,-76731,@Now), DATEADD(MINUTE,-74816,@Now)),
(10110, N'Cà Tím Kho Đậu Hủ', 10001, N'Món Kho Chay Đậm Đà', 'BUDDHIST', N'Cà Tím Kho Đậu Hủ – bản giữ cân', N'Cà Tím Kho Đậu Hủ – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-100), phù hợp người giữ cân, khoảng 233.3 kcal mỗi khẩu phần (2 khẩu phần).', N'1. chế biến:
2. Cà tím rửa sạch, cắt khoanh vừa ăn. Đậu hủ thái miếng hình chữ nhật.
3. Phi thơm dầu ăn, cho cà vào đảo sơ qua, tiếp đến cho đậu hủ vào. Nêm ít nước tương, đường, muối cho món ăn ngấm gia vị. Cuối cùng cho ít nước lọc vào kho nhỏ lửa đến khi món ăn thấm đều thì tắt bếp, rắc ít tiêu lên bề mặt và dùng nóng.
4. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 16, 20, 2, 233.3, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790999/veggielife/seed60/cookbook500/c500_100_ca_tim_kho_dau_hu.jpg', 'APPROVED_ACTIVE', 1672, 'SYSTEM', DATEADD(MINUTE,-65348,@Now), DATEADD(MINUTE,-63026,@Now)),
(10111, N'Cà Tím Kho Đậu Hủ', 10001, N'Món Kho Chay Đậm Đà', 'BUDDHIST', N'Cà Tím Kho Đậu Hủ – bản giảm cân', N'Cà Tím Kho Đậu Hủ – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-100), phù hợp người giảm cân, khoảng 325.6 kcal mỗi khẩu phần (2 khẩu phần).', N'1. chế biến:
2. Cà tím rửa sạch, cắt khoanh vừa ăn. Đậu hủ thái miếng hình chữ nhật.
3. Phi thơm dầu ăn, cho cà vào đảo sơ qua, tiếp đến cho đậu hủ vào. Nêm ít nước tương, đường, muối cho món ăn ngấm gia vị. Cuối cùng cho ít nước lọc vào kho nhỏ lửa đến khi món ăn thấm đều thì tắt bếp, rắc ít tiêu lên bề mặt và dùng nóng.
4. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 16, 20, 2, 325.6, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790999/veggielife/seed60/cookbook500/c500_100_ca_tim_kho_dau_hu.jpg', 'APPROVED_ACTIVE', 1281, 'SYSTEM', DATEADD(MINUTE,-74840,@Now), DATEADD(MINUTE,-72874,@Now)),
(10112, N'Mì Quảng Xào Chay', 10001, N'Món Mì, Bún & Phở Chay', 'BUDDHIST', N'Mì Quảng Xào Chay – bản tăng cân', N'Mì Quảng Xào Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-186), phù hợp người tăng cân, khoảng 938.5 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1. Sơ chế
2. Nấm kim châm cắt đôi. Cần tàu cắt khúc. Ớt hiểm băm nhỏ. Ớt sừng cắt sợi.
3. Đậu hũ chiên cắt sợi. Cà rốt cắt sợi nhỏ.
4. Mì Quảng nêm ít bột ngọt AJI-NO-MOTO, 1/2 muỗng nước tương.
5. Trộn đều hỗn hợp gia vị 1 muỗng bột năng, 1 muỗng dầu màu điều, 1 muỗng nước tương , 1/2 muỗn bột ngọt ,1/2muỗng đường, 1/2muỗng hạt nêm chay, 1 muỗng nước me, 1 muỗng ớt hiểm băm, 1/2 chén nước.
6. 2. Xào mì
7. Cho ít dầu vô chảo, áp chảo mì quảng cho vàng xém, gom lại thành bánh tròn rồi bắc xuống để ra đĩa.
8. Cho tiếp dầu ăn vào chảo , đợi cho nóng lên, trút đậu hũ, cà rốt, cần tàu, nấm vào xào, cho tiếp ớt cắt sợi, hỗn hợp gia vị, khuấy nhanh tay cho sánh, tắt lửa, rắc tiêu.
9. 3. Cách dùng
10. Khi ăn cho phần rau củ xào lên phía trên mì, rắc húng lủi và đậu phộng. Dùng kèm nước tương và ớt băm.
11. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 25, 20, 2, 938.5, 'HARD', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791182/veggielife/seed60/cookbook500/c500_186_mi_quang_xao_chay.jpg', 'APPROVED_ACTIVE', 116, 'SYSTEM', DATEADD(MINUTE,-69196,@Now), DATEADD(MINUTE,-68201,@Now)),
(10113, N'Bánh Cuốn Chay', 10054, N'Món Cuốn Chay', 'BUDDHIST', N'Bánh Cuốn Chay – bản tăng cân', N'Bánh Cuốn Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-055), phù hợp người tăng cân, khoảng 768.8 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đậu phụ rán vàng, thái lát mỏng vừa ăn.
2. Cà rốt gọt vỏ, rửa sạch, thái sợi.
3. Mộc nhĩ ngâm nở, thái nhỏ.
4. Đun nóng một ít dầu ăn ở chảo, cho cà rốt , hành barô vào xào vừa chín tới.
5. Cho tiếp mộc nhĩ vào xào cùng, nêm vào một ít muối.
6. Cho đậu phụ vào xào cùng, rưới một ít xì dầu vào chảo, dùng đũa đảo đều, nêm nếm gia vị lại cho vừa ăn.
7. Xào tiếp đậu phụ khoảng 5-7 phút thì tắt bếp, rắc một ít hạt tiêu lên bề mặt, múc ra đĩa để riêng.
8. Rau quế rửa sạch, để ráo.
9. Bánh ướt bạn có thể dùng gói bột pha sẵn, pha theo hướng dẫn và khi tráng bánh thì bạn tráng dày hơn bánh cuốn bình thường, để có thể đơn giản bạn có thể mua bánh ướt tráng sẵn hoặc bánh phở tươi.
10. Bánh ướt sau khi tráng, bạn đổ bánh ra đĩa hay mâm có phết một ít dầu ăn thực vật.
11. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 22, 10, 2, 768.8, 'HARD', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790912/veggielife/seed60/cookbook500/c500_055_banh_cuon_chay.jpg', 'APPROVED_ACTIVE', 74, 'AI', DATEADD(MINUTE,-17759,@Now), DATEADD(MINUTE,-15152,@Now)),
(10114, N'Bánh Cuốn Chay', 10001, N'Món Cuốn Chay', 'BUDDHIST', N'Bánh Cuốn Chay – bản giữ cân', N'Bánh Cuốn Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-055), phù hợp người giữ cân, khoảng 544.9 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đậu phụ rán vàng, thái lát mỏng vừa ăn.
2. Cà rốt gọt vỏ, rửa sạch, thái sợi.
3. Mộc nhĩ ngâm nở, thái nhỏ.
4. Đun nóng một ít dầu ăn ở chảo, cho cà rốt , hành barô vào xào vừa chín tới.
5. Cho tiếp mộc nhĩ vào xào cùng, nêm vào một ít muối.
6. Cho đậu phụ vào xào cùng, rưới một ít xì dầu vào chảo, dùng đũa đảo đều, nêm nếm gia vị lại cho vừa ăn.
7. Xào tiếp đậu phụ khoảng 5-7 phút thì tắt bếp, rắc một ít hạt tiêu lên bề mặt, múc ra đĩa để riêng.
8. Rau quế rửa sạch, để ráo.
9. Bánh ướt bạn có thể dùng gói bột pha sẵn, pha theo hướng dẫn và khi tráng bánh thì bạn tráng dày hơn bánh cuốn bình thường, để có thể đơn giản bạn có thể mua bánh ướt tráng sẵn hoặc bánh phở tươi.
10. Bánh ướt sau khi tráng, bạn đổ bánh ra đĩa hay mâm có phết một ít dầu ăn thực vật.
11. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 22, 10, 2, 544.9, 'HARD', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790912/veggielife/seed60/cookbook500/c500_055_banh_cuon_chay.jpg', 'APPROVED_ACTIVE', 1747, 'SYSTEM', DATEADD(MINUTE,-82524,@Now), DATEADD(MINUTE,-81779,@Now)),
(10115, N'Bánh Cuốn Chay', 10030, N'Món Cuốn Chay', 'BUDDHIST', N'Bánh Cuốn Chay – bản giảm cân (không cay)', N'Bánh Cuốn Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-055), phù hợp người giảm cân, khoảng 397.9 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đậu phụ rán vàng, thái lát mỏng vừa ăn.
2. Cà rốt gọt vỏ, rửa sạch, thái sợi.
3. Mộc nhĩ ngâm nở, thái nhỏ.
4. Đun nóng một ít dầu ăn ở chảo, cho cà rốt , hành barô vào xào vừa chín tới.
5. Cho tiếp mộc nhĩ vào xào cùng, nêm vào một ít muối.
6. Cho đậu phụ vào xào cùng, rưới một ít xì dầu vào chảo, dùng đũa đảo đều, nêm nếm gia vị lại cho vừa ăn.
7. Xào tiếp đậu phụ khoảng 5-7 phút thì tắt bếp, rắc một ít hạt tiêu lên bề mặt, múc ra đĩa để riêng.
8. Rau quế rửa sạch, để ráo.
9. Bánh ướt bạn có thể dùng gói bột pha sẵn, pha theo hướng dẫn và khi tráng bánh thì bạn tráng dày hơn bánh cuốn bình thường, để có thể đơn giản bạn có thể mua bánh ướt tráng sẵn hoặc bánh phở tươi.
10. Bánh ướt sau khi tráng, bạn đổ bánh ra đĩa hay mâm có phết một ít dầu ăn thực vật.
11. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ. Bản không cay: bỏ ớt, tương ớt và các gia vị cay khi nấu.', 22, 10, 2, 397.9, 'HARD', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790912/veggielife/seed60/cookbook500/c500_055_banh_cuon_chay.jpg', 'APPROVED_ACTIVE', 491, 'AI', DATEADD(MINUTE,-1648,@Now), DATEADD(MINUTE,-1,@Now)),
(10116, N'Canh Chua Chay', 10001, N'Món Canh & Súp Bổ Dưỡng', 'BUDDHIST', N'Canh Chua Chay – bản giảm cân', N'Canh Chua Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-038), phù hợp người giảm cân, khoảng 254.9 kcal mỗi khẩu phần (2 khẩu phần).', N'1. làm:Sơ chế:
2. Đậu hũ non cắt ra miếng vừa ăn. Có thể chia bớt 1 phần xắt mỏng đem chiên giòn để sau cùng bỏ vào nồi canh cho phong phú.
3. Cà chua bổ múi cau làm 6 miếng
4. Dứa chia đôi: 1 phần bằm nhuyễn, 1 phần xắt lát mỏng
5. Đậu bắp xắt khúc vừa ăn
6. Dọc mùng cắt xéo khúc vừa ăn. Sau đó cho vào tô nước có pha chút muối, bóp 1 chút rồi rửa lại bằng nước sạch, làm vậy để không bị ngứa.
7. Tỏi tây thái nhỏ. Ngò xắt khúc.
8. Đầu tiên ngâm vắt me vào trong cái chén nước ấm, dằm nhẹ cho ra nước chua (dằm mạnh cũng không sao).
9. Bắc cái nồi đủ nấu canh, cho vào chút dầu ăn, rồi thả vài lát boa rô vào phi thơm, sau đó cho phần dứa băm vào xào cho chín, ra nước vàng, rồi cho tiếp cà chua và dứa lát vào xào sơ vài phát cho vui (buồn thì đừng xào nha :))) .
10. Rồi mới đổ 1 lượng nước đủ nấu canh vào nồi, trút hết nấm vô luôn, nấu sôi.
11. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 16, 20, 2, 254.9, 'HARD', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790878/veggielife/seed60/cookbook500/c500_038_canh_chua_chay.jpg', 'APPROVED_ACTIVE', 30, 'SYSTEM', DATEADD(MINUTE,-73097,@Now), DATEADD(MINUTE,-71647,@Now)),
(10117, N'Canh Chua Chay', 10001, N'Món Canh & Súp Bổ Dưỡng', 'BUDDHIST', N'Canh Chua Chay – bản tăng cân', N'Canh Chua Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-038), phù hợp người tăng cân, khoảng 781.2 kcal mỗi khẩu phần (2 khẩu phần).', N'1. làm:Sơ chế:
2. Đậu hũ non cắt ra miếng vừa ăn. Có thể chia bớt 1 phần xắt mỏng đem chiên giòn để sau cùng bỏ vào nồi canh cho phong phú.
3. Cà chua bổ múi cau làm 6 miếng
4. Dứa chia đôi: 1 phần bằm nhuyễn, 1 phần xắt lát mỏng
5. Đậu bắp xắt khúc vừa ăn
6. Dọc mùng cắt xéo khúc vừa ăn. Sau đó cho vào tô nước có pha chút muối, bóp 1 chút rồi rửa lại bằng nước sạch, làm vậy để không bị ngứa.
7. Tỏi tây thái nhỏ. Ngò xắt khúc.
8. Đầu tiên ngâm vắt me vào trong cái chén nước ấm, dằm nhẹ cho ra nước chua (dằm mạnh cũng không sao).
9. Bắc cái nồi đủ nấu canh, cho vào chút dầu ăn, rồi thả vài lát boa rô vào phi thơm, sau đó cho phần dứa băm vào xào cho chín, ra nước vàng, rồi cho tiếp cà chua và dứa lát vào xào sơ vài phát cho vui (buồn thì đừng xào nha :))) .
10. Rồi mới đổ 1 lượng nước đủ nấu canh vào nồi, trút hết nấm vô luôn, nấu sôi.
11. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 16, 20, 2, 781.2, 'HARD', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790878/veggielife/seed60/cookbook500/c500_038_canh_chua_chay.jpg', 'APPROVED_ACTIVE', 760, 'SYSTEM', DATEADD(MINUTE,-82065,@Now), DATEADD(MINUTE,-81373,@Now)),
(10118, N'Canh Chua Chay', 10138, N'Món Canh & Súp Bổ Dưỡng', 'BUDDHIST', N'Canh Chua Chay – bản giữ cân', N'Canh Chua Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-038), phù hợp người giữ cân, khoảng 491.3 kcal mỗi khẩu phần (2 khẩu phần).', N'1. làm:Sơ chế:
2. Đậu hũ non cắt ra miếng vừa ăn. Có thể chia bớt 1 phần xắt mỏng đem chiên giòn để sau cùng bỏ vào nồi canh cho phong phú.
3. Cà chua bổ múi cau làm 6 miếng
4. Dứa chia đôi: 1 phần bằm nhuyễn, 1 phần xắt lát mỏng
5. Đậu bắp xắt khúc vừa ăn
6. Dọc mùng cắt xéo khúc vừa ăn. Sau đó cho vào tô nước có pha chút muối, bóp 1 chút rồi rửa lại bằng nước sạch, làm vậy để không bị ngứa.
7. Tỏi tây thái nhỏ. Ngò xắt khúc.
8. Đầu tiên ngâm vắt me vào trong cái chén nước ấm, dằm nhẹ cho ra nước chua (dằm mạnh cũng không sao).
9. Bắc cái nồi đủ nấu canh, cho vào chút dầu ăn, rồi thả vài lát boa rô vào phi thơm, sau đó cho phần dứa băm vào xào cho chín, ra nước vàng, rồi cho tiếp cà chua và dứa lát vào xào sơ vài phát cho vui (buồn thì đừng xào nha :))) .
10. Rồi mới đổ 1 lượng nước đủ nấu canh vào nồi, trút hết nấm vô luôn, nấu sôi.
11. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 16, 20, 2, 491.3, 'HARD', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790878/veggielife/seed60/cookbook500/c500_038_canh_chua_chay.jpg', 'REJECTED', 22, 'USER', DATEADD(MINUTE,-8714,@Now), DATEADD(MINUTE,-6121,@Now)),
(10119, N'Khổ Qua Kho Chay', 10090, N'Món Kho Chay Đậm Đà', 'VEGAN', N'Khổ Qua Kho Chay – bản tăng cân', N'Khổ Qua Kho Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-039), phù hợp người tăng cân, khoảng 668.1 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1. Sơ chế
2. Khổ qua rửa sạch, cắt khúc1.5cm, bỏ ruột. Ớt hiểm đập dập. Nấm mèo ngâm nở, thái sợi, băm nhỏ, bún tàu ngâm mềm, cắt khúc 2cm.
3. Đậu hũ chần qua nước sôi, cho vào vải mùng vắt ráo nước, tán nhuyễn, trộn chung với nấm mèo, bún tàu, 1m bột năng, 2M boaro phi vàng, 1/2m muối, 1/2m đường, 1/2m tiêu, 1/2m bột ngọt AJI-NO-MOTO®, nhồi nhân vào khổ qua.
4. Pha hỗn hợp 2M nước tương LISA, 1M đường, 1m bột ngọt AJI-NO-MOTO®, 1/5m muối.
5. Chiên áp chảo khổ qua đến khi rám mặt, rồi cho hỗn hợp nước tương vào, kho 2 phút cho thấm vị, thêm nước dừa, ớt hiểm đập dập rồi kho tiếp 10 phút cho tới khi nước xâm xấp là được.
6. 3. Cách dùng Trình bày món ăn ra dĩa, trang trí ớt hiểm, rưới thêm nước kho và boaro phi.
7. Mách nhỏ**
8. Chiên khổ qua giúp khổ qua săn mặt, không bị bở, nhân kết dính. Vị cay trong món ăn làm giảm cảm giác đắng của khổ qua.
9. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 22, 12, 2, 668.1, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790880/veggielife/seed60/cookbook500/c500_039_kho_qua_kho_chay.jpg', 'DRAFT', 11, 'USER', DATEADD(MINUTE,-16930,@Now), CAST(NULL AS DATETIME2(7))),
(10120, N'Khổ Qua Kho Chay', 10001, N'Món Kho Chay Đậm Đà', 'VEGAN', N'Khổ Qua Kho Chay – bản giảm cân', N'Khổ Qua Kho Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-039), phù hợp người giảm cân, khoảng 386.6 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1. Sơ chế
2. Khổ qua rửa sạch, cắt khúc1.5cm, bỏ ruột. Ớt hiểm đập dập. Nấm mèo ngâm nở, thái sợi, băm nhỏ, bún tàu ngâm mềm, cắt khúc 2cm.
3. Đậu hũ chần qua nước sôi, cho vào vải mùng vắt ráo nước, tán nhuyễn, trộn chung với nấm mèo, bún tàu, 1m bột năng, 2M boaro phi vàng, 1/2m muối, 1/2m đường, 1/2m tiêu, 1/2m bột ngọt AJI-NO-MOTO®, nhồi nhân vào khổ qua.
4. Pha hỗn hợp 2M nước tương LISA, 1M đường, 1m bột ngọt AJI-NO-MOTO®, 1/5m muối.
5. Chiên áp chảo khổ qua đến khi rám mặt, rồi cho hỗn hợp nước tương vào, kho 2 phút cho thấm vị, thêm nước dừa, ớt hiểm đập dập rồi kho tiếp 10 phút cho tới khi nước xâm xấp là được.
6. 3. Cách dùng Trình bày món ăn ra dĩa, trang trí ớt hiểm, rưới thêm nước kho và boaro phi.
7. Mách nhỏ**
8. Chiên khổ qua giúp khổ qua săn mặt, không bị bở, nhân kết dính. Vị cay trong món ăn làm giảm cảm giác đắng của khổ qua.
9. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 22, 12, 2, 386.6, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790880/veggielife/seed60/cookbook500/c500_039_kho_qua_kho_chay.jpg', 'APPROVED_ACTIVE', 1383, 'SYSTEM', DATEADD(MINUTE,-76109,@Now), DATEADD(MINUTE,-74289,@Now))
) AS v(rid, fn, uid, cat, sc, ti, de, ins, pr, ck, sv, kc, df, vf, im, st, vw, sr, cr, up)
JOIN #food f ON f.Food_Name = v.fn
JOIN #cat c ON c.Category_Name = v.cat
JOIN #sch s ON s.School_Code = v.sc;
IF @@ROWCOUNT <> 60 THROW 50001, N'Recipes: tra cứu dữ liệu master thất bại (thiếu tên/mã)', 1;
INSERT INTO dbo.Recipes (Recipe_ID, Food_ID, User_ID, Category_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At)
SELECT v.rid, f.Food_ID, v.uid, c.Category_ID, s.Dietary_School_ID, v.ti, v.de, v.ins, v.pr, v.ck, v.sv, v.kc, v.df, v.vf, v.im, v.st, v.vw, v.sr, v.cr, v.up
FROM (VALUES
(10121, N'Bún Trộn Dưa Leo Giòn Mát Ngày Hè', 10001, N'Món Mì, Bún & Phở Chay', 'VEGAN', N'Bún Trộn Dưa Leo Giòn Mát Ngày Hè – bản giữ cân', N'Bún Trộn Dưa Leo Giòn Mát Ngày Hè – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-020), phù hợp người giữ cân, khoảng 594.6 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Bún rửa sạch, ngâm nước cho bún khô mềm ra.
2. Dưa leo rửa sạch, bỏ ruột, bào sợi mảnh.
3. Đun nước sôi, sau đó thả bún vào trụng cho chín.
4. Vặn lửa nhỏ, để bún sôi trở lại cho bún chín thì thả vào 1 tô nước lạnh để bún không bị nát.
5. Gừng, tỏi băm nhỏ, cho vào tô trộn cùng giấm.
6. Thêm dầu mè, đường, nước mắm chay, ớt vào trộn đều rồi đặt lên bếp đun sôi sao cho hỗn hợp xốt vừa miệng.
7. Vớt bún ra để ráo, trộn cùng với dưa leo.
8. Đổ phần nước xốt vào trộn đều lên sau đó múc ra bát và thưởng thức.
9. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 19, 20, 2, 594.6, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790839/veggielife/seed60/cookbook500/c500_020_bun_tron_dua_leo_gion_mat_ngay_he.jpg', 'APPROVED_ACTIVE', 824, 'SYSTEM', DATEADD(MINUTE,-82022,@Now), DATEADD(MINUTE,-81124,@Now)),
(10122, N'Bún Trộn Dưa Leo Giòn Mát Ngày Hè', 10001, N'Món Mì, Bún & Phở Chay', 'VEGAN', N'Bún Trộn Dưa Leo Giòn Mát Ngày Hè – bản tăng cân', N'Bún Trộn Dưa Leo Giòn Mát Ngày Hè – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-020), phù hợp người tăng cân, khoảng 723.9 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Bún rửa sạch, ngâm nước cho bún khô mềm ra.
2. Dưa leo rửa sạch, bỏ ruột, bào sợi mảnh.
3. Đun nước sôi, sau đó thả bún vào trụng cho chín.
4. Vặn lửa nhỏ, để bún sôi trở lại cho bún chín thì thả vào 1 tô nước lạnh để bún không bị nát.
5. Gừng, tỏi băm nhỏ, cho vào tô trộn cùng giấm.
6. Thêm dầu mè, đường, nước mắm chay, ớt vào trộn đều rồi đặt lên bếp đun sôi sao cho hỗn hợp xốt vừa miệng.
7. Vớt bún ra để ráo, trộn cùng với dưa leo.
8. Đổ phần nước xốt vào trộn đều lên sau đó múc ra bát và thưởng thức.
9. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 19, 20, 2, 723.9, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790839/veggielife/seed60/cookbook500/c500_020_bun_tron_dua_leo_gion_mat_ngay_he.jpg', 'APPROVED_ACTIVE', 1328, 'SYSTEM', DATEADD(MINUTE,-73460,@Now), DATEADD(MINUTE,-71272,@Now)),
(10123, N'Canh Nấm Đậu Hũ', 10001, N'Món Canh & Súp Bổ Dưỡng', 'VEGAN', N'Canh Nấm Đậu Hũ – bản giữ cân', N'Canh Nấm Đậu Hũ – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-018), phù hợp người giữ cân, khoảng 84.4 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Ngâm rửa sạch nấm hương và nấm kim châm, sau đó cắt thành miếng nhỏ vừa ăn. Cắt đậu phụ non thành miếng.
2. Đun một nồi nước sôi, cho đậu phụ vào nấu, sau đó thêm nấm vào. Đun trong 5-10 phút cho đến khi nấm và đậu phụ chín hẳn thì nêm nếm gia vị và thêm hành lá vào.
3. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 13, 10, 2, 84.4, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790834/veggielife/seed60/cookbook500/c500_018_canh_nam_dau_hu.jpg', 'APPROVED_ACTIVE', 1076, 'SYSTEM', DATEADD(MINUTE,-77920,@Now), DATEADD(MINUTE,-77034,@Now)),
(10124, N'Đậu Phụ Sốt Nấm Kim Châm', 10089, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Đậu Phụ Sốt Nấm Kim Châm – bản giảm cân', N'Đậu Phụ Sốt Nấm Kim Châm – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-123), phù hợp người giảm cân, khoảng 341.7 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Bước 1: Phi thơm hành tỏi trên chảo dầu nóng, cho cà rốt vào xào qua. Đổ vào khoảng 1 bát nước lạnh, nêm 2 thìa xì dầu, thêm đường, muối cho vừa ăn.
2. Bước 2: Hòa bột ngô với ít nước rồi đổ vào chảo, khuấy đều cho hỗn hợp nước sốt sánh lại. (Bột ngô có tác dụng làm sánh phần nước sốt lại. Nếu bạn không mua được bột ngô thì món sẽ không được đẹp mắt thôi nhé).
3. Bước 3: Thả đậu vào chảo nước sốt khoảng 2-3 phút. Tắt bếp, đậy vung kín chảo trong vòng 5 phút để đậu thấm gia vị và nước sốt.
4. Bước 4: Bật bếp trở lại, cho nấm kim châm vào nấu khoảng 3 phút (Bạn không nên để quá lâu làm mất độ giòn của nấm).
5. Bước 5: Tắt bếp, đổ ra đĩa. Cuối cùng là rắc một chút rau mùi lên trên cho bắt mắt và giúp món ăn thơm hơn. Đậu phụ sốt nấm kim châm ăn rất hợp với cơm hoặc ăn không cũng được nhé!
6. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 19, 11, 2, 341.7, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791054/veggielife/seed60/cookbook500/c500_123_dau_phu_sot_nam_kim_cham.jpg', 'PENDING_REVIEW', 15, 'USER', DATEADD(MINUTE,-37255,@Now), DATEADD(MINUTE,-35729,@Now)),
(10125, N'Đậu Phụ Sốt Nấm Kim Châm', 10079, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Đậu Phụ Sốt Nấm Kim Châm – bản tăng cân', N'Đậu Phụ Sốt Nấm Kim Châm – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-123), phù hợp người tăng cân, khoảng 740.1 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Bước 1: Phi thơm hành tỏi trên chảo dầu nóng, cho cà rốt vào xào qua. Đổ vào khoảng 1 bát nước lạnh, nêm 2 thìa xì dầu, thêm đường, muối cho vừa ăn.
2. Bước 2: Hòa bột ngô với ít nước rồi đổ vào chảo, khuấy đều cho hỗn hợp nước sốt sánh lại. (Bột ngô có tác dụng làm sánh phần nước sốt lại. Nếu bạn không mua được bột ngô thì món sẽ không được đẹp mắt thôi nhé).
3. Bước 3: Thả đậu vào chảo nước sốt khoảng 2-3 phút. Tắt bếp, đậy vung kín chảo trong vòng 5 phút để đậu thấm gia vị và nước sốt.
4. Bước 4: Bật bếp trở lại, cho nấm kim châm vào nấu khoảng 3 phút (Bạn không nên để quá lâu làm mất độ giòn của nấm).
5. Bước 5: Tắt bếp, đổ ra đĩa. Cuối cùng là rắc một chút rau mùi lên trên cho bắt mắt và giúp món ăn thơm hơn. Đậu phụ sốt nấm kim châm ăn rất hợp với cơm hoặc ăn không cũng được nhé!
6. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 19, 11, 2, 740.1, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791054/veggielife/seed60/cookbook500/c500_123_dau_phu_sot_nam_kim_cham.jpg', 'QUARANTINED_HIDDEN', 11, 'USER', DATEADD(MINUTE,-36809,@Now), DATEADD(MINUTE,-34508,@Now)),
(10126, N'Đậu Phụ Sốt Nấm Kim Châm', 10038, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Đậu Phụ Sốt Nấm Kim Châm – bản giữ cân', N'Đậu Phụ Sốt Nấm Kim Châm – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-123), phù hợp người giữ cân, khoảng 516.2 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Bước 1: Phi thơm hành tỏi trên chảo dầu nóng, cho cà rốt vào xào qua. Đổ vào khoảng 1 bát nước lạnh, nêm 2 thìa xì dầu, thêm đường, muối cho vừa ăn.
2. Bước 2: Hòa bột ngô với ít nước rồi đổ vào chảo, khuấy đều cho hỗn hợp nước sốt sánh lại. (Bột ngô có tác dụng làm sánh phần nước sốt lại. Nếu bạn không mua được bột ngô thì món sẽ không được đẹp mắt thôi nhé).
3. Bước 3: Thả đậu vào chảo nước sốt khoảng 2-3 phút. Tắt bếp, đậy vung kín chảo trong vòng 5 phút để đậu thấm gia vị và nước sốt.
4. Bước 4: Bật bếp trở lại, cho nấm kim châm vào nấu khoảng 3 phút (Bạn không nên để quá lâu làm mất độ giòn của nấm).
5. Bước 5: Tắt bếp, đổ ra đĩa. Cuối cùng là rắc một chút rau mùi lên trên cho bắt mắt và giúp món ăn thơm hơn. Đậu phụ sốt nấm kim châm ăn rất hợp với cơm hoặc ăn không cũng được nhé!
6. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 19, 11, 2, 516.2, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791054/veggielife/seed60/cookbook500/c500_123_dau_phu_sot_nam_kim_cham.jpg', 'APPROVED_ACTIVE', 213, 'USER', DATEADD(MINUTE,-14088,@Now), DATEADD(MINUTE,-13604,@Now)),
(10127, N'Há Cảo Chay Chiên', 10134, N'Món Chiên & Rán Giòn Rụm', 'BUDDHIST', N'Há Cảo Chay Chiên – bản giữ cân', N'Há Cảo Chay Chiên – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-187), phù hợp người giữ cân, khoảng 190.8 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Mộc nhĩ, nấm hương ngâm nở, băm nhỏ. Nấm rơm rửa sạch, thái nhỏ. Cà-rốt gọt vỏ, thái hỏ.
2. Làm nhân: Đun nóng dầu ăn, cho nấm rơm, cà-rốt, mộc nhĩ, nấm hương vào xào chín. Nêm bột gia vị, hạt nêm chay và tiêu .
3. Cho hỗn hợp rau củ ra thố, trộn cùng đậu phụ đã bóp thật nhuyễn. Trải há cảo trên mặt phẳng, múc từng thìa nhân cho vào giữa, gập 4 mép lá há cảo, đầu lá cho chụm lại, dính vào nhau. Đun sôi dầu, cho há cảo vào rán vàng giòn. Cho ra đĩa và thưởng thức
4. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 16, 20, 2, 190.8, 'EASY', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791184/veggielife/seed60/cookbook500/c500_187_ha_cao_chay_chien.jpg', 'DRAFT', 2, 'AI', DATEADD(MINUTE,-14055,@Now), CAST(NULL AS DATETIME2(7))),
(10128, N'Há Cảo Chay Chiên', 10147, N'Món Chiên & Rán Giòn Rụm', 'BUDDHIST', N'Há Cảo Chay Chiên – bản tăng cân', N'Há Cảo Chay Chiên – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-187), phù hợp người tăng cân, khoảng 777.0 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Mộc nhĩ, nấm hương ngâm nở, băm nhỏ. Nấm rơm rửa sạch, thái nhỏ. Cà-rốt gọt vỏ, thái hỏ.
2. Làm nhân: Đun nóng dầu ăn, cho nấm rơm, cà-rốt, mộc nhĩ, nấm hương vào xào chín. Nêm bột gia vị, hạt nêm chay và tiêu .
3. Cho hỗn hợp rau củ ra thố, trộn cùng đậu phụ đã bóp thật nhuyễn. Trải há cảo trên mặt phẳng, múc từng thìa nhân cho vào giữa, gập 4 mép lá há cảo, đầu lá cho chụm lại, dính vào nhau. Đun sôi dầu, cho há cảo vào rán vàng giòn. Cho ra đĩa và thưởng thức
4. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 16, 20, 2, 777.0, 'EASY', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791184/veggielife/seed60/cookbook500/c500_187_ha_cao_chay_chien.jpg', 'APPROVED_ACTIVE', 257, 'USER', DATEADD(MINUTE,-34683,@Now), DATEADD(MINUTE,-32227,@Now)),
(10129, N'Mì Xào Chay', 10001, N'Món Mì, Bún & Phở Chay', 'BUDDHIST', N'Mì Xào Chay – bản giữ cân', N'Mì Xào Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-127), phù hợp người giữ cân, khoảng 154.2 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1 : Mì bạn đem trần qua nước sôi. Vớt ra ngâm ngay vào nước lạnh rồi để cho ráo nước.
2. Bước 2 : Đậu phụ đem chiên vàng, sau đó xắt miếng nhỏ.
3. Bước 3 : Nấm hương, nấm rơm, nấm mèo bạn đem cắt bỏ chân, rồi sau đó đem rửa sạch, xắt nhỏ (Nếu bạn mua nấm khô thì cần ngâm qua nước ấm khoảng 5 phút trước).
4. Bước 4 : Cải thảo và cải ngọt bạn đem rửa sạch, xắt miếng vừa ăn.
5. Bước 5 : Bắc chảo nóng lên trên bếp, sau đó đem cho cải và nấm vào xào, nêm muối, đường, nước tương, cho vào một ít nước khi xào. Khi rau và nấm chín tới, bạn đem cho mì vào xào áp chảo, đảo đều tay (Nên để lửa lớn).
6. Bước 6 : Cho mì ra đĩa, trang trí thêm ngò tây, dùng nóng.
7. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 19, 10, 2, 154.2, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791066/veggielife/seed60/cookbook500/c500_127_mi_xao_chay.jpg', 'APPROVED_ACTIVE', 150, 'SYSTEM', DATEADD(MINUTE,-75901,@Now), DATEADD(MINUTE,-73092,@Now)),
(10130, N'Mì Xào Chay', 10001, N'Món Mì, Bún & Phở Chay', 'BUDDHIST', N'Mì Xào Chay – bản giảm cân', N'Mì Xào Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-127), phù hợp người giảm cân, khoảng 332.3 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1 : Mì bạn đem trần qua nước sôi. Vớt ra ngâm ngay vào nước lạnh rồi để cho ráo nước.
2. Bước 2 : Đậu phụ đem chiên vàng, sau đó xắt miếng nhỏ.
3. Bước 3 : Nấm hương, nấm rơm, nấm mèo bạn đem cắt bỏ chân, rồi sau đó đem rửa sạch, xắt nhỏ (Nếu bạn mua nấm khô thì cần ngâm qua nước ấm khoảng 5 phút trước).
4. Bước 4 : Cải thảo và cải ngọt bạn đem rửa sạch, xắt miếng vừa ăn.
5. Bước 5 : Bắc chảo nóng lên trên bếp, sau đó đem cho cải và nấm vào xào, nêm muối, đường, nước tương, cho vào một ít nước khi xào. Khi rau và nấm chín tới, bạn đem cho mì vào xào áp chảo, đảo đều tay (Nên để lửa lớn).
6. Bước 6 : Cho mì ra đĩa, trang trí thêm ngò tây, dùng nóng.
7. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 19, 10, 2, 332.3, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791066/veggielife/seed60/cookbook500/c500_127_mi_xao_chay.jpg', 'APPROVED_ACTIVE', 1505, 'SYSTEM', DATEADD(MINUTE,-68463,@Now), DATEADD(MINUTE,-67706,@Now)),
(10131, N'Đậu Phụ Nhồi Nấm Chay', 10001, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Đậu Phụ Nhồi Nấm Chay – bản giảm cân', N'Đậu Phụ Nhồi Nấm Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-071), phù hợp người giảm cân, khoảng 320.5 kcal mỗi khẩu phần (2 khẩu phần).', N'1. làm đậu phụ nhồi nấm ngon
2. Bước 1: Bạn ngâm nấm tai mèo + nấm đông cô với nước ấm cho nấm được nở ra, cắt bỏ chân nấm, rửa lại với nước cho sạch rồi thái thành hạt lựu. Hành hoa cắt rễ, rửa sạch, để riêng phần đầu trắng của hành, giã nhuyễn, còn phần lá bạn thái nhỏ, bỏ vào bát riêng. Cà rốt rửa sạch, gọt vỏ rồi nạo sợi, băm nhỏ.
3. Bước 2: Bạn đặt chảo lên bếp, đổ chút dầu ăn rồi cho nửa phần đầu hành trắng đã giã nhuyễn vào phi cho thơm rồi cho bát nấm vào xào, nêm chút gia vị gồm nửa thìa cafe hạt nêm chay vào đảo đều. Khoảng 1 phút sau thì bạn tắt bếp nhé.
4. Bước 3: Đậu phụ bạn thái miếng vừa rồi khoét lỗ ở giữa miếng đậu. Phần đậu khoét ra bạn đừng vứt đi mà dầm nhuyễn rồi cho vào phần nấm xào, trộn thật đều. Sau đó, bạn đem nhồi vào phần khoảng trống của miếng đậu rồi rán vàng. Bạn lưu ý khi rán đậu đừng rán cả hai mặt mà chỉ rán một mặt để bên trong được mềm, miếng đậu bạn đừng cắt nhỏ quá kẻo bị vỡ nhé. Trong khi đun đậu trên bếp thì bạn đậy vung lại để nhân nấm được mềm.
5. Bước 4: Phần đầu hành còn lại bạn cho tiếp vào chảo, phi thơm rồi cho cà rốt + hành lá + 1 thìa cafe nước tương + nửa thìa cafe đường + 1/3 thìa cafe hạt nêm chay + chút muối, trộn đều. Bột ngô bạn hòa tan với chút nước rồi trút vào chảo cho hỗn hợp sền sệt thì tắt bếp. Bạn trang trí lại đĩa đậu rồi rưới nước sốt lên trên. Bày ra mâm và thưởng thức ngay cho nóng nhé! 😉Vào những ngày mâm cơm đã nhiều chất đạm hay đầu tháng thì các món mặn chay như đậu phụ nhồi nấm này cực kỳ thích hợp đó. Còn chần chừ gì nữa, ngay chiều nay hãy chiêu đãi cả nhà mình món ngon này nha bạn!
6. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 19, 10, 2, 320.5, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790943/veggielife/seed60/cookbook500/c500_071_dau_phu_nhoi_nam_chay.jpg', 'APPROVED_ACTIVE', 1051, 'SYSTEM', DATEADD(MINUTE,-78529,@Now), DATEADD(MINUTE,-78173,@Now)),
(10132, N'Đậu Phụ Nhồi Nấm Chay', 10001, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Đậu Phụ Nhồi Nấm Chay – bản tăng cân', N'Đậu Phụ Nhồi Nấm Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-071), phù hợp người tăng cân, khoảng 757.7 kcal mỗi khẩu phần (2 khẩu phần).', N'1. làm đậu phụ nhồi nấm ngon
2. Bước 1: Bạn ngâm nấm tai mèo + nấm đông cô với nước ấm cho nấm được nở ra, cắt bỏ chân nấm, rửa lại với nước cho sạch rồi thái thành hạt lựu. Hành hoa cắt rễ, rửa sạch, để riêng phần đầu trắng của hành, giã nhuyễn, còn phần lá bạn thái nhỏ, bỏ vào bát riêng. Cà rốt rửa sạch, gọt vỏ rồi nạo sợi, băm nhỏ.
3. Bước 2: Bạn đặt chảo lên bếp, đổ chút dầu ăn rồi cho nửa phần đầu hành trắng đã giã nhuyễn vào phi cho thơm rồi cho bát nấm vào xào, nêm chút gia vị gồm nửa thìa cafe hạt nêm chay vào đảo đều. Khoảng 1 phút sau thì bạn tắt bếp nhé.
4. Bước 3: Đậu phụ bạn thái miếng vừa rồi khoét lỗ ở giữa miếng đậu. Phần đậu khoét ra bạn đừng vứt đi mà dầm nhuyễn rồi cho vào phần nấm xào, trộn thật đều. Sau đó, bạn đem nhồi vào phần khoảng trống của miếng đậu rồi rán vàng. Bạn lưu ý khi rán đậu đừng rán cả hai mặt mà chỉ rán một mặt để bên trong được mềm, miếng đậu bạn đừng cắt nhỏ quá kẻo bị vỡ nhé. Trong khi đun đậu trên bếp thì bạn đậy vung lại để nhân nấm được mềm.
5. Bước 4: Phần đầu hành còn lại bạn cho tiếp vào chảo, phi thơm rồi cho cà rốt + hành lá + 1 thìa cafe nước tương + nửa thìa cafe đường + 1/3 thìa cafe hạt nêm chay + chút muối, trộn đều. Bột ngô bạn hòa tan với chút nước rồi trút vào chảo cho hỗn hợp sền sệt thì tắt bếp. Bạn trang trí lại đĩa đậu rồi rưới nước sốt lên trên. Bày ra mâm và thưởng thức ngay cho nóng nhé! 😉Vào những ngày mâm cơm đã nhiều chất đạm hay đầu tháng thì các món mặn chay như đậu phụ nhồi nấm này cực kỳ thích hợp đó. Còn chần chừ gì nữa, ngay chiều nay hãy chiêu đãi cả nhà mình món ngon này nha bạn!
6. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 19, 10, 2, 757.7, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790943/veggielife/seed60/cookbook500/c500_071_dau_phu_nhoi_nam_chay.jpg', 'APPROVED_ACTIVE', 504, 'SYSTEM', DATEADD(MINUTE,-82214,@Now), DATEADD(MINUTE,-81151,@Now)),
(10133, N'Đậu Phụ Nhồi Nấm Chay', 10001, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Đậu Phụ Nhồi Nấm Chay – bản giữ cân', N'Đậu Phụ Nhồi Nấm Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-071), phù hợp người giữ cân, khoảng 509.0 kcal mỗi khẩu phần (2 khẩu phần).', N'1. làm đậu phụ nhồi nấm ngon
2. Bước 1: Bạn ngâm nấm tai mèo + nấm đông cô với nước ấm cho nấm được nở ra, cắt bỏ chân nấm, rửa lại với nước cho sạch rồi thái thành hạt lựu. Hành hoa cắt rễ, rửa sạch, để riêng phần đầu trắng của hành, giã nhuyễn, còn phần lá bạn thái nhỏ, bỏ vào bát riêng. Cà rốt rửa sạch, gọt vỏ rồi nạo sợi, băm nhỏ.
3. Bước 2: Bạn đặt chảo lên bếp, đổ chút dầu ăn rồi cho nửa phần đầu hành trắng đã giã nhuyễn vào phi cho thơm rồi cho bát nấm vào xào, nêm chút gia vị gồm nửa thìa cafe hạt nêm chay vào đảo đều. Khoảng 1 phút sau thì bạn tắt bếp nhé.
4. Bước 3: Đậu phụ bạn thái miếng vừa rồi khoét lỗ ở giữa miếng đậu. Phần đậu khoét ra bạn đừng vứt đi mà dầm nhuyễn rồi cho vào phần nấm xào, trộn thật đều. Sau đó, bạn đem nhồi vào phần khoảng trống của miếng đậu rồi rán vàng. Bạn lưu ý khi rán đậu đừng rán cả hai mặt mà chỉ rán một mặt để bên trong được mềm, miếng đậu bạn đừng cắt nhỏ quá kẻo bị vỡ nhé. Trong khi đun đậu trên bếp thì bạn đậy vung lại để nhân nấm được mềm.
5. Bước 4: Phần đầu hành còn lại bạn cho tiếp vào chảo, phi thơm rồi cho cà rốt + hành lá + 1 thìa cafe nước tương + nửa thìa cafe đường + 1/3 thìa cafe hạt nêm chay + chút muối, trộn đều. Bột ngô bạn hòa tan với chút nước rồi trút vào chảo cho hỗn hợp sền sệt thì tắt bếp. Bạn trang trí lại đĩa đậu rồi rưới nước sốt lên trên. Bày ra mâm và thưởng thức ngay cho nóng nhé! 😉Vào những ngày mâm cơm đã nhiều chất đạm hay đầu tháng thì các món mặn chay như đậu phụ nhồi nấm này cực kỳ thích hợp đó. Còn chần chừ gì nữa, ngay chiều nay hãy chiêu đãi cả nhà mình món ngon này nha bạn!
6. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 19, 10, 2, 509.0, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790943/veggielife/seed60/cookbook500/c500_071_dau_phu_nhoi_nam_chay.jpg', 'APPROVED_ACTIVE', 760, 'SYSTEM', DATEADD(MINUTE,-79913,@Now), DATEADD(MINUTE,-79201,@Now)),
(10134, N'Hủ Tíu Xào Chay Vị Me', 10001, N'Món Mì, Bún & Phở Chay', 'BUDDHIST', N'Hủ Tíu Xào Chay Vị Me – bản giữ cân', N'Hủ Tíu Xào Chay Vị Me – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-214), phù hợp người giữ cân, khoảng 612.4 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đậu hũ cắt lát mỏng, chiên giòn, cắt sợi. Hẹ cắt khúc. Nấm rơm cắt đôi. Boa rô băm nhỏ. Cà rốt cắt sợi.
2. Hủ tiếu ướp đường, bột ngọt, nước tương, để thấm. Phi thơm boa rô, đổ ra chén. Áp chảo hủ tiếu với ít dầu boa rô, trút ra dĩa.
3. Pha xốt: khuấy tan 1 muỗng đường, 1 muỗng dầu hào chay, 1/3 chén nước cốt me, 1 muỗng nước tương , 1 muỗng muối, 1 muỗng bột ngọt, 1/2m tiêu, 1 muỗng tương ớt..
4. Phi thơm boa rô, cho các loại rau củ, đậu hũ và xốt vào xào nhanh tay, cho hủ tiếu và xốt vào xóc đều rồi tắt bếp.
5. Cho hủ tiếu xào ra dĩa, trang trí ớt sợi, ngò rí, chanh. Biểu tượng cảm xúc heart Mách nhỏ:
6. Hủ tiếu áp chảo trước sẽ giúp cọng hủ tiếu săn và thơm hơn.
7. Khi ướp hủ tiếu, thêm 1 ít nước tương sẽ giúp hủ tiếu có màu đẹp và vị vừa ăn.
8. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 22, 20, 2, 612.4, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791232/veggielife/seed60/cookbook500/c500_214_hu_tiu_xao_chay_vi_me.jpg', 'APPROVED_ACTIVE', 38, 'SYSTEM', DATEADD(MINUTE,-80081,@Now), DATEADD(MINUTE,-79404,@Now)),
(10135, N'Chả Giò Chay', 10001, N'Món Cuốn Chay', 'BUDDHIST', N'Chả Giò Chay – bản giữ cân', N'Chả Giò Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-118), phù hợp người giữ cân, khoảng 1048.9 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Bước 1: Cho tất cả các nguyên liệu đã sơ chế vào âu lớn. Thêm một muỗng cafe hạt tiêu, 2 muỗng cà phê đường và 2 muỗng cafe bột ngọt cùng với 1 muỗng cafe bột gạo. Đảo cho nguyên liệu và gia vị đều.
2. Bước 2: Bánh tráng cuốn nhúng qua nước cho mềm sau đó cho phần nhân và cuốn lại. (Bạn nên hòa 3 muỗng cafe bột gạo với 20ml nước dùng để dán bánh tráng cho khỏi bị bung ra khi bạn chiên.)
3. Lưu ý: Khi cuốn nhẹ tay để chả giò sẽ nở khi chiên.
4. Bước 3: Cho nhiều dầu ăn vào chảo bắc lên bếp đun sôi thì cho từng thanh chả giò vào chiên. Khi chả giò nổi lên và vàng giòn thì vớt chả giò ra giấy thấm dầu. Chả giò chay có thể ăn với tương cà hay ăn với nước tương dùng chung với bún sẽ rất ngon ngon.
5. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 22, 20, 2, 1048.9, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791042/veggielife/seed60/cookbook500/c500_118_cha_gio_chay.jpg', 'APPROVED_ACTIVE', 808, 'SYSTEM', DATEADD(MINUTE,-76380,@Now), DATEADD(MINUTE,-75201,@Now)),
(10136, N'Chả Giò Chay', 10001, N'Món Cuốn Chay', 'BUDDHIST', N'Chả Giò Chay – bản giảm cân', N'Chả Giò Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-118), phù hợp người giảm cân, khoảng 379.6 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Bước 1: Cho tất cả các nguyên liệu đã sơ chế vào âu lớn. Thêm một muỗng cafe hạt tiêu, 2 muỗng cà phê đường và 2 muỗng cafe bột ngọt cùng với 1 muỗng cafe bột gạo. Đảo cho nguyên liệu và gia vị đều.
2. Bước 2: Bánh tráng cuốn nhúng qua nước cho mềm sau đó cho phần nhân và cuốn lại. (Bạn nên hòa 3 muỗng cafe bột gạo với 20ml nước dùng để dán bánh tráng cho khỏi bị bung ra khi bạn chiên.)
3. Lưu ý: Khi cuốn nhẹ tay để chả giò sẽ nở khi chiên.
4. Bước 3: Cho nhiều dầu ăn vào chảo bắc lên bếp đun sôi thì cho từng thanh chả giò vào chiên. Khi chả giò nổi lên và vàng giòn thì vớt chả giò ra giấy thấm dầu. Chả giò chay có thể ăn với tương cà hay ăn với nước tương dùng chung với bún sẽ rất ngon ngon.
5. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 22, 20, 2, 379.6, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791042/veggielife/seed60/cookbook500/c500_118_cha_gio_chay.jpg', 'APPROVED_ACTIVE', 368, 'SYSTEM', DATEADD(MINUTE,-77875,@Now), DATEADD(MINUTE,-75787,@Now)),
(10137, N'Đậu Phụ Hấp La Hán', 10066, N'Món Hấp & Luộc Dưỡng Sinh', 'BUDDHIST', N'Đậu Phụ Hấp La Hán – bản giảm cân', N'Đậu Phụ Hấp La Hán – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-135), phù hợp người giảm cân, khoảng 410.1 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Đậu hũ cắt miếng khoảng 3x5cm, dày 1cm.
2. Bước 2: Nấm hương: cắt bỏ chân nấm, rửa sạch với nước muối, chần sơ, vớt ra cho vào tô nước lạnh. (Nếu dùng nấm hương khô thì bạn cần ngâm nước nóng trước khoảng 5 phút cho mềm rồi sơ chế).
3. Bước 3: Thịt nguội chay cắt miếng 3x5cm.
4. Bước 4: Cải thìa luộc với nước pha 1 thìa muối, 1 thìa dầu ăn. Rau chín tới thì cho vào tô nước lạnh cùng với nấm, sau đó chẻ đôi theo chiều dọc.
5. Bước 5: Trộn đều 1.5 thìa dầu hào chay, 1 thìa đường, 1/2 thìa nước tương, 1.5 thìa nước lọc, 1/3 thìa tiêu, 1/2 thìa dầu mè và 1.5 thìa bột năng với nhau.
6. Bước 6: Xếp xen kẽ lần lượt đậu hũ, thịt nguội chay, nấm hương, gừng lên đĩa. Sau đó xếp cải thìa xung quanh, chan sốt lên trên, rắc kỷ tử, hấp cách thủy 10 phút. Nên dùng nóng, chấm kèm nước tương và ớt cắt lát.
7. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 22, 15, 2, 410.1, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791085/veggielife/seed60/cookbook500/c500_135_dau_phu_hap_la_han.jpg', 'PENDING_REVIEW', 24, 'AI', DATEADD(MINUTE,-5637,@Now), DATEADD(MINUTE,-3299,@Now)),
(10138, N'Đậu Phụ Hấp La Hán', 10146, N'Món Hấp & Luộc Dưỡng Sinh', 'BUDDHIST', N'Đậu Phụ Hấp La Hán – bản giữ cân (không cay)', N'Đậu Phụ Hấp La Hán – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-135), phù hợp người giữ cân, khoảng 496.4 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Đậu hũ cắt miếng khoảng 3x5cm, dày 1cm.
2. Bước 2: Nấm hương: cắt bỏ chân nấm, rửa sạch với nước muối, chần sơ, vớt ra cho vào tô nước lạnh. (Nếu dùng nấm hương khô thì bạn cần ngâm nước nóng trước khoảng 5 phút cho mềm rồi sơ chế).
3. Bước 3: Thịt nguội chay cắt miếng 3x5cm.
4. Bước 4: Cải thìa luộc với nước pha 1 thìa muối, 1 thìa dầu ăn. Rau chín tới thì cho vào tô nước lạnh cùng với nấm, sau đó chẻ đôi theo chiều dọc.
5. Bước 5: Trộn đều 1.5 thìa dầu hào chay, 1 thìa đường, 1/2 thìa nước tương, 1.5 thìa nước lọc, 1/3 thìa tiêu, 1/2 thìa dầu mè và 1.5 thìa bột năng với nhau.
6. Bước 6: Xếp xen kẽ lần lượt đậu hũ, thịt nguội chay, nấm hương, gừng lên đĩa. Sau đó xếp cải thìa xung quanh, chan sốt lên trên, rắc kỷ tử, hấp cách thủy 10 phút. Nên dùng nóng, chấm kèm nước tương và ớt cắt lát.
7. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ. Bản không cay: bỏ ớt, tương ớt và các gia vị cay khi nấu.', 22, 15, 2, 496.4, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791085/veggielife/seed60/cookbook500/c500_135_dau_phu_hap_la_han.jpg', 'ARCHIVED', 10, 'USER', DATEADD(MINUTE,-12346,@Now), DATEADD(MINUTE,-9563,@Now)),
(10139, N'Bánh Bột Lọc Trần Nhân Đậu Xanh', 10032, N'Món Hấp & Luộc Dưỡng Sinh', 'VEGAN', N'Bánh Bột Lọc Trần Nhân Đậu Xanh – bản giữ cân', N'Bánh Bột Lọc Trần Nhân Đậu Xanh – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-140), phù hợp người giữ cân, khoảng 213.0 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đỗ xanh đãi nhiều lần cho sạch, ngâm vào thố nước lạnh có pha một ít muối, ngâm khoảng 1-2 tiếng.
2. Sau đó hấp chín đậu, giã mịn hoặc cho vào máy xay đậu, xay thật mịn.
3. Đun nóng hai thìa dầu ăn, phi hành khô thơm, cho đỗ xanh vào xào khô, thêm vào một thìa nhỏ muối, nửa thìa nhỏ hạt nêm, đảo đều, nêm đỗ xanh hơi mặn. Xào khoảng 15 phút đến khi đậu khô.
4. Dùng tay sạch, vo đỗ xanh thành những viên tròn.
5. Hành lá rửa sạch, thái nhỏ, thêm vào bát hành một ít dầu ăn
6. Cho bát hành lá vào lò vi sóng quay khoảng 30 giây để hành chín.
7. Bột năng khô đổ ra âu, thêm bột gạo, nửa thìa nhỏ muối, trộn đều.
8. Cho từ từ hơn một bát con nước sôi nóng già, hỗn hợp bột lúc này sẽ rất nóng dùng muôi trộn đều đến khi hỗn hợp bột nguội bớt bạn dùng tay nhồi đến khi bột trở nên mịn, sờ không dính tay là đạt. Tùy theo liều lượng hút nước khác nhau của mỗi loại bột mà bạn điều chỉnh lượng nước sôi cho phù hợp.
9. Tiếp theo dùng tay nhích bột thành những viên bột tròn bằng ngón tay trỏ, ấn dẹp ra.
10. Cho viên đỗ xanh vào giữa.
11. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 19, 15, 2, 213.0, 'HARD', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791095/veggielife/seed60/cookbook500/c500_140_banh_bot_loc_tran_nhan_dau_xanh.jpg', 'ARCHIVED', 19, 'USER', DATEADD(MINUTE,-27323,@Now), DATEADD(MINUTE,-24640,@Now)),
(10140, N'Bánh Bột Lọc Trần Nhân Đậu Xanh', 10001, N'Món Hấp & Luộc Dưỡng Sinh', 'VEGAN', N'Bánh Bột Lọc Trần Nhân Đậu Xanh – bản giảm cân', N'Bánh Bột Lọc Trần Nhân Đậu Xanh – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-140), phù hợp người giảm cân, khoảng 318.2 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đỗ xanh đãi nhiều lần cho sạch, ngâm vào thố nước lạnh có pha một ít muối, ngâm khoảng 1-2 tiếng.
2. Sau đó hấp chín đậu, giã mịn hoặc cho vào máy xay đậu, xay thật mịn.
3. Đun nóng hai thìa dầu ăn, phi hành khô thơm, cho đỗ xanh vào xào khô, thêm vào một thìa nhỏ muối, nửa thìa nhỏ hạt nêm, đảo đều, nêm đỗ xanh hơi mặn. Xào khoảng 15 phút đến khi đậu khô.
4. Dùng tay sạch, vo đỗ xanh thành những viên tròn.
5. Hành lá rửa sạch, thái nhỏ, thêm vào bát hành một ít dầu ăn
6. Cho bát hành lá vào lò vi sóng quay khoảng 30 giây để hành chín.
7. Bột năng khô đổ ra âu, thêm bột gạo, nửa thìa nhỏ muối, trộn đều.
8. Cho từ từ hơn một bát con nước sôi nóng già, hỗn hợp bột lúc này sẽ rất nóng dùng muôi trộn đều đến khi hỗn hợp bột nguội bớt bạn dùng tay nhồi đến khi bột trở nên mịn, sờ không dính tay là đạt. Tùy theo liều lượng hút nước khác nhau của mỗi loại bột mà bạn điều chỉnh lượng nước sôi cho phù hợp.
9. Tiếp theo dùng tay nhích bột thành những viên bột tròn bằng ngón tay trỏ, ấn dẹp ra.
10. Cho viên đỗ xanh vào giữa.
11. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 19, 15, 2, 318.2, 'HARD', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791095/veggielife/seed60/cookbook500/c500_140_banh_bot_loc_tran_nhan_dau_xanh.jpg', 'APPROVED_ACTIVE', 620, 'SYSTEM', DATEADD(MINUTE,-81912,@Now), DATEADD(MINUTE,-81520,@Now)),
(10141, N'Bánh Bột Lọc Trần Nhân Đậu Xanh', 10049, N'Món Hấp & Luộc Dưỡng Sinh', 'VEGAN', N'Bánh Bột Lọc Trần Nhân Đậu Xanh – bản giữ cân (không cay)', N'Bánh Bột Lọc Trần Nhân Đậu Xanh – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-140), phù hợp người giữ cân, khoảng 483.7 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đỗ xanh đãi nhiều lần cho sạch, ngâm vào thố nước lạnh có pha một ít muối, ngâm khoảng 1-2 tiếng.
2. Sau đó hấp chín đậu, giã mịn hoặc cho vào máy xay đậu, xay thật mịn.
3. Đun nóng hai thìa dầu ăn, phi hành khô thơm, cho đỗ xanh vào xào khô, thêm vào một thìa nhỏ muối, nửa thìa nhỏ hạt nêm, đảo đều, nêm đỗ xanh hơi mặn. Xào khoảng 15 phút đến khi đậu khô.
4. Dùng tay sạch, vo đỗ xanh thành những viên tròn.
5. Hành lá rửa sạch, thái nhỏ, thêm vào bát hành một ít dầu ăn
6. Cho bát hành lá vào lò vi sóng quay khoảng 30 giây để hành chín.
7. Bột năng khô đổ ra âu, thêm bột gạo, nửa thìa nhỏ muối, trộn đều.
8. Cho từ từ hơn một bát con nước sôi nóng già, hỗn hợp bột lúc này sẽ rất nóng dùng muôi trộn đều đến khi hỗn hợp bột nguội bớt bạn dùng tay nhồi đến khi bột trở nên mịn, sờ không dính tay là đạt. Tùy theo liều lượng hút nước khác nhau của mỗi loại bột mà bạn điều chỉnh lượng nước sôi cho phù hợp.
9. Tiếp theo dùng tay nhích bột thành những viên bột tròn bằng ngón tay trỏ, ấn dẹp ra.
10. Cho viên đỗ xanh vào giữa.
11. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ. Bản không cay: bỏ ớt, tương ớt và các gia vị cay khi nấu.', 19, 15, 2, 483.7, 'HARD', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791095/veggielife/seed60/cookbook500/c500_140_banh_bot_loc_tran_nhan_dau_xanh.jpg', 'PENDING_REVIEW', 5, 'USER', DATEADD(MINUTE,-1329,@Now), DATEADD(MINUTE,-804,@Now)),
(10142, N'Cuốn Chay', 10110, N'Món Cuốn Chay', 'BUDDHIST', N'Cuốn Chay – bản giữ cân', N'Cuốn Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-175), phù hợp người giữ cân, khoảng 175.6 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Măng, cà rốt thái sợi nhỏ.
2. Nấm rửa nhẹ nhàng dưới vòi nước chảy, sau đó đem thái mỏng.
3. Hòa tan tất cả các nguyên liệu trong phần gia vị xốt vào một bát nhỏ.
4. Làm nóng chảo với chút dầu, trút cà rốt, măng, nấm vào xào sơ, vì các nguyên liệu này đều mau chín, riêng có măng thì lâu hơn một chút nhưng đã được luộc sơ nên bạn không cần nhiều thời gian.
5. Nêm chút xíu muối cho đậm vị, đảo lại khoảng 1 phút rồi trút ra bát để riêng.
6. Váng đậu ngâm nước ấm cho nở mềm.
7. Vớt ra cho ráo rồi trải ra mặt phẳng rộng.
8. Múc một muỗng nguyên liệu xào ở bước 2 vào chính giữa, gấp mép.
9. Từ từ gói chặt cuốn váng đậu thành khối hình chữ nhật.
10. Gói xong thì dùng cọ phết một lớp dầu ăn lên vỏ ngoài của cuốn váng đậu.
11. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 16, 10, 2, 175.6, 'HARD', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791162/veggielife/seed60/cookbook500/c500_175_cuon_chay.jpg', 'APPROVED_ACTIVE', 264, 'USER', DATEADD(MINUTE,-14101,@Now), DATEADD(MINUTE,-11543,@Now)),
(10143, N'Cuốn Chay', 10032, N'Món Cuốn Chay', 'BUDDHIST', N'Cuốn Chay – bản tăng cân', N'Cuốn Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-175), phù hợp người tăng cân, khoảng 775.8 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Măng, cà rốt thái sợi nhỏ.
2. Nấm rửa nhẹ nhàng dưới vòi nước chảy, sau đó đem thái mỏng.
3. Hòa tan tất cả các nguyên liệu trong phần gia vị xốt vào một bát nhỏ.
4. Làm nóng chảo với chút dầu, trút cà rốt, măng, nấm vào xào sơ, vì các nguyên liệu này đều mau chín, riêng có măng thì lâu hơn một chút nhưng đã được luộc sơ nên bạn không cần nhiều thời gian.
5. Nêm chút xíu muối cho đậm vị, đảo lại khoảng 1 phút rồi trút ra bát để riêng.
6. Váng đậu ngâm nước ấm cho nở mềm.
7. Vớt ra cho ráo rồi trải ra mặt phẳng rộng.
8. Múc một muỗng nguyên liệu xào ở bước 2 vào chính giữa, gấp mép.
9. Từ từ gói chặt cuốn váng đậu thành khối hình chữ nhật.
10. Gói xong thì dùng cọ phết một lớp dầu ăn lên vỏ ngoài của cuốn váng đậu.
11. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 16, 10, 2, 775.8, 'HARD', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791162/veggielife/seed60/cookbook500/c500_175_cuon_chay.jpg', 'APPROVED_ACTIVE', 63, 'USER', DATEADD(MINUTE,-46355,@Now), DATEADD(MINUTE,-45204,@Now)),
(10144, N'Đậu Hủ Dồn Nhân Rau Quả', 10001, N'Món Xào Chay Thanh Vị', 'BUDDHIST', N'Đậu Hủ Dồn Nhân Rau Quả – bản tăng cân', N'Đậu Hủ Dồn Nhân Rau Quả – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-021), phù hợp người tăng cân, khoảng 742.9 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đậu hũ các bạn cắt đôi để sẵn Các nguyên liệu còn lại bạn băm nhỏ chung với nhau trộn gia vị vừa miệng...sau đó cho một ít bột mì vào nhân để kết dính.
2. Nhét nguyên liệu vào đậu hũ.
3. Cà chua chúng ta sẽ làm sốt (Để có sốt cà chua ngon các bạn nên xịt một ít tương cà và tương ớt cholimex vào sốt cà tươi)...sau đó bỏ đậu hũ đã dồn vào sốt cà chua sốt cho đến khi thấm đều.
4. Bước còn lại là thưởng thức với cơm nóng...
5. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 16, 20, 2, 742.9, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790841/veggielife/seed60/cookbook500/c500_021_dau_hu_don_nhan_rau_qua.jpg', 'APPROVED_ACTIVE', 899, 'SYSTEM', DATEADD(MINUTE,-67645,@Now), DATEADD(MINUTE,-67535,@Now)),
(10145, N'Nấm Đông Cô Cải Thìa Xốt Rau Củ', 10147, N'Món Xào Chay Thanh Vị', 'VEGAN', N'Nấm Đông Cô Cải Thìa Xốt Rau Củ – bản giữ cân', N'Nấm Đông Cô Cải Thìa Xốt Rau Củ – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-101), phù hợp người giữ cân, khoảng 178.5 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Nấm đông cô xắt bỏ gốc, ngâm qua nước muối pha loãng, vớt ra để ráo
2. Cải thìa rửa sạch, để ráo
3. Cà rốt gọt vỏ. Hành tây lột vỏ khô bên ngoài. Ớt chuông, cà chua lạng bỏ hạt. Tất cả xắt hạt lựu với ½ nấm đông cô. Ngò rí nhặt rửa sạch, xắt nhuyễn
4. Phần nấm còn lại cho vào nồi hấp chín. Cải thìa luộc chín, ngâm qua nước đá lạnh cho xanh và giòn, vớt ra để ráo nước
5. Làm nóng dầu ăn, phi thơm hành boa-rô, cho các loại rau củ vào xào khoảng 2 phút, nêm hạt nêm chay, dầu hào, đường, nước tương vừa ăn, vặn lửa lớn, cho nấm hấp, rau cải thìa vào đảo nhanh tay khoảng 30 giây, tắt bếp, rắc ngò rí lên trên
6. Cho nấm đông cô, cải thìa xốt rau củ ra đĩa, ăn nóng rất ngon.
7. Mách nhỏ**
8. Để dễ bỏ hạt cà chua, trụng cà vào nước thật sôi trong 1 phút, vớt ra xắt đôi, cầm nửa quả trong bàn tay và bóp nhẹ là sẽ ra hết hạt.
9. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 22, 10, 2, 178.5, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791001/veggielife/seed60/cookbook500/c500_101_nam_dong_co_cai_thia_xot_rau_cu.jpg', 'PENDING_REVIEW', 9, 'USER', DATEADD(MINUTE,-22291,@Now), DATEADD(MINUTE,-20290,@Now)),
(10146, N'Mướp Đắng Kho Chay', 10101, N'Món Kho Chay Đậm Đà', 'VEGAN', N'Mướp Đắng Kho Chay – bản tăng cân', N'Mướp Đắng Kho Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-124), phù hợp người tăng cân, khoảng 707.5 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Mướp đắng rửa sạch, cắt khúc dài khoảng 1.5cm, bỏ ruột. Ớt đập dập. Nấm mèo ngâm nở, thái sợi, băm nhỏ, miến ngâm mềm, cắt khúc 2cm.
2. Bước 2: Đậu phụ chần qua nước sôi, cho vào vải mùng vắt ráo nước, tán nhuyễn.
3. Bước 3: Trộn chung đậu phụ đã tán nhuyễn với nấm mèo, miến, 1 thìa bột năng, 2 thìa hành boa-ro phi vàng, 1/2 thìa muối, 1/2 thìa đường, 1/2 thìa hạt tiêu, 1/2 thìa bột ngọt, nhồi nhân vào mướp đắng.
4. Bước 4: Pha hỗn hợp 2 thìa nước tương, 1 thìa đường, 1 thìa bột ngọt, 1/5 thìa muối.
5. Bước 5: Chiên áp chảo mướp đắng đến khi rám mặt, rồi cho hỗn hợp nước tương vào, kho 2 phút cho thấm vị.
6. Bước 6: Thêm nước dừa, ớt đập dập rồi kho tiếp 10 phút cho tới khi nước xâm xấp là được. Khi ăn thì rưới thêm nước kho và boaro phi.
7. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 22, 12, 2, 707.5, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791058/veggielife/seed60/cookbook500/c500_124_muop_dang_kho_chay.jpg', 'ARCHIVED', 16, 'USER', DATEADD(MINUTE,-10962,@Now), DATEADD(MINUTE,-10824,@Now)),
(10147, N'Mướp Đắng Kho Chay', 10001, N'Món Kho Chay Đậm Đà', 'VEGAN', N'Mướp Đắng Kho Chay – bản giảm cân', N'Mướp Đắng Kho Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-124), phù hợp người giảm cân, khoảng 391.8 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Mướp đắng rửa sạch, cắt khúc dài khoảng 1.5cm, bỏ ruột. Ớt đập dập. Nấm mèo ngâm nở, thái sợi, băm nhỏ, miến ngâm mềm, cắt khúc 2cm.
2. Bước 2: Đậu phụ chần qua nước sôi, cho vào vải mùng vắt ráo nước, tán nhuyễn.
3. Bước 3: Trộn chung đậu phụ đã tán nhuyễn với nấm mèo, miến, 1 thìa bột năng, 2 thìa hành boa-ro phi vàng, 1/2 thìa muối, 1/2 thìa đường, 1/2 thìa hạt tiêu, 1/2 thìa bột ngọt, nhồi nhân vào mướp đắng.
4. Bước 4: Pha hỗn hợp 2 thìa nước tương, 1 thìa đường, 1 thìa bột ngọt, 1/5 thìa muối.
5. Bước 5: Chiên áp chảo mướp đắng đến khi rám mặt, rồi cho hỗn hợp nước tương vào, kho 2 phút cho thấm vị.
6. Bước 6: Thêm nước dừa, ớt đập dập rồi kho tiếp 10 phút cho tới khi nước xâm xấp là được. Khi ăn thì rưới thêm nước kho và boaro phi.
7. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 22, 12, 2, 391.8, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791058/veggielife/seed60/cookbook500/c500_124_muop_dang_kho_chay.jpg', 'APPROVED_ACTIVE', 978, 'SYSTEM', DATEADD(MINUTE,-67542,@Now), DATEADD(MINUTE,-64678,@Now)),
(10148, N'Mướp Đắng Kho Chay', 10001, N'Món Kho Chay Đậm Đà', 'VEGAN', N'Mướp Đắng Kho Chay – bản giữ cân', N'Mướp Đắng Kho Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-124), phù hợp người giữ cân, khoảng 541.0 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Mướp đắng rửa sạch, cắt khúc dài khoảng 1.5cm, bỏ ruột. Ớt đập dập. Nấm mèo ngâm nở, thái sợi, băm nhỏ, miến ngâm mềm, cắt khúc 2cm.
2. Bước 2: Đậu phụ chần qua nước sôi, cho vào vải mùng vắt ráo nước, tán nhuyễn.
3. Bước 3: Trộn chung đậu phụ đã tán nhuyễn với nấm mèo, miến, 1 thìa bột năng, 2 thìa hành boa-ro phi vàng, 1/2 thìa muối, 1/2 thìa đường, 1/2 thìa hạt tiêu, 1/2 thìa bột ngọt, nhồi nhân vào mướp đắng.
4. Bước 4: Pha hỗn hợp 2 thìa nước tương, 1 thìa đường, 1 thìa bột ngọt, 1/5 thìa muối.
5. Bước 5: Chiên áp chảo mướp đắng đến khi rám mặt, rồi cho hỗn hợp nước tương vào, kho 2 phút cho thấm vị.
6. Bước 6: Thêm nước dừa, ớt đập dập rồi kho tiếp 10 phút cho tới khi nước xâm xấp là được. Khi ăn thì rưới thêm nước kho và boaro phi.
7. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 22, 12, 2, 541.0, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791058/veggielife/seed60/cookbook500/c500_124_muop_dang_kho_chay.jpg', 'APPROVED_ACTIVE', 516, 'SYSTEM', DATEADD(MINUTE,-75398,@Now), DATEADD(MINUTE,-74690,@Now)),
(10149, N'Nem Chay', 10125, N'Món Cuốn Chay', 'VEGAN', N'Nem Chay – bản giảm cân', N'Nem Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-128), phù hợp người giảm cân, khoảng 397.8 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Bước 1: Cho các nguyên liệu lần lượt vào bát rồi cho miến vào, thêm chút xíu gia vị và trộn đều.
2. Bước 2: Chải bánh đa nem lên mặt thớt và cho ít nhân lên trên. Cuộn tròn bánh đa nem lại. Làm như vậy cho đến hết phần nhân.
3. Bước 3: Đun sôi dầu trong chảo, thả nem vào và rán vàng hai mặt (Khi thấy vỏ nem chuyển màu vàng thì phần nhân bên trong đã chín. Bạn không nên rán kỹ nem để tránh bị khô). Gắp ra đĩa và ăn với xì dầu.
4. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 16, 20, 2, 397.8, 'EASY', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791069/veggielife/seed60/cookbook500/c500_128_nem_chay.jpg', 'PENDING_REVIEW', 7, 'AI', DATEADD(MINUTE,-10015,@Now), DATEADD(MINUTE,-7348,@Now)),
(10150, N'Nem Chay', 10001, N'Món Cuốn Chay', 'VEGAN', N'Nem Chay – bản tăng cân', N'Nem Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-128), phù hợp người tăng cân, khoảng 781.1 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Bước 1: Cho các nguyên liệu lần lượt vào bát rồi cho miến vào, thêm chút xíu gia vị và trộn đều.
2. Bước 2: Chải bánh đa nem lên mặt thớt và cho ít nhân lên trên. Cuộn tròn bánh đa nem lại. Làm như vậy cho đến hết phần nhân.
3. Bước 3: Đun sôi dầu trong chảo, thả nem vào và rán vàng hai mặt (Khi thấy vỏ nem chuyển màu vàng thì phần nhân bên trong đã chín. Bạn không nên rán kỹ nem để tránh bị khô). Gắp ra đĩa và ăn với xì dầu.
4. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 16, 20, 2, 781.1, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791069/veggielife/seed60/cookbook500/c500_128_nem_chay.jpg', 'APPROVED_ACTIVE', 678, 'SYSTEM', DATEADD(MINUTE,-68068,@Now), DATEADD(MINUTE,-67508,@Now)),
(10151, N'Nem Chay', 10001, N'Món Cuốn Chay', 'VEGAN', N'Nem Chay – bản giữ cân', N'Nem Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-128), phù hợp người giữ cân, khoảng 524.8 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Bước 1: Cho các nguyên liệu lần lượt vào bát rồi cho miến vào, thêm chút xíu gia vị và trộn đều.
2. Bước 2: Chải bánh đa nem lên mặt thớt và cho ít nhân lên trên. Cuộn tròn bánh đa nem lại. Làm như vậy cho đến hết phần nhân.
3. Bước 3: Đun sôi dầu trong chảo, thả nem vào và rán vàng hai mặt (Khi thấy vỏ nem chuyển màu vàng thì phần nhân bên trong đã chín. Bạn không nên rán kỹ nem để tránh bị khô). Gắp ra đĩa và ăn với xì dầu.
4. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 16, 20, 2, 524.8, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791069/veggielife/seed60/cookbook500/c500_128_nem_chay.jpg', 'APPROVED_ACTIVE', 1671, 'SYSTEM', DATEADD(MINUTE,-68692,@Now), DATEADD(MINUTE,-67521,@Now)),
(10152, N'Bánh Bột Lọc Chay', 10001, N'Món Hấp & Luộc Dưỡng Sinh', 'VEGAN', N'Bánh Bột Lọc Chay – bản giữ cân', N'Bánh Bột Lọc Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-107), phù hợp người giữ cân, khoảng 213.0 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đỗ xanh đãi nhiều lần cho sạch, ngâm vào thố nước lạnh có pha một ít muối, ngâm khoảng 1-2 tiếng.
2. Sau đó hấp chín đậu, giã mịn hoặc cho vào máy xay đậu, xay thật mịn.
3. Đun nóng hai thìa dầu ăn, phi hành khô thơm, cho đỗ xanh vào xào khô, thêm vào một thìa nhỏ muối, nửa thìa nhỏ hạt nêm, đảo đều, nêm đỗ xanh hơi mặn. Xào khoảng 15 phút đến khi đậu khô.
4. Dùng tay sạch, vo đỗ xanh thành những viên tròn.
5. Hành lá rửa sạch, thái nhỏ, thêm vào bát hành một ít dầu ăn
6. Cho bát hành lá vào lò vi sóng quay khoảng 30 giây để hành chín.
7. Bột năng khô đổ ra âu, thêm bột gạo, nửa thìa nhỏ muối, trộn đều.
8. Cho từ từ hơn một bát con nước sôi nóng già, hỗn hợp bột lúc này sẽ rất nóng dùng muôi trộn đều đến khi hỗn hợp bột nguội bớt bạn dùng tay nhồi đến khi bột trở nên mịn, sờ không dính tay là đạt. Tùy theo liều lượng hút nước khác nhau của mỗi loại bột mà bạn điều chỉnh lượng nước sôi cho phù hợp.
9. Tiếp theo dùng tay nhích bột thành những viên bột tròn bằng ngón tay trỏ, ấn dẹp ra.
10. Cho viên đỗ xanh vào giữa.
11. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.', 19, 15, 2, 213.0, 'HARD', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791014/veggielife/seed60/cookbook500/c500_107_banh_bot_loc_chay.jpg', 'APPROVED_ACTIVE', 1156, 'SYSTEM', DATEADD(MINUTE,-79153,@Now), DATEADD(MINUTE,-78392,@Now)),
(10153, N'Bánh Bột Lọc Chay', 10001, N'Món Hấp & Luộc Dưỡng Sinh', 'VEGAN', N'Bánh Bột Lọc Chay – bản tăng cân', N'Bánh Bột Lọc Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-107), phù hợp người tăng cân, khoảng 781.6 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đỗ xanh đãi nhiều lần cho sạch, ngâm vào thố nước lạnh có pha một ít muối, ngâm khoảng 1-2 tiếng.
2. Sau đó hấp chín đậu, giã mịn hoặc cho vào máy xay đậu, xay thật mịn.
3. Đun nóng hai thìa dầu ăn, phi hành khô thơm, cho đỗ xanh vào xào khô, thêm vào một thìa nhỏ muối, nửa thìa nhỏ hạt nêm, đảo đều, nêm đỗ xanh hơi mặn. Xào khoảng 15 phút đến khi đậu khô.
4. Dùng tay sạch, vo đỗ xanh thành những viên tròn.
5. Hành lá rửa sạch, thái nhỏ, thêm vào bát hành một ít dầu ăn
6. Cho bát hành lá vào lò vi sóng quay khoảng 30 giây để hành chín.
7. Bột năng khô đổ ra âu, thêm bột gạo, nửa thìa nhỏ muối, trộn đều.
8. Cho từ từ hơn một bát con nước sôi nóng già, hỗn hợp bột lúc này sẽ rất nóng dùng muôi trộn đều đến khi hỗn hợp bột nguội bớt bạn dùng tay nhồi đến khi bột trở nên mịn, sờ không dính tay là đạt. Tùy theo liều lượng hút nước khác nhau của mỗi loại bột mà bạn điều chỉnh lượng nước sôi cho phù hợp.
9. Tiếp theo dùng tay nhích bột thành những viên bột tròn bằng ngón tay trỏ, ấn dẹp ra.
10. Cho viên đỗ xanh vào giữa.
11. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang.', 19, 15, 2, 781.6, 'HARD', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791014/veggielife/seed60/cookbook500/c500_107_banh_bot_loc_chay.jpg', 'APPROVED_ACTIVE', 163, 'SYSTEM', DATEADD(MINUTE,-67222,@Now), DATEADD(MINUTE,-64520,@Now)),
(10154, N'Bánh Bột Lọc Chay', 10001, N'Món Hấp & Luộc Dưỡng Sinh', 'VEGAN', N'Bánh Bột Lọc Chay – bản giảm cân', N'Bánh Bột Lọc Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-107), phù hợp người giảm cân, khoảng 318.2 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đỗ xanh đãi nhiều lần cho sạch, ngâm vào thố nước lạnh có pha một ít muối, ngâm khoảng 1-2 tiếng.
2. Sau đó hấp chín đậu, giã mịn hoặc cho vào máy xay đậu, xay thật mịn.
3. Đun nóng hai thìa dầu ăn, phi hành khô thơm, cho đỗ xanh vào xào khô, thêm vào một thìa nhỏ muối, nửa thìa nhỏ hạt nêm, đảo đều, nêm đỗ xanh hơi mặn. Xào khoảng 15 phút đến khi đậu khô.
4. Dùng tay sạch, vo đỗ xanh thành những viên tròn.
5. Hành lá rửa sạch, thái nhỏ, thêm vào bát hành một ít dầu ăn
6. Cho bát hành lá vào lò vi sóng quay khoảng 30 giây để hành chín.
7. Bột năng khô đổ ra âu, thêm bột gạo, nửa thìa nhỏ muối, trộn đều.
8. Cho từ từ hơn một bát con nước sôi nóng già, hỗn hợp bột lúc này sẽ rất nóng dùng muôi trộn đều đến khi hỗn hợp bột nguội bớt bạn dùng tay nhồi đến khi bột trở nên mịn, sờ không dính tay là đạt. Tùy theo liều lượng hút nước khác nhau của mỗi loại bột mà bạn điều chỉnh lượng nước sôi cho phù hợp.
9. Tiếp theo dùng tay nhích bột thành những viên bột tròn bằng ngón tay trỏ, ấn dẹp ra.
10. Cho viên đỗ xanh vào giữa.
11. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.', 19, 15, 2, 318.2, 'HARD', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791014/veggielife/seed60/cookbook500/c500_107_banh_bot_loc_chay.jpg', 'APPROVED_ACTIVE', 945, 'SYSTEM', DATEADD(MINUTE,-82306,@Now), DATEADD(MINUTE,-79808,@Now)),
(10155, N'Đậu Hủ Sốt Cay', 10001, N'Món Xào Chay Thanh Vị', 'BUDDHIST', N'Đậu Hủ Sốt Cay – bản giữ cân (không hành tỏi)', N'Đậu Hủ Sốt Cay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-159), phù hợp người giữ cân, khoảng 504.5 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đậu hũ rửa sạch cắt miếng vuông mỏng. Bột ngô bỏ ra đĩa, gắp từng miếng đậu tẩm qua bột rồi cho vào chảo dầu nóng rán vàng, bột chỉ cần bám sơ quanh miếng đậu là được.
2. Khi rán đậu chú ý để lửa nhỏ để tránh bột bị cháy. Đậu rán chín vàng hai mặt thì cho ra đĩa, để sang một bên.
3. Cho một chút nước nếu chảo quá khô và để tránh cà chua bị cháy.
4. Tiếp theo cho đậu rán vào chảo, thêm nửa chén nước, rồi mở lửa to đun sôi, nêm nếm lại bột canh cho vừa ăn. Nhỏ lửa âm ỉ cho đậu ngấm gia vị.
5. Đun đến khi nước cạn bớt là được. Khi đun tránh dùng đũa đảo nhiều, đậu sẽ bị vỡ. Xúc đậu ra bát to, rắc hạt tiêu lên trên, món chay ăn nóng với cơm.
6. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 19, 20, 2, 504.5, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791131/veggielife/seed60/cookbook500/c500_159_dau_hu_sot_cay.jpg', 'APPROVED_ACTIVE', 1668, 'SYSTEM', DATEADD(MINUTE,-76655,@Now), DATEADD(MINUTE,-75768,@Now)),
(10156, N'Đậu Xào Rau Củ', 10001, N'Món Xào Chay Thanh Vị', 'BUDDHIST', N'Đậu Xào Rau Củ – bản giữ cân (không hành tỏi)', N'Đậu Xào Rau Củ – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-017), phù hợp người giữ cân, khoảng 530.0 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Ngâm nấm hương trong nước ấm cho mềm ra, chia làm 3 phần bằng nhau rồi lấy 1 phần ra cắt miếng vừa ăn.
2. Rán vàng đậu rồi gắp ra để ráo dầu. Cho nấm hương vào chảo đảo qua, sau đó đến ớt chuông và đậu phụ đã rán. Nêm nếm với xì dầu và gia vị sao cho vừa miệng.
3. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 13, 20, 2, 530.0, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790832/veggielife/seed60/cookbook500/c500_017_dau_xao_rau_cu.jpg', 'APPROVED_ACTIVE', 1284, 'SYSTEM', DATEADD(MINUTE,-81792,@Now), DATEADD(MINUTE,-79429,@Now)),
(10157, N'Canh Nấm Đậu Hũ', 10101, N'Món Canh & Súp Bổ Dưỡng', 'BUDDHIST', N'Canh Nấm Đậu Hũ – bản giảm cân (không hành tỏi)', N'Canh Nấm Đậu Hũ – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-018), phù hợp người giảm cân, khoảng 355.0 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Ngâm rửa sạch nấm hương và nấm kim châm, sau đó cắt thành miếng nhỏ vừa ăn. Cắt đậu phụ non thành miếng.
2. Đun một nồi nước sôi, cho đậu phụ vào nấu, sau đó thêm nấm vào. Đun trong 5-10 phút cho đến khi nấm và đậu phụ chín hẳn thì nêm nếm gia vị và thêm hành lá vào.
3. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 13, 10, 2, 355.0, 'EASY', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790834/veggielife/seed60/cookbook500/c500_018_canh_nam_dau_hu.jpg', 'PENDING_REVIEW', 6, 'USER', DATEADD(MINUTE,-19674,@Now), DATEADD(MINUTE,-16692,@Now)),
(10158, N'Đậu Phụ Cay Sốt Nấm', 10039, N'Món Xào Chay Thanh Vị', 'BUDDHIST', N'Đậu Phụ Cay Sốt Nấm – bản giảm cân (không hành tỏi)', N'Đậu Phụ Cay Sốt Nấm – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-117), phù hợp người giảm cân, khoảng 375.4 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Đậu phụ rửa sạch cắt miếng vuông mỏng. Bột ngô bỏ ra đĩa, gắp từng miếng đậu tẩm qua bột rồi cho vào chảo dầu nóng rán vàng, bột chỉ cần bám sơ quanh miếng đậu là được (Khi rán đậu chú ý để lửa nhỏ để tránh bột bị cháy). Đậu rán chín vàng hai mặt thì cho ra đĩa, để sang một bên.
2. Cho một chút nước nếu chảo quá khô và để tránh cà chua bị cháy.
3. Bước 4: Tiếp theo cho đậu rán vào chảo, thêm nửa chén nước, rồi mở lửa to đun sôi, nêm nếm lại bột canh cho vừa ăn. Vặn nhỏ lửa âm ỉ cho đậu ngấm gia vị.
4. Bước 5: Đun đến khi nước cạn bớt là được. Khi đun tránh dùng đũa đảo nhiều, đậu sẽ bị vỡ.
5. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 19, 20, 2, 375.4, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791040/veggielife/seed60/cookbook500/c500_117_dau_phu_cay_sot_nam.jpg', 'ARCHIVED', 13, 'USER', DATEADD(MINUTE,-20635,@Now), DATEADD(MINUTE,-18946,@Now)),
(10159, N'Bún Huế Chay', 10001, N'Món Mì, Bún & Phở Chay', 'BUDDHIST', N'Bún Huế Chay – bản giữ cân (không hành tỏi)', N'Bún Huế Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-053), phù hợp người giữ cân, khoảng 585.8 kcal mỗi khẩu phần (2 khẩu phần).', N'1. làm bún huế chay ngon hấp dãn :
2. Bước 1 : Rau củ quả bạn đem rửa sạch, sau đó đem cắt khúc.
3. Cho rau củ quả vào nồi, rồi sau đó đem cho thêm nước, cùng với một ít muối vào đem hầm cho ra nước ngọt, sau đó đem hầm cho đến khi hỗn hợp mềm nhé
4. Sau dó đem bỏ lấy nước dùng chứ không lấy cái nhé ( chỉ sử dụng lại củ cải )
5. Bước 2 : Bắc nồi nước dùng lên trên bếp ( trong nồi còn củ cải ). Mình sử hay sử dụng gia vị bột nêm chay, bột ngọt, đường phèn ( tùy theo khẩu vị gia đình bạn nhé ) và cùng với một ít màu dầu điều vào để cho món ăn ngon hấp dẫn hơn
6. Bước 3 : Đậu hũ bạn đem cắt nhỏ, sau đó đem chiên vàng khi nào ăn thì cho vào nồi
7. Bước 4 : Làm sốt nấm : Đem cho thêm ít dầu ăn vào nồi, sau đó đem đun nóng. Sau đó nêm gia vị hơi đậm đà, rồi đun với lửa liu riu cho thấm đến khi mà nước còn sền sệt. Rồi sau đó bạn đem múc sốt nấm ra tô
8. Bạn Chuẩn bị bún ngon (dùng bún cọng lớn) cùng với các nguyên liệu ăn kèm như sa tế, rau thơm (bắp chuối, rau muống bào, giá, rau thơm).
9. Bún chần nóng, cho vào tô, vài miếng dứa, đậu hũ, củ cải trắng rồi chan nước dùng lên tô bún. Món bún Huế chay thơm ngon, hấp dẫn này đảm bảo cả nhà sẽ thích mê cho mà xem.Bạn hãy chia sẻ món ăn chay này cho mọi người cùng biết nhé.
10. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 22, 20, 2, 585.8, 'HARD', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790908/veggielife/seed60/cookbook500/c500_053b_bun_hue_chay.jpg', 'APPROVED_ACTIVE', 1683, 'SYSTEM', DATEADD(MINUTE,-78011,@Now), DATEADD(MINUTE,-75264,@Now)),
(10160, N'Rau Củ Xào Thập Cẩm', 10028, N'Món Xào Chay Thanh Vị', 'BUDDHIST', N'Rau Củ Xào Thập Cẩm – bản tăng cân (không hành tỏi)', N'Rau Củ Xào Thập Cẩm – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-008), phù hợp người tăng cân, khoảng 757.5 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Thêm đậu Hà Lan, bông cải, ớt chuông vào xào thêm 5-7 phút cho đến khi rau củ vừa chín.
2. Rắc hạt mè lên, tắt bếp.
3. Dọn rau củ xào thập cẩm ăn kèm cơm trắng.
4. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 19, 10, 2, 757.5, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790813/veggielife/seed60/cookbook500/c500_008_rau_cu_xao_thap_cam.jpg', 'PENDING_REVIEW', 6, 'USER', DATEADD(MINUTE,-21857,@Now), DATEADD(MINUTE,-21387,@Now)),
(10161, N'Bao Bố Đậu Phụ Chay', 10001, N'Món Hấp & Luộc Dưỡng Sinh', 'BUDDHIST', N'Bao Bố Đậu Phụ Chay – bản giữ cân (không hành tỏi)', N'Bao Bố Đậu Phụ Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-120), phù hợp người giữ cân, khoảng 496.2 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Đậu phụ chiên rọc đường nhỏ phía cạnh rồi dùng muỗng múc hết phần ruột trắng bên trong.
2. Bước 3: Cho vào nồi hấp thêm 5 phút.
3. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 19, 10, 2, 496.2, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791046/veggielife/seed60/cookbook500/c500_120_bao_bo_dau_phu_chay.jpg', 'APPROVED_ACTIVE', 1331, 'SYSTEM', DATEADD(MINUTE,-69098,@Now), DATEADD(MINUTE,-68788,@Now)),
(10162, N'Đậu Phụ Chua Ngọt', 10001, N'Món Xào Chay Thanh Vị', 'BUDDHIST', N'Đậu Phụ Chua Ngọt – bản giảm cân (không hành tỏi)', N'Đậu Phụ Chua Ngọt – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-010), phù hợp người giảm cân, khoảng 352.4 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1. Ướp thịt chay băm với 1/2 thìa cà phê xì dầu và 1/2 thìa cà phê bột bắp trong khoảng nửa giờ.
2. 2. Đậu phụ rửa sạch nhẹ nhàng, dùng giấy thấm thấm khô.
3. 3. Đổ dầu vào chảo đun nóng, rán vàng đậu phụ, rồi cho ra đĩa.
4. 4. Tiếp tục đổ thêm dầu vào chảo, phi thơm bơ rô, cho thịt chay vào đảo đều.
5. 5. Trộn đều tất cả gia vị và đậu phụ với nhau. Đổ tất cả vào thịt chay đã xào, đậy nắp, đun nhỏ lửa. Khuấy đều và nấu cho tới khi nước xốt đậm đặc.
6. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 16, 20, 2, 352.4, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790816/veggielife/seed60/cookbook500/c500_010_dau_phu_chua_ngot.jpg', 'APPROVED_ACTIVE', 1725, 'SYSTEM', DATEADD(MINUTE,-77021,@Now), DATEADD(MINUTE,-75001,@Now)),
(10163, N'Rau Xào Thập Cẩm', 10001, N'Món Xào Chay Thanh Vị', 'BUDDHIST', N'Rau Xào Thập Cẩm – bản giữ cân (không hành tỏi)', N'Rau Xào Thập Cẩm – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-083), phù hợp người giữ cân, khoảng 456.8 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đầu tiên, bạn làm nóng dầu ô liu trong một chảo trên ngọn lửa vừa.
2. Sau đó, bạn cho thêm chút nước lọc vào rồi đun sôi và nêm mắm muối vừa ăn.
3. Đun tiếp đến khi rau chín và nước cạn bớt là được.
4. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 16, 10, 2, 456.8, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790966/veggielife/seed60/cookbook500/c500_083_rau_xao_thap_cam.jpg', 'APPROVED_ACTIVE', 893, 'SYSTEM', DATEADD(MINUTE,-67267,@Now), DATEADD(MINUTE,-65293,@Now)),
(10164, N'Khổ Qua Kho Chay', 10001, N'Món Kho Chay Đậm Đà', 'BUDDHIST', N'Khổ Qua Kho Chay – bản giữ cân (không hành tỏi)', N'Khổ Qua Kho Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-039), phù hợp người giữ cân, khoảng 514.0 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1. Sơ chế
2. Khổ qua rửa sạch, cắt khúc1.5cm, bỏ ruột. Ớt hiểm đập dập. Nấm mèo ngâm nở, thái sợi, băm nhỏ, bún tàu ngâm mềm, cắt khúc 2cm.
3. Pha hỗn hợp 2M nước tương LISA, 1M đường, 1m bột ngọt AJI-NO-MOTO®, 1/5m muối.
4. Chiên áp chảo khổ qua đến khi rám mặt, rồi cho hỗn hợp nước tương vào, kho 2 phút cho thấm vị, thêm nước dừa, ớt hiểm đập dập rồi kho tiếp 10 phút cho tới khi nước xâm xấp là được.
5. 3.
6. Mách nhỏ**
7. Chiên khổ qua giúp khổ qua săn mặt, không bị bở, nhân kết dính. Vị cay trong món ăn làm giảm cảm giác đắng của khổ qua.
8. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 22, 12, 2, 514.0, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790880/veggielife/seed60/cookbook500/c500_039_kho_qua_kho_chay.jpg', 'APPROVED_ACTIVE', 1486, 'SYSTEM', DATEADD(MINUTE,-69015,@Now), DATEADD(MINUTE,-68448,@Now)),
(10165, N'Chả Lá Lốt Chay Đơn Giản', 10001, N'Món Chiên & Rán Giòn Rụm', 'BUDDHIST', N'Chả Lá Lốt Chay Đơn Giản – bản tăng cân (không hành tỏi)', N'Chả Lá Lốt Chay Đơn Giản – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-048), phù hợp người tăng cân, khoảng 712.9 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đầu tiên, bạn ngâm mộc nhĩ vào nước nóng cho nở, rồi cắt chân rửa sạch, thái sợi.
2. Lưu ý, cần để lá lốt thật ráo nước, kẻo khi rán sẽ bị bắn dầu.
3. Giờ bạn cho phần nhân đã trộn vào bên trong lá lốt rồi cuộn lại.
4. Bạn làm như thế đến khi hết nhân thì thôi.
5. Bắc chảo lên bếp, cho dầu ăn vào đun sôi, sau đó, thả từng cái chả vào rán ở lửa nhỏ.
6. Không nên để lửa to kẻo chả sẽ cháy.
7. Khi nào thấy bề mặt lá lốt ngả màu xanh đậm là chả đã chín.
8. Cuối cùng, chỉ cần gắp chả lá lốt chay ra đĩa ròi bày lên mâm cỗ là xong.
9. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 16, 20, 2, 712.9, 'HARD', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790896/veggielife/seed60/cookbook500/c500_048_cha_la_lot_chay_don_gian.jpg', 'APPROVED_ACTIVE', 386, 'SYSTEM', DATEADD(MINUTE,-69558,@Now), DATEADD(MINUTE,-67282,@Now)),
(10166, N'Nấm Đông Cô Cải Thìa Xốt Rau Củ', 10001, N'Món Xào Chay Thanh Vị', 'BUDDHIST', N'Nấm Đông Cô Cải Thìa Xốt Rau Củ – bản tăng cân (không hành tỏi)', N'Nấm Đông Cô Cải Thìa Xốt Rau Củ – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-101), phù hợp người tăng cân, khoảng 761.4 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Nấm đông cô xắt bỏ gốc, ngâm qua nước muối pha loãng, vớt ra để ráo
2. Cải thìa rửa sạch, để ráo
3. Cà rốt gọt vỏ. Ớt chuông, cà chua lạng bỏ hạt. Tất cả xắt hạt lựu với ½ nấm đông cô. Ngò rí nhặt rửa sạch, xắt nhuyễn
4. Phần nấm còn lại cho vào nồi hấp chín. Cải thìa luộc chín, ngâm qua nước đá lạnh cho xanh và giòn, vớt ra để ráo nước
5. Cho nấm đông cô, cải thìa xốt rau củ ra đĩa, ăn nóng rất ngon.
6. Mách nhỏ**
7. Để dễ bỏ hạt cà chua, trụng cà vào nước thật sôi trong 1 phút, vớt ra xắt đôi, cầm nửa quả trong bàn tay và bóp nhẹ là sẽ ra hết hạt.
8. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 22, 10, 2, 761.4, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791001/veggielife/seed60/cookbook500/c500_101_nam_dong_co_cai_thia_xot_rau_cu.jpg', 'APPROVED_ACTIVE', 1585, 'SYSTEM', DATEADD(MINUTE,-75129,@Now), DATEADD(MINUTE,-73584,@Now)),
(10167, N'Rau Củ Kho Chay', 10001, N'Món Kho Chay Đậm Đà', 'BUDDHIST', N'Rau Củ Kho Chay – bản tăng cân (không hành tỏi)', N'Rau Củ Kho Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-009), phù hợp người tăng cân, khoảng 746.7 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Rửa sạch các loại củ, nấm Cà rốt, cải trắng, cà tím cắt khúc vừa ăn Đậu hủ chiên cắt khúc vừa ăn
2. Để rau củ có vị thơm hơn, bạn có thể chiên sơ qua tất cả, Tuy nhiên để món ăn thanh đạm ít dầu mở thì với món rau củ kho chay mình thường không chiên. Để lửa nhỏ cho nước tương rút vào trong rau củ, cho tiêu xay rồi tắt bếp, chừa chút nước sệt để dùng với cơm. Các bạn có thể tự do sáng tạo thêm món rau củ kho chay với dứa (thơm), đậu cove, khổ qua, bí xanh…
3. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 13, 20, 2, 746.7, 'EASY', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790814/veggielife/seed60/cookbook500/c500_009_rau_cu_kho_chay.jpg', 'APPROVED_ACTIVE', 78, 'SYSTEM', DATEADD(MINUTE,-65869,@Now), DATEADD(MINUTE,-64587,@Now)),
(10168, N'Nấm Rơm Xào Đậu Phụ', 10001, N'Món Xào Chay Thanh Vị', 'BUDDHIST', N'Nấm Rơm Xào Đậu Phụ – bản giảm cân (không hành tỏi)', N'Nấm Rơm Xào Đậu Phụ – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-099), phù hợp người giảm cân, khoảng 314.6 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Trước tiên, rửa sạch nấm rơm nè, rồi cắt làm đôi.
2. Đậu phụ ấy cắt khúc hình chữ nhật, đem chiên sơ cho vàng.
3. Cà chua rửa sạch cắt lát mỏng. sau đó xay cho thật nát
4. Trút cà chua xay vào, nêm đường cát và ít nước lọc vào, đảo đều
5. Hỗn hợp cà chua sôi lên cho đậu khuôn vào tiếp tục cho nấm đã chín vào luôn trộn đều lên và dùng được rồi đó.
6. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 13, 20, 2, 314.6, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790997/veggielife/seed60/cookbook500/c500_099_nam_rom_xao_dau_phu.jpg', 'APPROVED_ACTIVE', 327, 'SYSTEM', DATEADD(MINUTE,-69532,@Now), DATEADD(MINUTE,-68689,@Now)),
(10169, N'Nấm Kho Carot', 10001, N'Món Kho Chay Đậm Đà', 'LACTO', N'Nấm Kho Carot – bản giảm cân (không hành tỏi)', N'Nấm Kho Carot – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-006), phù hợp người giảm cân, khoảng 308.0 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Cho dầu ăn vào chảo, dầu nóng cho bơ vào nấu cho tan chảy.
2. Thêm cà chua paste, bột mì vào trộn đều trong 1 phút.
3. Thêm rượu trắng vào đun lửa nhỏ trong vài phút cho sốt sền sệt là được.
4. Nêm nước tương, ít muối tiêu vừa ăn.
5. Cho ngò rí cắt nhỏ vào, trộn đều, tắt bếp.
6. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 19, 10, 2, 308.0, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790809/veggielife/seed60/cookbook500/c500_006_nam_kho_carot.jpg', 'APPROVED_ACTIVE', 876, 'SYSTEM', DATEADD(MINUTE,-78019,@Now), DATEADD(MINUTE,-75555,@Now)),
(10170, N'Mì Quảng Chay', 10001, N'Món Mì, Bún & Phở Chay', 'BUDDHIST', N'Mì Quảng Chay – bản giảm cân (không hành tỏi)', N'Mì Quảng Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-031), phù hợp người giảm cân, khoảng 377.3 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Gọt vỏ các loại rau củ cắt miếng vuông cho vào nồi nước hầm với ít muối và đường phèn. Cho tất cả vào nồi ủ hầm lấy nước dùng.
2. Mì căn cắt xéo miếng bản lớn và dày ( để khi rim kg bị nát ) ướp mì căn với hạt nêm + đường + nước tương + bột nghệ + tiêu để qua 1 bên cho thấm gia vị và đi chuẩn bị các thứ khác.
3. Đậu phộng sống cho vào máy xay , xay nhuyễn.
4. Cà chua cho vào máy xay or bằm tuỳ thích .
5. Nấm mèo ngâm mềm , tai nấm nào lớn thì cắt đôi còn không thì để nguyên.
6. Đậu hũ cắt miếng vuông chiên vàng
7. Ham cắt lát chiên sơ 2 mặt, hoà nước tương + đường hạt nêm cho tất cả vào chảo ham đã chiên và rim cho ham thấm gia vị .
8. Bắt chảo nóng phi dầu phộng cho mì căn đã ướp vào xào cho thơm , cho thêm tí nước dừa ( or nước lạnh ) vào và rim cho đến khi mì căn khô lại và nêm nếm cho hơi đậm 1 chút.
9. Bắt chảo nóng cho dầu phộng hơi nhiều tí tiếp đến cho cà chua vào xào cùng tí muối và đường để tạo màu. Kế đến cho đậu phộng sống đã xay vào nấu cùng với nấm mèo và ham chiên , đậu chiên. Cho nước hầm rau củ vào nấu cùng, sao cho lượng nước vừa đủ hơi sánh lại và nêm nếm lại cho nồi nước lèo hơi đậm đà 1 chút . Về phần nhân cơ bản là như vậy nhưng mọi người có thể thay đổi nguyên liệu sao cho dễ kiếm và phù hợp với khẩu vị nhà mình là được.
10. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 28, 20, 2, 377.3, 'HARD', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790861/veggielife/seed60/cookbook500/c500_031_mi_quang_chay.jpg', 'APPROVED_ACTIVE', 751, 'SYSTEM', DATEADD(MINUTE,-83222,@Now), DATEADD(MINUTE,-80445,@Now)),
(10171, N'Rau Luộc Kho Quẹt Chay', 10001, N'Món Hấp & Luộc Dưỡng Sinh', 'BUDDHIST', N'Rau Luộc Kho Quẹt Chay – bản giữ cân (không hành tỏi)', N'Rau Luộc Kho Quẹt Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-062), phù hợp người giữ cân, khoảng 531.6 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đậu hủ cắt nhỏ bằng đầu ngón tay, cho vào chảo dầu nóng, chiên vàng Nấm rơm cắt hạt lựu nhỏ Để kho quẹt ngon bạn nên dùng nồi đất để tạo nhiệt độ nóng đều.
2. Vì món kho quẹt nên ngọt mặn bạn nên cho nhiều đường tùy vào sở thích.
3. Để tạo độ sánh cho món kho quẹt chay, bạn cho một ít bột năng hòa vào nước loãng, cho 1 muỗng nước bột năng vào kho quẹt, để sánh lại.
4. Bạn cho đầu hủ chiên vào, đảo đều , cho rau mùi vào tắt bếp.
5. Để trang trí bạn có thể cho vài lát ớt sừng.
6. Kho quẹt chay sẽ đi ăn kèm với các loại rau củ luộc như đậu que, cà rốt, củ cải, su su, su hào, bí xanh, bầu, bắp cải…
7. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 13, 20, 2, 531.6, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790925/veggielife/seed60/cookbook500/c500_062_rau_luoc_kho_quet_chay.jpg', 'APPROVED_ACTIVE', 735, 'SYSTEM', DATEADD(MINUTE,-80190,@Now), DATEADD(MINUTE,-77218,@Now)),
(10172, N'Bún Trộn Dưa Leo', 10001, N'Món Mì, Bún & Phở Chay', 'BUDDHIST', N'Bún Trộn Dưa Leo – bản giảm cân (không hành tỏi)', N'Bún Trộn Dưa Leo – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-112), phù hợp người giảm cân, khoảng 366.4 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Bún rửa sạch, ngâm nước cho bún khô mềm ra.
2. Bước 2: Dưa chuột rửa sạch, bỏ ruột, bào sợi mảnh.
3. Bước 3: Đun nước sôi, sau đó thả bún vào trần cho chín. Khi bún đã chín vừa tới thì vớt ra cho vào nước lạnh và để ráo.
4. Bước 5: Thêm dầu mè, đường, nước mắm chay, ớt vào trộn đều rồi đặt lên bếp đun sôi sao cho hỗn hợp sốt vừa miệng.
5. Bước 6: Trộn bún cùng với dưa leo thái sợi.
6. Bước 7: Đổ phần nước sốt vào trộn đều lên và thưởng thức.
7. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 19, 20, 2, 366.4, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791028/veggielife/seed60/cookbook500/c500_112_bun_tron_dua_leo.jpg', 'APPROVED_ACTIVE', 95, 'SYSTEM', DATEADD(MINUTE,-74989,@Now), DATEADD(MINUTE,-74739,@Now)),
(10173, N'Ragu Chay', 10132, N'Món Kho Chay Đậm Đà', 'LACTO', N'Ragu Chay – bản giảm cân (không hành tỏi)', N'Ragu Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-132), phù hợp người giảm cân, khoảng 372.4 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Khoai tây, cà rốt rửa sạch, gọt vỏ. Khoai tây cắt làm tư, cà rốt cắt khúc vừa ăn. Đậu phụ, mít non cắt miếng vuông khoảng 2cm. Đậu phụ đem chiên sơ cho vàng hai mặt.
2. Bước 2: Đun nóng nồi cỡ vừa, cho 2 muỗng dầu ăn. Lần lượt cho khoai tây, cà rốt vào xào chung, nêm vào ít muối, hạt nêm.
3. Bước 3 : Cho khoảng 3 chén nước lọc, sữa tươi (nếu không dùng sữa tươi thì có thể tăng thêm nước lọc hoặc cốt dừa) vào nồi. Đun cho hỗn hợp khoai gần chín, cho tiếp đậu phụ, mít luộc vào nấu vừa chín. Rưới phần nước cốt dừa và cho bột ragu, bột ớt, 1/2 muỗng cà phê hạt nêm, ít đường, nấu sôi, tắt bếp. Cho Ragu chay ra tô, trang trí ngò rí, tiêu. Dùng nóng với bánh mì, bún hoặc cơm trắng.
4. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 19, 20, 2, 372.4, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791078/veggielife/seed60/cookbook500/c500_132_ragu_chay.jpg', 'REJECTED', 20, 'USER', DATEADD(MINUTE,-8492,@Now), DATEADD(MINUTE,-5981,@Now)),
(10174, N'Đậu Phụ Nhồi Chay', 10001, N'Món Xào Chay Thanh Vị', 'BUDDHIST', N'Đậu Phụ Nhồi Chay – bản giảm cân (không hành tỏi)', N'Đậu Phụ Nhồi Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-121), phù hợp người giảm cân, khoảng 375.1 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Đậu phụ chiên rọc đường nhỏ phía cạnh rồi dùng muỗng múc hết phần ruột trắng bên trong.
2. Bước 2: Nấm mèo & nấm hương ngâm nở cắt nhỏ.
3. Bước 3: Tất cả trộn đều cùng hạt nêm, dầu hào chay và hạt tiêu. Sau đó nhồi lại vào phần vỏ đậu phụ rồi dùng lá chuối tước nhỏ chần nhanh qua nước sôi buộc lại miếng đậu phụ và đem hấp 10 phút. Phần nhân dư thì bạn có thể vo viên tròn rồi cho vào hấp cùng.
4. Bước 4: Bắc chảo lên bếp cho nóng. Cho 1 bát nước, dầu hào chay, hạt nêm, nước tương và đường cùng ít tương ớt nấu sôi. Sau đó cho thêm 1 muỗng dầu mè vào.
5. Bước 5: Muốn phần sốt thêm sánh thì bạn cho thêm 1 bát nước nhỏ pha với bột năng vào chảo đang đun nhé. Nêm gia vị cho vừa ăn rồi đun thêm một lúc cho ngấm thì tắt bếp.
6. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 22, 10, 2, 375.1, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791048/veggielife/seed60/cookbook500/c500_121_dau_phu_nhoi_chay.jpg', 'APPROVED_ACTIVE', 1390, 'SYSTEM', DATEADD(MINUTE,-81744,@Now), DATEADD(MINUTE,-80979,@Now)),
(10175, N'Bún Trộn Rau Cải', 10031, N'Món Mì, Bún & Phở Chay', 'BUDDHIST', N'Bún Trộn Rau Cải – bản giảm cân (không hành tỏi)', N'Bún Trộn Rau Cải – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-019), phù hợp người giảm cân, khoảng 360.4 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Xào thịt bò chay: Thịt bò chay sau khi đã sơ chế (mì căng), xé hoặc thái miếng vừa ăn rồi tẩm ướp bằng nước tương,chút đường thô, tiêu, sả bằm, gừng băm nhỏ, tiêu, chút xíu dầu ăn thực vật. Trộn đều rồi để trong khoảng 15 phút. Có thể cho chút nước nếu cần. Bắc ra để cho âm ấm.
2. Chuẩn bị rau sống: Giá rửa sạch, để ráo Các loại rau rửa sạch, để ráo, rồi thái sợi. Trước khi ăn: Cho bún vào bát. Xếp rau sống, giá, thịt bò chay xào lên trên. Lúc ăn, rưới nước chấm bò bún Nên dùng nước lọc ấm để pha nước chấm.
3. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 22, 15, 2, 360.4, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790836/veggielife/seed60/cookbook500/c500_019_bun_tron_rau_cai.jpg', 'APPROVED_ACTIVE', 214, 'USER', DATEADD(MINUTE,-4885,@Now), DATEADD(MINUTE,-2684,@Now)),
(10176, N'Nui Gạo Chay', 10015, N'Món Mì, Bún & Phở Chay', 'BUDDHIST', N'Nui Gạo Chay – bản giữ cân (không hành tỏi)', N'Nui Gạo Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-073), phù hợp người giữ cân, khoảng 509.1 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Gọt cà rốt, khoai tây, khoai lang, su su, nấm cắt bỏ chân, ngâm chung vào âu nước muỗi pha loãng.
2. Chả chay cắt lát mỏng, rán sơ.
3. Nấm sau khi rửa sạch, để ráo, cắt lát mỏng.
4. Cho đậu phụ, chả chay vào xào cùng với nấm, tiếp tục đậy kín nắp, đun lửa nhỏ để
5. nguyên liệu thấm gia vị. Đun khoảng 10 phút thì tắt bếp.
6. Cà rốt, khoai tây, khoai lang, su su sau khi rửa sạch, để ráo, cắt khúc vừa ăn. Cho tất cả rau củ vào nồi, châm nước lạnh ngập mặt, đặt lên bếp đun sôi, thêm đường phèn, một ít muối. Đun đến khi dùng đũa xiên nhẹ qua cà rốt mềm.
7. Cho đậu phụ, nấm đã xào ở bước 4 vào nồi, tiếp tục đun sôi khoảng 5 phút nữa thì nêm nếm lại gia vị cho vừa ăn, tắt bếp.
8. Đun nồi nước sôi, cho nui gạo vào nồi, luộc chín, đổ ra rổ cho ráo nước.
9. Khi dùng múc một ít nui gạo vào bát, thêm nước súp, rau củ và đậu phụ, nấm, chả chay, dùng nóng.
10. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 22, 25, 2, 509.1, 'HARD', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790948/veggielife/seed60/cookbook500/c500_073_nui_gao_chay.jpg', 'APPROVED_ACTIVE', 219, 'AI', DATEADD(MINUTE,-41289,@Now), DATEADD(MINUTE,-39298,@Now)),
(10177, N'Mướp Đắng Kho Chay', 10001, N'Món Kho Chay Đậm Đà', 'BUDDHIST', N'Mướp Đắng Kho Chay – bản giữ cân (không hành tỏi)', N'Mướp Đắng Kho Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-124), phù hợp người giữ cân, khoảng 516.6 kcal mỗi khẩu phần (2 khẩu phần).', N'1. 1: Mướp đắng rửa sạch, cắt khúc dài khoảng 1.5cm, bỏ ruột. Ớt đập dập. Nấm mèo ngâm nở, thái sợi, băm nhỏ, miến ngâm mềm, cắt khúc 2cm.
2. Bước 2: Đậu phụ chần qua nước sôi, cho vào vải mùng vắt ráo nước, tán nhuyễn.
3. Bước 4: Pha hỗn hợp 2 thìa nước tương, 1 thìa đường, 1 thìa bột ngọt, 1/5 thìa muối.
4. Bước 5: Chiên áp chảo mướp đắng đến khi rám mặt, rồi cho hỗn hợp nước tương vào, kho 2 phút cho thấm vị.
5. Bước 6: Thêm nước dừa, ớt đập dập rồi kho tiếp 10 phút cho tới khi nước xâm xấp là được.
6. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 22, 12, 2, 516.6, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791058/veggielife/seed60/cookbook500/c500_124_muop_dang_kho_chay.jpg', 'APPROVED_ACTIVE', 1242, 'SYSTEM', DATEADD(MINUTE,-69868,@Now), DATEADD(MINUTE,-69677,@Now)),
(10178, N'Đậu Phụ Sốt Nấm Kim Châm', 10132, N'Món Xào Chay Thanh Vị', 'BUDDHIST', N'Đậu Phụ Sốt Nấm Kim Châm – bản giữ cân (không hành tỏi)', N'Đậu Phụ Sốt Nấm Kim Châm – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-123), phù hợp người giữ cân, khoảng 501.3 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đổ vào khoảng 1 bát nước lạnh, nêm 2 thìa xì dầu, thêm đường, muối cho vừa ăn.
2. Bước 2: Hòa bột ngô với ít nước rồi đổ vào chảo, khuấy đều cho hỗn hợp nước sốt sánh lại. (Bột ngô có tác dụng làm sánh phần nước sốt lại. Nếu bạn không mua được bột ngô thì món sẽ không được đẹp mắt thôi nhé).
3. Bước 3: Thả đậu vào chảo nước sốt khoảng 2-3 phút. Tắt bếp, đậy vung kín chảo trong vòng 5 phút để đậu thấm gia vị và nước sốt.
4. Bước 4: Bật bếp trở lại, cho nấm kim châm vào nấu khoảng 3 phút (Bạn không nên để quá lâu làm mất độ giòn của nấm).
5. Bước 5: Tắt bếp, đổ ra đĩa. Cuối cùng là rắc một chút rau mùi lên trên cho bắt mắt và giúp món ăn thơm hơn. Đậu phụ sốt nấm kim châm ăn rất hợp với cơm hoặc ăn không cũng được nhé!
6. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 19, 11, 2, 501.3, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791054/veggielife/seed60/cookbook500/c500_123_dau_phu_sot_nam_kim_cham.jpg', 'APPROVED_ACTIVE', 455, 'AI', DATEADD(MINUTE,-33812,@Now), DATEADD(MINUTE,-33108,@Now)),
(10179, N'Đậu Hũ Non Sốt Nấm Đông Cô', 10108, N'Món Xào Chay Thanh Vị', 'BUDDHIST', N'Đậu Hũ Non Sốt Nấm Đông Cô – bản giảm cân (không hành tỏi)', N'Đậu Hũ Non Sốt Nấm Đông Cô – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-003), phù hợp người giảm cân, khoảng 297.7 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Nấm đông cô cắt phần chân già, rửa sạch, ngâm trong nước muối loãng khoảng 15’ cho sạch. Đậu hũ non rửa sạch, thái miếng xếp ra đĩa. Dùng màng thực phẩm bao lại, làm chín trong lò vi song khoảng 2 phút. Hoặc bạn có thể làm chín đậu hũ non bằng cách trần qua nước sôi thêm chút muối.
2. Nấm sau khi ngâm, vớt ra để ráo, thái lát mỏng. Bắc chảo lên bếp, cho 1 thìa dầu mè. Tiếp đó cho nấm vào xào chín, thêm chút dầu hào chay, nước tương vào đảo kỹ. Hòa 1 thìa bột năng với chút nước, cho vào chảo nấm đảo đều, nêm nếm cho vừa ăn.
3. Cho nấm cùng với nước sốt ở trên vào đĩa đậu hũ non đã làm chín. Dùng thìa dưới đều phần nước sốt lên các miếng đậu để đậu hũ được ngấm đều.
4. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 19, 17, 2, 297.7, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790803/veggielife/seed60/cookbook500/c500_003_dau_hu_non_sot_nam_dong_co.jpg', 'DRAFT', 1, 'USER', DATEADD(MINUTE,-22720,@Now), CAST(NULL AS DATETIME2(7))),
(10180, N'Bún Trộn Dưa Leo Giòn Mát Ngày Hè', 10001, N'Món Mì, Bún & Phở Chay', 'BUDDHIST', N'Bún Trộn Dưa Leo Giòn Mát Ngày Hè – bản giảm cân (không hành tỏi)', N'Bún Trộn Dưa Leo Giòn Mát Ngày Hè – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-020), phù hợp người giảm cân, khoảng 366.4 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Bún rửa sạch, ngâm nước cho bún khô mềm ra.
2. Dưa leo rửa sạch, bỏ ruột, bào sợi mảnh.
3. Đun nước sôi, sau đó thả bún vào trụng cho chín.
4. Vặn lửa nhỏ, để bún sôi trở lại cho bún chín thì thả vào 1 tô nước lạnh để bún không bị nát.
5. Thêm dầu mè, đường, nước mắm chay, ớt vào trộn đều rồi đặt lên bếp đun sôi sao cho hỗn hợp xốt vừa miệng.
6. Vớt bún ra để ráo, trộn cùng với dưa leo.
7. Đổ phần nước xốt vào trộn đều lên sau đó múc ra bát và thưởng thức.
8. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 19, 20, 2, 366.4, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790839/veggielife/seed60/cookbook500/c500_020_bun_tron_dua_leo_gion_mat_ngay_he.jpg', 'APPROVED_ACTIVE', 772, 'SYSTEM', DATEADD(MINUTE,-70870,@Now), DATEADD(MINUTE,-68706,@Now))
) AS v(rid, fn, uid, cat, sc, ti, de, ins, pr, ck, sv, kc, df, vf, im, st, vw, sr, cr, up)
JOIN #food f ON f.Food_Name = v.fn
JOIN #cat c ON c.Category_Name = v.cat
JOIN #sch s ON s.School_Code = v.sc;
IF @@ROWCOUNT <> 60 THROW 50001, N'Recipes: tra cứu dữ liệu master thất bại (thiếu tên/mã)', 1;
INSERT INTO dbo.Recipes (Recipe_ID, Food_ID, User_ID, Category_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At)
SELECT v.rid, f.Food_ID, v.uid, c.Category_ID, s.Dietary_School_ID, v.ti, v.de, v.ins, v.pr, v.ck, v.sv, v.kc, v.df, v.vf, v.im, v.st, v.vw, v.sr, v.cr, v.up
FROM (VALUES
(10181, N'Bánh Bột Lọc Chay', 10146, N'Món Hấp & Luộc Dưỡng Sinh', 'BUDDHIST', N'Bánh Bột Lọc Chay – bản giữ cân (không hành tỏi)', N'Bánh Bột Lọc Chay – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-107), phù hợp người giữ cân, khoảng 479.3 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Đỗ xanh đãi nhiều lần cho sạch, ngâm vào thố nước lạnh có pha một ít muối, ngâm khoảng 1-2 tiếng.
2. Sau đó hấp chín đậu, giã mịn hoặc cho vào máy xay đậu, xay thật mịn.
3. Xào khoảng 15 phút đến khi đậu khô.
4. Bột năng khô đổ ra âu, thêm bột gạo, nửa thìa nhỏ muối, trộn đều.
5. Cho từ từ hơn một bát con nước sôi nóng già, hỗn hợp bột lúc này sẽ rất nóng dùng muôi trộn đều đến khi hỗn hợp bột nguội bớt bạn dùng tay nhồi đến khi bột trở nên mịn, sờ không dính tay là đạt. Tùy theo liều lượng hút nước khác nhau của mỗi loại bột mà bạn điều chỉnh lượng nước sôi cho phù hợp.
6. Cho viên đỗ xanh vào giữa.
7. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 19, 15, 2, 479.3, 'HARD', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791014/veggielife/seed60/cookbook500/c500_107_banh_bot_loc_chay.jpg', 'PENDING_REVIEW', 1, 'USER', DATEADD(MINUTE,-8451,@Now), DATEADD(MINUTE,-6665,@Now)),
(10182, N'Miến Xào Với Mít Non', 10150, N'Món Mì, Bún & Phở Chay', 'BUDDHIST', N'Miến Xào Với Mít Non – bản tăng cân (không hành tỏi)', N'Miến Xào Với Mít Non – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-032), phù hợp người tăng cân, khoảng 763.7 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Mít non rửa sạch nhựa, chần sơ qua nồi nước sôi sau đó để nguội dùng tay xé nhỏ.
2. Miến ngâm nở, cắt làm đôi, chần sơ miến qua nồi nước sôi, xả lại dưới vòi nước lạnh để miến không bị dính.
3. Mộc nhĩ ngâm nở, rửa sạch, cắt bỏ chân, thái sợi.
4. Cho mộc nhĩ vào đảo cùng, đều tay.
5. Cuối cùng cho miến vào xào cùng, đảo nhẹ tay để sợi miến không bị nát, thêm gia vị cho vừa miệng, xào khoảng 5 phút.
6. Tắt bếp, múc ra đĩa dùng nóng.
7. Ghi chú – Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 16, 10, 2, 763.7, 'MEDIUM', 0, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793790863/veggielife/seed60/cookbook500/c500_032_mien_xao_voi_mit_non.jpg', 'APPROVED_ACTIVE', 448, 'AI', DATEADD(MINUTE,-1497,@Now), DATEADD(MINUTE,-1440,@Now)),
(10183, N'Đậu Phụ Sốt Cay Tứ Xuyên', 10001, N'Món Xào Chay Thanh Vị', 'BUDDHIST', N'Đậu Phụ Sốt Cay Tứ Xuyên – bản giữ cân (không hành tỏi)', N'Đậu Phụ Sốt Cay Tứ Xuyên – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-111), phù hợp người giữ cân, khoảng 495.3 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Sau đó bạn đem cho đậu phụ vào nước muối ngâm trong vòng 15 phút rồi vớt ra để cho ráo nước.
2. Bước 2: Ngâm nấm hương trong nước nóng để khoảng 5 - 10 phút. Sau đó vớt ra rửa sạch rồi đem thái mỏng, còn phần nước ngâm bạn để riêng để cho vào nấu.
3. Gừng đem thái mỏng.
4. Bước 4 Pha nước sốt gồm : 1 thìa nước tương, 20ml nước, 1 thìa sa tế chay, 1 thìa đường và 1 thìa muối.
5. Bước 6: Cho nấm hương vào xào tiếp cho đến khi nấm hương chín mềm.
6. Bước 7: Đổ nước sốt đã pha và nước ngâm nấm hương vào chảo. Chờ đến khi nước sốt bắt đầu sôi lăn tăn thì cho đậu vào, đun lửa nhỏ khoảng 5 phút thì bắc ra. Đậu phụ sốt Tứ Xuyên rất phù hợp để ăn với cơm nóng nhé!
7. Ghi chú – Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 19, 30, 2, 495.3, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791023/veggielife/seed60/cookbook500/c500_111_dau_phu_sot_cay_tu_xuyen.jpg', 'APPROVED_ACTIVE', 1650, 'SYSTEM', DATEADD(MINUTE,-65396,@Now), DATEADD(MINUTE,-65157,@Now)),
(10184, N'Đậu Bắp Kho Nước Tương', 10001, N'Món Kho Chay Đậm Đà', 'BUDDHIST', N'Đậu Bắp Kho Nước Tương – bản giảm cân (không hành tỏi)', N'Đậu Bắp Kho Nước Tương – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (C500-119), phù hợp người giảm cân, khoảng 350.4 kcal mỗi khẩu phần (2 khẩu phần).', N'1. Bước 2: Chiên đậu bắp với lửa vừa, thêm ngô bao tử vào xào khoảng 2 phút, cho tiếp sả đập dập và 1/2 chén nước dừa.
2. Bước 3: Nêm 1 muỗng đường, 2 muỗng nước tương , 1 muỗng hạt nêm từ nấm hương và hạt sen, ⅓ muỗng bột ngọt , thêm ớt sừng và chả lụa vào đun lửa riu riu đến khi nước cạn, có độ sánh, rau củ và chả lụa thấm gia vị thì tắt bếp. Dùng nóng với cơm.
3. Ghi chú – Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ. Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.', 22, 10, 2, 350.4, 'MEDIUM', 1, 'https://res.cloudinary.com/swrsuenp/image/upload/v1793791044/veggielife/seed60/cookbook500/c500_119_dau_bap_kho_nuoc_tuong.jpg', 'APPROVED_ACTIVE', 1424, 'SYSTEM', DATEADD(MINUTE,-83221,@Now), DATEADD(MINUTE,-82041,@Now))
) AS v(rid, fn, uid, cat, sc, ti, de, ins, pr, ck, sv, kc, df, vf, im, st, vw, sr, cr, up)
JOIN #food f ON f.Food_Name = v.fn
JOIN #cat c ON c.Category_Name = v.cat
JOIN #sch s ON s.School_Code = v.sc;
IF @@ROWCOUNT <> 4 THROW 50001, N'Recipes: tra cứu dữ liệu master thất bại (thiếu tên/mã)', 1;
SET IDENTITY_INSERT dbo.Recipes OFF;
PRINT N'   + Recipes: 184 dòng';
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional)
SELECT v.rid, i.Ingredient_ID, v.qty, v.u, v.nt, v.op
FROM (VALUES
(10001, N'Đậu hũ trắng (Tofu)', 100.00, N'g', N'Ngâm nước ấm', 0),
(10001, N'Nấm rơm tươi', 200.00, N'g', NULL, 0),
(10001, N'Cà chua chín đỏ', 150.00, N'g', NULL, 0),
(10001, N'Rau muống tươi', 100.00, N'g', NULL, 0),
(10001, N'Rau thơm các loại', 30.00, N'g', N'Băm nhỏ', 0),
(10001, N'Dứa (thơm)', 100.00, N'g', N'Cắt miếng vừa ăn', 0),
(10001, N'Chanh', 15.00, N'g', N'Băm nhỏ', 0),
(10001, N'Me chua', 30.00, N'g', NULL, 0),
(10001, N'Hạt nêm nấm chay bào ngư', 60.00, N'g', NULL, 0),
(10001, N'Dầu đậu nành nguyên chất', 15.00, N'ml', N'Cắt miếng vừa ăn', 0),
(10001, N'Muối tinh', 15.00, N'g', N'Băm nhỏ', 0),
(10001, N'Đường cát trắng', 40.00, N'g', NULL, 0),
(10001, N'Ớt hiểm tươi cay', 5.00, N'g', N'Cắt miếng vừa ăn', 0),
(10001, N'Sả cây tươi', 35.00, N'g', N'Thái lát mỏng', 0),
(10002, N'Đậu hũ trắng (Tofu)', 145.00, N'g', N'Thái lát mỏng', 0),
(10002, N'Nấm rơm tươi', 285.00, N'g', NULL, 0),
(10002, N'Cà chua chín đỏ', 215.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10002, N'Rau muống tươi', 145.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10002, N'Rau thơm các loại', 30.00, N'g', N'Cắt miếng vừa ăn', 0),
(10002, N'Dứa (thơm)', 145.00, N'g', N'Cắt miếng vừa ăn', 0),
(10002, N'Chanh', 15.00, N'g', N'Ngâm nước ấm', 0),
(10002, N'Me chua', 30.00, N'g', NULL, 0),
(10002, N'Hạt nêm nấm chay bào ngư', 60.00, N'g', NULL, 0),
(10002, N'Dầu đậu nành nguyên chất', 20.00, N'ml', N'Cắt miếng vừa ăn', 0),
(10002, N'Muối tinh', 15.00, N'g', N'Thái lát mỏng', 0),
(10002, N'Đường cát trắng', 55.00, N'g', N'Ngâm nước ấm', 0),
(10002, N'Ớt hiểm tươi cay', 5.00, N'g', NULL, 0),
(10002, N'Sả cây tươi', 35.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10003, N'Đậu hũ trắng (Tofu)', 100.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10003, N'Nấm rơm tươi', 260.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10003, N'Cà chua chín đỏ', 195.00, N'g', NULL, 0),
(10003, N'Rau muống tươi', 130.00, N'g', NULL, 0),
(10003, N'Rau thơm các loại', 39.00, N'g', N'Ngâm nước ấm', 0),
(10003, N'Dứa (thơm)', 130.00, N'g', NULL, 0),
(10003, N'Chanh', 19.50, N'g', N'Ngâm nước ấm', 0),
(10003, N'Me chua', 39.00, N'g', NULL, 0),
(10003, N'Hạt nêm nấm chay bào ngư', 78.00, N'g', N'Thái lát mỏng', 0),
(10003, N'Dầu đậu nành nguyên chất', 7.50, N'ml', N'Cắt miếng vừa ăn', 0),
(10003, N'Muối tinh', 19.50, N'g', N'Ngâm nước ấm', 0),
(10003, N'Đường cát trắng', 20.00, N'g', NULL, 0),
(10003, N'Sả cây tươi', 45.50, N'g', N'Cắt miếng vừa ăn', 0),
(10004, N'Trứng gà ta tươi', 2.50, N'g', NULL, 0),
(10004, N'Đậu hũ trắng (Tofu)', 400.00, N'g', N'Ngâm nước ấm', 0),
(10004, N'Nấm rơm tươi', 100.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10004, N'Nấm hương tươi (Shiitake)', 100.00, N'g', NULL, 0),
(10004, N'Nấm mộc nhĩ khô (Nấm mèo)', 10.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10004, N'Bắp cải trắng', 100.00, N'g', N'Ngâm nước ấm', 0),
(10004, N'Cải ngọt tươi', 100.00, N'g', N'Ngâm nước ấm', 0),
(10004, N'Nước tương đậu nành (Xì dầu)', 5.00, N'ml', NULL, 0),
(10004, N'Muối tinh', 2.50, N'g', N'Rửa sạch, cắt khúc', 0),
(10004, N'Đường cát trắng', 5.00, N'g', NULL, 0),
(10005, N'Trứng gà ta tươi', 3.40, N'g', N'Băm nhỏ', 0),
(10005, N'Đậu hũ trắng (Tofu)', 1580.00, N'g', N'Băm nhỏ', 0),
(10005, N'Nấm rơm tươi', 295.00, N'g', NULL, 0),
(10005, N'Nấm hương tươi (Shiitake)', 295.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10005, N'Nấm mộc nhĩ khô (Nấm mèo)', 10.00, N'g', NULL, 0),
(10005, N'Bắp cải trắng', 295.00, N'g', N'Cắt miếng vừa ăn', 0),
(10005, N'Cải ngọt tươi', 295.00, N'g', N'Thái lát mỏng', 0),
(10005, N'Nước tương đậu nành (Xì dầu)', 5.00, N'ml', N'Băm nhỏ', 0),
(10005, N'Muối tinh', 2.50, N'g', NULL, 0),
(10005, N'Đường cát trắng', 5.00, N'g', N'Cắt miếng vừa ăn', 0),
(10006, N'Tàu hũ ky khô', 200.00, N'g', N'Ngâm nước ấm', 0),
(10006, N'Nấm mộc nhĩ khô (Nấm mèo)', 30.00, N'g', NULL, 0),
(10006, N'Xà lách lụa tươi', 100.00, N'g', NULL, 0),
(10006, N'Rau mùi tươi (Ngò rí)', 8.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10006, N'Rau thơm các loại', 15.00, N'g', NULL, 0),
(10006, N'Gạo nếp', 200.00, N'g', N'Cắt miếng vừa ăn', 0),
(10006, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', NULL, 0),
(10006, N'Tiêu đen xay nguyên chất', 1.00, N'g', NULL, 0),
(10007, N'Đậu hũ trắng (Tofu)', 200.00, N'g', N'Băm nhỏ', 0),
(10007, N'Cà chua chín đỏ', 60.00, N'g', N'Ngâm nước ấm', 0),
(10007, N'Hành Boa-rô (Tỏi tây / Leek)', 10.00, N'g', NULL, 0),
(10007, N'Bột bắp (bột ngô)', 20.00, N'g', N'Ngâm nước ấm', 0),
(10007, N'Nước tương đậu nành (Xì dầu)', 2.50, N'ml', N'Ngâm nước ấm', 0),
(10007, N'Muối tinh', 3.00, N'g', N'Ngâm nước ấm', 0),
(10007, N'Đường cát trắng', 5.00, N'g', NULL, 0),
(10008, N'Đậu hũ non (Silken Tofu)', 1.00, N'g', NULL, 0),
(10008, N'Nấm hương tươi (Shiitake)', 100.00, N'g', NULL, 0),
(10008, N'Hành lá tươi (Hành hoa)', 15.00, N'g', N'Ngâm nước ấm', 0),
(10008, N'Hạt mè trắng (Vừng trắng)', 10.00, N'g', NULL, 0),
(10008, N'Bột năng (Bột sắn)', 10.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10008, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', NULL, 0),
(10008, N'Dầu hào chay nấm hương', 15.00, N'ml', N'Ngâm nước ấm', 0),
(10008, N'Dầu đậu nành nguyên chất', 10.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10008, N'Muối tinh', 10.00, N'g', N'Ngâm nước ấm', 0),
(10009, N'Đậu hũ non (Silken Tofu)', 1.40, N'g', N'Băm nhỏ', 0),
(10009, N'Nấm hương tươi (Shiitake)', 1380.00, N'g', N'Ngâm nước ấm', 0),
(10009, N'Hành lá tươi (Hành hoa)', 15.00, N'g', N'Băm nhỏ', 0),
(10009, N'Hạt mè trắng (Vừng trắng)', 10.00, N'g', N'Ngâm nước ấm', 0),
(10009, N'Bột năng (Bột sắn)', 12.50, N'g', N'Cắt miếng vừa ăn', 0),
(10009, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Thái lát mỏng', 0),
(10009, N'Dầu hào chay nấm hương', 15.00, N'ml', NULL, 0),
(10009, N'Dầu đậu nành nguyên chất', 13.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10009, N'Muối tinh', 10.00, N'g', NULL, 0),
(10010, N'Tàu hũ ky khô', 160.00, N'g', N'Băm nhỏ', 0),
(10010, N'Đậu hũ trắng (Tofu)', 400.00, N'g', NULL, 0),
(10010, N'Nấm hương tươi (Shiitake)', 150.00, N'g', NULL, 0),
(10010, N'Nấm mộc nhĩ khô (Nấm mèo)', 30.00, N'g', NULL, 0),
(10010, N'Cà rốt củ tươi', 200.00, N'g', N'Thái lát mỏng', 0),
(10010, N'Củ sắn tươi (Củ đậu)', 200.00, N'g', NULL, 0),
(10010, N'Rau mùi tươi (Ngò rí)', 16.00, N'g', NULL, 0),
(10010, N'Miến dong khô', 100.00, N'g', N'Cắt miếng vừa ăn', 0),
(10010, N'Nước mắm chay (từ dứa & nấm rơm)', 15.00, N'ml', N'Cắt miếng vừa ăn', 0),
(10010, N'Hạt nêm nấm chay bào ngư', 10.00, N'g', N'Thái lát mỏng', 0),
(10010, N'Muối tinh', 8.00, N'g', N'Ngâm nước ấm', 0),
(10010, N'Tiêu đen xay nguyên chất', 10.00, N'g', N'Cắt miếng vừa ăn', 0),
(10011, N'Đậu hũ chiên vàng', 150.00, N'g', N'Thái lát mỏng', 0),
(10011, N'Đậu hũ trắng (Tofu)', 400.00, N'g', NULL, 0),
(10011, N'Mì căn tươi (Seitan đạm lúa mì)', 50.00, N'g', NULL, 0),
(10011, N'Nấm hương tươi (Shiitake)', 50.00, N'g', N'Thái lát mỏng', 0),
(10011, N'Cà rốt củ tươi', 100.00, N'g', N'Ngâm nước ấm', 0),
(10011, N'Su su quả tươi', 100.00, N'g', N'Thái lát mỏng', 0),
(10011, N'Bắp cải trắng', 100.00, N'g', NULL, 0),
(10011, N'Rau thơm các loại', 30.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10011, N'Bún gạo khô', 1.00, N'g', N'Thái lát mỏng', 0),
(10011, N'Chanh', 15.00, N'g', N'Thái lát mỏng', 0),
(10011, N'Ớt hiểm tươi cay', 10.00, N'g', N'Cắt miếng vừa ăn', 0),
(10011, N'Gừng củ tươi', 10.00, N'g', N'Ngâm nước ấm', 0),
(10011, N'Sả cây tươi', 30.00, N'g', N'Ngâm nước ấm', 0),
(10011, N'Nghệ củ tươi', 5.00, N'g', N'Cắt miếng vừa ăn', 0),
(10012, N'Đậu hũ chiên vàng', 110.00, N'g', NULL, 0),
(10012, N'Đậu hũ trắng (Tofu)', 295.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10012, N'Mì căn tươi (Seitan đạm lúa mì)', 35.00, N'g', N'Cắt miếng vừa ăn', 0),
(10012, N'Nấm hương tươi (Shiitake)', 50.00, N'g', NULL, 0),
(10012, N'Cà rốt củ tươi', 95.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10012, N'Su su quả tươi', 95.00, N'g', NULL, 0),
(10012, N'Bắp cải trắng', 95.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10012, N'Rau thơm các loại', 39.00, N'g', NULL, 0),
(10012, N'Bún gạo khô', 0.80, N'g', N'Ngâm nước ấm', 0),
(10012, N'Chanh', 19.50, N'g', NULL, 0),
(10012, N'Gừng củ tươi', 13.00, N'g', NULL, 0),
(10012, N'Sả cây tươi', 39.00, N'g', N'Thái lát mỏng', 0),
(10012, N'Nghệ củ tươi', 6.50, N'g', NULL, 0),
(10013, N'Đậu hũ chiên vàng', 265.00, N'g', NULL, 0),
(10013, N'Đậu hũ trắng (Tofu)', 710.00, N'g', N'Ngâm nước ấm', 0),
(10013, N'Mì căn tươi (Seitan đạm lúa mì)', 90.00, N'g', N'Cắt miếng vừa ăn', 0),
(10013, N'Nấm hương tươi (Shiitake)', 65.00, N'g', NULL, 0),
(10013, N'Cà rốt củ tươi', 130.00, N'g', N'Cắt miếng vừa ăn', 0),
(10013, N'Su su quả tươi', 130.00, N'g', N'Cắt miếng vừa ăn', 0),
(10013, N'Bắp cải trắng', 130.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10013, N'Rau thơm các loại', 30.00, N'g', N'Ngâm nước ấm', 0),
(10013, N'Bún gạo khô', 1.20, N'g', N'Rửa sạch, cắt khúc', 0),
(10013, N'Chanh', 15.00, N'g', N'Băm nhỏ', 0),
(10013, N'Ớt hiểm tươi cay', 10.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10013, N'Gừng củ tươi', 10.00, N'g', NULL, 0),
(10013, N'Sả cây tươi', 30.00, N'g', NULL, 0),
(10013, N'Nghệ củ tươi', 5.00, N'g', NULL, 0),
(10014, N'Đậu hũ trắng (Tofu)', 200.00, N'g', NULL, 0),
(10014, N'Nấm hương tươi (Shiitake)', 50.00, N'g', NULL, 0),
(10014, N'Hành lá tươi (Hành hoa)', 15.00, N'g', N'Thái lát mỏng', 0),
(10014, N'Ớt chuông', 100.00, N'g', N'Cắt miếng vừa ăn', 0),
(10015, N'Đậu hũ trắng (Tofu)', 1560.00, N'g', N'Cắt miếng vừa ăn', 0),
(10015, N'Nấm hương tươi (Shiitake)', 290.00, N'g', N'Băm nhỏ', 0),
(10015, N'Hành lá tươi (Hành hoa)', 15.00, N'g', NULL, 0),
(10015, N'Ớt chuông', 575.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10016, N'Đậu hũ trắng (Tofu)', 665.00, N'g', NULL, 0),
(10016, N'Nấm hương tươi (Shiitake)', 215.00, N'g', NULL, 0),
(10016, N'Hành lá tươi (Hành hoa)', 19.50, N'g', N'Thái lát mỏng', 0),
(10016, N'Ớt chuông', 430.00, N'g', N'Thái lát mỏng', 0),
(10017, N'Tàu hũ ky khô', 100.00, N'g', N'Băm nhỏ', 0),
(10017, N'Đậu hũ trắng (Tofu)', 400.00, N'g', N'Băm nhỏ', 0),
(10017, N'Nấm hương tươi (Shiitake)', 50.00, N'g', NULL, 0),
(10017, N'Nấm mộc nhĩ khô (Nấm mèo)', 10.00, N'g', N'Thái lát mỏng', 0),
(10017, N'Cà rốt củ tươi', 100.00, N'g', N'Thái lát mỏng', 0),
(10017, N'Củ sắn tươi (Củ đậu)', 100.00, N'g', N'Cắt miếng vừa ăn', 0),
(10017, N'Rau mùi tươi (Ngò rí)', 8.00, N'g', N'Cắt miếng vừa ăn', 0),
(10017, N'Miến dong khô', 20.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10017, N'Nước mắm chay (từ dứa & nấm rơm)', 15.00, N'ml', NULL, 0),
(10017, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', NULL, 0),
(10017, N'Muối tinh', 3.00, N'g', NULL, 0),
(10018, N'Tàu hũ ky khô', 185.00, N'g', NULL, 0),
(10018, N'Đậu hũ trắng (Tofu)', 745.00, N'g', N'Ngâm nước ấm', 0),
(10018, N'Nấm hương tươi (Shiitake)', 70.00, N'g', N'Thái lát mỏng', 0),
(10018, N'Nấm mộc nhĩ khô (Nấm mèo)', 10.00, N'g', NULL, 0),
(10018, N'Cà rốt củ tươi', 140.00, N'g', NULL, 0),
(10018, N'Củ sắn tươi (Củ đậu)', 140.00, N'g', N'Thái lát mỏng', 0),
(10018, N'Rau mùi tươi (Ngò rí)', 8.00, N'g', NULL, 0),
(10018, N'Miến dong khô', 35.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10018, N'Nước mắm chay (từ dứa & nấm rơm)', 15.00, N'ml', N'Cắt miếng vừa ăn', 0),
(10018, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', NULL, 0),
(10018, N'Muối tinh', 3.00, N'g', N'Cắt miếng vừa ăn', 0),
(10019, N'Tàu hũ ky khô', 80.00, N'g', N'Cắt miếng vừa ăn', 0),
(10019, N'Đậu hũ trắng (Tofu)', 315.00, N'g', NULL, 0),
(10019, N'Nấm hương tươi (Shiitake)', 50.00, N'g', N'Băm nhỏ', 0),
(10019, N'Nấm mộc nhĩ khô (Nấm mèo)', 13.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10019, N'Cà rốt củ tươi', 100.00, N'g', N'Cắt miếng vừa ăn', 0),
(10019, N'Củ sắn tươi (Củ đậu)', 100.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10019, N'Rau mùi tươi (Ngò rí)', 10.40, N'g', N'Băm nhỏ', 0),
(10019, N'Miến dong khô', 10.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10019, N'Nước mắm chay (từ dứa & nấm rơm)', 19.50, N'ml', N'Ngâm nước ấm', 0),
(10019, N'Hạt nêm nấm chay bào ngư', 6.50, N'g', N'Rửa sạch, cắt khúc', 0),
(10019, N'Muối tinh', 3.90, N'g', N'Rửa sạch, cắt khúc', 0),
(10020, N'Nấm rơm tươi', 200.00, N'g', NULL, 0),
(10020, N'Cà rốt củ tươi', 100.00, N'g', N'Cắt miếng vừa ăn', 0),
(10020, N'Củ cải trắng tươi', 50.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10020, N'Cà tím dài', 150.00, N'g', N'Ngâm nước ấm', 0),
(10020, N'Hành Boa-rô (Tỏi tây / Leek)', 80.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10021, N'Nấm rơm tươi', 855.00, N'g', N'Thái lát mỏng', 0),
(10021, N'Cà rốt củ tươi', 425.00, N'g', N'Thái lát mỏng', 0),
(10021, N'Củ cải trắng tươi', 210.00, N'g', N'Thái lát mỏng', 0),
(10021, N'Cà tím dài', 645.00, N'g', N'Ngâm nước ấm', 0),
(10021, N'Hành Boa-rô (Tỏi tây / Leek)', 104.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10022, N'Đậu hũ trắng (Tofu)', 400.00, N'g', NULL, 0),
(10022, N'Đậu nành hạt khô', 100.00, N'g', N'Ngâm nước ấm', 0),
(10022, N'Đậu Hà Lan hạt tươi', 100.00, N'g', NULL, 0),
(10022, N'Nấm mộc nhĩ khô (Nấm mèo)', 50.00, N'g', N'Băm nhỏ', 0),
(10022, N'Hành lá tươi (Hành hoa)', 45.00, N'g', NULL, 0),
(10022, N'Hạt nêm nấm chay bào ngư', 10.00, N'g', N'Thái lát mỏng', 0),
(10022, N'Dầu đậu nành nguyên chất', 15.00, N'ml', NULL, 0),
(10023, N'Đậu hũ trắng (Tofu)', 250.00, N'g', N'Thái lát mỏng', 0),
(10023, N'Đậu nành hạt khô', 65.00, N'g', NULL, 0),
(10023, N'Đậu Hà Lan hạt tươi', 80.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10023, N'Nấm mộc nhĩ khô (Nấm mèo)', 40.00, N'g', NULL, 0),
(10023, N'Hành lá tươi (Hành hoa)', 58.50, N'g', NULL, 0),
(10023, N'Hạt nêm nấm chay bào ngư', 13.00, N'g', N'Cắt miếng vừa ăn', 0),
(10023, N'Dầu đậu nành nguyên chất', 7.50, N'ml', N'Băm nhỏ', 0),
(10024, N'Bơ lạt từ sữa bò', 15.00, N'g', NULL, 0),
(10024, N'Nấm hương tươi (Shiitake)', 300.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10024, N'Cà rốt củ tươi', 300.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10024, N'Rau mùi tươi (Ngò rí)', 8.00, N'g', N'Thái lát mỏng', 0),
(10024, N'Hành Boa-rô (Tỏi tây / Leek)', 15.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10024, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Cắt miếng vừa ăn', 0),
(10024, N'Dầu đậu nành nguyên chất', 15.00, N'ml', N'Ngâm nước ấm', 0),
(10024, N'Muối tinh', 3.00, N'g', NULL, 0),
(10024, N'Tiêu đen xay nguyên chất', 1.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10025, N'Bơ lạt từ sữa bò', 55.00, N'g', NULL, 0),
(10025, N'Nấm hương tươi (Shiitake)', 885.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10025, N'Cà rốt củ tươi', 885.00, N'g', NULL, 0),
(10025, N'Rau mùi tươi (Ngò rí)', 8.00, N'g', NULL, 0),
(10025, N'Hành Boa-rô (Tỏi tây / Leek)', 15.00, N'g', NULL, 0),
(10025, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', NULL, 0),
(10025, N'Dầu đậu nành nguyên chất', 55.00, N'ml', N'Băm nhỏ', 0),
(10025, N'Muối tinh', 3.00, N'g', NULL, 0),
(10025, N'Tiêu đen xay nguyên chất', 1.00, N'g', N'Thái lát mỏng', 0),
(10026, N'Đậu hũ trắng (Tofu)', 400.00, N'g', N'Thái lát mỏng', 0),
(10026, N'Nấm rơm tươi', 200.00, N'g', N'Ngâm nước ấm', 0),
(10026, N'Củ cải trắng tươi', 30.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10026, N'Bí đỏ hạt đậu (Bí ngô)', 350.00, N'g', NULL, 0),
(10026, N'Cà chua chín đỏ', 300.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10026, N'Bắp cải trắng', 30.00, N'g', N'Băm nhỏ', 0),
(10026, N'Ngò gai tươi (Mùi tàu)', 5.00, N'g', NULL, 0),
(10026, N'Rau thơm các loại', 15.00, N'g', NULL, 0),
(10026, N'Hành Boa-rô (Tỏi tây / Leek)', 80.00, N'g', N'Ngâm nước ấm', 0),
(10026, N'Bún gạo khô', 150.00, N'g', N'Cắt miếng vừa ăn', 0),
(10026, N'Dứa (thơm)', 75.00, N'g', N'Thái lát mỏng', 0),
(10026, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Cắt miếng vừa ăn', 0),
(10026, N'Sả cây tươi', 60.00, N'g', NULL, 0),
(10026, N'Nghệ củ tươi', 20.00, N'g', N'Thái lát mỏng', 0),
(10027, N'Đậu hũ trắng (Tofu)', 540.00, N'g', NULL, 0),
(10027, N'Nấm rơm tươi', 200.00, N'g', NULL, 0)
) AS v(rid, nm, qty, u, nt, op)
JOIN #ing i ON i.Ingredient_Name = v.nm;
IF @@ROWCOUNT <> 250 THROW 50001, N'RecipeIngredients: tra cứu dữ liệu master thất bại (thiếu tên/mã)', 1;
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional)
SELECT v.rid, i.Ingredient_ID, v.qty, v.u, v.nt, v.op
FROM (VALUES
(10027, N'Củ cải trắng tươi', 30.00, N'g', NULL, 0),
(10027, N'Bí đỏ hạt đậu (Bí ngô)', 350.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10027, N'Cà chua chín đỏ', 300.00, N'g', N'Ngâm nước ấm', 0),
(10027, N'Bắp cải trắng', 30.00, N'g', N'Thái lát mỏng', 0),
(10027, N'Ngò gai tươi (Mùi tàu)', 5.00, N'g', N'Ngâm nước ấm', 0),
(10027, N'Rau thơm các loại', 15.00, N'g', N'Ngâm nước ấm', 0),
(10027, N'Hành Boa-rô (Tỏi tây / Leek)', 80.00, N'g', N'Cắt miếng vừa ăn', 0),
(10027, N'Bún gạo khô', 187.50, N'g', NULL, 0),
(10027, N'Dứa (thơm)', 75.00, N'g', NULL, 0),
(10027, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Cắt miếng vừa ăn', 0),
(10027, N'Sả cây tươi', 60.00, N'g', NULL, 0),
(10027, N'Nghệ củ tươi', 20.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10028, N'Đậu hũ trắng (Tofu)', 240.00, N'g', NULL, 0),
(10028, N'Nấm rơm tươi', 155.00, N'g', N'Thái lát mỏng', 0),
(10028, N'Củ cải trắng tươi', 25.00, N'g', NULL, 0),
(10028, N'Bí đỏ hạt đậu (Bí ngô)', 270.00, N'g', N'Ngâm nước ấm', 0),
(10028, N'Cà chua chín đỏ', 235.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10028, N'Bắp cải trắng', 25.00, N'g', NULL, 0),
(10028, N'Ngò gai tươi (Mùi tàu)', 6.50, N'g', NULL, 0),
(10028, N'Rau thơm các loại', 19.50, N'g', N'Cắt miếng vừa ăn', 0),
(10028, N'Hành Boa-rô (Tỏi tây / Leek)', 104.00, N'g', NULL, 0),
(10028, N'Bún gạo khô', 65.00, N'g', NULL, 0),
(10028, N'Dứa (thơm)', 60.00, N'g', NULL, 0),
(10028, N'Hạt nêm nấm chay bào ngư', 6.50, N'g', N'Thái lát mỏng', 0),
(10028, N'Sả cây tươi', 78.00, N'g', N'Băm nhỏ', 0),
(10028, N'Nghệ củ tươi', 26.00, N'g', N'Cắt miếng vừa ăn', 0),
(10029, N'Đậu hũ chiên vàng', 150.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10029, N'Nấm hương tươi (Shiitake)', 50.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10029, N'Cà rốt củ tươi', 20.00, N'g', N'Thái lát mỏng', 0),
(10029, N'Bắp cải trắng', 20.00, N'g', NULL, 0),
(10029, N'Hẹ lá tươi', 20.00, N'g', NULL, 0),
(10029, N'Dầu hào chay nấm hương', 15.00, N'ml', NULL, 0),
(10029, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', NULL, 0),
(10029, N'Muối tinh', 5.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10029, N'Đường cát trắng', 10.00, N'g', N'Cắt miếng vừa ăn', 0),
(10030, N'Đậu hũ chiên vàng', 485.00, N'g', NULL, 0),
(10030, N'Nấm hương tươi (Shiitake)', 120.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10030, N'Cà rốt củ tươi', 50.00, N'g', N'Băm nhỏ', 0),
(10030, N'Bắp cải trắng', 50.00, N'g', NULL, 0),
(10030, N'Hẹ lá tươi', 20.00, N'g', NULL, 0),
(10030, N'Dầu hào chay nấm hương', 15.00, N'ml', N'Ngâm nước ấm', 0),
(10030, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', NULL, 0),
(10030, N'Muối tinh', 5.00, N'g', N'Cắt miếng vừa ăn', 0),
(10030, N'Đường cát trắng', 10.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10031, N'Đậu hũ chiên vàng', 320.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10031, N'Nấm hương tươi (Shiitake)', 105.00, N'g', N'Băm nhỏ', 0),
(10031, N'Cà rốt củ tươi', 45.00, N'g', N'Cắt miếng vừa ăn', 0),
(10031, N'Bắp cải trắng', 45.00, N'g', N'Thái lát mỏng', 0),
(10031, N'Hẹ lá tươi', 20.00, N'g', N'Thái lát mỏng', 0),
(10031, N'Dầu hào chay nấm hương', 15.00, N'ml', N'Băm nhỏ', 0),
(10031, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Ngâm nước ấm', 0),
(10031, N'Muối tinh', 5.00, N'g', N'Ngâm nước ấm', 0),
(10031, N'Đường cát trắng', 10.00, N'g', N'Thái lát mỏng', 0),
(10032, N'Chả lụa chay (từ đậu xanh & bột mì)', 100.00, N'g', N'Thái lát mỏng', 0),
(10032, N'Mì căn tươi (Seitan đạm lúa mì)', 100.00, N'g', N'Cắt miếng vừa ăn', 0),
(10032, N'Cà rốt củ tươi', 100.00, N'g', N'Băm nhỏ', 0),
(10032, N'Rau thơm các loại', 15.00, N'g', NULL, 0),
(10032, N'Bánh tráng gỏi cuốn', 60.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10032, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', NULL, 0),
(10032, N'Ớt hiểm tươi cay', 5.00, N'g', N'Ngâm nước ấm', 0),
(10033, N'Chả lụa chay (từ đậu xanh & bột mì)', 325.00, N'g', N'Ngâm nước ấm', 0),
(10033, N'Mì căn tươi (Seitan đạm lúa mì)', 325.00, N'g', N'Thái lát mỏng', 0),
(10033, N'Cà rốt củ tươi', 240.00, N'g', NULL, 0),
(10033, N'Rau thơm các loại', 15.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10033, N'Bánh tráng gỏi cuốn', 180.00, N'g', N'Băm nhỏ', 0),
(10033, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', NULL, 0),
(10033, N'Ớt hiểm tươi cay', 5.00, N'g', N'Thái lát mỏng', 0),
(10034, N'Chả lụa chay (từ đậu xanh & bột mì)', 205.00, N'g', N'Cắt miếng vừa ăn', 0),
(10034, N'Mì căn tươi (Seitan đạm lúa mì)', 205.00, N'g', N'Thái lát mỏng', 0),
(10034, N'Cà rốt củ tươi', 205.00, N'g', N'Thái lát mỏng', 0),
(10034, N'Rau thơm các loại', 15.00, N'g', NULL, 0),
(10034, N'Bánh tráng gỏi cuốn', 125.00, N'g', NULL, 0),
(10034, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', NULL, 0),
(10034, N'Ớt hiểm tươi cay', 5.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10035, N'Nấm rơm tươi', 250.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10035, N'Nấm hương tươi (Shiitake)', 50.00, N'g', N'Cắt miếng vừa ăn', 0),
(10035, N'Cà rốt củ tươi', 200.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10035, N'Khoai tây tươi', 300.00, N'g', N'Băm nhỏ', 0),
(10035, N'Muối tinh', 3.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10036, N'Nấm rơm tươi', 850.00, N'g', NULL, 0),
(10036, N'Nấm hương tươi (Shiitake)', 170.00, N'g', N'Băm nhỏ', 0),
(10036, N'Cà rốt củ tươi', 680.00, N'g', N'Ngâm nước ấm', 0),
(10036, N'Khoai tây tươi', 1275.00, N'g', N'Ngâm nước ấm', 0),
(10036, N'Muối tinh', 3.00, N'g', N'Băm nhỏ', 0),
(10037, N'Nấm rơm tươi', 580.00, N'g', N'Cắt miếng vừa ăn', 0),
(10037, N'Nấm hương tươi (Shiitake)', 115.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10037, N'Cà rốt củ tươi', 465.00, N'g', NULL, 0),
(10037, N'Khoai tây tươi', 400.00, N'g', N'Thái lát mỏng', 0),
(10037, N'Muối tinh', 3.90, N'g', N'Cắt miếng vừa ăn', 0),
(10038, N'Đậu hũ trắng (Tofu)', 200.00, N'g', NULL, 0),
(10038, N'Nấm rơm tươi', 100.00, N'g', N'Thái lát mỏng', 0),
(10038, N'Cà chua chín đỏ', 300.00, N'g', N'Thái lát mỏng', 0),
(10038, N'Hành tím củ (Hành ta)', 15.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10039, N'Đậu hũ trắng (Tofu)', 1460.00, N'g', N'Ngâm nước ấm', 0),
(10039, N'Nấm rơm tươi', 540.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10039, N'Cà chua chín đỏ', 1615.00, N'g', N'Ngâm nước ấm', 0),
(10039, N'Hành tím củ (Hành ta)', 15.00, N'g', NULL, 0),
(10040, N'Đậu Hà Lan hạt tươi', 240.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10040, N'Cà rốt củ tươi', 120.00, N'g', N'Băm nhỏ', 0),
(10040, N'Bông cải trắng (Cauliflower)', 120.00, N'g', NULL, 0),
(10040, N'Hành tím củ (Hành ta)', 80.00, N'g', N'Cắt miếng vừa ăn', 0),
(10040, N'Tỏi củ tươi', 30.00, N'g', NULL, 0),
(10040, N'Dầu đậu nành nguyên chất', 15.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10040, N'Muối tinh', 3.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10041, N'Đậu Hà Lan hạt tươi', 805.00, N'g', NULL, 0),
(10041, N'Cà rốt củ tươi', 405.00, N'g', N'Băm nhỏ', 0),
(10041, N'Bông cải trắng (Cauliflower)', 405.00, N'g', N'Thái lát mỏng', 0),
(10041, N'Hành tím củ (Hành ta)', 80.00, N'g', NULL, 0),
(10041, N'Tỏi củ tươi', 30.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10041, N'Dầu đậu nành nguyên chất', 65.00, N'ml', NULL, 0),
(10041, N'Muối tinh', 3.00, N'g', N'Ngâm nước ấm', 0),
(10042, N'Đậu Hà Lan hạt tươi', 430.00, N'g', NULL, 0),
(10042, N'Cà rốt củ tươi', 215.00, N'g', N'Thái lát mỏng', 0),
(10042, N'Bông cải trắng (Cauliflower)', 215.00, N'g', N'Băm nhỏ', 0),
(10042, N'Hành tím củ (Hành ta)', 104.00, N'g', NULL, 0),
(10042, N'Tỏi củ tươi', 39.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10042, N'Dầu đậu nành nguyên chất', 7.50, N'ml', N'Ngâm nước ấm', 0),
(10042, N'Muối tinh', 3.90, N'g', NULL, 0),
(10043, N'Cà rốt củ tươi', 100.00, N'g', NULL, 0),
(10043, N'Bắp cải tím', 80.00, N'g', NULL, 0),
(10043, N'Bắp cải trắng', 80.00, N'g', NULL, 0),
(10043, N'Hạt mè trắng (Vừng trắng)', 20.00, N'g', N'Thái lát mỏng', 0),
(10043, N'Nước tương đậu nành (Xì dầu)', 40.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10043, N'Dầu đậu nành nguyên chất', 15.00, N'ml', N'Thái lát mỏng', 0),
(10043, N'Đường cát trắng', 8.00, N'g', N'Băm nhỏ', 0),
(10043, N'Giấm gạo', 20.00, N'g', NULL, 0),
(10044, N'Đậu Hà Lan hạt tươi', 200.00, N'g', N'Thái lát mỏng', 0),
(10044, N'Cà rốt củ tươi', 200.00, N'g', N'Ngâm nước ấm', 0),
(10044, N'Bông cải xanh (Broccoli)', 10.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10044, N'Hành tây củ trắng', 60.00, N'g', N'Băm nhỏ', 0),
(10044, N'Tỏi củ tươi', 10.00, N'g', N'Ngâm nước ấm', 0),
(10044, N'Hạt mè trắng (Vừng trắng)', 10.00, N'g', N'Thái lát mỏng', 0),
(10044, N'Ớt chuông', 150.00, N'g', NULL, 0),
(10044, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', NULL, 0),
(10044, N'Dầu đậu nành nguyên chất', 15.00, N'ml', NULL, 0),
(10045, N'Đậu Hà Lan hạt tươi', 405.00, N'g', NULL, 0),
(10045, N'Cà rốt củ tươi', 405.00, N'g', NULL, 0),
(10045, N'Bông cải xanh (Broccoli)', 10.00, N'g', N'Thái lát mỏng', 0),
(10045, N'Hành tây củ trắng', 60.00, N'g', N'Cắt miếng vừa ăn', 0),
(10045, N'Tỏi củ tươi', 10.00, N'g', N'Thái lát mỏng', 0),
(10045, N'Hạt mè trắng (Vừng trắng)', 10.00, N'g', NULL, 0),
(10045, N'Ớt chuông', 300.00, N'g', N'Thái lát mỏng', 0),
(10045, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Cắt miếng vừa ăn', 0),
(10045, N'Dầu đậu nành nguyên chất', 30.00, N'ml', NULL, 0),
(10046, N'Bò lát chay khô (Đạm đậu nành TVP)', 200.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10046, N'Đậu phộng hạt khô (Lạc)', 30.00, N'g', NULL, 0),
(10046, N'Xà lách lụa tươi', 100.00, N'g', NULL, 0),
(10046, N'Rau mùi tươi (Ngò rí)', 8.00, N'g', N'Thái lát mỏng', 0),
(10046, N'Húng quế', 8.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10046, N'Rau thơm các loại', 15.00, N'g', NULL, 0),
(10046, N'Hành tím củ (Hành ta)', 15.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10046, N'Hành tây củ trắng', 60.00, N'g', N'Ngâm nước ấm', 0),
(10046, N'Giá đỗ', 100.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10046, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Ngâm nước ấm', 0),
(10046, N'Dầu đậu nành nguyên chất', 15.00, N'ml', NULL, 0),
(10046, N'Tiêu đen xay nguyên chất', 1.00, N'g', NULL, 0),
(10046, N'Gừng củ tươi', 10.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10046, N'Sả cây tươi', 15.00, N'g', N'Băm nhỏ', 0),
(10047, N'Bò lát chay khô (Đạm đậu nành TVP)', 270.00, N'g', NULL, 0),
(10047, N'Đậu phộng hạt khô (Lạc)', 40.50, N'g', NULL, 0),
(10047, N'Xà lách lụa tươi', 100.00, N'g', N'Băm nhỏ', 0),
(10047, N'Rau mùi tươi (Ngò rí)', 8.00, N'g', N'Ngâm nước ấm', 0),
(10047, N'Húng quế', 8.00, N'g', NULL, 0),
(10047, N'Rau thơm các loại', 15.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10047, N'Hành tím củ (Hành ta)', 15.00, N'g', N'Thái lát mỏng', 0),
(10047, N'Hành tây củ trắng', 60.00, N'g', N'Thái lát mỏng', 0),
(10047, N'Giá đỗ', 100.00, N'g', N'Cắt miếng vừa ăn', 0),
(10047, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Thái lát mỏng', 0),
(10047, N'Dầu đậu nành nguyên chất', 19.50, N'ml', N'Ngâm nước ấm', 0),
(10047, N'Tiêu đen xay nguyên chất', 1.00, N'g', NULL, 0),
(10047, N'Gừng củ tươi', 10.00, N'g', NULL, 0),
(10047, N'Sả cây tươi', 15.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10048, N'Đậu hũ trắng (Tofu)', 300.00, N'g', N'Ngâm nước ấm', 0),
(10048, N'Nấm rơm tươi', 300.00, N'g', NULL, 0),
(10048, N'Cà chua chín đỏ', 75.00, N'g', N'Băm nhỏ', 0),
(10048, N'Tỏi củ tươi', 20.00, N'g', N'Thái lát mỏng', 0),
(10048, N'Bột bắp (bột ngô)', 50.00, N'g', N'Cắt miếng vừa ăn', 0),
(10048, N'Hạt nêm nấm chay bào ngư', 2.50, N'g', N'Ngâm nước ấm', 0),
(10048, N'Dầu đậu nành nguyên chất', 50.00, N'ml', NULL, 0),
(10048, N'Đường cát trắng', 8.00, N'g', N'Cắt miếng vừa ăn', 0),
(10048, N'Tiêu đen xay nguyên chất', 1.00, N'g', N'Thái lát mỏng', 0),
(10048, N'Ớt hiểm tươi cay', 10.00, N'g', N'Ngâm nước ấm', 0),
(10049, N'Đậu hũ trắng (Tofu)', 495.00, N'g', NULL, 0),
(10049, N'Nấm rơm tươi', 365.00, N'g', NULL, 0),
(10049, N'Cà chua chín đỏ', 90.00, N'g', N'Cắt miếng vừa ăn', 0),
(10049, N'Tỏi củ tươi', 20.00, N'g', N'Ngâm nước ấm', 0),
(10049, N'Bột bắp (bột ngô)', 75.00, N'g', N'Thái lát mỏng', 0),
(10049, N'Hạt nêm nấm chay bào ngư', 2.50, N'g', N'Ngâm nước ấm', 0),
(10049, N'Dầu đậu nành nguyên chất', 80.00, N'ml', NULL, 0),
(10049, N'Đường cát trắng', 8.00, N'g', N'Băm nhỏ', 0),
(10049, N'Tiêu đen xay nguyên chất', 1.00, N'g', N'Băm nhỏ', 0),
(10049, N'Ớt hiểm tươi cay', 10.00, N'g', N'Ngâm nước ấm', 0),
(10050, N'Đậu hũ trắng (Tofu)', 300.00, N'g', NULL, 0),
(10050, N'Nấm rơm tươi', 390.00, N'g', N'Cắt miếng vừa ăn', 0),
(10050, N'Cà chua chín đỏ', 97.50, N'g', N'Ngâm nước ấm', 0),
(10050, N'Tỏi củ tươi', 26.00, N'g', N'Băm nhỏ', 0),
(10050, N'Bột bắp (bột ngô)', 37.50, N'g', NULL, 0),
(10050, N'Hạt nêm nấm chay bào ngư', 3.20, N'g', N'Ngâm nước ấm', 0),
(10050, N'Dầu đậu nành nguyên chất', 25.00, N'ml', NULL, 0),
(10050, N'Đường cát trắng', 4.00, N'g', NULL, 0),
(10050, N'Tiêu đen xay nguyên chất', 1.30, N'g', NULL, 0),
(10050, N'Ớt hiểm tươi cay', 13.00, N'g', N'Băm nhỏ', 0),
(10051, N'Đậu hũ chiên vàng', 150.00, N'g', NULL, 0),
(10051, N'Chả lụa chay (từ đậu xanh & bột mì)', 350.00, N'g', NULL, 0),
(10051, N'Nấm rơm tươi', 350.00, N'g', NULL, 0),
(10051, N'Cà rốt củ tươi', 100.00, N'g', N'Băm nhỏ', 0),
(10051, N'Khoai tây tươi', 100.00, N'g', N'Ngâm nước ấm', 0),
(10051, N'Khoai lang mật', 100.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10051, N'Su su quả tươi', 100.00, N'g', NULL, 0),
(10051, N'Hành Boa-rô (Tỏi tây / Leek)', 80.00, N'g', NULL, 0),
(10051, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', NULL, 0),
(10051, N'Dầu đậu nành nguyên chất', 15.00, N'ml', N'Thái lát mỏng', 0),
(10051, N'Muối tinh', 3.00, N'g', NULL, 0),
(10051, N'Đường cát trắng', 16.00, N'g', NULL, 0),
(10051, N'Tiêu đen xay nguyên chất', 1.00, N'g', N'Ngâm nước ấm', 0),
(10052, N'Đậu hũ chiên vàng', 65.00, N'g', NULL, 0),
(10052, N'Chả lụa chay (từ đậu xanh & bột mì)', 165.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10052, N'Nấm rơm tươi', 210.00, N'g', N'Băm nhỏ', 0),
(10052, N'Cà rốt củ tươi', 60.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10052, N'Khoai tây tươi', 35.00, N'g', N'Thái lát mỏng', 0),
(10052, N'Khoai lang mật', 35.00, N'g', N'Thái lát mỏng', 0),
(10052, N'Su su quả tươi', 60.00, N'g', NULL, 0),
(10052, N'Hành Boa-rô (Tỏi tây / Leek)', 104.00, N'g', NULL, 0),
(10052, N'Nước tương đậu nành (Xì dầu)', 19.50, N'ml', N'Cắt miếng vừa ăn', 0),
(10052, N'Dầu đậu nành nguyên chất', 7.50, N'ml', NULL, 0),
(10052, N'Muối tinh', 3.90, N'g', NULL, 0),
(10052, N'Đường cát trắng', 8.00, N'g', N'Ngâm nước ấm', 0),
(10052, N'Tiêu đen xay nguyên chất', 1.30, N'g', N'Ngâm nước ấm', 0),
(10053, N'Sườn non chay khô (Đạm đậu nành TVP)', 300.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10053, N'Cà chua chín đỏ', 300.00, N'g', N'Băm nhỏ', 0),
(10053, N'Dưa leo tươi (Dưa chuột)', 150.00, N'g', N'Thái lát mỏng', 0),
(10053, N'Xà lách lụa tươi', 15.00, N'g', N'Cắt miếng vừa ăn', 0),
(10053, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Ngâm nước ấm', 0),
(10053, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Băm nhỏ', 0),
(10053, N'Dầu đậu nành nguyên chất', 15.00, N'ml', N'Thái lát mỏng', 0),
(10053, N'Muối tinh', 3.00, N'g', N'Thái lát mỏng', 0),
(10053, N'Đường cát trắng', 8.00, N'g', N'Băm nhỏ', 0),
(10053, N'Sả cây tươi', 45.00, N'g', NULL, 0),
(10054, N'Sườn non chay khô (Đạm đậu nành TVP)', 165.00, N'g', N'Thái lát mỏng', 0),
(10054, N'Cà chua chín đỏ', 215.00, N'g', N'Băm nhỏ', 0),
(10054, N'Dưa leo tươi (Dưa chuột)', 110.00, N'g', NULL, 0),
(10054, N'Xà lách lụa tươi', 10.00, N'g', NULL, 0),
(10054, N'Nước tương đậu nành (Xì dầu)', 19.50, N'ml', N'Băm nhỏ', 0),
(10054, N'Hạt nêm nấm chay bào ngư', 6.50, N'g', N'Cắt miếng vừa ăn', 0),
(10054, N'Dầu đậu nành nguyên chất', 7.50, N'ml', NULL, 0),
(10054, N'Muối tinh', 3.90, N'g', N'Băm nhỏ', 0),
(10054, N'Đường cát trắng', 4.00, N'g', NULL, 0),
(10054, N'Sả cây tươi', 58.50, N'g', N'Cắt miếng vừa ăn', 0),
(10055, N'Nấm mộc nhĩ khô (Nấm mèo)', 30.00, N'g', N'Cắt miếng vừa ăn', 0),
(10055, N'Hành Boa-rô (Tỏi tây / Leek)', 80.00, N'g', N'Rửa sạch, cắt khúc', 0)
) AS v(rid, nm, qty, u, nt, op)
JOIN #ing i ON i.Ingredient_Name = v.nm;
IF @@ROWCOUNT <> 250 THROW 50001, N'RecipeIngredients: tra cứu dữ liệu master thất bại (thiếu tên/mã)', 1;
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional)
SELECT v.rid, i.Ingredient_ID, v.qty, v.u, v.nt, v.op
FROM (VALUES
(10055, N'Miến dong khô', 100.00, N'g', NULL, 0),
(10055, N'Nước mắm chay (từ dứa & nấm rơm)', 15.00, N'ml', NULL, 0),
(10055, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Cắt miếng vừa ăn', 0),
(10055, N'Muối tinh', 3.00, N'g', N'Băm nhỏ', 0),
(10055, N'Ớt hiểm tươi cay', 5.00, N'g', NULL, 0),
(10056, N'Nấm mộc nhĩ khô (Nấm mèo)', 65.00, N'g', NULL, 0),
(10056, N'Hành Boa-rô (Tỏi tây / Leek)', 104.00, N'g', NULL, 0),
(10056, N'Miến dong khô', 120.00, N'g', N'Ngâm nước ấm', 0),
(10056, N'Nước mắm chay (từ dứa & nấm rơm)', 19.50, N'ml', N'Cắt miếng vừa ăn', 0),
(10056, N'Hạt nêm nấm chay bào ngư', 6.50, N'g', NULL, 0),
(10056, N'Muối tinh', 3.90, N'g', N'Băm nhỏ', 0),
(10056, N'Ớt hiểm tươi cay', 6.50, N'g', N'Thái lát mỏng', 0),
(10057, N'Đậu hũ non (Silken Tofu)', 200.00, N'g', N'Ngâm nước ấm', 0),
(10057, N'Nấm hương tươi (Shiitake)', 100.00, N'g', N'Ngâm nước ấm', 0),
(10057, N'Cải thìa tươi (Cải chíp)', 200.00, N'g', NULL, 0),
(10057, N'Hạt mè trắng (Vừng trắng)', 10.00, N'g', N'Thái lát mỏng', 0),
(10057, N'Bột năng (Bột sắn)', 20.00, N'g', N'Băm nhỏ', 0),
(10057, N'Bột bắp (bột ngô)', 20.00, N'g', NULL, 0),
(10057, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Băm nhỏ', 0),
(10057, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10057, N'Dầu đậu nành nguyên chất', 30.00, N'ml', N'Cắt miếng vừa ăn', 0),
(10057, N'Đường cát trắng', 8.00, N'g', NULL, 0),
(10057, N'Tiêu đen xay nguyên chất', 1.00, N'g', NULL, 0),
(10057, N'Gừng củ tươi', 60.00, N'g', N'Ngâm nước ấm', 0),
(10058, N'Đậu hũ non (Silken Tofu)', 290.00, N'g', N'Băm nhỏ', 0),
(10058, N'Nấm hương tươi (Shiitake)', 145.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10058, N'Cải thìa tươi (Cải chíp)', 290.00, N'g', N'Băm nhỏ', 0),
(10058, N'Hạt mè trắng (Vừng trắng)', 10.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10058, N'Bột năng (Bột sắn)', 30.00, N'g', NULL, 0),
(10058, N'Bột bắp (bột ngô)', 30.00, N'g', N'Băm nhỏ', 0),
(10058, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', NULL, 0),
(10058, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', NULL, 0),
(10058, N'Dầu đậu nành nguyên chất', 45.00, N'ml', NULL, 0),
(10058, N'Đường cát trắng', 8.00, N'g', N'Băm nhỏ', 0),
(10058, N'Tiêu đen xay nguyên chất', 1.00, N'g', N'Thái lát mỏng', 0),
(10058, N'Gừng củ tươi', 60.00, N'g', NULL, 0),
(10059, N'Đậu hũ non (Silken Tofu)', 475.00, N'g', N'Ngâm nước ấm', 0),
(10059, N'Nấm hương tươi (Shiitake)', 175.00, N'g', N'Thái lát mỏng', 0),
(10059, N'Cải thìa tươi (Cải chíp)', 350.00, N'g', N'Băm nhỏ', 0),
(10059, N'Hạt mè trắng (Vừng trắng)', 10.00, N'g', N'Ngâm nước ấm', 0),
(10059, N'Bột năng (Bột sắn)', 45.00, N'g', N'Ngâm nước ấm', 0),
(10059, N'Bột bắp (bột ngô)', 45.00, N'g', NULL, 0),
(10059, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Băm nhỏ', 0),
(10059, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', NULL, 0),
(10059, N'Dầu đậu nành nguyên chất', 70.00, N'ml', N'Băm nhỏ', 0),
(10059, N'Đường cát trắng', 8.00, N'g', N'Ngâm nước ấm', 0),
(10059, N'Tiêu đen xay nguyên chất', 1.00, N'g', N'Cắt miếng vừa ăn', 0),
(10059, N'Gừng củ tươi', 60.00, N'g', NULL, 0),
(10060, N'Dưa leo tươi (Dưa chuột)', 300.00, N'g', N'Băm nhỏ', 0),
(10060, N'Tỏi củ tươi', 20.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10060, N'Hạt mè trắng (Vừng trắng)', 10.00, N'g', N'Băm nhỏ', 0),
(10060, N'Bún gạo khô', 250.00, N'g', N'Ngâm nước ấm', 0),
(10060, N'Nước mắm chay (từ dứa & nấm rơm)', 15.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10060, N'Dầu đậu nành nguyên chất', 15.00, N'ml', NULL, 0),
(10060, N'Đường cát trắng', 8.00, N'g', N'Cắt miếng vừa ăn', 0),
(10060, N'Ớt hiểm tươi cay', 5.00, N'g', N'Cắt miếng vừa ăn', 0),
(10060, N'Gừng củ tươi', 10.00, N'g', N'Ngâm nước ấm', 0),
(10060, N'Giấm gạo', 10.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10061, N'Dưa leo tươi (Dưa chuột)', 300.00, N'g', N'Cắt miếng vừa ăn', 0),
(10061, N'Tỏi củ tươi', 20.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10061, N'Hạt mè trắng (Vừng trắng)', 10.00, N'g', NULL, 0),
(10061, N'Bún gạo khô', 312.50, N'g', N'Cắt miếng vừa ăn', 0),
(10061, N'Nước mắm chay (từ dứa & nấm rơm)', 15.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10061, N'Dầu đậu nành nguyên chất', 19.50, N'ml', N'Thái lát mỏng', 0),
(10061, N'Đường cát trắng', 8.00, N'g', N'Cắt miếng vừa ăn', 0),
(10061, N'Ớt hiểm tươi cay', 5.00, N'g', NULL, 0),
(10061, N'Gừng củ tươi', 10.00, N'g', N'Thái lát mỏng', 0),
(10061, N'Giấm gạo', 10.00, N'g', N'Ngâm nước ấm', 0),
(10062, N'Đậu hũ chiên vàng', 300.00, N'g', N'Băm nhỏ', 0),
(10062, N'Đậu hũ trắng (Tofu)', 200.00, N'g', N'Cắt miếng vừa ăn', 0),
(10062, N'Đậu phộng hạt khô (Lạc)', 30.00, N'g', N'Băm nhỏ', 0),
(10062, N'Nấm hương tươi (Shiitake)', 50.00, N'g', N'Thái lát mỏng', 0),
(10062, N'Nấm mộc nhĩ khô (Nấm mèo)', 10.00, N'g', NULL, 0),
(10062, N'Rau mùi tươi (Ngò rí)', 24.00, N'g', NULL, 0),
(10062, N'Hành Boa-rô (Tỏi tây / Leek)', 80.00, N'g', N'Băm nhỏ', 0),
(10062, N'Miến dong khô', 100.00, N'g', NULL, 0),
(10062, N'Bột năng (Bột sắn)', 20.00, N'g', NULL, 0),
(10062, N'Me chua', 80.00, N'g', N'Ngâm nước ấm', 0),
(10062, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', NULL, 0),
(10062, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', NULL, 0),
(10062, N'Dầu đậu nành nguyên chất', 15.00, N'ml', N'Thái lát mỏng', 0),
(10062, N'Muối tinh', 3.00, N'g', N'Thái lát mỏng', 0),
(10062, N'Đường cát trắng', 8.00, N'g', N'Cắt miếng vừa ăn', 0),
(10062, N'Tiêu đen xay nguyên chất', 1.00, N'g', N'Băm nhỏ', 0),
(10062, N'Ớt hiểm tươi cay', 10.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10063, N'Đậu hũ chiên vàng', 250.00, N'g', N'Cắt miếng vừa ăn', 0),
(10063, N'Đậu hũ trắng (Tofu)', 165.00, N'g', N'Ngâm nước ấm', 0),
(10063, N'Đậu phộng hạt khô (Lạc)', 25.00, N'g', N'Thái lát mỏng', 0),
(10063, N'Nấm hương tươi (Shiitake)', 30.00, N'g', NULL, 0),
(10063, N'Nấm mộc nhĩ khô (Nấm mèo)', 10.00, N'g', NULL, 0),
(10063, N'Rau mùi tươi (Ngò rí)', 24.00, N'g', N'Cắt miếng vừa ăn', 0),
(10063, N'Hành Boa-rô (Tỏi tây / Leek)', 80.00, N'g', N'Ngâm nước ấm', 0),
(10063, N'Miến dong khô', 75.00, N'g', NULL, 0),
(10063, N'Bột năng (Bột sắn)', 15.00, N'g', NULL, 0),
(10063, N'Me chua', 80.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10063, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', NULL, 0),
(10063, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Băm nhỏ', 0),
(10063, N'Dầu đậu nành nguyên chất', 10.00, N'ml', N'Băm nhỏ', 0),
(10063, N'Muối tinh', 3.00, N'g', NULL, 0),
(10063, N'Đường cát trắng', 8.00, N'g', N'Thái lát mỏng', 0),
(10063, N'Tiêu đen xay nguyên chất', 1.00, N'g', N'Thái lát mỏng', 0),
(10064, N'Đậu hũ chiên vàng', 250.00, N'g', N'Thái lát mỏng', 0),
(10064, N'Đậu hũ trắng (Tofu)', 165.00, N'g', N'Thái lát mỏng', 0),
(10064, N'Đậu phộng hạt khô (Lạc)', 25.00, N'g', N'Băm nhỏ', 0),
(10064, N'Nấm hương tươi (Shiitake)', 30.00, N'g', NULL, 0),
(10064, N'Nấm mộc nhĩ khô (Nấm mèo)', 10.00, N'g', N'Cắt miếng vừa ăn', 0),
(10064, N'Rau mùi tươi (Ngò rí)', 24.00, N'g', N'Thái lát mỏng', 0),
(10064, N'Hành Boa-rô (Tỏi tây / Leek)', 80.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10064, N'Miến dong khô', 75.00, N'g', N'Băm nhỏ', 0),
(10064, N'Bột năng (Bột sắn)', 15.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10064, N'Me chua', 80.00, N'g', N'Cắt miếng vừa ăn', 0),
(10064, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', NULL, 0),
(10064, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Thái lát mỏng', 0),
(10064, N'Dầu đậu nành nguyên chất', 10.00, N'ml', N'Ngâm nước ấm', 0),
(10064, N'Muối tinh', 3.00, N'g', N'Băm nhỏ', 0),
(10064, N'Đường cát trắng', 8.00, N'g', N'Thái lát mỏng', 0),
(10064, N'Tiêu đen xay nguyên chất', 1.00, N'g', N'Thái lát mỏng', 0),
(10064, N'Ớt hiểm tươi cay', 10.00, N'g', N'Ngâm nước ấm', 0),
(10065, N'Đậu hũ chiên vàng', 350.00, N'g', NULL, 0),
(10065, N'Nấm hương tươi (Shiitake)', 150.00, N'g', NULL, 0),
(10065, N'Nấm mộc nhĩ khô (Nấm mèo)', 30.00, N'g', NULL, 0),
(10065, N'Hạt mè trắng (Vừng trắng)', 10.00, N'g', N'Cắt miếng vừa ăn', 0),
(10065, N'Bột năng (Bột sắn)', 20.00, N'g', N'Băm nhỏ', 0),
(10065, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10065, N'Dầu hào chay nấm hương', 15.00, N'ml', NULL, 0),
(10065, N'Tương hột đậu nành lên men', 15.00, N'g', NULL, 0),
(10065, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10065, N'Dầu đậu nành nguyên chất', 15.00, N'ml', N'Ngâm nước ấm', 0),
(10065, N'Muối tinh', 3.00, N'g', NULL, 0),
(10065, N'Đường cát trắng', 8.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10065, N'Tiêu đen xay nguyên chất', 1.00, N'g', NULL, 0),
(10066, N'Đậu hũ chiên vàng', 260.00, N'g', NULL, 0),
(10066, N'Nấm hương tươi (Shiitake)', 110.00, N'g', NULL, 0),
(10066, N'Nấm mộc nhĩ khô (Nấm mèo)', 20.00, N'g', N'Ngâm nước ấm', 0),
(10066, N'Hạt mè trắng (Vừng trắng)', 10.00, N'g', NULL, 0),
(10066, N'Bột năng (Bột sắn)', 15.00, N'g', NULL, 0),
(10066, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10066, N'Dầu hào chay nấm hương', 15.00, N'ml', N'Thái lát mỏng', 0),
(10066, N'Tương hột đậu nành lên men', 15.00, N'g', NULL, 0),
(10066, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', NULL, 0),
(10066, N'Dầu đậu nành nguyên chất', 10.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10066, N'Muối tinh', 3.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10066, N'Đường cát trắng', 8.00, N'g', N'Ngâm nước ấm', 0),
(10066, N'Tiêu đen xay nguyên chất', 1.00, N'g', N'Băm nhỏ', 0),
(10067, N'Tàu hũ ky khô', 200.00, N'g', NULL, 0),
(10067, N'Nấm mộc nhĩ khô (Nấm mèo)', 30.00, N'g', NULL, 0),
(10067, N'Xà lách lụa tươi', 100.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10067, N'Rau mùi tươi (Ngò rí)', 8.00, N'g', NULL, 0),
(10067, N'Rau thơm các loại', 15.00, N'g', NULL, 0),
(10067, N'Gạo nếp', 300.00, N'g', N'Băm nhỏ', 0),
(10068, N'Tàu hũ ky khô', 175.00, N'g', N'Băm nhỏ', 0),
(10068, N'Nấm mộc nhĩ khô (Nấm mèo)', 20.00, N'g', NULL, 0),
(10068, N'Xà lách lụa tươi', 65.00, N'g', N'Thái lát mỏng', 0),
(10068, N'Rau mùi tươi (Ngò rí)', 8.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10068, N'Rau thơm các loại', 15.00, N'g', N'Băm nhỏ', 0),
(10068, N'Gạo nếp', 240.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10069, N'Đậu hũ non (Silken Tofu)', 200.00, N'g', NULL, 0),
(10069, N'Nấm hương tươi (Shiitake)', 50.00, N'g', NULL, 0),
(10069, N'Dầu hào chay nấm hương', 15.00, N'ml', N'Băm nhỏ', 0),
(10069, N'Muối tinh', 3.00, N'g', NULL, 0),
(10069, N'Đường cát trắng', 8.00, N'g', NULL, 0),
(10069, N'Ớt hiểm tươi cay', 5.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10070, N'Đậu hũ non (Silken Tofu)', 985.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10070, N'Nấm hương tươi (Shiitake)', 320.00, N'g', N'Ngâm nước ấm', 0),
(10070, N'Dầu hào chay nấm hương', 19.50, N'ml', N'Cắt miếng vừa ăn', 0),
(10070, N'Muối tinh', 3.90, N'g', N'Ngâm nước ấm', 0),
(10070, N'Đường cát trắng', 4.00, N'g', NULL, 0),
(10070, N'Ớt hiểm tươi cay', 6.50, N'g', N'Băm nhỏ', 0),
(10071, N'Đậu hũ non (Silken Tofu)', 2460.00, N'g', NULL, 0),
(10071, N'Nấm hương tươi (Shiitake)', 455.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10071, N'Dầu hào chay nấm hương', 15.00, N'ml', N'Thái lát mỏng', 0),
(10071, N'Muối tinh', 3.00, N'g', NULL, 0),
(10071, N'Đường cát trắng', 8.00, N'g', N'Băm nhỏ', 0),
(10072, N'Đậu que (Đậu cô ve)', 100.00, N'g', N'Thái lát mỏng', 0),
(10072, N'Đậu bắp tươi', 100.00, N'g', N'Thái lát mỏng', 0),
(10072, N'Nấm rơm tươi', 100.00, N'g', NULL, 0),
(10072, N'Nấm hương tươi (Shiitake)', 100.00, N'g', NULL, 0),
(10072, N'Nước mắm chay (từ dứa & nấm rơm)', 15.00, N'ml', NULL, 0),
(10072, N'Đường cát trắng', 8.00, N'g', N'Thái lát mỏng', 0),
(10072, N'Tiêu đen xay nguyên chất', 1.00, N'g', N'Băm nhỏ', 0),
(10073, N'Đậu que (Đậu cô ve)', 1160.00, N'g', N'Ngâm nước ấm', 0),
(10073, N'Đậu bắp tươi', 1160.00, N'g', N'Thái lát mỏng', 0),
(10073, N'Nấm rơm tươi', 1160.00, N'g', N'Ngâm nước ấm', 0),
(10073, N'Nấm hương tươi (Shiitake)', 1160.00, N'g', N'Cắt miếng vừa ăn', 0),
(10073, N'Nước mắm chay (từ dứa & nấm rơm)', 15.00, N'ml', N'Thái lát mỏng', 0),
(10073, N'Đường cát trắng', 8.00, N'g', NULL, 0),
(10073, N'Tiêu đen xay nguyên chất', 1.00, N'g', NULL, 0),
(10074, N'Đậu que (Đậu cô ve)', 520.00, N'g', NULL, 0),
(10074, N'Đậu bắp tươi', 520.00, N'g', NULL, 0),
(10074, N'Nấm rơm tươi', 520.00, N'g', NULL, 0),
(10074, N'Nấm hương tươi (Shiitake)', 520.00, N'g', N'Ngâm nước ấm', 0),
(10074, N'Nước mắm chay (từ dứa & nấm rơm)', 19.50, N'ml', NULL, 0),
(10074, N'Đường cát trắng', 4.00, N'g', N'Ngâm nước ấm', 0),
(10074, N'Tiêu đen xay nguyên chất', 1.30, N'g', N'Băm nhỏ', 0),
(10075, N'Tàu hũ ky khô', 130.00, N'g', N'Thái lát mỏng', 0),
(10075, N'Đậu nành hạt khô', 100.00, N'g', NULL, 0),
(10075, N'Đậu Hà Lan hạt tươi', 50.00, N'g', NULL, 0),
(10075, N'Nấm hương tươi (Shiitake)', 50.00, N'g', N'Ngâm nước ấm', 0),
(10075, N'Nấm mộc nhĩ khô (Nấm mèo)', 10.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10075, N'Cà rốt củ tươi', 50.00, N'g', NULL, 0),
(10075, N'Rau mùi tươi (Ngò rí)', 8.00, N'g', NULL, 0),
(10075, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', NULL, 0),
(10075, N'Dầu đậu nành nguyên chất', 15.00, N'ml', NULL, 0),
(10075, N'Muối tinh', 3.00, N'g', NULL, 0),
(10075, N'Tiêu đen xay nguyên chất', 1.00, N'g', N'Thái lát mỏng', 0),
(10076, N'Tàu hũ ky khô', 175.50, N'g', NULL, 0),
(10076, N'Đậu nành hạt khô', 135.00, N'g', NULL, 0),
(10076, N'Đậu Hà Lan hạt tươi', 50.00, N'g', N'Cắt miếng vừa ăn', 0),
(10076, N'Nấm hương tươi (Shiitake)', 50.00, N'g', NULL, 0),
(10076, N'Nấm mộc nhĩ khô (Nấm mèo)', 10.00, N'g', N'Băm nhỏ', 0),
(10076, N'Cà rốt củ tươi', 50.00, N'g', N'Băm nhỏ', 0),
(10076, N'Rau mùi tươi (Ngò rí)', 8.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10076, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Cắt miếng vừa ăn', 0),
(10076, N'Dầu đậu nành nguyên chất', 19.50, N'ml', N'Ngâm nước ấm', 0),
(10076, N'Muối tinh', 3.00, N'g', NULL, 0),
(10076, N'Tiêu đen xay nguyên chất', 1.00, N'g', N'Ngâm nước ấm', 0),
(10077, N'Tàu hũ ky khô', 80.00, N'g', NULL, 0),
(10077, N'Đậu nành hạt khô', 60.00, N'g', N'Ngâm nước ấm', 0),
(10077, N'Đậu Hà Lan hạt tươi', 40.00, N'g', N'Thái lát mỏng', 0),
(10077, N'Nấm hương tươi (Shiitake)', 40.00, N'g', N'Ngâm nước ấm', 0),
(10077, N'Nấm mộc nhĩ khô (Nấm mèo)', 13.00, N'g', N'Băm nhỏ', 0),
(10077, N'Cà rốt củ tươi', 40.00, N'g', N'Thái lát mỏng', 0),
(10077, N'Rau mùi tươi (Ngò rí)', 10.40, N'g', N'Ngâm nước ấm', 0),
(10077, N'Hạt nêm nấm chay bào ngư', 6.50, N'g', N'Thái lát mỏng', 0),
(10077, N'Dầu đậu nành nguyên chất', 7.50, N'ml', N'Ngâm nước ấm', 0),
(10077, N'Muối tinh', 3.90, N'g', N'Băm nhỏ', 0),
(10077, N'Tiêu đen xay nguyên chất', 1.30, N'g', NULL, 0),
(10078, N'Đậu hũ trắng (Tofu)', 400.00, N'g', NULL, 0),
(10078, N'Nấm rơm tươi', 200.00, N'g', N'Ngâm nước ấm', 0),
(10078, N'Củ cải trắng tươi', 30.00, N'g', N'Thái lát mỏng', 0),
(10078, N'Bí đỏ hạt đậu (Bí ngô)', 350.00, N'g', N'Ngâm nước ấm', 0),
(10078, N'Cà chua chín đỏ', 300.00, N'g', NULL, 0),
(10078, N'Bắp cải trắng', 30.00, N'g', NULL, 0),
(10078, N'Ngò gai tươi (Mùi tàu)', 5.00, N'g', NULL, 0),
(10078, N'Rau thơm các loại', 15.00, N'g', NULL, 0),
(10078, N'Hành Boa-rô (Tỏi tây / Leek)', 80.00, N'g', NULL, 0),
(10078, N'Dứa (thơm)', 75.00, N'g', NULL, 0),
(10078, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', NULL, 0),
(10078, N'Sả cây tươi', 60.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10078, N'Nghệ củ tươi', 20.00, N'g', N'Cắt miếng vừa ăn', 0),
(10079, N'Đậu hũ trắng (Tofu)', 615.00, N'g', N'Thái lát mỏng', 0),
(10079, N'Nấm rơm tươi', 310.00, N'g', NULL, 0),
(10079, N'Củ cải trắng tươi', 45.00, N'g', NULL, 0),
(10079, N'Bí đỏ hạt đậu (Bí ngô)', 540.00, N'g', N'Băm nhỏ', 0),
(10079, N'Cà chua chín đỏ', 460.00, N'g', NULL, 0),
(10079, N'Bắp cải trắng', 45.00, N'g', NULL, 0),
(10079, N'Ngò gai tươi (Mùi tàu)', 5.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10079, N'Rau thơm các loại', 15.00, N'g', NULL, 0),
(10079, N'Hành Boa-rô (Tỏi tây / Leek)', 80.00, N'g', N'Thái lát mỏng', 0),
(10079, N'Dứa (thơm)', 115.00, N'g', N'Thái lát mỏng', 0)
) AS v(rid, nm, qty, u, nt, op)
JOIN #ing i ON i.Ingredient_Name = v.nm;
IF @@ROWCOUNT <> 250 THROW 50001, N'RecipeIngredients: tra cứu dữ liệu master thất bại (thiếu tên/mã)', 1;
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional)
SELECT v.rid, i.Ingredient_ID, v.qty, v.u, v.nt, v.op
FROM (VALUES
(10079, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Cắt miếng vừa ăn', 0),
(10079, N'Sả cây tươi', 60.00, N'g', NULL, 0),
(10079, N'Nghệ củ tươi', 20.00, N'g', N'Ngâm nước ấm', 0),
(10080, N'Đậu hũ trắng (Tofu)', 200.00, N'g', N'Ngâm nước ấm', 0),
(10080, N'Nấm rơm tươi', 50.00, N'g', N'Thái lát mỏng', 0),
(10080, N'Hành Boa-rô (Tỏi tây / Leek)', 10.00, N'g', N'Thái lát mỏng', 0),
(10080, N'Bột năng (Bột sắn)', 20.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10081, N'Đậu hũ trắng (Tofu)', 625.00, N'g', NULL, 0),
(10081, N'Nấm rơm tươi', 200.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10081, N'Hành Boa-rô (Tỏi tây / Leek)', 13.00, N'g', NULL, 0),
(10081, N'Bột năng (Bột sắn)', 45.00, N'g', NULL, 0),
(10082, N'Đậu hũ trắng (Tofu)', 1340.00, N'g', N'Thái lát mỏng', 0),
(10082, N'Nấm rơm tươi', 250.00, N'g', N'Ngâm nước ấm', 0),
(10082, N'Hành Boa-rô (Tỏi tây / Leek)', 10.00, N'g', N'Cắt miếng vừa ăn', 0),
(10082, N'Bột năng (Bột sắn)', 125.00, N'g', N'Cắt miếng vừa ăn', 0),
(10083, N'Đậu hũ trắng (Tofu)', 200.00, N'g', N'Cắt miếng vừa ăn', 0),
(10083, N'Mì căn tươi (Seitan đạm lúa mì)', 180.00, N'g', N'Cắt miếng vừa ăn', 0),
(10083, N'Đậu phộng hạt khô (Lạc)', 60.00, N'g', NULL, 0),
(10083, N'Nấm mộc nhĩ khô (Nấm mèo)', 10.00, N'g', NULL, 0),
(10083, N'Củ cải trắng tươi', 100.00, N'g', N'Thái lát mỏng', 0),
(10083, N'Hoa chuối thái mỏng', 100.00, N'g', NULL, 0),
(10083, N'Bắp cải trắng', 80.00, N'g', N'Cắt miếng vừa ăn', 0),
(10083, N'Húng quế', 8.00, N'g', NULL, 0),
(10083, N'Rau thơm các loại', 15.00, N'g', N'Thái lát mỏng', 0),
(10083, N'Hành tím củ (Hành ta)', 15.00, N'g', NULL, 0),
(10083, N'Hành tây củ trắng', 60.00, N'g', N'Băm nhỏ', 0),
(10083, N'Bánh phở tươi', 200.00, N'g', N'Băm nhỏ', 0),
(10083, N'Bánh tráng gỏi cuốn', 30.00, N'g', N'Ngâm nước ấm', 0),
(10083, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Băm nhỏ', 0),
(10083, N'Dầu đậu nành nguyên chất', 15.00, N'ml', NULL, 0),
(10083, N'Muối tinh', 3.00, N'g', NULL, 0),
(10083, N'Đường cát trắng', 8.00, N'g', NULL, 0),
(10083, N'Nghệ củ tươi', 5.00, N'g', N'Băm nhỏ', 0),
(10084, N'Đậu hũ trắng (Tofu)', 160.00, N'g', N'Cắt miếng vừa ăn', 0),
(10084, N'Mì căn tươi (Seitan đạm lúa mì)', 145.00, N'g', NULL, 0),
(10084, N'Đậu phộng hạt khô (Lạc)', 50.00, N'g', NULL, 0),
(10084, N'Nấm mộc nhĩ khô (Nấm mèo)', 10.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10084, N'Củ cải trắng tươi', 80.00, N'g', N'Thái lát mỏng', 0),
(10084, N'Hoa chuối thái mỏng', 80.00, N'g', N'Thái lát mỏng', 0),
(10084, N'Bắp cải trắng', 65.00, N'g', NULL, 0),
(10084, N'Húng quế', 8.00, N'g', N'Ngâm nước ấm', 0),
(10084, N'Rau thơm các loại', 15.00, N'g', NULL, 0),
(10084, N'Hành tím củ (Hành ta)', 15.00, N'g', N'Băm nhỏ', 0),
(10084, N'Hành tây củ trắng', 60.00, N'g', N'Thái lát mỏng', 0),
(10084, N'Bánh phở tươi', 160.00, N'g', N'Cắt miếng vừa ăn', 0),
(10084, N'Bánh tráng gỏi cuốn', 25.00, N'g', N'Cắt miếng vừa ăn', 0),
(10084, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10084, N'Dầu đậu nành nguyên chất', 10.00, N'ml', NULL, 0),
(10084, N'Muối tinh', 3.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10084, N'Đường cát trắng', 8.00, N'g', NULL, 0),
(10084, N'Nghệ củ tươi', 5.00, N'g', N'Cắt miếng vừa ăn', 0),
(10085, N'Đậu xanh bóc vỏ', 100.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10085, N'Nấm linh chi nâu tươi', 100.00, N'g', NULL, 0),
(10085, N'Bí đỏ hạt đậu (Bí ngô)', 100.00, N'g', NULL, 0),
(10085, N'Khoai lang mật', 100.00, N'g', N'Ngâm nước ấm', 0),
(10085, N'Khoai môn cao', 100.00, N'g', NULL, 0),
(10085, N'Rau mùi tươi (Ngò rí)', 8.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10085, N'Gạo thơm lài miên', 200.00, N'g', N'Cắt miếng vừa ăn', 0),
(10085, N'Nước cốt dừa', 80.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10085, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10085, N'Dầu đậu nành nguyên chất', 15.00, N'ml', N'Băm nhỏ', 0),
(10085, N'Muối tinh', 3.00, N'g', NULL, 0),
(10086, N'Đậu xanh bóc vỏ', 55.00, N'g', N'Thái lát mỏng', 0),
(10086, N'Nấm linh chi nâu tươi', 75.00, N'g', NULL, 0),
(10086, N'Bí đỏ hạt đậu (Bí ngô)', 75.00, N'g', N'Thái lát mỏng', 0),
(10086, N'Khoai lang mật', 45.00, N'g', NULL, 0),
(10086, N'Khoai môn cao', 45.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10086, N'Rau mùi tươi (Ngò rí)', 10.40, N'g', N'Băm nhỏ', 0),
(10086, N'Gạo thơm lài miên', 85.00, N'g', N'Cắt miếng vừa ăn', 0),
(10086, N'Nước cốt dừa', 25.00, N'ml', NULL, 0),
(10086, N'Hạt nêm nấm chay bào ngư', 6.50, N'g', N'Rửa sạch, cắt khúc', 0),
(10086, N'Dầu đậu nành nguyên chất', 7.50, N'ml', NULL, 0),
(10086, N'Muối tinh', 3.90, N'g', N'Thái lát mỏng', 0),
(10087, N'Đậu xanh bóc vỏ', 65.00, N'g', N'Cắt miếng vừa ăn', 0),
(10087, N'Nấm linh chi nâu tươi', 65.00, N'g', NULL, 0),
(10087, N'Bí đỏ hạt đậu (Bí ngô)', 65.00, N'g', N'Thái lát mỏng', 0),
(10087, N'Khoai lang mật', 65.00, N'g', N'Ngâm nước ấm', 0),
(10087, N'Khoai môn cao', 65.00, N'g', N'Băm nhỏ', 0),
(10087, N'Rau mùi tươi (Ngò rí)', 8.00, N'g', N'Ngâm nước ấm', 0),
(10087, N'Gạo thơm lài miên', 130.00, N'g', NULL, 0),
(10087, N'Nước cốt dừa', 55.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10087, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Thái lát mỏng', 0),
(10087, N'Dầu đậu nành nguyên chất', 10.00, N'ml', N'Băm nhỏ', 0),
(10087, N'Muối tinh', 3.00, N'g', N'Thái lát mỏng', 0),
(10088, N'Đậu hũ chiên vàng', 150.00, N'g', N'Cắt miếng vừa ăn', 0),
(10088, N'Đậu phộng hạt khô (Lạc)', 30.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10088, N'Nấm mộc nhĩ khô (Nấm mèo)', 10.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10088, N'Cà rốt củ tươi', 200.00, N'g', N'Thái lát mỏng', 0),
(10088, N'Hành lá tươi (Hành hoa)', 15.00, N'g', N'Thái lát mỏng', 0),
(10088, N'Hành tây củ trắng', 60.00, N'g', N'Ngâm nước ấm', 0),
(10088, N'Tỏi củ tươi', 10.00, N'g', N'Cắt miếng vừa ăn', 0),
(10088, N'Gạo thơm lài miên', 300.00, N'g', N'Băm nhỏ', 0),
(10088, N'Bún gạo khô', 250.00, N'g', NULL, 0),
(10088, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Băm nhỏ', 0),
(10088, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', NULL, 0),
(10088, N'Muối tinh', 3.00, N'g', N'Thái lát mỏng', 0),
(10088, N'Đường cát trắng', 8.00, N'g', N'Cắt miếng vừa ăn', 0),
(10088, N'Tiêu đen xay nguyên chất', 1.00, N'g', NULL, 0),
(10089, N'Đậu hũ chiên vàng', 95.00, N'g', NULL, 0),
(10089, N'Đậu phộng hạt khô (Lạc)', 20.00, N'g', N'Thái lát mỏng', 0),
(10089, N'Nấm mộc nhĩ khô (Nấm mèo)', 13.00, N'g', N'Thái lát mỏng', 0),
(10089, N'Cà rốt củ tươi', 165.00, N'g', N'Ngâm nước ấm', 0),
(10089, N'Hành lá tươi (Hành hoa)', 19.50, N'g', NULL, 0),
(10089, N'Hành tây củ trắng', 78.00, N'g', N'Cắt miếng vừa ăn', 0),
(10089, N'Tỏi củ tươi', 13.00, N'g', N'Thái lát mỏng', 0),
(10089, N'Gạo thơm lài miên', 140.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10089, N'Bún gạo khô', 120.00, N'g', N'Băm nhỏ', 0),
(10089, N'Nước tương đậu nành (Xì dầu)', 19.50, N'ml', N'Ngâm nước ấm', 0),
(10089, N'Hạt nêm nấm chay bào ngư', 6.50, N'g', N'Ngâm nước ấm', 0),
(10089, N'Muối tinh', 3.90, N'g', NULL, 0),
(10089, N'Đường cát trắng', 4.00, N'g', NULL, 0),
(10089, N'Tiêu đen xay nguyên chất', 1.30, N'g', NULL, 0),
(10090, N'Đậu hũ trắng (Tofu)', 300.00, N'g', N'Ngâm nước ấm', 0),
(10090, N'Nấm rơm tươi', 300.00, N'g', NULL, 0),
(10090, N'Cà chua chín đỏ', 10.00, N'g', N'Băm nhỏ', 0),
(10090, N'Tỏi củ tươi', 10.00, N'g', NULL, 0),
(10090, N'Bột bắp (bột ngô)', 50.00, N'g', N'Thái lát mỏng', 0),
(10090, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10090, N'Dầu đậu nành nguyên chất', 50.00, N'ml', NULL, 0),
(10090, N'Đường cát trắng', 8.00, N'g', NULL, 0),
(10090, N'Tiêu đen xay nguyên chất', 1.00, N'g', N'Thái lát mỏng', 0),
(10090, N'Ớt hiểm tươi cay', 10.00, N'g', N'Ngâm nước ấm', 0),
(10091, N'Đậu hũ trắng (Tofu)', 500.00, N'g', N'Ngâm nước ấm', 0),
(10091, N'Nấm rơm tươi', 370.00, N'g', N'Ngâm nước ấm', 0),
(10091, N'Cà chua chín đỏ', 10.00, N'g', N'Thái lát mỏng', 0),
(10091, N'Tỏi củ tươi', 10.00, N'g', N'Cắt miếng vừa ăn', 0),
(10091, N'Bột bắp (bột ngô)', 75.00, N'g', N'Thái lát mỏng', 0),
(10091, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Ngâm nước ấm', 0),
(10091, N'Dầu đậu nành nguyên chất', 80.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10091, N'Đường cát trắng', 8.00, N'g', NULL, 0),
(10091, N'Tiêu đen xay nguyên chất', 1.00, N'g', N'Ngâm nước ấm', 0),
(10091, N'Ớt hiểm tươi cay', 10.00, N'g', NULL, 0),
(10092, N'Sữa tươi tiệt trùng bò', 80.00, N'g', NULL, 0),
(10092, N'Đậu hũ chiên vàng', 300.00, N'g', N'Băm nhỏ', 0),
(10092, N'Cà rốt củ tươi', 100.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10092, N'Khoai tây tươi', 200.00, N'g', N'Thái lát mỏng', 0),
(10092, N'Hành Boa-rô (Tỏi tây / Leek)', 20.00, N'g', NULL, 0),
(10092, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', NULL, 0),
(10092, N'Dầu đậu nành nguyên chất', 15.00, N'ml', NULL, 0),
(10092, N'Muối tinh', 20.00, N'g', N'Ngâm nước ấm', 0),
(10092, N'Đường cát trắng', 8.00, N'g', N'Cắt miếng vừa ăn', 0),
(10093, N'Sữa tươi tiệt trùng bò', 108.00, N'g', NULL, 0),
(10093, N'Đậu hũ chiên vàng', 405.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10093, N'Cà rốt củ tươi', 100.00, N'g', NULL, 0),
(10093, N'Khoai tây tươi', 250.00, N'g', N'Thái lát mỏng', 0),
(10093, N'Hành Boa-rô (Tỏi tây / Leek)', 20.00, N'g', NULL, 0),
(10093, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Thái lát mỏng', 0),
(10093, N'Dầu đậu nành nguyên chất', 19.50, N'ml', N'Cắt miếng vừa ăn', 0),
(10093, N'Muối tinh', 20.00, N'g', N'Băm nhỏ', 0),
(10093, N'Đường cát trắng', 8.00, N'g', N'Ngâm nước ấm', 0),
(10094, N'Trứng gà ta tươi', 50.00, N'g', N'Thái lát mỏng', 0),
(10094, N'Tàu hũ ky khô', 30.00, N'g', N'Băm nhỏ', 0),
(10094, N'Đậu que (Đậu cô ve)', 350.00, N'g', N'Băm nhỏ', 0),
(10094, N'Nấm hương tươi (Shiitake)', 10.00, N'g', N'Băm nhỏ', 0),
(10094, N'Cà rốt củ tươi', 100.00, N'g', N'Ngâm nước ấm', 0),
(10094, N'Măng tươi luộc chín', 30.00, N'g', N'Băm nhỏ', 0),
(10094, N'Hạt mè trắng (Vừng trắng)', 5.00, N'g', N'Cắt miếng vừa ăn', 0),
(10094, N'Gạo thơm lài miên', 200.00, N'g', NULL, 0),
(10094, N'Bột mì đa dụng', 50.00, N'g', N'Thái lát mỏng', 0),
(10094, N'Dầu đậu nành nguyên chất', 25.00, N'ml', NULL, 0),
(10094, N'Muối tinh', 5.00, N'g', N'Ngâm nước ấm', 0),
(10094, N'Tiêu đen xay nguyên chất', 1.00, N'g', N'Thái lát mỏng', 0),
(10095, N'Trứng gà ta tươi', 30.00, N'g', N'Thái lát mỏng', 0),
(10095, N'Tàu hũ ky khô', 20.00, N'g', NULL, 0),
(10095, N'Đậu que (Đậu cô ve)', 270.00, N'g', N'Thái lát mỏng', 0),
(10095, N'Nấm hương tươi (Shiitake)', 13.00, N'g', N'Ngâm nước ấm', 0),
(10095, N'Cà rốt củ tươi', 75.00, N'g', NULL, 0),
(10095, N'Măng tươi luộc chín', 25.00, N'g', N'Thái lát mỏng', 0),
(10095, N'Hạt mè trắng (Vừng trắng)', 6.50, N'g', NULL, 0),
(10095, N'Gạo thơm lài miên', 90.00, N'g', N'Cắt miếng vừa ăn', 0),
(10095, N'Bột mì đa dụng', 20.00, N'g', N'Ngâm nước ấm', 0),
(10095, N'Dầu đậu nành nguyên chất', 12.50, N'ml', NULL, 0),
(10095, N'Muối tinh', 6.50, N'g', N'Ngâm nước ấm', 0),
(10095, N'Tiêu đen xay nguyên chất', 1.30, N'g', N'Thái lát mỏng', 0),
(10096, N'Trứng gà ta tươi', 35.00, N'g', N'Băm nhỏ', 0),
(10096, N'Tàu hũ ky khô', 20.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10096, N'Đậu que (Đậu cô ve)', 250.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10096, N'Nấm hương tươi (Shiitake)', 10.00, N'g', N'Thái lát mỏng', 0),
(10096, N'Cà rốt củ tươi', 70.00, N'g', NULL, 0),
(10096, N'Măng tươi luộc chín', 20.00, N'g', NULL, 0),
(10096, N'Hạt mè trắng (Vừng trắng)', 5.00, N'g', NULL, 0),
(10096, N'Gạo thơm lài miên', 145.00, N'g', NULL, 0),
(10096, N'Bột mì đa dụng', 35.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10096, N'Dầu đậu nành nguyên chất', 20.00, N'ml', N'Cắt miếng vừa ăn', 0),
(10096, N'Muối tinh', 5.00, N'g', NULL, 0),
(10096, N'Tiêu đen xay nguyên chất', 1.00, N'g', NULL, 0),
(10097, N'Đậu hũ chiên vàng', 100.00, N'g', NULL, 0),
(10097, N'Đậu bắp tươi', 60.00, N'g', NULL, 0),
(10097, N'Nấm rơm tươi', 200.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10097, N'Nấm hương tươi (Shiitake)', 50.00, N'g', N'Cắt miếng vừa ăn', 0),
(10097, N'Hạt sen tươi', 100.00, N'g', N'Cắt miếng vừa ăn', 0),
(10097, N'Rau mùi tươi (Ngò rí)', 8.00, N'g', N'Cắt miếng vừa ăn', 0),
(10097, N'Nước cốt dừa', 250.00, N'ml', NULL, 0),
(10097, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Băm nhỏ', 0),
(10097, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Cắt miếng vừa ăn', 0),
(10097, N'Dầu đậu nành nguyên chất', 15.00, N'ml', N'Ngâm nước ấm', 0),
(10097, N'Đường cát trắng', 8.00, N'g', N'Thái lát mỏng', 0),
(10097, N'Tiêu đen xay nguyên chất', 1.00, N'g', N'Thái lát mỏng', 0),
(10097, N'Ớt hiểm tươi cay', 80.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10097, N'Sả cây tươi', 15.00, N'g', N'Ngâm nước ấm', 0),
(10098, N'Đậu hũ trắng (Tofu)', 200.00, N'g', N'Ngâm nước ấm', 0),
(10098, N'Rong biển khô (Nori)', 8.00, N'g', NULL, 0),
(10098, N'Nước mắm chay (từ dứa & nấm rơm)', 15.00, N'ml', N'Băm nhỏ', 0),
(10098, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Băm nhỏ', 0),
(10098, N'Dầu đậu nành nguyên chất', 15.00, N'ml', N'Cắt miếng vừa ăn', 0),
(10098, N'Tiêu đen xay nguyên chất', 2.50, N'g', NULL, 0),
(10098, N'Ớt hiểm tươi cay', 5.00, N'g', NULL, 0),
(10099, N'Đậu hũ non (Silken Tofu)', 200.00, N'g', N'Cắt miếng vừa ăn', 0),
(10099, N'Nấm hương tươi (Shiitake)', 50.00, N'g', NULL, 0),
(10099, N'Dầu hào chay nấm hương', 15.00, N'ml', N'Ngâm nước ấm', 0),
(10099, N'Muối tinh', 3.00, N'g', NULL, 0),
(10099, N'Đường cát trắng', 8.00, N'g', NULL, 0),
(10099, N'Ớt hiểm tươi cay', 5.00, N'g', N'Cắt miếng vừa ăn', 0),
(10100, N'Đậu hũ non (Silken Tofu)', 2460.00, N'g', N'Ngâm nước ấm', 0),
(10100, N'Nấm hương tươi (Shiitake)', 455.00, N'g', N'Băm nhỏ', 0),
(10100, N'Dầu hào chay nấm hương', 15.00, N'ml', N'Thái lát mỏng', 0),
(10100, N'Muối tinh', 3.00, N'g', NULL, 0),
(10100, N'Đường cát trắng', 8.00, N'g', N'Ngâm nước ấm', 0),
(10100, N'Ớt hiểm tươi cay', 5.00, N'g', NULL, 0),
(10101, N'Nấm mộc nhĩ khô (Nấm mèo)', 50.00, N'g', N'Băm nhỏ', 0),
(10101, N'Cà rốt củ tươi', 100.00, N'g', NULL, 0),
(10101, N'Bắp cải tím', 100.00, N'g', N'Băm nhỏ', 0),
(10101, N'Hạt mè trắng (Vừng trắng)', 5.00, N'g', NULL, 0),
(10101, N'Bún gạo khô', 100.00, N'g', N'Băm nhỏ', 0),
(10101, N'Ớt chuông', 300.00, N'g', N'Ngâm nước ấm', 0),
(10101, N'Nước tương đậu nành (Xì dầu)', 10.00, N'ml', N'Cắt miếng vừa ăn', 0),
(10101, N'Hạt nêm nấm chay bào ngư', 3.00, N'g', N'Thái lát mỏng', 0),
(10101, N'Dầu đậu nành nguyên chất', 35.00, N'ml', NULL, 0),
(10102, N'Nấm mộc nhĩ khô (Nấm mèo)', 55.00, N'g', N'Thái lát mỏng', 0),
(10102, N'Cà rốt củ tươi', 110.00, N'g', N'Thái lát mỏng', 0),
(10102, N'Bắp cải tím', 110.00, N'g', NULL, 0),
(10102, N'Hạt mè trắng (Vừng trắng)', 6.50, N'g', N'Thái lát mỏng', 0),
(10102, N'Bún gạo khô', 60.00, N'g', NULL, 0),
(10102, N'Ớt chuông', 325.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10102, N'Nước tương đậu nành (Xì dầu)', 13.00, N'ml', NULL, 0),
(10102, N'Hạt nêm nấm chay bào ngư', 3.90, N'g', N'Ngâm nước ấm', 0),
(10102, N'Dầu đậu nành nguyên chất', 15.00, N'ml', N'Ngâm nước ấm', 0),
(10103, N'Đậu hũ trắng (Tofu)', 400.00, N'g', NULL, 0),
(10103, N'Nấm hương tươi (Shiitake)', 50.00, N'g', N'Ngâm nước ấm', 0),
(10103, N'Hành tím củ (Hành ta)', 15.00, N'g', N'Thái lát mỏng', 0),
(10103, N'Tỏi củ tươi', 20.00, N'g', NULL, 0),
(10103, N'Nước tương đậu nành (Xì dầu)', 10.00, N'ml', NULL, 0),
(10103, N'Dầu hào chay nấm hương', 10.00, N'ml', NULL, 0),
(10103, N'Dầu đậu nành nguyên chất', 10.00, N'ml', N'Thái lát mỏng', 0),
(10103, N'Muối tinh', 13.00, N'g', N'Ngâm nước ấm', 0),
(10103, N'Đường cát trắng', 10.00, N'g', NULL, 0),
(10103, N'Gừng củ tươi', 10.00, N'g', N'Ngâm nước ấm', 0),
(10104, N'Đậu hũ trắng (Tofu)', 1655.00, N'g', NULL, 0),
(10104, N'Nấm hương tươi (Shiitake)', 155.00, N'g', NULL, 0),
(10104, N'Hành tím củ (Hành ta)', 15.00, N'g', N'Băm nhỏ', 0)
) AS v(rid, nm, qty, u, nt, op)
JOIN #ing i ON i.Ingredient_Name = v.nm;
IF @@ROWCOUNT <> 250 THROW 50001, N'RecipeIngredients: tra cứu dữ liệu master thất bại (thiếu tên/mã)', 1;
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional)
SELECT v.rid, i.Ingredient_ID, v.qty, v.u, v.nt, v.op
FROM (VALUES
(10104, N'Tỏi củ tươi', 20.00, N'g', NULL, 0),
(10104, N'Nước tương đậu nành (Xì dầu)', 10.00, N'ml', NULL, 0),
(10104, N'Dầu hào chay nấm hương', 10.00, N'ml', N'Cắt miếng vừa ăn', 0),
(10104, N'Dầu đậu nành nguyên chất', 13.00, N'ml', N'Thái lát mỏng', 0),
(10104, N'Muối tinh', 13.00, N'g', N'Cắt miếng vừa ăn', 0),
(10104, N'Đường cát trắng', 10.00, N'g', N'Thái lát mỏng', 0),
(10104, N'Gừng củ tươi', 10.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10105, N'Đậu hũ trắng (Tofu)', 1050.00, N'g', N'Cắt miếng vừa ăn', 0),
(10105, N'Nấm hương tươi (Shiitake)', 130.00, N'g', NULL, 0),
(10105, N'Hành tím củ (Hành ta)', 15.00, N'g', N'Thái lát mỏng', 0),
(10105, N'Tỏi củ tươi', 20.00, N'g', N'Ngâm nước ấm', 0),
(10105, N'Nước tương đậu nành (Xì dầu)', 10.00, N'ml', N'Băm nhỏ', 0),
(10105, N'Dầu hào chay nấm hương', 10.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10105, N'Dầu đậu nành nguyên chất', 10.00, N'ml', N'Băm nhỏ', 0),
(10105, N'Muối tinh', 13.00, N'g', N'Cắt miếng vừa ăn', 0),
(10105, N'Đường cát trắng', 10.00, N'g', N'Băm nhỏ', 0),
(10105, N'Gừng củ tươi', 10.00, N'g', NULL, 0),
(10106, N'Đậu hũ chiên vàng', 350.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10106, N'Nấm hương tươi (Shiitake)', 150.00, N'g', NULL, 0),
(10106, N'Nấm mộc nhĩ khô (Nấm mèo)', 30.00, N'g', N'Thái lát mỏng', 0),
(10106, N'Hành Boa-rô (Tỏi tây / Leek)', 30.00, N'g', N'Cắt miếng vừa ăn', 0),
(10106, N'Hạt mè trắng (Vừng trắng)', 10.00, N'g', N'Ngâm nước ấm', 0),
(10106, N'Bột năng (Bột sắn)', 20.00, N'g', N'Cắt miếng vừa ăn', 0),
(10106, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', NULL, 0),
(10106, N'Dầu hào chay nấm hương', 15.00, N'ml', N'Thái lát mỏng', 0),
(10106, N'Tương hột đậu nành lên men', 15.00, N'g', N'Ngâm nước ấm', 0),
(10106, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Ngâm nước ấm', 0),
(10106, N'Dầu đậu nành nguyên chất', 15.00, N'ml', NULL, 0),
(10106, N'Muối tinh', 3.00, N'g', N'Băm nhỏ', 0),
(10106, N'Đường cát trắng', 8.00, N'g', N'Băm nhỏ', 0),
(10106, N'Tiêu đen xay nguyên chất', 1.00, N'g', N'Ngâm nước ấm', 0),
(10107, N'Chả lụa chay (từ đậu xanh & bột mì)', 200.00, N'g', NULL, 0),
(10107, N'Đậu bắp tươi', 300.00, N'g', NULL, 0),
(10107, N'Nấm hương tươi (Shiitake)', 50.00, N'g', NULL, 0),
(10107, N'Hạt sen tươi', 100.00, N'g', NULL, 0),
(10107, N'Rau mùi tươi (Ngò rí)', 8.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10107, N'Hành Boa-rô (Tỏi tây / Leek)', 80.00, N'g', NULL, 0),
(10107, N'Nước cốt dừa', 100.00, N'ml', NULL, 0),
(10107, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Thái lát mỏng', 0),
(10107, N'Hạt nêm nấm chay bào ngư', 10.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10107, N'Dầu đậu nành nguyên chất', 15.00, N'ml', N'Cắt miếng vừa ăn', 0),
(10107, N'Đường cát trắng', 8.00, N'g', NULL, 0),
(10107, N'Tiêu đen xay nguyên chất', 1.00, N'g', N'Ngâm nước ấm', 0),
(10107, N'Ớt hiểm tươi cay', 10.00, N'g', NULL, 0),
(10107, N'Sả cây tươi', 30.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10108, N'Chả lụa chay (từ đậu xanh & bột mì)', 200.00, N'g', NULL, 0),
(10108, N'Đậu bắp tươi', 300.00, N'g', NULL, 0),
(10108, N'Nấm hương tươi (Shiitake)', 50.00, N'g', N'Cắt miếng vừa ăn', 0),
(10108, N'Hạt sen tươi', 100.00, N'g', NULL, 0),
(10108, N'Rau mùi tươi (Ngò rí)', 8.00, N'g', NULL, 0),
(10108, N'Hành Boa-rô (Tỏi tây / Leek)', 80.00, N'g', N'Băm nhỏ', 0),
(10108, N'Nước cốt dừa', 100.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10108, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Cắt miếng vừa ăn', 0),
(10108, N'Hạt nêm nấm chay bào ngư', 10.00, N'g', N'Thái lát mỏng', 0),
(10108, N'Dầu đậu nành nguyên chất', 15.00, N'ml', N'Băm nhỏ', 0),
(10108, N'Đường cát trắng', 8.00, N'g', NULL, 0),
(10108, N'Tiêu đen xay nguyên chất', 1.00, N'g', NULL, 0),
(10108, N'Sả cây tươi', 30.00, N'g', N'Băm nhỏ', 0),
(10109, N'Chả lụa chay (từ đậu xanh & bột mì)', 365.00, N'g', N'Băm nhỏ', 0),
(10109, N'Đậu bắp tươi', 405.00, N'g', NULL, 0),
(10109, N'Nấm hương tươi (Shiitake)', 70.00, N'g', N'Băm nhỏ', 0),
(10109, N'Hạt sen tươi', 135.00, N'g', N'Thái lát mỏng', 0),
(10109, N'Rau mùi tươi (Ngò rí)', 8.00, N'g', N'Cắt miếng vừa ăn', 0),
(10109, N'Hành Boa-rô (Tỏi tây / Leek)', 80.00, N'g', NULL, 0),
(10109, N'Nước cốt dừa', 175.00, N'ml', NULL, 0),
(10109, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10109, N'Hạt nêm nấm chay bào ngư', 10.00, N'g', NULL, 0),
(10109, N'Dầu đậu nành nguyên chất', 25.00, N'ml', N'Băm nhỏ', 0),
(10109, N'Đường cát trắng', 8.00, N'g', N'Cắt miếng vừa ăn', 0),
(10109, N'Tiêu đen xay nguyên chất', 1.00, N'g', NULL, 0),
(10109, N'Ớt hiểm tươi cay', 10.00, N'g', N'Băm nhỏ', 0),
(10109, N'Sả cây tươi', 30.00, N'g', NULL, 0),
(10110, N'Đậu hũ non (Silken Tofu)', 400.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10110, N'Cà tím dài', 300.00, N'g', N'Ngâm nước ấm', 0),
(10110, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', NULL, 0),
(10110, N'Dầu đậu nành nguyên chất', 15.00, N'ml', N'Thái lát mỏng', 0),
(10110, N'Muối tinh', 3.00, N'g', NULL, 0),
(10110, N'Đường cát trắng', 8.00, N'g', NULL, 0),
(10111, N'Đậu hũ non (Silken Tofu)', 705.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10111, N'Cà tím dài', 685.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10111, N'Nước tương đậu nành (Xì dầu)', 19.50, N'ml', NULL, 0),
(10111, N'Dầu đậu nành nguyên chất', 7.50, N'ml', N'Ngâm nước ấm', 0),
(10111, N'Muối tinh', 3.90, N'g', NULL, 0),
(10111, N'Đường cát trắng', 4.00, N'g', NULL, 0),
(10112, N'Đậu hũ chiên vàng', 300.00, N'g', N'Băm nhỏ', 0),
(10112, N'Đậu phộng hạt khô (Lạc)', 30.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10112, N'Nấm kim châm tươi', 150.00, N'g', NULL, 0),
(10112, N'Cà rốt củ tươi', 150.00, N'g', N'Thái lát mỏng', 0),
(10112, N'Húng quế', 50.00, N'g', NULL, 0),
(10112, N'Bánh phở tươi', 300.00, N'g', NULL, 0),
(10112, N'Bột năng (Bột sắn)', 20.00, N'g', NULL, 0),
(10112, N'Me chua', 10.00, N'g', N'Ngâm nước ấm', 0),
(10112, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10112, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', NULL, 0),
(10112, N'Dầu đậu nành nguyên chất', 30.00, N'ml', N'Ngâm nước ấm', 0),
(10112, N'Muối tinh', 3.00, N'g', NULL, 0),
(10112, N'Đường cát trắng', 8.00, N'g', N'Ngâm nước ấm', 0),
(10112, N'Ớt hiểm tươi cay', 55.00, N'g', NULL, 0),
(10112, N'Nghệ củ tươi', 5.00, N'g', N'Băm nhỏ', 0),
(10113, N'Đậu hũ trắng (Tofu)', 400.00, N'g', NULL, 0),
(10113, N'Đậu nành hạt khô', 100.00, N'g', N'Thái lát mỏng', 0),
(10113, N'Đậu phộng hạt khô (Lạc)', 30.00, N'g', N'Ngâm nước ấm', 0),
(10113, N'Nấm mộc nhĩ khô (Nấm mèo)', 10.00, N'g', NULL, 0),
(10113, N'Cà rốt củ tươi', 100.00, N'g', N'Thái lát mỏng', 0),
(10113, N'Rau thơm các loại', 15.00, N'g', N'Thái lát mỏng', 0),
(10113, N'Bánh phở tươi', 200.00, N'g', N'Ngâm nước ấm', 0),
(10113, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', NULL, 0),
(10113, N'Hạt nêm nấm chay bào ngư', 10.00, N'g', NULL, 0),
(10113, N'Dầu đậu nành nguyên chất', 30.00, N'ml', N'Cắt miếng vừa ăn', 0),
(10113, N'Muối tinh', 8.00, N'g', NULL, 0),
(10113, N'Đường cát trắng', 8.00, N'g', N'Băm nhỏ', 0),
(10113, N'Tiêu đen xay nguyên chất', 1.00, N'g', N'Cắt miếng vừa ăn', 0),
(10113, N'Ớt hiểm tươi cay', 5.00, N'g', NULL, 0),
(10114, N'Đậu hũ trắng (Tofu)', 280.00, N'g', N'Băm nhỏ', 0),
(10114, N'Đậu nành hạt khô', 70.00, N'g', N'Cắt miếng vừa ăn', 0),
(10114, N'Đậu phộng hạt khô (Lạc)', 20.00, N'g', N'Ngâm nước ấm', 0),
(10114, N'Nấm mộc nhĩ khô (Nấm mèo)', 10.00, N'g', N'Thái lát mỏng', 0),
(10114, N'Cà rốt củ tươi', 70.00, N'g', N'Ngâm nước ấm', 0),
(10114, N'Rau thơm các loại', 15.00, N'g', NULL, 0),
(10114, N'Bánh phở tươi', 140.00, N'g', N'Cắt miếng vừa ăn', 0),
(10114, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Ngâm nước ấm', 0),
(10114, N'Hạt nêm nấm chay bào ngư', 10.00, N'g', N'Ngâm nước ấm', 0),
(10114, N'Dầu đậu nành nguyên chất', 20.00, N'ml', N'Băm nhỏ', 0),
(10114, N'Muối tinh', 8.00, N'g', N'Cắt miếng vừa ăn', 0),
(10114, N'Đường cát trắng', 8.00, N'g', N'Thái lát mỏng', 0),
(10114, N'Tiêu đen xay nguyên chất', 1.00, N'g', N'Thái lát mỏng', 0),
(10114, N'Ớt hiểm tươi cay', 5.00, N'g', NULL, 0),
(10115, N'Đậu hũ trắng (Tofu)', 220.00, N'g', NULL, 0),
(10115, N'Đậu nành hạt khô', 55.00, N'g', N'Thái lát mỏng', 0),
(10115, N'Đậu phộng hạt khô (Lạc)', 15.00, N'g', N'Băm nhỏ', 0),
(10115, N'Nấm mộc nhĩ khô (Nấm mèo)', 13.00, N'g', N'Thái lát mỏng', 0),
(10115, N'Cà rốt củ tươi', 70.00, N'g', N'Ngâm nước ấm', 0),
(10115, N'Rau thơm các loại', 19.50, N'g', N'Thái lát mỏng', 0),
(10115, N'Bánh phở tươi', 80.00, N'g', NULL, 0),
(10115, N'Nước tương đậu nành (Xì dầu)', 19.50, N'ml', NULL, 0),
(10115, N'Hạt nêm nấm chay bào ngư', 13.00, N'g', NULL, 0),
(10115, N'Dầu đậu nành nguyên chất', 10.00, N'ml', N'Ngâm nước ấm', 0),
(10115, N'Muối tinh', 10.40, N'g', N'Băm nhỏ', 0),
(10115, N'Đường cát trắng', 4.00, N'g', N'Thái lát mỏng', 0),
(10115, N'Tiêu đen xay nguyên chất', 1.30, N'g', NULL, 0),
(10116, N'Đậu hũ non (Silken Tofu)', 400.00, N'g', NULL, 0),
(10116, N'Đậu bắp tươi', 350.00, N'g', NULL, 0),
(10116, N'Nấm rơm tươi', 100.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10116, N'Cà chua chín đỏ', 150.00, N'g', N'Thái lát mỏng', 0),
(10116, N'Dứa (thơm)', 75.00, N'g', NULL, 0),
(10116, N'Me chua', 20.00, N'g', NULL, 0),
(10116, N'Muối tinh', 3.00, N'g', NULL, 0),
(10116, N'Đường cát trắng', 8.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10117, N'Đậu hũ non (Silken Tofu)', 1575.00, N'g', N'Cắt miếng vừa ăn', 0),
(10117, N'Đậu bắp tươi', 1025.00, N'g', NULL, 0),
(10117, N'Nấm rơm tươi', 290.00, N'g', NULL, 0),
(10117, N'Cà chua chín đỏ', 440.00, N'g', N'Cắt miếng vừa ăn', 0),
(10117, N'Dứa (thơm)', 220.00, N'g', NULL, 0),
(10117, N'Me chua', 20.00, N'g', N'Ngâm nước ấm', 0),
(10117, N'Muối tinh', 3.00, N'g', N'Ngâm nước ấm', 0),
(10117, N'Đường cát trắng', 8.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10118, N'Đậu hũ non (Silken Tofu)', 840.00, N'g', N'Băm nhỏ', 0),
(10118, N'Đậu bắp tươi', 735.00, N'g', N'Băm nhỏ', 0),
(10118, N'Nấm rơm tươi', 210.00, N'g', NULL, 0),
(10118, N'Cà chua chín đỏ', 315.00, N'g', NULL, 0),
(10118, N'Dứa (thơm)', 155.00, N'g', NULL, 0),
(10118, N'Me chua', 20.00, N'g', N'Ngâm nước ấm', 0),
(10118, N'Muối tinh', 3.00, N'g', N'Cắt miếng vừa ăn', 0),
(10118, N'Đường cát trắng', 8.00, N'g', N'Cắt miếng vừa ăn', 0),
(10119, N'Đậu hũ trắng (Tofu)', 400.00, N'g', N'Băm nhỏ', 0),
(10119, N'Nấm mộc nhĩ khô (Nấm mèo)', 10.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10119, N'Khổ qua (Mướp đắng)', 300.00, N'g', NULL, 0),
(10119, N'Hành Boa-rô (Tỏi tây / Leek)', 80.00, N'g', N'Cắt miếng vừa ăn', 0),
(10119, N'Miến dong khô', 100.00, N'g', NULL, 0),
(10119, N'Bột năng (Bột sắn)', 20.00, N'g', N'Băm nhỏ', 0),
(10119, N'Nước cốt dừa', 160.00, N'ml', N'Băm nhỏ', 0),
(10119, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Thái lát mỏng', 0),
(10119, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10119, N'Dầu đậu nành nguyên chất', 15.00, N'ml', N'Thái lát mỏng', 0),
(10119, N'Muối tinh', 3.00, N'g', N'Cắt miếng vừa ăn', 0),
(10119, N'Đường cát trắng', 8.00, N'g', N'Ngâm nước ấm', 0),
(10119, N'Ớt hiểm tươi cay', 5.00, N'g', NULL, 0),
(10120, N'Đậu hũ trắng (Tofu)', 275.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10120, N'Nấm mộc nhĩ khô (Nấm mèo)', 13.00, N'g', N'Thái lát mỏng', 0),
(10120, N'Khổ qua (Mướp đắng)', 270.00, N'g', NULL, 0),
(10120, N'Hành Boa-rô (Tỏi tây / Leek)', 104.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10120, N'Miến dong khô', 50.00, N'g', N'Ngâm nước ấm', 0),
(10120, N'Bột năng (Bột sắn)', 10.00, N'g', NULL, 0),
(10120, N'Nước cốt dừa', 55.00, N'ml', N'Cắt miếng vừa ăn', 0),
(10120, N'Nước tương đậu nành (Xì dầu)', 19.50, N'ml', N'Thái lát mỏng', 0),
(10120, N'Hạt nêm nấm chay bào ngư', 6.50, N'g', NULL, 0),
(10120, N'Dầu đậu nành nguyên chất', 7.50, N'ml', N'Rửa sạch, cắt khúc', 0),
(10120, N'Muối tinh', 3.90, N'g', N'Ngâm nước ấm', 0),
(10120, N'Đường cát trắng', 4.00, N'g', N'Thái lát mỏng', 0),
(10120, N'Ớt hiểm tươi cay', 6.50, N'g', N'Ngâm nước ấm', 0),
(10121, N'Dưa leo tươi (Dưa chuột)', 300.00, N'g', NULL, 0),
(10121, N'Tỏi củ tươi', 20.00, N'g', NULL, 0),
(10121, N'Hạt mè trắng (Vừng trắng)', 10.00, N'g', NULL, 0),
(10121, N'Bún gạo khô', 250.00, N'g', N'Băm nhỏ', 0),
(10121, N'Nước mắm chay (từ dứa & nấm rơm)', 15.00, N'ml', N'Băm nhỏ', 0),
(10121, N'Dầu đậu nành nguyên chất', 15.00, N'ml', NULL, 0),
(10121, N'Đường cát trắng', 8.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10121, N'Ớt hiểm tươi cay', 5.00, N'g', N'Cắt miếng vừa ăn', 0),
(10121, N'Gừng củ tươi', 10.00, N'g', N'Cắt miếng vừa ăn', 0),
(10121, N'Giấm gạo', 10.00, N'g', N'Cắt miếng vừa ăn', 0),
(10122, N'Dưa leo tươi (Dưa chuột)', 300.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10122, N'Tỏi củ tươi', 20.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10122, N'Hạt mè trắng (Vừng trắng)', 10.00, N'g', NULL, 0),
(10122, N'Bún gạo khô', 312.50, N'g', N'Cắt miếng vừa ăn', 0),
(10122, N'Nước mắm chay (từ dứa & nấm rơm)', 15.00, N'ml', NULL, 0),
(10122, N'Dầu đậu nành nguyên chất', 19.50, N'ml', N'Băm nhỏ', 0),
(10122, N'Đường cát trắng', 8.00, N'g', N'Ngâm nước ấm', 0),
(10122, N'Ớt hiểm tươi cay', 5.00, N'g', N'Ngâm nước ấm', 0),
(10122, N'Gừng củ tươi', 10.00, N'g', N'Cắt miếng vừa ăn', 0),
(10122, N'Giấm gạo', 10.00, N'g', NULL, 0),
(10123, N'Đậu hũ non (Silken Tofu)', 200.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10123, N'Nấm kim châm tươi', 100.00, N'g', N'Thái lát mỏng', 0),
(10123, N'Nấm hương tươi (Shiitake)', 50.00, N'g', N'Thái lát mỏng', 0),
(10123, N'Hành lá tươi (Hành hoa)', 15.00, N'g', NULL, 0),
(10124, N'Đậu hũ trắng (Tofu)', 400.00, N'g', NULL, 0),
(10124, N'Nấm kim châm tươi', 200.00, N'g', N'Thái lát mỏng', 0),
(10124, N'Cà rốt củ tươi', 200.00, N'g', NULL, 0),
(10124, N'Rau mùi tươi (Ngò rí)', 13.00, N'g', N'Băm nhỏ', 0),
(10124, N'Tỏi củ tươi', 20.00, N'g', NULL, 0),
(10124, N'Bột bắp (bột ngô)', 5.00, N'g', N'Cắt miếng vừa ăn', 0),
(10124, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Cắt miếng vừa ăn', 0),
(10124, N'Dầu đậu nành nguyên chất', 15.00, N'ml', NULL, 0),
(10124, N'Muối tinh', 6.00, N'g', N'Thái lát mỏng', 0),
(10124, N'Đường cát trắng', 8.00, N'g', NULL, 0),
(10125, N'Đậu hũ trắng (Tofu)', 1025.00, N'g', NULL, 0),
(10125, N'Nấm kim châm tươi', 380.00, N'g', NULL, 0),
(10125, N'Cà rốt củ tươi', 380.00, N'g', NULL, 0),
(10125, N'Rau mùi tươi (Ngò rí)', 13.00, N'g', NULL, 0),
(10125, N'Tỏi củ tươi', 20.00, N'g', N'Ngâm nước ấm', 0),
(10125, N'Bột bắp (bột ngô)', 6.20, N'g', N'Ngâm nước ấm', 0),
(10125, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Thái lát mỏng', 0),
(10125, N'Dầu đậu nành nguyên chất', 35.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10125, N'Muối tinh', 6.00, N'g', NULL, 0),
(10125, N'Đường cát trắng', 8.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10126, N'Đậu hũ trắng (Tofu)', 625.00, N'g', N'Cắt miếng vừa ăn', 0),
(10126, N'Nấm kim châm tươi', 315.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10126, N'Cà rốt củ tươi', 315.00, N'g', NULL, 0),
(10126, N'Rau mùi tươi (Ngò rí)', 13.00, N'g', N'Ngâm nước ấm', 0),
(10126, N'Tỏi củ tươi', 20.00, N'g', NULL, 0),
(10126, N'Bột bắp (bột ngô)', 5.00, N'g', NULL, 0),
(10126, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', NULL, 0),
(10126, N'Dầu đậu nành nguyên chất', 25.00, N'ml', NULL, 0),
(10126, N'Muối tinh', 6.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10126, N'Đường cát trắng', 8.00, N'g', N'Băm nhỏ', 0),
(10127, N'Đậu hũ trắng (Tofu)', 200.00, N'g', N'Cắt miếng vừa ăn', 0),
(10127, N'Nấm rơm tươi', 100.00, N'g', N'Cắt miếng vừa ăn', 0),
(10127, N'Nấm hương tươi (Shiitake)', 10.00, N'g', NULL, 0),
(10127, N'Nấm mộc nhĩ khô (Nấm mèo)', 10.00, N'g', NULL, 0),
(10127, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', NULL, 0),
(10127, N'Dầu đậu nành nguyên chất', 15.00, N'ml', NULL, 0)
) AS v(rid, nm, qty, u, nt, op)
JOIN #ing i ON i.Ingredient_Name = v.nm;
IF @@ROWCOUNT <> 250 THROW 50001, N'RecipeIngredients: tra cứu dữ liệu master thất bại (thiếu tên/mã)', 1;
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional)
SELECT v.rid, i.Ingredient_ID, v.qty, v.u, v.nt, v.op
FROM (VALUES
(10127, N'Tiêu đen xay nguyên chất', 10.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10128, N'Đậu hũ trắng (Tofu)', 995.00, N'g', NULL, 0),
(10128, N'Nấm rơm tươi', 365.00, N'g', N'Băm nhỏ', 0),
(10128, N'Nấm hương tươi (Shiitake)', 10.00, N'g', N'Cắt miếng vừa ăn', 0),
(10128, N'Nấm mộc nhĩ khô (Nấm mèo)', 10.00, N'g', N'Ngâm nước ấm', 0),
(10128, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Thái lát mỏng', 0),
(10128, N'Dầu đậu nành nguyên chất', 70.00, N'ml', N'Thái lát mỏng', 0),
(10128, N'Tiêu đen xay nguyên chất', 10.00, N'g', N'Ngâm nước ấm', 0),
(10129, N'Đậu hũ trắng (Tofu)', 200.00, N'g', NULL, 0),
(10129, N'Nấm rơm tươi', 100.00, N'g', NULL, 0),
(10129, N'Nấm hương tươi (Shiitake)', 100.00, N'g', N'Băm nhỏ', 0),
(10129, N'Nấm mộc nhĩ khô (Nấm mèo)', 10.00, N'g', NULL, 0),
(10129, N'Bắp cải trắng', 100.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10129, N'Cải ngọt tươi', 100.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10129, N'Nước tương đậu nành (Xì dầu)', 5.00, N'ml', NULL, 0),
(10129, N'Muối tinh', 2.50, N'g', N'Thái lát mỏng', 0),
(10129, N'Đường cát trắng', 5.00, N'g', N'Ngâm nước ấm', 0),
(10130, N'Đậu hũ trắng (Tofu)', 425.00, N'g', N'Ngâm nước ấm', 0),
(10130, N'Nấm rơm tươi', 275.00, N'g', N'Ngâm nước ấm', 0),
(10130, N'Nấm hương tươi (Shiitake)', 275.00, N'g', NULL, 0),
(10130, N'Nấm mộc nhĩ khô (Nấm mèo)', 13.00, N'g', NULL, 0),
(10130, N'Bắp cải trắng', 275.00, N'g', NULL, 0),
(10130, N'Cải ngọt tươi', 275.00, N'g', NULL, 0),
(10130, N'Nước tương đậu nành (Xì dầu)', 6.50, N'ml', N'Thái lát mỏng', 0),
(10130, N'Muối tinh', 3.20, N'g', NULL, 0),
(10130, N'Đường cát trắng', 2.50, N'g', N'Băm nhỏ', 0),
(10131, N'Đậu hũ trắng (Tofu)', 400.00, N'g', N'Ngâm nước ấm', 0),
(10131, N'Nấm hương tươi (Shiitake)', 50.00, N'g', N'Cắt miếng vừa ăn', 0),
(10131, N'Nấm mộc nhĩ khô (Nấm mèo)', 10.00, N'g', NULL, 0),
(10131, N'Cà rốt củ tươi', 50.00, N'g', N'Thái lát mỏng', 0),
(10131, N'Hành lá tươi (Hành hoa)', 45.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10131, N'Bột bắp (bột ngô)', 20.00, N'g', NULL, 0),
(10131, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Băm nhỏ', 0),
(10131, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Cắt miếng vừa ăn', 0),
(10131, N'Dầu đậu nành nguyên chất', 15.00, N'ml', NULL, 0),
(10131, N'Muối tinh', 3.00, N'g', N'Ngâm nước ấm', 0),
(10131, N'Đường cát trắng', 8.00, N'g', NULL, 0),
(10132, N'Đậu hũ trắng (Tofu)', 1060.00, N'g', N'Cắt miếng vừa ăn', 0),
(10132, N'Nấm hương tươi (Shiitake)', 100.00, N'g', NULL, 0),
(10132, N'Nấm mộc nhĩ khô (Nấm mèo)', 10.00, N'g', N'Cắt miếng vừa ăn', 0),
(10132, N'Cà rốt củ tươi', 100.00, N'g', N'Cắt miếng vừa ăn', 0),
(10132, N'Hành lá tươi (Hành hoa)', 45.00, N'g', NULL, 0),
(10132, N'Bột bắp (bột ngô)', 50.00, N'g', N'Cắt miếng vừa ăn', 0),
(10132, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Thái lát mỏng', 0),
(10132, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Ngâm nước ấm', 0),
(10132, N'Dầu đậu nành nguyên chất', 40.00, N'ml', N'Cắt miếng vừa ăn', 0),
(10132, N'Muối tinh', 3.00, N'g', N'Thái lát mỏng', 0),
(10132, N'Đường cát trắng', 8.00, N'g', N'Thái lát mỏng', 0),
(10133, N'Đậu hũ trắng (Tofu)', 670.00, N'g', NULL, 0),
(10133, N'Nấm hương tươi (Shiitake)', 85.00, N'g', N'Thái lát mỏng', 0),
(10133, N'Nấm mộc nhĩ khô (Nấm mèo)', 10.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10133, N'Cà rốt củ tươi', 85.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10133, N'Hành lá tươi (Hành hoa)', 45.00, N'g', N'Băm nhỏ', 0),
(10133, N'Bột bắp (bột ngô)', 35.00, N'g', N'Băm nhỏ', 0),
(10133, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', NULL, 0),
(10133, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', NULL, 0),
(10133, N'Dầu đậu nành nguyên chất', 25.00, N'ml', N'Ngâm nước ấm', 0),
(10133, N'Muối tinh', 3.00, N'g', N'Ngâm nước ấm', 0),
(10133, N'Đường cát trắng', 8.00, N'g', NULL, 0),
(10134, N'Đậu hũ chiên vàng', 300.00, N'g', N'Thái lát mỏng', 0),
(10134, N'Nấm rơm tươi', 100.00, N'g', N'Ngâm nước ấm', 0),
(10134, N'Cà rốt củ tươi', 50.00, N'g', NULL, 0),
(10134, N'Rau mùi tươi (Ngò rí)', 8.00, N'g', N'Ngâm nước ấm', 0),
(10134, N'Me chua', 60.00, N'g', NULL, 0),
(10134, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Băm nhỏ', 0),
(10134, N'Dầu hào chay nấm hương', 15.00, N'ml', NULL, 0),
(10134, N'Tương hột đậu nành lên men', 15.00, N'g', NULL, 0),
(10134, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Ngâm nước ấm', 0),
(10134, N'Dầu đậu nành nguyên chất', 15.00, N'ml', NULL, 0),
(10134, N'Muối tinh', 3.00, N'g', NULL, 0),
(10134, N'Đường cát trắng', 8.00, N'g', N'Băm nhỏ', 0),
(10134, N'Tiêu đen xay nguyên chất', 1.00, N'g', N'Ngâm nước ấm', 0),
(10134, N'Ớt hiểm tươi cay', 5.00, N'g', NULL, 0),
(10135, N'Đậu hũ trắng (Tofu)', 220.00, N'g', NULL, 0),
(10135, N'Đậu xanh bóc vỏ', 140.00, N'g', NULL, 0),
(10135, N'Nấm mộc nhĩ khô (Nấm mèo)', 30.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10135, N'Cà rốt củ tươi', 200.00, N'g', N'Cắt miếng vừa ăn', 0),
(10135, N'Khoai tây tươi', 140.00, N'g', N'Thái lát mỏng', 0),
(10135, N'Khoai môn cao', 120.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10135, N'Củ sắn tươi (Củ đậu)', 130.00, N'g', N'Băm nhỏ', 0),
(10135, N'Gạo thơm lài miên', 200.00, N'g', N'Cắt miếng vừa ăn', 0),
(10135, N'Bánh tráng gỏi cuốn', 30.00, N'g', NULL, 0),
(10135, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10135, N'Dầu đậu nành nguyên chất', 15.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10135, N'Đường cát trắng', 8.00, N'g', NULL, 0),
(10135, N'Tiêu đen xay nguyên chất', 1.00, N'g', NULL, 0),
(10136, N'Đậu hũ trắng (Tofu)', 85.00, N'g', NULL, 0),
(10136, N'Đậu xanh bóc vỏ', 55.00, N'g', N'Cắt miếng vừa ăn', 0),
(10136, N'Nấm mộc nhĩ khô (Nấm mèo)', 15.00, N'g', NULL, 0),
(10136, N'Cà rốt củ tươi', 100.00, N'g', N'Ngâm nước ấm', 0),
(10136, N'Khoai tây tươi', 40.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10136, N'Khoai môn cao', 35.00, N'g', N'Băm nhỏ', 0),
(10136, N'Củ sắn tươi (Củ đậu)', 65.00, N'g', N'Cắt miếng vừa ăn', 0),
(10136, N'Gạo thơm lài miên', 55.00, N'g', NULL, 0),
(10136, N'Bánh tráng gỏi cuốn', 10.00, N'g', N'Thái lát mỏng', 0),
(10136, N'Hạt nêm nấm chay bào ngư', 6.50, N'g', N'Cắt miếng vừa ăn', 0),
(10136, N'Dầu đậu nành nguyên chất', 7.50, N'ml', N'Cắt miếng vừa ăn', 0),
(10136, N'Đường cát trắng', 4.00, N'g', N'Thái lát mỏng', 0),
(10136, N'Tiêu đen xay nguyên chất', 1.30, N'g', N'Rửa sạch, cắt khúc', 0),
(10137, N'Đậu hũ trắng (Tofu)', 200.00, N'g', NULL, 0),
(10137, N'Nấm hương tươi (Shiitake)', 350.00, N'g', N'Ngâm nước ấm', 0),
(10137, N'Cải thìa tươi (Cải chíp)', 100.00, N'g', N'Ngâm nước ấm', 0),
(10137, N'Hạt mè trắng (Vừng trắng)', 10.00, N'g', N'Thái lát mỏng', 0),
(10137, N'Bột năng (Bột sắn)', 20.00, N'g', N'Băm nhỏ', 0),
(10137, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Ngâm nước ấm', 0),
(10137, N'Dầu hào chay nấm hương', 15.00, N'ml', N'Thái lát mỏng', 0),
(10137, N'Dầu đậu nành nguyên chất', 30.00, N'ml', NULL, 0),
(10137, N'Muối tinh', 3.00, N'g', N'Ngâm nước ấm', 0),
(10137, N'Đường cát trắng', 8.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10137, N'Tiêu đen xay nguyên chất', 1.00, N'g', NULL, 0),
(10137, N'Ớt hiểm tươi cay', 50.00, N'g', NULL, 0),
(10137, N'Gừng củ tươi', 80.00, N'g', N'Thái lát mỏng', 0),
(10138, N'Đậu hũ trắng (Tofu)', 260.00, N'g', NULL, 0),
(10138, N'Nấm hương tươi (Shiitake)', 455.00, N'g', N'Cắt miếng vừa ăn', 0),
(10138, N'Cải thìa tươi (Cải chíp)', 130.00, N'g', NULL, 0),
(10138, N'Hạt mè trắng (Vừng trắng)', 10.00, N'g', NULL, 0),
(10138, N'Bột năng (Bột sắn)', 25.00, N'g', N'Thái lát mỏng', 0),
(10138, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Thái lát mỏng', 0),
(10138, N'Dầu hào chay nấm hương', 15.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10138, N'Dầu đậu nành nguyên chất', 40.00, N'ml', N'Ngâm nước ấm', 0),
(10138, N'Muối tinh', 3.00, N'g', N'Cắt miếng vừa ăn', 0),
(10138, N'Đường cát trắng', 8.00, N'g', N'Cắt miếng vừa ăn', 0),
(10138, N'Tiêu đen xay nguyên chất', 1.00, N'g', N'Cắt miếng vừa ăn', 0),
(10138, N'Gừng củ tươi', 80.00, N'g', NULL, 0),
(10139, N'Củ sắn tươi (Củ đậu)', 100.00, N'g', N'Băm nhỏ', 0),
(10139, N'Hành lá tươi (Hành hoa)', 15.00, N'g', N'Ngâm nước ấm', 0),
(10139, N'Hành tím củ (Hành ta)', 15.00, N'g', N'Băm nhỏ', 0),
(10139, N'Gạo thơm lài miên', 15.00, N'g', NULL, 0),
(10139, N'Bột năng (Bột sắn)', 80.00, N'g', NULL, 0),
(10139, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10139, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10139, N'Muối tinh', 3.00, N'g', NULL, 0),
(10139, N'Ớt hiểm tươi cay', 5.00, N'g', N'Cắt miếng vừa ăn', 0),
(10140, N'Củ sắn tươi (Củ đậu)', 265.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10140, N'Hành lá tươi (Hành hoa)', 19.50, N'g', NULL, 0),
(10140, N'Hành tím củ (Hành ta)', 19.50, N'g', NULL, 0),
(10140, N'Gạo thơm lài miên', 11.20, N'g', N'Băm nhỏ', 0),
(10140, N'Bột năng (Bột sắn)', 120.00, N'g', NULL, 0),
(10140, N'Nước tương đậu nành (Xì dầu)', 19.50, N'ml', N'Ngâm nước ấm', 0),
(10140, N'Hạt nêm nấm chay bào ngư', 6.50, N'g', N'Cắt miếng vừa ăn', 0),
(10140, N'Muối tinh', 3.90, N'g', N'Ngâm nước ấm', 0),
(10140, N'Ớt hiểm tươi cay', 6.50, N'g', N'Thái lát mỏng', 0),
(10141, N'Củ sắn tươi (Củ đậu)', 240.00, N'g', NULL, 0),
(10141, N'Hành lá tươi (Hành hoa)', 15.00, N'g', N'Cắt miếng vừa ăn', 0),
(10141, N'Hành tím củ (Hành ta)', 15.00, N'g', N'Cắt miếng vừa ăn', 0),
(10141, N'Gạo thơm lài miên', 35.00, N'g', N'Băm nhỏ', 0),
(10141, N'Bột năng (Bột sắn)', 190.00, N'g', N'Thái lát mỏng', 0),
(10141, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', NULL, 0),
(10141, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Cắt miếng vừa ăn', 0),
(10141, N'Muối tinh', 3.00, N'g', N'Ngâm nước ấm', 0),
(10142, N'Tàu hũ ky khô', 30.00, N'g', N'Băm nhỏ', 0),
(10142, N'Nấm hương tươi (Shiitake)', 50.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10142, N'Nấm mỡ trắng (Champignon)', 240.00, N'g', N'Ngâm nước ấm', 0),
(10142, N'Cà rốt củ tươi', 100.00, N'g', N'Ngâm nước ấm', 0),
(10142, N'Măng tươi luộc chín', 300.00, N'g', N'Thái lát mỏng', 0),
(10142, N'Nước tương đậu nành (Xì dầu)', 5.00, N'ml', NULL, 0),
(10142, N'Dầu hào chay nấm hương', 5.00, N'ml', N'Thái lát mỏng', 0),
(10142, N'Đường cát trắng', 10.00, N'g', NULL, 0),
(10143, N'Tàu hũ ky khô', 175.00, N'g', N'Băm nhỏ', 0),
(10143, N'Nấm hương tươi (Shiitake)', 220.00, N'g', NULL, 0),
(10143, N'Nấm mỡ trắng (Champignon)', 1050.00, N'g', NULL, 0),
(10143, N'Cà rốt củ tươi', 440.00, N'g', NULL, 0),
(10143, N'Măng tươi luộc chín', 1315.00, N'g', N'Thái lát mỏng', 0),
(10143, N'Nước tương đậu nành (Xì dầu)', 5.00, N'ml', NULL, 0),
(10143, N'Dầu hào chay nấm hương', 5.00, N'ml', N'Băm nhỏ', 0),
(10143, N'Đường cát trắng', 10.00, N'g', N'Cắt miếng vừa ăn', 0),
(10144, N'Đậu hũ chiên vàng', 350.00, N'g', N'Băm nhỏ', 0),
(10144, N'Nấm rơm tươi', 100.00, N'g', NULL, 0),
(10144, N'Nấm mộc nhĩ khô (Nấm mèo)', 30.00, N'g', NULL, 0),
(10144, N'Cà rốt củ tươi', 100.00, N'g', NULL, 0),
(10144, N'Khoai lang mật', 30.00, N'g', NULL, 0),
(10144, N'Cà chua chín đỏ', 350.00, N'g', NULL, 0),
(10144, N'Bột mì đa dụng', 80.00, N'g', NULL, 0),
(10145, N'Nấm hương tươi (Shiitake)', 300.00, N'g', N'Ngâm nước ấm', 0),
(10145, N'Cà rốt củ tươi', 50.00, N'g', N'Băm nhỏ', 0),
(10145, N'Cà chua chín đỏ', 50.00, N'g', NULL, 0),
(10145, N'Cải thìa tươi (Cải chíp)', 300.00, N'g', NULL, 0),
(10145, N'Rau mùi tươi (Ngò rí)', 10.00, N'g', NULL, 0),
(10145, N'Hành Boa-rô (Tỏi tây / Leek)', 5.00, N'g', NULL, 0),
(10145, N'Hành tây củ trắng', 50.00, N'g', N'Cắt miếng vừa ăn', 0),
(10145, N'Ớt chuông', 100.00, N'g', NULL, 0),
(10145, N'Nước tương đậu nành (Xì dầu)', 10.00, N'ml', N'Thái lát mỏng', 0),
(10145, N'Dầu hào chay nấm hương', 10.00, N'ml', N'Cắt miếng vừa ăn', 0),
(10145, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Băm nhỏ', 0),
(10145, N'Dầu đậu nành nguyên chất', 10.00, N'ml', N'Cắt miếng vừa ăn', 0),
(10145, N'Đường cát trắng', 5.00, N'g', NULL, 0),
(10146, N'Đậu hũ trắng (Tofu)', 400.00, N'g', N'Băm nhỏ', 0),
(10146, N'Nấm mộc nhĩ khô (Nấm mèo)', 10.00, N'g', N'Ngâm nước ấm', 0),
(10146, N'Khổ qua (Mướp đắng)', 300.00, N'g', NULL, 0),
(10146, N'Hành Boa-rô (Tỏi tây / Leek)', 80.00, N'g', N'Cắt miếng vừa ăn', 0),
(10146, N'Miến dong khô', 100.00, N'g', NULL, 0),
(10146, N'Bột năng (Bột sắn)', 20.00, N'g', N'Ngâm nước ấm', 0),
(10146, N'Nước cốt dừa', 200.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10146, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', NULL, 0),
(10146, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Ngâm nước ấm', 0),
(10146, N'Dầu đậu nành nguyên chất', 15.00, N'ml', NULL, 0),
(10146, N'Muối tinh', 20.00, N'g', N'Cắt miếng vừa ăn', 0),
(10146, N'Đường cát trắng', 8.00, N'g', N'Cắt miếng vừa ăn', 0),
(10146, N'Ớt hiểm tươi cay', 5.00, N'g', N'Cắt miếng vừa ăn', 0),
(10147, N'Đậu hũ trắng (Tofu)', 265.00, N'g', N'Băm nhỏ', 0),
(10147, N'Nấm mộc nhĩ khô (Nấm mèo)', 13.00, N'g', N'Cắt miếng vừa ăn', 0),
(10147, N'Khổ qua (Mướp đắng)', 260.00, N'g', N'Ngâm nước ấm', 0),
(10147, N'Hành Boa-rô (Tỏi tây / Leek)', 104.00, N'g', NULL, 0),
(10147, N'Miến dong khô', 50.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10147, N'Bột năng (Bột sắn)', 10.00, N'g', N'Băm nhỏ', 0),
(10147, N'Nước cốt dừa', 65.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10147, N'Nước tương đậu nành (Xì dầu)', 19.50, N'ml', N'Rửa sạch, cắt khúc', 0),
(10147, N'Hạt nêm nấm chay bào ngư', 6.50, N'g', N'Thái lát mỏng', 0),
(10147, N'Dầu đậu nành nguyên chất', 7.50, N'ml', N'Thái lát mỏng', 0),
(10147, N'Muối tinh', 26.00, N'g', NULL, 0),
(10147, N'Đường cát trắng', 4.00, N'g', N'Cắt miếng vừa ăn', 0),
(10147, N'Ớt hiểm tươi cay', 6.50, N'g', NULL, 0),
(10148, N'Đậu hũ trắng (Tofu)', 300.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10148, N'Nấm mộc nhĩ khô (Nấm mèo)', 10.00, N'g', N'Băm nhỏ', 0),
(10148, N'Khổ qua (Mướp đắng)', 225.00, N'g', N'Thái lát mỏng', 0),
(10148, N'Hành Boa-rô (Tỏi tây / Leek)', 80.00, N'g', N'Cắt miếng vừa ăn', 0),
(10148, N'Miến dong khô', 75.00, N'g', N'Băm nhỏ', 0),
(10148, N'Bột năng (Bột sắn)', 15.00, N'g', N'Ngâm nước ấm', 0),
(10148, N'Nước cốt dừa', 150.00, N'ml', N'Thái lát mỏng', 0),
(10148, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10148, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Thái lát mỏng', 0),
(10148, N'Dầu đậu nành nguyên chất', 10.00, N'ml', N'Ngâm nước ấm', 0),
(10148, N'Muối tinh', 20.00, N'g', N'Thái lát mỏng', 0),
(10148, N'Đường cát trắng', 8.00, N'g', NULL, 0),
(10148, N'Ớt hiểm tươi cay', 5.00, N'g', N'Cắt miếng vừa ăn', 0),
(10149, N'Nấm hương tươi (Shiitake)', 150.00, N'g', N'Cắt miếng vừa ăn', 0),
(10149, N'Nấm mộc nhĩ khô (Nấm mèo)', 60.00, N'g', N'Cắt miếng vừa ăn', 0),
(10149, N'Cà rốt củ tươi', 150.00, N'g', NULL, 0),
(10149, N'Hành tây củ trắng', 80.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10149, N'Miến dong khô', 100.00, N'g', NULL, 0),
(10149, N'Hạt nêm nấm chay bào ngư', 10.00, N'g', N'Cắt miếng vừa ăn', 0),
(10149, N'Dầu đậu nành nguyên chất', 15.00, N'ml', NULL, 0),
(10150, N'Nấm hương tươi (Shiitake)', 260.00, N'g', N'Thái lát mỏng', 0),
(10150, N'Nấm mộc nhĩ khô (Nấm mèo)', 105.00, N'g', NULL, 0),
(10150, N'Cà rốt củ tươi', 260.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10150, N'Hành tây củ trắng', 80.00, N'g', NULL, 0),
(10150, N'Miến dong khô', 215.00, N'g', N'Thái lát mỏng', 0),
(10150, N'Hạt nêm nấm chay bào ngư', 10.00, N'g', N'Cắt miếng vừa ăn', 0),
(10150, N'Dầu đậu nành nguyên chất', 35.00, N'ml', N'Băm nhỏ', 0),
(10151, N'Nấm hương tươi (Shiitake)', 200.00, N'g', NULL, 0),
(10151, N'Nấm mộc nhĩ khô (Nấm mèo)', 80.00, N'g', NULL, 0),
(10151, N'Cà rốt củ tươi', 200.00, N'g', NULL, 0),
(10151, N'Hành tây củ trắng', 80.00, N'g', NULL, 0),
(10151, N'Miến dong khô', 135.00, N'g', NULL, 0),
(10151, N'Hạt nêm nấm chay bào ngư', 10.00, N'g', N'Băm nhỏ', 0),
(10151, N'Dầu đậu nành nguyên chất', 20.00, N'ml', N'Thái lát mỏng', 0),
(10152, N'Củ sắn tươi (Củ đậu)', 100.00, N'g', N'Ngâm nước ấm', 0),
(10152, N'Hành lá tươi (Hành hoa)', 15.00, N'g', NULL, 0),
(10152, N'Hành tím củ (Hành ta)', 15.00, N'g', NULL, 0),
(10152, N'Gạo thơm lài miên', 15.00, N'g', N'Ngâm nước ấm', 0)
) AS v(rid, nm, qty, u, nt, op)
JOIN #ing i ON i.Ingredient_Name = v.nm;
IF @@ROWCOUNT <> 250 THROW 50001, N'RecipeIngredients: tra cứu dữ liệu master thất bại (thiếu tên/mã)', 1;
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional)
SELECT v.rid, i.Ingredient_ID, v.qty, v.u, v.nt, v.op
FROM (VALUES
(10152, N'Bột năng (Bột sắn)', 80.00, N'g', NULL, 0),
(10152, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Ngâm nước ấm', 0),
(10152, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', NULL, 0),
(10152, N'Muối tinh', 3.00, N'g', N'Băm nhỏ', 0),
(10152, N'Ớt hiểm tươi cay', 5.00, N'g', NULL, 0),
(10153, N'Củ sắn tươi (Củ đậu)', 315.00, N'g', N'Băm nhỏ', 0),
(10153, N'Hành lá tươi (Hành hoa)', 15.00, N'g', NULL, 0),
(10153, N'Hành tím củ (Hành ta)', 15.00, N'g', NULL, 0),
(10153, N'Gạo thơm lài miên', 60.00, N'g', NULL, 0),
(10153, N'Bột năng (Bột sắn)', 315.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10153, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Thái lát mỏng', 0),
(10153, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10153, N'Muối tinh', 3.00, N'g', N'Thái lát mỏng', 0),
(10153, N'Ớt hiểm tươi cay', 5.00, N'g', NULL, 0),
(10154, N'Củ sắn tươi (Củ đậu)', 265.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10154, N'Hành lá tươi (Hành hoa)', 19.50, N'g', NULL, 0),
(10154, N'Hành tím củ (Hành ta)', 19.50, N'g', N'Ngâm nước ấm', 0),
(10154, N'Gạo thơm lài miên', 11.20, N'g', NULL, 0),
(10154, N'Bột năng (Bột sắn)', 120.00, N'g', NULL, 0),
(10154, N'Nước tương đậu nành (Xì dầu)', 19.50, N'ml', N'Rửa sạch, cắt khúc', 0),
(10154, N'Hạt nêm nấm chay bào ngư', 6.50, N'g', NULL, 0),
(10154, N'Muối tinh', 3.90, N'g', N'Rửa sạch, cắt khúc', 0),
(10154, N'Ớt hiểm tươi cay', 6.50, N'g', N'Băm nhỏ', 0),
(10155, N'Đậu hũ trắng (Tofu)', 300.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10155, N'Nấm rơm tươi', 300.00, N'g', N'Ngâm nước ấm', 0),
(10155, N'Cà chua chín đỏ', 75.00, N'g', N'Thái lát mỏng', 0),
(10155, N'Bột bắp (bột ngô)', 50.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10155, N'Hạt nêm nấm chay bào ngư', 2.50, N'g', N'Băm nhỏ', 0),
(10155, N'Dầu đậu nành nguyên chất', 50.00, N'ml', N'Băm nhỏ', 0),
(10155, N'Đường cát trắng', 8.00, N'g', N'Thái lát mỏng', 0),
(10155, N'Tiêu đen xay nguyên chất', 1.00, N'g', N'Thái lát mỏng', 0),
(10155, N'Ớt hiểm tươi cay', 10.00, N'g', NULL, 0),
(10156, N'Đậu hũ trắng (Tofu)', 1060.00, N'g', N'Thái lát mỏng', 0),
(10156, N'Nấm hương tươi (Shiitake)', 265.00, N'g', NULL, 0),
(10156, N'Ớt chuông', 530.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10157, N'Đậu hũ non (Silken Tofu)', 790.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10157, N'Nấm kim châm tươi', 510.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10157, N'Nấm hương tươi (Shiitake)', 255.00, N'g', N'Thái lát mỏng', 0),
(10158, N'Đậu hũ trắng (Tofu)', 300.00, N'g', NULL, 0),
(10158, N'Nấm rơm tươi', 390.00, N'g', N'Ngâm nước ấm', 0),
(10158, N'Cà chua chín đỏ', 13.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10158, N'Bột bắp (bột ngô)', 37.50, N'g', NULL, 0),
(10158, N'Hạt nêm nấm chay bào ngư', 6.50, N'g', NULL, 0),
(10158, N'Dầu đậu nành nguyên chất', 25.00, N'ml', N'Ngâm nước ấm', 0),
(10158, N'Đường cát trắng', 4.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10158, N'Tiêu đen xay nguyên chất', 1.30, N'g', NULL, 0),
(10158, N'Ớt hiểm tươi cay', 13.00, N'g', NULL, 0),
(10159, N'Đậu hũ trắng (Tofu)', 400.00, N'g', NULL, 0),
(10159, N'Nấm rơm tươi', 200.00, N'g', N'Cắt miếng vừa ăn', 0),
(10159, N'Củ cải trắng tươi', 30.00, N'g', N'Cắt miếng vừa ăn', 0),
(10159, N'Bí đỏ hạt đậu (Bí ngô)', 350.00, N'g', NULL, 0),
(10159, N'Cà chua chín đỏ', 300.00, N'g', NULL, 0),
(10159, N'Bắp cải trắng', 30.00, N'g', N'Băm nhỏ', 0),
(10159, N'Ngò gai tươi (Mùi tàu)', 5.00, N'g', N'Ngâm nước ấm', 0),
(10159, N'Rau thơm các loại', 15.00, N'g', NULL, 0),
(10159, N'Bún gạo khô', 150.00, N'g', N'Cắt miếng vừa ăn', 0),
(10159, N'Dứa (thơm)', 75.00, N'g', NULL, 0),
(10159, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Ngâm nước ấm', 0),
(10159, N'Sả cây tươi', 60.00, N'g', N'Thái lát mỏng', 0),
(10159, N'Nghệ củ tươi', 20.00, N'g', N'Băm nhỏ', 0),
(10160, N'Đậu Hà Lan hạt tươi', 630.00, N'g', NULL, 0),
(10160, N'Cà rốt củ tươi', 630.00, N'g', NULL, 0),
(10160, N'Bông cải xanh (Broccoli)', 10.00, N'g', NULL, 0),
(10160, N'Hạt mè trắng (Vừng trắng)', 10.00, N'g', N'Băm nhỏ', 0),
(10160, N'Ớt chuông', 475.00, N'g', NULL, 0),
(10160, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Cắt miếng vừa ăn', 0),
(10160, N'Dầu đậu nành nguyên chất', 60.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10161, N'Đậu hũ chiên vàng', 320.00, N'g', N'Ngâm nước ấm', 0),
(10161, N'Nấm hương tươi (Shiitake)', 105.00, N'g', N'Băm nhỏ', 0),
(10161, N'Cà rốt củ tươi', 45.00, N'g', NULL, 0),
(10161, N'Bắp cải trắng', 45.00, N'g', NULL, 0),
(10161, N'Dầu hào chay nấm hương', 15.00, N'ml', N'Băm nhỏ', 0),
(10161, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', N'Ngâm nước ấm', 0),
(10161, N'Muối tinh', 5.00, N'g', NULL, 0),
(10161, N'Đường cát trắng', 10.00, N'g', N'Băm nhỏ', 0),
(10162, N'Đậu hũ trắng (Tofu)', 630.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10162, N'Cà chua chín đỏ', 240.00, N'g', NULL, 0),
(10162, N'Bột bắp (bột ngô)', 45.00, N'g', NULL, 0),
(10162, N'Nước tương đậu nành (Xì dầu)', 3.20, N'ml', N'Thái lát mỏng', 0),
(10162, N'Muối tinh', 3.90, N'g', N'Băm nhỏ', 0),
(10162, N'Đường cát trắng', 2.50, N'g', NULL, 0),
(10163, N'Đậu Hà Lan hạt tươi', 530.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10163, N'Cà rốt củ tươi', 265.00, N'g', NULL, 0),
(10163, N'Bông cải trắng (Cauliflower)', 265.00, N'g', N'Băm nhỏ', 0),
(10163, N'Dầu đậu nành nguyên chất', 35.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10163, N'Muối tinh', 3.00, N'g', N'Băm nhỏ', 0),
(10164, N'Đậu hũ trắng (Tofu)', 320.00, N'g', NULL, 0),
(10164, N'Nấm mộc nhĩ khô (Nấm mèo)', 10.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10164, N'Khổ qua (Mướp đắng)', 240.00, N'g', NULL, 0),
(10164, N'Miến dong khô', 80.00, N'g', N'Thái lát mỏng', 0),
(10164, N'Bột năng (Bột sắn)', 15.00, N'g', NULL, 0),
(10164, N'Nước cốt dừa', 130.00, N'ml', NULL, 0),
(10164, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10164, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', NULL, 0),
(10164, N'Dầu đậu nành nguyên chất', 10.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10164, N'Muối tinh', 3.00, N'g', NULL, 0),
(10164, N'Đường cát trắng', 8.00, N'g', N'Thái lát mỏng', 0),
(10164, N'Ớt hiểm tươi cay', 5.00, N'g', NULL, 0),
(10165, N'Đậu hũ trắng (Tofu)', 540.00, N'g', N'Thái lát mỏng', 0),
(10165, N'Đậu nành hạt khô', 135.00, N'g', N'Cắt miếng vừa ăn', 0),
(10165, N'Đậu Hà Lan hạt tươi', 100.00, N'g', NULL, 0),
(10165, N'Nấm mộc nhĩ khô (Nấm mèo)', 50.00, N'g', NULL, 0),
(10165, N'Hạt nêm nấm chay bào ngư', 10.00, N'g', N'Ngâm nước ấm', 0),
(10165, N'Dầu đậu nành nguyên chất', 19.50, N'ml', NULL, 0),
(10166, N'Nấm hương tươi (Shiitake)', 2030.00, N'g', N'Thái lát mỏng', 0),
(10166, N'Cà rốt củ tươi', 340.00, N'g', N'Thái lát mỏng', 0),
(10166, N'Cà chua chín đỏ', 340.00, N'g', N'Băm nhỏ', 0),
(10166, N'Cải thìa tươi (Cải chíp)', 2030.00, N'g', NULL, 0),
(10166, N'Rau mùi tươi (Ngò rí)', 10.00, N'g', NULL, 0),
(10166, N'Ớt chuông', 675.00, N'g', N'Cắt miếng vừa ăn', 0),
(10166, N'Nước tương đậu nành (Xì dầu)', 10.00, N'ml', N'Ngâm nước ấm', 0),
(10166, N'Dầu hào chay nấm hương', 10.00, N'ml', N'Ngâm nước ấm', 0),
(10166, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', NULL, 0),
(10166, N'Dầu đậu nành nguyên chất', 13.00, N'ml', N'Cắt miếng vừa ăn', 0),
(10166, N'Đường cát trắng', 5.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10167, N'Nấm rơm tươi', 2010.00, N'g', NULL, 0),
(10167, N'Cà rốt củ tươi', 1005.00, N'g', NULL, 0),
(10167, N'Củ cải trắng tươi', 505.00, N'g', N'Thái lát mỏng', 0),
(10167, N'Cà tím dài', 1510.00, N'g', N'Băm nhỏ', 0),
(10168, N'Đậu hũ trắng (Tofu)', 480.00, N'g', N'Cắt miếng vừa ăn', 0),
(10168, N'Nấm rơm tươi', 310.00, N'g', NULL, 0),
(10168, N'Cà chua chín đỏ', 935.00, N'g', NULL, 0),
(10169, N'Bơ lạt từ sữa bò', 7.50, N'g', NULL, 0),
(10169, N'Nấm hương tươi (Shiitake)', 640.00, N'g', NULL, 0),
(10169, N'Cà rốt củ tươi', 640.00, N'g', NULL, 0),
(10169, N'Rau mùi tươi (Ngò rí)', 10.40, N'g', NULL, 0),
(10169, N'Nước tương đậu nành (Xì dầu)', 19.50, N'ml', NULL, 0),
(10169, N'Dầu đậu nành nguyên chất', 7.50, N'ml', NULL, 0),
(10169, N'Muối tinh', 3.90, N'g', NULL, 0),
(10169, N'Tiêu đen xay nguyên chất', 1.30, N'g', NULL, 0),
(10170, N'Đậu hũ trắng (Tofu)', 120.00, N'g', NULL, 0),
(10170, N'Mì căn tươi (Seitan đạm lúa mì)', 110.00, N'g', NULL, 0),
(10170, N'Đậu phộng hạt khô (Lạc)', 35.00, N'g', N'Ngâm nước ấm', 0),
(10170, N'Nấm mộc nhĩ khô (Nấm mèo)', 13.00, N'g', N'Băm nhỏ', 0),
(10170, N'Củ cải trắng tươi', 80.00, N'g', NULL, 0),
(10170, N'Hoa chuối thái mỏng', 80.00, N'g', N'Ngâm nước ấm', 0),
(10170, N'Bắp cải trắng', 60.00, N'g', NULL, 0),
(10170, N'Húng quế', 10.40, N'g', N'Ngâm nước ấm', 0),
(10170, N'Rau thơm các loại', 19.50, N'g', N'Rửa sạch, cắt khúc', 0),
(10170, N'Bánh phở tươi', 90.00, N'g', N'Băm nhỏ', 0),
(10170, N'Bánh tráng gỏi cuốn', 15.00, N'g', NULL, 0),
(10170, N'Hạt nêm nấm chay bào ngư', 6.50, N'g', N'Thái lát mỏng', 0),
(10170, N'Dầu đậu nành nguyên chất', 7.50, N'ml', NULL, 0),
(10170, N'Muối tinh', 3.90, N'g', NULL, 0),
(10170, N'Đường cát trắng', 4.00, N'g', NULL, 0),
(10170, N'Nghệ củ tươi', 6.50, N'g', NULL, 0),
(10171, N'Đậu hũ trắng (Tofu)', 860.00, N'g', N'Băm nhỏ', 0),
(10171, N'Nấm rơm tươi', 215.00, N'g', NULL, 0),
(10171, N'Bột năng (Bột sắn)', 90.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10172, N'Dưa leo tươi (Dưa chuột)', 300.00, N'g', NULL, 0),
(10172, N'Hạt mè trắng (Vừng trắng)', 13.00, N'g', N'Băm nhỏ', 0),
(10172, N'Bún gạo khô', 145.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10172, N'Nước mắm chay (từ dứa & nấm rơm)', 19.50, N'ml', NULL, 0),
(10172, N'Dầu đậu nành nguyên chất', 7.50, N'ml', NULL, 0),
(10172, N'Đường cát trắng', 4.00, N'g', NULL, 0),
(10172, N'Ớt hiểm tươi cay', 6.50, N'g', N'Thái lát mỏng', 0),
(10172, N'Gừng củ tươi', 13.00, N'g', N'Ngâm nước ấm', 0),
(10172, N'Giấm gạo', 13.00, N'g', NULL, 0),
(10173, N'Sữa tươi tiệt trùng bò', 50.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10173, N'Đậu hũ chiên vàng', 190.00, N'g', NULL, 0),
(10173, N'Cà rốt củ tươi', 80.00, N'g', N'Thái lát mỏng', 0),
(10173, N'Khoai tây tươi', 95.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10173, N'Hạt nêm nấm chay bào ngư', 6.50, N'g', NULL, 0),
(10173, N'Dầu đậu nành nguyên chất', 7.50, N'ml', NULL, 0),
(10173, N'Muối tinh', 26.00, N'g', N'Băm nhỏ', 0),
(10173, N'Đường cát trắng', 4.00, N'g', NULL, 0),
(10174, N'Đậu hũ chiên vàng', 155.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10174, N'Nấm hương tươi (Shiitake)', 85.00, N'g', N'Cắt miếng vừa ăn', 0),
(10174, N'Nấm mộc nhĩ khô (Nấm mèo)', 15.00, N'g', N'Ngâm nước ấm', 0),
(10174, N'Hạt mè trắng (Vừng trắng)', 13.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10174, N'Bột năng (Bột sắn)', 10.00, N'g', N'Cắt miếng vừa ăn', 0),
(10174, N'Nước tương đậu nành (Xì dầu)', 19.50, N'ml', N'Băm nhỏ', 0),
(10174, N'Dầu hào chay nấm hương', 19.50, N'ml', NULL, 0),
(10174, N'Tương hột đậu nành lên men', 19.50, N'g', N'Thái lát mỏng', 0),
(10174, N'Hạt nêm nấm chay bào ngư', 6.50, N'g', N'Băm nhỏ', 0),
(10174, N'Dầu đậu nành nguyên chất', 7.50, N'ml', NULL, 0),
(10174, N'Muối tinh', 3.90, N'g', NULL, 0),
(10174, N'Đường cát trắng', 4.00, N'g', N'Thái lát mỏng', 0),
(10174, N'Tiêu đen xay nguyên chất', 1.30, N'g', N'Băm nhỏ', 0),
(10175, N'Bò lát chay khô (Đạm đậu nành TVP)', 140.00, N'g', N'Ngâm nước ấm', 0),
(10175, N'Đậu phộng hạt khô (Lạc)', 20.00, N'g', N'Cắt miếng vừa ăn', 0),
(10175, N'Xà lách lụa tươi', 90.00, N'g', N'Thái lát mỏng', 0),
(10175, N'Rau mùi tươi (Ngò rí)', 10.40, N'g', N'Thái lát mỏng', 0),
(10175, N'Húng quế', 10.40, N'g', N'Ngâm nước ấm', 0),
(10175, N'Rau thơm các loại', 19.50, N'g', N'Rửa sạch, cắt khúc', 0),
(10175, N'Giá đỗ', 90.00, N'g', N'Băm nhỏ', 0),
(10175, N'Nước tương đậu nành (Xì dầu)', 19.50, N'ml', NULL, 0),
(10175, N'Dầu đậu nành nguyên chất', 7.50, N'ml', N'Cắt miếng vừa ăn', 0),
(10175, N'Tiêu đen xay nguyên chất', 1.30, N'g', NULL, 0),
(10175, N'Gừng củ tươi', 13.00, N'g', N'Băm nhỏ', 0),
(10175, N'Sả cây tươi', 19.50, N'g', NULL, 0),
(10176, N'Đậu hũ chiên vàng', 105.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10176, N'Chả lụa chay (từ đậu xanh & bột mì)', 250.00, N'g', NULL, 0),
(10176, N'Nấm rơm tươi', 250.00, N'g', N'Thái lát mỏng', 0),
(10176, N'Cà rốt củ tươi', 70.00, N'g', N'Cắt miếng vừa ăn', 0),
(10176, N'Khoai tây tươi', 70.00, N'g', N'Cắt miếng vừa ăn', 0),
(10176, N'Khoai lang mật', 70.00, N'g', NULL, 0),
(10176, N'Su su quả tươi', 70.00, N'g', NULL, 0),
(10176, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Băm nhỏ', 0),
(10176, N'Dầu đậu nành nguyên chất', 10.00, N'ml', NULL, 0),
(10176, N'Muối tinh', 3.00, N'g', N'Cắt miếng vừa ăn', 0),
(10176, N'Đường cát trắng', 10.00, N'g', N'Băm nhỏ', 0),
(10176, N'Tiêu đen xay nguyên chất', 1.00, N'g', N'Ngâm nước ấm', 0),
(10177, N'Đậu hũ trắng (Tofu)', 300.00, N'g', N'Ngâm nước ấm', 0),
(10177, N'Nấm mộc nhĩ khô (Nấm mèo)', 10.00, N'g', NULL, 0),
(10177, N'Khổ qua (Mướp đắng)', 225.00, N'g', N'Ngâm nước ấm', 0),
(10177, N'Miến dong khô', 75.00, N'g', N'Ngâm nước ấm', 0),
(10177, N'Bột năng (Bột sắn)', 15.00, N'g', N'Ngâm nước ấm', 0),
(10177, N'Nước cốt dừa', 150.00, N'ml', N'Thái lát mỏng', 0),
(10177, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', NULL, 0),
(10177, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', NULL, 0),
(10177, N'Dầu đậu nành nguyên chất', 10.00, N'ml', NULL, 0),
(10177, N'Muối tinh', 20.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10177, N'Đường cát trắng', 8.00, N'g', N'Thái lát mỏng', 0),
(10177, N'Ớt hiểm tươi cay', 5.00, N'g', NULL, 0),
(10178, N'Đậu hũ trắng (Tofu)', 625.00, N'g', NULL, 0),
(10178, N'Nấm kim châm tươi', 315.00, N'g', N'Băm nhỏ', 0),
(10178, N'Cà rốt củ tươi', 315.00, N'g', N'Ngâm nước ấm', 0),
(10178, N'Rau mùi tươi (Ngò rí)', 13.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10178, N'Bột bắp (bột ngô)', 5.00, N'g', NULL, 0),
(10178, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10178, N'Dầu đậu nành nguyên chất', 25.00, N'ml', NULL, 0),
(10178, N'Muối tinh', 6.00, N'g', N'Ngâm nước ấm', 0),
(10178, N'Đường cát trắng', 8.00, N'g', N'Ngâm nước ấm', 0),
(10179, N'Đậu hũ non (Silken Tofu)', 1.00, N'g', N'Thái lát mỏng', 0),
(10179, N'Nấm hương tươi (Shiitake)', 1240.00, N'g', NULL, 0),
(10179, N'Hạt mè trắng (Vừng trắng)', 13.00, N'g', NULL, 0),
(10179, N'Bột năng (Bột sắn)', 7.50, N'g', NULL, 0),
(10179, N'Nước tương đậu nành (Xì dầu)', 19.50, N'ml', N'Ngâm nước ấm', 0),
(10179, N'Dầu hào chay nấm hương', 19.50, N'ml', NULL, 0),
(10179, N'Dầu đậu nành nguyên chất', 5.00, N'ml', NULL, 0),
(10179, N'Muối tinh', 13.00, N'g', NULL, 0),
(10180, N'Dưa leo tươi (Dưa chuột)', 300.00, N'g', N'Băm nhỏ', 0),
(10180, N'Hạt mè trắng (Vừng trắng)', 13.00, N'g', NULL, 0),
(10180, N'Bún gạo khô', 145.00, N'g', NULL, 0),
(10180, N'Nước mắm chay (từ dứa & nấm rơm)', 19.50, N'ml', N'Cắt miếng vừa ăn', 0),
(10180, N'Dầu đậu nành nguyên chất', 7.50, N'ml', NULL, 0),
(10180, N'Đường cát trắng', 4.00, N'g', N'Thái lát mỏng', 0),
(10180, N'Ớt hiểm tươi cay', 6.50, N'g', NULL, 0),
(10180, N'Gừng củ tươi', 13.00, N'g', NULL, 0),
(10180, N'Giấm gạo', 13.00, N'g', N'Cắt miếng vừa ăn', 0),
(10181, N'Củ sắn tươi (Củ đậu)', 240.00, N'g', NULL, 0),
(10181, N'Gạo thơm lài miên', 35.00, N'g', N'Thái lát mỏng', 0),
(10181, N'Bột năng (Bột sắn)', 190.00, N'g', N'Thái lát mỏng', 0),
(10181, N'Nước tương đậu nành (Xì dầu)', 15.00, N'ml', NULL, 0),
(10181, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', NULL, 0),
(10181, N'Muối tinh', 3.00, N'g', N'Thái lát mỏng', 0),
(10181, N'Ớt hiểm tươi cay', 5.00, N'g', N'Ngâm nước ấm', 0),
(10182, N'Nấm mộc nhĩ khô (Nấm mèo)', 90.00, N'g', N'Cắt miếng vừa ăn', 0),
(10182, N'Miến dong khô', 380.00, N'g', NULL, 0)
) AS v(rid, nm, qty, u, nt, op)
JOIN #ing i ON i.Ingredient_Name = v.nm;
IF @@ROWCOUNT <> 250 THROW 50001, N'RecipeIngredients: tra cứu dữ liệu master thất bại (thiếu tên/mã)', 1;
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional)
SELECT v.rid, i.Ingredient_ID, v.qty, v.u, v.nt, v.op
FROM (VALUES
(10182, N'Nước mắm chay (từ dứa & nấm rơm)', 15.00, N'ml', N'Băm nhỏ', 0),
(10182, N'Hạt nêm nấm chay bào ngư', 5.00, N'g', NULL, 0),
(10182, N'Muối tinh', 3.00, N'g', NULL, 0),
(10182, N'Ớt hiểm tươi cay', 5.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10183, N'Đậu hũ trắng (Tofu)', 1050.00, N'g', N'Cắt miếng vừa ăn', 0),
(10183, N'Nấm hương tươi (Shiitake)', 130.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10183, N'Nước tương đậu nành (Xì dầu)', 10.00, N'ml', NULL, 0),
(10183, N'Dầu hào chay nấm hương', 10.00, N'ml', N'Rửa sạch, cắt khúc', 0),
(10183, N'Dầu đậu nành nguyên chất', 10.00, N'ml', NULL, 0),
(10183, N'Muối tinh', 13.00, N'g', N'Rửa sạch, cắt khúc', 0),
(10183, N'Đường cát trắng', 10.00, N'g', NULL, 0),
(10183, N'Gừng củ tươi', 10.00, N'g', N'Băm nhỏ', 0),
(10184, N'Chả lụa chay (từ đậu xanh & bột mì)', 165.00, N'g', NULL, 0),
(10184, N'Đậu bắp tươi', 320.00, N'g', N'Ngâm nước ấm', 0),
(10184, N'Nấm hương tươi (Shiitake)', 55.00, N'g', NULL, 0),
(10184, N'Hạt sen tươi', 105.00, N'g', N'Thái lát mỏng', 0),
(10184, N'Rau mùi tươi (Ngò rí)', 10.40, N'g', N'Băm nhỏ', 0),
(10184, N'Nước cốt dừa', 40.00, N'ml', NULL, 0),
(10184, N'Nước tương đậu nành (Xì dầu)', 19.50, N'ml', N'Rửa sạch, cắt khúc', 0),
(10184, N'Hạt nêm nấm chay bào ngư', 13.00, N'g', N'Băm nhỏ', 0),
(10184, N'Dầu đậu nành nguyên chất', 7.50, N'ml', N'Cắt miếng vừa ăn', 0),
(10184, N'Đường cát trắng', 4.00, N'g', NULL, 0),
(10184, N'Tiêu đen xay nguyên chất', 1.30, N'g', N'Ngâm nước ấm', 0),
(10184, N'Ớt hiểm tươi cay', 13.00, N'g', NULL, 0),
(10184, N'Sả cây tươi', 39.00, N'g', NULL, 0)
) AS v(rid, nm, qty, u, nt, op)
JOIN #ing i ON i.Ingredient_Name = v.nm;
IF @@ROWCOUNT <> 25 THROW 50001, N'RecipeIngredients: tra cứu dữ liệu master thất bại (thiếu tên/mã)', 1;
PRINT N'   + RecipeIngredients: 1775 dòng';
