## Context

Mental Metal currently models hiring only as a coarse `PipelineStatus` value object on the candidate `Person` (`New → Screening → Interviewing → OfferStage → Hired / Rejected / Withdrawn`). There is no notion of an individual interview round, no place to record scorecards, and no way to paste a transcript for AI-driven analysis. Engineering managers running multiple candidates in parallel need round-level tracking and a quick-read summary of a just-completed loop. The `daily-weekly-briefing` spec (shipped) already established the pattern for wrapping `IAiCompletionService` with a deterministic-facts-in / narration-out service, and `people-lens` established the pattern for aggregates with owned entity collections (scorecards are the counterpart to one-on-one action items). This design applies both patterns to a new `Interview` aggregate.

This is Tier 3 of `design/spec-plan.md`. Dependencies (`person-management`, `ai-provider-abstraction`) are shipped and archived.

## Goals / Non-Goals

**Goals:**
- Introduce `Interview` as a standalone DDD aggregate rooted in its own lifecycle, reachable from a candidate `Person` by ID only (no direct containment).
- Support a structured, guarded stage lifecycle with explicit transitions and terminal states; raise domain events for every transition.
- Model scorecards as an owned entity collection with add / update / remove semantics and per-entry timestamps.
- Model the transcript as an owned value object holding pasted text plus AI-generated summary / recommendation / risk signals; allow transcript replacement and analysis regeneration without losing the interview row.
- Expose a minimal API surface under `/api/interviews` scoped strictly to the authenticated user.
- Provide an Angular pipeline view grouped by stage (kanban-style), a detail page with tabs (overview, scorecards, transcript, AI analysis), and consistent PrimeNG + `tailwindcss-primeui` theming — no hardcoded colours.
- Reuse `IAiCompletionService` behind a new `IInterviewAnalysisService`, with deterministic prompt construction and `IOptions<InterviewAnalysisOptions>` (validated via `ValidateDataAnnotations` / `ValidateOnStart`).

**Non-Goals:**
- Calendar / scheduling integration (no iCal, Google Calendar, etc.).
- Multi-interviewer collaboration — single-user app; `RecordedAtUtc` is the only audit metadata on scorecards.
- Audio capture or live transcription — that is `capture-audio`'s scope. Transcript must be pasted text.
- Automatic writes back to `Person.CandidateDetails.PipelineStatus`. The two remain independently editable.
- Multi-round aggregation analytics (e.g., "show me all onsite loops this quarter with decision split").
- Email / Slack notifications when stages change.

## Decisions

### D1. `Interview` is its own aggregate rooted in `InterviewId`, referencing `Person` by `CandidatePersonId`
**Why:** Interviews have their own lifecycle, invariants (stage transitions), and child collections (scorecards). Embedding interviews inside `Person` would bloat the `Person` aggregate, break the "aggregates reference by ID only" principle, and prevent loading interviews independently. Alternative considered: make interviews an owned collection of `Person.CandidateDetails` — rejected because that forces loading all interviews every time a Person is loaded and makes commands cross-aggregate.

### D2. `InterviewStage` enum with a transition map guarded on the aggregate
**Why:** Mirrors the `CandidateDetails.ValidateTransition` pattern already in the codebase (see `src/MentalMetal.Domain/People/CandidateDetails.cs`). Forward transitions: `Applied → ScreenScheduled → ScreenCompleted → OnsiteScheduled → OnsiteCompleted → OfferExtended → Hired`. Terminal: `Hired`, `Rejected`, `Withdrawn`. `Rejected` and `Withdrawn` reachable from any non-terminal state. Invalid transitions throw a `DomainException`; endpoint maps to HTTP 409. Alternative considered: free-form string stage — rejected for loss of type safety.

### D3. `InterviewScorecard` as an owned entity with a stable `ScorecardId`
**Why:** Scorecards need to be individually updatable and removable, and we want their IDs in the URL (`/api/interviews/{id}/scorecards/{scorecardId}`). A value-object scorecard collection (equality by content) would break update semantics. Consistent with `OneOnOne.ActionItem` from people-lens.

### D4. `InterviewTranscript` as an owned value object (single-per-interview, replaceable)
**Why:** There is only ever one current transcript per interview round. Making it a value object expresses that — `SetTranscript(text)` replaces it atomically with a fresh analysis payload. The AI analysis fields (`Summary`, `RecommendedDecision`, `RiskSignals`) live on the transcript VO so clearing a transcript also clears stale analysis. Alternative considered: storing transcript text and analysis as separate scalar columns on `Interview` — rejected because it spreads what is conceptually a single cohesive artefact.

### D5. AI analysis is an explicit endpoint (`POST /api/interviews/{id}/analyze`), not automatic on transcript upload
**Why:** Follows the `daily-weekly-briefing` pattern where AI generation is explicit and cacheable. Users may paste a transcript, edit it, and only when ready trigger analysis — saves tokens and avoids surprise costs. Transcript storage and analysis are decoupled.

