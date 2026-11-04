---
name: user-stories
description: Format and process for writing LeiMa user stories with acceptance criteria. Use when asked to write, refine, split, or estimate user stories or requirements.
---

# User stories

## When to use
Capturing a feature request or requirement before implementation.

## Conventions
- File: `docs/user-stories/<service>/<id>-<kebab-slug>.md`, created from `docs/user-stories/_template.md`. Use `platform` as the folder for cross-service stories.
- ID: numeric (e.g. `12345`), unique, never reused; stories, tasks and features share the same ID space. The ID is cited in commits, branches and PRs (see `git-commits`).
- Format: **As a** <role> **I want** <capability> **so that** <benefit>.
- Roles: administrator, operator, monitor/viewer, integrator, system.
- Acceptance criteria in Given / When / Then; each testable and mapped to a test.
- Include non-functional criteria where relevant: permissions required, provider rate limits, concurrency/cancellation behavior, audit trail.
- Size S/M/L; split stories larger than L into vertical slices.
- Link affected services and ADRs.

## Checklist
- [ ] Independent, valuable, small, testable
- [ ] Permissions and failure cases covered
- [ ] Out of scope stated

## Don'ts
- No implementation details in the story.
- No vague criteria ("fast", "user-friendly") without a measure.
