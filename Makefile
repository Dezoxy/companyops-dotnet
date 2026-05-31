# CompanyOps — developer task runner. Thin wrappers over the commands in AGENTS.md and
# docs/ — no logic lives here. Run `make` (or `make help`) to list targets.

COMPOSE      := docker compose -f infra/docker-compose.yml
COMPOSE_PROD := docker compose -f infra/docker-compose.prod.yml
EF           := dotnet ef
INFRA        := src/CompanyOps.Infrastructure
API          := src/CompanyOps.Api

.DEFAULT_GOAL := help

.PHONY: help build restore format format-check test test-unit test-integration check \
        migration-add migration-apply up up-fg backing logs down down-v \
        iac tf-validate prod-config shellcheck ansible-lint gitleaks

help: ## Show this help
	@awk 'BEGIN {FS = ":.*##"; printf "\nUsage: make \033[36m<target>\033[0m\n"} /^[a-zA-Z_-]+:.*?##/ { printf "  \033[36m%-17s\033[0m %s\n", $$1, $$2 } /^##@/ { printf "\n\033[1m%s\033[0m\n", substr($$0, 5) }' $(MAKEFILE_LIST)

##@ Backend
build: ## Build the solution
	dotnet build

restore: ## Restore NuGet packages
	dotnet restore

##@ Quality
format: ## Apply code formatting
	dotnet format

format-check: ## Verify formatting (CI gate)
	dotnet format --verify-no-changes

test: ## Run all tests (unit + Testcontainers integration; needs Docker)
	dotnet test

test-unit: ## Run the fast unit tests only (Domain + Application; no Docker)
	dotnet test tests/CompanyOps.Domain.Tests
	dotnet test tests/CompanyOps.Application.Tests

test-integration: ## Run the Testcontainers integration tests (needs Docker)
	dotnet test tests/CompanyOps.Api.IntegrationTests

check: format-check build test-unit ## Fast pre-commit gate: format + build + unit tests

##@ Database (EF Core)
migration-add: ## Add a migration: make migration-add NAME=YourMigration
	@test -n "$(NAME)" || { echo "Usage: make migration-add NAME=YourMigration"; exit 1; }
	$(EF) migrations add $(NAME) -p $(INFRA) -s $(API)

migration-apply: ## Apply EF migrations to the configured database
	$(EF) database update -p $(INFRA) -s $(API)

##@ Local stack (Docker Compose)
up: ## Start the full local stack (build + detached)
	$(COMPOSE) up --build -d

up-fg: ## Start the full local stack in the foreground
	$(COMPOSE) up --build

backing: ## Start only backing services (run the apps from your IDE)
	$(COMPOSE) up -d postgres keycloak rabbitmq redis fakeexternals

logs: ## Tail the local stack logs
	$(COMPOSE) logs -f

down: ## Stop the local stack (keep data)
	$(COMPOSE) down

down-v: ## Stop the local stack and WIPE data (volumes)
	$(COMPOSE) down -v

##@ Infrastructure (IaC — mirrors the CI iac-validate job)
iac: tf-validate prod-config shellcheck ansible-lint ## Run all IaC checks

tf-validate: ## Terraform fmt-check + init + validate
	cd infra/terraform && terraform fmt -check -recursive && terraform init -backend=false -input=false && terraform validate

prod-config: ## Validate the production Compose file (dummy env)
	@GHCR_OWNER=ci COMPANYOPS_VERSION=ci \
	APP_DOMAIN=app.example.com KEYCLOAK_DOMAIN=auth.example.com ACME_EMAIL=ci@example.com \
	POSTGRES_PASSWORD=dummy KC_DB_PASSWORD=dummy RABBITMQ_PASSWORD=dummy \
	KC_ADMIN_USER=ci KC_ADMIN_PASSWORD=dummy \
	$(COMPOSE_PROD) config -q && echo "prod compose: valid"

shellcheck: ## ShellCheck the infra shell scripts
	shellcheck infra/backup/*.sh infra/postgres/initdb/*.sh infra/terraform/*.sh

ansible-lint: ## Lint the Ansible playbook
	cd infra/ansible && ansible-lint playbook.yml

##@ Security
gitleaks: ## Scan the working tree for secrets
	gitleaks detect --no-banner
