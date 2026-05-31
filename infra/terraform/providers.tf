# Azure provider. Provisions the Linux VM that the Ansible playbook then configures. The
# Compose + Ansible layers are cloud-agnostic; only this file is Azure-specific. The AWS
# equivalent (aws_instance + VPC + security group + EIP) is sketched in infra/terraform/README.md.
#
# The release workflow (ADR 0012) applies this from CI using OIDC (ARM_USE_OIDC=true +
# ARM_CLIENT_ID/ARM_TENANT_ID/ARM_SUBSCRIPTION_ID) — no stored cloud secret. Run it locally with
# `az login` instead.
terraform {
  required_version = ">= 1.9"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }

  # Remote state in Azure Storage so CI and humans share one locked state. Partial config —
  # the bucket coordinates are supplied at `terraform init -backend-config=backend.hcl` (or
  # -backend-config=... flags in CI); see backend.hcl.example + bootstrap-state.sh. Keeping the
  # globally-unique storage-account name out of source avoids committing an environment detail.
  backend "azurerm" {}
}

provider "azurerm" {
  features {}
}
