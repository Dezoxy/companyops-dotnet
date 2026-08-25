# CompanyOps Enterprise Suite (Angular client)

The full client for the CompanyOps API — dashboard, requests, approvals, fulfilment, assets,
audit, reports, integrations, and settings. It is a **client, not a second backend**: every
screen is a view over the API, which re-validates everything
([ADR 0010](../docs/decisions/0010-frontend-full-client-angular-material.md)).

Conventions and hard rules for changing this code live in [CLAUDE.md](CLAUDE.md).

## Stack

| | |
|---|---|
| Framework | **Angular 21** — standalone components, signals, new control flow (`@if`/`@for`) |
| UI | **Angular Material (M3)** on a custom "Precision Enterprise" theme (`src/styles.scss`); Material Symbols Outlined icons |
| Auth | `angular-auth-oidc-client` — OIDC Authorization Code + **PKCE** against Keycloak's public `companyops-spa` client. No secret in the bundle. |
| Tests | **Vitest** (jsdom) — the Angular 21 default |
| Toolchain | Node.js 24 LTS |

## Run it

The SPA needs the backend running — bring the stack up first
([docs/local-development.md](../docs/local-development.md)), then:

```bash
npm install
npx ng serve
```

<http://localhost:4200>. The dev server proxies `/api` → `http://localhost:5080`
(`proxy.conf.json`, which strips the prefix), so there is no local CORS to configure.

> **Login gotcha:** the SPA's dev `authority` is `http://localhost:8080/realms/companyops`, but
> the local compose file runs Keycloak with `KC_HOSTNAME=http://keycloak:8080` — issuer mismatch,
> so browser login fails against the stack as shipped. The two fixes are written up in
> [docs/local-development.md → The Angular SPA](../docs/local-development.md#the-angular-spa).

## Checks (what CI runs)

```bash
npx ng build              # production build
npx ng lint               # ESLint
npx ng test --watch=false # Vitest unit tests
```

All three run in the `frontend` job of `.github/workflows/ci.yml` and must be green.
There are no end-to-end tests — a known gap, tracked in
[docs/production-readiness.md](../docs/production-readiness.md) §8.

## Layout

```text
src/app/
├── core/          # singletons: auth (OIDC session, guards, token interceptor), theme
├── shared/        # cross-feature UI + API types (PagedResultDto, status chips)
├── features/      # one folder per screen group: component(s) + service + models + routes
│   ├── dashboard/  requests/  approvals/  fulfilment/  assets/
│   └── audit/  reports/  integrations/  settings/
├── app.ts / app.html   # the shell: sidenav, toolbar, handset bottom-nav + FAB
└── app.routes.ts       # lazy routes, each behind authGuard / roleGuard
```

One **service per feature** owns the HTTP calls, maps DTOs to view models, and exposes signals;
components stay presentational. Route role guards are **UX only** — they hide what the user
can't do; the API is what enforces it.

## Design source

The visual reference is the Figma "CompanyOps Enterprise Suite" file
([ADR 0011](../docs/decisions/0011-design-source-figma.md)); screens are rebuilt in Angular
Material against the design tokens rather than ported from emitted markup. The screen-by-screen
upgrade to that design — and what was deliberately simplified out because the domain doesn't
back it (SLA countdowns, line items, asset specs) — is recorded in
[docs/ui-upgrade-plan.md](../docs/ui-upgrade-plan.md).
