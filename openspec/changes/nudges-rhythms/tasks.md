## 1. Domain

- [x] 1.1 Create `src/MentalMetal.Domain/Nudges/` folder with `Nudge.cs` aggregate (Id, UserId, Title, Cadence VO, StartDate, NextDueDate, LastNudgedAt, PersonId?, InitiativeId?, Notes, IsActive, CreatedAtUtc, UpdatedAtUtc).
- [x] 1.2 Add `NudgeCadence` value object with `CadenceType` enum (Daily, Weekly, Biweekly, Monthly, Custom) and pure `CalculateFirst(DateOnly from)` (returns next due on or after `from` -- for Create/Resume) and `CalculateNext(DateOnly after)` (returns next due strictly after `after` -- for MarkNudged) methods, including DayOfMonth clamping for short months.
- [x] 1.3 Implement domain behaviour: `Create(userId, title, cadence, today, ...)`, `UpdateDetails`, `UpdateCadence`, `MarkNudged(now)`, `Pause()`, `Resume(today)`, with length/cadence validation and invariant checks.
- [x] 1.4 Add domain events: `NudgeCreated`, `NudgeUpdated`, `NudgeCadenceChanged`, `NudgeNudged`, `NudgePaused`, `NudgeResumed`, `NudgeDeleted`.
- [x] 1.5 Add `INudgeRepository` interface in Domain.
- [x] 1.6 Add domain unit tests in `tests/MentalMetal.Domain.Tests/Nudges/` covering cadence math for all five cadences, pause/resume state transitions, validation (title length, cadence-specific required fields), and DayOfMonth=31 clamping edge case.

## 2. Application

- [x] 2.1 Create `src/MentalMetal.Application/Features/Nudges/` vertical-slice folder.
- [x] 2.2 Implement `CreateNudge` handler with link validation (PersonId/InitiativeId belong to user) and `TimeProvider` injection.
- [x] 2.3 Implement `GetNudge` handler (not-found on wrong owner -- no existence leak).
- [x] 2.4 Implement `ListNudges` handler with filters (isActive, personId, initiativeId, dueBefore, dueWithinDays) -- use `.ToLower()` and `List<T>.Contains()` patterns, no `ToLowerInvariant()` or `HashSet`.
- [x] 2.5 Implement `UpdateNudge` handler (title/notes/links).
- [x] 2.6 Implement `UpdateNudgeCadence` handler.
- [x] 2.7 Implement `MarkNudgeAsNudged`, `PauseNudge`, `ResumeNudge` handlers.
- [x] 2.8 Implement `DeleteNudge` handler.
- [x] 2.9 Define application-layer DTOs (`NudgeResponse`, `CreateNudgeRequest`, `UpdateNudgeRequest`, `UpdateCadenceRequest`).
- [x] 2.10 Add error codes: `nudge.notFound`, `nudge.validation`, `nudge.invalidCadence`, `nudge.linkedEntityNotFound`, `nudge.notActive`, `nudge.alreadyPaused`, `nudge.alreadyActive`.
- [x] 2.11 Add application handler tests for success + distinct error paths.

## 3. Infrastructure

- [x] 3.1 Add `NudgeConfiguration` EF config (table `Nudges`, Title max 200, Notes max 2000, cadence stored as owned value object, indexes on UserId + NextDueDate, IsActive).
- [x] 3.2 Implement `NudgeRepository : INudgeRepository` with EF queries (soft user scoping, filter translation).
- [x] 3.3 Register `INudgeRepository` in DI.
- [x] 3.4 Generate migration: `dotnet ef migrations add AddNudges --startup-project ../MentalMetal.Web`.

## 4. Web API

- [x] 4.1 Add `src/MentalMetal.Web/Features/Nudges/NudgesEndpoints.cs` with `MapNudgesEndpoints()` for `POST /api/nudges`, `GET /api/nudges` (list+filters), `GET /api/nudges/{id}`, `PATCH /api/nudges/{id}` (title/notes/links), `PATCH /api/nudges/{id}/cadence`, `POST /api/nudges/{id}/mark-nudged`, `POST /api/nudges/{id}/pause`, `POST /api/nudges/{id}/resume`, `DELETE /api/nudges/{id}`.
- [x] 4.2 Register endpoints in `Program.cs`.
- [x] 4.3 Map handler errors to ProblemDetails with distinct codes.
- [x] 4.4 Add integration tests in `tests/MentalMetal.Web.IntegrationTests/Nudges/` covering happy paths + each distinct error code.

## 5. Frontend

- [x] 5.1 Add `nudges` feature folder under `src/MentalMetal.Web/ClientApp/src/app/features/nudges/` (standalone components, signals).
- [x] 5.2 Add `NudgesService` with signal-based state (list signal, filter signals, loading, error).
- [x] 5.3 Implement `NudgesListComponent` with PrimeNG table, filter toolbar (active/paused, due today/this week, person, initiative), and action buttons (Mark as nudged, Pause/Resume, Edit, Delete). Use `@if`/`@for`, Tailwind for layout only, PrimeNG tokens for colour.
- [x] 5.4 Implement `NudgeEditDialogComponent` using Signal Forms with conditional anchor inputs (DayOfWeek / DayOfMonth / CustomIntervalDays).
- [x] 5.5 Register `/nudges` route and add nav link.
- [x] 5.6 Add component tests (list filters, dialog conditional inputs, mark-nudged flow).

## 6. Verification

- [x] 6.1 Run `dotnet test src/MentalMetal.slnx` -- all green.
- [x] 6.2 Run `(cd src/MentalMetal.Web/ClientApp && npx ng test --watch=false)` -- all green.
- [x] 6.3 Run `openspec validate nudges-rhythms --strict`.
- [x] 6.4 Manually smoke-test the `/nudges` page against dev-stack: create each cadence, mark-nudged, pause/resume, delete.
