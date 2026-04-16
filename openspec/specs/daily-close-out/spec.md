# Daily Close-Out

## Purpose

End-of-day triage flow over the user's pending captures. Lets an engineering manager walk through a focused queue of captures still needing attention (raw, processing, failed, or processed-but-unresolved), quickly confirm/discard/reassign/quick-discard each one, and record a per-day close-out log with counts so streaks and rhythm coaching can build on it later.

## Requirements

### Requirement: Get the close-out queue

The system SHALL expose `GET /api/daily-close-out/queue` returning the authenticated user's pending-triage captures and aggregate counts. A capture is in the queue when it belongs to the user, is not triaged, and is in one of: status `Raw`, `Processing`, `Failed`, or `Processed` with extraction not yet resolved (neither confirmed nor discarded). The response SHALL include an array of queue items (each with capture id, title, type, processing status, captured-at, and the AI extraction summary if processed) and counts grouped by `raw`, `processing`, `failed`, `processedPendingResolution`, and `total`. The list SHALL be ordered by CapturedAt descending.

#### Scenario: Queue with mixed statuses

- **WHEN** an authenticated user with one Raw capture, one Failed capture, and one Processed-but-unresolved capture sends GET /api/daily-close-out/queue
- **THEN** the response includes all three captures and counts { raw: 1, processing: 0, failed: 1, processedPendingResolution: 1, total: 3 }

#### Scenario: Triaged captures excluded

- **WHEN** an authenticated user has one capture marked Triaged and one Raw capture
- **THEN** the queue response includes only the Raw capture

#### Scenario: Confirmed-or-discarded extractions excluded

- **WHEN** an authenticated user has a Processed capture whose extraction was confirmed
- **THEN** the queue response does not include that capture

#### Scenario: Empty queue

- **WHEN** an authenticated user with no pending captures sends GET /api/daily-close-out/queue
- **THEN** the system returns an empty items array and all counts equal zero

#### Scenario: User isolation

- **WHEN** User A and User B each have pending captures
- **THEN** each user's queue contains only their own captures

### Requirement: Quick-discard a capture

The system SHALL expose `POST /api/daily-close-out/captures/{id}/quick-discard` that calls `Capture.QuickDiscard()` on the aggregate, marking the capture as triaged with TriagedAtUtc set to now, and raising a `CaptureQuickDiscarded` domain event. The capture SHALL no longer appear in the close-out queue. The endpoint SHALL be idempotent: calling it on an already-triaged capture returns HTTP 200 without error.

#### Scenario: Quick-discard a raw capture

- **WHEN** an authenticated user POSTs to /api/daily-close-out/captures/{id}/quick-discard for a Raw capture
- **THEN** the capture is marked Triaged with TriagedAtUtc set, a CaptureQuickDiscarded event is raised, and the system returns HTTP 200

#### Scenario: Quick-discard already-triaged capture

- **WHEN** an authenticated user POSTs quick-discard on a capture that is already triaged
- **THEN** the system returns HTTP 200 and does not raise a duplicate event

#### Scenario: Quick-discard not-found

- **WHEN** an authenticated user POSTs quick-discard for a capture id that does not exist or belongs to another user
- **THEN** the system returns HTTP 404

### Requirement: Reassign capture links during triage

The system SHALL expose `POST /api/daily-close-out/captures/{id}/reassign` accepting `{ personIds: Guid[], initiativeIds: Guid[] }`. The handler SHALL diff the supplied IDs against the capture's current `LinkedPersonIds` and `LinkedInitiativeIds` and call the existing `LinkPerson`, `UnlinkPerson`, `LinkInitiative`, `UnlinkInitiative` methods on the Capture aggregate to converge to the supplied set. The response SHALL be HTTP 200 with the updated capture summary.

#### Scenario: Add and remove links in one call

- **WHEN** an authenticated user reassigns a capture currently linked to PersonA and InitiativeX, sending personIds [PersonB] and initiativeIds [InitiativeX, InitiativeY]
- **THEN** the capture is unlinked from PersonA, linked to PersonB, kept linked to InitiativeX, and linked to InitiativeY

#### Scenario: Reassign with empty arrays clears links

- **WHEN** an authenticated user reassigns a capture with personIds [] and initiativeIds []
- **THEN** the capture has no linked people or initiatives

#### Scenario: Reassign not-found

- **WHEN** an authenticated user reassigns a capture id that does not exist or belongs to another user
- **THEN** the system returns HTTP 404

#### Scenario: Reassign with unknown person id

- **WHEN** an authenticated user reassigns a capture with a personId that does not match any of their People
- **THEN** the system returns HTTP 400 with a validation error and no changes are persisted

