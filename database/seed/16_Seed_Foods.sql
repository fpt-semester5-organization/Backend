-- ==========================================================================
-- SEED DATA: 30 MÓN ĂN THUỘC 4 NHÓM (MÓN XÀO, MÓN NƯỚC, ĐỒ KHÔ, ĐỒ UỐNG)
-- VÀ 120 CÔNG THỨC NẤU ĂN ĐA DẠNG MỤC ĐÍCH (GIẢM CÂN, CAY NỒNG, ĐẬM THỰC VẬT...)
-- ĐỊNH LƯỢNG NGUYÊN LIỆU ĐA DẠNG & THỰC TẾ THEO TỪNG MỤC TIÊU CÔNG THỨC
-- ==========================================================================
USE VeggieLifeDb;
GO

SET NOCOUNT ON;

UPDATE dbo.Foods SET Food_Name = N'TEMP_' + CAST(Food_ID AS NVARCHAR);
GO

-- 1. UPSERT 30 MÓN ĂN ĐẠI DIỆN
SET IDENTITY_INSERT dbo.Foods ON;


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 1)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Bông cải xanh xào nấm bào ngư', Category_ID = 14, Image_URL = N'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 1;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (1, 14, N'Bông cải xanh xào nấm bào ngư', N'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 2)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Đậu hũ non sốt nấm dầu mè', Category_ID = 14, Image_URL = N'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 2;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (2, 14, N'Đậu hũ non sốt nấm dầu mè', N'https://images.unsplash.com/photo-1541832676-9b763b0239ab?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 3)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Mì căn xào sả ớt giòn cay', Category_ID = 14, Image_URL = N'https://images.unsplash.com/photo-1541832676-9b763b0239ab?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 3;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (3, 14, N'Mì căn xào sả ớt giòn cay', N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 4)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Nấm đùi gà xào húng quế', Category_ID = 14, Image_URL = N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 4;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (4, 14, N'Nấm đùi gà xào húng quế', N'https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 5)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Măng tây xào hạt điều', Category_ID = 14, Image_URL = N'https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 5;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (5, 14, N'Măng tây xào hạt điều', N'https://images.unsplash.com/photo-1515543237350-b3eea1ec8082?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 6)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Khổ qua xào đậu hũ thanh nhiệt', Category_ID = 14, Image_URL = N'https://images.unsplash.com/photo-1515543237350-b3eea1ec8082?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 6;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (6, 14, N'Khổ qua xào đậu hũ thanh nhiệt', N'https://images.unsplash.com/photo-1540189549336-e6e99c3679fe?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 7)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Rau củ thập cẩm xào nấm đông cô', Category_ID = 14, Image_URL = N'https://images.unsplash.com/photo-1540189549336-e6e99c3679fe?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 7;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (7, 14, N'Rau củ thập cẩm xào nấm đông cô', N'https://images.unsplash.com/photo-1543339308-43e59d6b73a6?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 8)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Bún bò Huế chay', Category_ID = 11, Image_URL = N'https://images.unsplash.com/photo-1543339308-43e59d6b73a6?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 8;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (8, 11, N'Bún bò Huế chay', N'https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 9)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Phở chay Hà Nội nước dùng thanh ngọt', Category_ID = 11, Image_URL = N'https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 9;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (9, 11, N'Phở chay Hà Nội nước dùng thanh ngọt', N'https://images.unsplash.com/photo-1576577445504-6af96477db52?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 10)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Bún riêu cua chay thơm béo', Category_ID = 11, Image_URL = N'https://images.unsplash.com/photo-1576577445504-6af96477db52?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 10;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (10, 11, N'Bún riêu cua chay thơm béo', N'https://images.unsplash.com/photo-1555126634-323283e090fa?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 11)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Hủ tiếu Nam Vang chay', Category_ID = 11, Image_URL = N'https://images.unsplash.com/photo-1555126634-323283e090fa?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 11;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (11, 11, N'Hủ tiếu Nam Vang chay', N'https://images.unsplash.com/photo-1617093727343-374698b1b08d?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 12)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Bánh canh nấm rơm bột gạo', Category_ID = 11, Image_URL = N'https://images.unsplash.com/photo-1617093727343-374698b1b08d?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 12;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (12, 11, N'Bánh canh nấm rơm bột gạo', N'https://images.unsplash.com/photo-1594998893017-36147cbcae05?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 13)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Mì Quảng chay sợi vàng đậm đà', Category_ID = 11, Image_URL = N'https://images.unsplash.com/photo-1594998893017-36147cbcae05?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 13;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (13, 11, N'Mì Quảng chay sợi vàng đậm đà', N'https://images.unsplash.com/photo-1552611052-33e04de081de?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 14)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Canh chua Nam Bộ chay thanh mát', Category_ID = 3, Image_URL = N'https://images.unsplash.com/photo-1552611052-33e04de081de?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 14;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (14, 3, N'Canh chua Nam Bộ chay thanh mát', N'https://images.unsplash.com/photo-1547592166-23ac45744acd?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 15)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Súp bắp ngô ngọt hạt sen dưỡng tâm', Category_ID = 3, Image_URL = N'https://images.unsplash.com/photo-1547592166-23ac45744acd?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 15;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (15, 3, N'Súp bắp ngô ngọt hạt sen dưỡng tâm', N'https://images.unsplash.com/photo-1603105037880-880cd4edfb0d?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 16)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Lẩu nấm dưỡng sinh thảo mộc', Category_ID = 10, Image_URL = N'https://images.unsplash.com/photo-1603105037880-880cd4edfb0d?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 16;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (16, 10, N'Lẩu nấm dưỡng sinh thảo mộc', N'https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 17)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Cơm chiên Dương Châu ngũ sắc', Category_ID = 5, Image_URL = N'https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 17;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (17, 5, N'Cơm chiên Dương Châu ngũ sắc', N'https://images.unsplash.com/photo-1603133872878-684f208fb84b?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 18)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Cơm tấm sườn bì chả chay', Category_ID = 5, Image_URL = N'https://images.unsplash.com/photo-1603133872878-684f208fb84b?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 18;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (18, 5, N'Cơm tấm sườn bì chả chay', N'https://images.unsplash.com/photo-1512058564366-18510be2db19?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 19)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Cơm niêu nấm kho tộ cháy giòn', Category_ID = 5, Image_URL = N'https://images.unsplash.com/photo-1512058564366-18510be2db19?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 19;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (19, 5, N'Cơm niêu nấm kho tộ cháy giòn', N'https://images.unsplash.com/photo-1536304929831-ee1ca9d44906?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 20)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Xôi gấc hạt sen chay dẻo thơm', Category_ID = 5, Image_URL = N'https://images.unsplash.com/photo-1536304929831-ee1ca9d44906?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 20;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (20, 5, N'Xôi gấc hạt sen chay dẻo thơm', N'https://images.unsplash.com/photo-1505253758473-96b7015fcd40?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 21)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Đậu hũ kho nấm rơm tiêu đen', Category_ID = 9, Image_URL = N'https://images.unsplash.com/photo-1505253758473-96b7015fcd40?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 21;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (21, 9, N'Đậu hũ kho nấm rơm tiêu đen', N'https://images.unsplash.com/photo-1541832676-9b763b0239ab?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 22)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Nấm đông cô kho tiêu hạt sen', Category_ID = 9, Image_URL = N'https://images.unsplash.com/photo-1541832676-9b763b0239ab?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 22;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (22, 9, N'Nấm đông cô kho tiêu hạt sen', N'https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 23)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Cà tím kho tộ nước cốt dừa', Category_ID = 9, Image_URL = N'https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 23;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (23, 9, N'Cà tím kho tộ nước cốt dừa', N'https://images.unsplash.com/photo-1528735602780-2552fd46c7af?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 24)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Gỏi cuốn chay ngũ sắc sốt tương', Category_ID = 6, Image_URL = N'https://images.unsplash.com/photo-1528735602780-2552fd46c7af?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 24;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (24, 6, N'Gỏi cuốn chay ngũ sắc sốt tương', N'https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 25)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Chả giò chay khoai môn giòn rụm', Category_ID = 6, Image_URL = N'https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 25;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (25, 6, N'Chả giò chay khoai môn giòn rụm', N'https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 26)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Nước ép cần tây dưa leo táo xanh detox', Category_ID = 2, Image_URL = N'https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 26;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (26, 2, N'Nước ép cần tây dưa leo táo xanh detox', N'https://images.unsplash.com/photo-1556881286-fc6915169721?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 27)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Sinh tố bơ chuối hạt chia giàu đạm', Category_ID = 2, Image_URL = N'https://images.unsplash.com/photo-1556881286-fc6915169721?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 27;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (27, 2, N'Sinh tố bơ chuối hạt chia giàu đạm', N'https://images.unsplash.com/photo-1553530666-ba11a7da3888?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 28)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Trà hoa cúc táo đỏ kỷ tử dưỡng nhan', Category_ID = 2, Image_URL = N'https://images.unsplash.com/photo-1553530666-ba11a7da3888?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 28;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (28, 2, N'Trà hoa cúc táo đỏ kỷ tử dưỡng nhan', N'https://images.unsplash.com/photo-1576092768241-dec231879fc3?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 29)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Sữa hạt sen đậu xanh bùi béo', Category_ID = 2, Image_URL = N'https://images.unsplash.com/photo-1576092768241-dec231879fc3?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 29;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (29, 2, N'Sữa hạt sen đậu xanh bùi béo', N'https://images.unsplash.com/photo-1528751014936-863e6e7a319c?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


IF EXISTS (SELECT 1 FROM dbo.Foods WHERE Food_ID = 30)
BEGIN
    UPDATE dbo.Foods
    SET Food_Name = N'Nước ép củ dền cà rốt bổ máu', Category_ID = 2, Image_URL = N'https://images.unsplash.com/photo-1528751014936-863e6e7a319c?auto=format&fit=crop&w=800&q=80', Updated_At = SYSDATETIME()
    WHERE Food_ID = 30;
END
ELSE
BEGIN
    INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At, Updated_At)
    VALUES (30, 2, N'Nước ép củ dền cà rốt bổ máu', N'https://images.unsplash.com/photo-1534353473418-4cfa6c56fd38?auto=format&fit=crop&w=800&q=80', SYSDATETIME(), SYSDATETIME());
END


SET IDENTITY_INSERT dbo.Foods OFF;
GO

