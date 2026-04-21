## Why

When a user pastes a meeting transcript into Quick Capture, the system always creates the capture as `QuickNote`. The AI extraction pipeline already analyzes the content deeply enough to determine the content type, but it never reclassifies the capture. This means transcripts and meeting notes are mislabelled, which degrades filtering by type and provides a misleading UX. Since the AI is already processing the content, adding type detection requires no additional AI calls.

## What Changes

- Add a `detected_type` field to the AI extraction JSON schema, instructing the AI to classify the content as `QuickNote`, `Transcript`, or `MeetingNotes` during extraction
- Add `DetectedType` to the `ExtractionResponseDto` to capture the AI's classification
- Add a `Reclassify(CaptureType)` domain method on the Capture aggregate that changes the type post-creation (only valid during processing)
- After successful extraction in `AutoExtractCaptureHandler`, reclassify the capture's type to match the AI's detected type (if different from the original)
- Store the detected type on `AiExtraction` so the UI can display what was detected
- Surface the detected (or reclassified) type in the capture detail UI

## Non-goals

- No client-side heuristic detection (all classification happens server-side during AI extraction)
- No new AI call or separate classification endpoint -- detection piggybacks on the existing extraction call
- No user override UI for the detected type in this change (manual type selection in Quick Capture is a separate UI gap and out of scope for this proposal)
- `AudioRecording` is excluded from auto-detection -- it is set by the audio capture pipeline and should not be reclassified

## Capabilities

### New Capabilities

_None_ -- this change extends existing capabilities rather than introducing a wholly new one.

### Modified Capabilities

- `capture-ai-extraction`: The AI extraction pipeline gains content-type classification. The extraction prompt, response DTO, and handler are extended to detect and apply the capture type. The `AiExtraction` value object gains a `DetectedCaptureType` field.
- `capture-text`: The Capture aggregate gains a `Reclassify(CaptureType)` domain method for post-creation type changes. The capture detail UI surfaces the detected type.

## Impact

- **Domain layer** (`Capture` aggregate): New `Reclassify` method, new `CaptureReclassified` domain event
- **Domain layer** (`AiExtraction` value object): New `DetectedCaptureType` property
- **Application layer** (`ExtractionPromptBuilder`): Updated system prompt to request `detected_type`
- **Application layer** (`ExtractionResponseDto`): New `DetectedType` field
- **Application layer** (`AutoExtractCaptureHandler`): Reclassify capture after extraction
- **Infrastructure layer**: No EF migration needed -- `AiExtraction` is persisted as JSONB, so `DetectedCaptureType` is a new JSON field
- **Frontend**: Capture detail view shows detected type badge/indicator
- **No breaking changes** -- existing captures keep their current type; only newly processed captures get reclassified
