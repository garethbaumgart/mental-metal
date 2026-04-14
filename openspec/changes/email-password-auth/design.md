## Context

Authentication today is Google OAuth only (see `user-auth-tenancy` spec). JWT bearer is the default authenticate scheme; the cookie scheme exists solely to carry Google's callback state. The `User` aggregate stores `ExternalAuthId` (Google sub, required, unique) and `Email` (unique). Token issuance and refresh rotation are already implemented and reusable (`ITokenService`, `RefreshTokens` table).

Adding email/password login is additive: it introduces a second credential type on the existing `User` aggregate and two new unauthenticated endpoints that hand off to the existing token pipeline. No new auth scheme is needed — the JWT issued by a password login is indistinguishable from one issued by a Google login, so every downstream consumer (`ICurrentUserService`, interceptors, authorization) works unchanged.

## Goals / Non-Goals

**Goals:**
- Users can register a new account with email + password and log in.
- Existing Google users can attach a password to their account (linking) via an authenticated endpoint and settings UI.
- Password-auth users get the same JWT + refresh-token session as Google users — no forked auth code paths downstream.
- Password storage uses a modern, framework-supported hash (PBKDF2 via `Microsoft.AspNetCore.Identity.PasswordHasher<T>`).

**Non-Goals:**
- Email verification on register.
- Password reset / "forgot password".
- Login rate limiting / account lockout (deferred to a later hardening pass; document the gap).
- Strong password policy (minimum length only for v1).
- Email-based account linking (a Google user is not linked to a registration attempt that happens to share the same email — registration with an existing email fails).
- Migrating existing Google-only users; they keep working unchanged.

## Decisions

### D1: Extend existing User aggregate vs. separate Credential aggregate
**Choice:** Extend `User`. Add nullable `Password` value object; make `ExternalAuthId` nullable.

**Alternatives considered:**
- *Separate `Credential` aggregate per auth method.* More flexible (multiple credentials per user, per-credential revocation) but over-engineered for two fixed methods and violates the "keep it bare bones" scope. Adds a second aggregate for data that has a 1:1 relationship with `User`.

**Rationale:** `User` already owns identity fields (`Email`, `ExternalAuthId`). A password is another optional identity fact about the same user, not an independent aggregate. Mirrors how `AiProviderConfig` was added as an optional VO on `User`.

### D2: Password as a value object, not a plain string
**Choice:** New `Password` VO wrapping a hash string. Factory `Password.Create(plaintext, IPasswordHasher<User>)`; instance method `Verify(plaintext, IPasswordHasher<User>): bool`.

**Alternatives considered:**
- *Plain nullable `PasswordHash` string on `User` + domain methods on `User` itself.* Less ceremony, one fewer file, but inconsistent with `Email` (also a VO) and leaks hash-format concerns out of a cohesive type.

**Rationale:** Consistency with `Email`. Encapsulates hash-vs-plaintext confusion at the type level — a `Password` instance is *always* a hash, and the only way to get one is through a factory that takes plaintext + hasher.

### D3: Password hashing library
**Choice:** `Microsoft.AspNetCore.Identity.PasswordHasher<User>` from `Microsoft.AspNetCore.Identity`. Registered as `IPasswordHasher<User>` singleton in DI.

**Alternatives considered:**
- *BCrypt.Net-Next.* Well-known, focused. But adds a third-party dependency for a capability .NET ships in-framework.
- *Argon2 (Konscious).* Stronger (memory-hard) but overkill for current scope and adds a dep.

**Rationale:** In-framework, PBKDF2 with automatic rehash detection, used by millions of ASP.NET Identity deployments. We import `Microsoft.AspNetCore.Identity` for this one type only — no Identity schema, middleware, or endpoints.

### D4: Linking behaviour on email collision
**Choice:** Register rejects with 409 if the email already exists. Existing users (Google-only) add a password via the authenticated `POST /api/auth/password` endpoint.

**Alternatives considered:**
- *Automatic linking when emails match at register time.* Friendlier but trust-unsafe: anyone who knows Alice's email could claim her account by registering, then read her data. Would require email verification to be safe — which is out of scope.
- *Prompt "sign in with Google instead" at the UI layer on collision.* Already effectively what happens (409 surfaces a message); no backend change needed.

