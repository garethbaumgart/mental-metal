# Commitment Tracking

## Requirements

### Requirement: Create a commitment

The system SHALL allow an authenticated user to create a new Commitment with a description, direction (MineToThem or TheirsToMe), and a PersonId. Description MUST NOT be empty. PersonId MUST be provided. The system SHALL set Status to `Open` and set CreatedAt. Optional fields: DueDate, InitiativeId, SourceCaptureId, Notes. The system SHALL raise a `CommitmentCreated` domain event. The Commitment SHALL be scoped to the authenticated user's UserId.

#### Scenario: Create a commitment I owe to someone

- **WHEN** an authenticated user sends a POST to `/api/commitments` with description "Send Q3 roadmap draft", direction "MineToThem", and personId for "Sarah"
- **THEN** the system creates a Commitment with status Open, direction MineToThem, and returns HTTP 201

#### Scenario: Create a commitment someone owes me

- **WHEN** an authenticated user sends a POST to `/api/commitments` with description "Deliver design doc", direction "TheirsToMe", personId, and dueDate "2026-04-20"
- **THEN** the system creates a Commitment with status Open, direction TheirsToMe, the due date, and returns HTTP 201

#### Scenario: Create with optional initiative and capture links

- **WHEN** an authenticated user sends a POST to `/api/commitments` with description, direction, personId, initiativeId, and sourceCaptureId
- **THEN** the system creates a Commitment with all linked IDs and returns HTTP 201

#### Scenario: Empty description rejected

- **WHEN** an authenticated user sends a POST to `/api/commitments` with an empty description
- **THEN** the system returns HTTP 400 with a validation error

#### Scenario: Missing personId rejected

- **WHEN** an authenticated user sends a POST to `/api/commitments` without a personId
- **THEN** the system returns HTTP 400 with a validation error

#### Scenario: User isolation

- **WHEN** User A creates a commitment and User B creates a commitment
- **THEN** each commitment is scoped to its respective user's UserId

### Requirement: List commitments

The system SHALL allow an authenticated user to retrieve a list of their commitments. The list SHALL support optional filtering by direction, status, personId, initiativeId, and overdue state. The list SHALL be ordered by DueDate ascending (soonest first), with null due dates last.

#### Scenario: List all commitments

- **WHEN** an authenticated user sends a GET to `/api/commitments`
- **THEN** the system returns all commitments belonging to that user, ordered by due date

#### Scenario: Filter by direction

- **WHEN** an authenticated user sends a GET to `/api/commitments?direction=MineToThem`
- **THEN** the system returns only commitments with direction MineToThem

#### Scenario: Filter by status

- **WHEN** an authenticated user sends a GET to `/api/commitments?status=Open`
- **THEN** the system returns only commitments with status Open

#### Scenario: Filter by person

- **WHEN** an authenticated user sends a GET to `/api/commitments?personId={id}`
- **THEN** the system returns only commitments linked to that person

#### Scenario: Filter overdue

- **WHEN** an authenticated user sends a GET to `/api/commitments?overdue=true`
- **THEN** the system returns only open commitments whose DueDate is in the past

#### Scenario: Empty list

- **WHEN** an authenticated user with no commitments sends a GET to `/api/commitments`
- **THEN** the system returns an empty array with HTTP 200

### Requirement: Get commitment by ID

The system SHALL allow an authenticated user to retrieve a single commitment by ID.

#### Scenario: Get existing commitment

- **WHEN** an authenticated user sends a GET to `/api/commitments/{id}` with a valid commitment ID
- **THEN** the system returns the commitment with all fields including linked IDs and computed IsOverdue

#### Scenario: Commitment not found

- **WHEN** an authenticated user sends a GET to `/api/commitments/{id}` with an ID that does not exist or belongs to another user
- **THEN** the system returns HTTP 404

### Requirement: Update commitment

The system SHALL allow an authenticated user to update a commitment's description and notes. Description MUST NOT be empty. The system SHALL raise a `CommitmentDescriptionUpdated` domain event when the description changes.

#### Scenario: Update description

- **WHEN** an authenticated user sends a PUT to `/api/commitments/{id}` with a new description
- **THEN** the system updates the description, sets UpdatedAt, and returns HTTP 200

#### Scenario: Update notes

- **WHEN** an authenticated user sends a PUT to `/api/commitments/{id}` with new notes
- **THEN** the system updates the notes and returns HTTP 200

#### Scenario: Empty description rejected

- **WHEN** an authenticated user sends a PUT to `/api/commitments/{id}` with an empty description
- **THEN** the system returns HTTP 400

### Requirement: Complete a commitment

The system SHALL allow an authenticated user to mark an open commitment as completed. The system SHALL set CompletedAt and optionally accept completion notes. The system SHALL raise a `CommitmentCompleted` domain event. Only commitments with status `Open` can be completed.

#### Scenario: Complete an open commitment

- **WHEN** an authenticated user sends a POST to `/api/commitments/{id}/complete` for an open commitment
- **THEN** the system sets status to Completed, sets CompletedAt, and returns HTTP 200

#### Scenario: Complete with notes

- **WHEN** an authenticated user sends a POST to `/api/commitments/{id}/complete` with notes "Delivered in leadership sync"
- **THEN** the system sets status to Completed, appends the notes, and returns HTTP 200

#### Scenario: Complete non-open commitment rejected

- **WHEN** an authenticated user sends a POST to `/api/commitments/{id}/complete` for a commitment that is already Completed or Cancelled
- **THEN** the system returns HTTP 409 Conflict

### Requirement: Cancel a commitment

