# veggielife-backend

API của VeggieLife. **ASP.NET Core 8 Web API · C# · EF Core 8 (Database-first) · SQL Server 2022 · Redis 7 · JWT · Swagger · Polly · Serilog.**

> Repo hiện chỉ có **cây thư mục**. File `.gitkeep` chỉ giữ thư mục rỗng trong Git; khi thư mục đã có file thật thì xóa `.gitkeep` đi.
> Đọc hết README này trước khi tạo file đầu tiên. Cập nhật theo bộ tài liệu chuẩn ngày 09/10/2026 (Decision Log tới D119).

---

## 1. Nguồn sự thật

Đặc tả nằm trong `docs/` (bản sao của `plan/16. Context implement`; bảng số tệp ở `docs/README.md`). **Không sửa tay** các tệp này.

| Cần gì                                                                       | Mở tệp                                                                                                  |
| ------------------------------------------------------------------------------ | --------------------------------------------------------------------------------------------------------- |
| Quy tắc nghiệp vụ, số liệu (mã`BR-xxx`)                                | `docs/01. Business Rules.docx`                                                                          |
| Hành vi người dùng (UC)                                                    | `docs/02. Use Case Specification.docx`                                                                  |
| Đặc tả 82 chức năng (mã`FN-<MODULE>-nn`)                               | `docs/03. Functional Requirements.docx`                                                                 |
| Ngưỡng hiệu năng, bảo mật                                                | `docs/04. Nonfunctional Requirements Specification.docx`                                                |
| Kiến trúc, biến môi trường, Docker, CI                                   | `docs/05. Architecture and Deployment.docx`                                                             |
| 38 bảng / 312 cột                                                            | `docs/06. Logical Data Model Specification.docx`                                                        |
| 113 endpoint                                                                   | `docs/07. API Specification.docx` và `docs/08. OpenAPI Specification.yaml`                           |
| **Thẻ chức năng để code** (luồng, dữ liệu, endpoint, lỗi, test) | `docs/09. Task Cards.docx`                                                                              |
| Bảng/cột, hằng số, enum, mã lỗi, khóa Redis                             | `docs/10. Data Dictionary and Constants.docx`                                                           |
| Tên interface dùng chung, hợp đồng AI service, quy tắc cho agent         | Sprint Plan**Phụ lục B** và **§11** (`plan/16. Context implement/09. Sprint Plan.docx`) |

Thứ tự đọc cho một task: **README này → thẻ FN → Data Dictionary → Phụ lục B → FR/API khi cần đối chiếu nguyên văn**. Ưu tiên: BR cho số liệu, Data Model cho cấu trúc. Tài liệu mâu thuẫn thì **Decision Log thắng**. Không tự đặt tên mới; thiếu thông tin thì ghi `UNSPECIFIED: <câu hỏi>` và hỏi Leader.

---

## 2. Kiến trúc 3 layer

### Thứ tự ưu tiên giải quyết xung đột tài liệu:

```
VeggieLife.API  ──►  VeggieLife.BLL  ──►  VeggieLife.DAL  ──►  SQL Server / Redis / AI service / dịch vụ ngoài
(Presentation)       (Business Logic)      (Data Access)
```

| Cần tra cứu                              | Tài liệu chính thức                                                         | Nội dung cốt lõi                                                                  |
| ------------------------------------------ | ------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------ |
| **Quyết định chốt**              | **Decision Log** (`D1` – `D115`)                                     | **Luôn thắng mọi tài liệu khác nếu có xung đột**.                    |
| **Quy tắc nghiệp vụ**             | **01. Business Rules.docx**                                               | 90 quy tắc nghiệp vụ (`BR-xxx`) theo ma trận Karl Wiegers.                     |
| **Cơ sở dữ liệu**                | **06. Logical Data Model Specification.docx**                             | 38 thực thể dữ liệu logic chuẩn 3NF (v3.1), kiểu dữ liệu, PK, FK, CHECK.     |
| **Từ điển dữ liệu & Hằng số** | **9. Data Dictionary and Constants.docx**                                 | Từ điển cột, Enums, hằng số (`MEDICAL_DISCLAIMER`, guest quota), Redis keys. |
| **Hợp đồng API & Mã lỗi**       | **07. API Specification.docx** & **08. OpenAPI Specification.yaml** | 113 API endpoints, chuẩn Envelope Response D52, bảng mã lỗi`ERR_...`.          |
| **Kiến trúc & Tích hợp**         | **05. Architecture and Deployment.docx**                                  | Phân định tác vụ Gemini trực tiếp vs AI Service, Qdrant RAG, hạ tầng.       |
| **Quy chuẩn Git & Team**            | **Nguyên tắc làm việc nhóm.docx**                                    | Quy chuẩn commit, phân nhánh GitFlow, bảo mật API keys & secrets.               |

