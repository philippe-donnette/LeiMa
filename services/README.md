# Services

One folder per microservice, named in lowercase kebab-case (e.g. `task-orchestrator`).

## Adding a service
1. Create `services/<name>/` (use the `dotnet-backend` skill for layout).
2. Copy `_template/CLAUDE.md` to `services/<name>/CLAUDE.md` and fill it in.
3. Add the service to the table in the root `CLAUDE.md`.
4. Record notable decisions in `docs/adr/`.
