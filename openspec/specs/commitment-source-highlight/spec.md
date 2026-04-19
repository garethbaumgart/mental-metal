# Commitment Source Highlight

> **Offset conventions**: All offsets are 0-based character indices into the capture's `RawContent` string. `SourceEndOffset` is exclusive (the character at `endOffset` is not included). These map as: AI JSON (`source_start_offset`/`source_end_offset`) → Domain (`SourceStartOffset`/`SourceEndOffset`) → API (`sourceStartOffset`/`sourceEndOffset`) → Query params (`highlightStart`/`highlightEnd`).

## Requirements

### Requirement: Navigate from commitment to highlighted source text

The system SHALL, when a user clicks the source capture link on a commitment detail page, navigate to the capture detail page with query params `highlightStart` and `highlightEnd` set to the commitment's `SourceStartOffset` and `SourceEndOffset` values. If either offset is null, the system SHALL navigate without query params (current behavior).

#### Scenario: Navigate with valid offsets

- **WHEN** a user views a commitment with `sourceCaptureId` set and `sourceStartOffset=1234` and `sourceEndOffset=1456`
- **THEN** clicking the source link navigates to `/capture/:captureId?highlightStart=1234&highlightEnd=1456`

#### Scenario: Navigate without offsets

- **WHEN** a user views a commitment with `sourceCaptureId` set but `sourceStartOffset` and `sourceEndOffset` are null
- **THEN** clicking the source link navigates to `/capture/:captureId` without highlight query params

### Requirement: Highlight text in raw content view

The capture detail page SHALL, when `highlightStart` and `highlightEnd` query params are present, split the `RawContent` into three segments (before, highlighted, after) and render the highlighted segment inside a `<mark>` element. The system SHALL scroll the `<mark>` element into view with smooth scrolling after the content renders.

#### Scenario: Highlight a passage in a text capture

- **WHEN** a user navigates to `/capture/:id?highlightStart=100&highlightEnd=200` for a text capture with `RawContent` of 500 characters
- **THEN** the page renders characters 0–99 as plain text, characters 100–199 inside a `<mark>` element with highlight styling, and characters 200–499 as plain text
- **AND** the `<mark>` element is scrolled into view

#### Scenario: Offsets exceed content length

- **WHEN** a user navigates to `/capture/:id?highlightStart=100&highlightEnd=9999` for a capture with `RawContent` of 200 characters
- **THEN** the system clamps `highlightEnd` to 200 and highlights characters 100–199

#### Scenario: Invalid offsets are ignored

- **WHEN** a user navigates to `/capture/:id?highlightStart=200&highlightEnd=100` (start >= end)
- **THEN** the system renders `RawContent` without any highlighting (graceful degradation)

#### Scenario: No highlight params present

- **WHEN** a user navigates to `/capture/:id` without highlight query params
- **THEN** the capture detail page renders normally with no highlighting

### Requirement: Highlight text in transcript viewer

The transcript viewer component SHALL, when highlight offsets are provided as input signals, determine which transcript segment(s) contain the highlighted character range by computing cumulative character offsets across segments ordered by `startSeconds`. The overlapping text within the relevant segment(s) SHALL be rendered inside a `<mark>` element and scrolled into view.

#### Scenario: Highlight within a single transcript segment

- **WHEN** the transcript viewer receives `highlightStart=500` and `highlightEnd=550` and the target text falls within one segment
- **THEN** that segment renders with the matching substring inside a `<mark>` element
- **AND** the highlighted segment is scrolled into view

#### Scenario: Highlight spanning multiple segments

- **WHEN** the transcript viewer receives highlight offsets that span two consecutive segments
- **THEN** both segments render their respective overlapping portions inside `<mark>` elements
- **AND** the first highlighted segment is scrolled into view

#### Scenario: Offsets clamped to segment boundaries

- **WHEN** the transcript viewer receives highlight offsets that partially exceed the total segment text length
- **THEN** the system clamps the end offset to the total text length and highlights the valid portion

#### Scenario: No matching segment found

- **WHEN** the transcript viewer receives highlight offsets that don't map to any segment (e.g., offsets entirely beyond total text length)
- **THEN** the transcript renders normally without any highlighting

### Requirement: Highlight styling

The highlight `<mark>` element SHALL use PrimeNG design token variables for styling: a semi-transparent yellow background using `color-mix(in srgb, var(--p-yellow-100) 70%, transparent)` and a bottom border using `var(--p-yellow-500)`. The highlight SHALL be visible in both light and dark themes.

#### Scenario: Highlight visible in light mode

- **WHEN** the app is in light mode and a highlight is rendered
- **THEN** the `<mark>` element has a visible yellow background and bottom border

#### Scenario: Highlight visible in dark mode

- **WHEN** the app is in dark mode and a highlight is rendered
- **THEN** the `<mark>` element has a visible yellow-tinted background and bottom border that contrasts with the dark surface
