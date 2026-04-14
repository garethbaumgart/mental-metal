## Why

Google OAuth is currently the only way to sign in. This excludes anyone who doesn't want to (or can't) use a Google account, and leaves users without a fallback if their Google identity becomes unavailable. Adding email/password as a second authentication method removes that dependency and gives existing Google users a local credential they can use to recover or diversify access.

## What Changes

- Add email/password registration: `POST /api/auth/register` creates a new User with a hashed password and issues the same JWT + refresh-token pair the Google flow issues.
- Add email/password login: `POST /api/auth/login` verifies credentials and issues tokens.
- Add authenticated password set/change: `POST /api/auth/password` lets a signed-in user attach a password to their account (the linking mechanism for existing Google-only users).
- Extend the `User` aggregate with an optional `Password` value object wrapping a PBKDF2 hash; make `ExternalAuthId` nullable so password-only users are representable.
- Extend `GET /api/users/me` with `hasPassword: bool` so the frontend can show "Set a password" vs "Change password".
- Add a password form to the existing login page (beside the Google button) and a password section to the settings page.
- Reject register attempts whose email already exists — linking happens via the authenticated `/api/auth/password` endpoint, not by matching emails at registration time.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `user-auth-tenancy`: Adds email/password as a second authentication method. Extends the User aggregate to support a password hash alongside (or instead of) an external auth identity. Extends the `/api/users/me` response with password presence.

## Impact

**Domain / code**
- `src/MentalMetal.Domain/Users/User.cs` — new `Password` value object; `PasswordHash: Password?` property; `ExternalAuthId` becomes nullable; new factory `RegisterWithPassword`; new methods `SetPassword`, `VerifyPassword`.
- `src/MentalMetal.Domain/Users/IUserRepository.cs` — add `GetByEmailAsync(Email)`.
- `src/MentalMetal.Application/Users/` — new vertical-slice handlers: `RegisterWithPassword`, `LoginWithPassword`, `SetPassword`.
- `src/MentalMetal.Web/Program.cs` — three new minimal-API endpoints; register `IPasswordHasher<User>` singleton.
- `src/MentalMetal.Infrastructure/Migrations/` — new migration adding nullable `PasswordHash` column and making `ExternalAuthId` nullable.

**Frontend**
- `pages/login/login.page.ts` — email/password form alongside Google button.
- `shared/services/auth.service.ts` — `loginWithPassword`, `registerWithPassword`, `setPassword`.
- Settings page — new "Password" section (set/change).

**Dependencies**
- New NuGet: `Microsoft.AspNetCore.Identity` (used only for `PasswordHasher<T>`; no Identity schema or middleware).

**Non-goals (explicit out of scope for v1)**
- Email verification on register
- Password reset / "forgot password" flow
- Login rate limiting / lockout
- Password strength rules beyond a minimum length
- Linking by matching email during register (Google users must sign in first, then add a password via settings)

**Tier**
- Enhancement to Tier 1 foundation (`user-auth-tenancy`). No new tier dependencies introduced.

**Affected aggregates**
- `User` (only).
