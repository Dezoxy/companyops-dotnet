# CompanyOps — Deployment

How to deploy the production stack to a Linux VM. Topology and rationale: [ADR 0009](decisions/0009-deployment-topology-edge.md).
Day-2 operations: [runbook.md](runbook.md) · backups: [backup-restore.md](backup-restore.md).

> **What gets deployed:** the [`docker-compose.prod.yml`](../infra/docker-compose.prod.yml)
> stack behind a **Traefik** edge that terminates TLS (Let's Encrypt) and is the only public
> ingress. Everything is one origin, `APP_DOMAIN`: the **Angular SPA** is the site root, the
> **API** is under `APP_DOMAIN/api/*` (Traefik strips `/api`), and **Keycloak** is under
> `APP_DOMAIN/auth/*` (served there via `KC_HTTP_RELATIVE_PATH`, no strip). Postgres, RabbitMQ,
> and Redis stay internal.
>
> **Two layers, two scopes:** the Compose stack and the **Ansible** playbook are
> cloud-agnostic (any Ubuntu 24.04 VM). **Terraform** is the Azure provisioner — swap it for
> AWS, Hetzner, or a homelab VM and the rest is unchanged.
>
> **Images are prebuilt in CI and pulled from GHCR** ([ADR 0012](decisions/0012-release-driven-deployment.md)),
> not built on the VM. The release version (`COMPANYOPS_VERSION`) is the image tag.

## Two ways to deploy

- **Automated (recommended)** — publish a GitHub Release and the
  [`release.yml`](../.github/workflows/release.yml) workflow builds + pushes the images to GHCR,
  applies Terraform, and runs Ansible. One-time setup below; after that a deploy is "publish a
  release." [ADR 0012](decisions/0012-release-driven-deployment.md).
- **Manual** — run Terraform + Ansible yourself from a control machine (same playbook, same
  result). Good for the first bring-up or a CI-less box. Jump to [Manual deployment](#manual-deployment).

## Automated deployment (GitHub Release → Azure)

```
git tag v1.2.3 && gh release create v1.2.3   ─┐
   └─► build & push  ghcr.io/<owner>/companyops-{api,worker,fakeexternals,frontend}:1.2.3
   └─► (production environment approval)
   └─► terraform apply   → Azure VM + network (OIDC auth, remote state)
   └─► ansible-playbook  → VM: docker login ghcr, compose pull :1.2.3, up -d (migrator runs first)
```

### One-time setup

Done once; the pipeline can't create its own trust anchor. All commands assume `az login`
and the [`gh`](https://cli.github.com/) CLI authenticated to the repo.

**1. Terraform remote-state backend** — create the state storage, then record it:

```bash
cd infra/terraform
./bootstrap-state.sh                       # creates RG + storage account + container, prints values
cp backend.hcl.example backend.hcl         # paste the printed values (backend.hcl is gitignored)
```

**2. Azure OIDC app** (secret-less auth from Actions):

```bash
az ad app create --display-name companyops-deploy
APP_ID=$(az ad app list --display-name companyops-deploy --query '[0].appId' -o tsv)
az ad sp create --id "$APP_ID"

# Trust only this repo's 'production' environment (most restrictive subject):
az ad app federated-credential create --id "$APP_ID" --parameters '{
  "name": "companyops-prod",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:Dezoxy/companyops-dotnet:environment:production",
  "audiences": ["api://AzureADTokenExchange"]
}'

# Rights: manage infra + read/write remote state.
SUB=$(az account show --query id -o tsv)
az role assignment create --assignee "$APP_ID" --role Contributor --scope "/subscriptions/$SUB"
az role assignment create --assignee "$APP_ID" --role "Storage Blob Data Contributor" \
  --scope "$(az storage account show -n <tfstate-sa> -g companyops-tfstate --query id -o tsv)"
```

**3. A `production` environment** with you as a required reviewer (the manual approval gate
before Terraform/Ansible touch prod): repo **Settings → Environments → New environment →
`production` → Required reviewers**.

**4. GitHub Actions secrets + variables** (Settings → Secrets and variables → Actions):

| Kind | Name | Value |
|---|---|---|
| Secret | `AZURE_CLIENT_ID` | the deploy app's `appId` (`$APP_ID`) |
| Secret | `AZURE_TENANT_ID` | `az account show --query tenantId -o tsv` |
| Secret | `AZURE_SUBSCRIPTION_ID` | `$SUB` |
| Secret | `VM_SSH_PUBLIC_KEY` | the VM admin SSH **public** key (Terraform var) |
| Secret | `VM_SSH_PRIVATE_KEY` | the matching **private** key (Ansible SSH) |
| Secret | `GHCR_PULL_TOKEN` | a `read:packages` token so the VM can pull (omit if the packages are public) |
| Secret | `POSTGRES_PASSWORD` `KC_DB_PASSWORD` `RABBITMQ_PASSWORD` `KC_ADMIN_PASSWORD` | `openssl rand -base64 24` each (no `'` in `KC_DB_PASSWORD`) |
| Variable | `TFSTATE_RESOURCE_GROUP` | `companyops-tfstate` |
| Variable | `TFSTATE_STORAGE_ACCOUNT` | the bootstrap storage-account name |
| Variable | `ALLOWED_SSH_CIDR` | your management IP `/32` (the workflow also adds the runner IP for the run) |
| Variable | `APP_DOMAIN` `ACME_EMAIL` `KC_ADMIN_USER` | your values (SPA, API and Keycloak all share `APP_DOMAIN`) |
| Variable *(optional)* | `VM_LOCATION` `VM_SIZE` | override the Azure region / VM size without a code change — defaults are `westeurope` / `Standard_B2as_v2`. Set one if a region returns `SkuNotAvailable` (capacity restrictions), then re-run. |

`gh secret set NAME` / `gh variable set NAME` set these from the CLI.

**5. DNS** — point the `APP_DOMAIN` A record at the VM's public IP (one record — the SPA, API
and Keycloak all share it). On the first release the VM doesn't exist yet, so: run the release
once (Terraform creates the VM and the Ansible step's TLS will be self-signed until DNS
resolves), set the A record to the new IP (`terraform output public_ip`, or the Azure portal),
then re-run the workflow (`Run workflow`) so Let's Encrypt issues a real cert. The Static public
IP is stable across later releases.

### Deploy

```bash
gh release create v1.2.3 --generate-notes      # or the GitHub Releases UI
```

That's it — build → (approve the `production` environment) → provision → deploy. Re-deploy the
current release without a new tag via the workflow's **Run workflow** button (`workflow_dispatch`,
which deploys `:latest`).

### Rollback

Publish (or re-point) to a prior tag, or `Run workflow` after setting the image to a previous
version — the images are immutable per tag. The Postgres volume persists; for a data rollback,
restore a dump per [backup-restore.md](backup-restore.md).

---

# Manual deployment

Run the same Terraform + Ansible by hand — no GitHub Actions involved. The images must already
exist in GHCR (build them via a release, or `docker buildx build --push` locally).

## Prerequisites

- A **domain** with a DNS A record you can set: `APP_DOMAIN`, pointing at the VM's public IP
  (one record — the SPA, the API under `/api`, and Keycloak under `/auth` all share it). Real
  TLS needs this — Let's Encrypt validates over the public name on :443.
- A **VM** (~2 GB RAM to *run* the stack — images are pulled, not built, so no build
  headroom needed). Provision with Terraform, or bring your own / a homelab box.
- An **SSH key** and a control machine with `ansible` (`ansible-galaxy collection install -r
  infra/ansible/requirements.yml`).

## 1. Provision the VM

**Terraform (Azure example):**

```bash
cd infra/terraform
./bootstrap-state.sh && cp backend.hcl.example backend.hcl   # one-time: remote state (paste printed values)
cp terraform.tfvars.example terraform.tfvars                 # set ssh_public_key + allowed_ssh_cidr (your IP/32)
terraform init -backend-config=backend.hcl && terraform apply
terraform output public_ip
```

It opens **only** SSH (locked to your CIDR), 80, and 443. See [the Terraform README](../infra/terraform/README.md)
for the AWS/Hetzner equivalents and the managed-PaaS alternative.

**Or bring your own VM:** any Ubuntu 24.04 host reachable over SSH, with 80/443 open and SSH
locked down. Skip to step 2.

## 2. Point DNS at the VM

Create one A record: `APP_DOMAIN` → public IP. Wait for it to resolve (`dig +short APP_DOMAIN`)
— Let's Encrypt fails until it does.

## 3. Configure secrets

```bash
cd infra/ansible
cp inventory.example.ini inventory.ini                 # set the VM IP + SSH user/key
cp group_vars/all.yml.example group_vars/all.yml       # set ghcr_owner + companyops_version + domains + secrets
ansible-vault encrypt group_vars/all.yml               # encrypt at rest
```

Set `ghcr_owner` (lowercase) and `companyops_version` (the release tag to run, e.g. `1.2.3`),
the domains, and generate each secret with e.g. `openssl rand -base64 24` (no single quote in
`kc_db_password` — it's used in a SQL init string). For **private** GHCR packages also set
`ghcr_pull_username` + `ghcr_pull_token`. `group_vars/all.yml` and `inventory.ini` are gitignored.

## 4. Deploy

```bash
ansible-galaxy collection install -r requirements.yml
ssh-keyscan <vm-ip> >> ~/.ssh/known_hosts            # accept the host key on first connect
ansible-playbook -i inventory.ini playbook.yml --ask-vault-pass
```

The playbook installs Docker + the Compose plugin, configures the firewall (default-deny
incoming; allow SSH/80/443), checks out `repo_ref`, renders `infra/.env` from your vault vars,
pins the realm's redirect URIs/web origins to `APP_DOMAIN`, schedules the nightly backup, logs
in to GHCR (when `ghcr_pull_token` is set), and **pulls** the `companyops_version` images and
brings the stack up (`docker compose -f docker-compose.prod.yml pull` then `up -d`).

## 5. Verify

```bash
curl -sI https://APP_DOMAIN/                 # 200 text/html — the SPA loads
curl -s  https://APP_DOMAIN/api/health       # Healthy            (API under /api, prefix stripped)
curl -s  https://APP_DOMAIN/api/health/ready # Healthy (DB + RabbitMQ)
curl -sI http://APP_DOMAIN/api/health        # 301 -> https       (edge redirect)
curl -s  https://APP_DOMAIN/auth/realms/companyops/.well-known/openid-configuration | jq .issuer
```

The issuer must be `https://APP_DOMAIN/auth/realms/companyops`. **Create real users** in the
Keycloak admin console (`https://APP_DOMAIN/auth/admin`, the bootstrap admin from your vault)
and assign roles + the `department` attribute — the prod realm ships with **no seed users**
and **ROPC disabled**, so login is the browser Authorization-Code + PKCE flow (exercised by
the SPA in Phase 12).

## Backups

The playbook installs a nightly `pg_dump` cron (`infra/backup/pg-backup.sh`) to
`/var/backups/companyops` with 14-day retention. Restore procedure and a tested drill:
[backup-restore.md](backup-restore.md). Encrypted/offsite backups + managed PITR remain the
recommended production hardening (see that doc).

## Updating

Bump `companyops_version` (and `repo_ref`) to the new release tag in `group_vars/all.yml` and
re-run the playbook — it pulls the new images and restarts changed services. The automated
release path does exactly this for you on a published release.

## Rollback

Set `companyops_version` (and `repo_ref`) back to the previous release tag and re-run the
playbook — or on the VM, set `COMPANYOPS_VERSION` in `infra/.env` then
`docker compose -f infra/docker-compose.prod.yml --env-file infra/.env pull` and `up -d`. The
Postgres volume persists; for a data rollback, restore a dump per
[backup-restore.md](backup-restore.md).

## Images & registry

The stack **pulls prebuilt images from GHCR** ([ADR 0012](decisions/0012-release-driven-deployment.md))
— `ghcr.io/<owner>/companyops-{api,worker,fakeexternals,frontend}:<version>`, built by the release
workflow. The VM only pulls, so a small VM (no build headroom) is enough. If the packages are
**private**, set `ghcr_pull_username` + `ghcr_pull_token` (a `read:packages` token) so the
playbook can `docker login ghcr.io`; **public** packages need no token.

## Notes

- **Single domain (one origin).** The SPA, the API (`/api`) and Keycloak (`/auth`) share
  `APP_DOMAIN` — one DNS record, one cert, no cross-origin hop for auth. Keycloak is served
  under `/auth` via `KC_HTTP_RELATIVE_PATH`, so it owns the full path (Traefik passes the prefix
  through — no strip). Trade-off vs a dedicated auth subdomain: you give up origin isolation
  between the IdP and the app — minor here (public PKCE client, Keycloak session cookies are
  HttpOnly), so simplicity wins.
- **TLS is real Let's Encrypt** only once DNS resolves and :443 is reachable; until then
  Traefik serves its self-signed default cert. Certs persist in the `traefik-letsencrypt`
  volume across restarts (mind LE rate limits when testing).
- The bundled `fakeexternals` stands in for real Finance/Inventory systems; point
  `FINANCE_BASE_URL`/`INVENTORY_BASE_URL` at real services when they exist.
- If you set `OTEL_EXPORTER_OTLP_ENDPOINT`, point it at a **trusted/internal** collector —
  traces can carry request paths, user ids, and query text; never an unauthenticated public
  endpoint.
- Traefik stores the TLS private keys in `acme.json` (in the `traefik-letsencrypt` volume) at
  mode `0600`; if you ever copy/restore that volume, verify the file stays `0600`.
