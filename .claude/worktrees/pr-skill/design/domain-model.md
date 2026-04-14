# My Work Companion Domain Model

A domain model for an AI-powered command centre for engineering managers. The system passively accumulates context from raw captures, meeting transcripts, and quick notes, then uses AI to build a living, queryable picture of people, initiatives, and commitments.

---

## Aggregates Overview

| Aggregate | Purpose |
|-----------|---------|
| **User** | Authenticated user, profile, preferences, AI provider config |
| **Person** | Anyone the user interacts with -- direct reports, stakeholders, candidates |
| **Initiative** | The living brief for a project or workstream |
| **Capture** | Raw input unit -- text notes, pasted transcripts, meeting notes |
| **Commitment** | Bidirectional promise between the user and another person |
| **Delegation** | Task assigned by the user to another person with follow-up ownership |
| **OneOnOne** | A 1:1 meeting record linked to a person |
| **Interview** | Interview record linked to a candidate |
| **Observation** | Tagged evidence note about a person over time |
| **Goal** | A development goal belonging to a direct report |
| **Nudge** | A recurring reminder/rhythm prompt |
| **ChatThread** | A conversation with the AI assistant |

---

## 1. User Aggregate

**Purpose:** Represents the authenticated user who owns all data. Holds profile, preferences, and AI provider configuration. All other aggregates are scoped to a User.

### Entity: User

| Property | Type | Rules |
|----------|------|-------|
| Id | Guid | Immutable, generated |
| ExternalAuthId | string | Immutable after creation, unique |
| Email | Email (VO) | Unique, valid format |
| Name | string | Required, non-empty |
| AvatarUrl | string? | Nullable |
| Preferences | UserPreferences (VO) | Embedded, defaults applied |
| AiProviderConfig | AiProviderConfig (VO) | Embedded, required for AI features |
| Timezone | string | IANA timezone, required |
| CreatedAt | DateTimeOffset | Set on creation |
| LastLoginAt | DateTimeOffset | Updated on each login |

### Business Actions

| Action | Description | Business Rules | Domain Event |
|--------|-------------|----------------|--------------|
| `Register(authId, email, name, avatarUrl)` | Create user from OAuth | AuthId and Email must be unique | `UserRegistered` |
| `UpdateProfile(name, avatarUrl, timezone)` | Update profile info | Name cannot be empty | `UserProfileUpdated` |
| `ConfigureAiProvider(provider, apiKey, model)` | Set AI provider for all AI features | Provider must be supported, key validated | `AiProviderConfigured` |
| `UpdatePreferences(preferences)` | Change briefing time, notification prefs, etc. | Validated against allowed values | `PreferencesUpdated` |
| `RecordLogin()` | Update last login timestamp | Sets LastLoginAt to now | - |

### Invariants
- Email must be valid format
- ExternalAuthId is immutable after creation
- Timezone must be a valid IANA timezone
- AiProviderConfig must be set before any AI processing can occur
- One user = one external auth account

---

## 2. Person Aggregate

**Purpose:** Represents anyone the user interacts with professionally. Polymorphic by type: direct reports carry career context, candidates carry pipeline context, stakeholders carry relationship context. Accumulates linked observations, goals, commitments, and 1:1 history over time.

### Entity: Person

| Property | Type | Rules |
|----------|------|-------|
| Id | Guid | Immutable |
| UserId | Guid | FK -> User, immutable |
| Name | string | Required, non-empty |
| PersonType | PersonType (VO) | Required, can change (e.g. candidate -> direct-report) |
| Email | string? | Optional |
| Role | string? | Job title or role description |
| Team | string? | Team name |
| Notes | string? | Free-form context notes |
| CareerDetails | CareerDetails? (VO) | Only for direct-reports |
| CandidateDetails | CandidateDetails? (VO) | Only for candidates |
| CreatedAt | DateTimeOffset | Set on creation |
| UpdatedAt | DateTimeOffset | Updated on any change |

### Business Actions

| Action | Description | Business Rules | Domain Event |
|--------|-------------|----------------|--------------|
| `Create(userId, name, type, email?, role?)` | Add a new person | Name required, type determines required details | `PersonCreated` |
| `UpdateProfile(name, email, role, team, notes)` | Update person info | Name cannot be empty | `PersonProfileUpdated` |
| `ChangeType(newType)` | Transition person type | Must supply required details for new type; old type details archived | `PersonTypeChanged` |
| `UpdateCareerDetails(level, aspirations, growthAreas)` | Update career context | Only valid for direct-reports | `CareerDetailsUpdated` |
| `UpdateCandidateDetails(pipelineStatus, cvNotes)` | Update candidate pipeline | Only valid for candidates | `CandidateDetailsUpdated` |
| `AdvanceCandidatePipeline(newStatus)` | Move candidate to next stage | Must follow valid pipeline transitions | `CandidatePipelineAdvanced` |
| `Archive()` | Soft-archive the person | Archived people excluded from active views but data preserved | `PersonArchived` |

### Invariants
- Person belongs to exactly one User
- Name cannot be empty
- CareerDetails can only be set when PersonType is `direct-report`
- CandidateDetails can only be set when PersonType is `candidate`
- Changing PersonType from `candidate` to `direct-report` preserves interview history
- A Person's name must be unique within a User's people (prevents accidental duplicates)

---

## 3. Initiative Aggregate

**Purpose:** The living brief for a project, workstream, or cross-team effort. AI maintains an evolving summary, key decisions, risks, requirements, and design direction. Every linked capture automatically updates the brief.

### Entity: Initiative

| Property | Type | Rules |
|----------|------|-------|
| Id | Guid | Immutable |
| UserId | Guid | FK -> User, immutable |
| Title | string | Required, non-empty |
| Status | InitiativeStatus (VO) | Active, On Hold, Completed, Cancelled |
| AiSummary | string? | AI-maintained living summary |
| SummaryLastUpdatedAt | DateTimeOffset? | When AI last refreshed the summary |
| KeyDecisions | List\<KeyDecision\> (VO) | Ordered by date, append-only conceptually |
| OpenRisks | List\<Risk\> (VO) | Active risks, can be resolved |
| Requirements | List\<RequirementSnapshot\> (VO) | Current + historical snapshots |
| DesignDirection | List\<DesignSnapshot\> (VO) | Current + historical snapshots |
| Dependencies | List\<Dependency\> (VO) | Cross-team dependencies |
| Milestones | List\<Milestone\> (VO) | Key dates and deliverables |
| LinkedPersonIds | List\<Guid\> | References to involved People |
| CreatedAt | DateTimeOffset | Set on creation |
| UpdatedAt | DateTimeOffset | Updated on any change |

