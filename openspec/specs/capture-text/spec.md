# Capture Text

## Requirements

### Requirement: Create a capture

The system SHALL allow an authenticated user to create a new Capture with raw text content and a capture type (QuickNote, Transcript, or MeetingNotes). RawContent MUST NOT be empty. The system SHALL set ProcessingStatus to `Raw`, set CapturedAt to the current time, and scope the Capture to the authenticated user's UserId. The system SHALL raise a `CaptureCreated` domain event.

#### Scenario: Create a quick note

- **WHEN** an authenticated user sends a POST to `/api/captures` with rawContent "Follow up with Sarah on Q3 roadmap" and type "QuickNote"
- **THEN** the system creates a Capture with the given content, type QuickNote, status Raw, and returns HTTP 201 with the created capture

#### Scenario: Create a transcript capture with optional fields

- **WHEN** an authenticated user sends a POST to `/api/captures` with rawContent containing a pasted transcript, type "Transcript", title "Leadership sync 2026-04-10", and source "leadership sync"
- **THEN** the system creates a Capture with all provided fields and returns HTTP 201

#### Scenario: Create a meeting notes capture

- **WHEN** an authenticated user sends a POST to `/api/captures` with rawContent containing meeting notes and type "MeetingNotes"
- **THEN** the system creates a Capture with type MeetingNotes and returns HTTP 201

#### Scenario: Empty content rejected

- **WHEN** an authenticated user sends a POST to `/api/captures` with empty or whitespace-only rawContent
- **THEN** the system returns HTTP 400 with a validation error

#### Scenario: User isolation

- **WHEN** User A creates a capture and User B creates a capture
- **THEN** each capture is scoped to its respective user's UserId

### Requirement: List captures

The system SHALL allow an authenticated user to retrieve a paginated list of their captures, ordered by CapturedAt descending (newest first). The list SHALL support optional filtering by capture type and processing status.

#### Scenario: List all captures

- **WHEN** an authenticated user sends a GET to `/api/captures`
- **THEN** the system returns all captures belonging to that user, ordered by CapturedAt descending

#### Scenario: Filter by type

- **WHEN** an authenticated user sends a GET to `/api/captures?type=Transcript`
- **THEN** the system returns only captures with type Transcript

#### Scenario: Filter by status

- **WHEN** an authenticated user sends a GET to `/api/captures?status=Raw`
- **THEN** the system returns only captures with status Raw

#### Scenario: Empty list

- **WHEN** an authenticated user with no captures sends a GET to `/api/captures`
- **THEN** the system returns an empty array with HTTP 200

### Requirement: Get capture by ID

The system SHALL allow an authenticated user to retrieve a single capture by ID, including all linked IDs and metadata.

#### Scenario: Get existing capture

- **WHEN** an authenticated user sends a GET to `/api/captures/{id}` with a valid capture ID belonging to them
- **THEN** the system returns the capture with all fields including linkedPersonIds, linkedInitiativeIds, title, source, and processing status

#### Scenario: Capture not found

- **WHEN** an authenticated user sends a GET to `/api/captures/{id}` with an ID that does not exist or belongs to another user
- **THEN** the system returns HTTP 404

### Requirement: Update capture metadata

The system SHALL allow an authenticated user to update a capture's title and source fields. RawContent MUST NOT be modifiable after creation. The system SHALL raise a `CaptureMetadataUpdated` domain event.

#### Scenario: Update title and source

- **WHEN** an authenticated user sends a PUT to `/api/captures/{id}` with title "1:1 with Sarah" and source "weekly 1:1"
- **THEN** the system updates the title and source, sets UpdatedAt, and returns HTTP 200

#### Scenario: Clear optional fields

- **WHEN** an authenticated user sends a PUT to `/api/captures/{id}` with title null and source null
- **THEN** the system clears the title and source fields and returns HTTP 200

#### Scenario: Cannot modify raw content

- **WHEN** an authenticated user attempts to modify the rawContent of an existing capture
- **THEN** the system SHALL NOT allow the change; rawContent is immutable after creation

### Requirement: Link capture to person

The system SHALL allow an authenticated user to link a capture to a Person by ID. The system SHALL store the PersonId in the capture's LinkedPersonIds list. Duplicate links SHALL be ignored (idempotent). The system SHALL raise a `CaptureLinkedToPerson` domain event.

#### Scenario: Link to a person

- **WHEN** an authenticated user sends a POST to `/api/captures/{id}/link-person` with personId
- **THEN** the system adds the personId to LinkedPersonIds and returns HTTP 200

#### Scenario: Duplicate link is idempotent

- **WHEN** an authenticated user links the same personId to a capture twice
- **THEN** the system does not add a duplicate; LinkedPersonIds contains the personId only once

### Requirement: Link capture to initiative

The system SHALL allow an authenticated user to link a capture to an Initiative by ID. The system SHALL store the InitiativeId in the capture's LinkedInitiativeIds list. Duplicate links SHALL be ignored (idempotent). The system SHALL raise a `CaptureLinkedToInitiative` domain event.

#### Scenario: Link to an initiative

