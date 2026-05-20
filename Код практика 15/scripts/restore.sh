#!/usr/bin/env bash
set -euo pipefail

if [ $# -lt 1 ]; then
  echo "Usage: $0 path/to/backup.dump [host] [port] [source_database] [restore_database] [user]" >&2
  exit 1
fi

DUMP_PATH="$1"
HOST="${2:-localhost}"
PORT="${3:-5432}"
SOURCE_DATABASE="${4:-toir_db}"
RESTORE_DATABASE="${5:-toir_restore_check}"
USER_NAME="${6:-toir_admin}"

if [ ! -f "$DUMP_PATH" ]; then
  echo "Dump file was not found: $DUMP_PATH" >&2
  exit 1
fi

DUMP_FILE_NAME="$(basename "$DUMP_PATH")"

echo "Restoring '$DUMP_PATH' into separate database '$RESTORE_DATABASE'."
echo "The main database '$SOURCE_DATABASE' is not modified."

if command -v psql >/dev/null 2>&1 && command -v pg_restore >/dev/null 2>&1; then
  if [ -z "${PGPASSWORD:-}" ]; then
    echo "PGPASSWORD is not set. Set it before running the script if password authentication is required."
  fi
  EXISTS="$(psql -h "$HOST" -p "$PORT" -U "$USER_NAME" -d postgres -tAc "select 1 from pg_database where datname = '${RESTORE_DATABASE}'" | tr -d '[:space:]')"
  if [ "$EXISTS" != "1" ]; then
    psql -h "$HOST" -p "$PORT" -U "$USER_NAME" -d postgres -v ON_ERROR_STOP=1 -c "create database ${RESTORE_DATABASE}"
  fi
  pg_restore -h "$HOST" -p "$PORT" -U "$USER_NAME" -d "$RESTORE_DATABASE" --clean --if-exists "$DUMP_PATH"
else
  echo "Local psql/pg_restore were not found. Using PostgreSQL tools inside Docker container."
  EXISTS="$(docker compose exec -T postgres psql -U "$USER_NAME" -d postgres -tAc "select 1 from pg_database where datname = '${RESTORE_DATABASE}'" | tr -d '[:space:]')"
  if [ "$EXISTS" != "1" ]; then
    docker compose exec -T postgres psql -U "$USER_NAME" -d postgres -v ON_ERROR_STOP=1 -c "create database ${RESTORE_DATABASE}"
  fi
  docker compose exec -T postgres pg_restore -U "$USER_NAME" -d "$RESTORE_DATABASE" --clean --if-exists "/backups/${DUMP_FILE_NAME}"
fi

CHECKS="
select 'equipment_count' as check_name, count(*)::text as check_value from toir.equipment
union all
select 'maintenance_requests_count', count(*)::text from toir.maintenance_requests
union all
select 'work_orders_count', count(*)::text from toir.work_orders
union all
select 'work_orders_without_request', count(*)::text
from toir.work_orders wo
left join toir.maintenance_requests mr on mr.id = wo.request_id
where mr.id is null
union all
select 'requests_without_equipment', count(*)::text
from toir.maintenance_requests mr
left join toir.equipment e on e.id = mr.equipment_id
where e.id is null;
"

echo "Running restore checks..."

if command -v psql >/dev/null 2>&1; then
  psql -h "$HOST" -p "$PORT" -U "$USER_NAME" -d "$RESTORE_DATABASE" -v ON_ERROR_STOP=1 -c "$CHECKS"
else
  docker compose exec -T postgres psql -U "$USER_NAME" -d "$RESTORE_DATABASE" -v ON_ERROR_STOP=1 -c "$CHECKS"
fi
