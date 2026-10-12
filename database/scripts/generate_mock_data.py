import os
import random

sql_file = r"d:\GITHUB\Gruop\Backend\database\seed\99_MockData.sql"

schools = [
    (1, 'Thuần chay', 'VEGAN', '{"no_meat": true, "no_dairy": true, "no_eggs": true, "no_honey": true}'),
    (2, 'Chay có trứng', 'OVO', '{"no_meat": true, "no_dairy": true, "no_eggs": false}'),
    (3, 'Chay có sữa', 'LACTO', '{"no_meat": true, "no_dairy": false, "no_eggs": true}'),
    (4, 'Chay có trứng và sữa', 'LACTO_OVO', '{"no_meat": true, "no_dairy": false, "no_eggs": false}'),
    (5, 'Chay Phật giáo', 'BUDDHIST', '{"no_meat": true, "no_dairy": false, "no_eggs": true, "no_alliums": true}'),
    (6, 'Chay bán phần (Gia cầm)', 'POLLOTARIAN', '{"no_red_meat": true, "no_seafood": true}'),
    (7, 'Chay bán phần (Hải sản)', 'PESCATARIAN', '{"no_red_meat": true, "no_poultry": true}')
]

categories = [
    (1, 'Bữa sáng', 'Các món ăn nhẹ cho buổi sáng', 'breakfast.jpg'),
    (2, 'Bữa trưa', 'Các món ăn chính cho buổi trưa', 'lunch.jpg'),
    (3, 'Bữa tối', 'Các món ăn dinh dưỡng buổi tối', 'dinner.jpg')
]

with open(sql_file, "w", encoding="utf-8") as f:
    f.write("-- Tự động sinh dữ liệu mẫu\n")
    f.write("SET IDENTITY_INSERT [dbo].[Users] ON;\n")
    f.write("MERGE [dbo].[Users] AS Target USING (VALUES (1, 'mockuser', 'mock@test.com', 'pwd', 'ACTIVE')) AS Source ([User_ID], [Username], [Email], [Password_Hash], [Account_Status]) ON Target.[User_ID] = Source.[User_ID] WHEN NOT MATCHED THEN INSERT ([User_ID], [Username], [Email], [Password_Hash], [Account_Status]) VALUES (Source.[User_ID], Source.[Username], Source.[Email], Source.[Password_Hash], Source.[Account_Status]);\n")
    f.write("SET IDENTITY_INSERT [dbo].[Users] OFF;\n\n")

    f.write("SET IDENTITY_INSERT [dbo].[DietarySchools] ON;\n")
    f.write("MERGE [dbo].[DietarySchools] AS Target USING (VALUES \n")
    vals = []
    for s in schools:
        vals.append(f"    ({s[0]}, N'{s[1]}', '{s[2]}', '{s[3]}')")
    f.write(",\n".join(vals))
    f.write(") AS Source ([Dietary_School_ID], [School_Name], [School_Code], [Forbidden_Food_Rules]) ON Target.[Dietary_School_ID] = Source.[Dietary_School_ID] WHEN NOT MATCHED THEN INSERT ([Dietary_School_ID], [School_Name], [School_Code], [Forbidden_Food_Rules]) VALUES (Source.[Dietary_School_ID], Source.[School_Name], Source.[School_Code], Source.[Forbidden_Food_Rules]);\n")
    f.write("SET IDENTITY_INSERT [dbo].[DietarySchools] OFF;\n\n")

    f.write("SET IDENTITY_INSERT [dbo].[FoodCategories] ON;\n")
    f.write("MERGE [dbo].[FoodCategories] AS Target USING (VALUES \n")
    vals = []
    for c in categories:
        vals.append(f"    ({c[0]}, N'{c[1]}', N'{c[2]}', '{c[3]}')")
    f.write(",\n".join(vals))
    f.write(") AS Source ([Category_ID], [Category_Name], [Description], [Icon_Url]) ON Target.[Category_ID] = Source.[Category_ID] WHEN NOT MATCHED THEN INSERT ([Category_ID], [Category_Name], [Description], [Icon_Url]) VALUES (Source.[Category_ID], Source.[Category_Name], Source.[Description], Source.[Icon_Url]);\n")
    f.write("SET IDENTITY_INSERT [dbo].[FoodCategories] OFF;\n\n")

    f.write("MERGE [dbo].[Recipes] AS Target USING (VALUES \n")
    vals = []
    for i in range(1, 31):
        school_id = (i % 7) + 1
        cat_id = (i % 3) + 1
        title = f"Công thức mẫu số {i} - {schools[school_id-1][1]}"
        desc = f"Mô tả cho công thức {i}"
        inst = f"Bước 1: Làm sạch. Bước 2: Nấu ăn."
        vals.append(f"    (1, {cat_id}, {school_id}, N'{title}', N'{desc}', N'{inst}', 15, 20, 2, 250.0, 'EASY', 'APPROVED_ACTIVE')")
    f.write(",\n".join(vals))
    f.write(") AS Source ([User_ID], [Category_ID], [Dietary_School_ID], [Title], [Description], [Instructions], [Prep_Time_Minutes], [Cook_Time_Minutes], [Servings], [Calories_Kcal], [Difficulty_Level], [Status]) ON Target.[Title] = Source.[Title] WHEN NOT MATCHED THEN INSERT ([User_ID], [Category_ID], [Dietary_School_ID], [Title], [Description], [Instructions], [Prep_Time_Minutes], [Cook_Time_Minutes], [Servings], [Calories_Kcal], [Difficulty_Level], [Status]) VALUES (Source.[User_ID], Source.[Category_ID], Source.[Dietary_School_ID], Source.[Title], Source.[Description], Source.[Instructions], Source.[Prep_Time_Minutes], Source.[Cook_Time_Minutes], Source.[Servings], Source.[Calories_Kcal], Source.[Difficulty_Level], Source.[Status]);\n\n")

    f.write("MERGE [dbo].[Posts] AS Target USING (VALUES \n")
    vals = []
    for i in range(1, 11):
        title = f"Bài viết mẫu số {i}"
        content = f"Nội dung bài viết chia sẻ ẩm thực chay số {i}"
        vals.append(f"    (1, N'{title}', N'{content}', 'APPROVED_ACTIVE')")
    f.write(",\n".join(vals))
    f.write(") AS Source ([User_ID], [Title], [Content], [Moderation_Status]) ON Target.[Title] = Source.[Title] WHEN NOT MATCHED THEN INSERT ([User_ID], [Title], [Content], [Moderation_Status]) VALUES (Source.[User_ID], Source.[Title], Source.[Content], Source.[Moderation_Status]);\n\n")

print(f"Created {sql_file}")
