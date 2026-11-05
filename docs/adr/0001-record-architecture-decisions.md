# 0001. Record architecture decisions

- Status: accepted
- Date: 2026-11-05
- Deciders: @philippe-donnette
- Related: issue #2

## Context and problem statement
LeiMa is a suite of microservices built step by step. Decisions about brokers, authentication, contracts and orchestration shape every service and are costly to reverse. Without a written record, the reasons get lost and decisions are revisited repeatedly.

## Decision drivers
- Decisions must be discoverable and reviewable alongside the code.
- Low overhead for a small team.
- The repository is public, so the record doubles as project documentation.

## Considered options
1. ADRs as Markdown files in the repository (MADR format), reviewed through PRs
2. A wiki or external documentation tool
3. Decisions only in issues and PR discussions

## Decision outcome
Chosen option: "ADRs in the repository (MADR)", because they are versioned with the code, reviewed through the normal PR flow, and stay close to the services they affect.

### Consequences
- Good: history and rationale are preserved; supersession is explicit.
- Good: PR review gives every decision a discussion trail.
- Bad: a small writing overhead per significant decision.
- Follow-ups: keep `docs/adr/README.md` index current; the `documentation` skill describes the process.

## Pros and cons of the options
### Repository MADR files
- Good: versioned, reviewable, searchable, offline.
- Bad: needs discipline to keep the index current.

### Wiki or external tool
- Good: easy editing.
- Bad: drifts from the code, separate access control and history.

### Issues and PRs only
- Good: no extra files.
- Bad: hard to find later, no stable canonical statement of the decision.
