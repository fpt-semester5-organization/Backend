#!/usr/bin/env bash
# Tạo/cập nhật bí mật cho Backend trong Google Secret Manager. Giá trị đọc ẩn từ bàn phím (hoặc từ biến môi trường
# cùng tên), KHÔNG in ra màn hình và không lưu vào file. Bỏ trống một mục = giữ nguyên bản cũ.
#
#   PROJECT=veggielife-510817 ./deploy/create_secrets.sh
set -euo pipefail
PROJECT="${PROJECT:-veggielife-510817}"
gcloud services enable secretmanager.googleapis.com --project "$PROJECT" >/dev/null

# tên secret | biến môi trường tùy chọn | mô tả
SECRETS=(
  "veggielife-db-connection|DB_CONNECTION|Chuỗi kết nối SQL Server, vd Server=10.148.0.2,1433;Database=VeggieLifeDb;User Id=sa;Password=...;TrustServerCertificate=True;MultipleActiveResultSets=true;"
  "veggielife-redis-connection|REDIS_CONNECTION|vd 10.148.0.2:6379,password=...,abortConnect=false"
  "veggielife-jwt-key|JWT_KEY|Khóa ký JWT, ≥ 32 ký tự ngẫu nhiên"
  "veggielife-ai-internal-key|AI_INTERNAL_KEY|Phải trùng INTERNAL_API_KEY của ai-service và stt-service"
  "veggielife-google-client-secret|GOOGLE_CLIENT_SECRET|OAuth client secret"
  "veggielife-cloudinary-api-key|CLOUDINARY_API_KEY|Cloudinary API key"
  "veggielife-cloudinary-api-secret|CLOUDINARY_API_SECRET|Cloudinary API secret"
  "veggielife-goong-api-key|GOONG_API_KEY|Goong API key"
  "veggielife-goong-map-key|GOONG_MAP_KEY|Goong map key"
  "veggielife-payos-client-id|PAYOS_CLIENT_ID|PayOS Client ID"
  "veggielife-payos-api-key|PAYOS_API_KEY|PayOS API key"
  "veggielife-payos-checksum-key|PAYOS_CHECKSUM_KEY|PayOS Checksum key"
  "veggielife-vietqr-secret|VIETQR_SECRET|Khóa bí mật VietQR"
  "veggielife-smtp-password|SMTP_PASSWORD|Mật khẩu ứng dụng Gmail"
)
for entry in "${SECRETS[@]}"; do
  IFS='|' read -r name envvar desc <<<"$entry"
  value="${!envvar:-}"
  if [ -z "$value" ]; then
    printf '%s (%s)\n  nhập (ẩn, Enter để bỏ qua): ' "$name" "$desc"; read -rs value; echo
  fi
  [ -z "$value" ] && { echo "  - bỏ qua $name"; continue; }
  if gcloud secrets describe "$name" --project "$PROJECT" >/dev/null 2>&1; then
    printf '%s' "$value" | gcloud secrets versions add "$name" --project "$PROJECT" --data-file=- >/dev/null
    echo "  + đã thêm phiên bản mới cho $name"
  else
    printf '%s' "$value" | gcloud secrets create "$name" --project "$PROJECT" --replication-policy=automatic --data-file=- >/dev/null
    echo "  + đã tạo $name"
  fi
done
echo "Xong. Chạy ./deploy/deploy_backend.sh để cấp quyền đọc secret cho service account và triển khai."
