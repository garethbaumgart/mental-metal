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
- `oneOnOnesToday` — every OneOnOne with `OccurredOnUtc` (or scheduled date equivalent) whose local calendar date equals today.
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

### Requirement: Morning briefing dashboard widget

The frontend SHALL render a morning briefing widget on the authenticated dashboard home route. The widget SHALL call `POST /api/briefings/morning` on first mount via a signal-based `BriefingService`. The widget SHALL display the rendered markdown, the generated-at timestamp, and a "Regenerate" button that calls the endpoint with `force=true`.

The component SHALL NOT use `*ngIf`, `*ngFor`, `*ngSwitch`, or `[ngClass]`; it SHALL use `@if`, `@for`, `@switch`, `[class.x]` signal-aware syntax. Colours SHALL use PrimeNG / `tailwindcss-primeui` tokens only (no hardcoded Tailwind colour utilities, no `dark:` prefix).

#### Scenario: Widget generates a briefing on first visit

- **WHEN** the user lands on the dashboard and no briefing exists yet for today
- **THEN** the widget displays a loading state, then renders the generated markdown

#### Scenario: Regenerate reuses force=true

- **WHEN** the user clicks "Regenerate"
- **THEN** the frontend calls `POST /api/briefings/morning?force=true` and re-renders

#### Scenario: Provider-not-configured state

- **WHEN** the endpoint returns HTTP 409 with `ai_provider_not_configured`
- **THEN** the widget renders a "Configure your AI provider" empty state with a link to settings

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

