# 0002. Share cross-cutting infrastructure through LeiMa.Common NuGet packages

- Status: accepted
- Date: 2026-11-07
- Deciders: project maintainers
- Related: issues #9, #10, #11, [0001](0001-record-architecture-decisions.md)

## Context and problem statement
Every .NET microservice needs the same cross-cutting plumbing: logging and OpenTelemetry setup, ProblemDetails and exception handling, health checks, correlation ids, authentication wiring, EF Core conventions and integration-test fixtures. Re-implementing these per service leads to drift and repeated bugs. The root `CLAUDE.md` currently says services share no code, which exists to prevent coupling through domain models and contracts, not to forbid shared infrastructure.

## Decision drivers
- Consistent behavior (errors, logs, traces, health) across all services
- Services must stay independently deployable and evolvable
- Low maintenance for a small team
- Easy local development inside the monorepo
- The repository is public

## Considered options
1. Shared NuGet packages for cross-cutting infrastructure only, published to GitHub Packages
2. Copy the plumbing into each service (template or scaffold)
3. Project references to a shared library inside the monorepo, no packaging
4. Shared source via git submodule or shared project files
5. No sharing; each service decides

## Decision outcome
Chosen option: "Shared NuGet packages for cross-cutting infrastructure", because it gives consistency and one place to fix bugs while services keep control of when they upgrade.

Specifics:
- **Packages** (net10.0, SemVer): `LeiMa.Common` (no heavy dependencies), `LeiMa.Common.AspNetCore`, `LeiMa.Common.EntityFrameworkCore`, `LeiMa.Common.Testing`. Several small packages so a service only pulls what it uses.
- **Location**: `libs/dotnet/LeiMa.Common` in this monorepo.
- **Distribution**: GitHub Packages, released from tags (`common-vX.Y.Z`). Consumers need a token with `read:packages`, even for public packages.
- **Scope rule (exception to the independence rule)**: only cross-cutting infrastructure belongs in the packages. Domain models, business logic and API or event contracts never do. Contracts are owned by the service that publishes them.
- **Change policy**: breaking changes require a major version bump and are called out in the commit and release notes. Services pin a version and upgrade deliberately.

### Consequences
- Good: consistent behavior and one place to fix and improve plumbing.
- Good: services still deploy independently and choose when to upgrade.
- Bad: a release pipeline and package feed to maintain, and a token for restore.
- Bad: risk of the package growing into a dumping ground, mitigated by the scope rule and review.
- Bad: changing the package and a service together needs a local feed or temporary project reference.
- Follow-ups: scaffold the solution, CI and publishing (tasks under #9); the background-task base class and provider resilience defaults are deferred until the first service needs them.

## Pros and cons of the options
### Shared NuGet packages (chosen)
- Good: versioned, discoverable, standard tooling, Dependabot updates.
- Bad: needs publishing and feed authentication.

### Copy per service
- Good: zero coupling.
- Bad: drift, and every fix is repeated per service.

### Project references only
- Good: no publishing, always in sync.
- Bad: couples service builds and releases to the library, and breaks if a service moves out of the monorepo.

### Git submodule or shared project files
- Good: no feed needed.
- Bad: awkward tooling, hidden coupling, poor versioning.

### No sharing
- Good: maximum autonomy.
- Bad: inconsistent behavior across services.
