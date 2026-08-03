#!/usr/bin/env bash
set -euo pipefail

MAX_ATTEMPTS="${1:-60}"
POSTGRES_USER="${POSTGRES_USER:-loremetry}"
POSTGRES_DB="${POSTGRES_DB:-loremetry}"

for ((attempt = 1; attempt <= MAX_ATTEMPTS; attempt++)); do
  if pg_isready -h localhost -p 5432 -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" >/dev/null 2>&1; then
    echo "Postgres is ready."
    exit 0
  fi

  if pg_isready -h localhost -p 5432 >/dev/null 2>&1; then
    echo "Postgres is ready."
    exit 0
  fi

  echo "Waiting for Postgres... (${attempt}/${MAX_ATTEMPTS})"
  sleep 1
done

echo "ERROR: Postgres did not become ready within ${MAX_ATTEMPTS} seconds."
exit 1
