## Context

Mental Metal has two Tier 1 specs implemented (user-auth-tenancy, ai-provider-abstraction). The Person aggregate is defined in the domain model but has no implementation yet. Person is the hub aggregate — most Tier 2 features depend on it. The existing codebase establishes clear patterns: DDD aggregates with rich behaviour, vertical slice handlers, EF Core OwnsOne for value objects, minimal APIs, and Angular standalone components with signals.

## Goals / Non-Goals

**Goals:**
- Implement the Person aggregate with full DDD behaviour matching the domain model
- Provide CRUD API endpoints and Angular UI for managing people
- Support type-specific value objects (CareerDetails for direct reports, CandidateDetails for candidates)
- Enforce all domain invariants (unique name per user, type-specific constraints, pipeline state machine)

**Non-Goals:**
- 1:1 records, observations, goals (people-lens, Tier 2)
- Linking people to initiatives or captures (handled by those specs)
- Interview scorecards or detailed pipeline UI (interview-tracking, Tier 3)
- AI-powered features (no AI in this spec)

## Dependencies

- `user-auth-tenancy` — UserId scoping, ICurrentUserService, authentication

## Decisions

### 1. Person aggregate follows existing User aggregate patterns

**Decision:** Mirror the established patterns — factory method for creation, value objects via EF Core OwnsOne, domain events, IUserScoped interface.

**Rationale:** Consistency reduces cognitive load and leverages proven infrastructure (global query filters, event dispatching). The User aggregate is a well-tested reference implementation.

**Alternatives considered:**
- Anemic model with service-layer logic: Rejected — violates DDD principles established in CLAUDE.md
- Generic repository: Rejected — dedicated IPersonRepository allows aggregate-specific query methods

### 2. PersonType as enum, type-specific VOs as nullable owned entities

**Decision:** PersonType is a simple enum (DirectReport, Stakeholder, Candidate). CareerDetails and CandidateDetails are nullable value objects mapped via OwnsOne. Domain methods enforce which VO is valid for which type.

**Rationale:** EF Core OwnsOne handles nullable owned types well. The aggregate enforces invariants (CareerDetails only for DirectReport, CandidateDetails only for Candidate). Simpler than TPH inheritance.

**Alternatives considered:**
- Table-per-hierarchy with discriminator: Rejected — over-engineering for what is essentially optional VOs on a single entity
- JSON columns for type-specific data: Rejected — loses queryability and type safety

### 3. Candidate pipeline as a state machine on the value object

**Decision:** PipelineStatus is an enum with valid transitions enforced by the AdvanceCandidatePipeline domain method. Transitions: New → Screening → Interviewing → OfferStage → Hired/Rejected/Withdrawn. Rejected/Withdrawn can happen from any active state.

**Rationale:** Simple enum + guard method is sufficient. No need for a formal state machine library — the transitions are straightforward and the aggregate enforces them.

### 4. Soft archive via IsArchived flag

**Decision:** Archive sets an IsArchived bool + ArchivedAt timestamp. Archived people are excluded from default list queries but remain in the database for referential integrity with future Tier 2 entities.

**Rationale:** Hard delete would break future foreign key references from commitments, delegations, etc. Soft archive is the standard DDD approach for entities with cross-aggregate references.

### 5. API design: RESTful under /api/people

**Decision:** Endpoints follow REST conventions:
- `GET /api/people` — list (with optional type filter, includes archived=false by default)
- `GET /api/people/{id}` — get by ID
- `POST /api/people` — create
- `PUT /api/people/{id}` — update profile
- `PUT /api/people/{id}/type` — change type
- `PUT /api/people/{id}/career-details` — update career details (direct reports only)
- `PUT /api/people/{id}/candidate-details` — update candidate details (candidates only)
- `POST /api/people/{id}/advance-pipeline` — advance candidate pipeline
- `POST /api/people/{id}/archive` — archive

**Rationale:** Matches the domain's business actions. Separate endpoints for type-specific operations provide clear validation boundaries.

### 6. Frontend: PrimeNG DataTable for list, Dialog for create/edit

**Decision:** Use PrimeNG Table component for the people list with filtering by type. Use PrimeNG Dialog with reactive/signal forms for create and edit. Type-specific sections show/hide based on PersonType selection.

**Rationale:** PrimeNG-first approach per CLAUDE.md. DataTable provides sorting, filtering, and pagination out of the box. Dialog keeps the user in context.

### 7. Unique name constraint at both domain and database level

**Decision:** The domain method checks uniqueness via a repository method `ExistsByNameAsync(userId, name, excludeId?)`. The database has a unique filtered index on (UserId, Name) where IsArchived = false.

**Rationale:** Domain validation gives clear error messages. Database index prevents race conditions. Filtered index allows archived people to share names with active ones.

## Risks / Trade-offs

- **[Name uniqueness race condition]** → Mitigated by unique database index. Domain check is for UX; DB constraint is the safety net.
- **[OwnsOne nullable mapping complexity]** → EF Core handles this well with the existing patterns (see UserConfiguration for AiProviderConfig). Follow the same approach.
- **[Type change loses details]** → By design: changing from DirectReport clears CareerDetails, changing from Candidate clears CandidateDetails. The domain event captures the transition for audit. Candidate → DirectReport preserves interview history (handled by interview-tracking spec later).

## Open Questions

None — the domain model is well-defined and the implementation patterns are established.
