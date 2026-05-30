# 8. External system integration — worker-driven, resilient, advance-and-flag

Date: 2026-05-30
Status: Accepted

## Context

CompanyOps must integrate with external enterprise systems: a **Finance** system
(commit/reserve budget when a request is approved) and an **Inventory** system
(reserve the asset when a request is fulfilled). Phase 6 mocks these with real HTTP
services (`FakeFinanceApi`, `FakeInventoryApi`) so the integration — and its failure
modes — is exercised for real, not stubbed in-process.

External calls fail: timeouts, 5xx, the remote being down. Two questions follow: (1)
*where* do we call them, and (2) what happens to the user's request when the call
fails after retries.

## Decision

**Call external systems asynchronously from the Worker (event-driven), advance the
request regardless, and record the integration outcome as audit — "advance and flag".**

1. **The state transition does not block on the external call.** Finance approval and
   fulfillment advance the `Request` and commit immediately. The external call is *not*
   a precondition — the API stays responsive even when a downstream system is down.

2. **The Worker performs the external call**, triggered by the integration event
   already produced via the outbox (Phase 5 / ADR 0007):
   - `RequestApproved` → Worker calls `FakeFinanceApi` (commit budget).
   - `RequestFulfilled` (new) → Worker calls `FakeInventoryApi` (reserve asset).

3. **The queue is the out-of-band retry.** Each call is wrapped in a resilience pipeline
   (`Microsoft.Extensions.Http.Resilience` / Polly: timeout + retry with backoff).
   If it still fails, the consumer nacks → the message requeues, bounded by the queue's
   delivery limit, then dead-letters (ADR 0007). No bespoke retry table.

4. **The outcome is recorded as audit** (the "flag"): `BudgetCommitted` /
   `BudgetCommitFailed`, `AssetReserved` / `AssetReservationFailed`. The audit trail is
   the queryable record of integration state; the `Request` aggregate is not polluted
   with integration status fields.

5. **The consumer is now idempotent.** Real side effects make at-least-once delivery
   matter (the deferred Phase 5 item): the Worker dedups on the outbox message id before
   acting, so a redelivered message does not double-commit budget or double-reserve.

## Options considered

- **Synchronous call inside the approval/fulfillment handler** (the literal "during
  approval/fulfillment" reading). Simple and obvious, but couples the user's request to
  external availability — a slow/120down Finance system stalls or fails approvals. The
  resilience patterns would protect a request that shouldn't depend on the call at all.
  Rejected in favour of decoupling.
- **Advance + flag, worker-driven** (chosen): the request succeeds immediately; the
  integration is a reliable background effect with retry/dead-letter and an audit record.
  Reuses the outbox/queue we already built. Cost: eventual consistency (budget is
  committed shortly *after* approval) and the consumer must be idempotent.
- **Bespoke "pending integration" table + custom retry poller.** More moving parts than
  the queue already gives us. Rejected.

## Consequences

**Positive**
- The API is resilient to external outages; approvals/fulfillments don't fail because
  Finance is down.
- Reuses the outbox + queue + dead-letter machinery for retry — little new infrastructure.
- Audit captures integration outcomes, extending the existing trail.

**Negative / costs**
- **Eventual consistency:** a request is `Approved` before the budget is confirmed
  committed. A persistent external failure surfaces as a `BudgetCommitFailed` audit +
  a dead-lettered message, needing operator follow-up (monitoring is Phase 10/11).
- The Worker now needs database access (audit + dedup) and the HTTP gateways — it is no
  longer broker-only.
- Idempotency is mandatory: a `processed_messages` dedup guard is introduced.

## Affects

- **Phase 6** — `FakeFinanceApi`/`FakeInventoryApi` mock service; `IFinanceGateway`/
  `IInventoryGateway` ports + resilient `HttpClient`s; `RequestFulfilled` event; Worker
  consumes both events, calls the gateways, dedups, and audits; compose gains the mock.
- **Phase 10/11** — dead-letter + integration-failure monitoring, gateway metrics,
  external-call auth/TLS.
