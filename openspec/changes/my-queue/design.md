# Design — My Queue

## Context

Three Tier-2 capabilities already ship their own list endpoints and UI: `commitment-tracking`, `delegation-tracking`, and `capture-ai-extraction` (via the raw `Capture` aggregate and the recently-shipped `daily-close-out` queue). Users today have to visit three screens to see "what's pulling on me right now" — overdue commitments they owe, stale delegations waiting on someone else, and captures that were dropped in but never processed.

This change is **purely read-side**. No aggregates are modified, no events are added, no migrations run. We introduce an Application-layer `QueuePrioritizationService` that reads the three aggregates via their existing repositories and returns a unified, scored, filterable `QueueItem` projection. The Angular frontend gets a new route rendering it.

Per `design/spec-plan.md` line 34, the service placeholder for Tier-3 `my-queue` is `(QueuePrioritizationService)` — parenthesised because it is a domain/application service, not an aggregate.

## Dependencies

- `commitment-tracking` (shipped, archived) — reads `Commitment` aggregate
- `delegation-tracking` (shipped, archived) — reads `Delegation` aggregate
- `capture-ai-extraction` (shipped, archived) — reads `Capture` aggregate, including `Status`, `TriagedAtUtc`, `ExtractionConfirmed`/`ExtractionDiscarded` state
- `user-auth-tenancy` (shipped) — `ICurrentUserService` to scope all queries by `UserId`

## Goals / Non-Goals

**Goals:**

- Single endpoint `GET /api/my-queue` that returns a user-scoped, sorted, filterable list of items needing attention
- Deterministic, testable priority scoring that produces stable sort order
- Surface a "delegate this" suggestion on commitments where a reasonable heuristic fires, without persisting any new state
- Frontend feature slice mirroring existing patterns (commitments/delegations lists) using signals, PrimeNG, and `tailwindcss-primeui` tokens only

**Non-Goals:**

- No new persisted state, no new aggregate, no domain events
- No AI calls on the hot path — priority is a deterministic formula. (Future: layer LLM re-ranking on top.)
- No push/notification/reminder surface — that lives in the future `nudges-rhythms` spec
- No cross-user aggregation
- No mutating actions directly on `/api/my-queue` — all state changes still go through the existing commitment / delegation / capture endpoints. The queue returns IDs and enough context to dispatch actions via those existing APIs.

## Decisions

### D1. Query-side composition, not a new aggregate

**Decision:** Implement as an Application-layer `QueuePrioritizationService` + a single `GetMyQueue` handler. Union data across existing repositories.

**Alternatives considered:**

- *Materialised view table updated by domain events.* Rejected — creates schema churn, needs backfill, adds eventual-consistency window for a low-traffic single-user view. Not worth it.
- *Client-side aggregation by fanning out to `/api/commitments`, `/api/delegations`, `/api/captures`.* Rejected — three round-trips, duplicated priority logic in TS, and the "delegate-this" heuristic needs a cross-entity query (find delegations linked to same person) that is cleaner on the server.

**Rationale:** The read volume is tiny (one user, tens to low-hundreds of open items). Postgres can trivially serve three filtered queries per request. Keeping the logic server-side centralises scoring and isolates future changes from the frontend.

### D2. Priority scoring formula

**Decision:** A deterministic additive score computed per item. Higher score = higher priority. Rounded to an int for display stability.

For **commitments** (open, direction `MineToThem`):

```
score = 0
if IsOverdue: score += 100 + min(daysOverdue * 5, 100)          // caps contribution at +200
else if DueDate within CommitmentDueSoonDays:
    score += max(0, 50 - (daysUntilDue * 5))                    // 50 today, 0 at window edge
if DueDate is null: score += 10                                 // low-signal nudge
```

For **delegations** (status `Assigned`|`InProgress`|`Blocked`):

```
priorityWeight = { Urgent: 60, High: 40, Medium: 20, Low: 5 }[delegation.Priority]
score = priorityWeight
if IsOverdue: score += 80 + min(daysOverdue * 4, 80)
daysSinceTouch = (now - (LastFollowedUpAt ?? CreatedAt)).Days
if daysSinceTouch >= DelegationStalenessDays:
    score += min((daysSinceTouch - DelegationStalenessDays) * 3 + 20, 80)
if status == Blocked: score += 25                                // surface blockers
```

For **captures** (not triaged AND status in {Raw, Failed} OR status=Processed with extraction unresolved):

```
daysSinceCaptured = (now - CapturedAtUtc).Days
if daysSinceCaptured < CaptureStalenessDays: skip                // not in queue yet
score = 30 + min((daysSinceCaptured - CaptureStalenessDays) * 4, 60)
if status == Failed: score += 20                                 // failures are sticky
```

Secondary sort (ties): `DueDate asc, CapturedAt desc, Id asc` — deterministic.

**Alternatives considered:**

- *Weighted multiplicative formula.* Harder to reason about and to unit-test.
- *LLM-based priority.* Latency, cost, non-determinism; unsuitable for v1.

**Rationale:** Trivially unit-testable, documented, user-tunable later.

### D3. "Delegate this" suggestion heuristic

**Decision:** Transient, per-request, computed in `QueuePrioritizationService`.

A commitment queue item has `suggestDelegate = true` iff **all** hold:

1. Item is a commitment with direction `MineToThem` and status `Open`
2. Commitment has a non-null `PersonId`
3. The user has ≥ 1 existing delegation to that same `PersonId` whose status is not `Cancelled`/`Completed` OR any delegation (including completed) to that same person — we pick "any non-cancelled delegation ever" as the established-relationship signal
4. There is no existing open delegation whose `Notes` or `Description` references this commitment's ID (we don't persist a link; this is just "we haven't already acted on it"). **V1 simplification:** skip check 4 — we emit the hint regardless and rely on the user to ignore duplicates. Reconsider in v2 if it's noisy.

