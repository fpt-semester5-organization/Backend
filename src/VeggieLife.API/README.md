# VeggieLife.API — Presentation Layer (Web API)

> **Báo cáo Hiện trạng Kết thúc Sprint 1 & Tài liệu Kỹ thuật Tầng API**  
> *Dự án VeggieLife · ASP.NET Core 8 Web API · .NET 8*

---

## 1. Tổng quan & Vai trò Kiến trúc

Tầng **`VeggieLife.API`** đóng vai trò là tầng **Presentation (Giao diện lập trình ứng dụng Web API)** trong mô hình kiến trúc 3 tầng (3-Tier Architecture) của hệ thống VeggieLife:

```
[ Client: Web / Mobile ]
           │
           ▼ (HTTPS / JSON / Bearer Token)
┌─────────────────────────────────────────────────────────────┐
│                       VeggieLife.API                        │
│  - Controller Endpoints & HTTP Routing                      │
│  - Global Middleware Pipeline (Exception, RateLimit, Guest) │
│  - JWT Authentication & Policy-based Authorization          │
│  - Input Model Validation & D52 Envelope Standardization     │
│  - Swagger OpenAPI Documentation & Health Checks            │
└─────────────────────────────────────────────────────────────┘
           │
           ▼ (Dependency Injection)
[ VeggieLife.BLL: Business Logic Layer ]
```

### Công nghệ & Thư viện sử dụng:
- **Framework:** ASP.NET Core 8 Web API (`net8.0`).
- **Xác thực & Phân quyền:** `Microsoft.AspNetCore.Authentication.JwtBearer` (JWT Token, Role/Policy Claims).
- **Bộ nhớ đệm & Phiên làm việc:** `Microsoft.Extensions.Caching.StackExchangeRedis`, `StackExchange.Redis`.
- **Tài liệu API:** `Swashbuckle.AspNetCore` (Swagger UI v1 kèm tính năng Authorize Bearer).
- **Giám sát sức khỏe:** `Microsoft.AspNetCore.Diagnostics.HealthChecks` (`/health`).
- **Serialization:** `System.Text.Json` (chuẩn đặt tên camelCase, loại bỏ null khi serialize).

---

## 2. Cấu trúc Thư mục Thực tế

```
VeggieLife.API/
├── Controllers/
│   ├── BaseApiController.cs          # Controller cơ sở chuẩn hóa phản hồi ApiResponse<T>
│   ├── AuthController.cs              # Quản lý phiên khách, Đăng ký, OTP, Đăng nhập, Refresh Token, Đăng xuất
│   ├── GuestController.cs             # Tra cứu hạn mức dùng thử của khách vãng lai
│   ├── UsersController.cs             # Hồ sơ cá nhân, chỉ số sức khỏe, dị ứng, avatar 500x500, cân nặng
│   ├── MasterDataController.cs        # Tra cứu danh mục trường phái, dị ứng, nhóm thực phẩm, nguyên liệu
│   ├── RecipesController.cs           # Tra cứu, lọc và xem chi tiết công thức thuần chay đã duyệt
│   ├── PostsController.cs             # Danh sách, chi tiết, tạo mới (auto-moderate), sửa, hủy đăng, xóa bài viết
│   ├── VideosController.cs            # Tải lên video/ảnh công thức và bài viết (kiểm tra MP4 <= 90s)
│   ├── TestController.cs              # Kiểm tra kết nối CSDL và Redis trong môi trường nội bộ
│   └── Admin/                         # [Chưa triển khai] Các endpoint quản trị (/api/v1/admin/*)
├── Authorization/
│   ├── MemberOnlyAttribute.cs         # Chặn khách vãng lai gọi các tính năng cá nhân hóa (FN-AUTH-03)
│   └── RequireMemberAttribute.cs      # Chặn khách vãng lai thực hiện thao tác cộng đồng (FN-AUTH-03)
├── Middlewares/
│   ├── ExceptionHandlingMiddleware.cs # Xử lý ngoại lệ toàn cục, chuẩn hóa ApiErrorResponse D52
│   └── GuestSecurityMiddleware.cs     # Giới hạn tốc độ IP (10 req/phút/IP), chặn thao tác ghi khách, lưu guest_return
├── Extensions/
│   └── ServiceExtensions.cs          # Đăng ký cấu hình DI: JWT, Redis, Application Services, Swagger, HealthChecks
├── Properties/
│   └── launchSettings.json            # Cấu hình cổng chạy cục bộ (HTTP/HTTPS)
├── appsettings.json                   # Cấu hình kết nối và khóa bảo mật (Không commit lên Git)
├── appsettings.Example.json           # File mẫu cấu hình rỗng (Zero-Hardcoded Secrets)
└── Program.cs                         # Điểm khởi chạy ứng dụng & thiết lập Middleware Pipeline
```

