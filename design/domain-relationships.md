# My Work Companion Domain Relationships

Mermaid diagrams showing aggregate relationships, processing flows, and composition views.

---

## 1. Aggregate Relationship Map

Shows which aggregates reference which via ID. All aggregates are scoped to a User (UserId omitted from arrows for clarity). Solid arrows indicate required references; dashed arrows indicate optional references.

```mermaid
graph TB
    User["<b>User</b><br/>Profile, Preferences,<br/>AI Provider Config"]

    Person["<b>Person</b><br/>Direct Reports,<br/>Stakeholders, Candidates"]

    Initiative["<b>Initiative</b><br/>Living Brief,<br/>Decisions, Risks"]

    Capture["<b>Capture</b><br/>Raw Input,<br/>AI Extraction"]

    Commitment["<b>Commitment</b><br/>Bidirectional<br/>Promises"]

    Delegation["<b>Delegation</b><br/>Assigned Work<br/>+ Follow-up"]

    OneOnOne["<b>OneOnOne</b><br/>1:1 Meeting<br/>Records"]

    Interview["<b>Interview</b><br/>Scorecard,<br/>AI Analysis"]

    Observation["<b>Observation</b><br/>Performance<br/>Evidence"]

    Goal["<b>Goal</b><br/>Development<br/>Targets"]

    Nudge["<b>Nudge</b><br/>Recurring<br/>Reminders"]

    ChatThread["<b>ChatThread</b><br/>AI Conversation<br/>+ Sources"]

    %% Required references (solid)
    Commitment -->|PersonId| Person
    Delegation -->|DelegatePersonId| Person
    OneOnOne -->|PersonId| Person
    Interview -->|CandidatePersonId| Person
    Observation -->|PersonId| Person
    Goal -->|PersonId| Person

    %% Optional references (dashed)
    Commitment -.->|InitiativeId?| Initiative
    Commitment -.->|SourceCaptureId?| Capture
    Delegation -.->|InitiativeId?| Initiative
    Delegation -.->|SourceCaptureId?| Capture
    OneOnOne -.->|SourceCaptureId?| Capture
    Interview -.->|SourceCaptureId?| Capture
    Observation -.->|SourceCaptureId?| Capture
    Observation -.->|InitiativeId?| Capture

    Capture -.->|LinkedPersonIds| Person
    Capture -.->|LinkedInitiativeIds| Initiative
    Capture -.->|SpawnedCommitmentIds| Commitment
    Capture -.->|SpawnedDelegationIds| Delegation
    Capture -.->|SpawnedObservationIds| Observation

    Initiative -.->|LinkedPersonIds| Person

    Nudge -.->|LinkedPersonId?| Person
    Nudge -.->|LinkedInitiativeId?| Initiative

    ChatThread -.->|ContextEntityId?| Person
    ChatThread -.->|ContextEntityId?| Initiative
```

### Aggregate Boundary Rules

| Rule | Description |
|------|-------------|
| **No embedded aggregates** | Aggregates reference each other only by ID, never by direct containment |
| **Value objects are embedded** | KeyDecision, Risk, ActionItem, Scorecard etc. live inside their parent aggregate |
| **User scoping** | Every aggregate carries a `UserId` -- this is the tenancy boundary |
| **Capture is the bridge** | Capture links raw input to all other aggregates via spawned/linked IDs |
| **Person is the hub** | Most aggregates reference Person -- it is the central relationship entity |

---

## 2. Capture Processing Flow

Shows the journey from raw input through AI extraction to linked domain entities.

