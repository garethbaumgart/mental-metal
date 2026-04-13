# My Work Companion — Spec Plan

Specs organised by tier. Dependencies flow downward — Tier 2 depends on Tier 1, Tier 3 depends on Tier 2.

---

## Tier 1 — Foundation

| Spec | Scope | Key Aggregates |
|------|-------|----------------|
| `user-auth-tenancy` | Registration, OAuth, multi-tenant scoping, user preferences | User |
| `ai-provider-abstraction` | BYO provider config, key storage, provider interface, model selection | User (AiProviderConfig) |
| `person-management` | Create/edit people, types (report/stakeholder/candidate), career details, team assignment | Person |
| `initiative-management` | Create/edit initiatives, basic metadata, status, milestones | Initiative |

## Tier 2 — Core Capabilities

| Spec | Scope | Key Aggregates | Depends On |
|------|-------|----------------|------------|
| `capture-text` | Quick capture input, pasted transcripts, raw storage, processing status lifecycle | Capture | user-auth-tenancy |
| `capture-ai-extraction` | AI processing pipeline: extract action items, decisions, commitments, risks. Auto-link to People and Initiatives | Capture, Commitment, Delegation, Observation | ai-provider-abstraction, capture-text, person-management, initiative-management |
| `commitment-tracking` | Bidirectional commitments, status lifecycle, overdue detection, nudges, linked to people/initiatives | Commitment | person-management, initiative-management |
| `delegation-tracking` | Assign work to people, status tracking, follow-up ownership, linked to initiatives | Delegation | person-management, initiative-management |
| `initiative-living-brief` | AI-maintained summary, key decisions log, risk tracking, requirements evolution, design direction evolution, auto-update from captures | Initiative | ai-provider-abstraction, capture-ai-extraction |
| `initiative-ai-chat` | Conversational AI scoped to an initiative's knowledge base, source referencing | ChatThread, Initiative | ai-provider-abstraction, initiative-living-brief |
| `people-lens` | 1:1 records, observations, goals, delegation view per person, performance evidence accumulation | OneOnOne, Observation, Goal | person-management, commitment-tracking, delegation-tracking |
| `global-ai-chat` | Context-aware AI chat available everywhere, scopes dynamically, queries across all data | ChatThread | ai-provider-abstraction, initiative-ai-chat |

## Tier 3 — Enhancements

| Spec | Scope | Key Aggregates | Depends On |
|------|-------|----------------|------------|
| `daily-weekly-briefing` | Morning briefing generation, weekly overview, 1:1 prep sheets, context pre-loading | (BriefingService) | people-lens, commitment-tracking, delegation-tracking, initiative-living-brief |
| `my-queue` | Prioritised attention view, filtering, delegation suggestions | (QueuePrioritizationService) | commitment-tracking, delegation-tracking, capture-ai-extraction |
| `daily-close-out` | End-of-day triage flow, unprocessed capture review, quick confirm/reassign/discard | Capture | capture-ai-extraction |
| `interview-tracking` | Candidate pipeline, scorecards, transcript analysis, AI summary, recommendations | Interview | person-management, ai-provider-abstraction |
| `capture-audio` | Record/stop flow, transcription, speaker diarization, speaker identification, audio discard policy | Capture | capture-text, person-management |
| `nudges-rhythms` | Recurring reminders, linked to people/initiatives, schedule management | Nudge | person-management, initiative-management |
