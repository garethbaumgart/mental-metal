## MODIFIED Requirements

### Requirement: Create a commitment

The system SHALL allow an authenticated user to create a new Commitment with a description, direction (MineToThem or TheirsToMe), and a PersonId. Description MUST NOT be empty. PersonId MUST be provided. The system SHALL set Status to `Open` and set CreatedAt. Optional fields: DueDate, InitiativeId, SourceCaptureId, SourceStartOffset, SourceEndOffset, Notes. SourceStartOffset and SourceEndOffset are nullable integers representing character positions in the source capture's RawContent. The system SHALL raise a `CommitmentCreated` domain event. The Commitment SHALL be scoped to the authenticated user's UserId.

#### Scenario: Create a commitment I owe to someone

- **WHEN** an authenticated user sends a POST to `/api/commitments` with description "Send Q3 roadmap draft", direction "MineToThem", and personId for "Sarah"
- **THEN** the system creates a Commitment with status Open, direction MineToThem, and returns HTTP 201

#### Scenario: Create a commitment someone owes me

- **WHEN** an authenticated user sends a POST to `/api/commitments` with description "Deliver design doc", direction "TheirsToMe", personId, and dueDate "2026-04-20"
- **THEN** the system creates a Commitment with status Open, direction TheirsToMe, the due date, and returns HTTP 201

#### Scenario: Create with optional initiative and capture links

- **WHEN** an authenticated user sends a POST to `/api/commitments` with description, direction, personId, initiativeId, and sourceCaptureId
- **THEN** the system creates a Commitment with all linked IDs and returns HTTP 201

#### Scenario: Create with source offsets

- **WHEN** an authenticated user sends a POST to `/api/commitments` with description, direction, personId, sourceCaptureId, sourceStartOffset 1234, and sourceEndOffset 1456
- **THEN** the system creates a Commitment with the source offsets stored and returns HTTP 201

#### Scenario: Empty description rejected

- **WHEN** an authenticated user sends a POST to `/api/commitments` with an empty description
- **THEN** the system returns HTTP 400 with a validation error

#### Scenario: Missing personId rejected

- **WHEN** an authenticated user sends a POST to `/api/commitments` without a personId
- **THEN** the system returns HTTP 400 with a validation error

#### Scenario: User isolation

- **WHEN** User A creates a commitment and User B creates a commitment
- **THEN** each commitment is scoped to its respective user's UserId

### Requirement: Get commitment by ID

The system SHALL allow an authenticated user to retrieve a single commitment by ID. The response SHALL include `sourceStartOffset` and `sourceEndOffset` fields (nullable integers).

#### Scenario: Get existing commitment

- **WHEN** an authenticated user sends a GET to `/api/commitments/{id}` with a valid commitment ID
- **THEN** the system returns the commitment with all fields including linked IDs, computed IsOverdue, sourceStartOffset, and sourceEndOffset