### Business Actions

| Action | Description | Business Rules | Domain Event |
|--------|-------------|----------------|--------------|
| `Create(userId, title)` | Start a new initiative | Title required | `InitiativeCreated` |
| `UpdateTitle(title)` | Rename initiative | Cannot be empty | `InitiativeTitleUpdated` |
| `ChangeStatus(newStatus)` | Transition initiative status | Must follow valid transitions | `InitiativeStatusChanged` |
| `RefreshSummary(aiSummary)` | AI updates the living summary | Called by BriefUpdateService after capture processing | `InitiativeSummaryRefreshed` |
| `RecordDecision(description, madeBy, rationale, date)` | Log a key decision | Description required, madeBy references a PersonId | `DecisionRecorded` |
| `RaiseRisk(description, severity, mitigationPlan?)` | Add an open risk | Description required | `RiskRaised` |
| `ResolveRisk(riskId, resolution)` | Close a risk | Risk must exist and be open | `RiskResolved` |
| `UpdateRequirements(content, source)` | Snapshot current requirements state | Appends to history, previous snapshots preserved | `RequirementsUpdated` |
| `UpdateDesignDirection(content, source)` | Snapshot current design state | Appends to history, previous snapshots preserved | `DesignDirectionUpdated` |
| `AddDependency(team, description, status)` | Record cross-team dependency | Team and description required | `DependencyAdded` |
| `UpdateDependency(depId, status, notes)` | Update dependency status | Dependency must exist | `DependencyUpdated` |
| `SetMilestone(title, targetDate, description?)` | Add or update a milestone | Title and date required | `MilestoneSet` |
| `LinkPerson(personId)` | Associate a person with this initiative | Person must exist | `PersonLinkedToInitiative` |

### Initiative Status State Machine

```
                     Resume()
         ┌─────────────────────────┐
         │                         │
  ┌──────┴──┐    Hold()    ┌──────────┐
  │  ACTIVE  │────────────>│ ON_HOLD  │
  └─────┬────┘             └──────────┘
        │
        ├── Complete() ──> [ COMPLETED ]
        │
        └── Cancel() ────> [ CANCELLED ]
```

### Invariants
- Initiative belongs to exactly one User
- Title cannot be empty
- Completed/Cancelled initiatives cannot be modified (except status reversal within grace period)
- Requirements and Design snapshots are append-only; history is never deleted
- KeyDecisions are immutable once recorded
- Risks can only be resolved, never deleted
- At least one milestone should have a target date in the future for active initiatives (soft rule, not enforced)

---

## 4. Capture Aggregate

**Purpose:** The raw input unit. Everything enters the system through a Capture: quick text notes, pasted transcripts, meeting notes. AI processes the raw content to extract structured data and link it to people, initiatives, and commitments. The original raw content is always preserved.

### Entity: Capture

| Property | Type | Rules |
|----------|------|-------|
| Id | Guid | Immutable |
| UserId | Guid | FK -> User, immutable |
| RawContent | string | Required, immutable after creation |
| CaptureType | CaptureType (VO) | quick-note, transcript, meeting-notes |
| ProcessingStatus | ProcessingStatus (VO) | raw -> processing -> processed / failed |
| AiExtraction | AiExtraction? (VO) | Structured data extracted by AI |
| LinkedInitiativeIds | List\<Guid\> | Initiatives this capture relates to |
| LinkedPersonIds | List\<Guid\> | People mentioned or involved |
| SpawnedCommitmentIds | List\<Guid\> | Commitments created from this capture |
| SpawnedDelegationIds | List\<Guid\> | Delegations created from this capture |
| SpawnedObservationIds | List\<Guid\> | Observations created from this capture |
| Title | string? | Optional user-provided or AI-generated title |
| CapturedAt | DateTimeOffset | When the user created the capture |
| ProcessedAt | DateTimeOffset? | When AI processing completed |
| Source | string? | e.g. "leadership sync", "1:1 with Sarah" |

### Business Actions

| Action | Description | Business Rules | Domain Event |
|--------|-------------|----------------|--------------|
| `Create(userId, rawContent, type, source?, title?)` | Submit raw input | RawContent required, status set to `raw` | `CaptureCreated` |
| `BeginProcessing()` | Mark capture as being processed by AI | Must be in `raw` status | `CaptureProcessingStarted` |
| `CompleteProcessing(extraction)` | Store AI extraction results | Must be in `processing` status | `CaptureProcessed` |
| `FailProcessing(reason)` | Mark processing as failed | Must be in `processing` status, stores failure reason | `CaptureProcessingFailed` |
| `RetryProcessing()` | Reset failed capture for reprocessing | Must be in `failed` status, resets to `raw` | `CaptureRetryRequested` |
| `LinkToInitiative(initiativeId)` | Associate with an initiative | Initiative must exist | `CaptureLinkedToInitiative` |
| `LinkToPerson(personId)` | Associate with a person | Person must exist | `CaptureLinkedToPerson` |
| `RecordSpawnedCommitment(commitmentId)` | Track that processing created a commitment | Called during extraction | - |
| `RecordSpawnedDelegation(delegationId)` | Track that processing created a delegation | Called during extraction | - |
| `RecordSpawnedObservation(observationId)` | Track that processing created an observation | Called during extraction | - |
| `Discard()` | Mark capture as discarded during close-out | Only for unprocessed or uncertain captures | `CaptureDiscarded` |
| `ConfirmExtraction()` | User confirms AI extraction is correct | Must be in `processed` status | `CaptureExtractionConfirmed` |

### Processing Status State Machine

```
  ┌─────┐  BeginProcessing()  ┌────────────┐  CompleteProcessing()  ┌───────────┐
  │ RAW │────────────────────>│ PROCESSING │──────────────────────>│ PROCESSED │
  └─────┘                     └──────┬─────┘                       └───────────┘
     ▲                               │
     │        RetryProcessing()      │ FailProcessing()
     │                               ▼
     └───────────────────────┌────────┐
                             │ FAILED │
                             └────────┘
```

### Invariants
- Capture belongs to exactly one User
- RawContent is immutable after creation (original source always preserved)
- Processing status transitions must follow the state machine
- A capture in `raw` status has no AiExtraction
- A capture in `processed` status must have an AiExtraction
- SpawnedIds are append-only

---

## 5. Commitment Aggregate

**Purpose:** A bidirectional promise between the user and another person. "I owe X to person Y by date Z" or "Person Y owes me X by date Z." Surfaced in briefings, the queue, and person views. Linked optionally to an initiative.

### Entity: Commitment

