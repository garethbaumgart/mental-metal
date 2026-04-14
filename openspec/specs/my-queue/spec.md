# My Queue

## Purpose

Provide the authenticated user with a single prioritised attention queue drawing from their open commitments, in-flight delegations, and stale captures, so they can see at a glance what needs their attention next. The queue is computed on demand with deterministic scoring and exposed via a read-only API and a dedicated UI route.

## Requirements

### Requirement: Get the prioritised queue

The system SHALL expose `GET /api/my-queue` returning the authenticated user's prioritised attention queue. The queue SHALL contain items derived from three sources, all scoped to the authenticated user's UserId:

1. **Commitment items** — every Commitment belonging to the user with `Status = Open` AND (`IsOverdue = true` OR `DueDate` is null OR `DueDate` is within `CommitmentDueSoonDays` (default 7) calendar days from now).
2. **Delegation items** — every Delegation belonging to the user with `Status` in `{Assigned, InProgress, Blocked}` AND (`IsOverdue = true` OR `(now - (LastFollowedUpAt ?? CreatedAt)).Days >= DelegationStalenessDays` (default 7) OR `Priority` in `{High, Urgent}`).
3. **Capture items** — every Capture belonging to the user that is NOT triaged AND is in one of: `Status = Raw`, `Status = Failed`, or `Status = Processed` with its extraction neither confirmed nor discarded, AND `(now - CapturedAtUtc).Days >= CaptureStalenessDays` (default 3).

Each item SHALL have a deterministically computed `priorityScore` (integer) per the scoring rules, and items SHALL be returned sorted by `priorityScore` descending, with ties broken by (dueDate ascending, then daysSinceCaptured descending (older captures first), then id ascending).

#### Scenario: Empty queue

- **WHEN** an authenticated user with no qualifying commitments, delegations, or captures sends GET /api/my-queue
- **THEN** the system returns HTTP 200 with an empty items array and all counts equal to 0

#### Scenario: Mixed queue across all three sources

- **WHEN** an authenticated user has one overdue open MineToThem commitment, one In-Progress delegation last followed up 10 days ago, and one Raw capture from 5 days ago
- **THEN** the response contains three items of types `commitment`, `delegation`, and `capture`
- **AND** each has a positive priorityScore

#### Scenario: User isolation

- **WHEN** User A and User B each have qualifying items
- **THEN** each user's queue contains only their own items

#### Scenario: Non-qualifying items excluded

- **WHEN** an authenticated user has a Completed commitment, a Completed delegation, and a triaged capture
- **THEN** none of those items appear in the queue

#### Scenario: Commitment without due date qualifies

- **WHEN** an authenticated user has an Open MineToThem commitment with no DueDate
- **THEN** the commitment appears in the queue with a positive priorityScore

### Requirement: Priority score is deterministic

The system SHALL compute `priorityScore` via a pure deterministic function of the item's current state and the configured thresholds. Two identical queries against identical data at the same wall-clock day SHALL return identical scores and identical ordering.

#### Scenario: Overdue commitment scores higher than due-soon commitment

- **WHEN** the queue contains one commitment 3 days overdue and one commitment due in 3 days, all else equal
- **THEN** the overdue commitment has a higher priorityScore and appears first

#### Scenario: Urgent delegation scores higher than Low delegation

- **WHEN** the queue contains two in-progress delegations, one Urgent and one Low, with identical ages and no due dates
- **THEN** the Urgent delegation has a higher priorityScore and appears first

#### Scenario: Blocked delegation receives a blocker bump

- **WHEN** the queue contains two otherwise-identical delegations, one InProgress and one Blocked
- **THEN** the Blocked delegation has a strictly higher priorityScore than the InProgress one

#### Scenario: Failed capture scores higher than an equally-old Raw capture

- **WHEN** the queue contains two captures of equal age past the staleness threshold, one Raw and one Failed
- **THEN** the Failed capture has a strictly higher priorityScore

### Requirement: Filter by scope

The endpoint SHALL accept a `scope` query parameter with allowed values `overdue`, `today`, `thisWeek`, `all`. Default is `all`.

- `overdue` — include only items whose `isOverdue` is true OR captures that qualify on staleness
- `today` — include items due today or overdue; captures always included if they otherwise qualify
- `thisWeek` — include items due within 7 calendar days (inclusive of overdue); captures always included
- `all` — no additional filtering beyond base qualification

Unknown values SHALL return HTTP 400.

#### Scenario: Overdue scope filters out future-due items

- **WHEN** an authenticated user has one overdue commitment and one commitment due in 3 days and sends GET /api/my-queue?scope=overdue
- **THEN** only the overdue commitment appears in the response

