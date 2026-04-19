## Architecture Overview

V2 preserves Clean Architecture layers (Domain -> Application -> Infrastructure -> Web) and the vertical-slice handler pattern. The change is what lives in each layer, not how the layers relate.

```
┌─────────────────────────────────────────────────────────────────────┐
│                         BROWSER CLIENT                              │
│                                                                     │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐             │
│  │  Transcript   │  │   Audio      │  │  Quick Note  │             │
│  │  Upload       │  │   Capture    │  │  (voice/text)│             │
│  │  (drag-drop)  │  │  (Web Audio) │  │              │             │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘             │
│         │                  │                  │                      │
│         │     ┌────────────┘                  │                      │
│         │     │ WebSocket (PCM audio)         │                      │
│         │     │                               │                      │
│  ┌──────▼─────▼───────────────────────────────▼──────┐             │
│  │              Capture Ingestion Layer               │             │
│  │  POST /api/captures/import (files, JSON, PAT)     │             │
│  │  POST /api/captures (typed text)                   │             │
│  │  WS /api/transcription/stream (audio -> Deepgram)  │             │
│  └──────────────────────┬────────────────────────────┘             │
│                          │                                          │
│  ┌───────────────────────▼──────────────────────────────┐          │
│  │                    OUTPUT VIEWS                        │          │
│  │                                                       │          │
│  │  ┌──────────────┐  ┌───────────┐  ┌──────────────┐  │          │
│  │  │   People     │  │Commitments│  │  Daily/Weekly │  │          │
│  │  │   Dossier    │  │  Tracker  │  │    Brief     │  │          │
│  │  └──────────────┘  └───────────┘  └──────────────┘  │          │
│  └───────────────────────────────────────────────────────┘          │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                          BACKEND (.NET 10)                          │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  Web Layer (Minimal APIs)                                    │   │
│  │  • CaptureEndpoints (import, create, list, get)             │   │
│  │  • TranscriptionEndpoints (WebSocket relay to Deepgram)     │   │
│  │  • PeopleEndpoints (list, get, aliases)                     │   │
│  │  • InitiativeEndpoints (list, get, create)                  │   │
│  │  • CommitmentEndpoints (list, get, complete, dismiss)       │   │
│  │  • BriefingEndpoints (daily, weekly, person-prep)           │   │
│  │  • UserEndpoints (auth, profile, AI config)                 │   │
│  │  • PersonalAccessTokenEndpoints                             │   │
│  └────────────────────────────┬────────────────────────────────┘   │
│                               │                                     │
│  ┌────────────────────────────▼────────────────────────────────┐   │
│  │  Application Layer (Handlers)                                │   │
│  │  • Capture: Create, Import, Process (auto-extract)          │   │
│  │  • Extraction: AutoExtract, ScoreCommitments, ResolveNames  │   │
│  │  • Briefing: GenerateDaily, GenerateWeekly, PersonPrep      │   │
│  │  • People: CRUD + GetDossier (cross-capture synthesis)      │   │
│  │  • Initiative: CRUD + AutoRefreshBrief                      │   │
│  │  • Commitment: List, Complete, Dismiss, Reopen              │   │
│  └────────────────────────────┬────────────────────────────────┘   │
│                               │                                     │
│  ┌────────────────────────────▼────────────────────────────────┐   │
│  │  Domain Layer                                                │   │
│  │  • User, Person, Capture, Initiative, Commitment             │   │
│  │  • PersonalAccessToken                                       │   │
│  │  • Value Objects: AiExtraction, CommitmentConfidence,        │   │
│  │    PersonAlias, InitiativeBrief                              │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  Infrastructure Layer                                        │   │
│  │  • EF Core + PostgreSQL (5 aggregate tables + PATs)         │   │
│  │  • AI Provider Adapters (Anthropic, Google, OpenAI)         │   │
│  │  • Deepgram WebSocket Client                                │   │
│  │  • Transcript Parsers (docx, html, txt)                     │   │
│  │  • Token/Auth services                                      │   │
│  └─────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│  External Services                                                  │
│  • Deepgram (real-time transcription, Nova-3)                      │
│  • User-configured AI provider (extraction, briefing, dossier)     │
│  • PostgreSQL                                                       │
└─────────────────────────────────────────────────────────────────────┘
```

