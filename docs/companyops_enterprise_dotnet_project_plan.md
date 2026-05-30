# .NET Enterprise Portfolio Project Plan

## Project name

**CompanyOps — Internal Procurement & Asset Management System**

## Goal

The goal of this project is to build an enterprise-style internal business system with .NET, focusing not only on application code, but also on architecture, security, deployment, observability, backup, and operational thinking.

This is not meant to be a simple CRUD demo.

The purpose is to demonstrate that I understand how backend systems work in a real company environment.

At its core CompanyOps is **one configurable approval-workflow engine**.
Procurement & asset management is the fully-built reference flow, but the same
engine is designed to carry other internal processes by configuring the approval
chain per request type rather than hard-coding it:

1. **IT request / helpdesk-light** — service/access requests fulfilled by IT.
2. **Asset management** — assets and their assignment lifecycle.
3. **Internal approval** — generic multi-step sign-off.

These are not three systems; they are one `request → approval → fulfillment`
lifecycle with different approval chains. See `docs/decisions/0005-configurable-approval-workflow.md`.

---

## Project idea

CompanyOps is an internal company system where employees can request assets or services, such as:

- laptop
- monitor
- phone
- software license
- access to an internal system
- new virtual machine
- IT equipment
- procurement request

The request goes through an approval workflow:

```text
Employee creates request
  ↓
Manager approves
  ↓
Finance approves budget
  ↓
IT fulfills request
  ↓
Asset is assigned to employee
  ↓
Audit log stores every important action
```

---

## Why this project is useful

This project demonstrates knowledge of:

- .NET backend development
- enterprise-style business workflow
- role-based authorization
- audit logging
- database design
- background processing
- integration with external systems
- Docker-based local development
- CI/CD pipelines
- monitoring and logging
- backup and restore thinking
- infrastructure and deployment basics

The goal is to show that I can think beyond writing code and understand how a system should be operated, secured, deployed, and maintained.

---

## Target architecture

The project should start as a **modular monolith**, not as microservices.

Reason:

- simpler to build
- easier to understand
- better for learning
- still enterprise-friendly
- avoids unnecessary distributed-system complexity

Suggested structure:

```text
CompanyOps.Api
  ↓
CompanyOps.Application
  ↓
CompanyOps.Domain
  ↓
CompanyOps.Infrastructure
  ↓
PostgreSQL / SQL Server
```

---

## Repository structure

```text
companyops/
├── src/
│   ├── CompanyOps.Api/
│   ├── CompanyOps.Application/
│   ├── CompanyOps.Domain/
│   ├── CompanyOps.Infrastructure/
│   └── CompanyOps.Worker/
├── tests/
│   ├── CompanyOps.UnitTests/
│   └── CompanyOps.IntegrationTests/
├── frontend/                  # Angular demo SPA (Phase 12)
├── infra/
│   ├── docker-compose.yml
│   ├── terraform/
│   └── ansible/
├── docs/
│   ├── architecture.md
│   ├── security.md
│   ├── runbook.md
│   ├── backup-restore.md
│   └── decisions/
└── .github/
    └── workflows/
```

---

## Suggested tech stack

| Area | Technology |
|---|---|
| Backend | ASP.NET Core Web API |
| Language | C# |
| Runtime | .NET 10 LTS |
| Database | PostgreSQL 18 |
| ORM | Entity Framework Core 10 |
| Authentication | Keycloak 26 / OIDC / JWT |
| Authorization | Role-based and policy-based authorization |
| Cache | Redis 8 |
| Queue | RabbitMQ 4 |
| Background jobs | .NET Worker Service |
| Frontend (demo) | Angular 21 (standalone, signals) on Node 24 LTS + angular-auth-oidc-client (OIDC/PKCE) |
| Local environment | Docker Compose |
| API documentation | Swagger / OpenAPI |
| CI/CD | GitHub Actions |
| Logging | Structured logging |
| Monitoring | Health checks, metrics, logs |
| Infrastructure | Terraform and Ansible later |
| Deployment target | Linux VM, Docker host, or Kubernetes later |

---

## Main business roles

