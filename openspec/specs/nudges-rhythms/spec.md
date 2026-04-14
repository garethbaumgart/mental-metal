# nudges-rhythms

## Purpose

Engineering managers rely on recurring rhythms -- "check in on Project X risks every Thursday", "follow up with Sarah on career goals monthly" -- but these normally live in memory, calendars, or ad-hoc notes and get dropped. The `nudges-rhythms` capability provides a first-class recurring-reminder primitive (a `Nudge` aggregate) with cadence-driven scheduling (Daily, Weekly, Biweekly, Monthly, Custom), optional links to people and initiatives, pause/resume lifecycle, deterministic mark-nudged advancement, and a list/edit UI. Nudges are user-scoped and form the scheduling backbone that later cross-cutting query surfaces (`my-queue`, `daily-weekly-briefing`) can build on.

## Requirements

### Requirement: Create a nudge

The system SHALL allow an authenticated user to create a new Nudge with a title and cadence. Title MUST NOT be empty and MUST NOT exceed 200 characters. Cadence MUST be one of `Daily`, `Weekly`, `Biweekly`, `Monthly`, or `Custom`. When cadence is `Weekly` or `Biweekly`, `DayOfWeek` MUST be provided. When cadence is `Monthly`, `DayOfMonth` MUST be provided and MUST be between 1 and 31. When cadence is `Custom`, `CustomIntervalDays` MUST be provided and MUST be between 1 and 365. Optional fields: `PersonId`, `InitiativeId`, `Notes` (up to 2000 characters), `StartDate` (defaults to today). The system SHALL compute `NextDueDate` from the cadence and start date, set `IsActive=true`, set `CreatedAtUtc`, and raise a `NudgeCreated` domain event. The Nudge SHALL be scoped to the authenticated user's UserId.

#### Scenario: Create a daily nudge

- **WHEN** an authenticated user sends a POST to `/api/nudges` with title "Review risk log" and cadence "Daily"
- **THEN** the system creates a Nudge with cadence Daily, NextDueDate equal to today, IsActive=true, and returns HTTP 201

#### Scenario: Create a weekly nudge anchored to Thursday

- **WHEN** an authenticated user sends a POST to `/api/nudges` with title "Project X risks", cadence "Weekly", and dayOfWeek "Thursday"
- **THEN** the system creates a Nudge with NextDueDate equal to the next Thursday on or after today and returns HTTP 201

#### Scenario: Create a monthly nudge anchored to day 15

- **WHEN** an authenticated user sends a POST to `/api/nudges` with title "Career check-in with Sarah", cadence "Monthly", dayOfMonth 15, and personId for Sarah
- **THEN** the system creates a Nudge with NextDueDate equal to the 15th of the current or next month and returns HTTP 201

#### Scenario: Create a custom-interval nudge

- **WHEN** an authenticated user sends a POST to `/api/nudges` with title "Retro temperature", cadence "Custom", and customIntervalDays 10
- **THEN** the system creates a Nudge advancing every 10 days and returns HTTP 201

#### Scenario: Empty title rejected

- **WHEN** an authenticated user sends a POST to `/api/nudges` with an empty title
- **THEN** the system returns HTTP 400 with error code `nudge.validation`

#### Scenario: Title over 200 characters rejected

- **WHEN** an authenticated user sends a POST to `/api/nudges` with a title longer than 200 characters
- **THEN** the system returns HTTP 400 with error code `nudge.validation`

#### Scenario: Weekly without day of week rejected

- **WHEN** an authenticated user sends a POST to `/api/nudges` with cadence "Weekly" and no dayOfWeek
- **THEN** the system returns HTTP 400 with error code `nudge.invalidCadence`

#### Scenario: Custom without positive interval rejected

- **WHEN** an authenticated user sends a POST to `/api/nudges` with cadence "Custom" and customIntervalDays 0 or negative
- **THEN** the system returns HTTP 400 with error code `nudge.invalidCadence`

#### Scenario: PersonId for another user rejected

