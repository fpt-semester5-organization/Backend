namespace VeggieLife.BLL.Common.Constants;

/// <summary>
/// Danh mục mã lỗi kỹ thuật chuẩn hóa toàn hệ thống VeggieLife (theo Data Dictionary mục 5).
/// Được ánh xạ sang thông báo tiếng Việt thân thiện ở Frontend và trả về trong Error Envelope.
/// </summary>
public static class ErrorCodes
{
    // === 1. Lỗi hệ thống & Xác thực dữ liệu chung ===
    public const string ErrInternal = "ERR_INTERNAL";
    public const string ErrValidation = "ERR_VALIDATION";
    public const string ErrSysDatabaseUnavailable = "ERR_SYS_DATABASE_UNAVAILABLE";
    public const string ErrSysRedisUnavailable = "ERR_SYS_REDIS_UNAVAILABLE";

    // === 2. Lỗi Xác thực & Phân quyền (Auth) ===
    public const string ErrAuthInvalidToken = "ERR_AUTH_INVALID_TOKEN";
    public const string ErrAuthInvalidCredentials = "ERR_AUTH_INVALID_CREDENTIALS";
    public const string ErrAuthEmailExists = "ERR_AUTH_EMAIL_EXISTS";
    public const string ErrAuthEmailNotVerified = "ERR_AUTH_EMAIL_NOT_VERIFIED";
    public const string ErrAuthAccountBanned = "ERR_AUTH_ACCOUNT_BANNED";
    public const string ErrAuthAccountPendingDeletion = "ERR_AUTH_ACCOUNT_PENDING_DELETION";
    public const string ErrAuthAccountLinkRequired = "ERR_AUTH_ACCOUNT_LINK_REQUIRED";
    public const string ErrAuthLoginLocked = "ERR_AUTH_LOGIN_LOCKED";
    public const string ErrAuthOauthFailed = "ERR_AUTH_OAUTH_FAILED";
    public const string ErrAuthOtpExpired = "ERR_AUTH_OTP_EXPIRED";
    public const string ErrAuthOtpIncorrect = "ERR_AUTH_OTP_INCORRECT";
    public const string ErrAuthOtpLocked = "ERR_AUTH_OTP_LOCKED";
    public const string ErrAuthRefreshTokenInvalid = "ERR_AUTH_REFRESH_TOKEN_INVALID";
    public const string ErrAuthRefreshTokenReused = "ERR_AUTH_REFRESH_TOKEN_REUSED";
    public const string ErrAuthWeakPassword = "ERR_AUTH_WEAK_PASSWORD";

    // === 3. Lỗi Khách vãng lai & VIP (Guest & VIP) ===
    public const string ErrGuestCommunityActionDenied = "ERR_GUEST_COMMUNITY_ACTION_DENIED";
    public const string ErrGuestPersonalizationRestricted = "ERR_GUEST_PERSONALIZATION_RESTRICTED";
    public const string ErrGuestRateLimitExceeded = "ERR_GUEST_RATE_LIMIT_EXCEEDED";
    public const string ErrGuestRegionUnsupported = "ERR_GUEST_REGION_UNSUPPORTED";
    public const string ErrVipRequired = "ERR_VIP_REQUIRED";

    // === 4. Lỗi Định mức sử dụng (Quota) ===
    public const string ErrQuotaAiExceeded = "ERR_QUOTA_AI_EXCEEDED";
    public const string ErrQuotaFridgeExceeded = "ERR_QUOTA_FRIDGE_EXCEEDED";
    public const string ErrQuotaSttExceeded = "ERR_QUOTA_STT_EXCEEDED";
    public const string ErrQuotaBarcodeExceeded = "ERR_QUOTA_BARCODE_EXCEEDED";
    public const string ErrQuotaMapExceeded = "ERR_QUOTA_MAP_EXCEEDED";
    public const string ErrQuotaMealPlanExceeded = "ERR_QUOTA_MEAL_PLAN_EXCEEDED";
    public const string ErrQuotaMealSwapExceeded = "ERR_QUOTA_MEAL_SWAP_EXCEEDED";

    // === 5. Lỗi Trợ lý AI & Tủ lạnh (AI & Fridge) ===
    public const string ErrAiCircuitOpen = "ERR_AI_CIRCUIT_OPEN";
    public const string ErrAiUnavailable = "ERR_AI_UNAVAILABLE";
    public const string ErrChatSessionNotFound = "ERR_CHAT_SESSION_NOT_FOUND";
    public const string ErrChatUnsafeContent = "ERR_CHAT_UNSAFE_CONTENT";
    public const string ErrFridgeEmpty = "ERR_FRIDGE_EMPTY";
    public const string ErrFridgeImageTooSmall = "ERR_FRIDGE_IMAGE_TOO_SMALL";
    public const string ErrFridgeItemExists = "ERR_FRIDGE_ITEM_EXISTS";
    public const string ErrFridgeItemNotFound = "ERR_FRIDGE_ITEM_NOT_FOUND";

    // === 6. Lỗi Bài viết, Bình luận, Tương tác (Community) ===
    public const string ErrContentNotFound = "ERR_CONTENT_NOT_FOUND";
    public const string ErrContentNotOwner = "ERR_CONTENT_NOT_OWNER";
    public const string ErrContentViolationVegan = "ERR_CONTENT_VIOLATION_VEGAN";
    public const string ErrCommentDuplicateLimit = "ERR_COMMENT_DUPLICATE_LIMIT";
    public const string ErrCommentTooFast = "ERR_COMMENT_TOO_FAST";
    public const string ErrPostDailyLimitExceeded = "ERR_POST_DAILY_LIMIT_EXCEEDED";
    public const string ErrPostVisibilityUnsupported = "ERR_POST_VISIBILITY_UNSUPPORTED";
    public const string ErrReportDailyLimit = "ERR_REPORT_DAILY_LIMIT";
    public const string ErrReportDuplicate = "ERR_REPORT_DUPLICATE";

