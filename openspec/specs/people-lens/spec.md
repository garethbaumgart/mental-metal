# People Lens

## Requirements

### Requirement: Create a one-on-one record

The system SHALL allow an authenticated user to create a OneOnOne record for a Person. Required fields: PersonId and OccurredAt (date). Optional fields: Notes (string), Topics (list of strings), MoodRating (integer 1–5). The system SHALL raise a `OneOnOneCreated` domain event. The OneOnOne SHALL be scoped to the authenticated user's UserId.

#### Scenario: Create a one-on-one with minimal fields

- **WHEN** an authenticated user sends a POST to `/api/one-on-ones` with personId and occurredAt "2026-04-10"
- **THEN** the system creates a OneOnOne record and returns HTTP 201

#### Scenario: Create a one-on-one with all fields

- **WHEN** an authenticated user sends a POST to `/api/one-on-ones` with personId, occurredAt, notes "Discussed career growth", topics ["career", "project-x"], and moodRating 4
- **THEN** the system creates a OneOnOne with all provided fields and returns HTTP 201

#### Scenario: Missing personId rejected

- **WHEN** an authenticated user sends a POST to `/api/one-on-ones` without a personId
- **THEN** the system returns HTTP 400 with a validation error

#### Scenario: User isolation

- **WHEN** User A creates a one-on-one and User B creates a one-on-one
- **THEN** each record is scoped to its respective user's UserId

### Requirement: Update a one-on-one record

The system SHALL allow an authenticated user to update a OneOnOne record's notes, topics, and moodRating. The system SHALL set UpdatedAt and raise a `OneOnOneUpdated` domain event.

#### Scenario: Update notes and topics

- **WHEN** an authenticated user sends a PUT to `/api/one-on-ones/{id}` with new notes and topics
- **THEN** the system updates the fields and returns HTTP 200

#### Scenario: Clear optional fields

- **WHEN** an authenticated user sends a PUT to `/api/one-on-ones/{id}` with moodRating null and topics as empty list
- **THEN** the system clears those fields and returns HTTP 200

### Requirement: Manage one-on-one action items

The system SHALL allow an authenticated user to add, complete, and remove action items on a OneOnOne record. Each action item has a description (required) and a completed flag. The system SHALL raise `ActionItemAdded`, `ActionItemCompleted`, and `ActionItemRemoved` domain events respectively.

#### Scenario: Add an action item

- **WHEN** an authenticated user sends a POST to `/api/one-on-ones/{id}/action-items` with description "Send promotion packet"
- **THEN** the system adds the action item with completed=false and returns HTTP 200

#### Scenario: Complete an action item

- **WHEN** an authenticated user sends a POST to `/api/one-on-ones/{id}/action-items/{itemId}/complete`
- **THEN** the action item is marked as completed and returns HTTP 200

#### Scenario: Remove an action item

- **WHEN** an authenticated user sends a DELETE to `/api/one-on-ones/{id}/action-items/{itemId}`
- **THEN** the action item is removed and returns HTTP 200

### Requirement: Manage one-on-one follow-ups

The system SHALL allow an authenticated user to add and resolve follow-up items on a OneOnOne record. Each follow-up has a description (required) and a resolved flag. The system SHALL raise `FollowUpAdded` and `FollowUpResolved` domain events.

#### Scenario: Add a follow-up

- **WHEN** an authenticated user sends a POST to `/api/one-on-ones/{id}/follow-ups` with description "Check on training budget approval"
- **THEN** the system adds the follow-up with resolved=false and returns HTTP 200

#### Scenario: Resolve a follow-up

- **WHEN** an authenticated user sends a POST to `/api/one-on-ones/{id}/follow-ups/{followUpId}/resolve`
- **THEN** the follow-up is marked as resolved and returns HTTP 200

### Requirement: List one-on-ones

The system SHALL allow an authenticated user to retrieve a list of their one-on-one records. The list SHALL support filtering by personId and SHALL be ordered by OccurredAt descending (most recent first).

#### Scenario: List all one-on-ones

- **WHEN** an authenticated user sends a GET to `/api/one-on-ones`
- **THEN** the system returns all one-on-ones belonging to that user, ordered by OccurredAt descending

#### Scenario: Filter by person

- **WHEN** an authenticated user sends a GET to `/api/one-on-ones?personId={id}`
- **THEN** the system returns only one-on-ones for that person

#### Scenario: Empty list

- **WHEN** an authenticated user with no one-on-ones sends a GET to `/api/one-on-ones`
- **THEN** the system returns an empty array with HTTP 200

### Requirement: Get one-on-one by ID

