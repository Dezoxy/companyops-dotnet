---
name: new-slice
description: Scaffold a backend vertical slice for one CompanyOps business action (command) or read (query) across all four layers in the project's conventions — domain transition, Application command/query + handler (audit + outbox event) + DI registration, an authorized business-action endpoint, and tests. Use when adding a backend use case such as submit/approve/reject/fulfill/cancel a request, or a new read query.
---

# Scaffold a backend vertical slice (CompanyOps)

One slice = **one business action** (or one read). Generate the files across all
four layers so the developer fills in *logic*, not *plumbing*. This is the backend
counterpart to `new-angular-feature`.

**Read first:** `AGENTS.md` (conventions + non-negotiables) and the per-layer
`src/CompanyOps.*/CLAUDE.md`. **Reference shapes:** the `CreateRequest` slice (the
simplest end-to-end) and the `ApproveRequest` slice (shows audit + an outbox event on
a transition) — match their style, XML-doc tone, and file layout.

**Read `ACTIVE_PHASE` (repo root) first** — a single integer, the current phase. Never
scaffold a feature whose phase is greater than that value (see the phase-feature table
in `AGENTS.md`); if a slice needs a later-phase concern, stop and flag it.

## Inputs to confirm

- **Slice kind** — **command** (changes state) or **query** (read-only).
- **Action name** — PascalCase, not CRUD: `SubmitRequest`, `ApproveRequest`,
  `RejectRequest`, `FulfillRequest`, `CancelRequest`; queries like `GetRequestById`.
- **Aggregate + transition** (commands) — which status → status move, and the invariant
  that makes it illegal. The valid path is in `src/CompanyOps.Domain/CLAUDE.md`.
- **Authorization** — which role policy gates the endpoint, and any resource scope
  (department / own) the Domain must enforce. Source of truth: `docs/security.md`.
- **Async follow-on?** — does the transition need out-of-band work (notify, call an
  external system)? If so it emits an integration event the Worker handles.

## What to generate — command slice

1. **Domain** — `Request.cs`: add the transition method (e.g. `Submit(Guid actorId,
   DateTimeOffset nowUtc)`). It **validates the current status + the actor's eligibility
   (role, department scope, ownership) and throws `DomainException` on any illegal move**
   — never returns a bool, never silently no-ops. Mutate via the private setters. Keep
   it pure: no infrastructure, no I/O.
2. **Application** — `src/CompanyOps.Application/Requests/<Action>/`:
   - `<Action>Command.cs` — `sealed record` of inputs: the target id + the actor
     context (`ActorId`, and `ApproverRole`/`ApproverDepartmentId` for decisions). The
     Api fills these **from the authenticated principal**, not the request body.
   - `<Action>Handler.cs` — `sealed class`, primary-constructor DI. Orchestrate, in order:
     load aggregate via `GetForUpdateAsync` → capture `fromStatus` → call the domain
     method → `auditLogger.Add(AuditLog.ForRequest(...))` → **if the transition produces
     an integration event**, `eventPublisher.Enqueue(new <Event>(...))` → a single
     `unitOfWork.SaveChangesAsync` (state + audit + outbox commit atomically). Return
     `RequestDto`; `null` when the aggregate isn't found. Inject `TimeProvider`.
   - Register the handler in `DependencyInjection.cs`.
3. **Api** — add the action to `RequestsController`: `[Authorize(Policy = Policies.…)]`,
   bind the body (note/reason only — never identity), build the command from
   `User.GetUserId()` / `GetDepartmentId()` / `GetApproverRole()`, dispatch, map
   `null → NotFound()`. `[ProducesResponseType]` for 200/400/401/403/404. A **business
   action** route (`POST /requests/{id}/submit`), not a generic update.
4. **Worker (only if it emits an event)** — handle the new event type in
   `IntegrationEventProcessor`: it is **idempotent** (dedup on the message id), calls
   any external gateway through its port, records the outcome as an audit entry, and
   marks the message processed — all in one transaction. Bind the event's routing key
   in `MessagingTopology`.
5. **Tests** — domain unit tests (happy transition + each illegal-move throw) and, where
   it earns it, an integration test (see "Tests").

## What to generate — query slice

- **Application** `<Query>/`: `<Query>Query.cs` + `<Query>Handler.cs` returning DTOs via
  a read port (`AsNoTracking()` in the repo). Register the handler.
- **Api** — a `GET` endpoint, `[Authorize]` (role per the matrix); `null → NotFound()`.
- No domain method, no audit, no event (reads don't mutate).

## Ports

If the slice needs data the repo doesn't expose, add the method to the port in
`Application/Abstractions/` and implement it in Infrastructure. **Never** touch
`DbContext` from a handler.

## Conventions in force (current phase)

- **Auth (Phase 3):** identity comes from the validated JWT principal, never the body.
  Endpoints are deny-by-default with a role policy; resource scope (department) + stage
  + ownership are Domain invariants (defense in depth).
- **Audit (Phase 4):** every state change records who / what / when / old→new via
  `IAuditLogger`, enlisted in the same transaction. No state change without it.
- **Events (Phase 5–6):** cross-process effects go through the **outbox**
  (`IIntegrationEventPublisher.Enqueue`, committed with the change); the Worker consumes
  idempotently. The API never calls a broker or external system inline.
- **Still NOT adopted:** **MediatR** (handlers are plain injectable classes, registered
  explicitly) and **FluentValidation** (input is validated by the domain factory /
  transition, which throws → mapped to 400 by `DomainExceptionHandler`). Don't add
  either unless the project has adopted it.

## Rules (do not violate)

- Dependencies point **inward only**. No EF/HTTP/broker types in Domain or Application.
  Business rules live in the Domain method, not the controller or handler.
- **Business actions, not CRUD.** Invalid transitions **throw in Domain**.
- EF entities are not API contracts — map to/from DTOs (`*.FromDomain`).
- Auditor is read-only — never give it a mutating slice.

## Tests

Domain tests are plain xUnit (AAA, `Method_Scenario_ExpectedResult`). Integration tests
live in `tests/CompanyOps.Api.IntegrationTests` and run the real API behind real Keycloak
JWTs against real Postgres (Testcontainers) — extend them for an authorization-sensitive
or event-driven slice (assert 401/403/400 + the happy path, and any audit/round-trip).

Per slice, at minimum:
- Domain: the happy transition **and** the `DomainException` on each illegal one.
- An integration assertion when the slice changes the authz matrix or emits an event.

## After scaffolding

- `dotnet build` clean → `dotnet test` green → `dotnet format` clean.
- Run **architecture-guardian** on the diff; run **security-guardian** if it touches
  auth, endpoints, or data access. Update `docs/security.md` if the matrix changed.
