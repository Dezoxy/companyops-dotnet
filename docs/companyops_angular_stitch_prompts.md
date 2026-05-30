# CompanyOps Angular Frontend Stitch Prompts

This document contains page-by-page prompts for generating the **CompanyOps** Angular frontend design in Google Stitch or a similar UI generation tool.

The recommended workflow is:

1. Start with the global app shell / design system prompt.
2. Generate each page separately.
3. For each follow-up prompt, keep the phrase:  
   **"Keep the same CompanyOps Angular enterprise design system and app shell."**
4. After the UI direction is stable, use the Angular component structure prompt.

---

## Prompt 1 — Global Angular App Shell / Design System

```markdown
Design a modern enterprise-style Angular frontend for an internal company system called CompanyOps.

CompanyOps is an internal request, approval, IT asset management, audit logging, and integration platform. It is used by employees, managers, finance users, IT admins, and auditors.

The UI should feel like a serious B2B enterprise SaaS dashboard, not a playful consumer app.

Visual style:
- Angular enterprise application
- clean light theme
- professional slate/blue accent color
- subtle gray backgrounds
- white cards
- rounded corners
- compact but readable tables
- clear typography
- status badges
- clean forms
- audit-friendly layouts
- dashboard cards
- responsive layout

Inspiration:
- Microsoft Azure Portal
- GitHub Enterprise
- Linear
- modern internal admin dashboards
- enterprise SaaS tools

Create the main Angular application shell with:

- left sidebar navigation
- top header
- main content area
- global search
- notification icon
- environment badge showing "Staging"
- current user profile menu
- role indicator

Current user:

Tom Horvath  
Role: IT Admin

Sidebar navigation:

- Dashboard
- Requests
- New Request
- Approvals
- Assets
- Tasks
- Audit Logs
- Reports
- Integrations
- Settings

Use realistic enterprise spacing, icons, cards, tables, badges, and reusable UI components.

The result should look like the foundation for a serious Angular enterprise portfolio project.
```

---

## Prompt 2 — Dashboard Page

```markdown
Keep the same CompanyOps Angular enterprise design system and app shell.

Design the Dashboard page for CompanyOps.

The Dashboard should give an IT Admin or operations-focused user a high-level overview of the internal workflow system.

Show metric cards for:

- Open Requests
- Waiting for Manager Approval
- Waiting for Finance Approval
- In Fulfillment
- Completed This Month
- Active Assets
- Failed Integrations
- Recent Audit Events

Add a request status overview section with a clean chart-style visualization.

Add a Recent Requests table with columns:

- Request ID
- Title
- Type
- Requested By
- Department
- Status
- Priority
- Created At

Example requests:

- REQ-2026-00124 — New laptop for Sales employee — Finance Approved
- REQ-2026-00125 — GitHub Enterprise license — Waiting for Manager Approval
- REQ-2026-00126 — New VM for staging environment — In Fulfillment
- REQ-2026-00127 — VPN access for contractor — Submitted
- REQ-2026-00128 — Monitor replacement — Completed

Add a Recent Audit Events panel showing:

- timestamp
- actor
- action
- entity
- status change

Add a System Health section with compact status cards:

- API: Healthy
- Worker: Healthy
- PostgreSQL: Healthy
- RabbitMQ: Warning
- Keycloak: Healthy
- Fake SAP Integration: Mock Mode

Use professional status badges such as Healthy, Warning, Failed, Mock Mode.

The dashboard should communicate enterprise reliability, auditability, workflow visibility, and DevOps awareness.
```

---

## Prompt 3 — Requests List Page

