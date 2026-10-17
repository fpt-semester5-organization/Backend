-- Tự động sinh dữ liệu mẫu
SET IDENTITY_INSERT [dbo].[Users] ON;
MERGE [dbo].[Users] AS Target USING (VALUES (1, 'mockuser', 'mock@test.com', 'pwd', 'ACTIVE')) AS Source ([User_ID], [Username], [Email], [Password_Hash], [Account_Status]) ON Target.[User_ID] = Source.[User_ID] WHEN NOT MATCHED THEN INSERT ([User_ID], [Username], [Email], [Password_Hash], [Account_Status]) VALUES (Source.[User_ID], Source.[Username], Source.[Email], Source.[Password_Hash], Source.[Account_Status]);
SET IDENTITY_INSERT [dbo].[Users] OFF;

SET IDENTITY_INSERT [dbo].[DietarySchools] ON;
MERGE [dbo].[DietarySchools] AS Target USING (VALUES 
    (1, N'Thuần chay', 'VEGAN', '{"no_meat": true, "no_dairy": true, "no_eggs": true, "no_honey": true}'),
    (2, N'Chay có trứng', 'OVO', '{"no_meat": true, "no_dairy": true, "no_eggs": false}'),
    (3, N'Chay có sữa', 'LACTO', '{"no_meat": true, "no_dairy": false, "no_eggs": true}'),
    (4, N'Chay có trứng và sữa', 'LACTO_OVO', '{"no_meat": true, "no_dairy": false, "no_eggs": false}'),
    (5, N'Chay Phật giáo', 'BUDDHIST', '{"no_meat": true, "no_dairy": false, "no_eggs": true, "no_alliums": true}'),
    (6, N'Chay bán phần (Gia cầm)', 'POLLOTARIAN', '{"no_red_meat": true, "no_seafood": true}'),
    (7, N'Chay bán phần (Hải sản)', 'PESCATARIAN', '{"no_red_meat": true, "no_poultry": true}')) AS Source ([Dietary_School_ID], [School_Name], [School_Code], [Forbidden_Food_Rules]) ON Target.[Dietary_School_ID] = Source.[Dietary_School_ID] WHEN NOT MATCHED THEN INSERT ([Dietary_School_ID], [School_Name], [School_Code], [Forbidden_Food_Rules]) VALUES (Source.[Dietary_School_ID], Source.[School_Name], Source.[School_Code], Source.[Forbidden_Food_Rules]);
SET IDENTITY_INSERT [dbo].[DietarySchools] OFF;

SET IDENTITY_INSERT [dbo].[FoodCategories] ON;
MERGE [dbo].[FoodCategories] AS Target USING (VALUES 
    (1, N'Bữa sáng', N'Các món ăn nhẹ cho buổi sáng', 'breakfast.jpg'),
    (2, N'Bữa trưa', N'Các món ăn chính cho buổi trưa', 'lunch.jpg'),
    (3, N'Bữa tối', N'Các món ăn dinh dưỡng buổi tối', 'dinner.jpg')) AS Source ([Category_ID], [Category_Name], [Description], [Icon_Url]) ON Target.[Category_ID] = Source.[Category_ID] WHEN NOT MATCHED THEN INSERT ([Category_ID], [Category_Name], [Description], [Icon_Url]) VALUES (Source.[Category_ID], Source.[Category_Name], Source.[Description], Source.[Icon_Url]);
SET IDENTITY_INSERT [dbo].[FoodCategories] OFF;

