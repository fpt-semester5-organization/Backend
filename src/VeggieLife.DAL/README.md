# VeggieLife.DAL — Data Access Layer

> **Báo cáo Hiện trạng Kết thúc Sprint 1 & Tài liệu Kỹ thuật Tầng Truy cập Dữ liệu**  
> *Dự án VeggieLife · EF Core 8 (Database-First) · SQL Server 2022 · Cloudinary*

---

## 1. Tổng quan & Vai trò Kiến trúc

Tầng **`VeggieLife.DAL`** là tầng truy cập dữ liệu và tích hợp hạ tầng ngoại vi trong kiến trúc 3 tầng của VeggieLife. Tầng này chịu trách nhiệm:
- Quản lý ánh xạ đối tượng - quan hệ (ORM) giữa CSDL quan hệ **SQL Server 2022** và mã nguồn .NET thông qua **Entity Framework Core 8**.
- Tuân thủ nghiêm ngặt chiến lược **Database-First**: CSDL SQL Server là "Nguồn sự thật" duy nhất (Single Source of Truth); toàn bộ thực thể C# (Entities) và cấu hình `AppDbContext` đều được sinh tự động bằng công cụ Scaffold, **tuyệt đối không chỉnh sửa thủ công**.
- Quản lý các kết nối và client tích hợp dịch vụ lưu trữ đám mây bên ngoài (External Services) như **Cloudinary CDN**.

```
┌─────────────────────────────────────────────────────────────┐
│                       VeggieLife.BLL                        │
└──────────────────────────────┬──────────────────────────────┘
                               │ (LINQ Queries / ICloudinaryClient)
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                       VeggieLife.DAL                        │
│  - AppDbContext & AppDbContext.Partial.cs                   │
│  - 38 Scaffolded Entities (Chuẩn 3NF v3.1)                 │
│  - Master Seed Data (Thực phẩm, Dị ứng, Trường phái, VIP)   │
│  - External Clients: Cloudinary CDN Client                  │
└──────────────────────────────┬──────────────────────────────┘
                               │
               ┌───────────────┴───────────────┐
               ▼                               ▼
       [ SQL Server 2022 ]             [ Cloudinary CDN ]
```

---

## 2. Cấu trúc Thư mục Thực tế

```
VeggieLife.DAL/
├── Context/
│   ├── AppDbContext.cs               # ⚠ DO SCAFFOLD SINH: Khai báo 38 DbSet<T> và cấu hình Fluent API
│   └── AppDbContext.Partial.cs       # File mở rộng tùy biến: Cấu hình bổ sung không bị mất khi scaffold lại
├── Entities/                         # ⚠ 38 THỰC THỂ DO SCAFFOLD SINH, TUYỆT ĐỐI KHÔNG SỬA TAY
│   ├── User.cs, HealthProfile.cs, UserAllergy.cs, UserWeightHistory.cs
│   ├── DietarySchool.cs, Allergen.cs, FoodCategory.cs, Ingredient.cs, IngredientAllergen.cs
│   ├── Recipe.cs, RecipeIngredient.cs, Post.cs, Comment.cs, Vote.cs
│   ├── Video.cs, VideoTranscript.cs, ViolationReport.cs
│   ├── MealPlan.cs, MealSlot.cs, ShoppingList.cs, ShoppingListItem.cs
│   ├── Product.cs, ProductAllergen.cs, ProductEnumber.cs, EnumberDictionary.cs, UserScanHistory.cs
│   ├── FridgeInventory.cs, FridgeSession.cs
│   ├── VeganVenue.cs, VenueCategory.cs, VenueMenuItem.cs
│   ├── SubscriptionPlan.cs, PaymentTransaction.cs
│   ├── ChatSession.cs, ChatMessage.cs, Survey.cs
│   └── AiquotaLog.cs, AuditLog.cs
├── External/
│   ├── Cloudinary/                   # Dịch vụ CDN lưu trữ hình ảnh và video đã hoàn thành
│   │   ├── ICloudinaryClient.cs      # Hợp đồng giao tiếp tải lên, xóa và kiểm tra media
│   │   ├── CloudinaryClient.cs       # Thực thi gọi CloudinaryDotNet SDK kèm xử lý lỗi mạng
│   │   ├── CloudinarySettings.cs     # Đọc cấu hình CloudName, ApiKey, ApiSecret từ IConfiguration
│   │   ├── CloudinaryMediaResult.cs  # DTO kết quả upload trả về Url, PublicId, Duration, Format
│   │   ├── CloudinaryDeleteResult.cs # DTO kết quả xóa tài nguyên
│   │   └── CloudinaryException.cs    # Ngoại lệ chuẩn hóa khi lỗi CDN
│   ├── AiService/                    # [.gitkeep] Sẽ tích hợp client gọi microservice ai-service và stt-service
│   ├── Email/                        # [.gitkeep] Sẽ tích hợp client SMTP / SendGrid thật
│   ├── Geo/                          # [.gitkeep] Sẽ tích hợp client tra cứu GeoIP (MaxMind)
│   ├── Google/                       # [.gitkeep] Sẽ tích hợp Google OAuth & Google Places API
│   ├── HappyCow/                     # [.gitkeep] Sẽ tích hợp dịch vụ tra cứu quán chay HappyCow
│   ├── OpenFoodFacts/                # [.gitkeep] Sẽ tích hợp dịch vụ tra cứu Barcode quốc tế
│   └── Payments/                     # [.gitkeep] Sẽ tích hợp cổng thanh toán MoMo và VietQR
├── Repositories/                     # [.gitkeep] Thư mục dự phòng cho Generic Repository (Hiện tại BLL dùng trực tiếp AppDbContext)
│   ├── Interfaces/                   # [.gitkeep]
│   └── Implementations/              # [.gitkeep]
├── Cache/                            # [.gitkeep] (RedisService hiện được tổ chức tại tầng BLL)
└── Enums/                            # [.gitkeep] (Các hằng số và Role hiện được tổ chức tại tầng BLL)
```

