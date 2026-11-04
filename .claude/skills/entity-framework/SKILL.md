---
name: entity-framework
description: Guidelines for EF Core usage in LeiMa services (DbContext, entity configuration, migrations, queries, concurrency). Use when adding entities, writing queries, or creating migrations.
---

# Entity Framework Core

## When to use
Any change to a service's data model, DbContext, migrations, or data-access queries.

## Conventions
- One `DbContext` per service, in `Infrastructure`, targeting that service's own PostgreSQL database.
- Configure entities with `IEntityTypeConfiguration<T>` classes; no data annotations for mapping.
- Entities protect invariants (private setters, factory/methods); IDs are generated app-side (GUID v7 or similar).
- Concurrency: row version / `xmin` concurrency token on aggregates modified concurrently (essential for task state).
- Queries: `AsNoTracking` for reads, project to DTOs with `Select`, avoid N+1 (`Include` deliberately), always paginate lists.
- Migrations: `dotnet ef migrations add <PascalCaseDescription>`; one migration per change; review generated SQL; never edit applied migrations.
- Apply migrations through a dedicated step (job/CLI), not implicitly on every request path.
- Timestamps in UTC (`timestamptz`).

## Checklist
- [ ] Configuration class added, indexes and constraints explicit
- [ ] Migration generated, reviewed, and reversible
- [ ] Integration test against real PostgreSQL (Testcontainers)
- [ ] Service `CLAUDE.md` Data section updated

## Don'ts
- No cross-service joins or shared tables.
- No lazy loading.
- No raw SQL with string concatenation.
