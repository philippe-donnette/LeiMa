---
name: git-commits
description: Commit message, branch, and PR conventions for LeiMa. Use when creating commits, naming branches, or writing pull request descriptions.
---

# Git commits

## When to use
Whenever committing, branching, or opening a PR (only when the user asks).

## Conventions
- Commit format: `type: <ID> - <summary>`, e.g. `feat: 12345 - add retry policy to task orchestrator`.
  - Types: feat, fix, refactor, test, docs, chore, build, ci, perf.
  - `<ID>` is the numeric ID of the user story, task or feature the change belongs to. It is required.
  - Summary in imperative mood, ≤ 72 chars total subject, no trailing period. Body (optional) explains why and may mention the service.
  - If the ID is unknown, ask the user instead of guessing. Repo chores with no work item: `chore: 0 - <summary>`.
  - Story IDs match the file name in `docs/user-stories/` (see `user-stories`).
- Breaking changes: `type!: <ID> - <summary>` and a `BREAKING CHANGE:` footer.
- Branches: `<type>/<ID>-<short-kebab-description>`, e.g. `feat/12345-provider-credentials`.
- Small, atomic commits; one logical change each. Don't mix formatting with behavior.
- No AI attribution: never add `Co-Authored-By`, Claude/Anthropic emails, or "Generated with Claude Code" lines to commit messages, PR descriptions or pushes. The author is the configured git user only.
- Include the CLAUDE.md/doc update in the same commit as the change that requires it.

## PR description template
```
Title: type: <ID> - summary
## Work items (IDs)
## What
## Why
## How to test
## Notes (migrations, config, breaking changes)
```

## Checklist
- [ ] Tests and build pass
- [ ] No secrets or generated files staged
- [ ] Message follows format and includes the work item ID

## Don'ts
- No force-push to shared branches, no `--no-verify`.
- No commits unless the user asked for them.
- No co-author trailers or AI-generated footers, ever.
