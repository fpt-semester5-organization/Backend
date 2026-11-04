-- ==============================================================================
-- Script nạp dữ liệu mẫu cho Công thức chay giàu Calo (500 - 700 kcal)
-- Phục vụ thuật toán xếp thực đơn AI và Fallback Engine cho người dùng có nhu cầu calo cao (> 2000 kcal)
-- Đáp ứng hoàn toàn tiêu chuẩn Chay Phật giáo (Dietary_School_ID = 1) & Kiêng Ngũ vị tân (Allium-Free)
-- ==============================================================================

USE [VeggieLifeDb];
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 1. Xóa dữ liệu cũ của các công thức 117 - 123 nếu đã tồn tại trước đó
DELETE FROM dbo.ViolationReports WHERE Comment_ID IN (SELECT Comment_ID FROM dbo.Comments WHERE Post_ID IN (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID BETWEEN 117 AND 123));
DELETE FROM dbo.ViolationReports WHERE Post_ID IN (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID BETWEEN 117 AND 123);
DELETE FROM dbo.ViolationReports WHERE Recipe_ID BETWEEN 117 AND 123;
DELETE FROM dbo.Votes WHERE Post_ID IN (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID BETWEEN 117 AND 123);
UPDATE dbo.Comments SET Parent_Comment_ID = NULL WHERE Parent_Comment_ID IN (SELECT Comment_ID FROM dbo.Comments WHERE Post_ID IN (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID BETWEEN 117 AND 123)) OR Post_ID IN (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID BETWEEN 117 AND 123);
DELETE FROM dbo.Comments WHERE Post_ID IN (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID BETWEEN 117 AND 123);
DELETE FROM dbo.VideoTranscripts WHERE Recipe_ID BETWEEN 117 AND 123;
DELETE FROM dbo.Videos WHERE Post_ID IN (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID BETWEEN 117 AND 123);
DELETE FROM dbo.Posts WHERE Recipe_ID BETWEEN 117 AND 123;
DELETE FROM dbo.RecipeIngredients WHERE Recipe_ID BETWEEN 117 AND 123;

SET IDENTITY_INSERT dbo.Recipes ON;

