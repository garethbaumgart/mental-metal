## 1. Domain Layer

- [x] 1.1 Create `CommitmentDirection` enum (MineToThem, TheirsToMe) in `src/MentalMetal.Domain/Commitments/`
- [x] 1.2 Create `CommitmentStatus` enum (Open, Completed, Cancelled) in `src/MentalMetal.Domain/Commitments/`
- [x] 1.3 Create `Commitment` aggregate root entity with all properties (Id, UserId, Description, Direction, PersonId, InitiativeId, SourceCaptureId, DueDate, Status, CompletedAt, Notes, CreatedAt, UpdatedAt)
- [x] 1.4 Implement factory method `Create(userId, description, direction, personId, dueDate?, initiativeId?, sourceCaptureId?)` with validation and `CommitmentCreated` domain event
- [x] 1.5 Implement status transition methods: `Complete(notes?)`, `Cancel(reason?)`, `Reopen()` with transition validation and domain events
- [x] 1.6 Implement `UpdateDueDate(newDate)` with `CommitmentDueDateChanged` domain event
- [x] 1.7 Implement `UpdateDescription(description)` with validation and `CommitmentDescriptionUpdated` domain event
- [x] 1.8 Implement `LinkToInitiative(initiativeId)` with `CommitmentLinkedToInitiative` domain event
- [x] 1.9 Implement `MarkOverdue()` with guard (DueDate in past, status Open) and `CommitmentBecameOverdue` domain event
- [x] 1.10 Add computed `IsOverdue` property (Status == Open && DueDate != null && DueDate < today)
- [x] 1.11 Create `ICommitmentRepository` interface in `src/MentalMetal.Domain/Commitments/`
- [x] 1.12 Create domain event classes: `CommitmentCreated`, `CommitmentCompleted`, `CommitmentCancelled`, `CommitmentReopened`, `CommitmentDueDateChanged`, `CommitmentDescriptionUpdated`, `CommitmentLinkedToInitiative`, `CommitmentBecameOverdue`

## 2. Domain Unit Tests

- [x] 2.1 Test Commitment creation with valid inputs and domain event
- [x] 2.2 Test creation rejects empty description and missing personId
- [x] 2.3 Test status transitions: Open->Completed, Open->Cancelled, Completed->Open, Cancelled->Open
- [x] 2.4 Test invalid status transitions throw domain exception (Completed->Completed, Cancelled->Cancelled, Open->Reopen)
- [x] 2.5 Test CompletedAt is set on completion and cleared on reopen
- [x] 2.6 Test IsOverdue computation for all status/date combinations
- [x] 2.7 Test MarkOverdue raises event only when conditions met
- [x] 2.8 Test UpdateDueDate and UpdateDescription

## 3. Infrastructure Layer

- [x] 3.1 Create `CommitmentConfiguration` EF Core entity configuration
- [x] 3.2 Create `CommitmentRepository` implementing `ICommitmentRepository` with filtering support
- [x] 3.3 Register `CommitmentRepository` in DI
- [x] 3.4 Add EF Core migration for Commitments table

## 4. Application Layer (Vertical Slice Handlers)

- [x] 4.1 Create `CreateCommitment` command handler (POST /api/commitments)
- [x] 4.2 Create `GetUserCommitments` query handler (GET /api/commitments) with direction, status, personId, initiativeId, and overdue filters
- [x] 4.3 Create `GetCommitmentById` query handler (GET /api/commitments/{id})
- [x] 4.4 Create `UpdateCommitment` command handler (PUT /api/commitments/{id})
- [x] 4.5 Create `CompleteCommitment` command handler (POST /api/commitments/{id}/complete)
- [x] 4.6 Create `CancelCommitment` command handler (POST /api/commitments/{id}/cancel)
- [x] 4.7 Create `ReopenCommitment` command handler (POST /api/commitments/{id}/reopen)
- [x] 4.8 Create `UpdateCommitmentDueDate` command handler (PUT /api/commitments/{id}/due-date)
- [x] 4.9 Create `LinkCommitmentToInitiative` command handler (POST /api/commitments/{id}/link-initiative)

## 5. Web API Layer

- [x] 5.1 Create `CommitmentEndpoints` minimal API mapping with all routes
- [x] 5.2 Create request/response DTOs for commitment operations

## 6. Frontend — Service and Models

- [x] 6.1 Create `Commitment` TypeScript model with all fields including computed IsOverdue
- [x] 6.2 Create `CommitmentService` with methods for all API operations
- [x] 6.3 Add commitments route to Angular router

## 7. Frontend — Components

- [x] 7.1 Create commitment list page component with PrimeNG DataView and filter dropdowns (direction, status, overdue)
- [x] 7.2 Create commitment create/edit dialog component with PrimeNG Dialog, direction selector, person dropdown, date picker, initiative dropdown
- [x] 7.3 Add status action buttons (Complete, Cancel, Reopen) to list items and detail view
- [x] 7.4 Create commitment detail view showing all fields and linked entities

## 8. E2E Tests

- [x] 8.1 E2E test: create a commitment and verify it appears in the list
- [x] 8.2 E2E test: complete and reopen a commitment
- [x] 8.3 E2E test: user isolation — commitments are scoped per user
