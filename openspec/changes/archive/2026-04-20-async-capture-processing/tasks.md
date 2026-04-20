## 1. Backend — Decouple extraction from HTTP request

- [x] 1.1 Extract background extraction logic: create `BackgroundExtractionTrigger` service that accepts `(captureId, userId)`, creates a new DI scope via `IServiceScopeFactory`, sets user context via `IBackgroundUserScope.SetUserId()`, and calls `AutoExtractCaptureHandler.HandleAsync()`
- [x] 1.2 Modify `POST /api/captures` endpoint in `Program.cs` to return `201 Created` immediately with the capture in `Raw` status, then fire extraction via `BackgroundExtractionTrigger` (no await)
- [x] 1.3 Modify `POST /api/captures/import` endpoint to return immediately after capture creation, fire extraction in background
- [x] 1.4 Modify `POST /api/captures/audio` (UploadAudioCapture) to fire extraction in background after transcription completes (transcription itself may remain synchronous since it writes to the capture)
- [x] 1.5 Add unit tests for `BackgroundExtractionTrigger` verifying scope creation, user context setup, and error handling (capture marked `Failed` on exception)

## 2. Frontend — Global capture processing service

- [x] 2.1 Create `CaptureProcessingTracker` service (singleton/root-provided) with: `track(captureId)` to add a capture to the tracking set, `completions$` observable that emits when tracked captures finish, and internal 5-second polling logic
- [x] 2.2 Wire `CaptureProcessingTracker.track()` into `QuickCaptureDialogComponent` — call `track(capture.id)` after successful creation
- [x] 2.3 Wire `CaptureProcessingTracker.track()` into captures list page for file uploads and recorder submissions
- [x] 2.4 Add PrimeNG `Toast` to the app shell (`app.html`) and subscribe to `CaptureProcessingTracker.completions$` to show success/failure toasts with capture title, extraction summary (commitment count, people count), and View link

## 3. Frontend — Inline processing queue on captures list

- [x] 3.1 Add "Currently Processing" section to `CapturesListComponent` template: conditionally rendered when any captures have `processingStatus` of `Raw` or `Processing`, showing each item with spinner, title, and stage label
- [x] 3.2 Add 3-second polling logic to `CapturesListComponent`: start when processing items exist, stop after 2 clean polls, restart when new capture created
- [x] 3.3 Add optimistic queue entry: when a capture is created from this page (file drop, recorder), immediately add it to the processing queue signal from the API response
- [x] 3.4 Style the inline queue using PrimeNG tokens and Tailwind layout utilities (match mockup Option 5 — indigo-tinted background, spinner icons, stage labels)

## 4. Frontend — Quick capture dialog update

- [x] 4.1 Update `QuickCaptureDialogComponent` to close immediately on successful API response (no longer waits for extraction). The `submitting` signal still gates the button during the HTTP call, but the call now returns in <500ms
- [x] 4.2 Show a brief "Capture saved — processing in background" info toast immediately after dialog closes

## 5. Testing

- [x] 5.1 Add application-level tests verifying that capture creation endpoints no longer call `AutoExtractCaptureHandler` synchronously
- [x] 5.2 Add frontend unit tests for `CaptureProcessingTracker` service (tracking, polling, completion emission)
- [x] 5.3 Add frontend unit tests for the inline processing queue (visibility toggle, item rendering, polling lifecycle)
- [x] 5.4 Run full backend test suite (`dotnet test src/MentalMetal.slnx`) and frontend tests (`ng test --watch=false`) — verify no regressions
