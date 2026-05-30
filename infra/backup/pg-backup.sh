#!/usr/bin/env bash
# Nightly logical backup of the CompanyOps Postgres database (Phase 11). Installed as a cron
# job by the Ansible playbook. Dumps via the running container (so the password stays in the
# container's env, never on this host's command line) and prunes dumps past the retention
# window. Restore is documented in docs/backup-restore.md.
set -euo pipefail

# Dumps contain audit data + PII — keep the directory (0700) and dump files (0600) private.
umask 077

BACKUP_DIR="${COMPANYOPS_BACKUP_DIR:-/var/backups/companyops}"
RETENTION_DAYS="${COMPANYOPS_BACKUP_RETENTION_DAYS:-14}"
CONTAINER="${COMPANYOPS_PG_CONTAINER:-companyops-postgres}"
DB="${COMPANYOPS_DB:-companyops}"
DB_USER="${COMPANYOPS_DB_USER:-companyops}"

mkdir -p "$BACKUP_DIR"
stamp="$(date +%Y%m%d-%H%M%S)"
out="$BACKUP_DIR/companyops-${stamp}.dump"

# -Fc = compressed custom format (restore selectively with pg_restore).
docker exec "$CONTAINER" pg_dump -U "$DB_USER" -d "$DB" -Fc >"$out"

# Prune dumps older than the retention window.
find "$BACKUP_DIR" -name 'companyops-*.dump' -type f -mtime "+${RETENTION_DAYS}" -delete

echo "backup written: ${out} ($(du -h "$out" | cut -f1))"
