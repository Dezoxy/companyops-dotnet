---
name: ef-migration
description: Add, review, and apply an EF Core migration in CompanyOps conventions — generate the migration, inspect the generated SQL for destructive/data-loss operations before applying, then update the local Postgres. Use whenever the EF model changes (new entity/column/index, owned collection, mapping change) and a migration is needed. Bakes in the project's mapping conventions and the gotchas already hit.
---

# Add an EF Core migration (CompanyOps)

Migrations live in **Infrastructure**; the **Api** is the startup project (it supplies
configuration + the design-time factory). Always **review the generated SQL before
applying it** — that's the point of this skill.

Prereqs: `dotnet tool restore` (pins `dotnet-ef`); Postgres up
(`docker compose -f infra/docker-compose.yml up -d`).

## 1. Add the migration

```bash
dotnet ef migrations add <Name> -p src/CompanyOps.Infrastructure -s src/CompanyOps.Api
```

`<Name>` is PascalCase and describes the change (`AddApprovalWorkflow`, `AddAuditLog`).
This generates the migration + updates the model snapshot; it does **not** touch the DB.

## 2. Review the SQL (do not skip)

```bash
dotnet ef migrations script <PreviousMigration> <NewMigration> -p src/CompanyOps.Infrastructure -s src/CompanyOps.Api
```

Read every statement. **Flag and stop** if you see, on a table that holds (or will hold)
real data:

- `DROP TABLE` / `DROP COLUMN` — data loss. Is the column truly unused?
- `ALTER COLUMN ... TYPE` / `NOT NULL` added without a safe default — can fail or truncate.
- A `NOT NULL` column added with a sentinel default (e.g. `'00000000-...'`) — fine only
  while the table is empty; on populated tables, split into add-nullable → backfill →
  set-not-null across two migrations.
- New indexes on large tables (lock/skew) — acceptable here at dev scale; note it.

Additive changes (new table, new nullable column, new index on a small/empty table) are
safe to apply directly. The migrations so far have all been additive.

## 3. Apply to the local database

```bash
dotnet ef database update -p src/CompanyOps.Infrastructure -s src/CompanyOps.Api
```

Then verify with `docker exec companyops-postgres psql -U companyops -d companyops -c '\d <table>'`.

## Mapping conventions (so the migration comes out right)

- **Enums are stored as text:** `.HasConversion<string>().HasMaxLength(50)` — readable and
  stable across enum reordering. Never integer-backed.
- **Owned aggregate children** (e.g. approval steps) use `OwnsMany(... ToTable("…"))`, a
  field-backed navigation (`UsePropertyAccessMode(PropertyAccessMode.Field)`), and — when
  the aggregate assigns the child's key itself — **`step.Property(s => s.Id).ValueGeneratedNever()`**.
  Without it EF treats a graph-discovered new child as `Modified` (UPDATE → 0 rows →
  `DbUpdateConcurrencyException` on save). This bit us once; it's the fix.
- **Infrastructure-only tables** (outbox, processed-messages) have no public `DbSet` — they
  register via their `IEntityTypeConfiguration` and are reached through `Set<T>()`.

## Gotchas already banked

- **Postgres 18 volume path:** the compose volume mounts at `/var/lib/postgresql`
  (not `/var/lib/postgresql/data`) — wrong path crashes the container on start.
- **Never hand-edit an applied migration.** To change a migration that is the latest and
  only applied locally (dev only, no real data), revert and regenerate cleanly:
  ```bash
  dotnet ef database update <PreviousMigration> -p src/CompanyOps.Infrastructure -s src/CompanyOps.Api   # revert
  dotnet ef migrations remove -p src/CompanyOps.Infrastructure -s src/CompanyOps.Api                     # drop it
  # edit the model/config, then re-run step 1
  ```
  An already-shipped migration is immutable — fix forward with a new one instead.

## After

- `dotnet build` clean; the migration's `Up`/`Down` SQL reviewed; applied to local Postgres.
- Commit the migration with the slice that needs it (it's part of that vertical slice).
- The migration is exercised by the Testcontainers integration tests (they run
  `Database.MigrateAsync()` against a fresh Postgres on startup).
