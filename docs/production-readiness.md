# CompanyOps — Production-Readiness Guide

Date: 2026-06-01
Status: **Living checklist** — the honest gap list between "works on my machine / portfolio build"
and "I can hand this to a paying customer."

> **Trackable doc.** Every line is a GitHub task-list box (renders as a progress bar). Tick
> (`- [x]`) when an item is genuinely done and link the PR/doc. This is the single index of
> *readiness*; the authoritative per-area detail lives in [security.md](security.md),
> [deployment.md](deployment.md), [testing-strategy.md](testing-strategy.md),
> [backup-restore.md](backup-restore.md), and [future-improvements.md](future-improvements.md).

## How to read this

CompanyOps is a learning/portfolio project — *the journey is the deliverable*. Measured against a
real **customer handover**, some gaps are expected. Each item is tagged:

- **[x]** — done and verifiable in the repo.
- **[ ]** — not done; a real gap to close before a customer relies on it.
- **(partial)** — started but incomplete.
- **(out of scope)** — a deliberate non-goal for this project ([AGENTS.md](../AGENTS.md) Tier D:
  SLA, HA, multi-region, enterprise support). **For a customer handover these stop being
  "out of scope" and become an explicit, priced decision** — don't let them stay silent.

A defensible customer handover = every **[ ]** is either closed or consciously accepted in writing.

---

## 1. Security & authentication
Authoritative: [security.md](security.md) (role × action matrix + STRIDE threat model).

- [x] OIDC/JWT auth via Keycloak; API is a resource server re-validating every token.
- [x] Role- **and** resource-scoped authorization (department-scoped manager actions; Auditor read-only).
- [x] Authorization model proven by adversarial scan — no BOLA/BFLA (42Crunch session, 2026-06-01).
- [x] Audit logging — append-only, who/what/when/old→new on every state change.
- [x] Rate limiting at the API edge.
- [x] No secrets in git — env vars / user-secrets, gitleaks gate in CI.
- [x] TLS terminated at the Traefik edge; app speaks HTTP only on the internal network.
- [ ] **Input validation across all write endpoints** (partial) — FluentValidation added for
      create-request + register-asset; **extend to** submit/approve/reject/fulfill/cancel/comment/assign.
      See [openapi-contract-plan.md](openapi-contract-plan.md) and [security.md](security.md).
- [ ] Secrets manager (cloud KMS / Vault) for deployed environments — [future-improvements.md](future-improvements.md) Tier B.
- [ ] Token lifetime / refresh / signing-key rotation tuned away from Keycloak defaults — Tier B.
- [ ] Content-Security-Policy + security headers on the SPA edge — Tier B.
- [ ] Least-privilege DB user; `audit_logs` denied UPDATE/DELETE at the DB level — Tier B.
- [ ] Dependency vulnerability scanning + container image scanning in CI (CodeQL ✅ present; add
      `dotnet list package --vulnerable` / Trivy / dependency-review).

## 2. API contract & documentation
Authoritative: [openapi-contract-plan.md](openapi-contract-plan.md).

- [x] OpenAPI document generated at build time from the code.
- [x] Interactive API docs (Scalar) — currently dev-only.
- [ ] Single canonical, security-accurate contract; hand-tuned `openapi.json` retired (in progress — see the plan).
- [ ] CI gate that fails on contract drift.
- [ ] Explicit API versioning strategy (e.g. `/v1`) before external consumers integrate.
- [ ] Customer-facing integration guide (auth flow, examples, error model).

## 3. Reliability & resilience
Authoritative: [runbook.md](runbook.md), ADR 0007/0008.

- [x] Health endpoints — `/health` (liveness) + `/health/ready` (DB + RabbitMQ).
- [x] Transactional outbox + worker; at-least-once consumers with idempotency guard.
- [x] External integrations have timeouts + retry / graceful degradation (Polly).
- [x] DB migrations applied by a one-shot migrator; generated SQL reviewed before apply.
- [ ] Graceful-shutdown / draining verified under load.
- [ ] Dead-letter handling + replay procedure documented in the runbook.
- [ ] (out of scope) HA / multi-instance / failover — revisit for a customer SLA.

