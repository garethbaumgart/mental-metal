## Context

Mental Metal has no authentication or user management today. The `MentalMetalDbContext` is empty, `Program.cs` has no auth middleware, and the Angular app has no login flow. Every aggregate in the domain model requires a `UserId` foreign key, making this the foundational capability that all other features depend on.

The User aggregate is defined in `design/domain-model.md` with properties: Id, ExternalAuthId, Email (VO), Name, AvatarUrl, Preferences (VO), AiProviderConfig (VO), Timezone, CreatedAt, LastLoginAt. Business actions include Register, UpdateProfile, UpdatePreferences, and RecordLogin.

## Dependencies

None — this is a Tier 1 foundation spec with no upstream dependencies. All Tier 2 specs depend on this.

## Goals / Non-Goals

**Goals:**
- Implement the User aggregate with full DDD behaviour (Register, UpdateProfile, UpdatePreferences, RecordLogin)
- OAuth login via Google using OpenID Connect
- JWT-based API authentication with refresh token rotation
- Automatic UserId-scoped query filtering at the EF Core level
- Angular auth flow with route guards and token management
- User settings page for profile and preferences

**Non-Goals:**
- AI provider configuration (separate `ai-provider-abstraction` spec)
- Multiple OAuth providers (Google only for now)
- Role-based access control or permissions
- Admin/impersonation capabilities
- Email/password authentication

## Decisions

### 1. Google OAuth via OpenID Connect with backend-driven flow

The backend handles the full OAuth flow using `Microsoft.AspNetCore.Authentication.Google`. The Angular app redirects to a backend `/api/auth/login` endpoint which initiates the OAuth challenge. After Google callback, the backend creates/updates the User aggregate and returns JWT + refresh token.

**Why not a frontend-driven OAuth flow (e.g., Google Identity Services)?** Backend-driven keeps token exchange server-side, avoids CORS complexity with Google's token endpoint, and lets us create the User aggregate in the same request. The frontend only needs to handle JWTs it receives from our own backend.

**Why not Auth0/Clerk/other managed auth?** The auth requirements are simple (single OAuth provider, no RBAC). A managed service adds cost, latency, and an external dependency we don't need. Native .NET auth middleware is well-supported and sufficient.

### 2. JWT access tokens + HTTP-only refresh tokens

Access tokens are short-lived (15 min), signed with a symmetric key, and sent via `Authorization: Bearer` header. Refresh tokens are longer-lived (7 days), stored in an HTTP-only secure cookie, and support rotation (each use invalidates the old token and issues a new one).

**Why not session cookies only?** JWTs are stateless on the backend — no session store needed. This keeps the API horizontally scalable on Cloud Run without sticky sessions or shared session storage.

**Why not store refresh tokens in localStorage?** HTTP-only cookies are not accessible to JavaScript, mitigating XSS-based token theft. The refresh token never leaves the cookie jar.

### 3. Global EF Core query filter for tenant isolation

A global query filter on `MentalMetalDbContext` automatically appends `WHERE UserId = @currentUserId` to all queries on entities implementing `IUserScoped`. The current user ID is injected via `ICurrentUserService` which reads from the JWT claims.

**Why a global filter rather than per-repository filtering?** Global filters are impossible to forget. Every new entity that implements `IUserScoped` is automatically tenant-filtered. Per-repository filtering is error-prone — a single missed filter is a data leak. The filter can be explicitly bypassed with `IgnoreQueryFilters()` for the rare cases that need it (e.g., checking email uniqueness during registration).

### 4. User aggregate owns registration logic

`User.Register(authId, email, name, avatarUrl)` is a static factory method on the aggregate that validates invariants and raises `UserRegistered`. The application handler orchestrates: check if user exists by ExternalAuthId → if yes, RecordLogin() → if no, Register() → issue tokens.

**Why not a domain service for registration?** Registration logic is simple enough to live on the aggregate. The uniqueness check (ExternalAuthId, Email) is enforced at the database level with unique indexes, and at the application level by the handler. No cross-aggregate coordination is needed.

### 5. Value objects: Email, UserPreferences

`Email` is a value object that validates format on construction. `UserPreferences` holds theme (light/dark), timezone display format, notification settings, and briefing schedule time. Both are embedded in the User aggregate and stored as owned types in EF Core.

### 6. Angular auth with interceptor and route guards

`AuthService` manages login state via signals. An HTTP interceptor attaches the access token to API requests and handles 401 responses by attempting a silent refresh. A functional route guard (`authGuard`) redirects unauthenticated users to the login page. Token storage uses `localStorage` for the access token (short-lived, acceptable risk) and HTTP-only cookie for the refresh token (set by backend).

**Why signals for auth state?** Consistent with the app's zoneless architecture. Components reactively update when auth state changes without manual subscription management.

## Risks / Trade-offs

- **[Symmetric JWT signing key]** → Sufficient for a single-service deployment on Cloud Run. If we add multiple services later, we'll migrate to asymmetric keys (RSA/ECDSA). Low migration cost.
- **[No token revocation list]** → Short-lived access tokens (15 min) limit the blast radius. Refresh token rotation means a stolen refresh token is invalidated on next legitimate use. Acceptable for single-user tenancy.
- **[Global query filter bypass risk]** → `IgnoreQueryFilters()` usage must be reviewed carefully. Mitigated by limiting its use to infrastructure-layer code only and flagging it in code review.
- **[Google OAuth single provider]** → If Google has an outage, users cannot log in. Acceptable risk for initial launch; adding a second provider later is straightforward with the existing auth middleware pattern.

## Migration Plan

1. Add EF Core migration for `Users` table with unique indexes on ExternalAuthId and Email
2. Configure Google OAuth and JWT middleware in `Program.cs`
3. Deploy with feature — no existing data to migrate, no rollback concerns for a greenfield table

## Open Questions

- **Refresh token storage**: Database table vs in-memory cache? Database is safer for Cloud Run (instances can restart), but adds a query per refresh. Leaning toward database with a simple `RefreshToken` table.
- **Google OAuth credentials**: Where to store client ID/secret in staging/prod? Environment variables via Cloud Run secrets is the likely answer, but needs confirmation with infra setup.