### D6. Deterministic analysis service with `Temperature = 0.3`, strict system prompt
**Why:** Same pattern as `BriefingService`. The prompt instructs the model to narrate only from supplied facts (scorecards + transcript), forbids invention of names/dates, requires one of the five `InterviewDecision` enum values as `RecommendedDecision`, and caps `MaxTokens` via `InterviewAnalysisOptions.MaxAnalysisTokens`. Input is JSON-serialised facts — user-supplied transcript text SHALL have backtick characters escaped (`\u0060`) before being embedded into the user prompt, mirroring the prior-CodeRabbit-flagged prompt-injection mitigation.

### D7. Clock injection
**Why:** Services SHALL accept an `ISystemClock` (or `TimeProvider` — project already uses one) and MUST NOT read `DateTime.UtcNow` directly. Avoids the determinism bug flagged on prior AI PRs.

### D8. EF Core persistence
- `Interviews` table: `Id`, `UserId` (indexed), `CandidatePersonId` (indexed), `RoleTitle`, `Stage`, `ScheduledAtUtc`, `CompletedAtUtc`, `Decision`, `CreatedAtUtc`, `UpdatedAtUtc`, plus owned-type columns `Transcript_RawText`, `Transcript_Summary`, `Transcript_RecommendedDecision`, `Transcript_RiskSignals` (jsonb list), `Transcript_AnalyzedAtUtc`, `Transcript_Model`.
- `InterviewScorecards` table: `Id`, `InterviewId` (FK, cascade), `Competency`, `Rating`, `Notes`, `RecordedAtUtc`.
- Owned collection (`_scorecards`) appends will be persisted via `MarkOwnedAdded` / `MarkOwnedRemoved` repo helpers to work around the EF Core snapshot tracker bug documented in `project_tier2b_plan.md` memory.

### D9. API conventions
- All endpoints under `/api/interviews` require `RequireAuthorization()` and resolve the current `UserId` from the authenticated principal (consistent with existing Minimal API slices).
- DTOs live in `MentalMetal.Application/Interviews/` — never expose domain entities.
- Error codes: `candidate_not_found`, `invalid_stage_transition`, `decision_not_allowed`, `transcript_missing`, `ai_provider_not_configured`, `analysis_failed`.

### D10. Frontend
- New standalone feature `src/app/pages/interviews/`:
  - `interviews-pipeline.component.ts` — route `/interviews`, pipeline columns grouped by stage using PrimeNG Card + `@for` control flow.
  - `interview-detail.component.ts` — route `/interviews/:id`, PrimeNG TabView with Overview / Scorecards / Transcript / AI Analysis tabs.
  - Signal-based state, `inject()` DI, Signal Forms for create / edit, no `*ngIf` / `*ngFor`.
- Colours via PrimeNG tokens / `tailwindcss-primeui` (`bg-primary`, `text-muted-color`, `var(--p-surface-100)`) — no hardcoded Tailwind palette classes, no `dark:` prefix.

## Risks / Trade-offs

- **Transcript size** → Paste transcripts can be long; DB column is `text` (no fixed limit) but prompt length is bounded by `InterviewAnalysisOptions.MaxPromptChars` (default 16000). Over-length transcripts are rejected with HTTP 413.
- **Prompt injection via pasted transcript** → Mitigated by escaping backticks and using JSON serialisation for the facts block; system prompt explicitly instructs the model to treat transcript content as data, not instructions.
- **Cost runaway on re-analysis** → Mitigated by requiring an explicit `POST /analyze` call; no auto-regeneration on transcript edits.
- **Stage-enum drift vs. `PipelineStatus`** → Two separate enums deliberately; the `Interview` stage is round-level, `PipelineStatus` is candidate-level. Documented in the proposal as a non-goal.
- **EF Core owned-collection tracker bug** → Mitigated by applying the `MarkOwnedAdded/Removed` repository helper pattern already proven in `people-lens` and `capture-ai-extraction`.
- **Model-returned decision outside the enum** → AI response parser validates `RecommendedDecision` against the enum; invalid values are stored as null and surfaced as a warning in the response.

## Migration Plan

1. Ship spec + design + tasks PR (Stage 1).
2. Implement domain, infrastructure, application, web, and frontend changes in a single apply PR (Stage 2). Includes a new EF Core migration `AddInterviews`.
3. Archive PR (Stage 3) sync delta spec into `openspec/specs/interview-tracking/spec.md`.
4. Rollback strategy: the migration is purely additive (two new tables, no changes to existing tables), so `dotnet ef migrations remove` locally on a pre-merge branch is safe. Post-merge rollback requires a new "down" migration dropping the tables — no data loss risk for other aggregates.

## Open Questions

None blocking. Rating scale (1–5 integer) chosen to match the `MoodRating` field already in `people-lens`; if product direction shifts to H/M/L later, that is a non-breaking enum migration.

## Dependencies

- `person-management` (shipped): validates `CandidatePersonId` exists, belongs to the user, and has `Type` including `Candidate`.
- `ai-provider-abstraction` (shipped): provides `IAiCompletionService` and `AiProviderConfig`. If the user has no configured provider, the analyze endpoint returns HTTP 409 with code `ai_provider_not_configured`, matching `daily-weekly-briefing`.
