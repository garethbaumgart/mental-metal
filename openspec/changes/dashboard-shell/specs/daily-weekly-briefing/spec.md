## ADDED Requirements

### Requirement: Dashboard widget shell

The frontend SHALL render the authenticated dashboard route (`/dashboard`) as a responsive widget shell rather than a single briefing view. The shell SHALL host the morning briefing widget as a full-width anchor row at the top and SHALL host the following sibling widgets below it: today's commitments, today's 1:1s, top of queue, and overdue summary.

The layout SHALL use CSS grid. On viewports `lg` and wider, sibling widgets SHALL flow in two columns; on narrower viewports, widgets SHALL stack in a single column. The overdue summary widget SHALL span the full width on all breakpoints.

The shell SHALL NOT introduce a shared data-fetch facade: each widget SHALL be a standalone component that owns its own data fetch, loading state, error state, and empty state. Colours SHALL use PrimeNG / `tailwindcss-primeui` tokens; no hardcoded Tailwind colour utilities and no `dark:` prefix. Control flow SHALL use `@if` / `@for` / `@switch`, never `*ngIf` / `*ngFor` / `*ngSwitch`.

#### Scenario: Shell renders all widgets on dashboard load

- **WHEN** an authenticated user navigates to `/dashboard`
- **THEN** the page renders the morning briefing anchor at the top and the commitments, 1:1s, top-of-queue, and overdue-summary widgets below

#### Scenario: Responsive column behaviour

- **WHEN** the viewport is at least `lg` width
- **THEN** sibling widgets arrange in two columns and the overdue summary spans both columns

#### Scenario: Mobile stack

- **WHEN** the viewport is narrower than `lg`
- **THEN** all widgets stack in a single column in the order: briefing, commitments, 1:1s, top of queue, overdue summary

### Requirement: Widget isolation contract

Every widget on the dashboard SHALL be independently resilient: its data fetch, rendering, and error handling MUST be self-contained. A failure (any HTTP error, network failure, or rendering exception) in any one widget MUST NOT prevent any other widget from rendering.

Each widget SHALL render one of four local states: `loading`, `error`, `empty`, or `data`. The `error` state SHALL name the data source that failed so the user can locate it on its full route (for example, "Couldn't load commitments — try the Commitments page"). Widgets MUST NOT share mutable state; actions taken in one widget (for example, marking a commitment complete) MUST refetch only that widget's data.

#### Scenario: Briefing fails, other widgets still render

- **WHEN** the morning briefing endpoint returns HTTP 409 `ai_provider_not_configured`
- **THEN** the briefing widget renders the "Configure your AI provider" empty state with a settings link, and the commitments, 1:1s, top-of-queue, and overdue-summary widgets render their live data independently

#### Scenario: One sibling widget fails, others still render

- **WHEN** `GET /api/delegations` returns HTTP 500 while the dashboard is loading
- **THEN** the overdue-summary widget renders "Couldn't load delegations" for the delegations segment, but the commitments, 1:1s, and top-of-queue widgets render their live data and the briefing widget renders its markdown

#### Scenario: Quick action on one widget does not reload others

- **WHEN** the user clicks "Mark complete" on a commitment row in the today's-commitments widget
- **THEN** only the commitments widget refetches; the briefing, 1:1s, top-of-queue, and overdue-summary widgets do not re-issue their requests

### Requirement: Today's commitments widget

The dashboard SHALL include a "Today's commitments" widget that fetches open commitments from `GET /api/commitments` and displays those whose `DueDate` equals the user's local today OR whose `IsOverdue = true`. The widget SHALL cap the visible list at 5 rows, sorted by (overdue desc, dueDate asc). If more than 5 match, the widget SHALL show a "View all" link to `/commitments`.

Each row SHALL display the commitment description, the due date (if set), an overdue badge when `IsOverdue = true`, and an inline "Mark complete" quick action. The action SHALL call the existing `POST /api/commitments/{id}/complete` endpoint used on the `/commitments` route. On success the widget SHALL refetch its own list. A row's action button SHALL be disabled and show a loading indicator while the mutation is in flight.

(Snooze was considered but is deferred to a follow-up change — the backend has no first-class snooze verb today.)

When no commitments match, the widget SHALL render an empty-state message ("Nothing due today — nice.").

#### Scenario: Widget lists today's and overdue commitments

- **WHEN** the user has 2 commitments due today and 1 overdue
- **THEN** the widget renders all 3 rows with overdue first

#### Scenario: Cap at 5 rows with a View all link

- **WHEN** the user has 8 commitments matching today-or-overdue
- **THEN** the widget renders 5 rows and a "View all" link to `/commitments`

#### Scenario: Mark complete

- **WHEN** the user clicks "Mark complete" on a commitment row
- **THEN** the mutation fires, the widget refetches, and the row is no longer present

