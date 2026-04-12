## ADDED Requirements

### Requirement: Create a delegation

The system SHALL allow an authenticated user to create a new Delegation with a description and a DelegatePersonId (the person doing the work). Description MUST NOT be empty. DelegatePersonId MUST be provided. The system SHALL set Status to `Assigned` and set CreatedAt. Optional fields: DueDate, InitiativeId, SourceCaptureId, Priority (defaults to Medium), Notes. The system SHALL raise a `DelegationCreated` domain event. The Delegation SHALL be scoped to the authenticated user's UserId.

#### Scenario: Create a delegation with defaults

- **WHEN** an authenticated user sends a POST to `/api/delegations` with description "Write the API spec for payments service" and delegatePersonId
- **THEN** the system creates a Delegation with status Assigned, priority Medium, and returns HTTP 201

#### Scenario: Create a delegation with all fields

- **WHEN** an authenticated user sends a POST to `/api/delegations` with description, delegatePersonId, dueDate "2026-04-25", priority "High", initiativeId, and notes "Discussed in team standup"
- **THEN** the system creates a Delegation with all provided fields and returns HTTP 201

#### Scenario: Empty description rejected

- **WHEN** an authenticated user sends a POST to `/api/delegations` with an empty description
- **THEN** the system returns HTTP 400 with a validation error

#### Scenario: Missing delegatePersonId rejected

- **WHEN** an authenticated user sends a POST to `/api/delegations` without a delegatePersonId
- **THEN** the system returns HTTP 400 with a validation error

#### Scenario: User isolation

- **WHEN** User A creates a delegation and User B creates a delegation
- **THEN** each delegation is scoped to its respective user's UserId

### Requirement: List delegations

The system SHALL allow an authenticated user to retrieve a list of their delegations. The list SHALL support optional filtering by status, priority, delegatePersonId, and initiativeId. The list SHALL be ordered by priority descending then DueDate ascending (soonest first), with null due dates last.

#### Scenario: List all delegations

- **WHEN** an authenticated user sends a GET to `/api/delegations`
- **THEN** the system returns all delegations belonging to that user, ordered by priority then due date

#### Scenario: Filter by status

- **WHEN** an authenticated user sends a GET to `/api/delegations?status=Assigned`
- **THEN** the system returns only delegations with status Assigned

#### Scenario: Filter by priority

- **WHEN** an authenticated user sends a GET to `/api/delegations?priority=High`
- **THEN** the system returns only delegations with priority High

#### Scenario: Filter by person

- **WHEN** an authenticated user sends a GET to `/api/delegations?delegatePersonId={id}`
- **THEN** the system returns only delegations assigned to that person

#### Scenario: Empty list

- **WHEN** an authenticated user with no delegations sends a GET to `/api/delegations`
- **THEN** the system returns an empty array with HTTP 200

### Requirement: Get delegation by ID

The system SHALL allow an authenticated user to retrieve a single delegation by ID.

#### Scenario: Get existing delegation

- **WHEN** an authenticated user sends a GET to `/api/delegations/{id}` with a valid delegation ID
- **THEN** the system returns the delegation with all fields including linked IDs and LastFollowedUpAt

#### Scenario: Delegation not found

- **WHEN** an authenticated user sends a GET to `/api/delegations/{id}` with an ID that does not exist or belongs to another user
- **THEN** the system returns HTTP 404

### Requirement: Update delegation

The system SHALL allow an authenticated user to update a delegation's description and notes. Description MUST NOT be empty. The system SHALL set UpdatedAt.

#### Scenario: Update description

- **WHEN** an authenticated user sends a PUT to `/api/delegations/{id}` with a new description
- **THEN** the system updates the description and returns HTTP 200

#### Scenario: Update notes

- **WHEN** an authenticated user sends a PUT to `/api/delegations/{id}` with new notes
- **THEN** the system updates the notes and returns HTTP 200

#### Scenario: Empty description rejected

- **WHEN** an authenticated user sends a PUT to `/api/delegations/{id}` with an empty description
- **THEN** the system returns HTTP 400

### Requirement: Mark delegation in progress

The system SHALL allow an authenticated user to mark an assigned delegation as in-progress. The system SHALL raise a `DelegationStarted` domain event. Only delegations with status `Assigned` can be marked in-progress.

#### Scenario: Start an assigned delegation

- **WHEN** an authenticated user sends a POST to `/api/delegations/{id}/start` for an assigned delegation
- **THEN** the system sets status to InProgress and returns HTTP 200

#### Scenario: Start non-assigned delegation rejected

- **WHEN** an authenticated user sends a POST to `/api/delegations/{id}/start` for a delegation that is not in Assigned status
- **THEN** the system returns HTTP 409 Conflict

### Requirement: Complete a delegation

The system SHALL allow an authenticated user to mark a delegation as completed. The system SHALL set CompletedAt and optionally accept completion notes. The system SHALL raise a `DelegationCompleted` domain event. Only delegations with status `Assigned`, `InProgress`, or `Blocked` can be completed.

#### Scenario: Complete an in-progress delegation

- **WHEN** an authenticated user sends a POST to `/api/delegations/{id}/complete` for an in-progress delegation
- **THEN** the system sets status to Completed, sets CompletedAt, and returns HTTP 200

#### Scenario: Complete an assigned delegation directly

- **WHEN** an authenticated user sends a POST to `/api/delegations/{id}/complete` for an assigned delegation
- **THEN** the system sets status to Completed, sets CompletedAt, and returns HTTP 200

#### Scenario: Complete a blocked delegation

- **WHEN** an authenticated user sends a POST to `/api/delegations/{id}/complete` for a blocked delegation
- **THEN** the system sets status to Completed, sets CompletedAt, and returns HTTP 200

