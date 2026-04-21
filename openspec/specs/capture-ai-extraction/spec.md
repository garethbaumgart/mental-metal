# Capture AI Extraction

## Requirements

### Requirement: Begin processing a capture

The system SHALL automatically trigger AI processing on a Capture after it is created, as a background task outside the HTTP request lifecycle. The extraction SHALL run within a new DI scope using `IServiceScopeFactory`, with the user context set via `IBackgroundUserScope.SetUserId()`. The capture's status SHALL transition from `Raw` to `Processing` at the start of extraction and to `Processed` or `Failed` upon completion. If the background task fails to start or the process terminates during extraction, the capture SHALL remain in its current status (`Raw` or `Processing`) and the user MAY retry via the existing retry mechanism.

#### Scenario: Background extraction triggered after capture creation

- **WHEN** a capture is created via POST /api/captures
- **THEN** the HTTP response returns immediately with the capture in `Raw` status
- **AND** a background task begins extraction within the same server process

#### Scenario: Background extraction completes successfully

- **WHEN** the background extraction task runs on a capture
- **THEN** the capture transitions from `Raw` to `Processing` to `Processed`
- **AND** extracted commitments, people mentions, and initiative tags are persisted

#### Scenario: Background extraction fails

- **WHEN** the AI provider call fails during background extraction
- **THEN** the capture transitions to `Failed` with the error message as the failure reason
- **AND** the capture is available for retry via the existing capture detail page

#### Scenario: Process termination during extraction

- **WHEN** the server process terminates while extraction is in progress
- **THEN** the capture remains in `Processing` status
- **AND** the user can retry extraction from the capture detail page

#### Scenario: DI scope isolation

- **WHEN** the background extraction task runs
- **THEN** it uses its own DI scope independent of the HTTP request scope
- **AND** the HTTP request scope is not kept alive by the background task

### Requirement: AI extraction pipeline

The `AutoExtractCaptureHandler` SHALL orchestrate the extraction pipeline:

1. Load the Capture
2. Call `IAiCompletionService.CompleteAsync` with a system prompt instructing the AI to extract structured data from the raw content
3. Parse the AI response into an `AiExtraction` value object
4. If reclassification conditions are met, call `Reclassify(detectedType)` on the Capture while it is still in `Processing` status
5. Call `CompleteProcessing(extraction)` on the Capture aggregate
6. Persist the result

If the AI call fails, the handler SHALL call `FailProcessing(reason)` on the Capture.

The system prompt SHALL instruct the AI to include `source_start_offset` (0-based character index where the commitment starts in the input text) and `source_end_offset` (exclusive character index where the commitment ends) for each extracted commitment. The system prompt SHALL also instruct the AI to classify the content type as one of `QuickNote`, `Transcript`, or `MeetingNotes` via a `detected_type` field.

**Reclassification conditions:** the handler SHALL call `Reclassify(detectedType)` only when all of the following are true: (a) the detected type is a recognized `CaptureType` other than `AudioRecording`, (b) the capture's current type is not `AudioRecording`, and (c) the detected type differs from the current type. If any condition is not met, reclassification is skipped.

#### Scenario: Successful extraction from a quick note

- **WHEN** the processing pipeline runs on a QuickNote capture with content "Follow up with Sarah on Q3 roadmap — she committed to the API spec by Friday"
- **THEN** the AI extracts a summary, identifies one commitment (TheirsToMe: "Deliver API spec", person: Sarah, due: Friday), and suggests person link to Sarah
- **AND** the extracted commitment includes `source_start_offset` and `source_end_offset` referencing the position in the input text
- **AND** the AI returns `detected_type` of "QuickNote"
- **AND** CompleteProcessing is called with the AiExtraction value object

#### Scenario: Successful extraction from a transcript

- **WHEN** the processing pipeline runs on a Transcript capture containing a multi-topic meeting transcript
- **THEN** the AI extracts a summary, commitments, delegations, decisions, risks, observations, and suggested person/initiative links
- **AND** each extracted commitment includes `source_start_offset` and `source_end_offset`
- **AND** the AI returns `detected_type` of "Transcript"

