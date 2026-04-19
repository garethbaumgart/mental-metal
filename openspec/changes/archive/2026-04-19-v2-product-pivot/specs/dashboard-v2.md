# Dashboard V2

Reshape the dashboard from a 5-widget layout pulling from multiple killed aggregates into a focused landing page with three sections: daily brief, open commitments, and people quick access.

## Current State (V1)

The V1 dashboard has 5 widgets:
1. Morning Briefing Widget — depends on `BriefingFactsAssembler` (killed dependencies)
2. Today's Commitments Widget — KEEP concept, but rework data source
3. Today's One-on-Ones Widget — depends on OneOnOne aggregate (KILLED)
4. Top of Queue Widget — depends on MyQueue (KILLED)
5. Overdue Summary Widget — depends on Delegations (KILLED) + Commitments

## V2 Dashboard Layout

Three sections, top to bottom:

### 1. Daily Brief (Primary)
- The full daily brief rendered inline (see `daily-brief` spec)
- AI narrative, fresh commitments, due today, overdue items
- "View Weekly Brief" link at the bottom
- This is the first thing the user sees every morning

### 2. Open Commitments (Quick Action)
- Compact list of all open commitments, sorted by urgency
- Quick action buttons: Complete, Dismiss (inline, no navigation needed)
- Grouped: Overdue → Due Today → Due This Week → Later → No Date
- Shows: description, person, due date, confidence badge
- "View All" link to full commitments page

### 3. People Quick Access
- Grid/list of people with recent activity
- Shows: name, mention count (last 7 days), latest mention date
- Sorted by: most mentions in last 7 days (most active first)
- Click navigates to People Dossier
- Only shows people with at least 1 mention in the last 14 days (hides inactive)

## Removed Widgets

| Widget | Reason | Replacement |
|--------|--------|-------------|
| Morning Briefing (V1) | Depended on killed aggregates | Daily Brief (V2) — richer, from captures |
| Today's One-on-Ones | OneOnOne aggregate killed | People Dossier has pre-meeting prep |
| Top of Queue | MyQueue killed | Daily Brief + Commitments section |
| Overdue Summary | Depended on Delegations | Overdue section in Daily Brief + Commitments |

## Responsive Behavior

### Desktop (>1024px)
- Daily Brief: full width
- Open Commitments: full width below brief
- People Quick Access: full width below commitments

### Tablet (768-1024px)
- Same layout, narrower

### Mobile (<768px)
- Same layout, single column, all sections stacked

## Empty States

### No Captures Yet
Dashboard shows onboarding prompt:
- "Welcome to Mental Metal. Upload your first transcripts to get started."
- Upload button prominently displayed
- Brief explanation of the three input methods

### No Commitments
Commitments section hidden or shows: "No open commitments. They'll appear here as you upload transcripts."

### No People
People section hidden or shows: "Add your team to start building people dossiers."

## Navigation Changes

### Sidebar/Nav Updates
V2 navigation items:
- **Dashboard** (landing page — this spec)
- **Captures** (upload, record, list)
- **People** (list, dossier)
- **Commitments** (full list with filters)
- **Initiatives** (list, detail)
- **Settings** (AI provider, Deepgram, PATs, profile)

Removed from navigation:
- Delegations
- Goals / Observations
- One-on-Ones
- Interviews
- Global Chat
- Briefings (separate page — daily brief is on dashboard, weekly brief is linked from dashboard)

## Quick Capture Integration

The Quick Capture FAB (floating action button) remains on the dashboard:
- Click opens Quick Capture dialog (voice or type)
- Keyboard shortcut preserved
- After saving a quick note, it appears in the capture list and triggers extraction

## Acceptance Criteria

- [ ] Dashboard renders three sections: Daily Brief, Open Commitments, People
- [ ] Daily Brief section shows full brief content (narrative + structured data)
- [ ] Open Commitments section shows all open commitments sorted by urgency
- [ ] Complete and Dismiss actions work inline (no page navigation)
- [ ] People section shows recently active people with mention counts
- [ ] Clicking a person navigates to their dossier
- [ ] All V1 widgets removed (morning briefing V1, one-on-ones, queue, overdue V1)
- [ ] Navigation updated: killed pages removed, V2 pages added
- [ ] Empty states render correctly for new users
- [ ] Quick Capture FAB works from dashboard
- [ ] Responsive layout works on desktop, tablet, mobile
- [ ] Dashboard loads within 3 seconds (brief may show loading skeleton on first daily visit)
- [ ] "View Weekly Brief" link navigates to weekly brief page
- [ ] "View All" on commitments navigates to full commitments page
