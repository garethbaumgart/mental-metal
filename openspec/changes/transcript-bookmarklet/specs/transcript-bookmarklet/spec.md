## ADDED Requirements

### Requirement: Bookmarklet extracts Google Doc text and imports to Mental Metal

The bookmarklet SHALL, when clicked on a page with hostname `docs.google.com`, extract the document ID from the current URL (supporting both `/document/d/<ID>/` and `/document/u/<N>/d/<ID>/` patterns), fetch the document's plain text via the Google Docs export URL (`/document/d/<ID>/export?format=txt`) using the user's existing browser session, and POST the content as JSON to the user's Mental Metal instance at `POST /api/captures/import` with `Authorization: Bearer <PAT>`, `Content-Type: application/json`, and body `{ "type": "Transcript", "content": "<text>", "title": "<doc-title>", "sourceUrl": "<doc-url>" }`. The document title SHALL have the ` - Google Docs` suffix stripped before use.

#### Scenario: Import a Google Doc transcript

- **WHEN** a user clicks the bookmarklet while viewing a Google Doc at `docs.google.com/document/d/abc123/edit`
- **THEN** the bookmarklet fetches the document text via the export URL, POSTs it to the user's Mental Metal instance as a Transcript capture with the document title and URL, and shows a success toast

#### Scenario: Import from multi-account Google URL

- **WHEN** a user clicks the bookmarklet while viewing a Google Doc at `docs.google.com/document/u/0/d/abc123/edit`
- **THEN** the bookmarklet correctly extracts the document ID `abc123` and imports the transcript

#### Scenario: Non-Google-Doc page shows error

- **WHEN** a user clicks the bookmarklet on a page whose hostname is not `docs.google.com` or whose URL does not contain `/document/d/` or `/document/u/<N>/d/`
- **THEN** the bookmarklet shows an error toast "Not a Google Doc" and makes no network requests

#### Scenario: Export fetch failure shows error

- **WHEN** a user clicks the bookmarklet on a Google Doc but the export fetch fails (e.g., network error, 403)
- **THEN** the bookmarklet shows an error toast with the failure reason and does not POST to Mental Metal

#### Scenario: Import POST failure shows error

- **WHEN** the export fetch succeeds but the POST to Mental Metal fails (e.g., expired PAT, network error)
- **THEN** the bookmarklet shows an error toast with the failure reason (e.g., "401 Unauthorized — check your token in Settings")

### Requirement: Bookmarklet shows visual feedback via injected toast

The bookmarklet SHALL inject a fixed-position toast element into the Google Docs page DOM showing the result of the import operation. The toast SHALL use inline styles (no external CSS) to avoid interference with Google Docs' stylesheets. Success toasts SHALL auto-dismiss after 4 seconds; error toasts SHALL auto-dismiss after 6 seconds. On success, the toast SHALL include a link to the created capture in the user's Mental Metal instance (opening in a new tab). On error, the toast SHALL show the error reason.

#### Scenario: Success toast with capture link

- **WHEN** a transcript is successfully imported
- **THEN** a green toast appears at the top of the page reading "Imported to Mental Metal" with a "View capture" link that opens `<instanceUrl>/capture/<id>` in a new tab, and the toast auto-dismisses after 4 seconds

#### Scenario: Error toast auto-dismisses

- **WHEN** an import fails
- **THEN** a red toast appears at the top of the page with the error message and auto-dismisses after 6 seconds

### Requirement: Bookmarklet is self-contained with baked-in config

The bookmarklet SHALL be a single `javascript:` URL with no external script dependencies, no `localStorage` reads, and no popup windows. The user's Mental Metal instance URL and PAT SHALL be embedded directly in the bookmarklet source at generation time. The bookmarklet SHALL function without any prior setup on the target page.

#### Scenario: Bookmarklet works without prior setup

- **WHEN** a user drags the generated bookmarklet to their bookmarks bar and clicks it on a Google Doc for the first time
- **THEN** the import succeeds without any additional setup, login, or configuration prompt

#### Scenario: Bookmarklet contains no external dependencies

- **WHEN** the bookmarklet executes
- **THEN** no external scripts are loaded via `<script>` tags or dynamic imports; all logic is self-contained in the `javascript:` URL

### Requirement: Settings page bookmarklet installer

The Settings page SHALL include a "Bookmarklet" section (below the Personal Access Tokens section) that allows a user to generate and install the bookmarklet. The section SHALL display the Mental Metal instance URL (auto-detected from `window.location.origin`). The section SHALL allow the user to paste their PAT (shown once at creation) into a text field. When a valid PAT (starting with `mm_pat_`) is entered, the section SHALL display a draggable link element styled as a button containing the generated `javascript:` URL, with the label "Import to Mental Metal". The section SHALL include brief usage instructions.

#### Scenario: Auto-detect instance URL

- **WHEN** a user navigates to the Settings bookmarklet section
- **THEN** the instance URL is pre-filled with the current `window.location.origin`

#### Scenario: Paste PAT and generate bookmarklet

- **WHEN** a user pastes a valid PAT (starting with `mm_pat_`) into the text field
- **THEN** the draggable bookmarklet link appears with the PAT and instance URL baked in

#### Scenario: No PATs available

- **WHEN** a user has no active PATs with `captures:write` scope
- **THEN** the section shows a message prompting the user to generate a PAT first, with a link to the PAT section above

#### Scenario: Drag to bookmarks bar

- **WHEN** a user drags the generated bookmarklet link to their browser's bookmarks bar
- **THEN** the bookmark is created with the `javascript:` URL and can be clicked on any page

#### Scenario: Usage instructions displayed

- **WHEN** a user views the bookmarklet installer section
- **THEN** brief instructions are displayed: (1) Select a token, (2) Drag the button to your bookmarks bar, (3) Open any Google Doc transcript and click it