-- 2. UPSERT 120 CÔNG THỨC NẤU ĂN
SET IDENTITY_INSERT dbo.Recipes ON;


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 101)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Bông cải xanh xào nấm bào ngư - Công thức truyền thống gia đình', Description = N'Vị ngọt giòn tự nhiên từ bông cải xanh và nấm bào ngư xào tỏi tây thơm phức, chuẩn vị mâm cơm gia đình.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bông cải xanh xào nấm bào ngư.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 260.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 1, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 101;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (101, 1, 1, N'Bông cải xanh xào nấm bào ngư - Công thức truyền thống gia đình', N'Vị ngọt giòn tự nhiên từ bông cải xanh và nấm bào ngư xào tỏi tây thơm phức, chuẩn vị mâm cơm gia đình.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bông cải xanh xào nấm bào ngư.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 260.0, 'EASY', 1, N'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 11, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 1);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 102)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 2, Title = N'Bông cải xanh xào nấm bào ngư - Công thức vị sa tế cay nồng (ăn được ớt)', Description = N'Ớt sừng đỏ tươi xắt lát hòa quyện cùng sa tế thơm nồng tạo vị cay xuýt xoa hấp dẫn cho người ăn được ớt.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bông cải xanh xào nấm bào ngư.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 290.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 1, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 102;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (102, 2, 2, N'Bông cải xanh xào nấm bào ngư - Công thức vị sa tế cay nồng (ăn được ớt)', N'Ớt sừng đỏ tươi xắt lát hòa quyện cùng sa tế thơm nồng tạo vị cay xuýt xoa hấp dẫn cho người ăn được ớt.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bông cải xanh xào nấm bào ngư.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 290.0, 'EASY', 1, N'https://images.unsplash.com/photo-1455619452474-d2be8b1e70cd?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 12, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 1);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 103)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Bông cải xanh xào nấm bào ngư - Công thức dành cho người giảm cân', Description = N'Áp chảo ít dầu mè, giữ trọn vẹn chất xơ và vitamin, thanh đạm nhẹ nhàng chỉ 210 kcal hỗ trợ giữ dáng.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bông cải xanh xào nấm bào ngư.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 210.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1455619452474-d2be8b1e70cd?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 1, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 103;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (103, 1, 1, N'Bông cải xanh xào nấm bào ngư - Công thức dành cho người giảm cân', N'Áp chảo ít dầu mè, giữ trọn vẹn chất xơ và vitamin, thanh đạm nhẹ nhàng chỉ 210 kcal hỗ trợ giữ dáng.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bông cải xanh xào nấm bào ngư.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 210.0, 'EASY', 1, N'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 13, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 1);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 104)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Bông cải xanh xào nấm bào ngư - Công thức giàu đạm thực vật', Description = N'Bổ sung nấm đùi gà và đậu phụ chiên giòn, cung cấp hơn 22g protein thực vật cho ngày năng động.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bông cải xanh xào nấm bào ngư.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 350.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 1, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 104;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (104, 2, 1, N'Bông cải xanh xào nấm bào ngư - Công thức giàu đạm thực vật', N'Bổ sung nấm đùi gà và đậu phụ chiên giòn, cung cấp hơn 22g protein thực vật cho ngày năng động.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bông cải xanh xào nấm bào ngư.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 350.0, 'EASY', 1, N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 14, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 1);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 105)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Đậu hũ non sốt nấm dầu mè - Công thức truyền thống gia đình', Description = N'Đậu hũ non mềm mịn sốt cùng nấm đông cô cô đặc và dầu mè nguyên chất thơm lừng nức mũi.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Đậu hũ non sốt nấm dầu mè.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 280.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 2, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 105;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (105, 1, 1, N'Đậu hũ non sốt nấm dầu mè - Công thức truyền thống gia đình', N'Đậu hũ non mềm mịn sốt cùng nấm đông cô cô đặc và dầu mè nguyên chất thơm lừng nức mũi.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Đậu hũ non sốt nấm dầu mè.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 280.0, 'EASY', 1, N'https://images.unsplash.com/photo-1541832676-9b763b0239ab?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 15, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 2);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 106)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 2, Title = N'Đậu hũ non sốt nấm dầu mè - Công thức vị sa tế cay nồng (ăn được ớt)', Description = N'Sốt sa tế ớt hiểm cay xé lưỡi phủ trên lớp đậu hũ mềm ngậy, kích thích vị giác tột đỉnh.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Đậu hũ non sốt nấm dầu mè.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 310.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1541832676-9b763b0239ab?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 2, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 106;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (106, 2, 2, N'Đậu hũ non sốt nấm dầu mè - Công thức vị sa tế cay nồng (ăn được ớt)', N'Sốt sa tế ớt hiểm cay xé lưỡi phủ trên lớp đậu hũ mềm ngậy, kích thích vị giác tột đỉnh.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Đậu hũ non sốt nấm dầu mè.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 310.0, 'EASY', 1, N'https://images.unsplash.com/photo-1569718212165-3a8278d5f624?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 16, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 2);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 107)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Đậu hũ non sốt nấm dầu mè - Công thức dành cho người giảm cân', Description = N'Hấp chín đậu hũ non và sốt nấm hương mỏng nhẹ, ít calo, không tích mỡ thừa.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Đậu hũ non sốt nấm dầu mè.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 200.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1569718212165-3a8278d5f624?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 2, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 107;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (107, 1, 1, N'Đậu hũ non sốt nấm dầu mè - Công thức dành cho người giảm cân', N'Hấp chín đậu hũ non và sốt nấm hương mỏng nhẹ, ít calo, không tích mỡ thừa.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Đậu hũ non sốt nấm dầu mè.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 200.0, 'EASY', 1, N'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 17, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 2);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 108)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Đậu hũ non sốt nấm dầu mè - Công thức giàu đạm thực vật', Description = N'Đậu hũ non kết hợp nấm rơm và hạt sen bùi béo, giàu đạm và axit amin thiết yếu.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Đậu hũ non sốt nấm dầu mè.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 360.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 2, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 108;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (108, 2, 1, N'Đậu hũ non sốt nấm dầu mè - Công thức giàu đạm thực vật', N'Đậu hũ non kết hợp nấm rơm và hạt sen bùi béo, giàu đạm và axit amin thiết yếu.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Đậu hũ non sốt nấm dầu mè.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 360.0, 'EASY', 1, N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 18, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 2);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 109)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Mì căn xào sả ớt giòn cay - Công thức truyền thống gia đình', Description = N'Mì căn chiên vàng xào sả băm thơm phức, nêm nếm nước tương đậm đà ăn cùng cơm nóng.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Mì căn xào sả ớt giòn cay.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 320.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 3, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 109;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (109, 1, 1, N'Mì căn xào sả ớt giòn cay - Công thức truyền thống gia đình', N'Mì căn chiên vàng xào sả băm thơm phức, nêm nếm nước tương đậm đà ăn cùng cơm nóng.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Mì căn xào sả ớt giòn cay.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 320.0, 'EASY', 1, N'https://images.unsplash.com/photo-1628294895950-9805252327bc?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 19, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 3);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 110)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 2, Title = N'Mì căn xào sả ớt giòn cay - Công thức vị sa tế cay nồng (ăn được ớt)', Description = N'Ớt băm cay rực lửa phi cùng sả tạo lớp vỏ ngoài giòn cay tê tái đặc biệt cho tín đồ ăn cay.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Mì căn xào sả ớt giòn cay.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 360.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1628294895950-9805252327bc?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 3, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 110;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (110, 2, 2, N'Mì căn xào sả ớt giòn cay - Công thức vị sa tế cay nồng (ăn được ớt)', N'Ớt băm cay rực lửa phi cùng sả tạo lớp vỏ ngoài giòn cay tê tái đặc biệt cho tín đồ ăn cay.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Mì căn xào sả ớt giòn cay.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 360.0, 'EASY', 1, N'https://images.unsplash.com/photo-1574484284002-952d92456975?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 20, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 3);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 111)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Mì căn xào sả ớt giòn cay - Công thức dành cho người giảm cân', Description = N'Nướng nồi chiên không dầu rồi xào sả thơm, giảm 50% dầu mỡ hỗ trợ kiểm soát calo.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Mì căn xào sả ớt giòn cay.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 250.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1574484284002-952d92456975?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 3, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 111;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (111, 1, 1, N'Mì căn xào sả ớt giòn cay - Công thức dành cho người giảm cân', N'Nướng nồi chiên không dầu rồi xào sả thơm, giảm 50% dầu mỡ hỗ trợ kiểm soát calo.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Mì căn xào sả ớt giòn cay.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 250.0, 'EASY', 1, N'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 21, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 3);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 112)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Mì căn xào sả ớt giòn cay - Công thức giàu đạm thực vật', Description = N'Mì căn nguyên chất xào nấm đông cô, cung cấp lượng đạm thực vật dồi dào tăng cơ bắp.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Mì căn xào sả ớt giòn cay.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 390.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 3, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 112;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (112, 2, 1, N'Mì căn xào sả ớt giòn cay - Công thức giàu đạm thực vật', N'Mì căn nguyên chất xào nấm đông cô, cung cấp lượng đạm thực vật dồi dào tăng cơ bắp.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Mì căn xào sả ớt giòn cay.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 390.0, 'EASY', 1, N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 22, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 3);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 113)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Nấm đùi gà xào húng quế - Công thức truyền thống gia đình', Description = N'Nấm đùi gà ngọt dai xào lá quế thơm dịu, sốt tương ngọt thanh rất đưa cơm.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nấm đùi gà xào húng quế.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 270.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 4, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 113;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (113, 1, 1, N'Nấm đùi gà xào húng quế - Công thức truyền thống gia đình', N'Nấm đùi gà ngọt dai xào lá quế thơm dịu, sốt tương ngọt thanh rất đưa cơm.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nấm đùi gà xào húng quế.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 270.0, 'EASY', 1, N'https://images.unsplash.com/photo-1543339308-43e59d6b73a6?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 23, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 4);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 114)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 2, Title = N'Nấm đùi gà xào húng quế - Công thức vị sa tế cay nồng (ăn được ớt)', Description = N'Thêm sa tế và ớt hiểm cắt lát cay nồng quyện mùi thơm húng quế đặc trưng.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nấm đùi gà xào húng quế.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 300.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1543339308-43e59d6b73a6?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 4, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 114;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (114, 2, 2, N'Nấm đùi gà xào húng quế - Công thức vị sa tế cay nồng (ăn được ớt)', N'Thêm sa tế và ớt hiểm cắt lát cay nồng quyện mùi thơm húng quế đặc trưng.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nấm đùi gà xào húng quế.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 300.0, 'EASY', 1, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 24, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 4);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 115)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Nấm đùi gà xào húng quế - Công thức dành cho người giảm cân', Description = N'Xào áp chảo không thêm đường, nấm giòn ngọt tự nhiên hỗ trợ chế độ ăn Eat Clean.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nấm đùi gà xào húng quế.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 210.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 4, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 115;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (115, 1, 1, N'Nấm đùi gà xào húng quế - Công thức dành cho người giảm cân', N'Xào áp chảo không thêm đường, nấm giòn ngọt tự nhiên hỗ trợ chế độ ăn Eat Clean.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nấm đùi gà xào húng quế.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 210.0, 'EASY', 1, N'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 25, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 4);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 116)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Nấm đùi gà xào húng quế - Công thức giàu đạm thực vật', Description = N'Nấm đùi gà kết hợp tàu hũ ky chiên giòn, lượng đạm vượt trội cho người ăn chay trường.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nấm đùi gà xào húng quế.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 340.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 4, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 116;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (116, 2, 1, N'Nấm đùi gà xào húng quế - Công thức giàu đạm thực vật', N'Nấm đùi gà kết hợp tàu hũ ky chiên giòn, lượng đạm vượt trội cho người ăn chay trường.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nấm đùi gà xào húng quế.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 340.0, 'EASY', 1, N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 26, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 4);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 117)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Măng tây xào hạt điều - Công thức truyền thống gia đình', Description = N'Măng tây xanh non xào giòn cùng hạt điều rang vàng bùi béo, thanh tao và sang trọng.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Măng tây xào hạt điều.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 290.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 5, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 117;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (117, 1, 1, N'Măng tây xào hạt điều - Công thức truyền thống gia đình', N'Măng tây xanh non xào giòn cùng hạt điều rang vàng bùi béo, thanh tao và sang trọng.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Măng tây xào hạt điều.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 290.0, 'EASY', 1, N'https://images.unsplash.com/photo-1515543237350-b3eea1ec8082?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 27, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 5);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 118)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 2, Title = N'Măng tây xào hạt điều - Công thức vị sa tế cay nồng (ăn được ớt)', Description = N'Măng tây xào cùng ớt khô và tiêu sọ cay ấm, tăng hương vị thơm ngon hấp dẫn.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Măng tây xào hạt điều.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 320.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1515543237350-b3eea1ec8082?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 5, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 118;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (118, 2, 2, N'Măng tây xào hạt điều - Công thức vị sa tế cay nồng (ăn được ớt)', N'Măng tây xào cùng ớt khô và tiêu sọ cay ấm, tăng hương vị thơm ngon hấp dẫn.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Măng tây xào hạt điều.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 320.0, 'EASY', 1, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 28, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 5);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 119)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Măng tây xào hạt điều - Công thức dành cho người giảm cân', Description = N'Luộc sơ măng tây rồi xào đảo nhanh cùng hạt điều tươi, ít chất béo, giàu chất chống oxy hóa.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Măng tây xào hạt điều.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 220.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 5, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 119;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (119, 1, 1, N'Măng tây xào hạt điều - Công thức dành cho người giảm cân', N'Luộc sơ măng tây rồi xào đảo nhanh cùng hạt điều tươi, ít chất béo, giàu chất chống oxy hóa.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Măng tây xào hạt điều.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 220.0, 'EASY', 1, N'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 29, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 5);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 120)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Măng tây xào hạt điều - Công thức giàu đạm thực vật', Description = N'Hạt điều bùi béo kết hợp đậu hũ non và nấm tuyết, bổ sung nguồn đạm thực vật quý giá.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Măng tây xào hạt điều.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 370.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 5, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 120;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (120, 2, 1, N'Măng tây xào hạt điều - Công thức giàu đạm thực vật', N'Hạt điều bùi béo kết hợp đậu hũ non và nấm tuyết, bổ sung nguồn đạm thực vật quý giá.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Măng tây xào hạt điều.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 370.0, 'EASY', 1, N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 10, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 5);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 121)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Khổ qua xào đậu hũ thanh nhiệt - Công thức truyền thống gia đình', Description = N'Vị đắng dịu thanh của khổ qua hòa quyện đậu hũ béo mềm giúp giải nhiệt cơ thể hiệu quả.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Khổ qua xào đậu hũ thanh nhiệt.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 240.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 6, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 121;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (121, 1, 1, N'Khổ qua xào đậu hũ thanh nhiệt - Công thức truyền thống gia đình', N'Vị đắng dịu thanh của khổ qua hòa quyện đậu hũ béo mềm giúp giải nhiệt cơ thể hiệu quả.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Khổ qua xào đậu hũ thanh nhiệt.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 240.0, 'EASY', 1, N'https://images.unsplash.com/photo-1540189549336-e6e99c3679fe?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 11, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 6);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 122)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 2, Title = N'Khổ qua xào đậu hũ thanh nhiệt - Công thức vị sa tế cay nồng (ăn được ớt)', Description = N'Ớt chỉ thiên cay nồng áp đảo vị đắng, tạo hương vị cay ấm độc đáo kích thích vị giác.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Khổ qua xào đậu hũ thanh nhiệt.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 270.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1540189549336-e6e99c3679fe?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 6, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 122;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (122, 2, 2, N'Khổ qua xào đậu hũ thanh nhiệt - Công thức vị sa tế cay nồng (ăn được ớt)', N'Ớt chỉ thiên cay nồng áp đảo vị đắng, tạo hương vị cay ấm độc đáo kích thích vị giác.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Khổ qua xào đậu hũ thanh nhiệt.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 270.0, 'EASY', 1, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 12, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 6);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 123)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Khổ qua xào đậu hũ thanh nhiệt - Công thức dành cho người giảm cân', Description = N'Khổ qua bào mỏng xào chín tới cùng đậu phụ non, thanh lọc gan, hạ đường huyết.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Khổ qua xào đậu hũ thanh nhiệt.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 180.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 6, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 123;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (123, 1, 1, N'Khổ qua xào đậu hũ thanh nhiệt - Công thức dành cho người giảm cân', N'Khổ qua bào mỏng xào chín tới cùng đậu phụ non, thanh lọc gan, hạ đường huyết.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Khổ qua xào đậu hũ thanh nhiệt.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 180.0, 'EASY', 1, N'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 13, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 6);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 124)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Khổ qua xào đậu hũ thanh nhiệt - Công thức giàu đạm thực vật', Description = N'Đậu hũ áp chảo vàng giòn xào cùng nấm rơm và khổ qua giòn ngọt.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Khổ qua xào đậu hũ thanh nhiệt.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 320.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 6, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 124;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (124, 2, 1, N'Khổ qua xào đậu hũ thanh nhiệt - Công thức giàu đạm thực vật', N'Đậu hũ áp chảo vàng giòn xào cùng nấm rơm và khổ qua giòn ngọt.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Khổ qua xào đậu hũ thanh nhiệt.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 320.0, 'EASY', 1, N'https://images.unsplash.com/photo-1541832676-9b763b0239ab?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 14, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 6);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 125)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Rau củ thập cẩm xào nấm đông cô - Công thức truyền thống gia đình', Description = N'Đủ loại cà rốt, bắp non, đậu cô ve xào giòn ngọt cùng nấm đông cô ngập tràn màu sắc.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Rau củ thập cẩm xào nấm đông cô.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 250.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1541832676-9b763b0239ab?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 7, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 125;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (125, 1, 1, N'Rau củ thập cẩm xào nấm đông cô - Công thức truyền thống gia đình', N'Đủ loại cà rốt, bắp non, đậu cô ve xào giòn ngọt cùng nấm đông cô ngập tràn màu sắc.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Rau củ thập cẩm xào nấm đông cô.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 250.0, 'EASY', 1, N'https://images.unsplash.com/photo-1543339308-43e59d6b73a6?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 15, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 7);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 126)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 2, Title = N'Rau củ thập cẩm xào nấm đông cô - Công thức vị sa tế cay nồng (ăn được ớt)', Description = N'Ớt chuông đỏ và sa tế cay nồng cho đĩa rau củ xào bắt mắt và đậm vị khó quên.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Rau củ thập cẩm xào nấm đông cô.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 280.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1543339308-43e59d6b73a6?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 7, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 126;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (126, 2, 2, N'Rau củ thập cẩm xào nấm đông cô - Công thức vị sa tế cay nồng (ăn được ớt)', N'Ớt chuông đỏ và sa tế cay nồng cho đĩa rau củ xào bắt mắt và đậm vị khó quên.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Rau củ thập cẩm xào nấm đông cô.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 280.0, 'EASY', 1, N'https://images.unsplash.com/photo-1455619452474-d2be8b1e70cd?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 16, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 7);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 127)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Rau củ thập cẩm xào nấm đông cô - Công thức dành cho người giảm cân', Description = N'Luộc chín tới rồi đảo nhanh qua chảo dầu olive, giữ nguyên vitamin, chất xơ dồi dào.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Rau củ thập cẩm xào nấm đông cô.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 190.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1455619452474-d2be8b1e70cd?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 7, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 127;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (127, 1, 1, N'Rau củ thập cẩm xào nấm đông cô - Công thức dành cho người giảm cân', N'Luộc chín tới rồi đảo nhanh qua chảo dầu olive, giữ nguyên vitamin, chất xơ dồi dào.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Rau củ thập cẩm xào nấm đông cô.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 190.0, 'EASY', 1, N'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 17, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 7);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 128)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Rau củ thập cẩm xào nấm đông cô - Công thức giàu đạm thực vật', Description = N'Bổ sung chả lụa chay thái lát và đậu hà lan giàu đạm thực vật tự nhiên.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Rau củ thập cẩm xào nấm đông cô.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 330.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 7, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 128;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (128, 2, 1, N'Rau củ thập cẩm xào nấm đông cô - Công thức giàu đạm thực vật', N'Bổ sung chả lụa chay thái lát và đậu hà lan giàu đạm thực vật tự nhiên.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Rau củ thập cẩm xào nấm đông cô.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 330.0, 'EASY', 1, N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 18, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 7);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 129)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Bún bò Huế chay - Công thức truyền thống gia đình', Description = N'Nước dùng hầm từ củ quả ngọt lịm kết hợp sả đập dập và nấm rơm, không cay, tròn vị Huế.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bún bò Huế chay.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 350.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 8, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 129;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (129, 1, 1, N'Bún bò Huế chay - Công thức truyền thống gia đình', N'Nước dùng hầm từ củ quả ngọt lịm kết hợp sả đập dập và nấm rơm, không cay, tròn vị Huế.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bún bò Huế chay.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 350.0, 'EASY', 1, N'https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 19, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 8);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 130)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 2, Title = N'Bún bò Huế chay - Công thức vị sa tế cay nồng (ăn được ớt)', Description = N'Màu đỏ rực rỡ từ ớt sa tế Huế cay nồng bốc khói nghi ngút, chuẩn vị bún bò xứ Huế cay đã thèm.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bún bò Huế chay.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 420.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 8, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 130;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (130, 2, 2, N'Bún bò Huế chay - Công thức vị sa tế cay nồng (ăn được ớt)', N'Màu đỏ rực rỡ từ ớt sa tế Huế cay nồng bốc khói nghi ngút, chuẩn vị bún bò xứ Huế cay đã thèm.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bún bò Huế chay.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 420.0, 'EASY', 1, N'https://images.unsplash.com/photo-1569718212165-3a8278d5f624?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 20, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 8);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 131)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Bún bò Huế chay - Công thức dành cho người giảm cân', Description = N'Dùng bún gạo lứt, nước dùng thanh trong không váng dầu mỡ, chỉ 270 kcal no lâu nhẹ bụng.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bún bò Huế chay.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 270.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1569718212165-3a8278d5f624?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 8, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 131;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (131, 1, 1, N'Bún bò Huế chay - Công thức dành cho người giảm cân', N'Dùng bún gạo lứt, nước dùng thanh trong không váng dầu mỡ, chỉ 270 kcal no lâu nhẹ bụng.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bún bò Huế chay.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 270.0, 'EASY', 1, N'https://images.unsplash.com/photo-1594998893017-36147cbcae05?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 21, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 8);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 132)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Bún bò Huế chay - Công thức giàu đạm thực vật', Description = N'Tô bún đầy đặn đậu hũ chiên, chả lụa chay, nấm đùi gà cung cấp hơn 28g đạm chất lượng.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bún bò Huế chay.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 460.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1594998893017-36147cbcae05?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 8, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 132;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (132, 2, 1, N'Bún bò Huế chay - Công thức giàu đạm thực vật', N'Tô bún đầy đặn đậu hũ chiên, chả lụa chay, nấm đùi gà cung cấp hơn 28g đạm chất lượng.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bún bò Huế chay.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 460.0, 'EASY', 1, N'https://images.unsplash.com/photo-1555126634-323283e090fa?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 22, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 8);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 133)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Phở chay Hà Nội nước dùng thanh ngọt - Công thức truyền thống gia đình', Description = N'Nước phở hầm hoa hồi, quế, thảo quả và củ cải ngọt dịu, bánh phở mềm thơm mùi lúa gạo.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Phở chay Hà Nội nước dùng thanh ngọt.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 340.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1555126634-323283e090fa?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 9, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 133;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (133, 1, 1, N'Phở chay Hà Nội nước dùng thanh ngọt - Công thức truyền thống gia đình', N'Nước phở hầm hoa hồi, quế, thảo quả và củ cải ngọt dịu, bánh phở mềm thơm mùi lúa gạo.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Phở chay Hà Nội nước dùng thanh ngọt.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 340.0, 'EASY', 1, N'https://images.unsplash.com/photo-1576577445504-6af96477db52?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 23, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 9);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 134)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 2, Title = N'Phở chay Hà Nội nước dùng thanh ngọt - Công thức vị sa tế cay nồng (ăn được ớt)', Description = N'Ăn kèm tương ớt Hà Nội và ớt chỉ thiên tươi xắt khoanh, ấm áp cho ngày đông lạnh giá.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Phở chay Hà Nội nước dùng thanh ngọt.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 370.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1576577445504-6af96477db52?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 9, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 134;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (134, 2, 2, N'Phở chay Hà Nội nước dùng thanh ngọt - Công thức vị sa tế cay nồng (ăn được ớt)', N'Ăn kèm tương ớt Hà Nội và ớt chỉ thiên tươi xắt khoanh, ấm áp cho ngày đông lạnh giá.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Phở chay Hà Nội nước dùng thanh ngọt.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 370.0, 'EASY', 1, N'https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 24, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 9);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 135)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Phở chay Hà Nội nước dùng thanh ngọt - Công thức dành cho người giảm cân', Description = N'Bánh phở gạo lứt kết hợp nấm bào ngư thanh mát, nước dùng trong vắt ít calo.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Phở chay Hà Nội nước dùng thanh ngọt.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 260.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 9, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 135;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (135, 1, 1, N'Phở chay Hà Nội nước dùng thanh ngọt - Công thức dành cho người giảm cân', N'Bánh phở gạo lứt kết hợp nấm bào ngư thanh mát, nước dùng trong vắt ít calo.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Phở chay Hà Nội nước dùng thanh ngọt.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 260.0, 'EASY', 1, N'https://images.unsplash.com/photo-1594998893017-36147cbcae05?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 25, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 9);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 136)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Phở chay Hà Nội nước dùng thanh ngọt - Công thức giàu đạm thực vật', Description = N'Bổ sung phù trúc giòn rụm và bò lát chay giàu protein thực vật béo ngậy ngon miệng.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Phở chay Hà Nội nước dùng thanh ngọt.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 440.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1594998893017-36147cbcae05?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 9, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 136;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (136, 2, 1, N'Phở chay Hà Nội nước dùng thanh ngọt - Công thức giàu đạm thực vật', N'Bổ sung phù trúc giòn rụm và bò lát chay giàu protein thực vật béo ngậy ngon miệng.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Phở chay Hà Nội nước dùng thanh ngọt.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 440.0, 'EASY', 1, N'https://images.unsplash.com/photo-1555126634-323283e090fa?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 26, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 9);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 137)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Bún riêu cua chay thơm béo - Công thức truyền thống gia đình', Description = N'Riêu cua chay làm từ đậu hũ non và nấm tuyết bùi béo, nước lèo cà chua thanh chua tự nhiên.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bún riêu cua chay thơm béo.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 350.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1555126634-323283e090fa?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 10, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 137;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (137, 1, 1, N'Bún riêu cua chay thơm béo - Công thức truyền thống gia đình', N'Riêu cua chay làm từ đậu hũ non và nấm tuyết bùi béo, nước lèo cà chua thanh chua tự nhiên.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bún riêu cua chay thơm béo.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 350.0, 'EASY', 1, N'https://images.unsplash.com/photo-1555126634-323283e090fa?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 27, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 10);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 138)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 2, Title = N'Bún riêu cua chay thơm béo - Công thức vị sa tế cay nồng (ăn được ớt)', Description = N'Sa tế ớt hạt cay nồng rưới ngập mặt tô bún riêu, cay tê kích thích tột cùng.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bún riêu cua chay thơm béo.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 410.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1555126634-323283e090fa?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 10, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 138;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (138, 2, 2, N'Bún riêu cua chay thơm béo - Công thức vị sa tế cay nồng (ăn được ớt)', N'Sa tế ớt hạt cay nồng rưới ngập mặt tô bún riêu, cay tê kích thích tột cùng.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bún riêu cua chay thơm béo.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 410.0, 'EASY', 1, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 28, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 10);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 139)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Bún riêu cua chay thơm béo - Công thức dành cho người giảm cân', Description = N'Tăng lượng rau muống chẻ và giá đỗ, giảm lượng bún, no lâu mà chỉ 260 kcal.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bún riêu cua chay thơm béo.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 260.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 10, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 139;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (139, 1, 1, N'Bún riêu cua chay thơm béo - Công thức dành cho người giảm cân', N'Tăng lượng rau muống chẻ và giá đỗ, giảm lượng bún, no lâu mà chỉ 260 kcal.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bún riêu cua chay thơm béo.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 260.0, 'EASY', 1, N'https://images.unsplash.com/photo-1594998893017-36147cbcae05?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 29, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 10);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 140)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Bún riêu cua chay thơm béo - Công thức giàu đạm thực vật', Description = N'Riêu cua làm từ đậu nành nguyên chất và chả lụa chay, đạm thực vật cao nuôi dưỡng cơ thể.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bún riêu cua chay thơm béo.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 450.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1594998893017-36147cbcae05?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 10, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 140;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (140, 2, 1, N'Bún riêu cua chay thơm béo - Công thức giàu đạm thực vật', N'Riêu cua làm từ đậu nành nguyên chất và chả lụa chay, đạm thực vật cao nuôi dưỡng cơ thể.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bún riêu cua chay thơm béo.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 450.0, 'EASY', 1, N'https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 10, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 10);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 141)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Hủ tiếu Nam Vang chay - Công thức truyền thống gia đình', Description = N'Sợi hủ tiếu dai ngon, nước dùng củ cải muối ngọt thanh đậm đà kèm tôm chay và nấm.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Hủ tiếu Nam Vang chay.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 340.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 11, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 141;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (141, 1, 1, N'Hủ tiếu Nam Vang chay - Công thức truyền thống gia đình', N'Sợi hủ tiếu dai ngon, nước dùng củ cải muối ngọt thanh đậm đà kèm tôm chay và nấm.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Hủ tiếu Nam Vang chay.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 340.0, 'EASY', 1, N'https://images.unsplash.com/photo-1617093727343-374698b1b08d?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 11, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 11);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 142)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 2, Title = N'Hủ tiếu Nam Vang chay - Công thức vị sa tế cay nồng (ăn được ớt)', Description = N'Sốt tương ớt sa tế cay đậm đặc trộn hủ tiếu khô ăn kèm bát nước súp cay nóng hổi.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Hủ tiếu Nam Vang chay.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 390.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1617093727343-374698b1b08d?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 11, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 142;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (142, 2, 2, N'Hủ tiếu Nam Vang chay - Công thức vị sa tế cay nồng (ăn được ớt)', N'Sốt tương ớt sa tế cay đậm đặc trộn hủ tiếu khô ăn kèm bát nước súp cay nóng hổi.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Hủ tiếu Nam Vang chay.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 390.0, 'EASY', 1, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 12, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 11);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 143)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Hủ tiếu Nam Vang chay - Công thức dành cho người giảm cân', Description = N'Dùng nước súp hầm hoàn toàn từ bắp ngọt và lê, hạn chế tối đa gia vị muối đường.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Hủ tiếu Nam Vang chay.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 250.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 11, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 143;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (143, 1, 1, N'Hủ tiếu Nam Vang chay - Công thức dành cho người giảm cân', N'Dùng nước súp hầm hoàn toàn từ bắp ngọt và lê, hạn chế tối đa gia vị muối đường.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Hủ tiếu Nam Vang chay.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 250.0, 'EASY', 1, N'https://images.unsplash.com/photo-1594998893017-36147cbcae05?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 13, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 11);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 144)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Hủ tiếu Nam Vang chay - Công thức giàu đạm thực vật', Description = N'Thêm đậu hũ ky chiên giòn rụm và nấm đông cô dày thịt bổ sung đạm thực vật dồi dào.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Hủ tiếu Nam Vang chay.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 430.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1594998893017-36147cbcae05?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 11, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 144;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (144, 2, 1, N'Hủ tiếu Nam Vang chay - Công thức giàu đạm thực vật', N'Thêm đậu hũ ky chiên giòn rụm và nấm đông cô dày thịt bổ sung đạm thực vật dồi dào.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Hủ tiếu Nam Vang chay.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 430.0, 'EASY', 1, N'https://images.unsplash.com/photo-1555126634-323283e090fa?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 14, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 11);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 145)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Bánh canh nấm rơm bột gạo - Công thức truyền thống gia đình', Description = N'Sợi bánh canh bột gạo mềm mại, nấm rơm tươi giòn ngọt tự nhiên, nước lèo sánh nhẹ thơm ngon.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bánh canh nấm rơm bột gạo.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 330.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1555126634-323283e090fa?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 12, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 145;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (145, 1, 1, N'Bánh canh nấm rơm bột gạo - Công thức truyền thống gia đình', N'Sợi bánh canh bột gạo mềm mại, nấm rơm tươi giòn ngọt tự nhiên, nước lèo sánh nhẹ thơm ngon.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bánh canh nấm rơm bột gạo.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 330.0, 'EASY', 1, N'https://images.unsplash.com/photo-1594998893017-36147cbcae05?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 15, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 12);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 146)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 2, Title = N'Bánh canh nấm rơm bột gạo - Công thức vị sa tế cay nồng (ăn được ớt)', Description = N'Ớt sa tế cay nồng hòa cùng tiêu sọ đen xay tạo độ ấm nồng rực rỡ ấm lòng.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bánh canh nấm rơm bột gạo.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 380.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1594998893017-36147cbcae05?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 12, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 146;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (146, 2, 2, N'Bánh canh nấm rơm bột gạo - Công thức vị sa tế cay nồng (ăn được ớt)', N'Ớt sa tế cay nồng hòa cùng tiêu sọ đen xay tạo độ ấm nồng rực rỡ ấm lòng.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bánh canh nấm rơm bột gạo.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 380.0, 'EASY', 1, N'https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 16, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 12);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 147)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Bánh canh nấm rơm bột gạo - Công thức dành cho người giảm cân', Description = N'Sợi bánh canh gạo lứt dồi dào chất xơ, nước dùng rau củ ngọt mát chỉ 240 kcal.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bánh canh nấm rơm bột gạo.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 240.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 12, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 147;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (147, 1, 1, N'Bánh canh nấm rơm bột gạo - Công thức dành cho người giảm cân', N'Sợi bánh canh gạo lứt dồi dào chất xơ, nước dùng rau củ ngọt mát chỉ 240 kcal.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bánh canh nấm rơm bột gạo.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 240.0, 'EASY', 1, N'https://images.unsplash.com/photo-1576577445504-6af96477db52?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 17, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 12);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 148)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Bánh canh nấm rơm bột gạo - Công thức giàu đạm thực vật', Description = N'Nấm rơm xào thơm kết hợp đậu hũ chiên vàng và chả nấm giàu đạm tự nhiên.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bánh canh nấm rơm bột gạo.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 420.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1576577445504-6af96477db52?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 12, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 148;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (148, 2, 1, N'Bánh canh nấm rơm bột gạo - Công thức giàu đạm thực vật', N'Nấm rơm xào thơm kết hợp đậu hũ chiên vàng và chả nấm giàu đạm tự nhiên.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Bánh canh nấm rơm bột gạo.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 420.0, 'EASY', 1, N'https://images.unsplash.com/photo-1555126634-323283e090fa?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 18, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 12);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 149)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Mì Quảng chay sợi vàng đậm đà - Công thức truyền thống gia đình', Description = N'Sợi mì vàng óng nghệ, nước nhưn nấm đậm đà vừa phải, rắc đậu phộng rang giòn thơm lừng.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Mì Quảng chay sợi vàng đậm đà.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 360.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1555126634-323283e090fa?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 13, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 149;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (149, 1, 1, N'Mì Quảng chay sợi vàng đậm đà - Công thức truyền thống gia đình', N'Sợi mì vàng óng nghệ, nước nhưn nấm đậm đà vừa phải, rắc đậu phộng rang giòn thơm lừng.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Mì Quảng chay sợi vàng đậm đà.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 360.0, 'EASY', 1, N'https://images.unsplash.com/photo-1552611052-33e04de081de?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 19, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 13);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 150)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 2, Title = N'Mì Quảng chay sợi vàng đậm đà - Công thức vị sa tế cay nồng (ăn được ớt)', Description = N'Ớt sừng xanh cắn giòn cay cay và sa tế ớt dầu đậm chất miền Trung cay nồng.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Mì Quảng chay sợi vàng đậm đà.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 410.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1552611052-33e04de081de?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 13, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 150;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (150, 2, 2, N'Mì Quảng chay sợi vàng đậm đà - Công thức vị sa tế cay nồng (ăn được ớt)', N'Ớt sừng xanh cắn giòn cay cay và sa tế ớt dầu đậm chất miền Trung cay nồng.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Mì Quảng chay sợi vàng đậm đà.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 410.0, 'EASY', 1, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 20, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 13);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 151)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Mì Quảng chay sợi vàng đậm đà - Công thức dành cho người giảm cân', Description = N'Tăng cường rau sống bắp chuối thanh mát, nước nhưn thanh đạm ít dầu.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Mì Quảng chay sợi vàng đậm đà.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 280.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 13, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 151;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (151, 1, 1, N'Mì Quảng chay sợi vàng đậm đà - Công thức dành cho người giảm cân', N'Tăng cường rau sống bắp chuối thanh mát, nước nhưn thanh đạm ít dầu.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Mì Quảng chay sợi vàng đậm đà.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 280.0, 'EASY', 1, N'https://images.unsplash.com/photo-1563379091339-03b21ab4a4f8?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 21, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 13);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 152)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Mì Quảng chay sợi vàng đậm đà - Công thức giàu đạm thực vật', Description = N'Nấm đùi gà xào đậu hũ và hạt điều bùi béo tăng cường lượng đạm dồi dào.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Mì Quảng chay sợi vàng đậm đà.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 460.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1563379091339-03b21ab4a4f8?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 13, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 152;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (152, 2, 1, N'Mì Quảng chay sợi vàng đậm đà - Công thức giàu đạm thực vật', N'Nấm đùi gà xào đậu hũ và hạt điều bùi béo tăng cường lượng đạm dồi dào.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Mì Quảng chay sợi vàng đậm đà.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 460.0, 'EASY', 1, N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 22, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 13);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 153)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Canh chua Nam Bộ chay thanh mát - Công thức truyền thống gia đình', Description = N'Thơm, cà chua, bắp chuối và me chua thanh mát, nêm ngò om ngò gai thơm nức mũi.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Canh chua Nam Bộ chay thanh mát.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 250.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 14, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 153;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (153, 1, 1, N'Canh chua Nam Bộ chay thanh mát - Công thức truyền thống gia đình', N'Thơm, cà chua, bắp chuối và me chua thanh mát, nêm ngò om ngò gai thơm nức mũi.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Canh chua Nam Bộ chay thanh mát.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 250.0, 'EASY', 1, N'https://images.unsplash.com/photo-1547592166-23ac45744acd?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 23, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 14);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 154)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 2, Title = N'Canh chua Nam Bộ chay thanh mát - Công thức vị sa tế cay nồng (ăn được ớt)', Description = N'Vị chua cay hòa quyện tuyệt vời từ ớt hiểm đập dập thơm lừng cay xé lưỡi ấm bụng.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Canh chua Nam Bộ chay thanh mát.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 280.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1547592166-23ac45744acd?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 14, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 154;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (154, 2, 2, N'Canh chua Nam Bộ chay thanh mát - Công thức vị sa tế cay nồng (ăn được ớt)', N'Vị chua cay hòa quyện tuyệt vời từ ớt hiểm đập dập thơm lừng cay xé lưỡi ấm bụng.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Canh chua Nam Bộ chay thanh mát.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 280.0, 'EASY', 1, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 24, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 14);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 155)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Canh chua Nam Bộ chay thanh mát - Công thức dành cho người giảm cân', Description = N'Vị chua thanh tự nhiên từ me và thơm, giải nhiệt, detox thanh lọc cơ thể nhẹ nhàng.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Canh chua Nam Bộ chay thanh mát.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 170.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 14, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 155;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (155, 1, 1, N'Canh chua Nam Bộ chay thanh mát - Công thức dành cho người giảm cân', N'Vị chua thanh tự nhiên từ me và thơm, giải nhiệt, detox thanh lọc cơ thể nhẹ nhàng.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Canh chua Nam Bộ chay thanh mát.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 170.0, 'EASY', 1, N'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 25, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 14);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 156)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Canh chua Nam Bộ chay thanh mát - Công thức giàu đạm thực vật', Description = N'Đậu hũ non béo ngậy kết hợp nấm rơm ngọt nước, giàu protein dưỡng chất.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Canh chua Nam Bộ chay thanh mát.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 310.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 14, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 156;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (156, 2, 1, N'Canh chua Nam Bộ chay thanh mát - Công thức giàu đạm thực vật', N'Đậu hũ non béo ngậy kết hợp nấm rơm ngọt nước, giàu protein dưỡng chất.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Canh chua Nam Bộ chay thanh mát.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 310.0, 'EASY', 1, N'https://images.unsplash.com/photo-1541832676-9b763b0239ab?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 26, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 14);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 157)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Súp bắp ngô ngọt hạt sen dưỡng tâm - Công thức truyền thống gia đình', Description = N'Bắp ngọt Mỹ và hạt sen tươi hầm mềm thơm sánh mịn, an thần ngủ ngon giấc.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Súp bắp ngô ngọt hạt sen dưỡng tâm.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 260.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1541832676-9b763b0239ab?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 15, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 157;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (157, 1, 1, N'Súp bắp ngô ngọt hạt sen dưỡng tâm - Công thức truyền thống gia đình', N'Bắp ngọt Mỹ và hạt sen tươi hầm mềm thơm sánh mịn, an thần ngủ ngon giấc.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Súp bắp ngô ngọt hạt sen dưỡng tâm.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 260.0, 'EASY', 1, N'https://images.unsplash.com/photo-1603105037880-880cd4edfb0d?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 27, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 15);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 158)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 2, Title = N'Súp bắp ngô ngọt hạt sen dưỡng tâm - Công thức vị sa tế cay nồng (ăn được ớt)', Description = N'Rắc tiêu sọ cay ấm và ớt bột paprika tạo màu sắc hấp dẫn và vị cay kích thích.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Súp bắp ngô ngọt hạt sen dưỡng tâm.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 290.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1603105037880-880cd4edfb0d?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 15, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 158;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (158, 2, 2, N'Súp bắp ngô ngọt hạt sen dưỡng tâm - Công thức vị sa tế cay nồng (ăn được ớt)', N'Rắc tiêu sọ cay ấm và ớt bột paprika tạo màu sắc hấp dẫn và vị cay kích thích.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Súp bắp ngô ngọt hạt sen dưỡng tâm.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 290.0, 'EASY', 1, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 28, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 15);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 159)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Súp bắp ngô ngọt hạt sen dưỡng tâm - Công thức dành cho người giảm cân', Description = N'Không dùng bột năng đặc, ninh hạt sen và bắp ngô nguyên bản thanh khiết chỉ 180 kcal.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Súp bắp ngô ngọt hạt sen dưỡng tâm.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 180.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 15, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 159;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (159, 1, 1, N'Súp bắp ngô ngọt hạt sen dưỡng tâm - Công thức dành cho người giảm cân', N'Không dùng bột năng đặc, ninh hạt sen và bắp ngô nguyên bản thanh khiết chỉ 180 kcal.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Súp bắp ngô ngọt hạt sen dưỡng tâm.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 180.0, 'EASY', 1, N'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 29, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 15);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 160)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Súp bắp ngô ngọt hạt sen dưỡng tâm - Công thức giàu đạm thực vật', Description = N'Hạt sen tươi kết hợp nấm tuyết và đậu phụ non cung cấp năng lượng phục hồi thể lực.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Súp bắp ngô ngọt hạt sen dưỡng tâm.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 340.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 15, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 160;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (160, 2, 1, N'Súp bắp ngô ngọt hạt sen dưỡng tâm - Công thức giàu đạm thực vật', N'Hạt sen tươi kết hợp nấm tuyết và đậu phụ non cung cấp năng lượng phục hồi thể lực.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Súp bắp ngô ngọt hạt sen dưỡng tâm.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 340.0, 'EASY', 1, N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 10, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 15);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 161)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Lẩu nấm dưỡng sinh thảo mộc - Công thức truyền thống gia đình', Description = N'Nước lẩu hầm từ táo đỏ, kỷ tử, đảng sâm cùng hơn 6 loại nấm quý ngọt lịm bổ dưỡng.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Lẩu nấm dưỡng sinh thảo mộc.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 380.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 16, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 161;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (161, 1, 1, N'Lẩu nấm dưỡng sinh thảo mộc - Công thức truyền thống gia đình', N'Nước lẩu hầm từ táo đỏ, kỷ tử, đảng sâm cùng hơn 6 loại nấm quý ngọt lịm bổ dưỡng.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Lẩu nấm dưỡng sinh thảo mộc.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 380.0, 'EASY', 1, N'https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 11, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 16);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 162)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 2, Title = N'Lẩu nấm dưỡng sinh thảo mộc - Công thức vị sa tế cay nồng (ăn được ớt)', Description = N'Nồi lẩu nấm sa tế cay tê kiểu Tứ Xuyên với ớt khô đỏ rực thơm lừng.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Lẩu nấm dưỡng sinh thảo mộc.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 430.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 16, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 162;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (162, 2, 2, N'Lẩu nấm dưỡng sinh thảo mộc - Công thức vị sa tế cay nồng (ăn được ớt)', N'Nồi lẩu nấm sa tế cay tê kiểu Tứ Xuyên với ớt khô đỏ rực thơm lừng.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Lẩu nấm dưỡng sinh thảo mộc.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 430.0, 'EASY', 1, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 12, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 16);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 163)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Lẩu nấm dưỡng sinh thảo mộc - Công thức dành cho người giảm cân', Description = N'Nước lẩu thanh khiết nhiều rau xanh nhúng kèm, ít calo, giàu chất chống oxy hóa.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Lẩu nấm dưỡng sinh thảo mộc.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 280.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 16, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 163;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (163, 1, 1, N'Lẩu nấm dưỡng sinh thảo mộc - Công thức dành cho người giảm cân', N'Nước lẩu thanh khiết nhiều rau xanh nhúng kèm, ít calo, giàu chất chống oxy hóa.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Lẩu nấm dưỡng sinh thảo mộc.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 280.0, 'EASY', 1, N'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 13, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 16);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 164)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Lẩu nấm dưỡng sinh thảo mộc - Công thức giàu đạm thực vật', Description = N'Đầy đủ các loại nấm giàu đạm, tàu hũ ky và đậu hũ non giúp tăng cường sức đề kháng.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Lẩu nấm dưỡng sinh thảo mộc.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 480.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 16, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 164;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (164, 2, 1, N'Lẩu nấm dưỡng sinh thảo mộc - Công thức giàu đạm thực vật', N'Đầy đủ các loại nấm giàu đạm, tàu hũ ky và đậu hũ non giúp tăng cường sức đề kháng.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Lẩu nấm dưỡng sinh thảo mộc.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 480.0, 'EASY', 1, N'https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 14, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 16);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 165)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Cơm chiên Dương Châu ngũ sắc - Công thức truyền thống gia đình', Description = N'Hạt cơm tơi xốp chiên cùng cà rốt, bắp ngọt, đậu hà lan và chả chay giòn rụm màu sắc bắt mắt.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cơm chiên Dương Châu ngũ sắc.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 410.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 17, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 165;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (165, 1, 1, N'Cơm chiên Dương Châu ngũ sắc - Công thức truyền thống gia đình', N'Hạt cơm tơi xốp chiên cùng cà rốt, bắp ngọt, đậu hà lan và chả chay giòn rụm màu sắc bắt mắt.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cơm chiên Dương Châu ngũ sắc.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 410.0, 'EASY', 1, N'https://images.unsplash.com/photo-1603133872878-684f208fb84b?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 15, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 17);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 166)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 2, Title = N'Cơm chiên Dương Châu ngũ sắc - Công thức vị sa tế cay nồng (ăn được ớt)', Description = N'Chiên cùng sa tế tôm chay cay nồng và ớt sừng xắt hạt lựu, thơm giòn đậm đà.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cơm chiên Dương Châu ngũ sắc.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 450.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1603133872878-684f208fb84b?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 17, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 166;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (166, 2, 2, N'Cơm chiên Dương Châu ngũ sắc - Công thức vị sa tế cay nồng (ăn được ớt)', N'Chiên cùng sa tế tôm chay cay nồng và ớt sừng xắt hạt lựu, thơm giòn đậm đà.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cơm chiên Dương Châu ngũ sắc.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 450.0, 'EASY', 1, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 16, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 17);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 167)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Cơm chiên Dương Châu ngũ sắc - Công thức dành cho người giảm cân', Description = N'Dùng gạo lứt huyết rồng chiên không dầu bằng chảo chống dính, ít tinh bột xấu.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cơm chiên Dương Châu ngũ sắc.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 310.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 17, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 167;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (167, 1, 1, N'Cơm chiên Dương Châu ngũ sắc - Công thức dành cho người giảm cân', N'Dùng gạo lứt huyết rồng chiên không dầu bằng chảo chống dính, ít tinh bột xấu.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cơm chiên Dương Châu ngũ sắc.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 310.0, 'EASY', 1, N'https://images.unsplash.com/photo-1512058564366-18510be2db19?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 17, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 17);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 168)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Cơm chiên Dương Châu ngũ sắc - Công thức giàu đạm thực vật', Description = N'Bổ sung đậu gà, hạt sen và đậu hũ chiên hạt lựu, giàu protein và khoáng chất.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cơm chiên Dương Châu ngũ sắc.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 490.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1512058564366-18510be2db19?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 17, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 168;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (168, 2, 1, N'Cơm chiên Dương Châu ngũ sắc - Công thức giàu đạm thực vật', N'Bổ sung đậu gà, hạt sen và đậu hũ chiên hạt lựu, giàu protein và khoáng chất.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cơm chiên Dương Châu ngũ sắc.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 490.0, 'EASY', 1, N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 18, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 17);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 169)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Cơm tấm sườn bì chả chay - Công thức truyền thống gia đình', Description = N'Hạt tấm thơm dẻo, sườn non ướp ngũ vị nướng thơm lừng ăn kèm chả trứng chay và mỡ hành.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cơm tấm sườn bì chả chay.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 420.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 18, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 169;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (169, 1, 1, N'Cơm tấm sườn bì chả chay - Công thức truyền thống gia đình', N'Hạt tấm thơm dẻo, sườn non ướp ngũ vị nướng thơm lừng ăn kèm chả trứng chay và mỡ hành.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cơm tấm sườn bì chả chay.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 420.0, 'EASY', 1, N'https://images.unsplash.com/photo-1512058564366-18510be2db19?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 19, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 18);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 170)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 2, Title = N'Cơm tấm sườn bì chả chay - Công thức vị sa tế cay nồng (ăn được ớt)', Description = N'Nước mắm chay pha ớt tỏi băm đỏ rực cay xè chấm cùng miếng sườn chay nướng cay.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cơm tấm sườn bì chả chay.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 460.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1512058564366-18510be2db19?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 18, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 170;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (170, 2, 2, N'Cơm tấm sườn bì chả chay - Công thức vị sa tế cay nồng (ăn được ớt)', N'Nước mắm chay pha ớt tỏi băm đỏ rực cay xè chấm cùng miếng sườn chay nướng cay.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cơm tấm sườn bì chả chay.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 460.0, 'EASY', 1, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 20, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 18);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 171)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Cơm tấm sườn bì chả chay - Công thức dành cho người giảm cân', Description = N'Cơm tấm gạo lứt, sườn nướng nồi chiên không dầu, đồ chua củ cải dưa leo tươi mát.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cơm tấm sườn bì chả chay.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 320.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 18, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 171;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (171, 1, 1, N'Cơm tấm sườn bì chả chay - Công thức dành cho người giảm cân', N'Cơm tấm gạo lứt, sườn nướng nồi chiên không dầu, đồ chua củ cải dưa leo tươi mát.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cơm tấm sườn bì chả chay.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 320.0, 'EASY', 1, N'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 21, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 18);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 172)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Cơm tấm sườn bì chả chay - Công thức giàu đạm thực vật', Description = N'Gấp đôi sườn non đạm đậu nành và chả hấp đậu hũ nấm mèo giàu đạm thực vật.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cơm tấm sườn bì chả chay.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 510.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 18, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 172;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (172, 2, 1, N'Cơm tấm sườn bì chả chay - Công thức giàu đạm thực vật', N'Gấp đôi sườn non đạm đậu nành và chả hấp đậu hũ nấm mèo giàu đạm thực vật.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cơm tấm sườn bì chả chay.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 510.0, 'EASY', 1, N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 22, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 18);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 173)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Cơm niêu nấm kho tộ cháy giòn - Công thức truyền thống gia đình', Description = N'Lớp cháy vàng rụm giòn tan dưới đáy niêu đất ăn cùng nấm đông cô kho tiêu kẹo sánh.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cơm niêu nấm kho tộ cháy giòn.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 380.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 19, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 173;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (173, 1, 1, N'Cơm niêu nấm kho tộ cháy giòn - Công thức truyền thống gia đình', N'Lớp cháy vàng rụm giòn tan dưới đáy niêu đất ăn cùng nấm đông cô kho tiêu kẹo sánh.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cơm niêu nấm kho tộ cháy giòn.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 380.0, 'EASY', 1, N'https://images.unsplash.com/photo-1536304929831-ee1ca9d44906?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 23, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 19);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 174)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 2, Title = N'Cơm niêu nấm kho tộ cháy giòn - Công thức vị sa tế cay nồng (ăn được ớt)', Description = N'Ớt hiểm đỏ kho ngập trong nồi đất thơm nồng vị cay the, chan cơm cháy ngon khó cưỡng.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cơm niêu nấm kho tộ cháy giòn.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 420.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1536304929831-ee1ca9d44906?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 19, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 174;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (174, 2, 2, N'Cơm niêu nấm kho tộ cháy giòn - Công thức vị sa tế cay nồng (ăn được ớt)', N'Ớt hiểm đỏ kho ngập trong nồi đất thơm nồng vị cay the, chan cơm cháy ngon khó cưỡng.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cơm niêu nấm kho tộ cháy giòn.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 420.0, 'EASY', 1, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 24, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 19);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 175)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Cơm niêu nấm kho tộ cháy giòn - Công thức dành cho người giảm cân', Description = N'Cơm niêu gạo mầm ít cháy, nấm đùi gà kho tiêu nhạt không dầu ăn kiêng hiệu quả.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cơm niêu nấm kho tộ cháy giòn.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 290.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 19, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 175;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (175, 1, 1, N'Cơm niêu nấm kho tộ cháy giòn - Công thức dành cho người giảm cân', N'Cơm niêu gạo mầm ít cháy, nấm đùi gà kho tiêu nhạt không dầu ăn kiêng hiệu quả.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cơm niêu nấm kho tộ cháy giòn.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 290.0, 'EASY', 1, N'https://images.unsplash.com/photo-1512058564366-18510be2db19?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 25, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 19);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 176)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Cơm niêu nấm kho tộ cháy giòn - Công thức giàu đạm thực vật', Description = N'Nấm kho tộ thêm đậu hũ chiên và hạt sen tươi cung cấp dinh dưỡng phục hồi năng lượng.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cơm niêu nấm kho tộ cháy giòn.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 470.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1512058564366-18510be2db19?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 19, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 176;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (176, 2, 1, N'Cơm niêu nấm kho tộ cháy giòn - Công thức giàu đạm thực vật', N'Nấm kho tộ thêm đậu hũ chiên và hạt sen tươi cung cấp dinh dưỡng phục hồi năng lượng.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cơm niêu nấm kho tộ cháy giòn.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 470.0, 'EASY', 1, N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 26, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 19);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 177)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Xôi gấc hạt sen chay dẻo thơm - Công thức truyền thống gia đình', Description = N'Hạt nếp nương dẻo thơm ngấm màu gấc đỏ tươi may mắn, hạt sen bùi béo ngọt lành thanh tịnh.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Xôi gấc hạt sen chay dẻo thơm.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 390.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 20, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 177;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (177, 1, 1, N'Xôi gấc hạt sen chay dẻo thơm - Công thức truyền thống gia đình', N'Hạt nếp nương dẻo thơm ngấm màu gấc đỏ tươi may mắn, hạt sen bùi béo ngọt lành thanh tịnh.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Xôi gấc hạt sen chay dẻo thơm.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 390.0, 'EASY', 1, N'https://images.unsplash.com/photo-1505253758473-96b7015fcd40?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 27, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 20);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 178)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 2, Title = N'Xôi gấc hạt sen chay dẻo thơm - Công thức vị sa tế cay nồng (ăn được ớt)', Description = N'Xôi gấc ăn kèm sườn chay rim sa tế cay nồng độc đáo lạ miệng mà cực bắt vị.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Xôi gấc hạt sen chay dẻo thơm.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 440.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1505253758473-96b7015fcd40?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 20, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 178;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (178, 2, 2, N'Xôi gấc hạt sen chay dẻo thơm - Công thức vị sa tế cay nồng (ăn được ớt)', N'Xôi gấc ăn kèm sườn chay rim sa tế cay nồng độc đáo lạ miệng mà cực bắt vị.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Xôi gấc hạt sen chay dẻo thơm.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 440.0, 'EASY', 1, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 28, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 20);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 179)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Xôi gấc hạt sen chay dẻo thơm - Công thức dành cho người giảm cân', Description = N'Nấu kèm hạt diêm mạch quinoa và hạt sen, không thêm đường, thơm tự nhiên chỉ 290 kcal.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Xôi gấc hạt sen chay dẻo thơm.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 290.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 20, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 179;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (179, 1, 1, N'Xôi gấc hạt sen chay dẻo thơm - Công thức dành cho người giảm cân', N'Nấu kèm hạt diêm mạch quinoa và hạt sen, không thêm đường, thơm tự nhiên chỉ 290 kcal.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Xôi gấc hạt sen chay dẻo thơm.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 290.0, 'EASY', 1, N'https://images.unsplash.com/photo-1512058564366-18510be2db19?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 29, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 20);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 180)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Xôi gấc hạt sen chay dẻo thơm - Công thức giàu đạm thực vật', Description = N'Rắc chà bông nấm hương và đậu phộng rang giòn, cung cấp đạm thực vật quý giá dồi dào.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Xôi gấc hạt sen chay dẻo thơm.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 470.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1512058564366-18510be2db19?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 20, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 180;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (180, 2, 1, N'Xôi gấc hạt sen chay dẻo thơm - Công thức giàu đạm thực vật', N'Rắc chà bông nấm hương và đậu phộng rang giòn, cung cấp đạm thực vật quý giá dồi dào.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Xôi gấc hạt sen chay dẻo thơm.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 470.0, 'EASY', 1, N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 10, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 20);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 181)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Đậu hũ kho nấm rơm tiêu đen - Công thức truyền thống gia đình', Description = N'Đậu hũ thấm đẫm nước tương đậm đà, nấm rơm tươi ngọt, rắc tiêu đen xay thơm nức mũi.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Đậu hũ kho nấm rơm tiêu đen.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 290.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 21, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 181;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (181, 1, 1, N'Đậu hũ kho nấm rơm tiêu đen - Công thức truyền thống gia đình', N'Đậu hũ thấm đẫm nước tương đậm đà, nấm rơm tươi ngọt, rắc tiêu đen xay thơm nức mũi.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Đậu hũ kho nấm rơm tiêu đen.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 290.0, 'EASY', 1, N'https://images.unsplash.com/photo-1541832676-9b763b0239ab?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 11, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 21);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 182)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 2, Title = N'Đậu hũ kho nấm rơm tiêu đen - Công thức vị sa tế cay nồng (ăn được ớt)', Description = N'Kho cùng ớt hiểm nguyên trái và tiêu sọ Phú Quốc cay nồng râm ran thơm ngon.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Đậu hũ kho nấm rơm tiêu đen.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 330.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1541832676-9b763b0239ab?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 21, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 182;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (182, 2, 2, N'Đậu hũ kho nấm rơm tiêu đen - Công thức vị sa tế cay nồng (ăn được ớt)', N'Kho cùng ớt hiểm nguyên trái và tiêu sọ Phú Quốc cay nồng râm ran thơm ngon.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Đậu hũ kho nấm rơm tiêu đen.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 330.0, 'EASY', 1, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 12, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 21);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 183)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Đậu hũ kho nấm rơm tiêu đen - Công thức dành cho người giảm cân', Description = N'Đậu hũ áp chảo không dầu mỡ, kho cùng nước dừa tươi ngọt tự nhiên ít muối.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Đậu hũ kho nấm rơm tiêu đen.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 210.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 21, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 183;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (183, 1, 1, N'Đậu hũ kho nấm rơm tiêu đen - Công thức dành cho người giảm cân', N'Đậu hũ áp chảo không dầu mỡ, kho cùng nước dừa tươi ngọt tự nhiên ít muối.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Đậu hũ kho nấm rơm tiêu đen.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 210.0, 'EASY', 1, N'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 13, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 21);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 184)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Đậu hũ kho nấm rơm tiêu đen - Công thức giàu đạm thực vật', Description = N'Gấp đôi lượng đậu hũ và nấm đùi gà, bổ sung nguồn đạm thực vật lành tính dồi dào.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Đậu hũ kho nấm rơm tiêu đen.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 370.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 21, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 184;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (184, 2, 1, N'Đậu hũ kho nấm rơm tiêu đen - Công thức giàu đạm thực vật', N'Gấp đôi lượng đậu hũ và nấm đùi gà, bổ sung nguồn đạm thực vật lành tính dồi dào.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Đậu hũ kho nấm rơm tiêu đen.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 370.0, 'EASY', 1, N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 14, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 21);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 185)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Nấm đông cô kho tiêu hạt sen - Công thức truyền thống gia đình', Description = N'Nấm đông cô dày thịt kho kẹo cùng hạt sen bở tơi, nước sốt sền sệt chấm rau luộc cực ngon.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nấm đông cô kho tiêu hạt sen.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 280.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 22, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 185;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (185, 1, 1, N'Nấm đông cô kho tiêu hạt sen - Công thức truyền thống gia đình', N'Nấm đông cô dày thịt kho kẹo cùng hạt sen bở tơi, nước sốt sền sệt chấm rau luộc cực ngon.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nấm đông cô kho tiêu hạt sen.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 280.0, 'EASY', 1, N'https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 15, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 22);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 186)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 2, Title = N'Nấm đông cô kho tiêu hạt sen - Công thức vị sa tế cay nồng (ăn được ớt)', Description = N'Ớt sừng xắt lát và tiêu xanh cay the ngấm vào nấm đông cô giòn dai sần sật.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nấm đông cô kho tiêu hạt sen.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 310.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 22, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 186;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (186, 2, 2, N'Nấm đông cô kho tiêu hạt sen - Công thức vị sa tế cay nồng (ăn được ớt)', N'Ớt sừng xắt lát và tiêu xanh cay the ngấm vào nấm đông cô giòn dai sần sật.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nấm đông cô kho tiêu hạt sen.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 310.0, 'EASY', 1, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 16, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 22);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 187)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Nấm đông cô kho tiêu hạt sen - Công thức dành cho người giảm cân', Description = N'Kho nhạt bằng nước dừa tươi, nhiều chất xơ từ nấm giúp thanh lọc cơ thể và giảm cân.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nấm đông cô kho tiêu hạt sen.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 200.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 22, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 187;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (187, 1, 1, N'Nấm đông cô kho tiêu hạt sen - Công thức dành cho người giảm cân', N'Kho nhạt bằng nước dừa tươi, nhiều chất xơ từ nấm giúp thanh lọc cơ thể và giảm cân.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nấm đông cô kho tiêu hạt sen.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 200.0, 'EASY', 1, N'https://images.unsplash.com/photo-1543339308-43e59d6b73a6?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 17, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 22);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 188)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Nấm đông cô kho tiêu hạt sen - Công thức giàu đạm thực vật', Description = N'Hạt sen tươi kết hợp nấm đông cô và đậu phụ non, hàm lượng đạm thực vật tối ưu.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nấm đông cô kho tiêu hạt sen.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 360.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1543339308-43e59d6b73a6?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 22, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 188;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (188, 2, 1, N'Nấm đông cô kho tiêu hạt sen - Công thức giàu đạm thực vật', N'Hạt sen tươi kết hợp nấm đông cô và đậu phụ non, hàm lượng đạm thực vật tối ưu.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nấm đông cô kho tiêu hạt sen.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 360.0, 'EASY', 1, N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 18, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 22);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 189)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Cà tím kho tộ nước cốt dừa - Công thức truyền thống gia đình', Description = N'Cà tím mềm béo ngấm vị nước tương và nước cốt dừa ngọt thơm, chuẩn vị miền Tây sông nước.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cà tím kho tộ nước cốt dừa.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 260.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 23, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 189;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (189, 1, 1, N'Cà tím kho tộ nước cốt dừa - Công thức truyền thống gia đình', N'Cà tím mềm béo ngấm vị nước tương và nước cốt dừa ngọt thơm, chuẩn vị miền Tây sông nước.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cà tím kho tộ nước cốt dừa.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 260.0, 'EASY', 1, N'https://images.unsplash.com/photo-1628294895950-9805252327bc?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 19, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 23);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 190)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 2, Title = N'Cà tím kho tộ nước cốt dừa - Công thức vị sa tế cay nồng (ăn được ớt)', Description = N'Ớt hiểm đỏ kho kèm tạo vị cay nồng xua tan vị béo, ăn cùng cơm nóng ngon bất bại.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cà tím kho tộ nước cốt dừa.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 290.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1628294895950-9805252327bc?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 23, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 190;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (190, 2, 2, N'Cà tím kho tộ nước cốt dừa - Công thức vị sa tế cay nồng (ăn được ớt)', N'Ớt hiểm đỏ kho kèm tạo vị cay nồng xua tan vị béo, ăn cùng cơm nóng ngon bất bại.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cà tím kho tộ nước cốt dừa.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 290.0, 'EASY', 1, N'https://images.unsplash.com/photo-1574484284002-952d92456975?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 20, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 23);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 191)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Cà tím kho tộ nước cốt dừa - Công thức dành cho người giảm cân', Description = N'Nướng sơ cà tím rồi kho không nước cốt dừa, thanh nhẹ, giữ nguyên chất chống oxy hóa anthocyanin.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cà tím kho tộ nước cốt dừa.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 180.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1574484284002-952d92456975?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 23, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 191;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (191, 1, 1, N'Cà tím kho tộ nước cốt dừa - Công thức dành cho người giảm cân', N'Nướng sơ cà tím rồi kho không nước cốt dừa, thanh nhẹ, giữ nguyên chất chống oxy hóa anthocyanin.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cà tím kho tộ nước cốt dừa.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 180.0, 'EASY', 1, N'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 21, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 23);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 192)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Cà tím kho tộ nước cốt dừa - Công thức giàu đạm thực vật', Description = N'Kho kèm đậu hũ chiên và nấm rơm, bổ sung đạm và khoáng chất cần thiết.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cà tím kho tộ nước cốt dừa.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 340.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 23, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 192;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (192, 2, 1, N'Cà tím kho tộ nước cốt dừa - Công thức giàu đạm thực vật', N'Kho kèm đậu hũ chiên và nấm rơm, bổ sung đạm và khoáng chất cần thiết.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Cà tím kho tộ nước cốt dừa.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 340.0, 'EASY', 1, N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 22, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 23);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 193)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Gỏi cuốn chay ngũ sắc sốt tương - Công thức truyền thống gia đình', Description = N'Bánh tráng dẻo cuộn bún tươi, đậu hũ chiên, rau thơm và nấm, chấm sốt tương đen đậu phộng bùi béo.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Gỏi cuốn chay ngũ sắc sốt tương.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 240.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 24, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 193;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (193, 1, 1, N'Gỏi cuốn chay ngũ sắc sốt tương - Công thức truyền thống gia đình', N'Bánh tráng dẻo cuộn bún tươi, đậu hũ chiên, rau thơm và nấm, chấm sốt tương đen đậu phộng bùi béo.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Gỏi cuốn chay ngũ sắc sốt tương.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 240.0, 'EASY', 1, N'https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 23, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 24);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 194)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 2, Title = N'Gỏi cuốn chay ngũ sắc sốt tương - Công thức vị sa tế cay nồng (ăn được ớt)', Description = N'Sốt tương chấm pha ớt sa tế xay cay tê tái, chấm ngập cuộn gỏi tươi mát cay đã miệng.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Gỏi cuốn chay ngũ sắc sốt tương.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 270.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 24, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 194;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (194, 2, 2, N'Gỏi cuốn chay ngũ sắc sốt tương - Công thức vị sa tế cay nồng (ăn được ớt)', N'Sốt tương chấm pha ớt sa tế xay cay tê tái, chấm ngập cuộn gỏi tươi mát cay đã miệng.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Gỏi cuốn chay ngũ sắc sốt tương.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 270.0, 'EASY', 1, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 24, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 24);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 195)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Gỏi cuốn chay ngũ sắc sốt tương - Công thức dành cho người giảm cân', Description = N'Bánh tráng gạo lứt cuộn ức gà chay và rau mầm tươi, không sốt béo, hỗ trợ thon gọn vóc dáng.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Gỏi cuốn chay ngũ sắc sốt tương.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 170.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 24, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 195;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (195, 1, 1, N'Gỏi cuốn chay ngũ sắc sốt tương - Công thức dành cho người giảm cân', N'Bánh tráng gạo lứt cuộn ức gà chay và rau mầm tươi, không sốt béo, hỗ trợ thon gọn vóc dáng.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Gỏi cuốn chay ngũ sắc sốt tương.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 170.0, 'EASY', 1, N'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 25, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 24);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 196)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Gỏi cuốn chay ngũ sắc sốt tương - Công thức giàu đạm thực vật', Description = N'Cuộn gấp đôi đậu hũ áp chảo, chả lụa chay và nấm đùi gà, nguồn đạm dồi dào năng lượng.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Gỏi cuốn chay ngũ sắc sốt tương.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 330.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 24, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 196;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (196, 2, 1, N'Gỏi cuốn chay ngũ sắc sốt tương - Công thức giàu đạm thực vật', N'Cuộn gấp đôi đậu hũ áp chảo, chả lụa chay và nấm đùi gà, nguồn đạm dồi dào năng lượng.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Gỏi cuốn chay ngũ sắc sốt tương.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 330.0, 'EASY', 1, N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 26, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 24);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 197)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Chả giò chay khoai môn giòn rụm - Công thức truyền thống gia đình', Description = N'Vỏ bánh tráng xốp giòn tan, nhân khoai môn, cà rốt và nấm mèo thơm lừng chấm nước mắm chua ngọt.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Chả giò chay khoai môn giòn rụm.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 310.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 25, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 197;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (197, 1, 1, N'Chả giò chay khoai môn giòn rụm - Công thức truyền thống gia đình', N'Vỏ bánh tráng xốp giòn tan, nhân khoai môn, cà rốt và nấm mèo thơm lừng chấm nước mắm chua ngọt.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Chả giò chay khoai môn giòn rụm.\nBước 2: Chế biến theo phương pháp công thức truyền thống gia đình, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 310.0, 'EASY', 1, N'https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 27, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 25);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 198)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 2, Title = N'Chả giò chay khoai môn giòn rụm - Công thức vị sa tế cay nồng (ăn được ớt)', Description = N'Nhân chả giò trộn ớt sừng băm cay nồng, ăn kèm ớt tươi cho hương vị bùng nổ.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Chả giò chay khoai môn giòn rụm.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 350.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 25, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 198;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (198, 2, 2, N'Chả giò chay khoai môn giòn rụm - Công thức vị sa tế cay nồng (ăn được ớt)', N'Nhân chả giò trộn ớt sừng băm cay nồng, ăn kèm ớt tươi cho hương vị bùng nổ.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Chả giò chay khoai môn giòn rụm.\nBước 2: Chế biến theo phương pháp công thức vị sa tế cay nồng (ăn được ớt), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 350.0, 'EASY', 1, N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 28, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 25);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 199)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Chả giò chay khoai môn giòn rụm - Công thức dành cho người giảm cân', Description = N'Nướng bằng nồi chiên không dầu giòn rụm, giảm 70% chất béo, phù hợp ăn kiêng.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Chả giò chay khoai môn giòn rụm.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 210.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1565557623262-b51c2513a641?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 25, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 199;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (199, 1, 1, N'Chả giò chay khoai môn giòn rụm - Công thức dành cho người giảm cân', N'Nướng bằng nồi chiên không dầu giòn rụm, giảm 70% chất béo, phù hợp ăn kiêng.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Chả giò chay khoai môn giòn rụm.\nBước 2: Chế biến theo phương pháp công thức dành cho người giảm cân, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 210.0, 'EASY', 1, N'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 29, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 25);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 200)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Chả giò chay khoai môn giòn rụm - Công thức giàu đạm thực vật', Description = N'Nhân trộn đậu hũ nghiền và nấm đông cô băm nhỏ giàu đạm thực vật tự nhiên.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Chả giò chay khoai môn giòn rụm.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 380.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 25, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 200;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (200, 2, 1, N'Chả giò chay khoai môn giòn rụm - Công thức giàu đạm thực vật', N'Nhân trộn đậu hũ nghiền và nấm đông cô băm nhỏ giàu đạm thực vật tự nhiên.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Chả giò chay khoai môn giòn rụm.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 380.0, 'EASY', 1, N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 10, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 25);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 201)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Nước ép cần tây dưa leo táo xanh detox - Công thức thanh nhiệt giải khát truyền thống', Description = N'Nước ép cần tây tươi mát kết hợp dưa leo và táo xanh dịu ngọt, thanh lọc mát gan.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nước ép cần tây dưa leo táo xanh detox.\nBước 2: Chế biến theo phương pháp công thức thanh nhiệt giải khát truyền thống, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 130.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 26, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 201;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (201, 1, 1, N'Nước ép cần tây dưa leo táo xanh detox - Công thức thanh nhiệt giải khát truyền thống', N'Nước ép cần tây tươi mát kết hợp dưa leo và táo xanh dịu ngọt, thanh lọc mát gan.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nước ép cần tây dưa leo táo xanh detox.\nBước 2: Chế biến theo phương pháp công thức thanh nhiệt giải khát truyền thống, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 130.0, 'EASY', 1, N'https://images.unsplash.com/photo-1556881286-fc6915169721?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 11, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 26);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 202)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 2, Title = N'Nước ép cần tây dưa leo táo xanh detox - Công thức vị gừng sả ấm bụng (cay nhẹ)', Description = N'Thêm lát gừng tươi và sả ấm bụng, kích thích tiêu hóa và tuần hoàn máu cực tốt.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nước ép cần tây dưa leo táo xanh detox.\nBước 2: Chế biến theo phương pháp công thức vị gừng sả ấm bụng (cay nhẹ), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 140.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1556881286-fc6915169721?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 26, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 202;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (202, 2, 2, N'Nước ép cần tây dưa leo táo xanh detox - Công thức vị gừng sả ấm bụng (cay nhẹ)', N'Thêm lát gừng tươi và sả ấm bụng, kích thích tiêu hóa và tuần hoàn máu cực tốt.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nước ép cần tây dưa leo táo xanh detox.\nBước 2: Chế biến theo phương pháp công thức vị gừng sả ấm bụng (cay nhẹ), canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 140.0, 'EASY', 1, N'https://images.unsplash.com/photo-1534353473418-4cfa6c56fd38?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 12, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 26);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 203)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Nước ép cần tây dưa leo táo xanh detox - Công thức Detox giảm cân không đường', Description = N'Ép nguyên chất cần tây và dưa leo không đường, chỉ 80 kcal, đánh tan mỡ thừa vòng eo.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nước ép cần tây dưa leo táo xanh detox.\nBước 2: Chế biến theo phương pháp công thức detox giảm cân không đường, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 80.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1534353473418-4cfa6c56fd38?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 26, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 203;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (203, 1, 1, N'Nước ép cần tây dưa leo táo xanh detox - Công thức Detox giảm cân không đường', N'Ép nguyên chất cần tây và dưa leo không đường, chỉ 80 kcal, đánh tan mỡ thừa vòng eo.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nước ép cần tây dưa leo táo xanh detox.\nBước 2: Chế biến theo phương pháp công thức detox giảm cân không đường, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 80.0, 'EASY', 1, N'https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 13, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 26);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 204)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Nước ép cần tây dưa leo táo xanh detox - Công thức giàu vitamin & khoáng chất', Description = N'Bổ sung chanh tươi và mật ong hoa rừng tăng cường hệ miễn dịch khỏe khoắn.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nước ép cần tây dưa leo táo xanh detox.\nBước 2: Chế biến theo phương pháp công thức giàu vitamin & khoáng chất, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 160.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 26, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 204;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (204, 2, 1, N'Nước ép cần tây dưa leo táo xanh detox - Công thức giàu vitamin & khoáng chất', N'Bổ sung chanh tươi và mật ong hoa rừng tăng cường hệ miễn dịch khỏe khoắn.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nước ép cần tây dưa leo táo xanh detox.\nBước 2: Chế biến theo phương pháp công thức giàu vitamin & khoáng chất, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 160.0, 'EASY', 1, N'https://images.unsplash.com/photo-1553530666-ba11a7da3888?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 14, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 26);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 205)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Sinh tố bơ chuối hạt chia giàu đạm - Công thức thanh nhiệt giải khát truyền thống', Description = N'Bơ sáp dẻo quánh xay cùng chuối chín ngọt lịm và sữa hạt thanh béo dịu nhẹ.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Sinh tố bơ chuối hạt chia giàu đạm.\nBước 2: Chế biến theo phương pháp công thức thanh nhiệt giải khát truyền thống, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 280.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1553530666-ba11a7da3888?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 27, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 205;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (205, 1, 1, N'Sinh tố bơ chuối hạt chia giàu đạm - Công thức thanh nhiệt giải khát truyền thống', N'Bơ sáp dẻo quánh xay cùng chuối chín ngọt lịm và sữa hạt thanh béo dịu nhẹ.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Sinh tố bơ chuối hạt chia giàu đạm.\nBước 2: Chế biến theo phương pháp công thức thanh nhiệt giải khát truyền thống, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 280.0, 'EASY', 1, N'https://images.unsplash.com/photo-1553530666-ba11a7da3888?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 15, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 27);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 206)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Sinh tố bơ chuối hạt chia giàu đạm - Công thức vị quế ấm nồng thơm ngát', Description = N'Rắc bột quế thơm nồng ấm và hạt chia giòn sần sật kích thích trao đổi chất.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Sinh tố bơ chuối hạt chia giàu đạm.\nBước 2: Chế biến theo phương pháp công thức vị quế ấm nồng thơm ngát, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 300.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1553530666-ba11a7da3888?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 27, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 206;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (206, 2, 1, N'Sinh tố bơ chuối hạt chia giàu đạm - Công thức vị quế ấm nồng thơm ngát', N'Rắc bột quế thơm nồng ấm và hạt chia giòn sần sật kích thích trao đổi chất.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Sinh tố bơ chuối hạt chia giàu đạm.\nBước 2: Chế biến theo phương pháp công thức vị quế ấm nồng thơm ngát, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 300.0, 'EASY', 1, N'https://images.unsplash.com/photo-1556881286-fc6915169721?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 16, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 27);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 207)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Sinh tố bơ chuối hạt chia giàu đạm - Công thức Detox giảm cân không đường', Description = N'Xay bơ tươi cùng sữa đậu nành không đường và hạt chia, no lâu, kiểm soát cơn thèm ăn.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Sinh tố bơ chuối hạt chia giàu đạm.\nBước 2: Chế biến theo phương pháp công thức detox giảm cân không đường, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 190.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1556881286-fc6915169721?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 27, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 207;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (207, 1, 1, N'Sinh tố bơ chuối hạt chia giàu đạm - Công thức Detox giảm cân không đường', N'Xay bơ tươi cùng sữa đậu nành không đường và hạt chia, no lâu, kiểm soát cơn thèm ăn.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Sinh tố bơ chuối hạt chia giàu đạm.\nBước 2: Chế biến theo phương pháp công thức detox giảm cân không đường, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 190.0, 'EASY', 1, N'https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 17, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 27);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 208)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Sinh tố bơ chuối hạt chia giàu đạm - Công thức giàu đạm thực vật', Description = N'Bổ sung bột protein đậu nành và hạt điều bùi béo, thức uống hoàn hảo sau buổi tập gym.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Sinh tố bơ chuối hạt chia giàu đạm.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 380.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 27, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 208;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (208, 2, 1, N'Sinh tố bơ chuối hạt chia giàu đạm - Công thức giàu đạm thực vật', N'Bổ sung bột protein đậu nành và hạt điều bùi béo, thức uống hoàn hảo sau buổi tập gym.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Sinh tố bơ chuối hạt chia giàu đạm.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 380.0, 'EASY', 1, N'https://images.unsplash.com/photo-1596797038530-2c107229654b?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 18, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 27);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 209)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Trà hoa cúc táo đỏ kỷ tử dưỡng nhan - Công thức thanh nhiệt giải khát truyền thống', Description = N'Hoa cúc vàng thơm thanh, táo đỏ và kỷ tử ninh nước nóng ấm dưỡng nhan sáng mắt.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Trà hoa cúc táo đỏ kỷ tử dưỡng nhan.\nBước 2: Chế biến theo phương pháp công thức thanh nhiệt giải khát truyền thống, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 90.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1596797038530-2c107229654b?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 28, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 209;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (209, 1, 1, N'Trà hoa cúc táo đỏ kỷ tử dưỡng nhan - Công thức thanh nhiệt giải khát truyền thống', N'Hoa cúc vàng thơm thanh, táo đỏ và kỷ tử ninh nước nóng ấm dưỡng nhan sáng mắt.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Trà hoa cúc táo đỏ kỷ tử dưỡng nhan.\nBước 2: Chế biến theo phương pháp công thức thanh nhiệt giải khát truyền thống, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 90.0, 'EASY', 1, N'https://images.unsplash.com/photo-1576092768241-dec231879fc3?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 19, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 28);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 210)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Trà hoa cúc táo đỏ kỷ tử dưỡng nhan - Công thức vị gừng mật ong ấm nóng', Description = N'Thêm gừng tươi thái lát cay ấm, giải cảm và giữ ấm cơ thể trong những ngày mưa.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Trà hoa cúc táo đỏ kỷ tử dưỡng nhan.\nBước 2: Chế biến theo phương pháp công thức vị gừng mật ong ấm nóng, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 110.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1576092768241-dec231879fc3?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 28, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 210;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (210, 2, 1, N'Trà hoa cúc táo đỏ kỷ tử dưỡng nhan - Công thức vị gừng mật ong ấm nóng', N'Thêm gừng tươi thái lát cay ấm, giải cảm và giữ ấm cơ thể trong những ngày mưa.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Trà hoa cúc táo đỏ kỷ tử dưỡng nhan.\nBước 2: Chế biến theo phương pháp công thức vị gừng mật ong ấm nóng, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 110.0, 'EASY', 1, N'https://images.unsplash.com/photo-1544787219-7f47ccb76574?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 20, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 28);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 211)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Trà hoa cúc táo đỏ kỷ tử dưỡng nhan - Công thức Detox giảm cân không đường', Description = N'Hãm hoa cúc và kỷ tử thuần túy không đường, chỉ 45 kcal giúp ngủ sâu và đẹp da.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Trà hoa cúc táo đỏ kỷ tử dưỡng nhan.\nBước 2: Chế biến theo phương pháp công thức detox giảm cân không đường, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 45.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1544787219-7f47ccb76574?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 28, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 211;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (211, 1, 1, N'Trà hoa cúc táo đỏ kỷ tử dưỡng nhan - Công thức Detox giảm cân không đường', N'Hãm hoa cúc và kỷ tử thuần túy không đường, chỉ 45 kcal giúp ngủ sâu và đẹp da.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Trà hoa cúc táo đỏ kỷ tử dưỡng nhan.\nBước 2: Chế biến theo phương pháp công thức detox giảm cân không đường, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 45.0, 'EASY', 1, N'https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 21, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 28);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 212)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Trà hoa cúc táo đỏ kỷ tử dưỡng nhan - Công thức giàu vitamin & khoáng chất', Description = N'Bổ sung long nhãn và hạt sen bùi béo, an thần bổ khí huyết toàn diện.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Trà hoa cúc táo đỏ kỷ tử dưỡng nhan.\nBước 2: Chế biến theo phương pháp công thức giàu vitamin & khoáng chất, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 150.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 28, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 212;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (212, 2, 1, N'Trà hoa cúc táo đỏ kỷ tử dưỡng nhan - Công thức giàu vitamin & khoáng chất', N'Bổ sung long nhãn và hạt sen bùi béo, an thần bổ khí huyết toàn diện.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Trà hoa cúc táo đỏ kỷ tử dưỡng nhan.\nBước 2: Chế biến theo phương pháp công thức giàu vitamin & khoáng chất, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 150.0, 'EASY', 1, N'https://images.unsplash.com/photo-1528751014936-863e6e7a319c?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 22, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 28);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 213)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Sữa hạt sen đậu xanh bùi béo - Công thức thanh nhiệt giải khát truyền thống', Description = N'Hạt sen tươi kết hợp đậu xanh xát vỏ nấu chín mịn, vị ngọt thanh bùi béo tự nhiên.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Sữa hạt sen đậu xanh bùi béo.\nBước 2: Chế biến theo phương pháp công thức thanh nhiệt giải khát truyền thống, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 210.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1528751014936-863e6e7a319c?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 29, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 213;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (213, 1, 1, N'Sữa hạt sen đậu xanh bùi béo - Công thức thanh nhiệt giải khát truyền thống', N'Hạt sen tươi kết hợp đậu xanh xát vỏ nấu chín mịn, vị ngọt thanh bùi béo tự nhiên.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Sữa hạt sen đậu xanh bùi béo.\nBước 2: Chế biến theo phương pháp công thức thanh nhiệt giải khát truyền thống, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 210.0, 'EASY', 1, N'https://images.unsplash.com/photo-1528751014936-863e6e7a319c?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 23, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 29);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 214)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Sữa hạt sen đậu xanh bùi béo - Công thức vị lá dứa thơm ngát', Description = N'Nấu kèm lá dứa tươi tạo màu xanh nhẹ và hương thơm đồng nội quyến rũ.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Sữa hạt sen đậu xanh bùi béo.\nBước 2: Chế biến theo phương pháp công thức vị lá dứa thơm ngát, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 230.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1528751014936-863e6e7a319c?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 29, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 214;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (214, 2, 1, N'Sữa hạt sen đậu xanh bùi béo - Công thức vị lá dứa thơm ngát', N'Nấu kèm lá dứa tươi tạo màu xanh nhẹ và hương thơm đồng nội quyến rũ.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Sữa hạt sen đậu xanh bùi béo.\nBước 2: Chế biến theo phương pháp công thức vị lá dứa thơm ngát, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 230.0, 'EASY', 1, N'https://images.unsplash.com/photo-1576092768241-dec231879fc3?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 24, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 29);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 215)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Sữa hạt sen đậu xanh bùi béo - Công thức Detox giảm cân không đường', Description = N'Sữa hạt sen nguyên chất không thêm đường cát, giàu khoáng chất magiê và chất xơ chỉ 130 kcal.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Sữa hạt sen đậu xanh bùi béo.\nBước 2: Chế biến theo phương pháp công thức detox giảm cân không đường, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 130.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1576092768241-dec231879fc3?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 29, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 215;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (215, 1, 1, N'Sữa hạt sen đậu xanh bùi béo - Công thức Detox giảm cân không đường', N'Sữa hạt sen nguyên chất không thêm đường cát, giàu khoáng chất magiê và chất xơ chỉ 130 kcal.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Sữa hạt sen đậu xanh bùi béo.\nBước 2: Chế biến theo phương pháp công thức detox giảm cân không đường, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 130.0, 'EASY', 1, N'https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 25, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 29);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 216)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Sữa hạt sen đậu xanh bùi béo - Công thức giàu đạm thực vật', Description = N'Bổ sung hạt điều và đậu nành hữu cơ, lượng đạm vượt trội thay thế bữa sáng nhẹ nhàng.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Sữa hạt sen đậu xanh bùi béo.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 290.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 29, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 216;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (216, 2, 1, N'Sữa hạt sen đậu xanh bùi béo - Công thức giàu đạm thực vật', N'Bổ sung hạt điều và đậu nành hữu cơ, lượng đạm vượt trội thay thế bữa sáng nhẹ nhàng.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Sữa hạt sen đậu xanh bùi béo.\nBước 2: Chế biến theo phương pháp công thức giàu đạm thực vật, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 290.0, 'EASY', 1, N'https://images.unsplash.com/photo-1553530666-ba11a7da3888?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 26, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 29);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 217)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Nước ép củ dền cà rốt bổ máu - Công thức thanh nhiệt giải khát truyền thống', Description = N'Củ dền đỏ thắm ép cùng cà rốt và cam tươi, giàu sắt, beta-carotene và vitamin C.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nước ép củ dền cà rốt bổ máu.\nBước 2: Chế biến theo phương pháp công thức thanh nhiệt giải khát truyền thống, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 140.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1553530666-ba11a7da3888?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 30, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 217;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (217, 1, 1, N'Nước ép củ dền cà rốt bổ máu - Công thức thanh nhiệt giải khát truyền thống', N'Củ dền đỏ thắm ép cùng cà rốt và cam tươi, giàu sắt, beta-carotene và vitamin C.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nước ép củ dền cà rốt bổ máu.\nBước 2: Chế biến theo phương pháp công thức thanh nhiệt giải khát truyền thống, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 140.0, 'EASY', 1, N'https://images.unsplash.com/photo-1534353473418-4cfa6c56fd38?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 27, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 30);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 218)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 2, Title = N'Nước ép củ dền cà rốt bổ máu - Công thức vị gừng cay ấm giải độc', Description = N'Vắt thêm nước gừng cay the ấm nồng kích thích lưu thông máu và làm ấm cơ thể.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nước ép củ dền cà rốt bổ máu.\nBước 2: Chế biến theo phương pháp công thức vị gừng cay ấm giải độc, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 150.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1534353473418-4cfa6c56fd38?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 30, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 218;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (218, 2, 2, N'Nước ép củ dền cà rốt bổ máu - Công thức vị gừng cay ấm giải độc', N'Vắt thêm nước gừng cay the ấm nồng kích thích lưu thông máu và làm ấm cơ thể.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nước ép củ dền cà rốt bổ máu.\nBước 2: Chế biến theo phương pháp công thức vị gừng cay ấm giải độc, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 150.0, 'EASY', 1, N'https://images.unsplash.com/photo-1556881286-fc6915169721?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 28, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 30);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 219)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 1, Dietary_School_ID = 1, Title = N'Nước ép củ dền cà rốt bổ máu - Công thức Detox giảm cân không đường', Description = N'Ép nguyên chất củ dền và cần tây không đường, bổ sung nitrat tự nhiên tăng sức bền chỉ 90 kcal.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nước ép củ dền cà rốt bổ máu.\nBước 2: Chế biến theo phương pháp công thức detox giảm cân không đường, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 90.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1556881286-fc6915169721?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 30, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 219;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (219, 1, 1, N'Nước ép củ dền cà rốt bổ máu - Công thức Detox giảm cân không đường', N'Ép nguyên chất củ dền và cần tây không đường, bổ sung nitrat tự nhiên tăng sức bền chỉ 90 kcal.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nước ép củ dền cà rốt bổ máu.\nBước 2: Chế biến theo phương pháp công thức detox giảm cân không đường, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 90.0, 'EASY', 1, N'https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 29, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 30);
END


IF EXISTS (SELECT 1 FROM dbo.Recipes WHERE Recipe_ID = 220)
BEGIN
    UPDATE dbo.Recipes
    SET User_ID = 2, Dietary_School_ID = 1, Title = N'Nước ép củ dền cà rốt bổ máu - Công thức giàu vitamin & khoáng chất', Description = N'Bổ sung táo đỏ và mật ong rừng giúp da dẻ hồng hào, phục hồi sinh lực.',
        Instructions = N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nước ép củ dền cà rốt bổ máu.\nBước 2: Chế biến theo phương pháp công thức giàu vitamin & khoáng chất, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', Prep_Time_Minutes = 15, Cook_Time_Minutes = 25, Servings = 2,
        Calories_Kcal = 170.0, Difficulty_Level = 'EASY', Is_Verified = 1, Image_Url = N'https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?auto=format&fit=crop&w=800&q=80',
        Status = 'APPROVED_ACTIVE', Food_ID = 30, Updated_At = SYSDATETIME()
    WHERE Recipe_ID = 220;
END
ELSE
BEGIN
    INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At, Updated_At, Food_ID)
    VALUES (220, 2, 1, N'Nước ép củ dền cà rốt bổ máu - Công thức giàu vitamin & khoáng chất', N'Bổ sung táo đỏ và mật ong rừng giúp da dẻ hồng hào, phục hồi sinh lực.', N'Bước 1: Sơ chế và làm sạch các nguyên liệu tươi cho món Nước ép củ dền cà rốt bổ máu.\nBước 2: Chế biến theo phương pháp công thức giàu vitamin & khoáng chất, canh nhiệt chuẩn để giữ trọn dưỡng chất.\nBước 3: Nêm nếm vừa khẩu vị, bày ra tô/đĩa và thưởng thức khi còn nóng hổi.', 15, 25, 2, 170.0, 'EASY', 1, N'https://images.unsplash.com/photo-1528751014936-863e6e7a319c?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 10, 'SYSTEM', SYSDATETIME(), SYSDATETIME(), 30);
END


SET IDENTITY_INSERT dbo.Recipes OFF;
GO

-- 3. LÀM MỚI NGUYÊN LIỆU ĐA DẠNG CHO 120 CÔNG THỨC
DELETE FROM dbo.RecipeIngredients WHERE Recipe_ID BETWEEN 101 AND 220;
GO

INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (101, 12, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (101, 80, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (101, 18, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (101, 37, 15, N'g', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (101, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (102, 12, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (102, 80, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (102, 18, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (102, 63, 15, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (102, 105, 25, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (102, 42, 20, N'ml', N'Hòa quyện tạo nước sốt cay đậm đà', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (103, 12, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (103, 80, 150, N'g', N'Hấp hoặc luộc chín tới, thanh đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (103, 18, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (103, 37, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (103, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (104, 12, 200, N'g', N'Sơ chế sạch, dùng kèm nguồn đạm thực vật', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (104, 80, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (104, 38, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (104, 82, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (104, 37, 15, N'g', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (105, 39, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (105, 82, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (105, 84, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (105, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (105, 37, 15, N'g', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (106, 39, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (106, 82, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (106, 63, 15, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (106, 105, 25, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (106, 42, 20, N'ml', N'Hòa quyện tạo nước sốt cay đậm đà', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (107, 39, 150, N'g', N'Hấp hoặc luộc chín tới, thanh đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (107, 82, 150, N'g', N'Hấp hoặc luộc chín tới, thanh đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (107, 42, 5, N'ml', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (107, 37, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (107, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (108, 39, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (108, 82, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (108, 66, 60, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (108, 38, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (108, 37, 15, N'g', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (109, 108, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (109, 105, 5, N'g', N'Gia vị điểm xuyết nhẹ nhàng', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (109, 96, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (109, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (109, 97, 15, N'g', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (110, 108, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (110, 105, 25, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (110, 63, 15, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (110, 96, 20, N'ml', N'Hòa quyện tạo nước sốt cay đậm đà', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (110, 42, 20, N'ml', N'Hòa quyện tạo nước sốt cay đậm đà', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (111, 108, 150, N'g', N'Hấp hoặc luộc chín tới, thanh đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (111, 105, 5, N'g', N'Gia vị điểm xuyết nhẹ nhàng', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (111, 96, 5, N'ml', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (111, 37, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (111, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (112, 108, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (112, 82, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (112, 105, 5, N'g', N'Gia vị điểm xuyết nhẹ nhàng', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (112, 96, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (112, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (113, 82, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (113, 92, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (113, 96, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (113, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (113, 97, 15, N'g', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (114, 82, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (114, 92, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (114, 63, 15, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (114, 105, 25, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (114, 42, 20, N'ml', N'Hòa quyện tạo nước sốt cay đậm đà', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (115, 82, 150, N'g', N'Hấp hoặc luộc chín tới, thanh đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (115, 92, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (115, 96, 5, N'ml', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (115, 37, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (115, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (116, 82, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (116, 108, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (116, 92, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (116, 96, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (116, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (117, 18, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (117, 44, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (117, 96, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (117, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (117, 97, 15, N'g', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (118, 18, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (118, 44, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (118, 63, 15, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (118, 113, 5, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (118, 42, 20, N'ml', N'Hòa quyện tạo nước sốt cay đậm đà', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (119, 18, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (119, 44, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (119, 96, 5, N'ml', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (119, 37, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (119, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (120, 18, 200, N'g', N'Sơ chế sạch, dùng kèm nguồn đạm thực vật', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (120, 44, 60, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (120, 38, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (120, 82, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (120, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (121, 69, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (121, 38, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (121, 96, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (121, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (121, 97, 15, N'g', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (122, 69, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (122, 38, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (122, 63, 15, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (122, 105, 25, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (122, 42, 20, N'ml', N'Hòa quyện tạo nước sốt cay đậm đà', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (123, 69, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (123, 38, 150, N'g', N'Hấp hoặc luộc chín tới, thanh đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (123, 96, 5, N'ml', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (123, 37, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (123, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (124, 69, 200, N'g', N'Sơ chế sạch, dùng kèm nguồn đạm thực vật', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (124, 38, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (124, 90, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (124, 96, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (124, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (125, 18, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (125, 5, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (125, 82, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (125, 96, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (125, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (126, 18, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (126, 5, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (126, 82, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (126, 63, 15, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (126, 42, 20, N'ml', N'Hòa quyện tạo nước sốt cay đậm đà', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (127, 18, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (127, 5, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (127, 82, 150, N'g', N'Hấp hoặc luộc chín tới, thanh đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (127, 37, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (127, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (128, 18, 200, N'g', N'Sơ chế sạch, dùng kèm nguồn đạm thực vật', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (128, 5, 200, N'g', N'Sơ chế sạch, dùng kèm nguồn đạm thực vật', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (128, 82, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (128, 108, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (128, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (129, 16, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (129, 82, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (129, 105, 5, N'g', N'Gia vị điểm xuyết nhẹ nhàng', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (129, 38, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (129, 28, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (130, 16, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (130, 82, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (130, 105, 25, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (130, 63, 15, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (130, 38, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (131, 16, 120, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (131, 82, 150, N'g', N'Hấp hoặc luộc chín tới, thanh đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (131, 105, 5, N'g', N'Gia vị điểm xuyết nhẹ nhàng', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (131, 18, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (131, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (132, 16, 200, N'g', N'Sơ chế sạch, dùng kèm nguồn đạm thực vật', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (132, 38, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (132, 108, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (132, 82, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (132, 105, 5, N'g', N'Gia vị điểm xuyết nhẹ nhàng', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (133, 16, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (133, 83, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (133, 84, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (133, 28, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (133, 53, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (134, 16, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (134, 83, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (134, 84, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (134, 63, 15, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (134, 53, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (135, 16, 120, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (135, 83, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (135, 18, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (135, 28, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (135, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (136, 16, 200, N'g', N'Sơ chế sạch, dùng kèm nguồn đạm thực vật', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (136, 108, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (136, 38, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (136, 83, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (136, 84, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (137, 16, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (137, 39, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (137, 17, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (137, 38, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (137, 108, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (138, 16, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (138, 39, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (138, 17, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (138, 63, 15, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (138, 105, 25, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (139, 16, 120, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (139, 39, 150, N'g', N'Hấp hoặc luộc chín tới, thanh đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (139, 17, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (139, 18, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (139, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (140, 16, 200, N'g', N'Sơ chế sạch, dùng kèm nguồn đạm thực vật', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (140, 39, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (140, 38, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (140, 108, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (140, 17, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (141, 16, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (141, 28, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (141, 5, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (141, 82, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (141, 38, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (142, 16, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (142, 28, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (142, 63, 15, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (142, 82, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (142, 38, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (143, 16, 120, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (143, 28, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (143, 18, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (143, 82, 150, N'g', N'Hấp hoặc luộc chín tới, thanh đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (143, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (144, 16, 200, N'g', N'Sơ chế sạch, dùng kèm nguồn đạm thực vật', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (144, 108, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (144, 82, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (144, 38, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (144, 28, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (145, 16, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (145, 90, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (145, 18, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (145, 38, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (145, 28, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (146, 16, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (146, 90, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (146, 63, 15, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (146, 113, 5, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (146, 38, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (147, 16, 120, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (147, 90, 150, N'g', N'Hấp hoặc luộc chín tới, thanh đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (147, 18, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (147, 38, 150, N'g', N'Hấp hoặc luộc chín tới, thanh đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (147, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (148, 16, 200, N'g', N'Sơ chế sạch, dùng kèm nguồn đạm thực vật', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (148, 90, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (148, 38, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (148, 108, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (148, 18, 200, N'g', N'Sơ chế sạch, dùng kèm nguồn đạm thực vật', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (149, 16, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (149, 38, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (149, 82, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (149, 44, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (149, 97, 15, N'g', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (150, 16, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (150, 38, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (150, 82, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (150, 63, 15, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (150, 44, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (151, 16, 120, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (151, 38, 150, N'g', N'Hấp hoặc luộc chín tới, thanh đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (151, 18, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (151, 44, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (151, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (152, 16, 200, N'g', N'Sơ chế sạch, dùng kèm nguồn đạm thực vật', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (152, 38, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (152, 82, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (152, 108, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (152, 44, 60, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (153, 38, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (153, 17, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (153, 31, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (153, 29, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (153, 90, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (154, 38, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (154, 17, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (154, 31, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (154, 63, 15, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (154, 105, 25, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (155, 38, 150, N'g', N'Hấp hoặc luộc chín tới, thanh đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (155, 17, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (155, 31, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (155, 29, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (155, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (156, 38, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (156, 39, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (156, 90, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (156, 17, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (156, 31, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (157, 66, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (157, 5, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (157, 82, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (157, 18, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (157, 97, 15, N'g', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (158, 66, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (158, 5, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (158, 82, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (158, 63, 15, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (158, 113, 5, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (159, 66, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (159, 5, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (159, 18, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (159, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (159, 37, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (160, 66, 60, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (160, 39, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (160, 82, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (160, 5, 200, N'g', N'Sơ chế sạch, dùng kèm nguồn đạm thực vật', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (160, 18, 200, N'g', N'Sơ chế sạch, dùng kèm nguồn đạm thực vật', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (161, 82, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (161, 85, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (161, 86, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (161, 80, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (161, 21, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (162, 82, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (162, 85, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (162, 86, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (162, 63, 15, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (162, 105, 25, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (163, 82, 150, N'g', N'Hấp hoặc luộc chín tới, thanh đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (163, 85, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (163, 80, 150, N'g', N'Hấp hoặc luộc chín tới, thanh đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (163, 18, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (163, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (164, 82, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (164, 85, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (164, 38, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (164, 108, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (164, 66, 60, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (165, 52, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (165, 18, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (165, 36, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (165, 5, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (165, 25, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (166, 52, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (166, 18, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (166, 5, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (166, 63, 15, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (166, 105, 25, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (167, 52, 120, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (167, 18, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (167, 5, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (167, 37, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (167, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (168, 52, 200, N'g', N'Sơ chế sạch, dùng kèm nguồn đạm thực vật', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (168, 66, 60, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (168, 38, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (168, 108, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (168, 18, 200, N'g', N'Sơ chế sạch, dùng kèm nguồn đạm thực vật', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (169, 52, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (169, 108, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (169, 25, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (169, 49, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (169, 17, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (170, 52, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (170, 108, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (170, 25, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (170, 63, 15, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (170, 105, 25, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (171, 52, 120, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (171, 108, 150, N'g', N'Hấp hoặc luộc chín tới, thanh đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (171, 18, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (171, 49, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (171, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (172, 52, 200, N'g', N'Sơ chế sạch, dùng kèm nguồn đạm thực vật', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (172, 108, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (172, 38, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (172, 82, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (172, 49, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (173, 52, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (173, 90, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (173, 82, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (173, 96, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (173, 113, 5, N'g', N'Gia vị điểm xuyết nhẹ nhàng', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (174, 52, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (174, 90, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (174, 82, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (174, 63, 15, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (174, 113, 5, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (175, 52, 120, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (175, 82, 150, N'g', N'Hấp hoặc luộc chín tới, thanh đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (175, 96, 5, N'ml', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (175, 37, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (175, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (176, 52, 200, N'g', N'Sơ chế sạch, dùng kèm nguồn đạm thực vật', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (176, 82, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (176, 38, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (176, 66, 60, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (176, 96, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (177, 52, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (177, 66, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (177, 44, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (177, 62, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (177, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (178, 52, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (178, 66, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (178, 108, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (178, 63, 15, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (178, 42, 20, N'ml', N'Hòa quyện tạo nước sốt cay đậm đà', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (179, 52, 120, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (179, 66, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (179, 44, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (179, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (179, 37, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (180, 52, 200, N'g', N'Sơ chế sạch, dùng kèm nguồn đạm thực vật', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (180, 66, 60, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (180, 82, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (180, 44, 60, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (180, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (181, 38, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (181, 90, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (181, 96, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (181, 113, 5, N'g', N'Gia vị điểm xuyết nhẹ nhàng', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (181, 97, 15, N'g', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (182, 38, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (182, 90, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (182, 63, 15, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (182, 113, 5, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (182, 96, 20, N'ml', N'Hòa quyện tạo nước sốt cay đậm đà', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (183, 38, 150, N'g', N'Hấp hoặc luộc chín tới, thanh đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (183, 90, 150, N'g', N'Hấp hoặc luộc chín tới, thanh đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (183, 96, 5, N'ml', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (183, 37, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (183, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (184, 38, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (184, 90, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (184, 82, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (184, 96, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (184, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (185, 82, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (185, 84, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (185, 66, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (185, 113, 5, N'g', N'Gia vị điểm xuyết nhẹ nhàng', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (185, 96, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (186, 82, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (186, 66, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (186, 63, 15, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (186, 113, 5, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (186, 96, 20, N'ml', N'Hòa quyện tạo nước sốt cay đậm đà', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (187, 82, 150, N'g', N'Hấp hoặc luộc chín tới, thanh đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (187, 66, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (187, 96, 5, N'ml', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (187, 37, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (187, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (188, 82, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (188, 66, 60, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (188, 38, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (188, 96, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (188, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (189, 19, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (189, 90, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (189, 96, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (189, 113, 5, N'g', N'Gia vị điểm xuyết nhẹ nhàng', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (189, 97, 15, N'g', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (190, 19, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (190, 90, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (190, 63, 15, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (190, 113, 5, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (190, 96, 20, N'ml', N'Hòa quyện tạo nước sốt cay đậm đà', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (191, 19, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (191, 90, 150, N'g', N'Hấp hoặc luộc chín tới, thanh đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (191, 96, 5, N'ml', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (191, 37, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (191, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (192, 19, 200, N'g', N'Sơ chế sạch, dùng kèm nguồn đạm thực vật', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (192, 38, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (192, 90, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (192, 96, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (192, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (193, 1, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (193, 16, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (193, 38, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (193, 18, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (193, 49, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (194, 1, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (194, 16, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (194, 38, 180, N'g', N'Cắt lát vừa ăn, chiên sơ cho thơm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (194, 63, 15, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (194, 105, 25, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (195, 1, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (195, 38, 150, N'g', N'Hấp hoặc luộc chín tới, thanh đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (195, 18, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (195, 49, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (195, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (196, 1, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (196, 38, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (196, 108, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (196, 82, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (196, 16, 200, N'g', N'Sơ chế sạch, dùng kèm nguồn đạm thực vật', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (197, 1, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (197, 88, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (197, 18, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (197, 40, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (197, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (198, 1, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (198, 88, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (198, 18, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (198, 63, 15, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (198, 42, 20, N'ml', N'Hòa quyện tạo nước sốt cay đậm đà', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (199, 1, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (199, 88, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (199, 18, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (199, 40, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (199, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (200, 1, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (200, 88, 200, N'g', N'Sơ chế sạch, dùng kèm nguồn đạm thực vật', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (200, 38, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (200, 82, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (200, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (201, 18, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (201, 49, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (201, 97, 15, N'g', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (201, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (201, 63, 5, N'g', N'Gia vị điểm xuyết nhẹ nhàng', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (202, 18, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (202, 49, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (202, 105, 25, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (202, 97, 20, N'g', N'Hòa quyện tạo nước sốt cay đậm đà', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (202, 63, 15, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (203, 18, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (203, 49, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (203, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (203, 37, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (203, 28, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (204, 18, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (204, 49, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (204, 97, 15, N'g', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (204, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (204, 31, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (205, 18, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (205, 44, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (205, 97, 15, N'g', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (205, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (205, 66, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (206, 18, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (206, 44, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (206, 53, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (206, 97, 15, N'g', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (206, 66, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (207, 18, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (207, 44, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (207, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (207, 37, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (207, 38, 150, N'g', N'Hấp hoặc luộc chín tới, thanh đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (208, 18, 200, N'g', N'Sơ chế sạch, dùng kèm nguồn đạm thực vật', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (208, 44, 60, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (208, 38, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (208, 66, 60, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (208, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (209, 21, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (209, 66, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (209, 97, 15, N'g', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (209, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (209, 18, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (210, 21, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (210, 66, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (210, 105, 5, N'g', N'Gia vị điểm xuyết nhẹ nhàng', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (210, 97, 15, N'g', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (210, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (211, 21, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (211, 66, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (211, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (211, 37, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (211, 18, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (212, 21, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (212, 66, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (212, 44, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (212, 97, 15, N'g', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (212, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (213, 66, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (213, 44, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (213, 97, 15, N'g', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (213, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (213, 18, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (214, 66, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (214, 44, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (214, 92, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (214, 97, 15, N'g', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (214, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (215, 66, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (215, 44, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (215, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (215, 37, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (215, 18, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (216, 66, 60, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (216, 44, 60, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (216, 38, 250, N'g', N'Cắt miếng vừa ăn, áp chảo vàng giòn tăng đạm', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (216, 97, 15, N'g', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (216, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (217, 18, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (217, 5, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (217, 97, 15, N'g', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (217, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (217, 31, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (218, 18, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (218, 5, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (218, 105, 25, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (218, 97, 20, N'g', N'Hòa quyện tạo nước sốt cay đậm đà', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (218, 63, 15, N'g', N'Ớt hiểm đỏ và sa tế phi thơm cay nồng rực rỡ', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (219, 18, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (219, 5, 280, N'g', N'Rửa sạch, cắt khúc vừa ăn, giữ nguyên độ tươi giòn', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (219, 97, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (219, 37, 5, N'g', N'Nêm nhạt thanh dịu, hạn chế gia vị', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (219, 28, 100, N'g', N'Sơ chế sạch và để ráo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (220, 18, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (220, 5, 220, N'g', N'Sơ chế sạch, cắt lát mỏng đều', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (220, 66, 40, N'g', N'Rửa sạch, ngâm mềm rồi nấu chín bùi béo', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (220, 97, 15, N'g', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional) VALUES (220, 42, 15, N'ml', N'Nêm nếm vừa miệng chuẩn vị gia đình', 0);
GO

-- 4. CẬP NHẬT LẠI THỰC ĐƠN ĐỂ HIỂN THỊ CÁC CÔNG THỨC MỚI
UPDATE ms SET Recipe_ID = 101 + ((ms.Slot_ID - 1) % 120) FROM dbo.MealSlots ms;
GO

PRINT N'===> SEED 30 FOODS & 120 RECIPES HOÀN TẤT THÀNH CÔNG (NGUYÊN LIỆU ĐA DẠNG & THỰC TẾ)!';
GO
