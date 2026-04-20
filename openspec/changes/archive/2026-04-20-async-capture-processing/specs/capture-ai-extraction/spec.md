## MODIFIED Requirements

### Requirement: Begin processing a capture

The system SHALL automatically trigger AI processing on a Capture after it is created, as a background task outside the HTTP request lifecycle. The extraction SHALL run within a new DI scope using `IServiceScopeFactory`, with the user context set via `IBackgroundUserScope.SetUserId()`. The capture's status SHALL transition from `Raw` to `Processing` at the start of extraction and to `Processed` or `Failed` upon completion. If the background task fails to start or the process terminates during extraction, the capture SHALL remain in its current status (`Raw` or `Processing`) and the user MAY retry via the existing retry mechanism.

#### Scenario: Background extraction triggered after capture creation

- **WHEN** a capture is created via POST /api/captures
- **THEN** the HTTP response returns immediately with the capture in `Raw` status
- **AND** a background task begins extraction within the same server process

#### Scenario: Background extraction completes successfully

- **WHEN** the background extraction task runs on a capture
- **THEN** the capture transitions from `Raw` → `Processing` → `Processed`
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