## Domain Model

### Aggregates (6 total)

```
User (unchanged)
├── Id, Email, PasswordHash
├── AiProviderConfig (VO)
├── UserPreferences (VO)
└── RefreshTokens

Person (reshaped)
├── Id, UserId
├── CanonicalName: string
├── Aliases: List<string>        ← NEW (for transcript name resolution)
├── Type: PersonType (direct-report | peer | stakeholder | external)
└── (CareerDetails, CandidateDetails, PipelineStatus REMOVED)

Capture (reshaped)
├── Id, UserId
├── Content: string (raw transcript or note text)
├── Type: CaptureType (transcript | quick-note | meeting-recording)
├── Source: CaptureSource (upload | bookmarklet | audio-capture | typed | voice)  ← NEW
├── Title: string?
├── CapturedAt: DateTimeOffset
├── ProcessingStatus: (raw | processing | processed | failed)
├── AiExtraction: AiExtractionV2 (VO)        ← RESHAPED
│   ├── PeopleMentioned: List<PersonMention>
│   │   └── PersonMention: { PersonId?, RawName, Context }
│   ├── Commitments: List<ExtractedCommitment>
│   │   └── ExtractedCommitment: { Description, Direction, PersonId?,
│   │       DueDate?, Confidence: high|medium|low }
│   ├── Decisions: List<string>
│   ├── Risks: List<string>
│   ├── InitiativeTags: List<string>          ← NEW (auto-detected initiative names)
│   └── Summary: string
├── LinkedPersonIds: List<Guid>               (auto-linked from extraction)
└── LinkedInitiativeIds: List<Guid>           (auto-linked from extraction)

Initiative (reshaped)
├── Id, UserId
├── Title: string
├── Status: InitiativeStatus (active | on-hold | completed | cancelled)
├── AutoSummary: string?         ← NEW (AI-generated, auto-updated)
└── (Milestones, LivingBrief pending updates, Chat REMOVED)
    (LivingBrief simplified to just AutoSummary — auto-applied, no approval)

Commitment (reshaped)
├── Id, UserId
├── Description: string
├── Direction: CommitmentDirection (mine-to-them | theirs-to-me)
├── PersonId: Guid?
├── InitiativeId: Guid?
├── SourceCaptureId: Guid         ← NEW (trace back to the transcript)
├── DueDate: DateTimeOffset?
├── Confidence: CommitmentConfidence (high | medium | low)  ← NEW
├── Status: CommitmentStatus (open | completed | dismissed | cancelled)
│                                   └── dismissed is NEW (for false-positive AI extractions)
└── CreatedAt, CompletedAt

PersonalAccessToken (unchanged)
├── Id, UserId, Name, TokenHash
├── Scopes, CreatedAt, LastUsedAt, RevokedAt
```

### Removed from Domain

```
DELETED AGGREGATES:
  Delegation (entire folder)
  Goal (entire folder)
  Observation (entire folder)
  OneOnOne (entire folder)
  Interview (entire folder)
  Nudge (entire folder)
  ChatThread (entire folder)

DELETED FROM INITIATIVE:
  Milestone (entity)
  PendingBriefUpdate (entity + repository)
  LivingBrief complex VO (KeyDecision, Risk, RequirementsSnapshot, etc.)

DELETED FROM PERSON:
  CareerDetails (VO)
  CandidateDetails (VO)
  PipelineStatus (enum)

DELETED FROM USER:
  DailyCloseOutLog (VO)

DELETED READ MODELS:
  Briefing aggregate (replaced by on-demand generation)
```

## Data Flow: Capture to Intelligence

