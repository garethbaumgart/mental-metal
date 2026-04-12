## Why

Engineering managers constantly make and receive promises -- "I'll get you headcount approval by Friday" or "Sarah will send the design doc by Wednesday." These commitments easily get lost in the noise of meetings and messages. Commitment tracking gives users a structured way to record, track, and surface bidirectional promises, with overdue detection to prevent things from falling through the cracks.

This is a Tier 2 spec (`commitment-tracking`) that depends on `person-management` and `initiative-management` (Tier 1).

## Non-goals

- **No AI extraction of commitments** -- automatic creation from captures is handled by `capture-ai-extraction`
- **No nudges or rhythms** -- recurring reminders about commitments are in `nudges-rhythms` (Tier 3)
- **No briefing integration** -- surfacing overdue commitments in daily briefings is in `daily-weekly-briefing` (Tier 3)
- **No queue prioritisation** -- commitment-based queue items are in `my-queue` (Tier 3)

## What Changes

- **New Commitment aggregate** in the Domain layer with direction (MineToThem, TheirsToMe), status lifecycle (Open -> Completed/Cancelled, with Reopen), overdue detection, and links to Person (required), Initiative (optional), and Capture (optional)
- **CQRS handlers** for creating, completing, cancelling, reopening, updating commitments, and querying by person/initiative/status
- **API endpoints** for commitment CRUD and status transitions
- **EF Core persistence** with PostgreSQL for the Commitment entity, including migrations
- **Angular commitment UI** -- commitment list with filters, create/edit forms, status management, and person-scoped views

## Capabilities

### New Capabilities

- `commitment-tracking`: Core commitment aggregate -- create, track, and manage bidirectional promises between the user and their people, with status lifecycle, overdue detection, and links to people and initiatives

### Modified Capabilities

_(none)_

## Impact

- **Domain:** New `Commitment` aggregate root with value objects (`CommitmentDirection`, `CommitmentStatus`)
- **Application:** New vertical slice handlers for commitment use cases
- **Infrastructure:** New EF Core configuration and migration for `Commitments` table
- **Web API:** New minimal API endpoints under `/api/commitments`
- **Frontend:** New commitment components, commitment service, and routing
- **Dependencies:** `person-management` (PersonId is required), `initiative-management` (optional InitiativeId link)
