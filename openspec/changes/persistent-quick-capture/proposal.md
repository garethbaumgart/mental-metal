## Why

The product brief defines Quick Capture as "a persistent, low-friction input... No forms, no categorization required. Just text in, AI figures out the rest." Today's implementation falls short on both axes: Quick Capture is only reachable from the `/capture` route (not persistent), and the dialog requires selecting a Type (Quick Note / Transcript / Meeting Notes) — a form, and categorization. The friction cost shows up every time a user wants to dump a thought from any other screen — they have to break context, navigate away, and then classify the thought before the AI ever sees it.

This change makes Quick Capture match the brief: reachable from every authenticated page, and typing-plus-Enter fast on the happy path.

## What Changes

- Add a global keyboard shortcut (Cmd+K on macOS, Ctrl+K on other platforms) that opens the Quick Capture dialog from any authenticated page.
- Add a persistent floating action button (FAB) visible on every authenticated page that opens the Quick Capture dialog.
- Mount the Quick Capture dialog at the authenticated app shell level (not only inside `CapturesListComponent`), so it is available regardless of current route.
- Remove the required Type selection from the dialog happy path. New captures default to `QuickNote` (applied client-side; the existing POST `/api/captures` contract is unchanged).
- Move Type selection, Title, and Source into a collapsible "Advanced" section that is collapsed by default.
- Support submit-on-Enter (Cmd/Ctrl+Enter inside the textarea) so the happy path is: open (shortcut or FAB) → type → Enter → done.

## Capabilities

### New Capabilities

_None._ This change is purely a UI/interaction refinement on an existing capability.

### Modified Capabilities

- `capture-text`: Replace the "Quick capture input" UI requirement with a persistent, globally-available Quick Capture experience whose happy path has no required categorization. Adds requirements for the global keyboard shortcut, the persistent FAB, and the Advanced (collapsed by default) section for Type / Title / Source.

## Impact

- **Tier:** Tier 2 delta. Dependency: `capture-text` (already shipped and merged). Optional related spec: `capture-ai-extraction` — not modified by this change (AI pipeline is untouched; a `QuickNote` default flows through the same extraction path already supported).
- **Aggregates affected:** None. The `Capture` aggregate and its invariants do not change. `CaptureType` enum values and semantics are unchanged.
- **Backend:** No API shape changes. POST `/api/captures` continues to accept `type` as a required field on the wire; the client simply always sends `"QuickNote"` unless the user opens the Advanced section and chooses otherwise.
- **Frontend:**
  - `QuickCaptureDialogComponent` — default `selectedType` to `QuickNote`, wrap Type/Title/Source in a collapsible panel (PrimeNG `p-panel` with `toggleable`, `collapsed` by default), add Cmd/Ctrl+Enter submit handler.
  - Authenticated app shell / layout — mount `<app-quick-capture-dialog>` once at the shell level (move out of `CapturesListComponent`), expose a shared signal-backed open state.
  - New `GlobalCaptureShortcutDirective` (or equivalent host listener in the shell) — listens for Cmd/Ctrl+K and opens the dialog. Must not fire when the user is inside another modal or an editable field that owns Cmd/Ctrl+K (none today).
  - New floating-action-button element in the authenticated shell — PrimeNG `p-button` with `rounded`, positioned fixed bottom-right, `bg-primary`.
- **Tests:** Frontend unit tests for the dialog default-type behaviour and Advanced toggle; Playwright E2E test covering: from a non-`/capture` route, press Cmd+K → dialog opens → type content → Enter → capture created with `type=QuickNote`.

## Non-goals

- **No changes to the AI extraction pipeline** (`capture-ai-extraction`). The default `QuickNote` type flows through existing extraction logic unchanged.
- **No changes to the backend API shape.** POST `/api/captures`, GET `/api/captures`, and all link/unlink/update endpoints are untouched. The client-side default keeps the wire contract stable.
- **No changes to the captures list page or detail view.** The `/capture` route, list filters, triage, and detail editing all remain as-is.
- **No changes to audio capture** (`capture-audio`, Tier 3). The FAB and shortcut open the text dialog only.
- **No re-design of the sidebar.** The existing sidebar Capture link stays; this change is additive (global access), not a replacement.
