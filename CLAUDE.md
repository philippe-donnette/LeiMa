# LeiMa

LeiMa is a suite of microservices to **administer, monitor, manage permissions, and orchestrate long-running concurrent background tasks across external API providers**.

## Domain terms
- **Provider**: an external API LeiMa integrates with (rate limits, credentials, outages are the norm).
- **Task / Job**: a long-running, concurrent background unit of work executed against a provider.
- **Permission**: who may administer providers, trigger tasks, or view monitoring data.
- **Worker**: a process that executes tasks.

## Repo map
```
services/<name>/   one folder per microservice, each with its own CLAUDE.md
services/_template per-service CLAUDE.md template (not a service)
libs/dotnet/       shared .NET libraries, e.g. LeiMa.Common (NuGet packages, see below)
web/               React frontends
docs/              architecture/, adr/, user-stories/
tools/             scripts
.claude/skills/    project skills (see below)
```

## Services
_None yet. Services are added step by step._ Keep this list current:

| Service | Purpose | Folder |
|---|---|---|

## Default stack (editable; record changes in `docs/adr/`)
- .NET 10 / ASP.NET Core, C#
- EF Core + PostgreSQL, one database per service
- React + TypeScript (Vite), Tailwind CSS with the shared `web/packages/ui` design system (`@leima/ui`)
- Shared infrastructure for .NET services: `LeiMa.Common` NuGet packages in `libs/dotnet/LeiMa.Common` (ADR 0002, proposed)
- Message broker for task orchestration: TBD (ADR)

## Cross-cutting conventions
- Services are independent: no shared database, no shared domain code; communicate via HTTP APIs or events.
- Only exception: cross-cutting infrastructure (logging, telemetry, errors, health, auth wiring, EF conventions, test fixtures) comes from the `LeiMa.Common*` NuGet packages. Domain models, business logic and API/event contracts never go in shared packages. Use the packages; do not re-implement their features in a service.
- Long-running tasks must be idempotent, cancellable (`CancellationToken`) and resumable.
- Calls to providers need timeouts, retries with backoff, and rate-limit awareness.
- Secrets never in git; use environment variables / user-secrets / secret store.
- Structured logging and OpenTelemetry traces/metrics in every service.
- Authorization is enforced server-side in every service.
- Commits and PRs carry no AI attribution (no `Co-Authored-By`, no Anthropic email, no "Generated with" footer); see `git-commits`.

## Work tracking (GitHub)
- Repo and the Project "LeiMa" are **public**: no secrets, credentials, customer data or internal URLs in code, issues, PRs or docs.
- Plan work in the GitHub Project "LeiMa" (backlog, 2-week sprints, board). Features, stories, tasks, bugs are issues; the **issue number is the work item ID** in commits (`type: <ID> - summary`), branches (`type/<ID>-slug`) and PR titles. PR bodies use `Closes #<ID>`.
- Significant decisions are ADRs in `docs/adr/` (MADR, proposed by PR).
- Never commit, push, open PRs or change GitHub settings until the user has reviewed the changes. Details in `github-workflow` and `git-commits`.

## Skills (use them when the task matches)
| Skill | Use for |
|---|---|
| `dotnet-backend` | creating/changing .NET services |
| `react-frontend` | React UI work |
| `frontend-styling` | styling, design tokens, shared `@leima/ui` (same across all frontends) |
| `entity-framework` | DbContext, entities, migrations, queries |
| `testing` | writing/running tests |
| `code-review` | reviewing changes |
| `git-commits` | commits, branches, PRs |
| `github-workflow` | issues, Project board, sprints, labels, definition of ready/done |
| `documentation` | READMEs, ADRs, CLAUDE.md upkeep |
| `user-stories` | writing stories (as issues) and acceptance criteria |

## Maintaining this file
Update this CLAUDE.md in the same change whenever a service is added/removed/renamed, a stack decision changes, or a new cross-cutting convention is agreed. Keep it concise; service-specific detail belongs in `services/<name>/CLAUDE.md` (copy from `services/_template/CLAUDE.md`).
