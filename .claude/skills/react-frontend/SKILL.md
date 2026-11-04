---
name: react-frontend
description: Guidelines for building React + TypeScript UI in LeiMa (structure, data fetching, state, accessibility). Use when creating or changing frontend code under web/.
---

# React frontend

## When to use
Creating or modifying an app in `web/<app>/`.

## Conventions
- React + TypeScript (strict), Vite, function components and hooks only.
- Feature folders: `src/features/<feature>/{components,hooks,api,types}`; shared bits in `src/shared`.
- Server state with TanStack Query; local UI state with `useState`/`useReducer`; avoid global stores unless justified.
- Typed API client per backend service, generated from OpenAPI where available; no `any`.
- Routing with React Router; guard routes using the user's permissions, but the server remains the authority.
- Long-running tasks: show status via polling or push (SSE/WebSocket), with clear progress, cancel and error states.
- Accessibility: semantic HTML, labelled controls, keyboard support.
- Styling is identical across all apps: follow the `frontend-styling` skill (Tailwind + shared `@leima/ui` design system).

## Structure
```
web/<app>/
  CLAUDE.md
  src/{app,features,shared}
  tests/ (or colocated *.test.tsx)
```

## Checklist
- [ ] Loading, empty, error states handled
- [ ] Tests with Vitest + Testing Library (see `testing` skill)
- [ ] Lint, type-check and build pass
- [ ] App `CLAUDE.md` updated

## Don'ts
- No business rules or permission decisions only in the UI.
- No data fetching in presentational components.
- No secrets in client code or env vars exposed to the bundle.
