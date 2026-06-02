# Plan — UI upgrade to the "Enterprise Suite" design

Date: 2026-06-02
Status: **Proposed**
Owner hats: frontend (implementation), architecture (SPA-stays-a-thin-client rule), design (fidelity)

> **Trackable doc.** Boxes are GitHub task-lists. Tick (`- [x]`) as each screen lands; link the PR.
> Source of truth for the look is the Figma file (ADR 0011); the Stitch PNGs in
> `~/Downloads/stitch_companyops_enterprise_suite/` are the visual reference. Built with the
> `figma-port` skill (Angular Material, themed to the design tokens) — the SPA stays a **client**
> of the API (no business logic), per AGENTS.md / ADR 0010.

## In one paragraph

The Angular client already has every screen **scaffolded** and the **Precision-Enterprise (Slate &
Cobalt) M3 tokens** + Inter font in `styles.scss`, light/dark wired. But the screens are
**low-fidelity** — they don't yet render the polished dashboard KPI cards, dense tables with
status/priority chips, slide-over detail panels, the approval-timeline stepper, charts, or the app
shell from the designs. This plan upgrades each screen to match the suite, screen by screen.

## Decisions (this plan)
- **Fidelity:** *UI-faithful, backend only where cheap.* Match the designs for everything that maps
  to current data; extend the backend only for small, high-value gaps (pagination totals); gracefully
  **simplify** the richer bits the domain doesn't have and log them as follow-ups.
- **Variants:** the designs ship 3 variants per screen (`_1/_2/_3`). **The maintainer picks per
  screen** — each phase starts by reviewing the 3 and choosing one, then we build it.

## Design system (target)
- **Shell:** left sidebar (logo "CompanyOps / Enterprise ERP" → nav → user profile pinned bottom);
  top bar (global search, **Staging** chip, role chip, notifications, help, avatar, theme toggle).
- **Style:** light Slate & Cobalt (cobalt-blue primary), Inter, rounded cards, soft shadows.
- **Components:** KPI stat cards w/ trend deltas; dense tables (id-link, type+icon, avatar+name,
  colored status chips, priority arrow+colour, kebab, pagination footer); slide-over detail panels;
  vertical approval-timeline stepper; charts (reports); mobile = bottom tab bar + FAB + cards.

## Gap: design vs. current backend
| Design feature | Backend today | This plan |
|---|---|---|
| List pagination total ("142 results", page numbers) | bounded array, no total | **Extend (cheap):** paged envelope `{items,total,page,pageSize}` on the 3 list endpoints + contract + frontend |
| Reports charts | grouped counts (status/type/priority) | **UI from existing data:** real charts off the counts; defer time-series/spend/avg-approval/"AI insights" |
| Request line-items (qty/price/total) | none | **Simplify:** detail shows title/description; line-items = follow-up |
| Create form extra fields (cost center, delivery date, location) | title/desc/type/priority/category | **Simplify:** map "business justification" → description; defer new fields |
| Asset specs (serial/MAC/warranty) | tag/name/type/status/assignee | **Simplify:** slide-over shows current fields + history; specs = follow-up |

## Phased plan

### Phase 1 — App shell + theme lock ✅ done ([#89](https://github.com/Dezoxy/companyops-dotnet/pull/89))
- [x] Sidebar (brand + "Enterprise ERP" + **New Request** + nav + **user profile pinned bottom**)
      and top bar (env badge, role chip, notifications/help, **theme menu** light·dark·system,
      account menu), responsive (handset → over-mode drawer). Styled with the existing
      `--mat-sys-*` Precision tokens only — no hardcoded values. Search/notifications/help are
      honest disabled placeholders (no backend yet).
- [x] **Acceptance:** every route renders inside the new shell; `ng build` + `ng lint` + 88 unit
      tests green; angular-guardian reviewed (no must-fix). Shell is uniform across the design
      variants, so no per-screen variant pick here — that starts at the Dashboard (Phase 2).

