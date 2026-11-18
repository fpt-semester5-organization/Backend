#!/usr/bin/env bash
# Chạy TRÊN VM sau khi `docker compose up -d`: khôi phục VeggieLifeDb từ ./backup/VeggieLifeDb.bak.
set -euo pipefail
cd "$(dirname "$0")"
set -a; . ./.env; set +a
[ -f backup/VeggieLifeDb.bak ] || { echo "Thiếu backup/VeggieLifeDb.bak"; exit 1; }
docker compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -b -Q "
RESTORE DATABASE [VeggieLifeDb] FROM DISK = N'/var/opt/mssql/backup/VeggieLifeDb.bak' WITH REPLACE,
  MOVE N'VeggieLifeDb' TO N'/var/opt/mssql/data/VeggieLifeDb.mdf',
  MOVE N'VeggieLifeDb_log' TO N'/var/opt/mssql/data/VeggieLifeDb_log.ldf';"
echo "Đã khôi phục VeggieLifeDb."
