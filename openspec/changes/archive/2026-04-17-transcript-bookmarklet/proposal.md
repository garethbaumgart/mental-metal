## Why

`transcript-import-foundation` shipped the backend: PATs, `POST /api/captures/import`, CORS for `docs.google.com`, and a file-drop fallback in Quick Capture. But the daily workflow is still three steps — open the transcript in Google Docs, File → Download, drag into Mental Metal. That's too much friction for a manager who has 6–8 meetings a day and needs capture to be near-zero effort.

The bookmarklet collapses those three steps into one click. It runs inside the user's already-authenticated Google Docs session, fetches the document as plain text via Google's same-origin export URL, and POSTs it directly to the Mental Metal import endpoint using a pre-configured PAT. No extension install, no OAuth, no file download. The user installs it once from Settings and clicks it on every transcript thereafter.

## What Changes

- Add a **bookmarklet generator** in Settings that produces a self-contained `javascript:` URL with the user's instance URL and PAT baked in. The user drags it to their bookmarks bar once.
- The bookmarklet, when clicked on a Google Doc:
  1. Extracts the document ID from the current URL.
  2. Fetches `/document/d/<ID>/export?format=txt` (same-origin on `docs.google.com`, uses the user's browser session — no OAuth needed).
  3. POSTs the text to the user's Mental Metal instance via PAT-authenticated JSON.
  4. Shows a non-intrusive toast banner on the Google Docs page: success or failure with reason.
- If clicked on a non-Google-Doc page, shows an error toast and exits.
- The bookmarklet is a **static asset** (no server-side rendering) — the Settings page generates it client-side from a template + user-supplied config (instance URL + PAT).

## Capabilities

### New Capabilities

- `transcript-bookmarklet`: The bookmarklet JavaScript, the Settings installer UI, and the documentation for the one-click import workflow.

### Modified Capabilities

_None._ The import endpoint, PAT auth, CORS policy, and TranscriptFormatDetector are all shipped and unchanged.

## Impact

- **Tier:** Tier 2 follow-up. Depends on: `transcript-import-foundation` (shipped — PATs, import endpoint, CORS).
- **Aggregates affected:** None. The bookmarklet is a client-side tool that hits the existing `POST /api/captures/import` endpoint. No domain or persistence changes.
- **Backend:** No backend changes. The existing import endpoint and CORS policy already support this use case.
- **Frontend:**
  - New `BookmarkletInstallerComponent` in Settings (below PAT section) — generates the `javascript:` URL from a template, displays a draggable link, shows usage instructions.
  - New `src/assets/bookmarklet.template.js` — the bookmarklet source (readable, documented), minified at build time or inlined as a template literal.
- **Tests:** Angular unit test for the bookmarklet generator (correct URL encoding, PAT substitution, document-ID extraction regex). No E2E test — the bookmarklet runs on `docs.google.com`, not within the app's test harness.
- **Dependencies:** None. No new NuGet or npm packages.

## Non-goals

- **No browser extension.** The user's IT policy blocks personal extension installs — the bookmarklet is the workaround by design.
- **No Google Drive OAuth, API access, or service accounts.** By design.
- **No calendar.google.com scraping.** Calendar events link through to Docs; the bookmarklet handles Docs.
- **No changes to the import endpoint, PAT system, or CORS policy.** Already shipped.
- **No audio capture from the bookmarklet.** Text-only.
- **No mobile bookmarklet flow.** Desktop browsers only for v1.
- **No automatic detection of new transcripts.** The user clicks the bookmarklet manually per transcript.
- **No PAT rotation or refresh from the bookmarklet.** If the PAT is revoked, the user re-generates and re-installs from Settings.