| Property | Type | Rules |
|----------|------|-------|
| Id | Guid | Immutable |
| UserId | Guid | FK -> User, immutable |
| Description | string | Required, non-empty |
| Direction | CommitmentDirection (VO) | mine-to-them or theirs-to-me |
| PersonId | Guid | FK -> Person, required |
| InitiativeId | Guid? | FK -> Initiative, optional |
| SourceCaptureId | Guid? | FK -> Capture that spawned this |
| DueDate | DateOnly? | Optional deadline |
| Status | CommitmentStatus (VO) | open, completed, cancelled |
| CompletedAt | DateTimeOffset? | Set when completed |
| Notes | string? | Additional context |
| CreatedAt | DateTimeOffset | Set on creation |
| UpdatedAt | DateTimeOffset | Updated on any change |

### Business Actions

| Action | Description | Business Rules | Domain Event |
|--------|-------------|----------------|--------------|
| `Create(userId, description, direction, personId, dueDate?, initiativeId?, sourceId?)` | Record a commitment | Description and PersonId required | `CommitmentCreated` |
| `Complete(notes?)` | Mark commitment as fulfilled | Must be `open` | `CommitmentCompleted` |
| `Cancel(reason?)` | Cancel the commitment | Must be `open` | `CommitmentCancelled` |
| `Reopen()` | Reopen a completed or cancelled commitment | Must not already be `open` | `CommitmentReopened` |
| `UpdateDueDate(newDate)` | Change the deadline | Logs the change | `CommitmentDueDateChanged` |
| `UpdateDescription(description)` | Refine commitment text | Cannot be empty | `CommitmentDescriptionUpdated` |
| `LinkToInitiative(initiativeId)` | Associate with initiative | Initiative must exist | `CommitmentLinkedToInitiative` |
| `MarkOverdue()` | System marks commitment as overdue | DueDate must be in the past, status must be `open` | `CommitmentBecameOverdue` |

### Invariants
- Commitment belongs to exactly one User
- Must be linked to exactly one Person
- Description cannot be empty
- Status transitions: open -> completed, open -> cancelled, completed -> open, cancelled -> open
- CompletedAt only set when status is `completed`
- A commitment cannot be both completed and overdue

---

## 6. Delegation Aggregate

**Purpose:** A task the user has assigned to someone else. The user retains follow-up ownership. Tracks status and links to the relevant initiative.

### Entity: Delegation

| Property | Type | Rules |
|----------|------|-------|
| Id | Guid | Immutable |
| UserId | Guid | FK -> User, immutable |
| Description | string | Required, non-empty |
| DelegatePersonId | Guid | FK -> Person, the person doing the work |
| InitiativeId | Guid? | FK -> Initiative, optional |
| SourceCaptureId | Guid? | FK -> Capture that spawned this |
| DueDate | DateOnly? | Optional deadline |
| Status | DelegationStatus (VO) | assigned, in-progress, completed, blocked |
| Priority | Priority (VO) | low, medium, high, urgent |
| CompletedAt | DateTimeOffset? | Set when completed |
| Notes | string? | Context, updates, follow-up notes |
| LastFollowedUpAt | DateTimeOffset? | When user last checked in |
| CreatedAt | DateTimeOffset | Set on creation |
| UpdatedAt | DateTimeOffset | Updated on any change |

### Business Actions

| Action | Description | Business Rules | Domain Event |
|--------|-------------|----------------|--------------|
| `Create(userId, description, delegateId, dueDate?, initiativeId?, priority?)` | Assign work to someone | Description and DelegatePersonId required | `DelegationCreated` |
| `MarkInProgress()` | Delegate has started work | Must be `assigned` | `DelegationStarted` |
| `MarkCompleted(notes?)` | Work is done | Must be `assigned`, `in-progress`, or `blocked` | `DelegationCompleted` |
| `MarkBlocked(reason)` | Work is blocked | Must be `assigned` or `in-progress` | `DelegationBlocked` |
| `Unblock()` | Remove blocker | Must be `blocked` | `DelegationUnblocked` |
| `RecordFollowUp(notes?)` | Log that user followed up | Updates LastFollowedUpAt | `DelegationFollowedUp` |
| `UpdateDueDate(newDate)` | Change deadline | Logs the change | `DelegationDueDateChanged` |
| `Reprioritize(newPriority)` | Change priority level | - | `DelegationReprioritized` |
| `Reassign(newPersonId)` | Move to a different person | New person must exist | `DelegationReassigned` |

### Delegation Status State Machine

```
  ┌──────────┐  MarkInProgress()  ┌─────────────┐
  │ ASSIGNED │───────────────────>│ IN_PROGRESS │
  └────┬─────┘                    └──────┬──────┘
       │                                 │
       ├── MarkCompleted() ──────────────┤── MarkCompleted()
       │             │                   │          │
       │             ▼                   │          ▼
       │      [ COMPLETED ]              │   [ COMPLETED ]
       │                                 │
       ├── MarkBlocked() ───>┌─────────┐│── MarkBlocked()
       │                     │ BLOCKED │◄┘
       │                     └────┬────┘
       │                          │ Unblock()
       │                          ▼
       │                   ┌─────────────┐
       └──────────────────>│ IN_PROGRESS │
                           └─────────────┘
```

### Invariants
- Delegation belongs to exactly one User
- Must be linked to exactly one Person (the delegate)
- The user is always the follow-up owner, never the delegate
- Description cannot be empty
- CompletedAt only set when status is `completed`
- Blocked delegations require a reason

---

## 7. OneOnOne Aggregate

**Purpose:** A 1:1 meeting record with a direct report or stakeholder. Contains meeting notes, extracted action items, topics discussed, and follow-ups. Builds a chronological history for each person.

### Entity: OneOnOne

| Property | Type | Rules |
|----------|------|-------|
| Id | Guid | Immutable |
| UserId | Guid | FK -> User, immutable |
| PersonId | Guid | FK -> Person, required |
| MeetingDate | DateOnly | Required |
| Notes | string? | Free-form meeting notes |
| SourceCaptureId | Guid? | FK -> Capture if created from transcript |
| TopicsDiscussed | List\<string\> | Extracted or manually added |
| ActionItems | List\<ActionItem\> (VO) | Extracted items with owner and status |
| FollowUps | List\<FollowUp\> (VO) | Items to raise next meeting |
| Mood | Mood? (VO) | Optional sentiment indicator |
| CreatedAt | DateTimeOffset | Set on creation |
| UpdatedAt | DateTimeOffset | Updated on any change |

### Business Actions

