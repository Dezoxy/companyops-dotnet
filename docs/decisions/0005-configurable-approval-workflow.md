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

**Treat CompanyOps as one configurable approval-workflow engine, and build all
three use cases as first-class flows on top of it.** Procurement is the seed
example that proves the engine; helpdesk-light, asset lifecycle, and generic
internal approval are then implemented as real flows — each a configuration of the
same engine (its own request type + approval chain + fulfillment action), not a
fork or a separate subsystem.

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

The project is **positioned and documented as a workflow engine** that runs three
real internal processes (procurement/approval, helpdesk-light, asset management).
We do not rename it to a generic "ITSM platform" — the flows are bounded (see scope
guardrails) and that label would overclaim.

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

**Build order (all three are in scope; sequence keeps it sane)**
- Build the **engine + procurement flow first** (Phases 1–2) so the abstraction is
  validated against one real, complete flow before generalizing. Adding the engine
  after a hard-coded flow is far costlier than building it in from the start.
- Then add **helpdesk-light** and **asset lifecycle** as their own flows — each
  one is a new request type + approval-chain config + fulfillment handler + slice
  tests, reusing the engine, audit, and authz. Treat each as a vertical slice, not
  a parallel half-built subsystem (one working flow at a time).

**Scope guardrails (real flows, but bounded so they don't balloon into products)**
- Helpdesk stays **"light"**: request types + lifecycle + comments + audit. No ITSM
  queues, SLA timers, or escalation engine — those are enterprise-optional.
- Asset lifecycle covers the real states (in stock → assigned → in repair →
  retired) plus return/reclaim and asset history — but not full CMDB/discovery.
- "Generic internal approval" = the configurable chain itself; we don't build a
  no-code workflow *designer* UI. Request types are seeded/in-code for the MVP; a
  DB-backed admin editor is enterprise-optional.

## Affects

- **Phase 1–2** — design `RequestType` and the configurable `ApprovalStep`
  sequence *before* implementing the state machine. This ADR must be reflected in
  the Domain model from the start.
- **docs/security.md** — the authorization matrix becomes "per step, per request
  type"; the dept-scoped Manager invariant still holds.
