## Why

Capture creation (text paste, file upload, audio recording) currently runs AI extraction synchronously within the HTTP request, blocking the UI for 5–30 seconds while the pipeline completes. Users cannot interact with the app during this time — no navigation, no parallel work. This makes the tool feel sluggish and discourages frequent capture, which is the core value loop.

## What Changes

- **Decouple extraction from the HTTP response**: API endpoints return immediately after saving the capture in `Raw` status. Extraction runs as a background task on the server.
- **Add "Currently Processing" inline queue**: A dedicated section at the top of the captures list page shows each in-flight capture with its current stage (Uploading → Transcribing → Extracting → Resolving) and a spinner. Items move to the main table once processing completes.
- **Add completion toasts**: When a capture finishes processing (from any page), a toast notification appears with an extraction summary (e.g. "3 commitments · 2 people found") and a View link.
- **Frontend polling**: The captures list page polls for status updates on processing items so the inline queue reflects real-time progress.

## Non-goals

- WebSocket/SSE for real-time push — polling is sufficient for this use case and far simpler to implement on Cloud Run.
- Retry UI for failed extractions — the existing "Retry" button on the capture detail page is sufficient.
- Background job queue infrastructure (Hangfire, Cloud Tasks) — a simple `Task.Run` fire-and-forget within the request scope is adequate given the single-user architecture.
- Changes to the quick-capture dialog UX — it already closes on submit; we just make it close faster.

## Capabilities

### New Capabilities
- `async-capture-pipeline`: Background extraction pipeline, polling endpoint, and inline processing queue UI with completion toasts.

### Modified Capabilities
- `capture-text`: POST /api/captures, POST /api/captures/import, and POST /api/captures/audio now return immediately with `Raw` status instead of waiting for extraction.
- `capture-ai-extraction`: Extraction is triggered as a background task rather than inline in the HTTP request.

## Impact

- **Backend**: `POST /api/captures`, `POST /api/captures/import`, `POST /api/captures/audio` endpoints change response behavior (return Raw instead of Processed). New or modified endpoint for polling processing status.
- **Frontend**: Captures list component gains inline queue section and polling logic. New global toast service for extraction completion notifications.
- **Aggregates affected**: Capture (status transitions), no schema changes needed.
- **API contract**: Response shape unchanged, but `processingStatus` in the response will now be `Raw` instead of `Processed` on creation. Existing clients that relied on extraction results being present in the creation response will need updating.
- **Tier**: Cross-cuts Tier 2 (capture-text, capture-ai-extraction). No new dependencies.
