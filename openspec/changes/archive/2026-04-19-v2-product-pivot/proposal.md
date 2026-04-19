## Why

Mental Metal V1 was built on a thesis: give engineering managers structured forms to track commitments, delegations, goals, observations, 1:1s, interviews, and nudges, then layer AI on top. The result is 12 aggregates, 121 API endpoints, and a product that demands constant bookkeeping from a user who already has too many things competing for attention.

After uploading and analysing a full week of real meeting transcripts, a clear pattern emerged. The user sits in 15-20 meetings per week. The dominant cognitive load is **people context across conversations** — the same person discussed in 4-5 different meetings with different people giving different perspectives. The second load is **tracking commitments made in passing** — promises to follow up, send data, schedule meetings, give feedback. The third is **synthesising what happened** across a week of context-switching.

None of these require manual data entry. The transcripts already contain everything. The AI's job is to read them all, connect the dots, and surface what matters. The user's job is to dump transcripts in daily, and read what the AI produces.

V1 asks the user to be a bookkeeper. V2 makes the user a reader.

### Evidence from the transcript analysis

- **People context**: A single team member appeared in 5 separate meetings with different stakeholders, each giving a different assessment of their performance and motivation. Synthesising this across conversations is the hard problem no tool solves today.
- **Commitment tracking**: ~10 concrete commitments were made in one week ("I'll follow up on that tomorrow", "I'll send you the data", "I'll review and adjust the document"). All extractable from transcript text. None would have been manually entered.
- **Time allocation**: The majority of meeting time was spent on people management and team dynamics, with project oversight and strategic discussions filling the remainder. The app should reflect this reality, not an idealised workflow.

### What V1 got right

The engineering foundation is excellent: clean architecture, DDD aggregates, signals-only Angular, AI provider abstraction, PAT system, capture pipeline, CORS policy. This infrastructure stays. What changes is the product layer on top.

## What Changes

### Product pivot: three inputs, three outputs

**Inputs (zero friction each):**
1. **Daily transcript upload** — bulk drag-drop of .docx/.txt/.html files from Google Drive exports. The daily rhythm replaces the weekly cadence initially envisioned.
2. **Browser audio capture** — for Google Meet calls where the user can't enable Google's recording. Uses Web Audio API (`getDisplayMedia` for tab audio + `getUserMedia` for mic), streams to Deepgram for real-time transcription. Ported from the proven Praxis-note implementation (same tech stack: Angular + .NET).
3. **Quick note** — voice-to-text (via Deepgram) or typed text. Captures a thought between meetings in under 10 seconds.

**Outputs (read-only, AI-generated):**
1. **People Dossier** — per-person view synthesising everything said about/by them across ALL meetings, not just 1:1s. Surfaces contradictions, performance signals, open commitments, and a pre-meeting prep summary.
2. **Commitment Tracker** — auto-extracted from transcripts, ranked by AI confidence (high/medium/low). High-confidence items shown prominently, low-confidence items dismissable. Bidirectional: "I owe X" and "X owes me". Zero manual creation.
3. **Daily + Weekly Brief** — Daily: what happened yesterday, what's due today, fresh commitments. Weekly: full synthesis, patterns across conversations, open threads, time allocation analysis.

### Surgical removal of manual bookkeeping

Seven aggregates and their entire stack (domain, application, infrastructure, endpoints, UI, tests) are removed:

| Aggregate | Files | Reason |
|-----------|-------|--------|
| Delegation | ~33 | Commitments subsume this. "I delegated X to Y" is a commitment. |
| Goal | ~11 | Auto-extracted from transcripts or not tracked at all. |
| Observation | ~8 | The People Dossier replaces manual observations with transcript-derived signals. |
| OneOnOne | ~15 | A 1:1 is just a transcript. No separate entity needed. |
| Interview | ~28 | Lives in recruiting tools. Not Mental Metal's job. |
| Nudge | ~22 | The daily brief IS the nudge. |
| ChatThread | ~29 | The user already has Claude/ChatGPT. Mental Metal's job is synthesis, not chat. |

**Also removed:**
- Daily Close-Out workflow (extractions auto-apply, nothing to confirm)
- My Queue (the daily brief is the queue)
- Global/Initiative Chat (redundant with external AI tools)

**Total:** ~165 files deleted, ~67 files reworked.

### Kept aggregates reshaped

| Aggregate | V1 Shape | V2 Shape |
|-----------|----------|----------|
| User | Auth + preferences + AI config | Unchanged |
| Person | Contact card + goals + observations + career details | Stripped to: name, aliases, type. Everything else derived from captures. |
| Capture | Raw input with manual extraction confirmation | Raw input with auto-extraction. No confirmation step. |
| Initiative | Full living brief + milestones + pending updates + chat | Lightweight: name + auto-summary from captures. Auto-apply brief updates. |
| Commitment | Manual CRUD with status management | Auto-extracted with confidence ranking. Status management kept for marking complete/dismissed. |