---

## 3. Báo cáo Kết thúc Sprint 1: Những Gì ĐÃ Làm Được

### 3.1. Các Chức năng & Endpoint Đã Triển Khai Hoàn Tất

| Mã FN | Endpoint | Phương thức | Mô tả chi tiết |
|---|---|---|---|
| **FN-AUTH-01** | `/api/v1/auth/anonymous` | `POST` | Khởi tạo phiên khách vãng lai, sinh `guest_uuid` (Cookie HttpOnly/Secure + Header), cấp Anonymous JWT hiệu lực 24h. |
| **FN-AUTH-02** | `/api/v1/guests/trial-quota` | `GET` | Tra cứu 3 hạn mức dùng thử độc lập của khách: Chatbot (3), Nhận diện tủ lạnh (1), Trích xuất video STT (1). |
| **FN-AUTH-03** | Toàn bộ API nghiệp vụ | — | Chặn khách vãng lai truy cập tính năng cá nhân hóa/cộng đồng qua `MemberOnlyAttribute` và `GuestSecurityMiddleware`, trả HTTP 403 Forbidden kèm lưu `guest_return:{guestId}` trong 15 phút. |
| **FN-AUTH-04** | `/api/v1/auth/register` | `POST` | Đăng ký tài khoản bằng Email + Mật khẩu, sinh OTP 6 số ngẫu nhiên gửi qua email, tạo tài khoản trạng thái chờ kích hoạt (`Inactive`). |
| **FN-AUTH-05** | `/api/v1/auth/verify-otp` | `POST` | Kích hoạt tài khoản bằng OTP 6 chữ số, cấp cặp JWT Access Token (15 phút) và Refresh Token (7 ngày trong Cookie HttpOnly), đồng bộ context của khách. |
| **FN-AUTH-06** | `/api/v1/auth/resend-otp` | `POST` | Gửi lại mã OTP qua email (Rate limit 60 giây, chống lộ thông tin người dùng - User Enumeration). |
| **FN-AUTH-08** | `/api/v1/auth/login` | `POST` | Đăng nhập tài khoản, khóa đăng nhập 15 phút nếu nhập sai 5 lần liên tiếp (BR-AUTH-003), hỗ trợ khôi phục tài khoản bị xóa mềm trong 30 ngày (D46). |
| **FN-AUTH-09** | `/api/v1/auth/refresh-token` | `POST` | Làm mới Access Token và xoay vòng Refresh Token (Token Rotation), thu hồi toàn bộ session nếu phát hiện token bị dùng lại. |
| **FN-AUTH-10** | `/api/v1/auth/logout` | `POST` | Đăng xuất người dùng, đưa `jti` của Access Token vào Redis Blacklist, hủy Refresh Token và xóa cookie. |
| **FN-PROF-01** | `/api/v1/users/me` | `GET` | Xem hồ sơ cá nhân và các chỉ số sức khỏe được tính toán tự động (BMI, BMR, TDEE, Calo mục tiêu). |
| **FN-PROF-02** | `/api/v1/users/me` | `PUT` | Cập nhật thông tin cá nhân và trường phái ăn chay yêu thích. |
| **FN-PROF-02** | `/api/v1/users/me/health-profile` | `PUT` | Cập nhật hồ sơ sức khỏe (chiều cao, cân nặng, mức độ vận động, mục tiêu giảm/tăng cân), tự động tính toán lại macro dinh dưỡng. |
| **FN-PROF-02** | `/api/v1/users/me/allergies` | `PUT` | Cập nhật danh sách các dị ứng thức ăn của người dùng. |
| **FN-PROF-03** | `/api/v1/users/me/avatar` | `POST` | Upload ảnh đại diện chuẩn 500x500 qua Cloudinary CDN, cập nhật URL vào hồ sơ người dùng. |
| **FN-PROF-06** | `/api/v1/users/me/weight-history` | `GET`, `POST` | Xem lịch sử theo dõi cân nặng và ghi nhận cân nặng mới (ngày và trọng lượng). |
| **FN-POST-01** | `/api/v1/recipes` | `GET` | Tìm kiếm, lọc công thức nấu ăn chay theo trường phái, độ khó, thời gian, calo, dị ứng (phân trang chuẩn `PagedResult<T>`). |
| **FN-POST-02** | `/api/v1/recipes/{id}` | `GET` | Xem chi tiết công thức nấu ăn chay, danh sách nguyên liệu và cờ phân quyền `isMemberOnly`. |
| **FN-POST-05** | `/api/v1/posts` | `GET` | Danh sách bài viết cộng đồng, tìm kiếm từ khóa, phân trang, hiển thị tóm tắt tác giả và phương tiện. |
| **FN-POST-06** | `/api/v1/posts/{id}` | `GET` | Xem chi tiết bài viết cộng đồng (nội dung, ảnh/video, công thức đính kèm, quyền tương tác). |
| **FN-POST-07** | `/api/v1/posts` | `POST` | Tạo bài viết mới, kiểm duyệt nội dung tự động qua `IContentModerationService`, kiểm soát giới hạn đăng bài hàng ngày. |
| **FN-POST-07** | `/api/v1/posts/{id}` | `PUT`, `PATCH`, `DELETE` | Cập nhật nội dung bài viết, hủy đăng (`PATCH /unpublish`), và xóa mềm bài viết (`DELETE`). |
| **FN-POST-08** | `/api/v1/videos/upload` | `POST` | Upload video/ảnh cho công thức hoặc bài viết lên Cloudinary, xác thực thời lượng MP4 tối đa 90 giây. |
| **Master Data** | `/api/v1/master-data/*` | `GET` | 4 endpoints tra cứu dữ liệu gốc: trường phái, dị ứng, nhóm thực phẩm, nguyên liệu. |
| **System** | `/health` | `GET` | Endpoint kiểm tra sức khỏe hệ thống, trả JSON chi tiết trạng thái hoạt động. |

