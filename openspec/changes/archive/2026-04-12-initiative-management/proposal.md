## Why

Initiative is the evolving context aggregate in Mental Metal — it holds the living brief for every project and workstream the user manages. Without initiative-management, Tier 2 capabilities like commitment-tracking, delegation-tracking, initiative-living-brief, and capture-ai-extraction cannot link work to initiatives. This is one of the last two Tier 1 foundation specs and unblocks 6 of 8 Tier 2 capabilities.

## What Changes

- Add the **Initiative** aggregate with basic metadata and status management
- Implement the Initiative status state machine: Active → OnHold (resume back), Active → Completed, Active → Cancelled
- Support milestone tracking with target dates and descriptions
- Support linking people to initiatives (LinkedPersonIds)
- Expose minimal API endpoints for CRUD operations and status transitions
- Add EF Core persistence with PostgreSQL, including migrations
- Add Angular frontend for listing, creating, editing initiatives and managing milestones

## Non-goals

- **AI-maintained summary** (AiSummary, RefreshSummary) — belongs to `initiative-living-brief` (Tier 2)
- **Key decisions log, risk tracking, requirements/design snapshots** — belongs to `initiative-living-brief` (Tier 2)
- **Dependencies tracking** — belongs to `initiative-living-brief` (Tier 2)
- **Initiative-scoped AI chat** — belongs to `initiative-ai-chat` (Tier 2)
- **Auto-linking from captures** — belongs to `capture-ai-extraction` (Tier 2)

## Capabilities

### New Capabilities
- `initiative-management`: Create, edit, and manage initiatives with title, status state machine, milestones, and linked people

### Modified Capabilities
<!-- No existing spec requirements are changing -->

## Impact

- **Domain layer**: New Initiative aggregate root, InitiativeStatus enum, Milestone value object, domain events, IInitiativeRepository
- **Application layer**: Vertical slice handlers for create, update, change status, manage milestones, link people, list, get
- **Infrastructure layer**: EF Core configuration, InitiativeRepository, database migration
- **Web layer**: Minimal API endpoints under `/api/initiatives`
- **Frontend**: Initiative list view, create/edit forms, milestone management, person linking
- **Tier**: Tier 1 — Foundation
- **Dependencies**: `user-auth-tenancy` (for UserId scoping and authentication), `person-management` (for LinkedPersonIds validation — soft dependency, IDs only)
- **Dependents**: 6 of 8 Tier 2 specs depend on this
