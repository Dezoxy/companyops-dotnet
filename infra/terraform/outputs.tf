output "public_ip" {
  description = "VM public IP. Point your APP_DOMAIN DNS A record here (the SPA, API and Keycloak all share it), then run the Ansible playbook against it."
  value       = azurerm_public_ip.this.ip_address
}

output "ssh_command" {
  description = "Convenience SSH command once the VM is up."
  value       = "ssh ${var.admin_username}@${azurerm_public_ip.this.ip_address}"
}
