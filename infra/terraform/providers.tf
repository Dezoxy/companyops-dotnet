# Azure provider. This is an EXAMPLE target (the plan calls for "a Terraform example for
# cloud or homelab") — it provisions the Linux VM that the Ansible playbook then configures.
# It is not applied by CI. The Compose + Ansible layers are cloud-agnostic; only this file is
# Azure-specific. The AWS equivalent (aws_instance + VPC + security group + EIP) is sketched
# in infra/terraform/README.md.
terraform {
  required_version = ">= 1.9"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
}

provider "azurerm" {
  features {}
}
