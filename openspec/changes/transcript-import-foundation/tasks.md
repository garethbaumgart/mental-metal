## 1. Domain — PersonalAccessToken aggregate

- [x] 1.1 Add `src/MentalMetal.Domain/PersonalAccessTokens/PersonalAccessToken.cs` aggregate with `Id`, `UserId`, `Name`, `Scopes` (HashSet<string>), `TokenHash` (byte[]), `TokenLookupPrefix` (byte[]), `CreatedAt`, `LastUsedAt?`, `RevokedAt?`
- [x] 1.2 Enforce invariants: non-empty name, non-empty scopes, cannot revoke twice (idempotent no-op), cannot authenticate after `RevokedAt` is set; implement `Create`, `TouchLastUsed`, `Revoke`, `IsActive(utcNow)` methods
- [x] 1.3 Add domain events `PersonalAccessTokenCreated` and `PersonalAccessTokenRevoked`
- [x] 1.4 Add `IPersonalAccessTokenRepository` interface in Domain with `AddAsync`, `GetByIdAsync`, `GetByLookupPrefixAsync`, `ListForUserAsync`, `SaveChangesAsync`
- [x] 1.5 Add domain unit tests covering create / revoke / idempotent revoke / invariants / last-used update

## 2. Infrastructure — PAT persistence and hashing

- [x] 2.1 Add `IPatTokenHasher` abstraction (Domain or Application interface) with `HashToken(string plaintext)` returning `(hash, lookupPrefix)` and `VerifyAgainst(string plaintext, byte[] hash)` using SHA-256 + versioned server pepper; register implementation in Infrastructure DI
- [x] 2.2 Add `PersonalAccessTokenRepository` in `src/MentalMetal.Infrastructure/Repositories/` and `PersonalAccessTokenConfiguration` EF mapping: columns for the aggregate fields, indexes on `UserId`, `TokenLookupPrefix`, and unique on `TokenHash`; global query filter on `UserId`
- [x] 2.3 Add EF Core migration `AddPersonalAccessTokens` creating the `personal_access_tokens` table; verify `dotnet ef migrations script` is clean
- [x] 2.4 Add DbContext `DbSet<PersonalAccessToken>` and integrate query filter with `ICurrentUserService` closure consistent with other aggregates

## 3. Application — PAT vertical slice

- [x] 3.1 Add `src/MentalMetal.Application/PersonalAccessTokens/CreatePersonalAccessToken.cs` handler: validates name + scopes, generates 32 random bytes, formats as `mm_pat_<base64url>`, hashes, persists, returns DTO with plaintext token included
- [x] 3.2 Add `ListPersonalAccessTokens` handler returning metadata-only DTOs (no plaintext, no hash)
- [x] 3.3 Add `RevokePersonalAccessToken` handler with idempotent revoke semantics
- [x] 3.4 Add internal `ResolvePatBearerHandler` (or service) that takes a plaintext token, hashes it, looks up via prefix, verifies hash, updates `LastUsedAt`, returns the resolved user id + scopes — used only by the PAT auth scheme
- [x] 3.5 Add scope validation helper with the v1 supported set `{ "captures:write" }`
- [ ] 3.6 Add application unit tests for each handler: create happy path, create with unknown scope, create with empty scope, list isolates by user, revoke idempotent, resolve rejects revoked tokens, resolve updates `LastUsedAt`

## 4. Web — PAT authentication scheme and policy composition

- [x] 4.1 Implement `PatAuthenticationHandler : AuthenticationHandler<PatAuthenticationSchemeOptions>` that parses `Authorization: Bearer mm_pat_...`, rejects unprefixed tokens with 401, calls the resolve service, sets `ClaimsPrincipal` with `NameIdentifier` = user id and one `scope` claim per granted scope
- [x] 4.2 Register a PolicyScheme `MentalMetalAuth` that forwards to the cookie/JWT scheme when a session cookie is present and to the PAT scheme otherwise
- [x] 4.3 Add `ScopeRequirement` + authorization handler that passes only when the authenticated principal has the required `scope` claim; register policy `RequireCapturesWriteScope`
- [ ] 4.4 Register logging middleware (or configure existing request-logging) to redact `Authorization` header values from all log output
- [ ] 4.5 Add integration tests: valid PAT → 200 on a test endpoint; revoked PAT → 401; unknown token → 401; missing prefix → 401; PAT without required scope → 403; cookie + PAT coexist (cookie wins when present)

## 5. Web — PAT endpoints

- [x] 5.1 Add `src/MentalMetal.Web/PersonalAccessTokens/PersonalAccessTokenEndpoints.cs`: `POST /api/personal-access-tokens`, `GET /api/personal-access-tokens`, `POST /api/personal-access-tokens/{id}/revoke` — all on the cookie/JWT scheme only (never PAT-authenticated)
- [x] 5.2 Wire endpoints to handlers, enforce `ValidationProblem` on 400s
- [ ] 5.3 Integration tests: create → 201 with token in body; list → token metadata only (no plaintext, no hash); revoke → 204 idempotent; cross-user revoke → 404

## 6. Infrastructure — transcript file parsers

