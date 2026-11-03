using System;
using System.Collections.Generic;
using System.Linq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.API.Extensions;

/// <summary>
/// Seeder nạp dữ liệu chuẩn cho Video dạy nấu ăn (/videos) và Bài viết chia sẻ kinh nghiệm ăn chay (/community).
/// Hỗ trợ cả SQL Server và In-Memory Database khi khởi chạy môi trường phát triển / kiểm thử.
/// </summary>
public static class DevCommunityDataSeeder
{
    private const string DefaultPasswordHash = "$2a$11$GBRajsAbFfn4G4UI7bhu.O8MGs4mXfn/clcozGAZ0npIHYrlShxwO"; // Veggie@2026

    public static void Seed(AppDbContext db)
    {
        // Tránh nạp trùng lặp nếu đã nạp trước đó
        if (db.Posts.Any(p => p.PostId == 1011))
        {
            return;
        }

        var now = DateTime.UtcNow;

        // 1. Đảm bảo đầy đủ 15 tài khoản tác giả & hội viên thực tế
        var authors = new List<User>
        {
            new() { UserId = 4, Email = "chef.tuean@veggielife.vn", FullName = "Nghệ Nhân Tuệ An Chay", PasswordHash = DefaultPasswordHash, Role = UserRoles.Vip, AccountStatus = AccountStatuses.Active, CreatedAt = now.AddDays(-135) },
            new() { UserId = 5, Email = "chef.minhduc@veggielife.vn", FullName = "Chuyên Gia Ẩm Thực Minh Đức", PasswordHash = DefaultPasswordHash, Role = UserRoles.Vip, AccountStatus = AccountStatuses.Active, CreatedAt = now.AddDays(-130) },
            new() { UserId = 6, Email = "lelan.nutrition@gmail.com", FullName = "Bs. Lê Lan Dinh Dưỡng", PasswordHash = DefaultPasswordHash, Role = UserRoles.Vip, AccountStatus = AccountStatuses.Active, CreatedAt = now.AddDays(-120) },
            new() { UserId = 7, Email = "quangtri.buddhist@gmail.com", FullName = "Phật Tử Quảng Trí", PasswordHash = DefaultPasswordHash, Role = UserRoles.Vip, AccountStatus = AccountStatuses.Active, CreatedAt = now.AddDays(-115) },
            new() { UserId = 8, Email = "hoangnam.fitvegan@gmail.com", FullName = "HLV. Hoàng Nam Fitness Vegan", PasswordHash = DefaultPasswordHash, Role = UserRoles.Vip, AccountStatus = AccountStatuses.Active, CreatedAt = now.AddDays(-110) },
            new() { UserId = 9, Email = "maiphuong.yoga@gmail.com", FullName = "Mai Phương Yoga Life", PasswordHash = DefaultPasswordHash, Role = UserRoles.Vip, AccountStatus = AccountStatuses.Active, CreatedAt = now.AddDays(-100) },
            new() { UserId = 10, Email = "phamdinh.green@gmail.com", FullName = "Phạm Đình Sống Xanh", PasswordHash = DefaultPasswordHash, Role = UserRoles.Vip, AccountStatus = AccountStatuses.Active, CreatedAt = now.AddDays(-95) },
            new() { UserId = 11, Email = "lananh.healthy@veggielife.vn", FullName = "Lan Anh Healthy Food", PasswordHash = DefaultPasswordHash, Role = UserRoles.Member, AccountStatus = AccountStatuses.Active, CreatedAt = now.AddDays(-90) },
            new() { UserId = 12, Email = "minhtri.plantbased@veggielife.vn", FullName = "Minh Trí Plant-Based", PasswordHash = DefaultPasswordHash, Role = UserRoles.Member, AccountStatus = AccountStatuses.Active, CreatedAt = now.AddDays(-85) },
            new() { UserId = 13, Email = "thutrang.dr@gmail.com", FullName = "Bác sĩ Thu Trang", PasswordHash = DefaultPasswordHash, Role = UserRoles.Member, AccountStatus = AccountStatuses.Active, CreatedAt = now.AddDays(-80) },
            new() { UserId = 14, Email = "tueduc.chay@gmail.com", FullName = "Bếp Chay Tuệ Đức", PasswordHash = DefaultPasswordHash, Role = UserRoles.Member, AccountStatus = AccountStatuses.Active, CreatedAt = now.AddDays(-75) },
            new() { UserId = 15, Email = "huonggiang.yoga@gmail.com", FullName = "Hương Giang Yoga", PasswordHash = DefaultPasswordHash, Role = UserRoles.Member, AccountStatus = AccountStatuses.Active, CreatedAt = now.AddDays(-70) }
        };

        foreach (var author in authors)
        {
            if (!db.Users.Any(u => u.UserId == author.UserId))
            {
                db.Users.Add(author);
            }
        }
        db.SaveChanges();

        // 2. Nạp 16 bài viết (6 Video dạy nấu ăn + 10 Chia sẻ kinh nghiệm ăn chay)
        var p1011 = new Post
        {
            PostId = 1011,
            UserId = 2,
            Title = "Hướng dẫn nấu Canh chua chay Nam Bộ thanh mát giải nhiệt chuẩn vị",
            Content = "Hôm nay gian bếp VeggieLife xin chia sẻ cùng cả nhà công thức Canh chua chay chuẩn vị Nam Bộ thanh mát cho ngày hè. Nước canh chua thanh tự nhiên nhờ me dầm nước sôi, ngọt dịu từ thơm (dứa) và cà chua xào thơm với boa-rô, đậu hũ chiên giòn béo bùi, bạc hà đậu bắp giòn rụm. Xem video hướng dẫn chi tiết từng mốc thời gian kèm công thức chuẩn mực bên dưới nhé!",
            ImageUrl = "https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=800&q=80",
            Visibility = "PUBLIC",
            ViewCount = 1420,
            ModerationStatus = "APPROVED_ACTIVE",
            CreatedAt = now.AddDays(-14)
        };

        var p1012 = new Post
        {
            PostId = 1012,
            UserId = 3,
            Title = "Bí quyết làm Nấm đùi gà kho tiêu hạt sen đậm đà óng ả cực đưa cơm",
            Content = "Món mặn \"quốc dân\" trong mâm cơm chay gia đình! Nấm đùi gà tươi cắt khoanh tròn 2cm, khía hoa vảy rồng hai mặt để ngấm trọn gia vị, áp chảo xém vàng rồi kho liu riu cùng nước dừa xiêm, hạt sen bùi béo và tiêu sọ Phú Quốc đập dập. Nước sốt nâu cánh gián sánh keo, đậm vị mặn ngọt hài hòa, chan cùng cơm gạo lứt nóng hổi thơm nức mũi.",
            ImageUrl = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80",
            Visibility = "PUBLIC",
            ViewCount = 2150,
            ModerationStatus = "APPROVED_ACTIVE",
            CreatedAt = now.AddDays(-12)
        };

        var p1013 = new Post
        {
            PostId = 1013,
            UserId = 4,
            Title = "Nấu Phở bò chay Hà Nội nước dùng trong veo, thảo mộc thơm lừng nức mũi",
            Content = "Phở chay làm sao để có vị ngọt sâu và hương thơm nồng nàn đặc trưng của phở truyền thống? Bí quyết nằm ở nồi nước dùng ninh 2 tiếng từ củ sắn, mía lau, củ cải trắng và bắp ngọt. Gói thảo mộc gồm hoa hồi, thảo quả, quế thanh, tiểu hồi rang vàng thơm lừng. \"Thịt nấm\" dai ngọt từ nấm đùi gà xé sợi xào hạt điều đỏ bắt mắt.",
            ImageUrl = "https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?auto=format&fit=crop&w=800&q=80",
            Visibility = "PUBLIC",
            ViewCount = 3890,
            ModerationStatus = "APPROVED_ACTIVE",
            CreatedAt = now.AddDays(-10)
        };

        var p1014 = new Post
        {
            PostId = 1014,
            UserId = 9,
            Title = "Gỏi cuốn ngũ sắc thanh lọc chấm sốt bơ đậu phộng béo bùi gây nghiện",
            Content = "Bữa trưa tươi mát đầy màu sắc và vitamin cho ngày hè bận rộn! Bánh tráng gạo mè dẻo dai cuốn chặt cùng bắp cải tím, dưa leo, xoài xanh, cà rốt giòn ngọt và đậu hũ chiên vàng. Điểm nhấn là chén sốt tương bơ đậu phộng sánh đặc thơm béo ngậy, ai ăn một lần cũng mê mẩn.",
            ImageUrl = "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80",
            Visibility = "PUBLIC",
            ViewCount = 1840,
            ModerationStatus = "APPROVED_ACTIVE",
            CreatedAt = now.AddDays(-8)
        };

        var p1015 = new Post
        {
            PostId = 1015,
            UserId = 5,
            Title = "Cà ri chay khoai lang đậu gà cốt dừa béo ngậy ăn cùng bánh mì giòn",
            Content = "Món cà ri chay thơm lừng mùi sả cây và bột cà ri Ấn Độ hảo hạng, nước sốt vàng ươm sánh ngậy vị cốt dừa tươi Bến Tre. Khoai lang bùi ngọt, đậu gà giàu đạm thực vật mềm tan, nấm hương dai giòn sần sật. Thích hợp cho bữa tiệc gia đình ngày mưa se lạnh!",
            ImageUrl = "https://images.unsplash.com/photo-1546833999-b9f581a1996d?auto=format&fit=crop&w=800&q=80",
            Visibility = "PUBLIC",
            ViewCount = 2780,
            ModerationStatus = "APPROVED_ACTIVE",
            CreatedAt = now.AddDays(-6)
        };

        var p1016 = new Post
        {
            PostId = 1016,
            UserId = 2,
            Title = "Bí quyết đổ Bánh xèo chay giòn rụm cả tiếng không ỉu nhân nấm củ hũ dừa",
            Content = "Công thức pha bột bánh xèo gia truyền giòn tan: bột gạo, bột bắp, nước cốt dừa và một lon nước có ga để tạo độ xốp rỗng. Nhân bánh thanh mát từ củ hũ dừa Bến Tre bào sợi, nấm hương tươi xào thơm và giá đỗ tươi. Cuốn cùng rau rừng, xà lách chấm nước mắm chay chua ngọt cay cay.",
            ImageUrl = "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?auto=format&fit=crop&w=800&q=80",
            Visibility = "PUBLIC",
            ViewCount = 4120,
            ModerationStatus = "APPROVED_ACTIVE",
            CreatedAt = now.AddDays(-4)
        };

        // 10 bài viết chia sẻ kinh nghiệm
        var p1021 = new Post
        {
            PostId = 1021,
            UserId = 6,
            Title = "Hành trình 30 ngày chuyển từ ăn mặn sang thuần chay: Những thay đổi kỳ diệu về năng lượng và giấc ngủ",
            Content = "Chào cộng đồng VeggieLife, mình là bác sĩ Lê Lan. Hôm nay mình muốn chia sẻ hành trình 30 ngày chuyển đổi từ chế độ ăn mặn thông thường sang thuần chay (Plant-based) hoàn toàn.\n\n**Tuần 1: Giai đoạn giải độc và thích nghi**\nCơ thể bắt đầu có cảm giác thèm vị umami đậm đà của thịt. Bí quyết của mình là sử dụng nấm hương nướng, tương miso và men dinh dưỡng (nutritional yeast) để tạo vị ngọt thịt tự nhiên. Uống đủ 2.5 lít nước mỗi ngày giúp thận đào thải lượng axit uric tích tụ.\n\n**Tuần 2: Ổn định hệ tiêu hóa**\nLượng chất xơ tăng vọt từ rau xanh và đậu gà có thể gây chướng bụng nhẹ. Mẹo nhỏ: ngâm kỹ các loại hạt đậu 8 tiếng trước khi nấu và cho vài lát gừng tươi vào hầm. Sau 10 ngày, hệ vi sinh đường ruột thích nghi hoàn hảo, cảm giác nặng bụng sau bữa ăn hoàn toàn biến mất!\n\n**Tuần 3 & 4: Sự bừng sáng về thể chất và tinh thần**\nGiấc ngủ sâu hơn rõ rệt, không còn thức giấc giữa đêm. Sáng dậy tỉnh táo không cần cà phê. Đặc biệt, huyết áp và chỉ số mỡ máu giảm đáng kể. Ăn chay không phải là sự từ bỏ, mà là mở ra một thế giới ẩm thực phong phú, đầy màu sắc và nhân ái!",
            ImageUrl = "https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=800&q=80",
            Visibility = "PUBLIC",
            ViewCount = 3450,
            ModerationStatus = "APPROVED_ACTIVE",
            CreatedAt = now.AddDays(-15)
        };

        var p1022 = new Post
        {
            PostId = 1022,
            UserId = 6,
            Title = "Bí quyết bổ sung Vitamin B12, Sắt và Kẽm cho người ăn chay trường không lo thiếu máu hay suy nhược",
            Content = "Rất nhiều bạn ăn chay thường lo lắng về tình trạng thiếu máu, rụng tóc hay mệt mỏi. Với góc nhìn y khoa và dinh dưỡng, mình xin đúc kết các quy tắc vàng:\n\n1. **Vitamin B12 - Bắt buộc phải bổ sung đúng cách:**\nB12 không tồn tại tự nhiên trong thực vật. Người ăn chay trường nên bổ sung viên ngậm Methylcobalamin 500mcg - 1000mcg hàng tuần, hoặc sử dụng sữa hạt và ngũ cốc có tăng cường vi chất B12.\n\n2. **Hấp thu Sắt không chứa heme (Non-heme Iron) tối ưu:**\nSắt trong rau bina, đậu lăng, hạt bí và cải xoăn cần môi trường axit để hấp thu. Hãy luôn kết hợp nguồn Vitamin C (vắt vài giọt chanh, ăn kèm ớt chuông hoặc tráng miệng với cam, ổi) ngay trong bữa ăn để tăng hấp thu sắt gấp 3 - 4 lần! Tuyệt đối không uống trà xanh hay cà phê ngay sau bữa ăn vì chất tannin sẽ ức chế hấp thu sắt.\n\n3. **Kẽm (Zinc) cho hệ miễn dịch vững vàng:**\nHạt bí ngô, hạt gai dầu, mầm yến mạch và đậu phụ non là nguồn kẽm tuyệt vời. Ngâm và ủ mầm đậu trước khi chế biến sẽ vô hiệu hóa phytate, giúp cơ thể hấp thụ trọn vẹn khoáng chất.",
            ImageUrl = "https://images.unsplash.com/photo-1490645935967-10de6ba17061?auto=format&fit=crop&w=800&q=80",
            Visibility = "PUBLIC",
            ViewCount = 4820,
            ModerationStatus = "APPROVED_ACTIVE",
            CreatedAt = now.AddDays(-13)
        };

        var p1023 = new Post
        {
            PostId = 1023,
            UserId = 9,
            Title = "Thực đơn Eat Clean thuần chay giảm 4kg mỡ thừa trong 6 tuần: Vẫn no bụng và tràn đầy sinh lực",
            Content = "Nhiều bạn nghĩ ăn chay sẽ béo vì ăn nhiều tinh bột trắng hoặc đồ chiên xào giả mặn. Nhưng với phương pháp Eat Clean thuần chay nguyên bản (Whole Food Plant-Based), mình đã giúp hàng trăm học viên yoga giảm từ 3-5kg mỡ thừa nhẹ nhàng.\n\n**Quy tắc đĩa ăn cân bằng chuẩn khoa học:**\n- **50% Đĩa ăn:** Rau củ quả nhiều màu sắc giàu chất xơ (bông cải xanh, măng tây, ớt chuông, dưa leo, cà chua).\n- **25% Đĩa ăn:** Protein thực vật đậm đặc (đậu hũ nướng nồi chiên không dầu, tempeh xào tương, đậu gà luộc, edamame).\n- **25% Đĩa ăn:** Tinh bột hấp thu chậm (gạo lứt huyết rồng, khoai lang nướng, yến mạch cán dẹt, hạt diêm mạch quinoa).\n\n**Công thức nước sốt salad \"thần thánh\" không dầu mỡ:**\nXay nhuyễn 1/2 quả bơ chín, 1 muỗng bơ mè tahini, nước cốt 1/2 quả chanh vàng, 1 tép tỏi nhỏ và nhúm muối hồng. Trộn đều cùng salad giòn ngọt ăn no bụng mà lượng calo nạp vào chỉ bằng một nửa bữa ăn mặn thông thường!",
            ImageUrl = "https://images.unsplash.com/photo-1543339308-43e59d6b73a6?auto=format&fit=crop&w=800&q=80",
            Visibility = "PUBLIC",
            ViewCount = 5210,
            ModerationStatus = "APPROVED_ACTIVE",
            CreatedAt = now.AddDays(-11)
        };

        var p1024 = new Post
        {
            PostId = 1024,
            UserId = 2,
            Title = "Mẹo Meal Prep: Đi chợ 1 lần vào Chủ nhật, sơ chế nấm và rau củ tươi ngon cả tuần cho dân văn phòng",
            Content = "Là đầu bếp nhưng trong tuần mình cũng rất bận rộn. Đây là quy trình Meal Prep tiết kiệm 80% thời gian bếp núc mỗi tối dành cho bạn:\n\n**1. Bảo quản nấm tươi không bị nhớt hay thâm đen:**\nNấm đùi gà, nấm hương tuyệt đối không rửa trước khi cất tủ lạnh. Hãy gọt sạch chân nấm, bọc vào túi giấy xi măng hoặc hộp nhựa lót khăn giấy khô. Khăn giấy sẽ hút độ ẩm thừa, nấm giữ được độ giòn ngọt suốt 8-10 ngày.\n\n**2. Sơ chế rau thơm và củ quả:**\nNgò rí, húng quế, hành boa-rô rửa sạch, quay thật ráo nước bằng rổ quay ly tâm. Cắt vừa hộp thủy tinh và đặt một lớp khăn giấy ăn ở đáy lẫn trên bề mặt.\nCà rốt, củ cải gọt vỏ cắt que ngâm trong hộp nước sạch đậy kín, đổi nước 2 ngày một lần, củ luôn giòn ngọt như mới nhổ.\n\n**3. Nấu sẵn 3 hũ sốt vạn năng:**\n- Sốt kho tiêu gừng sánh kẹo (cho các món nấm kho, đậu kho).\n- Sốt chua ngọt chanh dây (cho gỏi cuốn, đậu hũ chiên).\n- Sốt mè rang dinh dưỡng (cho rau củ luộc và mì trộn).\nTối đi làm về chỉ mất 10 phút xào nấu là có ngay mâm cơm chay nóng sốt chuẩn vị!",
            ImageUrl = "https://images.unsplash.com/photo-1498837167922-ddd27525d352?auto=format&fit=crop&w=800&q=80",
            Visibility = "PUBLIC",
            ViewCount = 2930,
            ModerationStatus = "APPROVED_ACTIVE",
            CreatedAt = now.AddDays(-9)
        };

        var p1025 = new Post
        {
            PostId = 1025,
            UserId = 10,
            Title = "Cẩm nang sinh tồn khi đi tiệc mặn, liên hoan công ty hay du lịch xa cho người ăn chay",
            Content = "Ăn chay tại nhà thì rất dễ, nhưng ra ngoài xã hội tiệc tùng hay đi phượt cùng bạn bè mặn thì thế nào? Dưới đây là những kinh nghiệm xương máu sau 6 năm ăn chay trường của mình:\n\n- **Báo trước với người tổ chức một cách tinh tế:** Đừng tạo cảm giác mình đòi hỏi đặc quyền. Hãy nhắn tin nhẹ nhàng: \"Em ăn chay thanh đạm nên chỉ cần 1 đĩa đậu hũ chiên sả hoặc rau củ xào là no bụng rồi ạ\". Thường các nhà hàng tiệc cưới hiện nay đều có thực đơn chay riêng rất tươm tất nếu được báo trước.\n- **Xem menu nhà hàng mặn với con mắt sáng tạo:** Hầu hết quán lẩu hay nướng mặn đều có nấm tươi, ngô ngọt, khoai lang, đậu hũ trắng và rau xanh. Chỉ cần dặn nhà bếp cho nồi nước lẩu chay (hoặc nước dừa tươi đun sôi nêm muối ớt) là bạn đã có một nồi lẩu ấm cúng bên bạn bè.\n- **Túi \"cứu cánh\" khi đi du lịch phượt:** Luôn thủ sẵn trong balo: 2 gói rong biển sấy mè, 1 hũ ruốc nấm hương tự làm, thanh ngũ cốc hạt điều, và vài gói cháo yến mạch ăn liền. Dù có kẹt ở sân bay hay vùng sâu vùng xa, bạn vẫn luôn chủ động nguồn dinh dưỡng sạch!",
            ImageUrl = "https://images.unsplash.com/photo-1511690656952-34342bb7c2f2?auto=format&fit=crop&w=800&q=80",
            Visibility = "PUBLIC",
            ViewCount = 2170,
            ModerationStatus = "APPROVED_ACTIVE",
            CreatedAt = now.AddDays(-7)
        };

        var p1026 = new Post
        {
            PostId = 1026,
            UserId = 11,
            Title = "Làn da sạch mụn, cơ thể nhẹ nhõm: 90 ngày ăn chay thanh lọc đã thay đổi tôi như thế nào",
            Content = "Trước đây mình từng chiến đấu với mụn nội tiết dai dẳng suốt 4 năm đại học. Đi da liễu uống thuốc kháng sinh bôi kem đủ loại chỉ đỡ được một thời gian rồi tái phát dữ dội hơn.\n\nBước ngoặt xảy ra khi mình quyết định cắt bỏ 100% sữa bò, phô mai và thịt đỏ trong 90 ngày:\n- **Tháng thứ nhất:** Tuần đầu da có nổi vài nốt mụn thanh lọc li ti. Nhưng đến cuối tháng, các ổ viêm sưng đỏ dưới cằm xẹp hẳn, không còn cảm giác nóng bừng bức bối trong người.\n- **Tháng thứ hai:** Lượng dầu vùng chữ T giảm rõ rệt. Lỗ chân lông thu nhỏ lại. Mình bắt đầu uống thêm sinh tố cần tây, táo xanh và tảo xoắn spirulina mỗi sáng.\n- **Tháng thứ ba:** Làn da sáng bừng khỏe khoắn từ bên trong, các vết thâm mờ đi 80%. Quan trọng nhất là tinh thần luôn vui tươi, tràn đầy năng lượng tích cực.\n\nVẻ đẹp thực sự bắt đầu từ một hệ tiêu hóa khỏe mạnh và nguồn thức ăn thanh khiết từ thiên nhiên!",
            ImageUrl = "https://images.unsplash.com/photo-1506126613408-eca07ce68773?auto=format&fit=crop&w=800&q=80",
            Visibility = "PUBLIC",
            ViewCount = 3760,
            ModerationStatus = "APPROVED_ACTIVE",
            CreatedAt = now.AddDays(-5)
        };

        var p1027 = new Post
        {
            PostId = 1027,
            UserId = 3,
            Title = "Nấu nước dùng chay ngọt thanh tự nhiên từ 5 loại củ quả: Không cần một hạt mì chính hay hạt nêm",
            Content = "Nhiều quán ăn chay lạm dụng bột ngọt và hạt nêm giả thịt khiến người ăn xong bị khô cổ khát nước. Người đầu bếp chay chân chính lấy vị ngọt từ lòng đất mẹ.\n\n**Tỉ lệ vàng cho nồi nước dùng 5 lít trong vắt:**\n- **2 khúc mía lau chẻ đôi:** Cho vị ngọt thanh tao sâu lắng.\n- **1 bắp ngô ngọt cắt khúc:** Cung cấp hương thơm sữa bắp dịu dàng.\n- **1 củ sắn (củ đậu) gọt vỏ:** Giữ cho nước dùng luôn trong trẻo không bị đục.\n- **1 củ cải trắng + 1 củ cải muối khô ngâm rửa sạch:** Tạo chiều sâu hậu vị đằm thắm.\n- **5 tai nấm hương khô nướng cháy xém vỏ:** Tiết ra glutamate tự nhiên cực kỳ thơm ngon.\n\nHầm trên lửa nhỏ liu riu 90 phút, tuyệt đối không đậy nắp và không khuấy đảo mạnh. Lọc qua rây vải mịn, nêm một nhúm muối hầm Đề Gi. Bạn sẽ có thứ nước dùng tinh hoa để nấu bún, phở, hủ tiếu hay canh súp thơm ngon xuất sắc!",
            ImageUrl = "https://images.unsplash.com/photo-1547592166-23ac45744acd?auto=format&fit=crop&w=800&q=80",
            Visibility = "PUBLIC",
            ViewCount = 6120,
            ModerationStatus = "APPROVED_ACTIVE",
            CreatedAt = now.AddDays(-3)
        };

        var p1028 = new Post
        {
            PostId = 1028,
            UserId = 8,
            Title = "Tập gym và tăng cơ giảm mỡ thành công với chế độ ăn hoàn toàn từ thực vật (Plant-based Fitness)",
            Content = "\"Ăn cỏ thì sức đâu mà nâng tạ? Ăn chay sao to cơ được?\" - Đó là câu hỏi mình nhận được nhiều nhất suốt 3 năm qua tại phòng gym.\n\nSự thật là protein từ thực vật hoàn toàn có thể xây dựng cơ bắp săn chắc nếu bạn hiểu đúng về cấu trúc axit amin:\n1. **Các siêu thực phẩm tăng cơ thuần chay:**\n   - **Seitan (Mì căn tự làm):** Chứa tới 75g protein / 100g (cao gấp 3 lần ức gà!).\n   - **Tempeh (Đậu tương lên men):** 19g protein / 100g, chứa probiotics nuôi dưỡng đường ruột hấp thu tối đa.\n   - **Đậu lăng & Đậu gà:** Nguồn carb phức hợp và protein đồng thời, nạp năng lượng bền bỉ cho buổi squat nặng.\n2. **Lịch nạp dinh dưỡng quanh buổi tập:**\n   - **Trước tập 60 phút:** Sinh tố chuối tiêu, yến mạch, bơ đậu phộng và bột hạt chia.\n   - **Sau tập 30 phút:** Bột protein thực vật (Pea + Rice protein cô đặc) kết hợp sữa đậu nành ấm.\n\nHiện tại mình duy trì body fat 11-12%, nâng tạ deadlift 180kg nhẹ nhàng, không bao giờ bị đầy bụng hay nổi mụn như thời kỳ uống Whey Protein từ sữa bò!",
            ImageUrl = "https://images.unsplash.com/photo-1517838277536-f5f99be501cd?auto=format&fit=crop&w=800&q=80",
            Visibility = "PUBLIC",
            ViewCount = 4390,
            ModerationStatus = "APPROVED_ACTIVE",
            CreatedAt = now.AddDays(-2)
        };

        var p1029 = new Post
        {
            PostId = 1029,
            UserId = 4,
            Title = "Ăn chay chánh niệm và lắng nghe cơ thể: Nuôi dưỡng sự bình an và yêu thương từ bữa cơm gia đình",
            Content = "Trong nhịp sống hiện đại vội vã, bữa ăn thường bị biến thành việc vừa lướt điện thoại vừa nuốt vội thức ăn. Ăn chay chánh niệm không chỉ là ăn gì, mà là \"cách chúng ta ăn\".\n\nKhi ngồi vào bàn ăn, hãy dành 30 giây tĩnh lặng:\n- Ngắm nhìn màu xanh của đĩa rau luộc, màu đỏ của lát ớt, màu vàng ươm của miếng đậu hũ. Tri ân đất trời, mưa nắng và giọt mồ hôi của người nông dân đã làm nên hạt ngọc này.\n- Nhai chậm rãi từ 20 đến 30 lần mỗi miếng ăn. Khi nhai kỹ, enzym amylase trong nước bọt hòa quyện giúp tinh bột chuyển hóa thành vị ngọt tự nhiên thanh khiết vô cùng dịu mát.\n- Lắng nghe tín hiệu no của cơ thể để dừng lại ở mức 80% sức chứa dạ dày.\n\nKhi tâm an tịnh, bữa ăn chay đơn sơ với bát cơm gạo lứt, đĩa rau muống luộc và chén tương kho nấm cũng trở thành một đại tiệc của hạnh phúc và tình thương.",
            ImageUrl = "https://images.unsplash.com/photo-1505576399279-565b52d4ac71?auto=format&fit=crop&w=800&q=80",
            Visibility = "PUBLIC",
            ViewCount = 1890,
            ModerationStatus = "APPROVED_ACTIVE",
            CreatedAt = now.AddDays(-1)
        };

        var p1030 = new Post
        {
            PostId = 1030,
            UserId = 7,
            Title = "Kinh nghiệm nuôi con ăn chay đủ chất, trộm vía tăng cân và phát triển chiều cao vượt trội",
            Content = "Chào các bậc phụ huynh trên VeggieLife! Vợ chồng mình ăn chay trường và cả hai bé nhà mình (bé Bo 6 tuổi, bé Miu 3 tuổi) đều là những em bé thuần chay từ trong bụng mẹ.\n\nNhiều người e ngại con ăn chay sẽ còi cọc thiếu chất, nhưng thực tế cả hai bé đều đạt chuẩn phát triển của WHO, chiều cao luôn nằm trong top 10% của lớp. Dưới đây là thực đơn dinh dưỡng mình áp dụng:\n\n- **Sữa hạt tươi tự nấu mỗi ngày:** Luân phiên hạt óc chó, hạnh nhân, hạt sen, mè đen và yến mạch. Mè đen và hạt chia chứa hàm lượng canxi cao gấp nhiều lần sữa bò!\n- **Cháo & Súp đa sắc:** Nấu cháo cùng hạt kê, đậu lăng đỏ (rất giàu sắt và mềm nhanh), bí đỏ và rong biển wakame cung cấp iot tự nhiên.\n- **Bổ sung chất béo tốt cho não bộ:** Trộn trực tiếp 1 muỗng cà phê dầu hạt lanh hoặc dầu sacha inchi ép lạnh vào bát cháo ấm của bé để bổ sung Omega-3 (DHA/EPA).\n- **Tập cho con yêu thiên nhiên:** Cho con cùng mẹ lặt rau, gieo hạt giá đỗ tại nhà. Các bé rất hào hứng khi được ăn chính những mầm rau do mình chăm sóc!",
            ImageUrl = "https://images.unsplash.com/photo-1476224203421-9ac39bcb3327?auto=format&fit=crop&w=800&q=80",
            Visibility = "PUBLIC",
            ViewCount = 3120,
            ModerationStatus = "APPROVED_ACTIVE",
            CreatedAt = now
        };

        db.Posts.AddRange(p1011, p1012, p1013, p1014, p1015, p1016, p1021, p1022, p1023, p1024, p1025, p1026, p1027, p1028, p1029, p1030);
        db.SaveChanges();

        // 3. Nạp 6 Video nấu ăn & VideoTranscripts
        var v1 = new Video
        {
            VideoId = 2101,
            PostId = 1011,
            Title = "Video hướng dẫn nấu Canh chua chay Nam Bộ thanh mát",
            VideoUrl = "https://www.youtube.com/embed/5qap5aO4i9A",
            ThumbnailUrl = "https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=800&q=80",
            DurationSeconds = 180,
            ViewCount = 1420,
            Status = "READY",
            CreatedAt = now.AddDays(-14)
        };

        var v2 = new Video
        {
            VideoId = 2102,
            PostId = 1012,
            Title = "Video làm Nấm đùi gà kho tiêu hạt sen đậm đà óng ả",
            VideoUrl = "https://www.youtube.com/embed/gN3Zg3L6sX4",
            ThumbnailUrl = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80",
            DurationSeconds = 150,
            ViewCount = 2150,
            Status = "READY",
            CreatedAt = now.AddDays(-12)
        };

        var v3 = new Video
        {
            VideoId = 2103,
            PostId = 1013,
            Title = "Video nấu Phở bò chay Hà Nội nước dùng thảo mộc thơm lừng",
            VideoUrl = "https://www.tiktok.com/embed/v2/7234567890123456789",
            ThumbnailUrl = "https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?auto=format&fit=crop&w=800&q=80",
            DurationSeconds = 210,
            ViewCount = 3890,
            Status = "READY",
            CreatedAt = now.AddDays(-10)
        };

        var v4 = new Video
        {
            VideoId = 2104,
            PostId = 1014,
            Title = "Video cuốn Gỏi cuốn ngũ sắc thanh lọc sốt bơ đậu phộng",
            VideoUrl = "https://www.tiktok.com/embed/v2/7198765432109876543",
            ThumbnailUrl = "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80",
            DurationSeconds = 120,
            ViewCount = 1840,
            Status = "READY",
            CreatedAt = now.AddDays(-8)
        };

        var v5 = new Video
        {
            VideoId = 2105,
            PostId = 1015,
            Title = "Video nấu Cà ri chay khoai lang đậu gà béo ngậy nước cốt dừa",
            VideoUrl = "https://www.youtube.com/embed/kJQP7kiw5Fk",
            ThumbnailUrl = "https://images.unsplash.com/photo-1546833999-b9f581a1996d?auto=format&fit=crop&w=800&q=80",
            DurationSeconds = 160,
            ViewCount = 2780,
            Status = "READY",
            CreatedAt = now.AddDays(-6)
        };

        var v6 = new Video
        {
            VideoId = 2106,
            PostId = 1016,
            Title = "Video bí quyết đổ Bánh xèo chay giòn rụm cả tiếng không ỉu",
            VideoUrl = "https://www.youtube.com/embed/L_LUpnjgPso",
            ThumbnailUrl = "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?auto=format&fit=crop&w=800&q=80",
            DurationSeconds = 190,
            ViewCount = 4120,
            Status = "READY",
            CreatedAt = now.AddDays(-4)
        };

        db.Videos.AddRange(v1, v2, v3, v4, v5, v6);
        db.SaveChanges();

        // 4. Nạp VideoTranscripts kèm mốc thời gian Segments & JSON Công thức
        var vt1 = new VideoTranscript
        {
            TranscriptId = 3101,
            VideoId = 2101,
            RawTranscript = "Chào mừng các bạn đến với gian bếp VeggieLife. Hôm nay mình sẽ hướng dẫn nấu món Canh chua chay chuẩn vị Nam Bộ thanh mát giải nhiệt. Nguyên liệu gồm đậu hũ chiên cắt khối, thơm, cà chua, đậu bắp, bạc hà, giá đỗ và me chua dầm lấy nước cốt. Bắt đầu phi thơm hành bo-rô, cho cà chua và thơm vào xào nhẹ để tạo màu đỏ cam tự nhiên và tiết vị chua ngọt. Châm 800ml nước dùng rau củ đun sôi, rót nước cốt me vào nêm đường phèn, muối hầm và chút hạt nêm nấm hương. Thả đậu hũ, đậu bắp và bạc hà đun sôi bùng trong 2 phút, cuối cùng cho giá và ngò ôm ngò gai rồi tắt bếp. Múc canh ra tô, rắc chút ớt sừng cắt lát. Vị chua thanh của me hòa cùng rau củ giòn ngọt cực kỳ thanh mát đưa cơm.",
            ExtractedRecipeJson = "{\"segments\":[{\"start\":0.0,\"end\":25.0,\"text\":\"Chào mừng các bạn đến với gian bếp VeggieLife. Hôm nay mình sẽ hướng dẫn nấu món Canh chua chay chuẩn vị Nam Bộ thanh mát giải nhiệt.\"},{\"start\":25.0,\"end\":55.0,\"text\":\"Nguyên liệu gồm đậu hũ chiên cắt khối, thơm, cà chua, đậu bắp, bạc hà, giá đỗ và me chua dầm lấy nước cốt.\"},{\"start\":55.0,\"end\":85.0,\"text\":\"Bắt đầu phi thơm hành bo-rô, cho cà chua và thơm vào xào nhẹ để tạo màu đỏ cam tự nhiên và tiết vị chua ngọt.\"},{\"start\":85.0,\"end\":130.0,\"text\":\"Châm 800ml nước dùng rau củ đun sôi, rót nước cốt me vào nêm đường phèn, muối hầm và chút hạt nêm nấm hương.\"},{\"start\":130.0,\"end\":160.0,\"text\":\"Thả đậu hũ, đậu bắp và bạc hà đun sôi bùng trong 2 phút, cuối cùng cho giá và ngò ôm ngò gai rồi tắt bếp.\"},{\"start\":160.0,\"end\":180.0,\"text\":\"Múc canh ra tô, rắc chút ớt sừng cắt lát. Vị chua thanh của me hòa cùng rau củ giòn ngọt cực kỳ thanh mát đưa cơm.\"}],\"recipe\":{\"title\":\"Canh chua chay Nam Bộ\",\"servings\":4,\"cookTimeMinutes\":20,\"ingredients\":[{\"name\":\"Đậu hũ chiên vàng\",\"quantity\":2,\"unit\":\"miếng\"},{\"name\":\"Cà chua chín\",\"quantity\":2,\"unit\":\"quả\"},{\"name\":\"Thơm (Dứa)\",\"quantity\":0.25,\"unit\":\"quả\"},{\"name\":\"Đậu bắp tươi\",\"quantity\":5,\"unit\":\"quả\"},{\"name\":\"Bạc hà (Dọc mùng)\",\"quantity\":1,\"unit\":\"nhánh\"},{\"name\":\"Giá đỗ\",\"quantity\":100,\"unit\":\"g\"},{\"name\":\"Me chua chín\",\"quantity\":30,\"unit\":\"g\"},{\"name\":\"Ngò ôm, ngò gai, boa-rô\",\"quantity\":30,\"unit\":\"g\"}],\"steps\":[\"Bước 1: Sơ chế các loại rau củ, cà chua cắt múi cau, thơm cắt lát mỏng, bạc hà tước vỏ cắt xéo bóp muối rửa sạch.\",\"Bước 2: Dầm me với 100ml nước sôi lọc lấy nước cốt chua thanh.\",\"Bước 3: Phi thơm boa-rô với 1 muỗng dầu thực vật, cho thơm và cà chua vào xào sơ 2 phút.\",\"Bước 4: Châm 800ml nước dùng rau củ đun sôi, cho nước cốt me, nêm 1.5 muỗng đường phèn và 1 muỗng muối hầm.\",\"Bước 5: Thả đậu hũ chiên, đậu bắp, bạc hà đun sôi bùng 2 phút, tắt bếp cho giá đỗ và rau thơm lên trên.\"],\"tips\":[\"Dầm me bằng nước sôi để lấy hết vị chua thanh; không nấu bạc hà quá lâu để giữ độ giòn.\"]}}",
            AiSummary = "Canh chua chay Nam Bộ (Khẩu phần: 4 người, 8 nguyên liệu, 5 bước chế biến)",
            SttModelVersion = "faster-whisper-small-int8",
            Status = "COMPLETED",
            CreatedAt = now.AddDays(-14),
            CompletedAt = now.AddDays(-14)
        };

        var vt2 = new VideoTranscript
        {
            TranscriptId = 3102,
            VideoId = 2102,
            RawTranscript = "Hôm nay chúng ta cùng thực hiện món Nấm đùi gà kho tiêu hạt sen - món mặn chay đưa cơm số một cho cả nhà. Nấm đùi gà rửa sạch, cắt khoanh tròn 2cm rồi khía vảy rồng hai mặt để khi kho nấm thấm đều gia vị. Ướp nấm với 2 muỗng nước tương, 1 muỗng hắc xì dầu, nửa muỗng đường thốt nốt và nhiều tiêu đen đập dập. Đun nóng chút dầu mè trong nồi đất, cho nấm vào áp chảo hai mặt vàng xém rồi trút toàn bộ nước ướp cùng 50ml nước dừa tươi. Kho trên lửa nhỏ liu riu 15 phút cho nước sốt keo sánh lại, áo đều lên từng miếng nấm nâu bóng bẩy.",
            ExtractedRecipeJson = "{\"segments\":[{\"start\":0.0,\"end\":20.0,\"text\":\"Hôm nay chúng ta cùng thực hiện món Nấm đùi gà kho tiêu hạt sen - món mặn chay đưa cơm số một cho cả nhà.\"},{\"start\":20.0,\"end\":50.0,\"text\":\"Nấm đùi gà rửa sạch, cắt khoanh tròn 2cm rồi khía vảy rồng hai mặt để khi kho nấm thấm đều gia vị.\"},{\"start\":50.0,\"end\":80.0,\"text\":\"Ướp nấm với 2 muỗng nước tương, 1 muỗng hắc xì dầu, nửa muỗng đường thốt nốt và nhiều tiêu đen đập dập.\"},{\"start\":80.0,\"end\":120.0,\"text\":\"Đun nóng chút dầu mè trong nồi đất, cho nấm vào áp chảo hai mặt vàng xém rồi trút toàn bộ nước ướp cùng 50ml nước dừa tươi.\"},{\"start\":120.0,\"end\":150.0,\"text\":\"Kho trên lửa nhỏ liu riu 15 phút cho nước sốt keo sánh lại, áo đều lên từng miếng nấm nâu bóng bẩy.\"}],\"recipe\":{\"title\":\"Nấm đùi gà kho tiêu hạt sen\",\"servings\":3,\"cookTimeMinutes\":25,\"ingredients\":[{\"name\":\"Nấm đùi gà tươi\",\"quantity\":300,\"unit\":\"g\"},{\"name\":\"Hạt sen tươi luộc chín\",\"quantity\":80,\"unit\":\"g\"},{\"name\":\"Nước tương tamari\",\"quantity\":30,\"unit\":\"ml\"},{\"name\":\"Hắc xì dầu\",\"quantity\":10,\"unit\":\"ml\"},{\"name\":\"Nước dừa xiêm\",\"quantity\":100,\"unit\":\"ml\"},{\"name\":\"Tiêu đen sọ đập dập\",\"quantity\":10,\"unit\":\"g\"},{\"name\":\"Dầu mè thơm\",\"quantity\":10,\"unit\":\"ml\"}],\"steps\":[\"Bước 1: Cắt nấm đùi gà khoanh dày 2cm, dùng dao khía ca-rô hai mặt nấm.\",\"Bước 2: Ướp nấm với nước tương, tiêu, đường thốt nốt trong 15 phút.\",\"Bước 3: Cho dầu mè vào nồi đất, áp chảo vàng nhẹ 2 mặt nấm.\",\"Bước 4: Đổ nước ướp và nước dừa vào đun sôi, hạ lửa nhỏ kho liu riu cùng hạt sen.\",\"Bước 5: Khi nước sốt keo sánh lại sền sệt, rắc thêm tiêu sọ và hành boa-rô lên trên.\"],\"tips\":[\"Nên kho bằng nồi đất nung để nhiệt phân bổ đều và nước sốt sánh kẹo tự nhiên.\"]}}",
            AiSummary = "Nấm đùi gà kho tiêu hạt sen (Khẩu phần: 3 người, 7 nguyên liệu, 5 bước chế biến)",
            SttModelVersion = "faster-whisper-small-int8",
            Status = "COMPLETED",
            CreatedAt = now.AddDays(-12),
            CompletedAt = now.AddDays(-12)
        };

        var vt3 = new VideoTranscript
        {
            TranscriptId = 3103,
            VideoId = 2103,
            RawTranscript = "Chào mừng các bạn đến với công thức phở chay Hà Nội. Điểm nhấn là gói gia vị thảo mộc gồm quế, hoa hồi, thảo quả được nướng thơm trước khi thả vào nồi nước dùng. Nước dùng hầm 2 tiếng từ củ cải trắng, củ sắn, mía lau, bắp ngô ngọt tạo độ ngọt thanh sâu lắng. Phần thịt bò chay làm từ nấm đùi gà xé sợi và váng đậu ky chiên phồng ướp sốt hạt điều màu. Trần bánh phở tươi cho vào tô, xếp nấm, chả chay, rau mùi húng quế và chan nước dùng nóng hổi nghi ngút khói.",
            ExtractedRecipeJson = "{\"segments\":[{\"start\":0.0,\"end\":30.0,\"text\":\"Phở chay làm sao để có vị ngọt sâu và hương thơm đặc trưng của phở truyền thống? Bí quyết nằm ở nồi nước dùng thảo mộc.\"},{\"start\":30.0,\"end\":70.0,\"text\":\"Hầm 2 tiếng củ cải trắng, củ sắn, mía lau, bắp ngô ngọt và một củ hành tây nướng cháy xém vỏ ngoài.\"},{\"start\":70.0,\"end\":105.0,\"text\":\"Rang thơm hoa hồi, thảo quả, quế thanh, tiểu hồi và hạt ngò, bọc vào túi vải rồi thả vào nồi nước dùng.\"},{\"start\":105.0,\"end\":150.0,\"text\":\"Phần thịt bò chay làm từ nấm đùi gà xé sợi và tàu hũ ky ướp dầu điều xào săn thơm phức.\"},{\"start\":150.0,\"end\":210.0,\"text\":\"Trần bánh phở tươi, xếp nấm, chả nấm, hành tây cắt mỏng, rau húng quế và chan nước dùng nóng hổi thơm nức mũi.\"}],\"recipe\":{\"title\":\"Phở chay Hà Nội nước dùng thảo mộc\",\"servings\":4,\"cookTimeMinutes\":45,\"ingredients\":[{\"name\":\"Bánh phở tươi\",\"quantity\":500,\"unit\":\"g\"},{\"name\":\"Nấm đùi gà xé sợi\",\"quantity\":200,\"unit\":\"g\"},{\"name\":\"Tàu hũ ky chiên giòn\",\"quantity\":50,\"unit\":\"g\"},{\"name\":\"Gói thảo mộc phở (hồi, quế, thảo quả)\",\"quantity\":1,\"unit\":\"gói\"},{\"name\":\"Mía lau, bắp ngọt, củ sắn\",\"quantity\":500,\"unit\":\"g\"},{\"name\":\"Rau thơm (ngò gai, húng quế, giá)\",\"quantity\":150,\"unit\":\"g\"}],\"steps\":[\"Bước 1: Nướng thơm hành tây, gừng, hồi, quế, thảo quả trên bếp than hoặc chảo gang.\",\"Bước 2: Hầm mía lau, bắp ngọt, củ sắn cùng túi thảo mộc trong 2 lít nước suốt 60 phút.\",\"Bước 3: Xào săn nấm đùi gà xé sợi với chút dầu điều và nước tương làm nhân thịt phở.\",\"Bước 4: Nêm nước dùng với muối hầm, đường phèn và chút tương hột xay nhuyễn.\",\"Bước 5: Chần bánh phở qua nước sôi, xếp vào tô cùng nấm, tàu hũ ky rồi chan nước dùng đang sôi sùng sục.\"],\"tips\":[\"Nướng thảo mộc vừa tới thơm, không để cháy đen để nước dùng không bị đắng và đục.\"]}}",
            AiSummary = "Phở chay Hà Nội nước dùng thảo mộc (Khẩu phần: 4 người, 6 nguyên liệu, 5 bước chế biến)",
            SttModelVersion = "faster-whisper-small-int8",
            Status = "COMPLETED",
            CreatedAt = now.AddDays(-10),
            CompletedAt = now.AddDays(-10)
        };

        var vt4 = new VideoTranscript
        {
            TranscriptId = 3104,
            VideoId = 2104,
            RawTranscript = "Bữa trưa hè rực rỡ với Gỏi cuốn ngũ sắc thanh mát cùng nước chấm sốt tương bơ đậu phộng béo bùi gây nghiện. Các loại rau củ thái sợi dài: bắp cải tím, dưa chuột, cà rốt, xoài xanh, ớt chuông đỏ và đậu hũ chiên giòn. Làm ướt bánh tráng gạo mè, xếp lần lượt rau sống, bún tươi, các loại rau ngũ sắc và cuốn chặt tay đều mí. Công thức sốt thần thánh: 2 muỗng bơ đậu phộng mịn, 1 muỗng tương đen hoisin, 1 muỗng nước cốt dừa và chút ớt băm.",
            ExtractedRecipeJson = "{\"segments\":[{\"start\":0.0,\"end\":25.0,\"text\":\"Bữa trưa hè rực rỡ với Gỏi cuốn ngũ sắc thanh mát cùng nước chấm sốt tương bơ đậu phộng gây nghiện.\"},{\"start\":25.0,\"end\":55.0,\"text\":\"Các loại rau củ thái sợi dài: bắp cải tím, dưa chuột, cà rốt, xoài xanh, ớt chuông đỏ và đậu hũ chiên giòn.\"},{\"start\":55.0,\"end\":90.0,\"text\":\"Làm ướt bánh tráng gạo mè, xếp lần lượt rau sống, bún tươi, các loại rau ngũ sắc và cuốn chặt tay đều mí.\"},{\"start\":90.0,\"end\":120.0,\"text\":\"Công thức sốt thần thánh: 2 muỗng bơ đậu phộng mịn, 1 muỗng tương đen hoisin, 1 muỗng nước cốt dừa và chút ớt băm.\"}],\"recipe\":{\"title\":\"Gỏi cuốn ngũ sắc sốt bơ đậu phộng\",\"servings\":3,\"cookTimeMinutes\":15,\"ingredients\":[{\"name\":\"Bánh tráng dẻo gạo mè\",\"quantity\":1,\"unit\":\"xấp\"},{\"name\":\"Bún tươi sợi nhỏ\",\"quantity\":200,\"unit\":\"g\"},{\"name\":\"Đậu hũ chiên cắt thanh\",\"quantity\":2,\"unit\":\"miếng\"},{\"name\":\"Bắp cải tím bào sợi\",\"quantity\":80,\"unit\":\"g\"},{\"name\":\"Dưa leo và cà rốt thái sợi\",\"quantity\":150,\"unit\":\"g\"},{\"name\":\"Bơ đậu phộng mịn\",\"quantity\":50,\"unit\":\"g\"},{\"name\":\"Tương đen Hoisin và nước cốt dừa\",\"quantity\":40,\"unit\":\"ml\"}],\"steps\":[\"Bước 1: Rửa sạch và bào sợi các loại rau củ ngũ sắc để ráo nước.\",\"Bước 2: Cắt đậu hũ chiên thành các thanh dài bằng ngón tay út.\",\"Bước 3: Nhúng bánh tráng qua nước ấm, trải phẳng ra thớt, xếp xà lách, bún, rau ngũ sắc và đậu hũ.\",\"Bước 4: Gấp 2 mép bên hông cuộn tròn thật chặt tay.\",\"Bước 5: Nấu ấm bơ đậu phộng, tương hoisin và nước dừa thành hỗn hợp sốt mịn sánh rắc đậu phộng giã dập.\"],\"tips\":[\"Cuốn thật chặt tay từ vòng đầu tiên để gỏi cuốn căng tròn đẹp mắt khi cắt đôi.\"]}}",
            AiSummary = "Gỏi cuốn ngũ sắc sốt bơ đậu phộng (Khẩu phần: 3 người, 7 nguyên liệu, 5 bước chế biến)",
            SttModelVersion = "faster-whisper-small-int8",
            Status = "COMPLETED",
            CreatedAt = now.AddDays(-8),
            CompletedAt = now.AddDays(-8)
        };

        var vt5 = new VideoTranscript
        {
            TranscriptId = 3105,
            VideoId = 2105,
            RawTranscript = "Món Cà ri chay khoai lang đậu gà béo ngậy mùi nước cốt dừa, ăn kèm bánh mì nóng giòn ngày mưa se lạnh. Khoai lang mật và khoai môn cắt khối vuông chiên sơ lớp vỏ để khoai giữ nguyên hình dạng không bị nát nhừ. Phi sả cây đập dập cùng bột cà ri Ấn Độ cho dậy mùi, trút nấm hương và đậu gà đã luộc chín mềm vào đảo đều. Đổ nước dão dừa nấu sôi 10 phút, cho khoai vào ninh mềm, cuối cùng rưới chén cốt dừa đặc nguyên chất sánh ngậy. Nêm hạt nêm chay và muối hồng vừa miệng.",
            ExtractedRecipeJson = "{\"segments\":[{\"start\":0.0,\"end\":30.0,\"text\":\"Món Cà ri chay khoai lang đậu gà béo ngậy mùi nước cốt dừa, ăn kèm bánh mì nóng giòn ngày mưa se lạnh.\"},{\"start\":30.0,\"end\":60.0,\"text\":\"Khoai lang mật và khoai môn cắt khối vuông chiên sơ lớp vỏ để khoai giữ nguyên hình dạng không bị nát nhừ.\"},{\"start\":60.0,\"end\":95.0,\"text\":\"Phi sả cây đập dập cùng bột cà ri Ấn Độ cho dậy mùi, trút nấm hương và đậu gà đã luộc chín mềm vào đảo đều.\"},{\"start\":95.0,\"end\":135.0,\"text\":\"Đổ nước dão dừa nấu sôi 10 phút, cho khoai vào ninh mềm, cuối cùng rưới chén cốt dừa đặc nguyên chất sánh ngậy.\"},{\"start\":135.0,\"end\":160.0,\"text\":\"Nêm hạt nêm chay và muối hồng vừa miệng. Món ăn vàng ươm, thơm lừng ngào ngạt cả gian bếp.\"}],\"recipe\":{\"title\":\"Cà ri chay khoai lang đậu gà cốt dừa\",\"servings\":4,\"cookTimeMinutes\":30,\"ingredients\":[{\"name\":\"Khoai lang mật\",\"quantity\":250,\"unit\":\"g\"},{\"name\":\"Khoai môn cao\",\"quantity\":200,\"unit\":\"g\"},{\"name\":\"Đậu gà ngâm mềm luộc chín\",\"quantity\":150,\"unit\":\"g\"},{\"name\":\"Nấm hương tươi\",\"quantity\":100,\"unit\":\"g\"},{\"name\":\"Bột cà ri dầu và bột cà ri khô\",\"quantity\":2,\"unit\":\"muỗng canh\"},{\"name\":\"Nước cốt dừa nguyên chất\",\"quantity\":200,\"unit\":\"ml\"},{\"name\":\"Sả cây đập dập\",\"quantity\":4,\"unit\":\"nhánh\"}],\"steps\":[\"Bước 1: Khoai lang và khoai môn gọt vỏ, cắt khối vuông 3cm, chiên sơ vàng lớp ngoài.\",\"Bước 2: Phi thơm sả đập dập cùng bột cà ri với chút dầu thực vật.\",\"Bước 3: Cho nấm hương và đậu gà vào xào thấm gia vị cà ri trong 3 phút.\",\"Bước 4: Châm 600ml nước dão dừa hầm 15 phút, sau đó cho khoai đã chiên vào ninh mềm.\",\"Bước 5: Rưới nước cốt dừa đặc vào khuấy nhẹ tay, đun sôi liu riu nêm nếm vừa ăn rồi tắt bếp.\"],\"tips\":[\"Chiên sơ khoai giúp khoai thơm bùi hơn và không bị nát làm đục nồi sốt cà ri.\"]}}",
            AiSummary = "Cà ri chay khoai lang đậu gà cốt dừa (Khẩu phần: 4 người, 7 nguyên liệu, 5 bước chế biến)",
            SttModelVersion = "faster-whisper-small-int8",
            Status = "COMPLETED",
            CreatedAt = now.AddDays(-6),
            CompletedAt = now.AddDays(-6)
        };

        var vt6 = new VideoTranscript
        {
            TranscriptId = 3106,
            VideoId = 2106,
            RawTranscript = "Cách làm Bánh xèo miền Tây thuần chay mỏng giòn rụm cả tiếng đồng hồ không ỉu nhờ bí quyết pha bột chuẩn. Pha bột bánh xèo cùng nước cốt dừa béo ngậy, một lon nước có ga để tạo độ xốp giòn và hành boa-rô thái nhuyễn. Nhân bánh gồm củ hũ dừa bào sợi ngọt giòn, nấm hương tươi thái mỏng, đậu xanh hấp chín và giá đỗ. Làm nóng chảo gang thật bốc khói, tráng một lớp dầu mỏng, trút vá bột xoay đều viền chảo nghe tiếng xèo vang giòn. Rải đều nhân nấm đậy nắp 2 phút, mở nắp nhỏ vài giọt dầu quanh rìa bánh tới khi viền bánh giòn cong lên gấp đôi lại.",
            ExtractedRecipeJson = "{\"segments\":[{\"start\":0.0,\"end\":30.0,\"text\":\"Cách làm Bánh xèo miền Tây thuần chay mỏng giòn rụm cả tiếng đồng hồ không ỉu nhờ bí quyết pha bột chuẩn.\"},{\"start\":30.0,\"end\":60.0,\"text\":\"Pha bột bánh xèo cùng nước cốt dừa béo ngậy, một lon nước có ga để tạo độ xốp giòn và hành boa-rô thái nhuyễn.\"},{\"start\":60.0,\"end\":95.0,\"text\":\"Nhân bánh gồm củ hũ dừa bào sợi ngọt giòn, nấm hương tươi thái mỏng, đậu xanh hấp chín và giá đỗ.\"},{\"start\":95.0,\"end\":140.0,\"text\":\"Làm nóng chảo gang thật bốc khói, tráng một lớp dầu mỏng, trút vá bột xoay đều viền chảo nghe tiếng xèo vang giòn.\"},{\"start\":140.0,\"end\":190.0,\"text\":\"Rải đều nhân nấm đậy nắp 2 phút, mở nắp nhỏ vài giọt dầu quanh rìa bánh tới khi viền bánh giòn cong lên gấp đôi lại.\"}],\"recipe\":{\"title\":\"Bánh xèo chay giòn rụm củ hũ dừa\",\"servings\":4,\"cookTimeMinutes\":35,\"ingredients\":[{\"name\":\"Bột bánh xèo hương xưa\",\"quantity\":400,\"unit\":\"g\"},{\"name\":\"Nước cốt dừa\",\"quantity\":150,\"unit\":\"ml\"},{\"name\":\"Nước khoáng có ga (soda)\",\"quantity\":330,\"unit\":\"ml\"},{\"name\":\"Củ hũ dừa bào sợi\",\"quantity\":200,\"unit\":\"g\"},{\"name\":\"Nấm hương tươi thái mỏng\",\"quantity\":150,\"unit\":\"g\"},{\"name\":\"Đậu xanh hấp chín nguyên hạt\",\"quantity\":100,\"unit\":\"g\"},{\"name\":\"Giá đỗ và rau sống các loại\",\"quantity\":300,\"unit\":\"g\"}],\"steps\":[\"Bước 1: Khuấy đều bột bánh xèo, nước cốt dừa, nước soda và boa-rô thái nhỏ, để bột nghỉ 30 phút.\",\"Bước 2: Xào sơ nấm hương và củ hũ dừa nêm chút muối tiêu cho ráo nước.\",\"Bước 3: Đun nóng chảo chống dính hoặc chảo gang với 1 muỗng dầu ăn thật nóng già.\",\"Bước 4: Múc 1 vá bột đổ vào chảo, nghiêng chảo tráng thật mỏng đều khắp mặt chảo.\",\"Bước 5: Rải nhân nấm, đậu xanh, giá đỗ đậy nắp 2 phút, mở nắp chiên lửa vừa tới khi viền bánh giòn rụm gập đôi.\"],\"tips\":[\"Nước có ga tạo bọt khí li ti giúp vỏ bánh mỏng xốp giòn lâu vượt trội.\"]}}",
            AiSummary = "Bánh xèo chay giòn rụm củ hũ dừa (Khẩu phần: 4 người, 7 nguyên liệu, 5 bước chế biến)",
            SttModelVersion = "faster-whisper-small-int8",
            Status = "COMPLETED",
            CreatedAt = now.AddDays(-4),
            CompletedAt = now.AddDays(-4)
        };

        db.VideoTranscripts.AddRange(vt1, vt2, vt3, vt4, vt5, vt6);
        db.SaveChanges();

        // 5. Nạp 25 Bình luận (Comments) tương tác đa tầng
        var comments = new List<Comment>
        {
            new() { CommentId = 4001, UserId = 11, PostId = 1021, Content = "Bài viết quá hữu ích bác sĩ ơi! Em chuyển ăn chay được 2 tuần đầu bị mệt lả và thèm ngọt kinh khủng, may đọc được bài hướng dẫn bổ sung đậu gà và chuối nên giờ cơ thể rất nhẹ nhõm.", Status = "ACTIVE", CreatedAt = now.AddDays(-14) },
            new() { CommentId = 4002, UserId = 6, ParentCommentId = 4001, PostId = 1021, Content = "Chúc mừng em nhé! Cảm giác thèm ngọt 2 tuần đầu là do cơ thể đang thích nghi chuyển đổi nhiên liệu từ chất béo sang glucose chậm. Cứ duy trì hạt dinh dưỡng và quả mọng nha.", Status = "ACTIVE", CreatedAt = now.AddDays(-14) },
            new() { CommentId = 4003, UserId = 8, PostId = 1021, Content = "Chuẩn luôn ạ! Khi mới ăn chay nhiều bạn chỉ ăn rau luộc với đậu phụ rán nên bị thâm hụt calo và thiếu đạm trầm trọng, bảo sao không kiệt sức.", Status = "ACTIVE", CreatedAt = now.AddDays(-13) },

            new() { CommentId = 4004, UserId = 9, PostId = 1022, Content = "Bác sĩ cho em hỏi liều B12 1000mcg uống 2 lần/tuần hay 500mcg mỗi ngày thì hấp thu tốt hơn ạ?", Status = "ACTIVE", CreatedAt = now.AddDays(-12) },
            new() { CommentId = 4005, UserId = 6, ParentCommentId = 4004, PostId = 1022, Content = "Cả hai phác đồ đều rất tốt em nhé. Uống 500mcg mỗi ngày dạng ngậm dưới lưỡi (Methylcobalamin) hấp thu thụ động rất ổn định cho người ăn chay trường.", Status = "ACTIVE", CreatedAt = now.AddDays(-12) },
            new() { CommentId = 4006, UserId = 12, PostId = 1022, Content = "Mình ăn chay 4 năm đi xét nghiệm máu định kỳ các chỉ số Sắt, Ferritin và B12 đều ở mức lý tưởng. Mẹo vắt chanh vào rau bina của bác sĩ cực kỳ chuẩn xác!", Status = "ACTIVE", CreatedAt = now.AddDays(-11) },

            new() { CommentId = 4007, UserId = 11, PostId = 1023, Content = "Em đã áp dụng thực đơn Eat Clean này được 3 tuần và giảm được 2.2kg mỡ bụng rồi chị Phương ơi! Không hề bị đói lả như những lần nhịn ăn trước.", Status = "ACTIVE", CreatedAt = now.AddDays(-10) },
            new() { CommentId = 4008, UserId = 9, ParentCommentId = 4007, PostId = 1023, Content = "Tuyệt vời quá em! Nhớ uống đủ 2-2.5 lít nước mỗi ngày và duy trì 30 phút yoga buổi sáng để cơ thể đào thải độc tố tốt hơn nha.", Status = "ACTIVE", CreatedAt = now.AddDays(-10) },
            new() { CommentId = 4009, UserId = 14, PostId = 1023, Content = "Đĩa ăn 50% chất xơ và 25% đạm thực vật này nhìn rất bắt mắt, ăn ngon miệng không hề thấy nhàm chán.", Status = "ACTIVE", CreatedAt = now.AddDays(-9) },

            new() { CommentId = 4010, UserId = 13, PostId = 1024, Content = "Bí quyết bọc khăn giấy vào hộp rau của Bếp trưởng đỉnh thật! Xà lách với ngò rí để 10 ngày trong ngăn mát vẫn xanh mướt giòn tan.", Status = "ACTIVE", CreatedAt = now.AddDays(-8) },
            new() { CommentId = 4011, UserId = 2, ParentCommentId = 4010, PostId = 1024, Content = "Cảm ơn bạn! Lưu ý rau phải thật ráo nước trước khi bọc nhé, chỉ cần một giọt nước đọng là rau dễ bị thối nhũn ngay đấy.", Status = "ACTIVE", CreatedAt = now.AddDays(-8) },
            new() { CommentId = 4012, UserId = 15, PostId = 1024, Content = "Chef cho em hỏi công thức sốt mè rang bảo quản được bao lâu trong tủ lạnh ạ?", Status = "ACTIVE", CreatedAt = now.AddDays(-8) },
            new() { CommentId = 4013, UserId = 2, ParentCommentId = 4012, PostId = 1024, Content = "Đựng trong hũ thủy tinh tiệt trùng để ngăn mát tủ lạnh dùng thoải mái trong 10 đến 14 ngày em nhé.", Status = "ACTIVE", CreatedAt = now.AddDays(-7) },

            new() { CommentId = 4014, UserId = 8, PostId = 1025, Content = "Kinh nghiệm sinh tồn quá chuẩn! Đi liên hoan công ty mình toàn gọi nấm nướng muối ớt, đậu hũ lướt ván và rau luộc chấm kho quẹt chay, vừa vui vẻ vừa thanh đạm.", Status = "ACTIVE", CreatedAt = now.AddDays(-6) },
            new() { CommentId = 4015, UserId = 10, ParentCommentId = 4014, PostId = 1025, Content = "Đúng rồi bạn, quan trọng là tâm thế cởi mở vui vẻ không phán xét, bạn bè đồng nghiệp xung quanh cũng sẽ rất tôn trọng và ủng hộ mình.", Status = "ACTIVE", CreatedAt = now.AddDays(-6) },

            new() { CommentId = 4016, UserId = 9, PostId = 1026, Content = "Nhìn ảnh da của bạn trước và sau 3 tháng mà ngỡ ngàng! Mình cũng xác nhận từ ngày bỏ hẳn sữa bò là mụn lưng và mụn nội tiết biến mất 100%.", Status = "ACTIVE", CreatedAt = now.AddDays(-5) },
            new() { CommentId = 4017, UserId = 11, ParentCommentId = 4016, PostId = 1026, Content = "Dạ đúng rồi chị, sữa bò chứa nhiều hormone làm tuyến bã nhờn hoạt động quá mức, chuyển qua sữa hạt tự làm da dẻ căng bóng hẳn.", Status = "ACTIVE", CreatedAt = now.AddDays(-4) },

            new() { CommentId = 4018, UserId = 4, PostId = 1027, Content = "Thầy cũng luôn dùng công thức mía lau và bắp ngọt để ninh nước lèo bún chay cho chùa. Vị ngọt thanh tao không bao giờ bị khát nước như hạt nêm công nghiệp.", Status = "ACTIVE", CreatedAt = now.AddDays(-3) },
            new() { CommentId = 4019, UserId = 3, ParentCommentId = 4018, PostId = 1027, Content = "Dạ bạch Thầy, nướng sơ củ sắn và nấm hương trước khi ninh sẽ giúp nước dùng thơm sâu và có màu hổ phách rất đẹp mắt nữa ạ.", Status = "ACTIVE", CreatedAt = now.AddDays(-2) },

            new() { CommentId = 4020, UserId = 12, PostId = 1028, Content = "Anh Nam cho em hỏi Seitan tự làm từ bột mì số 11 có bị khó tiêu không ạ? Em tập tạ ngày ăn khoảng 150g.", Status = "ACTIVE", CreatedAt = now.AddDays(-2) },
            new() { CommentId = 4021, UserId = 8, ParentCommentId = 4020, PostId = 1028, Content = "Nếu em không bị dị ứng Gluten (Celiac) thì ăn hoàn toàn bình thường nhé. Khi luộc nhớ cho chút gừng và sả để trợ tiêu hóa tốt hơn.", Status = "ACTIVE", CreatedAt = now.AddDays(-1) },

            new() { CommentId = 4022, UserId = 15, PostId = 1029, Content = "Đọc bài của cô Tuệ An thấy lòng nhẹ nhàng bình yên quá. Đúng là bữa cơm chay ăn trong chánh niệm thì từng miếng đậu, cọng rau đều thơm ngọt lạ kỳ.", Status = "ACTIVE", CreatedAt = now.AddDays(-1) },

            new() { CommentId = 4023, UserId = 6, PostId = 1030, Content = "Bài viết rất khoa học và thực tế! Rất nhiều mẹ lo con ăn chay bị lùn hay thiếu chất, nhưng thực tế canxi trong mè đen, đậu hũ và rau cải xoăn còn dễ hấp thu hơn sữa bò nếu kết hợp đủ Vitamin D3-K2.", Status = "ACTIVE", CreatedAt = now.AddHours(-12) },
            new() { CommentId = 4024, UserId = 7, ParentCommentId = 4023, PostId = 1030, Content = "Dạ cảm ơn bác sĩ Lan! Hai bé nhà em trộm vía chiều cao luôn đứng top đầu của lớp, sức đề kháng rất tốt ít khi ốm vặt.", Status = "ACTIVE", CreatedAt = now.AddHours(-6) },

            new() { CommentId = 4025, UserId = 14, PostId = 1011, Content = "Vừa nấu thử nồi canh chua theo đúng video hướng dẫn trưa nay, cả nhà ăn khen nức nở! Nước canh trong veo ngọt lịm. Cảm ơn Chef Thanh Hòa rất nhiều!", Status = "ACTIVE", CreatedAt = now.AddHours(-2) }
        };

        db.Comments.AddRange(comments);
        db.SaveChanges();

        // 6. Nạp lượt Vote tích cực từ các User (mỗi User 1 vote / bài viết)
        var postIds = new[] { 1011, 1012, 1013, 1014, 1015, 1016, 1021, 1022, 1023, 1024, 1025, 1026, 1027, 1028, 1029, 1030 };
        var voterIds = Enumerable.Range(1, 15).ToList();

        foreach (var postId in postIds)
        {
            foreach (var userId in voterIds)
            {
                if (!db.Votes.Any(v => v.PostId == postId && v.UserId == userId))
                {
                    db.Votes.Add(new Vote
                    {
                        PostId = postId,
                        UserId = userId,
                        CreatedAt = now.AddHours(-new Random(postId * 100 + userId).Next(1, 240))
                    });
                }
            }
        }

        db.SaveChanges();
    }
}

