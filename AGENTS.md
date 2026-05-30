# CompanyOps — Agent Guide

Canonical instructions for any AI agent working in this repo. `CLAUDE.md` imports
this file; per-layer `src/*/CLAUDE.md` files add rules local to each project.

## What this is

**CompanyOps** — an internal **request & approval platform** built as an
**enterprise-style learning and portfolio project**. The full roadmap lives in
[docs/companyops_enterprise_dotnet_project_plan.md](docs/companyops_enterprise_dotnet_project_plan.md).

At its core it is **one configurable approval-workflow engine**. Three real
internal processes run on it — all built as first-class flows, distinguished by
**configurable approval chains per request type** (not hard-coded), see
[ADR 0005](docs/decisions/0005-configurable-approval-workflow.md):

1. **IT request / helpdesk-light** — service/access requests fulfilled by IT.
2. **Asset management** — assets and their assignment lifecycle.
3. **Internal approval** — generic multi-step sign-off (procurement is the seed example).

These are not three systems; they are one `request → approval → fulfillment`
lifecycle with different chains and fulfillment actions.

This is **not** a production product and **not** a CRUD demo. The point is to
demonstrate enterprise backend + DevOps thinking. **The journey is the deliverable.**

Treat architecture, tests, CI, and security practices as production-grade
requirements for learning outcomes. Operational hardening such as SLA, HA,
multi-region deployment, and enterprise support are out of scope for this project
and should be documented as a trade-off in PRs or ADRs.

**Scope:** backend-first. The core focus is the API, data, messaging, auth,
observability, and infra. A **thin Angular SPA** is included for **demo
purposes** — to show the approval workflow and the Keycloak login flow
end-to-end — added as Phase 12, after the backend MVP stands on its own. The UI
is a *client* of the API; it carries no business logic. Don't let frontend work
derail the backend phases.

## Learning mode (read this first)

- **Don't scaffold ahead of the current phase.** Build what the active phase needs,
  nothing more. We are following the 14 phases in order. The active phase is
  declared by the repo-root file `ACTIVE_PHASE` containing a single integer
  `1..14`; tools and humans must not introduce features for phases greater than
  that value.
- When you introduce an enterprise pattern in this repo/module, add a 2–3
  sentence rationale plus a 3-line alternative in the PR description and add a
  single-line code comment linking to the ADR if applicable.
- Scaffolding tools must validate generated files against the per-phase feature
  whitelist (below) and stop — listing the offending files — rather than scaffold
  ahead; the `new-slice` skill reads `ACTIVE_PHASE` and does this. A CI gate that
  fails the build on out-of-phase files is *planned, not yet implemented*: until
  it lands, phase-gating is enforced by tooling and review, not CI.
- Prefer one well-understood vertical slice over broad half-built scaffolding.

### Phase feature table

The per-phase feature whitelist. A phase unlocks its own row **and** every row
above it. Operational phases (8 tests, 9 CI/CD, 10 observability, 11 infra) and
the additional flows (13 helpdesk-light, 14 asset lifecycle) add no *new* gated
feature category — they harden operations or run on the existing engine — so they
have no row here. "Don't scaffold ahead" still applies to everything; the table
only enforces the categories most likely to be pulled in early.

| Phase | Allowed feature additions |
|---|---|
| 1–2 | Core API/domain/application slices only; no auth, no audit, no queue. |
| 3 | Add auth / JWT security. |
| 4 | Add audit logging for runtime/data changes. |
| 5 | Add queue/worker integration. |
| 6 | Add external-integration clients (Finance/Inventory mocks). |
| 7 | Add full local stack wiring via Docker Compose. |
| 12 | Add frontend SPA features. |

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

To change a locked stack item, create an ADR in `docs/decisions/` describing the
rationale, migration plan, and rollback. The ADR must list approvers: at minimum
the architecture owner and the security owner. Emergency exceptions may be
applied only after a temporary ADR and must be followed by a retrospective ADR
within 72 hours.

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