The system SHALL allow an authenticated user to retrieve a single one-on-one by ID, including action items and follow-ups.

#### Scenario: Get existing one-on-one

- **WHEN** an authenticated user sends a GET to `/api/one-on-ones/{id}` with a valid ID
- **THEN** the system returns the one-on-one with all fields, action items, and follow-ups

#### Scenario: One-on-one not found

- **WHEN** an authenticated user sends a GET to `/api/one-on-ones/{id}` with an ID that does not exist or belongs to another user
- **THEN** the system returns HTTP 404

### Requirement: Create an observation

The system SHALL allow an authenticated user to create an Observation about a Person. Required fields: PersonId, Description, and Tag (enum: Win, Growth, Concern, FeedbackGiven). Optional fields: OccurredAt (defaults to current date), SourceCaptureId. The system SHALL raise an `ObservationCreated` domain event. The Observation SHALL be scoped to the authenticated user's UserId.

#### Scenario: Create a win observation

- **WHEN** an authenticated user sends a POST to `/api/observations` with personId, description "Led the incident response flawlessly", and tag "Win"
- **THEN** the system creates an Observation and returns HTTP 201

#### Scenario: Create a concern observation

- **WHEN** an authenticated user sends a POST to `/api/observations` with personId, description "Missed two deadlines this sprint", tag "Concern", and occurredAt "2026-04-08"
- **THEN** the system creates an Observation with the specified date and returns HTTP 201

#### Scenario: Create observation from capture extraction

- **WHEN** an authenticated user sends a POST to `/api/observations` with personId, description, tag, and sourceCaptureId
- **THEN** the system creates an Observation linked to the source capture and returns HTTP 201

#### Scenario: Empty description rejected

- **WHEN** an authenticated user sends a POST to `/api/observations` with an empty description
- **THEN** the system returns HTTP 400 with a validation error

#### Scenario: Missing personId rejected

- **WHEN** an authenticated user sends a POST to `/api/observations` without a personId
- **THEN** the system returns HTTP 400 with a validation error

### Requirement: Update an observation

The system SHALL allow an authenticated user to update an observation's description and tag. The system SHALL set UpdatedAt and raise an `ObservationUpdated` domain event.

#### Scenario: Update description and tag

- **WHEN** an authenticated user sends a PUT to `/api/observations/{id}` with a new description and tag
- **THEN** the system updates the fields and returns HTTP 200

#### Scenario: Empty description rejected

- **WHEN** an authenticated user sends a PUT to `/api/observations/{id}` with an empty description
- **THEN** the system returns HTTP 400

### Requirement: List observations

The system SHALL allow an authenticated user to retrieve a list of their observations. The list SHALL support filtering by personId and tag. The list SHALL be ordered by OccurredAt descending.

#### Scenario: List all observations

- **WHEN** an authenticated user sends a GET to `/api/observations`
- **THEN** the system returns all observations belonging to that user, ordered by OccurredAt descending

#### Scenario: Filter by person

- **WHEN** an authenticated user sends a GET to `/api/observations?personId={id}`
- **THEN** the system returns only observations for that person

#### Scenario: Filter by tag

- **WHEN** an authenticated user sends a GET to `/api/observations?tag=Win`
- **THEN** the system returns only observations with tag Win

#### Scenario: Empty list

- **WHEN** an authenticated user with no observations sends a GET to `/api/observations`
- **THEN** the system returns an empty array with HTTP 200

### Requirement: Get observation by ID

The system SHALL allow an authenticated user to retrieve a single observation by ID.

#### Scenario: Get existing observation

- **WHEN** an authenticated user sends a GET to `/api/observations/{id}` with a valid ID
- **THEN** the system returns the observation with all fields

#### Scenario: Observation not found

- **WHEN** an authenticated user sends a GET to `/api/observations/{id}` with an ID that does not exist or belongs to another user
- **THEN** the system returns HTTP 404

### Requirement: Delete an observation

The system SHALL allow an authenticated user to delete an observation. The system SHALL raise an `ObservationDeleted` domain event.

#### Scenario: Delete an observation

- **WHEN** an authenticated user sends a DELETE to `/api/observations/{id}`
- **THEN** the system deletes the observation and returns HTTP 204

#### Scenario: Delete non-existent observation

- **WHEN** an authenticated user sends a DELETE to `/api/observations/{id}` for an ID that does not exist or belongs to another user
- **THEN** the system returns HTTP 404

### Requirement: Create a goal

The system SHALL allow an authenticated user to create a development or performance Goal for a Person. Required fields: PersonId, Title, and GoalType (enum: Development, Performance). Optional fields: Description, TargetDate. The system SHALL set Status to `Active` and raise a `GoalCreated` domain event. The Goal SHALL be scoped to the authenticated user's UserId.

