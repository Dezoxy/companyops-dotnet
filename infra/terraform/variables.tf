variable "location" {
  # EU region for GDPR residency. Default westeurope — germanywestcentral hit SkuNotAvailable
  # capacity restrictions (no placement for this subscription, across SKU families). Override
  # per-deploy with the VM_LOCATION GitHub variable (no code change) if a region runs short.
  description = "Azure region (EU). Default westeurope."
  type        = string
  default     = "westeurope"
}

variable "prefix" {
  description = "Name prefix for all created resources."
  type        = string
  default     = "companyops"
}

variable "vm_size" {
  # Images are pulled from GHCR (ADR 0012), not built on the VM, so no build headroom is needed.
  # B2as_v2 (AMD, 2 vCPU / 8 GB) over B2s (Intel, 4 GB): different capacity pool — B2s hit a
  # SkuNotAvailable capacity restriction in germanywestcentral. Swap if your region restricts this one.
  description = "VM size (2 vCPU). Default Standard_B2as_v2."
  type        = string
  default     = "Standard_B2as_v2"
}

variable "admin_username" {
  description = "Linux admin user for SSH."
  type        = string
  default     = "companyops"
}

variable "ssh_public_key" {
  description = "Contents of the SSH public key authorised for the admin user (no default — you must supply yours)."
  type        = string
}

variable "ci_ssh_cidr" {
  description = "CIDR allowed to reach SSH (port 22) as a /32. SSH is otherwise DEFAULT-DENY: the release workflow sets this to the CI runner's IP to open a transient hole for the Ansible deploy, then re-applies with \"\" to close it. Empty (the default, and every local apply) => no SSH rule at all => use `az vm run-command` for access (ADR 0012)."
  type        = string
  default     = ""
}
