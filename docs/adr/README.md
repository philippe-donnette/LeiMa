# Architecture Decision Records

Significant, hard-to-reverse decisions are recorded here using the [MADR](https://adr.github.io/madr/) format. Start from [0000-template.md](0000-template.md).

## Process
1. Open an issue (type: ADR) describing the decision to make.
2. Copy the template to `NNNN-kebab-title.md` using the next free number; status `proposed`.
3. Open a PR (label `type:adr`) with status `proposed` and discuss it there. When the decision is agreed, add a last commit to the same PR setting the status to `accepted` and the date, then merge. Nothing is left to do after the merge.
4. Never edit an accepted ADR except its status. To change a decision, write a new ADR that supersedes it.

## Index
| # | Title | Status | Date |
|---|---|---|---|
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions | accepted | 2026-11-05 |
| [0002](0002-shared-leima-common-package.md) | Share cross-cutting infrastructure through LeiMa.Common NuGet packages | accepted | 2026-11-07 |

## Planned
Message broker, auth and permissions model, service communication and contracts, task orchestration, observability stack, monorepo build/CI strategy.
