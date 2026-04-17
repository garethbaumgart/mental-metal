## Context

The user's primary transcript source is Google Meet notes that land in a corporate Google Drive. Three constraints rule out the usual integrations: no third-party OAuth is authorized on the user's Google account, no personal browser extensions can be installed, and the Drive Desktop client syncs Google Docs as `.gdoc` URL stubs (no readable text on disk). What the user *can* do: view transcripts in their browser, download them as `.txt`/`.docx`/`.html`, and run JavaScript inside their own authenticated session (bookmarklets).

The system already has a solid `Capture` aggregate and a working AI extraction pipeline that triggers on `POST /api/captures`. What's missing is a second ingress path that external tools and file uploads can target without depending on the SPA's cookie/CSRF auth. This change delivers that foundation. The follow-up `transcript-bookmarklet` change ships the one-click-from-Google-Docs piece; this change ships everything that path will depend on, plus a manual file-drop escape hatch available from day one.

## Dependencies

- `user-auth-tenancy` (Tier 1, shipped) — provides the User aggregate, cookie/JWT auth scheme, `ICurrentUserService`, and the DbContext global query filter pattern that `PersonalAccessToken` will reuse. No modifications to this spec.
- `capture-text` (Tier 2, shipped) — provides the `Capture` aggregate, factory, repository, and Quick Capture dialog. The new import endpoint constructs Captures via the same factory; the Quick Capture dialog gains a drop-zone in its existing Advanced section. This change delivers a delta to `capture-text` covering only the new Quick Capture drop-zone behavior.
- `capture-ai-extraction` (Tier 2, shipped) — **not modified**. Imported captures flow through the existing extraction pipeline with no changes; `ProcessCapture` is content-agnostic.

## Goals / Non-Goals

**Goals:**

- A user can generate, name, list, and revoke Personal Access Tokens from Settings, scoped to a specific action set (v1: `captures:write`).
- PATs are shown once at creation and hashed at rest; no plaintext token is ever persisted or recoverable.
- External tools can `POST /api/captures/import` with `Authorization: Bearer <PAT>` and either a JSON body or a `multipart/form-data` file upload, and receive a `201 Created` with the new capture id.
- The same endpoint is usable from the SPA via the existing session cookie, so the Quick Capture dialog's drop-zone can reuse it without separate plumbing.
- File uploads in `.txt`, `.docx`, and `.html` are parsed to plain text server-side; Google Meet transcript formatting (sectioned Summary + Transcript with speaker-labelled turns) is detected and preserved so the existing speaker-mapping UI works unchanged.
- CORS is configured so a future bookmarklet running on `https://docs.google.com` or `https://calendar.google.com` can POST to the endpoint.
- The AI extraction pipeline is reused as-is; imported captures start in `ProcessingStatus.Raw` and move through the same state machine.

**Non-Goals:**

