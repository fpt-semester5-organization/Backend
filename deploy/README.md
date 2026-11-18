# Triển khai VeggieLife lên Google Cloud (Cloud Run + VM)

Project `veggielife-510817`, vùng `asia-southeast1`, chưa có tên miền riêng (dùng địa chỉ `*.run.app`).

```
Trình duyệt ──► Frontend (Cloud Run, nginx tĩnh)
       └──────► Backend  (Cloud Run, min=max=1) ──VPC──► VM veggielife-core-vm (10.148.0.2)
                                    │                      ├─ SQL Server 2022   :1433
                                    │                      ├─ Redis 7           :6379
                                    │                      └─ ai-service+Qdrant :8081
                                    └──► stt-service (Cloud Run, Whisper)
```

Địa chỉ Cloud Run có dạng cố định `https://<dịch vụ>-<số project>.asia-southeast1.run.app`, nên các script tự tính
được URL Frontend/Backend trước khi deploy (dùng cho CORS và ReturnUrl PayOS).

## 0. Làm một lần
```bash
gcloud config set project veggielife-510817
gcloud services enable run.googleapis.com artifactregistry.googleapis.com cloudbuild.googleapis.com secretmanager.googleapis.com
gcloud artifacts repositories create veggielife --repository-format=docker --location=asia-southeast1
```
Firewall: mạng `default` đã cho phép lưu lượng nội bộ (`default-allow-internal`, dải 10.128.0.0/9 gồm 10.148.0.0/20).
**Không** mở 1433/6379/8081 ra `0.0.0.0/0`.

## 1. VM: SQL Server + Redis
Trên máy dev, tạo bản sao lưu DB (dùng bản sao lưu thay vì seeder, vì `DevDataSeeder` tạo tài khoản mẫu và lỗi khóa ngoại khi DB trống):
```bash
cd veggielife-backend
MSSQL_SA_PASSWORD=<mật khẩu SQL local> ./deploy/export-local-db.sh
gcloud compute scp deploy/vm/docker-compose.yml deploy/vm/.env.example deploy/vm/restore-db.sh deploy/vm/backup/VeggieLifeDb.bak \
  veggielife-core-vm:~/veggielife/ --zone <zone>
```
Trên VM (SSH):
```bash
sudo apt-get update && sudo apt-get install -y docker.io docker-compose-v2 && sudo usermod -aG docker $USER   # đăng nhập lại
cd ~/veggielife && mkdir -p backup && mv VeggieLifeDb.bak backup/ && cp .env.example .env && nano .env       # điền 2 mật khẩu
docker compose up -d && ./restore-db.sh
```
Kiểm tra RAM trước (`free -h`): SQL Server cần ≥ 2 GB, bản `Express` giới hạn 1 DB ≤ 10 GB là đủ cho demo.

## 2. VM: ai-service + Qdrant (repo `veggielife-ai-service`)
```bash
git clone <repo ai-service> && cd veggielife-ai-service
cp deploy/.env.vm.example .env && nano .env        # INTERNAL_API_KEY trùng secret veggielife-ai-internal-key
docker compose -f deploy/docker-compose.vm.yml up -d --build
docker compose -f deploy/docker-compose.vm.yml exec ai-service python -m scripts.ingest_books   # nạp kho tri thức vào Qdrant
```
Gemini dùng service account gắn vào VM (cần quyền `roles/aiplatform.user` + scope `cloud-platform`), không cần tệp JSON khóa.

## 3. stt-service (Cloud Run, repo `veggielife-ai-service`)
```bash
PROJECT=veggielife-510817 REGION=asia-southeast1 ./scripts/deploy_stt.sh     # in ra URL -> STT_URL
```

## 4. Bí mật (Secret Manager) và Backend
```bash
cd veggielife-backend
./deploy/create_secrets.sh          # nhập ẩn từng giá trị; không in ra màn hình
STT_URL=https://stt-service-....run.app GOOGLE_CLIENT_ID=... CLOUDINARY_CLOUD_NAME=... SMTP_USER=you@gmail.com \
  ./deploy/deploy_backend.sh
curl "$(gcloud run services describe veggielife-backend --region asia-southeast1 --format='value(status.url)')/health"
```
Backend chạy `min=max=1` + `--no-cpu-throttling` vì có tác vụ nền (hết hạn đơn/VIP, hàng đợi STT) và SignalR chưa có backplane.

## 5. Frontend
```bash
cd veggielife-frontend
GOOGLE_CLIENT_ID=... GOONG_MAPTILES_KEY=... ./deploy/deploy_frontend.sh
```

## 6. Cấu hình dịch vụ ngoài (làm tay)
- **Google OAuth**: thêm URL Frontend vào *Authorized JavaScript origins*.
- **PayOS**: khai webhook `https://<backend>.run.app/api/v1/payments/payos/webhook` trong dashboard (Backend đã có đối soát chủ động nên vẫn nâng VIP nếu webhook trượt).
- **Cloudinary / Goong**: giữ nguyên khoá; Goong tile key nhúng vào Frontend là khoá công khai.

## 7. Kiểm thử sau deploy
Đăng nhập, đăng bài (kiểm duyệt AI), chat AI, thanh toán PayOS gói 49.000đ (VIP lên, biên lai PDF, email), STT video, trang admin.

## Rủi ro đã biết
- Lên Production vẫn chạy `DevDataSeeder` khi khởi động; DB khôi phục từ bản sao lưu nên các bước tạo user mẫu bị bỏ qua.
- Tài khoản test dùng chung mật khẩu seed (xem `database/seed/timeline/TEST_ACCOUNTS.local.md` trên máy dev) — đổi hoặc vô hiệu hóa trước khi mở công khai.
- Cloud Run giới hạn kích thước một yêu cầu HTTP khoảng 32 MB: upload video lớn nên đi thẳng lên Cloudinary.
- Bản deploy Frontend không có tệp PDF sách dinh dưỡng (các symlink sang repo ai-service bị gỡ khi build), nên xem trước PDF trong trang Admin sẽ báo không có tệp.
- Xoay các khoá đã lộ trong chat (PayOS, Cloudinary, Google, SMTP) trước khi dùng thật.
