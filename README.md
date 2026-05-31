# CompanyOps

[![ci](https://github.com/Dezoxy/companyops-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/Dezoxy/companyops-dotnet/actions/workflows/ci.yml)
[![security](https://github.com/Dezoxy/companyops-dotnet/actions/workflows/security.yml/badge.svg)](https://github.com/Dezoxy/companyops-dotnet/actions/workflows/security.yml)

An internal **request & approval platform** — built as an enterprise-style .NET
learning/portfolio project. At its core it's **one configurable approval-workflow
engine**: a request flows `created → submitted → approved → fulfilled`, with the
**approval chain configured per request type** (procurement is the seed flow), every
state change **audit-logged**, and external systems integrated **asynchronously**.

This is not a CRUD demo — the point is to demonstrate enterprise backend + DevOps
thinking. **The journey is the deliverable.** Full plan:
[docs/companyops_enterprise_dotnet_project_plan.md](docs/companyops_enterprise_dotnet_project_plan.md).

## Architecture

Clean Architecture modular monolith — dependencies point inward only:

```
Api / Worker / Infrastructure  ─►  Application  ─►  Domain
```

- **Domain** — entities, the `Request` aggregate + step-driven approval state machine, rules. No I/O.
- **Application** — use cases (command/query + handler), ports.
- **Infrastructure** — EF Core, Keycloak, RabbitMQ, the external-system gateways.
- **Api / Worker** — thin entry points. The Worker consumes integration events (outbox → RabbitMQ) and calls external systems resiliently.

The **Angular SPA** (`frontend/`) sits *outside* this graph — a separate client of the API
([ADR 0010](docs/decisions/0010-frontend-full-client-angular-material.md)). It authenticates to
Keycloak directly (OIDC + PKCE) and sends the JWT to the API; it holds no business logic, and the
API re-validates everything.

Key decisions are recorded as ADRs in [docs/decisions/](docs/decisions). Conventions for
contributors (incl. AI agents) live in [AGENTS.md](AGENTS.md).

## Stack

- **Backend** — .NET 10 · EF Core 10 · PostgreSQL 18 · Keycloak 26 (OIDC/JWT) · RabbitMQ 4 · Redis 8 · xUnit + Testcontainers.
- **Frontend** — Angular 21 (standalone components, signals) · Angular Material (M3) · `angular-auth-oidc-client` (Auth Code + PKCE).
- **Platform** — Docker Compose · GitHub Actions · Traefik (TLS edge) · Terraform + Ansible.

## Run it

```bash
docker compose -f infra/docker-compose.yml up --build
```

Brings up the whole stack (Postgres, Keycloak, RabbitMQ, Redis, the mock external
systems, a one-shot migrator, the API, and the Worker). API at `http://localhost:5080`
(`/scalar` for interactive docs). Full guide, tokens, and seed users:
[docs/local-development.md](docs/local-development.md).

The SPA runs against that stack with the Angular dev server (proxies `/api` to the API):

```bash
cd frontend && npm install && npx ng serve   # http://localhost:4200
```

## Tests & CI

```bash
dotnet test    # domain + application unit tests + Testcontainers integration tests
```

CI (`.github/workflows/`) runs format + build + unit tests (fast), the Testcontainers
integration suite (Docker), a vulnerable-package scan, and a Docker image build; gitleaks
gates secrets; CodeQL activates if the repo goes public. See
[docs/testing-strategy.md](docs/testing-strategy.md) and [docs/security.md](docs/security.md).

## Operability

Structured JSON logs (Serilog), OpenTelemetry metrics + traces, a request correlation id
(`X-Correlation-ID`), and health endpoints (`/health` liveness, `/health/ready` checks
Postgres + RabbitMQ). How to run, observe, and recover it:
[docs/runbook.md](docs/runbook.md) · [docs/troubleshooting.md](docs/troubleshooting.md) ·
[docs/backup-restore.md](docs/backup-restore.md).

## Deployment

Production runs behind a **Traefik** TLS edge ([ADR 0009](docs/decisions/0009-deployment-topology-edge.md)),
on a Linux VM provisioned by **Terraform** and configured/deployed by **Ansible**. Deployment is
**release-driven** ([ADR 0012](docs/decisions/0012-release-driven-deployment.md)): publishing a
GitHub Release builds + pushes images to **GHCR**, applies Terraform (OIDC, remote state), and
runs Ansible to pull the version and roll the stack forward. Full runbook + one-time setup:
[docs/deployment.md](docs/deployment.md).

## Status

**Feature-complete — all 20 phases shipped** (see the [plan](docs/companyops_enterprise_dotnet_project_plan.md)).

*Backend (Phases 1–11):* API + domain (1), configurable approval workflow (2), Keycloak auth (3),
audit logging (4), worker + queue (5), external integration (6), Docker orchestration (7),
tests (8), CI/CD (9), observability & operations (10), infrastructure automation (11).

*Frontend (Phases 12–20)* — the "CompanyOps Enterprise Suite"
([ADR 0010](docs/decisions/0010-frontend-full-client-angular-material.md)), a full Angular client:
workspace foundation (12), OIDC/PKCE auth + typed API client (13), core workflow UI —
dashboard / requests / approvals / audit (14), helpdesk-light flow (15), asset console +
request-driven lifecycle (16), IT-admin fulfilment console (17), reports & analytics (18),
integrations status (19), settings & profile with light/dark theming (20).

Three real processes run on the one engine — IT/helpdesk requests, asset lifecycle, and generic
internal approvals — distinguished by configurable approval chains, not separate code paths
([ADR 0005](docs/decisions/0005-configurable-approval-workflow.md)). Deliberately deferred work
(pre-production hardening, enterprise-optional, and explicit non-goals) is tiered in
[docs/future-improvements.md](docs/future-improvements.md).
