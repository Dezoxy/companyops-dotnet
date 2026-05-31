# Terraform — VM provisioning (example)

Provisions the Linux VM that the [Ansible playbook](../ansible) configures and deploys to.
Azure, EU region. The release workflow applies this from CI on a published release, using
remote state + OIDC ([ADR 0012](../../docs/decisions/0012-release-driven-deployment.md)); you
can also apply it by hand (below). The Compose stack and Ansible playbook are cloud-agnostic —
they run on *any* Ubuntu VM — so you can also skip Terraform entirely and point Ansible at a VM
you created by hand (or a homelab VM).

## Use it

State lives in Azure Storage (remote backend) so CI and humans share one locked state — create
it once with `bootstrap-state.sh`, then `init` against it:

```bash
cd infra/terraform
./bootstrap-state.sh                           # one-time: creates the state storage, prints the values
cp backend.hcl.example backend.hcl             # paste the printed values (gitignored)
cp terraform.tfvars.example terraform.tfvars   # fill in ssh_public_key + allowed_ssh_cidr
terraform init -backend-config=backend.hcl
terraform plan
terraform apply
terraform output public_ip      # -> set DNS A records, then run Ansible
```

Creates: a resource group, VNet/subnet, a Standard public IP, an NSG that allows **only**
SSH (locked to `allowed_ssh_cidr`) + 80 + 443, and an Ubuntu 24.04 VM (key-only SSH). The
datastores are never opened in the NSG — Traefik is the sole ingress and everything else
stays on the internal Docker network behind the firewall.

The OS image uses `version = "latest"` for convenience in this example; pin a specific
gallery image version for fully reproducible applies. The committed `.terraform.lock.hcl`
already pins the provider version + multi-platform hashes.

Tear down: `terraform destroy`.

## Cost / residency

A `Standard_B2s` in an EU region (`germanywestcentral`, `westeurope`, `swedencentral`) is a
small, low-cost burstable VM and keeps data in the EU (GDPR). For a cheaper, equally-EU
option outside the hyperscalers, a Hetzner Cloud `cx22` is a fraction of the price — swap the
provider block and the VM/network resources; the Ansible side is unchanged.

## AWS equivalent

The same shape in AWS (`eu-central-1` / Frankfurt): an `aws_vpc` + `aws_subnet` +
`aws_internet_gateway` + route table, an `aws_security_group` with the same three ingress
rules, an `aws_instance` (Ubuntu 24.04 AMI) with an `aws_key_pair`, and an `aws_eip`. Outputs
the public IP the same way. The Ansible playbook and Compose stack are identical regardless of
provider.

## Managed-PaaS alternative (enterprise-optional)

Instead of a VM + Compose, a production deployment could run the API on a managed container
platform (Azure Container Apps / AWS ECS) with managed Postgres (Azure DB for PostgreSQL
Flexible Server / RDS) for automated backups + PITR. That trades ops burden for run cost and
some lock-in — out of scope here, where the deliverable is "deploy the Compose stack to a
Linux VM".