| Action | Description | Business Rules | Domain Event |
|--------|-------------|----------------|--------------|
| `Create(userId, personId, meetingDate, notes?)` | Record a 1:1 | PersonId and date required | `OneOnOneCreated` |
| `UpdateNotes(notes)` | Edit meeting notes | - | `OneOnOneNotesUpdated` |
| `AddTopic(topic)` | Record a topic discussed | Cannot be empty | `TopicAdded` |
| `AddActionItem(description, ownerId, dueDate?)` | Record an action item | Description required | `ActionItemAdded` |
| `CompleteActionItem(actionItemId)` | Mark action done | Item must exist and be open | `ActionItemCompleted` |
| `AddFollowUp(description)` | Something to raise next time | Description required | `FollowUpAdded` |
| `ResolveFollowUp(followUpId)` | Mark follow-up as addressed | Must exist and be unresolved | `FollowUpResolved` |
| `RecordMood(mood)` | Note the person's general sentiment | Valid mood value required | `MoodRecorded` |
| `PopulateFromExtraction(topics, actionItems, followUps)` | AI fills in structured data | Called after capture processing | `OneOnOnePopulatedFromCapture` |

### Invariants
- OneOnOne belongs to exactly one User
- Must be linked to exactly one Person
- MeetingDate cannot be in the future (you record meetings that happened)
- ActionItems are owned by either the user or the person
- A OneOnOne can only be linked to one Capture

---

## 8. Interview Aggregate

**Purpose:** An interview record linked to a candidate. Contains scorecard, transcript summary, AI analysis, stage, and outcome. Supports the hiring workflow from CV review through debrief.

### Entity: Interview

| Property | Type | Rules |
|----------|------|-------|
| Id | Guid | Immutable |
| UserId | Guid | FK -> User, immutable |
| CandidatePersonId | Guid | FK -> Person (must be candidate type) |
| Stage | InterviewStage (VO) | cv-review, technical, behavioural, leadership, debrief |
| InterviewDate | DateOnly | Required |
| Scorecard | Scorecard? (VO) | Structured evaluation |
| TranscriptSummary | string? | AI-generated summary of transcript |
| AiAnalysis | InterviewAnalysis? (VO) | AI strengths/concerns/recommendation |
| Outcome | InterviewOutcome? (VO) | advance, reject, hold, hire |
| SourceCaptureId | Guid? | FK -> Capture if from transcript |
| Notes | string? | Interviewer notes |
| CreatedAt | DateTimeOffset | Set on creation |
| UpdatedAt | DateTimeOffset | Updated on any change |

### Business Actions

| Action | Description | Business Rules | Domain Event |
|--------|-------------|----------------|--------------|
| `Create(userId, candidateId, stage, date)` | Record an interview | Candidate must be PersonType.candidate | `InterviewCreated` |
| `FillScorecard(scores)` | Complete the evaluation scorecard | All required dimensions must be scored | `ScorecardCompleted` |
| `RecordTranscriptSummary(summary)` | AI summarises transcript | Called after capture processing | `TranscriptSummarised` |
| `RecordAiAnalysis(strengths, concerns, recommendation)` | AI provides analysis | Called after capture processing | `InterviewAnalysed` |
| `RecordOutcome(outcome, notes?)` | Record the interview decision | Outcome required | `InterviewOutcomeRecorded` |
| `UpdateNotes(notes)` | Edit interviewer notes | - | `InterviewNotesUpdated` |

### Invariants
- Interview belongs to exactly one User
- CandidatePersonId must reference a Person with PersonType `candidate`
- Stage must be a valid InterviewStage
- Scorecard dimensions must all be scored before outcome can be recorded
- Outcome is immutable once recorded (decisions are final; create a new interview to reassess)

---

## 9. Observation Aggregate

**Purpose:** A tagged, timestamped note about a person used for performance evidence. Accumulated over time and surfaced during review cycles. Types include wins, growth moments, concerns, and feedback given.

### Entity: Observation

| Property | Type | Rules |
|----------|------|-------|
| Id | Guid | Immutable |
| UserId | Guid | FK -> User, immutable |
| PersonId | Guid | FK -> Person, required |
| Type | ObservationType (VO) | win, growth, concern, feedback-given |
| Content | string | Required, non-empty |
| OccurredAt | DateOnly | When the observation happened |
| SourceCaptureId | Guid? | FK -> Capture if extracted by AI |
| InitiativeId | Guid? | FK -> Initiative, optional context |
| Tags | List\<string\> | Free-form tags for filtering |
| CreatedAt | DateTimeOffset | Set on creation |

### Business Actions

| Action | Description | Business Rules | Domain Event |
|--------|-------------|----------------|--------------|
| `Create(userId, personId, type, content, occurredAt, initiativeId?, sourceId?)` | Record an observation | Content and PersonId required | `ObservationRecorded` |
| `UpdateContent(content)` | Edit observation text | Cannot be empty | `ObservationContentUpdated` |
| `Reclassify(newType)` | Change observation type | Must be valid type | `ObservationReclassified` |
| `AddTag(tag)` | Add a tag | Cannot be empty | - |
| `RemoveTag(tag)` | Remove a tag | Must exist | - |

### Invariants
- Observation belongs to exactly one User
- Must be linked to exactly one Person
- Content cannot be empty
- OccurredAt cannot be in the future
- Observations are effectively append-only evidence; deletion is discouraged (soft-delete only)

---

## 10. Goal Aggregate

**Purpose:** A development or performance goal belonging to a direct report. Tracks progress through check-ins over time. Supports quarterly review cycles.

### Entity: Goal

| Property | Type | Rules |
|----------|------|-------|
| Id | Guid | Immutable |
| UserId | Guid | FK -> User, immutable |
| PersonId | Guid | FK -> Person (must be direct-report) |
| Description | string | Required, non-empty |
| TargetDate | DateOnly | Required |
| Status | GoalStatus (VO) | active, achieved, missed, deferred |
| CheckIns | List\<GoalCheckIn\> (VO) | Chronological progress updates |
| CreatedAt | DateTimeOffset | Set on creation |
| UpdatedAt | DateTimeOffset | Updated on any change |

### Business Actions

