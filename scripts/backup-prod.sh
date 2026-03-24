#!/usr/bin/env bash
# backup-prod.sh — Backup PostgreSQL DB + uploads volume cho production
#
# Cách dùng:
#   bash scripts/backup-prod.sh
#
# Cài cron job (chạy lúc 3 giờ sáng mỗi ngày):
#   crontab -e
#   0 3 * * * /absolute/path/to/cafe-ordering/scripts/backup-prod.sh >> /absolute/path/to/cafe-ordering/scripts/backup-cron.log 2>&1

set -euo pipefail

# ── Cấu hình ──────────────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
ENV_FILE="$PROJECT_DIR/.env.prod"

BACKUP_DIR="${BACKUP_DIR:-$HOME/cafe-backups}"
KEEP_DAYS="${KEEP_DAYS:-7}"
DB_CONTAINER="${DB_CONTAINER:-cafe-db-prod}"
UPLOADS_VOLUME="${UPLOADS_VOLUME:-cafe-uploads}"
# ──────────────────────────────────────────────────────────────────────────────

# Màu cho terminal
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; NC='\033[0m'

log() { echo -e "[$(date '+%Y-%m-%d %H:%M:%S')] $*"; }
log_ok()   { log "${GREEN}✔${NC} $*"; }
log_err()  { log "${RED}✖${NC} $*"; }
log_warn() { log "${YELLOW}!${NC} $*"; }

# Đọc biến từ .env.prod
if [[ ! -f "$ENV_FILE" ]]; then
  log_err ".env.prod không tìm thấy tại $ENV_FILE"
  exit 1
fi

# Chỉ load DB_USER và DB_NAME, không expose toàn bộ env
DB_USER=$(grep -E '^DB_USER=' "$ENV_FILE" | cut -d= -f2)
DB_NAME=$(grep -E '^DB_NAME=' "$ENV_FILE" | cut -d= -f2)

if [[ -z "$DB_USER" || -z "$DB_NAME" ]]; then
  log_err "DB_USER hoặc DB_NAME không tìm thấy trong $ENV_FILE"
  exit 1
fi

# Kiểm tra Docker container đang chạy
if ! docker inspect "$DB_CONTAINER" --format '{{.State.Running}}' 2>/dev/null | grep -q true; then
  log_err "Container $DB_CONTAINER không chạy"
  exit 1
fi

# Tạo thư mục backup
DATE=$(date '+%Y-%m-%d')
TIMESTAMP=$(date '+%Y%m%d_%H%M%S')
BACKUP_DATE_DIR="$BACKUP_DIR/$DATE"
mkdir -p "$BACKUP_DATE_DIR"

LOG_FILE="$BACKUP_DIR/backup.log"
# Redirect output vào log file (append) đồng thời vẫn hiện trên terminal
exec > >(tee -a "$LOG_FILE") 2>&1

log "══════════════════════════════════════════════"
log "Bắt đầu backup — $TIMESTAMP"
log "  DB container : $DB_CONTAINER ($DB_USER@$DB_NAME)"
log "  Uploads volume: $UPLOADS_VOLUME"
log "  Lưu tại      : $BACKUP_DATE_DIR"
log "══════════════════════════════════════════════"

ERRORS=0

# ── 1. Backup Database ────────────────────────────────────────────────────────
DB_FILE="$BACKUP_DATE_DIR/db_${TIMESTAMP}.dump"
log "Đang dump database..."
if docker exec "$DB_CONTAINER" pg_dump -U "$DB_USER" "$DB_NAME" -F c > "$DB_FILE"; then
  DB_SIZE=$(du -sh "$DB_FILE" | cut -f1)
  log_ok "DB backup: $(basename "$DB_FILE") ($DB_SIZE)"
else
  log_err "Dump database thất bại"
  rm -f "$DB_FILE"
  ERRORS=$((ERRORS + 1))
fi

# ── 2. Backup Uploads Volume ──────────────────────────────────────────────────
UPLOADS_FILE="$BACKUP_DATE_DIR/uploads_${TIMESTAMP}.tar.gz"
log "Đang backup uploads volume..."
if docker run --rm \
    -v "${UPLOADS_VOLUME}:/data:ro" \
    -v "${BACKUP_DATE_DIR}:/backup" \
    alpine \
    tar czf "/backup/$(basename "$UPLOADS_FILE")" -C /data . 2>/dev/null; then
  UPLOADS_SIZE=$(du -sh "$UPLOADS_FILE" | cut -f1)
  log_ok "Uploads backup: $(basename "$UPLOADS_FILE") ($UPLOADS_SIZE)"
else
  log_err "Backup uploads thất bại"
  rm -f "$UPLOADS_FILE"
  ERRORS=$((ERRORS + 1))
fi

# ── 3. Xóa backup cũ ─────────────────────────────────────────────────────────
log "Xóa backup cũ hơn $KEEP_DAYS ngày..."
DELETED=0
while IFS= read -r -d '' dir; do
  rm -rf "$dir"
  log_warn "Đã xóa: $(basename "$dir")"
  DELETED=$((DELETED + 1))
done < <(find "$BACKUP_DIR" -maxdepth 1 -mindepth 1 -type d -mtime +"$KEEP_DAYS" -print0)
[[ $DELETED -eq 0 ]] && log "  Không có backup nào cần xóa"

# ── 4. Kết quả ───────────────────────────────────────────────────────────────
TOTAL_SIZE=$(du -sh "$BACKUP_DATE_DIR" | cut -f1)
log "──────────────────────────────────────────────"
if [[ $ERRORS -eq 0 ]]; then
  log_ok "Backup hoàn thành — tổng $TOTAL_SIZE tại $BACKUP_DATE_DIR"
  exit 0
else
  log_err "Backup hoàn thành với $ERRORS lỗi — kiểm tra log tại $LOG_FILE"
  exit 1
fi
