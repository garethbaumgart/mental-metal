## 1. Domain Layer

- [x] 1.1 Create `DelegationStatus` enum (Assigned, InProgress, Completed, Blocked) in `src/MentalMetal.Domain/Delegations/`
- [x] 1.2 Create `Priority` enum (Low, Medium, High, Urgent) in `src/MentalMetal.Domain/Delegations/`
- [x] 1.3 Create `Delegation` aggregate root entity with all properties (Id, UserId, Description, DelegatePersonId, InitiativeId, SourceCaptureId, DueDate, Status, Priority, CompletedAt, Notes, LastFollowedUpAt, CreatedAt, UpdatedAt)
- [x] 1.4 Implement factory method `Create(userId, description, delegatePersonId, dueDate?, initiativeId?, priority?, sourceCaptureId?)` with validation and `DelegationCreated` domain event
- [x] 1.5 Implement status transition methods: `MarkInProgress()`, `MarkCompleted(notes?)`, `MarkBlocked(reason)`, `Unblock()` with state machine validation and domain events
- [x] 1.6 Implement `RecordFollowUp(notes?)` updating LastFollowedUpAt with `DelegationFollowedUp` domain event
- [x] 1.7 Implement `UpdateDueDate(newDate)` with `DelegationDueDateChanged` domain event
- [x] 1.8 Implement `Reprioritize(newPriority)` with `DelegationReprioritized` domain event
- [x] 1.9 Implement `Reassign(newPersonId)` with `DelegationReassigned` domain event
- [x] 1.10 Create `IDelegationRepository` interface in `src/MentalMetal.Domain/Delegations/`
- [x] 1.11 Create domain event classes: `DelegationCreated`, `DelegationStarted`, `DelegationCompleted`, `DelegationBlocked`, `DelegationUnblocked`, `DelegationFollowedUp`, `DelegationDueDateChanged`, `DelegationReprioritized`, `DelegationReassigned`

## 2. Domain Unit Tests

- [x] 2.1 Test Delegation creation with valid inputs and domain event
- [x] 2.2 Test creation rejects empty description and missing delegatePersonId
- [x] 2.3 Test valid status transitions: Assigned->InProgress, Assigned->Completed, Assigned->Blocked, InProgress->Completed, InProgress->Blocked, Blocked->InProgress, Blocked->Completed
- [x] 2.4 Test invalid status transitions throw domain exception (Completed->InProgress, Completed->Blocked, etc.)
- [x] 2.5 Test CompletedAt is set on completion
- [x] 2.6 Test RecordFollowUp updates LastFollowedUpAt
- [x] 2.7 Test Reassign changes DelegatePersonId and raises event (idempotent for same person)
- [x] 2.8 Test Reprioritize and UpdateDueDate

## 3. Infrastructure Layer

- [x] 3.1 Create `DelegationConfiguration` EF Core entity configuration
- [x] 3.2 Create `DelegationRepository` implementing `IDelegationRepository` with filtering support
- [x] 3.3 Register `DelegationRepository` in DI
- [x] 3.4 Add EF Core migration for Delegations table

## 4. Application Layer (Vertical Slice Handlers)

- [x] 4.1 Create `CreateDelegation` command handler (POST /api/delegations)
- [x] 4.2 Create `GetUserDelegations` query handler (GET /api/delegations) with status, priority, delegatePersonId, and initiativeId filters
- [x] 4.3 Create `GetDelegationById` query handler (GET /api/delegations/{id})
- [x] 4.4 Create `UpdateDelegation` command handler (PUT /api/delegations/{id})
- [x] 4.5 Create `StartDelegation` command handler (POST /api/delegations/{id}/start)
- [x] 4.6 Create `CompleteDelegation` command handler (POST /api/delegations/{id}/complete)
- [x] 4.7 Create `BlockDelegation` command handler (POST /api/delegations/{id}/block)
- [x] 4.8 Create `UnblockDelegation` command handler (POST /api/delegations/{id}/unblock)
- [x] 4.9 Create `RecordDelegationFollowUp` command handler (POST /api/delegations/{id}/follow-up)
- [x] 4.10 Create `UpdateDelegationDueDate` command handler (PUT /api/delegations/{id}/due-date)
- [x] 4.11 Create `ReprioritizeDelegation` command handler (PUT /api/delegations/{id}/priority)
- [x] 4.12 Create `ReassignDelegation` command handler (POST /api/delegations/{id}/reassign)

## 5. Web API Layer

- [x] 5.1 Create `DelegationEndpoints` minimal API mapping with all routes
- [x] 5.2 Create request/response DTOs for delegation operations

## 6. Frontend — Service and Models

- [x] 6.1 Create `Delegation` TypeScript model with all fields
- [x] 6.2 Create `DelegationService` with methods for all API operations
- [x] 6.3 Add delegations route to Angular router

## 7. Frontend — Components

- [x] 7.1 Create delegation list page component with PrimeNG DataView and filter dropdowns (status, priority)
- [x] 7.2 Create delegation create/edit dialog component with PrimeNG Dialog, person dropdown, date picker, priority selector, initiative dropdown
- [x] 7.3 Add status action buttons (Start, Complete, Block, Unblock) and follow-up recording to list items and detail view
- [x] 7.4 Create delegation detail view showing all fields and linked entities

## 8. E2E Tests

- [x] 8.1 E2E test: create a delegation and verify it appears in the list
- [x] 8.2 E2E test: transition delegation through status lifecycle (Assigned -> InProgress -> Completed)
- [x] 8.3 E2E test: user isolation — delegations are scoped per user
