## 1. Backend — Domain Layer

- [ ] 1.1 Add `SourceStartOffset` (int?) and `SourceEndOffset` (int?) properties to `Commitment` aggregate in `Commitment.cs`; update the `Create` factory method to accept optional offset params
- [ ] 1.2 Add `SourceStartOffset` and `SourceEndOffset` to `ExtractedCommitment` value object in `AiExtraction.cs`
- [ ] 1.3 Add domain unit tests for `Commitment.Create` with and without source offsets

## 2. Backend — Application Layer

- [ ] 2.1 Update `ExtractionResponseDto.CommitmentDto` to include `SourceStartOffset` and `SourceEndOffset` fields
- [ ] 2.2 Update `ExtractionPromptBuilder` system prompt to instruct the AI to return `source_start_offset` and `source_end_offset` for each extracted commitment
- [ ] 2.3 Update `AutoExtractCaptureHandler` to pass source offsets from `ExtractionResponseDto` through to `ExtractedCommitment` and then to `Commitment.Create`
- [ ] 2.4 Update `CommitmentResponse` DTO to include `sourceStartOffset` and `sourceEndOffset`
- [ ] 2.5 Update `CreateCommitmentRequest` DTO to accept optional `sourceStartOffset` and `sourceEndOffset`
- [ ] 2.6 Update application tests for extraction handler to verify offsets are passed through

## 3. Backend — Infrastructure Layer

- [ ] 3.1 Add EF Core migration with two nullable int columns (`SourceStartOffset`, `SourceEndOffset`) on `Commitments` table
- [ ] 3.2 Update `CommitmentConfiguration` to map the new properties

## 4. Backend — Web Layer

- [ ] 4.1 Update commitment creation endpoint to accept and pass through source offset fields
- [ ] 4.2 Verify commitment GET endpoint returns the new offset fields in the response

## 5. Frontend — Commitment Model & Service

- [ ] 5.1 Add `sourceStartOffset` and `sourceEndOffset` (nullable number) to `Commitment` interface in `commitment.model.ts`

## 6. Frontend — Commitment Detail Navigation

- [ ] 6.1 Update the source capture link in `commitment-detail.component.ts` to include `highlightStart` and `highlightEnd` query params when offsets are available

## 7. Frontend — Capture Detail Highlighting

- [ ] 7.1 Add highlight query param reading to `capture-detail.component.ts` — read `highlightStart` and `highlightEnd` from `ActivatedRoute.queryParams`
- [ ] 7.2 Implement text splitting logic for raw content view: split `RawContent` into before/highlight/after segments, render highlight in a `<mark>` element with scroll-into-view
- [ ] 7.3 Add `source-highlight` CSS class using PrimeNG tokens (`--p-yellow-100`, `--p-yellow-500`)
- [ ] 7.4 Handle edge cases: clamp offsets to content bounds, skip highlighting if start >= end or params missing

## 8. Frontend — Transcript Viewer Highlighting

- [ ] 8.1 Add `highlightStart` and `highlightEnd` input signals to `TranscriptViewerComponent`
- [ ] 8.2 Implement cumulative offset calculation across segments to map character offsets to segment(s)
- [ ] 8.3 Render highlighted text within matched segment(s) using `<mark>` element and scroll into view
- [ ] 8.4 Pass highlight signals from `capture-detail.component.ts` to the transcript viewer

## 9. Testing

- [ ] 9.1 Add frontend unit tests for highlight offset clamping and text splitting logic
- [ ] 9.2 Add frontend unit tests for transcript viewer segment-to-offset mapping
