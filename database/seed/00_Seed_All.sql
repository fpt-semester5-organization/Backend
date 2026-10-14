-- ============================================================================
-- File: 00_Seed_All.sql
-- Thư mục: database/seed/
-- Mục đích: Kịch bản tổng hợp toàn bộ Master Data & Seed Data cho VeggieLifeDb:
--   1. 07 Trường phái ăn chay (DietarySchools) kèm quy tắc cấm JSON
--   2. 14 Nhóm dị ứng thực phẩm (Allergens) theo tiêu chuẩn FDA & Bộ Y tế
--   3. 15 Danh mục món ăn chay (FoodCategories)
--   4. 100 Nguyên liệu chuẩn hóa (Ingredients) có định mức calo, cờ ngũ vị tân, nhóm thực phẩm
--   5. Ánh xạ nguyên liệu và chất dị ứng (IngredientAllergens)
--   6. 65 Mã phụ gia thực phẩm E-Number quốc tế (ENumberDictionary)
--   7. Tài khoản Administrator đầu tiên (Users) hỗ trợ biến môi trường SQLCMD
--   8. 03 Gói hội viên VIP (SubscriptionPlans: 49.000đ, 129.000đ, 449.000đ)
--
-- Tính chất: IDEMPOTENT (Chạy 2 lần hoặc nhiều lần không trùng dữ liệu, không lỗi PK/Unique)
-- ============================================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'============================================================================';
PRINT N'  VeggieLife Database Master Seed Script (Toàn diện & Idempotent)';
PRINT N'============================================================================';

-- 1. SEED DIETARY SCHOOLS
:r "01_Seed_DietarySchools.sql"
GO

-- 2. SEED ALLERGENS
:r "02_Seed_Allergens.sql"
GO

-- 3. SEED FOOD CATEGORIES
:r "03_Seed_FoodCategories.sql"
GO

-- 4. SEED INGREDIENTS
:r "04_Seed_Ingredients.sql"
GO

-- 5. SEED INGREDIENT ALLERGENS
:r "05_Seed_IngredientAllergens.sql"
GO

-- 6. SEED E-NUMBER DICTIONARY
:r "06_Seed_ENumberDictionary.sql"
GO

-- 7. SEED ADMIN USER
:r "07_Seed_AdminUser.sql"
GO

-- 8. SEED SUBSCRIPTION PLANS
:r "08_Seed_SubscriptionPlans.sql"
GO

PRINT N'============================================================================';
PRINT N'  HOÀN TẤT THỰC THI TOÀN BỘ MASTER DATA CHO VEGGIELIFE DB THÀNH CÔNG!';
PRINT N'============================================================================';
GO

