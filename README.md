# veggielife-backend

Hệ thống Backend API cho nền tảng lối sống thuần chay toàn diện **VeggieLife**.
**ASP.NET Core 8 Web API · C# · Entity Framework Core 8 (Database-First) · SQL Server 2022 · Redis · Qdrant Vector DB · Google Gemini 2.5 Flash · JWT · Swagger (OpenAPI 3.0.3).**

> ⚠️ **ĐỌC KỸ TOÀN BỘ README NÀY TRƯỚC KHI VIẾT BẤT KỲ DÒNG CODE NÀO.**
> Mọi quy tắc trong tài liệu này là **tối thượng và bắt buộc tuân thủ 100%**.

---

## 1. NGUỒN SỰ THẬT & THỨ TỰ ƯU TIÊN

Toàn bộ tên bảng, tên cột, kiểu dữ liệu, endpoint và logic nghiệp vụ được trích xuất từ **Bộ tài liệu đặc tả chính thức (Approved v2.0 / v3.1, ngày 07/10/2026)** tại thư mục nội bộ `project-docs/` (đã cấu hình trong `.gitignore`).

### Thứ tự ưu tiên giải quyết xung đột tài liệu:
```
Decision Log (D1 - D115) 
  ──► 01. Business Rules (90 rules BR-xxx) 
  ──► 06. Logical Data Model Specification (38 bảng 3NF) 
  ──► 9. Data Dictionary & Constants 
  ──► 07. API Spec & 08. OpenAPI YAML (113 endpoints) 
  ──► 05. Architecture & Deployment 
  ──► Nguyên tắc làm việc nhóm
```

| Cần tra cứu | Tài liệu chính thức | Nội dung cốt lõi |
|---|---|---|
| **Quyết định chốt** | **Decision Log** (`D1` – `D115`) | **Luôn thắng mọi tài liệu khác nếu có xung đột**. |
| **Quy tắc nghiệp vụ** | **01. Business Rules.docx** | 90 quy tắc nghiệp vụ (`BR-xxx`) theo ma trận Karl Wiegers. |
| **Cơ sở dữ liệu** | **06. Logical Data Model Specification.docx** | 38 thực thể dữ liệu logic chuẩn 3NF (v3.1), kiểu dữ liệu, PK, FK, CHECK. |
| **Từ điển dữ liệu & Hằng số** | **9. Data Dictionary and Constants.docx** | Từ điển cột, Enums, hằng số (`MEDICAL_DISCLAIMER`, guest quota), Redis keys. |
| **Hợp đồng API & Mã lỗi** | **07. API Specification.docx** & **08. OpenAPI Specification.yaml** | 113 API endpoints, chuẩn Envelope Response D52, bảng mã lỗi `ERR_...`. |
| **Kiến trúc & Tích hợp** | **05. Architecture and Deployment.docx** | Phân định tác vụ Gemini trực tiếp vs AI Service, Qdrant RAG, hạ tầng. |
| **Quy chuẩn Git & Team** | **Nguyên tắc làm việc nhóm.docx** | Quy chuẩn commit, phân nhánh GitFlow, bảo mật API keys & secrets. |

*❌ Tuyệt đối không tự ý bịa đặt tên mới cho bảng, cột, endpoint, hay DTO ngoài tài liệu. Nếu thiếu thì hỏi Leader.*

---

## 2. KIẾN TRÚC 3 TẦNG (CLEAN ARCHITECTURE)

Hệ thống tuân thủ nghiêm ngặt mô hình phân tầng hướng phụ thuộc một chiều:

```
VeggieLife.API (Presentation Layer)
       │
       ▼
VeggieLife.BLL (Business Logic Layer)
       │
       ▼
VeggieLife.DAL (Data Access Layer) ──► SQL Server 2022 / Redis / Qdrant / External APIs
```

### Ranh giới kiến trúc bất khả xâm phạm:

