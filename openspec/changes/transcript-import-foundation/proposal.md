## Why

The product brief sells capture as the effortless front door: paste a transcript, drop a meeting recording, dump a raw thought, and the AI organizes the rest. In practice, the user's main transcript source — Google Meet notes stored in a corporate Google Drive — cannot be integrated the usual way. The Drive is locked down: no third-party OAuth authorization is permitted, so Mental Metal cannot use the Drive API, service accounts, or browser-extension OAuth flows. The user can *view* transcripts in their browser, and nothing else.

This change builds the foundation for an OAuth-free ingress path: Personal Access Tokens that let external browser tools (a future bookmarklet) POST directly into a new capture-import endpoint, plus a file-drop fallback on Quick Capture so the user can import today by downloading the transcript and dragging it in. The follow-up change (`transcript-bookmarklet`) makes it one-click. This change alone is enough to unblock manual import and set the contract every future ingress path will target.

## What Changes

- Introduce **Personal Access Tokens (PATs)** as a new auth primitive: user-generated, named, scope-limited (`captures:write` initially), revocable, shown once at creation, hashed at rest, with created/last-used/revoked metadata.
- Add `Authorization: Bearer <token>` middleware that resolves a PAT to a user and enforces the token's scopes alongside the existing cookie/JWT auth.
- Add `POST /api/captures/import`: a new ingress endpoint for externally-sourced captures.
  - Accepts `application/json` bodies: `{ type: "Transcript" | "QuickNote", content: string, sourceUrl?, title?, meetingAt? }`.
  - Accepts `multipart/form-data` with a single uploaded file (`.txt`, `.docx`, `.html`) plus the same optional metadata fields.
  - Authenticates via **either** session cookie (UI drop-zone) **or** PAT (external tools).
  - Returns `201 Created` with the new capture's id.