#### Scenario: ThisWeek scope includes items due within 7 days

- **WHEN** the queue has commitments due in 2, 5, and 10 days and the user requests scope=thisWeek
- **THEN** only the commitments due in 2 and 5 days appear

#### Scenario: Invalid scope rejected

- **WHEN** an authenticated user sends GET /api/my-queue?scope=someday
- **THEN** the system returns HTTP 400

### Requirement: Filter by item type

The endpoint SHALL accept a repeated `itemType` query parameter whose values match the `QueueItemType` enum (`Commitment`, `Delegation`, `Capture`) case-insensitively. When the parameter is absent, all three types are included. Unknown values SHALL return HTTP 400.

#### Scenario: Single type filter

- **WHEN** an authenticated user with mixed items sends GET /api/my-queue?itemType=commitment
- **THEN** the response contains only items with itemType=commitment

#### Scenario: Multiple type filter

- **WHEN** an authenticated user sends GET /api/my-queue?itemType=commitment&itemType=delegation
- **THEN** the response contains items with itemType commitment or delegation, but no captures

#### Scenario: Unknown item type rejected

- **WHEN** an authenticated user sends GET /api/my-queue?itemType=other
- **THEN** the system returns HTTP 400

### Requirement: Filter by linked person and initiative

The endpoint SHALL accept optional `personId` and `initiativeId` query parameters (Guid). When supplied:

- `personId` matches commitment.PersonId, delegation.DelegatePersonId, and captures whose LinkedPersonIds contain the value.
- `initiativeId` matches commitment.InitiativeId, delegation.InitiativeId, and captures whose LinkedInitiativeIds contain the value.

Filters SHALL combine conjunctively with scope and itemType filters.

#### Scenario: Filter by person

- **WHEN** an authenticated user sends GET /api/my-queue?personId={SarahId} and has one commitment with Sarah and one commitment with Alex
- **THEN** the response contains only the commitment linked to Sarah

#### Scenario: Filter by initiative

- **WHEN** an authenticated user sends GET /api/my-queue?initiativeId={PlatformId}
- **THEN** the response contains only items whose initiativeId equals PlatformId, or captures whose LinkedInitiativeIds contain PlatformId

#### Scenario: Combined filters

- **WHEN** an authenticated user sends GET /api/my-queue?scope=overdue&itemType=delegation&personId={AlexId}
- **THEN** the response contains only overdue delegations assigned to Alex

### Requirement: Suggest delegate hint on commitments

The system SHALL, for each commitment queue item, set `suggestDelegate = true` when all of the following hold:

1. The item type is `commitment`
2. The Commitment has `Status = Open` AND `Direction = MineToThem`
3. The Commitment has a non-null `PersonId`
4. The user has at least one Delegation whose `DelegatePersonId` equals the Commitment's `PersonId` and whose `Status` is not `Cancelled`

For all other items (including delegations, captures, and commitments that fail any check), `suggestDelegate` SHALL be `false`.

#### Scenario: Commitment with established delegation relationship

- **WHEN** the user has an open MineToThem commitment with PersonId=Sarah AND has an InProgress delegation to Sarah
- **THEN** the commitment queue item has suggestDelegate=true

#### Scenario: Commitment with no established delegation relationship

- **WHEN** the user has an open MineToThem commitment with PersonId=Alex and no delegations to Alex
- **THEN** the commitment queue item has suggestDelegate=false

#### Scenario: TheirsToMe commitment never suggests delegate

- **WHEN** the user has an open TheirsToMe commitment with PersonId=Sarah and an InProgress delegation to Sarah
- **THEN** the commitment queue item has suggestDelegate=false

#### Scenario: Delegation items never suggest delegate

- **WHEN** a delegation appears in the queue
- **THEN** its suggestDelegate field is false

#### Scenario: Capture items never suggest delegate

- **WHEN** a capture appears in the queue
- **THEN** its suggestDelegate field is false

### Requirement: Queue response includes counts

The response SHALL include a `counts` object with fields:

- `overdue` — number of items where `isOverdue = true` (captures never count here)
- `dueSoon` — number of commitment/delegation items due within `CommitmentDueSoonDays` and not overdue
- `staleCaptures` — number of capture items in the queue
- `staleDelegations` — number of delegation items whose days-since-touch equals or exceeds `DelegationStalenessDays`
- `total` — total number of items after filtering

Counts SHALL reflect the items returned AFTER filters are applied.

#### Scenario: Counts reflect filters

- **WHEN** a user has 2 overdue commitments and 3 captures and sends GET /api/my-queue?itemType=commitment
- **THEN** the response counts show total=2 and staleCaptures=0

