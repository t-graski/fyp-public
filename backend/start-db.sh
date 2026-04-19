#!/usr/bin/env bash
set -euo pipefail

PROJECT_PATH="${1:-./backend/backend.csproj}"

echo "Starting Postgres (docker compose)..."
docker compose up -d

echo "Waiting for Postgres to become healthy..."
for i in {1..40}; do
  status="$(docker inspect -f '{{.State.Health.Status}}' core_postgres_db 2>/dev/null || true)"
  if [[ "$status" == "healthy" ]]; then
    break
  fi
  sleep 2
done

status="$(docker inspect -f '{{.State.Health.Status}}' core_postgres_db 2>/dev/null || true)"
if [[ "$status" != "healthy" ]]; then
  echo "Postgres did not become healthy. Logs:"
  docker logs core_postgres_db --tail 120
  exit 1
fi

echo "Running EF migrations (if any)..."
dotnet ef database update --project "$PROJECT_PATH" || true
