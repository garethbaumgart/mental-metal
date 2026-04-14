## Context

Mental Metal has shipped all the building blocks: People, Initiatives, Commitments, Delegations, Captures, Living Briefs, My Queue, People Lens (OneOnOne / Observation / Goal). What users still do manually every morning is open six tabs to assemble context for the day. This spec delivers the integrating layer — a `BriefingService` that reads across aggregates, assembles deterministic facts, and asks the user's configured LLM to turn those facts into a short, readable briefing.

Three briefing shapes cover the daily workflow:

- **Morning** — "what to focus on today"
- **Weekly** — "what's coming this week and what's slipping"
- **1:1 Prep** — "context you need before talking to this person"

All data sources already exist via existing repositories. This change adds one read-model aggregate (`Briefing`), one Application slice, one Infrastructure repository + migration, API endpoints, and a thin frontend layer.

## Dependencies

- `user-auth-tenancy` — auth + `IUserContext` used for scoping.
- `ai-provider-abstraction` — `IAiCompletionService` + the user's `AiProviderConfig` for LLM calls.
- `person-management` — Person repository, `IPersonRepository`.
- `initiative-management` — Initiative repository, milestones.
- `commitment-tracking` — Commitment repository, due dates, direction.
- `delegation-tracking` — Delegation repository, status, last-followed-up-at.
- `people-lens` — OneOnOne, Observation, Goal repositories.
- `initiative-living-brief` — LivingBrief repository.
- `my-queue` — reuse queue scoring for "top items today" section (not a hard dependency — morning briefing computes its own top list using the same primitives but does not call the queue endpoint directly).

## Goals / Non-Goals

**Goals:**
- One on-demand API per briefing type that assembles facts deterministically and produces a short markdown narrative.
- Persist each generated briefing so users can scroll back through their week.
- Deterministic testability — services take `TimeProvider`; facts assembly is unit-testable without the LLM; a `FakeAiCompletionService` in tests returns canned content.
- Surface the morning briefing on the dashboard with a "last generated at" timestamp and a regenerate button.

**Non-Goals:**
- Scheduled generation or email delivery — deferred to `nudges-rhythms`.
- Streaming token-by-token UI — the endpoint returns the completed briefing.
- Editing stored briefings — read-only once generated.
- Cross-user comparison, team roll-ups, manager-of-manager views.
- Fancy "diff since yesterday" or delta summarisation — future enhancement.

## Decisions

### 1. Briefing as a thin read-model aggregate, not rich domain

**Decision:** `Briefing` is a lightweight user-scoped entity — Id, UserId, Type (enum), ScopeKey (string), GeneratedAtUtc, MarkdownBody, PromptFacts (jsonb — the deterministic inputs used), Model, InputTokens, OutputTokens. It has minimal behaviour (factory creation, nothing mutable after creation).

**Rationale:** Briefings are artefacts of a computation — not aggregates with long-lived invariants. Treating them as rich aggregates adds ceremony without value. This matches the pattern already used for `ChatMessage` append-only records.

**Alternative considered:** Generate-and-throw-away (no persistence). Rejected because history has real value — "what did I tell myself to focus on Monday morning?" — and persisting lets us avoid calling the LLM on every page load.

### 2. ScopeKey scheme

**Decision:** `ScopeKey` is a deterministic string per (type, date-or-person):
- Morning: `morning:{yyyy-MM-dd}` in the user's local date (derived from `User.Preferences.TimeZone`, or UTC if unset). The local *hour* in the same time zone is also used to apply the `MorningBriefingHour` rollback.
- Weekly: `weekly:{ISO-year}-W{ISO-week}` (e.g., `weekly:2026-W16`).
- 1:1 prep: `oneonone:{personId:N}` — one current prep per person; regenerating replaces the row semantically by appending a new row with the same scopeKey but newer `GeneratedAtUtc`; the "current" prep is simply max-generatedAt per scopeKey.

**Rationale:** Lets us look up "today's morning briefing" cheaply with `WHERE UserId=X AND Type=Morning AND ScopeKey='morning:2026-04-14'` and return it if recent (< the per-type staleness option, e.g. `MorningBriefingStaleHours`) without hitting the LLM.

### 3. Facts assembly is deterministic and LLM-free

**Decision:** Each briefing has two phases:
1. **Facts phase** — pure C# query layer assembles a strongly-typed `MorningBriefingFacts` / `WeeklyBriefingFacts` / `OneOnOnePrepFacts` DTO containing all names/dates/counts/descriptions. No LLM.
2. **Synthesis phase** — a small prompt formats the facts as JSON and asks the LLM to write a short markdown narrative (~200–400 words for morning, ~400–700 for weekly, ~300–500 for 1:1 prep), a one-sentence "focus" line, and 3–5 bullet talking points (1:1 prep only).

The stored `PromptFacts` column retains the exact facts JSON so we can re-run or audit.

**Rationale:** Deterministic inputs + small, structured prompts keep cost and latency bounded and eliminate a large class of hallucination (the LLM never invents names or dates — it only narrates supplied facts). Same pattern as `BriefMaintenanceService`.

### 4. "Top items today" reuses queue-style scoring primitives, not the queue API

**Decision:** Morning briefing computes its own "top 5 commitments / delegations due or overdue" list inline using the same EF queries as `my-queue` but scoped to due-today-or-overdue. It does not call `GET /api/my-queue` (no inter-service HTTP hops inside the server).

