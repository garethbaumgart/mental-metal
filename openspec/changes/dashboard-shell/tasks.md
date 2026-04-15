## 1. Dashboard shell layout

- [ ] 1.1 Replace the `DashboardPage` template with a responsive CSS-grid shell (`grid grid-cols-1 lg:grid-cols-2 gap-6`) that hosts the briefing as `lg:col-span-2` and reserves slots for the four sibling widgets.
- [ ] 1.2 Keep `MorningBriefingWidgetComponent` unchanged — only its parent moves. Confirm no colour regressions (PrimeNG tokens only).
- [ ] 1.3 Verify existing E2E / component tests for the dashboard route still pass; update any selector-specific assertions to account for the new grid wrapper.

## 2. Today's commitments widget

- [ ] 2.1 Create `TodaysCommitmentsWidgetComponent` (standalone, OnPush, signals) under `src/MentalMetal.Web/ClientApp/src/app/pages/dashboard/`.
- [ ] 2.2 Inject the existing `CommitmentsService`; fetch on mount, filter to `DueDate == local today || IsOverdue`, sort (overdue desc, dueDate asc), cap at 5.
- [ ] 2.3 Implement `loading | error | empty | data` render states using `@if` / `@switch`; error copy names the source ("Couldn't load commitments").
- [ ] 2.4 Add inline "Mark complete" and "Snooze" quick actions that call existing `CommitmentsService` mutations and refetch only this widget.
- [ ] 2.5 Add "View all" link to `/commitments` when more than 5 items match.
- [ ] 2.6 Add component unit tests: loading/empty/data states, 5-row cap, mark-complete refetches only this widget.

## 3. Today's 1:1s widget

- [ ] 3.1 Create `TodaysOneOnOnesWidgetComponent` (standalone, OnPush, signals).
- [ ] 3.2 Fetch via the existing one-on-ones service; filter to records whose scheduled/occurred local date equals today.
- [ ] 3.3 Implement the four local render states; each row links to the person detail route.
- [ ] 3.4 Add component unit tests for empty and populated states.

## 4. Top of queue widget

- [ ] 4.1 Create `TopOfQueueWidgetComponent` (standalone, OnPush, signals).
- [ ] 4.2 Inject `MyQueueService`; take the top 5 items in existing queue order.
- [ ] 4.3 Implement the four local render states; each row links to the queue item detail route; include a "View all" link to `/my-queue`.
- [ ] 4.4 Add component unit tests for empty, populated, and error states.

## 5. Overdue summary widget

- [ ] 5.1 Create `OverdueSummaryWidgetComponent` (standalone, OnPush, signals) spanning `lg:col-span-2`.
- [ ] 5.2 Fetch commitments, delegations, and nudges counts in parallel; track each source's status independently.
- [ ] 5.3 Render a single count bar: "N commitments overdue · M delegations stale · K unread nudges"; each count is a link.
- [ ] 5.4 When one source fails, render that segment as "— <label>" while other segments show live values; when nudges capability is unavailable, render "0 unread nudges".
- [ ] 5.5 Add component unit tests covering all-succeed, partial-failure, and all-fail scenarios.

## 6. Shell composition and isolation tests

- [ ] 6.1 Wire all four widgets into the dashboard shell template alongside the existing briefing widget.
- [ ] 6.2 Add a shell-level component test that forces the briefing endpoint to 409 and asserts sibling widgets still render their data.
- [ ] 6.3 Add a shell-level component test that forces one sibling endpoint to 500 and asserts the other four widgets render normally.
- [ ] 6.4 Add an E2E test (`tests/MentalMetal.E2E.Tests/`) that logs in and asserts all five widget slots render on `/dashboard`.

## 7. Documentation and polish

- [ ] 7.1 Verify CLAUDE.md rules: no `*ngIf` / `*ngFor` / `*ngSwitch` / `[ngClass]`, no hardcoded Tailwind colour utilities, no `dark:` prefix, PrimeNG tokens only.
- [ ] 7.2 Run `dotnet test src/MentalMetal.slnx`, `npx ng test --watch=false`, and the E2E suite locally.
- [ ] 7.3 Update `openspec/specs/daily-weekly-briefing/spec.md` via `/opsx:sync` after PR merge (sync is a separate step after archive; noted here for traceability).
