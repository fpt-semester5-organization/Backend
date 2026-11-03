-- ==============================================================================
-- Script nạp dữ liệu: 12 Món Chay Truyền Thống Giàu Năng Lượng (560 - 700 kcal)
-- Giúp kho món ăn có đủ biên độ calo và độ phong phú cho AI và Fallback Engine
-- 100% Thuần chay / Chay Phật giáo (Dietary_School_ID = 1), Không hành tỏi động vật
-- ==============================================================================

USE [VeggieLifeDb];
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 1. Dọn dẹp dữ liệu cũ từ 129 đến 140 nếu đã chạy trước đó
DELETE FROM dbo.ViolationReports WHERE Comment_ID IN (SELECT Comment_ID FROM dbo.Comments WHERE Post_ID IN (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID BETWEEN 129 AND 140));
DELETE FROM dbo.ViolationReports WHERE Post_ID IN (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID BETWEEN 129 AND 140);
DELETE FROM dbo.ViolationReports WHERE Recipe_ID BETWEEN 129 AND 140;
DELETE FROM dbo.Votes WHERE Post_ID IN (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID BETWEEN 129 AND 140);
UPDATE dbo.Comments SET Parent_Comment_ID = NULL WHERE Parent_Comment_ID IN (SELECT Comment_ID FROM dbo.Comments WHERE Post_ID IN (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID BETWEEN 129 AND 140)) OR Post_ID IN (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID BETWEEN 129 AND 140);
DELETE FROM dbo.Comments WHERE Post_ID IN (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID BETWEEN 129 AND 140);
DELETE FROM dbo.VideoTranscripts WHERE Recipe_ID BETWEEN 129 AND 140;
DELETE FROM dbo.Videos WHERE Post_ID IN (SELECT Post_ID FROM dbo.Posts WHERE Recipe_ID BETWEEN 129 AND 140);
DELETE FROM dbo.Posts WHERE Recipe_ID BETWEEN 129 AND 140;
DELETE FROM dbo.RecipeIngredients WHERE Recipe_ID BETWEEN 129 AND 140;

SET IDENTITY_INSERT dbo.Recipes ON;