- **WHEN** an authenticated user sends a POST to `/api/captures/{id}/link-initiative` with initiativeId
- **THEN** the system adds the initiativeId to LinkedInitiativeIds and returns HTTP 200

#### Scenario: Duplicate link is idempotent

- **WHEN** an authenticated user links the same initiativeId to a capture twice
- **THEN** the system does not add a duplicate; LinkedInitiativeIds contains the initiativeId only once

### Requirement: Unlink capture from person

The system SHALL allow an authenticated user to remove a Person link from a capture. Unlinking a non-existent link SHALL be idempotent (no error). The system SHALL raise a `CaptureUnlinkedFromPerson` domain event.

#### Scenario: Unlink a person

- **WHEN** an authenticated user sends a POST to `/api/captures/{id}/unlink-person` with a personId that is currently linked
- **THEN** the system removes the personId from LinkedPersonIds and returns HTTP 200

#### Scenario: Unlink non-existent link

- **WHEN** an authenticated user sends a POST to `/api/captures/{id}/unlink-person` with a personId that is not linked
- **THEN** the system returns HTTP 200 (idempotent, no error)

### Requirement: Unlink capture from initiative

The system SHALL allow an authenticated user to remove an Initiative link from a capture. Unlinking a non-existent link SHALL be idempotent (no error). The system SHALL raise a `CaptureUnlinkedFromInitiative` domain event.

#### Scenario: Unlink an initiative

- **WHEN** an authenticated user sends a POST to `/api/captures/{id}/unlink-initiative` with an initiativeId that is currently linked
- **THEN** the system removes the initiativeId from LinkedInitiativeIds and returns HTTP 200

#### Scenario: Unlink non-existent link

- **WHEN** an authenticated user sends a POST to `/api/captures/{id}/unlink-initiative` with an initiativeId that is not linked
- **THEN** the system returns HTTP 200 (idempotent, no error)

### Requirement: Processing status state machine

The Capture aggregate SHALL enforce a processing status state machine with transitions: Raw -> Processing (via BeginProcessing), Processing -> Processed (via CompleteProcessing), Processing -> Failed (via FailProcessing), Failed -> Raw (via RetryProcessing). Invalid transitions SHALL throw a domain exception. The system SHALL raise corresponding domain events for each transition.

#### Scenario: Begin processing from Raw

- **WHEN** BeginProcessing() is called on a capture with status Raw
- **THEN** the status transitions to Processing and a `CaptureProcessingStarted` domain event is raised

#### Scenario: Complete processing from Processing

- **WHEN** CompleteProcessing(extraction) is called on a capture with status Processing
- **THEN** the status transitions to Processed, ProcessedAt is set, and a `CaptureProcessed` domain event is raised

#### Scenario: Fail processing from Processing

- **WHEN** FailProcessing(reason) is called on a capture with status Processing
- **THEN** the status transitions to Failed and a `CaptureProcessingFailed` domain event is raised

#### Scenario: Retry processing from Failed

- **WHEN** RetryProcessing() is called on a capture with status Failed
- **THEN** the status transitions back to Raw and a `CaptureRetryRequested` domain event is raised

#### Scenario: Invalid transition rejected

- **WHEN** BeginProcessing() is called on a capture with status Processing or Processed
- **THEN** the system throws a domain exception indicating an invalid status transition

### Requirement: Capture list UI

The frontend SHALL provide a capture list page that displays captures in reverse chronological order. The list SHALL show the capture type, title (or a content preview if no title), processing status, and CapturedAt timestamp. The list SHALL support filtering by type and status using dropdown filters.

#### Scenario: View capture list

- **WHEN** a user navigates to the captures page
- **THEN** the system displays all their captures ordered by newest first, with type badge, title or content preview, status indicator, and timestamp

#### Scenario: Filter captures

- **WHEN** a user selects a type or status filter
- **THEN** the list updates to show only matching captures

### Requirement: Quick capture input

The frontend SHALL provide a quick-capture component allowing users to rapidly enter text, select a capture type, and optionally set a title and source. Submitting the form SHALL create a new capture and add it to the list.

#### Scenario: Quick capture submission

- **WHEN** a user enters text in the quick-capture input, selects a type, and submits
- **THEN** a new capture is created and appears at the top of the capture list

#### Scenario: Quick capture with optional metadata

- **WHEN** a user enters text, selects type "Transcript", sets title "Standup notes", and submits
- **THEN** a new capture is created with the provided title and type

### Requirement: Capture detail view

The frontend SHALL provide a detail view for a single capture showing the full raw content, metadata (type, status, title, source, timestamps), and linked people and initiatives. The detail view SHALL allow editing the title and source, and linking/unlinking people and initiatives.

#### Scenario: View capture detail

- **WHEN** a user clicks on a capture in the list
- **THEN** the system navigates to the detail view showing the full content, metadata, and linked entities

#### Scenario: Edit metadata from detail view

- **WHEN** a user edits the title or source in the detail view and saves
- **THEN** the metadata is updated and the view reflects the changes

#### Scenario: Link person from detail view

- **WHEN** a user adds a person link from the detail view
- **THEN** the person appears in the linked people section