### New capability: Browser Audio Capture

Ported from Praxis-note (same Angular + .NET stack):

- **AudioRecorderService**: Web Audio API capture with `getDisplayMedia()` (tab audio) + `getUserMedia()` (mic). Stereo mix: left channel = user, right channel = remote participants. Recovery logic with exponential backoff.
- **DeepgramTranscriptionService**: WebSocket streaming of PCM audio to Deepgram Nova-3. Multichannel mode for speaker separation. Interim + final transcript results.
- **Backend WebSocket relay**: `/api/transcription/stream` proxies between browser and Deepgram. Auth via session cookies.
- **AudioWorklet processor**: `audio-pcm-processor.js` for real-time Float32 to Int16 PCM conversion.

No raw audio stored. Only the transcript text enters the Capture pipeline.

### Reworked: Briefing Generation

V1 briefing pulls facts from Delegation, OneOnOne, Observation, and Goal repositories — all killed. V2 briefing is simpler and more powerful:

- Feed the AI all captures + extractions from the relevant time window (day or week)
- AI synthesises directly from transcript content
- No `BriefingFactsAssembler` needed — the raw material is richer than the structured facts it was assembling
- Two briefing types: Daily (yesterday's captures, today's commitments) and Weekly (full week synthesis)
- OneOnOnePrep briefing type becomes: "show me everything about [person] from the last 2 weeks" — a People Dossier query, not a separate briefing type

### Reworked: AI Extraction

V1 extraction required manual confirmation before spawning commitments. V2 extraction:

- Auto-applies with no confirmation step
- Extracts: people mentions (with alias resolution), commitments (with confidence score), decisions, risks, initiative tags
- Commitment confidence scoring: HIGH (explicit promise + person + deadline), MEDIUM (clear intent, fuzzy details), LOW (mentioned but ambiguous)
- Links captures to people and initiatives automatically based on extracted mentions
- Extraction remains a Value Object inside the Capture aggregate (not promoted to separate aggregate)

### Reworked: People Dossier (formerly People Lens)

The existing People Lens is a summary view. The V2 People Dossier is the **primary view** of the app:

- For each Person, query all Captures where they're mentioned (as participant or topic)
- AI synthesises: recent context, performance signals, contradictions across sources, open commitments to/from, relationship dynamics
- Pre-meeting prep: "You're meeting [person] in 30 minutes. Here's what's happened since your last meeting across 4 conversations."
- Name resolution: Person entity holds canonical name + aliases. AI learns new aliases over time. Transcription variations of the same name all resolve to one Person.

### Reworked: Initiative (Lightweight)

V1 Initiative has a full Living Brief with pending updates, milestones, risks, decisions, design snapshots, and requirements. V2:

- Name + status + auto-generated summary from captures
- AI auto-applies brief updates (no pending queue, no approve/reject)
- Milestones removed — if they matter, they appear in transcripts
- Initiative-scoped chat removed
- Initiatives function primarily as **tags** that the AI auto-applies to captures

### Reworked: Dashboard

V1 dashboard has 5 widgets pulling from multiple killed aggregates. V2 dashboard is the landing page with:

- **Daily Brief** — generated from yesterday's captures. What happened, what's due, fresh commitments.
- **Open Commitments** — ranked by urgency (overdue first, then by due date)
- **People Quick Access** — list of people with recent activity indicators

## Capabilities

### New Capabilities

- `browser-audio-capture`: Real-time meeting recording via Web Audio API with Deepgram transcription. Stereo capture (mic + tab audio), WebSocket relay, AudioWorklet PCM processing. Produces transcript text that feeds the standard Capture pipeline.
- `people-dossier`: Cross-transcript synthesis per Person. Queries all captures mentioning a person, AI-generates a dossier with recent context, signals, contradictions, and open commitments. Includes pre-meeting prep mode.
- `commitment-auto-tracker`: AI-extracted commitments with confidence ranking (high/medium/low). Auto-created from transcript analysis. Bidirectional tracking (mine-to-them, theirs-to-me). Dismissable low-confidence items.
- `daily-brief`: AI synthesis of the previous day's captures. Surfaces fresh commitments, decisions, and items due today.
- `weekly-brief`: AI synthesis of the full week's captures. Patterns, open threads, time allocation analysis, cross-conversation insights.
- `surgical-removal`: Coordinated deletion of 7 aggregates (Delegation, Goal, Observation, OneOnOne, Interview, Nudge, ChatThread) and all dependent code across every layer. Includes database migration to drop tables.
- `transcript-daily-ingest`: Enhanced bulk upload UX for daily transcript dumps. Drag-drop multiple .docx/.txt/.html files, auto-detect format, auto-process. Replaces the weekly rhythm with daily.

### Modified Capabilities