| ĐƯỢC PHÉP | TUYỆT ĐỐI CẤM (Vi phạm = Reject PR) |
|---|---|
| API gọi BLL thông qua service interface (`IService`) | ❌ Controller gọi trực tiếp `DbContext` hoặc thao tác với `Entity` |
| BLL gọi DAL qua repository interface hoặc external client | ❌ Controller chứa logic nghiệp vụ hoặc dùng `try/catch` (để Exception Middleware xử lý) |
| `Program.cs` tham chiếu DAL chỉ để đăng ký Dependency Injection | ❌ DAL tham chiếu ngược lại BLL |
| BLL map Entity sang DTO rồi mới trả về Controller | ❌ Trả trực tiếp `Entity` ra response JSON (bắt buộc qua DTO và bọc `ApiResponse<T>`) |
| BLL ném `BusinessException` kèm mã lỗi chuẩn `ERR_...` | ❌ Hardcode mã lỗi dạng text tùy ý hoặc tự bịa mã lỗi ngoài danh mục spec |

---

## 3. CÂY THƯ MỤC CHUẨN CỦA BACKEND

```
veggielife-backend/
├── README.md                          ★ Hướng dẫn dự án tối thượng
├── .gitignore                         Cấu hình loại trừ file nhạy cảm và tài liệu nội bộ
├── VeggieLife.sln                     Visual Studio Solution
├── .github/workflows/                 CI (GitHub Actions): build + test khi mở PR
├── docs/                              Tài liệu markdown kỹ thuật public
├── database/                          ★ DATABASE-FIRST: SQL là nguồn gốc, C# Entity sinh từ đây
│   ├── schema/                        38 file DDL (.sql), mỗi bảng 1 file, đánh số theo thứ tự FK
│   └── seed/                          Dữ liệu master nạp 1 lần: DietarySchools, Allergens, FoodCategories, ENumberDictionary, SubscriptionPlans...
├── src/
│   ├── VeggieLife.API/
│   │   ├── Controllers/               1 controller / resource: AuthController, RecipesController...
│   │   │   └── Admin/                 Toàn bộ /api/v1/admin/*: AdminUsersController, AdminAuditLogsController...
│   │   ├── Middlewares/               ExceptionHandlingMiddleware, GeoRestrictionMiddleware, RateLimitMiddleware
│   │   ├── Filters/                   GuestQuotaAttribute, ValidationFilterAttribute
│   │   ├── Authorization/             Policies (GuestOrMember, Member, Vip, Admin), ClaimTypes
│   │   ├── Extensions/                ServiceCollectionExtensions (DI, JwtAuth, Swagger, DbContext, Redis, Qdrant)
│   │   ├── appsettings.Example.json   ★ File cấu hình mẫu rỗng ĐƯỢC COMMIT lên Git
│   │   └── appsettings.Development.json (Bị gitignore - chứa secrets local)
│   ├── VeggieLife.BLL/
│   │   ├── Services/<Module>/         I<X>Service.cs + <X>Service.cs: NƠI DUY NHẤT hiện thực Business Rules
│   │   │   └── AI/                    AiUsageTracker.cs, PromptTemplates.cs (dùng chung cho các tác vụ Gemini)
│   │   ├── DTOs/<Module>/
│   │   │   ├── Requests/              CreateRecipeRequest.cs, UpdateProfileRequest.cs...
│   │   │   └── Responses/             RecipeDetailResponse.cs, MealPlanResponse.cs...
│   │   ├── Validators/<Module>/       FluentValidation cho Request DTOs
│   │   ├── Mappings/                  Entity ↔ DTO mapping profiles
│   │   └── Common/
│   │       ├── Responses/             ApiResponse<T>, PagedMeta, ErrorDetails
│   │       └── Exceptions/            BusinessException.cs, ErrorCodes.cs
│   └── VeggieLife.DAL/
│       ├── Entities/                  ⚠ DO EF CORE SCAFFOLD SINH TỰ ĐỘNG, CẤM SỬA TAY
│       ├── Context/                   AppDbContext.cs (scaffold) + AppDbContext.Partial.cs (code tùy biến)
│       ├── Repositories/
│       │   ├── Interfaces/            IGenericRepository<T>, IUnitOfWork, I<X>Repository
│       │   └── Implementations/       GenericRepository<T>, UnitOfWork, <X>Repository
│       ├── Cache/                     IRedisService, RedisKeys.cs (hằng số template key), Lua scripts
│       ├── External/                  Mỗi dịch vụ ngoài có 1 Interface + 1 Implementation
│       │   ├── Gemini/                Google Gemini 2.5 Flash Client (Prompt & Text Embedding)
│       │   ├── Qdrant/                Qdrant Vector DB Client (gRPC port 6334)
│       │   ├── Cloudinary/            Cloudinary Media Upload Client
│       │   ├── Google/                Google OAuth Client, Google Places/Maps Client (chỉ lãnh thổ VN)
│       │   ├── OpenFoodFacts/         Open Food Facts Client (tra cứu mã vạch)
│       │   ├── HappyCow/              HappyCow Scraper / Data Importer (seed 1 lần)
│       │   ├── Email/                 IEmailSender qua SMTP Server (KHÔNG dùng SMS)
│       │   ├── Payments/              MoMo Client & VietQR Client (KHÔNG dùng VNPay)
│       │   └── AiService/             Client gọi veggielife-ai-service FastAPI (Whisper STT, Fridge OCR)
│       └── Enums/                     UserRole, AccountStatus, GatewayProvider, FreshnessStatus...
└── tests/VeggieLife.Tests/
    ├── Unit/Services/<Module>/        Unit tests với xUnit, Moq, FluentAssertions
    └── Integration/                   Integration tests với WebApplicationFactory
```

