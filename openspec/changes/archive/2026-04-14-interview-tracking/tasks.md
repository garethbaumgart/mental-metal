## 1. Domain

- [ ] 1.1 Add `InterviewStage` enum in `src/MentalMetal.Domain/Interviews/InterviewStage.cs` with values `Applied, ScreenScheduled, ScreenCompleted, OnsiteScheduled, OnsiteCompleted, OfferExtended, Hired, Rejected, Withdrawn`.
- [ ] 1.2 Add `InterviewDecision` enum in `src/MentalMetal.Domain/Interviews/InterviewDecision.cs` with values `StrongHire, Hire, LeanHire, NoHire, StrongNoHire`.
- [ ] 1.3 Implement `InterviewScorecard` owned entity (Id, Competency, Rating, Notes, RecordedAtUtc) with `Update(competency, rating, notes, recordedAtUtc)` method validating rating 1–5 and non-empty competency.
- [ ] 1.4 Implement `InterviewTranscript` owned value object (RawText, Summary, RecommendedDecision, RiskSignals, AnalyzedAtUtc, Model) with `WithAnalysis(...)` and `Clear()` helpers.
- [ ] 1.5 Implement `Interview` aggregate root (Id, UserId, CandidatePersonId, RoleTitle, Stage, ScheduledAtUtc, CompletedAtUtc, Decision, Transcript, _scorecards) with factory `Create`, `UpdateMetadata`, `AdvanceStage(targetStage, now)`, `RecordDecision(decision)`, scorecard add/update/remove, and `SetTranscript(rawText)` (clears analysis) and `ApplyAnalysis(summary, recommendedDecision, riskSignals, model, now)` methods. Use a forward-transition map + terminal-state set; throw `DomainException` on invalid transitions or decisions-before-completion.
- [ ] 1.6 Add domain events: `InterviewCreated`, `InterviewUpdated`, `InterviewStageChanged`, `InterviewDecisionRecorded`, `InterviewScorecardAdded/Updated/Removed`, `InterviewTranscriptSet`, `InterviewAnalysisGenerated`, `InterviewDeleted`. Raise each from the aggregate at the right point.
- [ ] 1.7 Add `IInterviewRepository` interface in Domain (get-by-id, list-for-user with filters, add, update, delete; MarkScorecardAdded/Removed helpers per the EF tracker workaround).
- [ ] 1.8 Write unit tests covering: valid stage transitions, rejected invalid transitions, terminal-state guard, `CompletedAtUtc` set on Completed / Hired / Rejected, decision-before-completion guard, scorecard rating-range validation, transcript replacement clears analysis, backtick-bearing transcript is accepted as-is at the domain layer (escaping is a service concern).

## 2. Application

- [ ] 2.1 Create vertical slice folder `src/MentalMetal.Application/Interviews/` with DTOs (`InterviewResponse`, `InterviewScorecardResponse`, `InterviewTranscriptResponse`, `InterviewAnalysisResponse`) — never expose domain entities.
- [ ] 2.2 Add handlers: `CreateInterview`, `UpdateInterview`, `AdvanceInterviewStage`, `RecordInterviewDecision`, `DeleteInterview`, `GetUserInterviews`, `GetInterviewById`.
- [ ] 2.3 Add handlers: `AddScorecard`, `UpdateScorecard`, `RemoveScorecard`.
- [ ] 2.4 Add handlers: `SetInterviewTranscript`, `AnalyzeInterview`.
- [ ] 2.5 Add `IInterviewAnalysisService` + `InterviewAnalysisService` implementation in `src/MentalMetal.Application/Interviews/` that wraps `IAiCompletionService`. System prompt forbids invention; user prompt serialises facts JSON; escape backticks in transcript via `Replace("\u0060", "\\u0060")`; temperature 0.3; MaxTokens from options. Inject `TimeProvider`.
- [ ] 2.6 Add `InterviewAnalysisOptions` with `[Range]` validated `MaxAnalysisTokens` (default 800) and `MaxPromptChars` (default 16000); register via `.AddOptions<InterviewAnalysisOptions>().BindConfiguration("InterviewAnalysis").ValidateDataAnnotations().ValidateOnStart()`.
- [ ] 2.7 Wire services in `DependencyInjection.cs` / `ApplicationServiceCollectionExtensions.cs`.
- [ ] 2.8 Application unit tests for: handler candidate-not-found path, user-isolation filters, analyze service prompt construction (asserts backtick escaping and facts shape), decision parsing when AI returns invalid enum value (stores null + warning).

