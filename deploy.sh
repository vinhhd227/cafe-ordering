#!/bin/bash
set -e

REPO_DIR="$(cd "$(dirname "$0")" && pwd)"
COMPOSE_CMD="docker compose -p cafe-prod -f docker-compose.prod.yml -f docker-compose.prod.override.yml"

echo "==> Pulling latest code from origin/main..."
cd "$REPO_DIR"
git pull origin main

echo "==> Building and restarting services..."
$COMPOSE_CMD up -d --build

echo "==> Removing unused Docker images..."
docker image prune -f

echo "==> Done! Services are running:"
$COMPOSE_CMD ps
