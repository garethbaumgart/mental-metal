## 1. Bookmarklet source

- [x] 1.1 Write the bookmarklet JavaScript as a template string constant in a new file `src/MentalMetal.Web/ClientApp/src/app/pages/settings/bookmarklet-template.ts` — export a function `generateBookmarkletUrl(instanceUrl: string, pat: string): string` that returns the complete `javascript:...` URL
- [x] 1.2 Implement document-ID extraction regex supporting both `/document/d/<ID>/` and `/document/u/<N>/d/<ID>/` patterns
- [x] 1.3 Implement export fetch: `fetch('/document/d/' + docId + '/export?format=txt')` with error handling (network, 403, non-200)
- [x] 1.4 Implement cross-origin POST to `instanceUrl + '/api/captures/import'` with `Authorization: Bearer <PAT>`, JSON body `{ type: "Transcript", content, title, sourceUrl }`; strip ` - Google Docs` suffix from `document.title`
- [x] 1.5 Implement injected toast: fixed-position div with inline styles, success (green, "Imported to Mental Metal" + "View capture" link opening `instanceUrl/capture/<id>` in new tab) or error (red, reason), auto-dismiss after 4s (success) or 6s (error)
- [x] 1.6 Guard: if URL doesn't match Google Doc pattern, show error toast "Not a Google Doc" and exit without network requests
- [ ] 1.7 Unit test for `generateBookmarkletUrl`: returns a string starting with `javascript:`, contains the PAT, contains the instance URL, is valid URI-encoded

## 2. Settings installer component

- [x] 2.1 Add `BookmarkletInstallerComponent` as a standalone component in `src/app/pages/settings/bookmarklet-installer.component.ts`
- [x] 2.2 Auto-detect instance URL from `window.location.origin` and display it as a read-only field
- [x] 2.3 Load active PATs with `captures:write` scope via `PersonalAccessTokensService.list()`, filter to active only, populate availability check
- [x] 2.4 When a PAT is pasted, call `generateBookmarkletUrl(instanceUrl, pat)` and bind the result to an `<a>` element's `href` attribute — style the link as a draggable button with PrimeNG theming
- [x] 2.5 Show a "No tokens available" message with a prompt to generate one when the user has no active PATs
- [x] 2.6 Display usage instructions: numbered steps (1) Paste token, (2) Drag the button to your bookmarks bar, (3) Open any Google Doc transcript and click it
- [x] 2.7 Import `BookmarkletInstallerComponent` into `SettingsPage` and add it below the PAT section

## 3. Tests

- [ ] 3.1 Angular unit test for `generateBookmarkletUrl`: validates output format, PAT/URL substitution, URI encoding, document-ID regex correctness
- [ ] 3.2 Angular unit test for `BookmarkletInstallerComponent`: renders with no PATs → shows prompt; renders with PATs → shows dropdown and draggable link; selected PAT generates valid href

## 4. Documentation and release

- [x] 4.1 Update `docs/transcript-import.md` to add a "One-Click: Bookmarklet" section describing the install and usage flow
- [x] 4.2 Verify `dotnet test src/MentalMetal.slnx` and `ng test --watch=false` pass
- [ ] 4.3 Open PR via `/pr` skill