MERGE [dbo].[Recipes] AS Target USING (VALUES 
    (1, 2, 2, N'Công thức mẫu số 1 - Chay có trứng', N'Mô tả cho công thức 1', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 3, 3, N'Công thức mẫu số 2 - Chay có sữa', N'Mô tả cho công thức 2', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 1, 4, N'Công thức mẫu số 3 - Chay có trứng và sữa', N'Mô tả cho công thức 3', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 2, 5, N'Công thức mẫu số 4 - Chay Phật giáo', N'Mô tả cho công thức 4', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 3, 6, N'Công thức mẫu số 5 - Chay bán phần (Gia cầm)', N'Mô tả cho công thức 5', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 1, 7, N'Công thức mẫu số 6 - Chay bán phần (Hải sản)', N'Mô tả cho công thức 6', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 2, 1, N'Công thức mẫu số 7 - Thuần chay', N'Mô tả cho công thức 7', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 3, 2, N'Công thức mẫu số 8 - Chay có trứng', N'Mô tả cho công thức 8', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 1, 3, N'Công thức mẫu số 9 - Chay có sữa', N'Mô tả cho công thức 9', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 2, 4, N'Công thức mẫu số 10 - Chay có trứng và sữa', N'Mô tả cho công thức 10', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 3, 5, N'Công thức mẫu số 11 - Chay Phật giáo', N'Mô tả cho công thức 11', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 1, 6, N'Công thức mẫu số 12 - Chay bán phần (Gia cầm)', N'Mô tả cho công thức 12', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 2, 7, N'Công thức mẫu số 13 - Chay bán phần (Hải sản)', N'Mô tả cho công thức 13', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 3, 1, N'Công thức mẫu số 14 - Thuần chay', N'Mô tả cho công thức 14', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 1, 2, N'Công thức mẫu số 15 - Chay có trứng', N'Mô tả cho công thức 15', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 2, 3, N'Công thức mẫu số 16 - Chay có sữa', N'Mô tả cho công thức 16', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 3, 4, N'Công thức mẫu số 17 - Chay có trứng và sữa', N'Mô tả cho công thức 17', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 1, 5, N'Công thức mẫu số 18 - Chay Phật giáo', N'Mô tả cho công thức 18', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 2, 6, N'Công thức mẫu số 19 - Chay bán phần (Gia cầm)', N'Mô tả cho công thức 19', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 3, 7, N'Công thức mẫu số 20 - Chay bán phần (Hải sản)', N'Mô tả cho công thức 20', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 1, 1, N'Công thức mẫu số 21 - Thuần chay', N'Mô tả cho công thức 21', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 2, 2, N'Công thức mẫu số 22 - Chay có trứng', N'Mô tả cho công thức 22', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 3, 3, N'Công thức mẫu số 23 - Chay có sữa', N'Mô tả cho công thức 23', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 1, 4, N'Công thức mẫu số 24 - Chay có trứng và sữa', N'Mô tả cho công thức 24', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 2, 5, N'Công thức mẫu số 25 - Chay Phật giáo', N'Mô tả cho công thức 25', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 3, 6, N'Công thức mẫu số 26 - Chay bán phần (Gia cầm)', N'Mô tả cho công thức 26', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 1, 7, N'Công thức mẫu số 27 - Chay bán phần (Hải sản)', N'Mô tả cho công thức 27', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 2, 1, N'Công thức mẫu số 28 - Thuần chay', N'Mô tả cho công thức 28', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 3, 2, N'Công thức mẫu số 29 - Chay có trứng', N'Mô tả cho công thức 29', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE'),
    (1, 1, 3, N'Công thức mẫu số 30 - Chay có sữa', N'Mô tả cho công thức 30', N'Bước 1: Làm sạch. Bước 2: Nấu ăn.', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE')) AS Source ([User_ID], [Category_ID], [Dietary_School_ID], [Title], [Description], [Instructions], [Prep_Time_Minutes], [Cook_Time_Minutes], [Servings], [Calories_Kcal], [Difficulty_Level], [Status]) ON Target.[Title] = Source.[Title] WHEN NOT MATCHED THEN INSERT ([User_ID], [Category_ID], [Dietary_School_ID], [Title], [Description], [Instructions], [Prep_Time_Minutes], [Cook_Time_Minutes], [Servings], [Calories_Kcal], [Difficulty_Level], [Status]) VALUES (Source.[User_ID], Source.[Category_ID], Source.[Dietary_School_ID], Source.[Title], Source.[Description], Source.[Instructions], Source.[Prep_Time_Minutes], Source.[Cook_Time_Minutes], Source.[Servings], Source.[Calories_Kcal], Source.[Difficulty_Level], Source.[Status]);

MERGE [dbo].[Posts] AS Target USING (VALUES 
    (1, N'Bài viết mẫu số 1', N'Nội dung bài viết chia sẻ ẩm thực chay số 1', 'APPROVED_ACTIVE'),
    (1, N'Bài viết mẫu số 2', N'Nội dung bài viết chia sẻ ẩm thực chay số 2', 'APPROVED_ACTIVE'),
    (1, N'Bài viết mẫu số 3', N'Nội dung bài viết chia sẻ ẩm thực chay số 3', 'APPROVED_ACTIVE'),
    (1, N'Bài viết mẫu số 4', N'Nội dung bài viết chia sẻ ẩm thực chay số 4', 'APPROVED_ACTIVE'),
    (1, N'Bài viết mẫu số 5', N'Nội dung bài viết chia sẻ ẩm thực chay số 5', 'APPROVED_ACTIVE'),
    (1, N'Bài viết mẫu số 6', N'Nội dung bài viết chia sẻ ẩm thực chay số 6', 'APPROVED_ACTIVE'),
    (1, N'Bài viết mẫu số 7', N'Nội dung bài viết chia sẻ ẩm thực chay số 7', 'APPROVED_ACTIVE'),
    (1, N'Bài viết mẫu số 8', N'Nội dung bài viết chia sẻ ẩm thực chay số 8', 'APPROVED_ACTIVE'),
    (1, N'Bài viết mẫu số 9', N'Nội dung bài viết chia sẻ ẩm thực chay số 9', 'APPROVED_ACTIVE'),
    (1, N'Bài viết mẫu số 10', N'Nội dung bài viết chia sẻ ẩm thực chay số 10', 'APPROVED_ACTIVE')) AS Source ([User_ID], [Title], [Content], [Moderation_Status]) ON Target.[Title] = Source.[Title] WHEN NOT MATCHED THEN INSERT ([User_ID], [Title], [Content], [Moderation_Status]) VALUES (Source.[User_ID], Source.[Title], Source.[Content], Source.[Moderation_Status]);

