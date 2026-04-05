## Why

Mental Metal requires user authentication and multi-tenant data isolation as the foundational layer before any feature can be built. Every aggregate in the domain model is user-scoped — without a User aggregate backed by OAuth registration and tenant-scoped data access, no other Tier 1 or Tier 2 capability can function. This is the first spec to implement because all other specs depend on it.

## What Changes

- **User registration via OAuth** — users authenticate through an external OAuth provider (Google initially), creating a User aggregate on first login with ExternalAuthId, Email, Name, and AvatarUrl populated from the identity token
- **JWT-based session management** — after OAuth callback, the backend issues short-lived JWTs for API authentication; refresh tokens enable seamless session continuity
- **Multi-tenant data scoping** — all database queries are automatically filtered by the authenticated user's UserId via a global EF Core query filter, enforcing tenant isolation at the infrastructure layer
- **User preferences** — users can update their profile (name, avatar, timezone) and preferences (theme, notification settings, briefing schedule) through dedicated API endpoints
- **Frontend auth flow** — Angular auth service manages login/logout, token storage, route guards for protected pages, and automatic token refresh
- **User profile UI** — settings page for managing profile information and preferences

## Non-goals

- **AI provider configuration** — covered by the separate `ai-provider-abstraction` spec
- **Role-based access control (RBAC)** — single-user tenancy model; no roles or permissions beyond "owner of your own data"
- **Team/org hierarchy** — each user is an independent tenant; no shared workspaces
- **Social login providers beyond Google** — Google OAuth only for initial release; additional providers can be added later
- **Email verification flow** — OAuth provider handles identity verification

## Capabilities

### New Capabilities
- `user-auth-tenancy`: OAuth registration, JWT session management, multi-tenant scoping via UserId, user profile and preferences management

### Modified Capabilities
<!-- No existing specs to modify — this is the first spec -->

## Impact

- **Domain layer**: New `User` aggregate with `UserPreferences` value object, `Email` value object, domain events (`UserRegistered`, `UserProfileUpdated`, `PreferencesUpdated`)
- **Application layer**: Registration handler, profile update handler, preferences handler, current-user query
- **Infrastructure layer**: `UserRepository`, EF Core `User` entity configuration, global UserId query filter on `MentalMetalDbContext`, JWT token service, OAuth integration service
- **Web layer**: Auth endpoints (`/api/auth/login`, `/api/auth/callback`, `/api/auth/refresh`, `/api/auth/logout`), user endpoints (`/api/users/me`, `/api/users/me/preferences`), authentication middleware
- **Frontend**: `AuthService`, `AuthInterceptor`, route guards, login page, user settings page
- **Database**: New `Users` table migration
- **Affected aggregates**: User (primary). All future aggregates will depend on the UserId scoping established here
- **Tier**: Tier 1 (Foundation) — no dependencies; depended on by all Tier 2 specs
