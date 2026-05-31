# 11. Design source — Figma (replaces Stitch)

Date: 2026-05-31
Status: Accepted

## Context

[ADR 0010](0010-frontend-full-client-angular-material.md) chose **Google Stitch** as the
frontend design source: per-screen HTML + Tailwind exports, pulled via the Stitch MCP, with a
`stitch-port` skill to rebuild each screen in Angular Material. In practice the exports were
frozen snapshots living under `~/Downloads/stitch_companyops_enterprise_suite*` (three
iterations), tokens were captured in a hand-written `DESIGN.md`, and keeping them current meant
manual re-export.

The full suite has since been consolidated into a single **Figma** file — "CompanyOps
Enterprise Suite" (file key `EX9DRVlslQwRgRTPojErC6`) — holding every desktop screen plus mobile
variants. The **Figma MCP** is connected and read access is verified end to end: structure
(`get_metadata`), screenshots (`get_screenshot`), and design tokens (`get_variable_defs`).

This ADR changes only the **design source and its tooling**. Everything else in ADR 0010 — the
full-client scope, Angular Material M3, the theme built from the Precision-Enterprise tokens,
the rebuild-not-port rule, and the 12 → 20 phase track — stands.

## Decision

1. **Figma is the canonical design source** (file `EX9DRVlslQwRgRTPojErC6`). Pull references via
   the Figma MCP on demand:
   - `get_metadata` — frame/screen structure (omit the node id to list pages; pass a node id to
     drill in);
   - `get_screenshot` — the visual target for a screen/node;
   - `get_variable_defs` — the **exact** design tokens (colours/spacing/type) for a node;
   - `get_design_context` — reference code + screenshot + metadata for design→code.

2. **`stitch-port` is replaced by a `figma-port` skill** — identical rebuild-in-Material
   conventions, with Figma as the input (it builds on `new-angular-feature`).

3. **Unchanged from ADR 0010** (restated so this isn't misread as a bigger pivot):
   - The screen is **rebuilt with Angular Material**, themed from the Precision-Enterprise
     tokens. Figma's emitted code (React/HTML) is **reference only**, never pasted — same rule
     that applied to the Stitch Tailwind markup.
   - The SPA stays a **client with no business or authorization logic**; the API re-validates
     everything.
   - **Bind to real DTOs — never fabricate.** The Figma mockups carry the same domain-absent
     fields the Stitch ones did (human `REQ-####` ids, Priority, requester names/avatars,
     line-items, integration health); these are not rendered until/unless the API provides them.

## Options considered

- **Stitch local export (status quo).** Offline and self-contained, but frozen snapshots that
  drift from the live design, manual re-export, and tokens only as captured in `DESIGN.md`.
- **Figma via the Figma MCP (chosen).** One live source of truth; structured metadata; exact
  tokens via `get_variable_defs`; on-demand screenshots at any node; optional Code Connect later
  to map Figma components ↔ Angular components. Cost: a runtime dependency on the Figma
  connector being authenticated.

## Consequences

**Positive**
- A single, live design source — no stale export folders, no manual re-export.
- Exact tokens and real node structure, rather than hand-transcribed values.
- Screenshots on demand at any resolution for visual checks against a built screen.

**Negative / costs**
- A network/auth dependency: the Figma MCP needs the connector signed in. It works
  interactively; it may be **absent in headless CI/cron** runs (the build flow doesn't rely on
  it there, so this is acceptable).

**Rollback**
- The Stitch export under `~/Downloads/stitch_companyops_enterprise_suite*` is retained as an
  **offline snapshot**; if Figma access is unavailable, fall back to it.
  `docs/companyops_angular_stitch_prompts.md` remains as the historical generation prompts and
  per-page design intent.

## Affects

- **[ADR 0010](0010-frontend-full-client-angular-material.md)** — its design-source/tooling
  decisions (items 2 and 4) are superseded here; a forward-pointer note is added to that ADR.
- **AGENTS.md** — the tooling list (Stitch MCP + `stitch-port` → Figma MCP + `figma-port`).
- **frontend/CLAUDE.md**, **docs/companyops_enterprise_dotnet_project_plan.md** (Phase 14 note),
  and **.claude/skills/new-angular-feature/SKILL.md** — references to `stitch-port`.
- **.claude/skills/stitch-port/** — replaced by **.claude/skills/figma-port/**.
