# Weekly Brief

A deep synthesis of the full week's meetings and notes. Goes beyond the daily brief by surfacing patterns across conversations, identifying open threads that span multiple days, and providing a high-level view of where the user's time and attention went.

## What the Weekly Brief Contains

### Week Overview
- Total captures: meetings, quick notes, audio recordings
- Date range covered
- AI-generated narrative (3-5 paragraphs) synthesising the week

### Cross-Conversation Insights
The unique value of the weekly brief — things only visible when you look across an entire week:
- **Recurring themes**: topics that came up in multiple meetings
- **People featured heavily**: who dominated the user's week and in what context
- **Open threads**: discussions that started but haven't reached resolution (mentioned in early-week meetings but not closed by end of week)
- **Contradictions**: different people saying different things about the same topic across separate meetings

### Decisions Made This Week
- Aggregated from all capture extractions
- Deduplicated (same decision mentioned in multiple meetings)
- Each shows: decision, date, which meeting it was made in

### Commitment Status
- New commitments created this week (from extraction)
- Commitments completed this week
- Commitments now overdue
- Net change: are things getting better or worse?

### Risks and Concerns
- Aggregated from all capture extractions
- Risks mentioned in multiple meetings highlighted as recurring

### Initiative Activity
- For each active initiative that had captures this week:
  - Number of meetings where it was discussed
  - Key developments (from extraction summaries)
  - Auto-summary status (when last refreshed)

## Generation Process

### Trigger
- Generated on-demand when user views the weekly brief page
- Intended to be viewed Monday morning or Friday afternoon
- Not auto-generated on a schedule (V2 keeps it simple)

### Data Assembly
1. Query all Captures from the last 7 calendar days (user's timezone)
2. Query all Commitments (for status tracking: created, completed, overdue)
3. Query all Initiatives with linked captures from the period
4. For each capture: include extraction summary, decisions, risks, commitments, people mentions

### AI Synthesis
Feed the assembled data to the AI with a prompt designed for weekly-level synthesis:
- "Generate a weekly intelligence briefing for an engineering leader. Analyse patterns across meetings, identify recurring themes, surface open threads that need attention, and highlight contradictions between different sources."
- The prompt explicitly asks for cross-conversation analysis — not just a day-by-day recap
- Response is structured to match the brief sections

### Token Budget
A full week of transcripts could be very large. Strategy:
- Send extraction **summaries** to the AI, not raw transcript text
- For the top 5 most-mentioned people: include their extraction context snippets
- For the top 3 most-discussed initiatives: include initiative auto-summaries
- This keeps the AI prompt within reasonable token limits while preserving cross-reference ability

### Caching
- Cache key: `weekly-brief:{userId}:{weekStartDate}`
- Cached for 24 hours (user might re-read multiple times)
- Invalidated if new captures are added for the covered week
- Manual refresh available

## API

### Endpoints
- `GET /api/briefing/weekly` — returns the weekly brief for the most recent 7-day window
  - Query params: `weekOf=2026-04-14` (optional, defaults to current week)
  - Response: `{ narrative: string, crossConversationInsights: {...}, decisions: [], commitmentStatus: {...}, risks: [], initiativeActivity: [], captureCount: number, dateRange: {...}, generatedAt: timestamp }`
- `POST /api/briefing/weekly/refresh` — force regeneration

## Frontend

### Weekly Brief Page
Accessible from dashboard ("View Weekly Brief" link) and from navigation.

```
┌──────────────────────────────────────────────────┐
│  Weekly Brief — Apr 14-18               [Refresh]│
│  18 meetings, 5 quick notes                      │
│  ──────────────────────────────────────────────── │
│  [AI narrative — 3-5 paragraphs]                 │
│                                                   │
│  CROSS-CONVERSATION INSIGHTS                      │
│  ──────────────────────────────────────────────── │
│  Recurring themes:                                │
│  • [theme] — came up in 4 meetings               │
│  • [theme] — came up in 3 meetings               │
│                                                   │
│  Open threads:                                    │
│  • [topic] — discussed Mon + Wed, no resolution  │
│  • [topic] — raised Tue, pending follow-up       │
│                                                   │
│  DECISIONS (7 this week)                          │
│  ──────────────────────────────────────────────── │
│  • [decision] — [date] — [meeting]               │
│  • [decision] — [date] — [meeting]               │
│                                                   │
│  COMMITMENT TRACKER                               │
│  ──────────────────────────────────────────────── │
│  New: 8  |  Completed: 3  |  Overdue: 4          │
│                                                   │
│  RISKS & CONCERNS                                 │
│  ──────────────────────────────────────────────── │
│  ⚠ [risk] — mentioned in 2 meetings             │
│  • [risk] — mentioned once                       │
│                                                   │
│  INITIATIVE ACTIVITY                              │
│  ──────────────────────────────────────────────── │
│  [Initiative] — 5 meetings — [key development]   │
│  [Initiative] — 2 meetings — [key development]   │
└──────────────────────────────────────────────────┘
```

### Navigation
- Dashboard has a "View Weekly Brief" link/button
- Also accessible from main navigation under Briefings or as a top-level route

## Acceptance Criteria

- [ ] Weekly brief generated from last 7 days of captures
- [ ] AI narrative provides cross-conversation synthesis (not day-by-day recap)
- [ ] Recurring themes identified across multiple meetings
- [ ] Open threads surfaced (discussed but unresolved)
- [ ] Contradictions between sources highlighted
- [ ] Decisions aggregated and deduplicated across the week
- [ ] Commitment status shows: new, completed, overdue counts
- [ ] Risks aggregated with recurrence count
- [ ] Initiative activity shows per-initiative meeting count and developments
- [ ] Token budget managed by sending summaries, not raw transcripts
- [ ] Brief cached for 24 hours per week period
- [ ] Cache invalidated on new captures for the covered period
- [ ] Manual refresh available
- [ ] Accessible from dashboard and navigation
- [ ] Loading skeleton shown during generation
- [ ] Historical weeks accessible via date parameter
- [ ] Application tests cover synthesis with mocked AI
- [ ] Integration tests cover caching, refresh, and date range handling
