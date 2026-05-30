output "public_ip" {
  description = "VM public IP. Point your APP_DOMAIN and KEYCLOAK_DOMAIN DNS A records here, then run the Ansible playbook against it."
  value       = azurerm_public_ip.this.ip_address
}

output "ssh_command" {
  description = "Convenience SSH command once the VM is up."
  value       = "ssh ${var.admin_username}@${azurerm_public_ip.this.ip_address}"
}
