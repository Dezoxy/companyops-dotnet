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

### Phase 1 — App shell + theme lock ✅ foundation
- [ ] Sidebar nav + top bar (search, Staging/role chips, notifications, theme toggle, user profile),
      responsive layout, dark-mode parity. Lock/verify the design tokens against the Figma variables.
- [ ] **Acceptance:** every existing route renders inside the new shell; light/dark both clean.

### Phase 2 — Dashboard
- [ ] Pick variant → KPI stat cards (from reports counts), recent-activity table, system-status panel
      (from integrations status). Loading/empty/error states.

### Phase 3 — Requests flow
- [ ] **List:** table (id-link, type, requester avatar, status/priority chips), filter chips,
      pagination footer (needs the paged envelope — see Phase 0-backend below).
- [ ] **Detail:** approval-timeline stepper (from `approvalSteps`), requester info, audit preview.
- [ ] **Create:** sectioned form + approval-summary preview (chain from `ApprovalChains`).

### Phase 4 — Approvals + Assets
- [ ] Approvals queue (approve/reject inline). Assets list + **slide-over detail** + history timeline.

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
