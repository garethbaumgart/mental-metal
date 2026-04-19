## Why

When a user views a commitment and clicks the source capture link, they land on the full transcript page with no indication of where the commitment was mentioned. For long meeting transcripts (30+ minutes), finding the relevant passage is tedious. Users need to jump directly to the exact moment a commitment was made so they can review surrounding context — who said what, what conditions were discussed, and what was agreed.

## What Changes

- **AI extraction prompt**: Request character-offset position data (`startOffset`, `endOffset`) for each extracted commitment, referencing the `RawContent` string that's sent to the AI
- **Domain model**: Add `SourceStartOffset` and `SourceEndOffset` (nullable int) to the `Commitment` aggregate and `ExtractedCommitment` value object
- **API response**: Include `sourceStartOffset` and `sourceEndOffset` in `CommitmentResponse`
- **Commitment detail link**: Pass offset as query params when navigating to the capture page (e.g., `/capture/:id?highlightStart=1234&highlightEnd=1456`)
- **Capture detail page**: Read query params, scroll to the highlighted region, and apply a highlight style to the relevant text span
- **Transcript viewer**: For audio captures, map character offsets to the correct transcript segment and highlight within it

## Non-goals

- Highlighting multiple commitments simultaneously on one transcript page
- Editing or correcting the extracted position after the fact
- Word-level or token-level precision — character offsets at sentence boundaries are sufficient
- Retroactively populating offsets for existing commitments (they'll remain `null`)

## Capabilities

### New Capabilities

- `commitment-source-highlight`: Navigating from a commitment to its source capture scrolls to and highlights the relevant text passage using character offsets stored during AI extraction

### Modified Capabilities

- `capture-ai-extraction`: The AI extraction prompt must request character offsets for each extracted commitment, and the extraction pipeline must persist them on the spawned Commitment entity
- `commitment-tracking`: The Commitment aggregate gains optional `SourceStartOffset` / `SourceEndOffset` fields, and the API response includes them

## Impact

- **Domain**: `Commitment` aggregate — 2 new nullable int properties
- **Application**: `ExtractionPromptBuilder` — updated system prompt; `ExtractionResponseDto` — new fields; `AutoExtractCaptureHandler` — passes offsets to commitment creation; `CommitmentDtos` — new response fields
- **Infrastructure**: New EF Core migration for `SourceStartOffset` / `SourceEndOffset` columns
- **Frontend**: `commitment-detail` component — query params on source link; `capture-detail` component — highlight logic; `transcript-viewer` component — segment-level highlight mapping
- **Aggregates affected**: `Commitment`, `Capture` (extraction pipeline only, no schema change on Capture)
