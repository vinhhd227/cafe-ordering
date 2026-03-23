#!/bin/bash
set -e

REPO_DIR="$(cd "$(dirname "$0")" && pwd)"
COMPOSE_CMD="docker compose -p cafe-dev -f docker-compose.dev.yml -f docker-compose.dev.override.yml"

cd "$REPO_DIR"

case "${1:-up}" in
  up)
    echo "==> Starting dev services..."
    $COMPOSE_CMD up -d --build
    echo "==> Done! Services are running:"
    $COMPOSE_CMD ps
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
    ;;
  logs)
    $COMPOSE_CMD logs -f "${2:-}"
    ;;
  ps)
    $COMPOSE_CMD ps
    ;;
  *)
    echo "Usage: $0 [up|down|restart|logs [service]|ps]"
    exit 1
    ;;
esac
