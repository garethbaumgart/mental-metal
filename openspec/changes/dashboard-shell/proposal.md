## Why

The product brief says the dashboard must be "your first tab every morning" (success criteria #1 and #2). Today the dashboard route renders a single component: `<app-morning-briefing-widget />`. When the user's AI provider is unconfigured or the AI call fails, the page below the error state is blank — nothing actionable. Even on the happy path, every other "what's pulling on me now" signal (commitments due today, 1:1s on the calendar, the top of the queue, overdue counts) lives on a different route the user has to navigate to. The dashboard fails the product promise both when AI works and when it doesn't.

This change turns the dashboard into a composable widget shell: the AI briefing stays as the anchor widget at the top, and four sibling widgets pull live data from existing endpoints so the page is useful on day one, offline-AI or not.

## What Changes

- Restructure the dashboard route into a responsive widget grid. Morning briefing spans full width at the top; sibling widgets flow in two columns on `lg`+ screens and stack on mobile.
- Add **Today's commitments** widget — open commitments due today or overdue, with an inline "Mark complete" action (Snooze is deferred to a follow-up change). Data source: `GET /api/commitments` (already exists, see `commitment-tracking` spec).
- Add **Today's 1:1s** widget — OneOnOnes whose `OccurredOnUtc` (local date) equals today. Each row links to the person detail page. Data source: `GET /api/one-on-ones` (already exists, see `people-lens` spec).
- Add **Top of queue** widget — the top 5 items from My Queue in the queue's existing priority order. Data source: `GET /api/my-queue` (already exists, see `my-queue` spec).
- Add **Overdue summary** widget — a compact one-line count bar ("N commitments overdue · M delegations stale · K unread nudges"), each count linking to the relevant filtered list. Data source: same endpoints above plus `GET /api/delegations` (already exists, see `delegation-tracking` spec).
- Formalise a **widget isolation contract**: every dashboard widget SHALL render its own loading state, its own error state, and a failure in one widget SHALL NOT prevent other widgets from rendering. The briefing widget already does this for HTTP 409 / generic error; this change extends the requirement to all siblings.

## Capabilities

### New Capabilities
<!-- None. All widget data comes from existing capabilities; this delta composes them on the dashboard route. -->

### Modified Capabilities
- `daily-weekly-briefing`: adds requirements for a dashboard widget shell that hosts the morning briefing alongside sibling widgets, and a widget-isolation contract. The briefing's existing widget requirement is refined to live inside the shell rather than *be* the dashboard.

## Impact

- **Tier**: Tier 3 delta on `daily-weekly-briefing`. No Tier 1/2 changes.
- **Affected code**:
  - `src/MentalMetal.Web/ClientApp/src/app/pages/dashboard/dashboard.page.ts` — replaced inline template with a grid that hosts 5 widgets.
  - New widget components under `src/MentalMetal.Web/ClientApp/src/app/pages/dashboard/` (one per widget).
  - Reuses existing services: `CommitmentsService`, `PeopleService` / 1:1 list, `MyQueueService`, `DelegationsService`, `BriefingService`.
- **No backend changes.** No new endpoints, no DTO changes, no migrations.
- **Non-goals**:
  - No new API endpoints. All data comes from existing GET endpoints.
  - No change to briefing generation logic, caching rules, or DTOs.
  - No rewrite of `/commitments`, `/people`, `/my-queue`, or `/delegations` route layouts — widgets are new views over existing data.
  - No drag-and-drop widget reordering, no user-configurable widget visibility.
  - No new aggregates or domain events.
- **Dependencies** (from spec-plan.md): `daily-weekly-briefing` (anchor), plus read-only consumption of `commitment-tracking`, `delegation-tracking`, `people-lens`, `my-queue`. All already shipped.
