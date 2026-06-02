# CompanyOps — Future improvements

The consolidated backlog of work **deliberately not done**, with the reason and a pointer to
where it's referenced in the code/docs. CompanyOps is a learning/portfolio project — *the journey
is the deliverable* — so this list is about judgement (what would earn its complexity, and when),
not a roadmap commitment.

Items are grouped by **why** they're deferred, continuing the tiers from the post-feature review:

- **Tier A — done.** Small, well-scoped hardening completed after the 20-phase build:
  request read-scoping ([#53](https://github.com/Dezoxy/companyops-dotnet/pull/53)), manager-cancel
  ([#54](https://github.com/Dezoxy/companyops-dotnet/pull/54)), asset-audit holder
  ([#55](https://github.com/Dezoxy/companyops-dotnet/pull/55)), 409-on-duplicate-tag
  ([#56](https://github.com/Dezoxy/companyops-dotnet/pull/56)), comment-thread scoping
  ([#57](https://github.com/Dezoxy/companyops-dotnet/pull/57)). Listed here for context; not repeated below.
- **Tier B — deferred security hardening.** Would be done before a real production deployment.
- **Tier C — enterprise-grade optional.** A larger org / higher-stakes deployment would add these.
- **Tier D — out of scope.** Explicit non-goals for this project ([AGENTS.md](../AGENTS.md)).

The authoritative per-area notes live in [security.md](security.md) and
[testing-strategy.md](testing-strategy.md); this file is the single index over them.

For a **customer-handover lens** on the same gaps — tracked as a checklist across security,
reliability, data/GDPR, deployment, performance, and docs — see
[production-readiness.md](production-readiness.md). The API contract is now a single
code-generated, audit-clean, drift-gated artifact (delivered in v1.2.0,
[openapi-contract-plan.md](openapi-contract-plan.md)).

---

## Tier B — Deferred security hardening

Real holes-to-close for a production deployment, not just polish. Most are gated on Phase 11
(deployed environment) where they first have a concrete target.

| Item | Why it matters | Referenced |
|---|---|---|
| **Token lifetime / refresh / clock-skew / key rotation** — tune away from Keycloak defaults; define a refresh strategy and signing-key rotation. | Long-lived or non-rotated tokens widen the blast radius of a leak. | [security.md](security.md) "Authentication" / STRIDE Spoofing |
| **Secrets manager** in deployed environments (choose one — e.g. cloud KMS/Secret Manager or self-hosted Vault). | Env vars / user-secrets are fine for dev; a real deployment needs managed rotation + access control. EU/data-residency applies. | [security.md](security.md) "Secrets handling" |
| **Least-privilege DB user + grants** — app runs as a non-owner role; `audit_logs` denied `UPDATE`/`DELETE` at the DB level. | Defense in depth: a compromised app user shouldn't be able to rewrite history or alter schema. | [security.md](security.md) "Secrets handling" / "Audit logging" |
| **Content-Security-Policy** on the SPA's responses (script/style/connect-src for self + Keycloak). | **Now unblocked** — the SPA is dockerised and served behind the edge at `APP_DOMAIN`. Next step: a Traefik `headers` middleware on the `spa` router; held back only pending validation against the running Angular Material app (an over-tight CSP breaks inline styles). | [security.md](security.md) "Transport & headers" |
| **Explicit security-knob flags** — split env-name-keyed toggles (e.g. `RequireHttpsMetadata`) into named config flags. | A stray environment value shouldn't be able to silently drop a protection. | [security.md](security.md) "Transport & headers" |
| **Correlation / trace id on the audit record** — persist the ids (already in logs/traces) onto `AuditLog` too. | Lets an audited action be pivoted to its full trace directly from the record. | [security.md](security.md) "Audit logging" |
| **Race-proof the duplicate-tag conflict** — translate the Npgsql unique-violation at the persistence seam, not only the handler pre-check. | The pre-check + unique index handles the realistic case; a *truly concurrent* duplicate still surfaces as 500. Closing it means a `SaveChanges`-seam translation, deferred to avoid changing the Worker's save path (its dedup classifies exceptions for retry-vs-dead-letter). | [security.md](security.md) "Asset console"; PR [#56](https://github.com/Dezoxy/companyops-dotnet/pull/56) |
| **Log scoped-out access attempts** at `Warning` (without revealing the foreign resource). | A 404 for an out-of-scope read/write leaves no server-side IDOR-probe signal today. | PR [#57](https://github.com/Dezoxy/companyops-dotnet/pull/57) review |

---

## Tier C — Enterprise-grade optional

Earns its complexity only at a larger scale or higher stakes than this project targets.

| Item | Why it matters | Referenced |
|---|---|---|
| **Tamper-evident audit (hash chain)** — chain each `AuditLog` row to the previous so deletion/edit is detectable. | DB-level grants already make the log append-only for the app user; a hash chain defends against a privileged actor too. | [security.md](security.md) "Audit logging" |
| **Department-scoped reports** — a Manager sees their department's analytics, not org-wide. | Mirrors the per-step department check already applied to approvals; today reports are intentionally global. | [security.md](security.md) "Reports & Analytics" |
| **Rate-limiting refinements** — a tighter limit on auth/write endpoints, true exponential backoff, and a distributed limiter store for multi-instance. | The current limiter is a per-instance in-memory fixed window — correct for a single instance, insufficient horizontally. | [security.md](security.md) "Rate limiting" |
| **Encryption at rest + offsite/PITR backups** in a deployed (EU-region) environment. | The restore drill and RPO/RTO targets exist; managed encryption + point-in-time recovery are the deployment step. | [security.md](security.md) "Backup encryption & recovery" · [backup-restore.md](backup-restore.md) |
| **Mutation testing / coverage gates** in CI. | Coverage % and surviving mutants would quantify test strength; not enforced today. | [testing-strategy.md](testing-strategy.md) "Known gaps" |
| **Dedicated `CompanyOps.Worker.Tests`** — unit-test the consumer dedup/route logic in isolation. | Today it's covered end-to-end by the integration round-trip + resilience tests; worth isolating if that logic grows. | [testing-strategy.md](testing-strategy.md) "Known gaps" |

---

## Tier D — Out of scope

Explicit non-goals for this learning project. Recorded so the omission reads as a decision, not a gap.

- **Operational SLA, HA, multi-region deployment, enterprise support.** Per [AGENTS.md](../AGENTS.md),
  this hardening is out of scope; the project demonstrates enterprise *thinking*, not 24/7 operations.
- **Performance / load / chaos testing.** Out of scope per [testing-strategy.md](testing-strategy.md);
  the resilience paths (retry → dead-letter, idempotent dedup) are covered functionally, not under load.

---

*When an item here is picked up, move it out of this file and into the relevant doc's "implemented"
section (the same way Tier A graduated). Keep this list a backlog, not an archive.*
