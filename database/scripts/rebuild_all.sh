#!/usr/bin/env bash
# ============================================================
# Chạy toàn bộ quy trình tự động hóa Database: Reset DB -> Chạy DDL Schema -> Scaffold EF Core Entities.
# ============================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "============================================================"
echo " VeggieLife Full Database & Model Rebuild Pipeline"
echo "============================================================"

# Bước 1: Reset database
echo ""
echo ">>> BƯỚC 1: RESET DATABASE <<<"
bash "$SCRIPT_DIR/reset_db.sh"

# Bước 2: Chạy schema và seed
echo ""
echo ">>> BƯỚC 2: CHẠY SCHEMA DDL & SEED DATA <<<"
bash "$SCRIPT_DIR/run_all.sh"

# Bước 3: Scaffold EF Core Model
echo ""
echo ">>> BƯỚC 3: SCAFFOLD EF CORE MODEL & BUILD DAL <<<"
bash "$SCRIPT_DIR/scaffold.sh"

echo ""
echo "============================================================"
echo " TẤT CẢ CÁC BƯỚC ĐÃ HOÀN TẤT THÀNH CÔNG!"
echo "============================================================"
