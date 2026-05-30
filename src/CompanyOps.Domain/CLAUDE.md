# Domain layer — rules

This is the core. **It depends on nothing.**

Allowed here: entities, value objects, enums, domain events, domain exceptions,
and the business rules that operate on them — pure C# only.

**Forbidden here:**
- No EF Core (`DbContext`, `[Table]`, `DbSet`, migrations).
- No ASP.NET Core, no HTTP, no controllers.
- No I/O: no database, file, network, queue, or external API calls.
- No references to Application, Infrastructure, or Api.
- No third-party packages unless they're pure (no infrastructure deps).

If you need data from outside, the domain doesn't fetch it — it receives it.

## What belongs here for CompanyOps

- `Request` aggregate and its **state machine**. Invalid transitions
  (e.g. `Draft → Completed`) must **throw a domain exception**, not return false.
  The overall path: `Draft → Submitted → Approved → InFulfillment → Completed`,
  with `Rejected` / `Cancelled` as terminal branches. The status is chain-agnostic
  (ADR 0006): *which* approvers signed off lives per-step in `ApprovalStep`, and the
  chain itself is data-driven per `RequestType` (ADR 0005), not a fixed enum path.
- Entities: `User`, `Department`, `CostCenter`, `Request`, `RequestItem`,
  `ApprovalStep`, `Asset`, `AssetAssignment`, `AuditLog`, `Notification`.
- `RequestStatus` enum, role concepts, and the rules that enforce who-can-do-what
  at the domain level (the API layer also enforces authorization, but invariants
  live here).
- Raise **domain events** (e.g. `RequestManagerApproved`) for things the worker /
  audit log will react to — don't call infrastructure from the domain.
