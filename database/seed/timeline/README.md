# SEED60 – mock data 60 ngày cho VeggieLife

Sinh bằng Python 3 (không cần thư viện ngoài), nạp bằng sqlcmd/SSMS. Đọc master từ `../04_Seed_Ingredients.sql`, `../06_Seed_ENumberDictionary.sql`.

## Sinh file
    cd veggielife-backend/database/seed/timeline
    python3 generate_seed60.py                 # -> ./out  (seed 60, deterministic)
    python3 generate_seed60.py --seed 7 --out ./out7
    python3 generate_seed60.py --media-map ../../../../Data/_manifest/cloudinary_map.json   # sau khi upload ảnh thật

## Nạp (đã có schema + seed master 01..06)
    sqlcmd -S localhost -d VeggieLife -E -f 65001 -i out/seed60_all.sql
    sqlcmd -S localhost -d VeggieLife -E -f 65001 -i verify_timeline.sql      # mọi dòng phải PASS
    sqlcmd -S localhost -d VeggieLife -E -f 65001 -i out/update_ingredient_images.sql   # tuỳ chọn, 1 URL/nguyên liệu

Hoặc nạp từng Level theo thứ tự: `00_header, 01_cleanup, 02_lookups, 10..15 (Level 0/1), 20..24 (Level 2), 99_footer` (cần chạy chung 1 batch vì dùng biến `@Now`, bảng tạm `#sch/#cat/#alg/#plan/#ing` và 1 transaction).

## Tài khoản test (chỉ dùng cho DB local/dev)
Mọi tài khoản seed và admin hệ thống dùng chung mật khẩu test `Veggie@2026` (bcrypt `$2a$11$GBRaj...` trong `07_Seed_AdminUser.sql` và `HASH` của generator).

| Vai trò | Email |
|---|---|
| Admin hệ thống (`07_Seed_AdminUser.sql`) | `admin@veggielife.vn` |
| Admin seed60 | `admin01@seed60.veggielife.vn`, `admin02@seed60.veggielife.vn` |
| Thành viên VIP có nhiều dữ liệu | `user067@seed60.veggielife.vn` |
| Thành viên thường | `userNNN@seed60.veggielife.vn` (NNN = 003…150; tài khoản Google không có mật khẩu, tài khoản đã xoá không đăng nhập được) |

## Đặc điểm
- Dải ID seed 10001–99999, email `@seed60.veggielife.vn`, giữ nguyên dữ liệu cũ. Chạy lại nhiều lần an toàn: 01_cleanup xoá dải 10001–99999 theo thứ tự ngược phụ thuộc; Users/HealthProfiles/UserAllergies chỉ xoá user có email seed. Cuối file đẩy identity mọi bảng lên 100000 nên dữ liệu app tạo sau khi nạp (ID ≥ 100000) không bị xoá khi nạp lại. Ngoại lệ: dữ liệu app trỏ tới dòng seed (vd. bình luận vào bài seed) sẽ chặn bước xoá do khoá ngoại.
- `@Now = GETUTCDATE()`, mọi mốc là `DATEADD(MINUTE, -n, @Now)`; mật khẩu mọi user seed dùng chung bcrypt hash của dữ liệu cũ.
- Ảnh/video là URL Cloudinary placeholder `res.cloudinary.com/swrsuenp/.../veggielife/seed60/...` (chưa upload thật) – thay bằng `--media-map` rồi sinh lại.
- Không sinh `VenueMenuItems`; không tạo bảng mới.
- Nội dung công thức do generator tự soạn (placeholder hợp lý), sẽ thay bằng dữ liệu sách nấu ăn khi có.
