# Hướng dẫn Cơ sở dữ liệu VeggieLife (Database-First)

Tài liệu hướng dẫn khởi tạo, đồng bộ và kiểm tra nghiệm thu tự động cơ sở dữ liệu **VeggieLife** (38 bảng thực thể chuẩn 3NF, 312 thuộc tính) dành cho tất cả các thành viên trong nhóm phát triển.

---

## 1. Cấu trúc thư mục `database/`

```text
database/
├── README.md                 # Tài liệu hướng dẫn này
├── schema/                   # 38 file DDL SQL (mỗi bảng 1 file, sắp xếp theo thứ tự khóa ngoại)
│   ├── 01_DietarySchools.sql
│   ├── 02_Allergens.sql
│   ├── ...
│   ├── 34_VenueMenuItems.sql # Đủ 7 cột logic theo D100 (bỏ tính năng trong MVP)
│   └── 38_Surveys.sql
├── seed/                     # Dữ liệu mẫu khởi tạo ban đầu (Master Data, Admin User, VIP Plans...)
│   ├── 00_Seed_All.sql
│   ├── 01_Seed_DietarySchools.sql
│   └── ...
└── scripts/                  # Toàn bộ công cụ và script tự động hóa (PowerShell, Shell, Python, SQL)
    ├── generate_schema.py    # Tool tự động parse Word docx sinh 38 bảng & kiểm tra đếm
    ├── generate_schema.ps1   # Wrapper 1-click chạy tool trên Windows
    ├── verify_schema.sql     # Script SQL đếm tự động: đúng 38 bảng, 312 cột
    ├── verify_schema.ps1     # Wrapper 1-click chạy kiểm tra SQL Server
    ├── rebuild_all.ps1 / .sh # Pipeline 1-click: Reset DB -> Chạy DDL -> Nạp Seed -> Scaffold EF Core
    ├── run_all.ps1 / .sh     # Chạy toàn bộ 38 file schema và seed data
    ├── reset_db.ps1 / .sh    # Xóa sạch và tạo mới database trống
    └── scaffold.ps1 / .sh    # Scaffold Entities C# từ database vào VeggieLife.DAL
```

---

## 2. Hướng dẫn sử dụng nhanh (Dành cho thành viên mới clone code về máy)

### Bước 1: Khởi tạo toàn bộ CSDL và Entity C# (Khuyên dùng - 1 Click)
Khi mới clone code về hoặc muốn làm mới lại cơ sở dữ liệu từ đầu, mở PowerShell tại thư mục gốc của dự án (`Backend/`) và chạy:

```powershell
powershell -ExecutionPolicy Bypass -File "./database/scripts/rebuild_all.ps1"
```

*Lệnh trên sẽ tự động thực hiện 3 bước:*
1. Xóa và tạo mới database `VeggieLifeDb` trên SQL Server local.
2. Chạy lần lượt 38 file DDL theo đúng thứ tự phụ thuộc khóa ngoại và nạp dữ liệu mẫu (Seed Data).
3. Tự động sinh (scaffold) lại toàn bộ Entity C# trong `src/VeggieLife.DAL/Entities/`.

---

### Bước 2: Kiểm tra nghiệm thu tính toàn vẹn (Verify 38 bảng & 312 cột)
Để xác nhận cơ sở dữ liệu trên máy bạn đã khớp 100% với tài liệu đặc tả của BA/Lead:

```powershell
# Chạy kiểm tra nghiệm thu tự động:
powershell -ExecutionPolicy Bypass -File "./database/scripts/generate_schema.ps1" -VerifyDb
```

👉 Khi kết quả trả về:
```text
>>> KẾT QUẢ DATABASE TRỰC TIẾP: [PASS] ĐẠT 100% (38 BẢNG, 312 CỘT)!
```
Nghĩa là cơ sở dữ liệu trên máy bạn đã hoàn toàn hợp lệ, bạn có thể bắt đầu code tính năng ngay!

---

## 3. Các lệnh tiện ích khác khi cần

| Nhu cầu công việc | Lệnh thực hiện (PowerShell) |
|---|---|
| **Chạy lại schema & seed mà không xóa DB** | `powershell -ExecutionPolicy Bypass -File "./database/scripts/run_all.ps1"` |
| **Xóa trắng toàn bộ database** | `powershell -ExecutionPolicy Bypass -File "./database/scripts/reset_db.ps1"` |
| **Chỉ cập nhật lại Entity C# sau khi đổi DB** | `powershell -ExecutionPolicy Bypass -File "./database/scripts/scaffold.ps1"` |
| **Kiểm tra đếm nhanh bằng câu lệnh SQL** | `powershell -ExecutionPolicy Bypass -File "./database/scripts/verify_schema.ps1"` |

---

## 4. Các quy tắc quan trọng cần ghi nhớ
1. **Database-First là tối thượng:** Không bao giờ tự ý sửa tay các file Entity C# trong `src/VeggieLife.DAL/Entities/`. Nếu cần đổi cột/bảng, hãy sửa file SQL trong `database/schema/` rồi chạy lại `./database/scripts/scaffold.ps1`.
2. **Quyết định D100 (`VenueMenuItems`):** Bảng số 34 vẫn tồn tại trong CSDL với đủ 7 cột, nhưng dự án đã thống nhất bỏ tính năng thực đơn quán ăn trong MVP, không cần viết DTO hay Controller cho bảng này.
3. **Idempotency:** Toàn bộ file DDL đều có kiểm tra `IF NOT EXISTS` nên bạn có thể chạy lại nhiều lần an toàn mà không bị lỗi trùng bảng.
