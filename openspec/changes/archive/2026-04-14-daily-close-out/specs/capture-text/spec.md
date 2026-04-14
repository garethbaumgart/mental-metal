## ADDED Requirements

### Requirement: Capture triage flag

The Capture aggregate SHALL expose a boolean `Triaged` flag (default false) and a nullable `TriagedAtUtc` timestamp. The aggregate SHALL provide a `QuickDiscard()` method that, when called on a non-triaged capture, sets `Triaged = true`, sets `TriagedAtUtc = DateTime.UtcNow`, and raises a `CaptureQuickDiscarded` domain event. Calling `QuickDiscard()` on an already-triaged capture SHALL be a no-op (idempotent, no second event raised).

#### Scenario: Quick-discard a non-triaged capture

- **WHEN** QuickDiscard() is called on a Capture where Triaged is false
- **THEN** Triaged is set to true, TriagedAtUtc is set to the current UTC time, and a CaptureQuickDiscarded event is raised

#### Scenario: Quick-discard idempotent

- **WHEN** QuickDiscard() is called on a Capture where Triaged is already true
- **THEN** the aggregate state is unchanged and no event is raised

### Requirement: Capture extraction-resolved flag

The Capture aggregate SHALL expose a boolean `ExtractionResolved` flag (default false) that is set to true when `ConfirmExtraction()` or `DiscardExtraction()` is called, and remains false otherwise. The flag SHALL be persisted on the Captures table.

#### Scenario: Confirming extraction sets flag

- **WHEN** ConfirmExtraction() is called on a Processed capture
- **THEN** ExtractionResolved is true after the call

#### Scenario: Discarding extraction sets flag

- **WHEN** DiscardExtraction() is called on a Processed capture
- **THEN** ExtractionResolved is true after the call

#### Scenario: Flag false on Raw / Processing / Failed

- **WHEN** a Capture has status Raw, Processing, or Failed
- **THEN** ExtractionResolved is false

## MODIFIED Requirements

### Requirement: List captures

The system SHALL allow an authenticated user to retrieve a paginated list of their captures, ordered by CapturedAt descending (newest first). The list SHALL support optional filtering by capture type and processing status. By default, captures with `Triaged = true` SHALL be excluded from the list. An optional `includeTriaged=true` query parameter SHALL include them.

#### Scenario: List all captures

- **WHEN** an authenticated user sends a GET to `/api/captures`
- **THEN** the system returns all non-triaged captures belonging to that user, ordered by CapturedAt descending

#### Scenario: Filter by type

- **WHEN** an authenticated user sends a GET to `/api/captures?type=Transcript`
- **THEN** the system returns only non-triaged captures with type Transcript

#### Scenario: Filter by status

- **WHEN** an authenticated user sends a GET to `/api/captures?status=Raw`
- **THEN** the system returns only non-triaged captures with status Raw

#### Scenario: Include triaged

- **WHEN** an authenticated user sends a GET to `/api/captures?includeTriaged=true`
- **THEN** the system returns all of the user's captures including triaged ones

#### Scenario: Empty list

- **WHEN** an authenticated user with no captures sends a GET to `/api/captures`
- **THEN** the system returns an empty array with HTTP 200

### Requirement: Get capture by ID

The system SHALL allow an authenticated user to retrieve a single capture by ID, including all linked IDs and metadata. The response SHALL include the `triaged`, `triagedAtUtc`, and `extractionResolved` fields. Triaged captures SHALL still be retrievable by ID.

#### Scenario: Get existing capture

- **WHEN** an authenticated user sends a GET to `/api/captures/{id}` with a valid capture ID belonging to them
- **THEN** the system returns the capture with all fields including linkedPersonIds, linkedInitiativeIds, title, source, processing status, triaged, triagedAtUtc, and extractionResolved

#### Scenario: Get triaged capture by ID

- **WHEN** an authenticated user sends a GET to `/api/captures/{id}` for a triaged capture
- **THEN** the system returns the capture with triaged = true

#### Scenario: Capture not found

- **WHEN** an authenticated user sends a GET to `/api/captures/{id}` with an ID that does not exist or belongs to another user
- **THEN** the system returns HTTP 404
