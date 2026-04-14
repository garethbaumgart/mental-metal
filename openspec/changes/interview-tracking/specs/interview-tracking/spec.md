## ADDED Requirements

### Requirement: Create an interview

The system SHALL allow an authenticated user to create an `Interview` record linked to a candidate `Person`. Required fields: `candidatePersonId` (must reference a Person owned by the same user whose `Type` includes `Candidate`), `roleTitle` (non-empty). Optional fields: `scheduledAtUtc`. On success the Interview SHALL be created with `Stage = Applied`, `UserId` equal to the authenticated user, `CreatedAtUtc` set from the injected clock, and the system SHALL raise an `InterviewCreated` domain event. The endpoint SHALL be `POST /api/interviews`.

#### Scenario: Create interview with required fields only

- **WHEN** an authenticated user sends `POST /api/interviews` with `candidatePersonId` of a valid candidate Person and `roleTitle "Staff Engineer"`
- **THEN** the system creates an Interview with `Stage = Applied` and returns HTTP 201 with the Interview DTO

#### Scenario: Create interview with scheduled date

- **WHEN** an authenticated user sends `POST /api/interviews` with `candidatePersonId`, `roleTitle "Senior SRE"`, and `scheduledAtUtc "2026-04-20T15:00:00Z"`
- **THEN** the system creates the Interview with the scheduled date and returns HTTP 201

#### Scenario: Missing candidatePersonId rejected

- **WHEN** an authenticated user sends `POST /api/interviews` without a `candidatePersonId`
- **THEN** the system returns HTTP 400 with a validation error

#### Scenario: Empty roleTitle rejected

- **WHEN** an authenticated user sends `POST /api/interviews` with an empty `roleTitle`
- **THEN** the system returns HTTP 400 with a validation error

#### Scenario: CandidatePersonId references another user's person

- **WHEN** an authenticated user sends `POST /api/interviews` with a `candidatePersonId` that belongs to another user
- **THEN** the system returns HTTP 404 with error code `candidate_not_found`

#### Scenario: Person is not a candidate

- **WHEN** an authenticated user sends `POST /api/interviews` with a `candidatePersonId` whose Person Type does not include `Candidate`
- **THEN** the system returns HTTP 400 with error code `candidate_not_found`

#### Scenario: Unauthenticated request rejected

- **WHEN** an unauthenticated client sends `POST /api/interviews`
- **THEN** the system returns HTTP 401

### Requirement: Update interview metadata

The system SHALL allow an authenticated user to update an Interview's `roleTitle` and `scheduledAtUtc` via `PATCH /api/interviews/{id}`. `roleTitle`, when supplied, MUST NOT be empty. `scheduledAtUtc` MAY be set to null to clear it. The system SHALL set `UpdatedAtUtc` from the injected clock and raise an `InterviewUpdated` domain event.

#### Scenario: Update role title

- **WHEN** an authenticated user sends `PATCH /api/interviews/{id}` with `roleTitle "Principal Engineer"`
- **THEN** the Interview's role title is updated and HTTP 200 is returned

#### Scenario: Clear scheduled date

- **WHEN** an authenticated user sends `PATCH /api/interviews/{id}` with `scheduledAtUtc` null
- **THEN** the Interview's scheduled date is cleared and HTTP 200 is returned

#### Scenario: Empty roleTitle rejected

- **WHEN** an authenticated user sends `PATCH /api/interviews/{id}` with `roleTitle ""`
- **THEN** the system returns HTTP 400

#### Scenario: Interview not found

- **WHEN** an authenticated user sends `PATCH /api/interviews/{id}` for an id not owned by the user
- **THEN** the system returns HTTP 404

### Requirement: Interview stage lifecycle

