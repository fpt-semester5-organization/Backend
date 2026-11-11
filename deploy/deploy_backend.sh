#!/usr/bin/env bash
# Triển khai Backend (.NET 8) lên Google Cloud Run. Chạy tay bởi người có quyền project, SAU khi:
#   1. VM đã chạy SQL Server + Redis + ai-service (xem deploy/README.md)
#   2. stt-service đã deploy (scripts/deploy_stt.sh ở repo ai-service) -> có STT_URL
#   3. đã chạy ./deploy/create_secrets.sh
#
#   STT_URL=https://stt-service-xxxx.asia-southeast1.run.app \
#   GOOGLE_CLIENT_ID=... CLOUDINARY_CLOUD_NAME=... SMTP_USER=you@gmail.com ./deploy/deploy_backend.sh
#
# Không có tên miền riêng: URL Cloud Run có dạng xác định https://<dịch vụ>-<số project>.<vùng>.run.app, nên tính được
# địa chỉ Frontend (cho CORS, ReturnUrl PayOS) ngay trước khi Frontend được deploy.
set -euo pipefail
PROJECT="${PROJECT:-veggielife-510817}"; REGION="${REGION:-asia-southeast1}"
BE_SERVICE="${BE_SERVICE:-veggielife-backend}"; FE_SERVICE="${FE_SERVICE:-veggielife-frontend}"
VM_IP="${VM_IP:-10.148.0.2}"; AI_PORT="${AI_PORT:-8081}"
NETWORK="${NETWORK:-default}"; SUBNET="${SUBNET:-default}"
: "${STT_URL:?đặt STT_URL (URL Cloud Run của stt-service)}"
: "${GOOGLE_CLIENT_ID:?đặt GOOGLE_CLIENT_ID}"
: "${CLOUDINARY_CLOUD_NAME:?đặt CLOUDINARY_CLOUD_NAME}"
: "${SMTP_USER:?đặt SMTP_USER (địa chỉ Gmail gửi biên lai/OTP)}"
VIETQR_BANK_ID="${VIETQR_BANK_ID:-MB}"; VIETQR_ACCOUNT_NO="${VIETQR_ACCOUNT_NO:-}"; VIETQR_ACCOUNT_NAME="${VIETQR_ACCOUNT_NAME:-VeggieLife}"

PROJECT_NUMBER="$(gcloud projects describe "$PROJECT" --format='value(projectNumber)')"
FE_URL="https://${FE_SERVICE}-${PROJECT_NUMBER}.${REGION}.run.app"
SA="veggielife-backend@${PROJECT}.iam.gserviceaccount.com"
IMAGE="$REGION-docker.pkg.dev/$PROJECT/veggielife/backend:$(git rev-parse --short HEAD)"

echo ">> Service account và quyền đọc secret"
gcloud iam service-accounts describe "$SA" --project "$PROJECT" >/dev/null 2>&1 || \
  gcloud iam service-accounts create veggielife-backend --project "$PROJECT" --display-name "VeggieLife Backend"
for s in $(gcloud secrets list --project "$PROJECT" --filter="name~veggielife-" --format='value(name)'); do
  gcloud secrets add-iam-policy-binding "$s" --project "$PROJECT" \
    --member "serviceAccount:$SA" --role roles/secretmanager.secretAccessor >/dev/null
done

echo ">> Build image ($IMAGE)"
gcloud builds submit --project "$PROJECT" --region "$REGION" --tag "$IMAGE" .