### 3.2. Middleware Pipeline & Bảo mật Đã Hoàn Thiện
1. **`ExceptionHandlingMiddleware`**: Bắt toàn bộ ngoại lệ chưa được xử lý, ánh xạ `AppException` và các lỗi hệ thống sang cấu trúc phản hồi chuẩn `ApiErrorResponse` theo Quyết định D52 (StatusCode, ErrorCode, Message, Details, Timestamp).
2. **`GuestSecurityMiddleware`**:
   - Chặn quá tốc độ (IP Rate Limiting): Tối đa 10 requests/phút/IP cho khách vãng lai, lưu trữ bộ đếm trong Redis với TTL trượt 60 giây.
   - Chặn hành động ghi cộng đồng và cá nhân hóa: Chặn các phương thức `POST`, `PUT`, `DELETE`, `PATCH` trên các tài nguyên hội viên khi người dùng chưa đăng nhập.
   - Cơ chế lưu ngữ cảnh chuyển đổi (Guest Conversion Context): Tự động đọc header `X-Return-Url` và `X-Pending-Action` để lưu vào Redis `guest_return:{guestId}` trong 15 phút khi trả mã 403 Forbidden.
3. **JWT Authentication & Authorization**:
   - Kiểm tra tính hợp lệ của Token và xác thực claims (`sub`, `email`, `role`, `isVip`, `jti`).
   - Kiểm tra Token Revocation: Tra cứu `blacklist:{jti}` trong Redis tại sự kiện `OnTokenValidated`, tự động ngắt phiên nếu token đã bị đăng xuất.
   - 4 Policy phân quyền: `RequireAdmin`, `RequireVip`, `RequireMember`, `RequireGuestOrAbove`.
4. **Chuẩn hóa Model Validation**: Can thiệp `InvalidModelStateResponseFactory` để trả về lỗi định dạng 400 Bad Request theo chuẩn `ApiErrorResponse` với mã `ERR_VALIDATION`.

### 3.3. Tình trạng Kiểm thử Tầng API
- Đã hoàn thành 100% các bộ **Integration Tests** và **Unit Tests** cho toàn bộ Controllers tại `tests/VeggieLife.Tests/`:
  - `AuthLoginRefreshLogoutIntegrationTests.cs`
  - `GuestSessionAndGatekeeperIntegrationTests.cs`
  - `PostsEndpointIntegrationTests.cs`
  - `RecipesEndpointIntegrationTests.cs`
  - `UsersAvatarAndWeightHistoryIntegrationTests.cs`
  - `VideosEndpointIntegrationTests.cs`
- Kết quả chạy thực tế: **288/288 tests Passed** (0 Failed, 0 Skipped).

---

## 4. Báo cáo: Những Gì CHƯA Làm (Backlog cho Sprint Tiếp Theo)

