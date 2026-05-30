variable "location" {
  description = "Azure region. Default is an EU region for GDPR data residency."
  type        = string
  default     = "germanywestcentral"
}

variable "prefix" {
  description = "Name prefix for all created resources."
  type        = string
  default     = "companyops"
}

variable "vm_size" {
  description = "VM size. Needs ~2 GB+ RAM to build the .NET images on-host; use prebuilt images (GHCR) for smaller VMs — see deployment docs."
  type        = string
  default     = "Standard_B2s"
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

variable "allowed_ssh_cidr" {
  description = "CIDR allowed to reach SSH (port 22). Set to your address as a /32 — never 0.0.0.0/0 in real use. No default, so it's a conscious choice."
  type        = string
}
