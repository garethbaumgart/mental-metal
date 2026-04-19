## Context

Commitments are extracted from captures (transcripts and quick notes) by the AI extraction pipeline (`AutoExtractCaptureHandler`). The pipeline sends the capture's `RawContent` to the AI provider and receives structured JSON back. Currently, extracted commitments include description, direction, person, due date, and confidence — but no reference to *where* in the source text the commitment appears.

The `Commitment` aggregate has a `SourceCaptureId` linking back to the originating capture. The commitment detail page renders a "View source capture" link that navigates to `/capture/:id`. The capture detail page renders `RawContent` as a `whitespace-pre-wrap` text block, and for audio captures, a `TranscriptViewerComponent` renders segments grouped by speaker with timecodes.

## Goals / Non-Goals

**Goals:**
- Store character offsets on extracted commitments so the source passage can be located
- Navigate from commitment detail to the capture page with the relevant text scrolled into view and highlighted
- Support both text captures (raw content view) and audio captures (transcript segment view)

**Non-Goals:**
- Retroactive offset population for existing commitments
- Multi-highlight (showing several commitments on one page)
- Editable offsets or manual position correction

## Dependencies

- `capture-ai-extraction` spec (existing) — extraction prompt and pipeline
- `commitment-tracking` spec (existing) — commitment aggregate and API
- `capture-text` spec (existing) — capture detail page

## Decisions

### 1. Character offsets on `RawContent`, not segment indices

**Decision**: Store `SourceStartOffset` and `SourceEndOffset` as nullable ints on the `Commitment` aggregate, representing character positions within the capture's `RawContent` string.

**Why**: The AI receives the full `RawContent` as input, so character offsets in that string are the natural unit the AI can produce. For audio captures, `RawContent` is built by concatenating segment texts — so character offsets can be mapped back to segments at display time by computing cumulative segment lengths.

**Alternative considered**: Store `SourceSegmentId` referencing a specific `TranscriptSegment`. Rejected because (a) text captures don't have segments, (b) the AI prompt receives `RawContent` not individual segments, and (c) a commitment may span multiple segments.

### 2. AI prompt change — request `source_start_offset` and `source_end_offset`

**Decision**: Add two integer fields to the commitment extraction schema in `ExtractionPromptBuilder`. The system prompt will instruct the AI to return character offsets referencing the user message text (which is the `RawContent`).

**Prompt addition**:
```
For each commitment, include:
- source_start_offset: character index where this commitment starts in the input text (0-based)
- source_end_offset: character index where this commitment ends in the input text (exclusive)
```

**Why**: LLMs can reliably return approximate character offsets when explicitly asked. Exact precision isn't required — being within a sentence of the actual location is sufficient for scrolling and highlighting.

**Trade-off**: AI-produced offsets may occasionally be slightly inaccurate. This is acceptable because (a) the highlight is a visual aid, not a contract, and (b) we clamp offsets to valid `RawContent` bounds to prevent errors.

### 3. Query params for navigation, not route fragments

**Decision**: Pass highlight position via query params: `/capture/:id?highlightStart=N&highlightEnd=M`.

**Why**: Query params are easily read by Angular's `ActivatedRoute` and don't interfere with the page's scroll behavior. Route fragments (`#`) would require anchor elements at arbitrary text positions, which is more complex.

**Alternative considered**: Route fragment (`#highlight-1234`). Rejected because it requires injecting anchor elements at dynamic positions in the transcript text, and doesn't cleanly support the start+end range.

### 4. Frontend highlight approach — text splitting

**Decision**: When `highlightStart` and `highlightEnd` query params are present on the capture detail page:

**For text captures** (`RawContent` view):
- Split `RawContent` into three parts: before, highlighted, after
- Render with a `<mark>` element around the highlighted portion
- After render, use `scrollIntoView({ behavior: 'smooth', block: 'center' })` on the `<mark>` element

**For audio captures** (transcript viewer):
- Compute cumulative character offsets across segments (since `RawContent` = concatenation of segment texts with separators)
- Determine which segment(s) overlap with `[highlightStart, highlightEnd)`
- Pass highlight range to `TranscriptViewerComponent` via input signals
- The component marks the overlapping text within the relevant segment(s) and scrolls to it

**Why**: This approach keeps the highlight logic entirely in the frontend — no backend changes needed for the capture page. The `<mark>` HTML element is semantic, accessible, and stylable via PrimeNG tokens.

### 5. Highlight styling

**Decision**: Use a `<mark>` element styled with PrimeNG tokens:
```css
mark.source-highlight {
  background: color-mix(in srgb, var(--p-yellow-100) 70%, transparent);
  border-bottom: 2px solid var(--p-yellow-500);
  padding: 1px 2px;
  border-radius: 2px;
}
```

**Why**: Consistent with the app's design system. The `color-mix` approach matches the pattern used for overdue highlights in the commitments list. Yellow is the conventional highlight colour.

### 6. Domain model — value object placement

**Decision**: Add `SourceStartOffset` and `SourceEndOffset` directly on the `Commitment` aggregate root as nullable int properties (not a separate value object).

**Why**: Two nullable ints don't warrant a value object. They're set once at creation time and never change. This keeps the aggregate simple. The `ExtractedCommitment` value object on `AiExtraction` will also gain these fields to carry them through the extraction pipeline.

## Risks / Trade-offs

- **[AI offset accuracy]** → LLMs may produce approximate offsets. **Mitigation**: Clamp offsets to valid bounds (`0` to `RawContent.Length`). The highlight is a visual aid — being off by a few characters is acceptable. If offsets are null or clearly invalid (start >= end), skip highlighting gracefully.
- **[RawContent vs segment text mismatch]** → For audio captures, `RawContent` may not be a simple concatenation of segment texts (could have formatting differences). **Mitigation**: Use `RawContent` as the canonical source for both AI extraction and highlight mapping. The transcript viewer highlight maps offsets by reconstructing the concatenation order.
- **[Breaking change risk]** → None. New fields are nullable, existing commitments will have `null` offsets and the UI gracefully degrades (no highlight, same behavior as today).

## Migration Plan

1. Add EF Core migration with two nullable int columns on `Commitments` table
2. Deploy backend — existing data unaffected (null values)
3. Deploy frontend — capture detail page reads query params; if absent, no highlighting (backwards compatible)
4. New extractions going forward will populate offsets

## Open Questions

- Should we validate offsets against `RawContent.Length` at creation time in the domain, or just clamp in the UI? (Recommendation: clamp in UI — simpler, and the domain shouldn't need to load the capture's content to validate.)
