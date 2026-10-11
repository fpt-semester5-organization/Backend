# VeggieLife.BLL — Business Logic Layer

> **Báo cáo Hiện trạng Kết thúc Sprint 1 & Tài liệu Kỹ thuật Tầng Nghiệp vụ**  
> *Dự án VeggieLife · .NET 8 Class Library*

---

## 1. Tổng quan & Vai trò Kiến trúc

Tầng **`VeggieLife.BLL`** là trung tâm điều hành và xử lý toàn bộ logic nghiệp vụ (Core Business Logic) của hệ thống VeggieLife. Tầng này chịu trách nhiệm:
- Hiện thực hóa 100% các quy tắc nghiệp vụ chuẩn (`BR-xxx`).
- Thực hiện các phép tính toán dinh dưỡng, chỉ số y khoa và hạn mức sử dụng.
- Điều phối dữ liệu giữa tầng API và tầng truy cập dữ liệu (DAL), bộ nhớ đệm phân tán (Redis) và các dịch vụ ngoại vi.
- Quản lý xác thực dữ liệu đầu vào, mã hóa bảo mật mật khẩu, sinh mã định danh và cấp phát Token.

```
┌─────────────────────────────────────────────────────────────┐
│                       VeggieLife.API                        │
└──────────────────────────────┬──────────────────────────────┘
                               │ (Truyền Request DTOs)
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                       VeggieLife.BLL                        │
│  - Services: Nơi DUY NHẤT hiện thực BR-xxx                  │
│  - Calculators: Tính toán BMR, TDEE, Calo mục tiêu, BMI     │
│  - DTOs: Requests & Responses chuẩn hóa                     │
│  - Security: BCrypt Password Hashing, JWT Token Generation  │
│  - Cache Logic: Redis Session, OTP Cache, Fallback Memory   │
│  - Common: ApiResponse<T>, ApiErrorResponse, AppException   │
└──────────────────────────────┬──────────────────────────────┘
                               │ (Truy vấn CSDL & Gọi Client)
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                       VeggieLife.DAL                        │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. Cấu trúc Thư mục Thực tế

```
VeggieLife.BLL/
├── Common/
│   ├── Constants/
│   │   ├── UserRoles.cs              # Định nghĩa các vai trò hệ thống: GUEST, MEMBER, VIP, ADMIN
│   │   ├── MemberFeatureType.cs      # Định nghĩa các loại tính năng cá nhân hóa (FN-AUTH-03)
│   │   └── PendingActions.cs         # Danh sách hành động lưu tạm khi chuyển đổi khách (SAVE_RECIPE, v.v.)
│   ├── Exceptions/
│   │   ├── AppException.cs           # Ngoại lệ nghiệp vụ tùy biến ném mã lỗi ErrorCodes
│   │   └── ErrorCodes.cs             # Bảng mã lỗi chuẩn D52 (ERR_AUTH_..., ERR_GUEST_..., ERR_POST_...)
│   ├── Responses/
│   │   ├── ApiResponse.cs            # Chuẩn phong bì phản hồi thành công { success, statusCode, message, data }
│   │   ├── ApiErrorResponse.cs       # Chuẩn phong bì phản hồi lỗi { success: false, statusCode, errorCode, message }
│   │   ├── PagedResult.cs            # Chuẩn phong bì phân trang { items, meta }
│   │   └── PagedMeta.cs              # Metadata phân trang (page, pageSize, totalItems, totalPages)
│   └── Settings/
│       └── GuestSecuritySettings.cs  # Cấu hình ngưỡng bảo mật khách (IP rate limit, quota mặc định)
├── DTOs/
│   ├── Auth/                         # Request/Response cho Đăng ký, Đăng nhập, OTP, Refresh Token
│   ├── Guest/                        # Response cho Phiên khách và Hạn mức dùng thử (GuestTrialResultDto)
│   ├── Profile/                      # Request/Response cho Hồ sơ, Cân nặng, Dị ứng, Avatar
│   ├── MasterData/                   # Response cho 4 danh mục dùng chung
│   ├── Recipes/                      # Request/Response lọc và chi tiết công thức thuần chay
│   ├── Posts/                        # Request/Response quản lý bài viết cộng đồng
│   ├── Videos/                       # Request/Response tải lên video và phương tiện
│   └── Media/                        # Kết quả tải lên và xóa tệp Cloudinary
├── Services/
│   ├── Auth/                         # IAuthService, AuthService, IJwtTokenService, JwtTokenService
│   ├── Guest/                        # IGuestService, GuestService, IIpGeolocationService, IpGeolocationService
│   ├── Profile/                      # IProfileService, ProfileService, HealthMetricCalculator
│   ├── MasterData/                   # IMasterDataService, MasterDataService
│   ├── Recipes/                      # IRecipeService, RecipeService
│   ├── Posts/                        # IPostService, PostService
│   ├── Videos/                       # IVideoService, VideoService
│   ├── Media/                        # IMediaService, MediaService, MediaServiceStub, Mp4MetadataHelper
│   ├── Cache/                        # IRedisService, RedisService (kèm cơ chế tự phục hồi In-Memory Fallback)
│   ├── Moderation/                   # IContentModerationService, SimulatedContentModerationService
│   ├── Email/                        # IEmailService, EmailServiceStub
│   ├── Knowledge/                    # IKnowledgeSyncQueue, KnowledgeSyncQueueStub
│   ├── AI/                           # IAiService, AiServiceStub, IVectorDbService, VectorDbServiceStub
│   └── Payment/                      # IPaymentGatewayService, PaymentGatewayServiceStub
└── Validators/
    └── Auth/
        └── PasswordValidator.cs      # Kiểm tra độ phức tạp mật khẩu theo BR-AUTH-001
