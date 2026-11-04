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

## Structure
```
services/<name>/
  CLAUDE.md
  src/<Name>.Api  <Name>.Application  <Name>.Domain  <Name>.Infrastructure
  tests/<Name>.UnitTests  <Name>.IntegrationTests
```

## Checklist
- [ ] Service `CLAUDE.md` created/updated; root `CLAUDE.md` service table updated
- [ ] Tests added (see `testing` skill)
- [ ] Health checks, logging, auth in place
- [ ] No reference to another service's internals

## Don'ts
- No shared DB or shared domain library between services.
- No `async void`, `.Result`/`.Wait()`, or static mutable state.
- No business logic in endpoints or controllers.
