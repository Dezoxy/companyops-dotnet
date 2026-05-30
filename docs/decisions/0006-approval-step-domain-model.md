# 6. Approval-step domain model — how the configurable chain is built in code

Date: 2026-05-30
Status: Accepted

## Context

[ADR 0005](0005-configurable-approval-workflow.md) decided *that* the approval
chain is configurable and keyed by `RequestType`, and *that* the `Request` state
machine is driven by the configured steps. It deliberately left the concrete
domain model open. Phase 2 ("Workflow and business logic") now has to build that
model, so this ADR pins the concrete shapes before any state-machine code is
written — exactly the "design before the state machine" sequencing ADR 0005 and
the project plan call for.

Two forces shape the decisions below:

- **Phase 1 shipped a procurement-shaped `RequestStatus`** (`Draft`, `Submitted`,
  `ManagerApproved`, `FinanceApproved`, `Rejected`, `InFulfillment`, `Completed`,
  `Cancelled`). The `ManagerApproved`/`FinanceApproved` values hard-code one
  chain's shape into the overall status — directly at odds with ADR 0005. Only
  `Draft` rows exist so far, so this is cheap to correct now and expensive later.
- **Auth (users, roles, departments, JWT) does not land until Phase 3.** The
  department-scoped Manager invariant that ADR 0005 requires must still live in the
  Domain "from the start," so Phase 2 needs a way to enforce it before there is an
  authenticated principal to read the actor from.

## Decision

### 1. Refactor `RequestStatus` to be chain-agnostic

Replace the procurement-specific values with a status set that describes the
*overall lifecycle*, not any one chain:

```
Draft → Submitted → Approved → InFulfillment → Completed
                  ↘ Rejected (terminal)
   (Draft/Submitted/Approved) ↘ Cancelled (terminal)
```

Per-step progress (who approved which step, when) lives in `ApprovalStep`, **not**
in the overall status. "All required steps satisfied" is what flips `Submitted →
Approved`; the status no longer names individual approvers. This reverses the
Phase 1 enum and requires a migration (safe — only `Draft` data exists).

### 2. The domain model

All pure C# in `CompanyOps.Domain`, no infrastructure dependencies.

| Type | Kind | Purpose |
|---|---|---|
| `ApproverRole` | enum | `Manager`, `Finance`, `ItAdmin` — who can satisfy a step |
| `ApprovalScope` | enum | `Department`, `Global` — how the approver is matched |
| `ApprovalDecision` | enum | `Pending`, `Approved`, `Rejected` — a step's outcome |
| `ApprovalStepDefinition` | value object | the **template**: `Order`, `RequiredRole`, `Scope`, `IsRequired` |
| `ApprovalChains` | seeded in-code registry | `RequestType → ordered ApprovalStepDefinition[]` |
| `ApprovalStep` | entity (owned by `Request`) | the **instance** of a step on one request: definition fields + `Decision`, `DecidedById?`, `DecidedAtUtc?`, `Note?` |

Seed chain (Procurement, the seed flow):
`Procurement → [ Manager(Department, required), Finance(Global, required) ]`.
Helpdesk and asset-lifecycle chains are added with their flows (Phases 13–14).

`Request` gains `DepartmentId` (the owning department) and an ordered private
`ApprovalSteps` collection, **materialized from `ApprovalChains` at `Submit`** —
the chain is fixed for a request at submit time, so later config changes don't
mutate in-flight requests.

### 3. Transitions are computed, and illegal moves throw

The state machine derives behaviour from the steps rather than a fixed enum path:

- **"Approved"** = every `IsRequired` step has `Decision == Approved`.
- **Next actor** = the first step whose `Decision == Pending` (and `IsRequired`).
- `Submit`, `Approve`, `Reject`, `Fulfill` are methods on the `Request` aggregate.
  Any illegal transition (approving a `Draft`, approving an already-decided step,
  wrong role, wrong department, fulfilling an unapproved request) **throws
  `DomainException`** — never returns a bool or silently no-ops.

### 4. Approver identity and department scope before auth (Phase 2 bridge)

Until Phase 3 introduces the `User`/`Department` aggregates and JWT, the
`Approve`/`Reject` commands carry the approver's `{ id, role, departmentId }`
explicitly — the **same temporary pattern** Phase 1 uses for `RequesterId`. The
Domain enforces the real invariant now: the actor's role must match the current
step's `RequiredRole`, and when `Scope == Department` the actor's `departmentId`
must equal the request's `DepartmentId`. **Phase 3 changes only the *source* of
that identity** (request body → authenticated principal); the domain rule is
already in place and does not move.

### 5. API: one step-driven action, not role-named endpoints

Expose `POST /requests/{id}/approve` and `POST /requests/{id}/reject` (plus
`/submit` and `/fulfill`). The actor's role selects *which* step is being decided —
there is no `/approve-manager` vs `/approve-finance`. The project plan's "add
manager approval / add finance approval" are realised as **chain configuration
exercised through the one generic action**, which is the ADR 0005 model. This is
still a business action, not CRUD.

### 6. `ApprovalStep` is persisted as an owned entity, not a JSON column

Steps go in their own `approval_steps` table (owned-type / aggregate-child mapping
under `Request`), not serialized into a column on `requests`. Relational rows are
queryable ("show everything awaiting Finance"), and give Phase 4 a clean,
row-level audit seam. The cost is one extra table and an EF owned-collection
mapping — accepted.

## Consequences

**Positive**
- The procurement shape is gone from the overall status; helpdesk and asset-
  lifecycle flows become a new `ApprovalChains` entry + fulfillment handler.
- The dept-scoped Manager invariant is enforced in the Domain in Phase 2, so
  Phase 3 is a source swap, not a rule rewrite.
- Owned-entity steps make per-step queries and Phase 4 audit straightforward.

**Negative / costs**
- More upfront Domain modeling than a fixed enum chain — accepted, per ADR 0005.
- A migration is required (drop the two procurement enum values, add
  `Request.DepartmentId`, add the `approval_steps` table). Safe now (only `Draft`
  rows); the same change after real data would need backfill.
- The commands temporarily carry approver identity, which looks like trusting the
  client. This is the documented Phase 1/2 bridge and is removed in Phase 3 — the
  endpoints must not ship publicly before then.

## Affects

- **Phase 2** — implemented as TDD vertical slices: `Submit` → `Approve` →
  `Reject` → `Fulfill`, one migration for `DepartmentId` + `approval_steps` + the
  enum change.
- **docs/security.md** — the authorization matrix becomes per-step, per-request-
  type; the dept-scoped Manager invariant is now realised by `ApprovalScope.Department`.
- **Phase 3** — swaps the approver source from the command to the authenticated
  principal; enforces the same role/scope at the API boundary too.
- **Phase 4** — audit-logs each step decision (who / what / when / old→new) off the
  `ApprovalStep` rows.
