#!/bin/bash
set -e

REPO_DIR="$(cd "$(dirname "$0")" && pwd)"
ENV_FILE="$REPO_DIR/.env.prod"
DEPLOY_BACKUP_DIR="${DEPLOY_BACKUP_DIR:-$HOME/cafe-backups/deploy}"
KEEP_DEPLOY_BACKUPS=10
DB_CONTAINER="${DB_CONTAINER:-cafe-db-prod}"

PRINTER_OVERRIDE=""
if [ -e /dev/usb/lp0 ]; then
  echo "==> USB printer detected at /dev/usb/lp0, enabling device access..."
  PRINTER_OVERRIDE="-f $REPO_DIR/docker-compose.printer.yml"
fi

COMPOSE_CMD="docker compose -p cafe-prod --env-file $ENV_FILE -f $REPO_DIR/docker-compose.prod.yml $PRINTER_OVERRIDE"

GREEN='\033[0;32m'; YELLOW='\033[1;33m'; RED='\033[0;31m'; NC='\033[0m'
log()      { echo -e "[$(date '+%Y-%m-%d %H:%M:%S')] $*"; }
log_ok()   { log "${GREEN}✔${NC} $*"; }
log_warn() { log "${YELLOW}!${NC} $*"; }
log_err()  { log "${RED}✖${NC} $*"; }

if [ ! -f "$ENV_FILE" ]; then
  echo "ERROR: $ENV_FILE not found. Create it from .env.prod.example first."
  exit 1
fi

DB_USER=$(grep -E '^DB_USER=' "$ENV_FILE" | cut -d= -f2)
DB_NAME=$(grep -E '^DB_NAME=' "$ENV_FILE" | cut -d= -f2)

# ── 1. Capture commit hiện tại (trước khi pull) ──────────────────────────────
PREV_COMMIT=$(git -C "$REPO_DIR" rev-parse HEAD)
TIMESTAMP=$(date '+%Y%m%d_%H%M%S')
TAG_NAME="deploy/$(date '+%Y%m%d-%H%M')"

log "══════════════════════════════════════════════"
log "Deploy bắt đầu — $TIMESTAMP"
log "  Commit hiện tại: ${PREV_COMMIT:0:7}"
log "══════════════════════════════════════════════"

# ── 2. Backup DB trước khi deploy ────────────────────────────────────────────
mkdir -p "$DEPLOY_BACKUP_DIR"
BACKUP_FILE="$DEPLOY_BACKUP_DIR/db_${TIMESTAMP}.dump"

log "==> Đang backup database..."
if docker inspect "$DB_CONTAINER" --format '{{.State.Running}}' 2>/dev/null | grep -q true; then
  if docker exec "$DB_CONTAINER" pg_dump -U "$DB_USER" "$DB_NAME" -F c > "$BACKUP_FILE"; then
    BACKUP_SIZE=$(du -sh "$BACKUP_FILE" | cut -f1)
    log_ok "Backup: $(basename "$BACKUP_FILE") ($BACKUP_SIZE)"
  else
    log_err "Backup thất bại — hủy deploy"
    rm -f "$BACKUP_FILE"
    exit 1
  fi
else
  log_warn "Container DB không chạy — bỏ qua backup (first deploy?)"
  BACKUP_FILE=""
fi

# ── 3. Pull code mới ─────────────────────────────────────────────────────────
log "==> Pulling latest code from origin/main..."
cd "$REPO_DIR"
git pull origin main

NEW_COMMIT=$(git rev-parse HEAD)
log_ok "Code: ${PREV_COMMIT:0:7} → ${NEW_COMMIT:0:7}"

# ── 4. Build và khởi động lại ────────────────────────────────────────────────
log "==> Building and restarting services..."
if ! $COMPOSE_CMD up -d --build; then
  log_err "Build thất bại!"
  if [[ -n "$BACKUP_FILE" ]]; then
    log_warn "Để rollback thủ công:"
    log_warn "  bash $REPO_DIR/scripts/rollback.sh --commit $PREV_COMMIT --backup $BACKUP_FILE"
  fi
  exit 1
fi

# ── 5. Xóa Docker images cũ ──────────────────────────────────────────────────
log "==> Removing unused Docker images..."
docker image prune -f

# ── 6. Tạo git tag kèm thông tin backup + prev commit ────────────────────────
TAG_MSG="prev: $PREV_COMMIT"
[[ -n "$BACKUP_FILE" ]] && TAG_MSG="$TAG_MSG
backup: $BACKUP_FILE"

git tag -a "$TAG_NAME" -m "$TAG_MSG"
log_ok "Git tag: $TAG_NAME"
git push origin "$TAG_NAME" 2>/dev/null || log_warn "Không push được tag lên remote (OK nếu chạy offline)"

# ── 7. Xóa deploy backup cũ (giữ KEEP_DEPLOY_BACKUPS bản) ───────────────────
BACKUP_COUNT=$(find "$DEPLOY_BACKUP_DIR" -name "db_*.dump" | wc -l)
if (( BACKUP_COUNT > KEEP_DEPLOY_BACKUPS )); then
  find "$DEPLOY_BACKUP_DIR" -name "db_*.dump" | sort | head -$(( BACKUP_COUNT - KEEP_DEPLOY_BACKUPS )) | xargs rm -f
  log "  Đã xóa backup deploy cũ (giữ $KEEP_DEPLOY_BACKUPS bản)"
fi

log "══════════════════════════════════════════════"
log_ok "Deploy hoàn thành!"
log "  Tag   : $TAG_NAME"
[[ -n "$BACKUP_FILE" ]] && log "  Backup: $(basename "$BACKUP_FILE")"
log "  Rollback: bash $REPO_DIR/scripts/rollback.sh $TAG_NAME"
log "══════════════════════════════════════════════"
$COMPOSE_CMD ps
