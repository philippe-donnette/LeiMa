---
name: frontend-styling
description: Single styling approach and design system rules shared by every LeiMa React frontend (tokens, Tailwind, shared UI package, theming). Use when styling components, adding UI primitives, choosing colors/spacing, or creating a new frontend app.
---

# Frontend styling

## When to use
Any visual work in `web/`: styling a component, adding a UI primitive, theming, or bootstrapping a new app.

## Conventions
- **One approach for all apps**: Tailwind CSS, driven by shared design tokens. Do not introduce CSS-in-JS, CSS modules, or another UI kit in an app.
- **Shared design system** in `web/packages/ui` (workspace package `@leima/ui`): design tokens (colors, spacing, typography, radii, shadows), the Tailwind preset consuming them, and reusable primitives (Button, Input, Table, Dialog, Badge, StatusIndicator, ...).
- Apps import the preset and components from `@leima/ui`; they never redefine tokens or copy primitives. Missing primitive? Add it to `@leima/ui` first.
- Tokens only: no hard-coded hex colors, pixel spacing, or font sizes in app code. Use semantic names (`bg-surface`, `text-muted`, `status-failed`) over raw palette names.
- Light and dark themes via CSS variables on `:root` / `[data-theme="dark"]`; every color token has both values.
- Status vocabulary is consistent across apps (e.g. queued, running, succeeded, failed, cancelled, throttled) with one color + icon each; never rely on color alone.
- Accessibility: WCAG AA contrast, visible focus ring, respects `prefers-reduced-motion`.
- Responsive: mobile-first breakpoints from the preset; dense data tables are the norm, so provide a compact density option.
- Icons from a single library chosen in `@leima/ui` (record it in `web/packages/ui/CLAUDE.md`).
- Class composition via a small helper (`clsx` + `tailwind-merge`) exported from `@leima/ui`; component variants with `cva`.

## Structure
```
web/packages/ui/
  CLAUDE.md
  src/tokens   tailwind-preset   components   theme
web/<app>/  uses @leima/ui, own tailwind.config extends the preset
```

## Checklist
- [ ] Only tokens/semantic classes used, no magic values
- [ ] Works in light and dark
- [ ] New shared component added to `@leima/ui` with a test (see `testing`)
- [ ] `web/packages/ui/CLAUDE.md` updated when tokens or primitives change

## Don'ts
- No per-app overrides of shared tokens.
- No inline `style` for static styling.
- No copy-pasted components between apps.