*❌ Tuyệt đối không tự ý bịa đặt tên mới cho bảng, cột, endpoint, hay DTO ngoài tài liệu. Nếu thiếu thì hỏi Leader.*

**Ranh giới AI (D118, D119):** Backend **không** giữ khóa Gemini và **không** truy cập Qdrant. Mọi nghiệp vụ AI đi qua `IAiServiceClient`:

- **ai-service** trên VPS cùng Qdrant: chat RAG, thực đơn tuần, kiểm duyệt nội dung, nhận diện ảnh tủ lạnh, kho tri thức.
- **stt-service** trên Cloud Run: video → công thức.

Backend chỉ làm phần nghiệp vụ quanh lời gọi AI:

- kiểm hạn mức;
- đọc hồ sơ/dị ứng/công thức đã lọc rồi **gửi kèm trong yêu cầu**;
- lưu kết quả cùng `usage`/`audit` mà AI service trả về (`AIQuotaLogs`, `AuditLogs`).

**Middleware (đúng thứ tự):** `ExceptionHandling` → `CorrelationId` → `Serilog request logging` → `GeoRestriction` → `RateLimit (Guest 10 req/phút/IP)` → `Authentication (JWT)` → `Authorization (policy)` → `GuestQuota filter` → Controller.

---

## 3. Cây thư mục

```
veggielife-backend/
├── README.md
├── docs/                          bản sao đặc tả (xem docs/README.md)
├── .github/workflows/             CI: build + test + format + contract guard + gitleaks
├── database/                      ★ Database-first: SQL là gốc, Entity sinh từ đây
│   ├── schema/                    38 file DDL, mỗi bảng 1 file, đánh số theo thứ tự khóa ngoại (sinh bằng dm_to_ddl.py)
│   ├── seed/                      dữ liệu gốc: 7 trường phái (DietarySchools), Allergens, FoodCategories, Ingredients (+ IngredientAllergens),
│   │                              ENumberDictionary, SubscriptionPlans (49k/129k/449k), template_meal_plans.sql, Admin đầu tiên
│   └── scripts/                   run_all.sh (schema + seed bằng sqlcmd), scaffold.sh, reset_db.sh, dm_to_ddl.py
├── scripts/                       check_contract.py (contract guard: route/field khớp 08. OpenAPI)
├── src/
│   ├── VeggieLife.API/
│   │   ├── Controllers/           1 controller / resource (bảng mục 4)
│   │   │   └── Admin/             toàn bộ /api/v1/admin/*: AdminUsersController, AdminModerationController, AdminMasterDataController, AdminKnowledgeController, AdminAiController…
│   │   ├── Middlewares/           ExceptionHandling, CorrelationId, GeoRestriction, RateLimit
│   │   ├── Filters/               GuestQuotaAttribute, ValidationFilter
│   │   ├── Authorization/         policy GuestOrMember, Member, Vip, Admin; claim types (role, isVip)
│   │   ├── BackgroundJobs/        VipExpiryJob, PaymentExpiryJob, AccountAnonymizeJob, ModelDriftJob, SttWorker, KnowledgeSyncWorker
│   │   └── Extensions/            AddApplicationServices(), AddDataAccess(), AddJwtAuth(), AddSwagger()
│   ├── VeggieLife.BLL/
│   │   ├── Services/<Module>/     I<X>Service.cs + <X>Service.cs: nơi DUY NHẤT hiện thực BR
│   │   │   ├── AI/                IAiUsageTracker (ghi AIQuotaLogs + AuditLogs, ngưỡng chi phí $10/$12,5)
│   │   │   └── Knowledge/         IKnowledgeSyncQueue + KnowledgeSyncQueue (đẩy nội dung đã duyệt sang AI service)
│   │   ├── DTOs/<Module>/         Requests/ · Responses/
│   │   ├── Validators/<Module>/   FluentValidation cho Request DTO
│   │   ├── Mappings/              Entity ↔ DTO
│   │   └── Common/                Responses (ApiResponse<T>, PagedResult<T>) · Exceptions (BusinessException, ErrorCodes)
│   └── VeggieLife.DAL/
│       ├── Entities/              ⚠ DO SCAFFOLD SINH, KHÔNG SỬA TAY
│       ├── Context/               AppDbContext.cs (scaffold sinh) + AppDbContext.Partial.cs (code tùy biến)
│       ├── Repositories/          Interfaces/ (IGenericRepository<T>, IUnitOfWork, I<X>Repository) · Implementations/
│       ├── Cache/                 IRedisService, RedisKeys (hằng số khóa), Lua scripts
│       ├── External/              mỗi dịch vụ 1 interface + 1 implementation
│       │   ├── AiService/         IAiServiceClient (ai-service + stt-service; ngắt mạch Polly)
│       │   ├── Cloudinary/  Google/ (OAuth, Places)  OpenFoodFacts/  Email/ (SMTP)  Payments/ (MoMo, VietQR)  Geo/ (IP → quốc gia)
│       └── Enums/                 AccountStatus, UserRole, PaymentGateway…
└── tests/
    ├── VeggieLife.Tests/          Unit/Services/<Module>/ (mock repository, bản giả IAiServiceClient)
    └── VeggieLife.IntegrationTests/  WebApplicationFactory + Testcontainers (SQL Server, Redis); nhóm AiServiceIntegration
```