#### Scenario: Reclassify QuickNote to Transcript after extraction

- **WHEN** the processing pipeline runs on a capture with type `QuickNote` whose content is a multi-speaker transcript with speaker labels
- **THEN** the AI returns `detected_type` of "Transcript"
- **AND** the handler calls `Reclassify(CaptureType.Transcript)` on the capture
- **AND** the capture's `CaptureType` is now `Transcript`

#### Scenario: Reclassify QuickNote to MeetingNotes after extraction

- **WHEN** the processing pipeline runs on a capture with type `QuickNote` whose content contains structured meeting notes with headings and action items
- **THEN** the AI returns `detected_type` of "MeetingNotes"
- **AND** the handler calls `Reclassify(CaptureType.MeetingNotes)` on the capture
- **AND** the capture's `CaptureType` is now `MeetingNotes`

#### Scenario: No reclassification when detected type matches current type

- **WHEN** the processing pipeline runs on a capture with type `Transcript` and the AI returns `detected_type` of "Transcript"
- **THEN** the handler does NOT call `Reclassify` on the capture
- **AND** the capture's `CaptureType` remains `Transcript`

#### Scenario: No reclassification for AudioRecording captures

- **WHEN** the processing pipeline runs on a capture with type `AudioRecording` and the AI returns `detected_type` of "Transcript"
- **THEN** the handler does NOT call `Reclassify` on the capture
- **AND** the capture's `CaptureType` remains `AudioRecording`

#### Scenario: No reclassification when detected_type is missing or unrecognized

- **WHEN** the AI response omits the `detected_type` field or returns an unrecognized value
- **THEN** the handler does NOT call `Reclassify` on the capture
- **AND** the capture keeps its original `CaptureType`

#### Scenario: AI provider failure

- **WHEN** the AI completion call throws an AiProviderException
- **THEN** the handler calls FailProcessing with the error message as the reason
- **AND** the capture status transitions to Failed

#### Scenario: Taste limit exceeded

- **WHEN** the AI completion call throws a TasteLimitExceededException
- **THEN** the handler calls FailProcessing with reason "Daily AI limit reached"
- **AND** the capture status transitions to Failed

### Requirement: AiExtraction value object

