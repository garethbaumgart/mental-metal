## Context

`capture-text` shipped in Tier 2 with a working Quick Capture dialog, but the dialog is mounted inside `CapturesListComponent` (only live on `/capture`) and forces Type selection before submit. The product brief frames Quick Capture as the lowest-friction on-ramp to the whole product: "just text in, AI figures out the rest." Every extra click, every mandatory dropdown, is friction that causes users to not capture — which breaks the rest of the system (no captures → no AI extraction → no commitments → no briefing value).

The sidebar was just refactored to six verbs; Capture remains as a navigation entry, but sidebar navigation is still a context switch. We want capture to be truly ambient.

## Dependencies

- `capture-text` (Tier 2, shipped) — this change is a delta on that spec.
- `capture-ai-extraction` (Tier 2, shipped) — **not modified**. The existing pipeline already handles `QuickNote` captures; defaulting the dialog to `QuickNote` simply funnels more captures through that same path.
- No new backend dependencies. No new aggregates, no new domain events.

## Goals / Non-Goals

**Goals:**
- Quick Capture opens from any authenticated page in <300ms via (a) Cmd/Ctrl+K and (b) a persistent FAB.
- Happy path has zero required form fields beyond the content textarea: open → type → Enter → done.
- Advanced users can still set Type, Title, and Source via an Advanced expander.
- Backend API shape is untouched; client continues to send a valid `type` in every POST.

**Non-Goals:**
- Changing the AI extraction pipeline (see `capture-ai-extraction`).
- Changing the backend `POST /api/captures` contract (type still required on the wire).
- Changing the captures list, detail view, or triage flows.
- Adding audio capture to the FAB (Tier 3, separate spec).
- Making the shortcut configurable — Cmd/Ctrl+K is hard-coded for v1.

## Decisions

### D1. Default capture type is applied client-side, not server-side

**Decision:** The frontend sends `type: "QuickNote"` by default. The backend `POST /api/captures` contract is unchanged.

**Alternatives considered:**
- *Make `type` optional server-side and default to `QuickNote` in the handler.* Rejected: widens the API surface, requires a migration of clients and e2e tests, and couples UI-level friction reduction to domain-level defaults. The domain `CaptureType` remains a meaningful classification; we just stop forcing the user to pick one.

**Rationale:** Preserves a stable, explicit API. Keeps the "what UI thinks" vs. "what domain requires" boundary clean. If we later add audio or a different client (CLI, mobile), each client can pick its own default.

### D2. Mount the dialog at the authenticated shell level

**Decision:** Move `<app-quick-capture-dialog>` from `CapturesListComponent` to the authenticated layout component (the shell that wraps all authenticated routes). Its `visible` signal is owned by a small `QuickCaptureUiService` (or equivalent) that both the global shortcut directive and the FAB call into.

**Alternatives considered:**
- *Leave the dialog in each page that wants it.* Rejected: duplicates state and bindings across routes; inconsistent keyboard behaviour.
- *Use a PrimeNG `DialogService` with dynamic component creation.* Rejected: heavier than needed; loses the ability to reference a single component for testing and for wiring into the FAB.

### D3. Keyboard shortcut: Cmd+K / Ctrl+K

**Decision:** Listen on `window` (or the authenticated shell host) for `keydown` where `(event.metaKey && isMac) || (event.ctrlKey && !isMac)` and `event.key === 'k'`. `preventDefault()` unconditionally inside the authenticated shell. Skip when an existing modal is open (check a shared "modal open" signal or rely on PrimeNG's focus trap by only firing on shell-level hosts).

**Rationale:** Cmd/Ctrl+K is the industry-standard "open command palette / quick action" shortcut (Linear, GitHub, VS Code, Slack). Users already expect it. Overriding the browser default behaviour (focus address bar with `/` — not K) is acceptable within the app.

**Alternatives considered:**
- *Cmd+Shift+K:* avoids any possible browser conflict but is less discoverable.
- *`c` (single key):* Gmail-style but hostile to any future rich text surface that wants to let the letter `c` reach it.

### D4. FAB: fixed bottom-right, themed via PrimeNG tokens

**Decision:** A `p-button` with `rounded`, `icon="pi pi-plus"`, `severity="primary"`, positioned via Tailwind utilities (`fixed bottom-6 right-6 z-40`). Colours come from `bg-primary` (PrimeNG token bridge), never hardcoded.

**Accessibility:** `aria-label="Quick capture (Cmd+K)"`, focusable, visible focus ring.

### D5. Advanced section collapsed by default

**Decision:** Wrap Type + Title + Source in a PrimeNG `p-panel` with `toggleable`, `[collapsed]="true"`. The panel header reads "Advanced". Type, Title, and Source remain optional from the user's perspective; only the textarea is visually required.

### D6. Enter key submits; Shift+Enter inserts newline

**Decision:** On the textarea, bind `(keydown.enter)` with `$event.shiftKey === false` submitting, otherwise allow default newline insertion. Also accept Cmd/Ctrl+Enter globally inside the dialog for users who want newlines in content and still submit via shortcut.

**Alternatives considered:**
- *Cmd/Ctrl+Enter only:* too hidden for the "just type and hit Enter" use case the brief describes.

## Risks / Trade-offs

- **[Risk] Cmd/Ctrl+K collision with a future embedded third-party component (e.g., an embedded Monaco editor).** → Mitigation: the shortcut handler lives at the shell level and can be gated by a signal the embedded component flips while focused.
- **[Risk] Users submit an "oops" capture by pressing Enter too fast.** → Mitigation: `QuickDiscard` already exists (`capture-text` "Capture triage flag"); the cost of an accidental capture is one click to discard in the list.
- **[Trade-off] FAB adds a persistent visual element on every authenticated page.** → Acceptable: the product positions Quick Capture as *the* primary interaction; making it visible is aligned with the brief.
- **[Risk] Defaulting to `QuickNote` means captures that are really transcripts or meeting notes get the wrong type until the user expands Advanced.** → Mitigation: AI extraction is type-agnostic in practice; misclassification here doesn't change extraction behaviour. Users who care can still expand Advanced.

## Migration Plan

No data migration. No API migration. Frontend-only change. Deploy ships the relocated dialog, new shortcut directive, and FAB in a single release.

Rollback: revert the Angular changes; backend is unchanged.

## Open Questions

- Should the FAB be hideable via user preference? (Proposed: no for v1; revisit after feedback.)
- Should we show a small keyboard hint ("Cmd+K") as a badge near the FAB on desktop? (Proposed: yes, via `[pTooltip]` on hover; free via PrimeNG.)
- Should the dialog close on successful submit or stay open for rapid-fire capturing? (Proposed: close on submit for v1 — matches current behaviour; "rapid-fire mode" can be a later enhancement.)
