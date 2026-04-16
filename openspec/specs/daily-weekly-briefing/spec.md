# Daily / Weekly Briefing

## Purpose

Generate AI-powered briefings that assemble already-captured operational state (open commitments, in-flight delegations, today's 1:1s, recent observations, living-brief status, milestones, recently-touched captures) into short, scannable markdown narratives. Three briefing shapes are supported: the **morning** briefing summarising today's focus, the **weekly** briefing covering the current ISO week, and the **1:1 prep** sheet for an individual person. Facts are assembled deterministically from repository state and the user's clock; the LLM only narrates the supplied facts and is forbidden from inventing names, dates, or counts. Each generated briefing is persisted as a read-model row keyed by `(UserId, Type, ScopeKey)` with cache-or-regenerate semantics so refreshes don't burn LLM budget.

## Requirements
### Requirement: Generate morning briefing

The system SHALL expose `POST /api/briefings/morning` that returns the authenticated user's morning briefing for today. The endpoint SHALL accept an optional `force` query parameter (default `false`).

The response body SHALL be a `BriefingResponse` DTO containing `id`, `type` (`Morning`), `scopeKey`, `generatedAtUtc`, `markdownBody`, `model`, `inputTokens`, `outputTokens`, and `factsSummary` (an object with the deterministic facts used).

Caching and staleness rules:

- `scopeKey` SHALL equal `morning:{yyyy-MM-dd}` computed from the user's local date (derived from `User.Preferences.TimeZone`; fallback UTC). If the user's current local hour (in the same time zone) is less than `BriefingOptions.MorningBriefingHour`, the previous calendar date SHALL be used.
- When `force = false` and a persisted briefing exists with the same `(UserId, Type=Morning, ScopeKey)` whose `GeneratedAtUtc` is within `BriefingOptions.MorningBriefingStaleHours` of now, the system SHALL return HTTP 200 with that persisted briefing.
- Otherwise the system SHALL generate a new briefing, persist it, and return HTTP 201.

Facts assembly: the system SHALL assemble `MorningBriefingFacts` deterministically from the user's own data, scoped to UserId:

- `topCommitmentsDueToday` — up to `BriefingOptions.TopItemsPerSection` open commitments with `DueDate` equal to the user-local today OR `IsOverdue = true`, sorted by (overdue desc, dueDate asc, id asc).
- `oneOnOnesToday` — every OneOnOne with `OccurredAt` (or scheduled date equivalent) whose local calendar date equals today.
- `overdueDelegations` — up to `TopItemsPerSection` delegations with `IsOverdue = true`, sorted by daysOverdue desc.
- `recentCaptures` — up to `TopItemsPerSection` captures `CapturedAtUtc >= now - 24h`, sorted by CapturedAtUtc desc.
- `peopleNeedingAttention` — up to `TopItemsPerSection` people whose last OneOnOne was more than 14 days ago (or never) and who have at least one open commitment or delegation.

Synthesis: the system SHALL call `IAiCompletionService` with a system prompt forbidding invention, a user prompt containing the facts JSON, `Temperature = 0.3`, and `MaxTokens = BriefingOptions.MaxBriefingTokens`. The returned markdown SHALL be stored as `MarkdownBody`.

Authorization: the endpoint SHALL require authentication; unauthenticated requests SHALL return HTTP 401.

Provider precondition: if the user has no `AiProviderConfig` set, the system SHALL return HTTP 409 with an error code `ai_provider_not_configured`.

#### Scenario: First call of the day generates a new briefing

- **WHEN** an authenticated user with a configured AI provider sends `POST /api/briefings/morning` and no briefing exists for today's scopeKey
- **THEN** the system assembles facts, calls the AI provider, persists a new `Briefing` row, and returns HTTP 201 with the body

#### Scenario: Second call of the day returns cached briefing

- **WHEN** a briefing was generated 30 minutes ago for today's scopeKey and the user calls `POST /api/briefings/morning` again
- **THEN** the system returns HTTP 200 with the existing briefing and does NOT call the AI provider

#### Scenario: Force regenerates even if cached

- **WHEN** a briefing exists for today's scopeKey and the user calls `POST /api/briefings/morning?force=true`
- **THEN** the system generates a new briefing, persists a new row, and returns HTTP 201

#### Scenario: User without AI provider configured

- **WHEN** a user with no `AiProviderConfig` sends `POST /api/briefings/morning`
- **THEN** the system returns HTTP 409 with error code `ai_provider_not_configured` and does not persist a row

#### Scenario: Unauthenticated request rejected

- **WHEN** an unauthenticated client sends `POST /api/briefings/morning`
- **THEN** the system returns HTTP 401

#### Scenario: Time zone respected for scopeKey

- **WHEN** User A has `Preferences.TimeZone = "Pacific/Auckland"` and calls the endpoint at a UTC time that is early-morning next-day in Auckland
- **THEN** the scopeKey reflects the Auckland local date

### Requirement: Generate weekly briefing

The system SHALL expose `POST /api/briefings/weekly` that returns the authenticated user's briefing for the current ISO week. The endpoint SHALL accept an optional `force` query parameter.

`scopeKey` SHALL equal `weekly:{ISO-year}-W{ISO-week:D2}` computed from the user's local date. Caching follows the same cache-or-regenerate pattern as morning briefings using `BriefingOptions.WeeklyBriefingStaleHours`.

Facts assembly `WeeklyBriefingFacts`:

- `milestonesThisWeek` — up to 2 × `TopItemsPerSection` initiative milestones whose target date falls within Monday–Sunday of the current ISO week.
- `overdueCommitments` / `overdueDelegations` — all items currently overdue, capped at 2 × `TopItemsPerSection` per type.
- `initiativesNeedingAttention` — Active initiatives whose `LivingBrief.SummaryLastRefreshedAt` is null or older than 7 days. (The Initiative aggregate has no explicit "AtRisk" status; living-brief staleness is the closest "needs attention" signal currently modelled.)
- `peopleWithoutRecent1on1` — people with no OneOnOne in 21 days (or no OneOnOne at all) AND with at least one open commitment or delegation, capped at `TopItemsPerSection`. (The open-items precondition is intentional: people with no recent 1:1 AND no in-flight work do not produce actionable briefing items.)
- `weekNumber` / `weekStartIso` / `weekEndIso` — labels for the prompt.

Synthesis SHALL produce a markdown document with sections (Focus, Milestones, Overdue, Attention) of combined length ≤ `MaxBriefingTokens`.

#### Scenario: Weekly briefing generated for the current ISO week

- **WHEN** an authenticated user with a configured AI provider calls `POST /api/briefings/weekly` on 2026-04-14 (ISO week 16)
- **THEN** the persisted row has `scopeKey = "weekly:2026-W16"` and HTTP 201 is returned

#### Scenario: Weekly briefing caches within the week

- **WHEN** a weekly briefing was generated 2 hours ago and the user calls the endpoint again
- **THEN** HTTP 200 is returned and no AI call is made

#### Scenario: Force regenerates

- **WHEN** the user calls `POST /api/briefings/weekly?force=true`
- **THEN** a new briefing is generated and persisted regardless of staleness

### Requirement: Generate 1:1 prep sheet

The system SHALL expose `POST /api/briefings/one-on-one/{personId}` that returns a prep sheet for the given person. The endpoint SHALL accept an optional `force` query parameter.

`scopeKey` SHALL equal `oneonone:{personId:N}` (lowercase hex, no dashes). Caching uses `BriefingOptions.OneOnOnePrepStaleHours` (default 12).

The endpoint SHALL return HTTP 404 when the person does not exist or does not belong to the authenticated user.

Facts assembly `OneOnOnePrepFacts`:

- `person` — id, display name, type.
- `lastOneOnOne` — the most recent OneOnOne for this person (occurred date, summary or first 240 chars of notes).
- `openGoals` — all goals with status `Active` for this person.
- `recentObservations` — up to `TopItemsPerSection` observations created within 30 days, sorted by CreatedAtUtc desc, bodies truncated to 240 chars.
- `openCommitmentsWithPerson` — commitments with `PersonId = {personId}` whose `Status = Open`, capped at `TopItemsPerSection`.
- `openDelegationsToPerson` — delegations with `DelegatePersonId = {personId}` whose `Status` is not `Completed` or `Cancelled`, capped at `TopItemsPerSection`.

Synthesis SHALL produce a markdown document with sections (Context, Open Items, Recent Observations, Suggested Talking Points). The prompt SHALL instruct the model to produce exactly 3 to 5 talking-point bullets.

#### Scenario: Prep sheet generated for a valid person

- **WHEN** an authenticated user with a configured AI provider calls `POST /api/briefings/one-on-one/{personId}` for a person they own
- **THEN** the system persists a `Briefing` with `Type=OneOnOnePrep`, `ScopeKey="oneonone:{personId:N}"` and returns HTTP 201

#### Scenario: Person not found or not owned

- **WHEN** the user calls the endpoint for a personId that doesn't exist or belongs to another user
- **THEN** HTTP 404 is returned

#### Scenario: Cached prep sheet returned on second call

- **WHEN** a prep sheet was generated 1 hour ago and the user calls the endpoint again with the same personId
- **THEN** HTTP 200 is returned with the cached briefing

### Requirement: Get recent briefings

The system SHALL expose `GET /api/briefings/recent` returning the most-recent briefings for the authenticated user, sorted by `GeneratedAtUtc` descending. The number of items returned SHALL be capped by the `limit` query parameter (default 20, max 50).

Supported query parameters:

- `type` — optional; when provided MUST be one of `Morning`, `Weekly`, `OneOnOnePrep` (case-insensitive). Unknown values SHALL return HTTP 400.
- `limit` — integer 1..50 (default 20). Values outside the range SHALL return HTTP 400.

The response SHALL be a list of `BriefingSummary` (no `markdownBody`, no `factsSummary` — just id, type, scopeKey, generatedAtUtc, model, tokens).

#### Scenario: List recent briefings

- **WHEN** an authenticated user with 3 briefings calls `GET /api/briefings/recent`
- **THEN** HTTP 200 is returned with an array of 3 summaries ordered by generatedAtUtc desc

#### Scenario: Filter by type

- **WHEN** a user calls `GET /api/briefings/recent?type=Weekly`
- **THEN** only `Weekly` briefings are returned

#### Scenario: Invalid type rejected

- **WHEN** a user calls `GET /api/briefings/recent?type=monthly`
- **THEN** HTTP 400 is returned

#### Scenario: User isolation

- **WHEN** User A and User B each have briefings
- **THEN** each sees only their own via this endpoint

### Requirement: Get briefing by id

The system SHALL expose `GET /api/briefings/{id}` returning the full `BriefingResponse` (including `markdownBody` and `factsSummary`) when the briefing exists and belongs to the authenticated user.

#### Scenario: Read own briefing

- **WHEN** a user requests a briefing id they own
- **THEN** HTTP 200 is returned with full body

#### Scenario: Read unknown or foreign briefing

- **WHEN** a user requests a briefing id that does not exist or belongs to another user
- **THEN** HTTP 404 is returned

### Requirement: Briefing aggregate and repository

The system SHALL define a `Briefing` aggregate (lightweight read-model) with fields `Id` (Guid), `UserId` (Guid), `Type` (`BriefingType` enum: `Morning`, `Weekly`, `OneOnOnePrep`), `ScopeKey` (string, ≤128 chars), `GeneratedAtUtc` (UTC), `MarkdownBody` (string), `PromptFactsJson` (jsonb), `Model` (string ≤64 chars), `InputTokens` (int ≥ 0), `OutputTokens` (int ≥ 0).

A factory method `Briefing.Create(userId, type, scopeKey, generatedAtUtc, markdownBody, promptFactsJson, model, inputTokens, outputTokens)` SHALL enforce non-null `markdownBody` and non-empty `scopeKey`.

The repository `IBriefingRepository` SHALL expose `AddAsync`, `GetByIdAsync(userId, id)`, `GetLatestAsync(userId, type, scopeKey)`, and `ListRecentAsync(userId, type?, limit)`.

The EF configuration SHALL create a unique index on `(UserId, Type, ScopeKey, GeneratedAtUtc)` to support efficient `GetLatestAsync` lookups and to defend against duplicate-row races on identical timestamps.

#### Scenario: Scope key non-empty invariant

- **WHEN** the factory is called with an empty scopeKey
- **THEN** it throws an `ArgumentException`

#### Scenario: User scoping enforced in repository reads

- **WHEN** `GetByIdAsync(userId, id)` is called with a userId that does not match the stored row
- **THEN** the method returns null

### Requirement: Deterministic facts and time provider

The system SHALL inject `TimeProvider` into `BriefingService` and read all current-time values through it. Facts assembly SHALL be a pure function of repository state and the provided `TimeProvider` — two calls within the same clock tick with identical repository state SHALL produce identical facts.

#### Scenario: Two concurrent calls with same clock tick produce identical facts

- **WHEN** the service is invoked twice with a `FakeTimeProvider` fixed at the same instant against the same database state
- **THEN** the generated `MorningBriefingFacts` objects are equal

### Requirement: Options validation

The system SHALL register `BriefingOptions` via `AddOptions<BriefingOptions>().Bind(...).ValidateDataAnnotations().ValidateOnStart()`. The options class SHALL carry `[Range]` attributes: `MorningBriefingHour` (0..23, default 5), `MorningBriefingStaleHours` (1..72, default 12), `WeeklyBriefingStaleHours` (1..72, default 12), `OneOnOnePrepStaleHours` (1..72, default 12), `MaxBriefingTokens` (200..4000, default 1500), `TopItemsPerSection` (1..20, default 5).

#### Scenario: Out-of-range option rejected at startup

- **WHEN** `appsettings.json` sets `Briefing:MaxBriefingTokens = 99999`
- **THEN** application startup fails with an options-validation exception

#### Scenario: Defaults apply when unconfigured

- **WHEN** the application starts with no `Briefing` configuration section
- **THEN** `MorningBriefingHour = 5`, `MorningBriefingStaleHours = 12`, `WeeklyBriefingStaleHours = 12`, `OneOnOnePrepStaleHours = 12`, `MaxBriefingTokens = 1500`, `TopItemsPerSection = 5`

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

Each widget SHALL render one of four local states: `loading`, `error`, `empty`, or `data`. The `error` state SHALL name the data source that failed so the user can locate it on its full route (for example, "Couldn't load commitments — try the Commitments page"). Actions taken in one widget (for example, marking a commitment complete) MUST refetch only that widget's data. Widgets MAY read from shared singleton services (such as `MyQueueService`) but MUST treat that state as read-only; the widget is responsible for triggering its own load/refresh.

#### Scenario: Briefing fails, other widgets still render

- **WHEN** the morning briefing endpoint returns HTTP 409 `ai_provider_not_configured`
- **THEN** the briefing widget renders the "Configure your AI provider" empty state with a settings link, and the commitments, 1:1s, top-of-queue, and overdue-summary widgets render their live data independently

#### Scenario: One sibling widget fails, others still render

- **WHEN** `GET /api/delegations` returns HTTP 500 while the dashboard is loading
- **THEN** the overdue-summary widget renders "— delegations stale" for the delegations segment, but the commitments, 1:1s, and top-of-queue widgets render their live data and the briefing widget renders its markdown

#### Scenario: Quick action on one widget does not reload others

- **WHEN** the user clicks "Mark complete" on a commitment row in the today's-commitments widget
- **THEN** only the commitments widget refetches; the briefing, 1:1s, top-of-queue, and overdue-summary widgets do not re-issue their requests

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

### Requirement: Today's commitments widget

The dashboard SHALL include a "Today's commitments" widget that fetches open commitments from `GET /api/commitments` and displays those whose `DueDate` equals the user's local today OR whose `IsOverdue = true`. The widget SHALL cap the visible list at 5 rows, sorted by (overdue desc, dueDate asc). If more than 5 match, the widget SHALL show a "View all" link to `/commitments`.

Each row SHALL display the commitment description, the due date (if set), an overdue badge when `IsOverdue = true`, and an inline "Mark complete" quick action. The action SHALL call the existing `POST /api/commitments/{id}/complete` endpoint used on the `/commitments` route. On success the widget SHALL refetch its own list. A row's action button SHALL be disabled and show a loading indicator while the mutation is in flight.

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

The dashboard SHALL include a "Top of queue" widget that fetches `GET /api/my-queue` and renders the top 5 items in the queue's existing priority order (as defined by the `my-queue` capability). Each row SHALL show the item type badge and title. The widget SHALL provide a "View all" link to `/my-queue`.

When the queue is empty, the widget SHALL render an empty-state message ("Queue is empty. Nice work.").

#### Scenario: Widget shows top 5 queue items

- **WHEN** the user's queue contains 12 items
- **THEN** the widget renders the top 5 ranked items and a "View all" link to `/my-queue`

#### Scenario: Empty queue

- **WHEN** the queue has no items
- **THEN** the widget renders "Queue is empty. Nice work."

### Requirement: Overdue summary widget

The dashboard SHALL include an "Overdue summary" widget that renders a single compact count bar across three segments: commitments overdue, delegations stale, and unread nudges. Commitments overdue counts are derived from `GET /api/commitments?overdue=true`. A delegation is considered stale when it is not Completed and either (a) its `DueDate` is in the past (local day) or (b) its last follow-up — or its creation time if never followed up — is older than 7 days. Unread-nudge counts come from the nudges capability when available. Each count SHALL be a link to the corresponding filtered route.

If a count source fails to load, that segment SHALL render a placeholder ("— commitments overdue") while the other segments continue to render their live values. If the nudges capability is unavailable on the current deployment, the nudges segment SHALL render "0 unread nudges" rather than erroring.

#### Scenario: All counts available

- **WHEN** the user has 3 overdue commitments, 2 stale delegations, and 1 unread nudge
- **THEN** the widget renders "3 commitments overdue · 2 delegations stale · 1 unread nudge" with each count linking to its filtered route

#### Scenario: Partial source failure

- **WHEN** `GET /api/delegations` returns HTTP 500 and the other sources succeed
- **THEN** the delegations segment renders "— delegations stale" and the commitments and nudges segments render their live values

### Requirement: Weekly briefing page

The frontend SHALL provide a route `/briefings/weekly` with a `WeeklyBriefingPageComponent` that fetches the current weekly briefing on mount via `POST /api/briefings/weekly` and renders the markdown. The page SHALL provide a "Regenerate" button that calls the endpoint with `force=true`. It SHALL link back to the recent-briefings list.

#### Scenario: User opens the weekly briefing

- **WHEN** an authenticated user navigates to `/briefings/weekly`
- **THEN** the app calls the endpoint and renders the returned briefing

### Requirement: 1:1 prep action on person detail

The person detail page SHALL provide a "Generate 1:1 prep" button that, when clicked, calls `POST /api/briefings/one-on-one/{personId}` and displays the returned markdown in a PrimeNG dialog or drawer. The dialog SHALL show the generated-at timestamp and a "Regenerate" action.

#### Scenario: Prep sheet displayed in dialog

- **WHEN** an authenticated user clicks "Generate 1:1 prep" on a person's detail page
- **THEN** the dialog opens and shows the rendered markdown

#### Scenario: Regenerate prep sheet

- **WHEN** the user clicks "Regenerate" inside the dialog
- **THEN** the endpoint is called with `force=true` and the dialog content updates

