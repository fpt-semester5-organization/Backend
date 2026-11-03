-- ============================================================================
-- File: 21_Seed_Community_Videos_Posts.sql
-- Thư mục: database/seed/
-- Mục đích: Kịch bản nạp dữ liệu chuẩn cho:
--   1. 06 Video dạy nấu ăn chuẩn mực (link YouTube Embed, TikTok Embed)
--      kèm Timeline Phân đoạn Lời thoại (Transcript Segments) & JSON Công thức
--   2. 10 Bài viết chia sẻ kinh nghiệm ăn chay sâu sắc trên trang /community
--   3. 25 Bình luận thảo luận tương tác đa tầng chân thật (Parent - Child replies)
--   4. Hàng chục lượt Vote tích cực từ các hội viên thực tế
--
-- Tính chất: IDEMPOTENT (Chạy nhiều lần không trùng dữ liệu, an toàn tuyệt đối)
-- ============================================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'============================================================================';
PRINT N'  Đang nạp dữ liệu Video Nấu Ăn (/videos) & Bài Viết Chia Sẻ (/community)...';
PRINT N'============================================================================';

DECLARE @Now DATETIME2(7) = SYSDATETIME();

-- ============================================================================
-- 1. ĐẢM BẢO TỒN TẠI 15 TÀI KHOẢN TÁC GIẢ & HỘI VIÊN TIÊU BIỂU
-- ============================================================================
PRINT N'>> [1/5] Đảm bảo các tài khoản Hội viên, Bác sĩ & Bếp trưởng...';
SET IDENTITY_INSERT dbo.Users ON;

