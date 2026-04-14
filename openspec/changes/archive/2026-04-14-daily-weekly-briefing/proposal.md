# Daily / Weekly Briefing

## Why

Engineering managers spend the first 15–30 minutes of every morning mentally re-loading context: what's on today's calendar, who's waiting on me, what's overdue, what 1:1s are coming up. Mental Metal already has all the raw data — open commitments, delegations, people observations, living briefs, queue items — but nothing assembles it into a ready-to-read briefing. This change ties the core capabilities together into the highest-value daily user experience: one AI-generated page that tells the user "here's what matters today / this week / before your 1:1 with Sarah."

This is Tier 3 per `design/spec-plan.md` and depends on already-shipped `people-lens`, `commitment-tracking`, `delegation-tracking`, `initiative-living-brief`, `my-queue`, plus `ai-provider-abstraction` for LLM generation.

## What Changes

- Introduce a `BriefingService` that generates three briefing types on demand:
  - **Morning briefing** — today's priority commitments, today's 1:1s, top queue items, overdue delegations, one-sentence "focus for today".
  - **Weekly briefing** — milestones and due-dates in the next 7 days, overdue items, people who haven't had a 1:1 recently, initiatives needing attention.
  - **1:1 prep sheet** — per-person context: recent observations, open goals, pending commitments/delegations with that person, last 1:1 summary, AI-suggested talking points.
- Persist each generated briefing as a read-model row (`Briefing` aggregate) keyed by `(UserId, Type, ScopeKey, GeneratedAtUtc)` so users can see history and avoid regenerating when unchanged.
- Expose Minimal API endpoints:
  - `POST /api/briefings/morning` and `POST /api/briefings/weekly` — generate or return cached briefing for today/this-week.
  - `POST /api/briefings/one-on-one/{personId}` — generate 1:1 prep for a person.
  - `GET /api/briefings/recent` and `GET /api/briefings/{id}` — read history.
- AI generation uses the user's configured `AiProviderConfig` via `IAiCompletionService`; facts (dates, counts, names) are assembled deterministically from the database, and the AI only produces the narrative synthesis + talking points.
- Frontend:
  - Dashboard widget on the home page renders today's morning briefing (auto-generated once per day on first visit).
  - New `/briefings/weekly` route with a "Generate" button and latest briefing rendered as markdown.
  - Person detail page gains a **Generate 1:1 prep** action that opens a drawer/page with the prep sheet.

**Non-goals**
- Scheduled/emailed briefings (Tier 3 `nudges-rhythms` handles scheduling).
- Editing or annotating briefings — read-only artefacts.
- Cross-user or team briefings — single-user scope only.
- Regenerating a briefing if its inputs haven't changed (simple staleness check: most-recent-per-(type, scope) is served; user can explicitly regenerate).

## Capabilities

### New Capabilities
- `daily-weekly-briefing`: Morning, weekly, and 1:1 prep briefings generated on demand by a `BriefingService`, persisted as read-model rows, and surfaced in the dashboard, a weekly page, and the person detail page.

### Modified Capabilities
<!-- None. All inputs (commitments, delegations, people, observations, goals, one-on-ones, living briefs, queue scoring) are already specified; this spec consumes them through existing repositories and does not alter their requirements. -->

## Impact

- **New aggregate**: `Briefing` (lightweight read-model — id, userId, type, scopeKey, generatedAtUtc, markdownBody, tokensUsed, model). Not a rich domain aggregate; enforces only invariants on (userId, type, scopeKey) uniqueness per timestamp.
- **New Application slice**: `MentalMetal.Application/Briefings/` (BriefingService, GenerateMorningBriefing, GenerateWeeklyBriefing, GenerateOneOnOnePrep, GetRecentBriefings, GetBriefing).
- **New Infrastructure**: `BriefingRepository`, EF configuration, migration adding `Briefings` table.
- **New Web endpoints**: `BriefingEndpoints.cs` under `Features/Briefings/`.
- **AI**: consumes `IAiCompletionService` — no changes to provider abstraction.
- **Frontend**: new `BriefingService` (Angular signal service), dashboard widget, `/briefings/weekly` route, person-detail action. Follows CLAUDE.md: `@if`/`@for`, PrimeNG/`tailwindcss-primeui` tokens, standalone components, signals, `inject()`.
- **Options**: new `BriefingOptions` with `MorningBriefingHour` (local hour after which "today" briefing is valid; default 5), `WeeklyBriefingStaleHours` (default 12), `MaxBriefingTokens` (default 1500) — validated via `[Range]` + `ValidateDataAnnotations()` + `ValidateOnStart()`.
- **Clock**: service takes `TimeProvider` injected (not `DateTime.UtcNow`) for testability.
- **Dependencies**: consumes repositories for Commitments, Delegations, People, Observations, Goals, OneOnOnes, Initiatives, LivingBriefs, Captures (for queue-style "top items").
- **Migration**: new EF migration `AddBriefings`.