-- 2. Nạp hoặc cập nhật 12 công thức truyền thống giàu năng lượng (ID 129 - 140)
MERGE INTO dbo.Recipes AS target
USING (
    VALUES
    (
        129, 1, 5, 1,
        N'Cơm tấm sườn bì chả chay',
        N'Cơm tấm sườn non chay ướp sốt ngũ vị chiên vàng, bì miến trộn thính gạo thơm lừng cùng chả hấp đậu xanh mộc nhĩ béo bùi.',
        N'["Bước 1: Nấu gạo tấm nở xốp dẻo thơm.","Bước 2: Ướp sườn non chay với sả, ngũ vị hương, mật mía rồi nướng vàng giòn.","Bước 3: Trộn miến khô luộc dai cùng thính gạo lứt rang và dầu màu điều tạo sợi bì thơm lừng.","Bước 4: Hấp chả đậu xanh tán nhuyễn trộn nấm mèo và đậu hũ non.","Bước 5: Bày cơm tấm ra đĩa, xếp sườn, bì, chả và rưới mỡ hành boa rô béo ngậy."]',
        20, 25, 1, 680.0, 'MEDIUM',
        1, 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80',
        'APPROVED_ACTIVE', 450, 'SYSTEM', SYSDATETIME()
    ),
    (
        130, 1, 11, 1,
        N'Bún riêu cua chay đậu hũ chiên giòn',
        N'Nước dùng riêu nấu từ sữa đậu nành nguyên chất và nấm xào cà chua ngọt thanh, ăn kèm chả lụa chay và đậu hũ chiên phồng ngập sốt.',
        N'["Bước 1: Đun sữa đậu nành tươi cùng chút giấm gạo tạo váng riêu cua béo mịn.","Bước 2: Xào cà chua chín đỏ với dầu màu điều rồi trút vào nồi nước dùng hầm củ quả ngọt lịm.","Bước 3: Chiên vàng đậu hũ non và chả cây chay.","Bước 4: Trụng bún tươi ra tô, múc váng riêu, đậu hũ chiên, chả lụa và chan nước dùng chua dịu thơm nồng."]',
        15, 25, 1, 590.0, 'MEDIUM',
        1, 'https://images.unsplash.com/photo-1569718212165-3a8278d5f624?auto=format&fit=crop&w=800&q=80',
        'APPROVED_ACTIVE', 380, 'SYSTEM', SYSDATETIME()
    ),
    (
        131, 1, 1, 1,
        N'Bánh mì chảo chay bơ hạt điều xíu mại nấm',
        N'Chảo bánh mì nóng hổi gồm xíu mại nấm đông cô viên mềm mọng, bơ hạt điều béo ngậy, đậu hũ áp chảo sốt cà chua sánh quyện ăn kèm bánh mì giòn.',
        N'["Bước 1: Vo viên xíu mại nấm đông cô, đậu hũ bóp nhuyễn và mộc nhĩ rồi đem hấp chín.","Bước 2: Nấu sốt cà chua đậm đà cùng hạt bơ điều xay nhuyễn.","Bước 3: Làm nóng chảo gang, cho xíu mại, đậu hũ chiên và chan ngập nước sốt bơ sánh đỏ.","Bước 4: Ăn kèm bánh mì baguette nướng giòn rụm và dưa leo ngâm chua."]',
        20, 20, 1, 620.0, 'EASY',
        1, 'https://images.unsplash.com/photo-1509722747041-616f39b57569?auto=format&fit=crop&w=800&q=80',
        'APPROVED_ACTIVE', 410, 'SYSTEM', SYSDATETIME()
    ),
    (
        132, 1, 5, 1,
        N'Xôi xéo mỡ hành đậu xanh mè rang',
        N'Hạt xôi nếp nương vàng ươm sắc nghệ dẻo quánh, phủ lớp đậu xanh nghiền mịn mượt, rưới dầu màu điều béo ngậy và mè rang giòn rụm.',
        N'["Bước 1: Ngâm nếp thơm với bột nghệ rồi đồ chín mềm dẻo.","Bước 2: Hấp chín đậu xanh cà vỏ, tán nhuyễn mịn rồi nén chặt thành khối tròn.","Bước 3: Đun dầu mè và dầu màu điều cho thơm ngậy.","Bước 4: Xới xôi ra đĩa lá chuối, dùng dao gọt từng lát mỏng đậu xanh phủ kín mặt xôi, rưới mỡ hành và rắc mè rang giòn."]',
        15, 30, 1, 580.0, 'EASY',
        1, 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80',
        'APPROVED_ACTIVE', 320, 'SYSTEM', SYSDATETIME()
    ),
    (
        133, 1, 11, 1,
        N'Bún chả giò chay chạo nấm mía ngũ vị',
        N'Chả giò khoai môn nấm mèo chiên vàng rụm, chạo nấm đậu xanh quấn thân mía nướng thơm ngát ăn cùng bún tươi, rau sống và nước mắm chua ngọt.',
        N'["Bước 1: Cuốn bánh tráng với nhân khoai môn, đậu xanh và nấm rồi chiên giòn rụm.","Bước 2: Quết chạo nấm đùi gà với bột bắp và ngũ vị hương quanh khúc mía rồi nướng vàng mặt.","Bước 3: Pha nước chấm chay từ nước cốt dừa, nước tương, chanh và ớt hiểm.","Bước 4: Cho bún vào tô, xếp chả giò cắt xéo, chạo mía nướng, rau thơm và đậu phộng rang."]',
        20, 20, 1, 610.0, 'MEDIUM',
        1, 'https://images.unsplash.com/photo-1569718212165-3a8278d5f624?auto=format&fit=crop&w=800&q=80',
        'APPROVED_ACTIVE', 390, 'SYSTEM', SYSDATETIME()
    ),
    (
        134, 1, 5, 1,
        N'Cơm niêu nấm kho tiêu xanh chả lụa chay',
        N'Cơm nấu trong niêu đất tạo lớp cháy vàng giòn rụm đáy niêu, ăn kèm nấm đùi gà và chả lụa chay kho sệt với chùm tiêu xanh thơm nồng cay nhẹ.',
        N'["Bước 1: Nấu cơm trong niêu đất trên lửa nhỏ để tạo lớp cơm cháy vàng giòn rụm.","Bước 2: Nấm đùi gà và chả lụa chay cắt khối vuông vừa ăn.","Bước 3: Kho nấm và chả lụa cùng nước tương cốt dừa, đường mía và chùm tiêu xanh đập dập đến khi sốt sệt sánh óng.","Bước 4: Mở nắp niêu cơm nóng hổi, chan nước kho sệt lên lớp cơm cháy thưởng thức giòn rụm."]',
        15, 25, 1, 650.0, 'MEDIUM',
        1, 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80',
        'APPROVED_ACTIVE', 430, 'SYSTEM', SYSDATETIME()
    ),
    (
        135, 1, 11, 1,
        N'Mì Quảng chay đậu phộng bánh tráng mè',
        N'Sợi mì Quảng vàng óng mềm dai, nước nhưn nấm rơm kho đậu hũ béo ngậy mùi dầu điều, ăn kèm bánh tráng mè nướng giòn và đậu phộng rang bùi.',
        N'["Bước 1: Xào săn đậu hũ chiên, chả lụa và nấm rơm với sốt nghệ tươi và dầu điều.","Bước 2: Nấu nước nhưn sánh đậm đà bằng nước dừa xiêm hầm nấm.","Bước 3: Xếp sợi mì Quảng ra tô cùng búp chuối thái mỏng và rau thơm.","Bước 4: Chan nước nhưn xăm xắp mặt mì, rắc đậu phộng rang giòn và bẻ bánh tráng mè thưởng thức."]',
        15, 20, 1, 630.0, 'EASY',
        1, 'https://images.unsplash.com/photo-1569718212165-3a8278d5f624?auto=format&fit=crop&w=800&q=80',
        'APPROVED_ACTIVE', 360, 'SYSTEM', SYSDATETIME()
    ),
    (
        136, 1, 11, 1,
        N'Bún đậu chả cốm chay chiên giòn',
        N'Mẹt bún lá cắt miếng vuông, đậu hũ lướt ván giòn vỏ mềm ruột, chả cốm mộc chay chiên phồng béo ngậy chấm nước tương tỏi ớt hoặc tương đậu phộng.',
        N'["Bước 1: Trộn cốm dẻo thơm với đậu hũ non quết nhuyễn và gia vị chay rồi hấp chín.","Bước 2: Chiên chả cốm vàng ruộm hai mặt giòn phồng thơm nức.","Bước 3: Đậu hũ mơ cắt khối chiên ngập dầu giòn tan bên ngoài béo mịn bên trong.","Bước 4: Bày bún lá, đậu chiên, chả cốm ra mẹt lá chuối cùng dưa leo, tía tô, kinh giới và bát nước chấm đậm đà."]',
        15, 20, 1, 670.0, 'EASY',
        1, 'https://images.unsplash.com/photo-1569718212165-3a8278d5f624?auto=format&fit=crop&w=800&q=80',
        'APPROVED_ACTIVE', 480, 'SYSTEM', SYSDATETIME()
    ),
    (
        137, 1, 11, 1,
        N'Hủ tiếu xào mềm thập cẩm sốt dầu hào chay',
        N'Sợi hủ tiếu mềm mượt áp chảo thơm lừng khói wox, xào hòa quyện cùng nấm đùi gà, chả lụa chay, bông cải xanh và hạt sen bùi sốt sánh ngậy.',
        N'["Bước 1: Áp chảo sợi hủ tiếu với chút hắc xì dầu cho thơm xém cạnh.","Bước 2: Xào nấm đùi gà, cà rốt, bông cải và hạt sen luộc chín tới.","Bước 3: Pha sốt dầu hào chay từ nấm đông cô, nước dùng củ cải và bột bắp tạo độ sánh mịn.","Bước 4: Trút hỗn hợp rau nấm và sốt ngập sợi hủ tiếu, đảo nhanh tay trên lửa lớn trong 2 phút."]',
        15, 15, 1, 600.0, 'EASY',
        1, 'https://images.unsplash.com/photo-1569718212165-3a8278d5f624?auto=format&fit=crop&w=800&q=80',
        'APPROVED_ACTIVE', 310, 'SYSTEM', SYSDATETIME()
    ),
    (
        138, 1, 1, 1,
        N'Bánh xèo miền Tây chay củ hũ dừa nấm mối',
        N'Vỏ bánh xèo giòn rụm béo ngậy nước cốt dừa bột nghệ, nhân củ hũ dừa ngọt giòn, nấm mối xào đậu xanh và giá đỗ cuốn rau rừng bánh tráng.',
        N'["Bước 1: Pha bột gạo, bột nghệ, nước cốt dừa đậm đặc và hành boa rô thái nhỏ.","Bước 2: Xào nhân gồm củ hũ dừa thái mỏng, nấm mối và đậu xanh nguyên hạt chín mềm.","Bước 3: Tráng lớp bột mỏng xèo xèo giòn rụm quanh chảo gang, rải đều nhân rồi đậy vung 3 phút.","Bước 4: Gập đôi bánh xèo giòn rụm, thưởng thức cùng rau xà lách, cải bẹ xanh và nước mắm chua ngọt."]',
        20, 20, 1, 640.0, 'MEDIUM',
        1, 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80',
        'APPROVED_ACTIVE', 420, 'SYSTEM', SYSDATETIME()
    ),
    (
        139, 1, 5, 1,
        N'Cơm chiên dừa sấy hạt điều trái thơm',
        N'Hạt cơm tơi giòn vàng ruộm quyện vị chua ngọt của dứa tươi, cùi dừa nạo sấy béo thơm và hạt điều giòn tan kết hợp đậu cô ve bùi bùi.',
        N'["Bước 1: Nấu cơm tơi xốp, để nguội và trộn đều với dầu điều tạo màu vàng ươm óng ả.","Bước 2: Xắt hạt lựu dứa chín ngọt, cà rốt và đậu cô ve.","Bước 3: Chiên cơm lửa lớn cho hạt săn giòn, trút dứa và rau củ vào đảo đều tay.","Bước 4: Rắc dừa sấy giòn tan cùng hạt điều rang bùi béo lên mặt đĩa cơm chiên thơm ngát."]',
        15, 15, 1, 660.0, 'EASY',
        1, 'https://images.unsplash.com/photo-1603133872878-684f208fb84b?auto=format&fit=crop&w=800&q=80',
        'APPROVED_ACTIVE', 350, 'SYSTEM', SYSDATETIME()
    ),
    (
        140, 1, 5, 1,
        N'Cơm cháy kho quẹt chay nấm đông cô cốt dừa',
        N'Từng miếng cơm cháy đáy nồi giòn tan rụm chấm nồi kho quẹt nấm đông cô kẹo sệt, tóp mỡ bánh mì giòn tan béo ngậy tiêu sọ nồng cay.',
        N'["Bước 1: Ép cơm nếp mỏng vào chảo gang nướng vàng giòn hai mặt tạo cơm cháy vàng rộm.","Bước 2: Nấm đông cô khô ngâm nở xắt hạt lựu xào thơm với dầu màu điều.","Bước 3: Kho nấm cùng nước tương đặc, đường thốt nốt, nước cốt dừa và tiêu sọ đập dập đến khi kẹo quẹt sánh sệt óng ánh.","Bước 4: Bánh mì xắt hạt lựu chiên giòn làm tóp mỡ thả vào nồi kho quẹt, bẻ miếng cơm cháy giòn rụm quệt ăn say mê."]',
        20, 20, 1, 700.0, 'MEDIUM',
        1, 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80',
        'APPROVED_ACTIVE', 520, 'SYSTEM', SYSDATETIME()
    )
) AS source (Recipe_ID, User_ID, Category_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At)
ON target.Recipe_ID = source.Recipe_ID
WHEN MATCHED THEN
    UPDATE SET 
        User_ID = source.User_ID,
        Category_ID = source.Category_ID,
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
    INSERT (Recipe_ID, User_ID, Category_ID, Dietary_School_ID, Title, Description, Instructions, Prep_Time_Minutes, Cook_Time_Minutes, Servings, Calories_Kcal, Difficulty_Level, Is_Verified, Image_Url, Status, View_Count, Source, Created_At)
    VALUES (source.Recipe_ID, source.User_ID, source.Category_ID, source.Dietary_School_ID, source.Title, source.Description, source.Instructions, source.Prep_Time_Minutes, source.Cook_Time_Minutes, source.Servings, source.Calories_Kcal, source.Difficulty_Level, source.Is_Verified, source.Image_Url, source.Status, source.View_Count, source.Source, source.Created_At);

SET IDENTITY_INSERT dbo.Recipes OFF;
GO

PRINT N'Đã nạp thành công 12 công thức chay truyền thống năng lượng cao (129 - 140) vào CSDL!';
GO