- `capture-text`: Quick Capture gains voice-to-text mode (Deepgram via existing WebSocket relay). Mic button alongside text input. Transcript auto-created on stop.
- `capture-ai-extraction`: Extraction becomes fully automatic (no confirmation step). Adds confidence scoring for commitments. Adds initiative auto-tagging. Extraction remains VO inside Capture.
- `person-management`: Person entity stripped to name, aliases, type. Career details, candidate details, pipeline status removed. Alias management added.
- `initiative-management`: Stripped to name, status, auto-summary. Milestones, living brief pending updates, and initiative chat removed. Brief auto-applies.
- `initiative-living-brief`: Simplified to auto-apply mode only. No pending updates queue, no approve/reject flow. AI updates the summary directly from linked captures.
- `commitment-tracking`: Manual creation removed. Status management (complete, dismiss, reopen) retained. Confidence field added. Auto-linked to person and initiative from extraction.
- `daily-weekly-briefing`: Rewritten to synthesise directly from captures + extractions instead of assembling structured facts from killed aggregates.
- `people-lens`: Evolved into the People Dossier (new capability). The lens concept expands from a summary widget to the primary per-person view.

### Removed Capabilities

- `delegation-tracking`: Subsumed by commitment-auto-tracker
- `interview-tracking`: Out of scope — lives in recruiting tools
- `nudges-rhythms`: Subsumed by daily-brief
- `global-ai-chat`: Removed — external AI tools serve this need
- `initiative-ai-chat`: Removed — initiative context available through dossier/brief
- `daily-close-out`: Removed — no manual confirmation needed
- `my-queue`: Subsumed by daily-brief and commitment tracker
- `capture-audio` (V1 shape): Replaced by browser-audio-capture with Deepgram streaming

## Impact

- **Tier:** This is a product pivot, not an incremental tier. It touches every layer but preserves the core infrastructure (auth, AI provider, PATs, capture pipeline, deployment).
- **Aggregates affected:**
  - **Deleted (7):** Delegation, Goal, Observation, OneOnOne, Interview, Nudge, ChatThread
  - **Reshaped (4):** Person (stripped), Initiative (lightweight), Capture (auto-extraction), Commitment (auto-created + confidence)
  - **Unchanged (2):** User, PersonalAccessToken
- **Backend:**
  - Delete ~100 files across Domain, Application, Infrastructure layers for 7 killed aggregates
  - New: Deepgram WebSocket relay endpoint, AudioWorklet processor
  - Rework: BriefingService (direct transcript synthesis), extraction pipeline (auto-apply + confidence), Person aggregate (aliases), Initiative aggregate (auto-brief)
  - New migration: drop 7 aggregate tables, add commitment confidence column, add person aliases column, remove person career/candidate fields
- **Frontend:**
  - Delete ~30 files (killed feature pages, services, models)
  - New: AudioRecorderService, DeepgramTranscriptionService (ported from Praxis-note), People Dossier page, reworked Dashboard
  - Rework: Quick Capture (add voice mode), Commitment list (auto-populated, confidence UI), Person detail (dossier view), Initiative detail (simplified), Briefing pages (daily + weekly)
  - Remove routes for: delegations, goals, observations, one-on-ones, interviews, global-chat
- **Dependencies:**
  - NuGet: None new (Deepgram accessed via WebSocket, no SDK needed)
  - npm: None new (Web Audio API is browser-native)
  - External service: Deepgram API key required for audio transcription (user-configured alongside existing AI provider)
- **Tests:**
  - Delete ~15 test files for killed aggregates
  - New: AudioRecorderService tests, Deepgram WebSocket relay tests, auto-extraction confidence tests, People Dossier synthesis tests, daily/weekly brief generation tests
  - Rework: Briefing tests (new data sources), extraction tests (auto-apply + confidence)

## Non-goals

- **No mobile app.** Browser-only for V2. PWA consideration deferred.
- **No Google Drive API integration.** By design — the user's corporate Drive is locked down. Transcripts are exported and uploaded manually (daily drag-drop) or captured via browser audio.
- **No speaker diarization from transcript text.** Google's transcripts already have speaker labels. Deepgram provides diarization for live recordings. No additional NLP needed.
- **No calendar integration.** The user knows their schedule. The app doesn't need to.
- **No real-time collaboration.** Single-user tool by design.
- **No historical data migration.** V1 data in killed aggregate tables will be dropped. The user starts fresh with V2 by uploading transcripts going forward.
- **No Chrome extension.** Browser audio capture uses `getDisplayMedia()` which is available without an extension. The bookmarklet (already shipped) handles Google Docs text extraction.
- **No Whisper/local transcription.** Deepgram is the transcription provider for V2 (proven in Praxis-note, supports multichannel, streaming, and diarization).
- **No manual commitment creation.** Commitments are AI-extracted only. If the AI misses one, the user can note it via Quick Capture and it will be extracted.
- **No manual initiative creation retained as primary flow.** Initiatives can still be seeded manually (name only), but the expectation is that most emerge from AI tagging of captures.
- **No separate audio file storage.** Consistent with Praxis-note: only transcript text is stored. Raw audio is discarded after transcription.
