## 1. Domain Layer

- [ ] 1.1 Create `CaptureType` enum (QuickNote, Transcript, MeetingNotes) in `src/MentalMetal.Domain/Captures/`
- [ ] 1.2 Create `ProcessingStatus` enum (Raw, Processing, Processed, Failed) in `src/MentalMetal.Domain/Captures/`
- [ ] 1.3 Create `Capture` aggregate root entity with all properties (Id, UserId, RawContent, CaptureType, ProcessingStatus, AiExtraction, LinkedPersonIds, LinkedInitiativeIds, SpawnedCommitmentIds, SpawnedDelegationIds, SpawnedObservationIds, Title, CapturedAt, ProcessedAt, Source)
- [ ] 1.4 Implement factory method `Create(userId, rawContent, type, source?, title?)` with validation and `CaptureCreated` domain event
- [ ] 1.5 Implement processing status state machine methods: `BeginProcessing()`, `CompleteProcessing()`, `FailProcessing()`, `RetryProcessing()` with transition validation and domain events
- [ ] 1.6 Implement `LinkToPerson(personId)`, `UnlinkFromPerson(personId)`, `LinkToInitiative(initiativeId)`, `UnlinkFromInitiative(initiativeId)` with idempotency and domain events
- [ ] 1.7 Implement `UpdateMetadata(title, source)` with domain event
- [ ] 1.8 Implement `RecordSpawnedCommitment()`, `RecordSpawnedDelegation()`, `RecordSpawnedObservation()` methods
- [ ] 1.9 Create `ICaptureRepository` interface in `src/MentalMetal.Domain/Captures/`
- [ ] 1.10 Create domain event classes: `CaptureCreated`, `CaptureProcessingStarted`, `CaptureProcessed`, `CaptureProcessingFailed`, `CaptureRetryRequested`, `CaptureLinkedToPerson`, `CaptureLinkedToInitiative`, `CaptureUnlinkedFromPerson`, `CaptureUnlinkedFromInitiative`, `CaptureMetadataUpdated`

## 2. Domain Unit Tests

- [ ] 2.1 Test Capture creation with valid inputs and domain event
- [ ] 2.2 Test Capture creation rejects empty/whitespace rawContent
- [ ] 2.3 Test processing status state machine: valid transitions (Raw->Processing, Processing->Processed, Processing->Failed, Failed->Raw)
- [ ] 2.4 Test processing status state machine: invalid transitions throw domain exception
- [ ] 2.5 Test link/unlink person idempotency
- [ ] 2.6 Test link/unlink initiative idempotency
- [ ] 2.7 Test metadata update

## 3. Infrastructure Layer

- [ ] 3.1 Create `CaptureConfiguration` EF Core entity configuration with JSON columns for ID lists
- [ ] 3.2 Create `CaptureRepository` implementing `ICaptureRepository`
- [ ] 3.3 Register `CaptureRepository` in DI
- [ ] 3.4 Add EF Core migration for Captures table

## 4. Application Layer (Vertical Slice Handlers)

- [ ] 4.1 Create `CreateCapture` command handler (POST /api/captures)
- [ ] 4.2 Create `GetUserCaptures` query handler (GET /api/captures) with type and status filters
- [ ] 4.3 Create `GetCaptureById` query handler (GET /api/captures/{id})
- [ ] 4.4 Create `UpdateCaptureMetadata` command handler (PUT /api/captures/{id})
- [ ] 4.5 Create `LinkCaptureToPerson` command handler (POST /api/captures/{id}/link-person)
- [ ] 4.6 Create `LinkCaptureToInitiative` command handler (POST /api/captures/{id}/link-initiative)
- [ ] 4.7 Create `UnlinkCaptureFromPerson` command handler (POST /api/captures/{id}/unlink-person)
- [ ] 4.8 Create `UnlinkCaptureFromInitiative` command handler (POST /api/captures/{id}/unlink-initiative)

## 5. Web API Layer

- [ ] 5.1 Create `CaptureEndpoints` minimal API mapping with all routes
- [ ] 5.2 Create request/response DTOs for capture operations

## 6. Frontend — Service and Models

- [ ] 6.1 Create `Capture` TypeScript model with all fields
- [ ] 6.2 Create `CaptureService` with methods for all API operations
- [ ] 6.3 Add captures route to Angular router

## 7. Frontend — Components

- [ ] 7.1 Create capture list page component with PrimeNG DataView and filter dropdowns
- [ ] 7.2 Create quick-capture dialog component with PrimeNG Dialog, InputTextarea, and type selector
- [ ] 7.3 Create capture detail view component showing full content, metadata, and linked entities
- [ ] 7.4 Add link/unlink person and initiative controls to detail view

## 8. E2E Tests

- [ ] 8.1 E2E test: create a capture and verify it appears in the list
- [ ] 8.2 E2E test: link a capture to a person and verify the link
- [ ] 8.3 E2E test: user isolation — captures are scoped per user
