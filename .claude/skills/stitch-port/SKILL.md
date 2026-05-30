---
name: stitch-port
description: Turn a Stitch "CompanyOps Enterprise Suite" screen into an Angular Material feature in the project's conventions — pull the screen's reference (screenshot + HTML), rebuild it with Material components themed to the design tokens, and wire it to the real API. Use when implementing a designed screen (dashboard, requests list/detail/create, approvals, audit, assets, IT-admin, reports, integrations, settings) during the frontend track (Phases 12–18).
---

# Port a Stitch screen → Angular Material (CompanyOps)

The frontend is the **CompanyOps Enterprise Suite** (ADR 0010). Each screen was designed in
Google Stitch as HTML + Tailwind; we **rebuild it in Angular Material** — the Stitch output is
the **visual + information-architecture reference**, never copy-pasted markup.

This skill is `new-angular-feature` with a Stitch reference on the front. **Read
`new-angular-feature/SKILL.md` and follow its file structure** (models + signals service +
standalone component + lazy route + guard); this file only adds the *get-the-design* and
*translate-to-Material* steps. Read `frontend/CLAUDE.md` and match what already exists in
`frontend/` (the app shell, the M3 theme, existing features).

## 1. Get the reference

The screens live in Stitch project **`9358357771147700605`** ("CompanyOps Enterprise Suite",
design system "Precision Enterprise"). Two ways in — prefer whichever is present:

- **Local export** (fastest): `~/Downloads/stitch_companyops_enterprise_suite*/<screen>/` —
  each has `code.html` (Tailwind markup) + `screen.png` (the render). Read both. Prefer the
  most complete iteration if several exist.
- **Stitch MCP:** `mcp__stitch__list_screens` (projectId `9358357771147700605`) → find the
  screen → `mcp__stitch__get_screen` → WebFetch the `htmlCode.downloadUrl` for the markup;
  the `screenshot.downloadUrl` is the visual target.

The page-by-page design intent is also in `docs/companyops_angular_stitch_prompts.md`.

## 2. Extract the design intent (don't transcribe markup)

From the screenshot + HTML, list:
- **Layout regions** — the screen sits *inside the app shell* (sidenav + toolbar already
  exist); port only the page content, not the chrome.
- **Component inventory** → the Material component for each: tables → `mat-table` (+
  `matSort`/`mat-paginator`), forms → `mat-form-field` + `matInput`/`mat-select`/`mat-datepicker`,
  dialogs → `MatDialog`, menus → `mat-menu`, tabs → `mat-tabs`, cards → `mat-card`, status
  pills → a themed `mat-chip`/badge, icons → `mat-icon` (Material Symbols).
- **The data shown** — table columns, form fields, detail sections, status values — and which
  **API DTO** supplies each. The columns/fields are the spec; the mock values in the HTML are not.
- **The actions** — buttons and what API call each maps to, and the role that gates it.

## 3. Translate to Material (the rules that matter)

- **Material components, not ported HTML.** Recreate the layout with Material + Angular CDK
  layout; do not paste Tailwind classes or the Stitch `tailwind.config`.
- **Theme, don't hardcode.** Colors/typography/spacing come from the app's **M3 theme** (built
  from the Precision-Enterprise tokens). No hex literals, no per-component palettes — use theme
  classes / CSS custom properties so light/dark + brand stay consistent.
- **Real data, no mocks.** The service binds to the real endpoints and maps DTOs → view models;
  the shipped component renders live data with **loading / error / empty** states (Material
  progress bar / error card / empty state), not the Stitch placeholder rows.
- **Responsive.** Match the design's desktop + mobile behaviour with Material/CDK breakpoints
  (the sidenav already collapses in the shell).
- **Client, not authority.** Show/hide actions by role for UX, but the **API enforces** —
  no business or authorization decisions in the component (ADR 0010 / angular-guardian).

## 4. Generate

Follow `new-angular-feature`: `frontend/src/app/features/<feature>/` with `<feature>.models.ts`,
`<feature>.service.ts` (HttpClient + signals, DTO→VM mapping), `<feature>.component.ts`
(standalone, Material template, `@if`/`@for`, loading/error/empty), `<feature>.routes.ts`
(lazy), and the role guard wired via route `data`. Register the lazy route. Token attachment
goes through the existing HTTP interceptor.

## After porting

- `ng lint` + `ng test` pass.
- **Visually check against the Stitch screenshot** — if a browser/preview MCP is available
  (Claude-in-Chrome / Preview), run the screen and compare layout/IA to `screen.png` (it won't
  be pixel-identical — Material-flavoured is expected; check structure, content, and that the
  theme reads as Precision-Enterprise).
- Run the **angular-guardian** subagent on the diff.
