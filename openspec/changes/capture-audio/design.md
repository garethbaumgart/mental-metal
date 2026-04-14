## Context

Mental Metal's `capture-text` feature (shipped) gives managers a single `Capture` aggregate for any textual input. Many recorded conversations (1:1s, leadership syncs, interviews) are happening today, but typing notes live degrades attention. We already have an AI provider abstraction (`ai-provider-abstraction`) and a downstream `capture-ai-extraction` pipeline that turns any `Capture.RawContent` into commitments, delegations, and links. The missing step is a path from microphone → transcript → populated `RawContent` with speaker context preserved.

Dependencies: `capture-text` (base aggregate), `person-management` (speaker linking), `ai-provider-abstraction` (transcription provider pattern).

## Goals / Non-Goals

**Goals:**
- Record/upload audio from the browser and persist it to a capture.
- Transcribe audio asynchronously (for MVP: inline on upload) producing text + speaker-labeled segments.
- Link transcript speakers to existing `Person` entries with a bulk mapping API.
- Automatically discard the audio blob once transcription succeeds; retain duration + MIME for UX.
- Reuse the existing `Capture` aggregate — do not fork a new audio aggregate.

**Non-Goals:**
- Real-time streaming transcription.
- Cloud object storage (S3/GCS/Azure) — filesystem implementation only, behind an interface.
- Cross-capture voiceprint matching (same speaker recognized across recordings).
- Editing/splitting transcript segments.
- User-configurable retention (audio discard is the only policy for MVP).
- Translation / multi-language selection in the UI.

## Decisions

### D1: Extend `Capture` aggregate rather than creating `AudioCapture`

Adding optional audio fields + an owned `TranscriptSegments` collection keeps downstream extraction, triage, and link APIs identical. A new aggregate would fork every list/filter query and double the surface area. Alternative considered and rejected: separate `AudioCapture` aggregate referencing `Capture` by ID.

### D2: `TranscriptSegments` as an EF-owned collection

Segments are only meaningful in the context of their parent `Capture` and never queried independently. An owned collection keeps aggregate invariants (user-scoping, lifecycle) intact. Infrastructure uses the repo's existing `MarkOwnedAdded`/`MarkOwnedRemoved` helpers. Domain enforces segment text length (≤ 2000 chars) and `StartSeconds ≤ EndSeconds` in a `TranscriptSegment.Create(...)` factory so EF column limits match domain invariants.

### D3: Synchronous transcription for MVP with a dedicated `TranscriptionStatus`

Transcription is run synchronously during the upload request (no background queue — background queueing is explicitly out of scope for MVP). Because the existing `Capture.ProcessingStatus` (`Raw`/`Processing`/`Processed`/`Failed`) is already owned by the AI-extraction pipeline (where `Processed` means extraction finished and `AiExtraction` is populated, and `CaptureProcessingFailed` means extraction failed), we introduce a separate lifecycle on the aggregate:

```
TranscriptionStatus: NotApplicable | Pending | InProgress | Transcribed | Failed
```

