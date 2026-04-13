## 1. Domain Layer — Brief Value Objects

- [ ] 1.1 Create `KeyDecision`, `Risk`, `RequirementsSnapshot`, `DesignDirectionSnapshot` value objects in `src/MentalMetal.Domain/Initiatives/LivingBrief/`
- [ ] 1.2 Create `RiskSeverity` (Low, Medium, High, Critical) and `RiskStatus` (Open, Resolved) enums
- [ ] 1.3 Create `BriefSource` enum (Manual, AI)
- [ ] 1.4 Create `LivingBrief` value object with Summary, SummaryLastRefreshedAt, BriefVersion, KeyDecisions, Risks, RequirementsHistory, DesignDirectionHistory; include private constructor and `Empty()` factory
- [ ] 1.5 Add LivingBrief property to `Initiative` aggregate root with private setter; initialize as `LivingBrief.Empty()`
- [ ] 1.6 Add domain methods on Initiative: `RefreshSummary(string, BriefSource, IReadOnlyList<Guid> sourceCaptureIds)`, `RecordDecision(...)`, `RaiseRisk(...)`, `ResolveRisk(Guid riskId, string?)`, `SnapshotRequirements(...)`, `SnapshotDesignDirection(...)` — each increments BriefVersion and raises the matching domain event

## 2. Domain Layer — PendingBriefUpdate Aggregate

- [ ] 2.1 Create `BriefUpdateProposal` value object (proposed summary, new decisions, new risks, risks-to-resolve IDs, requirements snapshot, design direction snapshot, source capture IDs, AI confidence, rationale)
- [ ] 2.2 Create `PendingBriefUpdateStatus` enum (Pending, Edited, Applied, Rejected, Failed)
- [ ] 2.3 Create `PendingBriefUpdate` aggregate root with Id, UserId, InitiativeId, Proposal, Status, BriefVersionAtProposal, FailureReason, CreatedAt, UpdatedAt
- [ ] 2.4 Implement factory `Create(...)`, `MarkFailed(reason)`, `Edit(BriefUpdateProposal newProposal)`, `MarkApplied()`, `Reject(string? reason)` with status-transition guards
- [ ] 2.5 Create `IPendingBriefUpdateRepository` interface
- [ ] 2.6 Create domain events: `LivingBriefSummaryUpdated`, `LivingBriefDecisionLogged`, `LivingBriefRiskRaised`, `LivingBriefRiskResolved`, `LivingBriefRequirementsSnapshot`, `LivingBriefDesignDirectionSnapshot`, `LivingBriefUpdateProposed`, `LivingBriefUpdateApplied`, `LivingBriefUpdateRejected`

## 3. Domain Unit Tests

- [ ] 3.1 Test LivingBrief.Empty() returns expected zero state
- [ ] 3.2 Test each Initiative brief mutation increments BriefVersion exactly once and raises the right event
- [ ] 3.3 Test ResolveRisk only succeeds for Open risks and throws for unknown riskId / already-resolved
- [ ] 3.4 Test PendingBriefUpdate status transitions (Pending->Edited, Pending->Applied, Pending->Rejected, Failed terminal, etc.)
- [ ] 3.5 Test source attribution on appended decisions/risks/snapshots

## 4. Infrastructure Layer

- [ ] 4.1 Add `LivingBriefConfiguration` configuring LivingBrief as OwnsOne on Initiative with JSONB columns for the lists
- [ ] 4.2 Add `PendingBriefUpdateConfiguration` (table `PendingBriefUpdates`, JSONB Proposal column, indexes on UserId+InitiativeId+Status+CreatedAt)
- [ ] 4.3 Implement `PendingBriefUpdateRepository` and register in DI
- [ ] 4.4 Add EF Core migration `AddLivingBrief`

## 5. Application Layer — BriefMaintenanceService

- [ ] 5.1 Create `IBriefMaintenanceService` interface in Application
- [ ] 5.2 Implement `BriefMaintenanceService.RefreshAsync(userId, initiativeId)` orchestrating: load initiative, gather linked confirmed captures, build prompt, call IAiCompletionService, parse response, persist PendingBriefUpdate
- [ ] 5.3 Define structured prompt template + JSON schema for `BriefUpdateProposal` AI response; add response parser with defensive validation
- [ ] 5.4 Handle `AiProviderException` and `TasteLimitExceededException` by creating Failed proposals
- [ ] 5.5 Implement debouncing/coalescing of refresh jobs per (UserId, InitiativeId) via background queue (Channels or hosted background service)
- [ ] 5.6 Implement domain-event handler `LivingBriefUpdateOnCaptureConfirmed` subscribing to `CaptureExtractionConfirmed` and enqueuing refresh jobs per linked initiative belonging to the same user

