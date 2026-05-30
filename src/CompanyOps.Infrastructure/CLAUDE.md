# Infrastructure layer — rules

Implements the interfaces (ports) defined in **Application**. This is the only
place concrete external technology lives.

Belongs here:
- EF Core `AppDbContext`, entity configurations (`IEntityTypeConfiguration<T>`),
  and **migrations** (Postgres / Npgsql).
- Repository implementations and `IUnitOfWork`.
- Keycloak/OIDC integration, RabbitMQ publisher/consumer plumbing, Redis cache,
  external API clients (`FakeFinanceApi`, `FakeInventoryApi`), audit-log writer.

**Forbidden here:**
- No business rules — those live in Domain. Infrastructure persists and integrates;
  it does not decide.
- No ASP.NET Core endpoint/controller code (that's Api).
- Don't reference Api.

## Conventions for CompanyOps

- Map EF entities ↔ domain entities; keep persistence concerns (column types,
  indexes, concurrency tokens) out of Domain.
- **Migrations:** create with `dotnet ef migrations add <Name> -p
  src/CompanyOps.Infrastructure -s src/CompanyOps.Api`. **Review the generated
  SQL before applying.** Never hand-edit a migration that's already been applied.
- Least-privilege DB user; connection strings come from env vars / user-secrets,
  never hard-coded.
- External calls get timeouts + retry/graceful failure (Polly or equivalent) —
  enterprise integrations fail, and the system must degrade, not crash.
- Audit-log writer makes entries effectively immutable to normal users
  (append-only; no update/delete paths exposed).
