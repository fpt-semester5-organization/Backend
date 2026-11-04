-- ==============================================================================
-- Script nạp dữ liệu bổ sung cho Công thức chay Đại Bổ Giàu Năng Lượng (640 - 720 kcal)
-- Giúp kho món ăn có đủ biên độ calo và độ phong phú cho người dùng có nhu cầu calo cao (> 2100 kcal)
-- 100% Chay Phật giáo (Dietary_School_ID = 1) & Allium-Free (Không hành, tỏi, kiệu, hẹ, boa-rô)
-- ==============================================================================

USE [VeggieLifeDb];
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 1. Xóa dữ liệu cũ nếu đã tồn tại trước đó
DELETE FROM dbo.ViolationReports WHERE Comment_ID IN (SELECT Comment_ID FROM dbo.Comments WHERE Post_ID IN (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID BETWEEN 124 AND 128));
DELETE FROM dbo.ViolationReports WHERE Post_ID IN (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID BETWEEN 124 AND 128);
DELETE FROM dbo.ViolationReports WHERE Recipe_ID BETWEEN 124 AND 128;
DELETE FROM dbo.Votes WHERE Post_ID IN (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID BETWEEN 124 AND 128);
UPDATE dbo.Comments SET Parent_Comment_ID = NULL WHERE Parent_Comment_ID IN (SELECT Comment_ID FROM dbo.Comments WHERE Post_ID IN (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID BETWEEN 124 AND 128)) OR Post_ID IN (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID BETWEEN 124 AND 128);
DELETE FROM dbo.Comments WHERE Post_ID IN (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID BETWEEN 124 AND 128);
DELETE FROM dbo.VideoTranscripts WHERE Recipe_ID BETWEEN 124 AND 128;
DELETE FROM dbo.Videos WHERE Post_ID IN (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID BETWEEN 124 AND 128);
DELETE FROM dbo.Posts WHERE Recipe_ID BETWEEN 124 AND 128;
DELETE FROM dbo.RecipeIngredients WHERE Recipe_ID BETWEEN 124 AND 128;

SET IDENTITY_INSERT dbo.Recipes ON;

