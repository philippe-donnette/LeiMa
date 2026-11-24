# LeiMa.Common.Testing

Test helpers for LeiMa services (xUnit).

- `PostgreSqlFixture`: one PostgreSQL container (Testcontainers) per test class, with an isolated database per test
- `PostgresTestHost`: the service's own registration on a fresh test database with a fake clock; `CreateAsync(postgres, "<connection string name>", services => services.AddMyInfrastructure(), migrate: sp => ...)`, then `NewScope()`, `Clock`, `MigrateAsync()`, `ExecuteAsync(sql)` and `QueryAsync(sql)`
- `[RequiresDockerFact]`: skipped locally when Docker is not running, fails on CI so tests are never silently skipped
- `LeiMaWebApplicationFactory<TEntryPoint>`: runs a service in the `Testing` environment with a fake clock and configuration overrides

Reference this package from test projects only.

Part of the LeiMa.Common packages, which share one version. See the repository's `libs/dotnet/LeiMa.Common/CLAUDE.md` for details.
