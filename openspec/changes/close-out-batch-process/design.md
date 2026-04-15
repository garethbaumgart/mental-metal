## Context

The `/close-out` route (frontend feature folder `src/MentalMetal.Web/ClientApp/src/app/features/daily-close-out/`) renders a list of triage cards for captures still needing attention (Raw, Processing, Failed, Processed-pending-resolution). Each card today exposes Reassign and Quick-discard; Processed-pending-resolution cards also expose Confirm and Discard of the extraction. There is no way to trigger AI processing from this page — a user must click into the capture's detail route and press "Process with AI" (wired to the existing `POST /api/captures/{id}/process` handler in `src/MentalMetal.Application/Captures/ProcessCapture.cs`).

The product brief frames feature #9 as a "2-minute end-of-day ritual". A manager's typical end-of-day backlog is several Raw captures that all need extraction before they can be confirmed. Without a batch affordance, the ritual requires N navigations and N-1 returns to the queue.

## Goals / Non-Goals

**Goals:**
- Let the user fire AI extraction on a single Raw capture without leaving `/close-out`.
- Let the user fire AI extraction on every Raw capture in the current queue with one tap.
- Keep the bulk flow resilient: one capture's failure does not abort the rest.
- Surface the existing `ai_provider_not_configured` error UX (the same dashboard-widget surface) when the user has no provider configured, instead of spamming per-card failures.

**Non-Goals:**
- No backend changes. The existing `POST /api/captures/{id}/process` endpoint is reused verbatim.
- No new domain events, no new aggregates, no new value objects.
- No new bulk-process API. Orchestration is purely client-side.
- No re-processing of already-Processed captures, no retry-from-close-out for Failed captures (retry lives on the detail view today; bulk action targets Raw only).
- No audio-capture handling.
- No persisted "bulk job" state — the orchestration is ephemeral (a signal in the page component) and survives only the current session.

## Dependencies

Depends on capabilities already shipped:
- `capture-ai-extraction` — owns `POST /api/captures/{id}/process`, the `BeginProcessing()` / processing-pipeline behaviour, and the canonical per-capture processing scenarios. This change composes over it and does not duplicate its scenarios.
- `daily-close-out` — owns the `/close-out` route, the triage queue endpoint, and the existing per-row actions. This change modifies its UI requirements only.
- `ai-provider-abstraction` — owns the `ai_provider_not_configured` error shape already surfaced by the dashboard briefing widget.

No affected aggregates (no domain behaviour changes). No domain events added or modified.

## Decisions

### D1: Client-side fan-out instead of a backend bulk endpoint

The bulk action iterates the Raw captures currently visible in the queue signal and issues one `POST /api/captures/{id}/process` per capture. Rationale:
- Zero backend surface area. The existing endpoint is already `202 Accepted` and returns immediately after calling `BeginProcessing()` and enqueueing the job — so N client-side calls are cheap and do not hold the UI thread.
- The user's "Raw captures in the queue right now" is already computed on the client; a server-side bulk endpoint would have to re-derive that and re-validate ownership per capture anyway.
- Matches the scope constraint (no backend changes).

**Alternative considered**: a new `POST /api/daily-close-out/process-all` handler that loads the user's Raw captures and calls `BeginProcessing()` in a loop inside a single handler. Rejected because it adds a handler, a DTO, a route registration, and tests for behaviour that is already composed from an existing endpoint — violates the "keep scope SMALL" constraint.

### D2: Concurrency model — sequential with small parallelism, continue-on-error

Issue requests with a small fixed parallelism (e.g., 3 in-flight) using `Promise.allSettled` semantics per batch, continuing through failures. Rationale:
- A user with ~15 captures should not wait for 15 serialized round-trips, but unbounded parallelism could thunder-herd the AI provider and blow past per-user rate limits.
- `allSettled` is the idiomatic way to guarantee "failures in one call never abort the loop" — requirement from the proposal.
- Per-card progress uses the existing `Processing` status badge which the triage card already knows how to render; the page simply re-fetches the queue after all fan-outs resolve (or after each batch) to pick up status transitions from the backend.

**Alternative considered**: fully sequential. Rejected as too slow for the common end-of-day case.
**Alternative considered**: unbounded parallelism. Rejected due to provider rate-limit risk and noisy network behaviour.

### D3: Provider-not-configured detection — short-circuit on first occurrence

The first `POST /api/captures/{id}/process` call that returns the `ai_provider_not_configured` error cancels subsequent in-flight/queued calls and surfaces the existing dashboard-widget error UX (a PrimeNG message linking the user to `/settings`). Rationale:
- Every subsequent call would hit the same error; retrying the fan-out is user-hostile.
- This matches the established pattern (dashboard briefing widget) so the user already recognises the fix path.

**Alternative considered**: let every capture fail with the same error and report in the summary. Rejected — the summary would be misleading ("15 of 15 failed") and the user would have to parse per-card errors to discover the root cause.

### D4: Summary surface — PrimeNG toast with counts

When the bulk action completes (all resolved, one way or another), show a PrimeNG toast: "Processed N of M · K failed" where N = successful 202s, M = total attempts, K = M − N. Rationale:
- Mirrors the existing "Close out the day" summary toast/dialog pattern.
- A toast is non-blocking, which keeps the ritual fast.
- The per-card Processing badges give the user a durable, card-level view that survives toast dismissal.

### D5: Per-row Process button visibility

The per-row Process button is rendered only when the card's processing status is `Raw`. For `Processing`, `Failed`, `Processed-pending-resolution` cards, the button is hidden. This matches the existing detail-view rule ("Button hidden for non-raw captures" in `capture-ai-extraction`). Retry for Failed captures continues to live on the detail view — outside the scope of this change.

### D6: Disabled state for the bulk button

The Process-all button is disabled when (a) the bulk action is in flight, or (b) the current queue contains zero Raw captures. Rationale: (a) prevents double-fire; (b) avoids a tap that would no-op.

## Risks / Trade-offs

- [Stale queue after fan-out] The backend processes asynchronously; immediately after the bulk call resolves, cards may still read `Processing` rather than `Processed`. → Mitigation: the page already supports showing `Processing` badges; the user's next refresh or any existing polling picks up the transitions. No new polling introduced by this change.
- [Rate-limit near-miss with parallelism=3] Users on strict per-minute provider limits could still burst. → Mitigation: 3 is conservative, and the per-capture `TasteLimitExceededException` path already transitions the capture to `Failed` with a clear reason — the bulk summary will report those as failures.
- [User adds a capture mid-flow] The bulk action snapshots the Raw captures at click time; captures added later are not swept. → Accepted trade-off. The user can click again.
- [No telemetry] We are not emitting any new events or metrics for the bulk flow. → Accepted for a Tier 3 UI enhancement.

## Open Questions

- Parallelism value: 3 is an educated default. If early dogfood shows either too-slow (want 5) or too-bursty (want 1), the constant is trivial to tune — it lives in the close-out service.
