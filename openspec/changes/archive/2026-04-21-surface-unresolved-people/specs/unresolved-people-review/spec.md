## ADDED Requirements

### Requirement: Quick-create person and resolve mention

The system SHALL allow an authenticated user to create a new Person and resolve an unresolved person mention in a single operation by sending a POST to `/api/captures/{id}/resolve-person-mention/quick-create` with `rawName`, `personName`, and `personType`. The system SHALL create the Person, add the raw name as an alias (if different from the person name), update the extraction's PersonMention with the new PersonId, link the capture to the new person, and spawn any skipped commitments for that person. The operation SHALL be atomic -- if any step fails, no changes are persisted.

#### Scenario: Quick-create a new person from unresolved mention

- **WHEN** an authenticated user sends a POST to `/api/captures/{id}/resolve-person-mention/quick-create` with rawName "Sarah", personName "Sarah Chen", and personType "Stakeholder"
- **THEN** the system creates a Person named "Sarah Chen" with type Stakeholder
- **AND** adds "Sarah" as an alias on the new Person
- **AND** updates the extraction's PersonMention for "Sarah" with the new PersonId
- **AND** links the capture to the new Person
- **AND** spawns Commitment entities for any High/Medium confidence extracted commitments referencing "Sarah"
- **AND** returns HTTP 200 with the updated CaptureResponse

#### Scenario: Quick-create with same name as raw name

- **WHEN** an authenticated user sends a POST with rawName "Sarah Chen" and personName "Sarah Chen"
- **THEN** the system creates the Person but does NOT add a redundant alias
- **AND** resolves the mention and spawns commitments as normal

#### Scenario: Quick-create with duplicate person name rejected

- **WHEN** an authenticated user sends a POST with personName "Alice Smith" and a non-archived Person named "Alice Smith" already exists
- **THEN** the system returns HTTP 409 Conflict with a message suggesting to link to the existing person instead

#### Scenario: Quick-create with no extraction rejected

- **WHEN** an authenticated user sends a POST for a capture that has no AI extraction
- **THEN** the system returns HTTP 400 with message "Capture has no AI extraction to resolve"

#### Scenario: Quick-create with unknown raw name rejected

- **WHEN** an authenticated user sends a POST with a rawName that does not match any PersonMention in the extraction
- **THEN** the system returns HTTP 400 with message indicating the raw name was not found in extraction

### Requirement: Spawn skipped commitments on person resolution

The system SHALL, when resolving an unresolved person mention (via either link-to-existing or quick-create), spawn Commitment entities for extracted commitments that reference the resolved raw name, have High or Medium confidence, and have no `SpawnedCommitmentId`. The spawned commitments SHALL follow the same creation rules as initial extraction (direction, due date, source offsets, source capture link). The system SHALL update the extraction's `ExtractedCommitment` entries with the newly spawned `SpawnedCommitmentId` and resolved `PersonId`.

#### Scenario: Spawn commitments after resolving person mention

- **WHEN** an unresolved person mention for "Sarah" is resolved to PersonId X
- **AND** the extraction contains two commitments referencing "Sarah": one High confidence and one Low confidence
- **THEN** the system creates one Commitment entity (High confidence) with PersonId X and SourceCaptureId set to the capture
- **AND** updates the extraction's High confidence commitment with the SpawnedCommitmentId
- **AND** does NOT spawn a commitment for the Low confidence item

#### Scenario: No commitments to spawn

- **WHEN** an unresolved person mention is resolved but no extracted commitments reference that raw name
- **THEN** no commitments are spawned and the resolution completes successfully

#### Scenario: Commitments already spawned are not duplicated

- **WHEN** an extracted commitment already has a non-null SpawnedCommitmentId
- **THEN** the system does NOT create a duplicate commitment for that entry

### Requirement: Store raw person name on extracted commitments

The system SHALL store the `PersonRawName` on each `ExtractedCommitment` in the `AiExtraction` value object. This field records the original raw name string from the AI response, enabling correlation between unresolved person mentions and their associated commitments after extraction. The field SHALL be populated during the extraction pipeline for all commitments that reference a person.

#### Scenario: Extracted commitment stores raw person name

- **WHEN** the AI extraction pipeline processes a capture and extracts a commitment referencing "Sarah"
- **THEN** the `ExtractedCommitment` has `PersonRawName` set to "Sarah"

#### Scenario: Extracted commitment with no person reference

- **WHEN** the AI extraction pipeline extracts a commitment with no person reference
- **THEN** the `ExtractedCommitment` has `PersonRawName` set to null

### Requirement: Unresolved people banner on capture detail

The frontend capture detail view SHALL display a prominent banner when the extraction contains unresolved person mentions (PersonMention entries with null PersonId). The banner SHALL show the count of unresolved people and list each unresolved name with its context snippet. Each unresolved name SHALL have two action buttons: "Link to Existing" (opens a person search/select dropdown) and "Quick Create" (opens a dialog with name pre-filled). The banner SHALL disappear when all person mentions are resolved.

#### Scenario: Banner shown with unresolved people

- **WHEN** a user views a processed capture that has two unresolved person mentions ("Sarah" and "Mike")
- **THEN** the capture detail view shows a banner stating "2 unresolved people"
- **AND** each name is listed with "Link to Existing" and "Quick Create" buttons

#### Scenario: Banner hidden when all people resolved

- **WHEN** a user views a processed capture where all person mentions have a non-null PersonId
- **THEN** no unresolved people banner is displayed

#### Scenario: Banner updates after resolution

- **WHEN** a user resolves "Sarah" via quick-create or link-to-existing
- **THEN** the banner updates to show the remaining unresolved count
- **AND** "Sarah" is removed from the unresolved list

### Requirement: Link-to-existing person flow

The frontend SHALL provide a person search/select flow for the "Link to Existing" action on unresolved person mentions. The flow SHALL show a searchable dropdown of the user's existing people. Selecting a person SHALL call the existing `POST /api/captures/{id}/resolve-person-mention` endpoint with the raw name and selected PersonId.

#### Scenario: Link unresolved mention to existing person

- **WHEN** a user clicks "Link to Existing" on unresolved name "Sarah"
- **AND** selects "Sarah Chen" from the person dropdown
- **THEN** the system calls the resolve endpoint with rawName "Sarah" and the PersonId for "Sarah Chen"
- **AND** the UI updates to show "Sarah" as resolved

#### Scenario: Search filters people list

- **WHEN** a user types "Sar" in the person search dropdown
- **THEN** the dropdown shows only people whose name contains "Sar"

### Requirement: Quick-create person dialog

The frontend SHALL provide a quick-create dialog for the "Quick Create" action on unresolved person mentions. The dialog SHALL pre-fill the person name with the raw name from the extraction and default the person type to Stakeholder. The user SHALL be able to edit the name and select a different person type before confirming. Confirming SHALL call the `POST /api/captures/{id}/resolve-person-mention/quick-create` endpoint.

#### Scenario: Quick-create dialog pre-filled

- **WHEN** a user clicks "Quick Create" on unresolved name "Sarah"
- **THEN** a dialog opens with the name field pre-filled as "Sarah" and type defaulted to "Stakeholder"

#### Scenario: User edits name before creating

- **WHEN** a user changes the pre-filled name from "Sarah" to "Sarah Chen" and confirms
- **THEN** the system creates a Person named "Sarah Chen" and resolves the mention

#### Scenario: User changes person type

- **WHEN** a user selects "DirectReport" instead of the default "Stakeholder" and confirms
- **THEN** the system creates a Person with type DirectReport