> **15 Thư mục Module độc lập** trong `BLL/Services`, `BLL/DTOs`, `BLL/Validators`:
> **Auth, Guest, Profile, MasterData, Recipes, Community, Videos, MealPlans, ShoppingLists, Products, Fridge, Venues, Chat, Subscriptions, Admin**.

---

## 4. MA TRẬN 15 MODULES ↔ 38 BẢNG CSDL ↔ 113 API ENDPOINTS

Hệ thống bao gồm **15 modules nghiệp vụ**, tương tác với **38 bảng thực thể CSDL chuẩn 3NF**, và phục vụ **113 API Endpoints** chuẩn OpenAPI 3.0.3:

| # | Module | Controllers | Bảng CSDL chính | Số Endpoints | Chức năng cốt lõi & Ghi chú |
|---|---|---|---|:---:|---|
| 1 | **Auth** | `AuthController` | `Users` | 6 | Đăng ký, đăng nhập email/mật khẩu, Google OAuth 2.0, OTP xác thực Email 5 phút (Redis), Refresh Token xoay vòng (7 ngày), Anonymous JWT (24h) |
| 2 | **Guest** | `GuestController` | *(Dùng Redis, không bảng)* | 2 | Quản lý hạn mức khách vãng lai qua Anonymous JWT: 05 lượt AI/phiên, 5 lượt bản đồ/phiên, 3 lượt barcode/ngày (D114) |
| 3 | **Profile** | `ProfileController` | `HealthProfiles`, `UserWeightHistory`, `UserAllergies` | 7 | Hồ sơ sức khỏe, trường `HealthProfiles.HealthGoal` (D113), tính chỉ số BMI, BMR/TDEE (Mifflin-St Jeor), cập nhật dị ứng, lịch sử cân nặng |
| 4 | **MasterData** | `MasterDataController` | `DietarySchools`, `Allergens`, `FoodCategories` | 6 | Dữ liệu danh mục trường phái ăn chay (Vegan, Ovo, Lacto...), nhóm thực phẩm, dị ứng thực phẩm. Cache Redis 24h |
| 5 | **Recipes** | `RecipesController` | `Recipes`, `RecipeIngredients`, `Ingredients`, `IngredientAllergens` | 8 | Quản lý công thức món chay, định lượng nguyên liệu, lọc trường phái ăn chay, tự động đối soát cảnh báo dị ứng |
| 6 | **Community** | `PostsController`, `CommentsController`, `ReportsController` | `Posts`, `Comments`, `Votes`, `ViolationReports` | 13 | Mạng xã hội thuần chay: Bài viết, bình luận phân cấp vô hạn (`Parent_Comment_ID`), thả tim Post (`Votes`), báo cáo vi phạm |
| 7 | **Videos** | `VideosController` | `Videos`, `VideoTranscripts` | 5 | Video ngắn món chay (Cloudinary HLS), bóc tách phụ đề STT tự động qua AI Service (`faster-whisper`), tìm kiếm theo transcript |
| 8 | **MealPlans** | `MealPlansController` | `MealPlans`, `MealSlots` | 6 | Lập thực đơn tuần thông minh bằng Google Gemini 2.5 Flash, phân bổ 3 bữa chính + 1 bữa phụ, tính tổng calo tự động |
| 9 | **ShoppingLists** | `ShoppingListsController` | `ShoppingLists`, `ShoppingListItems` | 6 | Danh sách mua sắm trực tuyến (chỉ online), tự động gom nguyên liệu từ MealPlan hoặc Recipe, đánh dấu hoàn thành |
| 10 | **Products** | `ProductsController` | `Products`, `ENumberDictionary`, `Product_ENumbers`, `ProductAllergens`, `UserScanHistory` | 9 | Tra cứu mã vạch qua Open Food Facts, phân tích phụ gia thực phẩm E-number (thuần chay / không thuần chay), cảnh báo dị ứng |
| 11 | **Fridge** | `FridgeController` | `FridgeSessions`, `FridgeInventory` | 6 | Quản lý tủ lạnh thông minh: Nhận diện ảnh tủ lạnh qua AI Service (`Gemini 2.5 Vision`), theo dõi hạn sử dụng, enum `Freshness_Status = 'NON-FRESH'` (D113) |
| 12 | **Venues** | `VenuesController` | `VeganVenues`, `VenueCategories`, `VenueMenuItems` | 6 | Khám phá quán ăn thuần chay qua Google Maps & HappyCow (chỉ lãnh thổ VN). ⚠️ *Lưu ý D100: Bảng `VenueMenuItems` không làm nghiệp vụ trong MVP* |
| 13 | **Chat** | `ChatController` | `ChatSessions`, `ChatMessages`, `Surveys` | 5 | Trợ lý tư vấn dinh dưỡng AI RAG: Backend .NET 8 kết nối Qdrant collection `veggielife_knowledge` (top-5, similarity >= 0.55), Gemini 2.5 Flash, khảo sát phản hồi AI |
| 14 | **Subscriptions** | `SubscriptionsController` | `SubscriptionPlans`, `PaymentTransactions` | 6 | Gói hội viên VIP (Tháng 59k / Quý 149k / Năm 499k), thanh toán qua MoMo & VietQR Sandbox (D104), IPN webhook an toàn |
| 15 | **Admin** | `AdminUsersController`, `AdminModerationController`, `AdminAnalyticsController`, `AdminAuditLogsController`... | `AuditLogs`, `AIQuotaLogs` + toàn bộ các bảng trên | 22 | Quản trị người dùng, khóa tài khoản, hàng đợi duyệt AI (`Report_Source = 'AI'`), thống kê doanh thu Dapper, nhật ký hạn mức AI |
| | **Tổng cộng** | | **38 bảng thực thể chuẩn 3NF** | **113 Endpoints** | **Hệ thống hoàn chỉnh sẵn sàng cho Sprint 5 - 7** |

