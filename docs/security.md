# CompanyOps — Security

Security model, authorization rules, and threat model for CompanyOps. This is a
living document: sections marked **TODO** are filled in as the relevant phase
lands. The authorization matrix below is the source of truth that code reviews
and the (future) `security-guardian` check against.

## Principles

- **Defense in depth.** Authorization is enforced at the API boundary (policies)
  **and** as invariants in the Domain. The UI may hide actions, but it never
  enforces them — the API is the source of truth and re-validates everything.
- **Least privilege.** Each role gets only what it needs; the database user the
  app connects with is not a superuser.
- **Auditability.** Every meaningful state change is recorded (who / what / when /
  old→new / affected object). Audit records are append-only.
- **No secrets in git.** Enforced by gitleaks (local pre-commit hook + CI gate).
  Config comes from environment variables / user-secrets, never source.

## Roles

| Role | Responsibility |
|---|---|
| Employee | Creates and submits their own requests; cancels their own. |
| Manager | Approves/rejects requests **for their own department**. |
| Finance | Approves/rejects the budget step. |
| IT Admin | Fulfills approved requests; assigns assets. |
| Auditor | Reads everything (incl. audit logs). **Never mutates.** |

> A real person may hold more than one role (e.g. a Manager is also an Employee).
> Whether managers/finance can also *create* requests is an open decision —
> **TODO: resolve in an ADR** and update the matrix.

## Authorization matrix (role × action)

`✓` allowed · `✗` denied · `own` = only the actor's own resource · `dept` =
only resources in the actor's department · stage = only valid at that workflow stage.

Approval uses **one generic endpoint** `POST /requests/{id}/approve` (and `…/reject`),
not role-named endpoints (ADR 0006): the API policy admits Manager **or** Finance, and
the Domain matches the actor's role to the current step. The Manager/Finance rows below
are the *conceptual* steps that one endpoint serves.

The chain is configured **per request type** ([ADR 0005](decisions/0005-configurable-approval-workflow.md)):
**Procurement** = Manager (department) → Finance (global); **Helpdesk** = Manager (department) only
(Phase 15); **AssetLifecycle** = Manager (department) only (Phase 16). So a helpdesk or asset-lifecycle
request reaches Approved after a single manager sign-off, then IT Admin fulfils — the same `/approve`
and `/fulfill` endpoints serve every flow; the type's chain selects the step.

