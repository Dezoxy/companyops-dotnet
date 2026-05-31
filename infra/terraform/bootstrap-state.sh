#!/usr/bin/env bash
# One-time bootstrap of the Terraform remote-state backend (ADR 0012). The state storage must
# exist before `terraform init`, so this can't live in the Terraform itself — run it once,
# locally, after `az login`. Safe to re-run (create is idempotent on existing resources).
#
# It creates a dedicated resource group + Storage account + blob container for state, then
# prints the values to drop into backend.hcl. Override any default via env vars, e.g.:
#   LOCATION=westeurope TFSTATE_SA=companyopstf1234 ./bootstrap-state.sh
set -euo pipefail

LOCATION="${LOCATION:-germanywestcentral}"
RG="${TFSTATE_RG:-companyops-tfstate}"
# Storage account names are globally unique + 3-24 lowercase alphanumerics — append a random suffix.
SA="${TFSTATE_SA:-companyopstf$(openssl rand -hex 3)}"
CONTAINER="${TFSTATE_CONTAINER:-tfstate}"

echo "Creating resource group '$RG' in '$LOCATION'..."
az group create --name "$RG" --location "$LOCATION" --output none

echo "Creating storage account '$SA' (Entra/RBAC auth, no public blobs)..."
az storage account create \
  --name "$SA" --resource-group "$RG" --location "$LOCATION" \
  --sku Standard_LRS --kind StorageV2 --min-tls-version TLS1_2 \
  --allow-blob-public-access false --output none

echo "Creating blob container '$CONTAINER'..."
az storage container create \
  --name "$CONTAINER" --account-name "$SA" --auth-mode login --output none

# Grant the current user data-plane access so `terraform init` works locally over RBAC.
SCOPE="$(az storage account show --name "$SA" --resource-group "$RG" --query id -o tsv)"
ME="$(az ad signed-in-user show --query id -o tsv)"
az role assignment create \
  --assignee "$ME" --role "Storage Blob Data Contributor" --scope "$SCOPE" --output none || true

cat <<EOF

State backend ready. Put these in infra/terraform/backend.hcl (copy from backend.hcl.example):

  resource_group_name  = "$RG"
  storage_account_name = "$SA"
  container_name       = "$CONTAINER"
  key                  = "prod.terraform.tfstate"
  use_azuread_auth     = true

Then:  terraform init -backend-config=backend.hcl

For CI (release workflow): also grant the deploy app "Storage Blob Data Contributor" on this
storage account — see docs/deployment.md. The storage account name is also a GitHub variable
(TFSTATE_STORAGE_ACCOUNT) the workflow passes to \`terraform init\`.
EOF