Không có `DAL/External/Gemini`, `DAL/External/Qdrant` hay `BLL/Services/Knowledge/KnowledgeRetriever` (D118).

---

## 4. Module ↔ controller ↔ bảng

Module theo mã FN; hạn mức và hằng số tra ở `docs/10. Data Dictionary and Constants.docx` mục 3.

| Module (FN)   | Controller → Service                                                           | Bảng chính                                                                                                              | Ghi chú                                                                                                                                                               |
| ------------- | ------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| AUTH          | AuthController → IAuthService                                                  | Users (+ Redis: OTP, refresh, phiên Khách)                                                                              | Đăng ký bằng**Email**; OTP 6 số, 5 phút; Access 15 phút, Refresh 7 ngày xoay vòng (cookie HttpOnly); Anonymous JWT 24 giờ cho Khách; `/guest/quota` |
| PROF          | UsersController → IUserProfileService                                          | Users, HealthProfiles, UserWeightHistory, UserAllergies; danh mục DietarySchools, Allergens, FoodCategories, Ingredients | `/users/me/*`; 4 GET danh mục công khai (FN-PROF-08); BMI/BMR/TDEE theo BR-USR-NUT-03                                                                              |
| POST          | RecipesController / PostsController / VideosController → IContentService       | Recipes, RecipeIngredients, Posts, Videos, VideoTranscripts                                                               | Kiểm duyệt AI qua`IContentModerationService` (≥ 85% → QUARANTINED_HIDDEN); Red Alert theo trường phái (D109); video → công thức qua stt-service            |
| COMM          | CommentsController / VotesController / ReportsController → IInteractionService | Comments, Votes, ViolationReports                                                                                         | Bình luận nhiều tầng (`Parent_Comment_ID`)                                                                                                                       |
| MEAL          | MealPlansController / ShoppingListsController → IMealPlanService               | MealPlans, MealSlots, ShoppingLists, ShoppingListItems                                                                    | Free 1, VIP 3 thực đơn/tuần (21 bữa); ai-service`/meal/generate`; quá 4 giây hoặc mạch mở → thực đơn mẫu `TEMPLATE`                                 |
| VIS           | FridgeController → IFridgeService                                              | FridgeSessions, FridgeInventory                                                                                           | Free 10, VIP 30 ảnh/ngày; ai-service`/vision/detect-ingredients`; FRESH / NON-FRESH                                                                                |
| BAR           | ProductsController → IBarcodeService                                           | Products, ENumberDictionary, Product_ENumbers, ProductAllergens, UserScanHistory                                          | Open Food Facts; Khách 3 lần/ngày;**không** OCR nhãn                                                                                                        |
| MAP           | VenuesController → IVenueService                                               | VeganVenues, VenueCategories                                                                                              | Khách 5 km, Thành viên 1–10 km, chỉ trong Việt Nam;`VenueMenuItems` **không hiện thực** (D100)                                                        |
| CHAT          | ChatController / SurveysController → IChatService                              | ChatSessions, ChatMessages (`Metadata_JSON`), Surveys                                                                   | Free 20, VIP 200 câu/ngày; ai-service`/chat/answer` lo RAG; `/admin/knowledge/*` (FN-CHAT-07)                                                                    |
| SUB           | SubscriptionsController / PaymentsController → IPaymentService                 | SubscriptionPlans, PaymentTransactions                                                                                    | MoMo + VietQR sandbox; IPN HMAC SHA-256; đơn PENDING hết hạn sau 5 phút                                                                                           |
| ADM           | Admin*Controller → IAdminService                                               | AuditLogs + các bảng trên                                                                                              | Đăng nhập qua`/auth/login`; tự đăng xuất sau 30 phút không thao tác; **không** có 2FA                                                              |
| (dùng chung) |                                                                                 | AIQuotaLogs                                                                                                               | Ghi mỗi lần gọi AI (`Feature_Code`: AI_CHAT, MEAL_PLAN_GEN, AI_FRIDGE, AI_BARCODE, SUMMARIZE_VIDEO, AI_EMBEDDING, CONTENT_MODERATION)                             |

