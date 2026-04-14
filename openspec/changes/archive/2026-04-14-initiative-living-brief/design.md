## Context

Mental Metal already has the `Initiative` aggregate (Tier 1) and the `Capture` aggregate with AI extraction (`capture-ai-extraction`, Tier 2). Captures can be linked to Initiatives via `Capture.LinkedInitiativeIds`, and confirmed extractions raise `CaptureExtractionConfirmed`. What is missing is a rolled-up, evolving narrative on the Initiative itself — the "living brief" — that turns a stream of captures into the manager's working knowledge of the project.

The brief is the foundation for the next two specs: `initiative-ai-chat` queries it, and `daily-weekly-briefing` (Tier 3) summarises it. Getting its shape right matters.

### Dependencies

- `ai-provider-abstraction` (Tier 1) — the brief refresh calls `IAiCompletionService.CompleteAsync` and respects `TasteLimitExceededException`.
- `capture-ai-extraction` (Tier 2) — the brief subscribes to `CaptureExtractionConfirmed` and reads each capture's `AiExtraction` value object.
- `initiative-management` (Tier 1) — the `Initiative` aggregate is extended in place.

## Goals / Non-Goals

**Goals:**

- Keep brief content embedded on the `Initiative` aggregate (DDD value objects), not in a separate aggregate, so reading an initiative returns its current brief in one query.
- Make AI updates reviewable by default. Users see *what changed and why* before it touches their initiative.
- Keep the human escape hatch first-class: any field can be edited manually at any time, and manual edits are equally honoured in history.
- Use the existing AI provider abstraction — no provider-specific code in this spec.
- Idempotent and concurrency-safe brief updates: re-processing the same capture must not double-log decisions or risks.

**Non-Goals:**

- Real-time collaborative editing or sharing.
- Diffing brief snapshots at sub-paragraph granularity.
- Branching/forking briefs.
- Storing the AI prompt response verbatim for audit (we keep the structured proposal, not the raw completion).
- Cross-initiative roll-ups (e.g. portfolio dashboards) — Tier 3.
- Validating that linked captures actually exist at refresh time — the application handler trusts the event payload.

## Decisions

### 1. LivingBrief is a value-object cluster on Initiative, not a separate aggregate

**Decision:** Model `LivingBrief` as a set of owned value objects on the `Initiative` aggregate root: `Summary` (string + `LastRefreshedAt` + `BriefVersion`), `KeyDecisions` (`IReadOnlyList<KeyDecision>`), `Risks` (`IReadOnlyList<Risk>`), `RequirementsHistory` (`IReadOnlyList<RequirementsSnapshot>`), `DesignDirectionHistory` (`IReadOnlyList<DesignDirectionSnapshot>`).

**Rationale:** The brief is meaningless without its initiative. Embedding it preserves the aggregate boundary (one transaction per write), keeps invariants local (e.g. "RequirementsHistory is append-only"), and matches how `Capture` embeds `AiExtraction`. A reader gets the brief by loading the initiative.

**Alternatives considered:**
- Separate `LivingBrief` aggregate referenced by `InitiativeId` — rejected because there is a 1:1 lifecycle and no independent invariants. It would also force a second query on every initiative read.
- Event-sourced brief — rejected as overkill; we can rebuild from `RequirementsHistory`/`DesignDirectionHistory` snapshots without sourcing every event.

### 2. PendingBriefUpdate is a separate aggregate root

**Decision:** Proposed AI updates live in a `PendingBriefUpdate` aggregate root keyed by its own Id, with a required `InitiativeId`. Each pending update carries a `BriefUpdateProposal` value object (proposed summary, new decisions, new/resolved risks, new requirements snapshot, new design snapshot, source capture IDs, AI confidence score) plus a `Status` of `Pending | Applied | Rejected | Edited`.

**Rationale:** Pending updates have their own lifecycle independent of the brief: they are created, optionally edited, and then applied or rejected. Co-locating them inside the Initiative aggregate would inflate the aggregate, complicate concurrency (two pending updates from concurrent captures), and entangle a transient queue with permanent state. As a separate aggregate they can be queried/listed cheaply.

