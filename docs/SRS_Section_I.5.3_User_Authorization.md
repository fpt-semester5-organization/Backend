# I.5.3. USER AUTHORIZATION (PHÂN QUYỀN NGƯỜI DÙNG)

Phân quyền người dùng trong hệ thống **VeggieLife** được thiết kế theo mô hình **Role-Based Access Control (RBAC)** với cơ chế bảo vệ hai lớp (Two-tier Security):
- **Tầng Client (Frontend):** Sử dụng các bộ bảo vệ tuyến đường (**Route Guards** tại `guards/index.tsx`) gồm `GuestOnly`, `RequireAuth`, `RequireVip`, `RequireAdmin` để điều hướng hiển thị giao diện, chặn điều hướng trái phép và mở modal đăng ký/nâng cấp (`SignupPromptModal`).
- **Tầng Server (Backend):** Sử dụng các chính sách ủy quyền (**Authorization Policies & Middleware** trên ASP.NET Core 8 Web API tương ứng với `roles.ts`) để thẩm định JWT Access Token, bảo vệ dữ liệu tuyệt đối trước mọi hành vi vượt rào bằng công cụ bên ngoài.

---

### 1. MÔ TẢ CHI TIẾT CÁC VAI TRÒ HỆ THỐNG (ROLE DESCRIPTIONS)

Hệ thống phân định 4 nhóm vai trò người dùng với ranh giới quyền hạn và trách nhiệm rõ ràng như sau:

#### a) Guest (Khách vãng lai)
- **Đặc điểm & Xác thực:** Là người dùng truy cập ứng dụng nhưng chưa đăng nhập hoặc chưa có tài khoản. Hệ thống tự động cấp phát một **Anonymous JWT Token** mang vai trò danh nghĩa `Role: GUEST` (theo quy tắc nghiệp vụ `BR-GUEST-AUTH-01`).
- **Quyền hạn được phép:**
  - Thực hiện các thao tác xác thực cơ bản: Đăng nhập (`SCR-01`), Đăng ký tài khoản mới (`SCR-02`), Nhập OTP kích hoạt (`SCR-03`), Yêu cầu quên mật khẩu (`SCR-04`) và Đặt lại mật khẩu mới (`SCR-05`). (Các màn hình này được bảo vệ bởi Route Guard `GuestOnly` để tránh người đã đăng nhập truy cập lại).
  - Tra cứu và đọc thông tin công khai: Xem danh sách và chi tiết công thức món chay đã được kiểm duyệt, xem bảng tin bài viết chia sẻ từ cộng đồng, xem video hướng dẫn nấu ăn và phụ đề từng bước (Transcript).
  - Tìm kiếm và tra cứu địa điểm quán ăn chay, nhà hàng chay trên bản đồ số tương tác (Venue Map).
  - Tra cứu từ điển phụ gia thực phẩm E-Number và mức độ an toàn đối với người ăn chay.
  - Sử dụng công cụ tính toán cơ bản: Nhập chiều cao, cân nặng để tính chỉ số khối cơ thể (BMI) và nhu cầu năng lượng cơ bản (TDEE) ở chế độ xem tạm thời (không lưu vào cơ sở dữ liệu).
  - Quét mã vạch sản phẩm (Barcode Scanner) qua camera hoặc nhập tay để tra cứu thành phần dinh dưỡng từ Open Food Facts.
  - Trải nghiệm Chatbot AI tư vấn dinh dưỡng với hạn ngạch thử nghiệm bị giới hạn số lượt nghiêm ngặt theo phiên/IP.
- **Ràng buộc & Giới hạn:**
  - **Chặn toàn bộ thao tác GHI (Write/Mutate) vào cơ sở dữ liệu:** Không được đăng bài, không được bình luận, không được thả tim (like), không được tạo công thức cá nhân, không được lưu trữ dữ liệu tủ lạnh và không được lưu thực đơn tuần (theo `FN-AUTH-03`).
  - Khi thực hiện các thao tác ghi trên giao diện, Route Guard sẽ chặn lại và kích hoạt hiển thị `SignupPromptModal` để hướng dẫn người dùng đăng ký tài khoản.
  - Chịu sự kiểm soát của cơ chế giới hạn tần suất yêu cầu (**Rate Limiting**): Tối đa **10 yêu cầu/phút trên mỗi địa chỉ IP** (theo `BR-GUEST-SEC-01` tại `GuestSecurityMiddleware`).

