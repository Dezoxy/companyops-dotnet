---
name: figma-port
description: Turn a Figma "CompanyOps Enterprise Suite" screen into an Angular Material feature in the project's conventions — pull the screen's reference (screenshot + structure + tokens) from the Figma MCP, rebuild it with Material components themed to the design tokens, and wire it to the real API. Use when implementing a designed screen (dashboard, requests list/detail/create, approvals, audit, assets, IT-admin, reports, integrations, settings) during the frontend track (Phases 12–20).
---

# Port a Figma screen → Angular Material (CompanyOps)

The frontend is the **CompanyOps Enterprise Suite** ([ADR 0010](../../../docs/decisions/0010-frontend-full-client-angular-material.md),
design source updated by [ADR 0011](../../../docs/decisions/0011-design-source-figma.md)). Each
screen is designed in **Figma**; we **rebuild it in Angular Material** — the Figma output is the
**visual + information-architecture reference**, never copy-pasted markup or emitted code.

This skill is `new-angular-feature` with a Figma reference on the front. **Read
`../new-angular-feature/SKILL.md` and follow its file structure** (models + signals service +
standalone component + lazy route + guard); this file only adds the *get-the-design* and
*translate-to-Material* steps. Read `frontend/CLAUDE.md` and match what already exists in
`frontend/` (the app shell, the M3 theme, existing features).

## 1. Get the reference (Figma MCP)

The suite lives in one Figma file — **file key `EX9DRVlslQwRgRTPojErC6`** ("CompanyOps
Enterprise Suite", "Precision Enterprise" design system). Don't hardcode node ids — discover
them, because the file holds variants and gets edited:

- **List screens:** `get_metadata` with `fileKey` and **no** `nodeId` → top-level pages; then
  `get_metadata` with the page `nodeId` (e.g. `0:1`) → the top-level frames (the screens), each
  with id, name, and size. Pick the frame whose name matches the screen (e.g. "CompanyOps
  Approvals Management"). If two variants exist, confirm which is current before building.
- **Visual target:** `get_screenshot` (`fileKey` + the frame `nodeId`) → a PNG URL; download it
  and compare layout/IA. Bump `maxDimension` for fine detail.
- **Exact tokens:** `get_variable_defs` (`fileKey` + `nodeId`) → the real colour/spacing/type
  variables for that node — use these to confirm the screen maps onto the app's M3 theme tokens.
- **Design→code context:** `get_design_context` (`fileKey` + `nodeId`) → reference code +
  screenshot + metadata. The code is **reference only** — do not paste it.

URL → ids: `figma.com/design/:fileKey/:name?node-id=1-2` → `fileKey` is `:fileKey`, `nodeId` is
`1:2` (convert `-` to `:`). The per-page design intent is also in
`docs/companyops_angular_stitch_prompts.md` (historical generation prompts, still useful as IA).

## 2. Extract the design intent (don't transcribe markup)

From the screenshot + metadata, list:
- **Layout regions** — the screen sits *inside the app shell* (sidenav + toolbar already exist);
  port only the page content, not the chrome.
- **Component inventory** → the Material component for each: tables → `mat-table` (+
  `matSort`/`mat-paginator`), forms → `mat-form-field` + `matInput`/`mat-select`/`mat-datepicker`,
  dialogs → `MatDialog`, menus → `mat-menu`, tabs → `mat-tabs`, cards → `mat-card`, status
  pills → the shared `app-status-chip`, icons → `mat-icon` (Material Symbols).
- **The data shown** — table columns, form fields, detail sections, status values — and which
  **API DTO** supplies each. The columns/fields are the spec; the mock values in Figma are not.
  Figma mockups carry domain-absent fields (human `REQ-####` ids, Priority, requester
  names/avatars, line-items, integration health) — **do not render what the API can't supply.**
- **The actions** — buttons and what API call each maps to, and the role that gates it.

## 3. Translate to Material (the rules that matter)

- **Material components, not emitted markup.** Recreate the layout with Material + Angular CDK
  layout; do not paste Figma's code or any Tailwind/React output.
- **Theme, don't hardcode.** Colours/typography/spacing come from the app's **M3 theme** (built
  from the Precision-Enterprise tokens) — `var(--mat-sys-*)` and the app's `--app-tone-*` status
  tokens. No hex literals, no per-component palettes. Use `get_variable_defs` to confirm the map.
- **Real data, no mocks.** The service binds to the real endpoints and maps DTOs → view models;
  the shipped component renders live data with **loading / error / empty** states, not the Figma
  placeholder rows.
- **Responsive.** Match the design's desktop + mobile behaviour with Material/CDK breakpoints
  (the sidenav already collapses in the shell; Figma has mobile variants to reference).
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
- **Visually check against the Figma screenshot** — re-`get_screenshot` the frame and compare
  layout/IA to the built screen (it won't be pixel-identical — Material-flavoured is expected;
  check structure, content, and that the theme reads as Precision-Enterprise). A browser/preview
  MCP can render the running screen, but the suite is behind `authGuard`, so a live check needs
  the full stack + login.
- Run the **angular-guardian** subagent on the diff.
