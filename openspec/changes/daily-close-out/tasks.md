## 1. Domain — Capture aggregate

- [ ] 1.1 Add `Triaged` (bool) and `TriagedAtUtc` (DateTime?) properties to `Capture`
- [ ] 1.2 Add `ExtractionResolved` (bool) property to `Capture`; set true in existing `ConfirmExtraction()` and `DiscardExtraction()` methods
- [ ] 1.3 Add `Capture.QuickDiscard()` method (idempotent) raising `CaptureQuickDiscarded` domain event
- [ ] 1.4 Add `CaptureQuickDiscarded` domain event record
- [ ] 1.5 Unit tests: QuickDiscard on non-triaged sets flags + raises event; QuickDiscard on already-triaged is no-op; Confirm/Discard set ExtractionResolved

## 2. Domain — User aggregate

- [ ] 2.1 Add `DailyCloseOutLog` owned entity (Date DateOnly, ClosedAtUtc, ConfirmedCount, DiscardedCount, RemainingCount)
- [ ] 2.2 Add `DailyCloseOutLogs` owned-collection backing field on `User` with read-only accessor
- [ ] 2.3 Add `User.RecordDailyCloseOut(date, confirmed, discarded, remaining)` method (idempotent overwrite, validates non-negative counts)
- [ ] 2.4 Add `User.GetCloseOutLog(DateOnly date)` lookup method
- [ ] 2.5 Add `DailyCloseOutRecorded` domain event record
- [ ] 2.6 Unit tests: new-date append; same-date overwrite; negative count throws; multi-user isolation

## 3. Infrastructure — EF Core

- [ ] 3.1 Update `CaptureConfiguration` to map `Triaged`, `TriagedAtUtc`, `ExtractionResolved`
- [ ] 3.2 Add `DailyCloseOutLogConfiguration` as owned-collection of `User` (mirror `AiProviderConfig` pattern: snapshot change tracker, `PropertyAccessMode.Field`)
- [ ] 3.3 Generate EF migration `AddDailyCloseOut` adding columns + table; backfill `ExtractionResolved = (status != Processed)` for existing captures
- [ ] 3.4 Verify migration runs cleanly on the dev database

## 4. Application — vertical slices

- [ ] 4.1 Create `Application/Features/DailyCloseOut/` folder
- [ ] 4.2 `GetCloseOutQueue` query handler — returns queue items + counts, filters Triaged=false and (status in {Raw,Processing,Failed} or (Processed && !ExtractionResolved)), uses `List<T>.Contains` not `HashSet`
- [ ] 4.3 `QuickDiscardCapture` command handler — loads capture, calls `QuickDiscard`, persists, returns 200/404
- [ ] 4.4 `ReassignCapture` command handler — diffs supplied IDs vs current links, calls `Link/Unlink Person/Initiative`, validates referenced person/initiative IDs belong to user
- [ ] 4.5 `CloseOutDay` command handler — computes today's counts (confirmed/discarded/remaining), calls `User.RecordDailyCloseOut`, persists, returns log entry
- [ ] 4.6 `GetCloseOutLog` query handler — reads `DailyCloseOutLogs`, applies limit (default 30, max 90), descending date
- [ ] 4.7 DTOs in the same folder (`CloseOutQueueResponse`, `CloseOutQueueItem`, `ReassignCaptureRequest`, `CloseOutDayRequest`, `DailyCloseOutLogDto`)

## 5. Web — minimal API endpoints

- [ ] 5.1 Add `DailyCloseOutEndpoints.MapDailyCloseOutEndpoints(this IEndpointRouteBuilder)` extension
- [ ] 5.2 Wire `GET /api/daily-close-out/queue`
- [ ] 5.3 Wire `POST /api/daily-close-out/captures/{id}/quick-discard`
- [ ] 5.4 Wire `POST /api/daily-close-out/captures/{id}/reassign`
- [ ] 5.5 Wire `POST /api/daily-close-out/close`
- [ ] 5.6 Wire `GET /api/daily-close-out/log`
- [ ] 5.7 Register endpoints in `Program.cs`

## 6. Web — modify capture endpoints

- [ ] 6.1 Update `ListCaptures` query/handler to filter `Triaged = false` by default and accept `includeTriaged=true`
- [ ] 6.2 Update capture detail DTO to expose `triaged`, `triagedAtUtc`, `extractionResolved`
- [ ] 6.3 Update `GetCurrentUser` (`/api/me`) response to include `lastCloseOutAtUtc` (max of `DailyCloseOutLogs.ClosedAtUtc` or null)

## 7. Web.IntegrationTests

- [ ] 7.1 GetCloseOutQueue: mixed-status user returns expected items + counts; user isolation
- [ ] 7.2 QuickDiscard endpoint: 200 + capture excluded from queue; idempotent; 404 for foreign capture
- [ ] 7.3 Reassign endpoint: add/remove links converge; empty arrays clear; 400 for unknown person id
- [ ] 7.4 CloseOutDay endpoint: first-call records entry; second-call same date overwrites; explicit date param works
- [ ] 7.5 GetCloseOutLog endpoint: ordering, limit clamping, empty case
- [ ] 7.6 ListCaptures default-excludes triaged; `includeTriaged=true` includes them
- [ ] 7.7 GetCurrentUser includes `lastCloseOutAtUtc` after a recorded close-out

## 8. Frontend — feature module

- [ ] 8.1 Create `src/MentalMetal.Web/ClientApp/src/app/features/daily-close-out/` folder
- [ ] 8.2 `daily-close-out.routes.ts` exporting `/close-out` route
- [ ] 8.3 `daily-close-out.service.ts` with typed methods for all 5 endpoints
- [ ] 8.4 `daily-close-out.signals.ts` — root signal store (queue, counts, isLoading, lastSummary)
- [ ] 8.5 `daily-close-out-page.component.ts/html` — page shell with header, progress indicator, queue list, "Close out the day" button, summary toast/dialog
- [ ] 8.6 `triage-card.component.ts/html` — per-capture card with action buttons (Confirm/Discard/Reassign/Quick-discard); uses `@if`/`@for`, signals, `tailwindcss-primeui` colour utilities only
- [ ] 8.7 `reassign-dialog.component.ts/html` — PrimeNG dialog with people + initiative multi-selects (Signal Forms)
- [ ] 8.8 Wire route into the app router and add nav item to the side menu

## 9. Frontend — tests

- [ ] 9.1 `daily-close-out-page.component.spec.ts` — renders queue, empty state, progress count
- [ ] 9.2 `triage-card.component.spec.ts` — emits the right action; hides Confirm/Discard for non-Processed captures
- [ ] 9.3 `daily-close-out.service.spec.ts` — verifies HTTP shapes for all 5 endpoints

## 10. Verification

- [ ] 10.1 `dotnet test src/MentalMetal.slnx` — all green
- [ ] 10.2 `(cd src/MentalMetal.Web/ClientApp && npx ng test --watch=false)` — all green
- [ ] 10.3 Manually run `openspec validate daily-close-out --strict`
