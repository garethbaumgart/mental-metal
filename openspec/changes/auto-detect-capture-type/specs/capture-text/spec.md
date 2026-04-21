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

The frontend SHALL provide a detail view for a single capture showing the full raw content, metadata (type, status, title, source, timestamps), and linked people and initiatives. The detail view SHALL allow editing the title and source, and linking/unlinking people and initiatives. When the capture has been processed and the `AiExtraction` contains a `DetectedCaptureType` that differs from the capture's original creation type, the detail view SHALL display a "Detected as: {type}" indicator near the type badge.

#### Scenario: View capture detail

- **WHEN** a user clicks on a capture in the list
- **THEN** the system navigates to the detail view showing the full content, metadata, and linked entities

#### Scenario: Edit metadata from detail view

- **WHEN** a user edits the title or source in the detail view and saves
- **THEN** the metadata is updated and the view reflects the changes

#### Scenario: Link person from detail view

- **WHEN** a user adds a person link from the detail view
- **THEN** the person appears in the linked people section

#### Scenario: Display detected type when reclassified

- **WHEN** a user views a processed capture that was reclassified from QuickNote to Transcript
- **THEN** the detail view shows the current type as "Transcript" and displays a "Detected as: Transcript" indicator
