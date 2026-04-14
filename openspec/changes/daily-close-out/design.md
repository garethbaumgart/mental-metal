## Context

`capture-text` (Tier 2) lets users dump raw text and `capture-ai-extraction` (Tier 2) processes that text into structured commitments / delegations / observations / decisions. Today, those captures sit in a flat list and the user has no deliberate end-of-day moment to triage what's still raw, what's processed-but-unconfirmed, and what should be discarded. Without a triage ritual, AI extractions go unactioned and the capture inbox grows indefinitely.

The Capture aggregate already has rich behaviour (`BeginProcessing`, `CompleteProcessing`, `ConfirmExtraction`, `DiscardExtraction`, link/unlink person/initiative, retry). The User aggregate already exists with owned-collection patterns established by `ai-provider-abstraction` and other Tier 1 specs. This change is mostly a thin coordination capability over existing primitives.

## Goals / Non-Goals

**Goals:**
- A single endpoint that returns the "things still pending triage today" queue.
- A small set of triage actions reachable from one screen: confirm extraction, discard extraction, reassign links, quick-discard the whole capture.
- A persistent record per-user-per-date that the user closed out, with counts.
- A focused Angular UI that walks the user through the queue.

**Non-Goals:**
- AI re-processing inside this flow (use existing `/process` and `/retry`).
- Notifications, scheduling, streaks, or gamification.
- Cross-day analytics or trend dashboards.
- Replacing `my-queue` (this is closer to "inbox zero", not prioritisation).

## Decisions

### Decision 1: Mark captures as triaged via a `Triaged` flag, not a new status

**Choice:** Add a boolean `Triaged` (with `TriagedAtUtc` timestamp) to the Capture aggregate, separate from `ProcessingStatus`.

**Rationale:** `ProcessingStatus` describes the AI-processing lifecycle (Raw → Processing → Processed/Failed). Triage is an orthogonal user-intent concept ("I dealt with this"). Conflating them would force ugly status combinations (e.g., a `Failed` capture that the user explicitly chose to discard would lose the failure context).

**Alternatives considered:**
- Add `TriagedRaw`, `TriagedProcessed`, `TriagedDiscarded` to the status enum — rejected, conflates two axes.
- Hard-delete quick-discarded captures — rejected, loses content the user may want to revisit.

### Decision 2: `DailyCloseOutLog` as an owned collection on User

**Choice:** Add `DailyCloseOutLog` as an owned-entity collection on the `User` aggregate, keyed by `(UserId, Date)`, with `ClosedAtUtc`, `ConfirmedCount`, `DiscardedCount`, `RemainingCount`. Recording the same date twice overwrites.

**Rationale:** Close-out is a property of the user's daily rhythm; it doesn't have an independent lifecycle. Owned collection follows the same EF pattern used by `AiProviderConfig` on User. Idempotent overwrite keeps the API simple — pressing "Close out" twice in one day shouldn't create duplicate rows.

**EF Core gotcha (per `project_tier2b_plan.md`):** Owned collections backed by a private field need explicit `Metadata.SetPropertyAccessMode(PropertyAccessMode.Field)` and the snapshot change tracker; otherwise additions to the field-backed list aren't detected. Mirror the existing `AiProviderConfigs` configuration verbatim.

**Alternatives considered:**
- Standalone `DailyCloseOutLog` aggregate — rejected, no invariants of its own and no behaviour outside the user's session.
- Just stamp `User.LastCloseOutAtUtc` — rejected, loses per-day counts and history.

### Decision 3: "Close-out queue" definition

A capture is in the queue when **all** of:
- Belongs to the authenticated user.
- `Triaged == false`.
- `ProcessingStatus` ∈ { Raw, Processing, Failed, Processed-with-no-confirm-or-discard-yet }.

`Processed-with-no-confirm-or-discard-yet` is detected by a new `ExtractionResolved` flag on the Capture (set true when `ConfirmExtraction` or `DiscardExtraction` runs). Adding this flag is cheaper than re-deriving it from domain events.

**EF query:** Use `List<Guid>.Contains()` for the status filter, never `HashSet.Contains()` (untranslatable). String comparisons use `.ToLower()`, never `.ToLowerInvariant()`.

### Decision 4: Reassign endpoint reuses existing link/unlink methods

`POST /api/daily-close-out/captures/{id}/reassign` accepts a body of `{ personIds: Guid[], initiativeIds: Guid[] }`. The handler diffs against the capture's current linked IDs and calls `LinkPerson` / `UnlinkPerson` / `LinkInitiative` / `UnlinkInitiative` on the aggregate. Avoids inventing a new "set links wholesale" method on the aggregate.

### Decision 5: Frontend feature module structure

`src/MentalMetal.Web/ClientApp/src/app/features/daily-close-out/`:
- `daily-close-out.routes.ts`
- `daily-close-out-page.component.ts/html` — main page with queue list + close-out button
- `triage-card.component.ts/html` — per-capture card with action buttons
- `daily-close-out.service.ts` — typed HTTP client wrapping the API
- `daily-close-out.signals.ts` — root signal store for queue + counts
- All control flow uses `@if` / `@for`. All colours come from PrimeNG tokens via `tailwindcss-primeui` utilities (`bg-surface-*`, `text-primary`, etc.). Signal Forms for the reassign multi-select.

## Risks / Trade-offs

- **[Risk] `ExtractionResolved` flag duplicates state already implicit in domain events** → Mitigation: it's a derived-but-cached flag on the aggregate; cheap to keep in sync because the only writers are `ConfirmExtraction` and `DiscardExtraction`. Worth the cost vs. event-replay queries.
- **[Risk] User close-out date timezone confusion** → Mitigation: API takes an optional `date` query param (ISO date) and defaults to "today in UTC". Frontend passes the user's local date so what the user sees as "today" matches what gets logged. Storing `Date` as a `DateOnly` keeps it timezone-stable.
- **[Risk] Quick-discard hides content the user later wants** → Mitigation: triaged captures are still retrievable via `GET /api/captures/{id}`; only the default list view filters them out. Existing `?triaged=true` filter could be added later if needed; out of scope here.
- **[Risk] Owned-collection EF gotcha** → Mitigation: copy the exact configuration used for `AiProviderConfig` (snapshot change tracker, property access mode field) and add an integration test that adds two close-out logs and reloads.

## Migration Plan

- One EF migration: `AddDailyCloseOut`
  - Adds `Triaged`, `TriagedAtUtc`, `ExtractionResolved` columns to `Captures`.
  - Adds `DailyCloseOutLogs` owned-collection table referencing `Users(Id)`.
- Backfill: existing captures default `Triaged = false`; `ExtractionResolved` set true for any capture whose status is not `Processed` (only `Processed` captures have an unresolved extraction). Single SQL `UPDATE` in the migration's `Up`.
- Rollback: standard EF `Down`.

## Open Questions

None blocking. (Whether to add notifications / streak coaching is deferred to a future spec.)

## Dependencies

- `capture-text` (shipped)
- `capture-ai-extraction` (shipped)
- `user-auth-tenancy` (shipped)
