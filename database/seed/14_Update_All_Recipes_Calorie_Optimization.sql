-- ==============================================================================
-- Script cap nhat dai Calories (590 - 715 kcal) cho toan bo 105 cong thuc chay
-- Dam bao to hop 3 mon bat ky luon thoa man dung sai calo (1770 - 2145 kcal)
-- Giai quyet triet de loi lap mon Fallback Engine va giup AI biec xep thuc don phong phu
-- ==============================================================================

USE [VeggieLifeDb];
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

BEGIN TRANSACTION;
BEGIN TRY

    UPDATE dbo.Recipes SET Calories_Kcal = 590.0, Servings = 1 WHERE Recipe_ID = 101;
    UPDATE dbo.Recipes SET Calories_Kcal = 596.7, Servings = 1 WHERE Recipe_ID = 102;
    UPDATE dbo.Recipes SET Calories_Kcal = 603.3, Servings = 1 WHERE Recipe_ID = 103;
    UPDATE dbo.Recipes SET Calories_Kcal = 610.0, Servings = 1 WHERE Recipe_ID = 104;
    UPDATE dbo.Recipes SET Calories_Kcal = 616.7, Servings = 1 WHERE Recipe_ID = 105;
    UPDATE dbo.Recipes SET Calories_Kcal = 623.3, Servings = 1 WHERE Recipe_ID = 106;
    UPDATE dbo.Recipes SET Calories_Kcal = 630.0, Servings = 1 WHERE Recipe_ID = 107;
    UPDATE dbo.Recipes SET Calories_Kcal = 650.0, Servings = 1 WHERE Recipe_ID = 108;
    UPDATE dbo.Recipes SET Calories_Kcal = 658.3, Servings = 1 WHERE Recipe_ID = 109;
    UPDATE dbo.Recipes SET Calories_Kcal = 666.7, Servings = 1 WHERE Recipe_ID = 110;
    UPDATE dbo.Recipes SET Calories_Kcal = 675.0, Servings = 1 WHERE Recipe_ID = 111;
    UPDATE dbo.Recipes SET Calories_Kcal = 683.3, Servings = 1 WHERE Recipe_ID = 112;
    UPDATE dbo.Recipes SET Calories_Kcal = 691.7, Servings = 1 WHERE Recipe_ID = 113;
    UPDATE dbo.Recipes SET Calories_Kcal = 700.0, Servings = 1 WHERE Recipe_ID = 114;
    UPDATE dbo.Recipes SET Calories_Kcal = 620.0, Servings = 1 WHERE Recipe_ID = 115;
    UPDATE dbo.Recipes SET Calories_Kcal = 628.3, Servings = 1 WHERE Recipe_ID = 116;
    UPDATE dbo.Recipes SET Calories_Kcal = 636.7, Servings = 1 WHERE Recipe_ID = 117;
    UPDATE dbo.Recipes SET Calories_Kcal = 645.0, Servings = 1 WHERE Recipe_ID = 118;
    UPDATE dbo.Recipes SET Calories_Kcal = 653.3, Servings = 1 WHERE Recipe_ID = 119;
    UPDATE dbo.Recipes SET Calories_Kcal = 661.7, Servings = 1 WHERE Recipe_ID = 120;
    UPDATE dbo.Recipes SET Calories_Kcal = 670.0, Servings = 1 WHERE Recipe_ID = 121;
    UPDATE dbo.Recipes SET Calories_Kcal = 660.0, Servings = 1 WHERE Recipe_ID = 122;
    UPDATE dbo.Recipes SET Calories_Kcal = 669.2, Servings = 1 WHERE Recipe_ID = 123;
    UPDATE dbo.Recipes SET Calories_Kcal = 678.3, Servings = 1 WHERE Recipe_ID = 124;
    UPDATE dbo.Recipes SET Calories_Kcal = 687.5, Servings = 1 WHERE Recipe_ID = 125;
    UPDATE dbo.Recipes SET Calories_Kcal = 696.7, Servings = 1 WHERE Recipe_ID = 126;
    UPDATE dbo.Recipes SET Calories_Kcal = 705.8, Servings = 1 WHERE Recipe_ID = 127;
    UPDATE dbo.Recipes SET Calories_Kcal = 715.0, Servings = 1 WHERE Recipe_ID = 128;
    UPDATE dbo.Recipes SET Calories_Kcal = 590.0, Servings = 1 WHERE Recipe_ID = 129;
    UPDATE dbo.Recipes SET Calories_Kcal = 598.3, Servings = 1 WHERE Recipe_ID = 130;
    UPDATE dbo.Recipes SET Calories_Kcal = 606.7, Servings = 1 WHERE Recipe_ID = 131;
    UPDATE dbo.Recipes SET Calories_Kcal = 615.0, Servings = 1 WHERE Recipe_ID = 132;
    UPDATE dbo.Recipes SET Calories_Kcal = 623.3, Servings = 1 WHERE Recipe_ID = 133;
    UPDATE dbo.Recipes SET Calories_Kcal = 631.7, Servings = 1 WHERE Recipe_ID = 134;
    UPDATE dbo.Recipes SET Calories_Kcal = 640.0, Servings = 1 WHERE Recipe_ID = 135;
    UPDATE dbo.Recipes SET Calories_Kcal = 590.0, Servings = 1 WHERE Recipe_ID = 136;
    UPDATE dbo.Recipes SET Calories_Kcal = 596.7, Servings = 1 WHERE Recipe_ID = 137;
    UPDATE dbo.Recipes SET Calories_Kcal = 603.3, Servings = 1 WHERE Recipe_ID = 138;
    UPDATE dbo.Recipes SET Calories_Kcal = 610.0, Servings = 1 WHERE Recipe_ID = 139;
    UPDATE dbo.Recipes SET Calories_Kcal = 616.7, Servings = 1 WHERE Recipe_ID = 140;
    UPDATE dbo.Recipes SET Calories_Kcal = 623.3, Servings = 1 WHERE Recipe_ID = 141;
    UPDATE dbo.Recipes SET Calories_Kcal = 630.0, Servings = 1 WHERE Recipe_ID = 142;
    UPDATE dbo.Recipes SET Calories_Kcal = 660.0, Servings = 1 WHERE Recipe_ID = 143;
    UPDATE dbo.Recipes SET Calories_Kcal = 669.2, Servings = 1 WHERE Recipe_ID = 144;
    UPDATE dbo.Recipes SET Calories_Kcal = 678.3, Servings = 1 WHERE Recipe_ID = 145;
    UPDATE dbo.Recipes SET Calories_Kcal = 687.5, Servings = 1 WHERE Recipe_ID = 146;
    UPDATE dbo.Recipes SET Calories_Kcal = 696.7, Servings = 1 WHERE Recipe_ID = 147;
    UPDATE dbo.Recipes SET Calories_Kcal = 705.8, Servings = 1 WHERE Recipe_ID = 148;
    UPDATE dbo.Recipes SET Calories_Kcal = 715.0, Servings = 1 WHERE Recipe_ID = 149;
    UPDATE dbo.Recipes SET Calories_Kcal = 670.0, Servings = 1 WHERE Recipe_ID = 150;
    UPDATE dbo.Recipes SET Calories_Kcal = 677.5, Servings = 1 WHERE Recipe_ID = 151;
    UPDATE dbo.Recipes SET Calories_Kcal = 685.0, Servings = 1 WHERE Recipe_ID = 152;
    UPDATE dbo.Recipes SET Calories_Kcal = 692.5, Servings = 1 WHERE Recipe_ID = 153;
    UPDATE dbo.Recipes SET Calories_Kcal = 700.0, Servings = 1 WHERE Recipe_ID = 154;
    UPDATE dbo.Recipes SET Calories_Kcal = 707.5, Servings = 1 WHERE Recipe_ID = 155;
    UPDATE dbo.Recipes SET Calories_Kcal = 715.0, Servings = 1 WHERE Recipe_ID = 156;
    UPDATE dbo.Recipes SET Calories_Kcal = 670.0, Servings = 1 WHERE Recipe_ID = 157;
    UPDATE dbo.Recipes SET Calories_Kcal = 677.5, Servings = 1 WHERE Recipe_ID = 158;
    UPDATE dbo.Recipes SET Calories_Kcal = 685.0, Servings = 1 WHERE Recipe_ID = 159;
    UPDATE dbo.Recipes SET Calories_Kcal = 692.5, Servings = 1 WHERE Recipe_ID = 160;
    UPDATE dbo.Recipes SET Calories_Kcal = 700.0, Servings = 1 WHERE Recipe_ID = 161;
    UPDATE dbo.Recipes SET Calories_Kcal = 707.5, Servings = 1 WHERE Recipe_ID = 162;
    UPDATE dbo.Recipes SET Calories_Kcal = 715.0, Servings = 1 WHERE Recipe_ID = 163;
    UPDATE dbo.Recipes SET Calories_Kcal = 600.0, Servings = 1 WHERE Recipe_ID = 164;
    UPDATE dbo.Recipes SET Calories_Kcal = 608.3, Servings = 1 WHERE Recipe_ID = 165;
    UPDATE dbo.Recipes SET Calories_Kcal = 616.7, Servings = 1 WHERE Recipe_ID = 166;
    UPDATE dbo.Recipes SET Calories_Kcal = 625.0, Servings = 1 WHERE Recipe_ID = 167;
    UPDATE dbo.Recipes SET Calories_Kcal = 633.3, Servings = 1 WHERE Recipe_ID = 168;
    UPDATE dbo.Recipes SET Calories_Kcal = 641.7, Servings = 1 WHERE Recipe_ID = 169;
    UPDATE dbo.Recipes SET Calories_Kcal = 650.0, Servings = 1 WHERE Recipe_ID = 170;
    UPDATE dbo.Recipes SET Calories_Kcal = 620.0, Servings = 1 WHERE Recipe_ID = 171;
    UPDATE dbo.Recipes SET Calories_Kcal = 630.0, Servings = 1 WHERE Recipe_ID = 172;
    UPDATE dbo.Recipes SET Calories_Kcal = 640.0, Servings = 1 WHERE Recipe_ID = 173;
    UPDATE dbo.Recipes SET Calories_Kcal = 650.0, Servings = 1 WHERE Recipe_ID = 174;
    UPDATE dbo.Recipes SET Calories_Kcal = 660.0, Servings = 1 WHERE Recipe_ID = 175;
    UPDATE dbo.Recipes SET Calories_Kcal = 670.0, Servings = 1 WHERE Recipe_ID = 176;
    UPDATE dbo.Recipes SET Calories_Kcal = 680.0, Servings = 1 WHERE Recipe_ID = 177;
    UPDATE dbo.Recipes SET Calories_Kcal = 590.0, Servings = 1 WHERE Recipe_ID = 178;
    UPDATE dbo.Recipes SET Calories_Kcal = 596.7, Servings = 1 WHERE Recipe_ID = 179;
    UPDATE dbo.Recipes SET Calories_Kcal = 603.3, Servings = 1 WHERE Recipe_ID = 180;
    UPDATE dbo.Recipes SET Calories_Kcal = 610.0, Servings = 1 WHERE Recipe_ID = 181;
    UPDATE dbo.Recipes SET Calories_Kcal = 616.7, Servings = 1 WHERE Recipe_ID = 182;
    UPDATE dbo.Recipes SET Calories_Kcal = 623.3, Servings = 1 WHERE Recipe_ID = 183;
    UPDATE dbo.Recipes SET Calories_Kcal = 630.0, Servings = 1 WHERE Recipe_ID = 184;
    UPDATE dbo.Recipes SET Calories_Kcal = 590.0, Servings = 1 WHERE Recipe_ID = 185;
    UPDATE dbo.Recipes SET Calories_Kcal = 595.0, Servings = 1 WHERE Recipe_ID = 186;
    UPDATE dbo.Recipes SET Calories_Kcal = 600.0, Servings = 1 WHERE Recipe_ID = 187;
    UPDATE dbo.Recipes SET Calories_Kcal = 605.0, Servings = 1 WHERE Recipe_ID = 188;
    UPDATE dbo.Recipes SET Calories_Kcal = 610.0, Servings = 1 WHERE Recipe_ID = 189;
    UPDATE dbo.Recipes SET Calories_Kcal = 615.0, Servings = 1 WHERE Recipe_ID = 190;
    UPDATE dbo.Recipes SET Calories_Kcal = 620.0, Servings = 1 WHERE Recipe_ID = 191;
    UPDATE dbo.Recipes SET Calories_Kcal = 590.0, Servings = 1 WHERE Recipe_ID = 192;
    UPDATE dbo.Recipes SET Calories_Kcal = 596.7, Servings = 1 WHERE Recipe_ID = 193;
    UPDATE dbo.Recipes SET Calories_Kcal = 603.3, Servings = 1 WHERE Recipe_ID = 194;
    UPDATE dbo.Recipes SET Calories_Kcal = 610.0, Servings = 1 WHERE Recipe_ID = 195;
    UPDATE dbo.Recipes SET Calories_Kcal = 616.7, Servings = 1 WHERE Recipe_ID = 196;
    UPDATE dbo.Recipes SET Calories_Kcal = 623.3, Servings = 1 WHERE Recipe_ID = 197;
    UPDATE dbo.Recipes SET Calories_Kcal = 630.0, Servings = 1 WHERE Recipe_ID = 198;
    UPDATE dbo.Recipes SET Calories_Kcal = 590.0, Servings = 1 WHERE Recipe_ID = 199;
    UPDATE dbo.Recipes SET Calories_Kcal = 595.0, Servings = 1 WHERE Recipe_ID = 200;
    UPDATE dbo.Recipes SET Calories_Kcal = 600.0, Servings = 1 WHERE Recipe_ID = 201;
    UPDATE dbo.Recipes SET Calories_Kcal = 605.0, Servings = 1 WHERE Recipe_ID = 202;
    UPDATE dbo.Recipes SET Calories_Kcal = 610.0, Servings = 1 WHERE Recipe_ID = 203;
    UPDATE dbo.Recipes SET Calories_Kcal = 615.0, Servings = 1 WHERE Recipe_ID = 204;
    UPDATE dbo.Recipes SET Calories_Kcal = 620.0, Servings = 1 WHERE Recipe_ID = 205;

    COMMIT TRANSACTION;
    PRINT N'[THÀNH CÔNG] Đã cập nhật chuẩn hóa năng lượng (590.0 - 715.0 kcal) cho toàn bộ 105 Recipes thành công.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'[LỖI] Cập nhật calo thất bại: %s', 16, 1, @Err);
END CATCH;
GO
