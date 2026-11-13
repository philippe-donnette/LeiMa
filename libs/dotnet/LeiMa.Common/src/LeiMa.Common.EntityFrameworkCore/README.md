# LeiMa.Common.EntityFrameworkCore

EF Core and PostgreSQL conventions for LeiMa services.

```csharp
services.AddLeiMaEntityFrameworkCore();
services.AddDbContext<MyDbContext>((sp, o) => o.UseLeiMaNpgsql(sp, connectionString));

// DbContext
protected override void ConfigureConventions(ModelConfigurationBuilder c) => c.UseLeiMaConventions();

// entity configuration
entity.UseXminConcurrencyToken();
entity.HasAuditTimestamps();
```

- All date/time values stored as UTC `timestamptz`
- Optimistic concurrency through PostgreSQL `xmin`
- `CreatedAt` / `UpdatedAt` filled on save from the injected `TimeProvider`
- Npgsql with retry on transient failures

Part of the LeiMa.Common packages, which share one version. Cross-cutting infrastructure only. See the repository's `libs/dotnet/LeiMa.Common/CLAUDE.md` for details.
