## ADDED Requirements

### Requirement: Create an initiative

The system SHALL allow an authenticated user to create a new Initiative with a title. Title MUST NOT be empty. The Initiative SHALL be created with status Active. The system SHALL raise an `InitiativeCreated` domain event. The Initiative SHALL be scoped to the authenticated user's UserId.

#### Scenario: Create an initiative

- **WHEN** an authenticated user sends a POST to `/api/initiatives` with title "Platform Migration Q3"
- **THEN** the system creates an Initiative with the given title, status Active, sets CreatedAt and UpdatedAt, and returns the created initiative with HTTP 201

#### Scenario: Empty title rejected

- **WHEN** an authenticated user sends a POST to `/api/initiatives` with an empty title
- **THEN** the system returns HTTP 400 with a validation error

#### Scenario: User isolation

- **WHEN** User A creates an initiative titled "Migration" and User B creates an initiative titled "Migration"
- **THEN** both operations succeed because initiative titles are not required to be unique

### Requirement: List initiatives

The system SHALL allow an authenticated user to retrieve a list of their initiatives. The list SHALL support optional filtering by status.

#### Scenario: List all initiatives

- **WHEN** an authenticated user sends a GET to `/api/initiatives`
- **THEN** the system returns all initiatives belonging to that user

#### Scenario: Filter by status

- **WHEN** an authenticated user sends a GET to `/api/initiatives?status=Active`
- **THEN** the system returns only initiatives with status Active

#### Scenario: Empty list

- **WHEN** an authenticated user with no initiatives sends a GET to `/api/initiatives`
- **THEN** the system returns an empty array with HTTP 200

### Requirement: Get initiative by ID

The system SHALL allow an authenticated user to retrieve a single initiative by ID, including its milestones and linked person IDs.

#### Scenario: Get existing initiative

- **WHEN** an authenticated user sends a GET to `/api/initiatives/{id}` with a valid initiative ID
- **THEN** the system returns the initiative with all details including milestones and linked person IDs

#### Scenario: Initiative not found

- **WHEN** an authenticated user sends a GET to `/api/initiatives/{id}` with an ID that does not exist or belongs to another user
- **THEN** the system returns HTTP 404

### Requirement: Update initiative title

The system SHALL allow an authenticated user to update an initiative's title. Title MUST NOT be empty. The system SHALL reject updates to initiatives in terminal status (Completed, Cancelled). The system SHALL raise an `InitiativeTitleUpdated` domain event.

#### Scenario: Update title successfully

- **WHEN** an authenticated user sends a PUT to `/api/initiatives/{id}` with a new title for an Active initiative
- **THEN** the system updates the title, sets UpdatedAt, and returns the updated initiative

#### Scenario: Empty title rejected

- **WHEN** an authenticated user sends a PUT with an empty title
- **THEN** the system returns HTTP 400 with a validation error

#### Scenario: Update terminal initiative rejected

- **WHEN** an authenticated user sends a PUT to update a Completed or Cancelled initiative
- **THEN** the system returns HTTP 400 indicating terminal initiatives cannot be modified

### Requirement: Change initiative status

The system SHALL enforce the initiative status state machine. Valid transitions: Active → OnHold, Active → Completed, Active → Cancelled, OnHold → Active. Completed and Cancelled are terminal states with no outward transitions. The system SHALL raise an `InitiativeStatusChanged` domain event.

#### Scenario: Put initiative on hold

- **WHEN** an authenticated user sends a PUT to `/api/initiatives/{id}/status` with status "OnHold" for an Active initiative
- **THEN** the system changes status to OnHold and returns the updated initiative

#### Scenario: Resume initiative from hold

- **WHEN** an authenticated user sends a PUT to `/api/initiatives/{id}/status` with status "Active" for an OnHold initiative
- **THEN** the system changes status to Active and returns the updated initiative

#### Scenario: Complete an initiative

- **WHEN** an authenticated user sends a PUT to `/api/initiatives/{id}/status` with status "Completed" for an Active initiative
- **THEN** the system changes status to Completed and returns the updated initiative

#### Scenario: Cancel an initiative

- **WHEN** an authenticated user sends a PUT to `/api/initiatives/{id}/status` with status "Cancelled" for an Active initiative
- **THEN** the system changes status to Cancelled and returns the updated initiative

#### Scenario: Invalid transition rejected

- **WHEN** an authenticated user sends a PUT to change status from OnHold to Completed (must go through Active first)
- **THEN** the system returns HTTP 400 indicating invalid status transition

#### Scenario: Transition from terminal state rejected

- **WHEN** an authenticated user tries to change status of a Completed or Cancelled initiative
- **THEN** the system returns HTTP 400 indicating terminal status cannot be changed

#### Scenario: Same status is no-op

- **WHEN** an authenticated user sends a PUT to change status to the initiative's current status
- **THEN** the system returns the initiative unchanged with HTTP 200

### Requirement: Manage milestones

The system SHALL allow an authenticated user to add, update, remove, and complete milestones on an initiative. Milestones have a title (required), target date (required), and optional description. The system SHALL reject milestone operations on terminal initiatives. The system SHALL raise a `MilestoneSet` domain event on add/update and a `MilestoneCompleted` domain event on completion.

#### Scenario: Add a milestone