-- 2. Nạp hoặc cập nhật 7 công thức calo cao (ID 117 - 123)
MERGE INTO dbo.Recipes AS target
USING (
    VALUES
    (
        117, 1, 5, 1,
        N'Cơm chiên ngũ sắc hạt sen dinh dưỡng',
        N'Cơm chiên hạt tơi giòn kết hợp hạt sen tươi bùi béo, bắp ngọt, đậu hà lan, cà rốt và đậu hũ chiên vàng ruộm, cung cấp năng lượng tràn đầy.',
        N'["Bước 1: Luộc chín hạt sen tươi và đậu hà lan, để ráo.","Bước 2: Đậu hũ cắt hạt lựu chiên giòn, cà rốt xắt hạt lựu.","Bước 3: Phi thơm dầu mè với bo-rô hoặc gừng, cho cơm nguội vào đảo săn hạt.","Bước 4: Trút toàn bộ hạt sen, đậu hà lan, bắp ngọt và đậu hũ vào đảo đều cùng nước tương, tiêu xay trong 5 phút."]',
        15, 20, 1, 580.0, 'MEDIUM',
        1, 'https://images.unsplash.com/photo-1603133872878-684f208fb84b?auto=format&fit=crop&w=800&q=80',
        'APPROVED_ACTIVE', 180, 'SYSTEM', SYSDATETIME()
    ),
    (
        118, 1, 5, 1,
        N'Xôi nấm hạt dẻ cốt dừa béo ngậy',
        N'Hạt xôi nếp dẻo thơm nấu nước cốt dừa đậm đà, kết hợp nấm hương xào thấm vị cùng hạt điều rang bùi ngậy giàu chất béo lành mạnh.',
        N'["Bước 1: Gạo nếp ngâm qua đêm, trộn chút muối rồi đồ chín cùng nước cốt dừa.","Bước 2: Nấm hương ngâm nở, xắt sợi rồi xào săn với nước tương và tiêu.","Bước 3: Hạt điều rang thơm đập dập.","Bước 4: Bới xôi ra đĩa, rải nấm xào lên trên và rắc hạt điều rang thưởng thức nóng."]',
        20, 30, 1, 620.0, 'MEDIUM',
        1, 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80',
        'APPROVED_ACTIVE', 210, 'SYSTEM', SYSDATETIME()
    ),
    (
        119, 1, 1, 1,
        N'Bánh mì ngũ cốc chả nấm bơ hạt điều',
        N'Bánh mì ngũ cốc nguyên cám giòn tan kẹp chả lụa chay, nấm đùi gà áp chảo sốt tương mật và sốt bơ hạt điều béo ngậy giàu protein.',
        N'["Bước 1: Nướng giòn bánh mì ngũ cốc nguyên cám.","Bước 2: Nấm đùi gà cắt lát áp chảo vàng đều hai mặt với sốt nước tương tiêu.","Bước 3: Phết đều bơ hạt điều vào lòng bánh mì.","Bước 4: Kẹp chả lụa chay, nấm áp chảo, dưa leo và ngò rí rồi thưởng thức."]',
        10, 15, 1, 520.0, 'EASY',
        1, 'https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=800&q=80',
        'APPROVED_ACTIVE', 145, 'SYSTEM', SYSDATETIME()
    ),
    (
        120, 1, 11, 1,
        N'Hủ tiếu Nam Vang chay đặc biệt',
        N'Tô hủ tiếu chay thanh ngọt từ nước dùng củ cải táo đỏ, đầy đặn topping chả lụa chay, mì căn chiên giòn, nấm rơm và tàu hũ ky chiên xù.',
        N'["Bước 1: Hầm nước dùng từ củ cải trắng, bắp ngọt và gừng tươi trong 30 phút.","Bước 2: Trụng hủ tiếu chín tới, xốc đều với một chút dầu mè.","Bước 3: Chiên giòn mì căn và tàu hũ ky, xắt lát chả lụa chay.","Bước 4: Xếp nguyên liệu lên tô hủ tiếu, chan nước dùng sôi sùng sục và rắc tiêu."]',
        20, 25, 1, 540.0, 'MEDIUM',
        1, 'https://images.unsplash.com/photo-1569718212165-3a8278d5f624?auto=format&fit=crop&w=800&q=80',
        'APPROVED_ACTIVE', 260, 'SYSTEM', SYSDATETIME()
    ),
    (
        121, 1, 9, 1,
        N'Cà ri nấm khoai lang cốt dừa kèm bánh mì',
        N'Món cà ri chay béo ngậy thơm nức mùi sả tươi và nước cốt dừa, khoai lang mật dẻo bùi cùng nấm đùi gà sần sật và đậu gà giàu đạm.',
        N'["Bước 1: Khoai lang gọt vỏ cắt khối chiên sơ qua dầu cho săn mặt.","Bước 2: Xào thơm sả đập dập với bột cà ri chay, cho nấm đùi gà và đậu gà vào xào săn.","Bước 3: Đổ nước cốt dừa loãng và khoai lang vào đun nhỏ lửa 15 phút cho chín mềm.","Bước 4: Thêm nước cốt dừa đặc, nêm nếm vừa ăn rồi tắt bếp, dùng kèm bánh mì nóng."]',
        15, 25, 1, 650.0, 'MEDIUM',
        1, 'https://images.unsplash.com/photo-1455619452474-d2be8b1e70cd?auto=format&fit=crop&w=800&q=80',
        'APPROVED_ACTIVE', 320, 'SYSTEM', SYSDATETIME()
    ),
    (
        122, 1, 5, 1,
        N'Cơm gạo lứt xào rong biển và hạt dinh dưỡng',
        N'Gạo lứt đỏ huyết rồng dẻo bùi xào cùng hạt sen, hạt điều, đậu phộng béo ngậy và mè rang, cung cấp nguồn năng lượng bền bỉ cho ngày dài.',
        N'["Bước 1: Nấu chín cơm gạo lứt huyết rồng, xới tơi và để nguội.","Bước 2: Luộc chín hạt sen tươi; đậu phộng và hạt điều rang thơm giòn.","Bước 3: Cho dầu mè vào chảo, rang cơm gạo lứt trên lửa lớn cho săn hạt.","Bước 4: Cho hạt sen, đậu phộng, hạt điều vào đảo đều, rắc mè trắng và tiêu đen thơm lừng."]',
        15, 20, 1, 680.0, 'EASY',
        1, 'https://images.unsplash.com/photo-1512058564366-18510be2db19?auto=format&fit=crop&w=800&q=80',
        'APPROVED_ACTIVE', 190, 'SYSTEM', SYSDATETIME()
    ),
    (
        123, 1, 4, 1,
        N'Bánh xèo chay giòn rụm nhân nấm đậu xanh',
        N'Vỏ bánh xèo giòn tan thơm béo nước cốt dừa và bột nghệ, nhân đầy ắp nấm rơm tươi, đậu xanh hấp bùi ngậy và củ sắn giòn ngọt nước.',
        N'["Bước 1: Khuấy bột bánh xèo với nước cốt dừa, bột nghệ và chút muối cho mịn.","Bước 2: Đậu xanh hấp chín tới; nấm rơm xắt lát; củ sắn thái sợi.","Bước 3: Làm nóng chảo với dầu đậu nành, xào sơ nấm và củ sắn rồi tráng một vá bột mỏng.","Bước 4: Rải đậu xanh lên trên, đậy nắp 3 phút cho vành bánh giòn rụm thì gập đôi lại."]',
        20, 20, 1, 560.0, 'MEDIUM',
        1, 'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80',
        'APPROVED_ACTIVE', 230, 'SYSTEM', SYSDATETIME()
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
    -- Recipe 117: Cơm chiên ngũ sắc hạt sen
    (117, 52, 150.0, N'g', N'Cơm nguội nấu chín để tơi', 0),    -- Gạo thơm lài miên
    (117, 66, 60.0,  N'g', N'Luộc chín mềm', 0),               -- Hạt sen tươi
    (117, 36, 50.0,  N'g', N'Chần sơ nước sôi', 0),            -- Đậu Hà Lan hạt tươi
    (117, 5,  50.0,  N'g', N'Tách hạt ngọt', 0),               -- Bắp ngọt tươi
    (117, 38, 80.0,  N'g', N'Cắt hạt lựu chiên vàng', 0),      -- Đậu hũ chiên vàng
    (117, 42, 15.0,  N'ml', N'Dầu mè thơm', 0),                -- Dầu mè thơm thượng hạng

    -- Recipe 118: Xôi nấm hạt dẻ cốt dừa
    (118, 52, 160.0, N'g', N'Gạo nếp đồ chín với nước cốt dừa', 0), -- Gạo thơm lài miên
    (118, 59, 40.0,  N'g', N'Rang mộc đập dập', 0),            -- Hạt điều rang mộc
    (118, 66, 50.0,  N'g', N'Nấu chín cùng xôi', 0),           -- Hạt sen tươi
    (118, 84, 80.0,  N'g', N'Xắt sợi xào thấm vị', 0),         -- Nấm hương tươi
    (118, 32, 20.0,  N'ml', N'Dầu xào nấm', 0),                -- Dầu đậu nành nguyên chất

    -- Recipe 119: Bánh mì ngũ cốc chả nấm bơ hạt điều
    (119, 13, 100.0, N'g', N'Bánh mì nướng giòn', 0),          -- Bột mì đa dụng
    (119, 25, 80.0,  N'g', N'Xắt lát kẹp bánh mì', 0),         -- Chả lụa chay
    (119, 82, 70.0,  N'g', N'Áp chảo sốt tương', 0),           -- Nấm đùi gà
    (119, 59, 30.0,  N'g', N'Xay mịn thành sốt bơ hạt điều', 0), -- Hạt điều rang mộc
    (119, 49, 50.0,  N'g', N'Xắt lát mỏng', 0),                -- Dưa leo tươi

    -- Recipe 120: Hủ tiếu Nam Vang chay đặc biệt
    (120, 16, 120.0, N'g', N'Trụng chín xốc dầu mè', 0),       -- Bún gạo khô
    (120, 25, 60.0,  N'g', N'Xắt lát mỏng vừa ăn', 0),         -- Chả lụa chay
    (120, 77, 50.0,  N'g', N'Chiên giòn rụm', 0),              -- Mì căn chiên giòn
    (120, 110, 40.0, N'g', N'Chiên phồng giòn tan', 0),        -- Tàu hũ ky lá tươi
    (120, 90, 80.0,  N'g', N'Nấu ngọt nước dùng', 0),          -- Nấm rơm tươi
    (120, 28, 60.0,  N'g', N'Hầm lấy vị ngọt thanh', 0),       -- Củ cải trắng tươi

    -- Recipe 121: Cà ri nấm khoai lang cốt dừa kèm bánh mì
    (121, 70, 150.0, N'g', N'Cắt khối chiên sơ mặt', 0),       -- Khoai lang mật
    (121, 82, 100.0, N'g', N'Cắt khoanh tròn xào săn', 0),     -- Nấm đùi gà
    (121, 35, 80.0,  N'g', N'Hầm mềm bùi béo', 0),             -- Đậu gà (Chickpeas)
    (121, 13, 80.0,  N'g', N'Bánh mì ăn kèm', 0),              -- Bột mì đa dụng
    (121, 105, 30.0, N'g', N'Đập dập phi thơm', 0),            -- Sả cây tươi
    (121, 32, 20.0,  N'ml', N'Dầu xào cà ri', 0),              -- Dầu đậu nành nguyên chất

    -- Recipe 122: Cơm gạo lứt xào rong biển và hạt dinh dưỡng
    (122, 51, 160.0, N'g', N'Nấu chín xới tơi để nguội', 0),   -- Gạo lứt đỏ huyết rồng
    (122, 59, 40.0,  N'g', N'Rang mộc thơm bùi', 0),           -- Hạt điều rang mộc
    (122, 66, 50.0,  N'g', N'Luộc chín mềm', 0),               -- Hạt sen tươi
    (122, 44, 30.0,  N'g', N'Rang giòn bóc vỏ', 0),            -- Đậu phộng hạt khô
    (122, 62, 15.0,  N'g', N'Rang thơm rắc mặt', 0),           -- Hạt mè trắng
    (122, 42, 15.0,  N'ml', N'Dầu mè rang hạt', 0),            -- Dầu mè thơm thượng hạng

    -- Recipe 123: Bánh xèo chay giòn rụm nhân nấm đậu xanh
    (123, 13, 100.0, N'g', N'Pha bột bánh xèo cốt dừa nghệ', 0), -- Bột mì đa dụng
    (123, 48, 60.0,  N'g', N'Hấp chín mềm bùi', 0),            -- Đậu xanh bóc vỏ
    (123, 90, 80.0,  N'g', N'Xắt lát xào nhân', 0),            -- Nấm rơm tươi
    (123, 29, 60.0,  N'g', N'Thái sợi tạo vị ngọt giòn', 0),   -- Củ sắn tươi
    (123, 32, 25.0,  N'ml', N'Dầu chiên giòn bánh xèo', 0);    -- Dầu đậu nành nguyên chất

-- 4. Nạp Posts tương ứng cho 7 công thức
INSERT INTO dbo.Posts (User_ID, Recipe_ID, Title, Content, Visibility, View_Count, Moderation_Status, Created_At)
VALUES
    (1, 117, N'Cơm chiên ngũ sắc hạt sen dinh dưỡng', N'Công thức cơm chiên ngũ sắc chay thơm lừng giàu năng lượng.', 'PUBLIC', 180, 'APPROVED_ACTIVE', SYSDATETIME()),
    (1, 118, N'Xôi nấm hạt dẻ cốt dừa béo ngậy', N'Món xôi nếp dẻo thơm nấu cốt dừa cho bữa sáng đủ chất.', 'PUBLIC', 210, 'APPROVED_ACTIVE', SYSDATETIME()),
    (1, 119, N'Bánh mì ngũ cốc chả nấm bơ hạt điều', N'Bánh mì giòn rụm giàu đạm thực vật từ chả nấm và bơ hạt điều.', 'PUBLIC', 145, 'APPROVED_ACTIVE', SYSDATETIME()),
    (1, 120, N'Hủ tiếu Nam Vang chay đặc biệt', N'Nước dùng thanh ngọt tự nhiên cùng topping nấm và chả lụa chay tuyệt đỉnh.', 'PUBLIC', 260, 'APPROVED_ACTIVE', SYSDATETIME()),
    (1, 121, N'Cà ri nấm khoai lang cốt dừa kèm bánh mì', N'Cà ri thơm nồng sả tươi, khoai bở dẻo và cốt dừa béo ngậy.', 'PUBLIC', 320, 'APPROVED_ACTIVE', SYSDATETIME()),
    (1, 122, N'Cơm gạo lứt xào rong biển và hạt dinh dưỡng', N'Bữa ăn dồi dào năng lượng bền vững cho người tập luyện thể thao.', 'PUBLIC', 190, 'APPROVED_ACTIVE', SYSDATETIME()),
    (1, 123, N'Bánh xèo chay giòn rụm nhân nấm đậu xanh', N'Vỏ bánh xèo vàng ươm giòn rụm nhân nấm đậu xanh chuẩn vị quê nhà.', 'PUBLIC', 230, 'APPROVED_ACTIVE', SYSDATETIME());

PRINT N'Nạp 7 công thức chay giàu Calo (117 - 123) thành công!';
GO
