## ADDED Requirements

### Requirement: Inline processing queue on captures list page

The captures list page SHALL display a "Currently Processing" section above the main captures table when one or more captures have `processingStatus` of `Raw` or `Processing`. Each item in the queue SHALL show the capture title (or a truncated preview of raw content if no title), a spinner icon, and the current processing stage label. When no captures are processing, the section SHALL be hidden entirely.

#### Scenario: Processing queue visible with active items

- **WHEN** the user navigates to the captures list page and 2 captures have status `Processing`
- **THEN** a "Currently Processing" section appears above the captures table showing both items with spinners and stage labels

#### Scenario: Processing queue hidden when empty

- **WHEN** the user navigates to the captures list page and no captures have status `Raw` or `Processing`
- **THEN** the "Currently Processing" section is not rendered

#### Scenario: Item transitions from queue to table

- **WHEN** a capture in the processing queue transitions to `Processed` status
- **THEN** the item is removed from the "Currently Processing" section and appears in the main captures table on the next poll cycle

#### Scenario: Failed item in queue

- **WHEN** a capture in the processing queue transitions to `Failed` status
- **THEN** the item is removed from the processing queue and appears in the main table with a `Failed` status badge

### Requirement: Captures list polls for status updates

The captures list page SHALL poll `GET /api/captures` on a 3-second interval when the "Currently Processing" section is visible. Polling SHALL stop after 2 consecutive responses with no `Raw` or `Processing` captures. Polling SHALL restart when a new capture is created.

#### Scenario: Polling starts on page load with processing items

- **WHEN** the user loads the captures list page and 1 capture has status `Processing`
- **THEN** the page begins polling every 3 seconds

#### Scenario: Polling stops when queue empties

- **WHEN** all processing captures complete and 2 consecutive polls return no processing items
- **THEN** polling stops

#### Scenario: Polling restarts on new capture

- **WHEN** polling has stopped and the user creates a new capture
- **THEN** polling restarts at a 3-second interval

### Requirement: Global capture completion toast

A global service SHALL track capture IDs submitted during the current session. The service SHALL poll `GET /api/captures` every 5 seconds when tracking active captures. When a tracked capture transitions to `Processed`, the service SHALL display a PrimeNG success toast with the capture title and an extraction summary (commitment count, people count). When a tracked capture transitions to `Failed`, the service SHALL display an error toast. The toast SHALL include a "View" link that navigates to the capture detail page.

#### Scenario: Success toast on completion

- **WHEN** the user submits a capture from the quick-capture dialog and navigates to the dashboard
- **AND** the capture finishes processing in the background
- **THEN** a success toast appears showing the capture title, extraction summary, and a "View" link

#### Scenario: Failure toast on error

- **WHEN** a tracked capture transitions to `Failed` status
- **THEN** an error toast appears with the capture title and failure reason

#### Scenario: Toast navigation

- **WHEN** the user clicks the "View" link in a completion toast
- **THEN** the app navigates to `/capture/{id}` for that capture

#### Scenario: Tracking clears on completion

- **WHEN** a toast is shown for a tracked capture
- **THEN** the capture ID is removed from the tracking set and no further polls check for it

### Requirement: Optimistic queue entry on capture creation

When a capture is created via any method (text, file import, audio upload), the captures list (if mounted) SHALL optimistically add the new capture to the "Currently Processing" section using the data from the creation API response, without waiting for the next poll.

#### Scenario: Immediate queue entry after text capture

- **WHEN** the user submits a text capture via the quick-capture dialog while on the captures list page
- **THEN** the capture appears immediately in the "Currently Processing" section with status `Raw`
