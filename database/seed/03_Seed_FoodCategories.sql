-- ============================================================
-- File: 03_Seed_FoodCategories.sql
-- Thư mục: database/seed/
-- Mục đích: Nạp sẵn danh mục phân loại ẩm thực và các nhóm món ăn thuần chay hệ thống.
-- Tính chất: Idempotent - chạy nhiều lần không trùng dữ liệu (MERGE).
-- ============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'>> [Seed 03/08] Đang nạp dữ liệu Master Danh Mục Món Ăn (FoodCategories)...';

SET NOCOUNT ON;

DECLARE @Now DATETIME2(7) = SYSDATETIME();

MERGE INTO dbo.FoodCategories AS target
USING (
    VALUES
        (N'Món Cơm & Xôi Chay', N'Cơm tấm sườn bì chay, cơm chiên dương châu chay, xôi gấc, xôi nếp cẩm hạt sen', 1, 1),
        (N'Món Mì, Bún & Phở Chay', N'Phở chay Hà Nội nước dùng thanh ngọt, bún riêu cua chay từ đậu hũ, hủ tiếu nam vang chay, mì xào giòn rau củ', 1, 1),
        (N'Món Kho Chay Đậm Đà', N'Đậu hũ kho nấm rơm, nấm đông cô kho tiêu xanh, củ sen kho sả ớt, sườn non chay rim mặn ngọt', 1, 1),
        (N'Món Xào Chay Thanh Vị', N'Rau củ thập cẩm xào dầu hào chay nấm hương, bông cải xanh xào nấm bào ngư, đậu cô ve xào tàu hũ ky', 1, 1),
        (N'Món Canh & Súp Bổ Dưỡng', N'Canh chua cay thanh mát rau bổi, canh rong biển đậu hũ non, súp bí đỏ hạt sen dưỡng sinh, súp kem nấm', 1, 1),
        (N'Món Gỏi & Salad Tươi Mát', N'Gỏi ngó sen tôm chay ngũ sắc, gỏi hoa chuối tai nấm mè rang, salad rau mầm dầu giấm chanh leo, salad quinoa bơ sáp', 1, 1),
        (N'Món Chiên & Rán Giòn Rụm', N'Chả giò chay giòn rụm, nấm đùi gà chiên xù sốt mayonnaise thuần chay, đậu hũ lướt ván, bánh xèo chay giòn tan', 1, 1),
        (N'Món Hấp & Luộc Dưỡng Sinh', N'Rau củ luộc chấm kho quẹt chay tóp mỡ bánh mì, nấm bào ngư hấp sả gừng, chả lụa chay hấp lá chuối', 1, 1),
        (N'Món Lẩu Chay Gia Đình', N'Lẩu nấm dưỡng sinh thảo mộc, lẩu thái tomyum chua cay chay, lẩu chao môn thanh đạm, lẩu tiêu xanh đậm đà', 1, 1),
        (N'Món Nướng Chay', N'Nấm đùi gà nướng sa tế cay, cà tím nướng mỡ hành chay chấm nước mắm gừng, sườn chay nướng ngũ vị', 0, 1),
        (N'Món Cuốn Chay', N'Gỏi cuốn chay rau sống chấm tương đen hạt dẻ, bì cuốn thính gạo thơm, phở cuốn thanh mát', 1, 1),
        (N'Món Tráng Miệng & Chè', N'Chè dưỡng nhan tuyết yến long nhãn, chè hạt sen củ năng thanh mát, bánh flan sữa hạnh nhân cốt dừa', 0, 1),
        (N'Đồ Uống & Trà Dinh Dưỡng', N'Sữa hạt điều óc chó yến mạch nguyên chất, trà gạo lứt đậu đen rang mộc, sinh tố bơ chuối cải kale', 0, 1),
        (N'Nước Chấm & Sốt Gia Vị Chay', N'Nước mắm chay ủ từ quả dứa và nấm rơm, sốt mè rang chấm rau củ, sốt ớt chua ngọt thủ công', 0, 1),
        (N'Bánh Mặn & Bánh Ngọt Chay', N'Bánh bao chay nhân nấm miến mộc nhĩ, bánh mì kẹp chả lụa pate nấm, bánh mì hoa cúc thuần chay', 0, 1)
) AS source (Category_Name, Description, Is_Core, Is_Active)
ON (target.Category_Name = source.Category_Name)
WHEN MATCHED THEN
    UPDATE SET 
        target.Description = source.Description,
        target.Is_Core = source.Is_Core,
        target.Is_Active = source.Is_Active,
        target.Updated_At = @Now
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Category_Name, Description, Is_Core, Is_Active, Created_At, Updated_At)
    VALUES (source.Category_Name, source.Description, source.Is_Core, source.Is_Active, @Now, @Now);

PRINT N'   -> Hoàn tất nạp 15 Danh Mục Món Ăn thành công!';
GO
