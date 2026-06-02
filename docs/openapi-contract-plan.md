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
- [x] Write **ADR 0013 — code-generated API contract** ([decisions/0013-code-generated-api-contract.md](decisions/0013-code-generated-api-contract.md)):
      records the move from a hand-maintained spec to a build-time-generated + CI-gated contract,
      the alternatives, and approvers. (Phase 5 PR)
- [x] Link this plan from `docs/future-improvements.md` and from the API-layer notes. (#77)

### Phase 1 — Enable build-time generation ✅ (#79)
- [x] Re-add the `Microsoft.Extensions.ApiDescription.Server` package + `<OpenApiDocumentsDirectory>`
      to `src/CompanyOps.Api/CompanyOps.Api.csproj` so `dotnet build` emits the document.
- [x] Handle the **design-time gotcha** (solved): the generator runs `Program` up to `app.Run()`
      with no DB/Keycloak/RabbitMQ config, tripping the fail-fast `throw`s. Gate harmless placeholder
      config on the doc-tool entry assembly (`BuildTimeOpenApi.IsGenerating`) so a real boot still
      requires the real values.
- [x] **Acceptance:** a clean `dotnet build` writes the OpenAPI JSON (20 paths / 30 schemas); the
      normal app run is unchanged. Output → `artifacts/openapi/` (NOT a source folder — `openapi`
      collides with the `OpenApi/` namespace folder on case-insensitive filesystems).

### Phase 2 — Security in the contract (the one that matters most) ✅ (#79)
- [x] Add an OpenAPI **document transformer** that declares the Bearer/JWT (Keycloak) security
      scheme and applies it as the global security requirement.
- [x] **Acceptance:** the generated doc's `components.securitySchemes` contains `Bearer` and every
      operation requires it; `42c-ast audit` security score is **30/30**. (Servers folded in here —
      the score depends on an https server; gated to build-time only so the dev `/openapi` stays
      relative to localhost.)

### Phase 3 — Honest hardening (only what the code actually enforces) ✅ (this PR)
- [x] Document transformer: production **`servers`** entry (`https://companyops.toomhorvath.com`) — done in Phase 2.
- [x] Schema transformer: set **`additionalProperties:false`** on request/response objects — truthful,
      because the API rejects unknown request fields (`UnmappedMemberHandling.Disallow`) and returns
      exactly the declared DTO shape. RFC 7807 `ProblemDetails` is left open (extensible).
- [x] Confirm enums emit as string + nullable-required reflects the code (verified — already true).
- [x] **Acceptance:** no audit finding claims a constraint the API doesn't enforce; the only
      remaining `additionalProperties` finding is the deliberate `ProblemDetails` exclusion. Data
      score 4.17 → 8.89 (the rest is Phase 4). No synthetic free-text patterns added.

### Phase 4 — Completeness polish ✅ honest scope (this PR)
- [x] Standard error responses via a document transformer: `default`, `429` (real rate-limiter) on
      every operation; `415` (JSON-only) on body-bearing operations. **`406` skipped** — the API
      doesn't do content negotiation today (would need `ReturnHttpNotAcceptable`); claiming it
      would be dishonest.
- [x] Honest string constraints via a schema transformer: `uuid`/`date-time` get `pattern` +
      `maxLength`; free-text fields get the `maxLength` the Domain enforces (title/description/tag/
      name/body). **No free-text `pattern`s** (synthetic) and **no response `maxLength`** the code
      doesn't enforce.
- [ ] `maxItems` on list responses — **deferred**: only honest once the lists are paginated. Tracked
      as its own slice in [production-readiness.md](production-readiness.md) → Performance.
- [x] **Acceptance:** 42Crunch data score reached the **70 target honestly — 77.52/100** (security
      30/30, data 47.52/70), **with zero synthetic constraints**. Remaining findings are all genuine
      "the code doesn't enforce this" items (free-text patterns, response maxLengths, `maxItems`).

### Phase 5 — Make the generated doc the single source of truth ✅ (this PR)
- [x] Canonical location chosen: **repo-root `openapi.json`** (the conventional name the `.42c` scan
      config already references). The build emits to a gitignored `artifacts/openapi/` intermediate,
      then the `CopyOpenApiToRoot` target publishes it to `openapi.json`. Recorded in ADR 0013.
- [x] **Deleted the hand-maintained `openapi.json`** — the generated, hardened doc takes its place
      (same path/name, so the scan config keeps working).
- [x] **Committed** the generated doc (un-gitignored) so it's diffable/reviewed in every PR; the
      Phase 7 CI gate keeps it honest.

### Phase 6 — Re-prove it's clean ✅ (this PR)
- [x] **Fix surfaced by re-proving:** the generated contract had **null `operationId`s** (AddOpenApi
      doesn't emit them for controllers) — so it was un-scannable and bad for client codegen. Added
      `OperationIdTransformer` (stable ids by route). The committed `openapi.json` now has all 23.
- [x] `42crunch-audit` against the committed contract: **77.52/100** (security **30/30**, data 47.52/70).
- [x] `42crunch-scan` against the local stack: **0 critical / 0 high**; **BFLA re-confirmed denied
      (403)**, no vertical escalation. The API's authorization code is unchanged from the prior
      session that proved **both BOLA (404) and BFLA (403) denied**.
- [ ] **Follow-up (not blocking):** a *clean full BOLA re-execution* needs the scan config reconciled
      to the new 3.1 contract — the stricter contract surfaces pre-existing 401/403 content-type
      conformance noise (empty body vs declared `problem+json`) that blocks happy-path-dependent
      authz tests. No authorization regression; tracked for when the scan config is regenerated.
- [x] **Acceptance:** audit ≥ 70 ✅; scan shows no authorization findings ✅ (no regression).

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
