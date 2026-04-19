# Daily Brief

A morning intelligence briefing generated from the previous day's captures and the user's current commitment state. Answers the question: "What do I need to know right now to start my day?"

## What the Daily Brief Contains

### Yesterday's Summary
- How many meetings/notes were captured yesterday
- Key topics discussed (derived from extraction summaries)
- Key decisions made (from extraction `Decisions` fields)
- New risks surfaced (from extraction `Risks` fields)

### Fresh Commitments
- Commitments extracted from yesterday's captures (new since last brief)
- Grouped by confidence: high shown prominently, medium shown in secondary section
- Each shows: description, person, source capture link

### Due Today
- All open commitments with due date = today
- Sorted by: overdue first, then confidence (high first)
- Each shows: description, person, due date, days overdue (if applicable)

### Overdue Items
- All open commitments past their due date
- Count + list, sorted by how overdue they are

### People Activity
- People with the most mentions yesterday (top 5)
- Brief note: "[Person] mentioned in 3 meetings" — click navigates to dossier

## Generation Process

### Trigger
- Generated on-demand when user opens the dashboard or explicitly requests it
- Can also be pre-generated on a schedule (future optimisation, not V2 scope)

### Data Assembly
1. Query all Captures from the previous calendar day (user's timezone from preferences)
2. Query all open Commitments (any due date)
3. For captures: collect extraction summaries, decisions, risks, fresh commitments
4. For commitments: filter to due-today and overdue

### AI Synthesis
Feed the assembled data to the AI with a prompt:
- "Generate a morning briefing for an engineering leader. Summarise yesterday's meetings, highlight what needs attention today, and flag anything overdue."
- The AI produces a structured response matching the brief sections
- Raw data (commitments, captures) is also returned alongside the AI narrative so the UI can render structured lists

### Caching
- Cache key: `daily-brief:{userId}:{date}`
- Generated once per day, cached until next day
- Manual refresh available (re-generates from same data)
- If new captures are added for yesterday after the brief was generated: cache invalidated

## API

### Endpoints
- `GET /api/briefing/daily` — returns today's daily brief
  - If not yet generated: generates on-demand (2-5 second wait)
  - If cached: returns immediately
  - Response: `{ narrative: string, freshCommitments: [], dueToday: [], overdue: [], peopleActivity: [], captureCount: number, generatedAt: timestamp }`
- `POST /api/briefing/daily/refresh` — force regeneration

### Removed Endpoints
- `GET /api/briefing/morning` (V1) — replaced by this endpoint
- All `GenerateOneOnOnePrep` endpoints — replaced by People Dossier prep mode

## Frontend

### Dashboard Widget
The daily brief is the primary widget on the dashboard landing page:

```
┌──────────────────────────────────────────────────┐
│  Daily Brief — [Today's Date]         [Refresh]  │
│  ──────────────────────────────────────────────── │
│  [AI narrative paragraph]                         │
│                                                   │
│  FRESH COMMITMENTS (from yesterday)               │
│  ● [description] → [person]                      │
│  ● [description] → [person]                      │
│                                                   │
│  DUE TODAY                                        │
│  ● [description] — [person] — due today          │
│  ○ [description] — [person] — due today          │
│                                                   │
│  OVERDUE (3 items)                                │
│  ⚠ [description] — [person] — 2 days overdue    │
│  ⚠ [description] — [person] — 1 day overdue     │
│                                                   │
│  PEOPLE ACTIVE YESTERDAY                          │
│  [Person] — 3 mentions  [Person] — 2 mentions   │
└──────────────────────────────────────────────────┘
```

### Loading State
- Show skeleton loader while brief generates (first load of the day)
- Show cached brief immediately on subsequent visits

### Empty State
- If no captures from yesterday: "No meetings captured yesterday. Upload transcripts or record a meeting to get your daily brief."

## Relationship to Weekly Brief

The daily brief covers one day. The weekly brief (see `weekly-brief` spec) covers 7 days with deeper pattern analysis. Both are independent — the weekly brief is not an aggregation of daily briefs, it's a separate synthesis.

## Acceptance Criteria

- [ ] Daily brief generates from previous day's captures and current commitments
- [ ] AI narrative summarises key topics, decisions, and risks from yesterday
- [ ] Fresh commitments section shows commitments extracted yesterday
- [ ] Due today section shows commitments due on the current date
- [ ] Overdue section shows past-due commitments with days overdue count
- [ ] People activity shows top 5 most-mentioned people from yesterday
- [ ] Brief cached per day, returns instantly on repeat visits
- [ ] Cache invalidated if new captures added for yesterday
- [ ] Manual refresh available
- [ ] Empty state shown when no captures exist for yesterday
- [ ] Dashboard widget renders all sections
- [ ] Loading skeleton shown during generation
- [ ] Commitment items link to their source capture
- [ ] People names link to their dossier
- [ ] Timezone from user preferences used for "yesterday" calculation
- [ ] Application tests cover brief generation with mocked AI
- [ ] Integration tests cover endpoint caching and refresh behavior
