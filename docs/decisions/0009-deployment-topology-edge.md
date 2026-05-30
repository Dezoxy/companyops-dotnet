# 9. Deployment topology — Traefik edge, TLS termination, env-driven secrets

Date: 2026-05-30
Status: Accepted

## Context

Phase 11 deploys the Compose stack to a Linux VM and automates the surrounding
infrastructure. Until now everything ran locally: host-bound ports, dev throwaway
secrets, HTTP-only, and a dev Keycloak realm (ROPC, no TLS, wildcard origins — flagged
dev-only in [security.md](../security.md)). A deployed environment needs a single TLS
entry point, real secrets, the app to know it sits behind a proxy, and a hardened
identity realm.

Constraints from the project context: no cloud chosen yet (evaluating), cost-conscious,
EU/GDPR data residency. The plan's deliverable is "deploy the Compose stack to a Linux
VM" — i.e. an IaaS/VM target, not managed PaaS.

## Decision

1. **Traefik v3 as the edge reverse proxy** (locked stack addition). Single public entry
   point; terminates TLS; routes by Host rule to the API (and the SPA in Phase 12) via
   Docker-provider label discovery. Chosen over Caddy/nginx — see options.

2. **TLS at the edge via Let's Encrypt (ACME).** Traefik does the HTTP(80)→HTTPS(443)
   redirect and serves HSTS + security headers via middleware. The app speaks HTTP
   in-cluster; app-level `UseHttpsRedirection` stays **off** (`Security:EnableHttpsRedirection=false`,
   the Phase 10 fail-safe default) — the edge owns the redirect, so there is no app-level
   redirect-loop.

3. **`ForwardedHeaders` in the API**, trusting `X-Forwarded-Proto`/`-For` **only** from the
   Docker network (`KnownNetworks`). The API then sees the real `https` scheme and client
   IP without being spoofable from the public side.

4. **A production Compose overlay** (`docker-compose.prod.yml`) layered on the base file:
   adds Traefik, puts the API and Keycloak behind it (labels), runs `Production`, and
   replaces **every** dev throwaway secret with `${ENV}` values sourced from an untracked
   `.env` (`.env.example` documents the keys; gitleaks stays green). Internal services keep
   their `127.0.0.1` bindings — localhost-only on the VM, behind the host firewall.

5. **Keycloak in production mode with a hardened realm.** Keycloak runs `start` (prod mode)
   behind Traefik with a stable public `KC_HOSTNAME`, backed by Postgres (its own database).
   The prod realm disables direct access grants (ROPC), sets `sslRequired: external`, and
   pins redirect URIs / web origins — resolving the security.md Phase 11 item. The dev realm
   (`realm-companyops.json`) remains local-only.

6. **IaC: a Terraform example (Azure, EU) provisions the VM; Ansible configures it.**
   Terraform (azurerm) creates VM + network + NSG (22/80/443) + public IP in an EU region,
   written as an **example** (not auto-applied; the AWS equivalent is noted). Ansible
   installs Docker, the firewall, deploys the stack, and installs the backup cron. The
   Compose and Ansible layers are **cloud-agnostic** (any Linux VM); only Terraform is
   per-provider.

## Options considered

- **Reverse proxy — Traefik (chosen)** vs Caddy vs nginx+certbot. Traefik: Docker-native
  label discovery, built-in ACME, dashboard, concepts transfer to k8s Ingress. Caddy:
  simplest auto-HTTPS but less container-native/transferable. nginx+certbot: ubiquitous
  but manual TLS automation and more moving parts.
- **TLS at the edge (chosen)** vs app-level termination. Edge termination is standard for
  containerized apps and keeps certificate management out of the application.
- **IaC target — Azure example (chosen)** vs AWS/GCP (noted equivalents) vs homelab
  (Proxmox/libvirt). Azure pairs naturally with .NET and has strong EU regions;
  Terraform-as-example demonstrates the IaC skill without incurring cloud cost, while the
  runnable artifacts work on any VM.
- **Managed PaaS** (App Service + managed Postgres) instead of VM+Compose: less ops burden,
  but the plan's deliverable is a VM deploy. PaaS is recorded as the enterprise-optional
  cloud-native path in the deployment docs.

## Consequences

**Positive**
- One hardened TLS entry point; internal services are not externally reachable.
- Secrets leave git; prod runs on real env-driven credentials.
- The app is scheme/IP-aware behind the proxy, and the HTTP→HTTPS redirect is owned by the
  edge (no app-level redirect-loop).
- Identity is hardened for non-local use; closes the security.md transport + realm items.
- A cloud-agnostic, runnable deploy plus a portfolio-grade IaC example.

**Negative / costs**
- New infra component (Traefik) and an ACME/DNS prerequisite (a domain pointing at the VM)
  for real TLS — **not fully exercisable locally** (Let's Encrypt needs a public domain);
  local verification uses Traefik's self-signed default cert.
- Keycloak prod mode adds a database + configuration surface versus dev's in-memory
  `start-dev`.
- The Terraform example is not auto-applied (no cloud account assumed); it is validated
  (`fmt`/`validate`), not deployed by CI.
- More Compose files and env management; operators must populate `.env` from a secrets store.

## Affects

- **Phase 11** — `docker-compose.prod.yml` (Traefik + labels + env secrets), `ForwardedHeaders`
  in `Program.cs`, the hardened prod realm, Terraform (Azure) + Ansible + the backup cron +
  `docs/deployment.md`.
- **security.md** — transport (edge TLS, HSTS, `ForwardedHeaders`) and deployed-realm
  hardening move from TODO to implemented.
- **Phase 12** — the SPA slots behind the same Traefik edge; the realm's redirect URIs /
  web origins get the real SPA origin.