**Alternatives considered:**
- Embed pending updates as a list on Initiative — rejected for the reasons above.
- Apply AI updates immediately, with an "undo" — rejected because undo on a regenerated summary plus appended decisions is fiddly and surprising.

### 3. Append-only history; `Summary` is the only field regenerated

**Decision:** `KeyDecisions`, `RequirementsHistory`, and `DesignDirectionHistory` are append-only. `Risks` are not deleted but can be `Resolved` (status flip with `ResolvedAt`). The `Summary` field is wholly replaced on each update with the latest AI-generated or user-edited text; previous summaries are not retained verbatim (the user can scroll the decision/requirement/design history if they need provenance).

**Rationale:** The summary is a derived, lossy view; storing every revision bloats the aggregate without much user value. The structured logs (decisions, risks, snapshots) are the source of truth for "how did we get here?".

**Trade-off:** Users who want the previous summary text after an unwanted update must reject the pending update *before* applying it. Once applied, only the new summary remains. This is documented in the UI.

### 4. AI prompting strategy

**Decision:** Brief refresh uses one composite prompt rather than five field-specific prompts. The prompt input is structured JSON containing:

- `currentBrief`: { summary, openRisks, recentDecisions[10], latestRequirements, latestDesignDirection }
- `linkedCaptures`: array of { captureId, createdAt, summary, decisions, risks, requirementsHints, designHints } drawn from each capture's `AiExtraction`
- `triggeringCaptureId`: the capture that fired the event

The prompt instructs the AI to return a structured `BriefUpdateProposal` with: a regenerated summary; a list of *new* decisions not already in `recentDecisions` (matched by description similarity); a list of *new* risks and a list of *risks to resolve*; an optional new requirements snapshot (only if changes are detected); an optional new design direction snapshot; a free-text `rationale` shown in the review UI.

**Rationale:** One round-trip is cheaper and gives the AI cross-section context (e.g. a decision often implies a requirement change). The prompt is responsible for de-duplication; the application layer trusts the AI's "new" lists rather than diffing again.

**Alternatives considered:**
- Five separate prompts (one per section) — rejected for cost and loss of cross-section context.
- Streaming/incremental updates — rejected as unnecessary; brief refresh is a background job.

### 5. Trigger model: domain-event handler, debounced per initiative

**Decision:** A `LivingBriefUpdateHandler` subscribes to `CaptureExtractionConfirmed`. For each `LinkedInitiativeId` on the confirmed capture, the handler enqueues a brief-refresh job keyed by `(UserId, InitiativeId)`. If a job for the same key is already queued or running, the new trigger is coalesced (the in-flight or pending job will pick up the latest state when it runs).

**Rationale:** A single capture confirmation can touch multiple initiatives, and several confirmations can land in quick succession (e.g. a user processes a backlog). Debouncing avoids redundant AI calls and keeps the pending-update queue from filling with near-identical proposals.

**Trade-off:** A brief refresh may cover several captures at once; the proposal lists all source capture IDs.

### 6. Auto-apply preference, default off

**Decision:** A user preference `LivingBriefAutoApply` (boolean, default `false`) on the User aggregate controls whether `LivingBriefUpdateProposed` immediately calls `Apply()` or waits for human action. When `true`, the proposal is created and applied in the same transaction; the `LivingBriefUpdateApplied` event still fires.

**Rationale:** Manual review is the safe default because AI hallucination on critical decisions is high-stakes. Power users can opt in once they trust the model.

### 7. Concurrency and idempotency

**Decision:** The `PendingBriefUpdate` carries the `BriefVersion` of the initiative it was generated against. When the user (or auto-apply) calls `Apply()`, the handler reloads the initiative and rejects the apply with HTTP 409 if `Initiative.LivingBrief.BriefVersion` has advanced (i.e. a different update was applied first). The user is shown a "this proposal is stale, regenerate" prompt. `BriefVersion` increments on every apply.

**Rationale:** Without this, two concurrent applies could double-log decisions or stomp summaries.

### 8. API surface

