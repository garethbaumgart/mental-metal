## MODIFIED Requirements

### Requirement: AI extraction pipeline

The CaptureProcessingService SHALL orchestrate the extraction pipeline: (1) load the Capture, (2) call `IAiCompletionService.CompleteAsync` with a system prompt instructing the AI to extract structured data from the raw content, (3) parse the AI response into an `AiExtraction` value object, (4) call `CompleteProcessing(extraction)` on the Capture aggregate, and (5) persist the result. If the AI call fails, the service SHALL call `FailProcessing(reason)` on the Capture. The system prompt SHALL instruct the AI to include `source_start_offset` (0-based character index where the commitment starts in the input text) and `source_end_offset` (exclusive character index where the commitment ends) for each extracted commitment.

#### Scenario: Successful extraction from a quick note

- **WHEN** the processing pipeline runs on a QuickNote capture with content "Follow up with Sarah on Q3 roadmap — she committed to the API spec by Friday"
- **THEN** the AI extracts a summary, identifies one commitment (TheirsToMe: "Deliver API spec", person: Sarah, due: Friday), and suggests person link to Sarah
- **AND** the extracted commitment includes `source_start_offset` and `source_end_offset` referencing the position in the input text
- **AND** CompleteProcessing is called with the AiExtraction value object

#### Scenario: Successful extraction from a transcript

- **WHEN** the processing pipeline runs on a Transcript capture containing a multi-topic meeting transcript
- **THEN** the AI extracts a summary, commitments, delegations, decisions, risks, observations, and suggested person/initiative links
- **AND** each extracted commitment includes `source_start_offset` and `source_end_offset`

#### Scenario: AI provider failure

- **WHEN** the AI completion call throws an AiProviderException
- **THEN** the service calls FailProcessing with the error message as the reason
- **AND** the capture status transitions to Failed

#### Scenario: Taste limit exceeded

- **WHEN** the AI completion call throws a TasteLimitExceededException
- **THEN** the service calls FailProcessing with reason "Daily AI limit reached"
- **AND** the capture status transitions to Failed

### Requirement: AiExtraction value object

The system SHALL define an `AiExtraction` value object embedded on the Capture aggregate with the following properties: Summary (string, required), Commitments (list of extracted commitments with description, direction, person hint, optional due date, and optional source character offsets: SourceStartOffset and SourceEndOffset), Delegations (list of extracted delegations with description, person hint, and optional due date), Observations (list of extracted observations with description, person hint, and tag), Decisions (list of strings), RisksIdentified (list of strings), SuggestedPersonLinks (list of person name hints), SuggestedInitiativeLinks (list of initiative name hints), and ConfidenceScore (decimal, 0.0–1.0).

#### Scenario: AiExtraction with all fields populated

- **WHEN** a transcript yields commitments, delegations, observations, decisions, and risks
- **THEN** the AiExtraction value object contains all extracted items with their respective properties
- **AND** each commitment includes SourceStartOffset and SourceEndOffset

#### Scenario: AiExtraction with minimal content

- **WHEN** a quick note yields only a summary and one commitment
- **THEN** the AiExtraction value object contains the summary and one commitment, with empty lists for other properties

#### Scenario: AiExtraction equality

- **WHEN** two AiExtraction instances have identical property values
- **THEN** they are considered equal

### Requirement: Spawn entities from extraction

The system SHALL, after successful extraction, offer the user a confirmation step before spawning entities. Spawnable entities include: Commitments (created via the commitment-tracking API), Delegations (created via the delegation-tracking API), and Observations (stored for people-lens). Each spawned entity SHALL have its SourceCaptureId set to the originating capture's ID. Each spawned Commitment SHALL have its SourceStartOffset and SourceEndOffset set from the extracted commitment's source offsets (if available).

#### Scenario: Confirm and spawn all extracted entities

- **WHEN** an authenticated user sends a POST to `/api/captures/{id}/confirm-extraction`
- **THEN** the system creates Commitment entities for each extracted commitment, Delegation entities for each extracted delegation, and Observation records for each extracted observation
- **AND** each spawned entity has SourceCaptureId set to the capture's ID
- **AND** each spawned Commitment has SourceStartOffset and SourceEndOffset set from the extraction data
- **AND** the capture raises a `CaptureExtractionConfirmed` domain event
