## ADDED Requirements

### Requirement: Upload an audio capture

The system SHALL allow an authenticated user to upload an audio recording via multipart `POST /api/captures/audio`. The request MUST include a file field, an optional `title`, and an optional `source`. The system SHALL create a new `Capture` with `Type = AudioRecording`, set `CapturedAt` to the current time, scope the Capture to the authenticated user's `UserId`, persist the audio bytes via `IAudioBlobStore`, and record `AudioBlobRef`, `AudioMimeType`, and `AudioDurationSeconds` on the aggregate. The system SHALL raise a `CaptureAudioUploaded` domain event. On success the response is HTTP 201 with the created capture.

#### Scenario: Upload a valid audio file

- **WHEN** an authenticated user POSTs a multipart request to `/api/captures/audio` with a 2 MB `audio/webm` file and title "1:1 with Sarah"
- **THEN** the system creates a Capture with type `AudioRecording`, stores the blob, sets `AudioMimeType = "audio/webm"` and `AudioDurationSeconds`, and returns HTTP 201 with the created capture

#### Scenario: Reject file over size limit

- **WHEN** an authenticated user POSTs an audio file larger than the configured `MaxSizeBytes`
- **THEN** the system returns HTTP 400 with error code `audio.tooLarge` and no Capture is created

#### Scenario: Reject unsupported MIME type

- **WHEN** an authenticated user POSTs a file whose MIME type is not in the configured allow-list
- **THEN** the system returns HTTP 400 with error code `audio.invalidFormat` and no Capture is created

#### Scenario: Missing file field

- **WHEN** an authenticated user POSTs to `/api/captures/audio` without a file
- **THEN** the system returns HTTP 400 with a validation error

### Requirement: Transcribe an audio capture

The system SHALL transcribe an `AudioRecording` capture using `IAudioTranscriptionProvider`, populate `Capture.RawContent` with the full transcript text, populate the owned `TranscriptSegments` collection with per-segment start/end seconds, speaker label, and text, and raise a `CaptureTranscribed` domain event. Transcription SHALL run synchronously on upload. If transcription fails, the capture's processing status SHALL transition to `Failed` with an error reason, and the response SHALL include error code `transcription.failed`. The endpoint `POST /api/captures/{id}/transcribe` SHALL allow retrying a failed transcription.

#### Scenario: Successful transcription populates transcript

- **WHEN** an audio capture is uploaded and the transcription provider returns text and segments
- **THEN** `Capture.RawContent` contains the full transcript text, `TranscriptSegments` contains one segment per provider segment, and a `CaptureTranscribed` event is raised

#### Scenario: Transcription failure

- **WHEN** the transcription provider throws or returns an error
- **THEN** the capture's processing status becomes `Failed`, a `CaptureProcessingFailed` event is raised, and the upload response returns error code `transcription.failed`

#### Scenario: Retry transcription on failed capture

- **WHEN** an authenticated user POSTs to `/api/captures/{id}/transcribe` for a capture in status `Failed` with the audio still present
- **THEN** the system re-runs transcription and returns HTTP 200 with the updated capture

#### Scenario: Retry on capture with discarded audio

- **WHEN** an authenticated user POSTs to `/api/captures/{id}/transcribe` for a capture where `AudioDiscardedAt` is set
- **THEN** the system returns HTTP 400 with error code `audio.uploadFailed` (no audio available)

### Requirement: Discard audio after successful transcription

When transcription completes successfully the system SHALL delete the audio blob from storage and record the deletion by setting `AudioDiscardedAt` on the Capture and clearing `AudioBlobRef`. `AudioMimeType` and `AudioDurationSeconds` SHALL be retained. The system SHALL raise a `CaptureAudioDiscarded` domain event.

#### Scenario: Audio discarded on success

- **WHEN** an audio capture is transcribed successfully
- **THEN** `IAudioBlobStore.DeleteAsync` is called, `AudioBlobRef` is null, `AudioDiscardedAt` is set to the current time, and `AudioDurationSeconds` and `AudioMimeType` are retained

#### Scenario: Audio retained on transcription failure

- **WHEN** transcription fails
- **THEN** the audio blob is retained, `AudioBlobRef` is unchanged, and `AudioDiscardedAt` is null

### Requirement: Retrieve transcript segments

The system SHALL allow an authenticated user to retrieve the transcript segments of one of their captures via `GET /api/captures/{id}/transcript`. The response SHALL contain an array of segments ordered by `StartSeconds` ascending, each with `startSeconds`, `endSeconds`, `speakerLabel`, `text`, and optional `linkedPersonId`.