**Rationale:** Keeps the briefing service self-contained; the two features will drift independently and the queue API's scoring is for a different UX (attention sorting) whereas the briefing wants "what must I do today".

### 5. Cache-or-regenerate logic

**Decision:** `POST /api/briefings/morning` semantics:
- If a Briefing with `(UserId, Morning, morning:{today})` exists AND `GeneratedAtUtc` is within the last `MorningBriefingStaleHours` (default 12h), return 200 with that one.
- Otherwise, generate a new one, persist it, return 201.
- `?force=true` always regenerates.

Same pattern for weekly (`WeeklyBriefingStaleHours`, default 12h) and 1:1 prep (`OneOnOnePrepStaleHours`, default 12h).

### 6. Options + `TimeProvider`

**Decision:** New `BriefingOptions` record with `[Range]` attributes:
- `MorningBriefingHour` (0–23, default 5) — hour before which "today's" briefing is still considered yesterday's (in the user's local time zone).
- `MorningBriefingStaleHours` (1–72, default 12).
- `WeeklyBriefingStaleHours` (1–72, default 12).
- `OneOnOnePrepStaleHours` (1–72, default 12).
- `MaxBriefingTokens` (200–4000, default 1500).
- `TopItemsPerSection` (1–20, default 5).

Wired via `AddOptions<BriefingOptions>().Bind(...).ValidateDataAnnotations().ValidateOnStart()`.

`BriefingService` depends on `TimeProvider` (injected, `TimeProvider.System` in prod). All "now" reads go through it.

### 7. AI prompting strategy

**Decision:** A dedicated `BriefingPromptBuilder` produces:
- A short system prompt ("You are the user's engineering-management briefing assistant. Be concise. Use markdown. Do not invent facts not present in the input JSON.").
- A user prompt with the facts JSON and instructions per briefing type. For 1:1 prep, we ask for a specific markdown template (Context / Open items / Talking points / Recent observations).

Max tokens: `MaxBriefingTokens`. Temperature: 0.3 (low — factual). Returns `AiCompletionResult`; we store `Content` as `MarkdownBody` and `InputTokens`/`OutputTokens`/`Model` for cost accounting.

### 8. Frontend surface

**Decision:**
- A signal-based `BriefingService` (Angular) with `loadMorning()`, `loadWeekly(force?)`, `loadOneOnOnePrep(personId, force?)`, `recent()`.
- Dashboard already exists (home page); add a `MorningBriefingWidgetComponent` that lazily calls `loadMorning()` on mount, renders the markdown via an existing/new markdown pipe, shows generated-at + regenerate button.
- New route `/briefings/weekly` with `WeeklyBriefingPageComponent`.
- On the person detail page, a PrimeNG button "Generate 1:1 prep" opens a dialog/drawer with the 1:1 prep content.

All components use standalone + signals + `@if`/`@for`/`@switch`, PrimeNG + `tailwindcss-primeui` tokens only. No `*ngIf`, no hardcoded Tailwind colours, no `dark:` prefix.

### 9. Markdown rendering

**Decision:** Render via a sanitising markdown pipe. If one does not already exist in the codebase, use `marked` + `DOMPurify` (both are well-vetted, small) or keep a minimal inline renderer. Detailed choice in tasks.

## Risks / Trade-offs

- **[LLM cost per morning] →** Mitigation: cache per day (ScopeKey) + `WeeklyBriefingStaleHours` prevents double-calls on refresh; facts phase is the expensive query but it's a single round-trip to Postgres.
- **[LLM hallucination inventing names / counts] →** Mitigation: prompt explicitly forbids invention; facts are supplied as JSON; system prompt says "do not invent"; any rendered fact can be cross-checked against `PromptFacts`.
- **[Time-zone drift — "today" in user's zone vs server]** → Mitigation: read zone from `User.Preferences.TimeZone` (existing field). Fallback: UTC. All date math in the service goes through a helper method `userLocalDate(now)`.
- **[Large facts payload] →** Mitigation: `TopItemsPerSection` cap; never embed raw capture text, only titles; never embed full observation bodies for 1:1 prep, only summaries or the first 240 chars.
- **[Race on repeated rapid POSTs] →** Mitigation: the persisted row uses a unique index on `(UserId, Type, ScopeKey, GeneratedAtUtc)`; duplicate-insert on identical timestamp is extremely unlikely (microsecond resolution) — if it happens, retry with new timestamp. Not a correctness issue.
- **[AI provider not configured] →** If user has no `AiProviderConfig`, endpoint returns HTTP 409 with a clear error message; frontend surfaces "Set up your AI provider to generate briefings".

## Migration Plan

- New EF migration: `AddBriefings` — creates `Briefings` table with index on `(UserId, Type, ScopeKey, GeneratedAtUtc)`.
- Rollback: `dotnet ef migrations remove` on the PR branch only; after merge, a forward `RemoveBriefings` migration would be required.
- Feature is strictly additive — no changes to existing tables or read models.

## Open Questions

- Should we expose a "delete this briefing" endpoint? Not in this change — read-only history is fine; we can add later if storage becomes a concern.
- Should weekly briefing be Monday-anchored or rolling 7 days? Decision: ISO-week (Monday–Sunday) for predictability; scope-key uses ISO week.
