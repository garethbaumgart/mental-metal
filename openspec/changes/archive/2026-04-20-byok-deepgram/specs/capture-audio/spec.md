## MODIFIED Requirements

### Requirement: Transcribe an audio capture

The system SHALL transcribe an `AudioRecording` capture using `ITranscriptionProviderFactory` to resolve the current user's configured `IAudioTranscriptionProvider`, populate `Capture.RawContent` with the full transcript text, populate the owned `TranscriptSegments` collection with per-segment start/end seconds, speaker label, and text, transition `Capture.TranscriptionStatus` from `Pending` → `InProgress` → `Transcribed`, and raise a `CaptureTranscribed` domain event. Transcription SHALL run synchronously during upload (background queueing is out of scope for MVP).

`TranscriptionStatus` is a lifecycle independent of `ProcessingStatus` (which governs AI extraction). On successful transcription `ProcessingStatus` SHALL remain `Raw` so the existing `capture-ai-extraction` pipeline picks up the capture unchanged; transcription completion is represented exclusively by `TranscriptionStatus = Transcribed`.

If transcription fails the system SHALL transition `TranscriptionStatus` to `Failed`, raise a `CaptureTranscriptionFailed` domain event (distinct from `CaptureProcessingFailed`, which is reserved for AI-extraction failure), leave `ProcessingStatus` untouched, and return error code `transcription.failed` (or `transcription.providerUnavailable` when the provider was unreachable). If the user has no transcription provider configured, the system SHALL return error code `transcription.notConfigured`. The endpoint `POST /api/captures/{id}/transcribe` SHALL allow retrying a transcription when `TranscriptionStatus = Failed` and the audio blob is still present.

#### Scenario: Successful transcription populates transcript

- **WHEN** an audio capture is uploaded and the user has a configured transcription provider and the transcription provider returns text and segments
- **THEN** `Capture.RawContent` contains the full transcript text, `TranscriptSegments` contains one segment per provider segment, `Capture.TranscriptionStatus` is `Transcribed`, `Capture.ProcessingStatus` remains `Raw`, and a `CaptureTranscribed` event is raised

#### Scenario: Transcription failure

- **WHEN** the transcription provider throws or returns an error during upload
- **THEN** `Capture.TranscriptionStatus` becomes `Failed`, `Capture.ProcessingStatus` is unchanged, a `CaptureTranscriptionFailed` event is raised, and the upload response returns error code `transcription.failed`

#### Scenario: No transcription provider configured

- **WHEN** an authenticated user uploads audio without a configured transcription provider
- **THEN** the system returns error code `transcription.notConfigured` and no Capture is created

#### Scenario: Retry transcription on failed capture

- **WHEN** an authenticated user POSTs to `/api/captures/{id}/transcribe` for a capture with `TranscriptionStatus = Failed` and the audio still present
- **THEN** the system re-runs transcription using the user's configured provider and returns HTTP 200 with the updated capture (`TranscriptionStatus = Transcribed` on success)

#### Scenario: Retry on capture with discarded audio

- **WHEN** an authenticated user POSTs to `/api/captures/{id}/transcribe` for a capture where `AudioDiscardedAt` is set
- **THEN** the system returns HTTP 400 with error code `transcription.audioDiscarded`
