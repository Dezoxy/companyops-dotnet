# CompanyOps — Testing strategy

How we test, where each layer is tested, and what runs in CI. The guiding rule:
**push assertions to the cheapest layer that can make them**, and reserve the slow,
real-dependency tests for the wiring those cheap tests can't see.

## The pyramid

```
        integration (Testcontainers + WebApplicationFactory)   few, slow, real deps
      ────────────────────────────────────────────────────
    application handler tests (fake ports)                     some, fast
  ──────────────────────────────────────────────────────
domain unit tests (pure)                                       many, fastest
```

| Layer | Project | Tests | Why here |
|---|---|---|---|
| **Domain** | `CompanyOps.Domain.Tests` | Aggregate invariants (`Request.Create`), the **step-driven approval state machine** (every legal transition + every illegal one throws), `AuditLog` factory rules | Pure C#, no I/O — the business rules are the most important thing to test and the cheapest to test exhaustively. Every `DomainException` path is pinned here. |
| **Application** | `CompanyOps.Application.Tests` | Handler **orchestration** with hand-written fake ports (no DB): audit entry recorded per state change, integration event enqueued on the *right* transition (and not on intermediate ones), `null`-when-not-found, actor threaded from the command | The handler's job is orchestration, not rules. Fakes make "did it enqueue / audit / save" assertions in milliseconds, without a database. |
| **Integration** | `CompanyOps.Api.IntegrationTests` | The real API behind real Keycloak JWTs against real Postgres, RabbitMQ, and the FakeExternals service (Testcontainers + `WebApplicationFactory`): authorization matrix, audit trail, the outbox→relay→RabbitMQ→worker round-trip, the worker→FakeExternals→audit round-trip, and the resilience paths | Confidence in the *wiring* the lower layers can't see: auth, EF mapping/migrations, message delivery, HTTP gateways, real serialization. |

## Conventions

- **AAA** (Arrange / Act / Assert); names read `Method_Scenario_ExpectedResult`.
- **Real dependencies via Testcontainers — never in-memory EF.** Integration tests run
  against the same engines as production-shaped local dev (Postgres 18, Keycloak 26,
  RabbitMQ 4); the FakeExternals mock is hosted and called over real HTTP.
- **No mocking library.** Application tests use small hand-written fakes (in-memory
  repository, capturing audit logger / event publisher, a fixed `TimeProvider`) — they
  read clearly and carry no dependency.
- A slice's tests live with the slice (see the `new-slice` skill).

## What we cover / skip

**Cover:** business rules and every invalid workflow transition (Domain); handler
orchestration — audit, events, idempotency, not-found (Application); the authorization
matrix and invalid-transition HTTP responses, audit read access, and the async
round-trips incl. failure/retry and dedup (Integration).

**Skip:** trivial DTO mapping / getters, framework code, and one-off scripts.

## CI

`.github/workflows/ci.yml` runs two jobs (and `security.yml` runs gitleaks):

- **build-and-unit** (fast, no Docker): `format --verify-no-changes` → build → the
  Domain and Application unit-test projects. Fails in seconds on a format/build/unit slip.
- **integration-tests** (Docker, `needs: build-and-unit`): the Testcontainers suite.
  Gated on the fast job so container time isn't spent on a broken build.

Green CI is required to merge.

## Known gaps / deferrals

- **Row-level read scoping** (own/department on `GET`) is not implemented yet, so it
  isn't tested — tracked in [security.md](security.md).
- **Performance / load / chaos** testing is out of scope for this learning project.
- **Worker consumer internals** (the `IntegrationEventProcessor` dedup/route logic) are
  covered end-to-end by the integration round-trip + resilience tests rather than in
  isolation; a dedicated `CompanyOps.Worker.Tests` project is a future option if that
  logic grows.
- **Mutation testing / coverage gates** are enterprise-optional and not enforced.
