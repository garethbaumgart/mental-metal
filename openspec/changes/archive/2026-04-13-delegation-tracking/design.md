## Context

Mental Metal has Person and Initiative management (Tier 1) in place. Delegation tracking is a core Tier 2 capability that lets users assign tasks to people and retain follow-up ownership. Unlike commitments (bidirectional), delegations flow in one direction: the user assigns, the delegate executes. The Delegation aggregate is well-defined in `design/domain-model.md`.

### Dependencies

- `person-management` (Tier 1) -- DelegatePersonId is a required field
- `initiative-management` (Tier 1) -- InitiativeId is an optional link

## Goals / Non-Goals

**Goals:**

- Implement the Delegation aggregate with rich domain behaviour following established DDD patterns
- Support the full status state machine (Assigned -> InProgress -> Completed, with Blocked/Unblock paths)
- Support priority levels (Low, Medium, High, Urgent)
- Track follow-up activity with timestamps
- Support reassignment to a different person
- Provide list views with filtering by person, initiative, status, and priority
- Follow the same vertical slice, CQRS patterns established by person-management and initiative-management

**Non-Goals:**

- AI extraction of delegations from captures (capture-ai-extraction spec)
- Nudge/reminder scheduling for follow-ups (nudges-rhythms spec)
- Briefing integration (daily-weekly-briefing spec)
- Queue-based prioritisation (my-queue spec)
- People-lens per-person delegation view (people-lens spec)
- Validating that PersonId/InitiativeId actually exist (eventual consistency per DDD)

## Decisions

### 1. Delegation aggregate follows existing DDD patterns

**Decision:** Model the Delegation aggregate identically to Person, Initiative, and Commitment -- rich domain entity with factory method, business actions, value objects, and domain events.

**Rationale:** Consistency with established patterns.

### 2. Value objects for DelegationStatus and Priority

**Decision:** Implement `DelegationStatus` as a C# enum (`Assigned`, `InProgress`, `Completed`, `Blocked`) and `Priority` as a C# enum (`Low`, `Medium`, `High`, `Urgent`). Status transitions are enforced by the aggregate following the state machine in the domain model.

**Rationale:** Same approach as other aggregates. The state machine is richer than Commitment (four statuses, blocked/unblock paths) but still simple enough for enum + aggregate methods.

### 3. Follow-up tracking via LastFollowedUpAt

**Decision:** `RecordFollowUp(notes?)` updates `LastFollowedUpAt` to the current time and optionally appends to notes. This is a simple timestamp, not a log of all follow-ups.

**Rationale:** A full follow-up history would require a child entity or event sourcing, which is over-engineering for v1. The last follow-up timestamp is sufficient for "haven't checked in on this in X days" queries. If richer history is needed later, it can be added as an Observation linked to the person.

**Alternatives considered:**
- Follow-up log as a collection of value objects -- more complex, deferred to a future enhancement
- Separate FollowUp entity -- violates aggregate boundaries for minimal benefit

### 4. Reassignment as a business action

**Decision:** `Reassign(newPersonId)` changes `DelegatePersonId` and raises `DelegationReassigned`. The old person ID is not preserved in the aggregate (but the event captures it).

**Rationale:** Reassignment is a common real-world operation. Keeping a history of reassignments is a "nice to have" that the domain event provides for audit purposes.

### 5. No terminal status for Blocked

**Decision:** Blocked is a temporary state, not a terminal one. Blocked delegations can be unblocked (returning to InProgress) or completed directly. This matches the state machine in the domain model.

**Rationale:** Real-world blockers get resolved. A delegation shouldn't be stuck in Blocked forever with no way out.

### 6. API design follows established conventions

**Decision:** Endpoints under `/api/delegations`.

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/delegations` | POST | Create a new delegation |
| `/api/delegations` | GET | List user's delegations (filterable) |
| `/api/delegations/{id}` | GET | Get delegation by ID |
| `/api/delegations/{id}` | PUT | Update description, notes |
| `/api/delegations/{id}/start` | POST | Mark as in-progress |
| `/api/delegations/{id}/complete` | POST | Mark as completed |
| `/api/delegations/{id}/block` | POST | Mark as blocked |
| `/api/delegations/{id}/unblock` | POST | Remove blocker |
| `/api/delegations/{id}/follow-up` | POST | Record follow-up |
| `/api/delegations/{id}/due-date` | PUT | Update due date |
| `/api/delegations/{id}/priority` | PUT | Change priority |
| `/api/delegations/{id}/reassign` | POST | Reassign to different person |

### 7. Frontend uses signal-based state and PrimeNG components

**Decision:** Delegation UI consists of a delegation list page (PrimeNG DataView with filters), a create/edit dialog, and inline status actions with follow-up recording. All state via Angular signals.

## Risks / Trade-offs

- **[Orphaned DelegatePersonId]** If a Person is archived, the Delegation retains the stale ID. **Mitigation:** Person archival is soft delete; display can show "archived person" if needed.

- **[State machine complexity]** The Delegation state machine has more paths than Commitment (blocked/unblock, direct completion from multiple states). **Mitigation:** Thorough domain unit tests for every valid and invalid transition.

- **[Follow-up history lost]** Only the last follow-up timestamp is stored. **Mitigation:** Acceptable for v1. Domain events capture history for audit. A future enhancement could add follow-up log.

## Open Questions

_(none)_