```mermaid
sequenceDiagram
    actor User
    participant C as Capture
    participant CPS as CaptureProcessingService
    participant AI as AI Provider
    participant BUS as BriefUpdateService

    User->>C: Create(rawContent, type)
    Note over C: Status = Raw
    C-->>CPS: CaptureCreated event

    CPS->>C: BeginProcessing()
    Note over C: Status = Processing

    CPS->>AI: Extract structured data<br/>from raw content
    AI-->>CPS: AiExtraction result

    alt Extraction succeeds
        CPS->>C: CompleteProcessing(extraction)
        Note over C: Status = Processed

        par Spawn entities
            CPS->>CPS: Create Commitment(s)
            CPS->>CPS: Create Delegation(s)
            CPS->>CPS: Create Observation(s)
        end

        par Link entities
            CPS->>C: LinkToPerson(personId) [for each]
            CPS->>C: LinkToInitiative(initiativeId) [for each]
        end

        C-->>BUS: CaptureProcessed event
        BUS->>BUS: Update linked initiative briefs

    else Extraction fails
        CPS->>C: FailProcessing(reason)
        Note over C: Status = Failed
        Note over C: Available for retry
    end
```

### Extraction Data Flow

```mermaid
flowchart LR
    Raw["Raw Content<br/>(text, transcript,<br/>meeting notes)"]

    Raw --> AI["AI Extraction"]

    AI --> Commitments["Commitments<br/>(mine-to-them,<br/>theirs-to-me)"]
    AI --> Delegations["Delegations<br/>(assigned tasks)"]
    AI --> Observations["Observations<br/>(wins, growth,<br/>concerns)"]
    AI --> Decisions["Key Decisions<br/>(who/when/why)"]
    AI --> Risks["Risks Identified"]
    AI --> PersonLinks["Person Links<br/>(mentioned people)"]
    AI --> InitLinks["Initiative Links<br/>(related projects)"]

    Commitments --> CommitmentAgg["Commitment<br/>Aggregate"]
    Delegations --> DelegationAgg["Delegation<br/>Aggregate"]
    Observations --> ObservationAgg["Observation<br/>Aggregate"]
    Decisions --> InitiativeAgg["Initiative<br/>Aggregate"]
    Risks --> InitiativeAgg
    PersonLinks --> CaptureAgg["Capture<br/>Aggregate<br/>(LinkedPersonIds)"]
    InitLinks --> CaptureAgg2["Capture<br/>Aggregate<br/>(LinkedInitiativeIds)"]
```

---

## 3. Initiative Brief Update Flow

Shows how the living brief stays current as new data arrives from multiple sources.

```mermaid
sequenceDiagram
    participant C as Capture
    participant BUS as BriefUpdateService
    participant I as Initiative
    participant AI as AI Provider

    Note over C: CaptureProcessed event fires<br/>(linked to Initiative)

    C-->>BUS: CaptureProcessed(captureId, linkedInitiativeIds)

    loop For each linked Initiative
        BUS->>I: Load current state<br/>(summary, decisions, risks,<br/>requirements, design)

        BUS->>BUS: Gather all linked captures<br/>for this initiative

        BUS->>AI: Generate updated summary<br/>(previous summary + new capture<br/>+ all linked context)

        AI-->>BUS: Updated summary,<br/>extracted decisions,<br/>extracted risks,<br/>requirement changes,<br/>design changes

        BUS->>I: RefreshSummary(newSummary)

        opt New decisions found
            BUS->>I: RecordDecision(description,<br/>madeBy, rationale, date)
        end

        opt New risks found
            BUS->>I: RaiseRisk(description,<br/>severity, mitigation)
        end

        opt Requirements changed
            BUS->>I: UpdateRequirements(content, source)
            Note over I: Appends snapshot<br/>to history
        end

        opt Design direction changed
            BUS->>I: UpdateDesignDirection(content, source)
            Note over I: Appends snapshot<br/>to history
        end
    end
```

### Brief Composition

```mermaid
flowchart TB
    subgraph Initiative Brief
        Summary["AI Summary<br/>(regenerated on each update)"]
        Decisions["Key Decisions<br/>(append-only log)"]
        Risks["Open Risks<br/>(raised/resolved)"]
        Reqs["Requirements<br/>(snapshot history)"]
        Design["Design Direction<br/>(snapshot history)"]
        Deps["Dependencies<br/>(cross-team)"]
        Miles["Milestones<br/>(key dates)"]
    end

    Captures["Linked Captures"] --> Summary
    Captures --> Decisions
    Captures --> Risks
    Captures --> Reqs
    Captures --> Design

    Commitments["Linked Commitments"] --> Summary
    Delegations["Linked Delegations"] --> Summary

    ManualInput["Manual User Input"] --> Deps
    ManualInput --> Miles
    ManualInput --> Risks
    ManualInput --> Decisions
```