- **WHEN** an authenticated user sends a POST to `/api/initiatives/{id}/milestones` with title "Alpha Release" and targetDate "2026-06-15"
- **THEN** the system adds the milestone and returns the updated initiative

#### Scenario: Update a milestone

- **WHEN** an authenticated user sends a PUT to `/api/initiatives/{id}/milestones/{milestoneId}` with an updated targetDate
- **THEN** the system updates the milestone and returns the updated initiative

#### Scenario: Remove a milestone

- **WHEN** an authenticated user sends a DELETE to `/api/initiatives/{id}/milestones/{milestoneId}`
- **THEN** the system removes the milestone and returns HTTP 204

#### Scenario: Complete a milestone

- **WHEN** an authenticated user sends a POST to `/api/initiatives/{id}/milestones/{milestoneId}/complete`
- **THEN** the system marks the milestone as completed and returns the updated initiative

#### Scenario: Milestone on terminal initiative rejected

- **WHEN** an authenticated user tries to add/update/remove a milestone on a Completed or Cancelled initiative
- **THEN** the system returns HTTP 400 indicating terminal initiatives cannot be modified

#### Scenario: Milestone title required

- **WHEN** an authenticated user sends a POST to add a milestone with an empty title
- **THEN** the system returns HTTP 400 with a validation error

### Requirement: Link and unlink people

The system SHALL allow an authenticated user to link and unlink people to/from an initiative by person ID. The system SHALL validate that the person ID belongs to the user. The system SHALL reject link operations on terminal initiatives. The system SHALL raise a `PersonLinkedToInitiative` domain event on link and a `PersonUnlinkedFromInitiative` domain event on unlink.

#### Scenario: Link a person

- **WHEN** an authenticated user sends a POST to `/api/initiatives/{id}/link-person` with a valid personId
- **THEN** the system adds the personId to LinkedPersonIds and returns the updated initiative

#### Scenario: Link duplicate person is idempotent

- **WHEN** an authenticated user links a person who is already linked to the initiative
- **THEN** the system returns HTTP 200 without duplicating the link or raising a duplicate event

#### Scenario: Unlink a person

- **WHEN** an authenticated user sends a DELETE to `/api/initiatives/{id}/link-person/{personId}`
- **THEN** the system removes the personId from LinkedPersonIds and returns HTTP 204

#### Scenario: Link person on terminal initiative rejected

- **WHEN** an authenticated user tries to link a person to a Completed or Cancelled initiative
- **THEN** the system returns HTTP 400 indicating terminal initiatives cannot be modified

#### Scenario: Link non-existent person rejected

- **WHEN** an authenticated user sends a POST to link a personId that does not exist or belongs to another user
- **THEN** the system returns HTTP 400 indicating the person was not found

### Requirement: Multi-tenant initiative isolation

All initiative data SHALL be automatically scoped to the authenticated user's UserId via EF Core global query filters. A user SHALL NOT be able to access, modify, or list another user's initiatives.

#### Scenario: Cross-tenant access prevented

- **WHEN** User A attempts to GET `/api/initiatives/{id}` with an ID belonging to User B
- **THEN** the system returns HTTP 404 (not 403, to avoid leaking existence)

### Requirement: Frontend initiatives list page

The Angular application SHALL provide an initiatives list page displaying all initiatives in a PrimeNG Table with columns for title, status, and milestone count. The list SHALL support filtering by status. Status SHALL be displayed as coloured badges.

#### Scenario: View initiatives list

- **WHEN** an authenticated user navigates to the initiatives page
- **THEN** the page displays a table of all initiatives with title, status badge, and milestone count

#### Scenario: Filter by status

- **WHEN** a user selects a status filter (e.g., "Active")
- **THEN** the table shows only initiatives with that status

#### Scenario: Empty state

- **WHEN** a user has no initiatives
- **THEN** the page displays an empty state message with a prompt to create their first initiative

### Requirement: Frontend create initiative form

The Angular application SHALL provide a form (in a PrimeNG Dialog) to create a new initiative. The form SHALL include a title field (required).

#### Scenario: Create initiative via dialog

- **WHEN** a user clicks "New Initiative" and enters a title
- **THEN** the dialog submits to the API and on success closes the dialog and adds the initiative to the list

#### Scenario: Validation feedback

- **WHEN** a user submits the form with an empty title
- **THEN** the form displays a validation error without submitting to the API

### Requirement: Frontend initiative detail page

The Angular application SHALL provide a detail page for an initiative. The page SHALL display the title (editable), status with transition buttons, milestones list with add/edit/complete/remove, and linked people as chips with add/remove.

#### Scenario: View initiative details

- **WHEN** a user clicks on an initiative in the list
- **THEN** the application navigates to the initiative detail page showing title, status, milestones, and linked people

#### Scenario: Edit initiative title

- **WHEN** a user edits the title and saves
- **THEN** the application sends the update to the API and reflects changes on success

#### Scenario: Change initiative status

- **WHEN** a user clicks a status transition button (e.g., "Put On Hold")
- **THEN** the application calls the status change endpoint and updates the displayed status and available transition buttons

#### Scenario: Manage milestones

- **WHEN** a user adds, edits, completes, or removes a milestone
- **THEN** the application calls the appropriate API endpoint and updates the milestones display

#### Scenario: Link and unlink people

- **WHEN** a user adds or removes a linked person
- **THEN** the application calls the appropriate API endpoint and updates the linked people display
