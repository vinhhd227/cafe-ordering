$COMPOSE_CMD = "docker compose -p cafe-dev --env-file .env -f docker-compose.dev.yml"
$action = if ($args.Count -gt 0) { $args[0] } else { "up" }

switch ($action) {
  "up" {
    Write-Host "==> Starting dev services..."
    Invoke-Expression "$COMPOSE_CMD up -d --build"
    Write-Host "==> Done! Services are running:"
    Invoke-Expression "$COMPOSE_CMD ps"
    Write-Host ""
    Write-Host "==> Seq UI: http://localhost:5341"
  }
  "down" {
    Write-Host "==> Stopping dev services..."
    Invoke-Expression "$COMPOSE_CMD down"
  }
  "restart" {
    Write-Host "==> Restarting dev services..."
    Invoke-Expression "$COMPOSE_CMD down"
    Invoke-Expression "$COMPOSE_CMD up -d --build"
    Write-Host "==> Done! Services are running:"
    Invoke-Expression "$COMPOSE_CMD ps"
    Write-Host ""
    Write-Host "==> Seq UI: http://localhost:5341"
  }
  "logs" {
    $service = if ($args.Count -gt 1) { $args[1] } else { "" }
    Invoke-Expression "$COMPOSE_CMD logs -f $service"
  }
  "ps" {
    Invoke-Expression "$COMPOSE_CMD ps"
  }
  "seq" {
    Start-Process "http://localhost:5341"
  }
  default {
    Write-Host "Usage: .\dev.ps1 [up|down|restart|logs [service]|ps|seq]"
    exit 1
  }
}