The `Interview` aggregate SHALL enforce a stage lifecycle with guarded transitions. Forward transitions: `Applied → ScreenScheduled → ScreenCompleted → OnsiteScheduled → OnsiteCompleted → OfferExtended → Hired`. `Rejected` and `Withdrawn` SHALL be reachable from any non-terminal stage. Terminal stages (`Hired`, `Rejected`, `Withdrawn`) SHALL NOT have any outgoing transitions. Transitioning into `ScreenCompleted`, `OnsiteCompleted`, `Hired`, or `Rejected` SHALL set `CompletedAtUtc` from the injected clock if not already set; other transitions SHALL leave `CompletedAtUtc` unchanged. Each transition SHALL raise an `InterviewStageChanged` domain event. The endpoint SHALL be `POST /api/interviews/{id}/advance` with body `{ "targetStage": "<stage>" }`.

#### Scenario: Advance from Applied to ScreenScheduled

- **WHEN** an authenticated user sends `POST /api/interviews/{id}/advance` with `targetStage "ScreenScheduled"` on an Interview currently in `Applied`
- **THEN** the stage transitions to `ScreenScheduled` and HTTP 200 is returned

#### Scenario: Advance through onsite to offer

- **WHEN** an authenticated user advances an Interview currently in `OnsiteCompleted` to `OfferExtended`
- **THEN** the stage transitions and HTTP 200 is returned

#### Scenario: Reject from any non-terminal stage

- **WHEN** an authenticated user advances an Interview currently in `ScreenScheduled` with `targetStage "Rejected"`
- **THEN** the stage transitions to `Rejected`, `CompletedAtUtc` is set, and HTTP 200 is returned

#### Scenario: Withdraw from any non-terminal stage

- **WHEN** an authenticated user advances an Interview currently in `OfferExtended` with `targetStage "Withdrawn"`
- **THEN** the stage transitions to `Withdrawn` and HTTP 200 is returned

#### Scenario: Invalid forward transition rejected

- **WHEN** an authenticated user advances an Interview currently in `Applied` with `targetStage "OnsiteScheduled"` (skipping screen)
- **THEN** the system returns HTTP 409 with error code `invalid_stage_transition`

#### Scenario: Transition from terminal stage rejected

- **WHEN** an authenticated user advances an Interview currently in `Hired` with any `targetStage`
- **THEN** the system returns HTTP 409 with error code `invalid_stage_transition`

### Requirement: Record interview decision

The system SHALL allow an authenticated user to record a `Decision` on an Interview via `POST /api/interviews/{id}/decision` with body `{ "decision": "<value>" }` where value is one of `StrongHire`, `Hire`, `LeanHire`, `NoHire`, `StrongNoHire`. Decision MAY be recorded only when the Interview's stage is one of `ScreenCompleted`, `OnsiteCompleted`, `OfferExtended`, `Hired`, or `Rejected`. The decision MAY be updated at any time while the stage permits it. The system SHALL raise an `InterviewDecisionRecorded` domain event.

#### Scenario: Record decision after onsite

- **WHEN** an authenticated user records `Hire` on an Interview in `OnsiteCompleted`
- **THEN** the decision is saved and HTTP 200 is returned

#### Scenario: Update existing decision

- **WHEN** an authenticated user records `StrongHire` on an Interview that already has decision `Hire`
- **THEN** the decision is replaced and HTTP 200 is returned

#### Scenario: Decision rejected before completion

- **WHEN** an authenticated user records a decision on an Interview in `Applied`
- **THEN** the system returns HTTP 409 with error code `decision_not_allowed`

#### Scenario: Invalid decision value rejected

- **WHEN** an authenticated user records a decision of `"maybe"`
- **THEN** the system returns HTTP 400

### Requirement: List interviews

The system SHALL allow an authenticated user to retrieve a list of their Interviews via `GET /api/interviews`. The list SHALL support optional filters `candidatePersonId` and `stage`. The list SHALL be ordered by `CreatedAtUtc` descending and MUST include only Interviews belonging to the authenticated user.

#### Scenario: List all interviews

- **WHEN** an authenticated user sends `GET /api/interviews`
- **THEN** the system returns all Interviews owned by that user ordered by `CreatedAtUtc` descending

#### Scenario: Filter by candidate

- **WHEN** an authenticated user sends `GET /api/interviews?candidatePersonId={id}`
- **THEN** the system returns only Interviews whose `CandidatePersonId` matches

#### Scenario: Filter by stage

