## 1. Domain

- [ ] 1.1 Extend `CaptureType` enum with `AudioRecording`
- [ ] 1.2 Add audio fields to `Capture` aggregate: `AudioBlobRef`, `AudioMimeType`, `AudioDurationSeconds`, `AudioDiscardedAt`
- [ ] 1.3 Add `TranscriptSegment` owned entity with `StartSeconds`, `EndSeconds`, `SpeakerLabel`, `Text`, `LinkedPersonId`; enforce invariants in `Create`
- [ ] 1.4 Add `TranscriptSegments` owned collection to `Capture` with `ReplaceTranscript(...)` and `IdentifySpeakers(mapping)` methods
- [ ] 1.5 Add `Capture.CreateAudio(...)`, `Capture.AttachTranscript(...)`, `Capture.MarkAudioDiscarded(now)` methods
- [ ] 1.6 Add domain events: `CaptureAudioUploaded`, `CaptureTranscribed`, `CaptureAudioDiscarded`, `CaptureSpeakerIdentified`
- [ ] 1.7 Unit tests covering audio-capture creation, transcript attachment, speaker mapping, and segment invariants

## 2. Application

- [ ] 2.1 Define `IAudioBlobStore` interface in Application abstractions
- [ ] 2.2 Define `IAudioTranscriptionProvider` interface returning text + segments
- [ ] 2.3 Implement `UploadAudioCapture` handler (create capture, save blob, transcribe inline, discard blob, commit)
- [ ] 2.4 Implement `TranscribeCapture` handler for retry flow
- [ ] 2.5 Implement `GetCaptureTranscript` query handler
- [ ] 2.6 Implement `UpdateCaptureSpeakers` handler (verify person existence, invoke aggregate method)
- [ ] 2.7 Application-level unit tests for each handler (success, failure, not-found, invalid input)

## 3. Infrastructure

- [ ] 3.1 EF Core configuration for new `Capture` audio fields and owned `TranscriptSegments` collection (HasMaxLength 2000, 64)
- [ ] 3.2 Repository helpers `MarkOwnedAdded/MarkOwnedRemoved` for transcript segments
- [ ] 3.3 `FileSystemAudioBlobStore` implementation with `AudioBlobStoreOptions` (Range/Required, ValidateOnStart)
- [ ] 3.4 `StubAudioTranscriptionProvider` for dev/test environments (deterministic output)
- [ ] 3.5 EF migration `AddCaptureAudioFields`
- [ ] 3.6 DI registration of new services in `Program.cs` / composition root

## 4. Web API

- [ ] 4.1 `POST /api/captures/audio` minimal-API endpoint with multipart handling and `RequestSizeLimit`
- [ ] 4.2 `POST /api/captures/{id}/transcribe` endpoint
- [ ] 4.3 `GET /api/captures/{id}/transcript` endpoint
- [ ] 4.4 `PATCH /api/captures/{id}/speakers` endpoint
- [ ] 4.5 DTOs for transcript segments, speaker-mapping request, and audio-upload response
- [ ] 4.6 `AudioUploadOptions` (MaxSizeBytes, AllowedMimeTypes) with `ValidateDataAnnotations().ValidateOnStart()`
- [ ] 4.7 Error-code constants (`audio.invalidFormat`, `audio.tooLarge`, `transcription.failed`, `capture.notFound`, `speaker.personNotFound`, `speaker.labelNotFound`)
- [ ] 4.8 Integration tests for each endpoint (happy path + key failure modes)

## 5. Frontend

- [ ] 5.1 `CaptureRecorderComponent` standalone component using MediaRecorder with signals
- [ ] 5.2 `TranscriptViewerComponent` grouping segments by speaker with timecodes
- [ ] 5.3 `SpeakerPickerComponent` with Person autocomplete
- [ ] 5.4 API client methods for the four new endpoints
- [ ] 5.5 Integration into capture detail view
- [ ] 5.6 Component tests for recorder state machine and transcript grouping logic

## 6. Validation

- [ ] 6.1 `dotnet test src/MentalMetal.slnx` green
- [ ] 6.2 `(cd src/MentalMetal.Web/ClientApp && npx ng test --watch=false)` green
- [ ] 6.3 `openspec validate capture-audio --strict` passes
