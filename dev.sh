#!/bin/bash
set -e

REPO_DIR="$(cd "$(dirname "$0")" && pwd)"
COMPOSE_CMD="docker compose -p cafe-dev --env-file .env -f docker-compose.dev.yml"

cd "$REPO_DIR"

case "${1:-up}" in
  up)
    echo "==> Starting dev services..."
    $COMPOSE_CMD up -d --build
    echo "==> Done! Services are running:"
    $COMPOSE_CMD ps
    echo ""
    echo "==> Seq UI: http://localhost:5341"
    ;;
  down)
    echo "==> Stopping dev services..."
    $COMPOSE_CMD down
    ;;
  restart)
    echo "==> Restarting dev services..."
    $COMPOSE_CMD down
    $COMPOSE_CMD up -d --build
    echo "==> Done! Services are running:"
    $COMPOSE_CMD ps
    echo ""
    echo "==> Seq UI: http://localhost:5341"
    ;;
  logs)
    $COMPOSE_CMD logs -f "${2:-}"
    ;;
  ps)
    $COMPOSE_CMD ps
    ;;
  seq)
    xdg-open "http://localhost:5341" 2>/dev/null || open "http://localhost:5341" 2>/dev/null || echo "==> Seq UI: http://localhost:5341"
    ;;
  *)
    echo "Usage: $0 [up|down|restart|logs [service]|ps|seq]"
    exit 1
    ;;
esac
