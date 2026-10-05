# veggielife-backend

API của VeggieLife. **ASP.NET Core 8 Web API · C# · Entity Framework Core 8 (Database-first) · SQL Server 2022 · Redis · JWT · Swagger.**

> Repo hiện chỉ có **cây thư mục**. File `.gitkeep` chỉ giữ thư mục rỗng trong Git; khi thư mục đã có file thật thì xóa `.gitkeep` đi.
> Đọc hết README này trước khi tạo file đầu tiên.

---

## 1. Nguồn sự thật

Mọi tên bảng, cột, endpoint và rule đều lấy từ **bộ tài liệu đặc tả của nhóm**. Mỗi thành viên tải bộ tài liệu bản mới nhất từ nơi lưu chung của nhóm về máy mình. Đặt ở thư mục nào cũng được, nhưng **không** đặt bên trong repo code.

Thứ tự đọc: **BR → Data Model → FR → API**.

| Cần gì | Tài liệu cần mở |
|---|---|
| Business Rules | **Final Business Rules** (bản Final, mã `BR-xxx`) |
| Bảng và cột (36 bảng) | **Master Logical Data Model** (sơ đồ draw.io, 36 bảng) |
| Kiểu dữ liệu, CHECK | **Master Logical Data Model Specification** (Word). ⚠ Bản hiện tại chưa cập nhật lên 36 bảng |
| Chức năng | **Functional Requirements** (bản Master khi có) |
| Endpoint | **API Specification** (bản Master khi có) |
| Quyết định đã chốt | **Decision Log** (Kế hoạch đồng bộ & Decision Log, mã `D1`, `D2`…) |

Luôn kiểm tra mình đang dùng **bản mới nhất**: hỏi Leader nếu không chắc.

Nếu tài liệu mâu thuẫn nhau thì Decision Log thắng. Không tự đặt tên mới; thiếu thì hỏi Leader.

---

## 2. Kiến trúc 3 layer

```
VeggieLife.API  ──►  VeggieLife.BLL  ──►  VeggieLife.DAL  ──►  SQL Server / Redis / dịch vụ ngoài
(Presentation)       (Business Logic)      (Data Access)
```

| Được phép | Không được phép |
|---|---|
| API gọi BLL qua interface service | Controller dùng DbContext hoặc Entity |
| BLL gọi DAL qua repository / client interface | DAL tham chiếu BLL |
| `Program.cs` tham chiếu DAL để đăng ký DI | Trả Entity ra JSON (phải qua DTO) |

---

## 3. Cây thư mục

