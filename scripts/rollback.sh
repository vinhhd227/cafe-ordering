#!/usr/bin/env bash
# rollback.sh — Rollback về trạng thái trước một deploy
#
# Cách dùng:
#   bash scripts/rollback.sh                           # liệt kê deploy tags
#   bash scripts/rollback.sh deploy/20260526-1430      # rollback về tag cụ thể
#   bash scripts/rollback.sh --commit abc1234 --backup /path/db.dump  # thủ công

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
ENV_FILE="$PROJECT_DIR/.env.prod"
DB_CONTAINER="${DB_CONTAINER:-cafe-db-prod}"

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; CYAN='\033[0;36m'; NC='\033[0m'
log()      { echo -e "[$(date '+%Y-%m-%d %H:%M:%S')] $*"; }
log_ok()   { log "${GREEN}✔${NC} $*"; }
log_err()  { log "${RED}✖${NC} $*"; }
log_warn() { log "${YELLOW}!${NC} $*"; }
log_info() { log "${CYAN}ℹ${NC} $*"; }

PRINTER_OVERRIDE=""
if [ -e /dev/usb/lp0 ]; then
  PRINTER_OVERRIDE="-f $PROJECT_DIR/docker-compose.printer.yml"
fi
COMPOSE_CMD="docker compose -p cafe-prod --env-file $ENV_FILE -f $PROJECT_DIR/docker-compose.prod.yml $PRINTER_OVERRIDE"

if [[ ! -f "$ENV_FILE" ]]; then
  log_err ".env.prod không tìm thấy tại $ENV_FILE"
  exit 1
fi

DB_USER=$(grep -E '^DB_USER=' "$ENV_FILE" | cut -d= -f2)
DB_NAME=$(grep -E '^DB_NAME=' "$ENV_FILE" | cut -d= -f2)

# ── Parse args ────────────────────────────────────────────────────────────────
TAG=""
MANUAL_COMMIT=""
MANUAL_BACKUP=""

while [[ $# -gt 0 ]]; do
  case $1 in
    --commit) MANUAL_COMMIT="$2"; shift 2 ;;
    --backup) MANUAL_BACKUP="$2"; shift 2 ;;
    deploy/*) TAG="$1"; shift ;;
    *) log_err "Tham số không hợp lệ: $1"; echo "Dùng: $0 [deploy/<tag>] [--commit <sha> --backup <file>]"; exit 1 ;;
  esac
done

# ── List mode (không có arg) ─────────────────────────────────────────────────
if [[ -z "$TAG" && -z "$MANUAL_COMMIT" ]]; then
  echo ""
  log_info "Các deploy gần đây (mới nhất trước):"
  echo ""
  git -C "$PROJECT_DIR" tag -l "deploy/*" --sort=-version:refname | head -20 | while read -r t; do
    PREV=$(git -C "$PROJECT_DIR" for-each-ref "refs/tags/$t" --format='%(contents)' 2>/dev/null | grep '^prev:' | awk '{print $2}' | cut -c1-7)
    BKUP=$(git -C "$PROJECT_DIR" for-each-ref "refs/tags/$t" --format='%(contents)' 2>/dev/null | grep '^backup:' | awk '{print $2}' | xargs basename 2>/dev/null || echo "(none)")
    printf "  %-30s  prev=%-7s  backup=%s\n" "$t" "$PREV" "$BKUP"
  done
  echo ""
  log_info "Dùng: bash scripts/rollback.sh <tag>"
  exit 0
fi

# ── Xác định target commit và backup ─────────────────────────────────────────
if [[ -n "$MANUAL_COMMIT" ]]; then
  TARGET_COMMIT="$MANUAL_COMMIT"
  BACKUP_FILE="$MANUAL_BACKUP"
else
  TAG_CONTENTS=$(git -C "$PROJECT_DIR" for-each-ref "refs/tags/$TAG" --format='%(contents)' 2>/dev/null)
  TARGET_COMMIT=$(echo "$TAG_CONTENTS" | grep '^prev:' | awk '{print $2}')
  BACKUP_FILE=$(echo "$TAG_CONTENTS" | grep '^backup:' | awk '{print $2}')

  if [[ -z "$TARGET_COMMIT" ]]; then
    log_err "Không đọc được prev commit từ tag $TAG"
    log_err "Thử dùng: bash scripts/rollback.sh --commit <sha> --backup <file>"
    exit 1
  fi
fi

# ── Hiển thị kế hoạch ────────────────────────────────────────────────────────
echo ""
log "══════════════════════════════════════════════"
log_warn "KẾ HOẠCH ROLLBACK"
log "  Code target : ${TARGET_COMMIT:0:7}"
if [[ -n "$BACKUP_FILE" ]]; then
  log "  DB backup   : $(basename "$BACKUP_FILE")"
else
  log_warn "  DB backup   : (không có — schema GIỮ NGUYÊN)"
fi
log "══════════════════════════════════════════════"
echo ""
log_warn "Thao tác này sẽ:"
echo "  1. Stop và rebuild tất cả containers"
[[ -n "$BACKUP_FILE" ]] && echo "  2. Restore database về trạng thái pre-deploy"
echo "  3. Checkout code về commit ${TARGET_COMMIT:0:7}"
echo ""
echo -n "Tiếp tục? (yes/N): "
read -r CONFIRM
[[ "$CONFIRM" != "yes" ]] && { log "Hủy rollback."; exit 0; }

# ── 1. Restore DB ─────────────────────────────────────────────────────────────
if [[ -n "$BACKUP_FILE" ]]; then
  if [[ ! -f "$BACKUP_FILE" ]]; then
    log_err "File backup không tồn tại: $BACKUP_FILE"
    exit 1
  fi

  if ! docker inspect "$DB_CONTAINER" --format '{{.State.Running}}' 2>/dev/null | grep -q true; then
    log_err "Container $DB_CONTAINER không chạy"
    exit 1
  fi

  log "==> Đang restore database..."
  if docker exec "$DB_CONTAINER" psql -U "$DB_USER" -c "DROP DATABASE IF EXISTS \"${DB_NAME}\";" postgres 2>/dev/null && \
     docker exec "$DB_CONTAINER" psql -U "$DB_USER" -c "CREATE DATABASE \"${DB_NAME}\";" postgres 2>/dev/null && \
     docker exec -i "$DB_CONTAINER" pg_restore -U "$DB_USER" -d "$DB_NAME" --no-owner --role="$DB_USER" < "$BACKUP_FILE"; then
    log_ok "Database restore thành công"
  else
    log_err "Restore database thất bại — dừng rollback để tránh inconsistent state"
    exit 1
  fi
fi

# ── 2. Checkout code cũ ───────────────────────────────────────────────────────
log "==> Checkout code về ${TARGET_COMMIT:0:7}..."
git -C "$PROJECT_DIR" checkout "$TARGET_COMMIT"
log_ok "Code đã checkout về ${TARGET_COMMIT:0:7}"

# ── 3. Rebuild ────────────────────────────────────────────────────────────────
log "==> Rebuilding services..."
$COMPOSE_CMD up -d --build

log "══════════════════════════════════════════════"
log_ok "Rollback hoàn thành!"
log_warn "Đang ở detached HEAD tại ${TARGET_COMMIT:0:7}"
log_warn "Sau khi xác nhận OK: git -C $PROJECT_DIR checkout main"
log "══════════════════════════════════════════════"
$COMPOSE_CMD ps