---

## 5. CHUẨN ENVELOPE RESPONSE (QUYẾT ĐỊNH D52)

Toàn bộ 113 API endpoints bắt buộc tuân thủ định dạng Envelope Response chuẩn hóa theo quyết định D52:

### 1. Phản hồi thành công (Success Response):
```json
{
  "statusCode": 200,
  "message": "Thành công",
  "data": {
    "recipeId": 10,
    "title": "Đậu hũ sốt cà chua",
    "cookingTimeMinutes": 20
  },
  "meta": {
    "pageNumber": 1,
    "pageSize": 15,
    "totalRecords": 45,
    "totalPages": 3
  }
}
```
> *Lưu ý*: `meta` có giá trị khi dữ liệu là danh sách phân trang (`PagedResult<T>`); nếu là object đơn lẻ, `meta` có giá trị `null`.

### 2. Phản hồi thất bại (Error Response):
```json
{
  "statusCode": 400,
  "errorCode": "ERR_MEALPLAN_INVALID_DATES",
  "message": "Ngày kết thúc thực đơn phải sau ngày bắt đầu ít nhất 1 ngày",
  "details": [
    "StartDate: 2026-10-10",
    "EndDate: 2026-10-09"
  ]
}
```

### 3. Bảng mã lỗi `ERR_...` hệ thống cốt lõi:
| Mã lỗi | HTTP Status | Ý nghĩa nghiệp vụ |
|---|:---:|---|
| `ERR_INVALID_CREDENTIALS` | 401 | Email hoặc mật khẩu không chính xác |
| `ERR_UNAUTHORIZED` | 401 | Thiếu hoặc JWT token không hợp lệ / hết hạn |
| `ERR_FORBIDDEN` | 403 | Không có quyền truy cập tài nguyên (ví dụ: cần VIP hoặc ADMIN) |
| `ERR_USER_NOT_FOUND` | 404 | Không tìm thấy người dùng |
| `ERR_EMAIL_ALREADY_EXISTS` | 409 | Email đã được đăng ký trong hệ thống |
| `ERR_OTP_INVALID_OR_EXPIRED` | 400 | Mã OTP không đúng hoặc đã hết hạn 5 phút |
| `ERR_GUEST_QUOTA_EXCEEDED` | 429 | Khách vãng lai đã dùng hết hạn mức AI/Map/Barcode |
| `ERR_AI_SERVICE_UNAVAILABLE` | 503 | Dịch vụ AI hoặc Gemini bị quá tải, kích hoạt Fallback |
| `ERR_TRANSACTION_FAILED` | 400 | Giao dịch thanh toán MoMo / VietQR thất bại |

