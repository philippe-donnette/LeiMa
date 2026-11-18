---
name: dotnet-backend
description: Guidelines for creating or changing a .NET microservice in LeiMa (project layout, APIs, DI, config, background tasks). Use when adding a service, endpoint, background worker, or any C# backend code under services/.
---

# .NET backend

## When to use
Creating a new service or modifying C# code in `services/<name>/`.

## Conventions
- .NET 10, nullable enabled, implicit usings, warnings as errors.
- Layers: `Api` (endpoints, auth, ProblemDetails) → `Application` (use cases, interfaces) → `Domain` (entities, rules, no dependencies) ← `Infrastructure` (EF Core, provider clients, messaging).
- Minimal APIs grouped per feature via `MapGroup`; request/response DTOs are records, never expose entities.
- Configuration via options pattern with validation (`ValidateOnStart`); secrets never in appsettings.
- Errors: RFC 7807 `ProblemDetails`; no exceptions for expected flow.
- Pass `CancellationToken` through every async call.
- Provider calls: `HttpClientFactory` typed clients, timeouts, retry with backoff (Polly/resilience handler), rate-limit handling.
- Long-running work: `BackgroundService` / worker with bounded concurrency, idempotent and resumable steps, persisted state.
- Observability: Serilog/structured logs, OpenTelemetry, `/health/live` and `/health/ready`.
- AuthN/AuthZ enforced with policies on every endpoint.
- XML documentation comment on every code element in `src`, public or not: `GenerateDocumentationFile` plus the StyleCop analyzers with only SA1600 enabled (see `services/LeiMa.Runner`: `Directory.Build.props`, `src/Directory.Build.props`, `.editorconfig`, `src/stylecop.json`).
- Domain and Application use no ORM, web or cloud types; persistence, identity and cloud access are ports in Application implemented in Infrastructure. An architecture test enforces inward-only references and this rule (ADR 0003).
- Keep it light: no mediator library, no generic repositories, no separate persistence model.

## Shared packages (`LeiMa.Common*`)
- Reference `LeiMa.Common`, `LeiMa.Common.AspNetCore`, `LeiMa.Common.EntityFrameworkCore` and `LeiMa.Common.Testing` from `libs/dotnet/LeiMa.Common` (published to GitHub Packages; `services/nuget.config` is the feed, set `LEIMA_NUGET_OWNER` and `LEIMA_NUGET_TOKEN` to restore; see that folder's `CLAUDE.md`).
- Use them for logging and telemetry, ProblemDetails and exception handling, correlation ids, health checks, options validation, auth wiring, EF conventions and test fixtures. Do not re-implement these in a service; if something is missing, add it to the package.
- Only cross-cutting infrastructure belongs in the packages. Domain models, business logic and API or event contracts stay in the owning service.
- Pin a package version per service and upgrade deliberately; breaking changes arrive as a major version.

## Structure
```
services/<name>/
  CLAUDE.md
  src/<Name>.Api  <Name>.Application  <Name>.Domain  <Name>.Infrastructure
  tests/<Name>.ArchitectureTests  <Name>.UnitTests  <Name>.IntegrationTests
```
Folder `services/LeiMa.<Name>`, projects `LeiMa.<Name>.Api` and so on, one `LeiMa.<Name>.slnx` per service. Register the service in the shared `aspire/LeiMa.AppHost` with `AddDotnetProject` (and a database with `AddDatabase` if it has one).

## Checklist
- [ ] Service `CLAUDE.md` created/updated; root `CLAUDE.md` service table updated
- [ ] Registered in `aspire/LeiMa.AppHost`; nuget entry in `.github/dependabot.yml` (with the `leima-packages` registry if it uses `LeiMa.Common*`)
- [ ] Tests added (see `testing` skill), including the architecture test
- [ ] Health checks, logging, auth in place
- [ ] No reference to another service's internals

## Don'ts
- No shared DB or shared domain library between services (the `LeiMa.Common*` infrastructure packages are the only shared code).
- No `async void`, `.Result`/`.Wait()`, or static mutable state.
- No business logic in endpoints or controllers.
