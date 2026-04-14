## Why

Engineering managers spend many hours in 1:1s, leadership syncs, and interviews. Typing notes live disrupts attention; recording audio and transcribing after the fact captures context faithfully and lets users focus on the conversation. Speaker diarization and identification turn a raw transcript into structured dialogue linked to people already in Mental Metal, enabling downstream extraction (commitments, delegations, decisions) to attribute statements to the right person. Audio is discarded after transcription to minimize storage, privacy exposure, and retention risk.

## What Changes

- Extend the existing `Capture` aggregate with audio-specific fields: `AudioBlobRef` (storage pointer), `AudioMimeType`, `AudioDurationSeconds`, `AudioDiscardedAt`.
- Add an owned collection `TranscriptSegments` on `Capture` (per-segment `StartSeconds`, `EndSeconds`, `SpeakerLabel`, `Text`, optional `LinkedPersonId`).
- Add a new capture type `AudioRecording` (extension of existing enum).
- New AI abstraction: `IAudioTranscriptionProvider` returning transcript text plus speaker-labeled segments; transcript text is assigned to the existing `Capture.RawContent` so `capture-ai-extraction` works unchanged.
- New storage abstraction `IAudioBlobStore` with a filesystem-backed default implementation (configurable root path). Cloud providers deferred as future work.
- Retention policy: on successful transcription, the audio blob is deleted from storage and `AudioDiscardedAt` is set. Duration and MIME type remain for UX context.
- New endpoints:
  - `POST /api/captures/audio` (multipart upload; creates capture + queues/kicks transcription synchronously for MVP).
  - `POST /api/captures/{id}/transcribe` (re-trigger transcription on a failed capture).
  - `GET /api/captures/{id}/transcript` (returns transcript segments).
  - `PATCH /api/captures/{id}/speakers` (bulk mapping from speaker label → PersonId).
- Frontend: standalone recorder component using the browser `MediaRecorder` API (signals-driven), a transcript viewer grouping segments by speaker, and a speaker-identification picker with Person autocomplete.
- Error codes: `audio.invalidFormat`, `audio.tooLarge`, `audio.uploadFailed`, `transcription.failed`, `transcription.providerUnavailable`, `capture.notFound`, `speaker.personNotFound`, `speaker.labelNotFound`.

## Capabilities

### New Capabilities

- `capture-audio`: Record, upload, and transcribe audio into a Capture with speaker-diarized transcript segments linkable to Persons; discards the audio blob after successful transcription.

### Modified Capabilities

(None — no existing spec requirements change. `capture-text` stays intact; audio captures populate `RawContent` just as text captures do, and `capture-ai-extraction` consumes it unchanged.)

## Impact

- **Domain**: `Capture` aggregate gains audio fields and an owned `TranscriptSegment` collection; new domain events `CaptureAudioUploaded`, `CaptureTranscribed`, `CaptureAudioDiscarded`, `CaptureSpeakerIdentified`.
- **Application**: New vertical slices — `UploadAudioCapture`, `TranscribeCapture`, `GetCaptureTranscript`, `UpdateCaptureSpeakers`.
- **Infrastructure**: `IAudioBlobStore` (filesystem default), `IAudioTranscriptionProvider` (stub dev provider; real provider wiring out of scope); EF configuration for owned `TranscriptSegments` collection; new migration.
- **Web/API**: New endpoints and DTOs listed above; request-size limits enforced via options.
- **Frontend**: New recorder, transcript view, and speaker-mapping components wired into capture detail.
- **Tier**: Tier 3. Depends on `capture-text` (base aggregate) and `person-management` (speaker linking). Consumed by `capture-ai-extraction` without changes.
- **Non-goals**: Real-time live transcription; cloud blob storage (S3/GCS/Azure); multi-language transcription UX selection; speaker voiceprint matching across captures; editing transcript segments; retention opt-out (flagged as future).
