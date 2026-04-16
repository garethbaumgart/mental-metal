## Why

Daily Close-Out is meant to be a 2-minute end-of-day ritual (product brief feature #9), but today the close-out page only exposes Reassign and Quick-discard on each triage card. To fire AI extraction on a Raw capture the user has to click into the capture detail page and press "Process with AI" — a context switch that breaks the ritual. There is no batch action either, so triaging an end-of-day backlog of Raw captures means clicking in and out of N detail pages. This change adds the two missing close-out affordances so a user can process captures without leaving the page, including processing all Raw captures in a single tap.

## What Changes

- Add a per-row **Process** action to every Raw triage card on `/close-out`, rendered alongside (not replacing) the existing Reassign / Quick-discard actions. The button calls the existing `POST /api/captures/{id}/process` endpoint and the card's existing Processing status badge reflects the in-flight state.
- Add a top-of-page **Process all raw** button adjacent to the existing "Close out the day" button. When clicked, the frontend iterates the Raw captures currently visible in the triage queue and invokes `POST /api/captures/{id}/process` for each.
- While the bulk action is in flight, the Process-all button SHALL be disabled and each targeted card SHALL show its existing Processing badge.
- Individual `POST /api/captures/{id}/process` failures SHALL NOT abort the bulk flow; the UI continues through the remaining captures and reports a final summary of the form "Processed N of M · K failed".
- If the user has no AI provider configured, the bulk action SHALL surface the same `ai_provider_not_configured` error UX already used by the dashboard briefing widget (a message linking the user to `/settings`), rather than spamming per-card failures.

**Non-goals**:
- No backend API changes — this is a pure frontend composition over the existing `POST /api/captures/{id}/process` endpoint.
- No new aggregates, no new domain events, no new bulk-process backend handler.
- No re-triage of already-Processed captures (bulk action targets Raw only).
- No UX for audio captures — this is a `capture-text` triage ritual.
- No changes to the existing Confirm, Discard, Reassign, Quick-discard, or "Close out the day" flows.

## Capabilities

### New Capabilities

<!-- None. -->

### Modified Capabilities

- `daily-close-out`: Extend the Triage UI page requirement to include a per-row Process action on Raw cards, and add a new requirement for the bulk "Process all raw" action (including in-flight disabled state, per-card progress via the existing Processing badge, resilience to per-capture failures, final summary counts, and the provider-not-configured surface).

## Impact

- Tier: **Tier 3 enhancement** to an already-shipped Tier 3 capability (`daily-close-out`).
- Spec dependencies (already shipped): `capture-ai-extraction` (owns the canonical per-capture process semantics — this change composes over its `POST /api/captures/{id}/process` endpoint and does not duplicate its scenarios), `daily-close-out`, `ai-provider-abstraction` (owns the `ai_provider_not_configured` error surface).
- Affected code:
  - Frontend: `src/MentalMetal.Web/ClientApp/src/app/features/daily-close-out/` — `triage-card` component (add Process button), close-out page component (add Process-all button, orchestrate the loop, render summary toast/dialog), close-out service (wrap `POST /api/captures/{id}/process`).
  - No backend changes. Program.cs endpoint map is untouched. No EF migrations.
- Affected aggregates: none modified. `Capture` aggregate behaviour is reused as-is via the existing `BeginProcessing()` path.
- Telemetry/logging: none new.