### Requirement: Record daily close-out

The system SHALL expose `POST /api/daily-close-out/close` accepting an optional `{ date: ISODate }` body (defaulting to today in UTC) that calls `User.RecordDailyCloseOut(date, counts)` where counts are computed at the time of the call: confirmed = number of triaged captures whose extraction was confirmed today, discarded = number of triaged-via-quick-discard or extraction-discarded captures today, remaining = current queue size. The response SHALL be HTTP 200 with the recorded log entry. Calling close twice for the same date SHALL overwrite the prior entry (idempotent).

#### Scenario: First close-out of the day

- **WHEN** an authenticated user POSTs /api/daily-close-out/close with no body and the queue has 0 remaining items
- **THEN** the system records a DailyCloseOutLog for today with computed counts and returns HTTP 200 with the entry

#### Scenario: Close-out twice in one day

- **WHEN** an authenticated user POSTs close, triages another capture, then POSTs close again on the same day
- **THEN** the second call overwrites the day's log entry with the latest counts and ClosedAtUtc

#### Scenario: Close-out with explicit date

- **WHEN** an authenticated user POSTs close with body { date: "2026-04-13" }
- **THEN** the log entry is recorded against 2026-04-13

#### Scenario: User isolation

- **WHEN** User A closes out their day
- **THEN** User B's close-out log is unaffected

### Requirement: Get close-out log history

The system SHALL expose `GET /api/daily-close-out/log?limit=N` returning the authenticated user's most recent close-out log entries ordered by Date descending. Default limit is 30, max 90. Each entry SHALL include date, closedAtUtc, confirmedCount, discardedCount, and remainingCount.

#### Scenario: List recent close-outs

- **WHEN** an authenticated user with five close-out log entries sends GET /api/daily-close-out/log
- **THEN** the system returns all five entries ordered newest-date first

#### Scenario: Limit parameter

- **WHEN** an authenticated user sends GET /api/daily-close-out/log?limit=2 and has five entries
- **THEN** the system returns the two newest entries

#### Scenario: Limit clamped to maximum

- **WHEN** an authenticated user sends GET /api/daily-close-out/log?limit=500
- **THEN** the system returns at most 90 entries

#### Scenario: Empty log

- **WHEN** an authenticated user who has never closed out sends GET /api/daily-close-out/log
- **THEN** the system returns an empty array with HTTP 200

### Requirement: Triage UI page

The frontend SHALL provide a `/close-out` route rendering a page that shows the close-out queue. Each queue item SHALL be rendered as a PrimeNG card showing the capture's title (or first line of content), type, processing-status badge, captured-at timestamp, and — when extraction is available — the AI summary plus extracted-item counts (commitments, delegations, observations). Each card SHALL expose action buttons: Confirm extraction (visible only when extraction is unresolved Processed), Discard extraction (same visibility), Reassign, Quick-discard, and **Process** (visible only when the card's processing status is `Raw`). The Process action SHALL call `POST /api/captures/{id}/process` — the canonical per-capture processing semantics are defined by the `capture-ai-extraction` capability and are not duplicated here. The Process button SHALL be rendered alongside, not in place of, the existing Reassign and Quick-discard actions. The page SHALL use `@if` and `@for` control flow, signals for state, and PrimeNG/`tailwindcss-primeui` colour tokens only — no `*ngIf`, no `*ngFor`, no hardcoded Tailwind colour utilities, no `dark:` prefixes.

#### Scenario: Render queue with mixed items

- **WHEN** the user navigates to /close-out and the queue has three items
- **THEN** the page renders three cards in CapturedAt-descending order with the appropriate action buttons per item state

#### Scenario: Empty-state

- **WHEN** the user navigates to /close-out and the queue is empty
- **THEN** the page shows an empty-state message inviting the user to close out the day

#### Scenario: Confirm extraction from card

- **WHEN** the user clicks Confirm on a Processed-pending-resolution card
- **THEN** the frontend calls POST /api/captures/{id}/confirm-extraction, removes the card from the queue on success, and updates the progress indicator

#### Scenario: Quick-discard from card

- **WHEN** the user clicks Quick-discard on any card
- **THEN** the frontend calls POST /api/daily-close-out/captures/{id}/quick-discard, removes the card from the queue on success, and updates the progress indicator

#### Scenario: Process button visible only on Raw cards

- **WHEN** the queue contains a Raw card, a Processing card, a Failed card, and a Processed-pending-resolution card
- **THEN** only the Raw card renders the Process action button; the other three cards do not render it