# Dấu ^|^ đổi ký tự phân tách sang | vì giá trị có dấu phẩy
ENV_VARS="^|^ASPNETCORE_ENVIRONMENT=Production|ASPNETCORE_FORWARDEDHEADERS_ENABLED=true|Database__UseInMemory=false"
ENV_VARS+="|Jwt__Issuer=VeggieLife.API|Jwt__Audience=VeggieLife.Client|Jwt__AccessTokenExpiryMinutes=15"
ENV_VARS+="|JwtSettings__Issuer=VeggieLife|JwtSettings__Audience=VeggieLifeUsers"
ENV_VARS+="|AiService__BaseUrl=http://${VM_IP}:${AI_PORT}|AiService__SttBaseUrl=${STT_URL}"
ENV_VARS+="|Cors__AllowedOrigins=${FE_URL}"
ENV_VARS+="|Google__ClientId=${GOOGLE_CLIENT_ID}|Cloudinary__CloudName=${CLOUDINARY_CLOUD_NAME}|Goong__BaseUrl=https://rsapi.goong.io"
ENV_VARS+="|Payment__PayOS__ReturnUrl=${FE_URL}/payment/success|Payment__PayOS__CancelUrl=${FE_URL}/payment/cancel"
ENV_VARS+="|Payment__VietQR__BankId=${VIETQR_BANK_ID}|Payment__VietQR__AccountNo=${VIETQR_ACCOUNT_NO}|Payment__VietQR__AccountName=${VIETQR_ACCOUNT_NAME}"
ENV_VARS+="|Smtp__ActiveProvider=Gmail|Smtp__Gmail__Host=smtp.gmail.com|Smtp__Gmail__Port=587|Smtp__Gmail__EnableSsl=true"
ENV_VARS+="|Smtp__Gmail__User=${SMTP_USER}|Smtp__Gmail__From=${SMTP_USER}|Smtp__Gmail__DisplayName=VeggieLife"

SECRETS="ConnectionStrings__DefaultConnection=veggielife-db-connection:latest"
SECRETS+=",ConnectionStrings__Redis=veggielife-redis-connection:latest"
SECRETS+=",Jwt__SecretKey=veggielife-jwt-key:latest,JwtSettings__SecretKey=veggielife-jwt-key:latest"
SECRETS+=",AiService__InternalApiKey=veggielife-ai-internal-key:latest"
SECRETS+=",Google__ClientSecret=veggielife-google-client-secret:latest"
SECRETS+=",Cloudinary__ApiKey=veggielife-cloudinary-api-key:latest,Cloudinary__ApiSecret=veggielife-cloudinary-api-secret:latest"
SECRETS+=",Goong__ApiKey=veggielife-goong-api-key:latest,Goong__MapKey=veggielife-goong-map-key:latest"
SECRETS+=",Payment__PayOS__ClientId=veggielife-payos-client-id:latest,Payment__PayOS__ApiKey=veggielife-payos-api-key:latest,Payment__PayOS__ChecksumKey=veggielife-payos-checksum-key:latest"
SECRETS+=",Payment__VietQR__SecretKey=veggielife-vietqr-secret:latest"
SECRETS+=",Smtp__Gmail__Password=veggielife-smtp-password:latest"

echo ">> Deploy Cloud Run: $BE_SERVICE"
# min=max=1 + không giới hạn CPU: Backend có tác vụ nền (hết hạn đơn/VIP, hàng đợi STT) và SignalR chưa có backplane.
# Direct VPC egress để tới SQL Server/Redis/ai-service trên VM qua IP nội bộ (chỉ dải riêng đi qua VPC).
gcloud run deploy "$BE_SERVICE" --project "$PROJECT" --region "$REGION" --image "$IMAGE" \
  --service-account "$SA" --port 8080 --memory 1Gi --cpu 1 \
  --min-instances 1 --max-instances 1 --no-cpu-throttling --concurrency 80 --timeout 3600 \
  --network "$NETWORK" --subnet "$SUBNET" --vpc-egress private-ranges-only \
  --allow-unauthenticated \
  --set-env-vars "$ENV_VARS" --set-secrets "$SECRETS"

BE_URL="$(gcloud run services describe "$BE_SERVICE" --project "$PROJECT" --region "$REGION" --format='value(status.url)')"
echo
echo "Backend:  $BE_URL   (kiểm tra: curl $BE_URL/health)"
echo "Frontend sẽ ở: $FE_URL"
echo "Webhook PayOS cần khai trong dashboard PayOS: ${BE_URL}/api/v1/payments/payos/webhook"
echo "Google OAuth: thêm ${FE_URL} vào Authorized JavaScript origins."