---

## 3. Báo cáo Kết thúc Sprint 1: Những Gì ĐÃ Làm Được

### 3.1. Cấu trúc Cơ sở Dữ liệu 38 Bảng Chuẩn 3NF (v3.1)
- **Hoàn thành trọn bộ 38 tệp DDL SQL** trong thư mục `database/schema/`, được đánh số thứ tự tô-pô (Topological Sort) từ `01_DietarySchools.sql` đến `38_FridgeSessions.sql` đảm bảo không bị xung đột khóa ngoại khi chạy khởi tạo.
- **Script điều phối tự động:** File `database/scripts/create_all_tables.sql` và `run_all.sh` thực thi việc tạo toàn bộ bảng, khóa chính, khóa ngoại, chỉ mục (Unique Index) và các ràng buộc kiểm tra toàn vẹn (`CHECK Constraint`) theo đúng chuẩn thiết kế (D95, D109).

### 3.2. Ánh xạ Thực thể EF Core 8 Scaffold (38 Entities)
- Sinh tự động 38 lớp thực thể trong thư mục `Entities/` tương ứng với 38 bảng logic:
  - **Nhóm Người dùng & Hồ sơ:** `Users`, `HealthProfiles`, `UserAllergies`, `UserWeightHistories`.
  - **Nhóm Dữ liệu gốc & Dinh dưỡng:** `DietarySchools`, `Allergens`, `FoodCategories`, `Ingredients`, `IngredientAllergens`.
  - **Nhóm Cộng đồng & Công thức:** `Recipes`, `RecipeIngredients`, `Posts`, `Comments`, `Votes`, `Videos`, `VideoTranscripts`, `ViolationReports`.
  - **Nhóm Kế hoạch bữa ăn & Mua sắm:** `MealPlans`, `MealSlots`, `ShoppingLists`, `ShoppingListItems`.
  - **Nhóm Quét thực phẩm & Phụ gia:** `Products`, `ProductAllergens`, `ProductEnumbers`, `EnumberDictionaries`, `UserScanHistories`.
  - **Nhóm Tủ lạnh thông minh:** `FridgeInventories`, `FridgeSessions`.
  - **Nhóm Địa điểm quán chay:** `VeganVenues`, `VenueCategories`, `VenueMenuItems`.
  - **Nhóm Gói VIP & Thanh toán:** `SubscriptionPlans`, `PaymentTransactions`.
  - **Nhóm Trò chuyện AI & Khảo sát:** `ChatSessions`, `ChatMessages`, `Surveys`.
  - **Nhóm Kiểm toán & Hạn ngạch AI:** `AIQuotaLogs`, `AuditLogs`.