-- 2. Nạp hoặc cập nhật 5 công thức calo cao đặc biệt (ID 124 - 128)
MERGE INTO dbo.Recipes AS target
USING (
    VALUES
    (
        124, 1, 5, 1,
        N'Cơm chiên sa tế ngũ hạt nước cốt dừa',
        N'Cơm chiên hạt giòn tơi thấm đẫm sa tế sả thơm lừng kết hợp cốt dừa béo bùi, hạt sen, hạt điều, đậu phộng và tàu hũ ky chiên giòn rụm.',
        N'["Bước 1: Nấu cơm gạo thơm dẻo, để nguội và xới tơi từng hạt.","Bước 2: Chiên giòn tàu hũ ky và đậu hũ cắt hạt lựu.","Bước 3: Phi thơm sả băm với ớt và dầu màu điều tạo sốt sa tế thơm lừng, cho cơm vào đảo săn.","Bước 4: Rưới nước cốt dừa, trút hạt sen luộc, hạt điều và đậu phộng vào đảo lửa lớn cho hạt cơm vàng ruộm giòn tan."]',
        15, 20, 1, 720.0, 'MEDIUM',
        1, 'https://images.unsplash.com/photo-1603133872878-684f208fb84b?auto=format&fit=crop&w=800&q=80',
        'APPROVED_ACTIVE', 310, 'SYSTEM', SYSDATETIME()
    ),
    (
        125, 1, 11, 1,
        N'Bún gạo lứt xào sốt tương đậu phộng béo bùi',
        N'Sợi bún gạo lứt dai ngon xào cùng đạm thực vật từ chả lụa chay, nấm đùi gà áp chảo sốt tương đậu phộng béo ngậy đậm đà.',
        N'["Bước 1: Trụng bún gạo lứt vừa chín tới, xả nước lạnh để sợi bún tơi giòn.","Bước 2: Xay nhuyễn đậu phộng rang cùng nước tương, đường thốt nốt và chút dầu mè tạo sốt sánh quyện.","Bước 3: Xào săn nấm đùi gà và chả lụa chay thái sợi.","Bước 4: Cho bún và nước sốt tương đậu phộng vào đảo đều tay trên lửa vừa trong 3 phút rồi rắc mè rang thơm."]',
        15, 15, 1, 690.0, 'EASY',
        1, 'https://images.unsplash.com/photo-1569718212165-3a8278d5f624?auto=format&fit=crop&w=800&q=80',
        'APPROVED_ACTIVE', 280, 'SYSTEM', SYSDATETIME()
    ),
    (
        126, 1, 5, 1,
        N'Cơm thố nấm kho nước dừa hạt điều',
        N'Cơm thố cháy giòn đáy niêu ăn kèm nấm đông cô kho rim nước dừa tươi sánh đậm, hạt điều bùi béo và tiêu sọ cay nồng ấm bụng.',
        N'["Bước 1: Nấu cơm trong thố đất để tạo lớp cháy vàng giòn rụm đáy thố.","Bước 2: Nấm đông cô và nấm đùi gà ướp nước tương, đường thốt nốt và tiêu xay 15 phút.","Bước 3: Kho nấm với nước dừa tươi trên lửa liu riu cho nước sốt keo sánh lại, thả hạt điều vào rim cùng.","Bước 4: Dọn cơm thố nóng hổi rưới nước sốt nấm kho đậm đà thưởng thức ngay."]',
        20, 25, 1, 710.0, 'MEDIUM',
        1, 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80',
        'APPROVED_ACTIVE', 350, 'SYSTEM', SYSDATETIME()
    ),
    (
        127, 1, 11, 1,
        N'Mì tiềm ngũ quả hạt sen thảo mộc bồi bổ',
        N'Tô mì tiềm chay thanh mát nước dùng thảo mộc táo đỏ kỷ tử, hạt sen hầm nhừ bùi ngậy, nấm hương ngọt nước và mì căn chiên giòn.',
        N'["Bước 1: Hầm nước dùng tiềm từ mía lau, bắp ngọt, củ cải trắng và kỷ tử trong 45 phút.","Bước 2: Hạt sen tươi luộc chín mềm; nấm hương khía hoa xào săn.","Bước 3: Mì căn chiên vàng giòn cắt miếng vừa ăn.","Bước 4: Trụng mì sợi vàng, xếp nấm, hạt sen, mì căn rồi chan nước dùng tiềm nóng hổi thơm nức."]',
        20, 30, 1, 660.0, 'MEDIUM',
        1, 'https://images.unsplash.com/photo-1569718212165-3a8278d5f624?auto=format&fit=crop&w=800&q=80',
        'APPROVED_ACTIVE', 290, 'SYSTEM', SYSDATETIME()
    ),
    (
        128, 1, 3, 1,
        N'Cháo yến mạch nấm đông cô hạt óc chó béo ngậy',
        N'Cháo yến mạch dẻo sánh thơm lừng kết hợp hạt óc chó giàu Omega-3 béo ngậy, nấm đông cô ngọt đậm đà và hạt sen dưỡng tâm an thần.',
        N'["Bước 1: Yến mạch cán dẹt ngâm nở 10 phút rồi nấu cùng nước dùng rau củ cho sánh mịn.","Bước 2: Nấm đông cô tươi xắt hạt lựu xào thơm với dầu mè và hạt nêm chay.","Bước 3: Hạt óc chó và hạt sen giã dập vừa ăn.","Bước 4: Cho nấm và các loại hạt vào nồi cháo yến mạch, khuấy đều trong 5 phút trên lửa nhỏ rồi múc ra bát."]',
        10, 15, 1, 640.0, 'EASY',
        1, 'https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=800&q=80',
        'APPROVED_ACTIVE', 195, 'SYSTEM', SYSDATETIME()
    )
) AS source (Recipe_ID, User_ID, Temp_Category_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At)
ON target.Recipe_ID = source.Recipe_ID
WHEN MATCHED THEN
    UPDATE SET 
        User_ID = source.User_ID,
        Dietary_School_ID = source.Dietary_School_ID,
        Title = source.Title,
        Description = source.Description,
        Instructions = source.Instructions,
        Prep_Time_Minutes = source.Prep_Time_Minutes,
        Cook_Time_Minutes = source.Cook_Time_Minutes,
        Servings = source.Servings,
        Calories_Kcal = source.Calories_Kcal,
        Difficulty_Level = source.Difficulty_Level,
        Is_Verified = source.Is_Verified,
        Image_Url = source.Image_Url,
        Status = source.Status,
        View_Count = source.View_Count,
        Source = source.Source
