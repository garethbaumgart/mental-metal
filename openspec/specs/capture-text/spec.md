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

The frontend SHALL provide a Quick Capture dialog that is reachable from every authenticated page via (a) a global keyboard shortcut and (b) a persistent floating action button (FAB). The dialog's happy path SHALL require only the content textarea; the client SHALL default new captures to `CaptureType.QuickNote`. Type selection, Title, and Source SHALL be available in an "Advanced" section that is collapsed by default. Submitting the form SHALL create a new capture via the existing `POST /api/captures` endpoint, passing the defaulted or user-selected type.

#### Scenario: Quick capture submission with defaults

- **WHEN** an authenticated user opens the Quick Capture dialog, enters text, and submits without expanding the Advanced section
- **THEN** the system creates a capture with `type = QuickNote` and the entered content, and the capture is added to the user's capture list

#### Scenario: Quick capture with Advanced metadata

- **WHEN** an authenticated user opens the Quick Capture dialog, enters text, expands the Advanced section, selects type "Transcript", sets title "Standup notes", and submits
- **THEN** the system creates a capture with type Transcript, title "Standup notes", and the entered content

#### Scenario: Empty content disables submit

- **WHEN** the Quick Capture dialog is open and the content textarea is empty or whitespace-only
- **THEN** the Capture submit button is disabled and Enter does not submit

#### Scenario: Advanced section collapsed by default

- **WHEN** an authenticated user opens the Quick Capture dialog
- **THEN** the Advanced section (containing Type, Title, and Source) is collapsed and the content textarea is focused

### Requirement: Global Quick Capture keyboard shortcut

The frontend SHALL register a global keyboard shortcut that opens the Quick Capture dialog from any authenticated page. The shortcut SHALL be `Cmd+K` on macOS and `Ctrl+K` on non-macOS platforms. The shortcut SHALL call `preventDefault()` to override any browser default. The shortcut SHALL NOT fire on unauthenticated pages (login, sign-up). When the Quick Capture dialog is already open, the shortcut SHALL be a no-op.

#### Scenario: Shortcut opens dialog on macOS

- **WHEN** an authenticated user on macOS presses Cmd+K on any authenticated page
- **THEN** the Quick Capture dialog opens with focus in the content textarea

#### Scenario: Shortcut opens dialog on Windows/Linux

- **WHEN** an authenticated user on Windows or Linux presses Ctrl+K on any authenticated page
- **THEN** the Quick Capture dialog opens with focus in the content textarea

#### Scenario: Shortcut does not fire on unauthenticated pages

- **WHEN** an unauthenticated user on the login page presses Cmd+K or Ctrl+K
- **THEN** the Quick Capture dialog does not open and the browser default behavior is preserved

#### Scenario: Shortcut no-op when dialog open

- **WHEN** the Quick Capture dialog is already open and the user presses Cmd+K or Ctrl+K
- **THEN** the dialog remains open unchanged (no re-open, no focus reset)

### Requirement: Persistent Quick Capture floating action button

The frontend SHALL render a persistent floating action button (FAB) on every authenticated page. Clicking the FAB SHALL open the Quick Capture dialog. The FAB SHALL use PrimeNG theming (no hardcoded colours) and SHALL be positioned fixed in the bottom-right of the viewport. The FAB SHALL have an accessible label that mentions the keyboard shortcut.

#### Scenario: FAB visible on authenticated pages

- **WHEN** an authenticated user is on any authenticated page (captures list, people, initiatives, settings, etc.)
- **THEN** the Quick Capture FAB is visible fixed in the bottom-right of the viewport

#### Scenario: FAB hidden on unauthenticated pages

- **WHEN** a user is on an unauthenticated page such as the login page
- **THEN** the Quick Capture FAB is not rendered

#### Scenario: FAB click opens dialog

- **WHEN** an authenticated user clicks the Quick Capture FAB
- **THEN** the Quick Capture dialog opens with focus in the content textarea

#### Scenario: FAB has accessible label

- **WHEN** a screen reader user focuses the Quick Capture FAB
- **THEN** the accessible label announces "Quick capture" and includes the keyboard shortcut hint

### Requirement: Enter submits Quick Capture

The Quick Capture dialog SHALL submit the capture when the user presses Enter (without Shift) inside the content textarea, provided the content is non-empty. Shift+Enter SHALL insert a newline without submitting. Cmd+Enter on macOS and Ctrl+Enter on non-macOS SHALL also submit from anywhere within the dialog.

#### Scenario: Enter submits

- **WHEN** the user has entered non-empty content in the Quick Capture dialog and presses Enter without Shift
- **THEN** the capture is submitted and the dialog closes on success

#### Scenario: Shift+Enter inserts newline

- **WHEN** the user presses Shift+Enter inside the content textarea
- **THEN** a newline is inserted in the textarea and no submission occurs

#### Scenario: Cmd/Ctrl+Enter submits from dialog

- **WHEN** the user presses Cmd+Enter (macOS) or Ctrl+Enter (other) anywhere inside the open dialog with non-empty content
- **THEN** the capture is submitted

#### Scenario: Enter with empty content does nothing

- **WHEN** the user presses Enter in the Quick Capture dialog while the content textarea is empty or whitespace-only
- **THEN** no submission occurs and the dialog remains open

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