- Cấu hình `AppDbContext` chuẩn xác các mối quan hệ 1-Nhiều và Nhiều-Nhiều (ví dụ: `IngredientAllergen`, `ProductAllergen`, `UserAllergy`).
- Áp dụng kỹ thuật `AppDbContext.Partial.cs` để hỗ trợ mở rộng tùy biến mà không bị mất mã nguồn khi thực hiện scaffold lại CSDL.

### 3.3. Dữ liệu Gốc Idempotent (Master Seed Scripts)
Đã hoàn thành bộ script nạp dữ liệu gốc an toàn, cho phép chạy lặp lại nhiều lần mà không bị lỗi trùng khóa chính:
1. **7 Trường phái ăn chay (`DietarySchools`):** Bao gồm quy tắc cấm kỵ định dạng JSON chuẩn theo Quyết định D109 (Ăn chay kỳ, Thuần chay, Ăn chay có trứng/sữa, Ăn chay vì môi trường, Ăn chay tâm linh không ngũ vị tân, v.v.).
2. **14 Nhóm chất gây dị ứng (`Allergens`):** Các dị ứng phổ biến theo quy chuẩn quốc tế (Lạc, Đậu nành, Gluten, Hải sản, v.v.).
3. **15 Nhóm thực phẩm (`FoodCategories`):** Rau củ, Trái cây, Các loại hạt, Ngũ cốc, Đậu phụ, v.v.
4. **119 Nguyên liệu thuần chay (`Ingredients`):** Nạp đầy đủ thông tin calo, thành phần dinh dưỡng và liên kết dị ứng (`IngredientAllergens`).
5. **65 Phụ gia thực phẩm E-number (`EnumberDictionaries`):** Phân loại mức độ thuần chay (Thuần chay, Có nguồn gốc từ động vật, Nghi vấn cần kiểm tra) phục vụ tính năng quét mã vạch.
6. **3 Gói đăng ký hội viên VIP (`SubscriptionPlans`):** Gói 1 tháng (49.000 VNĐ), Gói 3 tháng (129.000 VNĐ), Gói 1 năm (449.000 VNĐ).
7. **Tài khoản Quản trị viên khởi tạo:** Tạo tài khoản Admin mặc định với mật khẩu được mã hóa chuẩn **BCrypt**, đảm bảo an toàn tuyệt đối.

### 3.4. Tích hợp Dịch vụ CDN Đám Mây (`CloudinaryClient`)
- Triển khai hoàn chỉnh `ICloudinaryClient` và `CloudinaryClient` sử dụng `CloudinaryDotNet` SDK:
  - Hỗ trợ tải lên ảnh với các chuyển đổi kích thước chuẩn (Upload avatar 500x500 crop thông minh, upload ảnh bài viết tối ưu nén).
  - Hỗ trợ tải lên video với phân loại thư mục `recipes/videos`, `posts/videos`.
  - Hỗ trợ xóa tài nguyên trên Cloudinary theo `publicId`.
  - Cơ chế bắt lỗi mạng và trả về `CloudinaryException` kèm thông điệp rõ ràng khi gặp sự cố máy chủ hoặc lỗi 502 Bad Gateway.
  - **Tuân thủ Zero-Hardcoded Secrets:** 100% thông tin bảo mật (`CloudName`, `ApiKey`, `ApiSecret`) được đọc từ `IConfiguration` thông qua `CloudinarySettings`, ném `InvalidOperationException` nếu thiếu cấu hình.

---

## 4. Báo cáo: Những Gì CHƯA Làm (Backlog cho Sprint Tiếp Theo)

