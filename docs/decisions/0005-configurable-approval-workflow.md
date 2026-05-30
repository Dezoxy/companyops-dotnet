# 5. Configurable approval workflow — one engine, multiple processes

Date: 2026-05-30
Status: Accepted

## Context

CompanyOps started as a procurement & asset-management system with a fixed
approval chain: **Employee → Manager → Finance → IT fulfillment**. We want the
same project to credibly serve three related internal use cases without becoming
three systems:

1. **IT request / helpdesk-light** — service/access requests fulfilled by IT.
2. **Asset management** — assets and their assignment lifecycle.
3. **Internal approval** — generic multi-step sign-off.

These are not three domains; they are one **request → approval → fulfillment**
lifecycle with different approval chains and different fulfillment actions. The
risk is hard-coding the procurement-shaped chain (manager → finance) so deeply
that the other two can't reuse it.

## Decision

**Treat CompanyOps as one configurable approval-workflow engine. Procurement is
the fully-built reference flow; the other use cases are configurations, not
forks.**

Two concrete commitments:

1. **The approval chain is data/config-driven, keyed by request type** — not a
   hard-coded manager→finance sequence. A request type defines an ordered list of
   approval steps (each step: which role/policy approves, scope such as
   department, and whether it is required). Examples:
   - `Procurement` → ManagerApproval (dept) → FinanceApproval
   - `Helpdesk` → ManagerApproval (dept) *(or none for low-risk)*
   - `AccessRequest` → ManagerApproval (dept) → SystemOwnerApproval

2. **The `Request` state machine is driven by the configured steps**, not by a
   fixed enum path. "Approved" means "all required steps satisfied"; the next
   actor is derived from the first unsatisfied step. Invalid transitions are still
   rejected in the Domain (throw).

The project is **positioned and documented as a workflow engine** running
procurement today, extensible to helpdesk/asset/access flows. We do not rename it
to a generic "ITSM platform" — that would overclaim what is built.

## Consequences

**Positive**
- One engine, one audit trail, one authorization model serves all three uses.
- Stronger portfolio narrative: "reusable approval pipeline," not "CRUD form."
- Adding a new internal process becomes configuration + a fulfillment handler,
  not a new subsystem.

**Negative / costs**
- More upfront design in Domain: `RequestType`, an ordered `ApprovalStep`
  definition, and step-driven transition logic instead of a simple enum chain.
  This is more complex than the original fixed path — accepted deliberately,
  because retrofitting configurability after the state machine is built is worse.
- The configuration source (seeded code vs. DB-backed admin) is itself a later
  decision — start with **seeded/in-code request-type definitions**; a DB-backed
  editor is enterprise-optional and out of MVP scope.

**Scope guardrails (so this doesn't balloon)**
- Procurement remains the only *fully* implemented flow for the MVP. Helpdesk /
  asset-lifecycle / access flows are demonstrated as configurations once the engine
  exists, not built as parallel feature sets up front.
- Helpdesk stays "light": request types + lifecycle + audit. No ITSM queues,
  SLAs, or escalation engine.
- Full asset lifecycle (in stock → assigned → in repair → retired, return/reclaim)
  is a follow-on once `Asset`/`AssetAssignment` exist — not part of the first slice.

## Affects

- **Phase 1–2** — design `RequestType` and the configurable `ApprovalStep`
  sequence *before* implementing the state machine. This ADR must be reflected in
  the Domain model from the start.
- **docs/security.md** — the authorization matrix becomes "per step, per request
  type"; the dept-scoped Manager invariant still holds.