**Rationale:** Proving account ownership by being logged in is the cheapest safe linking path. The user is already authenticated via Google when they set a password, so there's no ambiguity about who owns the account.

### D5: `POST /api/auth/password` semantics
**Choice:** One endpoint that both sets (first-time) and changes (replace) the password. Body: `{ newPassword }`. No old-password confirmation in v1.

**Alternatives considered:**
- *Separate `set` and `change` endpoints.* More REST-purist but the difference is a nullability check the backend already needs to perform.
- *Require current password on change.* Standard security control to prevent session-hijack-to-password-takeover. Deferred: v1 relies on the short JWT lifetime; add as a hardening follow-up.

**Rationale:** Single endpoint keeps handler count and UI state small. The current-password requirement is worth adding later but is not table stakes given our session lifetime.

### D6: Response shape for register/login endpoints
**Choice:** Return JWT in the JSON response body (`{ accessToken, user }`), set refresh token as HttpOnly cookie — same as the Google callback, just without the hash-fragment redirect.

**Alternatives considered:**
- *Also set access token as a cookie.* Would require frontend changes to read the token; breaks parity with the existing localStorage-based flow.
- *Redirect with hash fragment like Google.* Google does that because it starts with a server-side redirect from Google's IdP. Password endpoints are called directly from the SPA via fetch; a redirect is the wrong primitive.

**Rationale:** Parity with how `AuthService` already stores the Google-issued token (once extracted from the hash). Frontend plumbing downstream of "we got a JWT" is unchanged.

### D7: `ExternalAuthId` becomes nullable
**Choice:** Make `ExternalAuthId` nullable (drop NOT NULL, keep unique index — Postgres unique indexes ignore nulls by default).

**Alternatives considered:**
- *Sentinel value like `"local:{userId}"`.* Avoids nullability but pollutes query logic and lookups.

**Rationale:** Nullable is the honest modelling. A user registered with password + no Google linkage genuinely has no external auth id.

### D8: `hasPassword` exposed on `/api/users/me`
**Choice:** Add `hasPassword: bool` to the current-user response.

**Rationale:** The settings UI needs to pick "Set a password" vs "Change password" copy. Exposing only the boolean (never the hash or any derivative) is the minimal leak.

## Risks / Trade-offs

- **[No rate limiting]** → Brute-force attempts against `/api/auth/login` are not throttled. Mitigation: short-lived JWT limits session-theft blast radius; add middleware-level rate limiting in a follow-up hardening change. Document in the spec's non-goals.
- **[No current-password check on change]** → An attacker with a stolen JWT can rotate the password and extend access. Mitigation: 15-minute access token lifetime keeps the window small; add old-password requirement in a follow-up.
- **[No email verification]** → A user could register with someone else's email. Mitigation: this does not grant access to that person's existing account (register rejects on existing email), but does let an attacker squat on an unused email. Acceptable for v1; add verification in a follow-up.
- **[Identity package footprint]** → Pulling in `Microsoft.AspNetCore.Identity` for one class. Mitigation: we register only `IPasswordHasher<User>` and do not add Identity's EF schema, middleware, or endpoints. If footprint becomes an issue, swap to BCrypt.Net-Next (single-class replacement).
- **[Nullable ExternalAuthId + Email unique index]** → No behavioural risk, but test migration on a DB snapshot to confirm Postgres does not reject the column alter on existing rows.

## Migration Plan

1. Ship the migration adding `PasswordHash` (nullable text) and altering `ExternalAuthId` to nullable.
2. Deploy backend — existing Google users unaffected (all have ExternalAuthId set, PasswordHash null).
3. Deploy frontend — login page gains password form, settings gains password section.
4. Rollback: revert frontend first (UI disappears, endpoints still live but unreachable from UI), then revert backend. The migration is additive-only, so no data loss; if rollback is needed the new column and relaxed constraint are left in place to avoid churn, and a later forward migration can revisit.

## Dependencies

- `user-auth-tenancy` (Tier 1) — being modified by this change.
- `ai-provider-abstraction`, `person-management`, `initiative-management` — untouched; listed only to confirm no cross-capability impact.

## Open Questions

- Minimum password length: 8 or 10? Defaulting to 8 in tasks; flag for confirmation during implementation.
- Should register also accept an optional `timezone` (Google flow infers from browser later)? Defaulting to a sensible server-side value (`Etc/UTC`) until the user edits it on the settings page.