Each rule names an *owner* role (security, backend, platform, domain,
architecture). This is a deliberate enterprise simulation: in a team these map to
CODEOWNERS and reviewers. In this solo repo the single maintainer wears every
hat — the labels record *which hat* a decision belongs to, not separate people.

1. **No secrets in git.** Config must use environment variables or user-secrets.
   Enforced by gitleaks — local pre-commit hook (`.githooks/pre-commit`, enable
   with `git config core.hooksPath .githooks`) and a CI gate. If gitleaks or CI
   fails due to secrets:
   - abort the commit/PR,
   - run `gitleaks detect` locally to identify offending files,
   - remove secrets and rotate any exposed credentials,
   - document the rotation in the PR and notify the security owner per
     `docs/security.md`.
   Owner: security.
2. **Input validation is mandatory.** Validate all input at the Application
   boundary using FluentValidation or equivalent. Owner: backend.
3. **Audit logging is required for runtime/data changes.** Code changes that
   affect runtime behavior or data (API, Domain, Infrastructure, Worker,
   migrations) must include audit entries. Pure docs/config/CI changes do not
   require audit entries. Audit at minimum: request creation, all request status
   transitions, approval/rejection actions, assignment or department changes,
   permission/grant modifications, and fulfillment completion. Record
   who/what/when/old_value→new_value/affected_object for each. Owner: platform.
4. **Invalid status transitions must be rejected in the domain.** Throw in the
   domain model rather than only guarding in UI/API. The `Request` state machine
   is enforced in Domain. Owner: domain.
5. **Auditor role is read-only.** Never let it mutate. Owner: security.
6. **Authorization rules live in [docs/security.md](docs/security.md).** Treat the
   role × action matrix as the source of truth; enforce Manager actions as
   department-scoped (resource-level), not role-only. Owner: security.
7. **Business actions, not CRUD.** Model real workflow: `POST /requests/{id}/approve-manager`,
   not generic update. Endpoints map to domain operations. Owner: architecture.

## Conventions

- One **vertical slice per use case**: command/query + validator + handler +
  domain method + endpoint + audit entry + tests. Keep a slice's files together.
- EF entities are **not** API contracts — map to/from request/response DTOs.
- EF migrations: review the generated SQL before applying. Migrations live in
  Infrastructure; never hand-edit applied migrations. If generated SQL contains
  destructive operations such as `DROP`/`ALTER` that may lose data, do not apply
  to staging/production. Open a migration-fix PR, require DB owner sign-off, and
  run the migration on a staging DB restored from production before applying to
  production. Include the reviewed SQL in the PR description. (Staging/production
  arrive in Phase 11; until then, review the SQL and avoid destructive migrations.)
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
- **new-slice** skill — scaffolds a backend vertical slice for one business action
  (command) or read (query) across all four layers in the project's conventions:
  domain method, Application command/query + handler + DI registration, a
  business-action endpoint, and tests. Phase-aware tooling must read the active
  phase from `ACTIVE_PHASE` and refuse to add code for phases greater than that value.
- **new-angular-feature** skill — scaffolds an Angular feature (component +
  service + route + model + guard) in the project's conventions.
- Marketplace skills already cover ADRs (`engineering:architecture`), code review
  (`engineering:code-review`), testing strategy (`engineering:testing-strategy`),
  debugging (`engineering:debug`), runbooks (`operations:runbook`), security review
  (`security-review`). Reuse these — don't reinvent them.
- `ef-migration` skill will be added around Phase 8 when migration churn makes it
  worth it. (`phase-kickoff` was dropped — a short doc covers it without skill overhead.)

## Definition of done (per change)

`dotnet build` clean → `dotnet test` green → `dotnet format` clean →
layer rules respected → no secrets staged.

Code changes that affect runtime behavior or data (API, Domain,
Infrastructure, Worker, migrations) must include audit entries. Pure docs,
config, or CI changes do not require audit entries.