MERGE INTO dbo.Users AS target
USING (
    VALUES
        (1, 'admin@veggielife.vn', '$2a$11$GBRajsAbFfn4G4UI7bhu.O8MGs4mXfn/clcozGAZ0npIHYrlShxwO', N'Hệ Thống Quản Trị VeggieLife', '0901234567', 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=400&q=80', 'ADMIN', 0, NULL, DATEADD(HOUR, -2, @Now), DATEADD(DAY, -180, @Now), 'ACTIVE', 0),
        (2, 'chef.thanhhoa@veggielife.vn', '$2a$11$GBRajsAbFfn4G4UI7bhu.O8MGs4mXfn/clcozGAZ0npIHYrlShxwO', N'Bếp Trưởng Thanh Hòa', '0912345678', 'https://images.unsplash.com/photo-1577219491135-ce391730fb2c?auto=format&fit=crop&w=400&q=80', 'VIP', 0, NULL, DATEADD(HOUR, -5, @Now), DATEADD(DAY, -150, @Now), 'ACTIVE', 0),
        (3, 'chef.hoangkim@veggielife.vn', '$2a$11$GBRajsAbFfn4G4UI7bhu.O8MGs4mXfn/clcozGAZ0npIHYrlShxwO', N'Master Chef Hoàng Kim', '0922334455', 'https://images.unsplash.com/photo-1583394838336-acd977736f90?auto=format&fit=crop&w=400&q=80', 'VIP', 0, NULL, DATEADD(HOUR, -1, @Now), DATEADD(DAY, -140, @Now), 'ACTIVE', 0),
        (4, 'chef.tuean@veggielife.vn', '$2a$11$GBRajsAbFfn4G4UI7bhu.O8MGs4mXfn/clcozGAZ0npIHYrlShxwO', N'Nghệ Nhân Tuệ An Chay', '0933445566', 'https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&w=400&q=80', 'VIP', 0, NULL, DATEADD(HOUR, -12, @Now), DATEADD(DAY, -135, @Now), 'ACTIVE', 0),
        (5, 'chef.minhduc@veggielife.vn', '$2a$11$GBRajsAbFfn4G4UI7bhu.O8MGs4mXfn/clcozGAZ0npIHYrlShxwO', N'Chuyên Gia Ẩm Thực Minh Đức', '0944556677', 'https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?auto=format&fit=crop&w=400&q=80', 'VIP', 0, NULL, DATEADD(DAY, -1, @Now), DATEADD(DAY, -130, @Now), 'ACTIVE', 0),
        (6, 'lelan.nutrition@gmail.com', '$2a$11$GBRajsAbFfn4G4UI7bhu.O8MGs4mXfn/clcozGAZ0npIHYrlShxwO', N'Bs. Lê Lan Dinh Dưỡng', '0955667788', 'https://images.unsplash.com/photo-1594824813589-983226a3a41b?auto=format&fit=crop&w=400&q=80', 'VIP', 0, NULL, DATEADD(DAY, -2, @Now), DATEADD(DAY, -120, @Now), 'ACTIVE', 0),
        (7, 'quangtri.buddhist@gmail.com', '$2a$11$GBRajsAbFfn4G4UI7bhu.O8MGs4mXfn/clcozGAZ0npIHYrlShxwO', N'Phật Tử Quảng Trí', '0966778899', 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=400&q=80', 'VIP', 0, NULL, NULL, DATEADD(DAY, -115, @Now), 'ACTIVE', 0),
        (8, 'hoangnam.fitvegan@gmail.com', '$2a$11$GBRajsAbFfn4G4UI7bhu.O8MGs4mXfn/clcozGAZ0npIHYrlShxwO', N'HLV. Hoàng Nam Fitness Vegan', '0977889900', 'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=400&q=80', 'VIP', 0, NULL, DATEADD(HOUR, -8, @Now), DATEADD(DAY, -110, @Now), 'ACTIVE', 0),
        (9, 'maiphuong.yoga@gmail.com', '$2a$11$GBRajsAbFfn4G4UI7bhu.O8MGs4mXfn/clcozGAZ0npIHYrlShxwO', N'Mai Phương Yoga Life', '0988990011', 'https://images.unsplash.com/photo-1494790108377-be9c29b29330?auto=format&fit=crop&w=400&q=80', 'VIP', 0, NULL, DATEADD(MINUTE, -30, @Now), DATEADD(DAY, -100, @Now), 'ACTIVE', 0),
        (10, 'phamdinh.green@gmail.com', '$2a$11$GBRajsAbFfn4G4UI7bhu.O8MGs4mXfn/clcozGAZ0npIHYrlShxwO', N'Phạm Đình Sống Xanh', '0999001122', 'https://images.unsplash.com/photo-1522075469751-3a6694fb2f61?auto=format&fit=crop&w=400&q=80', 'VIP', 0, NULL, DATEADD(DAY, -10, @Now), DATEADD(DAY, -95, @Now), 'ACTIVE', 0),
        (11, 'lananh.healthy@veggielife.vn', '$2a$11$GBRajsAbFfn4G4UI7bhu.O8MGs4mXfn/clcozGAZ0npIHYrlShxwO', N'Lan Anh Healthy Food', '0811203487', 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=400&q=80', 'MEMBER', 0, NULL, DATEADD(HOUR, -33, @Now), DATEADD(DAY, -90, @Now), 'ACTIVE', 0),
        (12, 'minhtri.plantbased@veggielife.vn', '$2a$11$GBRajsAbFfn4G4UI7bhu.O8MGs4mXfn/clcozGAZ0npIHYrlShxwO', N'Minh Trí Plant-Based', '0812203804', 'https://images.unsplash.com/photo-1527980965255-d3b416303d12?auto=format&fit=crop&w=400&q=80', 'MEMBER', 0, NULL, DATEADD(HOUR, -36, @Now), DATEADD(DAY, -85, @Now), 'ACTIVE', 0),
        (13, 'thutrang.dr@gmail.com', '$2a$11$GBRajsAbFfn4G4UI7bhu.O8MGs4mXfn/clcozGAZ0npIHYrlShxwO', N'Bác sĩ Thu Trang', '0813204121', 'https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?auto=format&fit=crop&w=400&q=80', 'MEMBER', 0, NULL, DATEADD(HOUR, -39, @Now), DATEADD(DAY, -80, @Now), 'ACTIVE', 0),
        (14, 'tueduc.chay@gmail.com', '$2a$11$GBRajsAbFfn4G4UI7bhu.O8MGs4mXfn/clcozGAZ0npIHYrlShxwO', N'Bếp Chay Tuệ Đức', '0814204438', 'https://images.unsplash.com/photo-1560250097-0b93528c311a?auto=format&fit=crop&w=400&q=80', 'MEMBER', 0, NULL, DATEADD(HOUR, -42, @Now), DATEADD(DAY, -75, @Now), 'ACTIVE', 0),
        (15, 'huonggiang.yoga@gmail.com', '$2a$11$GBRajsAbFfn4G4UI7bhu.O8MGs4mXfn/clcozGAZ0npIHYrlShxwO', N'Hương Giang Yoga', '0815204755', 'https://images.unsplash.com/photo-1580489944761-15a19d654956?auto=format&fit=crop&w=400&q=80', 'MEMBER', 0, NULL, DATEADD(HOUR, -45, @Now), DATEADD(DAY, -70, @Now), 'ACTIVE', 0)
) AS source (User_ID, Email, Password_Hash, Full_Name, Phone_Number, Avatar_Url, Role, Failed_Login_Count, Lockout_End, Last_Login_At, Created_At, Account_Status, Is_Deleted)
ON target.User_ID = source.User_ID
WHEN MATCHED THEN
    UPDATE SET target.Full_Name = source.Full_Name, target.Avatar_Url = source.Avatar_Url, target.Account_Status = source.Account_Status
WHEN NOT MATCHED THEN
    INSERT (User_ID, Email, Password_Hash, Full_Name, Phone_Number, Avatar_Url, Role, Failed_Login_Count, Lockout_End, Last_Login_At, Created_At, Account_Status, Is_Deleted)
    VALUES (source.User_ID, source.Email, source.Password_Hash, source.Full_Name, source.Phone_Number, source.Avatar_Url, source.Role, source.Failed_Login_Count, source.Lockout_End, source.Last_Login_At, source.Created_At, source.Account_Status, source.Is_Deleted);

SET IDENTITY_INSERT dbo.Users OFF;
GO

-- ============================================================================
-- 2. NẠP 16 BÀI VIẾT (POSTS): 6 VIDEO NẤU ĂN + 10 CHIA SẺ KINH NGHIỆM ĂN CHAY
-- ============================================================================
PRINT N'>> [2/5] Nạp 16 bài viết (6 video hướng dẫn + 10 bài kinh nghiệm cộng đồng)...';
SET IDENTITY_INSERT dbo.Posts ON;

MERGE INTO dbo.Posts AS target
USING (
    VALUES
        -- 6 BÀI VIẾT ĐÍNH KÈM VIDEO NẤU ĂN (1011 - 1016)
        (1011, 2, NULL, N'Hướng dẫn nấu Canh chua chay Nam Bộ thanh mát giải nhiệt chuẩn vị',
         N'Hôm nay gian bếp VeggieLife xin chia sẻ cùng cả nhà công thức Canh chua chay chuẩn vị Nam Bộ thanh mát cho ngày hè. Nước canh chua thanh tự nhiên nhờ me dầm nước sôi, ngọt dịu từ thơm (dứa) và cà chua xào thơm với boa-rô, đậu hũ chiên giòn béo bùi, bạc hà đậu bắp giòn rụm. Xem video hướng dẫn chi tiết từng mốc thời gian kèm công thức chuẩn mực bên dưới nhé!',
         'https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=800&q=80', 'PUBLIC', 1420, 'APPROVED_ACTIVE', DATEADD(DAY, -14, SYSDATETIME()), NULL),

        (1012, 3, NULL, N'Bí quyết làm Nấm đùi gà kho tiêu hạt sen đậm đà óng ả cực đưa cơm',
         N'Món mặn "quốc dân" trong mâm cơm chay gia đình! Nấm đùi gà tươi cắt khoanh tròn 2cm, khía hoa vảy rồng hai mặt để ngấm trọn gia vị, áp chảo xém vàng rồi kho liu riu cùng nước dừa xiêm, hạt sen bùi béo và tiêu sọ Phú Quốc đập dập. Nước sốt nâu cánh gián sánh keo, đậm vị mặn ngọt hài hòa, chan cùng cơm gạo lứt nóng hổi thơm nức mũi.',
         'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80', 'PUBLIC', 2150, 'APPROVED_ACTIVE', DATEADD(DAY, -12, SYSDATETIME()), NULL),

        (1013, 4, NULL, N'Nấu Phở bò chay Hà Nội nước dùng trong veo, thảo mộc thơm lừng nức mũi',
         N'Phở chay làm sao để có vị ngọt sâu và hương thơm nồng nàn đặc trưng của phở truyền thống? Bí quyết nằm ở nồi nước dùng ninh 2 tiếng từ củ sắn, mía lau, củ cải trắng và bắp ngọt. Gói thảo mộc gồm hoa hồi, thảo quả, quế thanh, tiểu hồi rang vàng thơm lừng. "Thịt nấm" dai ngọt từ nấm đùi gà xé sợi xào hạt điều đỏ bắt mắt.',
         'https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?auto=format&fit=crop&w=800&q=80', 'PUBLIC', 3890, 'APPROVED_ACTIVE', DATEADD(DAY, -10, SYSDATETIME()), NULL),

        (1014, 9, NULL, N'Gỏi cuốn ngũ sắc thanh lọc chấm sốt bơ đậu phộng béo bùi gây nghiện',
         N'Bữa trưa tươi mát đầy màu sắc và vitamin cho ngày hè bận rộn! Bánh tráng gạo mè dẻo dai cuốn chặt cùng bắp cải tím, dưa leo, xoài xanh, cà rốt giòn ngọt và đậu hũ chiên vàng. Điểm nhấn là chén sốt tương bơ đậu phộng sánh đặc thơm béo ngậy, ai ăn một lần cũng mê mẩn.',
         'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80', 'PUBLIC', 1840, 'APPROVED_ACTIVE', DATEADD(DAY, -8, SYSDATETIME()), NULL),

        (1015, 5, NULL, N'Cà ri chay khoai lang đậu gà cốt dừa béo ngậy ăn cùng bánh mì giòn',
         N'Món cà ri chay thơm lừng mùi sả cây và bột cà ri Ấn Độ hảo hạng, nước sốt vàng ươm sánh ngậy vị cốt dừa tươi Bến Tre. Khoai lang bùi ngọt, đậu gà giàu đạm thực vật mềm tan, nấm hương dai giòn sần sật. Thích hợp cho bữa tiệc gia đình ngày mưa se lạnh!',
         'https://images.unsplash.com/photo-1546833999-b9f581a1996d?auto=format&fit=crop&w=800&q=80', 'PUBLIC', 2780, 'APPROVED_ACTIVE', DATEADD(DAY, -6, SYSDATETIME()), NULL),

        (1016, 2, NULL, N'Bí quyết đổ Bánh xèo chay giòn rụm cả tiếng không ỉu nhân nấm củ hũ dừa',
         N'Công thức pha bột bánh xèo gia truyền giòn tan: bột gạo, bột bắp, nước cốt dừa và một lon nước có ga để tạo độ xốp rỗng. Nhân bánh thanh mát từ củ hũ dừa Bến Tre bào sợi, nấm hương tươi xào thơm và giá đỗ tươi. Cuốn cùng rau rừng, xà lách chấm nước mắm chay chua ngọt cay cay.',
         'https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?auto=format&fit=crop&w=800&q=80', 'PUBLIC', 4120, 'APPROVED_ACTIVE', DATEADD(DAY, -4, SYSDATETIME()), NULL),

        -- 10 BÀI VIẾT CHIA SẺ KINH NGHIỆM ĂN CHAY TRANG CỘNG ĐỒNG (1021 - 1030)
        (1021, 6, NULL, N'Hành trình 30 ngày chuyển từ ăn mặn sang thuần chay: Những thay đổi kỳ diệu về năng lượng và giấc ngủ',
         N'Chào cộng đồng VeggieLife, mình là bác sĩ Lê Lan. Hôm nay mình muốn chia sẻ hành trình 30 ngày chuyển đổi từ chế độ ăn mặn thông thường sang thuần chay (Plant-based) hoàn toàn.

**Tuần 1: Giai đoạn giải độc và thích nghi**
Cơ thể bắt đầu có cảm giác thèm vị umami đậm đà của thịt. Bí quyết của mình là sử dụng nấm hương nướng, tương miso và men dinh dưỡng (nutritional yeast) để tạo vị ngọt thịt tự nhiên. Uống đủ 2.5 lít nước mỗi ngày giúp thận đào thải lượng axit uric tích tụ.

**Tuần 2: Ổn định hệ tiêu hóa**
Lượng chất xơ tăng vọt từ rau xanh và đậu gà có thể gây chướng bụng nhẹ. Mẹo nhỏ: ngâm kỹ các loại hạt đậu 8 tiếng trước khi nấu và cho vài lát gừng tươi vào hầm. Sau 10 ngày, hệ vi sinh đường ruột thích nghi hoàn hảo, cảm giác nặng bụng sau bữa ăn hoàn toàn biến mất!

**Tuần 3 & 4: Sự bừng sáng về thể chất và tinh thần**
Giấc ngủ sâu hơn rõ rệt, không còn thức giấc giữa đêm. Sáng dậy tỉnh táo không cần cà phê. Đặc biệt, huyết áp và chỉ số mỡ máu giảm đáng kể. Ăn chay không phải là sự từ bỏ, mà là mở ra một thế giới ẩm thực phong phú, đầy màu sắc và nhân ái!',
         'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=800&q=80', 'PUBLIC', 3450, 'APPROVED_ACTIVE', DATEADD(DAY, -15, SYSDATETIME()), NULL),

        (1022, 6, NULL, N'Bí quyết bổ sung Vitamin B12, Sắt và Kẽm cho người ăn chay trường không lo thiếu máu hay suy nhược',
         N'Rất nhiều bạn ăn chay thường lo lắng về tình trạng thiếu máu, rụng tóc hay mệt mỏi. Với góc nhìn y khoa và dinh dưỡng, mình xin đúc kết các quy tắc vàng:

1. **Vitamin B12 - Bắt buộc phải bổ sung đúng cách:**
B12 không tồn tại tự nhiên trong thực vật. Người ăn chay trường nên bổ sung viên ngậm Methylcobalamin 500mcg - 1000mcg hàng tuần, hoặc sử dụng sữa hạt và ngũ cốc có tăng cường vi chất B12.

2. **Hấp thu Sắt không chứa heme (Non-heme Iron) tối ưu:**
Sắt trong rau bina, đậu lăng, hạt bí và cải xoăn cần môi trường axit để hấp thu. Hãy luôn kết hợp nguồn Vitamin C (vắt vài giọt chanh, ăn kèm ớt chuông hoặc tráng miệng với cam, ổi) ngay trong bữa ăn để tăng hấp thu sắt gấp 3 - 4 lần! Tuyệt đối không uống trà xanh hay cà phê ngay sau bữa ăn vì chất tannin sẽ ức chế hấp thu sắt.

3. **Kẽm (Zinc) cho hệ miễn dịch vững vàng:**
Hạt bí ngô, hạt gai dầu, mầm yến mạch và đậu phụ non là nguồn kẽm tuyệt vời. Ngâm và ủ mầm đậu trước khi chế biến sẽ vô hiệu hóa phytate, giúp cơ thể hấp thụ trọn vẹn khoáng chất.',
         'https://images.unsplash.com/photo-1490645935967-10de6ba17061?auto=format&fit=crop&w=800&q=80', 'PUBLIC', 4820, 'APPROVED_ACTIVE', DATEADD(DAY, -13, SYSDATETIME()), NULL),

        (1023, 9, NULL, N'Thực đơn Eat Clean thuần chay giảm 4kg mỡ thừa trong 6 tuần: Vẫn no bụng và tràn đầy sinh lực',
         N'Nhiều bạn nghĩ ăn chay sẽ béo vì ăn nhiều tinh bột trắng hoặc đồ chiên xào giả mặn. Nhưng với phương pháp Eat Clean thuần chay nguyên bản (Whole Food Plant-Based), mình đã giúp hàng trăm học viên yoga giảm từ 3-5kg mỡ thừa nhẹ nhàng.

**Quy tắc đĩa ăn cân bằng chuẩn khoa học:**
- **50% Đĩa ăn:** Rau củ quả nhiều màu sắc giàu chất xơ (bông cải xanh, măng tây, ớt chuông, dưa leo, cà chua).
- **25% Đĩa ăn:** Protein thực vật đậm đặc (đậu hũ nướng nồi chiên không dầu, tempeh xào tương, đậu gà luộc, edamame).
- **25% Đĩa ăn:** Tinh bột hấp thu chậm (gạo lứt huyết rồng, khoai lang nướng, yến mạch cán dẹt, hạt diêm mạch quinoa).

**Công thức nước sốt salad "thần thánh" không dầu mỡ:**
Xay nhuyễn 1/2 quả bơ chín, 1 muỗng bơ mè tahini, nước cốt 1/2 quả chanh vàng, 1 tép tỏi nhỏ và nhúm muối hồng. Trộn đều cùng salad giòn ngọt ăn no bụng mà lượng calo nạp vào chỉ bằng một nửa bữa ăn mặn thông thường!',
         'https://images.unsplash.com/photo-1543339308-43e59d6b73a6?auto=format&fit=crop&w=800&q=80', 'PUBLIC', 5210, 'APPROVED_ACTIVE', DATEADD(DAY, -11, SYSDATETIME()), NULL),

        (1024, 2, NULL, N'Mẹo Meal Prep: Đi chợ 1 lần vào Chủ nhật, sơ chế nấm và rau củ tươi ngon cả tuần cho dân văn phòng',
         N'Là đầu bếp nhưng trong tuần mình cũng rất bận rộn. Đây là quy trình Meal Prep tiết kiệm 80% thời gian bếp núc mỗi tối dành cho bạn:

**1. Bảo quản nấm tươi không bị nhớt hay thâm đen:**
Nấm đùi gà, nấm hương tuyệt đối không rửa trước khi cất tủ lạnh. Hãy gọt sạch chân nấm, bọc vào túi giấy xi măng hoặc hộp nhựa lót khăn giấy khô. Khăn giấy sẽ hút độ ẩm thừa, nấm giữ được độ giòn ngọt suốt 8-10 ngày.

**2. Sơ chế rau thơm và củ quả:**
Ngò rí, húng quế, hành boa-rô rửa sạch, quay thật ráo nước bằng rổ quay ly tâm. Cắt vừa hộp thủy tinh và đặt một lớp khăn giấy ăn ở đáy lẫn trên bề mặt.
Cà rốt, củ cải gọt vỏ cắt que ngâm trong hộp nước sạch đậy kín, đổi nước 2 ngày một lần, củ luôn giòn ngọt như mới nhổ.

**3. Nấu sẵn 3 hũ sốt vạn năng:**
- Sốt kho tiêu gừng sánh kẹo (cho các món nấm kho, đậu kho).
- Sốt chua ngọt chanh dây (cho gỏi cuốn, đậu hũ chiên).
- Sốt mè rang dinh dưỡng (cho rau củ luộc và mì trộn).
Tối đi làm về chỉ mất 10 phút xào nấu là có ngay mâm cơm chay nóng sốt chuẩn vị!',
         'https://images.unsplash.com/photo-1498837167922-ddd27525d352?auto=format&fit=crop&w=800&q=80', 'PUBLIC', 2930, 'APPROVED_ACTIVE', DATEADD(DAY, -9, SYSDATETIME()), NULL),

        (1025, 10, NULL, N'Cẩm nang sinh tồn khi đi tiệc mặn, liên hoan công ty hay du lịch xa cho người ăn chay',
         N'Ăn chay tại nhà thì rất dễ, nhưng ra ngoài xã hội tiệc tùng hay đi phượt cùng bạn bè mặn thì thế nào? Dưới đây là những kinh nghiệm xương máu sau 6 năm ăn chay trường của mình:

- **Báo trước với người tổ chức một cách tinh tế:** Đừng tạo cảm giác mình đòi hỏi đặc quyền. Hãy nhắn tin nhẹ nhàng: "Em ăn chay thanh đạm nên chỉ cần 1 đĩa đậu hũ chiên sả hoặc rau củ xào là no bụng rồi ạ". Thường các nhà hàng tiệc cưới hiện nay đều có thực đơn chay riêng rất tươm tất nếu được báo trước.
- **Xem menu nhà hàng mặn với con mắt sáng tạo:** Hầu hết quán lẩu hay nướng mặn đều có nấm tươi, ngô ngọt, khoai lang, đậu hũ trắng và rau xanh. Chỉ cần dặn nhà bếp cho nồi nước lẩu chay (hoặc nước dừa tươi đun sôi nêm muối ớt) là bạn đã có một nồi lẩu ấm cúng bên bạn bè.
- **Túi "cứu cánh" khi đi du lịch phượt:** Luôn thủ sẵn trong balo: 2 gói rong biển sấy mè, 1 hũ ruốc nấm hương tự làm, thanh ngũ cốc hạt điều, và vài gói cháo yến mạch ăn liền. Dù có kẹt ở sân bay hay vùng sâu vùng xa, bạn vẫn luôn chủ động nguồn dinh dưỡng sạch!',
         'https://images.unsplash.com/photo-1511690656952-34342bb7c2f2?auto=format&fit=crop&w=800&q=80', 'PUBLIC', 2170, 'APPROVED_ACTIVE', DATEADD(DAY, -7, SYSDATETIME()), NULL),

        (1026, 11, NULL, N'Làn da sạch mụn, cơ thể nhẹ nhõm: 90 ngày ăn chay thanh lọc đã thay đổi tôi như thế nào',
         N'Trước đây mình từng chiến đấu với mụn nội tiết dai dẳng suốt 4 năm đại học. Đi da liễu uống thuốc kháng sinh bôi kem đủ loại chỉ đỡ được một thời gian rồi tái phát dữ dội hơn.

Bước ngoặt xảy ra khi mình quyết định cắt bỏ 100% sữa bò, phô mai và thịt đỏ trong 90 ngày:
- **Tháng thứ nhất:** Tuần đầu da có nổi vài nốt mụn thanh lọc li ti. Nhưng đến cuối tháng, các ổ viêm sưng đỏ dưới cằm xẹp hẳn, không còn cảm giác nóng bừng bức bối trong người.
- **Tháng thứ hai:** Lượng dầu vùng chữ T giảm rõ rệt. Lỗ chân lông thu nhỏ lại. Mình bắt đầu uống thêm sinh tố cần tây, táo xanh và tảo xoắn spirulina mỗi sáng.
- **Tháng thứ ba:** Làn da sáng bừng khỏe khoắn từ bên trong, các vết thâm mờ đi 80%. Quan trọng nhất là tinh thần luôn vui tươi, tràn đầy năng lượng tích cực.

Vẻ đẹp thực sự bắt đầu từ một hệ tiêu hóa khỏe mạnh và nguồn thức ăn thanh khiết từ thiên nhiên!',
         'https://images.unsplash.com/photo-1506126613408-eca07ce68773?auto=format&fit=crop&w=800&q=80', 'PUBLIC', 3760, 'APPROVED_ACTIVE', DATEADD(DAY, -5, SYSDATETIME()), NULL),

        (1027, 3, NULL, N'Nấu nước dùng chay ngọt thanh tự nhiên từ 5 loại củ quả: Không cần một hạt mì chính hay hạt nêm',
         N'Nhiều quán ăn chay lạm dụng bột ngọt và hạt nêm giả thịt khiến người ăn xong bị khô cổ khát nước. Người đầu bếp chay chân chính lấy vị ngọt từ lòng đất mẹ.

**Tỉ lệ vàng cho nồi nước dùng 5 lít trong vắt:**
- **2 khúc mía lau chẻ đôi:** Cho vị ngọt thanh tao sâu lắng.
- **1 bắp ngô ngọt cắt khúc:** Cung cấp hương thơm sữa bắp dịu dàng.
- **1 củ sắn (củ đậu) gọt vỏ:** Giữ cho nước dùng luôn trong trẻo không bị đục.
- **1 củ cải trắng + 1 củ cải muối khô ngâm rửa sạch:** Tạo chiều sâu hậu vị đằm thắm.
- **5 tai nấm hương khô nướng cháy xém vỏ:** Tiết ra glutamate tự nhiên cực kỳ thơm ngon.

Hầm trên lửa nhỏ liu riu 90 phút, tuyệt đối không đậy nắp và không khuấy đảo mạnh. Lọc qua rây vải mịn, nêm một nhúm muối hầm Đề Gi. Bạn sẽ có thứ nước dùng tinh hoa để nấu bún, phở, hủ tiếu hay canh súp thơm ngon xuất sắc!',
         'https://images.unsplash.com/photo-1547592166-23ac45744acd?auto=format&fit=crop&w=800&q=80', 'PUBLIC', 6120, 'APPROVED_ACTIVE', DATEADD(DAY, -3, SYSDATETIME()), NULL),

        (1028, 8, NULL, N'Tập gym và tăng cơ giảm mỡ thành công với chế độ ăn hoàn toàn từ thực vật (Plant-based Fitness)',
         N'"Ăn cỏ thì sức đâu mà nâng tạ? Ăn chay sao to cơ được?" - Đó là câu hỏi mình nhận được nhiều nhất suốt 3 năm qua tại phòng gym.

Sự thật là protein từ thực vật hoàn toàn có thể xây dựng cơ bắp săn chắc nếu bạn hiểu đúng về cấu trúc axit amin:
1. **Các siêu thực phẩm tăng cơ thuần chay:**
   - **Seitan (Mì căn tự làm):** Chứa tới 75g protein / 100g (cao gấp 3 lần ức gà!).
   - **Tempeh (Đậu tương lên men):** 19g protein / 100g, chứa probiotics nuôi dưỡng đường ruột hấp thu tối đa.
   - **Đậu lăng & Đậu gà:** Nguồn carb phức hợp và protein đồng thời, nạp năng lượng bền bỉ cho buổi squat nặng.
2. **Lịch nạp dinh dưỡng quanh buổi tập:**
   - **Trước tập 60 phút:** Sinh tố chuối tiêu, yến mạch, bơ đậu phộng và bột hạt chia.
   - **Sau tập 30 phút:** Bột protein thực vật (Pea + Rice protein cô đặc) kết hợp sữa đậu nành ấm.

Hiện tại mình duy trì body fat 11-12%, nâng tạ deadlift 180kg nhẹ nhàng, không bao giờ bị đầy bụng hay nổi mụn như thời kỳ uống Whey Protein từ sữa bò!',
         'https://images.unsplash.com/photo-1517838277536-f5f99be501cd?auto=format&fit=crop&w=800&q=80', 'PUBLIC', 4390, 'APPROVED_ACTIVE', DATEADD(DAY, -2, SYSDATETIME()), NULL),

        (1029, 4, NULL, N'Ăn chay chánh niệm và lắng nghe cơ thể: Nuôi dưỡng sự bình an và yêu thương từ bữa cơm gia đình',
         N'Trong nhịp sống hiện đại vội vã, bữa ăn thường bị biến thành việc vừa lướt điện thoại vừa nuốt vội thức ăn. Ăn chay chánh niệm không chỉ là ăn gì, mà là "cách chúng ta ăn".

Khi ngồi vào bàn ăn, hãy dành 30 giây tĩnh lặng:
- Ngắm nhìn màu xanh của đĩa rau luộc, màu đỏ của lát ớt, màu vàng ươm của miếng đậu hũ. Tri ân đất trời, mưa nắng và giọt mồ hôi của người nông dân đã làm nên hạt ngọc này.
- Nhai chậm rãi từ 20 đến 30 lần mỗi miếng ăn. Khi nhai kỹ, enzym amylase trong nước bọt hòa quyện giúp tinh bột chuyển hóa thành vị ngọt tự nhiên thanh khiết vô cùng dịu mát.
- Lắng nghe tín hiệu no của cơ thể để dừng lại ở mức 80% sức chứa dạ dày.

Khi tâm an tịnh, bữa ăn chay đơn sơ với bát cơm gạo lứt, đĩa rau muống luộc và chén tương kho nấm cũng trở thành một đại tiệc của hạnh phúc và tình thương.',
         'https://images.unsplash.com/photo-1505576399279-565b52d4ac71?auto=format&fit=crop&w=800&q=80', 'PUBLIC', 1890, 'APPROVED_ACTIVE', DATEADD(DAY, -1, SYSDATETIME()), NULL),

        (1030, 7, NULL, N'Kinh nghiệm nuôi con ăn chay đủ chất, trộm vía tăng cân và phát triển chiều cao vượt trội',
         N'Chào các bậc phụ huynh trên VeggieLife! Vợ chồng mình ăn chay trường và cả hai bé nhà mình (bé Bo 6 tuổi, bé Miu 3 tuổi) đều là những em bé thuần chay từ trong bụng mẹ.

Nhiều người e ngại con ăn chay sẽ còi cọc thiếu chất, nhưng thực tế cả hai bé đều đạt chuẩn phát triển của WHO, chiều cao luôn nằm trong top 10% của lớp. Dưới đây là thực đơn dinh dưỡng mình áp dụng:

- **Sữa hạt tươi tự nấu mỗi ngày:** Luân phiên hạt óc chó, hạnh nhân, hạt sen, mè đen và yến mạch. Mè đen và hạt chia chứa hàm lượng canxi cao gấp nhiều lần sữa bò!
- **Cháo & Súp đa sắc:** Nấu cháo cùng hạt kê, đậu lăng đỏ (rất giàu sắt và mềm nhanh), bí đỏ và rong biển wakame cung cấp iot tự nhiên.
- **Bổ sung chất béo tốt cho não bộ:** Trộn trực tiếp 1 muỗng cà phê dầu hạt lanh hoặc dầu sacha inchi ép lạnh vào bát cháo ấm của bé để bổ sung Omega-3 (DHA/EPA).
- **Tập cho con yêu thiên nhiên:** Cho con cùng mẹ lặt rau, gieo hạt giá đỗ tại nhà. Các bé rất hào hứng khi được ăn chính những mầm rau do mình chăm sóc!',
         'https://images.unsplash.com/photo-1476224203421-9ac39bcb3327?auto=format&fit=crop&w=800&q=80', 'PUBLIC', 3120, 'APPROVED_ACTIVE', SYSDATETIME(), NULL)
) AS source (Post_ID, User_ID, Recipe_ID, Title, Content, Image_Url, Visibility, View_Count, Moderation_Status, Created_At, Updated_At)
ON target.Post_ID = source.Post_ID
WHEN MATCHED THEN
    UPDATE SET target.Title = source.Title, target.Content = source.Content, target.Image_Url = source.Image_Url,
               target.Visibility = source.Visibility, target.View_Count = source.View_Count, target.Moderation_Status = source.Moderation_Status
WHEN NOT MATCHED THEN
    INSERT (Post_ID, User_ID, Recipe_ID, Title, Content, Image_Url, Visibility, View_Count, Moderation_Status, Created_At, Updated_At)
    VALUES (source.Post_ID, source.User_ID, source.Recipe_ID, source.Title, source.Content, source.Image_Url, source.Visibility, source.View_Count, source.Moderation_Status, source.Created_At, source.Updated_At);

SET IDENTITY_INSERT dbo.Posts OFF;
GO

-- ============================================================================
-- 3. NẠP 06 VIDEOS NẤU ĂN & BẢN PHIÊN ÂM KÈM TIMELINE PHÂN ĐOẠN (TRANSCRIPTS)
-- ============================================================================
PRINT N'>> [3/5] Nạp 6 Video dạy nấu ăn (YouTube embed, TikTok embed) & VideoTranscripts...';
SET IDENTITY_INSERT dbo.Videos ON;

MERGE INTO dbo.Videos AS target
USING (
    VALUES
        (2101, 1011, N'Video hướng dẫn nấu Canh chua chay Nam Bộ thanh mát', 'https://www.youtube.com/embed/r6UMcVnSFC0', 'https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=800&q=80', 180, 1420, 'READY', DATEADD(DAY, -14, SYSDATETIME())),
        (2102, 1012, N'Video làm Nấm đùi gà kho tiêu hạt sen đậm đà óng ả', 'https://www.youtube.com/embed/GA5BKBB1CLg', 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80', 150, 2150, 'READY', DATEADD(DAY, -12, SYSDATETIME())),
        (2103, 1013, N'Video nấu Phở bò chay Hà Nội nước dùng thảo mộc thơm lừng', 'https://www.youtube.com/embed/tKSWgq9q94M', 'https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?auto=format&fit=crop&w=800&q=80', 210, 3890, 'READY', DATEADD(DAY, -10, SYSDATETIME())),
        (2104, 1014, N'Video cuốn Gỏi cuốn ngũ sắc thanh lọc sốt bơ đậu phộng', 'https://www.youtube.com/embed/d_bznxbRfMk', 'https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80', 120, 1840, 'READY', DATEADD(DAY, -8, SYSDATETIME())),
        (2105, 1015, N'Video nấu Cà ri chay khoai lang đậu gà béo ngậy nước cốt dừa', 'https://www.youtube.com/embed/P2Fbt6lsiWU', 'https://images.unsplash.com/photo-1546833999-b9f581a1996d?auto=format&fit=crop&w=800&q=80', 160, 2780, 'READY', DATEADD(DAY, -6, SYSDATETIME())),
        (2106, 1016, N'Video bí quyết đổ Bánh xèo chay giòn rụm cả tiếng không ỉu', 'https://www.youtube.com/embed/WwIMQAtf_dI', 'https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?auto=format&fit=crop&w=800&q=80', 190, 4120, 'READY', DATEADD(DAY, -4, SYSDATETIME()))
) AS source (Video_ID, Post_ID, Title, Video_Url, Thumbnail_Url, Duration_Seconds, View_Count, Status, Created_At)
ON target.Video_ID = source.Video_ID
WHEN MATCHED THEN
    UPDATE SET target.Title = source.Title, target.Video_Url = source.Video_Url, target.Thumbnail_Url = source.Thumbnail_Url, target.Status = source.Status, target.Duration_Seconds = source.Duration_Seconds
WHEN NOT MATCHED THEN
    INSERT (Video_ID, Post_ID, Title, Video_Url, Thumbnail_Url, Duration_Seconds, View_Count, Status, Created_At)
    VALUES (source.Video_ID, source.Post_ID, source.Title, source.Video_Url, source.Thumbnail_Url, source.Duration_Seconds, source.View_Count, source.Status, source.Created_At);

SET IDENTITY_INSERT dbo.Videos OFF;
GO

SET IDENTITY_INSERT dbo.VideoTranscripts ON;

MERGE INTO dbo.VideoTranscripts AS target
USING (
    VALUES
        -- Video 2101: Canh Chua Chay Nam Bộ
        (3101, 2101, NULL,
         N'Chào mừng các bạn đến với gian bếp VeggieLife. Hôm nay mình sẽ hướng dẫn nấu món Canh chua chay chuẩn vị Nam Bộ thanh mát giải nhiệt. Nguyên liệu gồm đậu hũ chiên cắt khối, thơm, cà chua, đậu bắp, bạc hà, giá đỗ và me chua dầm lấy nước cốt. Bắt đầu phi thơm hành bo-rô, cho cà chua và thơm vào xào nhẹ để tạo màu đỏ cam tự nhiên và tiết vị chua ngọt. Châm 800ml nước dùng rau củ đun sôi, rót nước cốt me vào nêm đường phèn, muối hầm và chút hạt nêm nấm hương. Thả đậu hũ, đậu bắp và bạc hà đun sôi bùng trong 2 phút, cuối cùng cho giá và ngò ôm ngò gai rồi tắt bếp. Múc canh ra tô, rắc chút ớt sừng cắt lát. Vị chua thanh của me hòa cùng rau củ giòn ngọt cực kỳ thanh mát đưa cơm.',
         N'{"segments":[{"start":0.0,"end":25.0,"text":"Chào mừng các bạn đến với gian bếp VeggieLife. Hôm nay mình sẽ hướng dẫn nấu món Canh chua chay chuẩn vị Nam Bộ thanh mát giải nhiệt."},{"start":25.0,"end":55.0,"text":"Nguyên liệu gồm đậu hũ chiên cắt khối, thơm, cà chua, đậu bắp, bạc hà, giá đỗ và me chua dầm lấy nước cốt."},{"start":55.0,"end":85.0,"text":"Bắt đầu phi thơm hành bo-rô, cho cà chua và thơm vào xào nhẹ để tạo màu đỏ cam tự nhiên và tiết vị chua ngọt."},{"start":85.0,"end":130.0,"text":"Châm 800ml nước dùng rau củ đun sôi, rót nước cốt me vào nêm đường phèn, muối hầm và chút hạt nêm nấm hương."},{"start":130.0,"end":160.0,"text":"Thả đậu hũ, đậu bắp và bạc hà đun sôi bùng trong 2 phút, cuối cùng cho giá và ngò ôm ngò gai rồi tắt bếp."},{"start":160.0,"end":180.0,"text":"Múc canh ra tô, rắc chút ớt sừng cắt lát. Vị chua thanh của me hòa cùng rau củ giòn ngọt cực kỳ thanh mát đưa cơm."}],"recipe":{"title":"Canh chua chay Nam Bộ","servings":4,"cookTimeMinutes":20,"ingredients":[{"name":"Đậu hũ chiên vàng","quantity":2,"unit":"miếng"},{"name":"Cà chua chín","quantity":2,"unit":"quả"},{"name":"Thơm (Dứa)","quantity":0.25,"unit":"quả"},{"name":"Đậu bắp tươi","quantity":5,"unit":"quả"},{"name":"Bạc hà (Dọc mùng)","quantity":1,"unit":"nhánh"},{"name":"Giá đỗ","quantity":100,"unit":"g"},{"name":"Me chua chín","quantity":30,"unit":"g"},{"name":"Ngò ôm, ngò gai, boa-rô","quantity":30,"unit":"g"}],"steps":["Bước 1: Sơ chế các loại rau củ, cà chua cắt múi cau, thơm cắt lát mỏng, bạc hà tước vỏ cắt xéo bóp muối rửa sạch.","Bước 2: Dầm me với 100ml nước sôi lọc lấy nước cốt chua thanh.","Bước 3: Phi thơm boa-rô với 1 muỗng dầu thực vật, cho thơm và cà chua vào xào sơ 2 phút.","Bước 4: Châm 800ml nước dùng rau củ đun sôi, cho nước cốt me, nêm 1.5 muỗng đường phèn và 1 muỗng muối hầm.","Bước 5: Thả đậu hũ chiên, đậu bắp, bạc hà đun sôi bùng 2 phút, tắt bếp cho giá đỗ và rau thơm lên trên."],"tips":["Dầm me bằng nước sôi để lấy hết vị chua thanh; không nấu bạc hà quá lâu để giữ độ giòn."]}}',
         N'Canh chua chay Nam Bộ (Khẩu phần: 4 người, 8 nguyên liệu, 5 bước chế biến)',
         'faster-whisper-small-int8', 'COMPLETED', DATEADD(DAY, -14, SYSDATETIME()), DATEADD(DAY, -14, SYSDATETIME())),

        -- Video 2102: Nấm Đùi Gà Kho Tiêu Hạt Sen
        (3102, 2102, NULL,
         N'Hôm nay chúng ta cùng thực hiện món Nấm đùi gà kho tiêu hạt sen - món mặn chay đưa cơm số một cho cả nhà. Nấm đùi gà rửa sạch, cắt khoanh tròn 2cm rồi khía vảy rồng hai mặt để khi kho nấm thấm đều gia vị. Ướp nấm với 2 muỗng nước tương, 1 muỗng hắc xì dầu, nửa muỗng đường thốt nốt và nhiều tiêu đen đập dập. Đun nóng chút dầu mè trong nồi đất, cho nấm vào áp chảo hai mặt vàng xém rồi trút toàn bộ nước ướp cùng 50ml nước dừa tươi. Kho trên lửa nhỏ liu riu 15 phút cho nước sốt keo sánh lại, áo đều lên từng miếng nấm nâu bóng bẩy.',
         N'{"segments":[{"start":0.0,"end":20.0,"text":"Hôm nay chúng ta cùng thực hiện món Nấm đùi gà kho tiêu hạt sen - món mặn chay đưa cơm số một cho cả nhà."},{"start":20.0,"end":50.0,"text":"Nấm đùi gà rửa sạch, cắt khoanh tròn 2cm rồi khía vảy rồng hai mặt để khi kho nấm thấm đều gia vị."},{"start":50.0,"end":80.0,"text":"Ướp nấm với 2 muỗng nước tương, 1 muỗng hắc xì dầu, nửa muỗng đường thốt nốt và nhiều tiêu đen đập dập."},{"start":80.0,"end":120.0,"text":"Đun nóng chút dầu mè trong nồi đất, cho nấm vào áp chảo hai mặt vàng xém rồi trút toàn bộ nước ướp cùng 50ml nước dừa tươi."},{"start":120.0,"end":150.0,"text":"Kho trên lửa nhỏ liu riu 15 phút cho nước sốt keo sánh lại, áo đều lên từng miếng nấm nâu bóng bẩy."}],"recipe":{"title":"Nấm đùi gà kho tiêu hạt sen","servings":3,"cookTimeMinutes":25,"ingredients":[{"name":"Nấm đùi gà tươi","quantity":300,"unit":"g"},{"name":"Hạt sen tươi luộc chín","quantity":80,"unit":"g"},{"name":"Nước tương tamari","quantity":30,"unit":"ml"},{"name":"Hắc xì dầu","quantity":10,"unit":"ml"},{"name":"Nước dừa xiêm","quantity":100,"unit":"ml"},{"name":"Tiêu đen sọ đập dập","quantity":10,"unit":"g"},{"name":"Dầu mè thơm","quantity":10,"unit":"ml"}],"steps":["Bước 1: Cắt nấm đùi gà khoanh dày 2cm, dùng dao khía ca-rô hai mặt nấm.","Bước 2: Ướp nấm với nước tương, tiêu, đường thốt nốt trong 15 phút.","Bước 3: Cho dầu mè vào nồi đất, áp chảo vàng nhẹ 2 mặt nấm.","Bước 4: Đổ nước ướp và nước dừa vào đun sôi, hạ lửa nhỏ kho liu riu cùng hạt sen.","Bước 5: Khi nước sốt keo sánh lại sền sệt, rắc thêm tiêu sọ và hành boa-rô lên trên."],"tips":["Nên kho bằng nồi đất nung để nhiệt phân bổ đều và nước sốt sánh kẹo tự nhiên."]}}',
         N'Nấm đùi gà kho tiêu hạt sen (Khẩu phần: 3 người, 7 nguyên liệu, 5 bước chế biến)',
         'faster-whisper-small-int8', 'COMPLETED', DATEADD(DAY, -12, SYSDATETIME()), DATEADD(DAY, -12, SYSDATETIME())),

        -- Video 2103: Phở Bò Chay Hà Nội
        (3103, 2103, NULL,
         N'Chào mừng các bạn đến với công thức phở chay Hà Nội. Điểm nhấn là gói gia vị thảo mộc gồm quế, hoa hồi, thảo quả được nướng thơm trước khi thả vào nồi nước dùng. Nước dùng hầm 2 tiếng từ củ cải trắng, củ sắn, mía lau, bắp ngô ngọt tạo độ ngọt thanh sâu lắng. Phần thịt bò chay làm từ nấm đùi gà xé sợi và váng đậu ky chiên phồng ướp sốt hạt điều màu. Trần bánh phở tươi cho vào tô, xếp nấm, chả chay, rau mùi húng quế và chan nước dùng nóng hổi nghi ngút khói.',
         N'{"segments":[{"start":0.0,"end":30.0,"text":"Phở chay làm sao để có vị ngọt sâu và hương thơm đặc trưng của phở truyền thống? Bí quyết nằm ở nồi nước dùng thảo mộc."},{"start":30.0,"end":70.0,"text":"Hầm 2 tiếng củ cải trắng, củ sắn, mía lau, bắp ngô ngọt và một củ hành tây nướng cháy xém vỏ ngoài."},{"start":70.0,"end":105.0,"text":"Rang thơm hoa hồi, thảo quả, quế thanh, tiểu hồi và hạt ngò, bọc vào túi vải rồi thả vào nồi nước dùng."},{"start":105.0,"end":150.0,"text":"Phần thịt bò chay làm từ nấm đùi gà xé sợi và tàu hũ ky ướp dầu điều xào săn thơm phức."},{"start":150.0,"end":210.0,"text":"Trần bánh phở tươi, xếp nấm, chả nấm, hành tây cắt mỏng, rau húng quế và chan nước dùng nóng hổi thơm nức mũi."}],"recipe":{"title":"Phở chay Hà Nội nước dùng thảo mộc","servings":4,"cookTimeMinutes":45,"ingredients":[{"name":"Bánh phở tươi","quantity":500,"unit":"g"},{"name":"Nấm đùi gà xé sợi","quantity":200,"unit":"g"},{"name":"Tàu hũ ky chiên giòn","quantity":50,"unit":"g"},{"name":"Gói thảo mộc phở (hồi, quế, thảo quả)","quantity":1,"unit":"gói"},{"name":"Mía lau, bắp ngọt, củ sắn","quantity":500,"unit":"g"},{"name":"Rau thơm (ngò gai, húng quế, giá)","quantity":150,"unit":"g"}],"steps":["Bước 1: Nướng thơm hành tây, gừng, hồi, quế, thảo quả trên bếp than hoặc chảo gang.","Bước 2: Hầm mía lau, bắp ngọt, củ sắn cùng túi thảo mộc trong 2 lít nước suốt 60 phút.","Bước 3: Xào săn nấm đùi gà xé sợi với chút dầu điều và nước tương làm nhân thịt phở.","Bước 4: Nêm nước dùng với muối hầm, đường phèn và chút tương hột xay nhuyễn.","Bước 5: Chần bánh phở qua nước sôi, xếp vào tô cùng nấm, tàu hũ ky rồi chan nước dùng đang sôi sùng sục."],"tips":["Nướng thảo mộc vừa tới thơm, không để cháy đen để nước dùng không bị đắng và đục."]}}',
         N'Phở chay Hà Nội nước dùng thảo mộc (Khẩu phần: 4 người, 6 nguyên liệu, 5 bước chế biến)',
         'faster-whisper-small-int8', 'COMPLETED', DATEADD(DAY, -10, SYSDATETIME()), DATEADD(DAY, -10, SYSDATETIME())),

        -- Video 2104: Gỏi Cuốn Ngũ Sắc Sốt Bơ Đậu Phộng
        (3104, 2104, NULL,
         N'Bữa trưa hè rực rỡ với Gỏi cuốn ngũ sắc thanh mát cùng nước chấm sốt tương bơ đậu phộng béo bùi gây nghiện. Các loại rau củ thái sợi dài: bắp cải tím, dưa chuột, cà rốt, xoài xanh, ớt chuông đỏ và đậu hũ chiên giòn. Làm ướt bánh tráng gạo mè, xếp lần lượt rau sống, bún tươi, các loại rau ngũ sắc và cuốn chặt tay đều mí. Công thức sốt thần thánh: 2 muỗng bơ đậu phộng mịn, 1 muỗng tương đen hoisin, 1 muỗng nước cốt dừa và chút ớt băm.',
         N'{"segments":[{"start":0.0,"end":25.0,"text":"Bữa trưa hè rực rỡ với Gỏi cuốn ngũ sắc thanh mát cùng nước chấm sốt tương bơ đậu phộng gây nghiện."},{"start":25.0,"end":55.0,"text":"Các loại rau củ thái sợi dài: bắp cải tím, dưa chuột, cà rốt, xoài xanh, ớt chuông đỏ và đậu hũ chiên giòn."},{"start":55.0,"end":90.0,"text":"Làm ướt bánh tráng gạo mè, xếp lần lượt rau sống, bún tươi, các loại rau ngũ sắc và cuốn chặt tay đều mí."},{"start":90.0,"end":120.0,"text":"Công thức sốt thần thánh: 2 muỗng bơ đậu phộng mịn, 1 muỗng tương đen hoisin, 1 muỗng nước cốt dừa và chút ớt băm."}],"recipe":{"title":"Gỏi cuốn ngũ sắc sốt bơ đậu phộng","servings":3,"cookTimeMinutes":15,"ingredients":[{"name":"Bánh tráng dẻo gạo mè","quantity":1,"unit":"xấp"},{"name":"Bún tươi sợi nhỏ","quantity":200,"unit":"g"},{"name":"Đậu hũ chiên cắt thanh","quantity":2,"unit":"miếng"},{"name":"Bắp cải tím bào sợi","quantity":80,"unit":"g"},{"name":"Dưa leo và cà rốt thái sợi","quantity":150,"unit":"g"},{"name":"Bơ đậu phộng mịn","quantity":50,"unit":"g"},{"name":"Tương đen Hoisin và nước cốt dừa","quantity":40,"unit":"ml"}],"steps":["Bước 1: Rửa sạch và bào sợi các loại rau củ ngũ sắc để ráo nước.","Bước 2: Cắt đậu hũ chiên thành các thanh dài bằng ngón tay út.","Bước 3: Nhúng bánh tráng qua nước ấm, trải phẳng ra thớt, xếp xà lách, bún, rau ngũ sắc và đậu hũ.","Bước 4: Gấp 2 mép bên hông cuộn tròn thật chặt tay.","Bước 5: Nấu ấm bơ đậu phộng, tương hoisin và nước dừa thành hỗn hợp sốt mịn sánh rắc đậu phộng giã dập."],"tips":["Cuốn thật chặt tay từ vòng đầu tiên để gỏi cuốn căng tròn đẹp mắt khi cắt đôi."]}}',
         N'Gỏi cuốn ngũ sắc sốt bơ đậu phộng (Khẩu phần: 3 người, 7 nguyên liệu, 5 bước chế biến)',
         'faster-whisper-small-int8', 'COMPLETED', DATEADD(DAY, -8, SYSDATETIME()), DATEADD(DAY, -8, SYSDATETIME())),

        -- Video 2105: Cà Ri Chay Khoai Lang Đậu Gà
        (3105, 2105, NULL,
         N'Món Cà ri chay khoai lang đậu gà béo ngậy mùi nước cốt dừa, ăn kèm bánh mì nóng giòn ngày mưa se lạnh. Khoai lang mật và khoai môn cắt khối vuông chiên sơ lớp vỏ để khoai giữ nguyên hình dạng không bị nát nhừ. Phi sả cây đập dập cùng bột cà ri Ấn Độ cho dậy mùi, trút nấm hương và đậu gà đã luộc chín mềm vào đảo đều. Đổ nước dão dừa nấu sôi 10 phút, cho khoai vào ninh mềm, cuối cùng rưới chén cốt dừa đặc nguyên chất sánh ngậy. Nêm hạt nêm chay và muối hồng vừa miệng.',
         N'{"segments":[{"start":0.0,"end":30.0,"text":"Món Cà ri chay khoai lang đậu gà béo ngậy mùi nước cốt dừa, ăn kèm bánh mì nóng giòn ngày mưa se lạnh."},{"start":30.0,"end":60.0,"text":"Khoai lang mật và khoai môn cắt khối vuông chiên sơ lớp vỏ để khoai giữ nguyên hình dạng không bị nát nhừ."},{"start":60.0,"end":95.0,"text":"Phi sả cây đập dập cùng bột cà ri Ấn Độ cho dậy mùi, trút nấm hương và đậu gà đã luộc chín mềm vào đảo đều."},{"start":95.0,"end":135.0,"text":"Đổ nước dão dừa nấu sôi 10 phút, cho khoai vào ninh mềm, cuối cùng rưới chén cốt dừa đặc nguyên chất sánh ngậy."},{"start":135.0,"end":160.0,"text":"Nêm hạt nêm chay và muối hồng vừa miệng. Món ăn vàng ươm, thơm lừng ngào ngạt cả gian bếp."}],"recipe":{"title":"Cà ri chay khoai lang đậu gà cốt dừa","servings":4,"cookTimeMinutes":30,"ingredients":[{"name":"Khoai lang mật","quantity":250,"unit":"g"},{"name":"Khoai môn cao","quantity":200,"unit":"g"},{"name":"Đậu gà ngâm mềm luộc chín","quantity":150,"unit":"g"},{"name":"Nấm hương tươi","quantity":100,"unit":"g"},{"name":"Bột cà ri dầu và bột cà ri khô","quantity":2,"unit":"muỗng canh"},{"name":"Nước cốt dừa nguyên chất","quantity":200,"unit":"ml"},{"name":"Sả cây đập dập","quantity":4,"unit":"nhánh"}],"steps":["Bước 1: Khoai lang và khoai môn gọt vỏ, cắt khối vuông 3cm, chiên sơ vàng lớp ngoài.","Bước 2: Phi thơm sả đập dập cùng bột cà ri với chút dầu thực vật.","Bước 3: Cho nấm hương và đậu gà vào xào thấm gia vị cà ri trong 3 phút.","Bước 4: Châm 600ml nước dão dừa hầm 15 phút, sau đó cho khoai đã chiên vào ninh mềm.","Bước 5: Rưới nước cốt dừa đặc vào khuấy nhẹ tay, đun sôi liu riu nêm nếm vừa ăn rồi tắt bếp."],"tips":["Chiên sơ khoai giúp khoai thơm bùi hơn và không bị nát làm đục nồi sốt cà ri."]}}',
         N'Cà ri chay khoai lang đậu gà cốt dừa (Khẩu phần: 4 người, 7 nguyên liệu, 5 bước chế biến)',
         'faster-whisper-small-int8', 'COMPLETED', DATEADD(DAY, -6, SYSDATETIME()), DATEADD(DAY, -6, SYSDATETIME())),

        -- Video 2106: Bánh Xèo Chay Giòn Rụm Củ Hũ Dừa
        (3106, 2106, NULL,
         N'Cách làm Bánh xèo miền Tây thuần chay mỏng giòn rụm cả tiếng đồng hồ không ỉu nhờ bí quyết pha bột chuẩn. Pha bột bánh xèo cùng nước cốt dừa béo ngậy, một lon nước có ga để tạo độ xốp giòn và hành boa-rô thái nhuyễn. Nhân bánh gồm củ hũ dừa bào sợi ngọt giòn, nấm hương tươi thái mỏng, đậu xanh hấp chín và giá đỗ. Làm nóng chảo gang thật bốc khói, tráng một lớp dầu mỏng, trút vá bột xoay đều viền chảo nghe tiếng xèo vang giòn. Rải đều nhân nấm đậy nắp 2 phút, mở nắp nhỏ vài giọt dầu quanh rìa bánh tới khi viền bánh giòn cong lên gấp đôi lại.',
         N'{"segments":[{"start":0.0,"end":30.0,"text":"Cách làm Bánh xèo miền Tây thuần chay mỏng giòn rụm cả tiếng đồng hồ không ỉu nhờ bí quyết pha bột chuẩn."},{"start":30.0,"end":60.0,"text":"Pha bột bánh xèo cùng nước cốt dừa béo ngậy, một lon nước có ga để tạo độ xốp giòn và hành boa-rô thái nhuyễn."},{"start":60.0,"end":95.0,"text":"Nhân bánh gồm củ hũ dừa bào sợi ngọt giòn, nấm hương tươi thái mỏng, đậu xanh hấp chín và giá đỗ."},{"start":95.0,"end":140.0,"text":"Làm nóng chảo gang thật bốc khói, tráng một lớp dầu mỏng, trút vá bột xoay đều viền chảo nghe tiếng xèo vang giòn."},{"start":140.0,"end":190.0,"text":"Rải đều nhân nấm đậy nắp 2 phút, mở nắp nhỏ vài giọt dầu quanh rìa bánh tới khi viền bánh giòn cong lên gấp đôi lại."}],"recipe":{"title":"Bánh xèo chay giòn rụm củ hũ dừa","servings":4,"cookTimeMinutes":35,"ingredients":[{"name":"Bột bánh xèo hương xưa","quantity":400,"unit":"g"},{"name":"Nước cốt dừa","quantity":150,"unit":"ml"},{"name":"Nước khoáng có ga (soda)","quantity":330,"unit":"ml"},{"name":"Củ hũ dừa bào sợi","quantity":200,"unit":"g"},{"name":"Nấm hương tươi thái mỏng","quantity":150,"unit":"g"},{"name":"Đậu xanh hấp chín nguyên hạt","quantity":100,"unit":"g"},{"name":"Giá đỗ và rau sống các loại","quantity":300,"unit":"g"}],"steps":["Bước 1: Khuấy đều bột bánh xèo, nước cốt dừa, nước soda và boa-rô thái nhỏ, để bột nghỉ 30 phút.","Bước 2: Xào sơ nấm hương và củ hũ dừa nêm chút muối tiêu cho ráo nước.","Bước 3: Đun nóng chảo chống dính hoặc chảo gang với 1 muỗng dầu ăn thật nóng già.","Bước 4: Múc 1 vá bột đổ vào chảo, nghiêng chảo tráng thật mỏng đều khắp mặt chảo.","Bước 5: Rải nhân nấm, đậu xanh, giá đỗ đậy nắp 2 phút, mở nắp chiên lửa vừa tới khi viền bánh giòn rụm gập đôi."],"tips":["Nước có ga tạo bọt khí li ti giúp vỏ bánh mỏng xốp giòn lâu vượt trội."]}}',
         N'Bánh xèo chay giòn rụm củ hũ dừa (Khẩu phần: 4 người, 7 nguyên liệu, 5 bước chế biến)',
         'faster-whisper-small-int8', 'COMPLETED', DATEADD(DAY, -4, SYSDATETIME()), DATEADD(DAY, -4, SYSDATETIME()))
) AS source (Transcript_ID, Video_ID, Recipe_ID, Raw_Transcript, Extracted_Recipe_JSON, AI_Summary, STT_Model_Version, Status, Created_At, Completed_At)
ON target.Transcript_ID = source.Transcript_ID
WHEN MATCHED THEN
    UPDATE SET target.Raw_Transcript = source.Raw_Transcript, target.Extracted_Recipe_JSON = source.Extracted_Recipe_JSON,
               target.AI_Summary = source.AI_Summary, target.Status = source.Status, target.Completed_At = source.Completed_At
WHEN NOT MATCHED THEN
    INSERT (Transcript_ID, Video_ID, Recipe_ID, Raw_Transcript, Extracted_Recipe_JSON, AI_Summary, STT_Model_Version, Status, Created_At, Completed_At)
    VALUES (source.Transcript_ID, source.Video_ID, source.Recipe_ID, source.Raw_Transcript, source.Extracted_Recipe_JSON, source.AI_Summary, source.STT_Model_Version, source.Status, source.Created_At, source.Completed_At);

SET IDENTITY_INSERT dbo.VideoTranscripts OFF;
GO

-- ============================================================================
-- 4. NẠP 25 BÌNH LUẬN (COMMENTS) TƯƠNG TÁC ĐA TẦNG
-- ============================================================================
PRINT N'>> [4/5] Nạp 25 bình luận thảo luận chân thật và phản hồi đa tầng...';
SET IDENTITY_INSERT dbo.Comments ON;

MERGE INTO dbo.Comments AS target
USING (
    VALUES
        -- Post 1021: Hành trình 30 ngày thuần chay
        (4001, 11, NULL, 1021, N'Bài viết quá hữu ích bác sĩ ơi! Em chuyển ăn chay được 2 tuần đầu bị mệt lả và thèm ngọt kinh khủng, may đọc được bài hướng dẫn bổ sung đậu gà và chuối nên giờ cơ thể rất nhẹ nhõm.', 0, 'ACTIVE', DATEADD(DAY, -14, SYSDATETIME()), NULL),
        (4002, 6, 4001, 1021, N'Chúc mừng em nhé! Cảm giác thèm ngọt 2 tuần đầu là do cơ thể đang thích nghi chuyển đổi nhiên liệu từ chất béo sang glucose chậm. Cứ duy trì hạt dinh dưỡng và quả mọng nha.', 0, 'ACTIVE', DATEADD(DAY, -14, SYSDATETIME()), NULL),
        (4003, 8, NULL, 1021, N'Chuẩn luôn ạ! Khi mới ăn chay nhiều bạn chỉ ăn rau luộc với đậu phụ rán nên bị thâm hụt calo và thiếu đạm trầm trọng, bảo sao không kiệt sức.', 0, 'ACTIVE', DATEADD(DAY, -13, SYSDATETIME()), NULL),

        -- Post 1022: Bổ sung B12, Sắt & Kẽm
        (4004, 9, NULL, 1022, N'Bác sĩ cho em hỏi liều B12 1000mcg uống 2 lần/tuần hay 500mcg mỗi ngày thì hấp thu tốt hơn ạ?', 0, 'ACTIVE', DATEADD(DAY, -12, SYSDATETIME()), NULL),
        (4005, 6, 4004, 1022, N'Cả hai phác đồ đều rất tốt em nhé. Uống 500mcg mỗi ngày dạng ngậm dưới lưỡi (Methylcobalamin) hấp thu thụ động rất ổn định cho người ăn chay trường.', 0, 'ACTIVE', DATEADD(DAY, -12, SYSDATETIME()), NULL),
        (4006, 12, NULL, 1022, N'Mình ăn chay 4 năm đi xét nghiệm máu định kỳ các chỉ số Sắt, Ferritin và B12 đều ở mức lý tưởng. Mẹo vắt chanh vào rau bina của bác sĩ cực kỳ chuẩn xác!', 0, 'ACTIVE', DATEADD(DAY, -11, SYSDATETIME()), NULL),

        -- Post 1023: Thực đơn Eat Clean 6 tuần
        (4007, 11, NULL, 1023, N'Em đã áp dụng thực đơn Eat Clean này được 3 tuần và giảm được 2.2kg mỡ bụng rồi chị Phương ơi! Không hề bị đói lả như những lần nhịn ăn trước.', 0, 'ACTIVE', DATEADD(DAY, -10, SYSDATETIME()), NULL),
        (4008, 9, 4007, 1023, N'Tuyệt vời quá em! Nhớ uống đủ 2-2.5 lít nước mỗi ngày và duy trì 30 phút yoga buổi sáng để cơ thể đào thải độc tố tốt hơn nha.', 0, 'ACTIVE', DATEADD(DAY, -10, SYSDATETIME()), NULL),
        (4009, 14, NULL, 1023, N'Đĩa ăn 50% chất xơ và 25% đạm thực vật này nhìn rất bắt mắt, ăn ngon miệng không hề thấy nhàm chán.', 0, 'ACTIVE', DATEADD(DAY, -9, SYSDATETIME()), NULL),

        -- Post 1024: Mẹo Meal Prep cho dân văn phòng
        (4010, 13, NULL, 1024, N'Bí quyết bọc khăn giấy vào hộp rau của Bếp trưởng đỉnh thật! Xà lách với ngò rí để 10 ngày trong ngăn mát vẫn xanh mướt giòn tan.', 0, 'ACTIVE', DATEADD(DAY, -8, SYSDATETIME()), NULL),
        (4011, 2, 4010, 1024, N'Cảm ơn bạn! Lưu ý rau phải thật ráo nước trước khi bọc nhé, chỉ cần một giọt nước đọng là rau dễ bị thối nhũn ngay đấy.', 0, 'ACTIVE', DATEADD(DAY, -8, SYSDATETIME()), NULL),
        (4012, 15, NULL, 1024, N'Chef cho em hỏi công thức sốt mè rang bảo quản được bao lâu trong tủ lạnh ạ?', 0, 'ACTIVE', DATEADD(DAY, -8, SYSDATETIME()), NULL),
        (4013, 2, 4012, 1024, N'Đựng trong hũ thủy tinh tiệt trùng để ngăn mát tủ lạnh dùng thoải mái trong 10 đến 14 ngày em nhé.', 0, 'ACTIVE', DATEADD(DAY, -7, SYSDATETIME()), NULL),

        -- Post 1025: Cẩm nang ăn tiệc & du lịch
        (4014, 8, NULL, 1025, N'Kinh nghiệm sinh tồn quá chuẩn! Đi liên hoan công ty mình toàn gọi nấm nướng muối ớt, đậu hũ lướt ván và rau luộc chấm kho quẹt chay, vừa vui vẻ vừa thanh đạm.', 0, 'ACTIVE', DATEADD(DAY, -6, SYSDATETIME()), NULL),
        (4015, 10, 4014, 1025, N'Đúng rồi bạn, quan trọng là tâm thế cởi mở vui vẻ không phán xét, bạn bè đồng nghiệp xung quanh cũng sẽ rất tôn trọng và ủng hộ mình.', 0, 'ACTIVE', DATEADD(DAY, -6, SYSDATETIME()), NULL),

        -- Post 1026: Làn da sạch mụn sau 90 ngày
        (4016, 9, NULL, 1026, N'Nhìn ảnh da của bạn trước và sau 3 tháng mà ngỡ ngàng! Mình cũng xác nhận từ ngày bỏ hẳn sữa bò là mụn lưng và mụn nội tiết biến mất 100%.', 0, 'ACTIVE', DATEADD(DAY, -5, SYSDATETIME()), NULL),
        (4017, 11, 4016, 1026, N'Dạ đúng rồi chị, sữa bò chứa nhiều hormone làm tuyến bã nhờn hoạt động quá mức, chuyển qua sữa hạt tự làm da dẻ căng bóng hẳn.', 0, 'ACTIVE', DATEADD(DAY, -4, SYSDATETIME()), NULL),

        -- Post 1027: Nước dùng chay 5 loại củ quả
        (4018, 4, NULL, 1027, N'Thầy cũng luôn dùng công thức mía lau và bắp ngọt để ninh nước lèo bún chay cho chùa. Vị ngọt thanh tao không bao giờ bị khát nước như hạt nêm công nghiệp.', 0, 'ACTIVE', DATEADD(DAY, -3, SYSDATETIME()), NULL),
        (4019, 3, 4018, 1027, N'Dạ bạch Thầy, nướng sơ củ sắn và nấm hương trước khi ninh sẽ giúp nước dùng thơm sâu và có màu hổ phách rất đẹp mắt nữa ạ.', 0, 'ACTIVE', DATEADD(DAY, -2, SYSDATETIME()), NULL),

        -- Post 1028: Tập gym tăng cơ với thực vật
        (4020, 12, NULL, 1028, N'Anh Nam cho em hỏi Seitan tự làm từ bột mì số 11 có bị khó tiêu không ạ? Em tập tạ ngày ăn khoảng 150g.', 0, 'ACTIVE', DATEADD(DAY, -2, SYSDATETIME()), NULL),
        (4021, 8, 4020, 1028, N'Nếu em không bị dị ứng Gluten (Celiac) thì ăn hoàn toàn bình thường nhé. Khi luộc nhớ cho chút gừng và sả để trợ tiêu hóa tốt hơn.', 0, 'ACTIVE', DATEADD(DAY, -1, SYSDATETIME()), NULL),

        -- Post 1029: Ăn chay chánh niệm
        (4022, 15, NULL, 1029, N'Đọc bài của cô Tuệ An thấy lòng nhẹ nhàng bình yên quá. Đúng là bữa cơm chay ăn trong chánh niệm thì từng miếng đậu, cọng rau đều thơm ngọt lạ kỳ.', 0, 'ACTIVE', DATEADD(DAY, -1, SYSDATETIME()), NULL),

        -- Post 1030: Nuôi con ăn chay đủ chất
        (4023, 6, NULL, 1030, N'Bài viết rất khoa học và thực tế! Rất nhiều mẹ lo con ăn chay bị lùn hay thiếu chất, nhưng thực tế canxi trong mè đen, đậu hũ và rau cải xoăn còn dễ hấp thu hơn sữa bò nếu kết hợp đủ Vitamin D3-K2.', 0, 'ACTIVE', DATEADD(HOUR, -12, SYSDATETIME()), NULL),
        (4024, 7, 4023, 1030, N'Dạ cảm ơn bác sĩ Lan! Hai bé nhà em trộm vía chiều cao luôn đứng top đầu của lớp, sức đề kháng rất tốt ít khi ốm vặt.', 0, 'ACTIVE', DATEADD(HOUR, -6, SYSDATETIME()), NULL),

        -- Post 1011: Video Canh chua chay
        (4025, 14, NULL, 1011, N'Vừa nấu thử nồi canh chua theo đúng video hướng dẫn trưa nay, cả nhà ăn khen nức nở! Nước canh trong veo ngọt lịm. Cảm ơn Chef Thanh Hòa rất nhiều!', 0, 'ACTIVE', DATEADD(HOUR, -2, SYSDATETIME()), NULL)
) AS source (Comment_ID, User_ID, Parent_Comment_ID, Post_ID, Content, Is_Soft_Deleted, Status, Created_At, Updated_At)
ON target.Comment_ID = source.Comment_ID
WHEN MATCHED THEN
    UPDATE SET target.Content = source.Content, target.Status = source.Status
WHEN NOT MATCHED THEN
    INSERT (Comment_ID, User_ID, Parent_Comment_ID, Post_ID, Content, Is_Soft_Deleted, Status, Created_At, Updated_At)
    VALUES (source.Comment_ID, source.User_ID, source.Parent_Comment_ID, source.Post_ID, source.Content, source.Is_Soft_Deleted, source.Status, source.Created_At, source.Updated_At);

SET IDENTITY_INSERT dbo.Comments OFF;
GO

-- ============================================================================
-- 5. NẠP LƯỢT VOTE TÍCH CỰC CHO BÀI VIẾT (VOTES - MỖI USER LIKE 1 LẦN/BÀI)
-- ============================================================================
PRINT N'>> [5/5] Nạp các lượt Vote tích cực tự nhiên cho bài viết...';

MERGE INTO dbo.Votes AS target
USING (
    VALUES
        -- Post 1011: Canh chua chay
        (1, 1011), (2, 1011), (3, 1011), (4, 1011), (5, 1011), (6, 1011), (7, 1011), (8, 1011), (9, 1011), (10, 1011), (11, 1011), (12, 1011), (13, 1011), (14, 1011), (15, 1011),
        -- Post 1012: Nấm kho tiêu
        (1, 1012), (2, 1012), (3, 1012), (4, 1012), (5, 1012), (6, 1012), (7, 1012), (8, 1012), (9, 1012), (10, 1012), (11, 1012), (12, 1012), (13, 1012), (14, 1012), (15, 1012),
        -- Post 1013: Phở chay
        (1, 1013), (2, 1013), (3, 1013), (4, 1013), (5, 1013), (6, 1013), (7, 1013), (8, 1013), (9, 1013), (10, 1013), (11, 1013), (12, 1013), (13, 1013), (14, 1013), (15, 1013),
        -- Post 1014: Gỏi cuốn
        (1, 1014), (2, 1014), (3, 1014), (4, 1014), (5, 1014), (6, 1014), (7, 1014), (8, 1014), (9, 1014), (10, 1014), (11, 1014), (12, 1014), (13, 1014), (14, 1014), (15, 1014),
        -- Post 1015: Cà ri chay
        (1, 1015), (2, 1015), (3, 1015), (4, 1015), (5, 1015), (6, 1015), (7, 1015), (8, 1015), (9, 1015), (10, 1015), (11, 1015), (12, 1015), (13, 1015), (14, 1015), (15, 1015),
        -- Post 1016: Bánh xèo chay
        (1, 1016), (2, 1016), (3, 1016), (4, 1016), (5, 1016), (6, 1016), (7, 1016), (8, 1016), (9, 1016), (10, 1016), (11, 1016), (12, 1016), (13, 1016), (14, 1016), (15, 1016),

        -- Post 1021: 30 ngày thuần chay
        (1, 1021), (2, 1021), (3, 1021), (4, 1021), (5, 1021), (6, 1021), (7, 1021), (8, 1021), (9, 1021), (10, 1021), (11, 1021), (12, 1021), (13, 1021), (14, 1021), (15, 1021),
        -- Post 1022: Vitamin B12, Sắt & Kẽm
        (1, 1022), (2, 1022), (3, 1022), (4, 1022), (5, 1022), (6, 1022), (7, 1022), (8, 1022), (9, 1022), (10, 1022), (11, 1022), (12, 1022), (13, 1022), (14, 1022), (15, 1022),
        -- Post 1023: Thực đơn Eat Clean
        (1, 1023), (2, 1023), (3, 1023), (4, 1023), (5, 1023), (6, 1023), (7, 1023), (8, 1023), (9, 1023), (10, 1023), (11, 1023), (12, 1023), (13, 1023), (14, 1023), (15, 1023),
        -- Post 1024: Mẹo Meal Prep
        (1, 1024), (2, 1024), (3, 1024), (4, 1024), (5, 1024), (6, 1024), (7, 1024), (8, 1024), (9, 1024), (10, 1024), (11, 1024), (12, 1024), (13, 1024), (14, 1024), (15, 1024),
        -- Post 1025: Cẩm nang ăn tiệc
        (1, 1025), (2, 1025), (3, 1025), (4, 1025), (5, 1025), (6, 1025), (7, 1025), (8, 1025), (9, 1025), (10, 1025), (11, 1025), (12, 1025), (13, 1025), (14, 1025), (15, 1025),
        -- Post 1026: Làn da sạch mụn
        (1, 1026), (2, 1026), (3, 1026), (4, 1026), (5, 1026), (6, 1026), (7, 1026), (8, 1026), (9, 1026), (10, 1026), (11, 1026), (12, 1026), (13, 1026), (14, 1026), (15, 1026),
        -- Post 1027: Nước dùng chay 5 củ quả
        (1, 1027), (2, 1027), (3, 1027), (4, 1027), (5, 1027), (6, 1027), (7, 1027), (8, 1027), (9, 1027), (10, 1027), (11, 1027), (12, 1027), (13, 1027), (14, 1027), (15, 1027),
        -- Post 1028: Tập gym tăng cơ với thực vật
        (1, 1028), (2, 1028), (3, 1028), (4, 1028), (5, 1028), (6, 1028), (7, 1028), (8, 1028), (9, 1028), (10, 1028), (11, 1028), (12, 1028), (13, 1028), (14, 1028), (15, 1028),
        -- Post 1029: Ăn chay chánh niệm
        (1, 1029), (2, 1029), (3, 1029), (4, 1029), (5, 1029), (6, 1029), (7, 1029), (8, 1029), (9, 1029), (10, 1029), (11, 1029), (12, 1029), (13, 1029), (14, 1029), (15, 1029),
        -- Post 1030: Nuôi con ăn chay
        (1, 1030), (2, 1030), (3, 1030), (4, 1030), (5, 1030), (6, 1030), (7, 1030), (8, 1030), (9, 1030), (10, 1030), (11, 1030), (12, 1030), (13, 1030), (14, 1030), (15, 1030)
) AS source (User_ID, Post_ID)
ON target.User_ID = source.User_ID AND target.Post_ID = source.Post_ID
WHEN NOT MATCHED THEN
    INSERT (User_ID, Post_ID, Created_At)
    VALUES (source.User_ID, source.Post_ID, SYSDATETIME());
GO

PRINT N'============================================================================';
PRINT N'  HOÀN TẤT NẠP DỮ LIỆU VIDEO NẤU ĂN & BÀI VIẾT CỘNG ĐỒNG THÀNH CÔNG!';
PRINT N'============================================================================';
GO

