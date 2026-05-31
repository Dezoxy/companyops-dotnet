# 12. Release-driven deployment — GitHub Release → GHCR → Terraform/Ansible on Azure

Date: 2026-05-31
Status: Accepted

## Context

[ADR 0009](0009-deployment-topology-edge.md) stood up the deployment *topology* — a Linux
VM running the Compose stack behind a Traefik TLS edge, provisioned by Terraform (azurerm,
EU) and configured by Ansible. But it deliberately left the *pipeline* manual: Terraform is
"not applied by CI", the Ansible playbook **builds the images on the VM** from a `git clone`,
and there is no release trigger. The repo signposted the intended next step — `ci.yml` has a
commented GHCR push, and `vm_size` notes "use prebuilt images (GHCR) for smaller VMs".

We now want a **release-driven** flow on Azure (chosen cloud): publishing a GitHub Release
should package the app and roll the running environment forward — infra (Terraform), host
config + deploy (Ansible), and everything else — with no manual steps on a normal release.

Constraints unchanged from the project context: solo maintainer, cost-conscious, EU/GDPR
data residency. Compute target stays **VM + Compose** (keeps Terraform *and* Ansible in the
loop, reuses ADR 0009 wholesale); managed PaaS (Azure Container Apps) remains the recorded
cloud-native alternative.

## Decision

1. **Trigger: a published GitHub Release** (`on: release: [published]`, tag `vX.Y.Z`). The
   tag is the single source of truth — the same value is the **image tag** and the **repo ref**
   checked out on the VM, so the artifacts and the compose/realm files always match.

2. **Package: build versioned images in CI, push to GHCR** (GitHub Container Registry —
   locked-stack addition, the container registry). `ghcr.io/<owner>/companyops-{api,worker,
   fakeexternals}:<version>` (+ `:latest`). Chosen over ACR: free, repo-native, no extra Azure
   cost; the VM pulls with a read-only token. ACR (managed-identity pull, images inside Azure)
   is the noted upgrade. This **replaces building on the VM** (ADR 0009 §6) — the VM now pulls
   prebuilt images, so deploys are faster and a smaller VM suffices.

3. **Provision: `terraform apply` from CI, with remote state + OIDC.** State moves to an
   **Azure Storage `azurerm` backend** (local state can't be shared with CI). Azure auth uses
   **OIDC workload-identity federation** (`azure/login` + `id-token: write`) — no long-lived
   cloud credential stored in GitHub. The apply is idempotent: a normal release is a no-op
   unless infra changed.

4. **Deploy: Ansible against the VM** — `docker login ghcr.io`, `compose pull` the release
   version, `up -d` (the one-shot migrator applies EF migrations first). The runner reaches
   the VM over SSH (key in a GitHub secret); the VM's public IP comes from the Terraform output.

5. **Secrets: GitHub Actions secrets → Ansible extra-vars → `env.j2`.** The prod `.env` is
   rendered from CI-provided values; nothing secret is committed (gitleaks stays green). Azure
   Key Vault is the enterprise-optional upgrade (already tracked in
   [future-improvements.md](../future-improvements.md)).

6. **One-time manual bootstrap** (documented in [deployment.md](../deployment.md), cannot be
   automated from inside the pipeline): the Entra app + federated credential, the Terraform
   state storage account, the GitHub secrets, and the two DNS A records.

## Options considered

- **Registry — GHCR (chosen)** vs ACR. GHCR: free, release-native, zero Azure cost; token pull.
  ACR: managed-identity pull (no token), images stay in Azure, ~€5/mo Basic — the upgrade path.
- **Compute — VM + Compose (chosen)** vs Azure Container Apps. VM reuses ADR 0009's Terraform +
  Ansible + Traefik exactly and keeps both IaC layers in the loop; ACA is more cloud-native but
  discards Ansible/Traefik and is a larger rewrite. Recorded as the cloud-native alternative.
- **Azure auth — OIDC federation (chosen)** vs a stored service-principal secret. OIDC has no
  long-lived secret to leak/rotate; the federated subject is pinned to this repo's tags.
- **State — Azure Storage backend (chosen)** vs local/committed state. Remote state is required
  for CI applies (shared + locked); committing state would leak infra detail and race.
- **Image-build location — CI (chosen)** vs on the VM (ADR 0009). CI build is reproducible,
  versioned, and keeps build CPU/RAM off the runtime VM.

## Consequences

**Positive**
- A normal deploy is "publish a release" — build, provision, and roll-forward are one path.
- Images are versioned and reproducible; rollback is re-deploying a prior tag.
- No long-lived Azure credential in GitHub (OIDC); prod secrets never touch git.
- Smaller/cheaper VM possible (no on-host build).

**Negative / costs**
- A registry dependency and a GHCR pull token (or public packages) for the VM.
- Remote-state bootstrap is a chicken-and-egg one-time step (storage account must exist before
  `terraform init`) — handled by a documented bootstrap script.
- OIDC app + federated credential + GitHub secrets + DNS are a manual one-time setup; the
  pipeline can't create its own trust anchor.
- SSH-from-CI to the VM is a new access path (mitigated: key-only, NSG-restricted, key in a
  GitHub secret scoped to the deploy environment).
- Terraform now mutates real Azure infra from CI — guarded by `terraform plan` visibility and a
  protected `production` environment (manual approval gate) on the apply/deploy jobs.

## Affects

- **`infra/docker-compose.prod.yml`** — `build:` → `image:` GHCR refs keyed on `COMPANYOPS_VERSION`.
- **`infra/ansible/`** — playbook pulls (not builds), `docker login ghcr.io`, checks out the tag.
- **`infra/terraform/`** — `backend "azurerm"` + a state bootstrap script + README.
- **`.github/workflows/release.yml`** — new build→provision→deploy pipeline.
- **`docs/deployment.md`** — the release flow + the one-time bootstrap/secret/DNS setup.
- **[ADR 0009](0009-deployment-topology-edge.md) §6** — supersedes "Ansible builds the stack on
  the VM"; the topology (Traefik edge, env secrets, hardened realm) is unchanged.
