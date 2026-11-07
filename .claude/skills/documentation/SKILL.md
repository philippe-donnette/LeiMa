---
name: documentation
description: Documentation standards for LeiMa (CLAUDE.md upkeep, READMEs, ADRs, code docs). Use when writing or updating docs, recording a decision, or when a change alters structure or conventions.
---

# Documentation

## When to use
Adding/changing services, making architectural decisions, or any change that makes existing docs stale.

## Conventions
- **Root `CLAUDE.md`**: repo map, services table, stack, cross-cutting rules. Update it whenever those change.
- **`services/<name>/CLAUDE.md`**: from `services/_template/CLAUDE.md`; purpose, API/events, data, run/test, gotchas.
- **ADRs** in `docs/adr/NNNN-kebab-title.md` (MADR format, from `0000-template.md`) for significant, hard-to-reverse decisions (broker choice, auth model, etc.):
  - One decision per ADR, one or two pages, state the decision drivers and the options considered.
  - Process: issue first, then a PR labelled `type:adr`; the ADR starts as `proposed` and is discussed on the PR. Once agreed, set the status to `accepted` (and the date) in a last commit of the same PR, then merge, so no follow-up edit is needed after the merge.
  - Statuses: `proposed`, `accepted`, `rejected`, `deprecated`, `superseded by NNNN`.
  - Accepted ADRs are immutable except the status line; to change a decision write a new ADR that supersedes it and update both statuses.
  - Add every ADR to the index in `docs/adr/README.md`.
- **Architecture** overview and diagrams in `docs/architecture/`.
- **User stories** live in GitHub issues (see `user-stories`, `github-workflow`); `docs/user-stories/` holds the template and optional long-form detail.
- READMEs: what it is, how to run, how to test; no duplication of CLAUDE.md content, link instead.
- Code docs: XML doc comments on public APIs of shared contracts; TSDoc for exported frontend utilities; comment the *why*, not the *what*.
- Keep CLAUDE.md files concise; facts verified against the code.

## Checklist
- [ ] Relevant CLAUDE.md files updated in the same change
- [ ] ADR added for new significant decisions
- [ ] Links valid

## Don'ts
- No stale docs left behind; delete or fix.
- No secrets or environment-specific values in docs. The repository is public: nothing internal, no credentials, no customer data.
