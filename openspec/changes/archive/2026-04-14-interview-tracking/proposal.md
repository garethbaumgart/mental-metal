## Why

Engineering managers interviewing candidates currently have no structured way in Mental Metal to track a candidate's hiring pipeline beyond a single `PipelineStatus` on the `Person` aggregate. Every interview round, scorecard, transcript, and hiring signal either lives elsewhere (Notion, docs) or is lost. This spec closes that gap by introducing a first-class `Interview` aggregate that records per-round state, structured scorecards, and (optionally) pasted transcripts, then uses the existing AI provider abstraction to produce a summary, a recommended decision, and risk signals — the same pattern the `daily-weekly-briefing` spec established. This is Tier 3 of the spec plan (`design/spec-plan.md`) and depends only on `person-management` and `ai-provider-abstraction`, both shipped.

## What Changes

- Introduce a new `Interview` aggregate (new, no existing code) scoped to `UserId` and linked to a candidate `Person` via `CandidatePersonId`.
- Add an `InterviewStage` lifecycle (`Applied`, `ScreenScheduled`, `ScreenCompleted`, `OnsiteScheduled`, `OnsiteCompleted`, `OfferExtended`, `Hired`, `Rejected`, `Withdrawn`) with guarded transitions raising domain events on each change.
- Add an optional `InterviewDecision` (`StrongHire`, `Hire`, `LeanHire`, `NoHire`, `StrongNoHire`) recorded when the interview loop concludes.
- Add an owned `InterviewScorecard` entity collection (competency, rating 1–5, notes, `RecordedAtUtc`) with add / update / remove operations.
- Add an owned `InterviewTranscript` value object (single pasted transcript per interview, plus AI-generated `Summary`, `RecommendedDecision`, `RiskSignals`). Transcript can be replaced; analysis can be regenerated.
- Add backend minimal API endpoints:
  - `POST /api/interviews`, `GET /api/interviews` (filter by `candidatePersonId`, `stage`), `GET /api/interviews/{id}`, `PATCH /api/interviews/{id}` (role title, scheduled date), `POST /api/interviews/{id}/advance` (stage transitions), `POST /api/interviews/{id}/decision`, `DELETE /api/interviews/{id}`
  - `POST|PUT|DELETE /api/interviews/{id}/scorecards[/{scorecardId}]`
  - `PUT /api/interviews/{id}/transcript`, `POST /api/interviews/{id}/analyze` (AI)
- Add an `IInterviewAnalysisService` that wraps `IAiCompletionService` with a deterministic prompt forbidding invention (same pattern as `BriefingService`), temperature `0.3`, and bounded `MaxTokens` via `InterviewAnalysisOptions`.
- Add frontend Angular routes: `/interviews` (pipeline kanban / list view grouped by stage), `/interviews/{id}` detail page with scorecards tab, transcript tab, AI-analysis tab; use signals, `@if`/`@for`, PrimeNG + `tailwindcss-primeui` tokens only.
- Add EF Core migration creating `Interviews` and `InterviewScorecards` tables with owned `InterviewTranscript`.

## Capabilities

### New Capabilities
- `interview-tracking`: candidate hiring-loop aggregate with staged pipeline, scorecards, transcript capture, and AI-powered summary / recommendation / risk-signal generation. Scoped to the authenticated user.

### Modified Capabilities
<!-- None. Candidate PipelineStatus on Person remains the coarse-grained signal; Interview rounds are additive and do not change person-management requirements. -->

## Impact

- **Affected code**: new `MentalMetal.Domain/Interviews/`, new vertical slices under `MentalMetal.Application/Interviews/`, new `InterviewsEndpoints` mapped in `MentalMetal.Web`, new EF configurations and repository in `MentalMetal.Infrastructure/Interviews/`, new Angular feature module `src/app/pages/interviews/`.
- **Dependencies**: reuses `IAiCompletionService` and `AiProviderConfig` from `ai-provider-abstraction`; reads from `person-management` (validates candidate Person existence and user ownership).
- **Database**: additive migration; no changes to existing tables. Interviews are soft-independent of `Person.CandidateDetails.PipelineStatus` (no automatic writes back — users can still update pipeline status manually).
- **Non-goals**:
  - Scheduling / calendar integration (no external calendar API).
  - Multi-interviewer collaboration (single-user app; `RecordedAtUtc` is the only audit metadata).
  - Audio capture or live transcription — transcript must be pasted text (audio is a separate Tier 3 spec `capture-audio`).
  - Automatic promotion of `Interview` stage into `Person.CandidateDetails.PipelineStatus` — user-controlled.
- **Non-breaking**: no existing endpoint, aggregate, or UI is modified.