| Action | Description | Business Rules | Domain Event |
|--------|-------------|----------------|--------------|
| `Create(userId, personId, description, targetDate)` | Set a goal for a direct report | Person must be direct-report | `GoalCreated` |
| `UpdateDescription(description)` | Refine goal text | Cannot be empty | `GoalDescriptionUpdated` |
| `UpdateTargetDate(newDate)` | Adjust deadline | Logs the change | `GoalTargetDateChanged` |
| `RecordCheckIn(notes, progressPercent?)` | Log a progress check-in | Notes required | `GoalCheckInRecorded` |
| `MarkAchieved(notes?)` | Goal is met | Must be `active` | `GoalAchieved` |
| `MarkMissed(reason?)` | Goal was not met | Must be `active` | `GoalMissed` |
| `Defer(newTargetDate, reason)` | Push goal out | Must be `active`, reason required | `GoalDeferred` |
| `Reactivate()` | Reopen a closed goal | Must be `achieved`, `missed`, or `deferred` | `GoalReactivated` |

### Goal Status State Machine

```
  ┌────────┐
  │ ACTIVE │
  └───┬────┘
      │
      ├── MarkAchieved() ──> [ ACHIEVED ]──┐
      │                                     │ Reactivate()
      ├── MarkMissed() ────> [ MISSED ]────┤──────────────> [ ACTIVE ]
      │                                     │
      └── Defer() ─────────> [ DEFERRED ]──┘
```

### Invariants
- Goal belongs to exactly one User
- PersonId must reference a Person with PersonType `direct-report`
- Description cannot be empty
- TargetDate is required
- CheckIns are append-only and chronologically ordered
- Status transitions must follow the state machine

---

## 11. Nudge Aggregate

**Purpose:** A recurring reminder or rhythm prompt. Not a task -- a system-generated prompt that appears in briefings and the queue. Examples: "Check in on Project X risks every Thursday", "Follow up with Sarah on career goals monthly."

### Entity: Nudge

| Property | Type | Rules |
|----------|------|-------|
| Id | Guid | Immutable |
| UserId | Guid | FK -> User, immutable |
| Description | string | Required, non-empty |
| Cadence | NudgeCadence (VO) | daily, weekly, fortnightly, monthly, custom cron |
| DayOfWeek | DayOfWeek? | For weekly cadence |
| LinkedPersonId | Guid? | FK -> Person, optional |
| LinkedInitiativeId | Guid? | FK -> Initiative, optional |
| IsActive | bool | Can be paused |
| LastTriggeredAt | DateTimeOffset? | When nudge last fired |
| NextDueAt | DateTimeOffset | Calculated from cadence |
| CreatedAt | DateTimeOffset | Set on creation |

### Business Actions

| Action | Description | Business Rules | Domain Event |
|--------|-------------|----------------|--------------|
| `Create(userId, description, cadence, linkedPersonId?, linkedInitiativeId?)` | Set up a recurring nudge | Description and cadence required | `NudgeCreated` |
| `UpdateDescription(description)` | Change the nudge text | Cannot be empty | `NudgeUpdated` |
| `ChangeCadence(cadence, dayOfWeek?)` | Adjust frequency | Recalculates NextDueAt | `NudgeCadenceChanged` |
| `Pause()` | Temporarily disable | Must be active | `NudgePaused` |
| `Resume()` | Re-enable a paused nudge | Must be paused, recalculates NextDueAt | `NudgeResumed` |
| `Trigger()` | System fires the nudge | Updates LastTriggeredAt, calculates next NextDueAt | `NudgeTriggered` |
| `Retire()` | Permanently deactivate | Nudge no longer fires | `NudgeRetired` |

### Invariants
- Nudge belongs to exactly one User
- Description cannot be empty
- NextDueAt must always be in the future (recalculated on trigger)
- A paused nudge does not trigger
- Retired nudges cannot be resumed

---

## 12. ChatThread Aggregate

**Purpose:** A conversation between the user and the AI assistant. Context-scoped: can be global or pinned to a specific person, initiative, or other entity. Messages include references to the source data used in AI responses.

### Entity: ChatThread

| Property | Type | Rules |
|----------|------|-------|
| Id | Guid | Immutable |
| UserId | Guid | FK -> User, immutable |
| Title | string? | Auto-generated or user-provided |
| ContextScope | ContextScope (VO) | global, person, initiative, capture, one-on-one |
| ContextEntityId | Guid? | The entity this thread is scoped to |
| Messages | List\<ChatMessage\> (VO) | Ordered conversation messages |
| CreatedAt | DateTimeOffset | Set on creation |
| LastMessageAt | DateTimeOffset | Updated with each message |

### Business Actions

| Action | Description | Business Rules | Domain Event |
|--------|-------------|----------------|--------------|
| `Create(userId, contextScope, contextEntityId?, title?)` | Start a new chat thread | - | `ChatThreadCreated` |
| `AddUserMessage(content)` | User sends a message | Content cannot be empty | `UserMessageSent` |
| `AddAssistantMessage(content, sourceReferences)` | AI responds with sources | Content required, sources tracked | `AssistantMessageReceived` |
| `UpdateTitle(title)` | Rename the thread | - | `ChatThreadRenamed` |
| `Archive()` | Archive old thread | Preserved but hidden from active list | `ChatThreadArchived` |

### Invariants
- ChatThread belongs to exactly one User
- Messages are append-only and chronologically ordered
- If ContextScope is not `global`, ContextEntityId must be set
- If ContextScope is `global`, ContextEntityId must be null
- Source references in AI messages must point to valid entities

---

## 13. Value Objects

### CommitmentDirection
```csharp
public enum CommitmentDirection
{
    MineToThem,   // "I owe X to person Y"
    TheirsToMe    // "Person Y owes me X"
}
```

### ProcessingStatus
```csharp
public enum ProcessingStatus
{
    Raw,
    Processing,
    Processed,
    Failed
}

// Behavior
bool CanTransitionTo(ProcessingStatus target) => (this, target) switch
{
    (Raw, Processing) => true,
    (Processing, Processed) => true,
    (Processing, Failed) => true,
    (Failed, Raw) => true,       // retry
    _ => false
};
```

### PersonType
```csharp
public enum PersonType
{
    DirectReport,
    Stakeholder,
    Candidate
}
```

### ObservationType
```csharp
public enum ObservationType
{
    Win,
    Growth,
    Concern,
    FeedbackGiven
}
```

### InterviewStage
```csharp
public enum InterviewStage
{
    CvReview,
    Technical,
    Behavioural,
    Leadership,
    Debrief
}
```

### InterviewOutcome
```csharp
public enum InterviewOutcome
{
    Advance,
    Reject,
    Hold,
    Hire
}
```

### CaptureType
```csharp
public enum CaptureType
{
    QuickNote,
    Transcript,
    MeetingNotes
}
```

### InitiativeStatus
```csharp
public enum InitiativeStatus
{
    Active,
    OnHold,
    Completed,
    Cancelled
}
```

### CommitmentStatus
```csharp
public enum CommitmentStatus
{
    Open,
    Completed,
    Cancelled
}
```

