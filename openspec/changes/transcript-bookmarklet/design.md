## Context

`transcript-import-foundation` shipped the server-side contract: PATs with `captures:write` scope, `POST /api/captures/import` accepting JSON and multipart, CORS allowlisting `docs.google.com` and `calendar.google.com`, and the `TranscriptFormatDetector` that normalizes Google Meet speaker labels. The file-drop fallback in Quick Capture is already usable but requires a manual download step.

The user's IT constraints remain: no third-party OAuth, no personal browser extensions. But they can use bookmarklets — JavaScript snippets saved as bookmarks that run in the context of the current page. A bookmarklet on `docs.google.com` can make same-origin requests (including the `/export?format=txt` endpoint) and cross-origin requests to Mental Metal (CORS already configured).

## Dependencies

- `transcript-import-foundation` (shipped) — PATs, import endpoint, CORS policy. No modifications.
- `capture-text` (shipped) — Capture aggregate, Quick Capture dialog. No modifications.
- `capture-ai-extraction` (shipped) — extraction pipeline. Imported captures flow through unchanged.

## Goals / Non-Goals

**Goals:**

- A user can install the bookmarklet from Settings in under 30 seconds: select a PAT, drag a link to their bookmarks bar, done.
- Clicking the bookmarklet on any Google Doc extracts the full text and imports it as a Transcript capture in under 3 seconds (network-dependent).
- The bookmarklet is self-contained — no external scripts loaded, no localStorage dependencies, no popup windows.
- Visual feedback on the Google Docs page: success toast or error toast with actionable message.
- The bookmarklet works on both `docs.google.com/document/d/...` and `docs.google.com/document/u/0/d/...` (multi-account URLs).

**Non-Goals:**

- No browser extension.
- No OAuth or Drive API.
- No mobile bookmarklet support.
- No auto-detection of new transcripts.
- No PAT rotation from the bookmarklet — re-generate and re-install from Settings.
- No changes to backend endpoints.

## Decisions

### D1. Bookmarklet fetches `/export?format=txt` (same-origin), not DOM scraping

**Decision:** The bookmarklet extracts text via `fetch('/document/d/<ID>/export?format=txt')` on `docs.google.com`. This is a same-origin request that returns the document as plain text, using the user's existing session cookies.

**Alternatives considered:**
- *DOM scraping via `document.querySelector('.kix-appview-editor')`.* Rejected: the Google Docs editor DOM is complex, frequently changes, and requires traversing contenteditable structures. The export URL is stable, documented by Google, and returns clean text.
- *Google Drive API with a service account.* Rejected: requires OAuth, which the user's IT policy blocks.

**Rationale:** The export URL is the most reliable, maintainable, and minimal-code approach. It returns the complete document text including speaker labels, headers, and formatting markers — exactly what `TranscriptFormatDetector` expects.

### D2. PAT and instance URL are baked into the bookmarklet at generation time

**Decision:** The Settings page generates the bookmarklet `javascript:` URL with the instance URL and PAT token string-interpolated into the source. The user's PAT is embedded directly in the bookmark.

**Alternatives considered:**
- *Store PAT in `localStorage` on `docs.google.com`.* Rejected: `localStorage` is per-origin, so we'd need a setup flow that navigates to `docs.google.com` and writes to its `localStorage` — fragile and confusing. Also, any other script on `docs.google.com` could read it.
- *Prompt for PAT on each click.* Rejected: defeats the "one-click" promise. 
- *Store config on the Mental Metal server and have the bookmarklet fetch it.* Rejected: requires an unauthenticated config endpoint (security concern) or a chicken-and-egg auth problem.

**Rationale:** Baking the PAT into the bookmarklet is the standard pattern (GitHub, Slack, and others use this for webhook URLs). The PAT is visible only in the bookmark itself, on the user's own machine. The risk profile is equivalent to a saved password in a bookmark — acceptable for a user-generated, user-revocable token.

### D3. Toast is injected into the Google Docs DOM, not a browser alert

