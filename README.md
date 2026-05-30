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

Key decisions are recorded as ADRs in [docs/decisions/](docs/decisions). Conventions for
contributors (incl. AI agents) live in [AGENTS.md](AGENTS.md).

## Stack

.NET 10 · EF Core 10 · PostgreSQL 18 · Keycloak 26 (OIDC/JWT) · RabbitMQ 4 ·
Redis 8 · xUnit + Testcontainers · Docker Compose · GitHub Actions.
Angular 21 SPA is a later, demo-only phase.

## Run it

```bash
docker compose -f infra/docker-compose.yml up --build
```

Brings up the whole stack (Postgres, Keycloak, RabbitMQ, Redis, the mock external
systems, a one-shot migrator, the API, and the Worker). API at `http://localhost:5080`
(`/scalar` for interactive docs). Full guide, tokens, and seed users:
[docs/local-development.md](docs/local-development.md).

## Tests & CI

```bash
dotnet test    # domain + application unit tests + Testcontainers integration tests
```

CI (`.github/workflows/`) runs format + build + unit tests (fast), the Testcontainers
integration suite (Docker), a vulnerable-package scan, and a Docker image build; gitleaks
gates secrets; CodeQL activates if the repo goes public. See
[docs/testing-strategy.md](docs/testing-strategy.md) and [docs/security.md](docs/security.md).

## Status

Built in phases (see the plan). Implemented: API + data (1), approval workflow (2), auth (3),
audit (4), worker + queue (5), external integration (6), Docker orchestration (7), tests (8),
CI/CD (9). Next: observability, infra automation, and the Angular demo UI.