#### Scenario: Process a single Raw card from close-out

- **WHEN** the user clicks Process on a Raw card
- **THEN** the frontend calls POST /api/captures/{id}/process and the card's processing-status badge transitions to Processing on success
- **AND** the Reassign and Quick-discard buttons on that card remain available

### Requirement: Bulk process-all-raw action

The frontend close-out page SHALL provide a "Process all raw" button rendered adjacent to the existing "Close out the day" button. When clicked, the frontend SHALL iterate the Raw captures currently visible in the triage queue and invoke `POST /api/captures/{id}/process` for each, using a small fixed client-side parallelism. While the bulk action is in flight, the button SHALL be disabled and each targeted card SHALL surface its existing `Processing` status badge. The button SHALL also be disabled when the current queue contains zero Raw captures. Individual `POST /api/captures/{id}/process` failures SHALL NOT abort the bulk flow; the orchestration SHALL continue through the remaining captures. When the bulk action resolves, the frontend SHALL display a summary toast of the form "Processed N of M · K failed" where N is the count of successful responses, M is the total number of Raw captures attempted, and K is M − N. The canonical per-capture processing semantics (status transitions, conflict/not-found handling, pipeline behaviour) are owned by the `capture-ai-extraction` capability and are not duplicated here.

#### Scenario: Bulk process with all captures succeeding

- **WHEN** the user clicks "Process all raw" with five Raw captures visible in the queue and all five POST /api/captures/{id}/process calls return 202 Accepted
- **THEN** the frontend fires one process call per Raw capture, each card's badge transitions to Processing, and the summary toast reads "Processed 5 of 5 · 0 failed"

#### Scenario: Bulk process continues through per-capture failures

- **WHEN** the user clicks "Process all raw" with ten Raw captures visible and two of the POST /api/captures/{id}/process calls return an error response while the other eight succeed
- **THEN** the frontend completes calls for all ten captures (the two failures do not abort the flow), the eight successful cards transition to Processing, and the summary toast reads "Processed 8 of 10 · 2 failed"

#### Scenario: Button disabled while in flight

- **WHEN** the user clicks "Process all raw" and the bulk orchestration is still resolving
- **THEN** the "Process all raw" button is disabled until the final summary is shown

#### Scenario: Button disabled when no Raw captures present

- **WHEN** the close-out queue contains only Processing, Failed, and Processed-pending-resolution cards
- **THEN** the "Process all raw" button is rendered in a disabled state

#### Scenario: Provider not configured short-circuits the bulk flow

- **WHEN** the user clicks "Process all raw" and the first POST /api/captures/{id}/process response surfaces the `ai_provider_not_configured` error
- **THEN** the frontend cancels remaining/queued bulk calls, does not display the "Processed N of M" summary, and instead surfaces the same provider-not-configured message used by the dashboard briefing widget, including a link to /settings

#### Scenario: Per-card Processing badge during bulk flow

- **WHEN** the bulk action is in flight against a given Raw capture and its POST /api/captures/{id}/process call returns 202 Accepted
- **THEN** that card renders the existing Processing status badge without any new bespoke indicator introduced by this change

### Requirement: Close-out the day action

The frontend close-out page SHALL provide a "Close out the day" button that calls `POST /api/daily-close-out/close` and displays the returned summary (confirmed, discarded, remaining counts) in a PrimeNG dialog or toast. The button SHALL be enabled regardless of remaining queue size.

#### Scenario: Close out with empty queue

- **WHEN** the user clicks "Close out the day" and the queue is empty
- **THEN** the frontend calls the close endpoint and displays a summary showing remaining: 0

#### Scenario: Close out with items still in queue

- **WHEN** the user clicks "Close out the day" while three items remain
- **THEN** the frontend calls the close endpoint and displays a summary showing remaining: 3, with the items still visible on the page

### Requirement: Reassign UI dialog

The frontend SHALL provide a reassign dialog opened from the Reassign action button on a triage card. The dialog SHALL render a multi-select for People and a multi-select for Initiatives (both populated from the user's existing data) using PrimeNG and Signal Forms. Submitting the dialog SHALL call `POST /api/daily-close-out/captures/{id}/reassign` with the selected IDs and update the card on success.

#### Scenario: Open and submit reassign dialog

- **WHEN** the user opens the reassign dialog, selects two people and one initiative, and submits
- **THEN** the frontend calls the reassign endpoint with those IDs and the card updates to show the new links

#### Scenario: Cancel reassign dialog

- **WHEN** the user opens the reassign dialog and clicks cancel
- **THEN** no API call is made and the card is unchanged
