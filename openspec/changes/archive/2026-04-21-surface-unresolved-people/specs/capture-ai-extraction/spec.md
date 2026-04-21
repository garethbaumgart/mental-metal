## MODIFIED Requirements

### Requirement: Spawn entities from extraction

The system SHALL, after successful extraction, automatically spawn Commitment entities for High/Medium confidence commitments that have a resolved PersonId. Commitments referencing unresolved people (null PersonId) SHALL be recorded in the extraction metadata with their `PersonRawName` but SHALL NOT be spawned as Commitment entities. Each spawned entity SHALL have its SourceCaptureId set to the originating capture's ID. Each spawned Commitment SHALL have its SourceStartOffset and SourceEndOffset set from the extracted commitment's source offsets (if available). The extraction metadata SHALL preserve all extracted commitments (both spawned and skipped) so that skipped commitments can be spawned later when the person is resolved.

#### Scenario: Auto-spawn commitments for resolved people only

- **WHEN** the extraction pipeline finds two commitments: one for "Sarah" (resolved to PersonId X) and one for "Mike" (unresolved)
- **THEN** the system creates a Commitment entity for Sarah's commitment with PersonId X and SourceCaptureId set to the capture
- **AND** records Mike's commitment in the extraction with PersonRawName "Mike", PersonId null, and SpawnedCommitmentId null
- **AND** does NOT create a Commitment entity for Mike's commitment

#### Scenario: Auto-spawn with source offsets

- **WHEN** the extraction pipeline spawns a commitment with source offsets
- **THEN** the spawned Commitment has SourceStartOffset and SourceEndOffset set from the extraction data

#### Scenario: Name resolution matches existing person

- **WHEN** the extraction contains person hints (e.g., "Sarah") and the user has a Person named "Sarah Chen"
- **THEN** the system matches the hint to the existing Person by name similarity and sets the PersonId on the spawned commitment

#### Scenario: Name resolution finds no match

- **WHEN** the extraction contains a person hint that does not match any existing Person
- **THEN** the extracted commitment is recorded with PersonRawName set and PersonId null
- **AND** no Commitment entity is spawned for that item
- **AND** the user can resolve the person later via the unresolved people review flow

### Requirement: AiExtraction value object

The system SHALL define an `AiExtraction` value object embedded on the Capture aggregate with the following properties: Summary (string, required), Commitments (list of extracted commitments with description, direction, person hint, PersonRawName, optional due date, and optional source character offsets: SourceStartOffset and SourceEndOffset, optional SpawnedCommitmentId), Delegations (list of extracted delegations with description, person hint, and optional due date), Observations (list of extracted observations with description, person hint, and tag), Decisions (list of strings), RisksIdentified (list of strings), SuggestedPersonLinks (list of person name hints), SuggestedInitiativeLinks (list of initiative name hints), and ConfidenceScore (decimal, 0.0-1.0).

#### Scenario: AiExtraction with all fields populated

- **WHEN** a transcript yields commitments, delegations, observations, decisions, and risks
- **THEN** the AiExtraction value object contains all extracted items with their respective properties
- **AND** each commitment includes SourceStartOffset, SourceEndOffset, and PersonRawName

#### Scenario: AiExtraction with minimal content

- **WHEN** a quick note yields only a summary and one commitment
- **THEN** the AiExtraction value object contains the summary and one commitment, with empty lists for other properties

#### Scenario: AiExtraction equality

- **WHEN** two AiExtraction instances have identical property values
- **THEN** they are considered equal

### Requirement: Resolve person mention post-extraction

The system SHALL allow an authenticated user to resolve an unresolved person mention by sending a POST to `/api/captures/{captureId}/resolve-person-mention` with `rawName` and `personId`. The system SHALL update the extraction's PersonMention with the resolved PersonId, add the raw name as an alias on the person (if not already present), link the capture to the person, and spawn any skipped commitments for that person (High/Medium confidence with no existing SpawnedCommitmentId). The raw name used as alias SHALL be validated for uniqueness among the user's people.

#### Scenario: Resolve and spawn skipped commitments

- **WHEN** an authenticated user resolves "Sarah" to PersonId X
- **AND** the extraction has one High confidence commitment with PersonRawName "Sarah" and SpawnedCommitmentId null
- **THEN** the system creates a Commitment entity with PersonId X
- **AND** updates the extraction's commitment with the SpawnedCommitmentId
- **AND** records the spawned commitment on the capture

#### Scenario: Resolve with no skipped commitments

- **WHEN** an authenticated user resolves "Mike" to PersonId Y
- **AND** no extracted commitments reference "Mike"
- **THEN** the mention is resolved and linked, but no commitments are spawned

#### Scenario: Alias conflict rejected

- **WHEN** an authenticated user resolves "Sarah" to PersonId X
- **AND** another Person already has "Sarah" as an alias
- **THEN** the system returns HTTP 409 Conflict
