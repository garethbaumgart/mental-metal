# initiative-living-brief Specification

## Purpose
TBD - created by archiving change initiative-living-brief. Update Purpose after archive.
## Requirements
### Requirement: LivingBrief value-object cluster on Initiative

The `Initiative` aggregate SHALL embed a `LivingBrief` value-object cluster comprising: `Summary` (string, may be empty), `SummaryLastRefreshedAt` (datetime, nullable), `BriefVersion` (monotonic integer, starting at 0), `KeyDecisions` (append-only list of `KeyDecision`), `Risks` (list of `Risk`), `RequirementsHistory` (append-only list of `RequirementsSnapshot`), and `DesignDirectionHistory` (append-only list of `DesignDirectionSnapshot`). All embedded value objects are persisted with the Initiative.

#### Scenario: New initiative starts with empty brief

- **WHEN** an authenticated user creates a new Initiative
- **THEN** the Initiative's LivingBrief has empty Summary, BriefVersion 0, no decisions, no risks, no requirements snapshots, and no design direction snapshots

#### Scenario: BriefVersion increments on every applied change

- **WHEN** any brief mutation (RefreshSummary, RecordDecision, RaiseRisk, ResolveRisk, SnapshotRequirements, SnapshotDesignDirection, applying a PendingBriefUpdate) is committed on an Initiative
- **THEN** BriefVersion is incremented by 1

### Requirement: Get current brief

The system SHALL allow an authenticated user to retrieve the current LivingBrief for one of their initiatives via `GET /api/initiatives/{id}/brief`.

#### Scenario: Get brief for an initiative

- **WHEN** an authenticated user sends `GET /api/initiatives/{id}/brief` for an initiative they own
- **THEN** the system returns HTTP 200 with the LivingBrief: summary, summaryLastRefreshedAt, briefVersion, keyDecisions, openRisks, resolvedRisks, latest requirements snapshot with full history, latest design direction snapshot with full history

#### Scenario: Initiative not found

- **WHEN** an authenticated user sends `GET /api/initiatives/{id}/brief` for an initiative ID that does not exist or belongs to another user
- **THEN** the system returns HTTP 404

#### Scenario: User isolation

- **WHEN** User A and User B each have an initiative with the same name and User A requests User B's initiative brief by ID
- **THEN** the system returns HTTP 404

### Requirement: Manually update summary

The system SHALL allow an authenticated user to manually overwrite the summary on an initiative's brief via `PUT /api/initiatives/{id}/brief/summary` with a non-empty `summary` body. The system SHALL set `SummaryLastRefreshedAt` to the current time, increment `BriefVersion`, and raise a `LivingBriefSummaryUpdated` domain event with `source = "Manual"`.

#### Scenario: Update summary

- **WHEN** an authenticated user sends `PUT /api/initiatives/{id}/brief/summary` with body `{ "summary": "Project X is now in design review." }`
- **THEN** the LivingBrief.Summary is replaced, SummaryLastRefreshedAt is updated, BriefVersion increments, and the system returns HTTP 200

#### Scenario: Empty summary rejected

- **WHEN** an authenticated user sends `PUT /api/initiatives/{id}/brief/summary` with an empty or whitespace-only summary
- **THEN** the system returns HTTP 400

### Requirement: Manually log a key decision

The system SHALL allow an authenticated user to append a `KeyDecision` to an initiative's brief via `POST /api/initiatives/{id}/brief/decisions` with `description` (required), `madeBy` (optional string), `rationale` (optional string), and `decisionDate` (optional date, defaults to today). The system SHALL append the decision to `KeyDecisions`, increment `BriefVersion`, and raise a `LivingBriefDecisionLogged` event.

#### Scenario: Log a decision

- **WHEN** an authenticated user sends `POST /api/initiatives/{id}/brief/decisions` with `{ "description": "Adopt PostgreSQL", "madeBy": "Architecture review", "rationale": "Better JSONB support" }`
- **THEN** the decision is appended with a generated DecisionId, source = Manual, returns HTTP 201

#### Scenario: Empty description rejected

- **WHEN** an authenticated user sends `POST /api/initiatives/{id}/brief/decisions` with an empty description
- **THEN** the system returns HTTP 400

### Requirement: Manually raise a risk

The system SHALL allow an authenticated user to add a `Risk` to an initiative's brief via `POST /api/initiatives/{id}/brief/risks` with `description` (required), `severity` (required: `Low | Medium | High | Critical`), and `mitigation` (optional). The system SHALL append the risk with status `Open`, generate a `RiskId`, increment `BriefVersion`, and raise a `LivingBriefRiskRaised` event.

#### Scenario: Raise a high-severity risk