#### Scenario: Counts on empty queue

- **WHEN** a user with no items sends GET /api/my-queue
- **THEN** every count is 0

### Requirement: Queue item fields

Each queue item SHALL expose the following fields. Fields not applicable to a given item type SHALL be null:

- `itemType`: `Commitment` | `Delegation` | `Capture` (PascalCase; serialized via `JsonStringEnumConverter`)
- `id`: the source aggregate id (Guid)
- `title`: the commitment description, delegation description, or capture title
- `status`: the source aggregate's status enum value as a string
- `dueDate`: ISO date string (commitments and delegations only)
- `isOverdue`: boolean (commitments and delegations only; always false for captures)
- `personId` / `personName`: linked person (commitment.PersonId, delegation.DelegatePersonId, or first capture.LinkedPersonIds — otherwise null)
- `initiativeId` / `initiativeName`: linked initiative (commitment.InitiativeId, delegation.InitiativeId, or first capture.LinkedInitiativeIds — otherwise null)
- `daysSinceCaptured`: integer (captures only; null otherwise)
- `lastFollowedUpAt`: ISO timestamp (delegations only; null otherwise)
- `priorityScore`: integer
- `suggestDelegate`: boolean

#### Scenario: Commitment item fields populated

- **WHEN** a commitment queue item is returned
- **THEN** itemType=commitment, title equals the commitment description, dueDate, isOverdue, personId, personName are populated from the commitment, and daysSinceCaptured, lastFollowedUpAt are null

#### Scenario: Capture item fields populated

- **WHEN** a capture queue item is returned
- **THEN** itemType=capture, daysSinceCaptured is a non-negative integer, dueDate and isOverdue are null/false, and lastFollowedUpAt is null

#### Scenario: Delegation item fields populated

- **WHEN** a delegation queue item is returned
- **THEN** itemType=delegation, lastFollowedUpAt reflects the delegation's value, dueDate and isOverdue reflect the delegation, and daysSinceCaptured is null

### Requirement: Configurable thresholds

The system SHALL expose a `MyQueue` options section with three integer knobs: `CommitmentDueSoonDays` (default 7), `DelegationStalenessDays` (default 7), `CaptureStalenessDays` (default 3). The thresholds SHALL influence both inclusion in the queue and priority scoring.

#### Scenario: Defaults apply when unconfigured

- **WHEN** the application starts with no `MyQueue` configuration section
- **THEN** the effective thresholds are 7, 7, and 3 respectively

#### Scenario: Configured override takes effect

- **WHEN** appsettings configures `MyQueue:CaptureStalenessDays=1`
- **THEN** captures at least 1 day old qualify for the queue

### Requirement: Queue view UI

The frontend SHALL provide a top-level **My Queue** route that fetches `GET /api/my-queue` via a signal-based service and renders a list of queue items with itemType indicator, title, linked person/initiative, status/priority badges, due date or days-since-captured, and the numeric priority. The page SHALL render scope filter controls (overdue/today/thisWeek/all) and item-type toggles that re-issue the query. The page SHALL NOT use `*ngIf`, `*ngFor`, `*ngSwitch`, or `[ngClass]` directives; it SHALL use `@if`, `@for`, `@switch`, and `[class.x]` signal-aware syntax. Colours SHALL use PrimeNG / `tailwindcss-primeui` tokens only (no hardcoded Tailwind colour utilities, no `dark:` prefix).

#### Scenario: User opens the queue

- **WHEN** an authenticated user navigates to /my-queue
- **THEN** the app fetches the queue and displays the items ordered by priority

#### Scenario: User changes scope

- **WHEN** the user clicks the "Overdue" scope chip
- **THEN** the queue re-fetches with scope=overdue and the list updates

#### Scenario: Empty state

- **WHEN** the user's queue is empty
- **THEN** the page shows an empty-state message and no list

### Requirement: Inline delegate suggestion UI

When a commitment queue item's `suggestDelegate` is true, the UI SHALL render an inline "Delegate this" action. Activating the action SHALL navigate to the existing delegation creation route with query parameters pre-filling `description` (from the commitment description), `personId` (from the commitment's PersonId), `initiativeId` (from the commitment's InitiativeId, when set), and `sourceCommitmentId` (the commitment id).

#### Scenario: Delegate hint shown and action dispatched

- **WHEN** the queue includes a commitment with suggestDelegate=true AND the user clicks "Delegate this" on that row
- **THEN** the app navigates to the delegation create form with description, personId, and sourceCommitmentId pre-filled

#### Scenario: Delegate hint not shown

- **WHEN** a commitment queue item has suggestDelegate=false
- **THEN** the row does not render the "Delegate this" action