---

## 6. PHÂN ĐỊNH TÁC VỤ AI & DỊCH VỤ NGOÀI

Hệ thống phân định rành mạch giữa Backend .NET 8, AI Service FastAPI và các đối tác bên ngoài theo tài liệu kiến trúc v2.1:

```
                      ┌──────────────────────────────────────────────┐
                      │            VeggieLife Backend (.NET 8)       │
                      └───────┬──────────────────────┬───────────────┘
                              │                      │
                 Trực tiếp    │                      │ Bất đồng bộ
                 (HTTP/gRPC)  ▼                      ▼ (REST API)
          ┌───────────────────────────┐      ┌─────────────────────────────┐
          │  Google Gemini 2.5 Flash  │      │  veggielife-ai-service      │
          │  & Qdrant Vector DB       │      │  (Python FastAPI)           │
          ├───────────────────────────┤      ├─────────────────────────────┤
          │ • RAG Chatbot Dinh dưỡng  │      │ • Video STT (faster-whisper)│
          │ • Qdrant: collection      │      │ • Fridge OCR (Gemini Vision)│
          │   veggielife_knowledge    │      └─────────────────────────────┘
          │ • Sinh thực đơn MealPlan  │
          │ • Tự động duyệt nội dung  │
          │ • Text Embedding (768-d)  │
          └───────────────────────────┘
```

1. **Google Gemini 2.5 Flash (.NET 8 gọi trực tiếp)**:
   - **RAG Chatbot Dinh Dưỡng**: Backend nhúng câu hỏi qua model `text-embedding-004` (768 chiều), tìm kiếm vector top-5 trong Qdrant collection `veggielife_knowledge` (ngưỡng tương đồng cosine >= 0.55), ghép context và truyền cho Gemini 2.5 Flash sinh câu trả lời.
   - **Sinh thực đơn tuần (`MealPlans`)**: Dựa trên hồ sơ thể trạng BMR/TDEE, mục tiêu sức khỏe và trường phái ăn chay của người dùng.
   - **Kiểm duyệt nội dung tự động (`Auto Moderation`)**: Chấm điểm độc hại, tự động gắn cờ vi phạm vào bảng `ViolationReports` với `Report_Source = 'AI'`.
   - **Hằng số miễn trừ y khoa (D108)**: Tự động đính kèm câu khuyến nghị: *"Khuyến nghị mang tính chất tham khảo dinh dưỡng, vui lòng tham vấn ý kiến bác sĩ chuyên khoa đối với các bệnh lý nền."* (Hằng số `MEDICAL_DISCLAIMER` trong mã nguồn, không lưu DB).

