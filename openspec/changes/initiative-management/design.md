## Context

Mental Metal has two Tier 1 specs implemented (user-auth-tenancy, ai-provider-abstraction) and person-management is being proposed in parallel. The Initiative aggregate is defined in the domain model with a rich set of properties, but this Tier 1 spec covers only the foundational CRUD and status management. The AI-maintained brief, decisions, risks, requirements, and design snapshots are deferred to the initiative-living-brief Tier 2 spec.

## Goals / Non-Goals

**Goals:**
- Implement the Initiative aggregate with core metadata (title, status, milestones, linked people)
- Enforce the status state machine (Active ↔ OnHold, Active → Completed/Cancelled)
- Provide CRUD API endpoints and Angular UI for managing initiatives
- Support milestone tracking and person linking

**Non-Goals:**
- AI summary, key decisions, risks, requirements/design snapshots (initiative-living-brief, Tier 2)
- Dependencies tracking (initiative-living-brief, Tier 2)
- Initiative-scoped AI chat (initiative-ai-chat, Tier 2)
- Auto-linking from captures (capture-ai-extraction, Tier 2)

## Dependencies

- `user-auth-tenancy` — UserId scoping, ICurrentUserService, authentication
- `person-management` — LinkedPersonIds reference Person aggregate by ID (soft reference, no FK constraint)

## Decisions

### 1. Scope to core metadata only — defer living brief features

**Decision:** The Initiative aggregate in this spec includes only Title, Status, Milestones, and LinkedPersonIds. Properties like AiSummary, KeyDecisions, OpenRisks, Requirements, DesignDirection, and Dependencies are added by the initiative-living-brief Tier 2 spec.

**Rationale:** Keeps the Tier 1 implementation focused and unblocks dependents quickly. The domain model defines the full aggregate, but we build incrementally — the aggregate grows as specs are implemented.

**Alternatives considered:**
- Implement the full aggregate now: Rejected — most properties require AI infrastructure and capture processing that don't exist yet
- Stub all properties as empty: Rejected — unused columns in the database; migrations should reflect actual usage

### 2. Status state machine enforced in the domain

**Decision:** InitiativeStatus is an enum (Active, OnHold, Completed, Cancelled). The ChangeStatus method on the aggregate validates transitions:
- Active → OnHold, Completed, Cancelled
- OnHold → Active
- Completed/Cancelled → no transitions (terminal states)

**Rationale:** Simple and matches the domain model exactly. Terminal states prevent accidental modification. If a grace-period reversal is needed later, it can be added as a separate business action.

### 3. Milestones as owned collection of value objects

**Decision:** Milestones are a `List<Milestone>` owned by the Initiative, mapped via EF Core OwnsMany. Each Milestone has an Id (Guid, for update/remove), Title, TargetDate, Description, and IsCompleted.

**Rationale:** Milestones are value objects scoped to an initiative — they have no independent lifecycle. OwnsMany maps them to a separate table but manages them within the aggregate boundary.

### 4. LinkedPersonIds as a simple Guid list

**Decision:** LinkedPersonIds is stored as a separate table (InitiativeLinkedPeople) with InitiativeId and PersonId columns. No FK constraint to Person table — just ID references.

**Rationale:** Follows the DDD principle of cross-aggregate references by ID only. No FK constraint means person-management and initiative-management can be deployed independently. Validation of PersonId existence is done at the application layer when linking.

### 5. API design: RESTful under /api/initiatives

**Decision:** Endpoints:
- `GET /api/initiatives` — list (with optional status filter)
- `GET /api/initiatives/{id}` — get by ID
- `POST /api/initiatives` — create
- `PUT /api/initiatives/{id}` — update title
- `PUT /api/initiatives/{id}/status` — change status
- `POST /api/initiatives/{id}/milestones` — add milestone
- `PUT /api/initiatives/{id}/milestones/{milestoneId}` — update milestone
- `DELETE /api/initiatives/{id}/milestones/{milestoneId}` — remove milestone
- `POST /api/initiatives/{id}/milestones/{milestoneId}/complete` — mark milestone complete
- `POST /api/initiatives/{id}/link-person` — link a person
- `DELETE /api/initiatives/{id}/link-person/{personId}` — unlink a person

**Rationale:** Sub-resources for milestones and linked people reflect the aggregate's internal structure while keeping endpoints focused.

### 6. Frontend: PrimeNG DataTable for list, dedicated detail page

**Decision:** Use PrimeNG Table for the initiative list with status filtering and status badges. Detail page shows title, status with transition buttons, milestones timeline, and linked people chips.

**Rationale:** Initiatives have more sub-content than people (milestones, linked people), so a dedicated detail page rather than a dialog gives more room.

## Risks / Trade-offs

- **[LinkedPersonIds without FK]** → No referential integrity at DB level. Mitigated by application-layer validation on link and graceful handling if a person is archived/deleted. This is by design per DDD aggregate boundaries.
- **[Incremental aggregate growth]** → The Initiative entity will gain properties in Tier 2. Mitigated by clean domain model — new properties are added via new migrations when their specs are implemented.
- **[Terminal status is permanent]** → Completed/Cancelled cannot be reversed. This matches the domain model. If needed later, a grace-period reopen can be added as a separate business action.

## Open Questions

None — the scoping is clear and patterns are established.
