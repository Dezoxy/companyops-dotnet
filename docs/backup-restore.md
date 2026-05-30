# CompanyOps — Backup & Restore

What to back up, how to restore, and the recovery targets. Referenced by
[security.md](security.md) (backup encryption & recovery). Operational actions live
in [runbook.md](runbook.md).

## What actually needs backing up

| Data | Store | Back up? | Why |
|---|---|---|---|
| Requests, approval steps, **audit logs**, outbox, idempotency | **Postgres** | **Yes — this is the only source of truth** | Losing it loses business + audit history (append-only audit must survive). |
| Identity (realm, users, roles) | Keycloak | No (dev) | The dev realm is committed (`infra/keycloak/realm-companyops.json`) and re-imported on boot. A deployed realm is a separate backup target — **(Phase 11)**. |
| In-flight events | RabbitMQ | No | Transient. Durable quorum queue + outbox means unpublished work is reconstructable from `outbox_messages`; in-flight events replay safely (idempotent consumers). |
| Cache | Redis | No | Rebuildable from Postgres. |

So: **back up Postgres.** Everything else is either committed to git, transient, or
derived.

## Recovery targets (illustrative)

This is a learning/portfolio project, so these are stated as *intent*, not an SLA:

| Metric | Target | Meaning |
|---|---|---|
| **RPO** (max data loss) | ≤ 24h (dev: best-effort) | A daily logical dump bounds loss to one day. PITR shrinks this to minutes — **(Phase 11)**. |
| **RTO** (max downtime) | ≤ 1h | Time to restore a dump into a fresh Postgres and bring the app tier back. |

A backup is only real once its **restore has been tested** (drill below).

## Local backup (dev, against the Compose volume)

Logical dump with `pg_dump` in the running container — `-Fc` (custom format) is
compressed and restorable selectively:

```bash
# Dump to ./backups/ on the host
mkdir -p backups
docker compose -f infra/docker-compose.yml exec -T postgres \
  pg_dump -U companyops -d companyops -Fc \
  > backups/companyops-$(date +%Y%m%d-%H%M%S).dump
```

> The credentials are the compose local-only throwaways; never reuse them.

## Restore (tested procedure)

`pg_restore --clean` drops and recreates objects, so it overwrites the target DB.
**Destructive — confirm you're pointed at the right database.**

```bash
# 1. Stop the app tier so nothing writes mid-restore
docker compose -f infra/docker-compose.yml stop api worker

# 2. Restore into the existing database (drops/recreates objects first)
docker compose -f infra/docker-compose.yml exec -T postgres \
  pg_restore -U companyops -d companyops --clean --if-exists --no-owner \
  < backups/companyops-YYYYMMDD-HHMMSS.dump

# 3. Bring the app tier back; it picks up where the data leaves off
docker compose -f infra/docker-compose.yml start api worker
```

For a **clean-slate** restore instead (recommended for drills — proves the dump is
self-sufficient): `docker compose down -v` to wipe the volume, `up -d postgres`,
let the `migrator` create the schema (or restore creates it), then `pg_restore`.

### Restore drill checklist

1. Take a dump (above).
2. `docker compose down -v` (wipe), then `up -d postgres migrator`.
3. `pg_restore` the dump.
4. Start `api`/`worker`; hit `GET /health/ready` → `Healthy`.
5. Spot-check: a known request and its `audit_logs` rows are present.

Run this drill whenever the backup procedure changes — an untested backup is a
guess.

## Deployed environments — Phase 11

**Implemented:** the Ansible deploy installs a **nightly `pg_dump` cron**
(`infra/backup/pg-backup.sh` — `-Fc` custom format, 14-day retention) to
`/var/backups/companyops` on the VM. That delivers the scheduled-dump path below; the
remaining hardening (encryption, offsite copies, PITR) is what's still open.

Two paths for my context (EU-based, cost-conscious, evaluating clouds):

**Self-hosted (homelab / VPS):** the nightly dump above, hardened with **encrypted, offsite**
copies (and `pgBackRest` for incremental + PITR via WAL archiving — daily full + WAL shipping
gets RPO to minutes). Cheapest, most control, all the operational burden is yours.

**Managed Postgres (cloud):** automated backups + **PITR** out of the box, backups
**encrypted at rest** by default. RDS / Azure Database for PostgreSQL / Cloud SQL all
do this; pick an **EU region** (`eu-central-1` / `westeurope` / `europe-west3`) for
GDPR data residency. Less burden, higher run cost, some lock-in around the snapshot
format.

**Default recommendation:** managed Postgres with PITR in an EU region once
deployed — the audit log is compliance-relevant, so encrypted-at-rest backups with
point-in-time recovery are worth the run cost over hand-rolled dumps. Whichever path,
the audit table's durability and the **tested** restore drill are the non-negotiables.

Cross-cutting (both paths): encrypt backups at rest, store them off the primary host,
restrict who can read them (audit data is sensitive), and schedule periodic restore
drills.
