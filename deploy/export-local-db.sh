#!/usr/bin/env bash
# Chạy TRÊN MÁY DEV: sao lưu VeggieLifeDb đang chạy trong container local ra deploy/vm/backup/VeggieLifeDb.bak
# để chép lên VM và khôi phục (deploy/vm/restore-db.sh). Dùng bản sao lưu thay vì seeder để khỏi chạy DevDataSeeder
# (seeder dev tạo tài khoản mẫu và lỗi khóa ngoại khi DB trống).
set -euo pipefail
CT="${CT:-veggielife-sqlserver}"; DB="${DB:-VeggieLifeDb}"
: "${MSSQL_SA_PASSWORD:?đặt MSSQL_SA_PASSWORD của container local}"
OUT="$(cd "$(dirname "$0")" && pwd)/vm/backup"; mkdir -p "$OUT"
docker exec "$CT" /bin/bash -c 'S=/opt/mssql-tools18/bin/sqlcmd; [ -x $S ] || S=/opt/mssql-tools/bin/sqlcmd; \
  $S -S localhost -U sa -P "$0" -C -Q "BACKUP DATABASE [$1] TO DISK = N'"'"'/var/opt/mssql/backup/VeggieLifeDb.bak'"'"' WITH INIT, COMPRESSION"' "$MSSQL_SA_PASSWORD" "$DB"
docker cp "$CT:/var/opt/mssql/backup/VeggieLifeDb.bak" "$OUT/VeggieLifeDb.bak"
ls -la "$OUT/VeggieLifeDb.bak"
echo "Chép lên VM:  gcloud compute scp deploy/vm/backup/VeggieLifeDb.bak <vm>:~/veggielife/backup/ --zone <zone> --project veggielife-510817"
