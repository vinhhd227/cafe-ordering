#!/bin/bash
set -e

REPO_DIR="$(cd "$(dirname "$0")" && pwd)"
ENV_FILE="$REPO_DIR/.env.prod"
COMPOSE_CMD="docker compose -p cafe-prod --env-file $ENV_FILE -f $REPO_DIR/docker-compose.prod.yml"

# Kiểm tra .env.prod tồn tại
if [ ! -f "$ENV_FILE" ]; then
  echo "ERROR: $ENV_FILE not found. Create it from .env.prod.example first."
  exit 1
fi

echo "==> Pulling latest code from origin/main..."
cd "$REPO_DIR"
git pull origin main

echo "==> Building and restarting services..."
$COMPOSE_CMD up -d --build

echo "==> Removing unused Docker images..."
docker image prune -f

echo "==> Done! Services are running:"
$COMPOSE_CMD ps