## 4. Observability & operations
Authoritative: [runbook.md](runbook.md), [troubleshooting.md](troubleshooting.md).

- [x] Structured JSON logging (Serilog) with request correlation id.
- [x] OpenTelemetry traces + metrics emitted (OTLP exporter wired).
- [ ] Logs/traces/metrics shipped to a real backend (aggregation, retention policy, search).
- [ ] Dashboards + alerting (error rate, latency, queue depth, failed logins).
- [ ] On-call / incident-response process + escalation path.
- [ ] Correlation/trace id persisted onto the audit record — [future-improvements.md](future-improvements.md).

## 5. Data, backup & privacy (EU/GDPR)
Authoritative: [backup-restore.md](backup-restore.md), [security.md](security.md).

- [x] Backup & restore procedure documented.
- [x] EU data residency (Azure westeurope) — default region.
- [ ] **Restore actually tested** from a backup into a clean environment (drill, with RTO/RPO recorded).
- [ ] Data retention + deletion policy (how long audit/request data is kept).
- [ ] GDPR: PII inventory, data-subject-access/erasure process, processor agreement (DPA) — needed before real customer data.
- [ ] PII minimisation review of logs (ensure no tokens/PII in structured logs).

## 6. Deployment & infrastructure
Authoritative: [deployment.md](deployment.md), ADR 0009/0012.

- [x] CI: build + unit tests, integration tests (Testcontainers), docker image build, IaC validate, frontend lint.
- [x] Release-driven deploy to Azure (build & push images → provision & deploy).
- [x] Infrastructure as code (reviewed in CI).
- [ ] A real **staging** environment that mirrors prod, deployed to before prod.
- [ ] Documented rollback / previous-image redeploy procedure (beyond image retagging).
- [ ] Deploy-time secrets sourced from a manager, not env files.
- [ ] (out of scope) Blue/green or canary; multi-region — revisit per customer SLA.

## 7. Performance & scale
- [ ] **Pagination on list endpoints** — today `GET /requests`, `/assets`, `/audit-logs` return
      **all rows** (confirmed: no `Skip/Take`/limit). A real dataset needs limit/offset or cursor paging.
- [ ] Load / soak test to establish a baseline (throughput, p95 latency, failure point).
- [ ] N+1 / slow-query review on the read models; add DB indexes where needed.
- [x] Redis cache available (used where it earns it).
- [ ] Connection-pool + resource limits validated under concurrency.

## 8. Testing & quality
Authoritative: [testing-strategy.md](testing-strategy.md).

- [x] Domain + Application unit tests; integration tests on real Postgres (Testcontainers).
- [x] `dotnet format` + build + tests gated in CI; CodeQL SAST; gitleaks.
- [ ] Coverage measured + a minimum threshold gated in CI.
- [ ] API contract tests (verify the running API matches the published contract) — pairs with §2.
- [ ] Frontend E2E tests for the critical user journeys.
- [ ] Security scan (42Crunch audit/scan) run in CI as a report or gate.

## 9. Documentation & handover
- [x] Architecture decisions recorded as ADRs; per-layer `CLAUDE.md` guides.
- [x] Operational runbook + troubleshooting guide.
- [ ] Top-level `README` for a new engineer (what it is, how to run, how to deploy).
- [ ] Customer/operator handover pack: SLAs, support model, contacts, known limitations.
- [ ] Architecture diagram (C4 context/container) kept current.

---

## Suggested order (highest customer-impact first)

1. **Pagination** (§7) and **input validation across all write endpoints** (§1) — correctness/abuse gaps a customer hits immediately.
2. **API contract finalisation + CI drift gate** (§2, [openapi-contract-plan.md](openapi-contract-plan.md)) — already in motion.
3. **Tested restore drill + data retention/GDPR** (§5) — required before real customer data.
4. **Staging environment + rollback** (§6) and **alerting/dashboards** (§4) — operate it safely.
5. **Secrets manager, CSP, least-privilege DB, token rotation** (§1 Tier B) — deepen the security posture.
6. **README + customer handover pack** (§9) — make it ownable by someone else.
