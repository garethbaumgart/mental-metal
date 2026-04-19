## ADDED Requirements

### Requirement: Import capture from JSON body

The system SHALL accept `POST /api/captures/import` with `Content-Type: application/json` and body `{ type, content, sourceUrl?, title?, meetingAt? }` from a user authenticated via either session cookie or a Personal Access Token with the `captures:write` scope. `type` SHALL be one of `Transcript` or `QuickNote`. `content` SHALL be non-empty. The system SHALL create a `Capture` aggregate with `RawContent = content`, `CaptureType = type`, `ProcessingStatus = Raw`, the authenticated user's `UserId`, and any provided optional metadata. The system SHALL raise `CaptureCreated` and return HTTP 201 with the new capture id.

#### Scenario: JSON import with cookie auth

- **WHEN** an authenticated user (session cookie) POSTs `/api/captures/import` with `{ "type": "Transcript", "content": "Sarah: We agreed to ship Friday." }`
- **THEN** a Capture is created with type Transcript, the given content, status Raw, and HTTP 201 is returned

#### Scenario: JSON import with PAT auth

- **WHEN** a client POSTs `/api/captures/import` with `Authorization: Bearer mm_pat_<valid-token-with-captures:write>` and a JSON body
- **THEN** a Capture is created scoped to the token's owning user and HTTP 201 is returned

#### Scenario: PAT without captures:write scope rejected

- **WHEN** a client POSTs `/api/captures/import` with a valid PAT lacking the `captures:write` scope
- **THEN** the system returns HTTP 403 and no Capture is created

#### Scenario: Empty content rejected

- **WHEN** a client POSTs `/api/captures/import` with empty or whitespace-only `content`
- **THEN** the system returns HTTP 400 with a validation error

#### Scenario: Unsupported type rejected

- **WHEN** a client POSTs `/api/captures/import` with `type` set to a value other than `Transcript` or `QuickNote`
- **THEN** the system returns HTTP 400 with a validation error

#### Scenario: Optional metadata preserved

- **WHEN** a client POSTs `/api/captures/import` with `title`, `sourceUrl`, and `meetingAt` populated
- **THEN** the Capture's `Title`, `Source`, and any meeting-time metadata are set from the request and returned on subsequent GETs

### Requirement: Import capture from file upload

The system SHALL accept `POST /api/captures/import` with `Content-Type: multipart/form-data` containing a single file field (the document) and optional text fields `type`, `title`, `sourceUrl`, `meetingAt`. Supported file content types SHALL be `text/plain`, `text/html`, and `application/vnd.openxmlformats-officedocument.wordprocessingml.document`. Filename extensions `.txt`, `.html`, `.htm`, and `.docx` SHALL be accepted as secondary signals when the content type is generic (e.g., `application/octet-stream`). The system SHALL parse the file to plain text using the matching parser and create a Capture identically to the JSON path. If `type` is not provided, the system SHALL default to `Transcript` when the file appears to be a document (`.docx`/`.html`/`.htm`) and `QuickNote` when it is a plain-text file.

#### Scenario: Upload a .docx file

- **WHEN** an authenticated user POSTs `/api/captures/import` with a `.docx` file in the `file` field
- **THEN** the system parses the document to plain text, creates a Capture with type `Transcript`, stores the parsed content as `RawContent`, and returns HTTP 201

#### Scenario: Upload an .html file

- **WHEN** an authenticated user POSTs `/api/captures/import` with a `.html` file (Google Docs HTML export) in the `file` field
- **THEN** the system strips the HTML markup to plain text, creates a Capture with type `Transcript`, and returns HTTP 201

#### Scenario: Upload a .txt file

- **WHEN** an authenticated user POSTs `/api/captures/import` with a `.txt` file
- **THEN** the system reads the file as UTF-8 text, creates a Capture, and returns HTTP 201

#### Scenario: Unsupported content type rejected

- **WHEN** a client POSTs `/api/captures/import` with a file whose content type is outside the supported set (e.g., `application/pdf` or `image/png`)
- **THEN** the system returns HTTP 415 Unsupported Media Type

#### Scenario: Oversize file rejected

- **WHEN** a client POSTs `/api/captures/import` with a file larger than 10 MB
- **THEN** the system returns HTTP 413 Payload Too Large

#### Scenario: Malformed document handled gracefully

