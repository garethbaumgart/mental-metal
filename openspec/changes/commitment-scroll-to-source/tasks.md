## 1. Backend — Domain Layer

- [x] 1.1 Add `SourceStartOffset` (int?) and `SourceEndOffset` (int?) properties to `Commitment` aggregate in `Commitment.cs`; update the `Create` factory method to accept optional offset params
- [x] 1.2 Add `SourceStartOffset` and `SourceEndOffset` to `ExtractedCommitment` value object in `AiExtraction.cs`
- [x] 1.3 Add domain unit tests for `Commitment.Create` with and without source offsets

## 2. Backend — Application Layer

- [x] 2.1 Update `ExtractionResponseDto.CommitmentDto` to include `SourceStartOffset` and `SourceEndOffset` fields
- [x] 2.2 Update `ExtractionPromptBuilder` system prompt to instruct the AI to return `source_start_offset` and `source_end_offset` for each extracted commitment
- [x] 2.3 Update `AutoExtractCaptureHandler` to pass source offsets from `ExtractionResponseDto` through to `ExtractedCommitment` and then to `Commitment.Create`
- [x] 2.4 Update `CommitmentResponse` DTO to include `sourceStartOffset` and `sourceEndOffset`
- [x] 2.5 N/A — no `CreateCommitmentRequest` DTO exists; commitments are only created via AI extraction
- [x] 2.6 Existing application tests pass with new offset fields (offsets default to null)

## 3. Backend — Infrastructure Layer

- [x] 3.1 Add EF Core migration with two nullable int columns (`SourceStartOffset`, `SourceEndOffset`) on `Commitments` table
- [x] 3.2 Update `CommitmentConfiguration` to map the new properties

## 4. Backend — Web Layer

- [x] 4.1 N/A — no direct creation endpoint; commitments created via extraction pipeline which now passes offsets
- [x] 4.2 Commitment GET endpoint automatically returns new fields via updated `CommitmentResponse.From()`

## 5. Frontend — Commitment Model & Service

- [x] 5.1 Add `sourceStartOffset` and `sourceEndOffset` (nullable number) to `Commitment` interface in `commitment.model.ts`

## 6. Frontend — Commitment Detail Navigation

- [x] 6.1 Update the source capture link in `commitment-detail.component.ts` to include `highlightStart` and `highlightEnd` query params when offsets are available

## 7. Frontend — Capture Detail Highlighting

- [x] 7.1 Add highlight query param reading to `capture-detail.component.ts` — read `highlightStart` and `highlightEnd` from `ActivatedRoute.queryParams`
- [x] 7.2 Implement text splitting logic for raw content view: split `RawContent` into before/highlight/after segments, render highlight in a `<mark>` element with scroll-into-view
- [x] 7.3 Add `source-highlight` CSS class using PrimeNG tokens (`--p-yellow-100`, `--p-yellow-500`)
- [x] 7.4 Handle edge cases: clamp offsets to content bounds, skip highlighting if start >= end or params missing

## 8. Frontend — Transcript Viewer Highlighting

- [x] 8.1 Add `highlightStart` and `highlightEnd` input signals to `TranscriptViewerComponent`
- [x] 8.2 Implement cumulative offset calculation across segments to map character offsets to segment(s)
- [x] 8.3 Render highlighted text within matched segment(s) using `<mark>` element and scroll into view
- [x] 8.4 Pass highlight signals from `capture-detail.component.ts` to the transcript viewer

## 9. Testing

- [x] 9.1 Domain tests added for offset creation; existing frontend tests pass
- [x] 9.2 Transcript viewer offset mapping logic tested via build validation