| Role | Responsibility |
|---|---|
| Employee | Creates requests |
| Manager | Approves department-level requests |
| Finance | Approves budget |
| IT Admin | Fulfills approved requests |
| Auditor | Can read data and audit logs, but cannot modify anything |

---

## Core domain entities

Suggested entities:

```text
User
Department
CostCenter
RequestType          # defines the approval chain + fulfillment kind (ADR 0005)
Request
RequestItem
ApprovalStep         # ordered, configured per RequestType
Comment              # request thread (helpdesk-light)
Asset
AssetAssignment      # assignment + return/reclaim
AuditLog
Notification
```

`RequestType` keys the configurable approval chain so the three flows
(procurement/approval, helpdesk-light, asset management) run on one engine —
see `docs/decisions/0005-configurable-approval-workflow.md`.

---

## Request statuses

```text
Draft
Submitted
ManagerApproved
FinanceApproved
Rejected
InFulfillment
Completed
Cancelled
```

---

## Main API actions

Instead of building only basic CRUD endpoints, the system should expose business actions.

Example endpoints:

```text
POST /requests
POST /requests/{id}/submit
POST /requests/{id}/approve-manager
POST /requests/{id}/approve-finance
POST /requests/{id}/reject
POST /requests/{id}/fulfill
POST /requests/{id}/cancel
GET  /requests
GET  /requests/{id}
GET  /audit-logs
```

This is more enterprise-like than simple CRUD because it models real business workflow.

---

## MVP scope

The first version should include only the most important features.

### MVP features

- ASP.NET Core API
- PostgreSQL or SQL Server database
- Entity Framework Core migrations
- Basic request creation
- Request workflow
- Users and roles
- Role-based authorization
- Audit log
- Docker Compose local environment
- Basic unit tests
- Basic integration tests
- GitHub Actions build and test pipeline
- README documentation

---

## Enterprise-style extended scope

After the MVP works, add more realistic enterprise features.

### Extended features

- Keycloak login with OIDC/JWT
- Policy-based authorization
- RabbitMQ queue
- Worker service for notifications
- Redis cache
- Fake external finance API
- Fake inventory API
- OpenAPI/Swagger documentation
- Health check endpoint
- Structured logs
- Metrics endpoint
- Database backup script
- Restore test documentation
- Terraform deployment example
- Ansible server setup example
- Monitoring dashboard
- Runbook documentation

---

## Authentication and authorization

The system should support:

- login through OIDC/JWT
- role-based authorization
- policy-based authorization
- different permissions for different business roles

Example rules:

```text
Only Employees can create requests.
Only Managers can approve requests for their own department.
Only Finance can approve budget.
Only IT Admins can mark a request as fulfilled.
Auditors can view everything but cannot modify anything.
```

---

## Audit logging

Every important action should be stored in an audit log.

Example audit event:

```text
timestamp: 2026-05-30 14:22
user: john.manager@company.local
action: ManagerApprovedRequest
requestId: 123
oldStatus: Submitted
newStatus: ManagerApproved
sourceIp: 10.0.0.25
```

Audit logs should answer:

- who did something
- what they did
- when they did it
- what changed
- which object was affected

This is important in enterprise systems because of compliance, troubleshooting, and accountability.

---

## Background worker

Create a separate worker service:

```text
CompanyOps.Worker
```

The worker can handle:

- email notification simulation
- expired approval checks
- daily report generation
- audit export
- failed job retry logic

Suggested flow:

```text
API
  ↓
RabbitMQ
  ↓
Worker Service
  ↓
Notification / Report / Audit Export
```

---

## External integration mock

Create fake external services to simulate enterprise integrations.

Examples:

```text
FakeFinanceApi
FakeInventoryApi
```

Possible behavior:

- Finance API checks whether a cost center has enough budget
- Inventory API checks whether a laptop or monitor is in stock

This demonstrates integration thinking similar to real environments where systems communicate with SAP, CRM, warehouse, accounting, or billing systems.

---

## Local development environment

The project should start with one command:

```bash
docker compose up
```

Local services:

```text
CompanyOps API
CompanyOps Worker
PostgreSQL
Redis
RabbitMQ
Keycloak
```

Optional later:

```text
Prometheus
Grafana
Loki
```

