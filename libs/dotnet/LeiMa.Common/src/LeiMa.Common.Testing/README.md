# LeiMa.Common.Testing

Test helpers for LeiMa services (xUnit).

- `PostgreSqlFixture`: one PostgreSQL container (Testcontainers) per test class, with an isolated database per test
- `[RequiresDockerFact]`: skipped locally when Docker is not running, fails on CI so tests are never silently skipped
- `LeiMaWebApplicationFactory<TEntryPoint>`: runs a service in the `Testing` environment with a fake clock and configuration overrides

Reference this package from test projects only.

Part of the LeiMa.Common packages, which share one version. See the repository's `libs/dotnet/LeiMa.Common/CLAUDE.md` for details.