- **WHEN** an authenticated user sends `GET /api/interviews?stage=OnsiteScheduled`
- **THEN** the system returns only Interviews whose stage matches

#### Scenario: Another user's interviews excluded

- **WHEN** User A and User B each have Interviews and User A sends `GET /api/interviews`
- **THEN** the response contains only User A's Interviews

#### Scenario: Empty list

- **WHEN** an authenticated user with no Interviews sends `GET /api/interviews`
- **THEN** the system returns an empty array with HTTP 200

### Requirement: Get interview by ID

The system SHALL allow an authenticated user to retrieve a single Interview by ID via `GET /api/interviews/{id}`, including its scorecards collection and transcript (if any).

#### Scenario: Get existing interview

- **WHEN** an authenticated user sends `GET /api/interviews/{id}` for an Interview they own
- **THEN** the system returns the Interview DTO including scorecards and transcript with HTTP 200

#### Scenario: Interview belongs to another user

- **WHEN** an authenticated user sends `GET /api/interviews/{id}` for an Interview owned by another user
- **THEN** the system returns HTTP 404

#### Scenario: Interview not found

- **WHEN** an authenticated user sends `GET /api/interviews/{id}` for a non-existent id
- **THEN** the system returns HTTP 404

### Requirement: Delete interview

The system SHALL allow an authenticated user to delete an Interview via `DELETE /api/interviews/{id}`. Deletion SHALL cascade to its owned scorecards and transcript. The system SHALL raise an `InterviewDeleted` domain event.

#### Scenario: Delete existing interview

- **WHEN** an authenticated user sends `DELETE /api/interviews/{id}` for an Interview they own
- **THEN** the Interview and its owned children are removed and HTTP 204 is returned

#### Scenario: Delete non-existent interview

- **WHEN** an authenticated user sends `DELETE /api/interviews/{id}` for an id not owned by the user
- **THEN** the system returns HTTP 404

### Requirement: Manage scorecards

The system SHALL allow an authenticated user to add, update, and remove scorecards on an Interview. Each scorecard has `competency` (non-empty), `rating` (integer 1–5), optional `notes`, and `recordedAtUtc` (set from the injected clock on creation). Endpoints:

- `POST /api/interviews/{id}/scorecards` — body `{ competency, rating, notes? }`. Returns HTTP 201 with the scorecard DTO.
- `PUT /api/interviews/{id}/scorecards/{scorecardId}` — body `{ competency, rating, notes? }`. Returns HTTP 200.
- `DELETE /api/interviews/{id}/scorecards/{scorecardId}` — returns HTTP 204.

The system SHALL raise `InterviewScorecardAdded`, `InterviewScorecardUpdated`, and `InterviewScorecardRemoved` domain events respectively.

#### Scenario: Add a scorecard

- **WHEN** an authenticated user sends `POST /api/interviews/{id}/scorecards` with `competency "System Design"`, `rating 4`, and `notes "Strong distributed systems fundamentals"`
- **THEN** the scorecard is added with `recordedAtUtc` set and HTTP 201 is returned

#### Scenario: Update a scorecard

- **WHEN** an authenticated user sends `PUT /api/interviews/{id}/scorecards/{scorecardId}` with `rating 5`
- **THEN** the scorecard is updated and HTTP 200 is returned

#### Scenario: Remove a scorecard

- **WHEN** an authenticated user sends `DELETE /api/interviews/{id}/scorecards/{scorecardId}`
- **THEN** the scorecard is removed and HTTP 204 is returned

#### Scenario: Rating out of range rejected

- **WHEN** an authenticated user sends `POST /api/interviews/{id}/scorecards` with `rating 6`
- **THEN** the system returns HTTP 400

#### Scenario: Empty competency rejected

- **WHEN** an authenticated user sends `POST /api/interviews/{id}/scorecards` with `competency ""`
- **THEN** the system returns HTTP 400

#### Scenario: Scorecard on interview owned by another user

- **WHEN** an authenticated user adds a scorecard to an Interview they do not own
- **THEN** the system returns HTTP 404

### Requirement: Set interview transcript

