# Application layer — rules

Depends on **Domain only**. This is where use cases live.

Allowed here:
- Commands and queries + their handlers (one use case per slice).
- Validators (FluentValidation) at this boundary — validate all input here.
- **Interfaces (ports)** that Infrastructure implements: repositories,
  `IUnitOfWork`, `IAuditLogger`, `IClock`, external-service clients,
  message publishers. Define the contract here; never the implementation.
- DTOs for input/output of use cases.

**Forbidden here:**
- No concrete EF Core, HTTP, RabbitMQ, Redis, or Keycloak code — those are
  Infrastructure. If you're newing up a `DbContext` or `HttpClient`, stop.
- No ASP.NET Core / controller types.
- No reference to Infrastructure or Api.

## Conventions for CompanyOps

- One **vertical slice per business action** (`CreateRequest`, `SubmitRequest`,
  `ApproveRequest`, `RejectRequest`, `FulfillRequest`) — one step-driven `approve`/`reject`,
  not role-named handlers (ADR 0006). Keep command + validator + handler together.
- Handlers orchestrate: load aggregate via a repository port → call the domain
  method (which enforces the rule) → persist → record the audit entry → return.
- Authorization intent is expressed here (which role/policy a use case requires);
  enforcement is wired in Api. Business invariants stay in Domain.
- Async with `CancellationToken` on every handler.
