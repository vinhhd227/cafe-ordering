#!/usr/bin/env bash
# restore-prod.sh — Restore PostgreSQL DB + uploads volume từ backup
#
# Cách dùng:
#   bash scripts/restore-prod.sh                  # restore từ backup mới nhất
#   bash scripts/restore-prod.sh 2026-03-24       # restore từ ngày cụ thể
#   bash scripts/restore-prod.sh --db-only        # chỉ restore DB
#   bash scripts/restore-prod.sh --uploads-only   # chỉ restore uploads

set -euo pipefail

# ── Cấu hình ──────────────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
ENV_FILE="$PROJECT_DIR/.env.prod"

BACKUP_DIR="${BACKUP_DIR:-$HOME/cafe-backups}"
DB_CONTAINER="${DB_CONTAINER:-cafe-db-prod}"
UPLOADS_VOLUME="${UPLOADS_VOLUME:-cafe-uploads}"
# ──────────────────────────────────────────────────────────────────────────────

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; CYAN='\033[0;36m'; NC='\033[0m'

log()      { echo -e "[$(date '+%Y-%m-%d %H:%M:%S')] $*"; }
log_ok()   { log "${GREEN}✔${NC} $*"; }
log_err()  { log "${RED}✖${NC} $*"; }
log_warn() { log "${YELLOW}!${NC} $*"; }
log_info() { log "${CYAN}ℹ${NC} $*"; }

# Parse args
RESTORE_DATE=""
DB_ONLY=false
UPLOADS_ONLY=false

for arg in "$@"; do
  case $arg in
    --db-only)      DB_ONLY=true ;;
    --uploads-only) UPLOADS_ONLY=true ;;
    [0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]) RESTORE_DATE="$arg" ;;
    *) echo "Tham số không hợp lệ: $arg"; echo "Dùng: $0 [YYYY-MM-DD] [--db-only|--uploads-only]"; exit 1 ;;
  esac
done

# Đọc biến từ .env.prod
if [[ ! -f "$ENV_FILE" ]]; then
  log_err ".env.prod không tìm thấy tại $ENV_FILE"
  exit 1
fi

DB_USER=$(grep -E '^DB_USER=' "$ENV_FILE" | cut -d= -f2)
DB_NAME=$(grep -E '^DB_NAME=' "$ENV_FILE" | cut -d= -f2)

if [[ -z "$DB_USER" || -z "$DB_NAME" ]]; then
  log_err "DB_USER hoặc DB_NAME không tìm thấy trong $ENV_FILE"
  exit 1
fi

# Tìm thư mục backup
if [[ -n "$RESTORE_DATE" ]]; then
  BACKUP_DATE_DIR="$BACKUP_DIR/$RESTORE_DATE"
  if [[ ! -d "$BACKUP_DATE_DIR" ]]; then
    log_err "Không tìm thấy backup ngày $RESTORE_DATE tại $BACKUP_DATE_DIR"
    exit 1
  fi
else
  # Lấy backup mới nhất
  BACKUP_DATE_DIR=$(find "$BACKUP_DIR" -maxdepth 1 -mindepth 1 -type d | sort -r | head -1)
  if [[ -z "$BACKUP_DATE_DIR" ]]; then
    log_err "Không tìm thấy backup nào trong $BACKUP_DIR"
    exit 1
  fi
  RESTORE_DATE=$(basename "$BACKUP_DATE_DIR")
fi

# Tìm file backup trong thư mục
DB_FILE=$(find "$BACKUP_DATE_DIR" -name "db_*.dump" | sort -r | head -1)
UPLOADS_FILE=$(find "$BACKUP_DATE_DIR" -name "uploads_*.tar.gz" | sort -r | head -1)

log "══════════════════════════════════════════════"
log_info "Restore từ backup ngày: $RESTORE_DATE"
[[ -n "$DB_FILE" ]]      && log_info "  DB file     : $(basename "$DB_FILE")"
[[ -n "$UPLOADS_FILE" ]] && log_info "  Uploads file: $(basename "$UPLOADS_FILE")"
log "══════════════════════════════════════════════"

# Cảnh báo và xác nhận
log_warn "CẢNH BÁO: Thao tác này sẽ GHI ĐÈ dữ liệu hiện tại!"
echo -n "Bạn có chắc muốn tiếp tục? (yes/N): "
read -r CONFIRM
if [[ "$CONFIRM" != "yes" ]]; then
  log "Hủy restore."
  exit 0
fi

# Kiểm tra container đang chạy
if ! docker inspect "$DB_CONTAINER" --format '{{.State.Running}}' 2>/dev/null | grep -q true; then
  log_err "Container $DB_CONTAINER không chạy"
  exit 1
fi

ERRORS=0

# ── 1. Restore Database ───────────────────────────────────────────────────────
if [[ "$UPLOADS_ONLY" == false ]]; then
  if [[ -z "$DB_FILE" ]]; then
    log_err "Không tìm thấy file DB backup trong $BACKUP_DATE_DIR"
    ERRORS=$((ERRORS + 1))
  else
    log "Đang restore database từ $(basename "$DB_FILE")..."
    # Drop & recreate DB
    if docker exec "$DB_CONTAINER" psql -U "$DB_USER" -c "DROP DATABASE IF EXISTS \"${DB_NAME}\";" postgres 2>/dev/null && \
       docker exec "$DB_CONTAINER" psql -U "$DB_USER" -c "CREATE DATABASE \"${DB_NAME}\";" postgres 2>/dev/null && \
       docker exec -i "$DB_CONTAINER" pg_restore -U "$DB_USER" -d "$DB_NAME" --no-owner --role="$DB_USER" < "$DB_FILE"; then
      log_ok "Database restore thành công"
    else
      log_err "Restore database thất bại"
      ERRORS=$((ERRORS + 1))
    fi
  fi
fi

# ── 2. Restore Uploads ────────────────────────────────────────────────────────
if [[ "$DB_ONLY" == false ]]; then
  if [[ -z "$UPLOADS_FILE" ]]; then
    log_err "Không tìm thấy file uploads backup trong $BACKUP_DATE_DIR"
    ERRORS=$((ERRORS + 1))
  else
    log "Đang restore uploads từ $(basename "$UPLOADS_FILE")..."
    UPLOADS_BASENAME=$(basename "$UPLOADS_FILE")
    if docker run --rm \
        -v "${UPLOADS_VOLUME}:/data" \
        -v "${BACKUP_DATE_DIR}:/backup:ro" \
        alpine \
        sh -c "rm -rf /data/* /data/.[!.]* 2>/dev/null; tar xzf /backup/${UPLOADS_BASENAME} -C /data" 2>/dev/null; then
      log_ok "Uploads restore thành công"
    else
      log_err "Restore uploads thất bại"
      ERRORS=$((ERRORS + 1))
    fi
  fi
fi

# ── Kết quả ───────────────────────────────────────────────────────────────────
log "──────────────────────────────────────────────"
if [[ $ERRORS -eq 0 ]]; then
  log_ok "Restore hoàn thành từ backup $RESTORE_DATE"
  log_warn "Khởi động lại API để áp dụng migration nếu cần: docker restart coffee-api-prod"
  exit 0
else
  log_err "Restore hoàn thành với $ERRORS lỗi"
  exit 1
fi