### DelegationStatus
```csharp
public enum DelegationStatus
{
    Assigned,
    InProgress,
    Completed,
    Blocked
}
```

### GoalStatus
```csharp
public enum GoalStatus
{
    Active,
    Achieved,
    Missed,
    Deferred
}
```

### Priority
```csharp
public enum Priority
{
    Low,
    Medium,
    High,
    Urgent
}
```

### NudgeCadence
```csharp
public record NudgeCadence(CadenceType Type, string? CronExpression = null)
{
    public DateTimeOffset CalculateNext(DateTimeOffset from, DayOfWeek? dayOfWeek);
}

public enum CadenceType
{
    Daily,
    Weekly,
    Fortnightly,
    Monthly,
    Custom   // uses CronExpression
}
```

### ContextScope
```csharp
public enum ContextScope
{
    Global,
    Person,
    Initiative,
    Capture,
    OneOnOne
}
```

### Mood
```csharp
public enum Mood
{
    Positive,
    Neutral,
    Concerned,
    Frustrated
}
```

### CareerDetails (embedded in Person)
```csharp
public record CareerDetails(
    string? Level,           // e.g. "Senior Engineer", "Staff"
    string? Aspirations,     // career direction in their own words
    List<string> GrowthAreas // areas for development
);
```

### CandidateDetails (embedded in Person)
```csharp
public record CandidateDetails(
    PipelineStatus Status,
    string? CvNotes,
    string? SourceChannel      // referral, inbound, agency, etc.
);

public enum PipelineStatus
{
    New,
    Screening,
    Interviewing,
    OfferStage,
    Hired,
    Rejected,
    Withdrawn
}
```

### KeyDecision (embedded in Initiative)
```csharp
public record KeyDecision(
    Guid Id,
    string Description,
    Guid MadeByPersonId,
    string Rationale,
    DateOnly DecisionDate,
    Guid? SourceCaptureId
);
```

### Risk (embedded in Initiative)
```csharp
public record Risk(
    Guid Id,
    string Description,
    RiskSeverity Severity,
    string? MitigationPlan,
    bool IsResolved,
    string? Resolution,
    DateTimeOffset RaisedAt,
    DateTimeOffset? ResolvedAt
);

public enum RiskSeverity { Low, Medium, High, Critical }
```

### RequirementSnapshot / DesignSnapshot (embedded in Initiative)
```csharp
public record RequirementSnapshot(
    string Content,
    string Source,              // which capture or meeting drove this update
    DateTimeOffset SnapshotAt
);

public record DesignSnapshot(
    string Content,
    string Source,
    DateTimeOffset SnapshotAt
);
```

### Dependency (embedded in Initiative)
```csharp
public record Dependency(
    Guid Id,
    string Team,
    string Description,
    DependencyStatus Status,
    string? Notes
);

public enum DependencyStatus { Waiting, InProgress, Resolved, Blocked }
```

### Milestone (embedded in Initiative)
```csharp
public record Milestone(
    Guid Id,
    string Title,
    DateOnly TargetDate,
    string? Description,
    bool IsComplete
);
```

### ActionItem (embedded in OneOnOne)
```csharp
public record ActionItem(
    Guid Id,
    string Description,
    Guid OwnerPersonId,       // user or the other person
    DateOnly? DueDate,
    bool IsComplete
);
```

### FollowUp (embedded in OneOnOne)
```csharp
public record FollowUp(
    Guid Id,
    string Description,
    bool IsResolved
);
```

### Scorecard (embedded in Interview)
```csharp
public record Scorecard(List<ScorecardDimension> Dimensions)
{
    bool IsComplete() => Dimensions.All(d => d.Score.HasValue);
    double AverageScore() => Dimensions.Average(d => d.Score ?? 0);
}

public record ScorecardDimension(
    string Name,          // e.g. "Technical Depth", "Communication"
    int? Score,           // 1-5
    string? Evidence      // supporting notes
);
```

### InterviewAnalysis (embedded in Interview)
```csharp
public record InterviewAnalysis(
    List<string> Strengths,
    List<string> Concerns,
    string Recommendation     // AI's hire/no-hire recommendation with reasoning
);
```

### AiExtraction (embedded in Capture)
```csharp
public record AiExtraction(
    string? Summary,
    List<ExtractedCommitment> Commitments,
    List<ExtractedDelegation> Delegations,
    List<ExtractedObservation> Observations,
    List<ExtractedDecision> Decisions,
    List<string> RisksIdentified,
    List<Guid> SuggestedPersonLinks,
    List<Guid> SuggestedInitiativeLinks,
    double ConfidenceScore          // 0.0 - 1.0
);
```

### GoalCheckIn (embedded in Goal)
```csharp
public record GoalCheckIn(
    string Notes,
    int? ProgressPercent,      // 0-100, optional
    DateTimeOffset RecordedAt
);
```

### ChatMessage (embedded in ChatThread)
```csharp
public record ChatMessage(
    Guid Id,
    ChatRole Role,              // user or assistant
    string Content,
    List<SourceReference>? Sources,
    DateTimeOffset SentAt
);

public enum ChatRole { User, Assistant }

public record SourceReference(
    string EntityType,          // "Capture", "Initiative", "Commitment", etc.
    Guid EntityId,
    string? Excerpt             // relevant snippet
);
```

### UserPreferences (embedded in User)
```csharp
public record UserPreferences(
    TimeOnly BriefingTime,         // when to generate daily briefing
    bool WeeklyBriefingEnabled,
    DayOfWeek WeeklyBriefingDay,
    int CommitmentWarningDays,     // days before due to surface warnings
    bool AutoProcessCaptures       // process captures immediately vs. batch
);
```

### AiProviderConfig (embedded in User)
```csharp
public record AiProviderConfig(
    AiProvider Provider,
    string ApiKey,                  // encrypted at rest
    string Model,                   // e.g. "claude-sonnet-4-20250514", "gpt-4o"
    int? MaxTokens
);

public enum AiProvider { Anthropic, OpenAI, Google }
```

### Email
```csharp
public record Email(string Value)
{
    // Invariant: Must match valid email format
    // Equality: Case-insensitive
}
```

---

## 14. Domain Services

### CaptureProcessingService

Orchestrates AI extraction from raw captures. Takes a raw capture, sends content to the AI provider, and creates linked entities from the extraction results.

| Method | Trigger | Action |
|--------|---------|--------|
| `ProcessCapture(captureId)` | `CaptureCreated` or `CaptureRetryRequested` | Sends raw content to AI, stores extraction, spawns entities |
| `SpawnEntities(captureId, extraction)` | After AI extraction | Creates Commitments, Delegations, Observations from extraction |
| `LinkEntities(captureId, extraction)` | After entity spawning | Links capture to suggested People and Initiatives |

