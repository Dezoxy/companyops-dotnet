# CompanyOps — Agent Guide

Canonical instructions for any AI agent working in this repo. `CLAUDE.md` imports
this file; per-layer `src/*/CLAUDE.md` files add rules local to each project.

## What this is

**CompanyOps** — an internal procurement & asset-management system built as an
**enterprise-style learning and portfolio project**. The full roadmap lives in
[docs/companyops_enterprise_dotnet_project_plan.md](docs/companyops_enterprise_dotnet_project_plan.md).

This is **not** a production product and **not** a CRUD demo. The point is to
demonstrate enterprise backend + DevOps thinking. **The journey is the deliverable.**

**Scope:** backend-first. The core focus is the API, data, messaging, auth,
observability, and infra. A **thin Angular SPA** is included for **demo
purposes** — to show the approval workflow and the Keycloak login flow
end-to-end — added as Phase 12, after the backend MVP stands on its own. The UI
is a *client* of the API; it carries no business logic. Don't let frontend work
derail the backend phases.

## Learning mode (read this first)

- **Don't scaffold ahead of the current phase.** Build what the active phase needs,
  nothing more. We are following the 11 phases in order.
- When introducing an enterprise pattern **for the first time**, explain the
  trade-off and the realistic alternative *before or while* writing it — briefly.
  A silently-perfect repo teaches nothing.
- Prefer one well-understood vertical slice over broad half-built scaffolding.

## Locked stack decisions

Recorded as ADRs in `docs/decisions/`. Defaults for this repo:

| Area | Choice |
|---|---|
| Runtime | **.NET 10 LTS**, C# |
| Web | ASP.NET Core Web API |
| ORM | Entity Framework Core **10** (ships with .NET 10 LTS) |
| Database | **PostgreSQL 18** (Npgsql) |
| Auth | **Keycloak 26** — OIDC / JWT (Phase 3) |
| Queue | **RabbitMQ 4** (Phase 5) |
| Cache | **Redis 8** |
| Background | .NET Worker Service |
| Tests | xUnit + Testcontainers (real Postgres) |
| Node.js | **24 LTS** ("Krypton") — frontend toolchain |
| Frontend | **Angular 21** (standalone components, signals) — demo UI |
| Frontend auth | `angular-auth-oidc-client` — OIDC Authorization Code + PKCE to Keycloak |
| Local env | Docker Compose |
| CI/CD | GitHub Actions |

Don't swap any of these without a new ADR.

**Versioning policy: newest LTS / newest-supported, pinned.** .NET, EF Core, and
Node.js follow a formal LTS line — use the newest LTS major. PostgreSQL, Redis,
RabbitMQ, and Keycloak have no classic "LTS" tier — use the newest stable major
(each gets a long support window). **Angular is the exception:** every major gets
only 18 months total (6 active + 12 LTS), so the version *tagged* LTS is always an
older one near EOL. For Angular, use **latest stable** (currently 21; move to 22
when it leaves RC) — that maximizes the support window, which is the actual intent
behind "use LTS."

## Architecture — Clean Architecture, modular monolith

Dependencies point **inward only**. This is the rule the whole project lives by.

```
CompanyOps.Api  ─┐
CompanyOps.Worker┼─►  CompanyOps.Application  ─►  CompanyOps.Domain
CompanyOps.Infrastructure ─────────────────────►  (Domain)
```

- **Domain** depends on nothing. Pure C#: entities, value objects, enums, domain
  rules, domain events. No EF Core, no ASP.NET, no I/O.
- **Application** depends on Domain only. Use cases (commands/queries + handlers),
  validators, interfaces (ports) for infrastructure. No concrete EF/HTTP/queue code.
- **Infrastructure** implements Application's interfaces: EF Core `DbContext`,
  repositories, Keycloak, RabbitMQ, Redis, external API clients.
- **Api / Worker** are the entry points: wire DI, expose endpoints / consume queues.
  Thin — no business logic here.

If you reach for `DbContext` in Domain or business rules in a controller, stop —
you're in the wrong project.

**The Angular SPA (`frontend/`) sits outside this dependency graph.** It is a
separate client that talks to the API over HTTP only. It authenticates against
Keycloak directly (public client, PKCE) and sends the JWT to the API; the API
remains the resource server and re-validates everything. No business rules,
secrets, or authorization decisions live in the SPA — the API is the source of
truth. See [frontend/CLAUDE.md](frontend/CLAUDE.md).

## Non-negotiables

- **No secrets in git.** Config via environment variables / user-secrets. Enforced
  by gitleaks — local pre-commit hook (`.githooks/pre-commit`, enable with
  `git config core.hooksPath .githooks`) and a CI gate. See [docs/security.md](docs/security.md).
- **Business actions, not CRUD.** Model real workflow: `POST /requests/{id}/approve-manager`,
  not generic update. Endpoints map to domain operations.
- **Every meaningful state change is audit-logged** (who / what / when / old→new /
  affected object). Once Phase 4 lands, no approval/rejection/fulfillment without it.
- **Invalid status transitions must be rejected** in the domain (throw), not just
  guarded in the UI/API. The `Request` state machine is enforced in Domain.
- **Auditor role is read-only.** Never let it mutate.
- Validate all input at the Application boundary (FluentValidation or equivalent).
- **Authorization rules live in [docs/security.md](docs/security.md)** (role × action
  matrix + threat model). Treat that matrix as the source of truth; enforce
  Manager actions as **department-scoped** (resource-level), not role-only.

## Conventions

- One **vertical slice per use case**: command/query + validator + handler +
  domain method + endpoint + audit entry + tests. Keep a slice's files together.
- EF entities are **not** API contracts — map to/from request/response DTOs.
- EF migrations: review the generated SQL before applying. Migrations live in
  Infrastructure; never hand-edit applied migrations.
- Async all the way for I/O; `CancellationToken` on handler/repository methods.
- Tests: AAA, descriptive names (`Method_Scenario_ExpectedResult`). Integration
  tests use Testcontainers against real Postgres, not in-memory.

## Commands

```bash
dotnet build                       # build solution
dotnet test                        # all tests
dotnet ef migrations add <Name> -p src/CompanyOps.Infrastructure -s src/CompanyOps.Api
dotnet ef database update -p src/CompanyOps.Infrastructure -s src/CompanyOps.Api
docker compose up                  # full local stack (Phase 7+)
dotnet format                      # formatting (CI enforces this)
```

Frontend (from `frontend/`, Phase 12+):

```bash
npm install
ng serve                           # dev server (proxies /api to the backend)
ng build                           # production build
ng test                            # unit tests
ng lint                            # lint (CI enforces this)
```

## Tooling available

- **architecture-guardian** subagent — run it on backend diffs to catch Clean
  Architecture layer violations.
- **angular-guardian** subagent — run it on frontend diffs: keeps the SPA thin,
  OIDC/PKCE done right, no secrets/business logic in the UI, demo-quality basics.
- **new-angular-feature** skill — scaffolds an Angular feature (component +
  service + route + model + guard) in the project's conventions.
- Marketplace skills already cover ADRs (`engineering:architecture`), code review
  (`engineering:code-review`), testing strategy (`engineering:testing-strategy`),
  debugging (`engineering:debug`), runbooks (`operations:runbook`), security review
  (`security-review`). Reuse these — don't reinvent them.
- Project-specific skills (`new-slice`, `ef-migration`, `phase-kickoff`) will be
  added when Phases 1 and 8 make them worth it.

## Definition of done (per change)

`dotnet build` clean → `dotnet test` green → `dotnet format` clean →
layer rules respected → audit logged (where applicable) → no secrets staged.
