## Context

The dashboard route (`/dashboard`, component `DashboardPage`) currently consists of one child: `<app-morning-briefing-widget />`. The briefing widget handles its own loading / `ai_provider_not_configured` / generic-error states (merged in a prior change). The rest of the "first tab every morning" experience lives on separate routes:

- `/commitments` — open commitments (data via `CommitmentsService` → `GET /api/commitments`)
- `/people` / person detail — 1:1s (data via `GET /api/one-on-ones`)
- `/queue` — My Queue ranked items (data via `MyQueueService` → `GET /api/my-queue`)
- `/delegations` — open delegations (data via `GET /api/delegations`)

All endpoints are user-scoped and already live. The backend has nothing to do.

Zoneless Angular 21 with signals is the rendering model; PrimeNG + `tailwindcss-primeui` own colour. CSS grid is the layout primitive.

## Goals / Non-Goals

**Goals:**
- Dashboard is useful even when the AI provider is misconfigured or failing.
- Each widget owns its fetch, loading, error, and empty states in isolation — a failure in one MUST NOT blank the page.
- Zero new backend code: widgets compose existing endpoints.
- Layout is responsive: briefing full-width on top; sibling widgets two-column on `lg`+, single column on mobile.

**Non-Goals:**
- No new API endpoints, DTOs, or aggregates.
- No change to briefing generation, caching, or synthesis rules.
- No user-configurable widget visibility / ordering.
- No rewrite of `/commitments`, `/people`, `/queue`, `/delegations` routes.
- No drag-and-drop.

## Decisions

### D1: Keep "morning briefing" as the anchor widget; do not move it out of the shell
The briefing is the narrative summary — its visual prominence (full-width top row) matches its role. Moving it to a sibling slot would dilute the "AI writes your morning for you" product promise. The shell wraps it; it doesn't replace it.

**Alternatives considered:** making all five widgets equal-weight sibling tiles. Rejected — briefing reads like prose and needs horizontal room; the others are lists.

### D2: Widget isolation via per-component fetch boundaries
Each widget is a standalone component that injects its own service, calls its own endpoint on mount, and renders `loading | error | empty | data` states locally. There is **no** shared dashboard resolver or aggregate fetch. An HTTP 500 on `/api/delegations` degrades only the overdue-summary widget.

**Alternatives considered:** a `DashboardFacadeService` that fans out to all endpoints and exposes a single signal. Rejected — couples widget lifecycles, violates the isolation goal, and would require custom per-source error tracking anyway.

### D3: Reuse existing feature services; do not duplicate HTTP clients
`CommitmentsService`, `MyQueueService`, `DelegationsService`, and the existing 1:1 list service (under `people-lens` / person-detail area) are already the owners of their endpoints. Widgets call them directly. If a service lacks a "top N" convenience, the widget slices in-memory.

**Alternatives considered:** dedicated `DashboardCommitmentsService` etc. Rejected — duplicates HTTP parsing and caching behaviour.

### D4: Layout uses CSS grid with Tailwind utilities; no custom CSS colours
```
grid grid-cols-1 lg:grid-cols-2 gap-6
  <briefing class="lg:col-span-2" />
  <commitments /> <one-on-ones />
  <top-of-queue /> <overdue-summary class="lg:col-span-2" />
```
Overdue summary spans full width because it's a one-line count bar. All colours come from PrimeNG tokens (`bg-surface-*`, `text-muted-color`, `text-primary`) per CLAUDE.md.

### D5: Quick-action semantics on commitments widget
"Mark complete" and "snooze" dispatch via the existing `CommitmentsService` mutation methods used on the commitments page. On success the widget re-fetches its own list; no cross-widget invalidation. Other widgets remain unaffected — matches D2.

### D6: Overdue summary counts, not lists
This widget is a density signal, not an inbox. It renders `"N commitments overdue · M delegations stale · K unread nudges"` where each number links to the relevant filtered route. If any count source fails, that segment renders `"— commitments overdue"` but the other segments still show. Nudge count degrades gracefully to 0 if nudges-rhythms is not available.

### D7: "Today" is the user-local date
All widgets that filter by "today" (commitments, 1:1s) compute local date using the browser's timezone at render time. This matches how `daily-weekly-briefing` scopes `scopeKey`, and no backend computation is needed because the endpoints return full datasets that the widgets filter client-side. Acceptable because per-user lists are small (tens to low hundreds).

## Risks / Trade-offs

- **[Fan-out load on dashboard mount]** → Five parallel GETs on first render. Mitigation: all endpoints are already lightweight list reads; widgets render progressively as each response lands.
- **[Client-side "today" filtering could drift if dataset grows]** → Mitigation: widgets cap to N=5 display items; if datasets exceed ~500 rows, move filtering server-side in a follow-up. Out of scope here.
- **[Duplicated loading spinners look busy]** → Mitigation: widgets use compact skeleton rows matching their final shape, not spinners, so the page reads as "filling in" rather than "flashing".
- **[Widget failures can hide important state]** → Mitigation: error state MUST name the failed data source ("Couldn't load commitments") so the user knows to check the full route.
- **[Regression risk to current briefing widget]** → Mitigation: briefing component is unchanged; only its parent template moves.

## Migration Plan

- Shippable in one PR. No migrations, no feature flag required.
- Rollback: revert the `dashboard.page.ts` template to the single `<app-morning-briefing-widget />` line; no data implications.

## Open Questions

- Should "Top of queue" show 5 items or follow the user's `TopItemsPerSection` preference if/when we expose one to the frontend? Default **5** for now; revisit if/when `BriefingOptions` is user-configurable.
- Does the nudges-rhythms capability expose an "unread count" endpoint today? If not, the overdue-summary widget renders "0 unread nudges" as a stub — confirm during apply.

## Dependencies

- `daily-weekly-briefing` (Tier 3) — anchor widget and the capability this delta modifies.
- `commitment-tracking` (Tier 2) — read-only, `GET /api/commitments`.
- `delegation-tracking` (Tier 2) — read-only, `GET /api/delegations`.
- `people-lens` (Tier 2) — read-only, `GET /api/one-on-ones`.
- `my-queue` (Tier 3) — read-only, `GET /api/my-queue`.

No Tier 1 changes. All dependencies already shipped.