```markdown
Keep the same CompanyOps Angular enterprise design system and app shell.

Design the Requests list page.

This page is used to search, filter, and manage internal company requests.

Create a professional enterprise table with columns:

- Request ID
- Title
- Type
- Requested By
- Department
- Cost Center
- Status
- Priority
- Created At
- Last Updated
- Actions

Request statuses:

- Draft
- Submitted
- Manager Approved
- Finance Approved
- Rejected
- In Fulfillment
- Completed
- Cancelled

Request types:

- IT Equipment
- Software License
- System Access
- Virtual Machine
- Procurement
- Other

Departments:

- IT Operations
- Finance
- Sales
- HR
- Engineering
- Procurement

Cost centers:

- IT-OPS-001
- FIN-001
- SALES-001
- ENG-001
- HR-001

Add filters above the table:

- search input
- status filter
- request type filter
- department filter
- priority filter
- created date range
- assigned approver filter

Add a primary button:

New Request

Add row actions:

- View
- Approve
- Reject
- Cancel

Use colored status badges and priority badges.

The page should feel like a real internal enterprise workflow list, not a simple CRUD table.
```

---

## Prompt 4 — Request Detail Page

```markdown
Keep the same CompanyOps Angular enterprise design system and app shell.

Design a Request Detail page for one internal request.

Example request:

REQ-2026-00124  
Title: New laptop for Sales employee  
Type: IT Equipment  
Status: Finance Approved  
Priority: Medium  
Requested by: Anna Kovacs  
Department: Sales  
Cost Center: SALES-001  
Estimated Cost: €1,450  
Requested Delivery Date: 2026-06-15

Create a detailed enterprise layout with these sections:

1. Request header
- request ID
- title
- status badge
- priority badge
- created date
- last updated date
- assigned approver

2. Requester information
- name
- email
- department
- manager
- location

3. Business details
- description
- business justification
- estimated cost
- cost center
- requested delivery date

4. Requested items table
Columns:
- Item
- Category
- Quantity
- Estimated Unit Cost
- Total Cost
- Status

5. Approval workflow timeline

Show this workflow:

Submitted → Manager Approved → Finance Approved → IT Fulfillment → Completed

Highlight the current step: IT Fulfillment pending.

6. Role-based action panel

For IT Admin user, show buttons:

- Mark as In Fulfillment
- Mark as Fulfilled
- Send Back
- Cancel Request

7. Integration result panel

Show Fake SAP API integration result:

- SAP Purchase Requisition ID: PR-4500012345
- Integration Status: Success
- Last Sync: 2026-05-30 14:22
- Mode: Mock Mode

8. Audit log preview

Show the latest audit events related to this request.

The page should feel serious, traceable, approval-focused, and audit-friendly.
```

---

## Prompt 5 — New Request Form Page

```markdown
Keep the same CompanyOps Angular enterprise design system and app shell.

Design the New Request page.

This page should be a multi-section enterprise form for creating internal company requests.

The form should include:

1. Basic information
- Request title
- Request type
- Description
- Priority
- Requested delivery date

Request types:
- IT Equipment
- Software License
- System Access
- Virtual Machine
- Procurement
- Other

2. Organization details
- Department
- Cost center
- Manager
- Location

Departments:
- IT Operations
- Finance
- Sales
- HR
- Engineering
- Procurement

Cost centers:
- IT-OPS-001
- FIN-001
- SALES-001
- ENG-001
- HR-001

3. Business justification
- business reason
- expected impact
- risk if not approved

4. Requested items
Create an editable table with:
- item name
- category
- quantity
- estimated unit cost
- total cost

5. Attachment placeholder
Add a clean upload area for supporting documents.

6. Approval path preview

Show a right-side summary panel with:

- selected request type
- estimated total cost
- approval path
- required approvers
- risk level
- expected SLA

Example approval path:

Employee → Manager → Finance → IT Admin

7. Form actions

Buttons:
- Save Draft
- Submit Request
- Cancel

Use clean validation states, required field indicators, helper text, and enterprise-style layout.

The page should feel like a real internal request creation workflow, not a basic form.
```

---

## Prompt 6 — Approvals Page

