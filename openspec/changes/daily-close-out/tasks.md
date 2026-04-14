## 1. Domain — Capture aggregate

- [x] 1.1 Add `Triaged` (bool) and `TriagedAtUtc` (DateTime?) properties to `Capture`
- [x] 1.2 Add `ExtractionResolved` (bool) property to `Capture`; set true in existing `ConfirmExtraction()` and `DiscardExtraction()` methods
- [x] 1.3 Add `Capture.QuickDiscard()` method (idempotent) raising `CaptureQuickDiscarded` domain event
- [x] 1.4 Add `CaptureQuickDiscarded` domain event record
- [x] 1.5 Unit tests: QuickDiscard on non-triaged sets flags + raises event; QuickDiscard on already-triaged is no-op; Confirm/Discard set ExtractionResolved

## 2. Domain — User aggregate

- [x] 2.1 Add `DailyCloseOutLog` owned entity (Date DateOnly, ClosedAtUtc, ConfirmedCount, DiscardedCount, RemainingCount)
- [x] 2.2 Add `DailyCloseOutLogs` owned-collection backing field on `User` with read-only accessor
- [x] 2.3 Add `User.RecordDailyCloseOut(date, confirmed, discarded, remaining)` method (idempotent overwrite, validates non-negative counts)
- [x] 2.4 Add `User.GetCloseOutLog(DateOnly date)` lookup method
- [x] 2.5 Add `DailyCloseOutRecorded` domain event record
- [x] 2.6 Unit tests: new-date append; same-date overwrite; negative count throws; multi-user isolation

## 3. Infrastructure — EF Core

- [x] 3.1 Update `CaptureConfiguration` to map `Triaged`, `TriagedAtUtc`, `ExtractionResolved`
- [x] 3.2 Add `DailyCloseOutLogConfiguration` as owned-collection of `User` (mirror `AiProviderConfig` pattern: snapshot change tracker, `PropertyAccessMode.Field`)
- [x] 3.3 Generate EF migration `AddDailyCloseOut` adding columns + table; backfill `ExtractionResolved = (status != Processed)` for existing captures
- [x] 3.4 Verify migration runs cleanly on the dev database

## 4. Application — vertical slices

- [x] 4.1 Create `Application/Features/DailyCloseOut/` folder
- [x] 4.2 `GetCloseOutQueue` query handler — returns queue items + counts, filters Triaged=false and (status in {Raw,Processing,Failed} or (Processed && !ExtractionResolved)), uses `List<T>.Contains` not `HashSet`
- [x] 4.3 `QuickDiscardCapture` command handler — loads capture, calls `QuickDiscard`, persists, returns 200/404
- [x] 4.4 `ReassignCapture` command handler — diffs supplied IDs vs current links, calls `Link/Unlink Person/Initiative`, validates referenced person/initiative IDs belong to user
- [x] 4.5 `CloseOutDay` command handler — computes today's counts (confirmed/discarded/remaining), calls `User.RecordDailyCloseOut`, persists, returns log entry
- [x] 4.6 `GetCloseOutLog` query handler — reads `DailyCloseOutLogs`, applies limit (default 30, max 90), descending date
- [x] 4.7 DTOs in the same folder (`CloseOutQueueResponse`, `CloseOutQueueItem`, `ReassignCaptureRequest`, `CloseOutDayRequest`, `DailyCloseOutLogDto`)

## 5. Web — minimal API endpoints

- [x] 5.1 Add `DailyCloseOutEndpoints.MapDailyCloseOutEndpoints(this IEndpointRouteBuilder)` extension
- [x] 5.2 Wire `GET /api/daily-close-out/queue`
- [x] 5.3 Wire `POST /api/daily-close-out/captures/{id}/quick-discard`
- [x] 5.4 Wire `POST /api/daily-close-out/captures/{id}/reassign`
- [x] 5.5 Wire `POST /api/daily-close-out/close`
- [x] 5.6 Wire `GET /api/daily-close-out/log`
- [x] 5.7 Register endpoints in `Program.cs`

## 6. Web — modify capture endpoints

- [x] 6.1 Update `ListCaptures` query/handler to filter `Triaged = false` by default and accept `includeTriaged=true`
- [x] 6.2 Update capture detail DTO to expose `triaged`, `triagedAtUtc`, `extractionResolved`
- [x] 6.3 Update `GetCurrentUser` (`/api/me`) response to include `lastCloseOutAtUtc` (max of `DailyCloseOutLogs.ClosedAtUtc` or null)

## 7. Web.IntegrationTests

- [x] 7.1 GetCloseOutQueue: mixed-status user returns expected items + counts; user isolation
- [x] 7.2 QuickDiscard endpoint: 200 + capture excluded from queue; idempotent; 404 for foreign capture
- [x] 7.3 Reassign endpoint: add/remove links converge; empty arrays clear; 400 for unknown person id
- [x] 7.4 CloseOutDay endpoint: first-call records entry; second-call same date overwrites; explicit date param works
- [x] 7.5 GetCloseOutLog endpoint: ordering, limit clamping, empty case
- [x] 7.6 ListCaptures default-excludes triaged; `includeTriaged=true` includes them
- [x] 7.7 GetCurrentUser includes `lastCloseOutAtUtc` after a recorded close-out

## 8. Frontend — feature module

- [x] 8.1 Create `src/MentalMetal.Web/ClientApp/src/app/features/daily-close-out/` folder
- [x] 8.2 `daily-close-out.routes.ts` exporting `/close-out` route
- [x] 8.3 `daily-close-out.service.ts` with typed methods for all 5 endpoints
- [x] 8.4 `daily-close-out.signals.ts` — root signal store (queue, counts, isLoading, lastSummary)
- [x] 8.5 `daily-close-out-page.component.ts/html` — page shell with header, progress indicator, queue list, "Close out the day" button, summary toast/dialog
- [x] 8.6 `triage-card.component.ts/html` — per-capture card with action buttons (Confirm/Discard/Reassign/Quick-discard); uses `@if`/`@for`, signals, `tailwindcss-primeui` colour utilities only
- [x] 8.7 `reassign-dialog.component.ts/html` — PrimeNG dialog with people + initiative multi-selects (Signal Forms)
- [x] 8.8 Wire route into the app router and add nav item to the side menu

## 9. Frontend — tests

- [x] 9.1 `daily-close-out-page.component.spec.ts` — renders queue, empty state, progress count
- [x] 9.2 `triage-card.component.spec.ts` — emits the right action; hides Confirm/Discard for non-Processed captures
- [x] 9.3 `daily-close-out.service.spec.ts` — verifies HTTP shapes for all 5 endpoints

## 10. Verification

- [x] 10.1 `dotnet test src/MentalMetal.slnx` — all green
- [x] 10.2 `(cd src/MentalMetal.Web/ClientApp && npx ng test --watch=false)` — all green
- [x] 10.3 Manually run `openspec validate daily-close-out --strict`