Implementation: preload a `HashSet<Guid>` of `PersonIds` with active delegations for the user in one query, then membership-check per commitment item in memory. Because this HashSet lives purely in memory for scoring (never in EF LINQ), it does NOT trip the EF `HashSet.Contains` translation pitfall.

### D4. Filtering

**Decision:** Query-string filters applied server-side:

- `scope`: `overdue` (only items whose `IsOverdue` is true, or captures past staleness) | `today` (due today or overdue; captures always included) | `thisWeek` (due within 7 calendar days or overdue) | `all` (default)
- `itemType`: repeated query param, e.g. `?itemType=commitment&itemType=delegation`; defaults to all three
- `personId`: a single Guid. Matches commitment.PersonId, delegation.DelegatePersonId, capture.LinkedPersonIds containment
- `initiativeId`: a single Guid. Matches commitment.InitiativeId, delegation.InitiativeId, capture.LinkedInitiativeIds containment

All filters are composable. Unknown enum values return HTTP 400.

### D5. EF Core LINQ

**Decision:** Three independent `IQueryable`s (one per aggregate), each applying user-scope and applicable filters at the database level. Materialise, then union, score, sort, and page in memory.

**CLAUDE.md compliance:**

- Use `List<T>.Contains(x)` for Guid set membership inside `Where` — never `HashSet.Contains` (untranslatable)
- No `.ToLowerInvariant()` — we don't do any string comparison in predicates, but keep the rule in mind if filters ever expand
- Return DTOs (`QueueItemResponse`), never domain entities

### D6. Endpoint shape

`GET /api/my-queue?scope={scope}&itemType={type}&itemType={type}&personId={guid}&initiativeId={guid}`

Response:

```
{
  items: [
    {
      itemType: "commitment" | "delegation" | "capture",
      id: Guid,                     // source aggregate ID
      title: string,                // description or title
      status: string,               // source aggregate status
      dueDate: string? (ISO),       // commitments / delegations only
      isOverdue: bool,
      personId: Guid?,
      personName: string?,
      initiativeId: Guid?,
      initiativeName: string?,
      daysSinceCaptured: int?,      // captures only
      lastFollowedUpAt: string?,    // delegations only
      priorityScore: int,
      suggestDelegate: bool         // commitments only; always false otherwise
    },
    ...
  ],
  counts: {
    overdue: int,
    dueSoon: int,
    staleCaptures: int,
    staleDelegations: int,
    total: int
  },
  filters: {
    scope: "all",
    itemType: ["commitment", "delegation", "capture"],
    personId: null,
    initiativeId: null
  }
}
```

Sorted by `priorityScore desc` with D2's tiebreakers. No pagination in v1 — the list is expected to be short.

### D7. Config

Three `IOptions<MyQueueOptions>` values with defaults: `CommitmentDueSoonDays=7`, `DelegationStalenessDays=7`, `CaptureStalenessDays=3`. Bound from `appsettings.json` section `MyQueue`. Defaults baked in so the default build is fully functional without config changes.

### D8. Frontend structure

- `src/MentalMetal.Web/ClientApp/src/app/features/my-queue/`
  - `my-queue.page.ts` — standalone page component, injects `MyQueueService`, holds filter signals
  - `my-queue.service.ts` — `HttpClient` + `signal()` state for queue response
  - `my-queue-item.component.ts` — renders a single row, type-aware badges/buttons
  - `my-queue.routes.ts` — lazy-loaded route
- Navigation entry added to the existing shell layout
- Use PrimeNG `Card`, `Tag`, `Button`, `SelectButton`/`Chip` for scope filter; `tailwindcss-primeui` utilities (`bg-surface-0`, `text-muted-color`, `bg-primary`) for colours
- No `*ngIf`/`*ngFor` — exclusively `@if`/`@for`; no `dark:` prefixes; no hardcoded hex

Inline "Delegate this" button on commitment rows where `suggestDelegate` is true navigates to `/delegations/new?description=...&personId=...&sourceCommitmentId=...` (the delegation-create form already accepts pre-filled query params per its existing route).

## Risks / Trade-offs

- **[In-memory scoring on potentially growing lists]** → Cap the per-type query: in the handler, fetch at most (say) 200 candidate items per type from the database, sorted by due/captured date, so the post-union memory work stays bounded. 200 is far above typical usage; if a user exceeds it we'll extend paging.
- **[Priority formula feels arbitrary]** → Pure functions, fully unit-tested per branch; tunable via options later. Users can still use the filters to focus manually.
- **[Delegate-this hint may be noisy]** → V1 ships with the simplified heuristic. Include telemetry/log when hint fires. If friction is reported in dogfooding, upgrade to D3's stricter check 4 by persisting a `SuggestedDelegationSource` link on the `Delegation` aggregate — separate future change.
- **[Three queries per request]** → Fine at this scale. If it becomes hot, a Postgres materialised view is the natural next step (D1 alternative).
- **[Time-zone for "today" / "this week"]** → Use UTC day boundaries in v1, consistent with the rest of the codebase. Document in the endpoint summary. `nudges-rhythms` will likely introduce per-user TZ; we revisit then.

## Migration Plan

No database migration. Deployment is a standard application release. Rollback = redeploy previous image; no data to undo.

## Open Questions

- None blocking. Time-zone handling and stricter delegate-hint heuristic are flagged as known v2 follow-ups above.