The system SHALL allow an authenticated user to cancel an open commitment. The system SHALL optionally accept a cancellation reason. The system SHALL raise a `CommitmentCancelled` domain event. Only commitments with status `Open` can be cancelled.

#### Scenario: Cancel an open commitment

- **WHEN** an authenticated user sends a POST to `/api/commitments/{id}/cancel` for an open commitment
- **THEN** the system sets status to Cancelled and returns HTTP 200

#### Scenario: Cancel with reason

- **WHEN** an authenticated user sends a POST to `/api/commitments/{id}/cancel` with reason "No longer relevant after reorg"
- **THEN** the system sets status to Cancelled, stores the reason in notes, and returns HTTP 200

#### Scenario: Cancel non-open commitment rejected

- **WHEN** an authenticated user sends a POST to `/api/commitments/{id}/cancel` for a commitment that is already Completed or Cancelled
- **THEN** the system returns HTTP 409 Conflict

### Requirement: Reopen a commitment

The system SHALL allow an authenticated user to reopen a completed or cancelled commitment. The system SHALL set status back to Open, clear CompletedAt, and raise a `CommitmentReopened` domain event. Only commitments with status `Completed` or `Cancelled` can be reopened.

#### Scenario: Reopen a completed commitment

- **WHEN** an authenticated user sends a POST to `/api/commitments/{id}/reopen` for a completed commitment
- **THEN** the system sets status to Open, clears CompletedAt, and returns HTTP 200

#### Scenario: Reopen a cancelled commitment

- **WHEN** an authenticated user sends a POST to `/api/commitments/{id}/reopen` for a cancelled commitment
- **THEN** the system sets status to Open and returns HTTP 200

#### Scenario: Reopen an open commitment rejected

- **WHEN** an authenticated user sends a POST to `/api/commitments/{id}/reopen` for a commitment that is already Open
- **THEN** the system returns HTTP 409 Conflict

### Requirement: Update due date

The system SHALL allow an authenticated user to update or clear a commitment's due date. The system SHALL raise a `CommitmentDueDateChanged` domain event.

#### Scenario: Set due date

- **WHEN** an authenticated user sends a PUT to `/api/commitments/{id}/due-date` with dueDate "2026-05-01"
- **THEN** the system updates the due date and returns HTTP 200

#### Scenario: Clear due date

- **WHEN** an authenticated user sends a PUT to `/api/commitments/{id}/due-date` with dueDate null
- **THEN** the system clears the due date and returns HTTP 200

### Requirement: Link commitment to initiative

The system SHALL allow an authenticated user to link a commitment to an Initiative by ID. The system SHALL raise a `CommitmentLinkedToInitiative` domain event. Linking when already linked to the same initiative SHALL be idempotent.

#### Scenario: Link to an initiative

- **WHEN** an authenticated user sends a POST to `/api/commitments/{id}/link-initiative` with initiativeId
- **THEN** the system sets the InitiativeId and returns HTTP 200

#### Scenario: Replace initiative link

- **WHEN** an authenticated user sends a POST to `/api/commitments/{id}/link-initiative` with a different initiativeId
- **THEN** the system updates the InitiativeId to the new value and returns HTTP 200

### Requirement: Overdue detection

The system SHALL compute an `IsOverdue` property for each commitment. A commitment is overdue when its Status is `Open` and its DueDate is before the current date. Completed and cancelled commitments are never overdue. Commitments without a DueDate are never overdue.

#### Scenario: Open commitment past due date

- **WHEN** a commitment has status Open and DueDate is in the past
- **THEN** the system reports IsOverdue as true

#### Scenario: Completed commitment past due date

- **WHEN** a commitment has status Completed and DueDate is in the past
- **THEN** the system reports IsOverdue as false

#### Scenario: Open commitment with no due date

- **WHEN** a commitment has status Open and DueDate is null
- **THEN** the system reports IsOverdue as false

#### Scenario: Open commitment with future due date

- **WHEN** a commitment has status Open and DueDate is in the future
- **THEN** the system reports IsOverdue as false

### Requirement: Commitment list UI

The frontend SHALL provide a commitment list page that displays commitments with direction indicator, description, person name, status badge, due date, and overdue indicator. The list SHALL support filtering by direction, status, and overdue state.

#### Scenario: View commitment list

- **WHEN** a user navigates to the commitments page
- **THEN** the system displays all their commitments with direction, description, person, status, due date, and overdue indicators

#### Scenario: Filter commitments

- **WHEN** a user selects direction, status, or overdue filters
- **THEN** the list updates to show only matching commitments

### Requirement: Commitment create/edit form

The frontend SHALL provide a form for creating and editing commitments. The form SHALL include fields for description, direction selector, person selector, due date picker, initiative selector (optional), and notes. The person selector SHALL show the user's active people.

#### Scenario: Create commitment via form

- **WHEN** a user fills in the commitment form with description, selects direction, picks a person, and submits
- **THEN** a new commitment is created and appears in the list

#### Scenario: Edit commitment

- **WHEN** a user edits a commitment's description or notes and saves
- **THEN** the commitment is updated and the view reflects the changes

### Requirement: Commitment status actions

The frontend SHALL provide action buttons to complete, cancel, and reopen commitments directly from the list or detail view. Status transitions SHALL provide appropriate confirmation or optional notes input.

#### Scenario: Complete from list

- **WHEN** a user clicks "Complete" on an open commitment in the list
- **THEN** the commitment is marked as completed and the status badge updates

#### Scenario: Cancel with reason

- **WHEN** a user clicks "Cancel" on an open commitment and provides a reason
- **THEN** the commitment is marked as cancelled with the reason stored