**Decision:** The bookmarklet creates a small fixed-position `<div>` styled inline (no external CSS) at the top of the page with the success/error message. It auto-dismisses after 4 seconds. Styled with inline styles to avoid Google Docs CSS interference.

**Alternatives considered:**
- *`window.alert()`.* Rejected: blocks the page, requires a click to dismiss, feels hostile.
- *`console.log()` only.* Rejected: invisible to non-developer users.

**Rationale:** A self-dismissing toast is non-intrusive and provides clear feedback without blocking the user's workflow.

### D4. Bookmarklet source is a template literal in the Angular component, not a separate built asset

**Decision:** The bookmarklet JavaScript source lives as a TypeScript template string inside `BookmarkletInstallerComponent`. The component interpolates `instanceUrl` and `pat` into the template, URI-encodes the result, and produces the `javascript:...` URL. No separate build step or minification pipeline.

**Alternatives considered:**
- *Separate `.js` file minified by the Angular build.* Rejected: Angular's build pipeline doesn't naturally produce bookmarklet URLs; wiring a custom webpack/esbuild plugin for one file is overkill.
- *Server-side endpoint that generates the bookmarklet.* Rejected: adds backend work for a purely client-side concern; the generation is trivial string interpolation.

**Rationale:** Keeps the bookmarklet source co-located with the installer UI, easy to read and maintain, and avoids build complexity.

### D5. Document ID extraction handles multi-account Google URLs

**Decision:** The bookmarklet regex for extracting the document ID handles both `docs.google.com/document/d/<ID>/...` and `docs.google.com/document/u/<N>/d/<ID>/...` (multi-account selector). Pattern: `/\/document\/(?:u\/\d+\/)?d\/([a-zA-Z0-9_-]+)/`.

**Rationale:** Corporate Google Workspace users frequently have multiple accounts signed in. The `/u/0/` or `/u/1/` prefix is common in their URLs.

## Risks / Trade-offs

- **[Risk] Google changes the `/export?format=txt` endpoint.** → Mitigation: this endpoint has been stable for 10+ years and is used by numerous third-party tools. If it changes, the bookmarklet fails gracefully (fetch error → error toast). The file-drop fallback in Quick Capture remains available.
- **[Risk] PAT embedded in the bookmark is visible to anyone with access to the browser's bookmark data.** → Mitigation: the PAT is scoped to `captures:write` only, is user-revocable from Settings, and the bookmark bar is no less secure than a saved password. Document this in the installer UI.
- **[Risk] Content Security Policy on `docs.google.com` blocks the cross-origin fetch to Mental Metal.** → Mitigation: CSP `connect-src` on Google Docs is currently permissive for same-origin; cross-origin fetches from bookmarklets are typically not blocked by CSP (bookmarklets run in the page's context with the page's CSP, but `fetch` to arbitrary origins is allowed unless CSP explicitly restricts `connect-src`). If blocked, the bookmarklet will catch the error and show a clear message.
- **[Trade-off] No automatic bookmarklet update.** If the bookmarklet source changes (e.g., a bug fix), users must re-install from Settings. → Accepted: bookmarklets are inherently static; this is the standard trade-off.
- **[Trade-off] No build-time minification.** The bookmarklet URL will be longer than strictly necessary. → Accepted: bookmark URLs can be up to 65k chars in all modern browsers; our bookmarklet will be well under 2k chars.

## Migration Plan

No data migration. No API migration. Frontend-only change. Deploy ships the new Settings component.

Rollback: revert the Angular changes; backend is unchanged.

## Open Questions

- Should the toast include a clickable link to the created capture in Mental Metal? (Proposed: yes — include `<a href="${instanceUrl}/capture/${id}">View capture</a>` in the success toast, opening in a new tab.)
- Should the bookmarklet strip the Google Docs title suffix (e.g., " - Google Docs") before using it as the capture title? (Proposed: yes — `document.title.replace(/ - Google Docs$/, '').trim()`.)