**Processing Rules:**

| Step | Action | On Failure |
|------|--------|------------|
| 1. Transition to `processing` | `capture.BeginProcessing()` | Abort |
| 2. Call AI provider | Extract structured data from raw content | `capture.FailProcessing(reason)` |
| 3. Store extraction | `capture.CompleteProcessing(extraction)` | `capture.FailProcessing(reason)` |
| 4. Spawn commitments | Create Commitment per extracted commitment | Log error, continue |
| 5. Spawn delegations | Create Delegation per extracted delegation | Log error, continue |
| 6. Spawn observations | Create Observation per extracted observation | Log error, continue |
| 7. Link to people | `capture.LinkToPerson()` for each | Log error, continue |
| 8. Link to initiatives | `capture.LinkToInitiative()` for each | Log error, continue |
| 9. Trigger brief updates | Raise `CaptureProcessed` for BriefUpdateService | - |

### BriefUpdateService

Updates initiative living briefs when new data arrives. Listens for processed captures linked to initiatives and regenerates the AI summary.

| Method | Trigger | Action |
|--------|---------|--------|
| `UpdateBrief(initiativeId)` | `CaptureProcessed` (when linked to initiative) | Regenerates AI summary from all linked captures |
| `IncrementalUpdate(initiativeId, captureId)` | `CaptureLinkedToInitiative` | Merges new capture data into existing brief |
| `RecordDecisionFromCapture(initiativeId, decision)` | During capture processing | Adds extracted decision to initiative |
| `RecordRiskFromCapture(initiativeId, risk)` | During capture processing | Adds extracted risk to initiative |

**Update Rules:**
- AI summary considers all linked captures, not just the latest
- Previous summary is used as context for the update (incremental, not from scratch)
- Requirements and design snapshots are appended, never overwritten
- Decisions extracted from captures include the source capture reference

### BriefingService

Generates daily and weekly briefings. Aggregates data across all aggregates to build the user's morning command centre.

| Method | Trigger | Action |
|--------|---------|--------|
| `GenerateDailyBriefing(userId)` | Scheduled at user's preferred time | Builds today's briefing |
| `GenerateWeeklyBriefing(userId)` | Scheduled on preferred day | Builds week summary and lookahead |
| `GenerateOneOnOnePrep(userId, personId)` | On demand or before scheduled 1:1 | Builds 1:1 prep sheet |

**Daily Briefing Composition:**

| Section | Source |
|---------|--------|
| Commitments due today/this week | Commitment (open, due soon) |
| Overdue commitments | Commitment (open, past due) |
| Delegations needing follow-up | Delegation (overdue or stale) |
| Initiative risks | Initiative (open risks, high severity) |
| Upcoming 1:1 prep summaries | OneOnOne (recent history per person), Goal (active) |
| Nudges firing today | Nudge (NextDueAt = today) |
| Unprocessed captures | Capture (status = raw) |
| Interviews scheduled | Interview (upcoming) |

### CommitmentTrackingService

Monitors commitment deadlines and surfaces upcoming or overdue items.

| Method | Trigger | Action |
|--------|---------|--------|
| `CheckOverdue(userId)` | Scheduled daily | Finds open commitments past due date, raises `CommitmentBecameOverdue` |
| `GetUpcoming(userId, days)` | On demand | Returns commitments due within N days |
| `GetByPerson(userId, personId)` | On demand | Returns all commitments involving a person |
| `GetByInitiative(userId, initiativeId)` | On demand | Returns all commitments linked to an initiative |

**Rules:**
- A commitment is overdue when `DueDate < today` and `Status = Open`
- Overdue commitments are surfaced in briefings every day until resolved
- Commitments due within `CommitmentWarningDays` (from UserPreferences) appear as warnings

### QueuePrioritizationService

Builds the "My Queue" view -- a single prioritized list of everything needing the user's attention.

| Method | Trigger | Action |
|--------|---------|--------|
| `BuildQueue(userId, filters?)` | On demand | Assembles and ranks queue items |
| `GetQueueCount(userId)` | On demand | Returns total pending items |

**Queue Item Sources and Priority Weights:**

| Source | Item Type | Priority Signal |
|--------|-----------|-----------------|
| Commitment | Overdue commitment (mine-to-them) | Critical -- you broke a promise |
| Commitment | Overdue commitment (theirs-to-me) | High -- needs follow-up |
| Commitment | Due today | High |
| Commitment | Due this week | Medium |
| Delegation | Overdue delegation | High |
| Delegation | Blocked delegation | High -- needs unblocking |
| Delegation | Stale delegation (no follow-up in N days) | Medium |
| Capture | Unprocessed capture | Medium -- needs triage |
| Capture | Processed but unconfirmed | Low |
| Initiative | High/critical open risk | Medium |
| Nudge | Nudge due today | Low |
| Interview | Interview scheduled today | High -- needs prep |

**Filtering:**
- By person: show only items involving a specific person
- By initiative: show only items linked to an initiative
- By urgency: critical, high, medium, low
- By type: commitments, delegations, captures, etc.
- "Can I delegate this?": filters to items the user could potentially hand off

---

## 15. Domain Events Summary

