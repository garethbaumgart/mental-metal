## ADDED Requirements

### Requirement: Quick Capture drop-zone for document files

The Quick Capture dialog SHALL, within its existing "Advanced" section, provide a drop-zone and a file-picker button that accept a single file of type `.txt`, `.html`, `.htm`, or `.docx`. Selecting or dropping a file SHALL change the dialog's submit behaviour so that, on submit, the file is sent to `POST /api/captures/import` as `multipart/form-data` using the existing session cookie for authentication, instead of the JSON `POST /api/captures` call used on the typing happy path. The happy path (type text → Enter) SHALL be unaffected: the drop-zone lives inside the collapsed-by-default Advanced section and does not appear until the user expands it. On successful upload, the dialog SHALL close and display a toast with a "View capture" action linking to the created capture.

#### Scenario: Drop-zone lives inside the Advanced section

- **WHEN** an authenticated user opens the Quick Capture dialog
- **THEN** the drop-zone is not visible until the user expands the Advanced section; the happy path (textarea focused, Enter submits) is unchanged

#### Scenario: Drop a .docx file from the Quick Capture dialog

- **WHEN** an authenticated user expands the Advanced section, drops a `.docx` file onto the drop-zone, and submits
- **THEN** the file is sent to `POST /api/captures/import` as multipart/form-data with the session cookie, a Capture is created with type `Transcript`, the dialog closes, and a toast appears with a "View capture" action

#### Scenario: Pick a .txt file via the file-picker button

- **WHEN** an authenticated user expands the Advanced section, clicks the file-picker button, selects a `.txt` file, and submits
- **THEN** the file is sent to `POST /api/captures/import` and a Capture is created

#### Scenario: Unsupported file type disabled

- **WHEN** an authenticated user attempts to drop or pick a file whose extension is not `.txt`, `.html`, `.htm`, or `.docx`
- **THEN** the dialog displays an inline error and the file is not attached

#### Scenario: Typing happy path is unaffected

- **WHEN** an authenticated user opens the Quick Capture dialog, types content, and submits without expanding the Advanced section or selecting a file
- **THEN** the submission goes to `POST /api/captures` (not `/import`) with JSON, exactly as before this change

#### Scenario: File and typed content are mutually exclusive on submit

- **WHEN** an authenticated user has both typed content in the textarea and attached a file
- **THEN** submission sends the file to `/api/captures/import` with the filename-derived defaults and the typed content is discarded with a confirmation prompt before submit
