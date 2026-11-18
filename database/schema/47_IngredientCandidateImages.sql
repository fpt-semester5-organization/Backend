-- ============================================================================
-- File: 47_IngredientCandidateImages.sql
-- Table: IngredientCandidateImages
-- Cluster: Quản trị Nguyên liệu & Ảnh đại diện chuẩn
-- Description:
--   Kho ảnh ứng viên (Candidate Pool) cắt từ ảnh tủ lạnh người dùng theo box2d (FN-VIS-01),
--   chờ Admin duyệt làm ảnh đại diện nguyên liệu (FN-ADM-12).
--   Nguồn: plan/1.Implement/32. E2E Ingredient Avatars from Fridge Vision and Admin Lock (mục 3.2)
--   - BR-ADM-ING-02: chỉ thu thập khi box2d hợp lệ, confidence >= 0.80, nguyên liệu Is_Locked = 0;
--                    tối đa 05 ảnh PENDING mới nhất mỗi nguyên liệu (FIFO).
--   - BR-ADM-ING-03: ảnh ứng viên KHÔNG tự trở thành Ingredients.Image_Url; chỉ cập nhật khi Admin duyệt.
--   Cột [Image_Url], [Is_Locked] của Ingredients đã có ở 46_Ingredients_Add_ImageUrl_IsLocked.sql.
-- ============================================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[IngredientCandidateImages]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[IngredientCandidateImages] (
        [Candidate_ID]      INT IDENTITY(1,1) NOT NULL,
        [Ingredient_ID]     INT NOT NULL,
        [Session_ID]        INT NOT NULL,
        [Cropped_Image_URL] VARCHAR(500) NOT NULL,
        [Confidence_Score]  DECIMAL(4,3) NOT NULL,
        [Status]            VARCHAR(20) NOT NULL CONSTRAINT [DF_CandidateImages_Status] DEFAULT ('PENDING'),
        [Created_At]        DATETIME2(7) NOT NULL CONSTRAINT [DF_CandidateImages_Created_At] DEFAULT (SYSDATETIME()),

        CONSTRAINT [PK_IngredientCandidateImages] PRIMARY KEY CLUSTERED ([Candidate_ID] ASC),
        CONSTRAINT [FK_IngredientCandidateImages_Ingredients] FOREIGN KEY ([Ingredient_ID]) REFERENCES [dbo].[Ingredients]([Ingredient_ID]) ON DELETE CASCADE,
        CONSTRAINT [FK_IngredientCandidateImages_FridgeSessions] FOREIGN KEY ([Session_ID]) REFERENCES [dbo].[FridgeSessions]([Session_ID]) ON DELETE CASCADE,
        CONSTRAINT [CK_CandidateImages_Status] CHECK ([Status] IN ('PENDING', 'APPROVED', 'REJECTED')),
        CONSTRAINT [CK_CandidateImages_Confidence] CHECK ([Confidence_Score] >= 0 AND [Confidence_Score] <= 1)
    );

    CREATE NONCLUSTERED INDEX [IX_CandidateImages_Ingredient_ID] ON [dbo].[IngredientCandidateImages]([Ingredient_ID] ASC, [Status] ASC);
    CREATE NONCLUSTERED INDEX [IX_CandidateImages_Session_ID] ON [dbo].[IngredientCandidateImages]([Session_ID] ASC);
    PRINT N'>> Da khoi tao bang dbo.IngredientCandidateImages thanh cong.';
END;
GO
