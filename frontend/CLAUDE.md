# Frontend (Angular client) — rules

The **CompanyOps Enterprise Suite** — a full Angular client of the API
([ADR 0010](../docs/decisions/0010-frontend-full-client-angular-material.md)), built across
Phases 12–20. It is a **client, not a product backend**: every screen is a view over the API.

## Stack

- **Node.js 24 LTS** ("Krypton") for the toolchain.
- **Angular 21**, standalone components (no NgModules), **signals** for state. (Latest stable,
  not the LTS-tagged v20 — see the versioning policy in `AGENTS.md`. Move to v22 once it leaves RC.)
- New control flow (`@if` / `@for` / `@switch`), not the legacy structural directives.
- **Angular Material (M3)** is the component library. The theme is a custom M3 theme whose
  `--mat-sys-*` system tokens are overridden with the **Precision Enterprise** design tokens
  in `src/styles.scss`. Icons are **Material Symbols Outlined** (the default `mat-icon` font set).
- TypeScript strict mode on. Tests run on **Vitest** (the Angular 21 default, jsdom).
- Auth: **`angular-auth-oidc-client`** — OIDC Authorization Code + **PKCE** against Keycloak's
  public `companyops-spa` client. No client secret in the SPA. `AuthService` (`core/auth/`) wraps
  it and exposes the session as signals; `core/auth/auth.guard.ts` has `authGuard` + `roleGuard`.
  **Local login:** the OIDC `authority` (`environment.development.ts`) must match the token
  **issuer** the browser sees — run dev Keycloak with `KC_HOSTNAME=http://localhost:8080` (or map
  `keycloak` → `127.0.0.1` in `/etc/hosts` and use the `keycloak:8080` authority). `ng serve`
  proxies `/api` → `http://localhost:5080` (`proxy.conf.json`).
- HTTP via `HttpClient` + an interceptor that attaches the access token.

## Hard rules

- **The SPA is a client, not an authority.** No business rules, workflow validation, or
  authorization *decisions* live here. The API is the source of truth and re-validates
  everything. The UI may *hide/disable* a control, but the API still enforces it.
- **No secrets in the frontend.** No client secrets, API keys, or connection strings — anything
  shipped to the browser is public.
- **Talk to the backend only over the documented API.** No direct DB/queue access.
- **Map API DTOs to view models in services** — don't scatter raw HTTP shapes through components.

## Conventions

- **Style via the M3 theme, never hardcoded values.** Use Material components + theme tokens
  (`var(--mat-sys-*)`); no hex literals or ad-hoc palettes, so brand + light/dark stay
  consistent. Don't hand-roll a component Material already provides.
- Feature-first folders: `src/app/features/<feature>/` holds its component(s), service, models,
  and routes. Shared UI in `src/app/shared/`, core singletons (auth, interceptors, guards) in
  `src/app/core/`. The app shell (sidenav + toolbar) lives in `app.ts`/`app.html`.
- One **service per feature** owns HTTP calls and exposes signals; components stay presentational.
- Lazy-load feature routes (`loadComponent`). Protect authenticated routes with a functional
  guard (`CanActivateFn`) checking the OIDC session; reflect roles (Employee / Manager / Finance
  / IT Admin / Auditor) in route `data` + the UI.
- Approval is the one generic action `POST /requests/{id}/approve` (+ `…/reject`) — the actor's
  role + the chain pick the step (ADR 0006), not role-named endpoints.
- Signals/`computed` over manual change detection; render loading / error / empty states.
- Dev server proxies `/api` to the backend (`proxy.conf.json`) to avoid local CORS; the API sets
  real CORS for deployed origins. (Added with the API-client chunk.)
- To port a designed screen, use the **`stitch-port`** skill; for a fresh feature,
  **`new-angular-feature`**. Run **angular-guardian** on the diff.

## Commands

```bash
npm install          # from frontend/
npx ng serve         # dev server (proxies /api once configured)
npx ng build         # production build
npx ng test --watch=false   # Vitest unit tests
npx ng lint          # ESLint
```

## The suite (Phases 12–20)

Foundation (12, ✅) → Auth & API client (13) → Core workflow UI: Dashboard / Requests
(list/detail/create) / Approvals / Audit (14) → Helpdesk (15) → Assets (16) → IT-Admin console
(17) → Reports (18) → Integrations (19) → Settings (20).
Build each screen with its backend slice; never invent business logic to fill a screen.