## 6. Application Layer — Vertical Slice Handlers

- [ ] 6.1 `GetInitiativeBrief` query handler (GET /api/initiatives/{id}/brief)
- [ ] 6.2 `UpdateInitiativeBriefSummary` command handler (PUT .../brief/summary)
- [ ] 6.3 `LogInitiativeBriefDecision` command handler (POST .../brief/decisions)
- [ ] 6.4 `RaiseInitiativeBriefRisk` command handler (POST .../brief/risks)
- [ ] 6.5 `ResolveInitiativeBriefRisk` command handler (POST .../brief/risks/{riskId}/resolve)
- [ ] 6.6 `SnapshotInitiativeBriefRequirements` command handler (POST .../brief/requirements)
- [ ] 6.7 `SnapshotInitiativeBriefDesignDirection` command handler (POST .../brief/design-direction)
- [ ] 6.8 `RefreshInitiativeBrief` command handler (POST .../brief/refresh) calling BriefMaintenanceService
- [ ] 6.9 `ListPendingBriefUpdates` query handler (GET .../brief/pending-updates) with status filter
- [ ] 6.10 `GetPendingBriefUpdate` query handler (GET .../brief/pending-updates/{updateId})
- [ ] 6.11 `ApplyPendingBriefUpdate` command handler (POST .../brief/pending-updates/{updateId}/apply) with stale-version 409
- [ ] 6.12 `RejectPendingBriefUpdate` command handler (POST .../brief/pending-updates/{updateId}/reject)
- [ ] 6.13 `EditPendingBriefUpdate` command handler (PUT .../brief/pending-updates/{updateId})
- [ ] 6.14 Add `LivingBriefAutoApply` boolean to user preferences and gate auto-apply in BriefMaintenanceService

## 7. Application Unit Tests

- [ ] 7.1 Test BriefMaintenanceService creates a Pending proposal on success
- [ ] 7.2 Test BriefMaintenanceService creates a Failed proposal on AiProviderException and on TasteLimitExceededException
- [ ] 7.3 Test debouncing coalesces concurrent triggers per (UserId, InitiativeId)
- [ ] 7.4 Test ApplyPendingBriefUpdate returns Conflict when BriefVersion has advanced
- [ ] 7.5 Test auto-apply path with auto-apply preference on (current version) and off (default)
- [ ] 7.6 Test event handler ignores cross-user initiative IDs

## 8. Web API Layer

- [ ] 8.1 Create `InitiativeBriefEndpoints` minimal API mapping all routes under `/api/initiatives/{id}/brief`
- [ ] 8.2 Create request/response DTOs for brief, pending updates, manual mutations

## 9. Frontend — Models and Services

- [ ] 9.1 Create TypeScript models: `LivingBrief`, `KeyDecision`, `Risk`, `RequirementsSnapshot`, `DesignDirectionSnapshot`, `PendingBriefUpdate`, `BriefUpdateProposal`
- [ ] 9.2 Create `InitiativeBriefService` with methods for all brief endpoints
- [ ] 9.3 Add user-preferences signal extension for `livingBriefAutoApply`

## 10. Frontend — Living Brief Tab Components

- [ ] 10.1 Add a Tabs container to the initiative detail page (Overview + Living Brief)
- [ ] 10.2 Create `LivingBriefTabComponent` shell with signals for brief data and pending updates
- [ ] 10.3 Create `BriefSummaryCardComponent` with view + manual edit dialog
- [ ] 10.4 Create `BriefDecisionsListComponent` (with manual log dialog and source badges)
- [ ] 10.5 Create `BriefRisksListComponent` (open list, resolved collapsible, raise/resolve dialogs)
- [ ] 10.6 Create `BriefRequirementsHistoryComponent` (latest + expandable history)
- [ ] 10.7 Create `BriefDesignDirectionHistoryComponent`
- [ ] 10.8 Create `PendingBriefUpdatesPanelComponent` with expandable cards, Accept/Edit/Reject buttons, stale-proposal indicator
- [ ] 10.9 Add "Refresh now" button wired to the manual refresh endpoint
- [ ] 10.10 Show user-friendly error states for Failed pending updates ("Daily AI limit reached", "AI provider error")

## 11. E2E Tests

- [ ] 11.1 E2E: manually log a decision and see it on the brief tab
- [ ] 11.2 E2E: confirming a capture extraction linked to an initiative produces a pending brief update visible to the user
- [ ] 11.3 E2E: applying a pending brief update updates the summary, decisions, and risks; BriefVersion increments
- [ ] 11.4 E2E: rejecting a pending update leaves the brief unchanged
- [ ] 11.5 E2E: user isolation — User A cannot read or apply User B's pending updates (404)
- [ ] 11.6 E2E: stale proposal — apply returns Conflict when another update was applied first