#### Scenario: Get transcript for transcribed capture

- **WHEN** an authenticated user GETs `/api/captures/{id}/transcript` for a transcribed capture
- **THEN** the system returns HTTP 200 with the ordered segment list

#### Scenario: Transcript empty for non-transcribed capture

- **WHEN** an authenticated user GETs the transcript of a capture with no segments
- **THEN** the system returns HTTP 200 with an empty array

#### Scenario: Capture not found

- **WHEN** an authenticated user GETs the transcript of a capture that does not exist or belongs to another user
- **THEN** the system returns HTTP 404 with error code `capture.notFound`

### Requirement: Identify speakers on a transcript

The system SHALL allow an authenticated user to bulk-map transcript speaker labels to existing `Person` IDs via `PATCH /api/captures/{id}/speakers`. The request SHALL contain `mappings`, an array of `{ speakerLabel, personId }` pairs. For each pair the system SHALL set `LinkedPersonId` on every `TranscriptSegment` with that label. Each `personId` MUST exist and belong to the authenticated user. The system SHALL raise a `CaptureSpeakerIdentified` domain event per distinct label mapped.

#### Scenario: Map a speaker to a person

- **WHEN** an authenticated user PATCHes `/api/captures/{id}/speakers` with `{ mappings: [{ speakerLabel: "Speaker A", personId: "<sarah>" }] }`
- **THEN** every transcript segment labeled "Speaker A" has `LinkedPersonId = <sarah>` and the system returns HTTP 200

#### Scenario: Person does not exist

- **WHEN** an authenticated user passes a `personId` that does not exist or belongs to another user
- **THEN** the system returns HTTP 400 with error code `speaker.personNotFound`

#### Scenario: Speaker label not in transcript

- **WHEN** an authenticated user passes a `speakerLabel` not present in any segment
- **THEN** the system returns HTTP 400 with error code `speaker.labelNotFound`

#### Scenario: Empty mapping set is a no-op

- **WHEN** an authenticated user PATCHes with an empty `mappings` array
- **THEN** the system returns HTTP 200 and the transcript is unchanged

### Requirement: Transcript segment invariants

The `TranscriptSegment` value SHALL enforce the following invariants at creation and update: `Text` MUST NOT be empty and MUST be 2000 characters or fewer; `StartSeconds` MUST be non-negative; `EndSeconds` MUST be greater than or equal to `StartSeconds`; `SpeakerLabel` MUST NOT be empty and MUST be 64 characters or fewer. Violations SHALL throw a domain exception.

#### Scenario: Reject empty segment text

- **WHEN** a `TranscriptSegment` is created with empty `Text`
- **THEN** the domain throws an invariant-violation exception

#### Scenario: Reject segment with end before start

- **WHEN** a `TranscriptSegment` is created with `EndSeconds < StartSeconds`
- **THEN** the domain throws an invariant-violation exception

#### Scenario: Reject over-length segment text

- **WHEN** a `TranscriptSegment` is created with `Text` longer than 2000 characters
- **THEN** the domain throws an invariant-violation exception

### Requirement: Audio capture frontend recorder

The frontend SHALL provide a standalone, signal-driven recorder component that uses the browser `MediaRecorder` API to record audio in the user's default supported format, shows the elapsed duration while recording, and on stop uploads the resulting blob to `POST /api/captures/audio`. On successful upload the new capture SHALL appear in the capture list.

#### Scenario: Record and upload

- **WHEN** a user clicks Record, speaks, and clicks Stop in the recorder
- **THEN** the component uploads the audio and the new capture appears in the capture list with type `AudioRecording`

#### Scenario: Microphone permission denied

- **WHEN** the user denies microphone permission
- **THEN** the recorder displays an error message and does not attempt to upload

### Requirement: Transcript viewer UI

The frontend SHALL provide a transcript viewer on the capture detail page that groups consecutive segments by speaker, visually distinguishes speakers, and shows each segment's timecode. Each speaker group SHALL offer an action to link the speaker to an existing `Person` via an autocomplete picker.

#### Scenario: View transcript with speaker groups

- **WHEN** a user opens the detail view of a transcribed audio capture
- **THEN** the transcript is displayed as blocks grouped by speaker with timecodes visible

#### Scenario: Link speaker to person

- **WHEN** a user selects a Person in the speaker picker for a given speaker label
- **THEN** the frontend PATCHes `/api/captures/{id}/speakers` and the display updates to show the person's name next to every matching segment