#### b) Member (Thành viên chính thức)
- **Đặc điểm & Xác thực:** Là người dùng đã hoàn tất quy trình đăng ký tài khoản và xác thực thành công qua mã Email OTP 6 số (`FN-AUTH-04`, `FN-AUTH-05`). Khi đăng nhập, người dùng nhận được Access Token mang vai trò `Role: MEMBER`.
- **Quyền hạn được phép:**
  - Kế thừa toàn bộ quyền hạn đọc và tra cứu của Guest.
  - **Quản lý Hồ sơ cá nhân & Sức khỏe:** Đổi mật khẩu cá nhân (`SCR-06`), cập nhật thông tin cá nhân (`SCR-07`), khởi tạo và cập nhật hồ sơ sức khỏe (cân nặng, chiều cao, mục tiêu calo, tiền sử dị ứng thực phẩm); lựa chọn trường phái ăn chay áp dụng (Thuần chay Vegan, Chay có trứng Ovo, Chay có sữa Lacto, Chay kỳ Flexitarian,...).
  - **Quản lý Tủ lạnh thông minh (Fridge Management):** Thực hiện đầy đủ các thao tác Thêm, Sửa, Xóa (CRUD) nguyên liệu thực phẩm trong tủ lạnh cá nhân; theo dõi hạn sử dụng và nhận cảnh báo thực phẩm cận date.
  - **Kế hoạch bữa ăn & Thực đơn tuần:** Tự tạo và chỉnh sửa kế hoạch ăn chay theo tuần; tự động sinh danh sách nguyên liệu cần mua (Shopping List) tương ứng.
  - **Tương tác Cộng đồng:** Đăng bài viết chia sẻ kiến thức, đăng tải công thức món chay mới do bản thân sáng tạo (nội dung sẽ được chuyển vào hàng đợi kiểm duyệt tự động bằng AI trước khi hiển thị); tham gia bình luận đa cấp (nested comments), thả tim, chia sẻ và báo cáo bài viết vi phạm.
  - **Đề xuất địa điểm:** Đóng góp tọa độ và thông tin quán ăn chay mới lên bản đồ cộng đồng.
  - **Chatbot AI dinh dưỡng:** Sử dụng Chatbot AI để tư vấn thực đơn và chế độ ăn chay theo hạn mức tiêu chuẩn dành cho thành viên.
  - **Đăng ký gói VIP:** Được phép truy cập màn hình Checkout (`SCR-31`) và thanh toán nâng cấp tài khoản lên hội viên VIP.
- **Ràng buộc:** Không có quyền truy cập vào các tính năng vi chất chuyên sâu của gói VIP và toàn bộ phân hệ quản trị dành riêng cho Administrator. Khi đã đăng nhập, người dùng bị `GuestOnly` chặn truy cập các màn Login/Register (`SCR-01` đến `SCR-05`).

#### c) VIP (Hội viên trả phí nâng cao)
- **Đặc điểm & Xác thực:** Là thành viên chính thức đã thanh toán kích hoạt thành công một trong các gói thuê bao dịch vụ nâng cao (gói 1 tháng, 3 tháng hoặc 1 năm qua cổng thanh toán số VietQR/MoMo). Token mang vai trò `Role: VIP` kèm định danh `isVip: true`.
- **Quyền hạn đặc quyền:**
  - Kế thừa toàn bộ 100% quyền hạn của Member.
  - **Báo cáo vi chất calo chuyên sâu:** Xem phân tích chi tiết toàn diện về các chỉ số đa lượng (Đạm thực vật, Tinh bột, Chất béo lành mạnh) và các vi chất dinh dưỡng thiết yếu (Sắt, Kẽm, Canxi, Vitamin B12, Omega-3) của từng món ăn và toàn bộ thực đơn trong tuần.
  - **Xuất bản thực đơn chuẩn y khoa:** Xuất kế hoạch thực đơn dinh dưỡng cả tuần và bảng định lượng nguyên liệu ra định dạng **PDF** chuẩn mực để phục vụ in ấn hoặc gửi bác sĩ/chuyên gia dinh dưỡng.
  - **Đặc quyền AI toàn diện & Không giới hạn:**
    - Sử dụng Chatbot AI tư vấn dinh dưỡng chuyên sâu **không giới hạn số lượt truy vấn** (`BR-VIP-AI-01`).
    - Tính năng AI quét và nhận diện hình ảnh nguyên liệu trong tủ lạnh tự động, kết hợp đối chiếu tiền sử dị ứng và bệnh lý (tiểu đường, tim mạch, huyết áp) để đề xuất công thức nấu ăn an toàn.
  - **Ưu tiên xử lý:** Bài viết và công thức đóng góp của hội viên VIP được xếp quyền ưu tiên cao hơn trong hàng đợi kiểm duyệt nội dung.
  - **Gia hạn gói VIP:** Được phép tiếp tục thanh toán gia hạn gói VIP tại màn hình Checkout.

