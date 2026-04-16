## MODIFIED Requirements

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

## ADDED Requirements

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