```

---

## 3. Báo cáo Kết thúc Sprint 1: Những Gì ĐÃ Làm Được

### 3.1. Các Dịch vụ Nghiệp vụ Hoàn Thành

#### 1. Dịch vụ Xác thực & Bảo mật (`AuthService` & `JwtTokenService`)
- **Đăng ký tài khoản (FN-AUTH-04):** Kiểm tra trùng lặp email, mã hóa mật khẩu bằng thuật toán **BCrypt**, sinh mã OTP 6 số ngẫu nhiên lưu vào Redis với TTL 10 phút, gửi email xác thực qua `IEmailService`. Tạo người dùng trạng thái `Inactive`.
- **Kích hoạt tài khoản bằng OTP (FN-AUTH-05):** Kiểm tra khớp OTP từ Redis, chuyển trạng thái người dùng thành `Active`, cấp cặp JWT Access Token (15 phút) và Refresh Token (7 ngày). Tự động kế thừa và trả về ngữ cảnh khách vãng lai đã lưu (`guest_return:{guestId}`).
- **Gửi lại OTP (FN-AUTH-06):** Giới hạn tần suất 60 giây giữa 2 lần gửi, chống rò rỉ người dùng (User Enumeration - luôn trả về thông báo thành công chung).
- **Đăng nhập (FN-AUTH-08):** 
  - Khóa tài khoản tạm thời 15 phút nếu nhập sai mật khẩu 5 lần liên tiếp (BR-AUTH-003) lưu trữ bộ đếm trong Redis `login:fail:{email}`.
  - Hỗ trợ khôi phục tài khoản bị xóa mềm trong vòng 30 ngày qua cờ `restoreAccount` (Quyết định D46).
  - Cấp cặp JWT Access Token (15 phút) và lưu phiên Refresh Token vào Redis `refresh_token:{tokenHash}` (TTL 7 ngày).
- **Xoay vòng Refresh Token (Token Rotation - FN-AUTH-09):** Mỗi lần làm mới Access Token, Refresh Token cũ bị hủy ngay lập tức và sinh ra Refresh Token mới. Nếu phát hiện Refresh Token đã thu hồi bị dùng lại (Token Replay Attack), hệ thống lập tức hủy toàn bộ phiên làm việc của tài khoản đó.
- **Đăng xuất (FN-AUTH-10):** Thu hồi Refresh Token hiện tại và đưa định danh duy nhất `jti` của Access Token vào danh sách đen Redis (`blacklist:{jti}`) cho đến khi token tự hết hạn.

#### 2. Quản lý Phiên Khách & Hạn Mức Dùng Thử (`GuestService`)
- **Khởi tạo phiên khách (FN-AUTH-01):** Nhận diện hoặc sinh mới `Guest_UUID`, cấp Anonymous JWT thời hạn 24 giờ chứa claim `Role = GUEST` và `Guest_UUID`.
- **Tách bạch 3 hạn mức độc lập (FN-AUTH-02, Commit `bd39295`):** 
  - Lưu trữ bộ đếm trong Redis hash `guest:quota:{guestId}`:
    - `Chatbot`: 3 lượt hỏi đáp AI.
    - `FridgeVision`: 1 lượt nhận diện ảnh tủ lạnh.
    - `VideoStt`: 1 lượt trích xuất công thức từ video.
  - Cung cấp API kiểm tra hạn mức còn lại (`GetGuestTrialQuotaAsync`) trả về `GuestTrialResultDto`.
- **Chặn và Lưu ngữ cảnh chuyển đổi (FN-AUTH-03, BR-GUEST-AUTH-01):** Lưu URL chuyển hướng (`returnUrl`) và hành động đang làm dở (`pendingAction`) vào Redis `guest_return:{guestId}` với TTL 15 phút khi khách bị chặn bởi quyền hội viên.

#### 3. Hồ sơ Cá nhân & Tính toán Dinh dưỡng (`ProfileService` & `HealthMetricCalculator`)
- **Thuật toán tính toán y khoa (FN-PROF-01, FN-PROF-02):**
  - **BMI (Chỉ số khối cơ thể):** Phân loại theo thang chuẩn người châu Á (Thiếu cân, Bình thường, Thừa cân, Béo phì độ I & II).
  - **BMR (Tỷ lệ trao đổi chất cơ bản):** Tính theo công thức chuẩn **Mifflin-St Jeor**:
    - Nam: $BMR = 10 \times \text{weight} + 6.25 \times \text{height} - 5 \times \text{age} + 5$
    - Nữ: $BMR = 10 \times \text{weight} + 6.25 \times \text{height} - 5 \times \text{age} - 161$
  - **TDEE (Tổng năng lượng tiêu hao hàng ngày):** Nhân BMR với hệ số vận động thể chất (PAL: 1.2 đến 1.9).
  - **Calo mục tiêu:** Tự động bù trừ theo mục tiêu cân nặng (Giảm cân: -500 kcal, Tăng cân: +500 kcal, Duy trì: giữ nguyên).
- **Cập nhật hồ sơ sức khỏe & dị ứng:** Cập nhật thông tin chiều cao, cân nặng, trường phái ăn chay và danh sách dị ứng thực phẩm trong bảng `HealthProfiles` và `UserAllergies`.
- **Theo dõi cân nặng (FN-PROF-06):** Ghi nhận lịch sử cân nặng theo ngày vào bảng `UserWeightHistories`, hỗ trợ truy vấn biểu đồ tiến độ.
- **Upload Avatar (FN-PROF-03):** Xác thực định dạng ảnh, tải lên Cloudinary với kích thước cắt cúp chuẩn 500x500.

#### 4. Danh mục Dùng chung (`MasterDataService`)
- Truy vấn danh sách 7 trường phái ăn chay (kèm quy tắc cấm JSON), 14 chất gây dị ứng, 15 nhóm thực phẩm và 119 nguyên liệu phục vụ giao diện người dùng.

#### 5. Công thức Món Chay (`RecipeService`)
- **Tra cứu & Lọc công thức (FN-POST-01):** Xây dựng biểu thức LINQ linh hoạt lọc theo từ khóa, trường phái, độ khó (Easy, Medium, Hard), thời gian nấu tối đa, lượng calo, loại trừ các nguyên liệu chứa dị ứng của người dùng.
- **Chi tiết công thức (FN-POST-02):** Nạp đầy đủ danh sách nguyên liệu, định lượng, các bước nấu, và gắn cờ `isMemberOnly` (đối với khách vãng lai).

#### 6. Bài viết Cộng đồng & Kiểm duyệt (`PostService`)
- **Danh sách & Chi tiết bài viết (FN-POST-05, FN-POST-06):** Hỗ trợ tìm kiếm từ khóa, phân trang `PagedResult<PostSummaryDto>`, nạp thông tin tác giả, công thức và video đính kèm.
- **Tạo, sửa, hủy đăng và xóa bài viết (FN-POST-07):**
  - Kiểm soát giới hạn số lượng bài đăng tối đa trong ngày theo vai trò.
  - Tích hợp kiểm duyệt nội dung tự động qua `IContentModerationService`: Đánh dấu trạng thái bài viết (`Published` nếu đạt, `PendingModeration` nếu phát hiện nội dung nghi vấn).
  - Hỗ trợ ẩn bài viết (`Unpublish`) và xóa mềm (`IsDeleted = true`).

#### 7. Xử lý Video & Phương tiện Nấu ăn (`VideoService` & `Mp4MetadataHelper`)
- **Upload video công thức/bài viết (FN-POST-08):** Tải lên Cloudinary CDN vào thư mục riêng biệt.
- **Trích xuất nhị phân MP4 Header (`Mp4MetadataHelper`):** Tự phân tích nhị phân cấu trúc hộp ISO Base Media File Format (`moov` -> `mvhd`) để đọc chính xác thời lượng (duration) của video ngay trên luồng stream mà không cần cài đặt FFmpeg trên server. Chặn toàn bộ video vượt quá ngưỡng 90 giây.

#### 8. Bộ nhớ đệm Tự phục hồi (`RedisService`)
- Đóng gói toàn bộ thao tác Redis (String, Hash, Increment, Lua Scripting).
- **Cơ chế Fallback thông minh:** Khi Redis mất kết nối cục bộ, dịch vụ tự động chuyển hướng lưu tạm vào `IMemoryCache` (In-Memory Fallback), ghi log cảnh báo và không làm sập tiến trình của ứng dụng.

---

## 4. Báo cáo: Những Gì CHƯA Làm (Backlog cho Sprint Tiếp Theo)

1. **Khối AI Nghiệp vụ Chuyên sâu:**
   - **Tư vấn Dinh dưỡng Thuần Chay qua Chatbot (`ChatService`):** Chưa triển khai logic đóng gói hồ sơ người dùng (trường phái, dị ứng, calo tiêu chuẩn) gửi sang AI Microservice, chưa xử lý lưu vết phiên chat và tin nhắn vào `ChatSessions` và `ChatMessages`.
   - **Nhận diện Tủ lạnh (`FridgeService`):** Chưa triển khai nghiệp vụ kiểm kê tủ lạnh từ ảnh chụp và thuật toán gợi ý món ăn dựa trên nguyên liệu sắp hết hạn.
   - **Trích xuất Công thức từ Video qua STT (`SttService`):** Chưa triển khai luồng gửi video sang `stt-service` để nhận bản transcript và cấu trúc hóa thành Recipe.
2. **Khối Lập Kế Hoạch Bữa Ăn (`MealPlanService`):**
   - Thuật toán phân bổ các bữa ăn (Sáng, Trưa, Tối, Phụ) đảm bảo tổng calo trong ngày sai số không quá ±10% so với TDEE mục tiêu.
   - Cơ chế đổi món (Swap Meal Slot) tự động chọn món thay thế tương đương dinh dưỡng.
3. **Khối Quét Mã Vạch & Phân Tích Thực Phẩm (`ProductService`, `BarcodeService`):**
   - Nghiệp vụ phân tích thành phần sản phẩm đối chiếu với 65 chất phụ gia E-number và 7 trường phái ăn chay để đưa ra cảnh báo cấm chay.
   - Tích hợp gọi API OpenFoodFacts khi không tìm thấy barcode trong CSDL nội bộ.
4. **Khối Quán Ăn Thuần Chay (`VeganVenueService`):**
   - Thuật toán tính khoảng cách địa lý theo tọa độ GPS (Haversine Formula) để tìm quán chay trong bán kính người dùng chọn.
5. **Khối Danh Sách Mua Sắm (`ShoppingListService`):**
   - Tính năng tự động quét các nguyên liệu trong thực đơn tuần của người dùng và gộp khối lượng cần mua vào `ShoppingLists`.
6. **Khối Gói VIP & Thanh Toán (`SubscriptionService`, `PaymentService`):**
   - Nghiệp vụ khởi tạo đơn hàng thanh toán MoMo / VietQR, xử lý chữ ký số HMAC-SHA256, và cập nhật trạng thái giao dịch khi nhận IPN Webhook.
7. **Khối Tương tác Cộng đồng (`CommentService`, `VoteService`, `ViolationService`):**
   - Nghiệp vụ gửi bình luận phân cấp cha-con, Vote xác thực mức độ chuẩn chay của công thức, và xử lý báo cáo vi phạm cộng đồng.
8. **Khối Quản trị (`AdminService`):**
   - Thống kê Dashboard doanh thu VIP, số lượng bài viết, kiểm duyệt nội dung bị khiếu nại, và đồng bộ tri thức sang Vector DB.
9. **Cải tiến Kỹ thuật:**
   - **Validation:** Cần bổ sung các bộ `FluentValidation` hoàn chỉnh cho tất cả Request DTOs (hiện tại mới chỉ có `PasswordValidator`).
   - **Object Mapping:** Cần áp dụng thư viện ánh xạ tự động như `Mapster` để thay thế cho việc map thủ công bằng tay giữa Entity và DTO.

---

## 5. Thay đổi Kiến trúc & Điểm Lệch (Blockers / Divergences) Buộc Phải Chỉnh Docs

### 1. BLL Sử Dụng Trực Tiếp `AppDbContext` Thay Vì `IUnitOfWork` / `IGenericRepository`
- **Mô tả trong tài liệu (`05. Architecture`, `Backend/README.md`):** Quy định BLL phải gọi qua tầng Repository (`IGenericRepository<T>`, `IUnitOfWork`).
- **Thực tế triển khai:** Toàn bộ các dịch vụ trong BLL (`AuthService`, `ProfileService`, `RecipeService`, `PostService`, v.v.) hiện đang inject trực tiếp `AppDbContext`. Lý do: Giúp tận dụng tối đa sức mạnh của EF Core (LINQ IQueryable, AsNoTracking, Include lồng nhau, TransactionScope) và tăng tốc độ phát triển mà không bị cản trở bởi lớp trừu tượng thừa.
- **Tác động tài liệu:** Cần cập nhật `05. Architecture and Deployment.docx` để chính thức công nhận mô hình truy cập trực tiếp `AppDbContext` từ BLL, hoặc ghi rõ kế hoạch chuyển đổi sang Unit of Work ở giai đoạn sau nếu cần thiết.

### 2. Vị Trí Của `RedisService` Nằm Trong BLL Thay Vì DAL
- **Mô tả trong tài liệu:** `IRedisService` và `RedisKeys` được xếp trong `VeggieLife.DAL/Cache`.
- **Thực tế triển khai:** `IRedisService` và `RedisService` được đặt tại `VeggieLife.BLL/Services/Cache/`. Lý do: Redis trong dự án đóng vai trò lưu trữ nghiệp vụ phiên (OTP, Login Fail Counter, Guest Quota, Blacklist JTI, Guest Return Context) gắn liền với logic BLL.
- **Tác động tài liệu:** Cập nhật sơ đồ cấu trúc thư mục trong `Backend/README.md` và tài liệu kiến trúc.

### 3. Tách Rời 3 Hạn Mức Dùng Thử Của Khách Trong Redis Hash (FN-AUTH-02)
- **Mô tả trong tài liệu:** `01. Business Rules.docx` chỉ ghi chung hạn mức khách vãng lai.
- **Thực tế triển khai:** Cấu trúc Redis hash `guest:quota:{guestId}` lưu 3 trường riêng biệt `chat`, `fridge_vision`, `video_stt` tương ứng với 3 hạn mức (3, 1, 1).
- **Tác động tài liệu:** Đồng bộ định nghĩa khóa Redis và quy tắc hạn mức trong `01. Business Rules.docx` và `10. Data Dictionary and Constants.docx`.

### 4. Cơ Chế Tự Phục Hồi In-Memory Fallback Của Cache
- **Mô tả trong tài liệu:** Coi Redis là phụ thuộc bắt buộc (Hard Dependency).
- **Thực tế triển khai:** `RedisService` tích hợp cờ `abortConnect=false` và cơ chế fallback sang `IMemoryCache` nội bộ khi không kết nối được Redis cục bộ, đảm bảo môi trường phát triển và kiểm thử tự động không bị nghẽn.
- **Tác động tài liệu:** Bổ sung mô tả cơ chế hoạt động suy giảm (Degraded Mode) vào tài liệu kiến trúc hạ tầng `05. Architecture and Deployment.docx`.

### 5. Phân Tích Nhị Phân MP4 Box `mvhd` Để Xác Thực Thời Lượng Video
- **Mô tả trong tài liệu:** Yêu cầu video tối đa 90 giây nhưng chưa chỉ rõ phương án kiểm tra ở phía backend.
- **Thực tế triển khai:** Xây dựng `Mp4MetadataHelper` đọc byte stream trực tiếp, giải mã trường `timescale` và `duration` của hộp `mvhd` theo chuẩn ISO/IEC 14496-12.
- **Tác động tài liệu:** Bổ sung mô tả giải pháp kỹ thuật này vào tài liệu kiến trúc phần xử lý Video/Media.

---

## 6. Hướng dẫn Phát triển cho Sprint Tiếp Theo

1. **Thêm Dịch vụ Nghiệp vụ Mới:**
   - Tạo Interface `I<X>Service` và lớp thực thi `<X>Service` trong `Services/<Module>/`.
   - Tiêm phụ thuộc `AppDbContext`, `IRedisService`, `ILogger<<X>Service>` qua Constructor.
   - Ném ngoại lệ nghiệp vụ chuẩn bằng `AppException(StatusCodes.StatusXXX, ErrorCodes.ErrXXX, "Thông điệp lỗi chi tiết")`.
2. **Quy chuẩn Bảo mật (Zero-Hardcoded Secrets):**
   - Đọc 100% khóa bảo mật từ `IConfiguration`.
   - Ném `InvalidOperationException` ngay khi khởi tạo nếu thiếu giá trị cấu hình cần thiết.
   - Tuyệt đối không hardcode mật khẩu, connection string hay API key.
