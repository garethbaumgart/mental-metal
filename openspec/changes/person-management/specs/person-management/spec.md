## ADDED Requirements

### Requirement: Create a person

The system SHALL allow an authenticated user to create a new Person with a name and person type (DirectReport, Stakeholder, or Candidate). Name MUST NOT be empty. Name MUST be unique among the user's non-archived people. The system SHALL raise a `PersonCreated` domain event. The Person SHALL be scoped to the authenticated user's UserId.

#### Scenario: Create a direct report

- **WHEN** an authenticated user sends a POST to `/api/people` with name "Alice Smith" and type "DirectReport"
- **THEN** the system creates a Person with the given name and type, sets CreatedAt and UpdatedAt, and returns the created person with HTTP 201

#### Scenario: Create a stakeholder with optional fields

- **WHEN** an authenticated user sends a POST to `/api/people` with name "Bob Jones", type "Stakeholder", email "bob@example.com", role "VP Engineering", and team "Platform"
- **THEN** the system creates a Person with all provided fields and returns HTTP 201

#### Scenario: Create a candidate

- **WHEN** an authenticated user sends a POST to `/api/people` with name "Carol White" and type "Candidate"
- **THEN** the system creates a Person with type Candidate and initialises CandidateDetails with PipelineStatus "New"

#### Scenario: Empty name rejected

- **WHEN** an authenticated user sends a POST to `/api/people` with an empty name
- **THEN** the system returns HTTP 400 with a validation error

#### Scenario: Duplicate name rejected

- **WHEN** an authenticated user sends a POST to `/api/people` with a name that already exists among their non-archived people
- **THEN** the system returns HTTP 409 Conflict

#### Scenario: User isolation

- **WHEN** User A creates a person named "Alice" and User B creates a person named "Alice"
- **THEN** both operations succeed because name uniqueness is scoped per user

### Requirement: List people

The system SHALL allow an authenticated user to retrieve a list of their people. The list SHALL exclude archived people by default. The list SHALL support optional filtering by person type.

#### Scenario: List all active people

- **WHEN** an authenticated user sends a GET to `/api/people`
- **THEN** the system returns all non-archived people belonging to that user

#### Scenario: Filter by type

- **WHEN** an authenticated user sends a GET to `/api/people?type=DirectReport`
- **THEN** the system returns only non-archived people with type DirectReport

#### Scenario: Include archived

- **WHEN** an authenticated user sends a GET to `/api/people?includeArchived=true`
- **THEN** the system returns all people including archived ones

#### Scenario: Empty list

- **WHEN** an authenticated user with no people sends a GET to `/api/people`
- **THEN** the system returns an empty array with HTTP 200

### Requirement: Get person by ID

The system SHALL allow an authenticated user to retrieve a single person by ID.

#### Scenario: Get existing person

- **WHEN** an authenticated user sends a GET to `/api/people/{id}` with a valid person ID
- **THEN** the system returns the person with all details including type-specific details (CareerDetails or CandidateDetails)

#### Scenario: Person not found

- **WHEN** an authenticated user sends a GET to `/api/people/{id}` with an ID that does not exist or belongs to another user
- **THEN** the system returns HTTP 404

### Requirement: Update person profile

The system SHALL allow an authenticated user to update a person's profile fields: name, email, role, team, and notes. Name MUST NOT be empty. Updated name MUST be unique among the user's non-archived people. The system SHALL raise a `PersonProfileUpdated` domain event.

#### Scenario: Update profile successfully

- **WHEN** an authenticated user sends a PUT to `/api/people/{id}` with updated name, email, role, team, and notes
- **THEN** the system updates the person, sets UpdatedAt, and returns the updated person

#### Scenario: Partial update

- **WHEN** an authenticated user sends a PUT with only name and team (other fields null)
- **THEN** the system updates the provided fields and clears the null fields (full replacement semantics)

#### Scenario: Update with duplicate name rejected

- **WHEN** an authenticated user sends a PUT with a name that belongs to another non-archived person
- **THEN** the system returns HTTP 409 Conflict

#### Scenario: Update archived person rejected

- **WHEN** an authenticated user sends a PUT to update an archived person
- **THEN** the system returns HTTP 400 indicating archived people cannot be modified

