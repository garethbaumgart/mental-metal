## 1. Domain Layer

- [ ] 1.1 Create PersonType enum (DirectReport, Stakeholder, Candidate) and PipelineStatus enum (New, Screening, Interviewing, OfferStage, Hired, Rejected, Withdrawn)
- [ ] 1.2 Create CareerDetails value object (Level, Aspirations, GrowthAreas)
- [ ] 1.3 Create CandidateDetails value object (PipelineStatus, CvNotes, SourceChannel) with pipeline transition validation
- [ ] 1.4 Create Person aggregate root with all properties, factory Create method, and IUserScoped
- [ ] 1.5 Implement Person business actions: UpdateProfile, ChangeType, UpdateCareerDetails, UpdateCandidateDetails, AdvanceCandidatePipeline, Archive
- [ ] 1.6 Create domain events: PersonCreated, PersonProfileUpdated, PersonTypeChanged, CareerDetailsUpdated, CandidateDetailsUpdated, CandidatePipelineAdvanced, PersonArchived
- [ ] 1.7 Create IPersonRepository interface (GetByIdAsync, GetAllAsync, ExistsByNameAsync, AddAsync)

## 2. Domain Tests

- [ ] 2.1 Test Person.Create with valid inputs and all three types
- [ ] 2.2 Test Person.Create rejects empty name
- [ ] 2.3 Test UpdateProfile sets fields and raises event
- [ ] 2.4 Test ChangeType clears/initialises type-specific details correctly
- [ ] 2.5 Test CareerDetails only settable on DirectReport
- [ ] 2.6 Test CandidateDetails only settable on Candidate
- [ ] 2.7 Test AdvanceCandidatePipeline valid transitions and rejects invalid ones
- [ ] 2.8 Test Archive sets IsArchived and is idempotent

## 3. Application Layer

- [ ] 3.1 Create PersonDtos (CreatePersonRequest, UpdatePersonRequest, ChangeTypeRequest, CareerDetailsRequest, CandidateDetailsRequest, AdvancePipelineRequest, PersonResponse)
- [ ] 3.2 Create CreatePerson handler with name uniqueness check
- [ ] 3.3 Create GetPerson handler
- [ ] 3.4 Create GetPeople handler with type filter and includeArchived support
- [ ] 3.5 Create UpdatePersonProfile handler with name uniqueness check
- [ ] 3.6 Create ChangePersonType handler
- [ ] 3.7 Create UpdateCareerDetails handler
- [ ] 3.8 Create UpdateCandidateDetails handler
- [ ] 3.9 Create AdvanceCandidatePipeline handler
- [ ] 3.10 Create ArchivePerson handler

## 4. Infrastructure Layer

- [ ] 4.1 Create PersonConfiguration (EF Core fluent API with OwnsOne for CareerDetails and CandidateDetails, unique filtered index on UserId+Name)
- [ ] 4.2 Create PersonRepository implementing IPersonRepository
- [ ] 4.3 Add Person DbSet to MentalMetalDbContext and IUserScoped global query filter
- [ ] 4.4 Create EF Core migration for Person table
- [ ] 4.5 Register PersonRepository in DependencyInjection

## 5. Web Layer (API Endpoints)

- [ ] 5.1 Add POST /api/people endpoint (CreatePerson)
- [ ] 5.2 Add GET /api/people endpoint (GetPeople with type filter)
- [ ] 5.3 Add GET /api/people/{id} endpoint (GetPerson)
- [ ] 5.4 Add PUT /api/people/{id} endpoint (UpdatePersonProfile)
- [ ] 5.5 Add PUT /api/people/{id}/type endpoint (ChangePersonType)
- [ ] 5.6 Add PUT /api/people/{id}/career-details endpoint (UpdateCareerDetails)
- [ ] 5.7 Add PUT /api/people/{id}/candidate-details endpoint (UpdateCandidateDetails)
- [ ] 5.8 Add POST /api/people/{id}/advance-pipeline endpoint (AdvanceCandidatePipeline)
- [ ] 5.9 Add POST /api/people/{id}/archive endpoint (ArchivePerson)

## 6. Frontend — Service and Models

- [ ] 6.1 Create Person TypeScript models (Person, PersonType, PipelineStatus, CareerDetails, CandidateDetails, CreatePersonRequest, UpdatePersonRequest)
- [ ] 6.2 Create PeopleService with all API calls (list, get, create, update, changeType, updateCareerDetails, updateCandidateDetails, advancePipeline, archive)

## 7. Frontend — People List Page

- [ ] 7.1 Create PeopleListComponent with PrimeNG Table (name, type, role, team columns)
- [ ] 7.2 Add type filter dropdown and name search
- [ ] 7.3 Add empty state with "Add Person" prompt
- [ ] 7.4 Add "Add Person" button that opens create dialog
- [ ] 7.5 Create CreatePersonDialogComponent with form (name, type, email, role, team)
- [ ] 7.6 Add route for people list page

## 8. Frontend — Person Detail/Edit Page

- [ ] 8.1 Create PersonDetailComponent with profile display and edit form
- [ ] 8.2 Add type change section with confirmation
- [ ] 8.3 Add CareerDetails section (visible for DirectReport only)
- [ ] 8.4 Add CandidateDetails section with pipeline status and advance button (visible for Candidate only)
- [ ] 8.5 Add archive button with confirmation dialog
- [ ] 8.6 Add route for person detail page

## 9. Integration Testing

- [ ] 9.1 Add E2E tests for person CRUD (create, list, get, update, archive)
- [ ] 9.2 Add E2E tests for type-specific operations (career details, candidate pipeline)
- [ ] 9.3 Add E2E test for multi-tenant isolation