The system SHALL define an `AiExtraction` value object embedded on the Capture aggregate with the following properties: Summary (string, required), Commitments (list of extracted commitments with description, direction, person hint, PersonRawName, optional due date, and optional source character offsets: SourceStartOffset and SourceEndOffset, optional SpawnedCommitmentId), Delegations (list of extracted delegations with description, person hint, and optional due date), Observations (list of extracted observations with description, person hint, and tag), Decisions (list of strings), RisksIdentified (list of strings), PeopleMentioned (list of person mentions with RawName string and optional PersonId — null when unresolved), SuggestedPersonLinks (list of person name hints), SuggestedInitiativeLinks (list of initiative name hints), ConfidenceScore (decimal, 0.0-1.0), and DetectedCaptureType (nullable CaptureType indicating the AI's content classification).

#### Scenario: AiExtraction with all fields populated

- **WHEN** a transcript yields commitments, delegations, observations, decisions, and risks
- **THEN** the AiExtraction value object contains all extracted items with their respective properties
- **AND** each commitment includes SourceStartOffset, SourceEndOffset, and PersonRawName
- **AND** DetectedCaptureType is set to the type the AI classified the content as

#### Scenario: AiExtraction with minimal content

- **WHEN** a quick note yields only a summary and one commitment
- **THEN** the AiExtraction value object contains the summary and one commitment, with empty lists for other properties

#### Scenario: AiExtraction with null DetectedCaptureType

- **WHEN** the AI omits the `detected_type` field from its response
- **THEN** the AiExtraction value object has DetectedCaptureType as null

#### Scenario: AiExtraction equality

- **WHEN** two AiExtraction instances have identical property values including DetectedCaptureType
- **THEN** they are considered equal

### Requirement: Spawn entities from extraction

The system SHALL, after successful extraction, automatically spawn Commitment entities for High/Medium confidence commitments that have a resolved PersonId. Commitments referencing unresolved people (null PersonId) SHALL be recorded in the extraction metadata with their `PersonRawName` but SHALL NOT be spawned as Commitment entities. Each spawned entity SHALL have its SourceCaptureId set to the originating capture's ID. Each spawned Commitment SHALL have its SourceStartOffset and SourceEndOffset set from the extracted commitment's source offsets (if available). The extraction metadata SHALL preserve all extracted commitments (both spawned and skipped) so that skipped commitments can be spawned later when the person is resolved.

#### Scenario: Auto-spawn commitments for resolved people only

- **WHEN** the extraction pipeline finds two commitments: one for "Sarah" (resolved to PersonId X) and one for "Mike" (unresolved)
- **THEN** the system creates a Commitment entity for Sarah's commitment with PersonId X and SourceCaptureId set to the capture
- **AND** records Mike's commitment in the extraction with PersonRawName "Mike", PersonId null, and SpawnedCommitmentId null
- **AND** does NOT create a Commitment entity for Mike's commitment

#### Scenario: Auto-spawn with source offsets

- **WHEN** the extraction pipeline spawns a commitment with source offsets
- **THEN** the spawned Commitment has SourceStartOffset and SourceEndOffset set from the extraction data

#### Scenario: Name resolution matches existing person

- **WHEN** the extraction contains person hints (e.g., "Sarah") and the user has a Person named "Sarah Chen"
- **THEN** the system matches the hint to the existing Person by name similarity and sets the PersonId on the spawned commitment

#### Scenario: Name resolution finds no match

- **WHEN** the extraction contains a person hint that does not match any existing Person
- **THEN** the extracted commitment is recorded with PersonRawName set and PersonId null
- **AND** no Commitment entity is spawned for that item
- **AND** the user can resolve the person later via the unresolved people review flow

### Requirement: Resolve person mention post-extraction

The system SHALL allow an authenticated user to resolve an unresolved person mention by sending a POST to `/api/captures/{id}/resolve-person-mention` with `rawName` and `personId`. The system SHALL update the extraction's PersonMention with the resolved PersonId, add the raw name as an alias on the person (if not already present), link the capture to the person, and spawn any skipped commitments for that person (High/Medium confidence with no existing SpawnedCommitmentId). When multiple extracted commitments share the same `PersonRawName`, all matching commitments SHALL be spawned in a single resolution operation. The operation SHALL be atomic -- if any step fails, no changes are persisted. The raw name used as alias SHALL be validated for uniqueness among the user's people.

#### Scenario: Resolve and spawn skipped commitments

- **WHEN** an authenticated user resolves "Sarah" to PersonId X
- **AND** the extraction has one High confidence commitment with PersonRawName "Sarah" and SpawnedCommitmentId null
- **THEN** the system creates a Commitment entity with PersonId X
- **AND** updates the extraction's commitment with the SpawnedCommitmentId
- **AND** records the spawned commitment on the capture

#### Scenario: Resolve with no skipped commitments

- **WHEN** an authenticated user resolves "Mike" to PersonId Y
- **AND** no extracted commitments reference "Mike"
- **THEN** the mention is resolved and linked, but no commitments are spawned

#### Scenario: Alias conflict rejected

- **WHEN** an authenticated user resolves "Sarah" to PersonId X
- **AND** another Person already has "Sarah" as an alias
- **THEN** the system returns HTTP 409 Conflict

### Requirement: Retry failed processing

The system SHALL allow an authenticated user to retry processing on a capture with status `Failed` by sending a POST to `/api/captures/{id}/retry`. The system SHALL call `RetryProcessing()` on the Capture aggregate (transitioning status back to `Raw`) and the user can then trigger processing again.

#### Scenario: Retry a failed capture

- **WHEN** an authenticated user sends a POST to `/api/captures/{id}/retry` for a capture with status Failed
- **THEN** the status transitions to Raw and returns HTTP 200

#### Scenario: Retry a non-failed capture rejected

- **WHEN** an authenticated user sends a POST to `/api/captures/{id}/retry` for a capture not in Failed status
- **THEN** the system returns HTTP 409 Conflict

### Requirement: Auto-link capture to matched people and initiatives

After extraction confirmation, the system SHALL automatically link the capture to any People and Initiatives that were matched from the extraction's suggested links. Links SHALL use the existing link-person and link-initiative endpoints on the Capture aggregate.

#### Scenario: Auto-link matched people

- **WHEN** the extraction suggests "Sarah" and "Mike" and both match existing People
- **THEN** the capture's LinkedPersonIds includes both matched PersonIds

#### Scenario: Auto-link matched initiatives

- **WHEN** the extraction suggests "Project X" and it matches an existing Initiative by name similarity
- **THEN** the capture's LinkedInitiativeIds includes the matched InitiativeId

#### Scenario: No matches found

- **WHEN** the extraction suggests names that do not match any existing People or Initiatives
- **THEN** no auto-links are created and no error occurs

### Requirement: Get extraction results

The system SHALL expose the AiExtraction on the capture detail endpoint (`GET /api/captures/{id}`). When the capture has status `Processed`, the response SHALL include the full AiExtraction with all extracted items. When the capture has status `Raw`, `Processing`, or `Failed`, the extraction SHALL be null.

#### Scenario: Get processed capture with extraction

- **WHEN** an authenticated user sends a GET to `/api/captures/{id}` for a processed capture
- **THEN** the response includes the AiExtraction with summary, commitments, delegations, observations, decisions, risks, suggested links, and confidence score

#### Scenario: Get non-processed capture

- **WHEN** an authenticated user sends a GET to `/api/captures/{id}` for a capture with status Raw
- **THEN** the response includes extraction as null

### Requirement: Processing status indicator in UI

The frontend capture list and detail views SHALL display a processing status indicator. For status `Processing`, show a spinner or progress indicator. For status `Failed`, show an error indicator with the failure reason and a "Retry" button. For status `Processed`, show a success indicator.

#### Scenario: Processing in progress

- **WHEN** a capture is in Processing status
- **THEN** the UI shows a spinner indicator on the capture's status badge

#### Scenario: Processing failed

- **WHEN** a capture is in Failed status
- **THEN** the UI shows an error badge and a "Retry" action button

#### Scenario: Processing complete

- **WHEN** a capture is in Processed status
- **THEN** the UI shows a success indicator on the status badge

### Requirement: Extraction review UI

The frontend SHALL provide an extraction review panel on the capture detail view for processed captures. The panel SHALL display the AI-generated summary, extracted commitments, delegations, observations, decisions, and risks in organized sections. Each extracted item SHALL show its matched person (if any) and relevant details. The panel SHALL provide "Confirm & Create" and "Discard" action buttons.

#### Scenario: Review extraction results

- **WHEN** a user views a processed capture's detail page
- **THEN** the extraction panel displays the summary, extracted items grouped by type, suggested person/initiative links, and the confidence score

#### Scenario: Confirm extraction from review panel

- **WHEN** a user clicks "Confirm & Create" in the extraction review panel
- **THEN** the system spawns the extracted entities and the panel updates to show "Entities created" with links to the created items

#### Scenario: Discard extraction from review panel

- **WHEN** a user clicks "Discard" in the extraction review panel
- **THEN** the system discards the extraction, no entities are spawned, and the panel shows "Extraction discarded"

### Requirement: Process capture action in UI

The frontend capture detail view SHALL provide a "Process with AI" action button for captures with status `Raw`. The button SHALL trigger the processing endpoint and update the UI to show the processing status.

#### Scenario: Trigger processing from detail view

- **WHEN** a user clicks "Process with AI" on a raw capture's detail page
- **THEN** the system triggers processing and the status indicator changes to show processing in progress

#### Scenario: Button hidden for non-raw captures

- **WHEN** a capture is in Processing, Processed, or Failed status
- **THEN** the "Process with AI" button is not shown (Retry is shown for Failed, nothing for Processing/Processed)