1. **Các Controller Nghiệp vụ Chuyên sâu:**
   - **Module Chatbot AI RAG (`ChatController`):** Các endpoint `/api/v1/chat/sessions`, `/api/v1/chat/messages`, `/api/v1/chat/stream` phục vụ hỏi đáp dinh dưỡng thuần chay.
   - **Module Nhận diện Tủ lạnh (`FridgeController`):** Các endpoint `/api/v1/fridge/scan`, `/api/v1/fridge/inventory`, `/api/v1/fridge/suggest-recipes`.
   - **Module Kế hoạch Bữa ăn (`MealPlansController`):** Các endpoint `/api/v1/meal-plans/current`, `/api/v1/meal-plans/generate`, `/api/v1/meal-plans/{id}/slots`.
   - **Module Quét Mã Vạch & Sản phẩm (`ProductsController`):** Các endpoint tra cứu thông tin sản phẩm theo barcode, tra cứu phụ gia E-number (`/api/v1/barcodes/{code}`).
   - **Module Quán ăn chay (`VenuesController`):** Các endpoint tìm kiếm quán chay lân cận theo vị trí GPS, xem đánh giá, danh mục món ăn (`/api/v1/venues`).
   - **Module Danh sách Mua sắm (`ShoppingListsController`):** Các endpoint tổng hợp nguyên liệu cần mua từ thực đơn tuần (`/api/v1/shopping-lists`).
   - **Module Gói VIP & Thanh toán (`SubscriptionsController`, `PaymentsController`):** Danh sách gói cước VIP, khởi tạo giao dịch MoMo/VietQR và tiếp nhận Webhook IPN (`/api/v1/payments/ipn/*`).
   - **Module Tương tác Xã hội (`CommentsController`, `VotesController`, `ViolationsController`):** Bình luận lồng nhau, Vote đánh giá độ chuẩn chay (100% Thuần chay, Ngũ vị tân, Có thể dính mặn), và Gửi báo cáo vi phạm.
   - **Toàn bộ Khối Quản trị Admin (`Controllers/Admin/`):** Chưa có controller nào trong thư mục `Admin/` (Quản trị người dùng, duyệt công thức, kiểm duyệt bài viết vi phạm, cấu hình kho tri thức RAG).
2. **Background Jobs & Worker Services (`BackgroundJobs/`):**
   - Thư mục hiện tại chưa có job thực tế nào được kích hoạt:
     - `VipExpiryJob`: Quét và hạ cấp gói VIP hết hạn hàng đêm.
     - `PaymentExpiryJob`: Hủy các giao dịch quá hạn thanh toán sau 15 phút.
     - `AccountAnonymizeJob`: Ẩn danh hóa vĩnh viễn các tài khoản bị xóa mềm quá 30 ngày (D46).
     - `ModelDriftJob`: Đánh giá độ chính xác của AI.
     - `SttWorker`: Hàng đợi xử lý âm thanh video trích xuất công thức.
     - `KnowledgeSyncWorker`: Đồng bộ công thức/bài viết đã duyệt sang Vector DB Qdrant.
3. **Middleware Tách rời:**
   - Chưa triển khai `CorrelationIdMiddleware` để gán và theo dõi mã `X-Correlation-ID` xuyên suốt các luồng log Serilog.
   - Chưa tích hợp cơ sở dữ liệu GeoIP chuẩn (MaxMind) để chặn các IP ngoài phạm vi Việt Nam ở tầng mạng thực tế.

---

## 5. Thay đổi Kiến trúc & Điểm Lệch (Blockers / Divergences) Buộc Phải Chỉnh Docs

Qua quá trình triển khai Sprint 1, một số quyết định kỹ thuật thực tế đã thay đổi so với các tài liệu đặc tả ban đầu (`docs/01-11` và `Backend/README.md`). Dưới đây là danh sách chi tiết các điểm cần cập nhật lại tài liệu:

### 1. Tách Biệt 3 Hạn Mức Dùng Thử Của Khách Vãng Lai (FN-AUTH-02, Commit `bd39295`)
- **Tài liệu ban đầu (`01. Business Rules`, `03. Functional Requirements`, `07. API Specification`):** Đề cập chung hạn mức dùng thử cho khách là 3 lượt hỏi AI hoặc không phân định rõ hạn mức của từng dịch vụ.
- **Thực tế triển khai:** Đã tách thành 3 hạn mức độc lập trong `GuestTrialResultDto` và Redis key `guest:quota:{guestId}`:
  - `Chatbot`: 3 lượt.
  - `FridgeVision`: 1 lượt.
  - `VideoStt`: 1 lượt.
- **Tài liệu cần chỉnh sửa:** Cập nhật bảng quy tắc khách trong `01. Business Rules.docx`, schema phản hồi của `GET /api/v1/guests/trial-quota` trong `07. API Specification.docx` và `08. OpenAPI Specification.yaml`.

