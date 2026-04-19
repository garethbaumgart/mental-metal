# Commitment Auto-Tracker

Commitments are no longer manually created. They are auto-extracted from transcripts by the AI extraction pipeline, scored by confidence, and surfaced in the daily brief and commitment list. The user's only actions are: mark complete, dismiss (false positive), or reopen.

## Domain Changes

### Fields Kept
- `Id`: Guid
- `UserId`: Guid
- `Description`: string
- `Direction`: CommitmentDirection (mine-to-them, theirs-to-me)
- `PersonId`: Guid? (who the commitment is to/from)
- `InitiativeId`: Guid? (optional initiative link)
- `DueDate`: DateTimeOffset?
- `Status`: CommitmentStatus
- `CreatedAt`: DateTimeOffset
- `CompletedAt`: DateTimeOffset?

### Fields Added
- `SourceCaptureId`: Guid — the capture this commitment was extracted from. Required. Provides traceability back to the original transcript.
- `Confidence`: CommitmentConfidence enum (high, medium, low) — AI-assigned confidence that this is a real commitment.
- `DismissedAt`: DateTimeOffset? — when the user dismissed this as a false positive

### Fields Removed
- None structurally, but the `Create` command is removed from the public API

### Status Changes
- `CommitmentStatus` enum adds: `dismissed` (alongside existing open, completed, cancelled)
- `dismissed` is a terminal state — the user is saying "this is not a real commitment"
- `cancelled` remains for commitments that were real but are no longer needed

### Invariants
- `SourceCaptureId` is required (every commitment traces to a transcript)
- `Confidence` is required, set at creation by the extraction pipeline
- Only `high` and `medium` confidence commitments are created as Commitment entities
- `low` confidence items remain in the extraction JSON and are not promoted to Commitment entities
- A dismissed commitment cannot be completed (and vice versa)
- Reopening a dismissed commitment sets it back to `open` and clears `DismissedAt`

## API Changes

### Removed Endpoints
- `POST /api/commitments` — no manual creation. Commitments are created exclusively by the extraction pipeline.
- `PUT /api/commitments/{id}` — no manual editing of description/direction/person. The extraction is the source of truth.
- `POST /api/commitments/{id}/cancel` — replaced by dismiss for false positives; real cancellation is "complete with a note"
- `PUT /api/commitments/{id}/due-date` — AI sets the due date; user cannot override (simplification for V2)
- `POST /api/commitments/{id}/link-initiative` — auto-linked by extraction

### Kept Endpoints
- `GET /api/commitments` — List commitments for user. Supports filters: status, direction, person, initiative, confidence, overdue
- `GET /api/commitments/{id}` — Get commitment detail (includes source capture link)
- `POST /api/commitments/{id}/complete` — Mark as done
- `POST /api/commitments/{id}/reopen` — Reopen a completed or dismissed commitment

### New Endpoints
- `POST /api/commitments/{id}/dismiss` — Mark as false positive (not a real commitment). Sets status to `dismissed` and `DismissedAt`.

### List Endpoint Default Behavior
- Default filter: `status=open`, sorted by due date (overdue first), then confidence (high first)
- `dismissed` commitments excluded from default list (available via explicit filter)

## Confidence Model

Confidence is assigned by the AI extraction pipeline based on linguistic signals in the transcript:

### High Confidence
- Explicit promise with identifiable person and time signal
- Signal patterns: "I will [action] by [time]", "I'll get back to you [time]", "I promise to [action]"
- Always includes: a clear action, a person (speaker or addressee), and a time reference

### Medium Confidence
- Clear intent but missing either the person or the deadline
- Signal patterns: "I need to [action]", "I should [action]", "let me [action]", "I'll have a look at [thing]"
- Includes: a clear action, but may lack explicit person or time

### Low Confidence
- Mentioned but ambiguous — may be conversational rather than a commitment
- Signal patterns: "we could [action]", "maybe I'll [action]", "it would be good to [action]"
- NOT created as Commitment entities — stored only in extraction JSON
- Visible in People Dossier context if the AI deems them relevant

## Frontend Changes

### Commitment List Page
- Grouped by: Overdue, Due Today, Due This Week, Later, No Due Date
- Within each group: sorted by confidence (high first)
- Each item shows: description, person name, due date, confidence badge, source capture link
- Swipe/click actions: Complete, Dismiss
- Filter bar: direction (mine/theirs), person, initiative, confidence level
- Dismissed items hidden by default, accessible via "Show dismissed" toggle

### Commitment Detail
- Read-only view: description, direction, person, initiative, due date, confidence, status
- Link to source capture (click to view the transcript where this was extracted)
- Action buttons: Complete, Dismiss, Reopen (contextual based on status)
- No edit form — the extraction is the source of truth

### Dashboard Integration
- Daily brief surfaces: overdue commitments + commitments due today (high confidence prominent, medium shown smaller)
- See `daily-brief` spec for details

## Extraction Integration

Commitments are spawned by the `ai-auto-extraction-v2` pipeline:

1. AI analyses transcript text
2. Extracts commitment candidates with confidence scores
3. For `high` and `medium` confidence: creates a Commitment entity via domain command
4. For `low` confidence: stores in extraction JSON only
5. Links commitment to Person (via name resolution) and Initiative (via auto-tagging) where possible

See `ai-auto-extraction-v2` spec for extraction details.

## Migration

Part of the V2 migration:
- Add `source_capture_id` column to `commitments` table (Guid, FK to captures, required for new rows — nullable for any existing test data)
- Add `confidence` column to `commitments` table (smallint enum, default: high for existing rows)
- Add `dismissed_at` column to `commitments` table (timestamptz, nullable)
- Update `status` enum to include `dismissed` value

## Acceptance Criteria

- [ ] No manual commitment creation endpoint exists
- [ ] Commitments created exclusively by extraction pipeline
- [ ] Every commitment has a `SourceCaptureId` linking to the originating capture
- [ ] Every commitment has a `Confidence` score (high, medium, low)
- [ ] Only high and medium confidence items become Commitment entities
- [ ] Low confidence items stored in extraction JSON only
- [ ] Dismiss endpoint sets status to `dismissed` with timestamp
- [ ] Dismissed commitments excluded from default list
- [ ] Reopen works on both completed and dismissed commitments
- [ ] Commitment list grouped by urgency, sorted by confidence within groups
- [ ] Each commitment links to its source capture in the UI
- [ ] Complete and Dismiss actions available as quick actions in the list
- [ ] Domain tests cover: dismiss invariants, reopen from dismissed, confidence enum
- [ ] Integration tests cover: extraction-created commitments, dismiss/reopen cycle, list filtering