### Requirement: Change person type

The system SHALL allow an authenticated user to change a person's type. When changing from DirectReport, CareerDetails SHALL be cleared. When changing from Candidate, CandidateDetails SHALL be cleared. When changing to Candidate, CandidateDetails SHALL be initialised with PipelineStatus "New". The system SHALL raise a `PersonTypeChanged` domain event.

#### Scenario: Change stakeholder to direct report

- **WHEN** an authenticated user sends a PUT to `/api/people/{id}/type` with type "DirectReport"
- **THEN** the system changes the person's type and returns the updated person

#### Scenario: Change direct report to candidate

- **WHEN** an authenticated user sends a PUT to `/api/people/{id}/type` for a DirectReport with type "Candidate"
- **THEN** the system changes the type, clears CareerDetails, initialises CandidateDetails with PipelineStatus "New"

#### Scenario: Change candidate to direct report

- **WHEN** an authenticated user sends a PUT to `/api/people/{id}/type` for a Candidate with type "DirectReport"
- **THEN** the system changes the type and clears CandidateDetails (interview history preserved by interview-tracking spec)

#### Scenario: Same type is no-op

- **WHEN** an authenticated user sends a PUT to change type to the person's current type
- **THEN** the system returns the person unchanged with HTTP 200

### Requirement: Update career details for direct reports

The system SHALL allow an authenticated user to update CareerDetails (level, aspirations, growthAreas) for a person with type DirectReport. The system SHALL reject updates for non-DirectReport people. The system SHALL raise a `CareerDetailsUpdated` domain event.

#### Scenario: Update career details successfully

- **WHEN** an authenticated user sends a PUT to `/api/people/{id}/career-details` with level, aspirations, and growthAreas for a DirectReport
- **THEN** the system updates the CareerDetails and returns the updated person

#### Scenario: Career details for non-direct-report rejected

- **WHEN** an authenticated user sends a PUT to `/api/people/{id}/career-details` for a Stakeholder or Candidate
- **THEN** the system returns HTTP 400 indicating career details are only valid for direct reports

### Requirement: Update candidate details

The system SHALL allow an authenticated user to update CandidateDetails (pipelineStatus, cvNotes, sourceChannel) for a person with type Candidate. The system SHALL reject updates for non-Candidate people. The system SHALL raise a `CandidateDetailsUpdated` domain event.

#### Scenario: Update candidate details successfully

- **WHEN** an authenticated user sends a PUT to `/api/people/{id}/candidate-details` with cvNotes and sourceChannel for a Candidate
- **THEN** the system updates the CandidateDetails and returns the updated person

#### Scenario: Candidate details for non-candidate rejected

- **WHEN** an authenticated user sends a PUT to `/api/people/{id}/candidate-details` for a DirectReport or Stakeholder
- **THEN** the system returns HTTP 400 indicating candidate details are only valid for candidates

### Requirement: Advance candidate pipeline

The system SHALL allow an authenticated user to advance a candidate's pipeline status. Valid transitions: New → Screening → Interviewing → OfferStage → Hired. Rejected and Withdrawn are terminal states reachable from any non-terminal state. The system SHALL raise a `CandidatePipelineAdvanced` domain event.

#### Scenario: Advance from New to Screening

- **WHEN** an authenticated user sends a POST to `/api/people/{id}/advance-pipeline` with status "Screening" for a Candidate with status "New"
- **THEN** the system updates PipelineStatus to Screening and returns the updated person

#### Scenario: Advance through full pipeline

- **WHEN** a candidate progresses New → Screening → Interviewing → OfferStage → Hired
- **THEN** each transition succeeds and raises a CandidatePipelineAdvanced event

#### Scenario: Reject from any active state

- **WHEN** an authenticated user sends a POST to `/api/people/{id}/advance-pipeline` with status "Rejected" for a Candidate in any non-terminal state
- **THEN** the system updates PipelineStatus to Rejected

#### Scenario: Withdraw from any active state

- **WHEN** an authenticated user sends a POST to `/api/people/{id}/advance-pipeline` with status "Withdrawn" for a Candidate in any non-terminal state
- **THEN** the system updates PipelineStatus to Withdrawn

