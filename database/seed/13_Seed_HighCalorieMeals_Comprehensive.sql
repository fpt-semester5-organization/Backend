-- ==============================================================================
-- Script nạp dữ liệu: 15 Công Thức Chay Giàu Năng Lượng (490 - 720 kcal)
-- Phục vụ thuật toán xếp thực đơn tuần FN-MEAL-01 và Fallback Engine
-- 100% Chay Phật giáo (Dietary_School_ID = 1), Không hành tỏi, Đủ calo
-- ==============================================================================

USE [VeggieLifeDb];
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

BEGIN TRANSACTION;
BEGIN TRY

    -- 1. Bật tính năng chèn giá trị Identity thủ công
    SET IDENTITY_INSERT dbo.Recipes ON;

    -- Xóa các bản ghi từ 206 đến 220 nếu đã từng tồn tại
    DELETE FROM dbo.RecipeIngredients WHERE Recipe_ID BETWEEN 206 AND 220;
    DELETE FROM dbo.Recipes WHERE Recipe_ID BETWEEN 206 AND 220;

    -- 2. Chèn 15 Công thức vào bảng Recipes
    INSERT INTO dbo.Recipes (
        Recipe_ID, User_ID, Category_ID, Dietary_School_ID, Title, Description, Instructions,
        Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level,
        Is_Verified, Image_Url, Status, View_Count, Source, Created_At
    ) VALUES
    (206, 1, 5, 1, N'Xôi nếp lá dứa hạt sen bọc cốt dừa',
     N'Hạt xôi nếp dẻo thơm ngấm hương lá dứa thanh mát, kết hợp hạt sen tươi bùi béo và nước cốt dừa đậm đà rắc đậu phộng rang giòn.',
     N'["Bước 1: Gạo nếp vo sạch, ngâm nước cốt lá dứa trong 4 tiếng cho ngấm màu xanh tự nhiên.","Bước 2: Hạt sen tươi rửa sạch, hấp sơ 10 phút cho vừa chín tới nhưng không bị nát.","Bước 3: Trộn gạo nếp với hạt sen, một nhúm muối nhỏ và đồ chín bằng xửng hấp trong 25 phút.","Bước 4: Rưới nước cốt dừa pha chút dầu mè lên bề mặt xôi, xới đều đậy nắp 5 phút rồi bới ra đĩa, rắc mè trắng thưởng thức."]',
     20, 30, 1, 580.0, 'MEDIUM', 1, 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 250, 'SYSTEM', SYSDATETIME()),

    (207, 1, 1, 1, N'Bánh mì ngũ cốc bơ hạt điều chả lụa chay',
     N'Bánh mì nguyên cám giòn rụm phết sốt bơ hạt điều béo ngậy, kẹp cùng chả lụa chay đậu xanh đậm vị và dưa leo cà chua tươi giòn.',
     N'["Bước 1: Nướng giòn bánh mì ngũ cốc trong lò nướng ở 180 độ C trong 3 phút.","Bước 2: Chả lụa chay cắt lát áp chảo sơ với vài giọt dầu đậu nành cho vàng thơm.","Bước 3: Phết đều bơ hạt điều mịn béo ngậy vào hai mặt ruột bánh.","Bước 4: Xếp chả lụa, dưa leo, cà chua và rưới vài giọt nước tương thưởng thức."]',
     10, 10, 1, 520.0, 'EASY', 1, 'https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 190, 'SYSTEM', SYSDATETIME()),

    (208, 1, 11, 1, N'Hủ tiếu xào mềm đạm chay sốt dầu hào mè',
     N'Sợi hủ tiếu mềm mượt xào săn cùng đạm đậu nành TVP, nấm đông cô đậm đà và cải thìa xanh giòn trong làn sốt dầu hào chay sánh béo.',
     N'["Bước 1: Trụng hủ tiếu mềm rồi xả nước lạnh trộn dầu mè chống dính.","Bước 2: Ngâm mềm đạm đậu nành, vắt ráo rồi xào săn cùng nấm đông cô xắt lát.","Bước 3: Cho cải thìa và hủ tiếu vào đảo đều trên lửa lớn cùng dầu hào chay.","Bước 4: Tắt bếp, rắc tiêu sọ và mè rang thưởng thức nóng."]',
     15, 15, 1, 540.0, 'MEDIUM', 1, 'https://images.unsplash.com/photo-1552611052-33e04de081de?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 210, 'SYSTEM', SYSDATETIME()),

    (209, 1, 3, 1, N'Cháo yến mạch đậu đỏ tiềm hạt dẻ cốt dừa',
     N'Cháo sánh mịn kết hợp yến mạch giàu xơ, đậu đỏ bùi dẻo ninh nhừ và hạt dẻ nướng thơm ngậy nước cốt dừa bổ dưỡng.',
     N'["Bước 1: Đậu đỏ ngâm nước ấm ninh mềm nhừ trong nồi áp suất.","Bước 2: Cho yến mạch và hạt dẻ vào nấu sôi liu riu trong 10 phút.","Bước 3: Rưới nước cốt dừa nguyên chất vào khuấy đều tay cho cháo sánh mịn.","Bước 4: Múc ra bát, rắc mè đen rang thưởng thức nóng ấm bụng."]',
     15, 25, 1, 510.0, 'EASY', 1, 'https://images.unsplash.com/photo-1505253716362-afaea1d3d1af?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 160, 'SYSTEM', SYSDATETIME()),

    (210, 1, 4, 1, N'Bánh xếp nấm rơm chiên giòn sốt mè rang',
     N'Vỏ bánh bột mì chiên vàng rụm ôm trọn nhân nấm rơm xắt nhỏ, củ sen giòn và đậu hũ tán nhuyễn chấm sốt mè thơm lừng.',
     N'["Bước 1: Trộn nấm rơm, củ sen và đậu hũ tán nhuyễn nêm gia vị tiêu dầu mè làm nhân.","Bước 2: Cho nhân vào giữa vỏ bánh bột mì gói mép chặt tạo hình bán nguyệt.","Bước 3: Chiên ngập dầu đậu nành đến khi vỏ bánh vàng rụm giòn tan vớt ra ráo dầu.","Bước 4: Bày ra đĩa chấm kèm sốt tương mè rang béo ngậy."]',
     20, 15, 1, 490.0, 'MEDIUM', 1, 'https://images.unsplash.com/photo-1498654896293-37aacf113fd9?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 180, 'SYSTEM', SYSDATETIME()),

    (211, 1, 5, 1, N'Cơm chiên sa tế ngũ hạt nước cốt dừa',
     N'Hạt cơm chiên giòn tơi ngấm vị sa tế cay dịu, xào cùng hạt điều, hạt sen, bắp ngọt, đậu hà lan và đậu hũ béo ngậy nước cốt dừa.',
     N'["Bước 1: Cơm nấu khô ráo để nguội, bóp đều với chút dầu mè cho hạt cơm tơi xốp.","Bước 2: Hạt sen luộc chín; đậu hũ cắt hạt lựu chiên giòn; hạt điều giã dập.","Bước 3: Đảo cơm trên chảo nóng cùng sa tế chay trong 5 phút.","Bước 4: Trút hạt điều, hạt sen, bắp ngọt và đậu hũ vào đảo đều nêm nước tương thưởng thức."]',
     15, 20, 1, 720.0, 'MEDIUM', 1, 'https://images.unsplash.com/photo-1603133872878-684f208fb84b?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 320, 'SYSTEM', SYSDATETIME()),

    (212, 1, 5, 1, N'Cơm tấm sườn bì chả nấm đại bổ',
     N'Đĩa cơm tấm gạo lứt thơm lừng ăn kèm miếng sườn non chay nướng đậm đà, chả nấm hấp đậu xanh béo ngậy và bì bún tàu giòn giòn.',
     N'["Bước 1: Nấu chín cơm tấm gạo lứt dẻo thơm.","Bước 2: Đạm đậu nành ướp nước tương mật mía tiêu dầu mè rồi đem nướng vàng óng.","Bước 3: Trộn đậu hũ tán nhuyễn với nấm mèo và bún tàu đem hấp chín làm chả.","Bước 4: Xếp sườn nướng, chả nấm lên đĩa cơm tấm chan dầu mè thưởng thức."]',
     25, 25, 1, 680.0, 'HARD', 1, 'https://images.unsplash.com/photo-1569058242253-92a9c755a0ec?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 390, 'SYSTEM', SYSDATETIME()),

    (213, 1, 9, 1, N'Cà ri nấm đùi gà khoai môn béo dừa ăn kèm bánh mì',
     N'Nước sốt cà ri béo ngậy từ nước cốt dừa và khoai môn bùi rục, ôm trọn những tai nấm đùi gà dai ngọt và đậu hũ chiên phồng.',
     N'["Bước 1: Nấm đùi gà và khoai môn cắt khối vuông chiên vàng sơ mặt.","Bước 2: Xào bột cà ri với sả cây đập dập trên chảo dầu nóng cho dậy mùi thơm.","Bước 3: Trút khoai môn nấm và nước dừa vào hầm nhỏ lửa trong 15 phút.","Bước 4: Rưới nước cốt dừa đặc nêm muối đường vừa ăn thưởng thức cùng bánh mì nóng."]',
     20, 25, 1, 650.0, 'MEDIUM', 1, 'https://images.unsplash.com/photo-1546833999-b9f581a1996d?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 280, 'SYSTEM', SYSDATETIME()),

    (214, 1, 14, 1, N'Mì căn sốt tương ngọt hạt điều nướng',
     N'Mì căn tươi chiên vàng giòn quyện sốt tương đen ngọt dịu, xào cùng ớt chuông ngũ sắc và rải đầy hạt điều rang bùi thơm.',
     N'["Bước 1: Mì căn cắt khúc xoắn chiên phồng vàng giòn để ráo dầu.","Bước 2: Ớt chuông cắt vuông xào săn cùng tương ngọt và chút dầu mè.","Bước 3: Trút mì căn chiên vào đảo nhanh tay trong 3 phút cho áo đều sốt bóng đẹp.","Bước 4: Bày ra đĩa rắc ngập hạt điều rang giòn thưởng thức cùng cơm nóng."]',
     20, 20, 1, 640.0, 'MEDIUM', 1, 'https://images.unsplash.com/photo-1541832676-9b763b0239ab?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 240, 'SYSTEM', SYSDATETIME()),

    (215, 1, 10, 1, N'Lẩu nấm chao đỏ ngũ vị đậu hũ ky',
     N'Nước dùng chao đỏ béo thơm đặc trưng thảo mộc, nhúng cùng tàu hũ ky chiên giòn tan, nấm rơm, khoai lang và bún tươi no đủ năng lượng.',
     N'["Bước 1: Tán nhuyễn chao đỏ xào cùng sả băm cho dậy mùi thơm nức.","Bước 2: Đổ nước dừa tươi vào nấu sôi thả khoai lang và nấm rơm hầm ngọt nước.","Bước 3: Nhúng tàu hũ ky chiên giòn và cải xanh vào nồi nước lẩu sôi sùng sục.","Bước 4: Ăn kèm bún gạo tươi chấm sốt chao đỏ pha ớt sa tế cay béo."]',
     25, 30, 1, 670.0, 'MEDIUM', 1, 'https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 310, 'SYSTEM', SYSDATETIME()),

    (216, 1, 3, 1, N'Canh kiểm Nam Bộ thập cẩm nước cốt dừa',
     N'Món canh ngọt béo truyền thống xứ Nam Bộ nấu từ bí đỏ, khoai lang, bột khoai, nấm mèo và đậu phộng ninh mềm trong nước cốt dừa ngậy sánh.',
     N'["Bước 1: Đậu phộng luộc mềm; bí đỏ khoai lang gọt vỏ xắt khối vuông.","Bước 2: Nấu bí đỏ khoai lang và đậu phộng với nước dão dừa đến khi chín bùi.","Bước 3: Cho đậu hũ chiên nấm mèo và bột khoai vào nấu thêm 5 phút.","Bước 4: Rưới nước cốt dừa đặc nêm nhúm muối nhỏ khuấy đều rồi tắt bếp."]',
     20, 25, 1, 590.0, 'MEDIUM', 1, 'https://images.unsplash.com/photo-1547592166-23ac45744acd?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 270, 'SYSTEM', SYSDATETIME()),

    (217, 1, 9, 1, N'Đậu hũ kho tộ sốt tiêu đen hạt sen',
     N'Từng miếng đậu hũ chiên vàng ươm kho liu riu trong niêu đất với sốt tương đen cay nồng tiêu sọ, kết hợp hạt sen tươi bùi ngậy thấm vị.',
     N'["Bước 1: Đậu hũ chiên cắt vuông; hạt sen tươi hấp chín tới.","Bước 2: Xếp đậu hũ và hạt sen vào niêu đất nêm nước tương mật mía tiêu đen.","Bước 3: Đun sôi rồi hạ lửa nhỏ kho liu riu trong 15 phút cho nước sốt keo sánh lại.","Bước 4: Rưới một muỗng dầu mè thơm rắc tiêu sọ đập dập dùng kèm cơm nóng."]',
     15, 20, 1, 560.0, 'EASY', 1, 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 230, 'SYSTEM', SYSDATETIME()),

    (218, 1, 5, 1, N'Cơm niêu gạo lứt nấm bào ngư xào dầu mè',
     N'Cơm niêu cháy giòn đáy quyện mùi thơm nức của dầu mè thượng hạng, phủ đầy nấm bào ngư xào săn cùng mè đen rang bổ dưỡng.',
     N'["Bước 1: Nấu cơm gạo lứt trong niêu đất cho chín dẻo và tạo lớp cháy giòn ở đáy.","Bước 2: Nấm bào ngư xé sợi xào săn lửa lớn cùng dầu mè và gừng tươi.","Bước 3: Xới nấm xào lên bề mặt cơm niêu rắc ngập mè đen rang chín thơm.","Bước 4: Dọn ăn nóng kèm một chén nước tương tỏi ớt chay đậm đà."]',
     15, 25, 1, 610.0, 'MEDIUM', 1, 'https://images.unsplash.com/photo-1512058564366-18510be2db19?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 260, 'SYSTEM', SYSDATETIME()),

    (219, 1, 3, 1, N'Súp bí đỏ đậu gà cốt dừa bơ gừng',
     N'Súp bí đỏ xay nhuyễn mịn vàng óng sánh đặc, nấu cùng hạt đậu gà giàu đạm và nước cốt dừa thơm nồng gừng tươi ấm áp.',
     N'["Bước 1: Bí đỏ hấp chín mềm rồi đem xay mịn cùng chút nước dừa tươi.","Bước 2: Đậu gà ngâm qua đêm ninh chín nhừ mềm bùi trong nồi áp suất.","Bước 3: Đun sôi bí đỏ xay thả đậu gà vào nấu nhỏ lửa cùng nước cốt dừa béo.","Bước 4: Nêm chút muối gừng băm ấm nồng múc ra bát rắc hạt bí rang thưởng thức."]',
     15, 20, 1, 550.0, 'EASY', 1, 'https://images.unsplash.com/photo-1476718406336-bb5a9690ee2a?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 190, 'SYSTEM', SYSDATETIME()),

    (220, 1, 8, 1, N'Nấm đông cô nhồi đậu phụ hấp sốt hạt óc chó',
     N'Tai nấm đông cô dày cùi ôm trọn nhân đậu hũ mịn hấp thanh đạm, rưới sốt kem hạt óc chó xay béo ngậy đẳng cấp nhà hàng.',
     N'["Bước 1: Nấm đông cô ngâm nở bỏ chân; đậu hũ tán nhuyễn trộn tiêu muối.","Bước 2: Nhồi nhân đậu hũ chặt vào lòng tai nấm đem hấp chín trong 15 phút.","Bước 3: Hạt óc chó xay mịn cùng nước cốt dừa đun nhẹ tạo thành hỗn hợp sốt kem béo.","Bước 4: Bày nấm hấp ra đĩa rưới đẫm sốt óc chó rắc rau mùi thưởng thức nóng."]',
     20, 20, 1, 570.0, 'MEDIUM', 1, 'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80', 'APPROVED_ACTIVE', 220, 'SYSTEM', SYSDATETIME());

    SET IDENTITY_INSERT dbo.Recipes OFF;

    -- 3. Nạp nguyên liệu thành phần mẫu vào bảng RecipeIngredients
    -- Công thức 206: Xôi nếp lá dứa hạt sen (580 kcal)
    INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional)
    VALUES
    (206, 52, 100.0, N'g', N'Gạo nếp ngâm nước lá dứa', 0),
    (206, 66, 60.0, N'g', N'Hạt sen tươi hấp sơ', 0),
    (206, 42, 5.0, N'g', N'Dầu mè thơm', 0),
    (206, 62, 15.0, N'g', N'Mè trắng rang', 0);

    -- Công thức 211: Cơm chiên sa tế ngũ hạt (720 kcal)
    INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional)
    VALUES
    (211, 52, 120.0, N'g', N'Cơm nguội tơi hạt', 0),
    (211, 59, 20.0, N'g', N'Hạt điều rang mộc giã dập', 0),
    (211, 38, 40.0, N'g', N'Đậu hũ chiên hạt lựu', 0),
    (211, 66, 30.0, N'g', N'Hạt sen luộc chín', 0),
    (211, 42, 5.0, N'g', N'Dầu mè thơm', 0),
    (211, 5, 30.0, N'g', N'Bắp ngọt tươi tách hạt', 0);

    -- Công thức 212: Cơm tấm sườn bì chả nấm (680 kcal)
    INSERT INTO dbo.RecipeIngredients (Recipe_ID, Ingredient_ID, Quantity_Needed, Unit, Preparation_Note, Is_Optional)
    VALUES
    (212, 51, 110.0, N'g', N'Gạo lứt đỏ nấu dẻo', 0),
    (212, 38, 50.0, N'g', N'Đậu hũ chiên làm chả nấm', 0),
    (212, 9, 25.0, N'g', N'Đạm đậu nành TVP ướp nướng', 0),
    (212, 32, 6.0, N'g', N'Dầu đậu nành quét sườn', 0),
    (212, 88, 10.0, N'g', N'Nấm mèo xắt sợi nhỏ', 0);

    COMMIT TRANSACTION;
    PRINT N'[THÀNH CÔNG] Đã nạp hoàn tất 15 công thức chay giàu calo (ID 206 - 220) vào VeggieLifeDb.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'[LỖI HỆ THỐNG] Quá trình nạp dữ liệu thất bại: %s', 16, 1, @Err);
END CATCH;
GO
