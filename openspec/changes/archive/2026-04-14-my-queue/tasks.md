## 1. Application layer — MyQueue feature slice

- [x] 1.1 Create `src/MentalMetal.Application/Features/MyQueue/` folder with `MyQueueOptions.cs` binding `MyQueue:CommitmentDueSoonDays`, `MyQueue:DelegationStalenessDays`, `MyQueue:CaptureStalenessDays` (defaults 7, 7, 3)
- [x] 1.2 Register `MyQueueOptions` via `services.Configure<MyQueueOptions>(config.GetSection("MyQueue"))` in the Application DI extension (alongside the other feature-option registrations)
- [x] 1.3 Define DTOs in `MyQueue/Contracts/`: `QueueItemResponse`, `QueueCountsResponse`, `QueueFiltersResponse`, `MyQueueResponse`, `QueueItemType` enum (`Commitment`, `Delegation`, `Capture`), `QueueScope` enum (`All`, `Overdue`, `Today`, `ThisWeek`)
- [x] 1.4 Implement `QueuePrioritizationService` with pure `ScoreCommitment`, `ScoreDelegation`, `ScoreCapture` methods per design/D2, taking a `DateTimeOffset now` and `MyQueueOptions` — no I/O
- [x] 1.5 Implement `GetMyQueueHandler.cs` (record command/query + handler) that: (a) loads user's qualifying commitments, delegations, captures via existing repositories, (b) applies scope/itemType/personId/initiativeId filters, (c) projects to `QueueItemResponse`, (d) computes `suggestDelegate` using in-memory `HashSet<Guid>` of PersonIds from user's non-cancelled delegations, (e) orders by `priorityScore desc` with tiebreakers, (f) computes `counts`
- [x] 1.6 Resolve person/initiative display names via existing repos in a single batched lookup (collect Ids, one round-trip each); tolerate missing names (fall back to null)
- [x] 1.7 Ensure all EF LINQ predicates use `List<T>.Contains` (not `HashSet.Contains`) and `.ToLower()` (not `.ToLowerInvariant()`); cap per-type candidate fetch at 200 rows

## 2. Web layer — endpoint

- [x] 2.1 Add `src/MentalMetal.Web/Endpoints/MyQueueEndpoints.cs` with `MapMyQueueEndpoints(this IEndpointRouteBuilder)` mapping `GET /api/my-queue` with required auth
- [x] 2.2 Parse query params: `scope` (enum, default `All`), repeated `itemType` (enum list, default all), `personId` (Guid?), `initiativeId` (Guid?); return 400 on invalid enum values
- [x] 2.3 Dispatch to `GetMyQueueHandler`, return `Results.Ok(MyQueueResponse)`
- [x] 2.4 Wire `app.MapMyQueueEndpoints()` into `Program.cs` alongside the other feature endpoint groups

## 3. Backend tests

- [x] 3.1 Add `tests/MentalMetal.Application.Tests/Features/MyQueue/QueuePrioritizationServiceTests.cs` covering: overdue commitment > due-soon, urgent delegation > low, blocked delegation bump, failed capture bump, no-due-date commitment, tie-breakers, bounded overdue contribution
- [x] 3.2 Add `GetMyQueueHandlerTests.cs` covering: empty queue, mixed queue, user isolation, filter by scope (overdue/today/thisWeek/all), filter by itemType (single + multiple + invalid), filter by personId, filter by initiativeId, combined filters, counts correctness, `suggestDelegate` heuristic (all 4 scenarios from spec), candidate fetch cap
- [x] 3.3 Add `tests/MentalMetal.Web.IntegrationTests/MyQueueEndpointTests.cs` covering: 200 happy path, 400 on invalid scope, 400 on invalid itemType, response shape matches spec, auth required, cross-user isolation
- [x] 3.4 Run `dotnet test src/MentalMetal.slnx` and confirm green

## 4. Frontend feature slice

- [x] 4.1 Create `src/MentalMetal.Web/ClientApp/src/app/features/my-queue/my-queue.service.ts` using `inject(HttpClient)`, holding `readonly response = signal<MyQueueResponse | null>(null)` and `readonly loading = signal(false)`, exposing a `load(filters)` method
- [x] 4.2 Create `my-queue.page.ts` standalone component that injects the service, holds filter signals (`scope`, `itemType[]`, `personId`, `initiativeId`), triggers `load()` on filter change, and renders the list
- [x] 4.3 Create `my-queue-item.component.ts` standalone component rendering one queue item with type-aware badge, title, person/initiative name, due or days-since-captured, priority score, and the inline "Delegate this" action when `suggestDelegate` is true
- [x] 4.4 Use only `@if`/`@for`/`@switch` for control flow. Use only PrimeNG / `tailwindcss-primeui` tokens for colours (`bg-primary`, `text-muted-color`, `bg-surface-0`, PrimeNG `Tag`/`Badge` severities). No `*ngIf`/`*ngFor`, no `dark:`, no hardcoded Tailwind colour utilities
- [x] 4.5 Add lazy route at `/my-queue` in the app routes. Add a "My Queue" entry to the primary navigation shell
- [x] 4.6 On "Delegate this" click, `router.navigate` to the delegation create route with query params `description`, `personId`, `initiativeId?`, `sourceCommitmentId`
- [x] 4.7 Verify the existing delegation create page reads those query params on init; if it does not already, extend it to pre-fill from those params (minimal Boy-Scout edit, scoped to lines being modified)

## 5. Frontend tests

- [x] 5.1 Add `my-queue.service.spec.ts` covering success, filter-param serialisation (especially repeated `itemType`), and error
- [x] 5.2 Add `my-queue.page.spec.ts` covering: renders items, scope chip change triggers reload, empty state, loading state, delegate-this navigation
- [x] 5.3 Run `(cd src/MentalMetal.Web/ClientApp && npx ng test --watch=false)` and confirm green

## 6. Wiring & verification

- [x] 6.1 Add minimal appsettings documentation (comment block in appsettings.Development.json if present, otherwise rely on defaults) — do not add secrets
- [x] 6.2 Run `dotnet build src/MentalMetal.slnx` and `dotnet test src/MentalMetal.slnx`
- [x] 6.3 Run the Angular test suite
- [x] 6.4 Manual smoke: start dev stack, seed a commitment, delegation, and stale capture; hit `/api/my-queue` and the `/my-queue` page to verify round-trip
- [x] 6.5 Run `openspec verify --change my-queue` if available; otherwise spot-check that every spec scenario maps to at least one test
