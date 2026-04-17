## ADDED Requirements

### Requirement: Generate personal access token

The system SHALL allow an authenticated user to generate a Personal Access Token (PAT) scoped to a set of one or more permission strings. The request SHALL include a non-empty `name` and at least one `scope` from the supported scope set (v1 supported scopes: `captures:write`). The system SHALL produce a cryptographically random token, hash it with SHA-256 using a versioned server-side pepper, and persist the hash, the user id, the name, the scope set, and `CreatedAt`. The plaintext token SHALL be returned in the response body exactly once and SHALL NOT be retrievable again. The plaintext token SHALL be prefixed with `mm_pat_` for identifiability. The system SHALL raise a `PersonalAccessTokenCreated` domain event.

#### Scenario: Create a token with the captures:write scope

- **WHEN** an authenticated user sends a POST to `/api/personal-access-tokens` with `{ "name": "Import bookmarklet", "scopes": ["captures:write"] }`
- **THEN** the system returns HTTP 201 with `{ id, name, scopes, createdAt, token }` where `token` starts with `mm_pat_` and the token hash is persisted; subsequent GETs never return the plaintext

#### Scenario: Missing name rejected

- **WHEN** an authenticated user POSTs a token creation request with empty or whitespace-only `name`
- **THEN** the system returns HTTP 400 with a validation error

#### Scenario: Empty scope set rejected

- **WHEN** an authenticated user POSTs a token creation request with an empty `scopes` array
- **THEN** the system returns HTTP 400 with a validation error

#### Scenario: Unknown scope rejected

- **WHEN** an authenticated user POSTs a token creation request with a scope string that is not in the supported scope set (e.g., `"admin:all"`)
- **THEN** the system returns HTTP 400 with a validation error listing the unsupported scope

#### Scenario: Token plaintext only returned once

- **WHEN** an authenticated user later GETs `/api/personal-access-tokens` or `/api/personal-access-tokens/{id}`
- **THEN** the plaintext token is NOT returned; only `{ id, name, scopes, createdAt, lastUsedAt, revokedAt }` are returned

### Requirement: List personal access tokens

The system SHALL allow an authenticated user to retrieve their own tokens, ordered by `CreatedAt` descending. The response SHALL include `id`, `name`, `scopes`, `createdAt`, `lastUsedAt`, and `revokedAt`, but SHALL NOT include the plaintext token or the hash.

#### Scenario: List returns user's tokens

- **WHEN** an authenticated user sends a GET to `/api/personal-access-tokens`
- **THEN** the system returns an array of that user's tokens with metadata only, ordered by `createdAt` descending, and HTTP 200

#### Scenario: List isolates by user

- **WHEN** User A and User B each have tokens
- **THEN** User A's list contains only User A's tokens and User B's list contains only User B's tokens

#### Scenario: Empty list

- **WHEN** an authenticated user with no tokens sends a GET to `/api/personal-access-tokens`
- **THEN** the system returns an empty array with HTTP 200

### Requirement: Revoke personal access token

The system SHALL allow an authenticated user to revoke a token they own. Revoking SHALL set `RevokedAt` to the current UTC time. Subsequent requests using the revoked token SHALL fail authentication. Revoking an already-revoked token SHALL be idempotent. The system SHALL raise a `PersonalAccessTokenRevoked` domain event on the first revocation only.

#### Scenario: Revoke an active token

- **WHEN** an authenticated user sends a POST to `/api/personal-access-tokens/{id}/revoke` for a token they own
- **THEN** `RevokedAt` is set, a `PersonalAccessTokenRevoked` event is raised, and the system returns HTTP 204

#### Scenario: Revocation is idempotent

- **WHEN** an authenticated user sends a POST to `/api/personal-access-tokens/{id}/revoke` for a token they have already revoked
- **THEN** the system returns HTTP 204 and `RevokedAt` is unchanged; no second event is raised

#### Scenario: Cannot revoke another user's token

