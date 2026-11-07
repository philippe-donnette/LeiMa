# Libraries

Shared libraries used by several services. Only cross-cutting infrastructure belongs here; domain models, business logic and API or event contracts stay in the owning service (see [ADR 0002](../docs/adr/0002-shared-leima-common-package.md)).

| Library | Path | Purpose |
|---|---|---|
| LeiMa.Common | [dotnet/LeiMa.Common](dotnet/LeiMa.Common) | NuGet packages for .NET services: core, ASP.NET Core defaults, EF Core helpers, test fixtures (being scaffolded) |
