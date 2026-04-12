## Why

Person is the hub aggregate in Mental Metal — most Tier 2 capabilities (commitment-tracking, delegation-tracking, people-lens, capture-ai-extraction) require linking to people. Without person-management, no people can be created and those six dependent specs remain blocked. This is one of the last two Tier 1 foundation specs.

## What Changes

- Add the **Person** aggregate with full DDD behaviour: create, update profile, change type, archive
- Support three person types: **DirectReport**, **Stakeholder**, **Candidate** — each with type-specific value objects (CareerDetails, CandidateDetails)
- Enforce invariants: unique name per user, type-specific detail constraints, candidate pipeline state machine
- Expose minimal API endpoints for CRUD operations and type-specific actions
- Add EF Core persistence with PostgreSQL, including migrations
- Add Angular frontend for listing, creating, editing, and archiving people

## Non-goals

- **1:1 records, observations, goals** — those belong to `people-lens` (Tier 2)
- **Linking people to initiatives or captures** — handled by respective specs
- **AI-powered features** — no AI extraction or summarisation in this spec
- **Interview scorecards or pipeline management UI** — belongs to `interview-tracking` (Tier 3)
- **Bulk import/export** — future enhancement

## Capabilities

### New Capabilities
- `person-management`: Create, edit, archive, and manage people with types (direct-report, stakeholder, candidate), career details, candidate pipeline tracking, and team assignment

### Modified Capabilities
<!-- No existing spec requirements are changing -->

## Impact

- **Domain layer**: New Person aggregate root, PersonType enum, CareerDetails/CandidateDetails value objects, domain events, IPersonRepository
- **Application layer**: Vertical slice handlers for create, update, change type, archive, list, get
- **Infrastructure layer**: EF Core configuration, PersonRepository, database migration
- **Web layer**: Minimal API endpoints under `/api/people`
- **Frontend**: Person list view, create/edit forms with type-specific sections
- **Tier**: Tier 1 — Foundation
- **Dependencies**: `user-auth-tenancy` (for UserId scoping and authentication)
- **Dependents**: 6 of 8 Tier 2 specs depend on this
