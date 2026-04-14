## Context

Mental Metal tracks people and initiatives as rich aggregates, and has commitments/delegations for things with explicit completion. What's missing is a lightweight *recurring-rhythm* primitive: "remind me about X every Thursday" or "monthly career check-in with Sarah". These are not tasks -- they repeat forever (until paused or deleted) and exist to surface prompts at the right cadence. The domain model (design/domain-model.md §11) defines the `Nudge` aggregate; this spec lands a focused, deterministic implementation of it.

## Dependencies

- `person-management` (shipped) -- for optional `PersonId` link.
- `initiative-management` (shipped) -- for optional `InitiativeId` link.
- `user-auth-tenancy` (shipped) -- for UserId scoping.

## Goals / Non-Goals

**Goals:**
- Ship a deterministic, testable `Nudge` aggregate with cadence-driven schedule advancement.
- Provide CRUD + lifecycle (pause/resume, mark-nudged) endpoints under `/api/nudges`.
- Provide an Angular `/nudges` page with list/filter/create/edit/mark-nudged/pause-resume.
- Keep the schedule arithmetic in the domain and pure (inject `now`).

**Non-Goals:**
- Integrating nudges into `my-queue` or `daily-weekly-briefing` -- deferred to follow-up specs.
- Push/email/SMS notifications.
- Full cron expressions. `Custom` cadence uses `CustomIntervalDays`.
- Cross-user sharing.

## Decisions

### D1: `NudgeCadence` as a value object with `CalculateNext(from, anchors)`

A `NudgeCadence` record encapsulates `(Type, CustomIntervalDays?, DayOfWeek?, DayOfMonth?)` and exposes a single pure method `CalculateNext(DateOnly from)` returning the next due `DateOnly` on or after `from`. The `Nudge` aggregate calls this in `Create` and `MarkNudged` to compute `NextDueDate`.

**Alternatives considered:**
- A service `INudgeScheduler`. Rejected -- pure arithmetic belongs on the value object; a service adds indirection with no benefit.
- Storing just a cron string. Rejected -- overkill for the four fixed cadences + a simple interval.

### D2: Determinism -- inject `now` everywhere

`MarkNudged(DateOnly now)`, `Resume(DateOnly now)`, and `Create(..., DateOnly today)` all take the reference date as a parameter. Handlers receive `TimeProvider` via DI and pass `TimeProvider.GetUtcNow().UtcDateTime` converted to a `DateOnly`. Matches the pattern already used by `commitment-tracking`. Tests construct nudges with fixed `DateOnly` values.

### D3: `NextDueDate` is a persisted field, not computed

Cadence math is cheap, but persisting `NextDueDate` makes filters (`dueBefore`, `dueToday`, `dueThisWeek`) efficient SQL. It's recomputed on create, cadence change, resume, and mark-nudged. Invariant: when `IsActive` is true, `NextDueDate` is non-null.

### D4: Pause/Resume semantics

`Pause()` sets `IsActive=false` and leaves `NextDueDate` untouched (so it's hidden from due-queries but we remember the schedule). `Resume(now)` reactivates and sets `NextDueDate = Cadence.CalculateNext(now)` -- the user re-anchors from today, not the stale value.

### D5: Distinct error codes for distinct failure reasons

- `nudge.notFound` -- nudge does not exist OR belongs to another user (we do not leak existence).
- `nudge.invalidCadence` -- e.g., Custom without positive `CustomIntervalDays`, or Weekly without `DayOfWeek`.
- `nudge.alreadyPaused` / `nudge.alreadyActive` -- state-transition conflicts on pause/resume.
- `nudge.validation` -- title/notes length.

### D6: Title and Notes length caps in the domain

`Title`: required, 1-200 characters. `Notes`: optional, up to 2000 characters. Enforced in `Create`/`UpdateDetails` with explicit `ArgumentException` (matching EF `HasMaxLength` config). Don't rely on DB rejection.

### D7: Linked aggregates by ID only; foreign keys are soft

`PersonId` and `InitiativeId` are `Guid?`. The handler validates that referenced Person/Initiative belongs to the same user before accepting the link (returns `nudge.linkedEntityNotFound`). We do NOT add a hard EF FK -- avoids cross-aggregate join pulls and keeps `Nudge` independent.

### D8: `DayOfMonth` clamping for Monthly

If user picks `DayOfMonth=31` and the next month has 30 days, clamp to month-end. Documented in value-object tests.

### D9: Frontend uses signals + PrimeNG; Signal Forms for the dialog

Following CLAUDE.md: standalone components, `inject()`, signals, PrimeNG tokens. Control flow via `@if`/`@for`. The create/edit dialog uses Signal Forms (Angular 21). Person and Initiative selectors reuse existing `PeopleService` and `InitiativesService`.

## Risks / Trade-offs

- **Clock skew across timezones** → We operate in `DateOnly` anchored to UTC. Acceptable for v1 since briefings are also UTC-anchored. Future work: per-user timezone.
- **`NextDueDate` drift if a user never "marks nudged"** → Intentional: the user sees a growing overdue badge. No auto-advance.
- **Custom cadence abuse (`CustomIntervalDays=1` ≡ Daily)** → Allowed; harmless. Validation enforces a minimum of 1 and a reasonable cap of 365.
- **Link validation is an extra round-trip** → Acceptable; same pattern as `commitment-tracking` uses for PersonId.

## Migration Plan

1. Add `Nudge` aggregate, value object, events, repository interface in Domain.
2. Add EF configuration + repository in Infrastructure.
3. `dotnet ef migrations add AddNudges` generating `Nudges` table.
4. Ship vertical-slice handlers + Minimal API endpoints.
5. Ship Angular `/nudges` route.
6. No data backfill required (new table).

Rollback: revert the migration (`dotnet ef migrations remove`) on the feature branch only. Once merged, any correction ships as a new migration.

## Open Questions

None -- the integration questions (my-queue, briefing) are deliberately deferred to follow-up specs.
