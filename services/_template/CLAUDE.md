# <service-name>

> Copy to `services/<service-name>/CLAUDE.md`. Keep it short and current; update it when the service changes.

## Purpose
One or two sentences.

## Responsibilities
- What it owns / what it does not.

## Public API / events
- Endpoints, published and consumed events.

## Data
- Own database, main entities, migration notes.

## Dependencies
- Other services, external providers, infrastructure.

## Project layout
- `src/<Service>.Api`, `.Application`, `.Domain`, `.Infrastructure`, `tests/`

## Run / test
```bash
dotnet run --project src/<Service>.Api
dotnet test
```

## Conventions / gotchas
- Anything non-obvious a newcomer would get wrong.