### Phase 2 — Dashboard ✅ done (this PR)
- [x] **Variant 2** ("Dashboard Overview") → 4 KPI stat cards (active / pending approvals / critical
      priority / managed assets, from the `/reports` GROUP BY totals — accurate, not capped by a list
      page), recent-activity table (`/requests`), system-status panel honestly derived from the
      `/integrations` outbox snapshot (no fabricated external-system health, no invented trend %).
      Per-panel loading / empty / error states.
- [x] **Acceptance:** `ng build` + `ng lint` + 90 unit tests green; angular-guardian reviewed
      (no must-fix; applied its fixes — typed `KpiCounts` view model, a11y on the table + status
      indicator). Cross-feature read-only reuse of Reports/Integrations services (those screens land
      in later phases) — additive, no business logic in the SPA.

### Phase 3 — Requests flow
Split into two PRs to keep each diff focused.

**3a — List + paged envelope ✅ done (this PR)**
- [x] **List:** dense table (id-link, title + type icon, status/priority chips, created), **server-side
      pagination footer** ("Showing X–Y of N" + windowed page numbers) over the new paged envelope.
      `New request` role-gated to Employee. Per-panel loading/empty/error.
- [x] **Backend:** `PagedResult<T>` envelope on `GET /requests` (handler total + `CountAsync`,
      contract regen, drift gate, service consumes it) — the sanctioned "backend-where-cheap" slice.
- [x] **Deferred (not cheap):** status/type **filtering** needs backend filter params; requester /
      department **names** need a user directory — both simplified out for now (follow-ups).

**3b — Detail + Create ✅ done (this PR)**
- [x] **Detail:** two-column layout — main column (description, approval-timeline stepper from
      `approvalSteps`, comments) + a Details sidebar (status/priority/type/category/created/id).
      Line-items / requester-name / per-request integration health simplified out (domain lacks them).
- [x] **Create:** two-column — Basic Information form (title/type/priority/description/category) +
      an action sidebar (Create & submit / Save draft / Cancel) with a short approval note.
      Cost-center / location / delivery-date / live chain-preview simplified out (not in the domain).

### Phase 4 — Approvals + Assets
Split into two PRs (Assets' slide-over is substantial).

**4a — Approvals ✅ done (this PR)**
- [x] Approval-queue dense table (id-link, title + type icon, priority chip, "your step X of N",
      created, Review → routes to the request detail where approve/reject happens). The decision
      stays on the detail screen; the API re-checks role + department scope.
- [x] **Simplified out:** SLA countdown, risk score, and the Approved/Rejected/Escalated tabs —
      the domain has no SLA, risk, or per-user decision history to back them.

**4b — Assets (next PR)**
- [ ] Asset-inventory dense table + **slide-over detail** (summary + history timeline from
      `/assets/{id}/history`); lifecycle actions stay on the full detail route. Specs
      (serial/MAC/warranty) simplified — not in the domain.

### Phase 5 — Audit · Integrations · Reports
- [ ] Audit log table; integration-status board; **Reports** with charts off the existing grouped counts.

### Phase 6 — Settings + mobile + polish
- [ ] Settings/profile; mobile responsive (bottom nav + FAB); a11y, loading/empty/error everywhere;
      `angular-guardian` pass on the whole diff.

### Backend-where-cheap (parallel, small slices)
- [ ] **Paged envelope** `PagedResult<T>` on `listRequests`/`listAssets`/`listAuditLogs` (+ contract,
      drift-gate regen, frontend) — unlocks the designed pagination footer and `maxItems`.

## Per-screen loop (every screen)
review the 3 variants → maintainer picks → `figma-port` (pull Figma structure/tokens, rebuild in
Material) → wire to the real API → loading/empty/error + a11y → `angular-guardian` → PR.

## Risks / notes
- **Thin-client rule:** no business logic / authz decisions in the SPA; the API re-validates. The
  guardian enforces this.
- **One PR per phase** (independent off `main`) to avoid the stacked-PR squash pain seen on the
  contract work.
- Dark mode was a derived M3 scheme (the design deferred it); Phase 1/6 make it coherent, not
  Figma-exact.
