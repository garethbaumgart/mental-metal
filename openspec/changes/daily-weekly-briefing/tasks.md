## 1. Domain

- [ ] 1.1 Create `src/MentalMetal.Domain/Briefings/BriefingType.cs` (enum: `Morning`, `Weekly`, `OneOnOnePrep`).
- [ ] 1.2 Create `src/MentalMetal.Domain/Briefings/Briefing.cs` — aggregate with `Id`, `UserId`, `Type`, `ScopeKey`, `GeneratedAtUtc`, `MarkdownBody`, `PromptFactsJson`, `Model`, `InputTokens`, `OutputTokens`; `Create(...)` factory enforcing non-null body + non-empty scopeKey.
- [ ] 1.3 Create `src/MentalMetal.Domain/Briefings/IBriefingRepository.cs` with `AddAsync`, `GetByIdAsync(userId,id)`, `GetLatestAsync(userId,type,scopeKey)`, `ListRecentAsync(userId,type?,limit)`.
- [ ] 1.4 Domain unit tests: `tests/MentalMetal.Domain.Tests/Briefings/BriefingTests.cs` — factory enforces invariants; scopeKey empty throws.

## 2. Application

- [ ] 2.1 Create `src/MentalMetal.Application/Briefings/BriefingOptions.cs` with `[Range]` attrs (`MorningBriefingHour` 0..23 default 5; `WeeklyBriefingStaleHours` 1..72 default 12; `OneOnOnePrepStaleHours` 1..72 default 12; `MaxBriefingTokens` 200..4000 default 1500; `TopItemsPerSection` 1..20 default 5).
- [ ] 2.2 Create `src/MentalMetal.Application/Briefings/Facts/*Facts.cs` record types: `MorningBriefingFacts`, `WeeklyBriefingFacts`, `OneOnOnePrepFacts`, and their nested item records (e.g., `FactCommitment`, `FactDelegation`, `FactPerson`, `FactOneOnOne`, `FactMilestone`, `FactObservation`, `FactGoal`, `FactCapture`).
- [ ] 2.3 Create `src/MentalMetal.Application/Briefings/BriefingFactsAssembler.cs` — injects the required repos + `TimeProvider` + options + `IUserContext`; exposes `BuildMorningAsync`, `BuildWeeklyAsync`, `BuildOneOnOnePrepAsync(personId)`. Use `userLocalDate(now)` helper; apply `MorningBriefingHour` rollback rule. Uses `.ToLower()` (not `ToLowerInvariant`) and `List<T>.Contains()` if filtering with collections.
- [ ] 2.4 Create `src/MentalMetal.Application/Briefings/BriefingPromptBuilder.cs` — produces `(systemPrompt, userPrompt)` per briefing type from the facts JSON; prompt forbids invention; 1:1-prep requests 3–5 talking points.
- [ ] 2.5 Create `src/MentalMetal.Application/Briefings/BriefingService.cs` — orchestrates assemble → decide cache vs generate → serialize facts → call `IAiCompletionService` → persist → return; `TimeProvider` injected; respects `WeeklyBriefingStaleHours`/`OneOnOnePrepStaleHours`.
- [ ] 2.6 Create `src/MentalMetal.Application/Briefings/GenerateMorningBriefing.cs`, `GenerateWeeklyBriefing.cs`, `GenerateOneOnOnePrep.cs` — CQRS commands with handlers delegating to `BriefingService`; return DTOs + a `WasCached` bool so the endpoint picks 200 vs 201.
- [ ] 2.7 Create `src/MentalMetal.Application/Briefings/GetRecentBriefings.cs` — query handler; validates `type`/`limit`.
- [ ] 2.8 Create `src/MentalMetal.Application/Briefings/GetBriefing.cs` — query handler returning full briefing; enforces user scoping.
- [ ] 2.9 Create `src/MentalMetal.Application/Briefings/BriefingDtos.cs` — `BriefingResponse`, `BriefingSummary`.
- [ ] 2.10 Register options + services in `src/MentalMetal.Application/DependencyInjection.cs` with `AddOptions<BriefingOptions>().Bind(...).ValidateDataAnnotations().ValidateOnStart()`.
- [ ] 2.11 Application unit tests: `tests/MentalMetal.Application.Tests/Briefings/` — `BriefingFactsAssemblerTests` (deterministic output for fixed `FakeTimeProvider` + repo fakes), `BriefingServiceTests` (cache hit returns existing, force bypasses cache, 409 without AI config), `GetRecentBriefingsTests` (filter + limit validation).

## 3. Infrastructure

