-- ==============================================================================
-- Script nạp dữ liệu mẫu cho Công thức nấu ăn (Recipes, RecipeIngredients, Posts, Videos, VideoTranscripts, Votes, Comments)
-- Phục vụ kiểm thử FN-POST-01 (Tìm kiếm & lọc) và FN-POST-02 (Xem chi tiết)
-- ==============================================================================

USE [VeggieLifeDb];
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 1. Đảm bảo có User tác giả (User_ID = 1 đã có từ seed Admin)
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE User_ID = 2)
BEGIN
    INSERT INTO dbo.Users (Email, Full_Name, Role, Account_Status, Created_At)
    VALUES ('member@veggielife.vn', N'Nguyễn Văn Chay', 'MEMBER', 'ACTIVE', SYSDATETIME());
END

-- 2. Xóa dữ liệu cũ nếu chạy lại
DELETE FROM dbo.Votes WHERE Post_ID IN (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID BETWEEN 101 AND 106);
DELETE FROM dbo.Comments WHERE Post_ID IN (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID BETWEEN 101 AND 106);
DELETE FROM dbo.VideoTranscripts WHERE Recipe_ID BETWEEN 101 AND 106;
DELETE FROM dbo.Videos WHERE Post_ID IN (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID BETWEEN 101 AND 106);
DELETE FROM dbo.Posts WHERE Recipe_ID BETWEEN 101 AND 106;
DELETE FROM dbo.RecipeIngredients WHERE Recipe_ID BETWEEN 101 AND 106;

SET IDENTITY_INSERT dbo.Recipes ON;
DELETE FROM dbo.Recipes WHERE Recipe_ID BETWEEN 101 AND 106;

-- 3. Nạp 6 công thức mẫu (ID 101 - 106)
-- Công thức 101: APPROVED_ACTIVE, VEGAN (7), Món Canh (3), Nấu 25p, Đậu hũ + Nấm rơm
INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Category_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At)
VALUES (
    101, 1, 3, 7, 
    N'Canh nấm đậu phụ rong biển', 
    N'Món canh thanh đạm bổ dưỡng mát lành giải nhiệt cơ thể.', 
    N'["Bước 1: Rửa sạch nấm rơm và cắt miếng vừa ăn, cắt đậu hũ non thành khối vuông.","Bước 2: Nấu sôi 800ml nước, cho nấm rơm vào đun trong 5 phút.","Bước 3: Cho đậu hũ non và rong biển vào nấu thêm 3 phút, nêm nếm vừa ăn rồi tắt bếp."]', 
    10, 25, 2, 310.0, 'EASY', 1, 
    'https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=800&q=80', 
    'APPROVED_ACTIVE', 150, 'USER', DATEADD(DAY, -5, SYSDATETIME())
);

-- Công thức 102: APPROVED_ACTIVE, BUDDHIST (1), Món Kho (9), Nấu 35p, Nấm hương + Nấm đùi gà
INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Category_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At)
VALUES (
    102, 1, 9, 1, 
    N'Nấm kho tiêu hạt sen đậm đà', 
    N'Nấm kho sánh quyện đậm đà đưa cơm chuẩn vị chay Phật giáo.', 
    N'["Bước 1: Sơ chế nấm hương và nấm đùi gà, ngâm nước muối loãng 10 phút.","Bước 2: Ướp nấm với nước tương, tiêu hạt và đường thốt nốt trong 15 phút.","Bước 3: Kho trên lửa nhỏ trong 20 phút cho nước sốt sánh lại và thấm đều."]', 
    15, 35, 4, 240.0, 'MEDIUM', 1, 
    'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80', 
    'APPROVED_ACTIVE', 280, 'USER', DATEADD(DAY, -4, SYSDATETIME())
);