WHEN NOT MATCHED THEN
    INSERT (Recipe_ID, User_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At)
    VALUES (source.Recipe_ID, source.User_ID, source.Dietary_School_ID, source.Title, source.Description, source.Instructions, source.Prep_Time_Minutes, source.Cook_Time_Minutes, source.Servings, source.Calories_Kcal, source.Difficulty_Level, source.Is_Verified, source.Image_Url, source.Status, source.View_Count, source.Source, source.Created_At);

SET IDENTITY_INSERT dbo.Recipes OFF;

-- 3. Nạp RecipeIngredients chi tiết (100% Non-Allium, 100% Plant-based)
INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional)
VALUES
    -- Recipe 124: Cơm chiên sa tế ngũ hạt nước cốt dừa (720.0 kcal)
    (124, 52, 170.0, N'g', N'Cơm nguội nấu chín để tơi', 0),    -- Gạo thơm lài miên
    (124, 66, 60.0,  N'g', N'Luộc chín mềm', 0),               -- Hạt sen tươi
    (124, 59, 40.0,  N'g', N'Rang thơm giòn', 0),              -- Hạt điều rang mộc
    (124, 44, 30.0,  N'g', N'Rang mộc bóc vỏ', 0),             -- Đậu phộng hạt khô
    (124, 110, 50.0, N'g', N'Chiên phồng giòn tan', 0),        -- Tàu hũ ky lá tươi
    (124, 105, 20.0, N'g', N'Băm nhuyễn xào sa tế', 0),        -- Sả cây tươi
    (124, 32, 25.0,  N'ml', N'Dầu xào cơm chiên', 0),          -- Dầu đậu nành nguyên chất

    -- Recipe 125: Bún gạo lứt xào sốt tương đậu phộng (690.0 kcal)
    (125, 16, 130.0, N'g', N'Bún gạo lứt trụng chín', 0),      -- Bún gạo khô
    (125, 44, 50.0,  N'g', N'Xay mịn thành bơ đậu phộng', 0),  -- Đậu phộng hạt khô
    (125, 25, 70.0,  N'g', N'Thái sợi xào cùng bún', 0),       -- Chả lụa chay
    (125, 82, 80.0,  N'g', N'Cắt sợi áp chảo', 0),             -- Nấm đùi gà
    (125, 62, 15.0,  N'g', N'Rang thơm rắc lên đĩa bún', 0),   -- Hạt mè trắng
    (125, 42, 15.0,  N'ml', N'Dầu mè thơm', 0),                -- Dầu mè thơm

    -- Recipe 126: Cơm thố nấm kho nước dừa hạt điều (710.0 kcal)
    (126, 52, 170.0, N'g', N'Cơm nấu niêu cháy giòn đáy', 0),  -- Gạo thơm lài miên
    (126, 84, 100.0, N'g', N'Khía hoa kho rim nước dừa', 0),   -- Nấm hương tươi
    (126, 59, 50.0,  N'g', N'Rim cùng sốt nấm kho', 0),        -- Hạt điều rang mộc
    (126, 38, 80.0,  N'g', N'Cắt vuông chiên vàng', 0),        -- Đậu hũ chiên vàng
    (126, 32, 20.0,  N'ml', N'Dầu xào rim', 0),                -- Dầu đậu nành nguyên chất

    -- Recipe 127: Mì tiềm ngũ quả hạt sen thảo mộc (660.0 kcal)
    (127, 16, 120.0, N'g', N'Mì sợi vàng trụng chín tới', 0),  -- Bún gạo khô
    (127, 66, 70.0,  N'g', N'Hầm nhừ bùi béo', 0),             -- Hạt sen tươi
    (127, 77, 60.0,  N'g', N'Chiên phồng giòn rụm', 0),        -- Mì căn chiên giòn
    (127, 84, 80.0,  N'g', N'Hầm lấy nước ngọt thảo mộc', 0),  -- Nấm hương tươi
    (127, 28, 60.0,  N'g', N'Hầm lấy vị ngọt thanh', 0),       -- Củ cải trắng tươi
    (127, 42, 10.0,  N'ml', N'Dầu mè rưới tô mì tiềm', 0),     -- Dầu mè thơm

    -- Recipe 128: Cháo yến mạch nấm đông cô hạt óc chó (640.0 kcal)
    (128, 119, 100.0, N'g', N'Nấu sánh dẻo mịn', 0),           -- Yến mạch cán dẹt Úc
    (128, 64, 40.0,   N'g', N'Giã dập bùi béo', 0),            -- Hạt óc chó nhân
    (128, 66, 50.0,   N'g', N'Hấp chín mềm', 0),               -- Hạt sen tươi
    (128, 84, 80.0,   N'g', N'Xắt hạt lựu xào thơm', 0),       -- Nấm hương tươi
    (128, 42, 15.0,   N'ml', N'Dầu mè phi nấm', 0);            -- Dầu mè thơm

