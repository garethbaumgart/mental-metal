# V2 Product Pivot — Implementation Tasks

Tasks are grouped into phases with explicit dependencies. Each phase must complete before the next begins (except where noted). Within a phase, tasks can be parallelised where marked.

---

## Phase A: Surgical Removal

Remove the 7 killed aggregates and all dependent code. The codebase must compile cleanly after this phase.

**Spec:** `surgical-removal.md`

### A1. Remove killed frontend code
- [x] Delete pages: delegations, goals, observations, one-on-ones, interviews, global-chat
- [x] Delete features: nudges, my-queue, daily-close-out
- [x] Delete services: delegations, goals, observations, one-on-ones, interviews, global-chat-state, global-chat, ai-nudge
- [x] Delete models: delegation, goal, observation, one-on-one, interview, chat-thread, briefing (V1)
- [x] Remove routes from `app.routes.ts`
- [x] Remove navigation entries from `sidebar.component.ts`
- [x] Remove dashboard widgets: one-on-ones, top-of-queue, overdue (V1)
- [x] Verify: `npx ng build` compiles with zero errors

### A2. Remove killed backend endpoints
- [x] Remove endpoint registrations from `Program.cs` for: delegations, goals, observations, one-on-ones, interviews, nudges, global chat, initiative chat, daily close-out, my-queue
- [x] Delete standalone endpoint files: `InterviewEndpoints.cs`, `NudgesEndpoints.cs`
- [x] Delete `BriefingEndpoints.cs` (V1 — will be replaced in Phase E)
- [x] Delete `MyQueueEndpoints.cs`
- [x] Delete `DailyCloseOutEndpoints.cs`

### A3. Remove killed application layer
- [x] Delete feature folders: Delegations, Goals, Observations, OneOnOnes, Interviews, Nudges
- [x] Delete chat folders: Chat/Global, Initiatives/Chat
- [x] Delete: DailyCloseOut handlers, MyQueue handlers
- [x] Delete `BriefingFactsAssembler.cs` and `BriefingService.cs` (V1 — will be rewritten)
- [x] Delete all killed DTOs
- [x] Clean up `DependencyInjection.cs` — remove registrations for killed services

### A4. Remove killed infrastructure layer
- [x] Delete repositories: Delegation, Goal, Observation, OneOnOne, Interview, Nudge, ChatThread, Briefing (V1), PendingBriefUpdate
- [x] Delete EF configurations for killed aggregates
- [x] Remove DbSet properties from `MentalMetalDbContext.cs`
- [x] Remove `OnModelCreating` calls for killed entities

### A5. Remove killed domain layer
- [x] Delete aggregate folders: Delegations, Goals, Observations, OneOnOnes, Interviews, Nudges, ChatThreads
- [x] Delete `Briefings` aggregate folder (read-only V1 model)
- [x] Delete `DailyCloseOutLog` from User aggregate
- [x] Delete `PendingBriefUpdate` and related entities from Initiative/LivingBrief

### A6. Remove killed tests
- [x] Delete domain tests for killed aggregates
- [x] Delete application tests for killed handlers
- [x] Delete integration tests for killed endpoints
- [x] Verify: `dotnet test src/MentalMetal.slnx` passes

### A7. Create database migration: drop killed tables
- [x] Create migration `V2_DropKilledAggregates`
- [x] Drop all tables for killed aggregates (see list in `surgical-removal.md`)
- [x] Drop foreign keys from killed tables before dropping tables
- [x] Forward-only migration (no `Down()`)
- [ ] Verify migration applies cleanly to a fresh database

**Phase A gate:** `dotnet build`, `ng build`, and `dotnet test` all pass. No references to killed types remain (grep verification).

---

## Phase B: Reshape Kept Aggregates

Modify the 4 kept aggregates (Person, Initiative, Capture, Commitment) to their V2 shapes.

**Specs:** `person-v2.md`, `initiative-v2.md`, `commitment-auto-tracker.md`, `ai-auto-extraction-v2.md`

**Depends on:** Phase A complete

