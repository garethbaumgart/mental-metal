## Context

The Quick Capture dialog defaults all captures to `CaptureType.QuickNote`. The AI extraction pipeline (`AutoExtractCaptureHandler`) already performs deep content analysis but never classifies or reclassifies the capture type. Users who paste multi-speaker transcripts or structured meeting notes end up with mislabelled captures, making type-based filtering unreliable.

The existing extraction prompt instructs the AI to return a structured JSON response with summary, commitments, people, decisions, risks, and initiative tags. Adding a `detected_type` field to this response is a minimal change that piggybacks on the existing AI call with no additional cost or latency.

## Dependencies

- `capture-text` (Tier 2) -- the Capture aggregate and CaptureType enum
- `capture-ai-extraction` (Tier 2) -- the extraction pipeline, prompt builder, response DTO, and AiExtraction value object

## Goals / Non-Goals

**Goals:**
- Detect content type (`QuickNote`, `Transcript`, `MeetingNotes`) during AI extraction with zero additional AI calls
- Reclassify the capture's `CaptureType` when the detected type differs from the original
- Persist the detected type on the `AiExtraction` value object for audit/display purposes
- Surface the detected type in the frontend capture detail view

**Non-Goals:**
- Client-side heuristic detection before submission
- User override UI for the detected type (manual type selection in Quick Capture is a separate UI gap, out of scope)
- Reclassification of `AudioRecording` captures (set by the audio pipeline, must not be overwritten)
- Retroactive reclassification of already-processed captures

## Decisions

### 1. Classification via existing AI extraction call (not a separate call)

**Decision:** Add `detected_type` to the extraction JSON schema and system prompt. The AI classifies the content as part of its existing analysis pass.

**Alternatives considered:**
- Separate classification endpoint / AI call -- rejected: adds latency, cost, and complexity for a simple enum classification
- Client-side regex heuristics (e.g., detect `Speaker:` patterns) -- rejected: fragile, duplicates logic, and cannot handle ambiguous cases

**Rationale:** The AI already reads the full content and understands its structure. Adding one more field to the response schema is the simplest approach with the best accuracy.

### 2. Domain method `Reclassify(CaptureType)` on Capture aggregate

**Decision:** Add a domain method that changes `CaptureType` and raises a `CaptureReclassified` event. The method SHALL only be callable when `ProcessingStatus` is `Processing` (i.e., during the extraction pipeline, before `CompleteProcessing` transitions the capture to `Processed`), and SHALL reject reclassification to `AudioRecording`.

**Alternatives considered:**
- Direct property setter -- rejected: violates DDD encapsulation; no event, no guard
- Allow reclassification at any status -- rejected: could cause confusion if a user manually set the type and it gets overwritten on retry

**Rationale:** Restricting to `Processing` status ensures reclassification only happens as part of extraction. The domain event enables future audit/reaction.

### 3. Store `DetectedCaptureType` on `AiExtraction` value object

**Decision:** Add a `CaptureType? DetectedCaptureType` property to `AiExtraction`. This records what the AI detected, independent of whether the capture was actually reclassified.

**Rationale:** Separation of detected vs. applied type allows the UI to show "Detected as: Transcript" even if the type was already correct. Also useful for debugging and future analytics.

### 4. Prompt classification guidance

**Decision:** The system prompt will include explicit classification rules:
- `Transcript` -- content has speaker labels (`Name:` lines), turn-taking dialogue, or is explicitly identified as a transcript
- `MeetingNotes` -- structured notes with headings, bullet points, agenda items, or action items without speaker labels
- `QuickNote` -- short unstructured text, a single thought, reminder, or brief observation

**Rationale:** Clear classification criteria reduce ambiguity for the AI and improve consistency across providers.

## AI Prompting Strategy

The existing `ExtractionPromptBuilder.SystemPrompt` will be extended to include a 7th extraction field:

```
7. **detected_type**: Classify the content type. One of:
   - "QuickNote": Short unstructured text, a single thought, reminder, or brief note
   - "Transcript": Multi-speaker dialogue with speaker labels or turn-taking patterns
   - "MeetingNotes": Structured notes with headings, bullet points, agenda items, or action items
```

The field is added to the JSON schema at the end of the response object. Fallback: if the AI omits the field or returns an unrecognized value, the handler keeps the original capture type (no reclassification).

**Naming convention:** The JSON field is `detected_type` (snake_case, matching the existing extraction schema). The `ExtractionResponseDto` property is `DetectedType` with `[JsonPropertyName("detected_type")]`. The `AiExtraction` value object property is `DetectedCaptureType` (fully qualified to avoid ambiguity with the aggregate's `CaptureType` property).

## Risks / Trade-offs

- **[Risk] AI misclassifies content** -- Mitigation: keep original type as fallback if `detected_type` is null or unrecognized; the AI prompt includes clear classification criteria; users can still manually set type via Advanced section before capture
- **[Risk] EF JSONB serialization of new field** -- Mitigation: `AiExtraction` is persisted as JSONB via `ToJson()`, so adding `DetectedCaptureType` requires no schema migration; existing rows simply lack the field and deserialize as null
- **[Trade-off] No retroactive reclassification** -- Existing captures keep their original type. Acceptable because only newly processed captures benefit, and the backlog of mislabelled captures is small.

## Open Questions

_None -- the approach is straightforward and all decisions are resolved._