```
1. INGEST
   User uploads transcript / records meeting / captures note
          │
          ▼
2. CAPTURE CREATED (status: raw)
   Stored in Captures table with content, source, timestamp
          │
          ▼
3. AUTO-EXTRACTION (status: processing → processed)
   AI analyses the raw content and produces AiExtractionV2:
   • Identifies people mentioned (with fuzzy name matching to Person.Aliases)
   • Extracts commitments with confidence scores
   • Identifies decisions and risks
   • Tags relevant initiatives
   • Generates a capture summary
          │
          ├──→ AUTO-LINK to People (by resolved PersonId)
          ├──→ AUTO-LINK to Initiatives (by matched tag)
          ├──→ AUTO-CREATE Commitments (high + medium confidence)
          │    Low confidence stored in extraction but not spawned as Commitments
          └──→ AUTO-UPDATE Initiative summaries (if linked)
          
4. INTELLIGENCE GENERATION (on-demand)
   When user opens a view:
   
   People Dossier:
   ├── Query: all Captures linked to PersonId, ordered by date
   ├── AI synthesises: context, signals, contradictions, open commitments
   └── Cached briefly (TTL: 1 hour or until new capture for this person)
   
   Daily Brief:
   ├── Query: all Captures from yesterday + open Commitments due today
   ├── AI synthesises: what happened, what's due, fresh commitments
   └── Generated once per day, re-generable on demand
   
   Weekly Brief:
   ├── Query: all Captures from last 7 days + all open Commitments
   ├── AI synthesises: patterns, open threads, time allocation, cross-conversation insights
   └── Generated once per week, re-generable on demand
```

## Browser Audio Capture: Technical Design

Ported from Praxis-note with minimal adaptation.

```
BROWSER                                    BACKEND                    DEEPGRAM
──────                                    ───────                    ────────

┌─────────────────┐
│ User clicks      │
│ "Record Meeting" │
└────────┬────────┘
         │
         ▼
┌─────────────────────────┐
│ getDisplayMedia()        │──── Tab audio (remote participants)
│ getUserMedia()           │──── Mic audio (local user)
│                          │
│ ChannelMerger:           │
│   Left  = mic            │
│   Right = tab audio      │
│                          │
│ MediaRecorder → blob     │ (browser playback, not stored)
│ AudioWorklet → PCM Int16 │
└────────┬────────────────┘
         │ raw PCM binary (250ms chunks)
         │
         ▼
┌────────────────────────┐     WebSocket      ┌──────────────┐
│ WS /api/transcription/ │ ──────────────────▶ │  Deepgram    │
│ stream                 │                     │  Nova-3      │
│                        │ ◀────────────────── │              │
│ (auth via session      │  interim + final    │  Multichannel│
│  cookie)               │  transcript results │  diarization │
└────────┬───────────────┘                     └──────────────┘
         │
         │ final transcript text
         ▼
┌─────────────────────────┐
│ POST /api/captures       │
│ { type: "meeting-       │
│   recording",            │
│   content: transcript,   │
│   source: "audio-        │
│   capture" }             │
└──────────────────────────┘
         │
         ▼
    Normal extraction pipeline
```

### Recovery & Resilience (from Praxis-note)

- Max 5 audio recovery attempts per 60-second window
- Auto-reconnect mic if track ends unexpectedly
- Falls back to mic-only if system audio lost
- Deepgram WebSocket: max 10 reconnect attempts per session, exponential backoff 500ms → 15s
- Audio buffered during WebSocket reconnect, flushed on restore

### Deepgram Configuration

```
Provider: Deepgram (user adds API key in Settings, alongside AI provider)
Model: Nova-3
Encoding: linear16 (Int16 PCM, little-endian)
Sample rate: 48000 (browser default)
Channels: 2 (multichannel mode when tab + mic)
Features: interim_results, punctuate, diarize (single-channel fallback)
```

## Commitment Confidence Model