2. **AI Service FastAPI (Xử lý media nặng bất đồng bộ)**:
   - **Speech-to-Text (`faster-whisper`)**: Tách audio từ video món chay tải lên Cloudinary, sinh phụ đề và mốc thời gian lưu vào `VideoTranscripts`.
   - **Fridge Vision OCR (`Gemini 2.5 Vision`)**: Phân tích ảnh chụp tủ lạnh, nhận diện danh sách thực phẩm đưa vào `FridgeInventory`.

3. **Cổng thanh toán (Payments)**:
   - **MoMo Sandbox** & **VietQR Sandbox** (Chỉ hỗ trợ 2 cổng này theo quyết định D104; ❌ **TUYỆT ĐỐI KHÔNG CÓ VNPAY**).
   - Xử lý Callback/IPN kèm chữ ký số xác thực HMAC-SHA256, tự động cập nhật trạng thái `SUCCESS` và gia hạn VIP.

4. **Dịch vụ Email & Bản đồ**:
   - **Email**: Gửi mã xác thực OTP qua giao thức **SMTP** (`IEmailSender`). ❌ **KHÔNG CÓ SMS / eSMS / Twilio** theo D101.
   - **Bản đồ**: **Google Maps / Places API** (Phạm vi địa lý chỉ giới hạn trong lãnh thổ Việt Nam; GeoRestrictionMiddleware).

---

## 7. QUY TRÌNH DATABASE-FIRST (38 BẢNG CSDL)

Dự án áp dụng phương pháp **Database-First**: File DDL SQL trong `database/schema/` là gốc, code Entity C# trong `src/VeggieLife.DAL/Entities/` được sinh hoàn toàn tự động qua lệnh EF Core Scaffold.

### Thứ tự 38 file DDL trong `database/schema/` (chuẩn theo thứ tự khóa ngoại):
```
01_DietarySchools.sql          02_Allergens.sql               03_Users.sql
04_HealthProfiles.sql          05_UserWeightHistory.sql       06_UserAllergies.sql
07_SubscriptionPlans.sql       08_PaymentTransactions.sql     09_AIQuotaLogs.sql
10_AuditLogs.sql               11_FoodCategories.sql          12_Ingredients.sql
13_IngredientAllergens.sql     14_Posts.sql                   15_Recipes.sql
16_RecipeIngredients.sql       17_Videos.sql                  18_VideoTranscripts.sql
19_Comments.sql                20_Votes.sql                   21_ViolationReports.sql
22_MealPlans.sql               23_MealSlots.sql               24_ShoppingLists.sql
25_ShoppingListItems.sql       26_Products.sql                27_ENumberDictionary.sql
28_Product_ENumbers.sql        29_ProductAllergens.sql        30_UserScanHistory.sql
31_FridgeSessions.sql          32_FridgeInventory.sql         33_VeganVenues.sql
34_VenueMenuItems.sql          35_VenueCategories.sql         36_ChatSessions.sql
37_ChatMessages.sql            38_Surveys.sql
```

### Ràng buộc khóa chính & Ngoại lệ thiết kế:
- **Khóa chính**: Mặc định `INT IDENTITY(1,1)` hoặc `BIGINT IDENTITY(1,1)`.
- **Ngoại lệ PK**:
  - `ENumberDictionary`: PK là `Code VARCHAR(15)` (ví dụ: `E120`, `E441`).
  - `HealthProfiles`: PK là `User_ID` (quan hệ 1-1 chặt chẽ với `Users`).
  - 6 bảng nối dùng Composite PK: `UserAllergies`, `ProductAllergens`, `Product_ENumbers`, `RecipeIngredients`, `VenueCategories`, `IngredientAllergens`.
- **Ngoại lệ đặt tên theo D113**:
  - Cột `HealthProfiles.HealthGoal` (viết liền, không gạch dưới).
  - Giá trị Enum `Freshness_Status = 'NON-FRESH'` (dấu gạch nối).
- **Bảng `Votes` (D37)**: Ràng buộc CHECK bắt buộc đúng 1 trong 2 cột `Post_ID` hoặc `Recipe_ID` có giá trị; 2 unique filtered index chống vote trùng lặp.
- **Role hệ thống (D115)**: `MEMBER`, `VIP`, `ADMIN` (lưu DB trong `Users.Role`); `GUEST` là Anonymous JWT claim (hạn 24h).