**Decision:** Endpoints under the existing `/api/initiatives/{id}` resource:

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/initiatives/{id}/brief` | GET | Get the current LivingBrief |
| `/api/initiatives/{id}/brief/summary` | PUT | Manually edit the summary |
| `/api/initiatives/{id}/brief/decisions` | POST | Manually log a decision |
| `/api/initiatives/{id}/brief/risks` | POST | Manually raise a risk |
| `/api/initiatives/{id}/brief/risks/{riskId}/resolve` | POST | Mark a risk resolved |
| `/api/initiatives/{id}/brief/requirements` | POST | Snapshot new requirements text |
| `/api/initiatives/{id}/brief/design-direction` | POST | Snapshot new design direction text |
| `/api/initiatives/{id}/brief/refresh` | POST | Manually trigger an AI refresh (queues a proposal) |
| `/api/initiatives/{id}/brief/pending-updates` | GET | List pending brief updates for the initiative |
| `/api/initiatives/{id}/brief/pending-updates/{updateId}` | GET | Get one pending update |
| `/api/initiatives/{id}/brief/pending-updates/{updateId}/apply` | POST | Apply a pending update |
| `/api/initiatives/{id}/brief/pending-updates/{updateId}/reject` | POST | Reject a pending update |
| `/api/initiatives/{id}/brief/pending-updates/{updateId}` | PUT | Edit a pending update before applying |

**Rationale:** Brief is part of the Initiative resource. Pending updates are a sub-collection because they have IDs and lifecycle.

### 9. Frontend: tabbed initiative detail

**Decision:** Initiative detail page gains a PrimeNG `Tabs` component (or equivalent) with at minimum: "Overview" (existing), "Living Brief" (new). The Living Brief tab shows: Summary card (with edit button), Pending Updates panel (badge with count, expandable cards per proposal with Accept/Edit/Reject), Decisions list, Open Risks list, Resolved Risks (collapsed), Requirements (latest with "history" expandable), Design Direction (latest with "history" expandable). All state via Angular signals.

**Rationale:** Keeps overview minimal for users who don't use the brief; opt-in deeper surface for those who do.

### 10. Storage shape

**Decision:** Owned EF Core configurations:

- `LivingBrief` is configured as `OwnsOne` on `Initiative`, with `KeyDecisions`/`Risks`/`RequirementsHistory`/`DesignDirectionHistory` stored as JSONB columns (`jsonb` Postgres type).
- `PendingBriefUpdate` is its own table (`PendingBriefUpdates`), with `Proposal` as a JSONB column.

**Rationale:** JSONB keeps the schema simple, matches how nested value objects are typically stored in this codebase, and avoids EF Core join explosion. We do not need to query inside decisions/risks across initiatives in this spec.

## Risks / Trade-offs

- **[AI cost surprise]** Frequent capture confirmations could trigger many refreshes. **Mitigation:** Per-initiative debouncing (Decision 5), and the existing `TasteLimitExceededException` flow — when the limit is hit, the proposal is created with status `Failed` and surfaced in the pending-updates panel with a "limit reached" notice.
- **[AI hallucinated decisions]** The model could invent a "decision" that wasn't really made. **Mitigation:** Manual review by default (Decision 6), explicit Reject action, and a `rationale` field in the proposal that cites which capture(s) it came from.
- **[Stale proposals]** A pending update can become irrelevant if a more recent capture lands. **Mitigation:** `BriefVersion` check on apply (Decision 7); the UI shows a "regenerate" CTA on stale proposals.
- **[Append-only growth]** Decisions and risks accumulate forever. **Mitigation:** Acceptable for now — UI virtualises long lists and offers "show resolved" toggles. Archival is a future concern.
- **[JSONB migrations]** Changing the shape of `KeyDecision` etc. later requires data migration. **Mitigation:** Version the JSONB shape with a `schemaVersion` field on each value object from day one.
- **[Cross-aggregate event consistency]** The brief depends on `CaptureExtractionConfirmed` firing reliably. **Mitigation:** Domain events are dispatched in the same transaction as the capture confirmation; a missed event simply means the next capture confirmation will re-cover the gap when it triggers another refresh.

## Migration Plan

- One EF Core migration adds: `LivingBrief` owned columns on `Initiatives` (initially empty), and a new `PendingBriefUpdates` table. Existing initiatives get a default empty brief on first read (no backfill needed).

## Open Questions

_(none — auto-apply default is documented as `false`; debounce window is a queue concern, not a domain concern, and is left to the implementation.)_