- **WHEN** an authenticated user sends `POST /api/initiatives/{id}/brief/risks` with `{ "description": "Vendor API may not ship in Q3", "severity": "High" }`
- **THEN** the risk is appended with status Open and returns HTTP 201

#### Scenario: Invalid severity rejected

- **WHEN** an authenticated user sends a risk with `severity = "Catastrophic"`
- **THEN** the system returns HTTP 400

### Requirement: Resolve a risk

The system SHALL allow an authenticated user to mark a risk as resolved via `POST /api/initiatives/{id}/brief/risks/{riskId}/resolve` with optional `resolutionNotes`. The system SHALL set the risk's status to `Resolved`, set `ResolvedAt`, increment `BriefVersion`, and raise a `LivingBriefRiskResolved` event. Only risks with status `Open` SHALL be resolvable.

#### Scenario: Resolve an open risk

- **WHEN** an authenticated user sends `POST /api/initiatives/{id}/brief/risks/{riskId}/resolve` for an open risk
- **THEN** the risk's status is Resolved, ResolvedAt is set, returns HTTP 200

#### Scenario: Resolve already-resolved risk rejected

- **WHEN** the risk is already Resolved
- **THEN** the system returns HTTP 409 Conflict

#### Scenario: Risk not found

- **WHEN** the riskId does not exist on the initiative
- **THEN** the system returns HTTP 404

### Requirement: Snapshot requirements

The system SHALL allow an authenticated user to append a `RequirementsSnapshot` (containing `content` text and `source` of `Manual` or `AI`) to the initiative's brief via `POST /api/initiatives/{id}/brief/requirements`. Each snapshot has a `SnapshotId`, `CreatedAt`, and `BriefVersionAtSnapshot`. The system SHALL raise a `LivingBriefRequirementsSnapshot` event.

#### Scenario: Manual requirements snapshot

- **WHEN** an authenticated user sends `POST /api/initiatives/{id}/brief/requirements` with `{ "content": "Must support SSO via Okta" }`
- **THEN** a snapshot is appended with source = Manual and returns HTTP 201

#### Scenario: Empty content rejected

- **WHEN** the content is empty or whitespace
- **THEN** the system returns HTTP 400

### Requirement: Snapshot design direction

The system SHALL allow an authenticated user to append a `DesignDirectionSnapshot` to the initiative's brief via `POST /api/initiatives/{id}/brief/design-direction` with `content` (required) and `source` (`Manual` or `AI`). The system SHALL raise a `LivingBriefDesignDirectionSnapshot` event.

#### Scenario: Manual design snapshot

- **WHEN** an authenticated user sends `POST /api/initiatives/{id}/brief/design-direction` with `{ "content": "Switch to event-driven architecture for billing." }`
- **THEN** a snapshot is appended with source = Manual and returns HTTP 201

### Requirement: Auto-generate brief update on capture extraction confirmed

The system SHALL subscribe to the `CaptureExtractionConfirmed` domain event from `capture-ai-extraction`. For each `LinkedInitiativeId` on the confirmed capture that belongs to the same user, the system SHALL enqueue a brief-refresh job keyed by `(UserId, InitiativeId)`. If a job for the same key is already queued or in-flight, the new trigger SHALL be coalesced.

#### Scenario: Capture linked to one initiative triggers a refresh

- **WHEN** a capture with one LinkedInitiativeId is confirmed
- **THEN** exactly one brief-refresh job is enqueued for that initiative

#### Scenario: Capture linked to multiple initiatives triggers a refresh per initiative

- **WHEN** a capture with three LinkedInitiativeIds is confirmed
- **THEN** three brief-refresh jobs are enqueued, one per initiative

#### Scenario: Coalesced trigger

- **WHEN** two captures for the same initiative are confirmed within the debounce window
- **THEN** only one in-flight or queued brief-refresh job exists for that (UserId, InitiativeId) and the second trigger is coalesced

#### Scenario: User isolation in event handling

- **WHEN** User B's capture confirmation references an InitiativeId that belongs to User A
- **THEN** no refresh job is enqueued and no error escapes the handler

### Requirement: BriefMaintenanceService produces a BriefUpdateProposal

When a brief-refresh job runs, the `BriefMaintenanceService` SHALL: load the Initiative and its current LivingBrief, gather all confirmed captures whose `LinkedInitiativeIds` include the Initiative ID, call `IAiCompletionService.CompleteAsync` with a structured prompt containing the current brief state and the linked captures' `AiExtraction` data, parse the response into a `BriefUpdateProposal`, persist a new `PendingBriefUpdate` aggregate with status `Pending`, and raise a `LivingBriefUpdateProposed` event.

#### Scenario: Successful proposal generation

