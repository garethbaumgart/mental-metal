## Why

Initiatives in Mental Metal currently hold static metadata (name, status, milestones) but no narrative summary, decisions log, or evolving requirements/design history. As captures (notes, transcripts) flow in and get linked to an initiative, the manager's mental model of that initiative changes — but those changes are stranded inside individual captures rather than rolled up into a coherent, current-state view.

This is a Tier 2 spec (`initiative-living-brief`) that depends on `ai-provider-abstraction` (Tier 1) and `capture-ai-extraction` (Tier 2). It introduces an AI-maintained "living brief" that updates automatically when linked captures are processed, plus a human review workflow so the user retains editorial control.

## Non-goals

- **No audio transcription** — the brief consumes captures that have already been processed by `capture-ai-extraction`. Audio capture is in `capture-audio` (Tier 3).
- **No auto-generated presentations or stakeholder reports** — the brief is a working document, not a publishable artifact.
- **No conversational chat over the brief** — Q&A about an initiative belongs in `initiative-ai-chat`.
- **No briefing or queue surfacing** — the daily/weekly briefing and queue prioritization are Tier 3.
- **No collaborative editing or shared briefs** — single-user-scoped.
- **No diff/blame view at character granularity** — history is captured as snapshots and append-only logs, not text diffs.

## What Changes

- **Extend the `Initiative` aggregate** with a `LivingBrief` value-object cluster: `Summary` (regenerated each update), `KeyDecisions` (append-only log), `Risks` (raised/resolved with severity), `RequirementsHistory` (snapshot list), `DesignDirectionHistory` (snapshot list), and `BriefVersion` (monotonic counter).
- **New domain service `BriefMaintenanceService`** in the Application layer that subscribes to the `CaptureExtractionConfirmed` domain event from `capture-ai-extraction`, gathers all captures linked to each affected initiative, calls the AI provider via `IAiCompletionService`, and produces a structured `BriefUpdateProposal`.
- **Human-in-the-loop review** — proposed updates are persisted as `PendingBriefUpdate` records that the user can accept (apply to the initiative), reject (discard), or edit (modify before applying). Auto-apply is opt-in per user preference, defaulting to manual review.
- **Manual edit endpoints** for every brief field (summary, decisions, risks, requirements, design direction) so the user can override AI output at any time. Manual edits also append to history.
- **CQRS handlers and minimal API endpoints** under `/api/initiatives/{id}/brief` for reading the brief, listing pending updates, applying/rejecting them, and manual editing.
- **EF Core persistence** for the new value objects and the `PendingBriefUpdate` table; one new migration.
- **Angular "Living Brief" tab** on the initiative detail page — current state panels for each section, a pending-updates panel with accept/reject/edit actions, and a history viewer per section.
- **Domain events** `LivingBriefSummaryUpdated`, `LivingBriefDecisionLogged`, `LivingBriefRiskRaised`, `LivingBriefRiskResolved`, `LivingBriefRequirementsSnapshot`, `LivingBriefDesignDirectionSnapshot`, `LivingBriefUpdateProposed`, `LivingBriefUpdateApplied`, `LivingBriefUpdateRejected`.

## Capabilities

### New Capabilities

- `initiative-living-brief`: AI-maintained narrative brief on each Initiative — summary, decisions log, risks, requirements snapshots, and design direction snapshots — automatically refreshed when linked captures are processed, with a human review queue and manual override.

### Modified Capabilities

_(none — the existing `initiative-management` and `capture-ai-extraction` specs are not modified. The brief is additive: it reads from the published `CaptureExtractionConfirmed` event but does not change the contract of either parent capability. Future cross-spec coupling, if any, will be a separate proposal.)_

## Impact

- **Domain:** `Initiative` aggregate gains the `LivingBrief` value-object cluster and new business actions (`RefreshSummary`, `RecordDecision`, `RaiseRisk`, `ResolveRisk`, `SnapshotRequirements`, `SnapshotDesignDirection`). New `PendingBriefUpdate` aggregate root, `BriefUpdateProposal` value object, and supporting enums (`RiskSeverity`, `RiskStatus`).
- **Application:** New `BriefMaintenanceService`, new vertical-slice handlers under `Initiatives/Brief/`, and a domain-event handler subscribing to `CaptureExtractionConfirmed`.
- **Infrastructure:** EF Core configuration for the new owned types and the `PendingBriefUpdates` table; one migration.
- **Web API:** New endpoints under `/api/initiatives/{id}/brief`.
- **Frontend:** New "Living Brief" tab component, pending-updates panel, history viewer, and brief service.
- **AI prompting:** New prompt template for brief refresh; uses the existing `IAiCompletionService` abstraction.
- **Dependencies:** `ai-provider-abstraction`, `capture-ai-extraction`, `initiative-management`.