```markdown
Keep the same CompanyOps Angular enterprise design system and app shell.

Design the Approvals page.

This page is used by managers and finance users to approve or reject internal requests.

Create tabs:

- My Pending Approvals
- Approved by Me
- Rejected
- Escalated

For the main tab, show an approval queue using enterprise-style cards or a table.

Each approval item should show:

- Request ID
- Request title
- Requested by
- Department
- Cost center
- Estimated cost
- Current approval step
- Business justification summary
- SLA timer
- Risk badge
- Priority badge

Example approval items:

- REQ-2026-00125 — GitHub Enterprise license — Waiting for Manager Approval
- REQ-2026-00131 — New VM for analytics test environment — Waiting for Finance Approval
- REQ-2026-00134 — Contractor VPN access — Escalated

Actions:

- Approve
- Reject
- Request Changes
- View Details

Add a right-side detail preview panel for the selected request.

The page should communicate controlled approval flow, accountability, and auditability.
```

---

## Prompt 7 — Assets Page

```markdown
Keep the same CompanyOps Angular enterprise design system and app shell.

Design the Assets page for IT asset management.

Create an enterprise asset table with columns:

- Asset ID
- Asset Type
- Name
- Serial Number
- Assigned To
- Department
- Status
- Warranty Until
- Last Seen
- Actions

Asset types:

- Laptop
- Monitor
- Phone
- Software License
- Virtual Machine
- Server
- Other

Asset statuses:

- Available
- Assigned
- In Repair
- Retired
- Lost

Example assets:

- AST-2026-00091 — Laptop — Dell Latitude 7440 — Assigned to Anna Kovacs
- AST-2026-00092 — Virtual Machine — staging-api-01 — Assigned to IT Operations
- AST-2026-00093 — Software License — GitHub Enterprise Seat — Assigned to Peter Nagy
- AST-2026-00094 — Monitor — Dell U2723QE — Available

Add filters:

- asset type
- status
- department
- assigned user
- warranty expiration

Add actions:

- View Asset
- Assign
- Mark In Repair
- Retire
- Export

Add an asset detail drawer or side panel showing:

- asset metadata
- assignment history
- linked request
- warranty info
- audit history

The page should feel practical for internal IT operations.
```

---

## Prompt 8 — Audit Logs Page

```markdown
Keep the same CompanyOps Angular enterprise design system and app shell.

Design the Audit Logs page.

This page should feel serious, compliance-friendly, and enterprise-grade.

Create a large audit table with columns:

- Timestamp
- Actor
- Role
- Action
- Entity
- Entity ID
- Old Value
- New Value
- Source IP
- Result

Example audit events:

- 2026-05-30 14:22 — john.manager@company.local — ManagerApprovedRequest — REQ-2026-00124 — oldStatus: Submitted — newStatus: ManagerApproved — 10.0.0.25
- 2026-05-30 14:28 — finance.user@company.local — FinanceApprovedRequest — REQ-2026-00124 — oldStatus: ManagerApproved — newStatus: FinanceApproved — 10.0.0.31
- 2026-05-30 14:35 — tom.horvath@company.local — MarkedInFulfillment — REQ-2026-00124 — oldStatus: FinanceApproved — newStatus: InFulfillment — 10.0.0.12

Add filters:

- actor
- role
- action
- entity type
- entity ID
- date range
- source IP
- result

Add buttons:

- Export Audit Logs
- Save Filter
- Clear Filters

Add a small compliance information panel explaining:

- audit logs are append-only
- normal users cannot modify audit logs
- logs are retained according to retention policy

Use a compact but readable enterprise table design.

This page should clearly show that CompanyOps is designed with accountability, traceability, and compliance in mind.
```

---

## Prompt 9 — Integrations Page