#### Scenario: Create a development goal

- **WHEN** an authenticated user sends a POST to `/api/goals` with personId, title "Complete AWS Solutions Architect certification", goalType "Development", and targetDate "2026-06-30"
- **THEN** the system creates a Goal with status Active and returns HTTP 201

#### Scenario: Create a performance goal

- **WHEN** an authenticated user sends a POST to `/api/goals` with personId, title "Reduce P1 incident response time to under 15 minutes", and goalType "Performance"
- **THEN** the system creates a Goal with status Active and returns HTTP 201

#### Scenario: Empty title rejected

- **WHEN** an authenticated user sends a POST to `/api/goals` with an empty title
- **THEN** the system returns HTTP 400 with a validation error

#### Scenario: Missing personId rejected

- **WHEN** an authenticated user sends a POST to `/api/goals` without a personId
- **THEN** the system returns HTTP 400 with a validation error

### Requirement: Update a goal

The system SHALL allow an authenticated user to update a goal's title, description, and targetDate. Title MUST NOT be empty. The system SHALL set UpdatedAt and raise a `GoalUpdated` domain event.

#### Scenario: Update title and description

- **WHEN** an authenticated user sends a PUT to `/api/goals/{id}` with a new title and description
- **THEN** the system updates the fields and returns HTTP 200

#### Scenario: Update target date

- **WHEN** an authenticated user sends a PUT to `/api/goals/{id}` with a new targetDate
- **THEN** the system updates the target date and returns HTTP 200

#### Scenario: Empty title rejected

- **WHEN** an authenticated user sends a PUT to `/api/goals/{id}` with an empty title
- **THEN** the system returns HTTP 400

### Requirement: Goal status lifecycle

The Goal aggregate SHALL enforce a status lifecycle: Active -> Achieved (via Achieve), Active -> Missed (via Miss), Active -> Deferred (via Defer with reason), Achieved -> Active (via Reactivate), Missed -> Active (via Reactivate), Deferred -> Active (via Reactivate). Invalid transitions SHALL throw a domain exception. The system SHALL raise corresponding domain events for each transition.

#### Scenario: Achieve an active goal

- **WHEN** an authenticated user sends a POST to `/api/goals/{id}/achieve` for an active goal
- **THEN** the status transitions to Achieved, AchievedAt is set, and returns HTTP 200

#### Scenario: Miss an active goal

- **WHEN** an authenticated user sends a POST to `/api/goals/{id}/miss` for an active goal
- **THEN** the status transitions to Missed and returns HTTP 200

#### Scenario: Defer an active goal

- **WHEN** an authenticated user sends a POST to `/api/goals/{id}/defer` with reason "Reprioritized to next quarter"
- **THEN** the status transitions to Deferred with the reason stored, and returns HTTP 200

#### Scenario: Reactivate a deferred goal

- **WHEN** an authenticated user sends a POST to `/api/goals/{id}/reactivate` for a deferred goal
- **THEN** the status transitions to Active and returns HTTP 200

#### Scenario: Invalid transition rejected

- **WHEN** an authenticated user sends a POST to `/api/goals/{id}/achieve` for a goal that is already Achieved
- **THEN** the system returns HTTP 409 Conflict

### Requirement: Record goal check-in

The system SHALL allow an authenticated user to record a check-in on a goal. Each check-in has a note (required) and an optional progress percentage (0–100). The system SHALL raise a `GoalCheckInRecorded` domain event.

#### Scenario: Record a check-in with progress

- **WHEN** an authenticated user sends a POST to `/api/goals/{id}/check-ins` with note "Completed 3 of 5 modules" and progress 60
- **THEN** the system records the check-in and returns HTTP 200

#### Scenario: Record a check-in without progress

- **WHEN** an authenticated user sends a POST to `/api/goals/{id}/check-ins` with note "Discussed blockers in 1:1"
- **THEN** the system records the check-in with progress null and returns HTTP 200

#### Scenario: Empty note rejected

- **WHEN** an authenticated user sends a POST to `/api/goals/{id}/check-ins` with an empty note
- **THEN** the system returns HTTP 400

### Requirement: List goals

The system SHALL allow an authenticated user to retrieve a list of their goals. The list SHALL support filtering by personId, goalType, and status. The list SHALL be ordered by status (Active first) then CreatedAt descending.

#### Scenario: List all goals

- **WHEN** an authenticated user sends a GET to `/api/goals`
- **THEN** the system returns all goals belonging to that user

#### Scenario: Filter by person

