## Why

Capture is the primary entry point for all data in Mental Metal. Users need a way to quickly dump raw text -- meeting notes, pasted transcripts, quick thoughts -- into the system with minimal friction. This is the foundational "inbox" that all AI extraction, commitment/delegation spawning, and initiative linking builds upon. Without capture-text, there is no raw material for the AI pipeline to process.

This is a Tier 2 spec (`capture-text`) that depends on `user-auth-tenancy` only. It deliberately excludes AI extraction (that's `capture-ai-extraction`, a separate Tier 2 spec).

## Non-goals

- **No AI processing** -- extraction of action items, commitments, risks is out of scope (see `capture-ai-extraction`)
- **No audio capture** -- recording and transcription is a Tier 3 concern (see `capture-audio`)
- **No daily close-out triage** -- reviewing/discarding unprocessed captures is a separate spec
- **No cross-aggregate validation** -- linking to Person/Initiative IDs is stored but not validated at the domain level (eventual consistency)

## What Changes

- **New Capture aggregate** in the Domain layer with the processing status state machine (Raw -> Processing -> Processed / Failed), capture types (quick-note, transcript, meeting-notes), and ID-only links to people and initiatives
- **CQRS handlers** for creating captures, listing user captures, getting capture details, linking captures to people/initiatives, and managing the title/source metadata
- **API endpoints** for capture CRUD and linking operations
- **EF Core persistence** with PostgreSQL for the Capture entity, including migrations
- **Angular capture UI** -- a quick-capture input component and a capture list view with filtering by type and status

## Capabilities

### New Capabilities

- `capture-text`: Core capture aggregate -- create, list, view, and manage raw text captures with processing status lifecycle, type classification, and optional links to people and initiatives

### Modified Capabilities

_(none)_

## Impact

- **Domain:** New `Capture` aggregate root with value objects (`CaptureType`, `ProcessingStatus`)
- **Application:** New vertical slice handlers for capture use cases
- **Infrastructure:** New EF Core configuration and migration for `Captures` table
- **Web API:** New minimal API endpoints under `/api/captures`
- **Frontend:** New capture components, capture service, and routing
- **Dependencies:** Only `user-auth-tenancy` (for UserId scoping)