```
veggielife-backend/
├── README.md
├── .github/workflows/             CI (GitHub Actions): build + test khi mở PR
├── docs/                          tài liệu kỹ thuật riêng của BE (Redis keys, error codes, endpoint map)
├── database/                      ★ Database-first: SQL là gốc, Entity sinh từ đây
│   ├── schema/                    36 file DDL, mỗi bảng 1 file, đánh số theo thứ tự FK (mục 5)
│   ├── seed/                      dữ liệu master: DietarySchools, Allergens, FoodCategories, ENumberDictionary, VipSubscriptions, admin đầu tiên
│   └── scripts/                   run_all.sh (chạy schema + seed bằng sqlcmd), scaffold.sh, reset_db.sh
├── src/
│   ├── VeggieLife.API/
│   │   ├── Controllers/           1 controller / resource: AuthController, RecipesController…
│   │   │   └── Admin/             toàn bộ /api/v1/admin/*: AdminUsersController…
│   │   ├── Middlewares/           ExceptionHandling, GeoRestriction, RateLimit, MaintenanceMode
│   │   ├── Filters/               GuestQuotaAttribute, ValidationFilter
│   │   ├── Authorization/         tên policy (GuestOrMember, Member, Vip, Admin), claim types
│   │   └── Extensions/            AddApplicationServices(), AddDataAccess(), AddJwtAuth(), AddSwagger()
│   ├── VeggieLife.BLL/
│   │   ├── Services/<Module>/     I<X>Service.cs + <X>Service.cs: nơi DUY NHẤT hiện thực BR
│   │   │   └── AI/                AiUsageTracker, PromptTemplates (dùng chung cho các module gọi Gemini)
│   │   ├── DTOs/<Module>/
│   │   │   ├── Requests/          CreateRecipeRequest…
│   │   │   └── Responses/         RecipeDetailResponse…
│   │   ├── Validators/<Module>/   FluentValidation cho Request DTO
│   │   ├── Mappings/              Entity ↔ DTO
│   │   └── Common/
│   │       ├── Responses/         ApiResponse<T>, PagedResult<T>
│   │       └── Exceptions/        BusinessException, ErrorCodes
│   └── VeggieLife.DAL/
│       ├── Entities/              ⚠ DO SCAFFOLD SINH, KHÔNG SỬA TAY
│       ├── Context/               AppDbContext.cs (scaffold sinh) + AppDbContext.Partial.cs (code tùy biến)
│       ├── Repositories/
│       │   ├── Interfaces/        IGenericRepository<T>, IUnitOfWork, I<X>Repository
│       │   └── Implementations/
│       ├── Cache/                 IRedisService, RedisKeys (hằng số key), Lua scripts
│       ├── External/              mỗi dịch vụ ngoài có 1 interface + 1 implementation
│       │   ├── Gemini/  Cloudinary/  Google/ (OAuth, Places)  HappyCow/  OpenFoodFacts/
│       │   ├── Email/   Payments/ (MoMo, VietQR)  AiService/ (gọi veggielife-ai-service)  Geo/ (IP → quốc gia)
│       └── Enums/                 AccountStatus, UserRole, PaymentGateway, VenueSource, MenuItemSource…
└── tests/VeggieLife.Tests/
    ├── Unit/Services/<Module>/    test service, mock repository
    └── Integration/               WebApplicationFactory
```

`<Module>` gồm 15 module: **Auth, Guest, Profile, MasterData, Recipes, Community, Videos, MealPlans, ShoppingLists, Products, Fridge, Venues, Chat, Subscriptions, Admin**.

---

## 4. Module ↔ bảng ↔ controller