- Add server-side **transcript parsers**: `.docx` → plain text (DocumentFormat.OpenXml), `.html` → plain text (HtmlAgilityPack, strips Google's export markup), and a best-effort Google Meet transcript-format detector that normalizes `Speaker name: text` turns so the existing speaker-mapping UI works on imports without extra handling.
- Extend **Quick Capture** with a drop-zone + file-picker inside the dialog's Advanced section that sends `.txt`/`.docx`/`.html` files to the same import endpoint. Defaults the capture `type` to `Transcript` for document uploads, `QuickNote` otherwise. Success toast links to the new capture.
- CORS allowlist for `https://docs.google.com` and `https://calendar.google.com` on the import endpoint so the follow-up bookmarklet can POST directly from those origins.

## Capabilities

### New Capabilities

- `personal-access-tokens`: User-generated bearer tokens scoped to a subset of API actions. Covers generation, listing, revocation, last-used tracking, hashed storage, and the bearer-token middleware that resolves a PAT to a user/scope set. Foundation for any future programmatic ingress (bookmarklets, CLIs, scripts).
- `capture-import`: A new capture ingress path parallel to `POST /api/captures`. Accepts transcripts and document files from external authenticated tools (PAT) or from the UI (session). Owns the file-parsing pipeline (`.txt`/`.docx`/`.html`), the Google Meet format detector, and the JSON+multipart endpoint contract.

### Modified Capabilities

- `capture-text`: The Quick Capture dialog's Advanced section gains a drop-zone + file-picker for `.txt`/`.docx`/`.html` files. The dialog now has two submission modes — typed content (existing `POST /api/captures`) and file upload (new `POST /api/captures/import`). No changes to the typing happy path.

## Impact

- **Tier:** Tier 1 extension. Depends on: `user-auth-tenancy` (shipped) for the User aggregate and existing auth middleware; `capture-text` (shipped) for the Capture aggregate and Quick Capture dialog.
- **Aggregates affected:**
  - **New:** `PersonalAccessToken` aggregate — `Id`, `UserId`, `Name`, `Scopes` (set of string-valued flags), `TokenHash`, `CreatedAt`, `LastUsedAt?`, `RevokedAt?`. Raises `PersonalAccessTokenCreated` and `PersonalAccessTokenRevoked` domain events. User-scoped (UserId is the tenancy boundary, consistent with every other aggregate).
  - **Unchanged:** `Capture` aggregate. The import endpoint creates Capture aggregates using the same factory/invariants as `POST /api/captures`. AI extraction is triggered via the existing pipeline with no changes.
- **Backend:**
  - New `src/MentalMetal.Domain/PersonalAccessTokens/` folder — aggregate, events, repository interface.
  - New `src/MentalMetal.Application/PersonalAccessTokens/` vertical slice — `CreatePersonalAccessToken`, `ListPersonalAccessTokens`, `RevokePersonalAccessToken`, `ResolvePatBearer` (internal query used by auth middleware).
  - New `src/MentalMetal.Application/Captures/ImportCapture/` vertical slice — `ImportCaptureFromJson`, `ImportCaptureFromFile` handlers plus `TranscriptFormatDetector` and parser interfaces.
  - New `src/MentalMetal.Infrastructure/Parsers/` — `DocxTextParser` (DocumentFormat.OpenXml), `HtmlTextParser` (HtmlAgilityPack), `PlainTextPassthroughParser`.
  - New `src/MentalMetal.Infrastructure/Repositories/PersonalAccessTokenRepository.cs` + EF Core configuration.
  - New migration adding `personal_access_tokens` table (UserId-scoped query filter).
  - New `PatAuthenticationHandler` registered in `Program.cs` as an additional authentication scheme, composed with the existing cookie/JWT scheme via a policy scheme so endpoints can accept either.
  - `src/MentalMetal.Web/Captures/CaptureEndpoints.cs` — new `POST /api/captures/import` route on the new scheme.
  - CORS policy update to allow `https://docs.google.com` and `https://calendar.google.com` on the import endpoint only (not the full API).
- **Frontend:**
  - New `src/MentalMetal.Web/ClientApp/src/app/features/settings/personal-access-tokens/` — list page, generate dialog, revoke confirm. Tokens shown once at creation in a copyable panel with a clear "you will not see this again" message.
  - `QuickCaptureDialogComponent` — add a drop-zone and file-picker inside the existing Advanced section (does not change the happy path). On file select/drop, switch to multipart upload targeting `/api/captures/import` instead of `/api/captures`.
  - No changes to the captures list, detail, triage, or AI extraction UIs. Imported captures surface through the existing flows.
- **Dependencies:**
  - NuGet: `DocumentFormat.OpenXml` (Microsoft, MIT), `HtmlAgilityPack` (MIT). Both widely used and actively maintained.
  - No new npm packages.
- **Tests:**
  - Domain tests for `PersonalAccessToken` invariants (scope required, revoke idempotent, cannot use revoked token, last-used updates).
  - Application tests for Create/Revoke/List handlers and for `ImportCaptureFromJson` / `ImportCaptureFromFile` including parser dispatch and format detection.
  - Integration tests for `POST /api/captures/import` covering: PAT auth (valid/revoked/unknown), session auth, JSON shape, multipart upload for each file type, oversized-file rejection, unknown-content-type rejection, and verification that a Meet-format transcript surfaces speaker labels.
  - E2E (Playwright) smoke: user generates a PAT, posts a JSON transcript via PAT and sees it in the captures list; separately, drops a `.docx` into Quick Capture and sees it arrive as a Transcript capture.

## Non-goals

- **No bookmarklet.** Deferred to the follow-up `transcript-bookmarklet` change. This change only delivers the server-side contract and the manual file-drop UI.
- **No Settings UI for bookmarklet install.** Also deferred.
- **No Google Drive OAuth, Drive API, service accounts, or push notifications from Google.** By design — the whole point of this change is to avoid Drive-side integration entirely.
- **No calendar.google.com or meet.google.com scraping.** Out of scope for both this change and the follow-up (calendar events link through to Docs, so the Docs-targeted bookmarklet covers both flows).
- **No email ingest.** Not needed given the bookmarklet path covers the Docs-format transcripts the user has.
- **No changes to the AI extraction pipeline** (`capture-ai-extraction`). Imported captures flow through existing extraction unchanged.
- **No changes to `POST /api/captures`.** The new endpoint is additive. The typed-text happy path is untouched.
- **No speaker auto-diarization.** The transcript format detector preserves speaker labels so the existing manual speaker-mapping UI works; it does not attempt automated speaker identity resolution.
- **No general file attachments feature.** The import endpoint accepts `.txt`/`.docx`/`.html` because those are what Google Docs exports. Arbitrary file attachment is a separate product concern.
- **No multi-user or organisation-level token sharing.** PATs are per-user, user-owned, and never visible to another user.
