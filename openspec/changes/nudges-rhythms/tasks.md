## 1. Domain

- [ ] 1.1 Create `src/MentalMetal.Domain/Nudges/` folder with `Nudge.cs` aggregate (Id, UserId, Title, Cadence VO, StartDate, NextDueDate, LastNudgedAt, PersonId?, InitiativeId?, Notes, IsActive, CreatedAtUtc, UpdatedAtUtc).
- [ ] 1.2 Add `NudgeCadence` value object with `CadenceType` enum (Daily, Weekly, Biweekly, Monthly, Custom) and pure `CalculateFirst(DateOnly from)` (returns next due on or after `from` -- for Create/Resume) and `CalculateNext(DateOnly after)` (returns next due strictly after `after` -- for MarkNudged) methods, including DayOfMonth clamping for short months.
- [ ] 1.3 Implement domain behaviour: `Create(userId, title, cadence, today, ...)`, `UpdateDetails`, `UpdateCadence`, `MarkNudged(now)`, `Pause()`, `Resume(today)`, with length/cadence validation and invariant checks.
- [ ] 1.4 Add domain events: `NudgeCreated`, `NudgeUpdated`, `NudgeCadenceChanged`, `NudgeNudged`, `NudgePaused`, `NudgeResumed`, `NudgeDeleted`.
- [ ] 1.5 Add `INudgeRepository` interface in Domain.
- [ ] 1.6 Add domain unit tests in `tests/MentalMetal.Domain.Tests/Nudges/` covering cadence math for all five cadences, pause/resume state transitions, validation (title length, cadence-specific required fields), and DayOfMonth=31 clamping edge case.

## 2. Application

- [ ] 2.1 Create `src/MentalMetal.Application/Features/Nudges/` vertical-slice folder.
- [ ] 2.2 Implement `CreateNudge` handler with link validation (PersonId/InitiativeId belong to user) and `TimeProvider` injection.
- [ ] 2.3 Implement `GetNudge` handler (not-found on wrong owner -- no existence leak).
- [ ] 2.4 Implement `ListNudges` handler with filters (isActive, personId, initiativeId, dueBefore, dueWithinDays) -- use `.ToLower()` and `List<T>.Contains()` patterns, no `ToLowerInvariant()` or `HashSet`.
- [ ] 2.5 Implement `UpdateNudge` handler (title/notes/links).
- [ ] 2.6 Implement `UpdateNudgeCadence` handler.
- [ ] 2.7 Implement `MarkNudgeAsNudged`, `PauseNudge`, `ResumeNudge` handlers.
- [ ] 2.8 Implement `DeleteNudge` handler.
- [ ] 2.9 Define application-layer DTOs (`NudgeResponse`, `CreateNudgeRequest`, `UpdateNudgeRequest`, `UpdateCadenceRequest`).
- [ ] 2.10 Add error codes: `nudge.notFound`, `nudge.validation`, `nudge.invalidCadence`, `nudge.linkedEntityNotFound`, `nudge.notActive`, `nudge.alreadyPaused`, `nudge.alreadyActive`.
- [ ] 2.11 Add application handler tests for success + distinct error paths.

## 3. Infrastructure

- [ ] 3.1 Add `NudgeConfiguration` EF config (table `Nudges`, Title max 200, Notes max 2000, cadence stored as owned value object, indexes on UserId + NextDueDate, IsActive).
- [ ] 3.2 Implement `NudgeRepository : INudgeRepository` with EF queries (soft user scoping, filter translation).
- [ ] 3.3 Register `INudgeRepository` in DI.
- [ ] 3.4 Generate migration: `dotnet ef migrations add AddNudges --startup-project ../MentalMetal.Web`.

## 4. Web API

- [ ] 4.1 Add `src/MentalMetal.Web/Features/Nudges/NudgesEndpoints.cs` with `MapNudgesEndpoints()` for `POST /api/nudges`, `GET /api/nudges` (list+filters), `GET /api/nudges/{id}`, `PATCH /api/nudges/{id}` (title/notes/links), `PATCH /api/nudges/{id}/cadence`, `POST /api/nudges/{id}/mark-nudged`, `POST /api/nudges/{id}/pause`, `POST /api/nudges/{id}/resume`, `DELETE /api/nudges/{id}`.
- [ ] 4.2 Register endpoints in `Program.cs`.
- [ ] 4.3 Map handler errors to ProblemDetails with distinct codes.
- [ ] 4.4 Add integration tests in `tests/MentalMetal.Web.IntegrationTests/Nudges/` covering happy paths + each distinct error code.

## 5. Frontend

- [ ] 5.1 Add `nudges` feature folder under `src/MentalMetal.Web/ClientApp/src/app/features/nudges/` (standalone components, signals).
- [ ] 5.2 Add `NudgesService` with signal-based state (list signal, filter signals, loading, error).
- [ ] 5.3 Implement `NudgesListComponent` with PrimeNG table, filter toolbar (active/paused, due today/this week, person, initiative), and action buttons (Mark as nudged, Pause/Resume, Edit, Delete). Use `@if`/`@for`, Tailwind for layout only, PrimeNG tokens for colour.
- [ ] 5.4 Implement `NudgeEditDialogComponent` using Signal Forms with conditional anchor inputs (DayOfWeek / DayOfMonth / CustomIntervalDays).
- [ ] 5.5 Register `/nudges` route and add nav link.
- [ ] 5.6 Add component tests (list filters, dialog conditional inputs, mark-nudged flow).

## 6. Verification

- [ ] 6.1 Run `dotnet test src/MentalMetal.slnx` -- all green.
- [ ] 6.2 Run `(cd src/MentalMetal.Web/ClientApp && npx ng test --watch=false)` -- all green.
- [ ] 6.3 Run `openspec validate nudges-rhythms --strict`.
- [ ] 6.4 Manually smoke-test the `/nudges` page against dev-stack: create each cadence, mark-nudged, pause/resume, delete.