```
HIGH CONFIDENCE — explicit promise + identifiable person + time signal
  Signal words: "I will", "I'll", "I promise", "I commit to", "by tomorrow",
                "by [date]", "I'll get back to you"
  Example: "I'll send you the updated spec by end of day tomorrow"
  → Auto-created as Commitment, shown prominently in daily brief

MEDIUM CONFIDENCE — clear intent but missing person or deadline
  Signal words: "I need to", "I should", "let me", "I want to",
                "we should", "I'll have a look"
  Example: "I'll review the migration plan and get back to you"
  → Auto-created as Commitment, shown in commitment list (not brief headline)

LOW CONFIDENCE — mentioned but ambiguous, may be conversational
  Signal words: "we could", "maybe I'll", "it would be good to",
                "I'm thinking about"
  Example: "We should probably revisit the testing strategy at some point"
  → Stored in extraction JSON only, NOT created as Commitment entity
  → Visible in People Dossier context if relevant
```

## Person Name Resolution

```
SEEDED PEOPLE (user creates once):
  Person { name: "Alice Johnson", aliases: ["Ali", "AJ"] }
  Person { name: "Bob Smith", aliases: ["Bobby", "BS"] }
  Person { name: "Charlie Park", aliases: ["CP", "Chuck"] }

EXTRACTION FLOW:
  1. AI extracts raw names from transcript: ["Ali", "Bob", "CP"]
  2. For each name, fuzzy-match against Person.CanonicalName + Person.Aliases
  3. If match found → link Capture to Person, annotate extraction with PersonId
  4. If no match → store raw name in extraction, surface as "unresolved mention"
  5. User can resolve unresolved mentions → creates new Person or adds alias

ALIAS LEARNING:
  When user resolves "Chuck" → Person "Charlie Park", system adds "Chuck" to aliases.
  Future extractions auto-resolve "Chuck" without user intervention.
```

## UI Structure

```
NAVIGATION (simplified):
┌─────────────────────────────────────┐
│  ┌─────────┐                        │
│  │ Mental  │  Dashboard  People     │
│  │ Metal   │  Captures   Commits    │
│  │         │  Initiatives Settings  │
│  └─────────┘                        │
└─────────────────────────────────────┘

DASHBOARD (landing page):
┌─────────────────────────────────────────────────┐
│  Daily Brief                          [Refresh] │
│  ─────────────────────────────────────────────── │
│  Yesterday: 6 meetings, 3 quick notes           │
│  • You told Alice you'd share the API spec...   │
│  • Migration timeline confirmed: June go-live   │
│  • Q3 roadmap review flagged 2 scope risks      │
│                                                  │
│  Due Today                                       │
│  ─────────────────────────────────────────────── │
│  ● HIGH: Share API spec with Alice              │
│  ● HIGH: Review Charlie's architecture proposal │
│  ○ MED:  Update sprint planning board           │
│                                                  │
│  [View Weekly Brief]                             │
└─────────────────────────────────────────────────┘

PEOPLE page:
┌─────────────────────────────────────────────────┐
│  People                            [Add Person] │
│  ─────────────────────────────────────────────── │
│  Alice Johnson      ●● 5 mentions this week     │
│  Bob Smith          ●● 4 mentions this week     │
│  Charlie Park       ●● 3 mentions this week     │
│  Dana Lee           ●  2 mentions this week     │
│  Eve Torres         ●  2 mentions this week     │
│  Frank Chen         ●  1 mention this week      │
└─────────────────────────────────────────────────┘

PERSON DOSSIER (click into a person):
┌─────────────────────────────────────────────────┐
│  ← Charlie Park                     direct-report│
│  Aliases: Charlie, CP               [edit]      │
│  ─────────────────────────────────────────────── │
│  SYNTHESIS (AI-generated)                        │
│  Strong delivery on the migration project.       │
│  Alice praised his technical design in Tuesday's │
│  standup, but Bob raised concerns about          │
│  communication gaps with the QA team in your     │
│  Thursday 1:1. You mentioned coaching him on     │
│  stakeholder updates in last week's retro.       │
│                                                  │
│  OPEN COMMITMENTS                                │
│  → You: Review Charlie's architecture proposal   │
│  → Charlie: Send updated test plan by Friday     │
│                                                  │
│  TRANSCRIPT MENTIONS (4 this week)               │
│  Thu — Bob/You 1:1  [view]                      │
│  Wed — Team standup  [view]                     │
│  Tue — Alice/You sync  [view]                   │
│  Mon — Sprint planning  [view]                  │
└─────────────────────────────────────────────────┘

CAPTURES page:
┌─────────────────────────────────────────────────┐
│  Captures                [Upload] [Record] [+]  │
│  ─────────────────────────────────────────────── │
│  Today                                           │
│  📄 Alice/You sync — 10:00       2 commits      │
│  📄 Sprint planning — 14:00      0 commits      │
│  📄 Bob/You 1:1 — 15:30         3 commits      │
│  🎙️ Quick note — 16:45           0 commits      │
│                                                  │
│  Yesterday                                       │
│  📄 Team standup — 09:15         1 commit       │
│  📄 Design review — 11:00        2 commits      │
│  ...                                            │
└─────────────────────────────────────────────────┘

Quick Capture (FAB or keyboard shortcut):
┌─────────────────────────────────────────────────┐
│  Quick Note                              [×]    │
│  ─────────────────────────────────────────────── │
│  [🎤 Voice]  [⌨️ Type]                          │
│                                                  │
│  ┌───────────────────────────────────────────┐  │
│  │ Just spoke to Alice, she confirmed the    │  │
│  │ vendor demo is moving to next Thursday.   │  │
│  │ Need to update the stakeholder deck.      │  │
│  └───────────────────────────────────────────┘  │
│                                          [Save] │
└─────────────────────────────────────────────────┘
```