### B1. Reshape Person aggregate
- [ ] Remove: CareerDetails, CandidateDetails, PipelineStatus from domain
- [ ] Add: `Aliases` (List<string>) with invariants (case-insensitive uniqueness per user)
- [ ] Update Person handlers: strip career/candidate endpoints, add alias endpoints
- [ ] Update Person DTOs
- [ ] Update frontend: strip career details from create/edit, add alias management
- [ ] Update EF configuration for aliases (JSONB)
- [ ] Domain tests for alias invariants

### B2. Reshape Initiative aggregate
- [ ] Remove: Milestones entity, LivingBrief complex VO (decisions, risks, requirements, design, dependencies), PendingBriefUpdate entity and approval flow, Chat associations
- [ ] Add: `AutoSummary` (string?), `LastSummaryRefreshedAt` (DateTimeOffset?)
- [ ] Update Initiative handlers: strip milestone/brief/chat endpoints, add refresh-summary endpoint
- [ ] Update frontend: strip milestone tab, brief tab, chat tab, pending updates section
- [ ] Simplify Initiative detail to: title, status, auto-summary, linked captures
- [ ] Domain tests for simplified aggregate

### B3. Reshape Commitment aggregate
- [ ] Add: `SourceCaptureId` (Guid), `Confidence` (enum: high/medium/low), `DismissedAt` (DateTimeOffset?)
- [ ] Add: `dismissed` to CommitmentStatus enum
- [ ] Remove: `POST /api/commitments` (manual creation), `PUT /api/commitments/{id}` (manual edit)
- [ ] Add: `POST /api/commitments/{id}/dismiss` endpoint
- [ ] Update list endpoint: default filter=open, sort by urgency then confidence
- [ ] Update frontend: remove create dialog, add dismiss action, add confidence badge, add source link
- [ ] Domain tests for dismiss/reopen invariants, confidence enum

### B4. Reshape Capture extraction
- [ ] Update `AiExtraction` VO to `AiExtractionV2` shape (see `ai-auto-extraction-v2.md`)
- [ ] Add `Source` enum to Capture (upload, bookmarklet, audio-capture, typed, voice)
- [ ] Remove: `POST /api/captures/{id}/process` (auto-trigger on create)
- [ ] Remove: `POST /api/captures/{id}/confirm-extraction`, `POST /api/captures/{id}/discard-extraction`
- [ ] Remove: manual link/unlink endpoints for person and initiative
- [ ] Update capture creation to auto-trigger extraction
- [ ] Update EF configuration for new extraction JSON shape
- [ ] Application tests for auto-trigger behavior

### B5. Create database migration: reshape kept aggregates
- [ ] Migration `V2_ReshapeAggregates`
- [ ] Person: add `aliases` JSONB, drop career/candidate columns
- [ ] Initiative: add `auto_summary`, `last_summary_refreshed_at`, drop milestone/brief complex columns
- [ ] Commitment: add `source_capture_id`, `confidence`, `dismissed_at`
- [ ] Capture: add `source` column, update extraction JSON column type if needed
- [ ] Verify migration applies cleanly

**Phase B gate:** All kept aggregates match V2 shapes. Build and tests pass.

---

## Phase C: Port Audio Capture from Praxis-note

Add browser-based meeting recording with Deepgram transcription.

**Specs:** `browser-audio-capture.md`, `quick-note-voice.md`

**Depends on:** Phase A complete (clean codebase). Can run in parallel with Phase B.

### C1. Port backend transcription relay
- [ ] Create `TranscriptionEndpoints.cs` with WebSocket relay endpoint
- [ ] Create health check endpoint `/api/transcription/status`
- [ ] Add Deepgram configuration to user settings (or app settings)
- [ ] Register endpoints in `Program.cs`
- [ ] Integration tests for WebSocket relay (mock Deepgram)