- **WHEN** a client POSTs `/api/captures/import` with a corrupted `.docx` file the parser cannot read
- **THEN** the system returns HTTP 400 with a generic validation error and no Capture is created

#### Scenario: Explicit type overrides filename default

- **WHEN** a client POSTs `/api/captures/import` with a `.docx` file and `type` set to `QuickNote`
- **THEN** the created Capture has type `QuickNote`

### Requirement: Parse Google Meet transcript formatting

The system SHALL inspect imported content (from JSON or file paths) for Google Meet transcript formatting — characterised by one or more of: a heading line matching "Summary" or "Transcript", and lines matching `^[A-Z][A-Za-z ''-]{1,40}:\s` indicating speaker turns. When the pattern is matched, the system SHALL normalize line endings, preserve speaker-labelled turns intact so each turn is on its own line prefixed with `Speaker name:`, and trim runs of whitespace. When the pattern is not matched, the system SHALL store the content as provided (after basic line-ending normalisation). Format detection SHALL NOT reject any input; it is a best-effort normalizer.

#### Scenario: Meet-formatted transcript preserves speaker labels

- **WHEN** a user imports a transcript whose body contains `Summary\n...\nTranscript\nAlice: hi\nBob: hello`
- **THEN** the stored `RawContent` preserves `Alice: hi` and `Bob: hello` as separate lines so the existing speaker-mapping UI can bind labels to Person records

#### Scenario: Non-Meet content stored raw

- **WHEN** a user imports content that does not match the Meet pattern (e.g., a plain narrative note)
- **THEN** the stored `RawContent` is the original text with only line-ending normalisation applied

#### Scenario: Detection does not gate import

- **WHEN** format detection fails or yields no matches
- **THEN** the Capture is still created successfully; the endpoint does not return a validation error based on format

### Requirement: Import endpoint CORS allowlist

The `POST /api/captures/import` endpoint SHALL accept cross-origin requests from `https://docs.google.com` and `https://calendar.google.com`, permitting the `POST` method and `Authorization` and `Content-Type` headers. No other API endpoints SHALL accept cross-origin requests from those origins. Credentials mode on the CORS policy SHALL be disabled so that PAT-based authentication, not session cookies, is used from those origins.

#### Scenario: Preflight from docs.google.com allowed on import route

- **WHEN** a browser sends a CORS preflight (`OPTIONS`) from origin `https://docs.google.com` to `POST /api/captures/import`
- **THEN** the system responds with an `Access-Control-Allow-Origin: https://docs.google.com` header and permits the request

#### Scenario: Preflight from docs.google.com rejected on other routes

- **WHEN** a browser sends a CORS preflight from origin `https://docs.google.com` to `POST /api/captures`
- **THEN** the system does NOT include an `Access-Control-Allow-Origin` header for that origin

#### Scenario: Unknown origin rejected on import route

- **WHEN** a browser sends a CORS preflight from an origin not in the allowlist to `POST /api/captures/import`
- **THEN** the system does NOT include an `Access-Control-Allow-Origin` header for that origin

### Requirement: Imported captures flow through the standard extraction pipeline

The system SHALL treat imported captures identically to captures created via `POST /api/captures` for the purposes of downstream processing. Imported captures SHALL start in `ProcessingStatus.Raw` and SHALL be eligible for the same AI extraction pipeline, daily close-out triage, and linking flows.

#### Scenario: Imported capture is processable

- **WHEN** a capture is imported via `/api/captures/import` and the user subsequently triggers processing via the existing process endpoint
- **THEN** the capture transitions Raw → Processing → Processed (or Failed) identically to a capture created via `POST /api/captures`

#### Scenario: Imported capture appears in triage queue

- **WHEN** an imported capture has `ProcessingStatus = Raw` and `Triaged = false`
- **THEN** the capture appears in the daily close-out triage queue alongside captures from other sources

### Requirement: Authorization header never logged

The system SHALL ensure that the full value of the `Authorization` header is never written to application logs, error records, or audit trails. Logging middleware SHALL redact the header value before emission.

#### Scenario: Authorization header redacted on successful request

- **WHEN** a PAT-authenticated import succeeds and the request is logged
- **THEN** the logged record does not contain the plaintext token or the `Authorization` header value

#### Scenario: Authorization header redacted on failure

- **WHEN** a PAT-authenticated import fails and an error is logged
- **THEN** the logged error does not contain the plaintext token or the `Authorization` header value