#### d) Administrator (Quản trị viên hệ thống)
- **Đặc điểm & Xác thực:** Tài khoản quản trị nội bộ cấp cao nhất, nhận Access Token mang vai trò `Role: ADMIN`. Tài khoản Admin được khởi tạo sẵn trong Database (DB Seed), tuyệt đối không thông qua màn hình đăng ký công khai. Chỉ Administrator mới có quyền truy cập vào phân hệ Quản trị (`/admin/*`) với bố cục giao diện quản trị riêng biệt (`AdminLayout`).
- **Quyền hạn được phép:**
  - **Kiểm duyệt nội dung (Content Moderation):** Phê duyệt hoặc từ chối các bài viết, công thức nấu ăn, bình luận từ cộng đồng; trực tiếp giám sát và xử lý hàng đợi kiểm duyệt tự động của AI (`AI Moderation Queue`) cũng như các khiếu nại báo cáo vi phạm (`FN-MOD-01..06`).
  - **Thẩm định công thức chuẩn dinh dưỡng (Recipe Verification):** Kiểm tra chuyên môn và cấp huy hiệu công thức chuẩn ("Verified Badge").
  - **Quản lý người dùng (User Management):** Tra cứu danh sách người dùng toàn hệ thống, xem nhật ký hoạt động, thực hiện khóa/mở khóa tài khoản khi phát hiện hành vi vi phạm tiêu chuẩn cộng đồng.
  - **Quản lý Danh mục & Cơ sở dữ liệu chuyên ngành (Master Data Management):** Thêm, sửa, xóa danh mục món ăn, 7 trường phái ăn chay, từ điển phụ gia E-Numbers, danh mục nguyên liệu và bảng phân loại các chất gây dị ứng (Allergens).
  - **Cấu hình gói VIP & Tài chính:** Thiết lập biểu phí, thời hạn và quyền lợi các gói thuê bao VIP; thực hiện đối soát giao dịch thanh toán từ các cổng ngân hàng/ví điện tử. (Admin không cần mua gói VIP vì đã có toàn quyền truy cập).
  - **Quản trị Địa điểm quán chay:** Xét duyệt, cập nhật trạng thái hiển thị hoặc gỡ bỏ các địa điểm quán ăn chay do cộng đồng đề xuất.
  - **Giám sát hệ thống AI (LlmOps & Vector Database):** Theo dõi hạn mức sử dụng token AI, kiểm soát trần chi phí vận hành API, đánh giá độ chính xác của phản hồi AI và điều phối tiến trình đồng bộ dữ liệu vào cơ sở dữ liệu véc-tơ Qdrant (Knowledge Base).
  - **Nhật ký kiểm toán (Audit Logs):** Tra cứu và thanh tra toàn bộ nhật ký ghi vết thao tác trên hệ thống (ai thực hiện, thao tác gì, tác động lên bảng dữ liệu nào, thời gian nào) phục vụ công tác an toàn và bảo mật dữ liệu.

---

### 2. MA TRẬN PHÂN QUYỀN NGƯỜI DÙNG (TABLE 3: USER AUTHORIZATION MATRIX)

*Ghi chú quy ước:*
- Ký hiệu **X**: Có quyền truy cập màn hình và thực thi thao tác tương ứng.
- Để trống: Bị chặn quyền truy cập (bị Route Guard chặn hoặc Backend từ chối với mã HTTP 403 Forbidden).

