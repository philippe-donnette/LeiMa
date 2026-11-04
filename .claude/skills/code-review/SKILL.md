---
name: code-review
description: Checklist and output format for reviewing changes in LeiMa. Use when asked to review a diff, branch, or PR, or before finalizing significant changes.
---

# Code review

## When to use
Reviewing a diff, branch, or PR (own or others').

## Conventions
Review in this order:
1. **Correctness**: logic, edge cases, error handling, null/empty cases.
2. **Security & permissions**: authorization on every endpoint, input validation, secret handling, provider credential exposure.
3. **Concurrency & reliability**: idempotency, cancellation, retries, race conditions, duplicate task execution, transaction boundaries.
4. **Data**: migrations safe and reversible, indexes, query efficiency.
5. **Design**: service boundaries respected, layering, simplicity.
6. **Tests**: new behavior covered, tests meaningful (see `testing`).
7. **Docs**: CLAUDE.md, README, ADR updated where needed.

## Output format
```
Summary: <1-2 sentences>
- [blocker|major|minor|nit] path:line – issue and suggested fix
Verdict: approve | approve with comments | request changes
```

## Checklist
- [ ] Read the whole diff, not just changed lines
- [ ] Ran or inspected tests/build
- [ ] Every finding has a concrete suggestion

## Don'ts
- No style nitpicks that a formatter/linter handles.
- Don't flag unverified suspicions as blockers; label uncertainty.
