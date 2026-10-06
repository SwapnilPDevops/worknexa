#!/usr/bin/env sh
# Restore a backup made by backup.sh into a stack. Overwrites the database, attachments and keys.
#   docker/scripts/restore.sh docker/.env.production backups/20261006T020000Z
set -eu

ENV_FILE=${1:?usage: restore.sh <env-file> <backup-dir>}
SOURCE=${2:?usage: restore.sh <env-file> <backup-dir>}
COMPOSE_FILE=$(dirname "$0")/../docker-compose.prod.yml

compose() { docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" "$@"; }

[ -f "$SOURCE/database.dump" ] && [ -f "$SOURCE/files.tar.gz" ] || { echo "$SOURCE is not a backup directory" >&2; exit 1; }

printf 'This replaces all data in the stack configured by %s. Type "restore" to continue: ' "$ENV_FILE"
read -r answer
[ "$answer" = "restore" ] || { echo "Aborted."; exit 1; }

compose stop web api
compose up -d db
until compose exec -T db sh -c 'pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB"' > /dev/null 2>&1; do sleep 2; done

compose exec -T db sh -c 'pg_restore -U "$POSTGRES_USER" -d "$POSTGRES_DB" --clean --if-exists --no-owner --single-transaction' < "$SOURCE/database.dump"

API_CONTAINER=$(compose ps -a -q api)
[ -n "$API_CONTAINER" ] || { echo "api container not found; run 'compose up --no-start' first" >&2; exit 1; }
docker run --rm -i --volumes-from "$API_CONTAINER" alpine:3 \
    sh -c 'rm -rf /data/attachments/* /data/keys/* && tar xzf - -C /data && chown -R 1654:1654 /data/attachments /data/keys' < "$SOURCE/files.tar.gz"

# Starting the stack re-runs the migration job, which brings an older backup up to the deployed version.
compose up -d
echo "Restore complete. Check: curl -fsS http://127.0.0.1:\${WEB_PORT:-8080}/api/health/ready"