- [ ] 3.1 Create `src/MentalMetal.Infrastructure/Persistence/Configurations/BriefingConfiguration.cs` — map columns, jsonb for `PromptFactsJson`, unique/composite index `(UserId, Type, ScopeKey, GeneratedAtUtc DESC)`.
- [ ] 3.2 Add `DbSet<Briefing> Briefings` to `MentalMetalDbContext`.
- [ ] 3.3 Create `src/MentalMetal.Infrastructure/Repositories/BriefingRepository.cs` implementing `IBriefingRepository`; uses `.ToLower()`, `List<T>.Contains()` where applicable.
- [ ] 3.4 Register `IBriefingRepository → BriefingRepository` in `Infrastructure/DependencyInjection.cs`.
- [ ] 3.5 Add EF migration `AddBriefings` (run `dotnet build` first, then `dotnet ef migrations add AddBriefings --no-build`).
- [ ] 3.6 Integration test `tests/MentalMetal.Web.IntegrationTests/Briefings/BriefingRepositoryTests.cs` — AddAsync + GetLatestAsync + user isolation.

## 4. Web (API)

- [ ] 4.1 Create `src/MentalMetal.Web/Features/Briefings/BriefingEndpoints.cs` with Minimal API routes:
  - `POST /api/briefings/morning` (force?)
  - `POST /api/briefings/weekly` (force?)
  - `POST /api/briefings/one-on-one/{personId}` (force?)
  - `GET /api/briefings/recent` (type?, limit?)
  - `GET /api/briefings/{id}`
- [ ] 4.2 Register endpoints in `Program.cs` via `app.MapBriefingEndpoints()`.
- [ ] 4.3 Handle `ai_provider_not_configured` via a typed exception or result — return 409 JSON error.
- [ ] 4.4 Integration tests `tests/MentalMetal.Web.IntegrationTests/Briefings/BriefingEndpointsTests.cs` — morning happy path 201, second call 200, force regenerates, 401 unauth, 409 no AI config, 404 person not owned, 400 bad type, 400 bad limit, user isolation on GET.

## 5. Frontend — core

- [ ] 5.1 Create `src/MentalMetal.Web/ClientApp/src/app/shared/services/briefing.service.ts` — signal-based service; methods `loadMorning(force?)`, `loadWeekly(force?)`, `loadOneOnOnePrep(personId, force?)`, `recent(type?, limit?)`, `getById(id)`; expose signals for latest morning/weekly briefings.
- [ ] 5.2 Create `src/MentalMetal.Web/ClientApp/src/app/shared/models/briefing.model.ts` — matching DTO types.
- [ ] 5.3 If a markdown pipe doesn't already exist, add `src/MentalMetal.Web/ClientApp/src/app/shared/pipes/markdown.pipe.ts` using `marked` + `DOMPurify`; add to `package.json`.

## 6. Frontend — morning widget

- [ ] 6.1 Create `src/MentalMetal.Web/ClientApp/src/app/pages/home/morning-briefing-widget.component.ts` — standalone signal component using `@if`/`@for`, PrimeNG Card + Button, `tailwindcss-primeui` tokens only.
- [ ] 6.2 Wire widget into the dashboard home page.
- [ ] 6.3 Component test: loading → success renders markdown; regenerate calls `force=true`; 409 renders empty-state with settings link.

## 7. Frontend — weekly page

- [ ] 7.1 Create `src/MentalMetal.Web/ClientApp/src/app/pages/briefings/weekly-briefing.page.ts` + route registration at `/briefings/weekly`.
- [ ] 7.2 Component test: happy path + regenerate + 409 empty state.

## 8. Frontend — 1:1 prep action

- [ ] 8.1 Add "Generate 1:1 prep" PrimeNG Button to person detail page; opens PrimeNG Dialog with `OneOnOnePrepDialogComponent`.
- [ ] 8.2 Create `src/MentalMetal.Web/ClientApp/src/app/pages/people/one-on-one-prep-dialog.component.ts` — calls `loadOneOnOnePrep(personId)`; shows markdown + generated-at + regenerate button.
- [ ] 8.3 Component test: dialog opens and renders; regenerate re-calls with `force=true`; 409 state.

## 9. E2E

- [ ] 9.1 Add Playwright scenario in `tests/MentalMetal.E2E.Tests/` covering: dashboard widget renders after login, weekly page renders after clicking "Generate".

## 10. Docs & polish

- [ ] 10.1 Add `Briefing` configuration section stub to `appsettings.json` with defaults + a comment.
- [ ] 10.2 Verify all backend tests pass (`dotnet test src/MentalMetal.slnx`).
- [ ] 10.3 Verify frontend tests pass (`(cd src/MentalMetal.Web/ClientApp && npx ng test --watch=false)`).
- [ ] 10.4 Run `openspec validate daily-weekly-briefing --strict` before opening the Apply PR.
