# CompanyOps — Deployment

How to deploy the production stack to a Linux VM. Topology and rationale: [ADR 0009](decisions/0009-deployment-topology-edge.md).
Day-2 operations: [runbook.md](runbook.md) · backups: [backup-restore.md](backup-restore.md).

> **What gets deployed:** the [`docker-compose.prod.yml`](../infra/docker-compose.prod.yml)
> stack behind a **Traefik** edge that terminates TLS (Let's Encrypt) and is the only public
> ingress. The API and Keycloak sit behind it; Postgres, RabbitMQ, and Redis stay internal.
>
> **Two layers, two scopes:** the Compose stack and the **Ansible** playbook are
> cloud-agnostic (any Ubuntu 24.04 VM). **Terraform** is an *example* that provisions an
> Azure VM — swap it for AWS, Hetzner, or a homelab VM and the rest is unchanged.

## Prerequisites

- A **domain** with two DNS A records you can set: `APP_DOMAIN` (the API) and
  `KEYCLOAK_DOMAIN` (Keycloak), both pointing at the VM's public IP. Real TLS needs this —
  Let's Encrypt validates over the public name on :443.
- A **VM** (2 GB+ RAM to build the images on-host; smaller works with prebuilt images — see
  below). Provision with Terraform, or bring your own / a homelab box.
- An **SSH key** and a control machine with `ansible` (`ansible-galaxy collection install -r
  infra/ansible/requirements.yml`).

## 1. Provision the VM

**Terraform (Azure example):**

```bash
cd infra/terraform
cp terraform.tfvars.example terraform.tfvars   # set ssh_public_key + allowed_ssh_cidr (your IP/32)
terraform init && terraform apply
terraform output public_ip
```

It opens **only** SSH (locked to your CIDR), 80, and 443. See [the Terraform README](../infra/terraform/README.md)
for the AWS/Hetzner equivalents and the managed-PaaS alternative.

**Or bring your own VM:** any Ubuntu 24.04 host reachable over SSH, with 80/443 open and SSH
locked down. Skip to step 2.

## 2. Point DNS at the VM

Create A records: `APP_DOMAIN` → public IP, `KEYCLOAK_DOMAIN` → public IP. Wait for them to
resolve (`dig +short APP_DOMAIN`) — Let's Encrypt fails until they do.

## 3. Configure secrets

```bash
cd infra/ansible
cp inventory.example.ini inventory.ini                 # set the VM IP + SSH user/key
cp group_vars/all.yml.example group_vars/all.yml       # set domains + generate secrets
ansible-vault encrypt group_vars/all.yml               # encrypt at rest
```

Generate each secret with e.g. `openssl rand -base64 24` (no single quote in
`kc_db_password` — it's used in a SQL init string). `group_vars/all.yml` and `inventory.ini`
are gitignored.

## 4. Deploy

```bash
ansible-galaxy collection install -r requirements.yml
ssh-keyscan <vm-ip> >> ~/.ssh/known_hosts            # accept the host key on first connect
ansible-playbook -i inventory.ini playbook.yml --ask-vault-pass
```

The playbook installs Docker + the Compose plugin, configures the firewall (default-deny
incoming; allow SSH/80/443), clones the repo, renders `infra/.env` from your vault vars, pins
the realm's redirect URIs/web origins to `APP_DOMAIN`, schedules the nightly backup, and
brings the stack up (`docker compose -f docker-compose.prod.yml up -d --build`).

## 5. Verify

```bash
curl -s https://APP_DOMAIN/health            # Healthy
curl -s https://APP_DOMAIN/health/ready      # Healthy (DB + RabbitMQ)
curl -sI http://APP_DOMAIN/health            # 301 -> https (edge redirect)
curl -s https://KEYCLOAK_DOMAIN/realms/companyops/.well-known/openid-configuration | jq .issuer
```

The issuer must be `https://KEYCLOAK_DOMAIN/realms/companyops`. **Create real users** in the
Keycloak admin console (`https://KEYCLOAK_DOMAIN/admin`, the bootstrap admin from your vault)
and assign roles + the `department` attribute — the prod realm ships with **no seed users**
and **ROPC disabled**, so login is the browser Authorization-Code + PKCE flow (exercised by
the SPA in Phase 12).

## Backups

The playbook installs a nightly `pg_dump` cron (`infra/backup/pg-backup.sh`) to
`/var/backups/companyops` with 14-day retention. Restore procedure and a tested drill:
[backup-restore.md](backup-restore.md). Encrypted/offsite backups + managed PITR remain the
recommended production hardening (see that doc).

## Updating

Re-run the playbook — it pulls the latest `repo_ref`, re-renders `.env`, and rebuilds/restarts
changed services. For a **reproducible, intentional** deploy, set `repo_ref` to a pinned tag
or commit SHA rather than `main` — otherwise any push to `main` takes effect on the next
playbook run with no separate promotion step.

## Rollback

`ssh` to the VM, `cd /opt/companyops`, `git checkout <previous-sha>`, then
`docker compose -f infra/docker-compose.prod.yml --env-file infra/.env up -d --build`. The
Postgres volume persists across this; for a data rollback, restore a dump per
[backup-restore.md](backup-restore.md).

## VM sizing / prebuilt images

Building the .NET images on the VM needs ~2 GB RAM. For a smaller/cheaper VM, build once in
CI and **push to GHCR** (the guarded push step is sketched in `.github/workflows/ci.yml`),
then change the `build:` services in `docker-compose.prod.yml` to `image: ghcr.io/<owner>/…`
so the VM only pulls. That also makes deploys faster and more reproducible.

## Notes

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
