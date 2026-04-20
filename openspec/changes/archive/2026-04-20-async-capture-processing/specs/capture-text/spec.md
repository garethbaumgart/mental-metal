## MODIFIED Requirements

### Requirement: Create a capture

The system SHALL allow an authenticated user to create a new Capture with raw text content and a capture type (QuickNote, Transcript, or MeetingNotes). RawContent MUST NOT be empty. The system SHALL set ProcessingStatus to `Raw`, set CapturedAt to the current time, and scope the Capture to the authenticated user's UserId. The system SHALL raise a `CaptureCreated` domain event. The API SHALL return HTTP 201 immediately with the capture in `Raw` status. The system SHALL trigger AI extraction as a background task after the response is sent — the HTTP response MUST NOT wait for extraction to complete.

#### Scenario: Create a quick note

- **WHEN** an authenticated user sends a POST to `/api/captures` with rawContent "Follow up with Sarah on Q3 roadmap" and type "QuickNote"
- **THEN** the system creates a Capture with the given content, type QuickNote, status Raw, and returns HTTP 201 with the created capture
- **AND** AI extraction begins asynchronously in the background

#### Scenario: Create a transcript capture with optional fields

- **WHEN** an authenticated user sends a POST to `/api/captures` with rawContent containing a pasted transcript, type "Transcript", title "Leadership sync 2026-04-10", and source "leadership sync"
- **THEN** the system creates a Capture with all provided fields, returns HTTP 201
- **AND** the response returns within 500ms (extraction runs in background)

#### Scenario: Create a meeting notes capture

- **WHEN** an authenticated user sends a POST to `/api/captures` with rawContent containing meeting notes and type "MeetingNotes"
- **THEN** the system creates a Capture with type MeetingNotes, returns HTTP 201 with status `Raw`
- **AND** the capture transitions to `Processed` asynchronously after extraction completes

#### Scenario: Empty content rejected

- **WHEN** an authenticated user sends a POST to `/api/captures` with empty or whitespace-only rawContent
- **THEN** the system returns HTTP 400 with a validation error

#### Scenario: User isolation

- **WHEN** User A creates a capture and User B creates a capture
- **THEN** each capture is scoped to its respective user's UserId
