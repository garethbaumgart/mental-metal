# Transcript Daily Ingest

Enhance the bulk upload experience to support a daily rhythm: the user exports the day's Google Meet transcripts from Drive, drag-drops them into Mental Metal, and the AI processes them all automatically.

## User Workflow

1. End of day: open Google Drive, navigate to transcript folder
2. Select the day's transcripts (Ctrl+A or multi-select), click Download → Google zips them as .docx
3. Unzip on local machine
4. Open Mental Metal → Captures page → click "Upload" or drag files onto the page
5. All files are ingested, parsed, and auto-extracted. User sees progress.
6. Next morning: daily brief reflects yesterday's uploads

## Upload UX

### Drag-Drop Zone
- Full-width drop zone on the Captures page (not hidden in a dialog)
- Accepts multiple files simultaneously
- Supported formats: `.docx`, `.txt`, `.html`
- Visual feedback: file count, total size, format detection per file
- Files that aren't supported formats show an error inline (don't block other files)

### Upload Progress
- Per-file progress indicator: uploading → parsing → extracting → done
- Files process in parallel (up to a configurable concurrency limit, default 3)
- Failed files show error with retry button (don't block other files)
- Summary at completion: "8 transcripts uploaded, 7 processed, 1 failed"

### Auto-Detection
- `.docx` files: parsed via `DocxTranscriptParser` (existing)
- `.html` files: parsed via `HtmlTranscriptParser` (existing)
- `.txt` files: parsed via `PlainTextTranscriptParser` (existing)
- Google Meet format detection: if content has `Speaker Name: text` pattern, set capture type to `transcript` and preserve speaker labels
- Default capture type: `transcript` for file uploads, `quick-note` for typed text

### Metadata Extraction
- Title: derived from filename (strip extension, clean up Google's naming convention)
- Date: attempt to extract from filename (Google Meet format includes date) or fall back to file modification date or upload time
- Source: `upload`

## API Changes

### Existing Endpoint (enhanced)
`POST /api/captures/import` — already supports JSON and multipart. Enhancements:

- Accept multiple files in a single multipart request (multiple `file` parts)
- Return a batch response: `{ results: [{ filename, captureId, status, error? }] }`
- Each file creates a separate Capture entity
- Processing (extraction) is triggered automatically for each — no separate process call

### New Endpoint
`POST /api/captures/import/batch` — dedicated batch endpoint if multipart multi-file is awkward:
- Accepts: `multipart/form-data` with multiple file parts
- Returns: `201 Created` with array of results
- Each file independently succeeds or fails

## Frontend Changes

### Captures Page
- Permanent drag-drop zone at top of page (not a modal/dialog)
- "Upload Transcripts" button as alternative to drag-drop
- When files are dropped/selected: show upload queue with per-file status
- After upload: new captures appear at top of list with "processing" badge
- Badge transitions to "done" when extraction completes (poll or WebSocket)

### Upload Queue Component
- Shows each file: name, size, format detected, status (uploading/parsing/extracting/done/failed)
- Failed files: show error message + retry button
- "Upload more" action to add additional files to the queue
- Queue clears automatically after all files complete (with brief delay for review)

## Relationship to Other Inputs

This spec covers file-based transcript upload only. The other two input channels are:
- Browser audio capture → see `browser-audio-capture` spec
- Quick note (voice/text) → see `quick-note-voice` spec

All three channels produce Capture entities that feed the same extraction pipeline.

## Acceptance Criteria

- [ ] Drag-drop zone on Captures page accepts multiple .docx/.txt/.html files
- [ ] Files upload and parse in parallel (configurable concurrency)
- [ ] Each file creates a separate Capture entity
- [ ] Extraction triggers automatically for each capture (no manual process call)
- [ ] Per-file progress visible: uploading → parsing → extracting → done
- [ ] Failed files show error and retry button without blocking others
- [ ] Batch summary shown on completion
- [ ] Title extracted from filename
- [ ] Date extracted from filename where possible
- [ ] Google Meet speaker format detected and preserved
- [ ] Capture source set to `upload`
- [ ] New captures appear in list immediately with processing status
- [ ] Status updates as extraction completes
- [ ] Unsupported file formats rejected with clear error message
- [ ] Integration tests cover: multi-file upload, mixed success/failure, format detection