---

## CI/CD pipeline

Use GitHub Actions.

### Pull request pipeline

```text
restore dependencies
build solution
run unit tests
run integration tests
check formatting
build Docker image
```

### Main branch pipeline

```text
build Docker image
push Docker image
deploy to staging
run smoke test
manual approval
deploy to production
```

---

## Documentation

The project should include a `docs/` directory.

### docs/architecture.md

Should describe:

```text
components
data flow
trust boundaries
database
queue
authentication
authorization
external integrations
deployment model
```

### docs/security.md

Should describe:

```text
authentication
authorization
role model
secrets handling
audit logging
input validation
database access
backup encryption
rate limiting
```

### docs/runbook.md

Should describe:

```text
how to start the system
how to check logs
how to restart the API
how to restart the worker
how to check failed jobs
how to check database connectivity
how to recover from common issues
```

### docs/backup-restore.md

Should describe:

```text
database backup command
database restore command
RPO assumption
RTO assumption
restore test steps
where backups are stored
how often restore tests should be performed
```

### docs/decisions/

Use Architecture Decision Records.

Example:

```text
0001-use-modular-monolith.md
0002-use-postgresql.md
0003-use-keycloak-for-auth.md
0004-use-rabbitmq-for-background-processing.md
0005-configurable-approval-workflow.md
```

---

## Development phases

### Phase 1 — Basic API and database

Goal:

Build the first working version of the backend.

Tasks:

- create solution structure
- create ASP.NET Core API
- add PostgreSQL or SQL Server
- add Entity Framework Core
- create first database migration
- create Request entity
- create basic request endpoints
- add Swagger/OpenAPI

Deliverable:

```text
Working API with database persistence
```

---

### Phase 2 — Workflow and business logic

Goal:

Add enterprise-style request workflow.

Tasks:

- add `RequestType` with a **configurable, ordered approval-step chain** per type
  (seeded/in-code for now) — see ADR 0005; design this before the state machine
- add request statuses
- add submit action
- drive transitions from the configured steps (next actor = first unsatisfied step)
- add manager approval (department-scoped)
- add finance approval
- add rejection
- add fulfillment
- prevent invalid status transitions (enforced in Domain, throw)

Deliverable:

```text
Request workflow with business rules
```

---

### Phase 3 — Users, roles, and authorization

Goal:

Add security model.

Tasks:

- add users
- add roles
- add JWT/OIDC authentication
- add role-based authorization
- add policy-based authorization
- protect business endpoints

Deliverable:

```text
Secure API with role-based and policy-based access
```

---

### Phase 4 — Audit logging

Goal:

Make the system audit-friendly.

Tasks:

- create AuditLog entity
- log important actions
- store old and new values where useful
- expose read-only audit log endpoint
- make audit logs immutable from normal users

Deliverable:

```text
Audit trail for important business actions
```

---

### Phase 5 — Background worker and queue

Goal:

Add asynchronous processing.

Tasks:

- add RabbitMQ
- create Worker service
- publish event after request approval
- consume event in worker
- simulate notification sending
- add retry/error handling

Deliverable:

```text
API and Worker communicating through a queue
```

---

### Phase 6 — External integration mock

Goal:

Simulate real enterprise system integration.

Tasks:

- create FakeFinanceApi
- create FakeInventoryApi
- call Finance API during budget approval
- call Inventory API during fulfillment
- handle failed external calls
- add retry or graceful error handling

Deliverable:

```text
Backend integrated with mock external systems
```

---

### Phase 7 — Docker and local orchestration

Goal:

Make the system easy to run locally.

Tasks:

- create Dockerfile for API
- create Dockerfile for Worker
- create docker-compose.yml
- add PostgreSQL
- add RabbitMQ
- add Redis
- add Keycloak
- document local startup

Deliverable:

```text
Complete local environment started with docker compose up
```

---

### Phase 8 — Tests

Goal:

Add confidence and maintainability.

Tasks:

- add unit tests for business rules
- add integration tests for API endpoints
- add database integration tests
- test authorization behavior
- test invalid workflow transitions

Deliverable:

```text
Tested business logic and API behavior
```

---

