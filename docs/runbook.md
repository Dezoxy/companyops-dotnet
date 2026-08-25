# CompanyOps — Runbook

How to operate, observe, and recover the running system. Pairs with
[troubleshooting.md](troubleshooting.md) (symptom → fix) and
[backup-restore.md](backup-restore.md) (data recovery). For first-time setup see
[local-development.md](local-development.md).

> Scope: the commands below target the **local Docker Compose** stack
> (`infra/docker-compose.yml`). A deployed environment now exists too — the
> `infra/docker-compose.prod.yml` stack behind a Traefik TLS edge on a VM
> ([deployment.md](deployment.md), [ADR 0009](decisions/0009-deployment-topology-edge.md)) — where
> the same services run under the same names; substitute the prod compose file and add `sudo`
> where the playbook installed it. Items still **open** in the deployed stack (nothing scrapes the
> health endpoints, no metrics backend) are marked **(open)** and listed under
> [Known gaps](#known-gaps).

## Service map

| Service | Role | Host port | State |
|---|---|---|---|
| `api` | Web API (resource server) | 5080 | stateless |
| `worker` | Queue consumer, external calls | — | stateless |
| `postgres` | **Source of truth** (requests, steps, audit, outbox, idempotency) | 5432 | **durable** |
| `keycloak` | Identity provider (OIDC/JWT) | 8080 | realm is committed |
| `rabbitmq` | Event broker (+ mgmt UI :15672) | 5672 | transient (quorum queue) |
| `redis` | Cache (not yet consumed) | 6379 | transient |
| `fakeexternals` | Mock Finance/Inventory | 5090 | stateless mock |

Two more services exist **only in the prod stack** (`infra/docker-compose.prod.yml`): `traefik`
(the TLS edge and only public ingress, ports 80/443) and `frontend` (the built Angular SPA served
by nginx at the site root). Locally the SPA runs from the Angular dev server instead — see
[local-development.md](local-development.md).

The API and Worker hold **no durable state** — they can be killed and restarted
freely. All business state lives in Postgres.

## Start / stop / restart

```bash
# Start the whole stack (build on first run)
docker compose -f infra/docker-compose.yml up --build -d

# Restart just the app tier after a code change
docker compose -f infra/docker-compose.yml up -d --build api worker

# Tail logs (structured JSON)
docker compose -f infra/docker-compose.yml logs -f api worker

# Stop, keep data
docker compose -f infra/docker-compose.yml down
# Stop and WIPE data (drops the Postgres volume) — destructive
docker compose -f infra/docker-compose.yml down -v
```

Startup is ordered: `migrator` applies the schema after Postgres is healthy, then
`api` and `worker` start (they never self-migrate). See
[ADR 0007](decisions/0007-async-messaging-outbox.md) for the messaging flow.

## Health & readiness

The API exposes two anonymous endpoints (see `Observability/ObservabilitySetup.cs`):

| Endpoint | Checks | Use for |
|---|---|---|
| `GET /health` | process is up (no dependencies) | **liveness** — restart if failing |
| `GET /health/ready` | Postgres + RabbitMQ reachable | **readiness** — pull from rotation if failing |

```bash
curl -s -o /dev/null -w '%{http_code}\n' http://localhost:5080/health        # 200
curl -s http://localhost:5080/health/ready                                    # Healthy | Unhealthy
```

`200 Healthy` / `503 Unhealthy`. The Worker has no HTTP server — its liveness is
the process plus its broker connection (it logs `Listening for integration events
on queue 'companyops.worker'` once connected). **(open)** nothing scrapes these endpoints in
either stack — Postgres and RabbitMQ have compose healthchecks, `api`/`worker` do not, so an
unhealthy app tier is not auto-restarted. Wiring the probes is a follow-up
([production-readiness.md](production-readiness.md) §4).

## Logs

Structured JSON to stdout via Serilog (`JsonFormatter`), one object per line.
Every line carries `TraceId` and `SpanId` (OpenTelemetry trace context); API
request logs and anything within a request also carry `CorrelationId`.

```bash
# Follow API logs, pretty-print, and pull the fields that matter
docker compose -f infra/docker-compose.yml logs -f api \
  | grep '^{' | jq -c '{t:.Timestamp, lvl:.Level, msg:.MessageTemplate, cid:.Properties.CorrelationId, trace:.Properties.TraceId}'
```

- **Correlation id:** clients may send `X-Correlation-ID`; if absent the API
  generates one and echoes it on the response. Use it to grep one request's logs.
- **Cross-service tracing:** the RabbitMQ consume span continues the API's publish
  span, so an API request and the Worker processing its event share one `TraceId`.
  Grep both services' logs by that `TraceId` to follow a request end-to-end.
- Levels are config-driven (`Serilog:MinimumLevel` in `appsettings.json`); framework
  noise is pinned to `Warning`, app logs to `Information`.

## Metrics & traces

OpenTelemetry is wired in both services (ASP.NET Core / HTTP client / runtime /
Npgsql / RabbitMQ). Export is environment-driven:

- **No exporter configured (default):** in Development, metrics/traces print to the
  console; otherwise they're collected but not exported.
- **`OTEL_EXPORTER_OTLP_ENDPOINT` set:** export via OTLP to that collector.

```bash
# Point the stack at an OTLP collector
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
```

The prod stack passes `OTEL_EXPORTER_OTLP_ENDPOINT` through to `api` and `worker`
(`infra/docker-compose.prod.yml`, optional `otel_exporter_otlp_endpoint` in the Ansible vars), so
the wiring is there — but **(open)** no collector or backend is deployed, so nothing is stored or
queried today. Standing one up (Prometheus/Tempo/Grafana or a managed APM) is a follow-up.

## Inspecting the broker

RabbitMQ management UI: <http://localhost:15672> (`companyops` / the compose
throwaway password). Key objects (`MessagingTopology.cs`):

