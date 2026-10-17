#!/usr/bin/env bash
# ============================================================
# Thực thi toàn bộ DDL trong schema/ và master data trong seed/ bằng sqlcmd
# ============================================================

set -e

SERVER="${DB_SERVER:-(localdb)\\MSSQLLocalDB}"
DATABASE="${DB_NAME:-VeggieLifeDb}"
USER="${DB_USER:-}"
PASSWORD="${DB_PASSWORD:-}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SCHEMA_DIR="$(cd "$SCRIPT_DIR/../schema" && pwd)"
SEED_DIR="$(cd "$SCRIPT_DIR/../seed" && pwd)"

echo "============================================================"
echo " VeggieLife Schema & Seed Runner"
echo " Server:   $SERVER"
echo " Database: $DATABASE"
echo " Schema:   $SCHEMA_DIR"
echo "============================================================"

AUTH_FLAGS=()
if [ -n "$USER" ] && [ -n "$PASSWORD" ]; then
    AUTH_FLAGS=(-U "$USER" -P "$PASSWORD")
else
    AUTH_FLAGS=(-E)
fi

# 1. Tạo database nếu chưa tồn tại
sqlcmd -S "$SERVER" "${AUTH_FLAGS[@]}" -Q "IF DB_ID('$DATABASE') IS NULL CREATE DATABASE [$DATABASE];" -b

# 2. Chạy toàn bộ file DDL trong schema/
echo ""
echo "[1/2] Đang thực thi các file DDL trong schema/..."
for file in $(ls "$SCHEMA_DIR"/*.sql 2>/dev/null | sort); do
    echo "  -> Running $(basename "$file")..."
    sqlcmd -S "$SERVER" -d "$DATABASE" "${AUTH_FLAGS[@]}" -I -i "$file" -b
done

# 3. Chạy seed data nếu có file
echo ""
if ls "$SEED_DIR"/*.sql 1> /dev/null 2>&1; then
    echo "[2/2] Đang thực thi các file Seed Data..."
    for file in $(ls "$SEED_DIR"/0*.sql | sort); do
        filename=$(basename "$file")
        if [ "$filename" = "00_Seed_All.sql" ]; then continue; fi
        echo "  -> Running $filename..."
        sqlcmd -S "$SERVER" -d "$DATABASE" "${AUTH_FLAGS[@]}" -I -i "$file" -b
    done
else
    echo "[2/2] Không có file seed data nào trong thư mục seed/ (bỏ qua)."
fi

echo ""
echo "============================================================"
echo " Hoàn tất thực thi toàn bộ schema thành công!"
echo "============================================================"
