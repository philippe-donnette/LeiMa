# LeiMa.Common

Shared NuGet packages with cross-cutting infrastructure for all LeiMa .NET services. Decision: [ADR 0002](../../../docs/adr/0002-shared-leima-common-package.md). Tracking: feature #9.

## Packages
| Package | Purpose | Heavy dependencies |
|---|---|---|
| `LeiMa.Common` | Result/error types, time and id helpers, correlation-id constants | none |
| `LeiMa.Common.AspNetCore` | `AddLeiMaDefaults` / `UseLeiMaDefaults`: logging and telemetry, ProblemDetails, correlation ids, health checks, auth wiring | ASP.NET Core |
| `LeiMa.Common.EntityFrameworkCore` | UTC timestamps, concurrency tokens, audit interceptor, configuration helpers | EF Core, Npgsql |
| `LeiMa.Common.Testing` | Web application factory base, PostgreSQL test container fixture, fake clock | xUnit, Testcontainers |

Current state: all four packages are implemented (tasks #13 to #16) and the publish workflow exists (task #17); see Releasing and Consuming below.

## Using LeiMa.Common
`Result` / `Result<T>` / `Error` (typed by `ErrorType`) for expected failures; `Ids.NewId()` for time-ordered GUIDs (version 7); inject `TimeProvider` instead of `DateTime.UtcNow`; `CorrelationConstants` for the correlation header and log property.

## Using LeiMa.Common.AspNetCore
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddLeiMaDefaults();                                  // logging, telemetry, ProblemDetails, health, auth services
builder.Services.AddLeiMaJwtAuthentication(builder.Configuration);   // needs Authentication:Jwt:Authority and :Audience
builder.Services.AddLeiMaAuthorization(o => o.AddPolicy("providers:admin", p => p.RequireClaim("permission", "providers:admin")));

var app = builder.Build();
app.UseLeiMaDefaults();                                      // before the service's own endpoints
app.MapGet("/providers/{id}", (Guid id) => Result.Failure<string>(Error.NotFound("provider.not_found", "No such provider.")).ToHttpResult());
app.Run();
```
- Logging: Serilog (readable console in Development, compact JSON otherwise); every event carries `Service` and `CorrelationId`. Extra sinks and levels come from the `Serilog` configuration section.
- Telemetry: OpenTelemetry traces and metrics (ASP.NET Core, HttpClient, runtime). Exported over OTLP only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set.
- Errors: unhandled exceptions become a 500 ProblemDetails (details only in logs); `Error` maps to 400/401/403/404/409/500 with the error code in the `code` extension. Responses carry `traceId` and `correlationId`.
- Correlation: `X-Correlation-Id` is echoed or generated; incoming values over 64 characters or with characters other than letters, digits, `-`, `_`, `.` are replaced.
- Health: `/health/live` runs no checks; `/health/ready` runs checks tagged `LeiMaDefaultsExtensions.ReadyTag`. Both are anonymous.
- Authorization is deny-by-default once `AddLeiMaAuthorization` is used: endpoints need an authenticated user unless marked `AllowAnonymous`.
- Options: `services.AddValidatedOptions<T>(configuration, "Section")` validates data annotations and fails the startup on invalid configuration.

## Using LeiMa.Common.EntityFrameworkCore
```csharp
services.AddLeiMaEntityFrameworkCore();   // audit interceptor (+ TimeProvider.System if none registered)
services.AddDbContext<MyDbContext>((sp, o) => o.UseLeiMaNpgsql(sp, connectionString));   // retries on transient failures

// in the DbContext
protected override void ConfigureConventions(ModelConfigurationBuilder c) => c.UseLeiMaConventions();
// in an entity configuration
entity.UseXminConcurrencyToken();   // optimistic concurrency via PostgreSQL xmin (shadow property)
entity.HasAuditTimestamps();        // CreatedAt / UpdatedAt, filled on save from the injected TimeProvider
```
- `UseLeiMaConventions` stores every `DateTime` / `DateTimeOffset` as `timestamptz` in UTC; reads return `DateTimeKind.Utc`, unspecified values are treated as UTC.
- `HasAuditTimestamps` adds `DateTimeOffset` shadow properties unless the entity declares `CreatedAt` / `UpdatedAt` itself (`DateTime` or `DateTimeOffset`). `CreatedAt` is never changed on update.

## Using LeiMa.Common.Testing
- `PostgreSqlFixture` (`IClassFixture`) starts one `postgres:17-alpine` container per test class; `CreateDatabaseAsync()` returns the connection string of a fresh, isolated database.
- Mark tests that need Docker with `[RequiresDockerFact]`: skipped when Docker is not running locally, but they fail on CI (`CI` / `GITHUB_ACTIONS` set), so CI never silently skips them.
- `LeiMaWebApplicationFactory<Program>`: runs the service in the `Testing` environment, exposes `Clock` (a `FakeTimeProvider` registered as the `TimeProvider`) and `WithSetting(key, value)` for configuration overrides.

## Scope rule
Only cross-cutting infrastructure belongs here. If it contains a domain concept, an API or event contract, or business logic, it does not belong in these packages. Deferred until a service needs it: background-task base class, provider-call resilience defaults.

## Layout
```
LeiMa.Common.slnx
Directory.Build.props        # shared settings (net10.0, nullable, warnings as errors)
Directory.Packages.props     # central package versions
src/<Package>/               # packable; src/Directory.Build.props holds package metadata
tests/<Package>.Tests/       # xUnit; same-named test project sees internals
```
`LeiMa.Common.Testing` has no test project of its own; `PostgreSqlFixture` is exercised by the EF Core tests. `LeiMaWebApplicationFactory` needs a real service entry point, so it is first exercised by the first service that uses it.

## Commands (run in this folder)
```bash
dotnet build LeiMa.Common.slnx -c Release
dotnet test LeiMa.Common.slnx -c Release
dotnet pack LeiMa.Common.slnx -c Release -o ./artifacts -p:Version=0.1.0-local
```

## Conventions
- Target `net10.0` only; SemVer. The version comes from the release tag (`common-vX.Y.Z`) in CI; local builds use the default in `Directory.Build.props`.
- Every public API has XML docs (documentation file is generated, warnings are errors).
- Add package versions in `Directory.Packages.props` only (central package management).
- Breaking changes need a major version bump and are called out in the commit message and release notes.
- SourceLink comes from the .NET SDK; the repository URL is derived from `GITHUB_REPOSITORY` in CI, never hard-coded.
- Changing a package together with a service: pack to a local folder and add it as a temporary NuGet source, or use a temporary `ProjectReference` that is never committed.

## Releasing
Releases are manual, so every version is a deliberate decision (nothing is published on merge). All four packages share one version.

1. GitHub > Actions > **Publish LeiMa.Common** > *Run workflow*, branch `main`. Or from a terminal: `gh workflow run publish-common.yml --ref main -f bump=patch`.
2. Choose the inputs:
   - `bump`: `patch`, `minor` or `major`, applied to the latest `common-vX.Y.Z` release (breaking change = `major`). Ignored for the very first release, which uses `VersionPrefix` from `Directory.Build.props` (0.1.0).
   - `prerelease` (optional): a suffix such as `rc.1`, giving for example `0.2.0-rc.1`. The release is marked as a prerelease.
   - `dry_run`: build, test and pack only. Use it to check a release before publishing.
3. The workflow computes the version (`tools/next-common-version.sh`), builds and tests (including the Docker tests), packs, pushes to GitHub Packages (existing versions are skipped, never overwritten) and creates the tag `common-vX.Y.Z` with a GitHub release and generated notes.

It refuses to run from any branch other than `main` or when the computed tag already exists. Published versions cannot be changed: to fix a bad release, publish the next version.


## Consuming from a service
`services/nuget.config` takes `LeiMa.Common*` from GitHub Packages and everything else from nuget.org (package source mapping). Account and token come from environment variables, so nothing account-specific or secret is committed:

| Variable | Value |
|---|---|
| `LEIMA_NUGET_OWNER` | the GitHub account that owns the repository |
| `LEIMA_NUGET_TOKEN` | a personal access token (classic) with `read:packages`; in GitHub Actions use `secrets.GITHUB_TOKEN` |

```bash
export LEIMA_NUGET_OWNER=<github account>
export LEIMA_NUGET_TOKEN=<token with read:packages>
dotnet add package LeiMa.Common.AspNetCore
```
GitHub Packages requires a token to restore even when the repository is public. Services' CI jobs must set both variables on the restore step (`LEIMA_NUGET_OWNER: ${{ github.repository_owner }}`, `LEIMA_NUGET_TOKEN: ${{ secrets.GITHUB_TOKEN }}`). Dependabot needs the same feed configured as a registry in `.github/dependabot.yml` when the first service is added.