-- 4. Nạp Posts tương ứng
INSERT INTO dbo.Posts (User_ID, Recipe_ID, Title, Content, Visibility, View_Count, Moderation_Status, Created_At)
VALUES
    (1, 124, N'Cơm chiên sa tế ngũ hạt nước cốt dừa', N'Món cơm chiên năng lượng bùng nổ hương vị sa tế và nước cốt dừa thơm ngậy.', 'PUBLIC', 310, 'APPROVED_ACTIVE', SYSDATETIME()),
    (1, 125, N'Bún gạo lứt xào sốt tương đậu phộng béo bùi', N'Sợi bún dai ngon kết hợp sốt đậu phộng thơm béo đậm đà dưỡng sinh.', 'PUBLIC', 280, 'APPROVED_ACTIVE', SYSDATETIME()),
    (1, 126, N'Cơm thố nấm kho nước dừa hạt điều', N'Cơm thố nóng giòn quyện nấm kho nước dừa ngọt đậm đà đưa cơm tuyệt hảo.', 'PUBLIC', 350, 'APPROVED_ACTIVE', SYSDATETIME()),
    (1, 127, N'Mì tiềm ngũ quả hạt sen thảo mộc bồi bổ', N'Tô mì tiềm ngọt lịm từ rau củ và hạt sen dưỡng tâm giải tỏa căng thẳng.', 'PUBLIC', 290, 'APPROVED_ACTIVE', SYSDATETIME()),
    (1, 128, N'Cháo yến mạch nấm đông cô hạt óc chó béo ngậy', N'Bữa sáng ấm áp giàu chất béo lành mạnh Omega-3 và chất xơ dồi dào.', 'PUBLIC', 195, 'APPROVED_ACTIVE', SYSDATETIME());

PRINT N'Nạp 5 công thức chay đại bổ giàu năng lượng (124 - 128) thành công!';
GO
