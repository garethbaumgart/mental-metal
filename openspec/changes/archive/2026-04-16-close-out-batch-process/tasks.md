## 1. Frontend — close-out service

- [ ] 1.1 Add a `processCapture(id)` method to the close-out feature service that wraps `POST /api/captures/{id}/process` and returns a typed result (success | known-error discriminants, including `ai_provider_not_configured`).
- [ ] 1.2 Add a `processAllRaw(rawCaptureIds)` orchestration method that fans out `processCapture` calls with parallelism 3, uses `Promise.allSettled`-style semantics, short-circuits on the first `ai_provider_not_configured` response, and returns `{ attempted, succeeded, failed, providerNotConfigured }`.

## 2. Frontend — triage card component

- [ ] 2.1 Add a Process button to the triage-card component rendered only when the card's status is `Raw` (use `@if`, PrimeNG button, no `*ngIf` / no hardcoded Tailwind colours).
- [ ] 2.2 Wire the Process button to call the service's `processCapture` and emit a `processed` output the parent page listens to for queue refresh.
- [ ] 2.3 Ensure Reassign and Quick-discard remain rendered on the same card alongside Process.

## 3. Frontend — close-out page

- [ ] 3.1 Add a "Process all raw" PrimeNG button adjacent to the existing "Close out the day" button, driven by a computed signal for "raw captures present" and an in-flight signal for disabled-while-running.
- [ ] 3.2 On click, snapshot the current Raw capture IDs, call `processAllRaw`, then refresh the queue.
- [ ] 3.3 On completion, show a PrimeNG success toast "Processed N of M · K failed" using the returned counts.
- [ ] 3.4 On `providerNotConfigured`, do NOT show the summary toast; instead surface the same provider-not-configured message component used by the dashboard briefing widget (link to `/settings`).

## 4. Frontend — tests

- [ ] 4.1 Add unit tests for `processAllRaw`: all-succeed (5 of 5), mixed failures (8 of 10), provider-not-configured short-circuit on first call.
- [ ] 4.2 Add a component test for triage-card: Process button renders only when status is Raw; not rendered for Processing / Failed / Processed cards.
- [ ] 4.3 Add a component test for the close-out page: button disabled while in-flight, disabled when no Raw captures present, summary toast rendered with correct counts.

## 5. E2E

- [ ] 5.1 Add a Playwright scenario that seeds two Raw captures, clicks "Process all raw", and asserts both cards transition to the Processing badge and the summary toast shows "Processed 2 of 2 · 0 failed".

## 6. Verification

- [ ] 6.1 Run `(cd src/MentalMetal.Web/ClientApp && npx ng test --watch=false)` — all frontend unit tests pass.
- [ ] 6.2 Run `dotnet test src/MentalMetal.slnx` — confirm no backend tests regress (no backend changes expected).
- [ ] 6.3 Run the close-out E2E scenario against the dev-stack profile.
- [ ] 6.4 Manual check: no new `*ngIf` / `*ngFor` / hardcoded Tailwind colour utilities / `dark:` prefixes introduced (grep the diff).
