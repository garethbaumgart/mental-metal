# Capture AI Extraction

## Requirements

### Requirement: Begin processing a capture

The system SHALL allow an authenticated user to trigger AI processing on a Capture with status `Raw` by sending a POST to `/api/captures/{id}/process`. The system SHALL call `BeginProcessing()` on the Capture aggregate (transitioning status to `Processing`) and enqueue an asynchronous processing job. If the capture is not in status `Raw`, the system SHALL return HTTP 409 Conflict.

#### Scenario: Trigger processing on a raw capture

- **WHEN** an authenticated user sends a POST to `/api/captures/{id}/process` for a capture with status Raw
- **THEN** the system transitions the capture to Processing and returns HTTP 202 Accepted

#### Scenario: Trigger processing on a non-raw capture

- **WHEN** an authenticated user sends a POST to `/api/captures/{id}/process` for a capture with status Processing or Processed
- **THEN** the system returns HTTP 409 Conflict

#### Scenario: Capture not found

- **WHEN** an authenticated user sends a POST to `/api/captures/{id}/process` for a capture that does not exist or belongs to another user
- **THEN** the system returns HTTP 404

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

#### Scenario: Confirm extraction with person matching

- **WHEN** the extraction contains person hints (e.g., "Sarah") and the user has a Person named "Sarah Chen"
- **THEN** the system matches the hint to the existing Person by name similarity and sets the PersonId on spawned entities

#### Scenario: Confirm extraction with no person match

- **WHEN** the extraction contains a person hint that does not match any existing Person
- **THEN** the spawned entity is created without a PersonId and the user can link it manually

#### Scenario: Discard extraction

- **WHEN** an authenticated user sends a POST to `/api/captures/{id}/discard-extraction`
- **THEN** the system does NOT spawn any entities
- **AND** the AiExtraction is retained on the capture for reference
- **AND** the capture raises a `CaptureExtractionDiscarded` domain event

#### Scenario: Confirm on non-processed capture rejected

- **WHEN** an authenticated user sends a POST to `/api/captures/{id}/confirm-extraction` for a capture that is not in Processed status
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