- **WHEN** the BriefMaintenanceService runs for an initiative with at least one linked confirmed capture
- **THEN** a PendingBriefUpdate is created with status Pending, containing the proposed summary, new decisions, new/resolved risks, optional new requirements snapshot, optional new design direction snapshot, source capture IDs, AI confidence score, AI rationale, and the BriefVersion of the initiative at proposal time

#### Scenario: AI provider failure

- **WHEN** the AI completion call throws an `AiProviderException`
- **THEN** a PendingBriefUpdate is created with status `Failed` and the error message recorded; no `LivingBriefUpdateProposed` event is raised

#### Scenario: Taste limit exceeded

- **WHEN** the AI completion call throws a `TasteLimitExceededException`
- **THEN** a PendingBriefUpdate is created with status `Failed` and message "Daily AI limit reached"

#### Scenario: No linked captures

- **WHEN** the refresh runs but no confirmed captures are linked to the initiative
- **THEN** no PendingBriefUpdate is created and the job completes silently

### Requirement: Manually trigger a brief refresh

The system SHALL allow an authenticated user to manually enqueue a brief-refresh job via `POST /api/initiatives/{id}/brief/refresh`. The system SHALL behave identically to an event-triggered refresh and return HTTP 202 Accepted.

#### Scenario: Manual refresh

- **WHEN** an authenticated user sends `POST /api/initiatives/{id}/brief/refresh`
- **THEN** the system enqueues a brief-refresh job for that initiative and returns HTTP 202

#### Scenario: Manual refresh on initiative with no captures

- **WHEN** the user manually refreshes an initiative with no linked captures
- **THEN** the system returns HTTP 202 and the job exits without creating a proposal

### Requirement: List pending brief updates

The system SHALL allow an authenticated user to list pending brief updates for an initiative via `GET /api/initiatives/{id}/brief/pending-updates`, optionally filtered by `status` (`Pending | Failed | Applied | Rejected`). The list SHALL be ordered by `CreatedAt` descending.

#### Scenario: List pending updates

- **WHEN** an authenticated user sends `GET /api/initiatives/{id}/brief/pending-updates`
- **THEN** the system returns all PendingBriefUpdates for that initiative belonging to the user, newest first

#### Scenario: Filter by status

- **WHEN** the user sends `GET /api/initiatives/{id}/brief/pending-updates?status=Pending`
- **THEN** only updates with status Pending are returned

#### Scenario: Empty list

- **WHEN** the user has no pending updates for the initiative
- **THEN** the system returns an empty array with HTTP 200

### Requirement: Get a single pending brief update

The system SHALL allow an authenticated user to fetch one pending update via `GET /api/initiatives/{id}/brief/pending-updates/{updateId}`.

#### Scenario: Get pending update

- **WHEN** the user requests an existing pending update
- **THEN** the system returns HTTP 200 with the full BriefUpdateProposal, status, source capture IDs, BriefVersion-at-proposal, and AI rationale

#### Scenario: Pending update not found

- **WHEN** the updateId does not exist or belongs to another user
- **THEN** the system returns HTTP 404

### Requirement: Apply a pending brief update

The system SHALL allow an authenticated user to apply a `Pending` brief update via `POST /api/initiatives/{id}/brief/pending-updates/{updateId}/apply`. The system SHALL: reload the Initiative, verify `Initiative.LivingBrief.BriefVersion == PendingBriefUpdate.BriefVersionAtProposal`, and if so, replace the Summary, append new KeyDecisions, append new Risks, set ResolvedAt on resolved Risks, append the new RequirementsSnapshot (if present, source = AI), append the new DesignDirectionSnapshot (if present, source = AI), set the PendingBriefUpdate status to `Applied`, increment `BriefVersion` once, and raise `LivingBriefUpdateApplied` plus the corresponding per-section events.

#### Scenario: Apply a current proposal

- **WHEN** the user applies a Pending update whose BriefVersionAtProposal matches the initiative's current BriefVersion
- **THEN** all proposed changes are applied in one transaction, BriefVersion increments by 1, the PendingBriefUpdate.Status is Applied, and the system returns HTTP 200

#### Scenario: Apply a stale proposal

- **WHEN** the user applies a Pending update whose BriefVersionAtProposal is less than the initiative's current BriefVersion
- **THEN** the system returns HTTP 409 Conflict with code "stale_proposal" and the proposal remains Pending

#### Scenario: Apply a non-pending update

- **WHEN** the user applies an update whose status is `Failed`, `Applied`, or `Rejected`
- **THEN** the system returns HTTP 409 Conflict

#### Scenario: Apply by wrong user

- **WHEN** User A attempts to apply a pending update belonging to User B
- **THEN** the system returns HTTP 404

### Requirement: Reject a pending brief update