1. **Repository Pattern & Unit of Work:**
   - Các thư mục `Repositories/Interfaces/` và `Repositories/Implementations/` hiện tại vẫn chỉ là `.gitkeep`.
   - Chưa triển khai `IGenericRepository<T>`, `IUnitOfWork` và các repository chuyên biệt (`IUserRepository`, `IPostRepository`, `IRecipeRepository`, v.v.).
   - *Ghi chú:* Tầng BLL trong Sprint 1 đang gọi trực tiếp qua `AppDbContext`. Cần quyết định kiến trúc ở Sprint 2 xem có tiếp tục mô hình Direct DbContext hay chuyển sang Unit of Work.
2. **Các Dịch vụ Tích hợp Ngoại vi Chưa Triển Khai (`External/`):**
   - **`AiService/`:** Chưa có `AiServiceClient` gọi HTTP REST / gRPC sang microservice `ai-service` (FastAPI) và `stt-service` (Cloud Run) kèm chính sách ngắt mạch dự phòng Polly (Circuit Breaker).
   - **`Email/`:** Chưa có `SmtpEmailClient` gửi email thật qua giao thức SMTP hoặc các nhà cung cấp như SendGrid / Amazon SES.
   - **`Geo/`:** Chưa có `IpGeolocationClient` tra cứu tọa độ và mã quốc gia IP thực tế từ cơ sở dữ liệu MaxMind GeoLite2 hoặc ip-api.com.
   - **`Google/`:** Chưa có client xác thực Google OAuth Token và client gọi Google Places API để tìm kiếm địa điểm quán ăn chay.
   - **`HappyCow/`:** Chưa có client cào dữ liệu hoặc tích hợp với HappyCow API để làm phong phú dữ liệu nhà hàng chay.
   - **`OpenFoodFacts/`:** Chưa có client gọi API OpenFoodFacts để truy xuất thông tin sản phẩm khi quét barcode chưa có trong CSDL cục bộ.
   - **`Payments/`:** Chưa có `MoMoPaymentClient` và `VietQrPaymentClient` để tạo đơn thanh toán trực tuyến và ký số bảo mật HMAC-SHA256.
3. **Tối ưu Hóa Chỉ Mục CSDL (Database Indexing & Full-Text Search):**
   - Chưa thiết lập SQL Server Full-Text Index trên các cột tìm kiếm văn bản như `Recipes.Title`, `Recipes.Instructions`, `Posts.Content` (hiện đang dùng `EF.Functions.Like`).

---

## 5. Thay đổi Kiến trúc & Điểm Lệch (Blockers / Divergences) Buộc Phải Chỉnh Docs

### 1. Không Sử Dụng Repository Pattern (BLL Truy Cập Trực Tiếp `AppDbContext`)
- **Mô tả trong tài liệu (`Backend/README.md` mục 2 & 3, `05. Architecture and Deployment.docx`):** Quy định nghiêm ngặt luồng phụ thuộc: `VeggieLife.API` ──► `VeggieLife.BLL` ──► `VeggieLife.DAL (Repositories)` ──► `SQL Server`. BLL không được phụ thuộc trực tiếp vào EF Core.
- **Thực tế triển khai:** Tầng `VeggieLife.DAL` chỉ cung cấp `AppDbContext` và `Entities`. Tầng `VeggieLife.BLL` inject trực tiếp `AppDbContext` để thực hiện các truy vấn LINQ.
- **Lý do kỹ thuật:** Việc dựng Generic Repository gây phát sinh mã thừa (Boilerplate code) và làm mất đi các tính năng mạnh mẽ của EF Core 8 (như `AsNoTracking()`, `Include()` nhiều cấp, `Select()` chiếu sang DTO trực tiếp ở tầng CSDL để tối ưu hiệu năng).
- **Tác động tài liệu:** Cần cập nhật `05. Architecture and Deployment.docx` và `Backend/README.md` để:
  - Chính thức ghi nhận việc BLL sử dụng trực tiếp `AppDbContext` cho các tác vụ truy vấn và cập nhật.
  - Cập nhật sơ đồ phân lớp không còn bắt buộc phải qua `IRepository`.