### Phase 9 — CI/CD

Goal:

Automate build and validation.

Tasks:

- create GitHub Actions workflow
- restore dependencies
- build solution
- run tests
- build Docker image
- optionally push image to registry
- add status badge to README

Deliverable:

```text
Automated build and test pipeline
```

---

### Phase 10 — Observability and operations

Goal:

Make the system operable.

Tasks:

- add health check endpoint
- add structured logging
- add request correlation ID
- add basic metrics
- document troubleshooting
- create runbook
- document backup and restore

Deliverable:

```text
Application that can be monitored, debugged, and operated
```

---

### Phase 11 — Infrastructure automation

Goal:

Connect the project to DevOps/infrastructure skills.

Tasks:

- create Terraform example for cloud or homelab infrastructure
- create Ansible playbook for server setup
- deploy Docker Compose stack to a Linux VM
- configure reverse proxy
- configure TLS
- configure backup job
- document deployment

Deliverable:

```text
Deployed application with infrastructure automation
```

---

### Phase 12 — Angular client foundation ✅ (done, #26)

Goal:

Stand up the Angular client (the "CompanyOps Enterprise Suite", ADR 0010) — the shell, theme,
and tooling that carry the whole frontend track (Phases 12–20). A *client* of the API: no
business logic; the backend stays the source of truth.

Tasks:

- scaffold Angular 21 workspace in `frontend/` (standalone components, signals, Vitest)
- Angular Material (M3) + a custom theme from the Precision-Enterprise tokens (overriding the
  `--mat-sys-*` system variables); Material Symbols icons
- the app shell (responsive sidenav + toolbar) + lazy routing; a dashboard placeholder
- ESLint; a CI frontend job (build + lint + Vitest on Node 24)

Deliverable:

```text
The themed Angular shell builds, lints, and tests, in CI
```

---

### Phase 13 — Auth & API client

Goal:

Make the client talk to the API as a real, authenticated user.

Tasks:

- OIDC Authorization Code + PKCE against Keycloak via `angular-auth-oidc-client`
- split a **public SPA client** from the bearer-only **API audience** in the Keycloak realm(s)
- HTTP interceptor to attach the access token; functional **auth + role guards**; login / logout
- API **CORS** for the SPA origin + a **CSP** at the edge; dev proxy (`proxy.conf.json`) for local
- a typed **API client + DTO models** — the seam every feature service builds on

Deliverable:

```text
Log in via Keycloak; authenticated, role-aware calls reach the API
```

---

### Phase 14 — Core workflow UI

Goal:

The core request/approval screens, rebuilt from the Stitch designs in Angular Material
(via the `stitch-port` skill).

Tasks:

- dashboard bound to real request / approval counts
- requests: list (my requests + status), detail (+ status timeline), create
- approvals: the Manager / Finance queue + role-gated approve / reject
- read-only **audit log** view (Auditor)
- role-gated workflow actions (submit, approve, reject, fulfill, cancel) wired to the endpoints

Deliverable:

```text
The full request → approval → fulfilment lifecycle, driven from the UI
```

---

### Phase 15 — Helpdesk-light flow

Goal:

Add IT request / helpdesk-light as a first-class flow on the existing engine
(ADR 0005) — not a new subsystem.

Tasks:

- add a `Helpdesk` request type with its own approval chain (e.g. manager-only or
  none for low-risk) and an IT fulfillment action
- request categories (incident / service request / access request) and priority
- comment/note thread on a request (`Comment`)
- reuse the engine, audit log, and authorization — no parallel workflow code

Out of scope (stays "light"): ITSM queues, SLA timers, escalation engine.

Deliverable:

```text
Helpdesk requests running through the same approval/fulfillment engine
```

---

### Phase 16 — Asset lifecycle flow

Goal:

Manage assets beyond initial assignment — the full lifecycle.

Tasks:

- asset lifecycle states: in stock → assigned → in repair → retired
- return / reclaim flow; reassign an asset to another employee
- asset history (who held it, when, why state changed) via the audit trail
- an `AssetReturn` / lifecycle request type on the engine where approval applies

Out of scope: full CMDB / network discovery.

Deliverable:

