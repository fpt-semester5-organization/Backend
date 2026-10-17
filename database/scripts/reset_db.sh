#!/usr/bin/env bash
# ============================================================
# Reset database VeggieLifeDb về trạng thái sạch (Drop và Create lại)
# ============================================================

set -e

SERVER="${DB_SERVER:-(localdb)\\MSSQLLocalDB}"
DATABASE="${DB_NAME:-VeggieLifeDb}"
USER="${DB_USER:-}"
PASSWORD="${DB_PASSWORD:-}"

echo "============================================================"
echo " VeggieLife Database Reset Tool"
echo " Server:   $SERVER"
echo " Database: $DATABASE"
echo "============================================================"

AUTH_FLAGS=()
if [ -n "$USER" ] && [ -n "$PASSWORD" ]; then
    AUTH_FLAGS=(-U "$USER" -P "$PASSWORD")
else
    AUTH_FLAGS=(-E)
fi

SQL_QUERY="
IF DB_ID('$DATABASE') IS NOT NULL
BEGIN
    ALTER DATABASE [$DATABASE] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$DATABASE];
    PRINT 'Đã xóa database [$DATABASE] thành công.';
END
CREATE DATABASE [$DATABASE];
PRINT 'Đã khởi tạo database mới [$DATABASE] thành công.';
"

sqlcmd -S "$SERVER" "${AUTH_FLAGS[@]}" -Q "$SQL_QUERY" -b
echo "==> Reset database $DATABASE thành công!"
