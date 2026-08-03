#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

POSTGRES_USER="${POSTGRES_USER:-loremetry}"
POSTGRES_DB="${POSTGRES_DB:-loremetry}"
POSTGRES_PASSWORD="${POSTGRES_PASSWORD:-loremetry}"

start_with_docker() {
  echo "Starting Postgres with Docker Compose..."
  docker compose up -d
}

start_with_brew() {
  echo "Docker not available. Starting Homebrew PostgreSQL..."

  local pg_formula=""
  for version in 17 16 15 14; do
    if brew list "postgresql@${version}" >/dev/null 2>&1; then
      pg_formula="postgresql@${version}"
      break
    fi
  done

  if [[ -z "$pg_formula" ]]; then
    echo "ERROR: No Postgres installation found."
    echo "Install Docker Desktop, or run: brew install postgresql@16"
    exit 1
  fi

  local pg_prefix
  pg_prefix="$(brew --prefix "$pg_formula")"
  local pg_data="$(dirname "${pg_prefix}")/var/${pg_formula}"

  export PATH="${pg_prefix}/bin:${PATH}"

  if pg_isready -h localhost -p 5432 >/dev/null 2>&1; then
    echo "Postgres is already running."
  elif brew services start "$pg_formula" 2>/dev/null; then
    echo "Started Postgres via brew services."
  else
    echo "brew services failed; trying pg_ctl..."
    if [[ -d "$pg_data" ]]; then
      pg_ctl -D "$pg_data" -l "${pg_data}/server.log" start
    else
      echo "ERROR: Postgres data directory not found at ${pg_data}"
      exit 1
    fi
  fi

  echo "Ensuring database user and database exist..."
  sleep 2

  psql postgres -v ON_ERROR_STOP=0 -c "DO \$\$ BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '${POSTGRES_USER}') THEN
      CREATE ROLE ${POSTGRES_USER} WITH LOGIN SUPERUSER PASSWORD '${POSTGRES_PASSWORD}';
    END IF;
  END \$\$;" >/dev/null 2>&1 || true

  psql postgres -v ON_ERROR_STOP=0 -c "ALTER USER ${POSTGRES_USER} WITH PASSWORD '${POSTGRES_PASSWORD}';" >/dev/null 2>&1 || true
  createdb -O "${POSTGRES_USER}" "${POSTGRES_DB}" 2>/dev/null || true
}

if command -v docker >/dev/null 2>&1 && docker info >/dev/null 2>&1; then
  start_with_docker
else
  start_with_brew
fi

echo "Postgres start command completed."