- **WHEN** an authenticated user sends a POST to `/api/nudges` with a personId that belongs to a different user
- **THEN** the system returns HTTP 400 with error code `nudge.linkedEntityNotFound`

#### Scenario: User isolation

- **WHEN** User A creates a nudge and User B creates a nudge
- **THEN** each nudge is scoped to its respective user's UserId

### Requirement: List nudges with filters

The system SHALL allow an authenticated user to retrieve their nudges. The list SHALL support optional filters: `isActive` (true/false), `personId`, `initiativeId`, `dueBefore` (DateOnly -- returns active nudges with `NextDueDate <= dueBefore`), and `dueWithinDays` (integer -- returns active nudges due within N days from today). The list SHALL be ordered by `NextDueDate` ascending, with paused nudges sorted last when `isActive` is not filtered.

#### Scenario: List all nudges

- **WHEN** an authenticated user sends a GET to `/api/nudges`
- **THEN** the system returns all of that user's nudges ordered by NextDueDate ascending with HTTP 200

#### Scenario: Filter active

- **WHEN** an authenticated user sends a GET to `/api/nudges?isActive=true`
- **THEN** the system returns only nudges with IsActive=true

#### Scenario: Filter due today

- **WHEN** an authenticated user sends a GET to `/api/nudges?dueBefore=<today>&isActive=true`
- **THEN** the system returns only active nudges whose NextDueDate is on or before today

#### Scenario: Filter due this week

- **WHEN** an authenticated user sends a GET to `/api/nudges?dueWithinDays=7`
- **THEN** the system returns only active nudges whose NextDueDate is within 7 days from today

#### Scenario: Filter by person

- **WHEN** an authenticated user sends a GET to `/api/nudges?personId={id}`
- **THEN** the system returns only nudges linked to that person

#### Scenario: Filter by initiative

- **WHEN** an authenticated user sends a GET to `/api/nudges?initiativeId={id}`
- **THEN** the system returns only nudges linked to that initiative

#### Scenario: Empty list

- **WHEN** an authenticated user with no nudges sends a GET to `/api/nudges`
- **THEN** the system returns an empty array with HTTP 200

### Requirement: Get nudge by ID

The system SHALL allow an authenticated user to retrieve a single nudge by ID. A nudge belonging to another user SHALL be treated as not found (no existence leak).

#### Scenario: Get existing nudge

- **WHEN** an authenticated user sends a GET to `/api/nudges/{id}` with a valid nudge ID they own
- **THEN** the system returns the nudge with all fields including cadence, anchors, NextDueDate, LastNudgedAt, IsActive, and linked IDs

#### Scenario: Nudge not found

- **WHEN** an authenticated user sends a GET to `/api/nudges/{id}` with an ID that does not exist
- **THEN** the system returns HTTP 404 with error code `nudge.notFound`

#### Scenario: Nudge belongs to another user

- **WHEN** an authenticated user sends a GET to `/api/nudges/{id}` for a nudge owned by a different user
- **THEN** the system returns HTTP 404 with error code `nudge.notFound`

### Requirement: Update nudge details

The system SHALL allow an authenticated user to update a nudge's title, notes, and optional links (PersonId, InitiativeId) via `PATCH /api/nudges/{id}`. Title MUST NOT be empty and MUST NOT exceed 200 characters. Notes MUST NOT exceed 2000 characters. Updating details SHALL set `UpdatedAtUtc` and raise a `NudgeUpdated` domain event when any field changes.

#### Scenario: Update title

- **WHEN** an authenticated user sends a PATCH to `/api/nudges/{id}` with a new title
- **THEN** the system updates the title, sets UpdatedAtUtc, and returns HTTP 200

#### Scenario: Update notes

- **WHEN** an authenticated user sends a PATCH to `/api/nudges/{id}` with new notes
- **THEN** the system updates the notes and returns HTTP 200

#### Scenario: Set person link

- **WHEN** an authenticated user sends a PATCH to `/api/nudges/{id}` with a personId
- **THEN** the system sets the PersonId and returns HTTP 200

#### Scenario: Clear person link