-- Công thức 103: APPROVED_ACTIVE, VEGAN (7), Món Xào (14), Nấu 15p, Nấm kim châm + Đậu hũ chiên (Có Video READY)
INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Category_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At)
VALUES (
    103, 2, 14, 7, 
    N'Mì xào nấm rau củ giòn ngọt', 
    N'Món mì xào giòn thơm kèm video hướng dẫn từng bước.', 
    N'["Bước 1: Chần sơ mì qua nước sôi rồi để ráo.","Bước 2: Xào chín nấm kim châm và đậu hũ chiên.","Bước 3: Cho mì vào đảo đều lửa lớn trong 3 phút, rưới nước sốt xào."]', 
    10, 15, 2, 350.0, 'EASY', 0, 
    'https://images.unsplash.com/photo-1569718212165-3a8278d5f624?auto=format&fit=crop&w=800&q=80', 
    'APPROVED_ACTIVE', 95, 'USER', DATEADD(DAY, -3, SYSDATETIME())
);

-- Công thức 104: APPROVED_ACTIVE, LACTO (2), Món Canh (3), Nấu 20p, Bắp ngọt + Bơ lạt
INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Category_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At)
VALUES (
    104, 2, 3, 2, 
    N'Súp ngô ngọt kem tươi dinh dưỡng', 
    N'Súp thơm mùi bơ sữa và ngọt thanh tự nhiên từ bắp ngọt.', 
    N'["Bước 1: Tách hạt ngô ngọt và rửa sạch.","Bước 2: Xào thơm ngô với bơ lạt, xay nhuyễn 1/2 lượng ngô.","Bước 3: Nấu sôi hỗn hợp súp và khuấy đều tay đến khi sánh mịn."]', 
    10, 20, 3, 280.0, 'EASY', 1, 
    'https://images.unsplash.com/photo-1547592166-23ac45744acd?auto=format&fit=crop&w=800&q=80', 
    'APPROVED_ACTIVE', 120, 'USER', DATEADD(DAY, -2, SYSDATETIME())
);

-- Công thức 105: DRAFT (Bản nháp - KHÔNG HIỂN THỊ CÔNG KHAI)
INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Category_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At)
VALUES (
    105, 2, 3, 7, 
    N'Canh bí đỏ nháp đang soạn', 
    N'Bản nháp chưa gửi duyệt.', 
    N'["Bước 1: Gọt vỏ bí đỏ."]', 
    5, 20, 2, 190.0, 'EASY', 0, 
    NULL, 'DRAFT', 0, 'USER', DATEADD(DAY, -1, SYSDATETIME())
);

-- Công thức 106: QUARANTINED_HIDDEN (Cách ly - KHÔNG HIỂN THỊ CÔNG KHAI)
INSERT INTO dbo.Recipes (Recipe_ID, User_ID, Category_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At)
VALUES (
    106, 2, 9, 7, 
    N'Công thức vi phạm cách ly', 
    N'Bị cách ly kiểm duyệt.', 
    N'["Cách ly"]', 
    5, 30, 2, 200.0, 'HARD', 0, 
    NULL, 'QUARANTINED_HIDDEN', 0, 'USER', SYSDATETIME()
);

SET IDENTITY_INSERT dbo.Recipes OFF;

-- 4. Nạp RecipeIngredients
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional)
VALUES
    (101, 39, 200.0, N'g', N'Cắt khối vuông nhỏ', 0),    -- Đậu hũ non (Silken Tofu)
    (101, 90, 150.0, N'g', N'Cắt đôi', 0),              -- Nấm rơm tươi
    (102, 84, 250.0, N'g', N'Ngâm nở, khía hoa', 0),    -- Nấm hương tươi
    (102, 82, 150.0, N'g', N'Cắt khoanh tròn', 0),      -- Nấm đùi gà
    (103, 85, 100.0, N'g', N'Bỏ gốc, tách sợi', 0),     -- Nấm kim châm tươi
    (103, 38, 100.0, N'g', N'Cắt sợi vừa ăn', 0),       -- Đậu hũ chiên vàng
    (104, 5, 200.0, N'g', N'Tách hạt', 0),              -- Bắp ngọt tươi
    (104, 10, 30.0, N'g', N'Để mềm nhiệt độ phòng', 0); -- Bơ lạt từ sữa bò

