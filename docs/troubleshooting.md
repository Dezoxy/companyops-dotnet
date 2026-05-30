# CompanyOps — Troubleshooting

Symptom → likely cause → fix, for the failure modes this stack actually hits.
For operational *actions* (restart, drain DLQ, recover) see [runbook.md](runbook.md).

## How to diagnose anything

1. **Find the request's `TraceId`.** Client got the `X-Correlation-ID` back; grep
   the API logs for it, read off the `TraceId`, then grep **both** API and Worker
   logs by that `TraceId` — the trace spans both services.
   ```bash
   docker compose -f infra/docker-compose.yml logs api worker \
     | grep '^{' | jq -c 'select(.Properties.TraceId=="<traceid>")'
   ```
2. **Check readiness:** `curl -s http://localhost:5080/health/ready`.
3. **Check the broker:** mgmt UI <http://localhost:15672> — queue depth, consumers,
   dead-letter queue.

## Auth / tokens

| Symptom | Likely cause | Fix |
|---|---|---|
| `401` on every authenticated call | Token issuer ≠ what the API validates | Tokens must be minted with issuer `http://keycloak:8080/realms/companyops`. `KC_HOSTNAME=http://keycloak:8080` pins this; fetch tokens via the host port but the issuer stays `keycloak:8080` (the API accepts it). See [local-development.md](local-development.md#auth--tokens). |
| `401` only from the IDE-run API | API can't reach Keycloak / wrong authority | `appsettings.Development.json` → `Keycloak:Authority` must match the issuer above; Keycloak must be up on :8080. |
| `403` (authenticated but denied) | Role/department/stage check failed — **working as intended** | Confirm the user's realm role and `department` claim vs the action; Manager actions are department-scoped (see [security.md](security.md)). Not a bug unless the matrix says it should be allowed. |
| Keycloak realm import fails on boot | Unknown fields (e.g. `comment`) in the realm JSON | Keep `infra/keycloak/realm-companyops.json` to fields the importer accepts; no `comment` keys. |

## Messaging / Worker

| Symptom | Likely cause | Fix |
|---|---|---|
| Worker logs `Connection refused` to RabbitMQ on startup | AMQP listener not bound yet (startup race) | Expected transient; the connection has a retry loop and the compose healthcheck uses `check_port_connectivity` + `start_period`. It self-heals — if it loops forever, RabbitMQ itself is unhealthy. |
| Events created but never processed | Worker down, or not consuming | `docker compose logs worker`; expect `Listening for integration events on queue 'companyops.worker'`. Restart `worker`. Events are durable — backlog drains on recovery. |
| Queue depth climbing, consumers present | Processing erroring or slow (often `fakeexternals` down) | Find the failing event's `TraceId` in Worker logs; verify `fakeexternals` is up (:5090). |
| Messages in `companyops.worker.dead-letter` | Exhausted `x-delivery-limit=5` (transient) or rejected as poison | **Not lost.** Fix the cause, then shovel/replay onto `companyops.events`. Replays are safe — consumers dedup via `processed_messages`. |
| Same event applied twice? | It isn't — at-least-once delivery + idempotent consumer | Confirm the dedup key in `processed_messages`; a duplicate delivery is a logged no-op (ADR 0007). |

## Database / migrations

| Symptom | Likely cause | Fix |
|---|---|---|
| `relation "..." does not exist` | Migrations not applied | The `migrator` service applies them on `up`. Run it: `docker compose -f infra/docker-compose.yml run --rm migrator`, or run the API once with `--migrate`. |
| Postgres won't start / data not persisting | Wrong volume mount path for PG 18 | PG 18 mounts at `/var/lib/postgresql` (not `/var/lib/postgresql/data`) — already set in compose. If you changed it, data won't persist. |
| `migrator` exits non-zero | Bad migration or Postgres not healthy | Read its logs; it depends on `postgres: service_healthy`, so a flapping DB blocks it. |
| Can't connect from IDE-run app | Wrong connection string / port | `appsettings.Development.json` points at `localhost:5432`; the compose port binds to `127.0.0.1:5432`. |

## Observability

| Symptom | Likely cause | Fix |
|---|---|---|
| Logs are plain text, not JSON | Serilog not active / old build | Both apps use `JsonFormatter`; rebuild. Startup banner lines are also JSON. |
| Logs too noisy / too quiet | `Serilog:MinimumLevel` | Tune `appsettings.json` → `Serilog:MinimumLevel.Override` (framework pinned to `Warning`, app `Information`). |
| Console flooded with OTel metric dumps in dev | Console exporter is on by default in Development | Expected. Set `OTEL_EXPORTER_OTLP_ENDPOINT` to ship to a collector instead, or run outside Development. |
| No traces/metrics in my collector | `OTEL_EXPORTER_OTLP_ENDPOINT` unset/wrong | Set it per service to the collector's OTLP gRPC endpoint (e.g. `http://otel-collector:4317`). |
| Log lines missing `CorrelationId` | Logged outside the request scope (e.g. Worker, startup) | Expected — `CorrelationId` is request-scoped (API). Cross-service correlation uses `TraceId`, which is always present. |

## Still stuck?

Reproduce with the smallest request, capture its `TraceId`, and read the trace
across both services. Most issues are a dependency being down (`/health/ready`) or
a config/issuer mismatch — not application logic.