- [x] 6.1 Add NuGet dependencies `DocumentFormat.OpenXml` and `HtmlAgilityPack` to `MentalMetal.Infrastructure.csproj`
- [x] 6.2 Add `ITranscriptFileParser` abstraction with `bool CanHandle(string contentType, string fileName)` and `Task<string> ExtractTextAsync(Stream, CancellationToken)` in Application
- [x] 6.3 Implement `DocxTranscriptParser` (OpenXml → plain text), `HtmlTranscriptParser` (HtmlAgilityPack → plain text with markup stripped), `PlainTextTranscriptParser` (UTF-8 passthrough) in Infrastructure; register all three in DI
- [x] 6.4 Implement `TranscriptFormatDetector` service (Application) that detects Google Meet formatting (Summary/Transcript headings, `^[A-Z][A-Za-z ''-]{1,40}:\s` speaker pattern), normalizes line endings, and preserves speaker turns; returns normalized content plus a flag indicating whether a Meet pattern was matched
- [ ] 6.5 Unit tests for each parser (happy path, corrupted input, encoding variations) and for the format detector (Meet-formatted content preserved, non-Meet content passed through, regex false-matches don't mangle)

## 7. Application — capture import slice

- [x] 7.1 Add `src/MentalMetal.Application/Captures/ImportCapture/ImportCaptureFromJsonCommand.cs` handler: validates payload, applies `TranscriptFormatDetector` to `content`, creates `Capture` via existing factory, persists, returns capture id
- [x] 7.2 Add `ImportCaptureFromFileCommand` handler: accepts `(Stream, contentType, fileName, metadata)`, dispatches to the matching parser, applies format detector, creates `Capture`, returns capture id
- [x] 7.3 Enforce file size cap (10 MB) and supported content-type/extension allowlist before invoking parsers; map to 413 / 415 responses
- [x] 7.4 Default `CaptureType` rules: request `type` wins when provided; else `Transcript` for `.docx`/`.html`/`.htm`, `QuickNote` for `.txt`
- [ ] 7.5 Application unit tests covering each scenario in `capture-import` spec (JSON happy path, file for each parser, empty content, unsupported type, oversize, malformed docx, Meet-format detection, explicit type override)

## 8. Web — capture import endpoint and CORS

- [x] 8.1 Add `POST /api/captures/import` in `CaptureEndpoints.cs` accepting both `application/json` and `multipart/form-data`, authenticated with `MentalMetalAuth` + `RequireCapturesWriteScope`
- [x] 8.2 Add named CORS policy `ImportIngestFromGoogle` permitting origins `https://docs.google.com` and `https://calendar.google.com`, `POST` method, `Authorization` + `Content-Type` headers, credentials disabled; apply with `.RequireCors("ImportIngestFromGoogle")` on the import endpoint only
- [ ] 8.3 Integration tests: JSON with cookie auth → 201; JSON with PAT → 201; PAT without scope → 403; multipart docx → 201 with parsed content; multipart html → 201; multipart txt → 201; unsupported content type → 415; oversize → 413; malformed docx → 400; CORS preflight from `docs.google.com` on import route → allowed; CORS preflight from `docs.google.com` on `POST /api/captures` → rejected
- [ ] 8.4 Assert in an integration test that the logged request/response records for a PAT-authenticated import do not contain the plaintext token or the `Authorization` header value

## 9. Frontend — PAT management in Settings

- [x] 9.1 Add Angular route `/settings/personal-access-tokens` with a standalone `PersonalAccessTokensPageComponent` using signals, PrimeNG table, Tailwind layout utilities — no banned patterns per CLAUDE.md
- [x] 9.2 Implement `PersonalAccessTokenStore` (signal-backed service) with `list`, `generate`, `revoke`, `clearPlaintext` operations hitting the new endpoints
- [x] 9.3 Implement generate dialog: name input, scopes checklist (v1 shows only `captures:write`), submit → display plaintext token once in a copyable panel with an explicit "You won't see this again" warning; copy-to-clipboard button; dismiss clears the plaintext from memory
- [x] 9.4 Implement revoke confirmation with PrimeNG `ConfirmDialog` and visual state change in the list (Active vs Revoked chip, no reuse of banned color utilities)
- [x] 9.5 Add a link to the new page from the Settings shell/sidebar per existing pattern

## 10. Frontend — Quick Capture drop-zone

- [x] 10.1 In `QuickCaptureDialogComponent`, add a drop-zone + file-picker button inside the existing collapsed Advanced section, accepting `.txt`, `.html`, `.htm`, `.docx` only
- [x] 10.2 On file selection or drop, hold the file in a signal alongside the existing form state; show the filename and a clear button
- [x] 10.3 On submit, when a file is present: send multipart/form-data to `POST /api/captures/import` with defaulted `type` from filename (unless user overrode in Advanced); when no file is present, preserve the existing `POST /api/captures` JSON submit
- [x] 10.4 Confirm prompt if both typed content and a file are present before submitting (file wins, typed content discarded)
- [x] 10.5 Success toast with "View capture" action routing to the capture detail for the new id; error toast on 4xx with the server's validation message
- [ ] 10.6 Angular unit tests for the dialog: drop-zone hidden until Advanced is expanded; typing happy path still hits `/api/captures`; file-present submit hits `/api/captures/import`; unsupported extensions rejected inline

## 11. E2E tests (Playwright)

- [ ] 11.1 Smoke test: user generates a PAT from Settings, copies the plaintext once, revokes it, and confirms subsequent use is 401
- [ ] 11.2 Smoke test: user POSTs a JSON transcript via PAT to `/api/captures/import` (via a test harness request) and sees the capture appear in their captures list
- [ ] 11.3 Smoke test: user opens Quick Capture, expands Advanced, drops a small `.docx` fixture, submits, sees the success toast, clicks "View capture", and lands on the capture detail showing the parsed text
- [ ] 11.4 Isolation test: User B cannot view or revoke User A's PATs (404) and cannot see User A's imported captures

## 12. Documentation and release

- [ ] 12.1 Add a short README section or `docs/transcript-import.md` note describing how to generate a PAT and how the manual file-drop flow works, referencing that the bookmarklet is deferred to `transcript-bookmarklet`
- [x] 12.2 Verify `dotnet test src/MentalMetal.slnx`, Angular unit tests, and the E2E suite pass locally and in CI
- [ ] 12.3 Open PR via `/pr` skill