**Hạn mức Khách** (Redis, BR-GUEST-02): 03 lượt AI/phiên (dùng chung), quét mã vạch 03/ngày, tìm quán 03 lần.

**VIP (D104):** `Users.Role ∈ {MEMBER, VIP, ADMIN}`. IPN thành công thì đặt VIP; `VipExpiryJob` hạ về MEMBER sau 23:59:59 ngày hết hạn. JWT mang `role` (GUEST/MEMBER/VIP/ADMIN) và claim `isVip`. Không có bảng `VipSubscriptions`.

Module theo mã FN; hạn mức và hằng số tra ở `docs/10. Data Dictionary and Constants.docx` mục 3.

## 5. CHUẨN ENVELOPE RESPONSE (QUYẾT ĐỊNH D52)

**38 bảng:** Users, DietarySchools, HealthProfiles, UserWeightHistory, SubscriptionPlans, PaymentTransactions, Allergens, UserAllergies, AIQuotaLogs, Posts, Comments, ViolationReports, Videos, VideoTranscripts, Recipes, FoodCategories, Ingredients, RecipeIngredients, MealPlans, MealSlots, ShoppingLists, ShoppingListItems, Products, ENumberDictionary, Product_ENumbers, ProductAllergens, UserScanHistory, FridgeSessions, FridgeInventory, VeganVenues, VenueMenuItems, VenueCategories, ChatSessions, ChatMessages, Surveys, AuditLogs, Votes, IngredientAllergens.

Kiểu, độ dài, nullable, CHECK, FK lấy đúng từ `docs/06` và `docs/10` mục 2. File DDL đánh số theo thứ tự khóa ngoại (bảng cha trước) và được **sinh bằng script** (`database/scripts/dm_to_ddl.py`), không gõ tay.

**Đổi schema** (chỉ **DB owner Nguyễn Văn Minh Tâm** được merge; người khác mở issue `db-change`; D103: chỉ được **thêm cột**, không thêm bảng, không đổi tên hay CHECK cũ):

1. Sửa file `.sql` trong `database/schema/`, chạy `database/scripts/run_all.sh` trên DB local.
2. Chạy `database/scripts/scaffold.sh`:

   ```bash
   dotnet ef dbcontext scaffold "<connection>" Microsoft.EntityFrameworkCore.SqlServer \
     -p src/VeggieLife.DAL -s src/VeggieLife.API \
     --output-dir Entities --context-dir Context --context AppDbContext \
     --no-onconfiguring --force
   ```

   Scaffold tự sinh `UserId` kèm `HasColumnName("User_ID")` và class số ít (`User` cho bảng `Users`).
3. Commit **cả** file SQL **và** code sinh ra trong cùng 1 PR.

Code tùy biến (enum ↔ chuỗi, query filter xóa mềm…) chỉ viết trong `AppDbContext.Partial.cs`, vì `--force` ghi đè các file khác.

---

## 6. PHÂN ĐỊNH TÁC VỤ AI & DỊCH VỤ NGOÀI

