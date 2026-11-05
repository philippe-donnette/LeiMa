---
name: github-workflow
description: How LeiMa plans and tracks work on GitHub (public Project "LeiMa", issue types and hierarchy, labels, sprints, definition of ready/done, gh recipes). Use when creating or triaging issues, planning a sprint, updating the Project board, or linking work to PRs.
---

# GitHub workflow

## When to use
Creating or refining features, stories, tasks, bugs, spikes or chores; planning or reviewing a sprint; linking commits and PRs to work items.

## Conventions
- **The Project "LeiMa" is the planning hub** (public, owned by `philippe-donnette`): backlog, sprint planning, sprint board, roadmap. Create and sort work there.
- **Every work item is a real issue.** The issue number is the work item ID used in commits, branches and PRs (see `git-commits`). Draft project items are for brainstorming only; convert them to issues before work starts.
- **Types** (no native Issue Types on a personal account): Project field `Issue type` (Feature, Story, Task, Bug, Spike, Chore) plus the matching label `type:feature|story|task|bug|spike|chore|adr`. Set both when creating.
- **Hierarchy** with native sub-issues: Feature → Story → Task. Bugs and spikes stand alone or sit under a feature.
- **Project fields**: Status (Backlog, Todo, In progress, To be reviewed, In review, Reviewed, Done), Issue type, Sprint (2-week iteration), Priority (P0–P3), Size (XS–XL), Service (platform, web; add an option per new service). Priority, status and size live in the Project, not in labels.
- **Labels**: `type:*`, `svc:<service>` (one per service, plus `svc:platform`), `area:web|docs|infra`.
- **Public repository and Project**: no secrets, provider credentials, customer data or internal URLs in issues, comments, screenshots or logs.

## Writing items
- Story: body follows `docs/user-stories/_template.md` (As a / I want / so that, Given-When-Then criteria, out of scope). See `user-stories`.
- Task: one line of intent, a short checklist, link to the parent story.
- Bug: expected vs actual, steps to reproduce, affected service, logs without secrets.
- Spike: question to answer, timebox, expected output (usually an ADR).
- Decisions that are significant and hard to reverse become an ADR (see `documentation`): issue first, then a PR with label `type:adr`.

## Definition of Ready (to enter a sprint)
- [ ] Clear goal and acceptance criteria (stories), reproducible steps (bugs)
- [ ] Size set; split if larger than L
- [ ] Permissions, failure cases and provider limits considered
- [ ] Parent feature and `svc:` label set; dependencies linked

## Definition of Done
- [ ] PR merged with `Closes #<ID>` in the description
- [ ] Tests added and passing; build green
- [ ] Docs updated: service `CLAUDE.md`, root `CLAUDE.md`, ADR if a decision was made
- [ ] Item moved to Done (automatic on merge/close)

## Sprint rhythm (2 weeks)
- **Planning**: items that meet the definition of ready move from Backlog to Todo and into the next sprint, sized to capacity; sprint goal in one sentence.
- **During**: Status moves Todo → In progress (branch created) → To be reviewed (PR opened, waiting for a reviewer) → In review (reviewer has started) → Reviewed (review finished) → Done (PR merged). If the review is rejected, Reviewed goes back to In progress for rework, then To be reviewed again.
- **Weekly triage**: items without Status, Issue type or Size.
- **End**: move unfinished items to the next sprint, write a short retro note.

## Linking work
- Branch `type/<ID>-slug`; commits `type: <ID> - summary`; PR title the same, body includes `Closes #<ID>` (use `Refs #<ID>` when the PR doesn't finish the item).
- One PR per issue where practical; PRs of a story reference the story, not each task.

## gh recipes
```bash
# create an issue directly in the Project
gh issue create --title "..." --body-file body.md --label "type:story,svc:platform" --project "LeiMa"

# make #<child> a sub-issue of #<parent> (needs the child's database id, not its number)
child_id=$(gh api repos/philippe-donnette/LeiMa/issues/<child> --jq .id)
gh api repos/philippe-donnette/LeiMa/issues/<parent>/sub_issues -F sub_issue_id=$child_id

# list the Project's items and fields
gh project item-list <project-number> --owner philippe-donnette
gh project field-list <project-number> --owner philippe-donnette
```
Setting Project fields on an item uses `gh project item-edit` with project, item and field ids from `item-list`/`field-list`.

## Don'ts
- No work without an issue (apart from `chore: 0` housekeeping agreed with the user).
- Don't create labels or fields ad hoc; extend this skill first.
- Don't leave items in the sprint without Status or Size.