- The bookmarklet itself and its Settings-page installer (deferred to `transcript-bookmarklet`).
- Google Drive OAuth, service accounts, Drive API push notifications, or any Drive-side integration (explicitly ruled out by the user's IT constraints).
- `calendar.google.com` or `meet.google.com` scraping (calendar events link through to Docs; the Docs bookmarklet covers both flows).
- Email ingest (not needed given the Docs-plus-bookmarklet path).
- Speaker auto-diarization, voice profiles, or any ML-based speaker identification (out of scope for this tier; out of scope for the follow-up too).
- Generalised file attachments on captures. The parsers exist to turn a document export into a plain-text `Capture.RawContent`, not to preserve the file as an artifact.
- Multi-user token sharing, organisation-level tokens, or OAuth client credentials for third parties.
- Arbitrary scope definitions in v1. Scope strings are an `ISet<string>` on the domain, but the only validated/enforced value shipped in this change is `captures:write`.

## Decisions

### D1. `PersonalAccessToken` is a standalone user-scoped aggregate, not an entity of `User`

**Decision:** Introduce a new aggregate in `src/MentalMetal.Domain/PersonalAccessTokens/` with `Id`, `UserId`, `Name`, `Scopes: ISet<string>` (value object), `TokenHash: byte[]`, `CreatedAt`, `LastUsedAt?`, `RevokedAt?`. Raises `PersonalAccessTokenCreated` and `PersonalAccessTokenRevoked` events.

**Alternatives considered:**
- *Entity inside the `User` aggregate.* Rejected: token lifecycles are independent of user profile changes; loading a User to touch a token would be wasteful; concurrency control on User would ripple into every token write.
- *Store on the `ApplicationUser` identity row directly.* Rejected: blurs domain and identity concerns; the existing auth layer is deliberately thin and we don't want to broaden it.

**Rationale:** Consistent with every other aggregate in the codebase (user-scoped, `IUserScoped`, global query filter applied by `DbContext`). Independent lifecycle is modelled by an independent aggregate.

### D2. Store only the hash; display the plaintext exactly once

**Decision:** When a PAT is generated, the handler produces a cryptographically random 32-byte token, base64url-encodes it with a short `mm_pat_` prefix for identifiability (e.g., `mm_pat_<44chars>`), hashes it with SHA-256 using a versioned server-side pepper, and persists `(UserId, Name, Scopes, TokenHash, CreatedAt)`. The plaintext is returned in the 201 response body once and is never retrievable again. The UI surfaces a copy-to-clipboard affordance and a clear "you won't see this again" warning. Revocation sets `RevokedAt` but retains the row for audit.

**Alternatives considered:**
- *Bcrypt/argon2 for the token hash.* Rejected: PATs have 256 bits of entropy; slow-hashing is designed for low-entropy secrets (passwords). SHA-256 with a pepper is sufficient, dramatically faster on every request (auth middleware hashes on every call), and standard practice for token-style secrets (GitHub, Stripe, etc. use this pattern).
- *Encrypt the token at rest and decrypt on auth.* Rejected: reversible storage is strictly worse than one-way hashing for a secret that only needs to be compared on presentation.
- *Show the token again later on demand.* Rejected: defeats the purpose of hashed storage; provides no user value once they've stored it.

**Rationale:** Aligns with the standard industry pattern. Middleware cost per request is a single SHA-256 + an indexed lookup on a prefix/short-hash.

### D3. Bearer middleware lookup uses a prefix index, not a full-table scan

**Decision:** The first 8 chars after the `mm_pat_` prefix of each token are stored in a separate indexed column `TokenLookupPrefix` (the first bytes of the hash, not the plaintext — we derive it at creation time from the hash's first bytes). On auth, the middleware hashes the presented token, extracts the same prefix, indexes into the table, then constant-time compares the full hash on the matched row(s). This avoids O(n) scans as the token table grows.

**Alternatives considered:**
- *Indexed hash column with a unique constraint.* Accepted as a further safeguard; both indexes co-exist. Lookup by unique hash directly on an indexed column is also O(log n).
- *Put the prefix in the plaintext token (visible) and index that.* Rejected: leaks token structure; hashed prefix is safer and equally fast.

**Rationale:** Linear scans on every authenticated request are a latent performance problem; this avoids it from day one.

### D4. Compose the bearer scheme with the existing cookie/JWT scheme via a policy scheme

**Decision:** Register a new `AuthenticationHandler<PatAuthenticationSchemeOptions>` that parses `Authorization: Bearer <token>` and emits a `ClaimsPrincipal` with the user's id plus a `scope` claim per granted scope. Register a `PolicyScheme` named `MentalMetalAuth` that forwards to cookie/JWT when a cookie is present, and to the PAT scheme when an `Authorization: Bearer` header is present. The new `POST /api/captures/import` uses `.RequireAuthorization("MentalMetalAuth")` with a scope requirement of `captures:write`. No other endpoints are affected.

**Alternatives considered:**
- *Chain multiple `[Authorize]` attributes.* Rejected: ASP.NET Core's attribute-based composition is cumbersome and fragile with minimal APIs.
- *Accept bearer tokens on every endpoint.* Rejected: drastically expands attack surface; v1 only needs PATs to reach the import endpoint. If future endpoints want bearer auth, they can opt into the scheme explicitly.

**Rationale:** The policy-scheme pattern is the ASP.NET Core–idiomatic way to route different credential types to different handlers. Keeps the scheme composition explicit and per-endpoint.

### D5. Scope strings are data, scope enforcement is a single authorization requirement

**Decision:** `PersonalAccessToken.Scopes` is a `HashSet<string>` persisted as a JSONB array. The bearer handler adds a `scope` claim per element. A `ScopeRequirement("captures:write")` is attached to the import endpoint's authorization policy. Adding a new scope later is additive: emit more claims, reference a different string on a new endpoint. No enum churn.

**Alternatives considered:**
- *Strongly-typed scope enum.* Rejected: every new scope becomes a migration and a client-side code change. String-valued scopes are conventional for PAT systems (see GitHub, Stripe) and trivial to validate on creation.

**Rationale:** Extensibility without churn.

### D6. Import endpoint is a parallel ingress to `POST /api/captures`, not a replacement

**Decision:** `POST /api/captures/import` is a new route in `CaptureEndpoints.cs`. It accepts `application/json` **or** `multipart/form-data`, dispatches to `ImportCaptureFromJson` or `ImportCaptureFromFile` respectively, both of which resolve a `Capture` via the existing factory and persist via the existing repository. The existing `POST /api/captures` is untouched.

**Alternatives considered:**
- *Extend `POST /api/captures` to accept multipart.* Rejected: blurs the contract for the SPA's happy path and couples the import feature to the existing form-dialog flow.
- *Put the multipart path at a different route (`POST /api/captures/upload`).* Rejected: two endpoints for "external ingress" adds cognitive load and duplicates auth/CORS policy. One route, two content types.

**Rationale:** Clean split: SPA typed-form happy path → `/api/captures`; external tools and file drops → `/api/captures/import`. Both end at the same `Capture` aggregate.

### D7. Server-side parsers: DocumentFormat.OpenXml for `.docx`, HtmlAgilityPack for `.html`, UTF-8 passthrough for `.txt`

**Decision:** Wrap each in a `ITranscriptFileParser` with `bool CanHandle(string contentType, string fileName)` and `Task<string> ExtractTextAsync(Stream, CancellationToken)`. Dispatch in `ImportCaptureFromFile`.

**Alternatives considered:**
- *Client-side parsing in the SPA.* Rejected: shifts complexity to the client, doesn't help the external-tool path, and `.docx` parsing in the browser is fiddlier than in .NET.
- *Chain to `pandoc` via a process invocation.* Rejected: adds an OS-level dependency, hard to run reliably on Cloud Run, and is overkill for the three formats we care about.
- *Use Aspose or a commercial parser.* Rejected: licensing and cost; both chosen libraries are MIT and widely used.

**Rationale:** Two small MIT-licensed libraries cover the target formats. Keeps parsing in-process and predictable.

### D8. Google Meet transcript-format detection is an opportunistic normalizer, not a gate

**Decision:** After extracting plain text, a `TranscriptFormatDetector` inspects the content for the Google Meet pattern (section heading "Summary" and/or "Transcript", and speaker lines matching `^[A-Z][A-Za-z ''-]{1,40}:\s`). If matched, it normalizes line endings and ensures the speaker turns survive as `Speaker name: text` so the existing `SpeakerLabel` pipeline (from `capture-audio`) works. If not matched, content is stored raw. Detection never rejects content — at worst it's a no-op.

**Alternatives considered:**
- *Hard-fail when the transcript shape isn't recognised.* Rejected: users paste transcripts from many sources; we shouldn't punish the ones that don't match a specific tool's output.
- *Store the detected sections as structured value objects on `Capture`.* Rejected: widens the aggregate for a benefit (structured summary) that the existing AI extraction already produces itself. Keep the aggregate unchanged for this tier.

**Rationale:** Best-effort enhancement; zero downside when detection fails.

### D9. CORS: allowlist `docs.google.com` and `calendar.google.com` on the import route only

**Decision:** Add a dedicated CORS policy `"ImportIngestFromGoogle"` permitting `https://docs.google.com` and `https://calendar.google.com` origins, `POST` method, `Authorization` + `Content-Type` headers, credentials disabled. Apply with `.RequireCors("ImportIngestFromGoogle")` on `POST /api/captures/import` only. No other endpoints are exposed to those origins.

**Alternatives considered:**
- *Wildcard origins for the import endpoint.* Rejected: unbounded CORS undermines token-based auth in practice (anyone's site could trigger a PAT-authenticated request if the user has a PAT cached in localStorage).
- *Apply to the whole API.* Rejected: blast radius too wide for a single feature.

**Rationale:** Minimum-necessary scope; ready for the follow-up bookmarklet without leaking the rest of the API.

### D10. Quick Capture dialog gains a drop-zone inside the existing Advanced section

**Decision:** The file-picker and drag-drop surface live inside the already-collapsed Advanced section of `QuickCaptureDialogComponent` — the happy path (type → Enter) is unaffected. Dropping a file switches the submit handler from JSON `/api/captures` to multipart `/api/captures/import`. Successful submit closes the dialog and emits a toast with a "View capture" action.

**Alternatives considered:**
- *A separate import page.* Rejected: fragments the capture UX; users already know the FAB.
- *Drop zone always visible above the textarea.* Rejected: visual clutter on the happy path; file imports are secondary to typed capture.

**Rationale:** Respects the persistent-quick-capture ethos (happy path stays happy); advanced operations live behind the expander.

### D11. `multipart/form-data` content-type and size limits

**Decision:** Accept files up to 10 MB, content types `text/plain`, `text/html`, `application/vnd.openxmlformats-officedocument.wordprocessingml.document`. Reject anything else with a `415 Unsupported Media Type`. Oversize returns `413 Payload Too Large`. Filename extension is used as a secondary signal when content type is generic (`application/octet-stream` from Drive Desktop sometimes).

**Rationale:** Bounded resource usage; clear errors. Google Meet transcripts are always small text files — 10 MB is generous.

## Risks / Trade-offs

- **[Risk] PAT leakage via logs, error messages, or bookmarklet source.** → Mitigation: never log the full `Authorization` header; bookmarklet (shipped later) will store the PAT in `localStorage` scoped to Mental Metal's origin, never in the bookmark URL itself; add explicit guards in logging middleware that strip `Authorization` values.
- **[Risk] CORS allowlist of `docs.google.com` could be broadened accidentally, exposing other endpoints.** → Mitigation: the CORS policy is applied per-route, not globally. A test asserts that `POST /api/captures` (without `/import`) returns a CORS preflight rejection from `docs.google.com`.
- **[Risk] `.docx` parser encounters a malformed or malicious file, throwing or consuming resources.** → Mitigation: size cap + a try/catch around the parser returning 400 with a generic message; no stack traces exposed. Run the parser on a streamed input, not the full buffer.
- **[Risk] Transcript format detection misfires on non-Meet content that happens to match the regex.** → Mitigation: detection is a normalizer only; on a false match, the content is still stored as plain text. The only observable effect is that the speaker-mapping UI sees labels that don't correspond to real speakers, which the user can ignore or clean up.
- **[Trade-off] Storing `Scopes` as a string set instead of typed claims.** → Accepted: extensibility > type safety here; invalid scopes are caught at creation time.
- **[Trade-off] Running parsing synchronously inside the request pipeline.** → Accepted for v1: file sizes are bounded and parsing is fast for the target formats. If large uploads become common, the endpoint can return `202 Accepted` and hand off to a background worker, reusing the existing `BriefRefreshQueue`-style pattern.
- **[Risk] PAT rotation is manual — a leaked token can only be remediated by the user revoking it.** → Accepted: consistent with industry PAT norms; revocation is one click. Future: email alerts on new token creation, similar to GitHub.

## Migration Plan

- **Database:** one new migration adding `personal_access_tokens` table with `Id`, `UserId` (indexed, FK to `users`), `Name`, `Scopes` (jsonb), `TokenHash` (bytea, indexed unique), `TokenLookupPrefix` (bytea, indexed), `CreatedAt`, `LastUsedAt`, `RevokedAt`. Global query filter on `UserId`. No data backfill.
- **No existing data changes.** `captures`, `users`, etc. are untouched.
- **Deployment order:** backend first (PAT middleware + import endpoint), then frontend (Settings PAT UI + Quick Capture drop-zone).
- **Rollback:** revert Angular changes for UI issues; revert backend + roll the migration back (`dotnet ef migrations remove` on an unmerged branch; once merged, a reverse migration) for server-side issues. Existing captures are unaffected.
- **Feature flagging:** none. The endpoint is safe-by-default (requires auth); the UI additions are additive.

## Open Questions

- **Should PATs expire automatically?** Proposed: no hard expiry in v1; the user revokes explicitly. Revisit if we add organisation-managed tokens.
- **Should we surface `LastUsedAt` with precise IP / user-agent?** Proposed: store `LastUsedAt` only in v1; audit detail can be added later without a schema change if needed.
- **Should imported transcripts default `triaged = true` or surface in the daily close-out queue like regular captures?** Proposed: same as regular captures — enter the close-out queue. The user still reviews imports as part of their triage ritual.
- **Do we want to persist `sourceUrl` as a structured property on `Capture`, or treat it as free-text metadata in the existing `source` field?** Proposed: reuse the existing `Source` field (free-text) for v1. A structured URL field would be a `capture-text` change we can defer until a UI surface needs it.
- **File size cap 10 MB is generous but arbitrary.** Proposed: keep at 10 MB; revisit if Drive exports of long meetings approach the limit.