#### Scenario: Complete already-completed delegation rejected

- **WHEN** an authenticated user sends a POST to `/api/delegations/{id}/complete` for a delegation already in Completed status
- **THEN** the system returns HTTP 409 Conflict

### Requirement: Mark delegation as blocked

The system SHALL allow an authenticated user to mark a delegation as blocked with a reason. The system SHALL raise a `DelegationBlocked` domain event. Only delegations with status `Assigned` or `InProgress` can be blocked.

#### Scenario: Block an in-progress delegation

- **WHEN** an authenticated user sends a POST to `/api/delegations/{id}/block` with reason "Waiting on third-party API access"
- **THEN** the system sets status to Blocked, stores the reason, and returns HTTP 200

#### Scenario: Block an assigned delegation

- **WHEN** an authenticated user sends a POST to `/api/delegations/{id}/block` with reason
- **THEN** the system sets status to Blocked and returns HTTP 200

#### Scenario: Block already blocked/completed rejected

- **WHEN** an authenticated user sends a POST to `/api/delegations/{id}/block` for a delegation in Blocked or Completed status
- **THEN** the system returns HTTP 409 Conflict

### Requirement: Unblock a delegation

The system SHALL allow an authenticated user to unblock a blocked delegation, returning it to InProgress status. The system SHALL raise a `DelegationUnblocked` domain event. Only delegations with status `Blocked` can be unblocked.

#### Scenario: Unblock a blocked delegation

- **WHEN** an authenticated user sends a POST to `/api/delegations/{id}/unblock` for a blocked delegation
- **THEN** the system sets status to InProgress and returns HTTP 200

#### Scenario: Unblock non-blocked delegation rejected

- **WHEN** an authenticated user sends a POST to `/api/delegations/{id}/unblock` for a delegation not in Blocked status
- **THEN** the system returns HTTP 409 Conflict

### Requirement: Record follow-up

The system SHALL allow an authenticated user to record a follow-up on a delegation. The system SHALL update LastFollowedUpAt to the current time and optionally accept follow-up notes. The system SHALL raise a `DelegationFollowedUp` domain event.

#### Scenario: Record follow-up

- **WHEN** an authenticated user sends a POST to `/api/delegations/{id}/follow-up` with optional notes "Checked in, on track for Friday"
- **THEN** the system updates LastFollowedUpAt and returns HTTP 200

#### Scenario: Record follow-up without notes

- **WHEN** an authenticated user sends a POST to `/api/delegations/{id}/follow-up` without notes
- **THEN** the system updates LastFollowedUpAt and returns HTTP 200

### Requirement: Update due date

The system SHALL allow an authenticated user to update or clear a delegation's due date. The system SHALL raise a `DelegationDueDateChanged` domain event.

#### Scenario: Set due date

- **WHEN** an authenticated user sends a PUT to `/api/delegations/{id}/due-date` with dueDate "2026-05-01"
- **THEN** the system updates the due date and returns HTTP 200

#### Scenario: Clear due date

- **WHEN** an authenticated user sends a PUT to `/api/delegations/{id}/due-date` with dueDate null
- **THEN** the system clears the due date and returns HTTP 200

### Requirement: Change priority

The system SHALL allow an authenticated user to change a delegation's priority (Low, Medium, High, Urgent). The system SHALL raise a `DelegationReprioritized` domain event.

#### Scenario: Change priority

- **WHEN** an authenticated user sends a PUT to `/api/delegations/{id}/priority` with priority "Urgent"
- **THEN** the system updates the priority and returns HTTP 200

### Requirement: Reassign delegation

The system SHALL allow an authenticated user to reassign a delegation to a different person. The system SHALL update DelegatePersonId and raise a `DelegationReassigned` domain event.

#### Scenario: Reassign to a different person

- **WHEN** an authenticated user sends a POST to `/api/delegations/{id}/reassign` with a new delegatePersonId
- **THEN** the system updates the DelegatePersonId and returns HTTP 200

#### Scenario: Reassign to same person is idempotent

- **WHEN** an authenticated user sends a POST to `/api/delegations/{id}/reassign` with the current delegatePersonId
- **THEN** the system returns HTTP 200 without raising an event

### Requirement: Delegation list UI

The frontend SHALL provide a delegation list page that displays delegations with description, delegate person name, status badge, priority indicator, due date, and last follow-up timestamp. The list SHALL support filtering by status and priority.

#### Scenario: View delegation list

- **WHEN** a user navigates to the delegations page
- **THEN** the system displays all their delegations with description, person, status, priority, due date, and last follow-up time

#### Scenario: Filter delegations

- **WHEN** a user selects status or priority filters
- **THEN** the list updates to show only matching delegations

### Requirement: Delegation create/edit form

The frontend SHALL provide a form for creating and editing delegations. The form SHALL include fields for description, person selector, due date picker, priority selector, initiative selector (optional), and notes.

#### Scenario: Create delegation via form

- **WHEN** a user fills in the delegation form with description, picks a person, and submits
- **THEN** a new delegation is created and appears in the list

#### Scenario: Edit delegation

- **WHEN** a user edits a delegation's description or notes and saves
- **THEN** the delegation is updated and the view reflects the changes

### Requirement: Delegation status and follow-up actions

The frontend SHALL provide action buttons for status transitions (Start, Complete, Block, Unblock) and follow-up recording directly from the list or detail view.

#### Scenario: Complete from list

- **WHEN** a user clicks "Complete" on a delegation in the list
- **THEN** the delegation is marked as completed and the status badge updates

#### Scenario: Record follow-up from detail

- **WHEN** a user clicks "Follow Up" on a delegation and optionally adds notes
- **THEN** the LastFollowedUpAt updates and the follow-up is recorded
