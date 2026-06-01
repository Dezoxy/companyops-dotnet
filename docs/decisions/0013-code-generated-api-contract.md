# 13. Code-generated, committed API contract (retire the hand-maintained spec)

Date: 2026-06-01
Status: Accepted
Approvers: architecture owner, security owner (the solo maintainer wears both hats — see AGENTS.md).

## Context

The API contract (`openapi.json`) was **hand-written**. A hand-maintained spec drifts from the code
the moment an endpoint or DTO changes and nobody updates the file — it silently lies to consumers
(the SPA, customer integrators, the 42Crunch audit/scan). The hand-tuned file also carried
42Crunch-friendly hardening (security scheme, servers, error responses, schema constraints) that
existed **only in the file**, not in the code — so it was both authoritative and untrustworthy.

.NET 10 ships first-class OpenAPI generation (`Microsoft.AspNetCore.OpenApi`) plus build-time
emission (`Microsoft.Extensions.ApiDescription.Server`). The hardening that mattered can be
expressed in code via document/schema transformers, so the generated document can be made as good
as the hand-tuned one — and always accurate.

See the execution detail in [openapi-contract-plan.md](../openapi-contract-plan.md).

## Decision

1. **The API contract is generated from the code at build time**, not hand-written. `dotnet build`
   emits the document (no running server) and publishes it to the canonical, **committed** repo-root
   `openapi.json`.
2. **The hardening lives in code**, as `IOpenApiDocumentTransformer` / `IOpenApiSchemaTransformer`
   (`src/CompanyOps.Api/OpenApi/`): Bearer security scheme + global requirement, the HTTPS production
   server (build-time only — the dev `/openapi` stays relative to localhost), `additionalProperties:
   false`, standard error responses, and the string constraints the code actually enforces.
3. **Honesty over score.** The contract documents only what the API enforces. Synthetic free-text
   `pattern`s and `maxItems` on un-paginated lists are deliberately omitted — claiming them would be
   a contract that lies. This costs some audit points and is the correct trade-off.
4. **The committed contract is gated in CI** (Phase 7): the build regenerates `openapi.json` and the
   pipeline fails if it differs from the committed copy, so the contract can never drift again.
5. **The hand-maintained `openapi.json` is retired** (deleted); the generated file takes its name and
   path so existing tooling (the 42Crunch `.42c` scan config) keeps working.

## Alternatives considered

- **Keep the hand-maintained spec.** Rejected — it drifts and there is no enforcement that it
  matches the code.
- **Keep both (generated for accuracy, hand-tuned for the audit).** Rejected — two sources of truth,
  the hand-tuned one still drifts, and reviewers can't tell which is authoritative.

## Consequences

- The contract is always accurate; reviewers see the contract diff in every PR that changes the API.
- Some hardening is now C# (transformers) rather than a static file — a small, well-contained amount
  of code, exercised by the build and an integration test.
- The audit score is lower than the hand-tuned file achieved, by exactly the synthetic constraints we
  refuse to fake (free-text patterns, `maxItems` without pagination). Pagination is tracked
  separately ([production-readiness.md](../production-readiness.md) → Performance); once it lands,
  `maxItems` becomes honest to add.
- Build-time generation requires the host to construct without live infrastructure; handled by
  placeholder config gated on the doc-tool entry assembly (`BuildTimeOpenApi.IsGenerating`).

## Rollback

Revert the transformer classes + the csproj emission/copy target + the CI gate. The generated
`openapi.json` stays in git history. No runtime behaviour depends on the generation.