The system SHALL allow an authenticated user to set or replace the transcript on an Interview via `PUT /api/interviews/{id}/transcript` with body `{ "rawText": "<text>" }`. Setting or replacing the transcript SHALL atomically clear any existing AI analysis fields (`Summary`, `RecommendedDecision`, `RiskSignals`, `AnalyzedAtUtc`, `Model`) on the transcript value object. The raw text length MUST NOT exceed `InterviewAnalysisOptions.MaxPromptChars` (default 16000); requests exceeding this SHALL return HTTP 413. The system SHALL raise an `InterviewTranscriptSet` domain event.

#### Scenario: Set transcript for the first time

- **WHEN** an authenticated user sends `PUT /api/interviews/{id}/transcript` with raw text
- **THEN** the transcript is stored with analysis fields null and HTTP 200 is returned

#### Scenario: Replace transcript clears stale analysis

- **WHEN** an authenticated user replaces a transcript that previously had an AI analysis
- **THEN** the new raw text is stored AND `Summary`, `RecommendedDecision`, `RiskSignals`, `AnalyzedAtUtc`, `Model` are all cleared and HTTP 200 is returned

#### Scenario: Transcript too long rejected

- **WHEN** an authenticated user sends a transcript exceeding `MaxPromptChars`
- **THEN** the system returns HTTP 413

#### Scenario: Empty raw text rejected

- **WHEN** an authenticated user sends `PUT /api/interviews/{id}/transcript` with empty `rawText`
- **THEN** the system returns HTTP 400

### Requirement: Analyze transcript with AI

The system SHALL expose `POST /api/interviews/{id}/analyze` which generates an AI-driven summary, recommended decision, and risk signals from the Interview's transcript and scorecards. The implementation SHALL call `IAiCompletionService` via an `IInterviewAnalysisService` with:

- `Temperature = 0.3`
- `MaxTokens = InterviewAnalysisOptions.MaxAnalysisTokens`
- A system prompt forbidding the model from inventing names, dates, or scores outside the supplied facts
- A user prompt consisting of a JSON-serialised facts block containing scorecards and the escaped transcript (backtick characters in the transcript SHALL be escaped to the Unicode form `\u0060` before being embedded into the prompt)

The response SHALL include `summary` (markdown), `recommendedDecision` (one of the five `InterviewDecision` enum values or null when the model returns a value outside the enum) and `riskSignals` (list of short strings). On success the analysis fields SHALL be persisted on the transcript value object and the system SHALL raise an `InterviewAnalysisGenerated` domain event.

Preconditions:
- The Interview SHALL have a transcript with non-empty `rawText`. Otherwise the system SHALL return HTTP 409 with error code `transcript_missing`.
- The user SHALL have an `AiProviderConfig`. Otherwise the system SHALL return HTTP 409 with error code `ai_provider_not_configured`.
- Any provider error SHALL return HTTP 502 with error code `analysis_failed` and SHALL NOT overwrite existing analysis.

The service SHALL obtain "now" from the injected clock; implementations MUST NOT read `DateTime.UtcNow` directly.

#### Scenario: Analyze an interview with transcript and scorecards

- **WHEN** an authenticated user with a configured AI provider sends `POST /api/interviews/{id}/analyze` and the Interview has a transcript and at least one scorecard
- **THEN** the system calls the AI provider, persists `summary`, `recommendedDecision`, `riskSignals`, `analyzedAtUtc`, and `model` on the transcript, and returns HTTP 200 with the analysis DTO

#### Scenario: Analyze regenerates over existing analysis

- **WHEN** an authenticated user analyzes an Interview that already has a prior analysis
- **THEN** the new analysis replaces the old analysis fields and HTTP 200 is returned

#### Scenario: Analyze without transcript rejected

- **WHEN** an authenticated user sends `POST /api/interviews/{id}/analyze` on an Interview with no transcript
- **THEN** the system returns HTTP 409 with error code `transcript_missing`

#### Scenario: Analyze without configured AI provider rejected

- **WHEN** a user with no `AiProviderConfig` sends `POST /api/interviews/{id}/analyze`
- **THEN** the system returns HTTP 409 with error code `ai_provider_not_configured` and does not call the provider

