## Context

Mental Metal has Person and Initiative management (Tier 1) in place. Commitment tracking is a core Tier 2 capability that lets users record bidirectional promises -- things they owe to others and things others owe to them. The Commitment aggregate is well-defined in `design/domain-model.md` and references Person (required) and optionally Initiative and Capture by ID.

### Dependencies

- `person-management` (Tier 1) -- PersonId is a required field on Commitment
- `initiative-management` (Tier 1) -- InitiativeId is an optional link

## Goals / Non-Goals

**Goals:**

- Implement the Commitment aggregate with rich domain behaviour following established DDD patterns
- Support bidirectional tracking via CommitmentDirection (MineToThem, TheirsToMe)
- Implement the status lifecycle (Open -> Completed/Cancelled, Reopen from either terminal state)
- Support overdue detection based on DueDate
- Provide list views with filtering by person, initiative, direction, status, and overdue state
- Follow the same vertical slice, CQRS patterns established by person-management and initiative-management

**Non-Goals:**

- AI extraction of commitments from captures (capture-ai-extraction spec)
- Nudge/reminder scheduling for overdue commitments (nudges-rhythms spec)
- Briefing integration showing commitments due today (daily-weekly-briefing spec)
- Queue-based prioritisation of commitments (my-queue spec)
- Validating that PersonId/InitiativeId actually exist (eventual consistency per DDD)

## Decisions

### 1. Commitment aggregate follows existing DDD patterns

**Decision:** Model the Commitment aggregate identically to Person and Initiative -- rich domain entity with factory method, business actions, value objects, and domain events.

**Rationale:** Consistency with Tier 1 patterns. The domain model is well-defined.

**Alternatives considered:**
- Anemic model -- rejected per project DDD principles

### 2. Value objects for Direction and Status

**Decision:** Implement `CommitmentDirection` as a C# enum (`MineToThem`, `TheirsToMe`) and `CommitmentStatus` as a C# enum (`Open`, `Completed`, `Cancelled`). Status transitions are enforced by the aggregate.

**Rationale:** Same approach as CaptureType/ProcessingStatus and PersonType. Simple enums with aggregate-enforced transitions.

### 3. Overdue detection as a domain method

**Decision:** Implement `MarkOverdue()` as a business action on the aggregate that checks `DueDate < today && Status == Open`. Overdue is NOT a separate status -- it is a computed concern. The `CommitmentBecameOverdue` event is raised for notification purposes. A query-time computed property `IsOverdue` is used for filtering.

**Rationale:** Overdue is a temporal concern, not a status transition. A commitment can be open AND overdue. Keeping it as a computed property avoids a complex status matrix. The `MarkOverdue()` action exists for a future background job to raise domain events.

**Alternatives considered:**
- Overdue as a status value -- rejected because it creates a parallel status axis (open+overdue, but never completed+overdue)
- Pure query-time computation only -- rejected because we want domain events for notification integration

### 4. PersonId is required, not validated cross-aggregate

**Decision:** PersonId is a required `Guid` on the Commitment, but the Commitment aggregate does not validate that the Person exists. This is consistent with DDD aggregate boundaries.

**Rationale:** Cross-aggregate validation would require a domain service or application-layer check. The application handler can optionally verify the person exists before creating the commitment, but this is not a domain invariant.

### 5. SourceCaptureId for traceability

**Decision:** An optional `SourceCaptureId` field tracks which Capture spawned this commitment. This field is set at creation and is immutable. It is not used in this spec but enables `capture-ai-extraction` to link extracted commitments back to their source.

**Rationale:** Forward-compatible design. The field is cheap to add now and avoids a migration later.

### 6. API design follows established conventions

**Decision:** Endpoints under `/api/commitments` following existing patterns.

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/commitments` | POST | Create a new commitment |
| `/api/commitments` | GET | List user's commitments (filterable) |
| `/api/commitments/{id}` | GET | Get commitment by ID |
| `/api/commitments/{id}` | PUT | Update description, notes |
| `/api/commitments/{id}/complete` | POST | Mark as completed |
| `/api/commitments/{id}/cancel` | POST | Mark as cancelled |
| `/api/commitments/{id}/reopen` | POST | Reopen a completed/cancelled commitment |
| `/api/commitments/{id}/due-date` | PUT | Update due date |
| `/api/commitments/{id}/link-initiative` | POST | Link to an initiative |

**Rationale:** Status transitions as POST actions (not PATCH on status field) because they are distinct business actions with their own domain events and rules.

### 7. Frontend uses signal-based state and PrimeNG components

**Decision:** Commitment UI consists of a commitment list page (PrimeNG DataView with filters for direction, status, person, overdue), a create/edit dialog (PrimeNG Dialog with form fields), and inline status actions. All state via Angular signals.

**Rationale:** Follows Angular 21 conventions and PrimeNG-first component selection.

## Risks / Trade-offs

- **[Orphaned PersonId]** If a linked Person is deleted, the Commitment retains a stale PersonId. **Mitigation:** Person archival (soft delete) means the person record still exists. Display can show "archived person" if needed.

- **[Overdue detection timing]** Without a background job, overdue detection is purely query-time. Commitments won't raise `CommitmentBecameOverdue` events until a future spec adds a scheduled check. **Mitigation:** Query-time `IsOverdue` computed property handles UI display. Event-based detection is a future concern.

- **[No cascade on Capture deletion]** If a source Capture is deleted, the SourceCaptureId becomes orphaned. **Mitigation:** Captures are not deleted (only discarded), so this is unlikely.

## Open Questions

_(none)_
