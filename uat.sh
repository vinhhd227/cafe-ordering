#!/bin/bash
set -e

REPO_DIR="$(cd "$(dirname "$0")" && pwd)"
COMPOSE_CMD="docker compose -p cafe-uat --env-file .env.uat -f docker-compose.uat.yml"

cd "$REPO_DIR"

case "${1:-up}" in
  up)
    echo "==> Starting UAT services..."
    $COMPOSE_CMD up -d --build
    echo "==> Done! Services are running:"
    $COMPOSE_CMD ps
    ;;
  down)
    echo "==> Stopping UAT services..."
    $COMPOSE_CMD down
    ;;
  restart)
    echo "==> Restarting UAT services..."
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