The system SHALL allow an authenticated user to reject a `Pending` brief update via `POST /api/initiatives/{id}/brief/pending-updates/{updateId}/reject` with optional `reason`. The system SHALL set status to `Rejected`, store the reason, and raise `LivingBriefUpdateRejected`. The Initiative's brief SHALL NOT change.

#### Scenario: Reject pending update

- **WHEN** the user rejects a Pending update with reason "AI misread the meeting"
- **THEN** status becomes Rejected, the reason is stored, BriefVersion does not change, returns HTTP 200

#### Scenario: Reject non-pending update

- **WHEN** the user rejects an update with status `Applied`, `Rejected`, or `Failed`
- **THEN** the system returns HTTP 409

### Requirement: Edit a pending brief update before applying

The system SHALL allow an authenticated user to modify a `Pending` brief update via `PUT /api/initiatives/{id}/brief/pending-updates/{updateId}` with any subset of: edited summary text, edited list of new decisions, edited list of new risks, edited list of risk IDs to resolve, edited requirements snapshot text, edited design direction snapshot text. The status SHALL transition to `Edited` (still applyable). The system SHALL raise `LivingBriefUpdateProposed` again with `editedByUser = true`.

#### Scenario: Edit summary in proposal

- **WHEN** the user edits the proposed summary text and saves
- **THEN** the PendingBriefUpdate.Proposal.Summary is replaced, status becomes Edited, returns HTTP 200

#### Scenario: Edit non-pending update rejected

- **WHEN** the user edits an update with status `Applied`, `Rejected`, or `Failed`
- **THEN** the system returns HTTP 409

### Requirement: Auto-apply preference

When the user's `LivingBriefAutoApply` preference is `true`, the system SHALL apply each newly proposed brief update immediately within the same transaction that creates it, provided the proposal's BriefVersionAtProposal still matches the initiative's BriefVersion. If the preference is `false` (default), proposals SHALL remain in status `Pending` until the user explicitly applies, edits, or rejects them.

#### Scenario: Auto-apply on

- **WHEN** the user has LivingBriefAutoApply = true and a brief-refresh job creates a proposal
- **THEN** the proposal is created with status `Applied`, the brief is updated, and `LivingBriefUpdateApplied` is raised

#### Scenario: Auto-apply off (default)

- **WHEN** the user has LivingBriefAutoApply = false and a brief-refresh job creates a proposal
- **THEN** the proposal is created with status `Pending` and the brief is unchanged

#### Scenario: Auto-apply when stale

- **WHEN** auto-apply is on but BriefVersion has advanced since the proposal was generated
- **THEN** the proposal is created with status `Pending` (the user can manually intervene) — auto-apply does NOT silently overwrite

### Requirement: Source attribution on history entries

Every `KeyDecision`, `RequirementsSnapshot`, and `DesignDirectionSnapshot` SHALL record `Source` (`Manual` or `AI`), `CreatedAt`, and (for AI-sourced entries) the `SourceCaptureIds` that produced it. Every `Risk` SHALL record its `Source` (`Manual` or `AI`) and (for AI-sourced) `SourceCaptureIds`.

#### Scenario: Manual decision attribution

- **WHEN** a decision is logged manually
- **THEN** Source = Manual, SourceCaptureIds is empty

#### Scenario: AI decision attribution

- **WHEN** a decision is appended by applying a PendingBriefUpdate
- **THEN** Source = AI, SourceCaptureIds contains the proposal's source capture IDs

### Requirement: Living Brief tab in UI

The frontend SHALL provide a "Living Brief" tab on the initiative detail page. The tab SHALL render: a Summary card with the current summary, last-refreshed timestamp, BriefVersion, and an Edit button; a Pending Updates panel showing a count badge and expandable proposal cards with Accept/Edit/Reject buttons; a Decisions list (newest first) with source badges; an Open Risks list with severity, with a separate collapsed Resolved Risks section; a Requirements section showing the latest snapshot with an expandable history; a Design Direction section with the same shape; and a "Refresh now" button that calls the manual refresh endpoint. All component state SHALL be managed via Angular signals.

#### Scenario: View brief tab

- **WHEN** a user navigates to an initiative's "Living Brief" tab
- **THEN** the system displays the summary, decisions, open and resolved risks, latest requirements and design direction, and a pending-updates panel with count

#### Scenario: Pending update review

- **WHEN** a user expands a pending update card and clicks "Accept"
- **THEN** the system applies the update, the brief refreshes, and the pending-updates count decreases

#### Scenario: Pending update edit

- **WHEN** a user clicks "Edit" on a pending update, modifies the summary, and saves
- **THEN** the proposal status becomes Edited and the user can then Accept or Reject

#### Scenario: Manual summary edit

- **WHEN** a user clicks Edit on the Summary card, types a new summary, and saves
- **THEN** the summary is replaced and the last-refreshed timestamp updates

