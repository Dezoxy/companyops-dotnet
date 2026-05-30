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

Anyone holding the **Employee** role may create requests; Managers/Finance create via
their Employee role (roles compose — resolves the earlier Create TODO).

| Action (endpoint) | Employee | Manager | Finance | IT Admin | Auditor |
|---|---|---|---|---|---|
| Create request — `POST /requests` | ✓ | ✓ (as Employee) | ✓ (as Employee) | ✗ | ✗ |
| Submit — `POST /requests/{id}/submit` | ✓ own | ✗ | ✗ | ✗ | ✗ |
| Manager step — `…/approve` | ✗ | ✓ dept, stage | ✗ | ✗ | ✗ |
| Finance step — `…/approve` | ✗ | ✗ | ✓ stage | ✗ | ✗ |
| Reject — `…/reject` | ✗ | ✓ dept, stage | ✓ stage | ✗ | ✗ |
| Fulfill — `…/fulfill` | ✗ | ✗ | ✗ | ✓ stage | ✗ |
| Cancel — `…/cancel` (not yet built) | ✓ own, stage | TODO dept? | ✗ | ✗ | ✗ |
| View a request — `GET /requests/{id}` | ✓ (auth) | ✓ (auth) | ✓ | ✓ | ✓ read |
| List requests — `GET /requests` | ✓ (auth) | ✓ (auth) | ✓ | ✓ | ✓ read |
| View audit log — `GET /audit-logs` | ✗ | ✗ | ✗ | TODO | ✓ read |

> **Read scoping is a known gap (Phase 3):** `GET` endpoints currently require
> authentication but are not yet narrowed to own/department — any authenticated role
> sees all requests. Row-level read scoping is a tracked follow-up.

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
  (currently Keycloak defaults; revisit Phase 10/11).
- **The committed realm (`infra/keycloak/realm-companyops.json`) is dev-only and must
  not be imported as-is into any deployed environment.** It enables direct access
  grants (ROPC), `sslRequired: none`, and wildcard `redirectUris`/`webOrigins` for
  local convenience. Before non-local use (Phase 11): split a deployed realm that
  disables ROPC, sets `sslRequired`, and pins redirect URIs / web origins to the SPA
  origin. Phase 4 audit must key on the immutable `sub`, not `preferred_username`.

## Audit logging — TODO (Phase 4)

- Append-only `AuditLog`; no update/delete path exposed to any role.
- Record actor, action, target, old→new state, timestamp, source IP, correlation ID.
- TODO: tamper-evidence (e.g. hash chain) — enterprise-optional.

## Secrets handling

- gitleaks runs as a local pre-commit hook (`.githooks/pre-commit`) and as a CI
  gate (`.github/workflows/security.yml`). Allowlist lives in `.gitleaks.toml`.
- Connection strings / client secrets via env vars or .NET user-secrets in dev;
  a secrets manager in deployed environments (TODO: choose — Phase 11).
- Least-privilege DB user (not owner/superuser). TODO: define roles/grants.

## Input validation

- All input validated at the Application boundary (FluentValidation). TODO: wire
  global model-validation + problem-details responses in the API (Phase 1–3).

## Transport & headers — TODO (Phase 10–11)

- HTTPS/TLS in deployed environments; HSTS, secure headers, CORS restricted to
  known SPA origins.

## Rate limiting — TODO (enterprise-optional)

- Consider per-user/IP limits on auth and write endpoints.

## Backup encryption & recovery — TODO (Phase 10–11)

- See [backup-restore.md](backup-restore.md). Encrypt backups at rest; document
  RPO/RTO and a tested restore procedure.

## Threat model (skeleton — STRIDE)

Trust boundaries: Browser/SPA ↔ API ↔ Database/Queue/Cache; API ↔ Keycloak;
API ↔ external mock services (Finance/Inventory).

| Category | Example threat | Primary mitigation | Status |
|---|---|---|---|
| **S**poofing | Forged/replayed JWT | OIDC validation (sig/iss/aud/exp), short tokens | ✓ P3 (token lifetime tuning TODO) |
| **T**ampering | Altering another dept's request (IDOR) | Resource-scoped authz on loaded aggregate | ✓ P3 (Domain dept-scope) |
| **R**epudiation | "I didn't approve that" | Append-only audit log w/ actor + IP | TODO P4 |
| **I**nfo disclosure | Leaking entities/PII via API | DTO mapping, least-data responses, authz on reads | partial: DTOs ✓; read scoping TODO |
| **D**oS | Flooding write/auth endpoints | Rate limiting, timeouts on external calls | TODO P5/opt |
| **E**oP | Auditor or Employee performing privileged action | Policies + domain invariants, deny-by-default | ✓ P3 |
| Supply chain | Vulnerable NuGet/npm dep, leaked secret | gitleaks + dep/vuln scan + CodeQL in CI | gitleaks ✓, rest TODO P9 |

## Security checklist

Tracked in the project plan; mirrored here as the working list. See
[companyops_enterprise_dotnet_project_plan.md](companyops_enterprise_dotnet_project_plan.md#security-checklist).
