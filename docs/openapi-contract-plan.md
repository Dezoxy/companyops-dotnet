# Plan — single, code-generated, audited API contract

Date: 2026-06-01
Status: **Proposed** (not started — this document is the plan, to be approved before the code changes)
Owner hats: architecture (decision), backend (implementation), security (audit/scan verification)

> **Trackable doc.** The boxes below are GitHub task-lists — they render as a progress bar on the
> PR/file view. Tick them (`- [x]`) as each step lands; link the PR next to the box. Nothing here
> changes runtime behaviour until a step is actually implemented and merged.

## In one paragraph (plain language)

An **API contract** is the document a customer's developers read to integrate with CompanyOps.
Today it's a **hand-written file** (`openapi.json`) that can silently fall out of sync with the
real code — a contract that lies to the customer. We have already switched on **build-time
generation** (the contract is produced automatically from the code on every build). This plan
finishes the job: make that generated contract complete and security-accurate, retire the
hand-written file, re-prove it's clean with the 42Crunch audit + scan, and add a CI gate so it can
**never drift again**. End state: one always-true, audit-clean contract that maintains itself.

## Where we are now

| | State |
|---|---|
| Build-time emission (`Microsoft.Extensions.ApiDescription.Server`) | ❌ **not in the repo.** Prototyped and verified working this session (it emitted `src/CompanyOps.Api/openapi/CompanyOps.Api.json`), then **reverted** so the PRs stayed focused. Re-introduced as Phase 1 below — including the design-time gotcha already solved. |
| Input-validation hardening (FluentValidation + JSON `Disallow`/`allowIntegerValues:false`) | ✅ done (separate PR) — so `additionalProperties:false` will be *honest* of the running API |
| Hand-tuned `openapi.json` (repo root) | ⚠️ the only audited contract today; **drifts** from code; gitignored, slated for deletion |
| Generated doc — security scheme, servers, error-response polish | ❌ absent |
| CI drift gate | ❌ none |

Once emission is (re-)enabled, the generated doc is **accurate but bare**; the hand-tuned one is
**hardened but drifts**. This plan turns the generated doc into the single hardened source of truth.

## The plan

### Phase 0 — Decide & record
- [ ] Write **ADR 0013 — code-generated API contract** (`docs/decisions/0013-*.md`): records the move
      from a hand-maintained spec to a build-time-generated + CI-gated contract; lists rationale,
      the 3-line alternative (keep hand-tuned / keep split), and approvers (architecture + security).
- [ ] Link this plan from `docs/future-improvements.md` and from the API-layer notes.

### Phase 1 — Enable build-time generation
- [ ] Re-add the `Microsoft.Extensions.ApiDescription.Server` package + `<OpenApiDocumentsDirectory>`
      to `src/CompanyOps.Api/CompanyOps.Api.csproj` so `dotnet build` emits the document.
- [ ] Handle the **design-time gotcha** (already solved this session): the generator runs `Program`
      up to `app.Run()` with no DB/Keycloak/RabbitMQ config, tripping the fail-fast `throw`s. Gate
      harmless placeholder config on the doc-tool entry assembly
      (`Assembly.GetEntryAssembly()?.GetName().Name is "dotnet-getdocument" or "GetDocument.Insider"`)
      so a real boot still requires the real values.
- [ ] **Acceptance:** a clean `dotnet build` writes the OpenAPI JSON (≈20 paths / 30 schemas), and
      the normal app run is unchanged.

### Phase 2 — Security in the contract (the one that matters most)
- [ ] Add an OpenAPI **document transformer** that declares the Bearer/JWT (Keycloak) security
      scheme and applies it as the global security requirement.
- [ ] **Acceptance:** the generated doc's `components.securitySchemes` contains `Bearer` and every
      operation requires it. *Verify:* `42c-ast audit` security score returns to 30/30 (without it,
      the audit sees an unauthenticated API).

### Phase 3 — Honest hardening (only what the code actually enforces)
- [ ] Document transformer: add the production **`servers`** entry (`https://companyops.toomhorvath.com`).
- [ ] Schema transformer: set **`additionalProperties:false`** on request bodies — now truthful,
      because the API rejects unknown fields (`UnmappedMemberHandling.Disallow`).
- [ ] Confirm enums emit as string + nullable-required reflects the code (already true).
- [ ] **Acceptance:** no audit finding claims a constraint the API doesn't enforce (the free-text
      `pattern` tension disappears — the code never declares patterns it doesn't enforce).

### Phase 4 — Completeness polish
- [ ] Add `maxItems` to list responses (document the real, intended bound) — **and** decide whether
      to add real **pagination** to the list endpoints (see `production-readiness.md` → Performance).
- [ ] Add the standard error responses to all operations: `default`, `429` (real rate-limiter),
      `415` (JSON-only), `406`. Prefer expressing these once via a convention/transformer, not 23×.
- [ ] **Acceptance:** 42Crunch data-validation score is at target with no synthetic constraints.

### Phase 5 — Make the generated doc the single source of truth
- [ ] Choose the canonical output path + name (e.g. emit to repo root as `openapi.json`, or keep
      `src/CompanyOps.Api/openapi/CompanyOps.Api.json` and reference it). Record the choice in ADR 0013.
- [ ] **Delete the hand-maintained `openapi.json`** once the generated one carries the hardening.
- [ ] Decide **commit vs. gitignore**: recommended → *commit* the generated doc so it's diffable and
      reviewed in every PR (the contract is visible), with the CI gate (Phase 7) keeping it honest.

### Phase 6 — Re-prove it's clean
- [ ] Run `42crunch-audit` against the generated contract; record the score in the PR.
- [ ] Run `42crunch-scan` against a non-prod instance (the local stack); confirm **0 critical / 0
      high**, no BOLA/BFLA (as in the prior session).
- [ ] **Acceptance:** audit ≥ 70 (target), scan shows no authorization findings.

### Phase 7 — CI drift gate (so it can never go stale)
- [ ] Add a CI step (extend `.github/workflows/ci.yml`): regenerate the doc and **fail the build if
      it differs** from the committed copy (`dotnet build` then `git diff --exit-code` on the doc).
- [ ] (Optional) Publish the contract as browsable docs (Scalar is already wired, dev-only) and/or
      run the 42Crunch audit in CI as a non-blocking report.
- [ ] **Acceptance:** a PR that changes an endpoint without updating the contract is rejected by CI.

## Risks & rollback

- **Risk:** a transformer uses a `Microsoft.OpenApi` API that differs across versions → caught at
  build time; iterate against the actual package. **Low** — additive, behind the build.
- **Risk:** `additionalProperties:false` rejects a field a real client sends → it's already the
  runtime behaviour, so the contract only *documents* reality; no behaviour change.
- **Rollback:** the changes are the transformer classes + csproj/CI lines. Reverting the PR restores
  the prior state; the hand-tuned `openapi.json` stays in git history until Phase 5 deletes it.

## Definition of done

Generated contract documents auth + servers + honest schema rules → hand-tuned file deleted →
42Crunch audit ≥ target and scan clean → CI fails on contract drift → ADR 0013 merged.