- **WHEN** an authenticated user sends a PATCH to `/api/nudges/{id}` with personId=null
- **THEN** the system clears the PersonId and returns HTTP 200

#### Scenario: Empty title rejected

- **WHEN** an authenticated user sends a PATCH to `/api/nudges/{id}` with an empty title
- **THEN** the system returns HTTP 400 with error code `nudge.validation`

#### Scenario: PersonId for another user rejected

- **WHEN** an authenticated user sends a PATCH to `/api/nudges/{id}` with a personId belonging to a different user
- **THEN** the system returns HTTP 400 with error code `nudge.linkedEntityNotFound`

### Requirement: Update cadence

The system SHALL expose `PATCH /api/nudges/{id}/cadence` for cadence updates (distinct from `PATCH /api/nudges/{id}`, which handles title/notes/links). When the cadence changes, the system SHALL recompute `NextDueDate` from today using the new cadence's `CalculateFirst(today)`. Validation rules from "Create a nudge" SHALL apply. The system SHALL raise a `NudgeCadenceChanged` domain event.

#### Scenario: Change from Weekly to Monthly

- **WHEN** an authenticated user sends PATCH `/api/nudges/{id}/cadence` changing a Weekly nudge to Monthly with dayOfMonth 1
- **THEN** the system updates the cadence, recomputes NextDueDate to the next 1st of the month on or after today, and returns HTTP 200

#### Scenario: Invalid new cadence rejected

- **WHEN** an authenticated user sends PATCH `/api/nudges/{id}/cadence` with cadence Custom and customIntervalDays 0
- **THEN** the system returns HTTP 400 with error code `nudge.invalidCadence`

### Requirement: Mark nudge as nudged

The system SHALL allow an authenticated user to record that they acted on a nudge via `POST /api/nudges/{id}/mark-nudged`. The system SHALL set `LastNudgedAt` to the current UTC timestamp and advance `NextDueDate` to the next occurrence strictly after today via `Cadence.CalculateNext(today)`. Only active nudges can be marked. The system SHALL raise a `NudgeNudged` domain event.

#### Scenario: Mark a daily nudge

- **WHEN** an authenticated user sends a POST to `/api/nudges/{id}/mark-nudged` for an active daily nudge
- **THEN** the system advances NextDueDate to tomorrow, sets LastNudgedAt, and returns HTTP 200

#### Scenario: Mark a weekly Thursday nudge

- **WHEN** an authenticated user sends a POST to `/api/nudges/{id}/mark-nudged` for an active weekly Thursday nudge on a Thursday
- **THEN** the system advances NextDueDate to the following Thursday, sets LastNudgedAt, and returns HTTP 200

#### Scenario: Mark a monthly nudge with day clamp

- **WHEN** a monthly nudge is anchored to dayOfMonth 31 and is marked on January 31st
- **THEN** the system advances NextDueDate to February 28th or 29th (clamped to month-end)

#### Scenario: Mark a paused nudge rejected

- **WHEN** an authenticated user sends a POST to `/api/nudges/{id}/mark-nudged` for a paused nudge
- **THEN** the system returns HTTP 409 with error code `nudge.notActive`

#### Scenario: Nudge not found

- **WHEN** an authenticated user sends a POST to `/api/nudges/{id}/mark-nudged` with an ID that does not exist or belongs to another user
- **THEN** the system returns HTTP 404 with error code `nudge.notFound`

### Requirement: Pause a nudge

The system SHALL allow an authenticated user to pause an active nudge via `POST /api/nudges/{id}/pause`. The system SHALL set `IsActive=false` while preserving `NextDueDate` and `LastNudgedAt`. The system SHALL raise a `NudgePaused` domain event. Only active nudges can be paused.

#### Scenario: Pause an active nudge

- **WHEN** an authenticated user sends a POST to `/api/nudges/{id}/pause` for an active nudge
- **THEN** the system sets IsActive=false and returns HTTP 200

#### Scenario: Pause an already paused nudge rejected

- **WHEN** an authenticated user sends a POST to `/api/nudges/{id}/pause` for a paused nudge
- **THEN** the system returns HTTP 409 with error code `nudge.alreadyPaused`

