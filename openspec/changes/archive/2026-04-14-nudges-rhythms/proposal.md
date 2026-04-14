## Why

Engineering managers rely on recurring rhythms -- "check in on Project X risks every Thursday", "follow up with Sarah on career goals monthly" -- but these live in memory, calendars, or ad-hoc notes and get dropped. Mental Metal needs a first-class recurring-reminder primitive (a "Nudge") that can be linked to the people and initiatives already tracked in the system, so rhythms are captured, scheduled, and surfaced consistently. This closes the last Tier 3 gap before the remaining cross-cutting query surfaces (my-queue, daily-weekly-briefing) can include nudges.

## What Changes

- Introduce a `Nudge` aggregate owned by a user, with a cadence (Daily, Weekly, Biweekly, Monthly, Custom), optional day-of-week / day-of-month anchors, and a computed `NextDueDate`.
- Nudge schedule advancement (`MarkNudged(now)`) is deterministic: `now` is injected, never read from `DateTime.UtcNow` inside domain or handlers.
- Optional links to a `Person` and/or `Initiative` by ID (no cross-aggregate references).
- Pause / Resume lifecycle. Paused nudges do not advance.
- CRUD + lifecycle Minimal API endpoints under `/api/nudges`.
- Angular `/nudges` page: list with filters (active/paused, due today/this week, by person/initiative), create/edit dialog, inline "Mark as nudged", and pause/resume toggle.
- EF Core migration adding `Nudges` table.
- Vertical-slice handlers in `MentalMetal.Application/Features/Nudges/`.

## Non-goals

- `my-queue` and `daily-weekly-briefing` integration -- deferred to follow-up specs to keep scope focused. This spec only exposes the read surface needed (filters for due-today / due-this-week) so those integrations are straightforward later.
- Push/email notifications. Nudges are surfaced via the UI and future query-side specs; no external delivery channel.
- Arbitrary cron expressions. `Custom` cadence uses a fixed interval in days; richer scheduling can come later.
- Sharing nudges between users.

## Capabilities

### New Capabilities
- `nudges-rhythms`: Recurring reminder primitive (Nudge aggregate) with cadence-driven scheduling, optional person/initiative links, pause/resume, mark-nudged advancement, and a list/edit UI.

### Modified Capabilities
<!-- None. Integrations with my-queue and daily-weekly-briefing are deferred. -->

## Impact

- **Domain**: new `Nudge` aggregate in `MentalMetal.Domain/Nudges/`, value objects (`NudgeCadence`), domain events (`NudgeCreated`, `NudgeNudged`, `NudgeCadenceChanged`, `NudgePaused`, `NudgeResumed`, `NudgeUpdated`, `NudgeDeleted`). Tier: **Tier 3**. Depends on: `person-management` (shipped), `initiative-management` (shipped).
- **Application**: new `Features/Nudges/` vertical slice (Create/Get/List/Update/Delete/MarkNudged/Pause/Resume handlers).
- **Infrastructure**: `NudgeRepository`, EF `NudgeConfiguration`, new migration.
- **Web**: Minimal API endpoint group `MapNudgesEndpoints()` registered in `Program.cs`.
- **Frontend**: new `/nudges` route with list page, create/edit dialog, `NudgesService` signal store; person/initiative selectors reuse existing services.
- **Tests**: Domain unit tests (cadence advancement, pause/resume, validation), Application handler tests, Web integration tests for endpoints, Angular component tests.
- No changes to existing aggregates or other specs.