## 3. Infrastructure

- [ ] 3.1 Add EF Core configuration `InterviewConfiguration` mapping `Interviews` table, indexes on `UserId` and `CandidatePersonId`, owned `InterviewTranscript` columns (`Transcript_RawText`, `Transcript_Summary`, `Transcript_RecommendedDecision`, `Transcript_RiskSignals` as jsonb, `Transcript_AnalyzedAtUtc`, `Transcript_Model`), and owned collection `InterviewScorecards` with cascade delete.
- [ ] 3.2 Implement `InterviewRepository` using the `MarkOwnedAdded/Removed` pattern proven in `OneOnOneRepository`. Use `List<T>.Contains()` and `.ToLower()` in any LINQ filters.
- [ ] 3.3 Register `IInterviewRepository` and DbSet in the DI container and `MentalMetalDbContext`.
- [ ] 3.4 Generate EF Core migration: `dotnet ef migrations add AddInterviews --startup-project ../MentalMetal.Web` (after clean `dotnet build` to avoid sha512 issues; use `--no-build` if needed).
- [ ] 3.5 Verify migration SQL creates two tables and all expected columns/indexes/FKs.

## 4. Web / API

- [ ] 4.1 Add `InterviewEndpoints.cs` in `src/MentalMetal.Web/Features/Interviews/` mapping all endpoints listed in the spec with `RequireAuthorization()` and resolving `UserId` from the authenticated principal.
- [ ] 4.2 Map error codes → HTTP status codes: `candidate_not_found` → 404, `invalid_stage_transition` → 409, `decision_not_allowed` → 409, `transcript_missing` → 409, `ai_provider_not_configured` → 409, `analysis_failed` → 502, transcript too long → 413.
- [ ] 4.3 Wire `app.MapInterviewEndpoints()` into `Program.cs`.
- [ ] 4.4 Web integration tests (using `WebApplicationFactory`): create → advance → record decision happy path; invalid transition 409; user isolation 404; analyze without provider 409; analyze without transcript 409.

## 5. Frontend

- [ ] 5.1 Add `src/app/shared/models/interview.model.ts` with `Interview`, `InterviewStage`, `InterviewDecision`, `InterviewScorecard`, `InterviewTranscript`, and request/response DTOs.
- [ ] 5.2 Add `InterviewsService` in `src/app/shared/services/interviews.service.ts` with signals-based state, `inject(HttpClient)`, and methods for list/get/create/update/advance/decision/delete and scorecards/transcript/analyze.
- [ ] 5.3 Create standalone route `/interviews` with `InterviewsPipelineComponent` using PrimeNG Card + Tag + `@for`/`@if` control flow, grouping Interviews by stage. Layout via Tailwind grid/flex utilities only; colours via PrimeNG tokens (`bg-surface-50`, `text-muted-color`, `bg-primary`). No `*ngIf`/`*ngFor`, no hardcoded palette classes, no `dark:` prefix.
- [ ] 5.4 Create `InterviewCreateDialogComponent` (Signal Forms) invoked from pipeline "New interview" action.
- [ ] 5.5 Create `/interviews/:id` `InterviewDetailComponent` with PrimeNG TabView: Overview, Scorecards, Transcript, AI Analysis — each a child signal-based component using Signal Forms where needed.
- [ ] 5.6 Add routes to `app.routes.ts` and a nav entry to the main shell.
- [ ] 5.7 Jest/Karma unit tests for `InterviewsService` and the pipeline component (render/empty-state/filter).

## 6. E2E

- [ ] 6.1 Add Playwright spec in `tests/MentalMetal.E2E.Tests/` that: logs in, creates a candidate Person, navigates to `/interviews`, creates an Interview, advances to `ScreenScheduled`, adds a scorecard, sets a transcript, and asserts the expected UI updates.

## 7. Tests & polish

- [ ] 7.1 Run `dotnet test src/MentalMetal.slnx` — fix any failures.
- [ ] 7.2 Run `(cd src/MentalMetal.Web/ClientApp && npx ng test --watch=false)` — fix any failures.
- [ ] 7.3 Verify no new hardcoded Tailwind colour classes (`bg-gray-*`, `text-violet-*`, `dark:*`) nor `*ngIf`/`*ngFor` introduced via grep; lint should be clean.
- [ ] 7.4 Manual smoke test via dev stack: `docker compose --profile dev-stack up -d --wait` and exercise the `/interviews` flow end-to-end.
