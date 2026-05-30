# 10. Frontend — full Angular Material client, expanded phase track

Date: 2026-05-30
Status: Accepted

## Context

The plan (Phase 12) scoped the frontend as a **thin, demo-only** Angular SPA — a handful of
screens to show the workflow and the Keycloak login. In practice a full UI was designed in
Google Stitch ("CompanyOps Enterprise Suite", the "Precision Enterprise" design system):
~13 desktop screens — dashboard, requests (list/detail/create), approvals, audit logs,
assets, IT-admin dashboard, reports & analytics, integrations, system settings — plus mobile
variants. The maintainer wants to build the **full suite**, not a thin demo.

Two things follow: (1) the frontend scope grows well beyond "thin demo", and (2) several
designed screens have **no backing API yet** (assets, reporting, integrations, settings), so a
"full working frontend" requires the backend to grow alongside it.

Stitch generated standalone HTML + Tailwind (CDN) per screen, with a shared token config,
Material Symbols icons, dark mode, and responsive layout — high-quality, but framework-agnostic
markup, not Angular.

## Decision

1. **Reframe the frontend from "thin demo" to a full client UI** — the CompanyOps Enterprise
   Suite. The one principle that does **not** change: the SPA is a **client of the API with no
   business logic**; the API stays the source of truth and re-validates everything (authz,
   state transitions, audit). The SPA never makes authorization or workflow decisions.

2. **Angular Material (M3) is the UI component library** (locked-stack addition), with a
   **custom M3 theme built from the Precision-Enterprise palette + Inter type scale**, and
   Material Symbols icons. The Stitch designs (screenshots + HTML) are the **visual /
   information-architecture reference**; each screen is **rebuilt with Material components**,
   not ported from the Tailwind markup.

3. **The full frontend is an expanded phase track (12 → 18)**, each screen-group shipping with
   the backend slice it needs — reusing the request/approval/audit engine, and adding asset,
   reporting read-model, integration-status, and settings capabilities as required. See the
   phase feature table in `AGENTS.md`.

4. **Tooling:** the **Stitch MCP** is the design source (pull a screen's HTML + screenshot on
   demand); a **`stitch-port` skill** scaffolds a Material screen from a Stitch reference in the
   project's conventions; **`angular-guardian`** reviews frontend diffs; a **browser/preview
   MCP** gives lightweight visual checks against the Stitch screenshots.

## Options considered

- **Thin demo SPA (original plan).** Smallest scope, fastest to a login demo. Rejected: the
  maintainer wants the full designed suite; a demo would waste the Stitch work.
- **Reuse the Stitch Tailwind markup in Angular** (Tailwind-first, + CDK/Material where needed).
  Fastest and most pixel-faithful — the generated markup drops in almost directly and the token
  config becomes the Tailwind theme. Rejected in favour of Material despite the higher build
  cost.
- **Pure Angular Material, themed from the design tokens (chosen).** Accessible components out
  of the box (tables, dialogs, forms, menus, overlays), one idiomatic styling system, strongest
  long-term maintainability. Cost: every screen is rebuilt rather than ported, and the result is
  "Material-flavoured" — close to the Precision-Enterprise palette, not pixel-identical to the
  mockups.
- **Material-led hybrid** (Material components + Tailwind for layout). A middle ground; rejected
  to avoid running two styling systems in one codebase.

## Consequences

**Positive**
- A complete, demoable enterprise UI over the existing API and engine, exercising the full
  request → approval → fulfilment lifecycle end to end.
- Accessible, consistent components and theming from one system (Material M3); the design tokens
  give brand continuity.
- The phased track keeps each screen-group a reviewable vertical slice with its backend, rather
  than a big-bang UI drop.

**Negative / costs**
- **Bigger scope, more phases** (12 → 18) and a longer timeline.
- **The backend must grow** to back the designed screens: asset lifecycle (Phase 14, already
  planned), an IT-admin/fulfilment surface (15), reporting read-models/aggregations (16, a
  read-side / CQRS-flavoured addition), integration-status endpoints over the existing
  worker/outbox (17), and a settings/profile store (18). Each is a real slice, not just UI.
- **The Stitch Tailwind markup is reference-only** — rebuilding in Material is more work and
  won't reproduce the mockups pixel-for-pixel.
- Angular Material is a new locked-stack dependency to track (the versioning policy applies).

## Affects

- **AGENTS.md** — scope section (thin demo → full client), the locked-stack table
  (+ Angular Material), the phase feature table (split Phase 12, add 15–18), the tooling list
  (Stitch MCP + `stitch-port`).
- **docs/companyops_enterprise_dotnet_project_plan.md** — Phase 12 expanded; Phases 15–18 added.
- **docs/security.md** — the Phase-12 items already queued are prerequisites: split the Keycloak
  client into a public SPA client + a bearer-only API audience, restrict CORS to the SPA origin,
  and add a CSP at the edge.
- **Phases 12–18** — the frontend track and its backend slices.
