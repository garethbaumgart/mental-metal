## MODIFIED Requirements

### Requirement: AI extraction pipeline

The CaptureProcessingService SHALL orchestrate the extraction pipeline: (1) load the Capture, (2) call `IAiCompletionService.CompleteAsync` with a system prompt instructing the AI to extract structured data from the raw content, (3) parse the AI response into an `AiExtraction` value object, (4) call `CompleteProcessing(extraction)` on the Capture aggregate, and (5) persist the result. If the AI call fails, the service SHALL call `FailProcessing(reason)` on the Capture. The system prompt SHALL instruct the AI to include `source_start_offset` (0-based character index where the commitment starts in the input text) and `source_end_offset` (exclusive character index where the commitment ends) for each extracted commitment. The system prompt SHALL also instruct the AI to classify the content type as one of `QuickNote`, `Transcript`, or `MeetingNotes` via a `detected_type` field. After successful extraction, the handler SHALL call `Reclassify(detectedType)` on the Capture aggregate if the detected type is a valid `CaptureType` other than `AudioRecording`, the current capture type is not `AudioRecording`, and the detected type differs from the current type. The handler SHALL skip reclassification entirely for `AudioRecording` captures.

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
- **THEN** the service calls FailProcessing with the error message as the reason
- **AND** the capture status transitions to Failed

#### Scenario: Taste limit exceeded

- **WHEN** the AI completion call throws a TasteLimitExceededException
- **THEN** the service calls FailProcessing with reason "Daily AI limit reached"
- **AND** the capture status transitions to Failed

### Requirement: AiExtraction value object

The system SHALL define an `AiExtraction` value object embedded on the Capture aggregate with the following properties: Summary (string, required), Commitments (list of extracted commitments with description, direction, person hint, optional due date, and optional source character offsets: SourceStartOffset and SourceEndOffset), Delegations (list of extracted delegations with description, person hint, and optional due date), Observations (list of extracted observations with description, person hint, and tag), Decisions (list of strings), RisksIdentified (list of strings), SuggestedPersonLinks (list of person name hints), SuggestedInitiativeLinks (list of initiative name hints), ConfidenceScore (decimal, 0.0-1.0), and DetectedCaptureType (nullable CaptureType indicating the AI's content classification).

#### Scenario: AiExtraction with all fields populated

- **WHEN** a transcript yields commitments, delegations, observations, decisions, and risks
- **THEN** the AiExtraction value object contains all extracted items with their respective properties
- **AND** each commitment includes SourceStartOffset and SourceEndOffset
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