- **WHEN** an authenticated user sends a GET to `/api/goals?personId={id}`
- **THEN** the system returns only goals for that person

#### Scenario: Filter by status

- **WHEN** an authenticated user sends a GET to `/api/goals?status=Active`
- **THEN** the system returns only active goals

#### Scenario: Empty list

- **WHEN** an authenticated user with no goals sends a GET to `/api/goals`
- **THEN** the system returns an empty array with HTTP 200

### Requirement: Get goal by ID

The system SHALL allow an authenticated user to retrieve a single goal by ID, including check-in history.

#### Scenario: Get existing goal

- **WHEN** an authenticated user sends a GET to `/api/goals/{id}` with a valid ID
- **THEN** the system returns the goal with all fields and check-in history ordered by most recent first

#### Scenario: Goal not found

- **WHEN** an authenticated user sends a GET to `/api/goals/{id}` with an ID that does not exist or belongs to another user
- **THEN** the system returns HTTP 404

### Requirement: Person detail view

The frontend SHALL provide a person detail page that aggregates all data related to a person. The page SHALL display: person info (name, role, relationship), recent one-on-one records, observations timeline, active goals with progress, open commitments (both directions), active delegations, and linked captures. Each section SHALL link to the respective detail views.

#### Scenario: View person detail with full data

- **WHEN** a user navigates to a person's detail page
- **THEN** the system displays the person's info, recent 1:1s, observations, goals, commitments, delegations, and captures in organized sections

#### Scenario: View person detail with no data

- **WHEN** a user navigates to a person's detail page for a person with no linked records
- **THEN** the system displays the person's info with empty states for each section

#### Scenario: Navigate to related records

- **WHEN** a user clicks on a commitment in the person detail view
- **THEN** the system navigates to the commitment detail view

### Requirement: One-on-one list and form UI

The frontend SHALL provide a one-on-one list view filtered by person, and a form for creating and editing one-on-one records. The form SHALL include fields for date, notes (rich text), topics (tag input), mood rating (1–5 selector), action items, and follow-ups.

#### Scenario: Create one-on-one via form

- **WHEN** a user fills in the one-on-one form with date, notes, and topics, then submits
- **THEN** a new one-on-one is created and appears in the list

#### Scenario: Add action items during creation

- **WHEN** a user adds action items while creating a one-on-one
- **THEN** the action items are saved with the one-on-one record

#### Scenario: Edit one-on-one

- **WHEN** a user edits a one-on-one's notes and saves
- **THEN** the one-on-one is updated and the view reflects the changes

### Requirement: Observation list and form UI

The frontend SHALL provide an observation list view filterable by person and tag, and a form for creating and editing observations. The form SHALL include fields for person selector, description, tag selector (Win, Growth, Concern, FeedbackGiven), and date.

#### Scenario: Create observation via form

- **WHEN** a user fills in the observation form with person, description, and tag, then submits
- **THEN** a new observation is created and appears in the list

#### Scenario: Filter observations by tag

- **WHEN** a user selects the "Win" tag filter
- **THEN** the list updates to show only win observations

### Requirement: Goal list and form UI

The frontend SHALL provide a goal list view filterable by person and status, and a form for creating and editing goals. The form SHALL include fields for person selector, title, type selector (Development, Performance), description, and target date. The detail view SHALL show the goal's check-in history and provide a "Record Check-In" action.

#### Scenario: Create goal via form

- **WHEN** a user fills in the goal form with person, title, and type, then submits
- **THEN** a new goal is created with status Active and appears in the list

#### Scenario: Record check-in from goal detail

- **WHEN** a user clicks "Record Check-In" on a goal detail view and enters a note with progress
- **THEN** the check-in is recorded and appears in the check-in history

#### Scenario: Goal status actions

- **WHEN** a user clicks "Achieve" on an active goal
- **THEN** the goal is marked as Achieved and the status badge updates

### Requirement: Performance evidence summary

The person detail view SHALL provide a "Performance Evidence" section that aggregates observations, goal outcomes, commitment completion rates, and delegation completion rates for a given time period. The section SHALL support filtering by date range (defaulting to the current quarter).

#### Scenario: View quarterly evidence summary

- **WHEN** a user views the performance evidence section for a person with the default current quarter range
- **THEN** the system displays: observation counts by tag, goals achieved vs. missed, commitments completed on time, and delegations completed

#### Scenario: Change date range

- **WHEN** a user changes the date range to the previous quarter
- **THEN** the evidence summary updates to reflect the selected period

#### Scenario: No evidence in period

- **WHEN** a user views the performance evidence section for a period with no data
- **THEN** the system displays an empty state with "No evidence recorded in this period"