#### Scenario: Empty state

- **WHEN** the user has no commitments due today and none overdue
- **THEN** the widget renders "Nothing due today — nice."

### Requirement: Today's 1:1s widget

The dashboard SHALL include a "Today's 1:1s" widget that fetches one-on-ones from the existing one-on-ones endpoint and displays those whose `OccurredAt` (a `DateOnly`) resolves to the user's local today. Each row SHALL show the person's display name and link to the person detail page. (The underlying aggregate does not carry a scheduled time — date-only suffices.)

When no 1:1s are scheduled today, the widget SHALL render an empty-state message ("No 1:1s today.").

#### Scenario: Widget lists today's 1:1s

- **WHEN** the user has 2 OneOnOne records with local-today dates
- **THEN** the widget renders both rows, each linking to the respective person detail page

#### Scenario: Empty state

- **WHEN** the user has no OneOnOne records for today
- **THEN** the widget renders "No 1:1s today."

### Requirement: Top of queue widget

The dashboard SHALL include a "Top of queue" widget that fetches `GET /api/my-queue` and renders the top 5 items in the queue's existing priority order (as defined by the `my-queue` capability). Each row SHALL show the item's summary and its queue-rank label, and SHALL link to the item's detail route. The widget SHALL provide a "View all" link to `/my-queue`.

When the queue is empty, the widget SHALL render an empty-state message ("Queue is empty.").

#### Scenario: Widget shows top 5 queue items

- **WHEN** the user's queue contains 12 items
- **THEN** the widget renders the top 5 ranked items and a "View all" link to `/my-queue`

#### Scenario: Empty queue

- **WHEN** the queue has no items
- **THEN** the widget renders "Queue is empty."

### Requirement: Overdue summary widget

The dashboard SHALL include an "Overdue summary" widget that renders a single compact count bar across three segments: commitments overdue, delegations stale, and unread nudges. Commitments overdue counts are derived from `GET /api/commitments?overdue=true`. A delegation is considered stale when it is not Completed and either (a) its `DueDate` is in the past (local day) or (b) its last follow-up — or its creation time if never followed up — is older than 7 days. Unread-nudge counts come from the nudges capability when available. Each count SHALL be a link to the corresponding filtered route.

If a count source fails to load, that segment SHALL render a placeholder ("— commitments overdue") while the other segments continue to render their live values. If the nudges capability is unavailable on the current deployment, the nudges segment SHALL render "0 unread nudges" rather than erroring.

#### Scenario: All counts available

- **WHEN** the user has 3 overdue commitments, 2 stale delegations, and 1 unread nudge
- **THEN** the widget renders "3 commitments overdue · 2 delegations stale · 1 unread nudge" with each count linking to its filtered route

#### Scenario: Partial source failure

- **WHEN** `GET /api/delegations` returns HTTP 500 and the other sources succeed
- **THEN** the delegations segment renders "— delegations stale" and the commitments and nudges segments render their live values

## MODIFIED Requirements

### Requirement: Morning briefing dashboard widget

The frontend SHALL render a morning briefing widget on the authenticated dashboard home route, positioned as the full-width anchor row at the top of the dashboard widget shell. The widget SHALL call `POST /api/briefings/morning` on first mount via a signal-based `BriefingService`. The widget SHALL display the rendered markdown, the generated-at timestamp, and a "Regenerate" button that calls the endpoint with `force=true`.

The component SHALL NOT use `*ngIf`, `*ngFor`, `*ngSwitch`, or `[ngClass]`; it SHALL use `@if`, `@for`, `@switch`, `[class.x]` signal-aware syntax. Colours SHALL use PrimeNG / `tailwindcss-primeui` tokens only (no hardcoded Tailwind colour utilities, no `dark:` prefix).

The widget SHALL satisfy the widget isolation contract: its loading, error, and empty states SHALL render locally, and a failure in this widget MUST NOT prevent sibling dashboard widgets from rendering.

#### Scenario: Widget generates a briefing on first visit

- **WHEN** the user lands on the dashboard and no briefing exists yet for today
- **THEN** the widget displays a loading state, then renders the generated markdown

#### Scenario: Regenerate reuses force=true

- **WHEN** the user clicks "Regenerate"
- **THEN** the frontend calls `POST /api/briefings/morning?force=true` and re-renders

#### Scenario: Provider-not-configured state

- **WHEN** the endpoint returns HTTP 409 with `ai_provider_not_configured`
- **THEN** the widget renders a "Configure your AI provider" empty state with a link to settings

#### Scenario: Briefing error does not blank the dashboard

- **WHEN** the briefing endpoint returns any error (409, 500, network failure)
- **THEN** the widget renders its error or empty state locally and the sibling dashboard widgets (commitments, 1:1s, top of queue, overdue summary) still render their own live data
