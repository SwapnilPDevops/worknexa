#!/usr/bin/env sh
# Back up a running Worknexa stack: database (pg_dump custom format), attachments and Data Protection keys.
#   docker/scripts/backup.sh docker/.env.production [backup-root]
# Schedule it (cron/systemd timer) and copy the output off the host. The keys archive decrypts stored
# integration tokens: keep backups encrypted and access-restricted.
set -eu

ENV_FILE=${1:?usage: backup.sh <env-file> [backup-root]}
BACKUP_ROOT=${2:-backups}
RETENTION_DAYS=${RETENTION_DAYS:-14}
COMPOSE_FILE=$(dirname "$0")/../docker-compose.prod.yml
STAMP=$(date -u +%Y%m%dT%H%M%SZ)
TARGET="$BACKUP_ROOT/$STAMP"

compose() { docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" "$@"; }

umask 077
mkdir -p "$TARGET"

compose exec -T db sh -c 'pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" --format=custom --no-owner' > "$TARGET/database.dump"

API_CONTAINER=$(compose ps -a -q api)
[ -n "$API_CONTAINER" ] || { echo "api container not found" >&2; exit 1; }
docker run --rm --volumes-from "$API_CONTAINER" alpine:3 tar czf - -C /data attachments keys > "$TARGET/files.tar.gz"

compose images api --format json > "$TARGET/images.json" 2>/dev/null || true

# Verify the dump is readable before trusting it.
compose exec -T db pg_restore --list < "$TARGET/database.dump" > /dev/null

find "$BACKUP_ROOT" -mindepth 1 -maxdepth 1 -type d -mtime +"$RETENTION_DAYS" -exec rm -rf {} +
echo "Backup written to $TARGET"
