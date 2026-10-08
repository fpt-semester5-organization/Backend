#!/usr/bin/env bash

# ============================================================
# Tự động reverse-engineer (scaffold) database thành Entity POCO và AppDbContext
# cho VeggieLife.DAL theo chuẩn Backend/README.md
# ============================================================

set -e

SERVER="${DB_SERVER:-localhost}"
DATABASE="${DB_NAME:-VeggieLifeDb}"
USER="${DB_USER:-}"
PASSWORD="${DB_PASSWORD:-}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"
DAL_PROJECT="$BACKEND_DIR/src/VeggieLife.DAL/VeggieLife.DAL.csproj"
API_PROJECT="$BACKEND_DIR/src/VeggieLife.API/VeggieLife.API.csproj"

STARTUP_PROJECT="$DAL_PROJECT"
if [ -f "$API_PROJECT" ]; then
    STARTUP_PROJECT="$API_PROJECT"
fi

if [ -n "$USER" ] && [ -n "$PASSWORD" ]; then
    CONN_STR="Server=$SERVER;Database=$DATABASE;User Id=$USER;Password=$PASSWORD;TrustServerCertificate=True;"
else
    CONN_STR="Server=$SERVER;Database=$DATABASE;Trusted_Connection=True;TrustServerCertificate=True;"
fi

echo "============================================================"
echo " VeggieLife EF Core Database Scaffold Tool"
echo " Server:          $SERVER"
echo " Database:        $DATABASE"
echo " Target Project:  $DAL_PROJECT"
echo " Startup Project: $STARTUP_PROJECT"
echo " Output Dir:      Entities"
echo " Context Dir:     Context (AppDbContext)"
echo "============================================================"

# Kiểm tra dotnet ef tool
if ! command -v dotnet-ef &> /dev/null && ! dotnet ef --version &> /dev/null; then
    echo "Đang cài đặt dotnet-ef tool (global)..."
    dotnet tool install --global dotnet-ef || true
fi

echo ""
echo "Đang khôi phục NuGet packages (dotnet restore)..."
dotnet restore "$DAL_PROJECT"

echo ""
echo "Đang thực thi Scaffold từ database..."
dotnet ef dbcontext scaffold "$CONN_STR" Microsoft.EntityFrameworkCore.SqlServer \
    -p "$DAL_PROJECT" -s "$STARTUP_PROJECT" \
    --output-dir Entities --context-dir Context --context AppDbContext \
    --no-onconfiguring --force

echo ""
echo "==> Scaffold hoàn tất thành công!"
echo "Đang biên dịch kiểm tra dự án VeggieLife.DAL..."
dotnet build "$DAL_PROJECT" --no-incremental
echo "==> Dự án VeggieLife.DAL biên dịch thành công 100%!"