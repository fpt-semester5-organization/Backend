#!/usr/bin/env bash
# ==============================================================================
# File: seed_master_data.sh
# Mục đích: Thực thi nạp dữ liệu Master Data và kiểm chứng tính Idempotent trên Linux / CI
# ==============================================================================

set -euo pipefail

DB_SERVER="${DB_SERVER:-localhost}"
DB_NAME="${DB_NAME:-VeggieLifeDb}"
DB_USER="${DB_USER:-}"
DB_PASSWORD="${DB_PASSWORD:-}"
ADMIN_EMAIL="${ADMIN_EMAIL:-admin@veggielife.vn}"
ADMIN_PASSWORD="${ADMIN_PASSWORD:-Admin@VeggieLife2026!}"
ADMIN_FULLNAME="${ADMIN_FULLNAME:-Quản Trị Viên Hệ Thống}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/../.." && pwd)"
SEED_DIR="${ROOT_DIR}/database/seed"

echo "============================================================"
echo "  VeggieLife Master Seed Runner (Linux / Bash)"
echo "  Server:   ${DB_SERVER}"
echo "  Database: ${DB_NAME}"
echo "  Admin:    ${ADMIN_EMAIL}"
echo "============================================================"

AUTH_ARGS=()
if [[ -n "${DB_USER}" && -n "${DB_PASSWORD}" ]]; then
    AUTH_ARGS=("-U" "${DB_USER}" "-P" "${DB_PASSWORD}")
fi

run_seed_batch() {
    local batch_name="$1"
    echo "--- [${batch_name}] Đang chạy Seed Scripts ---"
    for file in "${SEED_DIR}"/0*.sql; do
        filename=$(basename "$file")
        if [[ "$filename" == "00_Seed_All.sql" ]]; then continue; fi
        echo -n "  -> Chạy ${filename}... "
        sqlcmd -S "${DB_SERVER}" -d "${DB_NAME}" "${AUTH_ARGS[@]}" \
            -v ADMIN_EMAIL="${ADMIN_EMAIL}" \
            -v ADMIN_FULLNAME="${ADMIN_FULLNAME}" \
            -I -i "$file" -b
        echo "[OK]"
    done
}

# Chạy lần 1
run_seed_batch "LẦN CHẠY THỨ 1"

# Chạy lần 2 để kiểm thử tính Idempotent
run_seed_batch "LẦN CHẠY THỨ 2"

echo "============================================================"
echo " Hoàn tất thực thi Seed Data thành công!"
echo "============================================================"