### 2. Bổ Sung Headers Mở Rộng: `X-Return-Url` và `X-Pending-Action` (BR-GUEST-AUTH-01, FN-AUTH-03)
- **Tài liệu ban đầu:** Chưa quy định cách thức client truyền tải ngữ cảnh chuyển đổi khi bị chặn 403 Forbidden.
- **Thực tế triển khai:** API hỗ trợ đọc header `X-Return-Url` và `X-Pending-Action` (các hành động hợp lệ: `SAVE_RECIPE`, `CREATE_POST`, `COMMENT_POST`, `RATE_RECIPE`, `JOIN_CHALLENGE`, `SCAN_BARCODE`, `UPLOAD_VIDEO`) và tự động lưu vào Redis `guest_return:{guestId}` (TTL 15 phút). Dữ liệu này được trả về kèm payload của `POST /api/v1/auth/verify-otp` và `POST /api/v1/auth/login`.
- **Tài liệu cần chỉnh sửa:** Bổ sung mô tả 2 headers này vào `07. API Specification.docx` và `08. OpenAPI Specification.yaml`.

### 3. Tích Hợp Gộp RateLimit & GeoRestriction vào `GuestSecurityMiddleware`
- **Tài liệu ban đầu (`Backend/README.md` mục 2 & `05. Architecture and Deployment`):** Quy định chuỗi middleware riêng rẽ: `GeoRestriction` → `RateLimit (Guest 10 req/phút/IP)` → `GuestQuota filter`.
- **Thực tế triển khai:** Toàn bộ việc kiểm tra IP Rate Limit cho khách (10 req/phút) và kiểm tra cờ vị trí địa lý được gom gọn trong `GuestSecurityMiddleware` để giảm thiểu độ trễ truy vấn Redis lặp lại và đồng bộ xử lý trạng thái phiên khách.
- **Tài liệu cần chỉnh sửa:** Cập nhật mục sơ đồ Pipeline Middleware trong `05. Architecture and Deployment.docx` và `Backend/README.md`.

### 4. Tách Rời Hoàn Toàn Ranh Giới AI (AI Service Boundary - D118, D119)
- **Tài liệu ban đầu:** Một số tài liệu cũ nhắc đến việc Backend nắm Gemini API Key hoặc kết nối trực tiếp CSDL vector Qdrant.
- **Thực tế triển khai:** Tuyệt đối loại bỏ Gemini SDK và Qdrant SDK khỏi Backend. Toàn bộ các tương tác AI được ủy quyền sang AI Microservice thông qua interface hợp đồng `IAiServiceClient` (hiện tại trong Sprint 1 đang sử dụng `AiServiceStub` và `SimulatedContentModerationService` để đảm bảo độc lập tiến độ phát triển).
- **Tài liệu cần chỉnh sửa:** Xác nhận lại trong `05. Architecture and Deployment.docx` rằng tầng API không có bất kỳ cấu hình hay endpoint nào gọi trực tiếp tới Google Gemini hay Qdrant.

---

## 6. Hướng dẫn Phát triển cho Sprint Tiếp Theo

1. **Thêm Controller mới:** Kế thừa từ `BaseApiController`, sử dụng các helper `OkResponse()`, `CreatedResponse()` để đảm bảo toàn bộ phản hồi tuân thủ cấu trúc phong bì D52:
   ```csharp
   [HttpGet("{id:int}")]
   public async Task<IActionResult> GetById(int id, CancellationToken ct)
   {
       var result = await _someService.GetByIdAsync(id, ct);
       return OkResponse(result, "Lấy thông tin thành công.");
   }
   ```
2. **Bảo vệ Endpoint:**
   - Dành cho hội viên đã đăng nhập: Dùng `[Authorize(Policy = PolicyNames.RequireMember)]`.
   - Dành riêng cho tài khoản VIP: Dùng `[Authorize(Policy = PolicyNames.RequireVip)]`.
   - Dành cho Quản trị viên: Dùng `[Authorize(Policy = PolicyNames.RequireAdmin)]`.
   - Chặn khách vãng lai gọi tính năng cá nhân hóa (trả 403 chuyên biệt): Dùng thuộc tính `[MemberOnly]`.
3. **Quy chuẩn Bảo mật (Zero-Hardcoded Secrets):** Tuyệt đối không để chuỗi kết nối, secret key, hoặc API key tĩnh trong mã nguồn. Mọi cấu hình bắt buộc đọc qua `IConfiguration` và ném `InvalidOperationException` nếu thiếu giá trị.