- Text captures keep `TranscriptionStatus = NotApplicable`.
- `POST /api/captures/audio` creates the capture with `ProcessingStatus = Raw` and `TranscriptionStatus = Pending`, stores the blob, flips to `InProgress`, calls `IAudioTranscriptionProvider.TranscribeAsync` inline, on success sets segments + `RawContent`, transitions `TranscriptionStatus = Transcribed`, discards the blob, and raises `CaptureTranscribed` + `CaptureAudioDiscarded`. `ProcessingStatus` stays `Raw` so the extraction pipeline picks the capture up exactly as it does for a text capture.
- On provider failure (exception or error response) the aggregate transitions `TranscriptionStatus = Failed`, raises `CaptureTranscriptionFailed`, and leaves `ProcessingStatus = Raw` (the extraction pipeline's lifecycle is untouched). The HTTP response returns `transcription.failed` (or `transcription.providerUnavailable` when the provider was unreachable).
- `POST /api/captures/{id}/transcribe` re-runs transcription for captures in `TranscriptionStatus = Failed` whose audio blob is still present.

This separation keeps the two concerns — transcription and AI extraction — cleanly addressable and prevents the audio flow from ever synthesising a `Processed` state that downstream consumers would read as "extraction complete".

### D3.1: Over-length transcript segments

`TranscriptSegment.Text` is bounded at 2000 characters to match the EF column and the domain invariant. When `IAudioTranscriptionProvider` returns a segment whose `Text` exceeds 2000 characters, the `UploadAudioCapture`/`TranscribeCapture` handler SHALL split the segment into adjacent segments of ≤ 2000 characters, preserving `SpeakerLabel` and allocating `StartSeconds`/`EndSeconds` proportionally to each chunk's character count. Splitting rather than truncating was chosen so no content is lost — transcripts feed downstream extraction where truncation would silently drop context.

### D4: Audio discard after successful transcription

A `Capture.MarkAudioDiscarded(now)` method nulls `AudioBlobRef` and sets `AudioDiscardedAt`. The `IAudioBlobStore.DeleteAsync(ref)` is called from the handler after the aggregate state transitions, not inside the aggregate — blob storage is infrastructure. If delete fails we log and continue (the capture is still valid; an orphan cleanup job can reap later — future work).

### D5: Storage abstraction

`IAudioBlobStore` with `SaveAsync(userId, stream, mime) → string ref`, `OpenReadAsync(ref) → Stream`, `DeleteAsync(ref)`. Default `FileSystemAudioBlobStore` writes to `{options.RootPath}/{userId}/{guid}.{ext}`. Options (`AudioBlobStoreOptions`) validated via `ValidateDataAnnotations().ValidateOnStart()`. Root path is ephemeral in Docker — acceptable because blobs are discarded within seconds of upload on the happy path.

### D6: Upload size + format validation

Options (`AudioUploadOptions`): `MaxSizeBytes` (default 50 MB, `[Range(1, 500_000_000)]`), `AllowedMimeTypes` (default `audio/webm,audio/mp4,audio/mpeg,audio/wav`). The endpoint uses minimal API `[FromForm] IFormFile` with `RequestSizeLimit` attribute honoring the option. Rejection returns `audio.tooLarge` or `audio.invalidFormat` before any bytes land on disk (Kestrel enforces the size cap).

### D7: Speaker identification API shape

`PATCH /api/captures/{id}/speakers` takes `{ mappings: [{ speakerLabel, personId }] }`. The aggregate method `IdentifySpeakers(IReadOnlyDictionary<string, PersonId> mapping)` walks `TranscriptSegments` and sets `LinkedPersonId` on every segment with a matching label. Unmapped labels are untouched. Passing a label not present in any segment returns `speaker.labelNotFound`. Person existence is verified in the handler (not in the aggregate) to avoid a cross-aggregate dependency in Domain; failure returns `speaker.personNotFound`.

### D8: Frontend recorder

Standalone `CaptureRecorderComponent` using `navigator.mediaDevices.getUserMedia` + `MediaRecorder`. Signals: `isRecording`, `durationSeconds`, `error`. Stop → `Blob` → `FormData` → `POST /api/captures/audio`. No waveform visualization for MVP.

### D9: Determinism and prompt-safe text

Handlers accept `TimeProvider` / injected `now` (the repo uses `TimeProvider` abstraction for `BriefingService`). Transcript text that flows into downstream AI prompts is escaped for backticks to `\u0060` — this escaping lives in the extraction pipeline (already shipped in `capture-ai-extraction`), not here, but we add a regression test that an audio-origin capture triggers the same safe path.

## Risks / Trade-offs

- **Long synchronous upload** → Mitigation: 50 MB cap; provider call is the bottleneck. If this becomes painful we'll move to background jobs (tracked as future work).
- **Ephemeral filesystem on Cloud Run** → Mitigation: blobs are discarded on success; any in-flight blob lost on container restart results in a `transcription.failed` and an orphaned capture with no audio. User can re-upload. Acceptable for MVP.
- **Speaker diarization accuracy varies by provider** → Mitigation: we store whatever labels the provider returns and let users re-map; no guarantees are made about label stability.
- **Orphaned blobs on transcription failure** → Mitigation: future cleanup job; note in ops docs. Not a hard failure.
- **MediaRecorder MIME output differs per browser** → Mitigation: allow the common set; reject unknown formats with `audio.invalidFormat`.

## Dependencies

- `capture-text` — `Capture` aggregate, `CaptureType` enum (adds `AudioRecording`).
- `person-management` — `PersonId`, `PersonRepository` for existence checks.
- `ai-provider-abstraction` — `IAudioTranscriptionProvider` added alongside existing AI provider interfaces.

## Open Questions

- Which production transcription provider to ship first (Claude/OpenAI/Gemini)? Deferred — stub dev provider is wired; real provider plug-in is a small follow-up.
- Retention opt-out UX (keep audio for N days) — explicitly deferred.