### 2. Loại Bỏ Hoàn Toàn Gemini SDK & Qdrant SDK Khỏi DAL (Ranh Giới AI - D118, D119)
- **Mô tả trong tài liệu ban đầu:** Trong một số tài liệu thiết kế ban đầu có thư mục `DAL/External/Gemini` và `DAL/External/Qdrant`.
- **Thực tế triển khai:** Tuyệt đối không cài đặt hay giữ khóa Gemini API và Qdrant SDK trong DAL. Mọi nghiệp vụ AI được đẩy sang AI Microservice bên ngoài. Thư mục `External/` chỉ chuẩn bị `AiService/` để gọi API nội bộ sang AI Microservice.
- **Tác động tài liệu:** Xóa bỏ hoàn toàn mọi nội dung mô tả DAL kết nối Gemini / Qdrant trong `05. Architecture and Deployment.docx`.

### 3. Vị Trí Của `RedisService` và `Enums`
- **Mô tả trong tài liệu ban đầu:** `IRedisService` nằm tại `DAL/Cache` và các `Enums` nằm tại `DAL/Enums`.
- **Thực tế triển khai:** `RedisService` được đặt tại `VeggieLife.BLL/Services/Cache` do gắn chặt với logic nghiệp vụ phiên, hạn mức và xác thực. Các Enums và Constants nghiệp vụ được đặt tại `VeggieLife.BLL/Common/Constants`.
- **Tác động tài liệu:** Cập nhật lại sơ đồ thư mục của `VeggieLife.DAL` trong tài liệu kiến trúc.

### 4. Quy Chuẩn Bất Khả Xâm Phạm: Zero-Hardcoded Secrets (Theo `GEMINI.md`)
- **Nguyên tắc:** Toàn bộ chuỗi kết nối CSDL SQL Server và thông tin định danh Cloudinary đều được nạp từ biến môi trường hoặc `IConfiguration`. Ném `InvalidOperationException` ngay tại thời điểm khởi tạo nếu thiếu giá trị.
- **Tác động tài liệu:** Ghi chú rõ trong tài liệu hướng dẫn triển khai (`05. Architecture and Deployment.docx`) rằng file `appsettings.json` thực tế không bao giờ được commit lên repository, nhóm phát triển chỉ chia sẻ `appsettings.Example.json`.

---

## 6. Hướng dẫn Phát triển & Vận hành cho Sprint Tiếp Theo

### 6.1. Quy trình Cập nhật CSDL (Database-First Workflow)
Khi có sự thay đổi về cấu trúc bảng hoặc thêm bảng mới, thực hiện đúng quy trình 4 bước:
1. **Chỉnh sửa / Tạo mới file DDL SQL:** Thêm hoặc cập nhật file tương ứng trong `database/schema/` (đặt tên theo định dạng `<Số_thứ_tự>_<Tên_bảng>.sql`).
2. **Thực thi cập nhật CSDL:** Chạy script tái tạo CSDL trên máy chủ SQL Server cục bộ:
   ```bash
   cd database/scripts
   bash run_all.sh
   ```
3. **Scaffold lại EF Core Model:** Chạy script scaffold để cập nhật các file trong `src/VeggieLife.DAL/Entities` và `AppDbContext.cs`:
   ```bash
   bash scaffold.sh
   ```
4. **Không sửa tay Entities:** Mọi logic mở rộng (như nạp dữ liệu mềm, thuộc tính phụ trợ) bắt buộc phải viết trong `AppDbContext.Partial.cs` hoặc các lớp `partial class` riêng biệt.

### 6.2. Hướng dẫn Triển khai External Service Mới (Ví dụ: `AiServiceClient`)
1. Tạo thư mục `External/<ServiceName>/`.
2. Tạo Interface `I<ServiceName>Client` định nghĩa các phương thức nghiệp vụ.
3. Tạo lớp cài đặt `<ServiceName>Client` tiêm `HttpClient` từ `IHttpClientFactory` và `IOptions<<ServiceName>Settings>`.
4. Đăng ký HttpClient kèm chính sách Resilience (Polly Retry, Circuit Breaker) trong `VeggieLife.API/Extensions/ServiceExtensions.cs`.
5. Đảm bảo cấu hình URL và Secret Key được nạp an toàn từ `IConfiguration`, tuyệt đối không hardcode.