| Thứ                  | Quy ước                                                                                                                                                                                 |
| --------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Bảng / cột SQL      | `MealPlans` / `User_ID`                                                                                                                                                               |
| Class / property C#   | `MealPlan` / `UserId`                                                                                                                                                                 |
| JSON                  | camelCase`userId`                                                                                                                                                                       |
| Route                 | `/api/v1/<danh-từ-số-nhiều-kebab>`; Khách và Thành viên dùng chung resource; chỉ `/auth/anonymous`, `/guest/quota`, `/admin/*` là riêng                                |
| Response thành công | `{ statusCode, message, data[, meta] }` (`ApiResponse<T>`)                                                                                                                            |
| Response lỗi         | `{ statusCode, errorCode, message[, details] }`. **Không** có trường `success`                                                                                              |
| Mã lỗi              | Chỉ dùng mã trong`docs/10` mục 5 (`ERR_VALIDATION`, `ERR_INTERNAL`, `ERR_QUOTA_*`, `ERR_AUTH_REFRESH_TOKEN_*`…), sinh `ErrorCodes.cs` từ đó                           |
| Phân trang           | `?page=1&size=20`; mặc định/tối đa theo thẻ FN; `meta = { page, size, total }`; lịch sử chat dùng `before/size` (D111)                                                     |
| Lỗi nghiệp vụ      | `throw new BusinessException(ErrorCodes.X, …)`; controller không try/catch                                                                                                            |
| Rule                  | Comment`// BR-XXX-NN` ngay chỗ hiện thực                                                                                                                                             |
| Bảo mật             | BCrypt cost 12; không log mật khẩu/token/OTP; kiểm tra magic bytes; HMAC cho webhook; truy vấn tham số hóa                                                                         |
| Secret                | Chỉ để trong biến môi trường / user-secrets, không commit                                                                                                                         |
| Gọi AI               | Chỉ qua`IAiServiceClient`; timeout, retry 2 lần, circuit breaker (lỗi/timeout > 50% trong 60 giây → mở 5 phút, `ERR_AI_CIRCUIT_OPEN`); kiểm tra lại đầu ra AI ở tầng mã |
| Kho tri thức         | Đổi trạng thái nội dung thì gọi`IKnowledgeSyncQueue.EnqueueUpsert/EnqueueDelete`; chỉ đẩy nội dung công khai đã duyệt                                                    |
| Khóa Redis           | Đúng tên trong thẻ FN, hằng số ở`DAL/Cache/RedisKeys.cs`; đếm hạn mức bằng Lua/`INCR` nguyên tử                                                                         |
| Dapper                | Chỉ dùng cho truy vấn thống kê Admin                                                                                                                                                 |

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

| Muốn thêm           | Đặt tại                                                                                                                           |
| --------------------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| Endpoint              | `API/Controllers/<X>Controller.cs` + method trong `BLL/Services/<Module>/I<X>Service.cs` (route phải có trong `08. OpenAPI`) |
| Rule nghiệp vụ      | `BLL/Services/<Module>/<X>Service.cs`                                                                                              |
| Request/response      | `BLL/DTOs/<Module>/Requests` hoặc `Responses`                                                                                   |
| Query phức tạp      | `DAL/Repositories/Interfaces` + `Implementations`                                                                                |
| Gọi dịch vụ ngoài | `DAL/External/<Dịch vụ>/`                                                                                                        |
| Job nền              | `API/BackgroundJobs/` (`BackgroundService`, chạy lặp lại an toàn, ghi `AuditLogs` với `Actor_Role = SYSTEM`)            |
| Cột mới             | Báo DB owner (mục 5)                                                                                                               |

---

## 7. Cấu hình (`.env.example`)

| Biến                                                                     | Ý nghĩa                                                                   |
| ------------------------------------------------------------------------- | --------------------------------------------------------------------------- |
| `ConnectionStrings__Default`                                            | SQL Server                                                                  |
| `Redis__ConnectionString`                                               | Redis                                                                       |
| `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKey`                   | JWT (khóa ≥ 32 ký tự ngẫu nhiên)                                      |
| `Jwt__AccessMinutes=15`, `Jwt__RefreshDays=7`, `Jwt__GuestHours=24` | Thời hạn token                                                            |
| `Cors__AllowedOrigins`                                                  | Miền Frontend (không dùng`*`)                                          |
| `AiService__BaseUrl`, `AiService__ApiKey`                             | ai-service trên VPS (mọi nghiệp vụ AI trừ STT)                         |
| `SttService__BaseUrl`                                                   | stt-service trên Cloud Run (`/stt/*`), dùng chung `AiService__ApiKey` |
| `AiBudget__WarnUsd=10`, `AiBudget__ThrottleUsd=12.5`                  | Ngưỡng chi phí AI (BR-ADM-AI-001)                                        |
| `Cloudinary__CloudName/ApiKey/ApiSecret`                                | Ảnh, video                                                                 |
| `Google__ClientId`, `Google__PlacesApiKey`                            | OAuth, Places                                                               |
| `Smtp__Host/Port/User/Password/From`                                    | Gửi OTP Email                                                              |
| `Payments__MoMo__*`, `Payments__VietQR__*`, `Payments__IpnSecret`   | Thanh toán sandbox                                                         |
| `Geo__Bypass=false`                                                     | `true` khi chạy local                                                    |
| `Seed__AdminEmail`, `Seed__AdminPassword`                             | Admin đầu tiên                                                           |

