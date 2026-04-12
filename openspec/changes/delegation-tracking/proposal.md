## Why

Engineering managers delegate work constantly but often lose track of what they have assigned and to whom. Unlike commitments (bidirectional promises), delegations are one-directional: the user assigns work to a person and retains follow-up ownership. Delegation tracking gives users a structured way to assign, track, follow up on, and manage delegated tasks with priority, status, and accountability.

This is a Tier 2 spec (`delegation-tracking`) that depends on `person-management` and `initiative-management` (Tier 1).

## Non-goals

- **No AI extraction of delegations** -- automatic creation from captures is handled by `capture-ai-extraction`
- **No nudges or rhythms** -- recurring follow-up reminders are in `nudges-rhythms` (Tier 3)
- **No briefing integration** -- surfacing delegations in daily briefings is in `daily-weekly-briefing` (Tier 3)
- **No queue prioritisation** -- delegation-based queue items are in `my-queue` (Tier 3)
- **No people-lens integration** -- viewing delegations per person is in `people-lens`

## What Changes

- **New Delegation aggregate** in the Domain layer with status lifecycle (Assigned -> InProgress -> Completed, with Blocked/Unblock), priority levels, follow-up tracking, and links to Person (required), Initiative (optional), and Capture (optional)
- **CQRS handlers** for creating, updating status, reassigning, following up on, and querying delegations
- **API endpoints** for delegation CRUD, status transitions, and follow-up recording
- **EF Core persistence** with PostgreSQL for the Delegation entity, including migrations
- **Angular delegation UI** -- delegation list with filters, create/edit forms, status management, and follow-up recording

## Capabilities

### New Capabilities

- `delegation-tracking`: Core delegation aggregate -- assign tasks to people, track status (Assigned, InProgress, Completed, Blocked), manage priority, record follow-ups, and link to initiatives

### Modified Capabilities

_(none)_

## Impact

- **Domain:** New `Delegation` aggregate root with value objects (`DelegationStatus`, `Priority`)
- **Application:** New vertical slice handlers for delegation use cases
- **Infrastructure:** New EF Core configuration and migration for `Delegations` table
- **Web API:** New minimal API endpoints under `/api/delegations`
- **Frontend:** New delegation components, delegation service, and routing
- **Dependencies:** `person-management` (DelegatePersonId is required), `initiative-management` (optional InitiativeId link)
