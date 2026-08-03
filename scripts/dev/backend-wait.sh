#!/usr/bin/env bash
set -euo pipefail

MAX_ATTEMPTS="${1:-120}"
BACKEND_URL="${BACKEND_URL:-http://localhost:5092/health}"

for ((attempt = 1; attempt <= MAX_ATTEMPTS; attempt++)); do
  if curl -s -o /dev/null "${BACKEND_URL}" 2>/dev/null; then
    echo "Backend is ready at ${BACKEND_URL}"
    exit 0
  fi

  echo "Waiting for Backend... (${attempt}/${MAX_ATTEMPTS})"
  sleep 1
done

echo "ERROR: Backend did not become ready within ${MAX_ATTEMPTS} seconds."
echo "Expected a response from ${BACKEND_URL}"
exit 1