| Module | Controller | Bảng chính | Ghi chú |
|---|---|---|---|
| Auth | AuthController | Users | Đăng ký/đăng nhập bằng **Email**. OTP Email **5 phút** (Redis). Anonymous JWT 24h cho Guest |
| Guest | GuestController | (không có bảng) | Hạn mức trong Redis: AI 05/phiên, map 5, barcode 3/ngày |
| Profile | ProfileController | HealthProfiles, UserWeightHistory, UserAllergies | BMI/BMR/TDEE; nhắc cập nhật sau 30 ngày (D31) |
| MasterData | MasterDataController | DietarySchools, Allergens, FoodCategories | Dữ liệu cho dropdown |
| Recipes | RecipesController | Recipes, RecipeIngredients, Ingredients, Comments, Votes | |
| Community | PostsController, CommentsController, ReportsController | Posts, Videos, Comments, Votes, ViolationReports | Bình luận nhiều tầng (`Parent_Comment_ID`) |
| Videos | VideosController | Videos, VideoTranscripts | STT gọi qua AI service |
| MealPlans | MealPlansController | MealPlans, MealSlots | Gemini lập thực đơn |
| ShoppingLists | ShoppingListsController | ShoppingLists, ShoppingListItems | Chỉ online |
| Products | ProductsController | Products, ENumberDictionary, Product_ENumbers, ProductAllergens, UserScanHistory | Barcode, OCR nhãn, Open Food Facts |
| Fridge | FridgeController | FridgeInventoryList | Gemini Vision qua AI service |
| Venues | VenuesController | VeganVenues, VenueMenuItems, VenueCategories | HappyCow + Google Maps; thực đơn đối soát ShopeeFood/GrabFood (D34) |
| Chat | ChatController | ChatSessions, ChatMessages, Surveys | |
| Subscriptions | SubscriptionsController | VipSubscriptions (danh mục gói), PaymentTransactions | VIP = giao dịch SUCCESS và `Ends_At > now` (D39) |
| Admin | Controllers/Admin/* | AuditLogs + các bảng trên | 2FA bằng **OTP Email**. Hàng đợi AI = ViolationReports với `Report_Source='AI'`; feedback AI = Surveys |
| (dùng chung) | | AIQuotaLogs | Ghi mỗi lần gọi AI |

---

## 5. Database-first: quy trình bắt buộc

**Thứ tự file trong `database/schema/`** (bảng cha trước, bảng có khóa ngoại sau):

```
01_DietarySchools  02_Allergens  03_Users  04_HealthProfiles  05_UserWeightHistory  06_UserAllergies
07_VipSubscriptions  08_PaymentTransactions  09_AIQuotaLogs  10_AuditLogs  11_FoodCategories  12_Ingredients
13_Posts  14_Recipes  15_RecipeIngredients  16_Videos  17_VideoTranscripts  18_Comments  19_Votes
20_ViolationReports  21_MealPlans  22_MealSlots  23_ShoppingLists  24_ShoppingListItems  25_Products
26_ENumberDictionary  27_Product_ENumbers  28_ProductAllergens  29_UserScanHistory  30_FridgeInventoryList
31_VeganVenues  32_VenueMenuItems  33_VenueCategories  34_ChatSessions  35_ChatMessages  36_Surveys
```

**Lưu ý khi viết DDL**
- PK mặc định `INT IDENTITY(1,1)`. Ngoại lệ:
  - `ENumberDictionary` dùng PK `Code` kiểu chuỗi.
  - `HealthProfiles` dùng PK chính là `User_ID`.
  - 5 bảng nối dùng PK ghép: UserAllergies, ProductAllergens, Product_ENumbers, RecipeIngredients, VenueCategories.
- `Votes`: CHECK đúng 1 trong 2 cột `Post_ID` / `Recipe_ID` có giá trị, và 2 UNIQUE filtered index (D37).
- Theo quyết định ngày 05/10, **không** tạo cột `Users.Totp_Secret` và `ShoppingLists.Is_Offline_Synced`.

**Đổi schema** (chỉ **DB owner** được merge):
1. Sửa file `.sql` trong `database/schema/`, rồi chạy `database/scripts/run_all.sh` trên DB local.
2. Chạy `database/scripts/scaffold.sh`. Script này gọi:
   ```bash
   dotnet ef dbcontext scaffold "<connection>" Microsoft.EntityFrameworkCore.SqlServer \
     -p src/VeggieLife.DAL -s src/VeggieLife.API \
     --output-dir Entities --context-dir Context --context AppDbContext \
     --no-onconfiguring --force
   ```
   Scaffold tự sinh `UserId` kèm `HasColumnName("User_ID")` và class số ít (`User` cho bảng `Users`).
3. Commit **cả** file SQL **và** code sinh ra trong cùng 1 PR.

Code tùy biến (chuyển enum sang chuỗi, query filter…) chỉ viết trong `AppDbContext.Partial.cs`, vì `--force` sẽ ghi đè các file khác.

---

## 6. Quy ước code

| Thứ | Quy ước |
|---|---|
| Bảng / cột SQL | `MealPlans` / `User_ID` (D17) |
| Class / property C# | `MealPlan` / `UserId` |
| JSON | camelCase `userId` (tự động) |
| Route | `/api/v1/<danh-từ-số-nhiều-kebab>`; Guest và Member dùng chung resource; chỉ `/auth/anonymous`, `/guest/quota`, `/admin/*` là riêng |
| Response | Luôn bọc `ApiResponse<T>`: `{ statusCode, success, message, data, errors[] }` |
| Phân trang | `?pageNumber=1&pageSize=15` → `PagedResult<T>` |
| Lỗi nghiệp vụ | `throw new BusinessException(ErrorCodes.X, …)`; controller không try/catch |
| Rule | Comment `// BR-XXX-NN` ngay chỗ hiện thực |
| Role | `MEMBER`, `ADMIN` trong DB; `GUEST` chỉ là claim JWT; VIP là claim `isVip` tính từ PaymentTransactions |
| Secret | Chỉ để trong biến môi trường / user-secrets, không commit |
| Gọi dịch vụ ngoài | Qua `External/*`, có timeout + Polly (circuit breaker, BR-ADM-AI-002) |
| Dapper | Chỉ dùng cho truy vấn thống kê Admin |

**Muốn thêm… thì đặt ở đâu?**

| Muốn thêm | Đặt tại |
|---|---|
| Endpoint mới | `API/Controllers/<X>Controller.cs` + method trong `BLL/Services/<Module>/I<X>Service.cs` |
| Rule nghiệp vụ | `BLL/Services/<Module>/<X>Service.cs` |
| Request/response | `BLL/DTOs/<Module>/Requests` hoặc `Responses` |
| Query phức tạp | `DAL/Repositories/Interfaces` + `Implementations` |
| Gọi API bên ngoài | `DAL/External/<Dịch vụ>/` |
| Redis key | Hằng số trong `DAL/Cache/RedisKeys.cs` |
| Cột / bảng mới | Báo DB owner, sửa `database/schema/` (mục 5) |

---

## 7. Quy trình làm việc (Jira + GitHub)

- **Nhánh:** `main` ← `develop` ← `feature/<JIRA-KEY>-<mô-tả>`, ví dụ `feature/VL-42-recipe-search`.
- **Commit:** `feat(recipes): VL-42 add search endpoint [FR-REC-01]`.
- **PR:** tiêu đề có mã Jira; mô tả ghi FR-ID, BR-ID, bảng liên quan; cần 1 người review; CI phải xanh.
- **Viết hợp đồng trước:** viết interface service + DTO, review xong mới hiện thực.

---

## 8. Khởi tạo (Leader chạy 1 lần)

```bash
dotnet new sln -n VeggieLife
dotnet new webapi   -o src/VeggieLife.API --use-controllers -f net8.0
dotnet new classlib -o src/VeggieLife.BLL -f net8.0
dotnet new classlib -o src/VeggieLife.DAL -f net8.0
dotnet new xunit    -o tests/VeggieLife.Tests -f net8.0
dotnet sln add src/VeggieLife.API src/VeggieLife.BLL src/VeggieLife.DAL tests/VeggieLife.Tests
dotnet add src/VeggieLife.API reference src/VeggieLife.BLL src/VeggieLife.DAL
dotnet add src/VeggieLife.BLL reference src/VeggieLife.DAL
dotnet add tests/VeggieLife.Tests reference src/VeggieLife.API src/VeggieLife.BLL
```

Package: EF Core SqlServer + Design + Tools, StackExchange.Redis, Dapper, FluentValidation, BCrypt.Net-Next, Google.Apis.Auth, JwtBearer, Swashbuckle, Serilog, Polly; test dùng Moq và FluentAssertions.
Dev local: chạy SQL Server + Redis bằng Docker.

---

## 9. Ngoài MVP: không code

Follow/Block, Streak, Swipe, Badges, Notifications, Share, Seasonal, Appeals, SavedPosts, OTP SMS (eSMS/Twilio), Health Connect/Google Fit, Qdrant/AI Knowledge Base, các tầng menu OCR / crowdsourcing / AI synthetic.

## 10. Đang chờ chốt (hỏi Leader trước khi làm phần liên quan)

- **API Master** (route chuẩn, error code). Spec Data Model cho kiểu dữ liệu của 36 bảng.
- **DB owner.**
- **Jira project key** (`VL` chỉ là ví dụ).
- **Thanh toán:** có giữ VNPay không.
- **Email:** SendGrid hay SMTP.
- **Giá trị nghiệp vụ:** giá 3 gói VIP, công thức BMR/TDEE, TTL các Redis key.
- **Deploy:** nơi deploy.

Danh sách đầy đủ các mục còn thiếu: hỏi Leader (file tổng hợp "Dữ liệu còn thiếu" của nhóm).
