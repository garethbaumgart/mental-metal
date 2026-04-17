# Transcript Import

Import externally-captured transcripts (e.g., Google Meet transcripts from Google Drive) into Mental Metal without requiring OAuth to the source system.

## How It Works

### Manual: File Drop via Quick Capture

1. In Google Drive, open your meeting transcript and download it (File → Download → `.txt`, `.docx`, or `.html`).
2. In Mental Metal, open Quick Capture (Cmd+K or the FAB).
3. Expand the **Advanced** section.
4. Drop the file onto the drop-zone, or click to browse.
5. Click **Capture**. The file is parsed server-side and enters the normal AI extraction pipeline.

### Programmatic: Personal Access Token + Import API

For external tools (like the upcoming bookmarklet):

1. Go to **Settings → Personal Access Tokens**.
2. Click **Generate Token**, name it, and copy the token (it's shown only once).
3. POST to the import endpoint:

```bash
curl -X POST https://your-instance/api/captures/import \
  -H "Authorization: Bearer mm_pat_YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{"type": "Transcript", "content": "Alice: We agreed to ship Friday.\nBob: I will update the docs by Thursday."}'
```

The endpoint also accepts `multipart/form-data` for file uploads:

```bash
curl -X POST https://your-instance/api/captures/import \
  -H "Authorization: Bearer mm_pat_YOUR_TOKEN_HERE" \
  -F "file=@transcript.docx"
```

### Supported File Formats

- `.txt` — plain text, UTF-8
- `.html` / `.htm` — Google Docs HTML export (markup stripped)
- `.docx` — Microsoft Word / Google Docs download

### Google Meet Transcript Detection

When imported content matches the Google Meet transcript format (speaker-labelled turns like `Alice: text`), the system preserves the speaker labels so the existing speaker-to-Person mapping UI works without extra handling.

## Coming Next

- **`transcript-bookmarklet`**: A one-click bookmarklet that exports the current Google Doc's text and POSTs it directly to Mental Metal via your PAT. No extension install required.