---

## 4. My Queue Composition

Shows what feeds into the prioritized queue view and how items are ranked.

```mermaid
flowchart TB
    subgraph Sources
        COM["Commitment<br/>(open, due/overdue)"]
        DEL["Delegation<br/>(overdue, blocked, stale)"]
        CAP["Capture<br/>(unprocessed, unconfirmed)"]
        INIT["Initiative<br/>(high/critical risks)"]
        NUD["Nudge<br/>(due today)"]
        INT["Interview<br/>(scheduled today)"]
    end

    subgraph QueuePrioritizationService
        RANK["Priority Ranking<br/>Engine"]
    end

    subgraph Queue Output
        CRIT["CRITICAL<br/>- Overdue mine-to-them commitments"]
        HIGH["HIGH<br/>- Overdue theirs-to-me commitments<br/>- Commitments due today<br/>- Overdue delegations<br/>- Blocked delegations<br/>- Today's interviews"]
        MED["MEDIUM<br/>- Commitments due this week<br/>- Stale delegations<br/>- Unprocessed captures<br/>- High/critical initiative risks"]
        LOW["LOW<br/>- Unconfirmed captures<br/>- Nudges due today"]
    end

    COM --> RANK
    DEL --> RANK
    CAP --> RANK
    INIT --> RANK
    NUD --> RANK
    INT --> RANK

    RANK --> CRIT
    RANK --> HIGH
    RANK --> MED
    RANK --> LOW
```

### Queue Filtering

```mermaid
flowchart LR
    Queue["Full Queue"] --> FilterPerson["By Person<br/>Show items involving<br/>a specific person"]
    Queue --> FilterInit["By Initiative<br/>Show items linked<br/>to an initiative"]
    Queue --> FilterUrgency["By Urgency<br/>Critical / High /<br/>Medium / Low"]
    Queue --> FilterType["By Type<br/>Commitments / Delegations /<br/>Captures / etc."]
    Queue --> FilterDelegate["Can I delegate this?<br/>Items suitable for<br/>hand-off"]
```

---

## 5. Briefing Generation Flow

Shows how the daily and weekly briefings are assembled from multiple data sources.

```mermaid
sequenceDiagram
    participant SCH as Scheduler
    participant BS as BriefingService
    participant COM as Commitments
    participant DEL as Delegations
    participant INIT as Initiatives
    participant PERS as People + OneOnOnes
    participant GOAL as Goals
    participant NUD as Nudges
    participant CAP as Captures
    participant INT as Interviews
    participant AI as AI Provider

    SCH->>BS: GenerateDailyBriefing(userId)<br/>(at user's preferred time)

    par Gather data
        BS->>COM: Get overdue + due today/this week
        COM-->>BS: Commitment list

        BS->>DEL: Get overdue + blocked + stale
        DEL-->>BS: Delegation list

        BS->>INIT: Get active initiatives<br/>with open high/critical risks
        INIT-->>BS: Initiative + risk list

        BS->>PERS: Get people with<br/>1:1s today/this week
        PERS-->>BS: Person + OneOnOne history

        BS->>GOAL: Get active goals<br/>for upcoming 1:1 people
        GOAL-->>BS: Goal list

        BS->>NUD: Get nudges due today
        NUD-->>BS: Nudge list

        BS->>CAP: Get unprocessed captures
        CAP-->>BS: Capture list

        BS->>INT: Get interviews scheduled<br/>today/this week
        INT-->>BS: Interview list
    end

    BS->>AI: Generate natural language<br/>briefing from aggregated data

    AI-->>BS: Formatted briefing

    Note over BS: Briefing sections:<br/>1. Urgent items (overdue)<br/>2. Today's commitments<br/>3. 1:1 prep (per person)<br/>4. Initiative risks<br/>5. Delegation follow-ups<br/>6. Upcoming interviews<br/>7. Nudges<br/>8. Captures needing triage
```

