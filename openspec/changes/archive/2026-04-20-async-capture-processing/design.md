## Context

Capture creation currently runs the full AI extraction pipeline synchronously within the HTTP request handler. The pipeline (AI completion → name resolution → initiative tagging → commitment spawning) takes 5–30 seconds depending on content length and AI provider latency. During this time the frontend is blocked — the submit button shows a spinner and the user cannot navigate or do anything else.

The app is single-tenant (one user per deployment) running on Google Cloud Run. There is no job queue infrastructure (Hangfire, Cloud Tasks, etc.) and adding one is out of scope.

**Dependencies**: capture-text, capture-ai-extraction (both already implemented).

## Goals / Non-Goals

**Goals:**
- Capture creation endpoints return in <500ms with the capture in `Raw` status
- Extraction runs as a fire-and-forget background task within the same process
- Users see real-time processing status on the captures list page
- Users get notified when extraction completes, regardless of which page they're on

**Non-Goals:**
- Durable job queue infrastructure (Hangfire, Cloud Tasks) — overkill for single-tenant
- WebSocket/SSE push notifications — polling is simpler and sufficient
- Retry logic beyond what already exists on the capture detail page
- Changes to the quick-capture dialog layout

## Decisions

### 1. Fire-and-forget background extraction via `IHostedService` scope

**Decision**: Use `Task.Run` with a manually-created DI scope to fire the extraction pipeline after the HTTP response returns.

**Why**: Cloud Run processes are ephemeral but long-lived enough for our use case (requests complete in <30s). A full job queue adds deployment complexity (Redis, Hangfire dashboard, etc.) that isn't justified for a single-tenant app. The `IServiceScopeFactory` pattern gives us a proper DI scope with its own `DbContext` and `ICurrentUserService` — critical since the HTTP request scope is disposed before extraction completes.

**Alternative considered**: `IHostedService` with a `Channel<T>` — more structured but adds complexity for no benefit when we only need fire-and-forget.

**Risk mitigation**: The `IBackgroundUserScope.SetUserId()` mechanism already exists for setting the user context outside of HTTP requests. If the process terminates mid-extraction, the capture remains in `Processing` status and the user can retry from the detail page.

### 2. Polling via existing GET /api/captures endpoint

**Decision**: The frontend polls `GET /api/captures?status=Processing` on a 3-second interval when there are items in the processing queue. No new polling endpoint needed.

**Why**: The existing endpoint already returns `processingStatus` for each capture. Filtering by `status=Processing` is already supported. Adding a dedicated polling endpoint would be redundant.

**Polling lifecycle**: Start polling when a capture is submitted or when the page loads with processing items visible. Stop when no items have `Processing` status for 2 consecutive polls. Use `setInterval` with cleanup on component destroy.

**Alternative considered**: New `GET /api/captures/processing-status` lightweight endpoint — cleaner but adds API surface for marginal performance gain.

### 3. Global toast service for completion notifications

**Decision**: Create a singleton `CaptureProcessingService` in the frontend that tracks in-flight capture IDs. It polls for status changes and emits completion events. The app shell subscribes and shows PrimeNG `Toast` notifications.

**Why**: Toasts must appear from any page, not just the captures list. A global service that polls for recently-submitted captures gives us cross-page notifications without WebSockets.

**Lifecycle**: When a capture is created (from quick-capture dialog or captures page), its ID is added to the tracking set. The service polls every 5 seconds. When a tracked capture transitions to `Processed` or `Failed`, a toast is shown and the ID is removed from tracking. Tracking state lives in memory (no persistence needed — refresh = re-discover from API).

### 4. Inline processing queue UI (Option 5 from mockups)

**Decision**: Add a "Currently Processing" section at the top of the captures list component. Items in this section show a spinner with the current stage label. When processing completes, items animate into the main table.

**Why**: User selected this approach from 10 mockup options. It's contextually appropriate (visible on the page where captures live), doesn't add permanent UI chrome to other pages, and complements the toast notifications for cross-page awareness.

## Risks / Trade-offs

- **[Process termination during extraction]** → Capture stays in `Processing` forever. Mitigation: existing "Retry" button on capture detail page; future enhancement could add a stale-processing detector.
- **[Polling battery/network cost]** → 3s interval is aggressive. Mitigation: only poll when processing items exist; stop after 2 clean polls; only poll on captures list page. Global service polls at 5s, only for tracked IDs.
- **[Race condition: capture created but not yet visible]** → Between HTTP response and first poll, the capture might not appear in the inline queue. Mitigation: optimistically add the capture to the queue from the creation response (Raw status).
- **[DI scope leak in background task]** → The background task must create its own scope and not capture any request-scoped services. Mitigation: only pass primitive values (captureId, userId) to the background task; resolve all services from the new scope.
