#!/usr/bin/env bash
set -euo pipefail

HOST="${1:-localhost}"
PORT="${2:-5432}"
DATABASE="${3:-toir_db}"
USER_NAME="${4:-toir_admin}"
OUTPUT_DIR="${5:-backups}"

mkdir -p "$OUTPUT_DIR"

TIMESTAMP="$(date +%Y%m%d_%H%M%S)"
FILE_NAME="${DATABASE}_${TIMESTAMP}.dump"
OUTPUT_PATH="${OUTPUT_DIR}/${FILE_NAME}"

echo "Creating backup for database '${DATABASE}'..."

if command -v pg_dump >/dev/null 2>&1; then
  if [ -z "${PGPASSWORD:-}" ]; then
    echo "PGPASSWORD is not set. Set it before running the script if password authentication is required."
  fi
  pg_dump -h "$HOST" -p "$PORT" -U "$USER_NAME" -d "$DATABASE" -F c -f "$OUTPUT_PATH"
else
  echo "Local pg_dump was not found. Using pg_dump inside Docker container."
  docker compose exec -T postgres pg_dump -U "$USER_NAME" -d "$DATABASE" -F c -f "/backups/${FILE_NAME}"
fi

if [ ! -s "$OUTPUT_PATH" ]; then
  echo "Backup file was not created or is empty: $OUTPUT_PATH" >&2
  exit 1
fi

echo "Backup created: $OUTPUT_PATH"
echo "Backup size: $(wc -c < "$OUTPUT_PATH") bytes"
