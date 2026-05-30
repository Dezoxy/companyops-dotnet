---
name: new-slice
description: Scaffold a backend vertical slice for one CompanyOps business action (command) or read (query) across all four layers in the project's conventions — domain method, Application command/query + handler + DI registration, a business-action API endpoint, and tests. Use when adding a backend use case such as submit/approve-manager/approve-finance/reject/fulfill/cancel a request, or a new read query.
---

# Scaffold a backend vertical slice (CompanyOps)

One slice = **one business action** (or one read). Generate the files across all
four layers so the developer fills in *logic*, not *plumbing*. This is the backend
counterpart to `new-angular-feature`.

**Read first:** `AGENTS.md` (conventions + non-negotiables) and the per-layer
`src/CompanyOps.*/CLAUDE.md`. Use the merged Phase 1 `CreateRequest` slice as the
reference shape — match its style, XML-doc tone, and file layout exactly.

## Inputs to confirm

- **Slice kind** — **command** (changes state / a business action) or **query** (read-only).
- **Action name** — PascalCase use-case name, not CRUD: `SubmitRequest`,
  `ApproveRequestByManager`, `ApproveRequestByFinance`, `RejectRequest`,
  `FulfillRequest`, `CancelRequest`; queries like `GetRequestById`, `ListRequests`.
- **Aggregate + transition** (commands) — which status → status move it performs,
  and the invariant that makes it illegal (e.g. can't approve a `Draft`; only the
  owning department's manager). The valid path is in `src/CompanyOps.Domain/CLAUDE.md`.
- **Endpoint** — the business-action route, e.g. `POST /requests/{id}/submit`.
- **Authorization intent** — which role/policy the use case requires (Employee /
  Manager / Finance / IT Admin / Auditor). Expressed in Application; *enforced* in Api.

## What to generate

### Command slice (business action)

1. **Domain** — `src/CompanyOps.Domain/Requests/Request.cs`: add the transition
   method (e.g. `public void Submit(DateTimeOffset nowUtc)`). It **validates the
   current status and throws `DomainException` on an invalid transition** — never
   returns a bool, never silently no-ops. Mutates state through the private setters.
   From Phase 2 this raises a domain event (e.g. `RequestSubmitted`) for the worker /
   audit log to react to — don't call infrastructure from the domain.
2. **Application** — `src/CompanyOps.Application/Requests/<Action>/`:
   - `<Action>Command.cs` — `sealed record` of the inputs. The target id + any
     actor id. (Actor comes from the request until Phase 3, then from the principal.)
   - `<Action>Handler.cs` — `sealed class`, primary-constructor DI. Orchestrate:
     load aggregate via the repository port → call the domain method (rule lives
     there) → `SaveChangesAsync` → **record the audit entry (Phase 4+)** → return a
     `RequestDto`. Inject `TimeProvider` for "now". `async` + `CancellationToken`.
   - Register the handler in `src/CompanyOps.Application/DependencyInjection.cs`
     (`services.AddScoped<<Action>Handler>();`).
3. **Api** — add the action to `RequestsController` (or the relevant controller):
   thin — bind DTO → build command → dispatch via `[FromServices]` handler → map to
   HTTP. Add `[ProducesResponseType]` for success + 400/404. The route is a
   **business action** (`POST /requests/{id}/submit`), not a generic update.
4. **Tests** — see "Tests" below: a Domain unit test for the transition (happy path
   + the throw) and a handler test.

### Query slice (read)

1. **Application** — `src/CompanyOps.Application/Requests/<Query>/`:
   `<Query>Query.cs` (record of filter inputs) + `<Query>Handler.cs` returning
   `RequestDto` / `IReadOnlyList<RequestDto>` via the repository port. Reads use
   `AsNoTracking()` in the repo. Register the handler in `DependencyInjection.cs`.
2. **Api** — a `GET` endpoint mapping to the handler; `null` → `NotFound()`.
3. No domain method, no audit entry (reads don't mutate).

### Ports

If the slice needs data access the repo doesn't expose yet, add the method to
`IRequestRepository` (Application, `Abstractions/`) and implement it in
`RequestRepository` (Infrastructure). **Never** touch `DbContext` from a handler.

## Phase-awareness (do NOT scaffold ahead)

- **No MediatR.** Handlers are plain injectable classes, registered explicitly.
  Note in a comment where a mediator pipeline would later live; don't add it.
- **No FluentValidation yet.** Input shape is validated by the domain factory /
  transition (which throws). Add a `<Action>Validator.cs` (FluentValidation) **only
  once the project has adopted it** — until then, don't.
- **Audit logging from Phase 4.** Before Phase 4, leave a `// TODO(Phase 4): audit`
  marker at the persist step — do not invent an `IAuditLogger` call that maps to
  nothing. From Phase 4 on, every state change records who / what / when / old→new.
- **Auth from Phase 3.** Until then the actor id arrives in the request DTO
  (documented as temporary, as in Phase 1). From Phase 3 it comes from the principal
  and is removed from the body; enforcement (role + department scope) is wired in Api.

## Rules (do not violate)

- Dependencies point **inward only**. No EF/HTTP/queue types in Domain or
  Application. No business rules in the controller or the handler — the rule lives
  in the Domain method.
- **Business actions, not CRUD.** The endpoint and the command name a domain
  operation.
- **Invalid transitions throw in Domain**, not just guarded in Api/UI.
- EF entities are not API contracts — map to/from DTOs (`RequestDto.FromDomain`).
- Auditor role is read-only; never give it a mutating slice.

## Tests

Integration tests use **Testcontainers against real Postgres** (not in-memory);
domain tests are plain xUnit. AAA, names `Method_Scenario_ExpectedResult`.

If no test project exists yet (true through early Phase 1), bootstrap it **once**:
create `tests/CompanyOps.Domain.Tests` (and `tests/CompanyOps.Application.Tests`
when a handler test needs the DB) as xUnit projects, reference the projects under
test, and add them to `CompanyOps.slnx`. Mention this to the user before creating
projects — it's a structural change, not part of the slice.

Per slice, add at minimum:
- Domain: `Request_<Action>_<Scenario>_<Expected>` — the happy transition **and**
  the `DomainException` on an illegal one.
- Handler: the orchestration (loads, calls domain, persists, returns DTO).

## After scaffolding

- `dotnet build` clean → `dotnet test` green → `dotnet format` clean.
- Run the **architecture-guardian** subagent on the diff (layer-rule check).
- From Phase 3: confirm the new endpoint is covered by the authorization matrix in
  `docs/security.md`.