| Object | Name | Notes |
|---|---|---|
| Exchange | `companyops.events` | direct, routed by event-type name |
| Work queue | `companyops.worker` | durable **quorum** queue, `x-delivery-limit=5` |
| Dead-letter exchange | `companyops.events.dlx` | fanout |
| Dead-letter queue | `companyops.worker.dead-letter` | poison messages land here |

A message redelivered past the delivery limit (transient failures) or rejected as
poison is dead-lettered — it is **not** lost. See the DLQ playbook below.

## Where state lives (Postgres)

| Table | Holds |
|---|---|
| `requests`, `approval_steps` | the request aggregate + per-step decisions |
| `audit_logs` | append-only who/what/when/old→new (never mutate) |
| `outbox_messages` | events awaiting publish (producer side) |
| `processed_messages` | consumer idempotency dedup keys |

## Incident playbooks

Diagnose with [troubleshooting.md](troubleshooting.md); these are the *actions*.

**`/health/ready` is 503**
1. `docker compose ps` — is `postgres` / `rabbitmq` healthy?
2. Check the failing dependency's logs; restart it if unhealthy.
3. The API recovers on its own once the dependency is back (no API restart needed).

**Worker not processing (queue depth climbing)**
1. Mgmt UI → `companyops.worker`: is depth rising with no consumers? → Worker is
   down or disconnected. `docker compose logs worker`; restart `worker`.
2. Consumers present but depth still climbing → processing is slow/erroring; check
   Worker logs for the failing event `TraceId`, and whether `fakeexternals` is up.
3. Backlog drains on its own once the Worker is healthy (events are durable).

**Messages in `companyops.worker.dead-letter`**
1. These exhausted retries or were rejected as poison — **safe, not lost**.
2. Inspect a message in the mgmt UI; the failure is in the Worker logs under that
   event's `TraceId`.
3. Fix the cause (e.g. `fakeexternals` outage, a bug), then **shovel** the DLQ back
   onto `companyops.events` (mgmt UI → Shovel, or re-publish). Consumers are
   idempotent (`processed_messages`), so replaying an already-applied event is a
   no-op.

**Token rejected (401 everywhere)**
- Almost always an issuer/hostname mismatch — see the token section of
  [troubleshooting.md](troubleshooting.md). The API is the resource server; it does
  not mint tokens, so this is a Keycloak/config issue, not an API restart.

## Known gaps

Open in **both** stacks, including the deployed one. Tiered in
[future-improvements.md](future-improvements.md), tracked in
[production-readiness.md](production-readiness.md).

- No alerting, dashboards, or metrics backend (the OTLP endpoint is wired but points nowhere).
- No health probes (endpoints exist; nothing scrapes them, and `api`/`worker` have no compose
  healthcheck).
- DB-level grants so the app user cannot `UPDATE/DELETE audit_logs` (see
  [security.md](security.md)).
- **Backups are automated** — the Ansible deploy installs a nightly `pg_dump` cron with 14-day
  retention — but they are **unencrypted, on the same VM, and the restore drill has not been run
  against them**; the Keycloak database is not in the dump. See
  [backup-restore.md](backup-restore.md).