### Requirement: Resume a nudge

The system SHALL allow an authenticated user to resume a paused nudge via `POST /api/nudges/{id}/resume`. The system SHALL set `IsActive=true` and recompute `NextDueDate` from today using the current cadence. The system SHALL raise a `NudgeResumed` domain event. Only paused nudges can be resumed.

#### Scenario: Resume a paused nudge

- **WHEN** an authenticated user sends a POST to `/api/nudges/{id}/resume` for a paused nudge
- **THEN** the system sets IsActive=true, recomputes NextDueDate from today, and returns HTTP 200

#### Scenario: Resume an active nudge rejected

- **WHEN** an authenticated user sends a POST to `/api/nudges/{id}/resume` for an active nudge
- **THEN** the system returns HTTP 409 with error code `nudge.alreadyActive`

### Requirement: Delete a nudge

The system SHALL allow an authenticated user to delete a nudge via `DELETE /api/nudges/{id}`. The system SHALL raise a `NudgeDeleted` domain event.

#### Scenario: Delete an existing nudge

- **WHEN** an authenticated user sends a DELETE to `/api/nudges/{id}` for a nudge they own
- **THEN** the system removes the nudge and returns HTTP 204

#### Scenario: Delete nudge not found

- **WHEN** an authenticated user sends a DELETE to `/api/nudges/{id}` with an ID that does not exist or belongs to another user
- **THEN** the system returns HTTP 404 with error code `nudge.notFound`

### Requirement: Determinism -- inject current time

The system SHALL compute `NextDueDate` using a `TimeProvider` injected into application handlers. Domain methods (`Create`, `MarkNudged`, `Resume`, `UpdateCadence`) SHALL accept the current date as a parameter. Handlers SHALL NOT read `DateTime.UtcNow` directly.

#### Scenario: Tests use fixed time

- **WHEN** a unit test constructs a Nudge with a fixed `today` value and marks it as nudged with a fixed `now`
- **THEN** the resulting NextDueDate is deterministic and asserts pass

### Requirement: Nudges list UI

The frontend SHALL provide a `/nudges` page that displays nudges with title, cadence label, next due date, person link, initiative link, active/paused badge, and actions (Mark as nudged, Pause/Resume, Edit, Delete). The page SHALL support filters for active/paused, due today/this week, by person, and by initiative.

#### Scenario: View nudge list

- **WHEN** a user navigates to `/nudges`
- **THEN** the page displays the user's nudges with cadence, next due date, person/initiative links, and active status

#### Scenario: Filter due today

- **WHEN** a user selects the "Due today" filter
- **THEN** only nudges with NextDueDate on or before today and IsActive=true are shown

#### Scenario: Mark as nudged from list

- **WHEN** a user clicks "Mark as nudged" on an active nudge in the list
- **THEN** the nudge's NextDueDate advances and the row updates without a full page reload

#### Scenario: Pause and resume from list

- **WHEN** a user clicks "Pause" on an active nudge, then "Resume" on the same nudge
- **THEN** the badge toggles and NextDueDate recomputes from today on resume

### Requirement: Nudge create/edit form

The frontend SHALL provide a dialog for creating and editing nudges. The form SHALL include title, cadence selector, conditional anchor inputs (DayOfWeek for Weekly/Biweekly, DayOfMonth for Monthly, CustomIntervalDays for Custom), start date picker, optional person selector, optional initiative selector, and notes.

#### Scenario: Create a nudge via dialog

- **WHEN** a user opens the create dialog, fills in title, selects Weekly, picks Thursday, and submits
- **THEN** a new nudge is created and appears in the list

#### Scenario: Edit a nudge

- **WHEN** a user edits a nudge's title and cadence and submits
- **THEN** the nudge is updated and the list reflects the new values

#### Scenario: Cadence-specific anchors shown conditionally

- **WHEN** a user selects cadence "Monthly" in the dialog
- **THEN** a DayOfMonth input is shown and the DayOfWeek input is hidden