-- 5. Nạp Posts đại diện
INSERT INTO dbo.Posts (User_ID, Recipe_ID, Title, Content, Visibility, View_Count, Moderation_Status, Created_At)
VALUES
    (1, 101, N'Canh nấm đậu phụ rong biển', N'Chia sẻ công thức canh nấm ngon tuyệt.', 'PUBLIC', 150, 'APPROVED_ACTIVE', DATEADD(DAY, -5, SYSDATETIME())),
    (1, 102, N'Nấm kho tiêu hạt sen đậm đà', N'Món kho đậm đà chuẩn vị chay.', 'PUBLIC', 280, 'APPROVED_ACTIVE', DATEADD(DAY, -4, SYSDATETIME())),
    (2, 103, N'Mì xào nấm rau củ giòn ngọt', N'Mì xào rau củ dễ làm tại nhà kèm video.', 'PUBLIC', 95, 'APPROVED_ACTIVE', DATEADD(DAY, -3, SYSDATETIME())),
    (2, 104, N'Súp ngô ngọt kem tươi dinh dưỡng', N'Súp ngọt dịu trẻ nhỏ đều thích.', 'PUBLIC', 120, 'APPROVED_ACTIVE', DATEADD(DAY, -2, SYSDATETIME()));

DECLARE @Post101 INT = (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID = 101);
DECLARE @Post102 INT = (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID = 102);
DECLARE @Post103 INT = (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID = 103);
DECLARE @Post104 INT = (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID = 104);

-- 6. Nạp Video & VideoTranscript cho công thức 103
INSERT INTO dbo.Videos (Post_ID, Title, Video_Url, Thumbnail_Url, Duration_Seconds, View_Count, Status, Created_At)
VALUES (@Post103, N'Video làm Mì xào nấm rau củ', 'https://res.cloudinary.com/demo/video/upload/dog.mp4', 'https://images.unsplash.com/photo-1569718212165-3a8278d5f624?auto=format&fit=crop&w=800&q=80', 120, 95, 'READY', DATEADD(DAY, -3, SYSDATETIME()));

DECLARE @Video103 INT = SCOPE_IDENTITY();

INSERT INTO dbo.VideoTranscripts (Video_ID, Recipe_ID, Raw_Transcript, Status, Created_At, Completed_At)
VALUES (@Video103, 103, N'Hôm nay chúng ta cùng thực hiện món mì xào nấm...', 'COMPLETED', DATEADD(DAY, -3, SYSDATETIME()), DATEADD(DAY, -3, SYSDATETIME()));

-- 7. Nạp Votes (Post 102 có nhiều vote nhất -> 42 votes, Post 101 có 25 votes, ...)
INSERT INTO dbo.Votes (User_ID, Post_ID, Created_At)
VALUES 
    (1, @Post102, SYSDATETIME()),
    (2, @Post102, SYSDATETIME()),
    (1, @Post101, SYSDATETIME());

-- 8. Nạp Comments
INSERT INTO dbo.Comments (User_ID, Post_ID, Content, Is_Soft_Deleted, Status, Created_At)
VALUES
    (1, @Post101, N'Canh nấu rất thanh mát!', 0, 'ACTIVE', SYSDATETIME()),
    (2, @Post101, N'Cảm ơn bạn đã chia sẻ công thức.', 0, 'ACTIVE', SYSDATETIME()),
    (1, @Post102, N'Món kho ăn với cơm nóng rất ngon.', 0, 'ACTIVE', SYSDATETIME());

PRINT N'Nạp dữ liệu mẫu công thức thành công!';
GO
