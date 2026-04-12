## Context

Mental Metal currently supports managing People and Initiatives (Tier 1). The next step is enabling users to capture raw text input -- the primary data entry point for the system. Captures are the raw material from which AI extraction (a separate spec) will later derive commitments, delegations, and observations.

This design covers the `capture-text` spec, which handles creating, storing, viewing, and managing raw text captures with a processing status lifecycle. It does NOT cover AI extraction or audio capture.

### Dependencies

- `user-auth-tenancy` (Tier 1) -- for UserId scoping and authentication

## Goals / Non-Goals

**Goals:**

- Implement the Capture aggregate with rich domain behaviour following established DDD patterns
- Support the full processing status state machine (Raw -> Processing -> Processed -> Failed, with retry)
- Allow captures to be optionally linked to People and Initiatives by ID
- Provide a low-friction quick-capture UI and a filterable capture list
- Follow the same vertical slice, CQRS patterns established by person-management and initiative-management

**Non-Goals:**

- AI extraction pipeline (capture-ai-extraction spec)
- Audio recording and transcription (capture-audio spec)
- Daily close-out triage flow (daily-close-out spec)
- Validating that linked PersonIds/InitiativeIds actually exist (eventual consistency; the Capture aggregate stores IDs only)
- Full-text search across captures (future enhancement)

## Decisions

### 1. Capture aggregate follows existing DDD patterns

**Decision:** Model the Capture aggregate identically to Person and Initiative -- a rich domain entity with factory method, business actions, value objects, and domain events.

**Rationale:** Consistency with established patterns reduces cognitive load. The Person and Initiative implementations provide a proven template. The Capture domain model is well-defined in `design/domain-model.md`.

**Alternatives considered:**
- Anemic model with logic in handlers -- rejected because it contradicts the project's DDD principles

### 2. Value objects for CaptureType and ProcessingStatus

**Decision:** Implement `CaptureType` as a C# enum (`QuickNote`, `Transcript`, `MeetingNotes`) and `ProcessingStatus` as a C# enum (`Raw`, `Processing`, `Processed`, `Failed`). Status transitions are enforced by the aggregate's business actions.

**Rationale:** Enums are simple, EF Core maps them natively, and the state machine logic lives on the aggregate root where it belongs. This matches how PersonType is implemented.

**Alternatives considered:**
- Smart enum pattern (e.g., Ardalis.SmartEnum) -- unnecessary complexity for this use case
- Status as a separate value object class -- over-engineering given the straightforward state machine

### 3. LinkedPersonIds and LinkedInitiativeIds as List<Guid>

**Decision:** Store linked IDs as `List<Guid>` on the Capture entity. No foreign key constraints. No cross-aggregate validation at the domain level.

**Rationale:** DDD aggregate boundaries: aggregates reference each other by ID only. The Capture aggregate cannot validate that a PersonId exists because Person is a separate aggregate. Eventual consistency is acceptable -- if a linked Person is later deleted, the orphaned ID is harmless.

**Alternatives considered:**
- Foreign key constraints in EF Core -- violates aggregate boundary principles
- Domain service to validate IDs before linking -- adds coupling; out of scope for capture-text

### 4. Spawned entity ID lists are empty in capture-text scope

**Decision:** The `SpawnedCommitmentIds`, `SpawnedDelegationIds`, and `SpawnedObservationIds` properties exist on the aggregate but are only populated by the `capture-ai-extraction` spec. The `RecordSpawned*` methods will be implemented but not called by any handler in this spec.

**Rationale:** The domain model defines these properties and they must exist for the aggregate to be complete, but the feature that populates them is in a different spec.

### 5. EF Core configuration for collections

**Decision:** Store `LinkedPersonIds`, `LinkedInitiativeIds`, and spawned ID lists as JSON columns in PostgreSQL using EF Core's JSON column support.

**Rationale:** These are simple `List<Guid>` values that don't need their own tables. PostgreSQL JSON columns are well-supported by Npgsql and avoid join overhead. This is simpler than a many-to-many join table approach.

**Alternatives considered:**
- Separate join tables -- more complex, unnecessary for ID-only lists that are always loaded with the aggregate
- PostgreSQL arrays -- JSON is more flexible and better supported by EF Core

### 6. API design follows established conventions

**Decision:** Endpoints under `/api/captures` following the same patterns as `/api/people` and `/api/initiatives`.

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/captures` | POST | Create a new capture |
| `/api/captures` | GET | List user's captures (filterable) |
| `/api/captures/{id}` | GET | Get capture by ID |
| `/api/captures/{id}` | PUT | Update title/source metadata |
| `/api/captures/{id}/link-person` | POST | Link to a person |
| `/api/captures/{id}/link-initiative` | POST | Link to an initiative |
| `/api/captures/{id}/unlink-person` | POST | Remove person link |
| `/api/captures/{id}/unlink-initiative` | POST | Remove initiative link |

**Rationale:** Consistent with existing API patterns. Link/unlink are separate endpoints because they are distinct business actions with their own domain events.

### 7. Frontend uses signal-based state and PrimeNG components

**Decision:** The capture UI consists of a quick-capture dialog (PrimeNG Dialog + InputTextarea), a capture list page (PrimeNG DataView with filters), and a capture detail view. All state management uses Angular signals.

**Rationale:** Follows Angular 21 conventions (zoneless, signals, standalone components) and PrimeNG-first component selection as mandated by CLAUDE.md.

## Risks / Trade-offs

- **[Orphaned links]** If a linked Person or Initiative is deleted, the Capture retains a stale ID. **Mitigation:** This is acceptable per DDD aggregate boundaries. Future clean-up can be handled by a background process or during display (show "deleted person" placeholder).

- **[JSON column query performance]** Querying captures by linked PersonId or InitiativeId requires JSON column queries which may be slower at scale. **Mitigation:** These queries are user-scoped (small dataset per user). If needed, a GIN index on the JSON column can be added later.

- **[Processing status without a processor]** The processing status lifecycle exists but nothing transitions captures past `Raw` status until `capture-ai-extraction` is implemented. **Mitigation:** This is by design. The state machine is correct and ready for the next spec. The UI will show captures as "Raw" status until then.

## Open Questions

_(none -- all decisions are straightforward applications of established patterns)_
