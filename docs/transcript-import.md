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

### One-Click: Bookmarklet

The fastest way to import transcripts. Works on any Google Doc — no extension, no OAuth, no file download.

**Setup (once, ~30 seconds):**

1. Go to **Settings → Personal Access Tokens** and generate a token (copy it — it's shown only once).
2. Scroll down to **Settings → Bookmarklet**.
3. Paste your token into the field.
4. Drag the **"Import to Mental Metal"** button to your browser's bookmarks bar.

**Daily use:**

1. Open a Google Doc transcript (from Drive or a calendar event link).
2. Click the bookmarklet in your bookmarks bar.
3. A green toast confirms the import. Click "View capture" to see it in Mental Metal.

The bookmarklet uses Google Docs' export URL to fetch the document text (same-origin, uses your existing Google session) and POSTs it to Mental Metal via your PAT. No data passes through any third party. If the bookmarklet stops working (e.g., you revoked the token), generate a new PAT and re-install from Settings.
