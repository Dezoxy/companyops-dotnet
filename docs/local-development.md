# Local development

Two ways to run CompanyOps locally: the **whole stack in Docker**, or **backing
services in Docker + the .NET apps from your IDE**.

## Prerequisites

- Docker Desktop (or a Docker daemon)
- .NET 10 SDK (only for running/building the apps outside containers) — pinned in `global.json`

## Run the whole stack

```bash
docker compose -f infra/docker-compose.yml up --build
```

This brings up the full system:

| Service | What | Host port |
|---|---|---|
| `postgres` | PostgreSQL 18 | 5432 |
| `keycloak` | Keycloak 26 (realm `companyops` auto-imported) | 8080 |
| `rabbitmq` | RabbitMQ 4 (+ management UI) | 5672 / 15672 |
| `redis` | Redis 8 (not yet consumed by code) | 6379 |
| `fakeexternals` | Mock Finance/Inventory systems | 5090 |
| `migrator` | Applies EF migrations, then exits | — |
| `api` | The Web API | 5080 |
| `worker` | Background worker (queue consumer) | — |

Startup is ordered: the **migrator** runs after Postgres is healthy and applies the
schema; **api** and **worker** wait for the migrator to complete (so the apps never
self-migrate). The API is at `http://localhost:5080` (`/scalar` for interactive docs).

Stop it (keep data): `docker compose -f infra/docker-compose.yml down`
Wipe data too: add `-v`.

## Auth / tokens

Keycloak runs with `KC_HOSTNAME=http://keycloak:8080`, so **every token's issuer is
`http://keycloak:8080/realms/companyops`** — the value the API validates against. Fetch
a token from the host against the published port (the issuer is still `keycloak:8080`,
which the API accepts):

```bash
curl -s -X POST http://localhost:8080/realms/companyops/protocol/openid-connect/token \
  -d grant_type=password -d client_id=companyops-api \
  -d username=manager.eng -d password='Passw0rd!'
```

Seed users (all password `Passw0rd!`): `employee.eng`, `manager.eng`, `manager.sales`,
`finance.user`, `itadmin.user`, `auditor.user`. Roles + departments are in
`infra/keycloak/realm-companyops.json`.

## Backing services only (apps from the IDE)

To run/debug the API or Worker from your IDE against containerized dependencies, start
just the infra and the mock:

```bash
docker compose -f infra/docker-compose.yml up postgres keycloak rabbitmq redis fakeexternals
```

The apps' `appsettings.Development.json` already point at `localhost` for these. Apply
migrations with the `ef-migration` flow (or run the API once with `--migrate`).

## Notes

- All credentials in compose are **local-only throwaways** — never reuse them anywhere.
- The committed Keycloak realm is dev-only (ROPC, no TLS, wildcard redirects); see
  [security.md](security.md) — a deployed realm is split out in Phase 11.