    // === 7. Lỗi Công thức, Thực đơn, Mua sắm (Culinary) ===
    public const string ErrRecipeEditRejected = "ERR_RECIPE_EDIT_REJECTED";
    public const string ErrRecipeLocked = "ERR_RECIPE_LOCKED";
    public const string ErrRecipeNotEligible = "ERR_RECIPE_NOT_ELIGIBLE";
    public const string ErrRecipeRedAlert = "ERR_RECIPE_RED_ALERT";
    public const string ErrRecipeStructureInvalid = "ERR_RECIPE_STRUCTURE_INVALID";
    public const string ErrMealNoCandidate = "ERR_MEAL_NO_CANDIDATE";
    public const string ErrMealNoSubstitute = "ERR_MEAL_NO_SUBSTITUTE";
    public const string ErrMealPlanNotActive = "ERR_MEAL_PLAN_NOT_ACTIVE";
    public const string ErrMealPlanNotDraft = "ERR_MEAL_PLAN_NOT_DRAFT";
    public const string ErrMealPlanNotFound = "ERR_MEAL_PLAN_NOT_FOUND";
    public const string ErrShoppingListExists = "ERR_SHOPPING_LIST_EXISTS";
    public const string ErrShoppingListNotFound = "ERR_SHOPPING_LIST_NOT_FOUND";

    // === 8. Lỗi Master Data (Dị ứng, Phụ gia, Nguyên liệu, Danh mục) ===
    public const string ErrAllergenDuplicate = "ERR_ALLERGEN_DUPLICATE";
    public const string ErrAllergenNotFound = "ERR_ALLERGEN_NOT_FOUND";
    public const string ErrCategoryCoreProtected = "ERR_CATEGORY_CORE_PROTECTED";
    public const string ErrCategoryDuplicate = "ERR_CATEGORY_DUPLICATE";
    public const string ErrCategoryNeedsMigration = "ERR_CATEGORY_NEEDS_MIGRATION";
    public const string ErrDietSchoolNotFound = "ERR_DIET_SCHOOL_NOT_FOUND";
    public const string ErrEnumberDuplicate = "ERR_ENUMBER_DUPLICATE";
    public const string ErrIngredientDuplicate = "ERR_INGREDIENT_DUPLICATE";
    public const string ErrProductNotFound = "ERR_PRODUCT_NOT_FOUND";
    public const string ErrProfileNotCompleted = "ERR_PROFILE_NOT_COMPLETED";
    public const string ErrVenueNotFound = "ERR_VENUE_NOT_FOUND";

    // === 9. Lỗi Thanh toán & Gói dịch vụ (Payment & Subscription) ===
    public const string ErrPaymentPendingExists = "ERR_PAYMENT_PENDING_EXISTS";
    public const string ErrPayAlreadyReconciled = "ERR_PAY_ALREADY_RECONCILED";
    public const string ErrPayAmountMismatch = "ERR_PAY_AMOUNT_MISMATCH";
    public const string ErrPayInvalidSignature = "ERR_PAY_INVALID_SIGNATURE";
    public const string ErrPayOrderExpired = "ERR_PAY_ORDER_EXPIRED";
    public const string ErrPayOrderNotFound = "ERR_PAY_ORDER_NOT_FOUND";
    public const string ErrPlanDuplicate = "ERR_PLAN_DUPLICATE";
    public const string ErrPlanNotFound = "ERR_PLAN_NOT_FOUND";

    // === 10. Lỗi Media & Video (Media) ===
    public const string ErrMediaFileTooLarge = "ERR_MEDIA_FILE_TOO_LARGE";
    public const string ErrMediaInvalidFile = "ERR_MEDIA_INVALID_FILE";
    public const string ErrSttAlreadyDone = "ERR_STT_ALREADY_DONE";
    public const string ErrSttDurationInvalid = "ERR_STT_DURATION_INVALID";
    public const string ErrTranscriptNotReady = "ERR_TRANSCRIPT_NOT_READY";

    // === 11. Lỗi Dịch vụ ngoài (External Services) ===
    public const string ErrExtCloudinaryFailed = "ERR_EXT_CLOUDINARY_FAILED";
    public const string ErrExtGatewayFailed = "ERR_EXT_GATEWAY_FAILED";
    public const string ErrExtOpenfoodfactsFailed = "ERR_EXT_OPENFOODFACTS_FAILED";
    public const string ErrExtQdrantUnavailable = "ERR_EXT_QDRANT_UNAVAILABLE";
    public const string ErrExtSourceUnavailable = "ERR_EXT_SOURCE_UNAVAILABLE";
    public const string ErrKnowledgeReindexRunning = "ERR_KNOWLEDGE_REINDEX_RUNNING";

    // === 12. Lỗi Quản trị viên (Admin) ===
    public const string ErrAdmForbidden = "ERR_ADM_FORBIDDEN";
    public const string ErrAdmInvalidTransition = "ERR_ADM_INVALID_TRANSITION";
    public const string ErrAdmLastAdmin = "ERR_ADM_LAST_ADMIN";
    public const string ErrAdmNotFound = "ERR_ADM_NOT_FOUND";
    public const string ErrAdmReportClosed = "ERR_ADM_REPORT_CLOSED";
}
