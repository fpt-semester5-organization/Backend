#!/bin/bash
# Double-click: nạp seed60 vào SQL Server (Docker hoặc sqlcmd cài sẵn). Mật khẩu đọc từ appsettings.json, không in ra màn hình.
cd "$(dirname "$0")" || exit 1
ROOT="$(cd ../../../.. && pwd)"
SQL1="$PWD/out/seed60_all.sql"; SQL2="$PWD/out/update_ingredient_images.sql"
CFG="$ROOT/veggielife-backend/src/VeggieLife.API/appsettings.json"
read -r SRV DB USR PWD_ < <(python3 - "$CFG" <<'PY'
import json,re,sys
cs=json.load(open(sys.argv[1],encoding='utf-8'))["ConnectionStrings"]["DefaultConnection"]
g=lambda k:(re.search(k+r"=([^;]*)",cs,re.I) or [None,""])[1]
print(g("Server"),g("Database"),g("User Id"),g("Password"))
PY
)
HOST="${SRV%%,*}"; PORT="${SRV##*,}"; [ "$PORT" = "$SRV" ] && PORT=1433
run_sql() {  # $1 = file
  if command -v sqlcmd >/dev/null 2>&1; then
    sqlcmd -S "$HOST,$PORT" -U "$USR" -P "$PWD_" -d "$DB" -C -f 65001 -b -i "$1"
  else
    CT=$(docker ps --format '{{.Names}} {{.Image}}' | awk '/mssql\/server/ {print $1; exit}')
    [ -z "$CT" ] && { echo "Không thấy container SQL Server đang chạy. Chạy: docker compose up -d (trong veggielife-backend)"; return 1; }
    docker cp "$1" "$CT:/tmp/seed_in.sql" >/dev/null
    docker exec -u 0 "$CT" chmod 644 /tmp/seed_in.sql
    docker exec "$CT" /bin/bash -c "S=/opt/mssql-tools18/bin/sqlcmd; [ -x \$S ] || S=/opt/mssql-tools/bin/sqlcmd; \$S -S localhost -U '$USR' -P '$PWD_' -d '$DB' -C -f 65001 -b -i /tmp/seed_in.sql"
  fi
}
echo ">> Nạp dữ liệu seed60 (xóa theo dải ID >= 10001 rồi nạp lại)..."
run_sql "$SQL1" && echo ">> Cập nhật ảnh nguyên liệu..." && run_sql "$SQL2" && echo ">> XONG." || echo ">> LỖI - gửi nội dung ở trên cho Claude."
echo "Có thể đóng cửa sổ này."