Backend **không** có biến `Gemini__*`, `Qdrant__*`, `Embedding__*`, `Rag__*` (D118).

---

## 8. Chạy local và kiểm tra

```bash
docker compose up -d                 # SQL Server, Redis, API, ai-service, Qdrant (+ stt-service khi cần)
bash database/scripts/run_all.sh     # 38 bảng + dữ liệu gốc
```

Trước khi báo xong một task, các lệnh sau phải xanh:

```bash
dotnet build
dotnet test
dotnet format --verify-no-changes
python scripts/check_contract.py
```

CI khi mở PR chạy các lệnh trên cùng `gitleaks` và coverage; **bỏ qua** nhóm test `AiServiceIntegration`. Khi merge `develop`, CI chạy thêm nhóm này: ai-service thật qua Testcontainers, gọi Gemini thật bằng khóa trong GitHub Secrets (D119).

```bash
docker compose up -d                 # SQL Server, Redis, API, ai-service, Qdrant (+ stt-service khi cần)
bash database/scripts/run_all.sh     # 38 bảng + dữ liệu gốc
```

## 9. Quy trình làm việc (Jira + GitHub)

- **Nhánh:** `main` ← `develop` ← `feature/VL-<số>-<mô-tả>`. Hotfix tách từ `main` rồi merge vào cả `main` và `develop`.
- **Commit:** `feat(meal-plans): VL-42 generate weekly plan [FN-MEAL-01]` (`feat|fix|test|docs|refactor|chore`).
- **PR:**
  - tiêu đề có mã Jira; mô tả có FN-ID, BR-ID, bảng, endpoint và ví dụ request/response;
  - cần 1 người duyệt; CI xanh (gồm contract guard); ≤ 400 dòng đổi.
  - Còn câu hỏi thì thêm mục "Câu hỏi cho Leader / UNSPECIFIED".
- **Contract-first:** mở PR nhỏ gồm interface service + DTO + validator + mã lỗi; duyệt xong mới hiện thực.

---

## 10. Khởi tạo (Leader chạy 1 lần)

### Lệnh Scaffold Entity EF Core:

```bash
dotnet new sln -n VeggieLife
dotnet new webapi   -o src/VeggieLife.API --use-controllers -f net8.0
dotnet new classlib -o src/VeggieLife.BLL -f net8.0
dotnet new classlib -o src/VeggieLife.DAL -f net8.0
dotnet new xunit    -o tests/VeggieLife.Tests -f net8.0
dotnet new xunit    -o tests/VeggieLife.IntegrationTests -f net8.0
dotnet sln add src/VeggieLife.API src/VeggieLife.BLL src/VeggieLife.DAL tests/VeggieLife.Tests tests/VeggieLife.IntegrationTests
dotnet add src/VeggieLife.API reference src/VeggieLife.BLL src/VeggieLife.DAL
dotnet add src/VeggieLife.BLL reference src/VeggieLife.DAL
dotnet add tests/VeggieLife.Tests reference src/VeggieLife.API src/VeggieLife.BLL
dotnet add tests/VeggieLife.IntegrationTests reference src/VeggieLife.API
```

Package chính:

- EF Core SqlServer + Design + Tools, StackExchange.Redis, Dapper, FluentValidation, BCrypt.Net-Next, Google.Apis.Auth, JwtBearer, Swashbuckle, Serilog, Polly, QuestPDF.
- Test: Moq, FluentAssertions, Testcontainers.

Không thêm thư viện lớn ngoài danh sách này khi chưa hỏi Leader.

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

> 📖 *Xem hướng dẫn chi tiết toàn bộ các công cụ và script tại: [database/README.md](<file:///d:/FALL%2026/SWP391/Backend/database/README.md>)*.

---

## 11. Ngoài phạm vi MVP: không code

Follow/Block, Streak, Swipe, Badges, Notifications, Share, Bookmark, DMCA, CAPTCHA, Admin 2FA, chế độ bảo trì, Google Fit / thiết bị đeo, OTP SMS (eSMS, Twilio), VNPay, URL-to-recipe (TikTok/Reels), OCR nhãn thành phần, thực đơn quán và đối soát ShopeeFood/GrabFood, ghi đè Ground Truth bởi Admin.