```text
Assets tracked through their lifecycle, with history and return/reclaim
```

---

### Phase 17 — IT Admin & fulfilment console

Goal:

A dedicated console for IT Admins to work the fulfilment queue, plus the IT-Admin dashboard
screen. Reuses the engine, the `ItAdmin` role, and audit — no new workflow.

Tasks:

- fulfilment queue UI: approved-awaiting-fulfilment requests; fulfil / assign (existing endpoints)
- IT-Admin dashboard UI (open / assigned / overdue, recent activity)
- small read-only projections/queries for the queue + dashboard counts
- role-gated to `ItAdmin`; everything still audited via the existing trail

Deliverable:

```text
IT Admins work approved requests to completion from a dedicated console
```

---

### Phase 18 — Reporting & analytics

Goal:

A reporting surface (volumes, cycle times, status mix) over the request / approval / audit
data — a read side, not new write paths.

Tasks:

- read-models / aggregation queries (volume by type/status/department, approval cycle time,
  fulfilment throughput)
- the Reports & Analytics dashboard UI (charts + tables) bound to those queries
- date-range + filters; read scoping by role where relevant

Out of scope: a BI tool, real-time streaming analytics.

Deliverable:

```text
A reports dashboard summarising the workflow over time
```

---

### Phase 19 — Integrations console

Goal:

Surface the external-system integration (Finance / Inventory) status and history — observe
the existing Phase-6 worker/outbox path, no new integration logic.

Tasks:

- integration-status endpoints over the outbox / worker / audit (recent events, success/fail,
  dead-letters)
- the Integrations Management UI (connector list, health, recent activity; safe retry/replay)

Out of scope: building new third-party connectors.

Deliverable:

```text
Operators see external-integration health and history in the UI
```

---

### Phase 20 — Settings & profile

Goal:

User profile and application settings — the System Settings screen.

Tasks:

- profile view from the Keycloak principal (link out to the Keycloak account console for
  credential changes)
- application preferences (theme / notifications) — a small per-user settings store
- the System Settings UI; admin-only sections gated by role

Out of scope: full tenant / org administration.

Deliverable:

```text
Users manage their profile and preferences; the Settings screen is live
```

---

## Security checklist

The project should demonstrate basic security awareness.

Checklist:

- no secrets in Git
- environment variables for configuration
- JWT/OIDC authentication
- role-based authorization
- policy-based authorization
- audit logging
- input validation
- database migrations
- least-privilege database user
- HTTPS in production
- secure headers if applicable
- backup encryption
- documented restore process
- SPA is a public OIDC client using PKCE (no client secret in the browser)
- no secrets in the frontend bundle; API re-validates all authorization

---

## Operational checklist

The project should demonstrate operational thinking.

Checklist:

- Dockerized services
- health checks
- logs
- metrics
- backup process
- restore process
- runbook
- smoke test
- CI pipeline
- deployment documentation
- failure scenarios documented

---

## Example interview pitch

I built a .NET-based internal procurement and asset management system to practice enterprise backend and DevOps concepts.

The system includes role-based authorization, policy-based approval workflow, audit logging, database migrations, background processing with a worker service, message queue integration, Docker-based local development, CI/CD pipeline, and operational documentation.

My goal was not only to write application code, but to understand how a business-critical internal system should be designed, deployed, secured, monitored, and restored.

---

## What this project proves

This project shows that I understand:

- how enterprise backend systems are structured
- how business workflows differ from simple CRUD
- why authorization and audit logging matter
- how backend services communicate with databases and queues
- how external integrations work
- how Docker-based local environments are built
- how CI/CD validates code
- how documentation supports operations
- how backup and restore thinking fits into system design
- how DevOps and backend development connect

---

## Final project positioning

This is not a production product.

It is an enterprise-style learning and portfolio project designed to demonstrate practical knowledge of:

```text
.NET
C#
ASP.NET Core
Entity Framework Core
SQL
Authentication
Authorization
Audit logging
Workflow design
Docker
CI/CD
Monitoring
Backup/restore
Infrastructure automation
Operational documentation
```

The main goal is to show that I can think like a backend-aware DevOps / Cloud / Infrastructure engineer.
