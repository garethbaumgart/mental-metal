## Why

Engineering managers accumulate captures throughout the day (quick notes, transcripts, meeting notes). Without a deliberate end-of-day triage flow, raw and processed-but-unconfirmed captures pile up and AI extractions go unactioned, defeating the purpose of `capture-ai-extraction`. A daily close-out ritual gives the user a focused queue to confirm/discard/reassign each pending capture and a recorded "I closed out today" timestamp so streaks and rhythm coaching can build on it later.

## What Changes

- New backend endpoints under `/api/daily-close-out/`:
  - `GET /api/daily-close-out/queue` — returns the user's current close-out queue: captures with status `Raw`, `Processing`, `Failed`, or `Processed-but-not-confirmed-or-discarded`, plus aggregate counts.
  - `POST /api/daily-close-out/captures/{id}/quick-discard` — remove a capture from the queue without processing its extraction. Implemented by marking the capture `Triaged = true` (the same flag set by any confirm/discard action); the "discard" in the name refers to the user intent ("skip this one") and the domain event (`CaptureQuickDiscarded`), not a separate storage state.
  - `POST /api/daily-close-out/captures/{id}/reassign` — reassign linked people/initiatives on a capture as part of triage (delegates to existing link/unlink methods on the Capture aggregate).
  - `POST /api/daily-close-out/close` — record that the user closed out today; returns a summary (confirmed/discarded/remaining counts).
  - `GET /api/daily-close-out/log` — list recent close-out log entries (most-recent-first, paginated/limited).
- Domain additions on the `User` aggregate:
  - A small `DailyCloseOutLog` owned-collection entry per user/date with `Date`, `ClosedAtUtc`, `ConfirmedCount`, `DiscardedCount`, `RemainingCount`.
  - One `User.RecordDailyCloseOut(date, counts)` method (idempotent for the same date — overwrites the day's entry with the latest snapshot).
- Domain additions on the `Capture` aggregate:
  - A `Triaged` boolean (or status flag) so quick-discard can mark a capture as out-of-queue without losing its content.
  - `Capture.QuickDiscard()` method that sets the triaged flag and raises `CaptureQuickDiscarded`.
- Frontend Angular feature module `daily-close-out`:
  - Triage list view at `/close-out` that shows each pending capture (PrimeNG card/list), its AI extraction summary if processed, and per-card actions: Confirm, Discard extraction, Reassign, Quick-discard.
  - Progress indicator (X of Y triaged) and a "Close out the day" button that calls the close endpoint and shows the summary.
  - Uses `@if`/`@for`, signals, signal forms, `tailwindcss-primeui` color tokens — no hardcoded colors, no `*ngIf`.

## Capabilities

### New Capabilities
- `daily-close-out`: End-of-day triage flow over unprocessed captures with quick confirm/reassign/discard actions and a recorded close-out log per day.

### Modified Capabilities
- `capture-text`: Add a `Triaged` flag and `QuickDiscard` method on the Capture aggregate so the close-out flow can remove a capture from the queue without deleting it. List/get endpoints expose `triaged` and the list endpoint excludes triaged items by default.
- `user-auth-tenancy`: Add `DailyCloseOutLog` owned collection on the User aggregate with `RecordDailyCloseOut` method to track close-out history.

## Impact

- New aggregates: none (extensions to `User` and `Capture`).
- New domain events: `CaptureQuickDiscarded`, `DailyCloseOutRecorded`.
- DB migration: add `Triaged` column to Captures; add `DailyCloseOutLogs` owned-collection table for the User aggregate.
- New vertical slice handlers in `MentalMetal.Application/Features/DailyCloseOut/`.
- New Angular feature in `src/MentalMetal.Web/ClientApp/src/app/features/daily-close-out/`.
- Tier: 3. Depends on already-shipped Tier 2 specs `capture-text` and `capture-ai-extraction`.

## Non-goals

- No scheduled reminders or push notifications.
- No weekly rollup or streak gamification (separate spec).
- No queue prioritisation logic (covered by `my-queue`).
- No AI re-processing inside the close-out flow (user can still trigger Process / Retry via existing capture endpoints).