```markdown
Keep the same CompanyOps Angular enterprise design system and app shell.

Design the Integrations page.

This page shows the status of internal and external system integrations used by CompanyOps.

Create integration status cards for:

- Fake SAP API
- Fake Finance API
- Fake Inventory API
- RabbitMQ
- Keycloak
- PostgreSQL
- Redis
- GitHub Actions
- Terraform/Ansible Runner

Each integration card should show:

- integration name
- description
- status
- environment
- last sync
- latency
- last error
- mode
- action button

Statuses:

- Healthy
- Warning
- Failed
- Disabled
- Mock Mode

Example card:

Fake SAP API  
Description: Creates mock purchase requisitions and stores ERP reference IDs.  
Status: Mock Mode  
Environment: Staging  
Last Sync: 2026-05-30 14:22  
Latency: 120ms  
Last Error: None

Add a small event log section below the cards showing recent integration events.

Add action buttons:

- Test Connection
- View Logs
- Retry Failed Jobs
- Configure

The page should communicate DevOps awareness, operational visibility, and enterprise integration thinking.
```

---

## Prompt 10 — Settings / Admin Page

```markdown
Keep the same CompanyOps Angular enterprise design system and app shell.

Design the Settings page for CompanyOps.

This page is used by admins to configure organization-level settings.

Create settings sections:

1. Organization
- organization name
- default timezone
- default currency
- environment name

2. Departments
- list of departments
- add/edit/remove department

Departments:
- IT Operations
- Finance
- Sales
- HR
- Engineering
- Procurement

3. Cost Centers
- cost center code
- name
- department
- owner
- active/inactive status

4. Roles & Permissions
Roles:
- Employee
- Manager
- Finance
- IT Admin
- Auditor

Show a permissions matrix with read/write/approve/audit permissions.

5. Approval Rules
Configure rules such as:

- requests over €1,000 require Finance approval
- system access requests require IT Admin approval
- VM requests require IT Operations approval
- procurement requests require Manager and Finance approval

6. Notification Settings
- email notifications
- approval reminders
- escalation timeout
- daily summary

7. Integration Settings
- Fake SAP API endpoint
- Fake Finance API endpoint
- Fake Inventory API endpoint
- GitHub Actions webhook
- Terraform/Ansible Runner endpoint

8. Audit Retention
- retention period
- export schedule
- append-only mode indicator

Use a serious enterprise admin UI with cards, tables, toggles, forms, and clear save/cancel actions.

The page should feel like a real configuration area for an internal enterprise system.
```

---

## Optional Prompt 11 — Angular Component Structure

Use this prompt after the design direction is stable.

```markdown
Based on the CompanyOps Angular frontend design, propose a clean Angular component and routing structure.

Use Angular-style naming and enterprise application structure.

Include:

- app shell layout
- sidebar component
- topbar component
- dashboard page
- requests list page
- request detail page
- new request page
- approvals page
- assets page
- audit logs page
- integrations page
- settings page

Also suggest reusable components:

- metric-card
- status-badge
- priority-badge
- request-table
- approval-timeline
- audit-event-list
- integration-status-card
- asset-table
- form-section
- detail-panel
- empty-state
- loading-state
- error-state

Suggest a folder structure suitable for an Angular enterprise portfolio project.
```

---

## Suggested Angular Frontend Architecture

A simple frontend-to-backend architecture for this project:

```text
Angular frontend
  ↓
ASP.NET Core API
  ↓
PostgreSQL
  ↓
RabbitMQ Worker
  ↓
Fake SAP / Fake Finance / Fake Inventory integrations
```

Recommended enterprise-style stack:

```text
Angular
ASP.NET Core
PostgreSQL or SQL Server
Docker
GitHub Actions or Azure DevOps
Kubernetes later
```

---

## Recommended Stitch Workflow

Use this order:

1. Prompt 1 — Global app shell / design system
2. Prompt 2 — Dashboard
3. Prompt 3 — Requests list
4. Prompt 4 — Request detail
5. Prompt 5 — New request form
6. Prompt 6 — Approvals
7. Prompt 7 — Assets
8. Prompt 8 — Audit logs
9. Prompt 9 — Integrations
10. Prompt 10 — Settings
11. Prompt 11 — Angular component structure

Do not paste all prompts into Stitch at once.

Generate the shell first, then improve page by page.