#### Scenario: Invalid transition rejected

- **WHEN** an authenticated user tries to advance a candidate from Screening directly to OfferStage (skipping Interviewing)
- **THEN** the system returns HTTP 400 indicating invalid pipeline transition

#### Scenario: Advance from terminal state rejected

- **WHEN** an authenticated user tries to advance a Hired, Rejected, or Withdrawn candidate
- **THEN** the system returns HTTP 400 indicating the pipeline is in a terminal state

#### Scenario: Advance non-candidate rejected

- **WHEN** an authenticated user sends a POST to `/api/people/{id}/advance-pipeline` for a non-Candidate person
- **THEN** the system returns HTTP 400 indicating pipeline advancement is only valid for candidates

### Requirement: Archive a person

The system SHALL allow an authenticated user to archive a person via soft-delete. Archived people SHALL be excluded from default list queries. Archived people SHALL remain in the database for referential integrity. The system SHALL raise a `PersonArchived` domain event.

#### Scenario: Archive a person

- **WHEN** an authenticated user sends a POST to `/api/people/{id}/archive`
- **THEN** the system sets IsArchived to true and ArchivedAt to the current time, and returns HTTP 204

#### Scenario: Archive already-archived person is idempotent

- **WHEN** an authenticated user archives an already-archived person
- **THEN** the system returns HTTP 204 without raising a duplicate event

### Requirement: Multi-tenant person isolation

All person data SHALL be automatically scoped to the authenticated user's UserId via EF Core global query filters. A user SHALL NOT be able to access, modify, or list another user's people.

#### Scenario: Cross-tenant access prevented

- **WHEN** User A attempts to GET `/api/people/{id}` with an ID belonging to User B
- **THEN** the system returns HTTP 404 (not 403, to avoid leaking existence)

### Requirement: Frontend people list page

The Angular application SHALL provide a people list page displaying all active people in a PrimeNG Table with columns for name, type, role, and team. The list SHALL support filtering by person type. The list SHALL support a search/filter for name.

#### Scenario: View people list

- **WHEN** an authenticated user navigates to the people page
- **THEN** the page displays a table of all active people with name, type, role, and team columns

#### Scenario: Filter by type

- **WHEN** a user selects a type filter (e.g., "Direct Reports")
- **THEN** the table shows only people of that type

#### Scenario: Empty state

- **WHEN** a user has no people
- **THEN** the page displays an empty state message with a prompt to add their first person

### Requirement: Frontend create person form

The Angular application SHALL provide a form (in a PrimeNG Dialog) to create a new person. The form SHALL include fields for name (required), type (required), email, role, and team. Type-specific sections SHALL appear based on the selected type.

#### Scenario: Create person via dialog

- **WHEN** a user clicks the "Add Person" button and fills in name and type
- **THEN** the dialog submits to the API and on success closes the dialog and adds the person to the list

#### Scenario: Validation feedback

- **WHEN** a user submits the form with an empty name
- **THEN** the form displays a validation error without submitting to the API

### Requirement: Frontend edit person page

The Angular application SHALL provide a detail/edit view for a person. The view SHALL display all person fields and allow editing. Type-specific sections (career details, candidate details) SHALL be shown based on person type.

#### Scenario: View person details

- **WHEN** a user clicks on a person in the list
- **THEN** the application navigates to the person detail view showing all fields

#### Scenario: Edit person profile

- **WHEN** a user edits profile fields and saves
- **THEN** the application sends the update to the API and reflects changes on success

#### Scenario: Change person type

- **WHEN** a user changes the person's type in the detail view
- **THEN** the application calls the type change endpoint and updates the displayed type-specific sections

#### Scenario: Update career details for direct report

- **WHEN** a user edits career details (level, aspirations, growth areas) for a direct report
- **THEN** the application saves the career details via the API

#### Scenario: Advance candidate pipeline

- **WHEN** a user clicks an advance button for a candidate
- **THEN** the application calls the advance-pipeline endpoint and updates the displayed status

#### Scenario: Archive person

- **WHEN** a user clicks "Archive" and confirms
- **THEN** the application calls the archive endpoint and removes the person from the active list