### Lệnh Scaffold Entity EF Core:
```bash
dotnet ef dbcontext scaffold "<Connection_String>" Microsoft.EntityFrameworkCore.SqlServer \
  -p src/VeggieLife.DAL -s src/VeggieLife.API \
  --output-dir Entities --context-dir Context --context AppDbContext \
  --no-onconfiguring --force
```
> ⚠️ **CẤM sửa tay bất kỳ file nào trong `src/VeggieLife.DAL/Entities/`**. Mọi cấu hình Fluent API bổ sung (chuyển đổi enum chuỗi, query filter) CHỈ được viết trong `src/VeggieLife.DAL/Context/AppDbContext.Partial.cs`.

### 7.4. Khởi tạo Database & Kiểm tra nghiệm thu tự động (1-Click Pipeline):
Dành cho thành viên nhóm khi clone repository về máy local:
1. **Khởi tạo trọn gói CSDL & Model Entity (1 Click):**
   ```powershell
   powershell -ExecutionPolicy Bypass -File "./database/scripts/rebuild_all.ps1"
   ```
2. **Kiểm tra nghiệm thu tính toàn vẹn (38 bảng & 312 cột):**
   ```powershell
   powershell -ExecutionPolicy Bypass -File "./database/scripts/generate_schema.ps1" -VerifyDb
   ```
