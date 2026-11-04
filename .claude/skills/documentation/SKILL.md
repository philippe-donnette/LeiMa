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
- **ADRs** in `docs/adr/NNNN-kebab-title.md` from `0000-template.md` for significant, hard-to-reverse decisions (broker choice, auth model, etc.). Never rewrite accepted ADRs; supersede them.
- **Architecture** overview and diagrams in `docs/architecture/`.
- **User stories** in `docs/user-stories/` (see `user-stories`).
- READMEs: what it is, how to run, how to test; no duplication of CLAUDE.md content, link instead.
- Code docs: XML doc comments on public APIs of shared contracts; TSDoc for exported frontend utilities; comment the *why*, not the *what*.
- Keep CLAUDE.md files concise; facts verified against the code.

## Checklist
- [ ] Relevant CLAUDE.md files updated in the same change
- [ ] ADR added for new significant decisions
- [ ] Links valid

## Don'ts
- No stale docs left behind; delete or fix.
- No secrets or environment-specific values in docs.
