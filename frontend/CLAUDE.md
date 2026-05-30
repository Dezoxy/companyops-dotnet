# Frontend (Angular SPA) — rules

A **thin demo UI** for CompanyOps. Its job is to show the approval workflow and
the Keycloak login flow end-to-end — not to be a product. Keep it lean.

## Stack

- **Node.js 24 LTS** ("Krypton") for the toolchain.
- **Angular 21**, standalone components (no NgModules), **signals** for state.
  (Latest stable, not the LTS-tagged v20 — see the versioning policy in `AGENTS.md`.
  Move to Angular 22 once it leaves RC.)
- New control flow (`@if` / `@for` / `@switch`), not the legacy structural directives.
- TypeScript strict mode on.
- Auth: **`angular-auth-oidc-client`** — OIDC Authorization Code flow + **PKCE**
  against Keycloak as a **public client**. No client secret in the SPA.
- HTTP via `HttpClient` + an interceptor that attaches the access token.

## Hard rules

- **The SPA is a client, not an authority.** No business rules, no workflow
  validation logic, no authorization *decisions* live here. The API is the source
  of truth and re-validates everything. The UI may *hide* a button the user can't
  use, but the API still enforces it.
- **No secrets in the frontend.** No client secrets, API keys, or connection
  strings. Anything shipped to the browser is public.
- **Talk to the backend only over the documented API.** No direct DB/queue access.
- Map API DTOs to view models in services — don't scatter raw HTTP shapes through
  components.

## Conventions

- Feature-first folders: `src/app/features/<feature>/` holds its components,
  service, models, and routes together. Shared UI in `src/app/shared/`, core
  singletons (auth, interceptors, guards) in `src/app/core/`.
- One **service per feature** owns HTTP calls and exposes signals; components stay
  presentational where practical.
- Lazy-load feature routes. Protect authenticated routes with a functional guard
  (`CanActivateFn`) that checks the OIDC session; reflect roles
  (Employee / Manager / Finance / IT Admin / Auditor) in route data + UI.
- `OnPush`-friendly: prefer signals/`computed` over manual change detection.
- Dev server proxies `/api` to the backend (`proxy.conf.json`) to avoid CORS pain
  locally; the API configures real CORS for deployed origins.
- Keep it demo-grade but not sloppy: basic loading/error states, sensible empty
  states, and accessible forms (labels, focus, keyboard).

## What this UI needs to demo (minimum)

- Keycloak login/logout.
- Create a request; list my requests with their status.
- Role-gated actions wired to the business endpoints: submit, approve-manager,
  approve-finance, reject, fulfill, cancel.
- Read-only audit log view (visible to Auditor).
