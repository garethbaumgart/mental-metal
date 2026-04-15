## 1. Frontend: shell-level dialog mounting

- [ ] 1.1 Create `QuickCaptureUiService` (or equivalent) exposing a `readonly visible = signal(false)` and an `open()` method; inject where needed.
- [ ] 1.2 Remove `<app-quick-capture-dialog>` from `CapturesListComponent` and remove the local `dialogVisible` state.
- [ ] 1.3 Mount `<app-quick-capture-dialog>` once in the authenticated app shell (layout component that wraps authenticated routes), binding `visible` to the UI service signal.
- [ ] 1.4 Wire the existing "New Capture" button on `/capture` to call `QuickCaptureUiService.open()` instead of owning local state.
- [ ] 1.5 Subscribe to `(created)` on the shell-level dialog and broadcast via a shared `capturesCreated$` signal/subject so `CapturesListComponent` still updates its list.

## 2. Frontend: dialog UX changes

- [ ] 2.1 Default `selectedType` to `'QuickNote'` in `QuickCaptureDialogComponent` and remove the "select type" placeholder requirement from `isValid()`.
- [ ] 2.2 Wrap the Type, Title, and Source fields in a PrimeNG `p-panel` with `toggleable` and `[collapsed]="true"`, headered "Advanced".
- [ ] 2.3 Add `(keydown.enter)` handler on the content textarea: if `!event.shiftKey` and content non-empty, submit; otherwise allow default.
- [ ] 2.4 Add dialog-level `(keydown)` handler for Cmd/Ctrl+Enter to submit.
- [ ] 2.5 Autofocus the content textarea when the dialog opens.
- [ ] 2.6 Ensure PrimeNG-only theming (no hardcoded `bg-gray-*` / `text-violet-*` / `dark:` — follow CLAUDE.md banned patterns).

## 3. Frontend: global keyboard shortcut

- [ ] 3.1 Add a `GlobalCaptureShortcutDirective` (or host listener on the authenticated shell) that listens for `keydown` on `window`.
- [ ] 3.2 Detect platform via `navigator.platform` / `navigator.userAgent`: macOS uses `event.metaKey`, others use `event.ctrlKey`; match `event.key === 'k'`.
- [ ] 3.3 On match, `preventDefault()` and call `QuickCaptureUiService.open()`; no-op if dialog already visible.
- [ ] 3.4 Ensure the listener is only wired inside the authenticated shell so login/signup pages are unaffected.

## 4. Frontend: persistent FAB

- [ ] 4.1 Add a FAB `p-button` (rounded, `icon="pi pi-plus"`, `severity="primary"`) in the authenticated shell template, positioned `fixed bottom-6 right-6 z-40`.
- [ ] 4.2 Wire `(onClick)` to `QuickCaptureUiService.open()`.
- [ ] 4.3 Set `aria-label="Quick capture (Cmd+K)"` (dynamically adapted for Ctrl on non-mac), and attach `pTooltip` with the same hint on desktop breakpoints.
- [ ] 4.4 Confirm FAB theming uses only PrimeNG tokens / Tailwind layout utilities per CLAUDE.md.

## 5. Frontend: tests

- [ ] 5.1 Unit test `QuickCaptureDialogComponent`: default type is `QuickNote`, Advanced is collapsed by default, submit sends `type: "QuickNote"` when user doesn't touch Advanced.
- [ ] 5.2 Unit test: Enter submits; Shift+Enter does not submit; empty content disables submit.
- [ ] 5.3 Unit test `GlobalCaptureShortcutDirective`: Cmd+K on mac and Ctrl+K elsewhere call `open()`; shortcut is a no-op when dialog already visible.
- [ ] 5.4 Unit test `QuickCaptureUiService`: `open()` sets `visible` to true; idempotent when already true.

## 6. E2E tests

- [ ] 6.1 Playwright: from `/people` (a non-capture route), press Ctrl+K → dialog opens → type "remember to follow up" → press Enter → dialog closes, capture created with `type=QuickNote`.
- [ ] 6.2 Playwright: click FAB on `/initiative` → dialog opens → expand Advanced → change type to Transcript → submit → capture is created with type Transcript.
- [ ] 6.3 Playwright: on login page, Ctrl+K does not open the dialog.

## 7. Verification

- [ ] 7.1 Run `dotnet test src/MentalMetal.slnx` — confirm no backend changes broke anything (should be green with zero backend edits).
- [ ] 7.2 Run `(cd src/MentalMetal.Web/ClientApp && npx ng test --watch=false)` — all frontend tests pass.
- [ ] 7.3 Run the E2E suite per CLAUDE.md dev commands — new scenarios pass.
- [ ] 7.4 Manual smoke: open each authenticated route, verify FAB is visible and Cmd/Ctrl+K opens the dialog with content textarea focused.
