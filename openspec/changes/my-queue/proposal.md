# My Queue — Prioritised Attention View

## Why

Engineering managers juggle open commitments they owe, delegations they're waiting on, and captures they haven't yet triaged. Today these live on three separate screens with three independent filters; nothing surfaces "what deserves your attention right now". This Tier 3 feature unifies those existing data sources into a single prioritised view so the user can act from one place instead of context-switching through three lists.

## What Changes

- Add a new **My Queue** read model that unions data from already-shipped aggregates into a single list of queue items:
  - Open **commitments** owed by the user (direction `MineToThem`) that are overdue or due within a configurable window (default: 7 days)
  - Open **delegations** (`Assigned`, `InProgress`, `Blocked`) that are overdue OR have not been followed up on within a staleness threshold (default: 7 days since `LastFollowedUpAt` or `CreatedAt` if never followed up)
  - **Captures** that are still awaiting triage after a configurable threshold (default: 3 days) — i.e. status `Raw` / `Failed` / `Processed`-but-unresolved beyond that window
- Compute a numeric `priorityScore` per queue item from factors: overdue magnitude, delegation priority, days-until-due, capture staleness. Items are returned sorted by priority descending.
- Add server-side filtering on the queue endpoint:
  - `scope`: `overdue` | `today` | `thisWeek` | `all` (default `all`)
  - `itemType`: `commitment` | `delegation` | `capture` (multi-select; default all)
  - `personId` and `initiativeId` link filters (intersect against each item's linked person/initiative IDs)
- Emit a `suggestDelegate: true` hint on commitment queue items where the heuristic fires: commitment is open, direction `MineToThem`, linked to a `PersonId` that the user has at least one existing (non-cancelled) delegation with, AND not already linked to an open delegation. This is a transient suggestion computed at query time — no persisted state.
- Add a new **My Queue** Angular view (signals, zoneless, `@if`/`@for`, PrimeNG + `tailwindcss-primeui` tokens) as a top-level navigation entry. The view renders grouped queue items, scope/type filter chips, and an inline "Delegate this" action on commitments where `suggestDelegate` is true (navigates to the delegation create form pre-filled with description, linked person and `sourceCaptureId` carried through if present).
- No new aggregate, no schema migration. All data derives from existing `Commitment`, `Delegation`, and `Capture` tables via a new `QueuePrioritizationService` in the Application layer.

## Capabilities

### New Capabilities
- `my-queue`: Prioritised unified attention view that aggregates commitments, delegations, and pending captures into a single scored, filterable list with a contextual delegate-suggestion hint.

### Modified Capabilities
<!-- None — existing specs retain their behaviour. The new capability is strictly query-side over their data. -->

## Non-goals

- No changes to the `Commitment`, `Delegation`, or `Capture` aggregates, events, or persistence
- No new background jobs, notifications, or push surfaces (that's `nudges-rhythms`, Tier 3)
- No briefing generation or daily/weekly summarisation (that's `daily-weekly-briefing`, Tier 3)
- No accept-and-create flow for the delegate suggestion beyond a pre-filled navigation — user still confirms in the existing delegation form
- No AI-driven prioritisation in v1; priority is a deterministic formula. AI re-ranking can be layered later.
- No persistence of the computed priority score or suggestions — each request recomputes

## Affected Aggregates & Dependencies

- **Tier**: 3 (Enhancement)
- **Depends on**: `commitment-tracking`, `delegation-tracking`, `capture-ai-extraction` (all shipped and archived)
- **Domain model aggregates read from**: `Commitment`, `Delegation`, `Capture` — all read-only. No behaviour added to existing aggregates.
- **Service introduced**: `QueuePrioritizationService` (Application layer, stateless, per the `design/spec-plan.md` line-34 aggregate-column entry `(QueuePrioritizationService)`)

## Impact

- **Backend**: one new vertical-slice folder `src/MentalMetal.Application/Features/MyQueue/` with `GetMyQueue.cs` handler, supporting DTOs, and a `QueuePrioritizationService`. One new Minimal API endpoint group `/api/my-queue`. No migrations.
- **Frontend**: one new feature folder under `src/MentalMetal.Web/ClientApp/src/app/features/my-queue/` with the queue list page component, signal-based service, filter controls, and route registration. New nav entry in the primary shell.
- **Tests**: unit tests for `QueuePrioritizationService` scoring and suggestion heuristics, handler test (unioning + filtering + user isolation), Angular component test, and an integration test over the endpoint using seeded commitments/delegations/captures.
- **Config**: two app-settings knobs (with sensible defaults baked in): `MyQueue:CommitmentDueSoonDays` (7), `MyQueue:CaptureStalenessDays` (3), `MyQueue:DelegationStalenessDays` (7). No secrets.
- **API consumers**: none today beyond the Angular app. No versioning concerns.
