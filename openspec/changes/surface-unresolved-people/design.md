## Context

When AI extraction processes a capture, `NameResolutionService` attempts to match raw person names against the user's existing people. Unresolved names (no match found) result in `PersonMention` entries with `PersonId = null` and extracted commitments with `PersonId = null` that are **not** spawned as Commitment entities (the handler requires a resolved person).

A `ResolvePersonMentionHandler` already exists that links an existing person to an unresolved mention and adds the raw name as an alias. However, it does not spawn the skipped commitments after resolution, and there is no way to quick-create a new person from the capture context.

The frontend capture detail view shows extraction results but does not highlight unresolved people or provide inline resolution actions.

## Goals / Non-Goals

**Goals:**
- Surface unresolved person mentions prominently on the capture detail view
- Allow users to resolve unresolved names by linking to an existing person (already partially implemented) or quick-creating a new person
- Spawn skipped commitments when an unresolved person is resolved post-extraction
- Add the raw name as an alias on the person (existing behaviour, extend to quick-create flow)

**Non-Goals:**
- Making `Commitment.PersonId` nullable
- Changing the AI extraction prompt or fuzzy matching algorithm
- Auto-creating people without user confirmation
- Batch resolution across multiple captures

## Dependencies

- `person-management` (create person API, Person aggregate)
- `commitment-tracking` (Commitment.Create, commitment repository)
- `capture-ai-extraction` (AiExtraction value object, extraction pipeline)

## Decisions

### 1. Extend `ResolvePersonMentionHandler` to spawn skipped commitments

**Decision:** After resolving a person mention, iterate the extraction's commitments that reference the same raw name and have no `SpawnedCommitmentId`, then create Commitment entities for High/Medium confidence items.

**Rationale:** The handler already updates the extraction and links the person. Adding commitment spawning here keeps the resolution flow atomic -- one API call resolves the person and creates all associated commitments. The alternative (separate "spawn commitments" endpoint) adds unnecessary round-trips and coordination.

**Alternatives considered:**
- Separate endpoint for spawning commitments after resolution -- rejected: adds complexity and partial-state risk
- Domain event on PersonMention resolution -- rejected: over-engineering for a synchronous flow within a single handler

### 2. New `QuickCreateAndResolveHandler` for inline person creation

**Decision:** Create a new handler that creates a Person (name + type, minimal fields), then delegates to the existing `ResolvePersonMentionHandler` logic (resolve mention, add alias, spawn commitments).

**Rationale:** Reuses the existing resolution path. The quick-create flow needs only name and person type (defaulting to Stakeholder) -- full person details can be edited later. This avoids duplicating resolution logic.

**Alternatives considered:**
- Calling two separate APIs from the frontend (create person, then resolve mention) -- rejected: race conditions, poor UX, two round-trips
- Adding person creation into `ResolvePersonMentionHandler` -- rejected: violates SRP, the existing handler takes a PersonId

### 3. Frontend: unresolved-people banner on capture detail

**Decision:** Show a prominent banner/card on the capture detail view when there are unresolved person mentions. Each unresolved name shows: the raw name, context snippet, and action buttons for "Link to Existing" (opens person search dropdown) and "Quick Create" (opens inline dialog with name pre-filled).

**Rationale:** Users need to see unresolved names immediately after extraction completes. Inline actions avoid navigation away from the capture. PrimeNG Message component for the banner, Dialog for quick-create.

**Alternatives considered:**
- Toast notification only -- rejected: transient, easy to miss, no action affordance
- Separate "unresolved people" page -- rejected: adds navigation, breaks capture-centric workflow
- Inline expansion in the extraction panel -- acceptable but banner is more visible

### 4. Track unresolved commitment details in extraction metadata

**Decision:** The `ExtractedCommitment` already stores `PersonId` (null for unresolved) and `SpawnedCommitmentId` (null when not spawned). A commitment with a non-null `PersonId` referencing a raw name in the extraction and a null `SpawnedCommitmentId` is the indicator for "needs spawning after resolution." No schema changes needed for tracking.

**Rationale:** The existing data model already captures the necessary state. Adding new fields would be redundant.

### 5. Match extracted commitments to unresolved names via raw name correlation

**Decision:** When resolving a person mention for raw name X, find extracted commitments where the `PersonRawName` from the original AI response matches X. Since `ExtractedCommitment` does not currently store the raw name (only the resolved `PersonId`), add a `PersonRawName` field to `ExtractedCommitment`.

**Rationale:** Without the raw name on the extracted commitment, there is no reliable way to correlate which commitments belong to which unresolved person after the fact. The `PersonId` is null for unresolved names, but multiple unresolved names could exist. Storing the raw name is the simplest correlation mechanism.

## Risks / Trade-offs

- **[Risk] Adding `PersonRawName` to `ExtractedCommitment` requires a migration** -- Mitigation: `AiExtraction` is stored as a JSON column, so this is a non-breaking additive change. No SQL migration needed, just a code change.
- **[Risk] Quick-create might produce duplicate people** -- Mitigation: Person creation already enforces name uniqueness per user. The quick-create dialog will show a warning if the name matches an existing person and suggest linking instead.
- **[Risk] Spawning commitments post-resolution could create unexpected items** -- Mitigation: Only High/Medium confidence commitments are spawned (same rules as initial extraction). The UI will show a preview of what will be created.

## Open Questions

None -- the existing data model and handler patterns provide a clear path.