| Screen / Function | Guest | Member | VIP | Administrator |
|:---|:---:|:---:|:---:|:---:|
| **SCR-01: auth/Login** — Đăng nhập hệ thống (Email, Mật khẩu, Google OAuth) | **X** | | | |
| **SCR-02: auth/Register** — Đăng ký tài khoản người dùng mới | **X** | | | |
| **SCR-03: auth/VerifyOtp** — Xác thực mã OTP kích hoạt tài khoản | **X** | | | |
| **SCR-04: auth/ForgotPassword** — Yêu cầu cấp lại mật khẩu (Gửi OTP qua Email) | **X** | | | |
| **SCR-05: auth/ResetPassword** — Đặt lại mật khẩu mới bằng mã xác thực OTP | **X** | | | |
| **SCR-06: auth/ChangePassword** — Thay đổi mật khẩu tài khoản cá nhân | | **X** | **X** | **X** |
| **SCR-07: profile/Profile** — Xem và cập nhật thông tin cá nhân (Họ tên, Avatar) | | **X** | **X** | **X** |
| **SCR-08: profile/HealthProfile** — Tính chỉ số khối cơ thể (BMI) tạm thời | **X** | **X** | **X** | **X** |
| **SCR-08: profile/HealthProfile** — Lưu và cập nhật hồ sơ sức khỏe & mục tiêu calo | | **X** | **X** | **X** |
| **SCR-09: profile/DietaryPreferences** — Xem danh mục trường phái ăn chay & dị ứng | **X** | **X** | **X** | **X** |
| **SCR-09: profile/DietaryPreferences** — Cập nhật trường phái ăn chay & tiền sử dị ứng | | **X** | **X** | **X** |
| **SCR-10: home/Home** — Trang chủ, xem bảng tin nổi bật & tìm kiếm tổng hợp | **X** | **X** | **X** | **X** |
| **SCR-11: recipes/RecipeList** — Tra cứu, tìm kiếm & lọc công thức món chay | **X** | **X** | **X** | **X** |
| **SCR-12: recipes/RecipeDetail** — Xem chi tiết công thức nấu ăn, nguyên liệu, các bước làm | **X** | **X** | **X** | **X** |
| **SCR-12: recipes/RecipeDetail** — Lưu công thức vào bộ sưu tập cá nhân | | **X** | **X** | **X** |
| **SCR-12: recipes/RecipeDetail** — Xem phân tích chi tiết vi chất calo chuyên sâu (Macro & Micro) | | | **X** | **X** |
| **SCR-13: recipes/RecipeEditor** — Tạo mới (Create) & Chỉnh sửa (Update) công thức nấu ăn | | **X** | **X** | **X** |
| **SCR-14: recipes/MyRecipes** — Quản lý công thức cá nhân (Xem danh sách, Sửa, Xóa) | | **X** | **X** | **X** |
| **SCR-15: scanner/BarcodeScan** — Quét mã vạch thực phẩm bằng Camera / Nhập mã tra cứu | **X** | **X** | **X** | **X** |
| **SCR-16: scanner/ScanHistory** — Xem và quản lý lịch sử các sản phẩm đã quét | | **X** | **X** | **X** |
| **SCR-17: fridge/Fridge** — Quản lý tủ lạnh (Xem, Thêm, Sửa, Xóa thực phẩm & hạn dùng) | | **X** | **X** | **X** |
| **SCR-17: fridge/Fridge** — AI quét nhận diện ảnh chụp tủ lạnh & gợi ý món ăn theo vi chất | | | **X** | **X** |
| **SCR-18: venues/VenueMap** — Tra cứu và điều hướng bản đồ số các quán ăn, nhà hàng chay | **X** | **X** | **X** | **X** |
| **SCR-19: venues/VenueDetail** — Xem thông tin chi tiết, địa chỉ, thực đơn và giờ mở cửa quán chay | **X** | **X** | **X** | **X** |
| **SCR-20: venues/SuggestVenue** — Đề xuất địa điểm quán ăn chay mới lên bản đồ hệ thống | | **X** | **X** | **X** |
| **SCR-21: videos/VideoPlayer** — Xem video nấu ăn và phụ đề phân đoạn (Transcript) | **X** | **X** | **X** | **X** |
| **SCR-21: videos/VideoPlayer** — Lưu video yêu thích và đánh dấu công thức nấu ăn | | **X** | **X** | **X** |
| **SCR-22: meal-plans/MealPlan** — Xem kế hoạch thực đơn tuần, kích hoạt và đổi món | | **X** | **X** | **X** |
| **SCR-22: meal-plans/MealPlan** — Xuất kế hoạch thực đơn dinh dưỡng tuần ra file PDF y khoa | | | **X** | **X** |
| **SCR-23: meal-plans/Generate** — Sinh thực đơn ăn chay tự động bằng AI (hạn mức cơ bản) | | **X** | **X** | **X** |
| **SCR-23: meal-plans/Generate** — AI sinh thực đơn chuyên sâu tối ưu theo bệnh lý (không giới hạn) | | | **X** | **X** |
| **SCR-24: shopping-lists/ShoppingList** — Xem & đánh dấu danh sách nguyên liệu cần mua | | **X** | **X** | **X** |
| **SCR-25: community/PostFeed** — Xem bảng tin bài viết chia sẻ kinh nghiệm cộng đồng | **X** | **X** | **X** | **X** |
| **SCR-26: community/PostDetail** — Đọc nội dung chi tiết bài viết cộng đồng | **X** | **X** | **X** | **X** |
| **SCR-26: community/PostDetail** — Tương tác bình luận nhiều tầng, Thả tim, Báo cáo bài viết | | **X** | **X** | **X** |
| **SCR-27: community/PostEditor** — Soạn thảo, đăng tải (Create) & Sửa (Update) bài viết | | **X** | **X** | **X** |
| **SCR-28: community/MyPosts** — Quản lý bài viết cá nhân (Xem danh sách, Xóa bài) | | **X** | **X** | **X** |
| **SCR-29: chat/Chat** — Chatbot AI tư vấn dinh dưỡng cơ bản (giới hạn lượt theo phiên) | **X** | **X** | **X** | **X** |
| **SCR-29: chat/Chat** — Chatbot AI tư vấn chuyên sâu, phân tích dinh dưỡng không giới hạn | | | **X** | **X** |
| **SCR-30: subscription/Pricing** — Xem biểu phí và quyền lợi các gói thuê bao VIP | **X** | **X** | **X** | **X** |
| **SCR-31: subscription/Checkout** — Thanh toán đăng ký gói hội viên VIP (VietQR/MoMo) | | **X** | **X** | |
| **SCR-32: subscription/PaymentResult** — Xem kết quả xác nhận giao dịch thanh toán VIP | | **X** | **X** | |
| **SCR-33: subscription/MySubscription** — Quản lý trạng thái gói VIP và lịch sử thanh toán | | **X** | **X** | |
| **SCR-34: admin/AdminModerationQueue** — Kiểm duyệt bài viết & Hàng đợi AI Moderation | | | | **X** |
| **SCR-35: admin/CommentModeration** — Kiểm duyệt và xóa bình luận vi phạm | | | | **X** |
| **SCR-36: admin/Categories** — Quản lý danh mục món ăn (Thêm, Sửa, Xóa) | | | | **X** |
| **SCR-37: admin/RecipeVerification** — Thẩm định và cấp huy hiệu công thức chuẩn dinh dưỡng | | | | **X** |
| **SCR-38: admin/Dashboard** — Bảng điều khiển thống kê tổng quan toàn hệ thống | | | | **X** |
| **SCR-39: admin/Users** — Quản lý danh sách người dùng, xem hồ sơ & Khóa/Mở tài khoản | | | | **X** |
| **SCR-40: admin/AuditLogs** — Tra cứu nhật ký kiểm toán hệ thống (Audit Logs) | | | | **X** |
| **SCR-41: admin/LlmOps** — Giám sát AI, chi phí API, chất lượng & Đồng bộ Qdrant | | | | **X** |
| **SCR-42: admin/DietarySchools** — Quản lý 7 trường phái ăn chay & Từ điển phụ gia E-Numbers | | | | **X** |
| **SCR-43: admin/Ingredients** — Quản lý từ điển nguyên liệu & Danh mục chất dị ứng | | | | **X** |
| **SCR-44: admin/SubscriptionPlans** — Cấu hình gói thuê bao VIP & Đối soát doanh thu | | | | **X** |
| **SCR-45: admin/Venues** — Quản lý, phê duyệt & Ẩn/Hiện địa điểm quán ăn chay | | | | **X** |
