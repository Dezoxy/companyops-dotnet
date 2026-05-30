---
name: new-angular-feature
description: Scaffold a new Angular feature for the CompanyOps client (the "Enterprise Suite") in the project's conventions — a feature folder with a standalone Angular Material component, a feature service (HTTP + signals), view models, a lazy route, and an optional role guard. Use when adding a screen/feature to frontend/ such as the requests list, request detail, an approval action, the audit log, or any suite screen. To start from a Stitch design, use the stitch-port skill (it builds on this).
---

# Scaffold an Angular feature (CompanyOps)

Create a cohesive, convention-correct feature under `frontend/src/app/features/`.
Read `frontend/CLAUDE.md` first and match what already exists in `frontend/` — the app shell
(sidenav + toolbar), the Angular **Material M3 theme** (built from the Precision-Enterprise
tokens, ADR 0010), and existing features. To begin from a designed Stitch screen, use
**`stitch-port`**, which wraps this skill.

## Inputs to confirm

- **Feature name** (kebab-case, e.g. `request-list`, `request-detail`, `audit-log`).
- **API endpoints** it calls (business actions, not CRUD — e.g. `GET /requests`,
  `POST /requests/{id}/approve`, `POST /requests/{id}/reject`, `…/submit`, `…/fulfill`).
  Approval is the one generic `/approve` (the actor's role + the chain pick the step — ADR
  0006), not role-named endpoints.
- **Role(s)** allowed to reach it (Employee / Manager / Finance / IT Admin / Auditor), if
  route-gated. Source of truth: `docs/security.md`.

## What to generate

Under `frontend/src/app/features/<feature>/`:

1. `<feature>.models.ts` — view models for this feature. Map API DTOs → these in the service;
   never pass raw HTTP shapes into components.
2. `<feature>.service.ts` — injectable service that owns the `HttpClient` calls, maps DTOs →
   view models, and exposes state as **signals** (`signal`/`computed`), plus `loading` and
   `error` signals.
3. `<feature>.component.ts` (+ template/styles) — **standalone** component using **Angular
   Material** components (`mat-table`, `mat-form-field`, `mat-card`, `mat-dialog`, `mat-menu`,
   `mat-icon` with Material Symbols, …), `@if`/`@for`, `OnPush`. Consumes the service's signals
   and renders **loading / error / empty** states. Presentational; no business logic.
4. `<feature>.routes.ts` — lazy route(s) for the feature.
5. If role-gated, wire the existing functional auth guard (`CanActivateFn`) and put required
   roles in route `data` — don't invent a new auth mechanism.

Then register the lazy route in the app's route config.

## Rules (do not violate)

- The SPA is a **client, not an authority**: no business rules or authorization *enforcement*
  in the component — only show/hide and call the API; the API re-validates everything.
- **Style via the M3 theme, not hardcoded values.** Use Material components + theme tokens /
  CSS custom properties for color, type, and spacing — no hex literals or ad-hoc palettes, so
  brand + light/dark stay consistent. Don't reimplement a Material component by hand.
- No secrets in any frontend file.
- Token attachment goes through the existing HTTP interceptor — never attach tokens per call.
- Strict types; accessible by default (Material handles most a11y — keep labels, focus, and
  keyboard correct).

## After scaffolding

- `ng lint` and `ng test` pass.
- Run the **angular-guardian** subagent on the diff.
