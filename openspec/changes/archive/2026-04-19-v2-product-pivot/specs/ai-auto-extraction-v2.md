# AI Auto-Extraction V2

Rework the extraction pipeline to be fully automatic with no user confirmation step. Extraction auto-applies immediately after processing, spawning commitments, linking people and initiatives, and updating initiative summaries — all without waiting for user approval.

## Key Changes from V1

| Aspect | V1 | V2 |
|--------|----|----|
| Confirmation | Required — user confirms or discards extraction | None — auto-applied immediately |
| Commitment creation | User reviews extracted commitments | Auto-created for high/medium confidence |
| People linking | User confirms person links | Auto-linked via name resolution against Person aliases |
| Initiative linking | User manually links | Auto-tagged by AI, auto-linked to matching Initiatives |
| Extraction status | raw → processing → processed (awaiting confirm) → confirmed | raw → processing → processed (done) |

## Extraction Pipeline

### Trigger
Extraction is triggered when a Capture is created (from any input: upload, audio capture, quick note). No manual "process" button needed — processing begins automatically.

### Processing Steps

1. **Capture enters `processing` status**

2. **AI analyses raw content** with a structured prompt:
   - Extract people mentioned (raw name strings)
   - Extract commitments (description, direction, confidence, due date if mentioned)
   - Extract key decisions made
   - Extract risks or concerns raised
   - Identify initiative/project names mentioned
   - Generate a brief summary of the capture

3. **Name resolution** (see `person-v2` spec for algorithm):
   - For each extracted person name, attempt to resolve against existing Person entities
   - Resolved names → auto-link Capture to Person
   - Unresolved names → stored in extraction as unresolved mentions

4. **Initiative auto-tagging**:
   - For each extracted initiative/project name, fuzzy-match against existing Initiative titles
   - Matched → auto-link Capture to Initiative, queue initiative summary refresh
   - Unmatched → stored in extraction as unresolved initiative tags (visible in UI for user to create or link)

5. **Commitment spawning** (see `commitment-auto-tracker` spec):
   - For `high` and `medium` confidence commitments: create Commitment entity
   - Set `SourceCaptureId` to this capture
   - Link to resolved Person and Initiative where available
   - `low` confidence: store in extraction JSON only

6. **Initiative summary refresh**:
   - For each initiative newly linked to this capture, queue an auto-summary refresh
   - See `initiative-v2` spec for refresh process

7. **Capture enters `processed` status** — extraction complete, all side effects applied

### Failure Handling
- If AI call fails: Capture enters `failed` status
- Retry available via `POST /api/captures/{id}/retry`
- Failed captures surface in the capture list with a failure indicator
- No blocking — other captures continue processing independently

## Extraction Value Object (V2 Shape)

```
AiExtractionV2
├── Summary: string
├── PeopleMentioned: List<PersonMention>
│   └── PersonMention
│       ├── RawName: string (as it appeared in transcript)
│       ├── PersonId: Guid? (null if unresolved)
│       └── Context: string (sentence/paragraph where mentioned)
├── Commitments: List<ExtractedCommitment>
│   └── ExtractedCommitment
│       ├── Description: string
│       ├── Direction: CommitmentDirection
│       ├── PersonId: Guid? (resolved target/source)
│       ├── DueDate: DateTimeOffset?
│       ├── Confidence: CommitmentConfidence (high, medium, low)
│       └── SpawnedCommitmentId: Guid? (null for low confidence)
├── Decisions: List<string>
├── Risks: List<string>
├── InitiativeTags: List<InitiativeTag>
│   └── InitiativeTag
│       ├── RawName: string (as mentioned in transcript)
│       ├── InitiativeId: Guid? (null if unresolved)
│       └── Context: string
└── ExtractedAt: DateTimeOffset
```

## AI Prompt Design

The extraction prompt must:
- Be structured to return JSON matching the `AiExtractionV2` schema
- Include commitment confidence scoring guidance (high/medium/low criteria — see `commitment-auto-tracker` spec)
- Instruct the AI to preserve speaker attribution where available (Google Meet transcripts include speaker labels)
- Handle both Google Meet transcript format (speaker-labelled turns) and free-form text (quick notes)
- Not hallucinate people or commitments not present in the text
- Extract the user's own commitments (direction: mine-to-them) and commitments others made to the user (direction: theirs-to-me)

## API Changes

### Removed Endpoints
- `POST /api/captures/{id}/process` — processing is automatic on creation
- `POST /api/captures/{id}/confirm-extraction` — no confirmation step
- `POST /api/captures/{id}/discard-extraction` — no discard step
- `POST /api/captures/{id}/link-person` — auto-linked by extraction
- `POST /api/captures/{id}/unlink-person` — not needed (auto-linked)
- `POST /api/captures/{id}/link-initiative` — auto-linked by extraction
- `POST /api/captures/{id}/unlink-initiative` — not needed (auto-linked)

### Kept Endpoints
- `POST /api/captures/{id}/retry` — retry failed extraction
- `GET /api/captures/{id}` — returns capture with extraction results

### New Behavior
- `POST /api/captures` and `POST /api/captures/import` now auto-trigger extraction (no separate process call needed)
- Extraction results are part of the capture response — no separate endpoint needed

## Unresolved Mentions UI

When extraction finds names it cannot resolve to existing People:

1. Unresolved mentions shown in capture detail view with a "Resolve" action
2. User clicks "Resolve" → shown a picker: select existing Person (adds alias) or create new Person
3. On resolution: alias added to Person, capture re-linked, and optionally re-extract to catch related commitments

When extraction finds initiative names it cannot resolve:

1. Unresolved initiative tags shown in capture detail with a "Link" action
2. User clicks "Link" → select existing Initiative or create new one
3. On link: capture linked to initiative, initiative summary refresh queued

## Acceptance Criteria

- [ ] Extraction triggers automatically on capture creation (no manual process call)
- [ ] No confirmation/discard step exists in the pipeline
- [ ] Extraction produces `AiExtractionV2` JSON shape with all fields
- [ ] People mentions resolved against Person.CanonicalName + Person.Aliases
- [ ] Resolved people auto-linked to capture
- [ ] Unresolved people stored with raw name and context
- [ ] Initiative tags fuzzy-matched against existing Initiative titles
- [ ] Matched initiatives auto-linked to capture
- [ ] Unmatched initiative tags stored with raw name and context
- [ ] High + medium confidence commitments auto-created as Commitment entities
- [ ] Low confidence commitments stored in extraction JSON only
- [ ] Each spawned commitment has `SourceCaptureId` set
- [ ] Initiative auto-summary refresh queued when capture linked to initiative
- [ ] Failed extractions set capture to `failed` status with retry available
- [ ] Unresolved mentions surfaced in capture detail UI with resolve action
- [ ] Resolving a mention adds alias to Person and re-links capture
- [ ] Process/confirm/discard endpoints removed
- [ ] Manual link/unlink endpoints removed
- [ ] Domain tests cover extraction VO structure and commitment spawning logic
- [ ] Application tests cover name resolution, initiative matching, confidence filtering
- [ ] Integration tests cover end-to-end: create capture → extraction → commitments created → people linked