The fulfillment **action** differs by type (the action, not the authorization — IT Admin fulfils all
flows). An **AssetLifecycle** fulfillment additionally assigns a concrete in-stock asset to the
requester (`Asset.Assign`, recorded as the request's `FulfilledAssetId`) — a real internal transition
in the same transaction. IT names the asset in the `…/fulfill` body (`assignedAssetId`); the Domain
rejects a fulfillment that names no asset for this type, or names one for any other type.

Anyone holding the **Employee** role may create requests; Managers/Finance create via
their Employee role (roles compose — resolves the earlier Create TODO).

**Cancel** (`…/cancel`) *withdraws* a request that hasn't been decided yet (Draft or Submitted) —
distinct from `…/reject`, which is an approver's decision *on the merits* (carries a reason,
recorded against the step). The **requester** may cancel their own request; a **department
Manager** may cancel any request in their department (oversight). The `✗` for Finance / IT Admin /
Auditor means *not by virtue of those roles* — but a Finance or IT user who raised a request
themselves cancels it via their **Employee** role, exactly as with create/submit (roles compose).
The **Auditor** holds no Employee role and is stopped at the policy (403). The API policy admits
Employee **or** Manager; the Domain enforces the fine-grained rule (own request, or manager of the
request's department) on the loaded aggregate — a domain rejection (not the requester, not this
department's manager, or past the Draft/Submitted stage) is a **400**.

| Action (endpoint) | Employee | Manager | Finance | IT Admin | Auditor |
|---|---|---|---|---|---|
| Create request — `POST /requests` | ✓ | ✓ (as Employee) | ✓ (as Employee) | ✗ | ✗ |
| Submit — `POST /requests/{id}/submit` | ✓ own | ✗ | ✗ | ✗ | ✗ |
| Manager step — `…/approve` | ✗ | ✓ dept, stage | ✗ | ✗ | ✗ |
| Finance step — `…/approve` | ✗ | ✗ | ✓ stage | ✗ | ✗ |
| Reject — `…/reject` | ✗ | ✓ dept, stage | ✓ stage | ✗ | ✗ |
| Fulfill — `…/fulfill` | ✗ | ✗ | ✗ | ✓ stage | ✗ |
| Cancel — `…/cancel` | ✓ own, Draft/Submitted | ✓ dept, Draft/Submitted | ✗ | ✗ | ✗ |
| View a request — `GET /requests/{id}` | ✓ own | ✓ dept | ✓ all | ✓ all | ✓ all |
| List requests — `GET /requests` | ✓ own | ✓ dept | ✓ all | ✓ all | ✓ all |
| View audit log — `GET /audit-logs` | ✗ | ✗ | ✗ | ✓ read | ✓ read |

> **Read scoping:** both request reads — `GET /requests` (list) and `GET /requests/{id}` (single)
> — are scoped to the caller, mirroring who can act on what: an Employee sees only their own, a
> Manager their department, and Finance / IT Admin / Auditor see all (Finance approves the global
> step; IT fulfils any; Auditor oversees). The Api derives the scope from the principal's role
> (`RequestsController.ReadScope`); the list applies it in the repository query, the single read in
> the handler. An out-of-scope single read returns **404, not 403** — a request's existence isn't
> revealed to someone not entitled to see it.

### Asset console (Phase 16)

The asset inventory + lifecycle is an IT-Admin console; **reads also admit the read-only
Auditor**. Assets are **not department-scoped** (unlike requests) — any IT Admin manages any
asset (an intentional central-IT model).

| Action (endpoint) | IT Admin | Auditor | Employee / Manager / Finance |
|---|---|---|---|
| List / get / history — `GET /assets`, `/assets/{id}`, `/assets/{id}/history` | ✓ | ✓ read | ✗ |
| Register — `POST /assets` | ✓ | ✗ | ✗ |
| Assign / reclaim / repair / return-from-repair / retire — `POST /assets/{id}/…` | ✓ | ✗ | ✗ |

Reads use the `ReadAssets` policy (IT Admin + Auditor); writes use `ManageAssets` (IT Admin).
Every lifecycle transition is audited via `AuditLog.ForAsset` (target type `"Asset"`) — the
asset's history, also surfaced in `GET /audit-logs`. Each custody change also records **who
held it** (`AffectedUserId` — the assignee on assign, the prior holder on reclaim / send-to-
repair / retire, captured before the transition clears it), surfaced in the asset history.
A duplicate tag returns **409 Conflict** (`ConflictException` → `ConflictExceptionHandler`): the
handler pre-checks the normalized tag, and the unique index on the column is the integrity
backstop. (A truly concurrent duplicate registration would still hit the index and surface as a
500 — negligible for manual IT registration; fully closing it means translating the unique-
violation at the persistence seam, which is left out to avoid changing the Worker's save path.)

There is a **second path to `Asset.Assign`** (Phase 16c): when IT Admin fulfils an
**AssetLifecycle** request, the assignment happens through the request flow rather than the
console. It is still IT-Admin-only — gated by `FulfillRequests`, not `ManageAssets` — and the
employee who raised the request never gains a write path to `/assets`. Both paths converge on the
same Domain transition and audit entry.

### Reports & Analytics (Phase 18)

Aggregate, read-only counts for the oversight roles. The numbers are computed in the database
(`GROUP BY` / `COUNT` — see `ReportingStore`); only the grouped buckets leave Postgres.

| Action (endpoint) | Employee | Manager | Finance | IT Admin | Auditor |
|---|---|---|---|---|---|
| Request report — `GET /reports/requests` | ✗ | ✓ read | ✓ read | ✓ read | ✓ read |
| Asset report — `GET /reports/assets` | ✗ | ✓ read | ✓ read | ✓ read | ✓ read |

All endpoints use one `ReadReports` policy (Manager / Finance / IT Admin / Auditor). Plain
Employees are excluded — they see only their own requests, not org-wide analytics. The asset
report exposes aggregate counts only (not the inventory records `ReadAssets` gates), so it is
open to the same oversight set rather than IT-Admin/Auditor alone. **Reports are global**, not
department-scoped: a Manager sees org-wide counts, not just their department. Department-scoped
reporting is a deliberate enterprise-optional follow-up (it would mirror the per-step department
check already applied to approvals). Read-only — no mutation, no audit entry.

### Integrations status (Phase 19)

A read-only operational view of the async pipeline (the transactional outbox + the Worker's
processed-message markers, ADR 0007/0008): outbox counts (pending / published / failed), how many
messages the Worker has consumed, and the most recent messages with their relay status.

| Action (endpoint) | Employee | Manager | Finance | IT Admin | Auditor |
|---|---|---|---|---|---|
| Integration status — `GET /integrations/status` | ✗ | ✗ | ✗ | ✓ read | ✓ read |

Gated by `ReadIntegrations` (IT Admin + Auditor) — this is plumbing/observability for operators and
oversight, not a business view, so the business roles (Employee / Manager / Finance) are excluded
(narrower than `ReadReports`). The response carries only message metadata (type, status, timestamps,
attempt count, error text) — **never the event payload**. Read-only — no mutation, no audit entry.

**Hard invariants (must always hold):**
- Auditor has **no** mutating path anywhere.
- Manager actions are **department-scoped** — a manager cannot act on another
  department's request (resource/row-level check, not just role). This is the main
  IDOR risk; enforce on the loaded aggregate, not just the route.
- Workflow actions are only valid at the correct status; invalid transitions are
  rejected in the Domain (throw), independent of who calls them.

## Authentication — Phase 3 (implemented)

- **Keycloak 26 (OIDC).** The API is a resource server validating JWTs (issuer,
  audience `companyops-api`, expiry, signature) via `JwtBearer`. Realm + seed users
  are a committed export imported on `compose up` (`infra/keycloak/`). The SPA will be
  a public client using Authorization Code + PKCE (Phase 12) on the same client.
- **Role mapping:** Keycloak realm roles → ASP.NET role claims (the nested
  `realm_access.roles` is flattened in `OnTokenValidated`); endpoint policies
  (`AuthorizationPolicies.cs`) gate by role. Actor id (`sub`) and `department` claim
  are read from the principal — never the request body.
- **Defense in depth:** policies are the coarse role gate at the boundary; department
  scope, workflow stage, and submit-own are enforced as Domain invariants.
- TODO: token lifetime tuning, refresh strategy, clock-skew tolerance, key rotation
  (currently Keycloak defaults; revisit Phase 11).
- **Two realms.** The committed dev realm (`infra/keycloak/realm-companyops.json`) is
  **local-only** — ROPC on, `sslRequired: none`, wildcard `redirectUris`/`webOrigins`,
  seed users with throwaway passwords. The **deployed realm**
  (`infra/keycloak/realm-companyops.prod.json`, Phase 11) hardens it: ROPC **disabled**,
  `sslRequired: external`, PKCE (S256) enforced, brute-force protection on, pinned redirect
  URIs / web origins (placeholder until the SPA origin lands in Phase 12), and **no seed
  users** (real users are created by an admin — no committed credentials). Audit keys on the
  immutable `sub`, not `preferred_username`.

## Audit logging — Phase 4 (implemented)

- **Append-only `AuditLog`** (Domain entity, factory only, no mutators). Written as a
  side effect of each business action via the `IAuditLogger` port, **enlisted in the
  same transaction** as the state change — no approved-but-unaudited request. No
  write/update/delete API path; reads go through `GET /audit-logs` (Auditor + IT Admin).
- Records who (`ActorId` = `sub`) / what (`AuditAction`) / when / old→new status /
  affected object (`TargetType`+`TargetId`) / affected holder (`AffectedUserId`, on asset
  custody changes) / source IP for create, submit, approve, reject, fulfill, cancel. The source IP
  is stamped by the audit writer from the request context (null for Worker-driven outcomes, which
  have no HTTP request); behind the edge it's the real client IP (ForwardedHeaders).
- Worker-driven outcomes (budget committed / asset reserved, Phase 6) have no human
  principal, so they record the **reserved system actor**
  `ffffffff-ffff-ffff-ffff-ffffffffffff` (`WellKnownActors.SystemWorker`) — never assign
  it to a real user.
- Correlation id + trace id flow through logs and traces (Phase 10), so any audited action is
  traceable end-to-end; the source IP is now persisted on the audit record itself (above), while
  persisting the correlation/trace ids onto it too remains an optional follow-up. DB-level grants
  so even the app user cannot UPDATE/DELETE `audit_logs` (Phase 11); tamper-evidence / hash chain —
  enterprise-optional.

## Secrets handling

- gitleaks runs as a local pre-commit hook (`.githooks/pre-commit`) and as a CI
  gate (`.github/workflows/security.yml`). Allowlist lives in `.gitleaks.toml`.
- **GitHub native secret scanning + push protection are enabled** on the repo
  (defense in depth alongside gitleaks): pushes containing a detected secret are
  blocked, and history is scanned. The committed local-dev throwaways
  (`localdev_only_not_a_secret`, `Passw0rd!`) are low-entropy and non-routable, so
  neither scanner flags them.
- Connection strings / client secrets via env vars or .NET user-secrets in dev;
  a secrets manager in deployed environments (TODO: choose — Phase 11).
- Least-privilege DB user (not owner/superuser). TODO: define roles/grants.

## Input validation

- All input validated at the Application boundary (FluentValidation). TODO: wire
  global model-validation + problem-details responses in the API (Phase 1–3).

## Transport & headers

- **TLS terminates at the Traefik edge** (Phase 11, [ADR 0009](decisions/0009-deployment-topology-edge.md)):
  Let's Encrypt certificates, HTTP→HTTPS redirect, and HSTS + `nosniff` / `frameDeny` /
  `referrer-policy` response headers as Traefik middleware. The app speaks HTTP in-cluster.
- **`ForwardedHeaders` is wired** (`ForwardedHeaders:Enabled`, on in the prod compose): the
  API trusts `X-Forwarded-Proto`/`-For` from the single Traefik ingress (it has no public
  binding), so it sees the real scheme + client IP. App-level `UseHttpsRedirection` stays
  off — the edge owns the redirect.
- **CORS restricted to the SPA origin** (Phase 13): the API allows only `Cors:AllowedOrigins`
  (dev `http://localhost:4200`; the deployed SPA origin via env), Bearer-token only (no
  credentials). Verified by an integration test (allowed vs unknown origin).
- **Keycloak client split** (Phase 13): a public **`companyops-spa`** client (Auth Code + PKCE,
  pinned redirect/web origins, audience mapper → `companyops-api`) is separate from the
  **bearer-only `companyops-api`** audience. The dev realm keeps `companyops-api` with ROPC for
  the integration tests; the prod realm makes it bearer-only.
- TODO (when the SPA is served in prod): a **Content-Security-Policy** on the SPA's responses
  (script/style/connect-src for the API + Keycloak). Deferred until the SPA is dockerised behind
  the edge — a CSP on the API/Keycloak routers now would be the wrong target.
- TODO (enterprise-optional): split the security knobs keyed on the environment name (e.g.
  `RequireHttpsMetadata`) into explicit config flags so a stray environment value can't
  silently drop a protection.

## Rate limiting

- A global rate limit (`RateLimitingSetup`) guards against floods: a fixed window **partitioned by
  the authenticated user (`sub`)**, falling back to client IP for anonymous callers; health probes
  are exempt. Over-limit requests get **429 + `Retry-After`**. It runs after authentication (so the
  partition keys on the user) and before authorization/endpoints. Limits are config-bound
  (`RateLimiting:PermitLimit` / `:WindowSeconds`, default 120/60s) and tunable per environment.
- Enterprise-optional refinements (not done): a tighter limit on the auth/write endpoints
  specifically, true exponential backoff, and a distributed limiter store for multi-instance
  deployments (the current limiter is per-instance/in-memory).

## Backup encryption & recovery

- [backup-restore.md](backup-restore.md) documents what to back up (Postgres is the
  only source of truth), RPO/RTO targets, and a tested restore drill (Phase 10).
- TODO (Phase 11): encryption at rest, scheduled/offsite backups, and managed
  point-in-time recovery in a deployed (EU-region) environment.

## Threat model (skeleton — STRIDE)

Trust boundaries: Browser/SPA ↔ API ↔ Database/Queue/Cache; API ↔ Keycloak;
API ↔ external mock services (Finance/Inventory).

| Category | Example threat | Primary mitigation | Status |
|---|---|---|---|
| **S**poofing | Forged/replayed JWT | OIDC validation (sig/iss/aud/exp), short tokens | ✓ P3 (token lifetime tuning TODO) |
| **T**ampering | Altering another dept's request (IDOR) | Resource-scoped authz on loaded aggregate | ✓ P3 (Domain dept-scope) |
| **R**epudiation | "I didn't approve that" | Append-only audit log w/ actor + old→new + source IP | ✓ P4 |
| **I**nfo disclosure | Leaking entities/PII via API | DTO mapping, least-data responses, authz on reads | DTOs ✓; read-scoping ✓ (list + GET-by-id; out-of-scope → 404) |
| **D**oS | Flooding write/auth endpoints | Rate limiting, timeouts on external calls | ✓ (per-user/IP rate limit + external-call timeouts/retries) |
| **E**oP | Auditor or Employee performing privileged action | Policies + domain invariants, deny-by-default | ✓ P3 |
| Supply chain | Vulnerable NuGet/npm dep, leaked secret | gitleaks + native secret scanning/push protection + `dotnet list --vulnerable` + Dependabot + CodeQL | ✓ P9 |

## Security checklist

Tracked in the project plan; mirrored here as the working list. See
[companyops_enterprise_dotnet_project_plan.md](companyops_enterprise_dotnet_project_plan.md#security-checklist).
