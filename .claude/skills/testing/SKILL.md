---
name: testing
description: Testing guidelines for LeiMa (xUnit, Testcontainers, Vitest, Testing Library). Use when writing, fixing, or running tests, or deciding what to test.
---

# Testing

## When to use
Adding or changing behavior, fixing bugs, or reviewing test quality.

## Conventions
- Pyramid: many unit tests, fewer integration tests, few end-to-end.
- Backend: xUnit, FluentAssertions (or built-in asserts), NSubstitute; integration tests use `WebApplicationFactory` and Testcontainers PostgreSQL (no in-memory EF provider).
- Frontend: Vitest + Testing Library; query by role/label; mock the network with MSW.
- Naming: `Method_Scenario_ExpectedResult`; arrange / act / assert.
- External providers are always faked or stubbed; never call real provider APIs in tests.
- Concurrency-sensitive code (task orchestration, retries, cancellation) needs explicit tests; avoid `Thread.Sleep`, use fake time (`TimeProvider`).
- Bug fix = failing test first.

## Commands
```bash
dotnet test
npm test
```

## Checklist
- [ ] Happy path, failure path, and edge cases
- [ ] Tests deterministic and independent
- [ ] Tests run in CI-friendly time

## Don'ts
- No tests that assert implementation details or only mocks.
- No shared mutable state between tests.
