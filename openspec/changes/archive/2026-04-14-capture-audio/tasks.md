## 1. Domain

- [x] 1.1 Extend `CaptureType` enum with `AudioRecording`
- [x] 1.2 Add audio fields to `Capture` aggregate: `AudioBlobRef`, `AudioMimeType`, `AudioDurationSeconds`, `AudioDiscardedAt`
- [x] 1.3 Add `TranscriptionStatus` enum (`NotApplicable`, `Pending`, `InProgress`, `Transcribed`, `Failed`) and property on `Capture`, independent of `ProcessingStatus`; existing (text) captures default to `NotApplicable`
- [x] 1.4 Add `TranscriptSegment` owned entity with `StartSeconds`, `EndSeconds`, `SpeakerLabel`, `Text`, `LinkedPersonId`; enforce invariants in `Create`
- [x] 1.5 Add `TranscriptSegments` owned collection to `Capture` with `ReplaceTranscript(...)` and `IdentifySpeakers(mapping)` methods
- [x] 1.6 Add `Capture.CreateAudio(...)`, `Capture.AttachTranscript(...)`, `Capture.MarkTranscriptionFailed(reason)`, `Capture.MarkAudioDiscarded(now)` methods with explicit `TranscriptionStatus` transitions
- [x] 1.7 Add domain events: `CaptureAudioUploaded`, `CaptureTranscribed`, `CaptureTranscriptionFailed`, `CaptureAudioDiscarded`, `CaptureSpeakerIdentified`
- [x] 1.8 Unit tests covering audio-capture creation, transcript attachment, speaker mapping, `TranscriptionStatus` transitions (Pending→InProgress→Transcribed / Pending→InProgress→Failed), and segment invariants

## 2. Application

- [x] 2.1 Define `IAudioBlobStore` interface in Application abstractions
- [x] 2.2 Define `IAudioTranscriptionProvider` interface returning text + segments
- [x] 2.3 Implement `UploadAudioCapture` handler (create capture with `TranscriptionStatus=Pending`, save blob, transition to `InProgress`, transcribe inline, split any provider segment whose `Text` exceeds 2000 chars into adjacent 2000-char segments with proportional `StartSeconds`/`EndSeconds`, on success mark `Transcribed` + discard blob, on failure mark `Failed` + raise `CaptureTranscriptionFailed` + retain blob, commit)
- [x] 2.4 Implement `TranscribeCapture` handler for retry flow (allow only when `TranscriptionStatus=Failed` and `AudioBlobRef` is present; reject retry on a capture whose audio was already discarded with error code `transcription.audioDiscarded`)
- [x] 2.5 Implement `GetCaptureTranscript` query handler
- [x] 2.6 Implement `UpdateCaptureSpeakers` handler (verify person existence, invoke aggregate method)
- [x] 2.7 Application-level unit tests for each handler (success, failure, not-found, invalid input)

## 3. Infrastructure

- [x] 3.1 EF Core configuration for new `Capture` audio fields and owned `TranscriptSegments` collection (HasMaxLength 2000, 64)
- [x] 3.2 Repository helpers `MarkOwnedAdded/MarkOwnedRemoved` for transcript segments
- [x] 3.3 `FileSystemAudioBlobStore` implementation with `AudioBlobStoreOptions` (Range/Required, ValidateOnStart)
- [x] 3.4 `StubAudioTranscriptionProvider` (dev/test only — named to make clear it must never be registered in production; deterministic output, fixed speaker labels)
- [x] 3.5 EF migration `AddCaptureAudioFields`
- [x] 3.6 DI registration of new services in `Program.cs` / composition root

## 4. Web API

- [x] 4.1 `POST /api/captures/audio` minimal-API endpoint with multipart handling and `RequestSizeLimit`
- [x] 4.2 `POST /api/captures/{id}/transcribe` endpoint
- [x] 4.3 `GET /api/captures/{id}/transcript` endpoint
- [x] 4.4 `PATCH /api/captures/{id}/speakers` endpoint
- [x] 4.5 DTOs for transcript segments, speaker-mapping request, and audio-upload response
- [x] 4.6 `AudioUploadOptions` (MaxSizeBytes, AllowedMimeTypes) with `ValidateDataAnnotations().ValidateOnStart()`
- [x] 4.7 Error-code constants — canonical set, identical to the proposal and spec: `audio.invalidFormat`, `audio.tooLarge`, `audio.uploadFailed`, `transcription.audioDiscarded`, `transcription.failed`, `transcription.providerUnavailable`, `capture.notFound`, `speaker.personNotFound`, `speaker.labelNotFound`
- [x] 4.8 Integration tests for each endpoint (happy path + key failure modes)

## 5. Frontend

- [x] 5.1 `CaptureRecorderComponent` standalone component using MediaRecorder with signals
- [x] 5.2 `TranscriptViewerComponent` grouping segments by speaker with timecodes
- [x] 5.3 `SpeakerPickerComponent` with Person autocomplete
- [x] 5.4 API client methods for the four new endpoints
- [x] 5.5 Integration into capture detail view
- [x] 5.6 Component tests for recorder state machine and transcript grouping logic

## 6. Validation

- [x] 6.1 `dotnet test src/MentalMetal.slnx` green
- [x] 6.2 `(cd src/MentalMetal.Web/ClientApp && npx ng test --watch=false)` green
- [x] 6.3 `openspec validate capture-audio --strict` passes