### Weekly Briefing Additional Sections

```mermaid
flowchart TB
    subgraph Weekly Briefing Extras
        RETRO["Week in Review<br/>- Commitments completed<br/>- Delegations finished<br/>- Decisions made<br/>- Goals progressed"]

        LOOK["Week Ahead<br/>- Commitments due next week<br/>- Scheduled 1:1s with prep<br/>- Upcoming milestones<br/>- Interviews scheduled"]

        HEALTH["Initiative Health<br/>- Per-initiative status summary<br/>- New risks since last week<br/>- Requirement/design changes<br/>- Dependency status"]

        PEOPLE["People Summary<br/>- Observations recorded this week<br/>- Goal check-ins due<br/>- Candidates in pipeline"]
    end

    COM_DATA["Commitment data"] --> RETRO
    COM_DATA --> LOOK
    DEL_DATA["Delegation data"] --> RETRO
    INIT_DATA["Initiative data"] --> HEALTH
    OBS_DATA["Observation data"] --> PEOPLE
    GOAL_DATA["Goal data"] --> PEOPLE
    INT_DATA["Interview data"] --> PEOPLE
    MILE_DATA["Milestone data"] --> LOOK
    O1O_DATA["OneOnOne data"] --> LOOK
```

### 1:1 Prep Sheet Composition

```mermaid
flowchart TB
    subgraph "1:1 Prep for [Person]"
        PREV["Previous 1:1<br/>- Open action items<br/>- Unresolved follow-ups"]
        COMMITS["Open Commitments<br/>- Mine to them<br/>- Theirs to me"]
        DELS["Active Delegations<br/>- Status of assigned work"]
        GOALS["Active Goals<br/>- Progress check-ins<br/>- Target dates"]
        OBS["Recent Observations<br/>- Wins to acknowledge<br/>- Concerns to discuss<br/>- Growth moments"]
        CONTEXT["Recent Context<br/>- Captures mentioning them<br/>- Initiative updates<br/>involving them"]
    end

    OneOnOneHistory["OneOnOne<br/>history"] --> PREV
    CommitmentStore["Commitment<br/>store"] --> COMMITS
    DelegationStore["Delegation<br/>store"] --> DELS
    GoalStore["Goal<br/>store"] --> GOALS
    ObservationStore["Observation<br/>store"] --> OBS
    CaptureStore["Capture<br/>store"] --> CONTEXT
    InitiativeStore["Initiative<br/>store"] --> CONTEXT
```

---

## 6. Entity Lifecycle Overview

Shows how the main entities flow through the system over time.

```mermaid
stateDiagram-v2
    state "Capture Lifecycle" as CL {
        [*] --> Raw: User creates capture
        Raw --> Processing: AI begins extraction
        Processing --> Processed: Extraction complete
        Processing --> Failed: Extraction error
        Failed --> Raw: Retry requested
        Processed --> Confirmed: User confirms
        Processed --> Discarded: User discards (close-out)
    }

    state "Commitment Lifecycle" as COL {
        [*] --> Open: Created (manual or from capture)
        Open --> Completed: Fulfilled
        Open --> Cancelled: No longer needed
        Open --> Overdue: Past due date (system)
        Overdue --> Completed: Fulfilled late
        Overdue --> Cancelled: Abandoned
        Completed --> Open: Reopened
        Cancelled --> Open: Reopened
    }

    state "Delegation Lifecycle" as DL {
        [*] --> Assigned: User assigns work
        Assigned --> InProgress: Work started
        Assigned --> Completed: Done immediately
        Assigned --> Blocked: Blocker raised
        InProgress --> Completed: Work finished
        InProgress --> Blocked: Blocker raised
        Blocked --> InProgress: Unblocked
    }

    state "Initiative Lifecycle" as IL {
        [*] --> Active: Created
        Active --> OnHold: Paused
        OnHold --> Active: Resumed
        Active --> Completed: Finished
        Active --> Cancelled: Abandoned
    }
```
