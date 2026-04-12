## 1. Domain Layer

- [ ] 1.1 Create InitiativeStatus enum (Active, OnHold, Completed, Cancelled) with valid transition logic
- [ ] 1.2 Create Milestone value object (Id, Title, TargetDate, Description, IsCompleted)
- [ ] 1.3 Create Initiative aggregate root with core properties (Title, Status, Milestones, LinkedPersonIds), factory Create method, and IUserScoped
- [ ] 1.4 Implement Initiative business actions: UpdateTitle, ChangeStatus (with state machine validation), SetMilestone, RemoveMilestone, CompleteMilestone, LinkPerson, UnlinkPerson
- [ ] 1.5 Create domain events: InitiativeCreated, InitiativeTitleUpdated, InitiativeStatusChanged, MilestoneSet, MilestoneCompleted, PersonLinkedToInitiative, PersonUnlinkedFromInitiative
- [ ] 1.6 Create IInitiativeRepository interface (GetByIdAsync, GetAllAsync, AddAsync)

## 2. Domain Tests

- [ ] 2.1 Test Initiative.Create with valid title and rejects empty title
- [ ] 2.2 Test ChangeStatus valid transitions (Active→OnHold, OnHold→Active, Active→Completed, Active→Cancelled)
- [ ] 2.3 Test ChangeStatus rejects invalid transitions (OnHold→Completed, terminal→any)
- [ ] 2.4 Test UpdateTitle on active/on-hold and rejects on terminal
- [ ] 2.5 Test milestone operations: add, update, remove, complete, rejects on terminal
- [ ] 2.6 Test LinkPerson/UnlinkPerson and idempotent link behaviour

## 3. Application Layer

- [ ] 3.1 Create InitiativeDtos (CreateInitiativeRequest, UpdateTitleRequest, ChangeStatusRequest, MilestoneRequest, LinkPersonRequest, InitiativeResponse, MilestoneResponse)
- [ ] 3.2 Create CreateInitiative handler
- [ ] 3.3 Create GetInitiative handler
- [ ] 3.4 Create GetInitiatives handler with status filter
- [ ] 3.5 Create UpdateInitiativeTitle handler
- [ ] 3.6 Create ChangeInitiativeStatus handler
- [ ] 3.7 Create AddMilestone, UpdateMilestone, RemoveMilestone, CompleteMilestone handlers
- [ ] 3.8 Create LinkPerson and UnlinkPerson handlers (with person existence validation via IPersonRepository)

## 4. Infrastructure Layer

- [ ] 4.1 Create InitiativeConfiguration (EF Core fluent API with OwnsMany for Milestones, separate table for LinkedPersonIds)
- [ ] 4.2 Create InitiativeRepository implementing IInitiativeRepository
- [ ] 4.3 Add Initiative DbSet to MentalMetalDbContext and IUserScoped global query filter
- [ ] 4.4 Create EF Core migration for Initiative and related tables
- [ ] 4.5 Register InitiativeRepository in DependencyInjection

## 5. Web Layer (API Endpoints)

- [ ] 5.1 Add POST /api/initiatives endpoint (CreateInitiative)
- [ ] 5.2 Add GET /api/initiatives endpoint (GetInitiatives with status filter)
- [ ] 5.3 Add GET /api/initiatives/{id} endpoint (GetInitiative)
- [ ] 5.4 Add PUT /api/initiatives/{id} endpoint (UpdateInitiativeTitle)
- [ ] 5.5 Add PUT /api/initiatives/{id}/status endpoint (ChangeInitiativeStatus)
- [ ] 5.6 Add POST /api/initiatives/{id}/milestones endpoint (AddMilestone)
- [ ] 5.7 Add PUT /api/initiatives/{id}/milestones/{milestoneId} endpoint (UpdateMilestone)
- [ ] 5.8 Add DELETE /api/initiatives/{id}/milestones/{milestoneId} endpoint (RemoveMilestone)
- [ ] 5.9 Add POST /api/initiatives/{id}/milestones/{milestoneId}/complete endpoint (CompleteMilestone)
- [ ] 5.10 Add POST /api/initiatives/{id}/link-person endpoint (LinkPerson)
- [ ] 5.11 Add DELETE /api/initiatives/{id}/link-person/{personId} endpoint (UnlinkPerson)

## 6. Frontend — Service and Models

- [ ] 6.1 Create Initiative TypeScript models (Initiative, InitiativeStatus, Milestone, CreateInitiativeRequest, UpdateTitleRequest, MilestoneRequest, LinkPersonRequest)
- [ ] 6.2 Create InitiativesService with all API calls (list, get, create, updateTitle, changeStatus, addMilestone, updateMilestone, removeMilestone, completeMilestone, linkPerson, unlinkPerson)

## 7. Frontend — Initiatives List Page

- [ ] 7.1 Create InitiativesListComponent with PrimeNG Table (title, status badge, milestone count)
- [ ] 7.2 Add status filter dropdown
- [ ] 7.3 Add empty state with "New Initiative" prompt
- [ ] 7.4 Add "New Initiative" button that opens create dialog
- [ ] 7.5 Create CreateInitiativeDialogComponent with title field
- [ ] 7.6 Add route for initiatives list page

## 8. Frontend — Initiative Detail Page

- [ ] 8.1 Create InitiativeDetailComponent with title editing and status display
- [ ] 8.2 Add status transition buttons based on current status
- [ ] 8.3 Add milestones section with add, edit, complete, and remove functionality
- [ ] 8.4 Add linked people section with add/remove using person autocomplete
- [ ] 8.5 Add route for initiative detail page

## 9. Integration Testing

- [ ] 9.1 Add E2E tests for initiative CRUD (create, list, get, update title)
- [ ] 9.2 Add E2E tests for status transitions (hold, resume, complete, cancel, invalid)
- [ ] 9.3 Add E2E tests for milestone operations
- [ ] 9.4 Add E2E test for multi-tenant isolation
