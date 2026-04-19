# Initiative V2

Simplify the Initiative aggregate from a full living brief system with milestones, pending updates, and scoped chat into a lightweight entity: a name, a status, and an AI-generated auto-summary that updates itself from linked captures.

## Domain Changes

### Fields Kept
- `Id`: Guid
- `UserId`: Guid
- `Title`: string
- `Status`: InitiativeStatus (active, on-hold, completed, cancelled)
- `CreatedAt`: DateTimeOffset

### Fields Added
- `AutoSummary`: string? — AI-generated summary of the initiative based on all linked captures. Updated automatically when new captures are linked. Null until first AI generation.
- `LastSummaryRefreshedAt`: DateTimeOffset? — timestamp of last auto-summary generation

### Fields Removed
- `Milestone` entity (and all milestone-related operations)
- `LivingBrief` value object complex (summary, decisions, risks, requirements, design direction, dependencies)
- `PendingBriefUpdate` entity (and approval/rejection flow)
- All chat-related associations

### Invariants
- `Title` is required and non-empty
- `AutoSummary` is read-only from the domain perspective — set only by the AI extraction/refresh pipeline
- Status transitions follow the existing state machine (active ↔ on-hold, active → completed, active → cancelled)

## API Changes

### Kept Endpoints (simplified)
- `POST /api/initiatives` — Create initiative (title only)
- `GET /api/initiatives` — List initiatives for user
- `GET /api/initiatives/{id}` — Get initiative detail (includes auto-summary)
- `PUT /api/initiatives/{id}` ��� Update title
- `POST /api/initiatives/{id}/status` — Change status

### New Endpoints
- `POST /api/initiatives/{id}/refresh-summary` — Manually trigger AI summary regeneration from all linked captures

### Removed Endpoints
- All Living Brief sub-endpoints (summary update, decisions, risks, requirements, design direction, dependencies)
- All Pending Brief Update endpoints (list, get, apply, reject, edit)
- All Milestone endpoints (create, update, complete, remove)
- All Initiative Chat endpoints (start thread, list threads, post message, rename, archive, unarchive)
- `POST /api/initiatives/{id}/link-person` — initiatives are auto-linked by AI, not manually

## Auto-Summary Generation

### Trigger
When the AI extraction pipeline links a capture to an initiative (via initiative auto-tagging — see `ai-auto-extraction-v2` spec), the initiative's auto-summary is queued for refresh.

### Generation Process
1. Query all captures linked to this initiative, ordered by date
2. Send capture summaries (from extraction) to the AI with a prompt: "Summarise the current state of this initiative based on these meeting notes. Include: current status, key decisions, open risks, and next steps."
3. Replace `AutoSummary` with the AI response
4. Update `LastSummaryRefreshedAt`

### Refresh Policy
- Auto-refresh is triggered by new capture linkage (not on every page view)
- Manual refresh available via API endpoint
- No approval queue — AI writes directly to `AutoSummary`
- If the user disagrees with the summary, they can manually refresh (which re-reads all captures) or just wait for the next capture to correct it

## Frontend Changes

### Initiative List Page
- Simplified: shows title, status, auto-summary preview (first 200 chars), linked capture count
- Remove milestone progress indicators
- Remove living brief status indicators

### Initiative Detail Page
- Shows: title, status, full auto-summary, linked captures list
- Remove: milestones tab, living brief tab (decisions, risks, requirements, design), chat tab, pending updates section
- Add: "Refresh Summary" button
- Add: list of linked captures with dates and summaries

### Create Initiative Dialog
- Title only (and optional status)
- Remove all other fields

## Migration

Part of the V2 migration:
- Add `auto_summary` text column to `initiatives` table (nullable)
- Add `last_summary_refreshed_at` timestamptz column to `initiatives` table (nullable)
- Drop `pending_brief_updates` table
- Drop milestone columns/table from initiatives
- Drop living brief JSON columns from initiatives (decisions, risks, requirements, design_direction, dependencies, summary — the old complex structure)
- Note: existing `summary` column may need to be renamed or replaced by `auto_summary` to avoid confusion

## Acceptance Criteria

- [ ] Initiative aggregate has only: Id, UserId, Title, Status, AutoSummary, LastSummaryRefreshedAt, CreatedAt
- [ ] All milestone-related code removed (domain, handlers, endpoints, UI)
- [ ] All living brief complex code removed (pending updates, decisions, risks, requirements, design, dependencies)
- [ ] All initiative chat code removed
- [ ] Auto-summary generated from linked captures via AI
- [ ] Auto-summary refreshes when new capture linked to initiative
- [ ] Manual refresh endpoint works
- [ ] No approval/pending update flow — AI writes directly
- [ ] Initiative list shows auto-summary preview and capture count
- [ ] Initiative detail shows full auto-summary and linked captures
- [ ] Create dialog is title-only
- [ ] Domain tests cover simplified status transitions
- [ ] Integration tests cover auto-summary refresh trigger