#### Scenario: Model returns decision outside the enum

- **WHEN** the AI provider returns `recommendedDecision` of `"maybe"` which is not a valid `InterviewDecision`
- **THEN** the system stores `recommendedDecision` as null, still persists `summary` and `riskSignals`, and returns HTTP 200 with a warning in the response

#### Scenario: Provider error surfaces as 502

- **WHEN** the AI provider returns an error during analysis
- **THEN** the system returns HTTP 502 with error code `analysis_failed` and the previously persisted analysis (if any) is unchanged

#### Scenario: Transcript backticks escaped in prompt

- **WHEN** the transcript `rawText` contains backtick characters
- **THEN** the prompt sent to the AI provider contains those characters replaced with the Unicode escape `\u0060`

### Requirement: Interview user isolation

All Interview endpoints SHALL enforce user isolation: responses SHALL only contain Interviews whose `UserId` matches the authenticated user, and all mutations SHALL reject requests that reference Interviews, scorecards, or candidate People owned by another user. EF Core queries SHALL filter by `UserId`. LINQ predicates involving identifier collections MUST use `List<T>.Contains()` (not `HashSet<T>.Contains()`), and string comparisons MUST use `.ToLower()` (not `.ToLowerInvariant()`), to remain translatable to SQL.

#### Scenario: Mutation on another user's interview rejected

- **WHEN** User A attempts any mutation (`PATCH`, `POST advance`, `POST decision`, scorecard CRUD, transcript set, analyze, `DELETE`) on an Interview owned by User B
- **THEN** the system returns HTTP 404

#### Scenario: Query returns only own interviews

- **WHEN** User A calls `GET /api/interviews`
- **THEN** User B's Interviews are never returned, regardless of filters

### Requirement: Interviews pipeline view

The frontend SHALL provide an `/interviews` route rendering a pipeline view grouped by `InterviewStage`. Each stage column SHALL list the user's Interviews in that stage showing candidate name, role title, scheduled date (if any), and decision badge (if set). The view SHALL use PrimeNG components (Card, Tag, Button) and `tailwindcss-primeui` utility classes — no hardcoded Tailwind colour utilities, no `*ngIf` / `*ngFor`, and no `dark:` prefix. State SHALL be managed with signals; data access SHALL use `inject()` DI.

#### Scenario: Render pipeline with interviews across stages

- **WHEN** a user with Interviews in multiple stages navigates to `/interviews`
- **THEN** the system renders one column per stage and populates each column with that stage's Interviews

#### Scenario: Empty state per column

- **WHEN** a user navigates to `/interviews` and a stage has no Interviews
- **THEN** the corresponding column renders an empty-state placeholder

#### Scenario: Create interview from pipeline

- **WHEN** a user clicks "New interview" and submits the form with candidate, role title, and optional scheduled date
- **THEN** a new Interview is created and appears in the `Applied` column

### Requirement: Interview detail view

The frontend SHALL provide an `/interviews/:id` route rendering the Interview detail page with a PrimeNG TabView containing four tabs: **Overview** (metadata, stage advance, decision), **Scorecards** (list / add / edit / remove), **Transcript** (paste / replace), and **AI Analysis** (trigger analyze, show summary / recommended decision / risk signals). Controls SHALL use Signal Forms where forms are involved.

#### Scenario: View interview details

- **WHEN** a user navigates to `/interviews/:id` for an Interview they own
- **THEN** the page renders the four tabs populated with the Interview's data

#### Scenario: Advance stage from overview tab

- **WHEN** a user selects a valid next stage and clicks "Advance"
- **THEN** the Interview's stage updates and the tab reflects the new stage

#### Scenario: Add scorecard from scorecards tab

- **WHEN** a user adds a scorecard with competency, rating, and notes
- **THEN** the scorecard appears in the list

#### Scenario: Run analysis from AI analysis tab

- **WHEN** a user with a configured AI provider clicks "Analyze" on an Interview that has a transcript
- **THEN** the system displays the summary, recommended decision, and risk signals returned by the API
