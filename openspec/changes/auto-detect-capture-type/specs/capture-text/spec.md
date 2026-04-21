## ADDED Requirements

### Requirement: Reclassify capture type

The Capture aggregate SHALL expose a `Reclassify(CaptureType newType)` method that changes the capture's `CaptureType`. The method SHALL only be callable when `ProcessingStatus` is `Processing`. The method SHALL reject reclassification to `AudioRecording` by throwing a domain exception. If the new type is the same as the current type, the method SHALL be a no-op. On successful reclassification, the aggregate SHALL raise a `CaptureReclassified` domain event containing the capture ID, the old type, and the new type.

#### Scenario: Reclassify from QuickNote to Transcript during processing

- **WHEN** `Reclassify(CaptureType.Transcript)` is called on a capture with type `QuickNote` and status `Processing`
- **THEN** the capture's `CaptureType` changes to `Transcript`
- **AND** a `CaptureReclassified` domain event is raised with oldType `QuickNote` and newType `Transcript`

#### Scenario: Reclassify from QuickNote to MeetingNotes during processing

- **WHEN** `Reclassify(CaptureType.MeetingNotes)` is called on a capture with type `QuickNote` and status `Processing`
- **THEN** the capture's `CaptureType` changes to `MeetingNotes`
- **AND** a `CaptureReclassified` domain event is raised with oldType `QuickNote` and newType `MeetingNotes`

#### Scenario: No-op when new type matches current type

- **WHEN** `Reclassify(CaptureType.QuickNote)` is called on a capture with type `QuickNote`
- **THEN** the capture's `CaptureType` remains `QuickNote`
- **AND** no domain event is raised

#### Scenario: Reject reclassification to AudioRecording

- **WHEN** `Reclassify(CaptureType.AudioRecording)` is called on any capture
- **THEN** the system throws a domain exception indicating that reclassification to AudioRecording is not allowed

#### Scenario: Reject reclassification when not in Processing status

- **WHEN** `Reclassify(CaptureType.Transcript)` is called on a capture with status `Raw`
- **THEN** the system throws a domain exception indicating reclassification is only allowed during processing

#### Scenario: Reject reclassification when in Processed status

- **WHEN** `Reclassify(CaptureType.Transcript)` is called on a capture with status `Processed`
- **THEN** the system throws a domain exception indicating reclassification is only allowed during processing

## MODIFIED Requirements

### Requirement: Capture detail view

The frontend SHALL provide a detail view for a single capture showing the full raw content, metadata (type, status, title, source, timestamps), and linked people and initiatives. The detail view SHALL allow editing the title and source, and linking/unlinking people and initiatives. When the capture has been processed and the `AiExtraction` contains a non-null `DetectedCaptureType`, the detail view SHALL display a "Detected as: {type}" indicator near the type badge. The indicator compares `DetectedCaptureType` against the capture's current persisted `CaptureType` -- if they match (i.e., the capture was reclassified or was already correct), the indicator serves as confirmation; if they differ (which would only happen if reclassification was skipped, e.g., for AudioRecording captures), the indicator highlights the discrepancy.

#### Scenario: View capture detail

- **WHEN** a user clicks on a capture in the list
- **THEN** the system navigates to the detail view showing the full content, metadata, and linked entities

#### Scenario: Edit metadata from detail view

- **WHEN** a user edits the title or source in the detail view and saves
- **THEN** the metadata is updated and the view reflects the changes

#### Scenario: Link person from detail view

- **WHEN** a user adds a person link from the detail view
- **THEN** the person appears in the linked people section

#### Scenario: Display detected type after reclassification

- **WHEN** a user views a processed capture whose `CaptureType` is `Transcript` and whose `AiExtraction.DetectedCaptureType` is `Transcript`
- **THEN** the detail view shows the type badge as "Transcript" and displays a "Detected as: Transcript" indicator

#### Scenario: Display detected type for AudioRecording capture

- **WHEN** a user views a processed `AudioRecording` capture whose `AiExtraction.DetectedCaptureType` is `Transcript`
- **THEN** the detail view shows the type badge as "AudioRecording" and displays a "Detected as: Transcript" indicator highlighting the discrepancy

#### Scenario: No detected type indicator when DetectedCaptureType is null

- **WHEN** a user views a processed capture whose `AiExtraction.DetectedCaptureType` is null
- **THEN** the detail view shows the type badge only, with no "Detected as" indicator