| Event | Raised By | Key Payload |
|-------|-----------|-------------|
| `UserRegistered` | User.Register() | UserId, Email |
| `AiProviderConfigured` | User.ConfigureAiProvider() | UserId, Provider |
| `PersonCreated` | Person.Create() | PersonId, UserId, PersonType |
| `PersonTypeChanged` | Person.ChangeType() | PersonId, OldType, NewType |
| `CandidatePipelineAdvanced` | Person.AdvanceCandidatePipeline() | PersonId, OldStatus, NewStatus |
| `InitiativeCreated` | Initiative.Create() | InitiativeId, UserId, Title |
| `InitiativeStatusChanged` | Initiative.ChangeStatus() | InitiativeId, OldStatus, NewStatus |
| `InitiativeSummaryRefreshed` | Initiative.RefreshSummary() | InitiativeId |
| `DecisionRecorded` | Initiative.RecordDecision() | InitiativeId, DecisionId |
| `RiskRaised` | Initiative.RaiseRisk() | InitiativeId, RiskId, Severity |
| `RiskResolved` | Initiative.ResolveRisk() | InitiativeId, RiskId |
| `RequirementsUpdated` | Initiative.UpdateRequirements() | InitiativeId, SnapshotIndex |
| `CaptureCreated` | Capture.Create() | CaptureId, UserId, CaptureType |
| `CaptureProcessingStarted` | Capture.BeginProcessing() | CaptureId |
| `CaptureProcessed` | Capture.CompleteProcessing() | CaptureId, LinkedInitiativeIds |
| `CaptureProcessingFailed` | Capture.FailProcessing() | CaptureId, Reason |
| `CaptureLinkedToInitiative` | Capture.LinkToInitiative() | CaptureId, InitiativeId |
| `CommitmentCreated` | Commitment.Create() | CommitmentId, Direction, PersonId |
| `CommitmentCompleted` | Commitment.Complete() | CommitmentId |
| `CommitmentBecameOverdue` | Commitment.MarkOverdue() | CommitmentId, DueDate |
| `DelegationCreated` | Delegation.Create() | DelegationId, DelegatePersonId |
| `DelegationCompleted` | Delegation.MarkCompleted() | DelegationId |
| `DelegationBlocked` | Delegation.MarkBlocked() | DelegationId, Reason |
| `OneOnOneCreated` | OneOnOne.Create() | OneOnOneId, PersonId, MeetingDate |
| `InterviewCreated` | Interview.Create() | InterviewId, CandidatePersonId, Stage |
| `InterviewOutcomeRecorded` | Interview.RecordOutcome() | InterviewId, Outcome |
| `ObservationRecorded` | Observation.Create() | ObservationId, PersonId, Type |
| `GoalCreated` | Goal.Create() | GoalId, PersonId |
| `GoalAchieved` | Goal.MarkAchieved() | GoalId |
| `GoalCheckInRecorded` | Goal.RecordCheckIn() | GoalId, ProgressPercent |
| `NudgeCreated` | Nudge.Create() | NudgeId, Cadence |
| `NudgeTriggered` | Nudge.Trigger() | NudgeId, NextDueAt |
| `ChatThreadCreated` | ChatThread.Create() | ThreadId, ContextScope |
| `UserMessageSent` | ChatThread.AddUserMessage() | ThreadId, MessageId |
| `AssistantMessageReceived` | ChatThread.AddAssistantMessage() | ThreadId, MessageId, SourceCount |

---

## 16. Cross-Cutting Business Rules

### User Scoping (Multi-Tenancy)
- All aggregates belong to exactly one User via `UserId`
- Users can only see and modify their own data
- Cross-user data access returns "not found" (security through obscurity at the domain level)
- All queries are implicitly scoped to the authenticated user

### Raw Content Preservation
- Capture.RawContent is immutable after creation
- The original source material is always preserved, even after AI extraction
- This enables re-processing with improved AI models and powers the chat feature
- AI summaries supplement but never replace raw content

### AI Extraction Spawning
- A single Capture can spawn multiple Commitments, Observations, and Delegations
- Spawned entities maintain a reference back to their source Capture
- Users can confirm, modify, or discard AI-generated entities during daily close-out
- AI extraction confidence scores guide which items need human review

### Initiative Brief Auto-Update
- When a Capture is processed and linked to an Initiative, the brief is refreshed
- The refresh is incremental: new data is merged into the existing summary
- Requirements and design direction maintain full snapshot history
- Key decisions and risks extracted from captures are appended automatically

### Commitment Lifecycle
- Commitments are surfaced in briefings and queue every day until resolved
- Overdue commitments escalate in priority within the queue
- Both directions (mine-to-them, theirs-to-me) are tracked equally
- Completing a commitment records the completion timestamp for audit

### Evidence Accumulation
- Observations, 1:1 records, goal check-ins, and commitments build a person's evidence profile
- This evidence is queryable via AI chat ("What has Sarah delivered this quarter?")
- Evidence is time-stamped and source-linked for traceability
- At review time, the system can generate a draft summary from accumulated evidence

---

## 17. Recommended Project Structure

```
MyWorkCompanion.Domain/
├── Aggregates/
│   ├── Users/
│   │   ├── User.cs
│   │   └── Events/
│   ├── People/
│   │   ├── Person.cs
│   │   └── Events/
│   ├── Initiatives/
│   │   ├── Initiative.cs
│   │   └── Events/
│   ├── Captures/
│   │   ├── Capture.cs
│   │   └── Events/
│   ├── Commitments/
│   │   ├── Commitment.cs
│   │   └── Events/
│   ├── Delegations/
│   │   ├── Delegation.cs
│   │   └── Events/
│   ├── OneOnOnes/
│   │   ├── OneOnOne.cs
│   │   └── Events/
│   ├── Interviews/
│   │   ├── Interview.cs
│   │   └── Events/
│   ├── Observations/
│   │   ├── Observation.cs
│   │   └── Events/
│   ├── Goals/
│   │   ├── Goal.cs
│   │   └── Events/
│   ├── Nudges/
│   │   ├── Nudge.cs
│   │   └── Events/
│   └── ChatThreads/
│       ├── ChatThread.cs
│       └── Events/
├── ValueObjects/
│   ├── CommitmentDirection.cs
│   ├── ProcessingStatus.cs
│   ├── PersonType.cs
│   ├── ObservationType.cs
│   ├── InterviewStage.cs
│   ├── InterviewOutcome.cs
│   ├── CaptureType.cs
│   ├── InitiativeStatus.cs
│   ├── CommitmentStatus.cs
│   ├── DelegationStatus.cs
│   ├── GoalStatus.cs
│   ├── Priority.cs
│   ├── NudgeCadence.cs
│   ├── ContextScope.cs
│   ├── Mood.cs
│   ├── CareerDetails.cs
│   ├── CandidateDetails.cs
│   ├── PipelineStatus.cs
│   ├── KeyDecision.cs
│   ├── Risk.cs
│   ├── Dependency.cs
│   ├── Milestone.cs
│   ├── ActionItem.cs
│   ├── FollowUp.cs
│   ├── Scorecard.cs
│   ├── InterviewAnalysis.cs
│   ├── AiExtraction.cs
│   ├── GoalCheckIn.cs
│   ├── ChatMessage.cs
│   ├── SourceReference.cs
│   ├── UserPreferences.cs
│   ├── AiProviderConfig.cs
│   └── Email.cs
├── Services/
│   ├── CaptureProcessingService.cs
│   ├── BriefUpdateService.cs
│   ├── BriefingService.cs
│   ├── CommitmentTrackingService.cs
│   └── QueuePrioritizationService.cs
├── Events/
│   ├── IDomainEvent.cs
│   └── DomainEventBase.cs
└── Common/
    ├── Entity.cs
    ├── AggregateRoot.cs
    └── ValueObject.cs
```
