## MODIFIED Requirements

### Requirement: Quick capture input

The frontend SHALL provide a Quick Capture dialog that is reachable from every authenticated page via (a) a global keyboard shortcut and (b) a persistent floating action button (FAB). The dialog's happy path SHALL require only the content textarea; the client SHALL default new captures to `CaptureType.QuickNote`. Type selection, Title, and Source SHALL be available in an "Advanced" section that is collapsed by default. Submitting the form SHALL create a new capture via the existing `POST /api/captures` endpoint, passing the defaulted or user-selected type.

#### Scenario: Quick capture submission with defaults

- **WHEN** an authenticated user opens the Quick Capture dialog, enters text, and submits without expanding the Advanced section
- **THEN** the system creates a capture with `type = QuickNote` and the entered content, and the capture is added to the user's capture list

#### Scenario: Quick capture with Advanced metadata

- **WHEN** an authenticated user opens the Quick Capture dialog, enters text, expands the Advanced section, selects type "Transcript", sets title "Standup notes", and submits
- **THEN** the system creates a capture with type Transcript, title "Standup notes", and the entered content

#### Scenario: Empty content disables submit

- **WHEN** the Quick Capture dialog is open and the content textarea is empty or whitespace-only
- **THEN** the Capture submit button is disabled and Enter does not submit

#### Scenario: Advanced section collapsed by default

- **WHEN** an authenticated user opens the Quick Capture dialog
- **THEN** the Advanced section (containing Type, Title, and Source) is collapsed and the content textarea is focused

## ADDED Requirements

### Requirement: Global Quick Capture keyboard shortcut

The frontend SHALL register a global keyboard shortcut that opens the Quick Capture dialog from any authenticated page. The shortcut SHALL be `Cmd+K` on macOS and `Ctrl+K` on non-macOS platforms. The shortcut SHALL call `preventDefault()` to override any browser default. The shortcut SHALL NOT fire on unauthenticated pages (login, sign-up). When the Quick Capture dialog is already open, the shortcut SHALL be a no-op.

#### Scenario: Shortcut opens dialog on macOS

- **WHEN** an authenticated user on macOS presses Cmd+K on any authenticated page other than the one hosting the capture list
- **THEN** the Quick Capture dialog opens with focus in the content textarea

#### Scenario: Shortcut opens dialog on Windows/Linux

- **WHEN** an authenticated user on Windows or Linux presses Ctrl+K on any authenticated page
- **THEN** the Quick Capture dialog opens with focus in the content textarea

#### Scenario: Shortcut does not fire on unauthenticated pages

- **WHEN** an unauthenticated user on the login page presses Cmd+K or Ctrl+K
- **THEN** the Quick Capture dialog does not open and the browser default behavior is preserved

#### Scenario: Shortcut no-op when dialog open

- **WHEN** the Quick Capture dialog is already open and the user presses Cmd+K or Ctrl+K
- **THEN** the dialog remains open unchanged (no re-open, no focus reset)

### Requirement: Persistent Quick Capture floating action button

The frontend SHALL render a persistent floating action button (FAB) on every authenticated page. Clicking the FAB SHALL open the Quick Capture dialog. The FAB SHALL use PrimeNG theming (no hardcoded colours) and SHALL be positioned fixed in the bottom-right of the viewport. The FAB SHALL have an accessible label that mentions the keyboard shortcut.

#### Scenario: FAB visible on authenticated pages

- **WHEN** an authenticated user is on any authenticated page (captures list, people, initiatives, settings, etc.)
- **THEN** the Quick Capture FAB is visible fixed in the bottom-right of the viewport

#### Scenario: FAB hidden on unauthenticated pages

- **WHEN** a user is on an unauthenticated page such as the login page
- **THEN** the Quick Capture FAB is not rendered

#### Scenario: FAB click opens dialog

- **WHEN** an authenticated user clicks the Quick Capture FAB
- **THEN** the Quick Capture dialog opens with focus in the content textarea

#### Scenario: FAB has accessible label

- **WHEN** a screen reader user focuses the Quick Capture FAB
- **THEN** the accessible label announces "Quick capture" and includes the keyboard shortcut hint

### Requirement: Enter submits Quick Capture

The Quick Capture dialog SHALL submit the capture when the user presses Enter (without Shift) inside the content textarea, provided the content is non-empty. Shift+Enter SHALL insert a newline without submitting. Cmd+Enter on macOS and Ctrl+Enter on non-macOS SHALL also submit from anywhere within the dialog.

#### Scenario: Enter submits

- **WHEN** the user has entered non-empty content in the Quick Capture dialog and presses Enter without Shift
- **THEN** the capture is submitted and the dialog closes on success

#### Scenario: Shift+Enter inserts newline

- **WHEN** the user presses Shift+Enter inside the content textarea
- **THEN** a newline is inserted in the textarea and no submission occurs

#### Scenario: Cmd/Ctrl+Enter submits from dialog

- **WHEN** the user presses Cmd+Enter (macOS) or Ctrl+Enter (other) anywhere inside the open dialog with non-empty content
- **THEN** the capture is submitted

#### Scenario: Enter with empty content does nothing

- **WHEN** the user presses Enter in the Quick Capture dialog while the content textarea is empty or whitespace-only
- **THEN** no submission occurs and the dialog remains open