- **WHEN** User A sends a POST to `/api/personal-access-tokens/{id}/revoke` with a token id belonging to User B
- **THEN** the system returns HTTP 404 and User B's token is unchanged

#### Scenario: Revoked token cannot authenticate

- **WHEN** a client presents a revoked token as `Authorization: Bearer <token>` to a PAT-accepting endpoint
- **THEN** the system returns HTTP 401 and does not create or modify any resource

### Requirement: Bearer authentication for PAT-accepting endpoints

The system SHALL register an authentication scheme that parses `Authorization: Bearer <token>` headers, rejects tokens not prefixed with `mm_pat_`, hashes the presented token with SHA-256 and the server pepper, looks up the hashed value against stored token hashes, and authenticates the request as the owning user if and only if an active (non-revoked) matching token is found. On a successful authentication, the system SHALL update the token's `LastUsedAt` to the current UTC time. The authenticated principal SHALL include a `scope` claim per scope string granted to the token.

#### Scenario: Valid token authenticates

- **WHEN** a client presents `Authorization: Bearer mm_pat_<valid-token>` on an endpoint that accepts PAT auth
- **THEN** the request is authenticated as the token's owning user, the owner's user id is available via the standard current-user abstraction, and `LastUsedAt` is updated

#### Scenario: Token without prefix rejected

- **WHEN** a client presents `Authorization: Bearer <token-without-mm_pat_-prefix>`
- **THEN** the system returns HTTP 401 without performing any hash lookup

#### Scenario: Unknown token rejected

- **WHEN** a client presents a well-formatted but unknown `mm_pat_<token>` value
- **THEN** the system returns HTTP 401 and no `LastUsedAt` is updated on any row

#### Scenario: Scope required by endpoint enforced

- **WHEN** a client presents a valid token that does NOT include the scope required by the endpoint
- **THEN** the system returns HTTP 403

#### Scenario: Cookie auth and bearer auth coexist

- **WHEN** an endpoint is configured to accept both session-cookie auth and PAT bearer auth, and a request arrives with a session cookie and no `Authorization` header
- **THEN** the request is authenticated via the cookie scheme and the PAT scheme is not consulted

### Requirement: PAT storage is one-way

The system SHALL persist only the hash of each token, never the plaintext. The hash algorithm SHALL be SHA-256 applied to the concatenation of a versioned server-side pepper and the plaintext token. The stored row SHALL include a small (derived from the hash) prefix column indexed for fast lookup. The plaintext token SHALL be excluded from logs, error messages, and audit records.

#### Scenario: Plaintext never persisted

- **WHEN** a token is generated
- **THEN** the database row contains the hash and prefix but no column ever contains the plaintext

#### Scenario: Authorization header redacted in logs

- **WHEN** the system logs a request or error involving an `Authorization: Bearer mm_pat_<token>` header
- **THEN** the logged value SHALL be redacted (e.g., replaced with `[REDACTED]`)

### Requirement: PAT management UI in Settings

The frontend SHALL provide a Personal Access Tokens page under Settings where a user can view their active tokens, generate a new token, and revoke an existing token. The generation flow SHALL display the plaintext token exactly once in a copyable surface with an explicit warning that it will not be shown again. Revoked tokens SHALL be visually distinct from active tokens and SHALL remain visible in the list until the user chooses to clear them.

#### Scenario: View active and revoked tokens

- **WHEN** an authenticated user navigates to the Settings → Personal Access Tokens page
- **THEN** the system displays each of the user's tokens with name, scopes, created-at, last-used-at, and active/revoked status

#### Scenario: Generate a token from the UI

- **WHEN** an authenticated user opens the generate dialog, enters a name, selects at least one scope, and submits
- **THEN** the UI displays the plaintext token exactly once in a copyable panel with a "You will not see this token again" warning, and the new token appears in the list with status Active

#### Scenario: Revoke from the UI

- **WHEN** an authenticated user clicks the Revoke action on an active token and confirms
- **THEN** the token's status changes to Revoked in the list and subsequent uses of the token are rejected