### C2. Port frontend audio services
- [ ] Port `AudioRecorderService` from Praxis-note (adapt to Mental Metal's service patterns)
- [ ] Port `DeepgramTranscriptionService` from Praxis-note
- [ ] Port `audio-pcm-processor.js` AudioWorklet to `public/` assets
- [ ] Adapt service interfaces to work with Mental Metal's Capture creation flow
- [ ] Unit tests for service configuration and state management

### C3. Build recording UI
- [ ] Add "Record Meeting" button to Captures page
- [ ] Build inline recording panel: duration, audio levels, interim transcript
- [ ] Build stop/cancel/discard controls
- [ ] On stop: show transcript review, save creates Capture with type=meeting-recording, source=audio-capture
- [ ] Handle permissions: mic denied, screen share cancelled (fallback to mic-only)
- [ ] Add Deepgram API key to Settings page

### C4. Add voice mode to Quick Capture
- [ ] Add Voice/Type mode toggle to Quick Capture dialog
- [ ] Voice mode: mic capture → Deepgram → interim transcript → editable text area
- [ ] On save: Capture with type=quick-note, source=voice
- [ ] Disable voice mode when Deepgram not configured
- [ ] Handle: mic denied, empty transcript, connection failure

**Phase C gate:** Can record a meeting from browser, see real-time transcript, save as capture. Voice quick notes work.

---

## Phase D: Rework Extraction Pipeline

Make extraction fully automatic with confidence scoring and auto-linking.

**Spec:** `ai-auto-extraction-v2.md`

**Depends on:** Phase B complete (reshaped aggregates with new fields)

### D1. Implement auto-extraction pipeline
- [ ] Modify capture creation to auto-trigger extraction (remove manual process trigger)
- [ ] Update AI prompt for V2 extraction shape (people mentions, confidence-scored commitments, decisions, risks, initiative tags)
- [ ] Parse AI response into `AiExtractionV2` structure
- [ ] Handle failures gracefully (capture → failed status, retry available)

### D2. Implement name resolution
- [ ] Build name resolution service: match extracted names against Person.CanonicalName + Aliases
- [ ] Exact match (case-insensitive) → link
- [ ] Fuzzy substring match (min 3 chars, unambiguous) → link
- [ ] Unresolved → store in extraction with raw name
- [ ] Auto-link captures to resolved People

### D3. Implement initiative auto-tagging
- [ ] Match extracted initiative/project names against Initiative titles (fuzzy match)
- [ ] Auto-link captures to matched Initiatives
- [ ] Queue initiative auto-summary refresh for newly linked initiatives
- [ ] Unresolved initiative tags stored in extraction

### D4. Implement commitment spawning
- [ ] For high + medium confidence extracted commitments: create Commitment entity
- [ ] Set SourceCaptureId, Confidence, Direction, PersonId (if resolved), InitiativeId (if matched)
- [ ] Low confidence: store in extraction JSON only, do not create entity
- [ ] Application tests covering confidence filtering and commitment creation

### D5. Build unresolved mentions UI
- [ ] Show unresolved person mentions in capture detail view
- [ ] "Resolve" action: pick existing Person (adds alias) or create new Person
- [ ] Show unresolved initiative tags in capture detail view
- [ ] "Link" action: pick existing Initiative or create new one
- [ ] On resolve: re-link capture, add alias, optionally re-extract

**Phase D gate:** Creating a capture auto-extracts, auto-links people and initiatives, auto-creates commitments. Unresolved mentions are actionable in UI.

---

## Phase E: Build Output Views

The three read-only views that surface the intelligence.

**Specs:** `people-dossier.md`, `daily-brief.md`, `weekly-brief.md`

**Depends on:** Phase D complete (extraction populates the data these views read)

### E1. Build People Dossier
- [ ] Backend: `GET /api/people/{id}/dossier` endpoint — queries captures, generates AI synthesis
- [ ] Backend: caching layer (1-hour TTL, invalidate on new capture link)
- [ ] Backend: pre-meeting prep mode (`?mode=prep`) with different AI prompt
- [ ] Frontend: replace person detail page with dossier view
- [ ] Frontend: synthesis section, open commitments, transcript mentions, unresolved mentions
- [ ] Frontend: loading skeleton for AI generation
- [ ] Application tests with mocked AI

### E2. Build Daily Brief
- [ ] Backend: rewrite `BriefingService` for V2 (direct capture synthesis, no facts assembler)
- [ ] Backend: `GET /api/briefing/daily` endpoint — assembles yesterday's data, generates AI brief
- [ ] Backend: caching (per day per user, invalidate on new captures)
- [ ] Backend: `POST /api/briefing/daily/refresh` endpoint
- [ ] Frontend: daily brief component (narrative, fresh commitments, due today, overdue, people activity)
- [ ] Application tests with mocked AI

### E3. Build Weekly Brief
- [ ] Backend: `GET /api/briefing/weekly` endpoint — assembles 7 days of data, generates AI synthesis
- [ ] Backend: token budget management (send summaries not raw transcripts)
- [ ] Backend: caching (24-hour TTL per week period)
- [ ] Frontend: weekly brief page (narrative, cross-conversation insights, decisions, commitment status, risks, initiative activity)
- [ ] Application tests with mocked AI

**Phase E gate:** People Dossier shows cross-transcript synthesis. Daily and weekly briefs generate correctly.

---

## Phase F: Reshape Frontend

Final UI assembly — dashboard, navigation, and cleanup.

**Spec:** `dashboard-v2.md`, `transcript-daily-ingest.md`

**Depends on:** Phase E complete (output views exist to render on dashboard)

### F1. Build V2 Dashboard
- [ ] Replace dashboard shell with 3-section layout: Daily Brief, Open Commitments, People
- [ ] Daily Brief section: render inline from daily brief component (Phase E2)
- [ ] Open Commitments section: compact list with inline Complete/Dismiss actions
- [ ] People Quick Access section: recently active people with mention counts
- [ ] Empty states for new users
- [ ] "View Weekly Brief" link

### F2. Enhance Captures page for daily upload
- [ ] Add drag-drop zone to Captures page (not in a dialog)
- [ ] Support multi-file upload with per-file progress
- [ ] Show extraction status as captures process
- [ ] Failed files: inline error + retry
- [ ] Batch summary on completion

### F3. Update navigation and routing
- [ ] Update sidebar: Dashboard, Captures, People, Commitments, Initiatives, Settings
- [ ] Remove all killed routes
- [ ] Add weekly brief route
- [ ] Verify all navigation links work
- [ ] Verify deep links work (bookmarkable URLs)

**Phase F gate:** Complete V2 UI. User can upload transcripts, view dashboard, browse people dossiers, manage commitments, read briefs.

---

## Phase G: Tests and Verification

Comprehensive testing of the V2 system.

**Depends on:** All previous phases complete

### G1. Domain and application tests
- [ ] Run full `dotnet test` — all remaining tests pass
- [ ] Verify no test references killed aggregates
- [ ] New domain tests: Person aliases, Commitment confidence/dismiss, Initiative auto-summary, Capture source enum
- [ ] New application tests: auto-extraction pipeline, name resolution, commitment spawning, briefing generation

### G2. Integration tests
- [ ] New integration tests: multi-file upload, dossier endpoint, daily brief endpoint, weekly brief endpoint
- [ ] Existing integration tests: verify auth, capture import, PAT endpoints still pass
- [ ] WebSocket relay test (mocked Deepgram)

### G3. Manual smoke test
- [ ] Upload 3 transcript files → all process and extract automatically
- [ ] Verify people auto-linked from transcripts
- [ ] Verify commitments auto-created with confidence scores
- [ ] Verify initiatives auto-tagged
- [ ] Open dashboard → daily brief renders
- [ ] Click person → dossier loads with synthesis
- [ ] View weekly brief → renders cross-conversation insights
- [ ] Record meeting (if Deepgram configured) → transcript saved as capture
- [ ] Quick note voice → transcript saved
- [ ] Dismiss a false-positive commitment
- [ ] Complete a real commitment
- [ ] Resolve an unresolved person mention

**Phase G gate:** All tests pass. Manual smoke test confirms end-to-end V2 flow.

---

## Task Count Summary

| Phase | Tasks | Parallelisable |
|-------|-------|----------------|
| A: Surgical Removal | 7 | A1-A5 can run in sequence (compilation dependency), A6 after A5, A7 after A4 |
| B: Reshape Aggregates | 5 | B1-B4 can partially parallelise, B5 after B1-B4 |
| C: Port Audio | 4 | C1-C2 parallel, C3 after C2, C4 after C2 |
| D: Extraction Pipeline | 5 | D1 first, D2-D4 can parallelise after D1, D5 after D2-D3 |
| E: Output Views | 3 | E1-E3 can parallelise (independent views) |
| F: Frontend Assembly | 3 | F1 after E, F2 parallel with F1, F3 after F1 |
| G: Tests | 3 | G1-G2 parallel, G3 after G1-G2 |
| **Total** | **30** | |
