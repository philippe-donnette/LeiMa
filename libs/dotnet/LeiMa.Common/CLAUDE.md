# LeiMa.Common

Shared NuGet packages with cross-cutting infrastructure for all LeiMa .NET services. Decision: [ADR 0002](../../../docs/adr/0002-shared-leima-common-package.md). Tracking: feature #9.

## Packages
| Package | Purpose | Heavy dependencies |
|---|---|---|
| `LeiMa.Common` | Result/error types, time and id helpers, correlation-id constants | none |
| `LeiMa.Common.AspNetCore` | `AddLeiMaDefaults` / `UseLeiMaDefaults`: logging and telemetry, ProblemDetails, correlation ids, health checks, auth wiring | ASP.NET Core |
| `LeiMa.Common.EntityFrameworkCore` | UTC timestamps, concurrency tokens, audit interceptor, configuration helpers | EF Core, Npgsql |
| `LeiMa.Common.Testing` | Web application factory base, PostgreSQL test container fixture, fake clock | xUnit, Testcontainers |

Current state: scaffold only (empty packages with smoke tests). Features are added by the tasks under feature #9.

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
`LeiMa.Common.Testing` has no test project of its own; its fixtures are exercised by the other test projects.

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
- Publishing and consuming from GitHub Packages is added in task #17.
