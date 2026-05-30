---
name: new-angular-feature
description: Scaffold a new Angular feature for the CompanyOps demo SPA in the project's conventions — a feature folder with a standalone component, a feature service (HTTP + signals), view models, lazy route, and an optional role guard. Use when adding a screen/feature to frontend/ such as request list, request detail, an approval action, or the audit log view.
---

# Scaffold an Angular feature (CompanyOps)

Create a cohesive, convention-correct feature under `frontend/src/app/features/`.
Read `frontend/CLAUDE.md` first and match what already exists in `frontend/`.

## Inputs to confirm

- **Feature name** (kebab-case, e.g. `request-list`, `request-detail`, `audit-log`).
- **API endpoints** it calls (from the backend, e.g. `GET /requests`,
  `POST /requests/{id}/approve-manager`).
- **Role(s)** allowed to reach it (Employee / Manager / Finance / IT Admin /
  Auditor), if route-gated.

## What to generate

Under `frontend/src/app/features/<feature>/`:

1. `<feature>.models.ts` — view models for this feature. Map API DTOs to these in
   the service; don't pass raw HTTP shapes into components.
2. `<feature>.service.ts` — injectable service that owns the `HttpClient` calls,
   maps DTOs → view models, and exposes state as **signals**
   (`signal`/`computed`), plus loading and error signals.
3. `<feature>.component.ts` (+ template/styles) — **standalone** component, uses
   `@if`/`@for`, consumes the service's signals, renders loading/error/empty
   states. Presentational where practical; no business logic.
4. `<feature>.routes.ts` — lazy route(s) for the feature.
5. If role-gated, wire the existing functional auth guard (`CanActivateFn`) and
   put required roles in route `data` — don't invent a new auth mechanism.

Then register the lazy route in the app's route config.

## Rules (do not violate)

- The SPA is a **client, not an authority**: no business rules or authorization
  *enforcement* in the component — only show/hide and call the API. The API
  re-validates everything.
- No secrets in any frontend file.
- Token attachment goes through the existing HTTP interceptor — don't add tokens
  manually per call.
- Keep it demo-grade but clean: strict types, loading/error states, accessible
  forms.

## After scaffolding

- `ng lint` and `ng test` should pass.
- Suggest running the **angular-guardian** subagent on the diff.
