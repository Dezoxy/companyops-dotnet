output "public_ip" {
  description = "VM public IP. Point your APP_DOMAIN DNS A record here (the SPA, API and Keycloak all share it), then run the Ansible playbook against it."
  value       = azurerm_public_ip.this.ip_address
}

output "ssh_command" {
  description = "SSH command for the admin user. NOTE: inbound 22 is default-deny (no standing NSG rule) — this only connects from inside the transient CI deploy window. For ad-hoc access use `az vm run-command invoke` (control plane + RBAC, no open port)."
  value       = "ssh ${var.admin_username}@${azurerm_public_ip.this.ip_address}"
}
