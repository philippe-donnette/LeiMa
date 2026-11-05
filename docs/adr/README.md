# Architecture Decision Records

Significant, hard-to-reverse decisions are recorded here using the [MADR](https://adr.github.io/madr/) format. Start from [0000-template.md](0000-template.md).

## Process
1. Open an issue (type: ADR) describing the decision to make.
2. Copy the template to `NNNN-kebab-title.md` using the next free number; status `proposed`.
3. Open a PR (label `type:adr`), discuss in the PR; merging it accepts the ADR (set status to `accepted`).
4. Never edit an accepted ADR except its status. To change a decision, write a new ADR that supersedes it.

## Index
| # | Title | Status | Date |
|---|---|---|---|
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions | accepted | 2026-11-05 |

## Planned
Message broker, auth and permissions model, service communication and contracts, task orchestration, observability stack, monorepo build/CI strategy.