> 📖 *Xem hướng dẫn chi tiết toàn bộ các công cụ và script tại: [database/README.md](file:///d:/FALL%2026/SWP391/Backend/database/README.md)*.

---

## 8. QUY ƯỚC MÃ NGUỒN (CODE CONVENTIONS)

| Đối tượng | Quy ước | Ví dụ đúng | Ví dụ sai |
|---|---|---|---|
| **Bảng SQL** | PascalCase số nhiều | `Users`, `MealPlans`, `VeganVenues` | `user`, `tbl_users` |
| **Cột SQL** | Snake_Case_Caps | `User_ID`, `Created_At`, `Post_ID` | `userId`, `created_at` |
| **Lớp C# (Class)** | PascalCase số ít | `User`, `MealPlan`, `RecipeService` | `users`, `meal_plan` |
| **Thuộc tính C# (Property)**| PascalCase | `UserId`, `CreatedAt`, `HealthGoal` | `user_id`, `created_at` |
| **JSON API** | camelCase | `userId`, `createdAt`, `healthGoal` | `User_ID`, `UserId` |
| **API Route** | kebab-case số nhiều | `/api/v1/recipes`, `/api/v1/meal-plans` | `/api/v1/getRecipes` |
| **Quy tắc nghiệp vụ** | Comment mã quy tắc | `// BR-REC-001`, `// BR-AUT-003` | Không comment mã BR |
| **Async methods** | Bắt buộc CancellationToken | `Task<RecipeResponse> GetByIdAsync(int id, CancellationToken ct)` | Bỏ qua CancellationToken |

---

## 9. QUY TẮC BẢO MẬT & QUẢN LÝ SECRETS

❌ **TUYỆT ĐỐI CẤM COMMIT BẤT KỲ API KEY, MẬT KHẨU, HOẶC CONNECTION STRING LÊN GITHUB.**

- Các secrets phải được bảo mật tuyệt đối:
  - `GEMINI_API_KEY`
  - `GOOGLE_MAPS_API_KEY`
  - `JWT_SECRET_KEY`
  - `SQL_CONNECTION_STRING`
  - `CLOUDINARY_API_SECRET`
  - `MOMO_SECRET_KEY` / `VIETQR_API_KEY`
  - SMTP Credentials
- **Môi trường Development (Local)**:
  - Lưu trong file `src/VeggieLife.API/appsettings.Development.json` (đã nằm trong `.gitignore`).
  - Hoặc sử dụng `dotnet user-secrets`.
- **Trên GitHub Repository**:
  - **CHỈ ĐƯỢC COMMIT file mẫu cấu trúc rỗng `src/VeggieLife.API/appsettings.Example.json`**.

---

## 10. WHITELIST VS BLACKLIST GIT

### ✅ WHITELIST (Được phép đẩy lên Git):
- Toàn bộ mã nguồn C# sạch trong `src/VeggieLife.API/`, `src/VeggieLife.BLL/`, `src/VeggieLife.DAL/`.
- 38 file DDL `.sql` trong `database/schema/` và seed script trong `database/seed/`.
- File cấu hình mẫu rỗng `appsettings.Example.json`.
- File tài liệu `README.md`, tài liệu public trong `docs/`, CI trong `.github/workflows/`.
- Solution `VeggieLife.sln`, các file `.csproj`, mã kiểm thử trong `tests/`.

### ❌ BLACKLIST (TUYỆT ĐỐI CẤM ĐẨY LÊN GIT - ĐÃ GITIGNORE):
- ❌ **Thư mục tài liệu đặc tả nội bộ**: `project-docs/`, `local-docs/`.
- ❌ **Thư mục cấu hình & Tri thức AI**: `.agents/` (chứa local skills, rules của AI).
- ❌ **File cấu hình chứa secret thật**: `appsettings.json`, `appsettings.Development.json`, `appsettings.Local.json`, `*.env`.
- ❌ **Thư mục build & logs**: `bin/`, `obj/`, `logs/`.
- ❌ **File cấu hình IDE cá nhân**: `.vs/`, `.vscode/`, `.idea/`, `*.user`, `*.suo`, `*.slnx`.
- ❌ **File rác OS**: `.DS_Store`, `Thumbs.db`.

---

## 11. DANH SÁCH TÍNH NĂNG NGOÀI MVP — TUYỆT ĐỐI KHÔNG CODE

Bất kỳ yêu cầu nào liên quan đến các tính năng sau đều phải từ chối hoặc cảnh báo vì **nằm ngoài phạm vi MVP của đề tài**:
1. ❌ **Follow / Block** người dùng
2. ❌ **Streak** (Chuỗi ngày điểm danh liên tục)
3. ❌ **Swipe / Tinder-style** Meal Matcher
4. ❌ **Badges** (Huy hiệu gamification)
5. ❌ **Push Notifications** qua FCM / OneSignal
6. ❌ **Share** tự động sang Facebook / Instagram
7. ❌ **Seasonal Events** (Sự kiện mùa lễ hội)
8. ❌ **Appeals** (Kháng cáo mở khóa tài khoản)
9. ❌ **SavedPosts** (Lưu bài viết yêu thích)
10. ❌ **OTP SMS** (eSMS / Twilio — chỉ dùng OTP Email qua SMTP)
11. ❌ **Health Connect / Apple Health / Google Fit**
12. ❌ **Quản lý thực đơn chi tiết quán chay (`VenueMenuItems` - D100)**: Quán chay chỉ nạp dữ liệu địa điểm cơ bản 1 lần từ HappyCow & Google Places.
13. ❌ **Menu OCR đa tầng / Crowdsourcing / AI Synthetic phức tạp**.

---

## 12. HƯỚNG DẪN CHẠY DỰ ÁN TRÊN MÁY LOCAL

### 1. Chuẩn bị môi trường:
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server 2022](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (hoặc Docker SQL Server)
- [Redis](https://redis.io/) (hoặc Docker Redis)
- [Qdrant](https://qdrant.tech/) (Docker: `docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant`)

### 2. Thiết lập cấu hình:
1. Sao chép file `src/VeggieLife.API/appsettings.Example.json` thành `src/VeggieLife.API/appsettings.Development.json`.
2. Điền chuỗi kết nối SQL Server, Redis, Gemini API Key và JWT Secret Key của bạn vào file `appsettings.Development.json`.

### 3. Build và kiểm thử:
```bash
# Khôi phục dependencies và build toàn bộ solution
dotnet build

# Chạy toàn bộ Unit & Integration tests
dotnet test

# Khởi chạy API
dotnet run --project src/VeggieLife.API
```
Sau khi khởi động, truy cập Swagger UI tại: `http://localhost:5000/swagger` (hoặc port được cấp phát bởi Kestrel).
