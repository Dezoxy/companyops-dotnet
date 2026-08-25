# Local development

Two ways to run the backend locally: the **whole stack in Docker**, or **backing
services in Docker + the .NET apps from your IDE**. The Angular SPA always runs from the
Angular dev server in local development ([below](#the-angular-spa)) — there is no `frontend`
service in the local compose file.

## Prerequisites

- Docker Desktop (or a Docker daemon)
- .NET 10 SDK (only for running/building the apps outside containers) — pinned in `global.json`
- Node.js 24 LTS (only for the SPA)

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

The SPA is **not** in this table — it runs from `ng serve` ([below](#the-angular-spa)). The
production stack does containerise it (`infra/docker-compose.prod.yml`, nginx behind Traefik).

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

## The Angular SPA

```bash
cd frontend
npm install
npx ng serve          # http://localhost:4200
```

The dev server proxies `/api` → `http://localhost:5080` (`proxy.conf.json`, which strips the
`/api` prefix), so the browser never makes a cross-origin call to the API and local CORS is a
non-issue. The SPA logs in against Keycloak's **public `companyops-spa` client** with
Authorization Code + PKCE — no client secret ships in the bundle.

> ### Gotcha: the SPA needs `KC_HOSTNAME=http://localhost:8080`
>
> An OIDC client rejects a token whose **issuer** doesn't match the `authority` it was configured
> with. The SPA's dev authority is `http://localhost:8080/realms/companyops`
> (`frontend/src/environments/environment.development.ts`), but the compose file above runs
> Keycloak with `KC_HOSTNAME=http://keycloak:8080` — so tokens are issued as
> `http://keycloak:8080/...` and **browser login fails** against the stack as shipped.
>
> Two ways out, pick one:
>
> - Start Keycloak with the host name the browser uses:
>   `KC_HOSTNAME=http://localhost:8080 docker compose -f infra/docker-compose.yml up keycloak`
>   (override it in the compose file or an env file). The IDE-run API already validates
>   `http://localhost:8080/realms/companyops`, so this path is consistent end to end — but the
>   *containerised* API, which is pinned to the `keycloak:8080` issuer, will then reject the token.
> - Or map `keycloak` → `127.0.0.1` in `/etc/hosts` and switch the SPA's `authority` to
>   `http://keycloak:8080/realms/companyops`, which keeps one issuer for both the browser and the
>   containerised API.
>
> The deployed stack has none of this friction: the SPA, API, and Keycloak all sit on one origin
> (`APP_DOMAIN`, Keycloak under `/auth`) — see [deployment.md](deployment.md).

Build and lint exactly as CI does: `npx ng build`, `npx ng lint`,
`npx ng test --watch=false`. More frontend conventions: [../frontend/README.md](../frontend/README.md)
and [../frontend/CLAUDE.md](../frontend/CLAUDE.md).

## Notes

- All credentials in compose are **local-only throwaways** — never reuse them anywhere.
- The committed Keycloak realm is dev-only (ROPC, no TLS, wildcard redirects); see
  [security.md](security.md). The hardened deployed realm is a separate file,
  `infra/keycloak/realm-companyops.prod.json` (ROPC off, PKCE enforced, brute-force protection on,
  pinned redirect URIs, **no seed users**).