## Database Migration Strategy

Since V1 migrations are merged to main (immutable per CLAUDE.md), we create a **new forward migration** that:

1. Drops tables: `delegations`, `goals`, `goal_check_ins`, `observations`, `one_on_ones`, `action_items`, `follow_ups`, `interviews`, `interview_scorecards`, `nudges`, `nudge_cadences`, `chat_threads`, `chat_messages`, `context_scopes`, `source_references`, `briefings`, `pending_brief_updates`
2. Drops columns from `people`: career-detail and candidate-detail fields, pipeline_status
3. Adds columns to `people`: `aliases` (jsonb array)
4. Adds columns to `captures`: `source` (enum), modifies `ai_extraction` JSON schema
5. Adds columns to `commitments`: `confidence` (enum), `source_capture_id` (FK), `dismissed_at`
6. Strips `initiatives` table: drops milestone columns, simplifies brief to `auto_summary` text column
7. Drops `daily_close_out_logs` from `users`

**No data archival.** V1 data in killed tables is development/test data only. Clean drop.

## Key Technical Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Extraction lifecycle | VO in Capture, not separate aggregate | No independent lifecycle. Born with capture processing, never changes independently. Simpler. |
| Commitment creation | Auto-created for high + medium confidence | Low confidence too noisy. High + medium gives useful signal. User dismisses false positives. |
| Initiative brief updates | Auto-apply, no approval queue | Approval was friction with no clear value. If AI gets it wrong, user sees it in the brief and captures correct it over time. |
| Briefing generation | On-demand from raw captures | Richer than structured facts. The AI reads the actual transcript, not a lossy summary of it. |
| Audio storage | None — transcript text only | Privacy, storage cost, and simplicity. Audio is ephemeral; transcript is the artifact. |
| Deepgram vs Whisper | Deepgram (streaming) | Real-time results during recording. Proven in Praxis-note. Multichannel support. |
| Person aliases | Manual seed + AI learning | User knows their 15 key people. AI handles variations. Hybrid gives accuracy without setup friction. |
| Daily + Weekly brief | Both, not either/or | Daily keeps commitments fresh. Weekly gives the patterns-across-conversations insight that's the unique value prop. |
