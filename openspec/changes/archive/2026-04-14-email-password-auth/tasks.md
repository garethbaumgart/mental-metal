## 1. Domain

- [x] 1.1 Add `Microsoft.AspNetCore.Identity` NuGet reference to `MentalMetal.Domain` (or `MentalMetal.Application`, whichever already owns hashing contracts — prefer keeping Domain dependency-free and take the dep in Application/Infrastructure only, with the domain exposing an `IPasswordHasher` abstraction)
- [x] 1.2 Create `Password` value object in `src/MentalMetal.Domain/Users/Password.cs` — private ctor taking hash string, static `Create(string plaintext, IPasswordHasher<User> hasher)`, `Verify(string plaintext, IPasswordHasher<User> hasher): bool`, equality on hash value. Mirror style of `Email` VO
- [x] 1.3 Update `User` aggregate in `src/MentalMetal.Domain/Users/User.cs`: add nullable `Password? PasswordHash`; make `ExternalAuthId` nullable; invariant-check that at least one of the two is set after construction
- [x] 1.4 Add `User.RegisterWithPassword(Email, Name, Password, timezone, ...)` factory that returns a User with null `ExternalAuthId` and raises `UserRegistered`
- [x] 1.5 Add `User.SetPassword(string plaintext, IPasswordHasher<User> hasher)` domain method that replaces or sets `PasswordHash`
- [x] 1.6 Add `User.VerifyPassword(string plaintext, IPasswordHasher<User> hasher): bool` returning false if `PasswordHash` is null
- [x] 1.7 Add `IUserRepository.GetByEmailAsync(Email)` abstraction
- [x] 1.8 Domain unit tests: `Password` VO creation + verification round-trip, rejects short plaintext at factory boundary; `User.RegisterWithPassword` sets state correctly; `User.SetPassword` sets and replaces; `User.VerifyPassword` returns false when hash is null, false on wrong password, true on correct

## 2. Infrastructure

- [x] 2.1 Implement `IUserRepository.GetByEmailAsync` in `UserRepository.cs`
- [x] 2.2 Update EF Core config for `User`: map `PasswordHash` as owned/converted to nullable text column; relax `ExternalAuthId` nullability; keep the unique index on `ExternalAuthId` but ensure Postgres null-tolerance (default behaviour)
- [x] 2.3 Create EF migration `AddPasswordHashToUsers` adding the nullable `PasswordHash` column and altering `ExternalAuthId` to nullable; verify the generated SQL is safe to apply to a non-empty Users table
- [x] 2.4 Register `IPasswordHasher<User>` → `PasswordHasher<User>` singleton in DI composition (Infrastructure module registration or `Program.cs`)

## 3. Application (handlers)

- [x] 3.1 `RegisterWithPasswordHandler` — validates email format, password length ≥ 8, non-empty name; checks `ExistsByEmailAsync`; constructs `Password` via factory; creates User via `RegisterWithPassword`; persists; generates tokens via existing `ITokenService`
- [x] 3.2 `LoginWithPasswordHandler` — looks up via `GetByEmailAsync`; treats null/missing user and null `PasswordHash` the same as bad credentials; calls `User.VerifyPassword`; on success calls `RecordLogin` and issues tokens
- [x] 3.3 `SetPasswordHandler` — takes current `UserId` from `ICurrentUserService`; loads user; validates password length ≥ 8; calls `User.SetPassword`; persists
- [x] 3.4 Application-layer unit tests for each handler: happy path, duplicate email → 409, bad credentials → 401, Google-only user login → 401, unauthenticated set-password → 401 (handler-level rejection or covered at endpoint level)

## 4. Web (minimal APIs)

- [x] 4.1 `POST /api/auth/register` — anonymous; binds `{ email, password, name }`; returns `{ accessToken, user }` with refresh token cookie on 200; 409 on duplicate email; 400 on validation failure
- [x] 4.2 `POST /api/auth/login` — anonymous; binds `{ email, password }`; returns `{ accessToken, user }` with refresh token cookie on 200; 401 on any credential failure (no case disclosure)
- [x] 4.3 `POST /api/auth/password` — requires JWT; binds `{ newPassword }`; returns 204 on success; 400 on short password
- [x] 4.4 Update the current-user endpoint (`GET /api/users/me` or equivalent) to include `hasPassword: bool` derived from `User.PasswordHash != null`
- [x] 4.5 Ensure OpenAPI/Swagger docs (if auto-generated) list the new endpoints and request/response types

## 5. Frontend — auth service

- [x] 5.1 `auth.service.ts` — add `loginWithPassword(email, password)` → POST `/api/auth/login`, take access token from response body, hand into existing token-storage signal plumbing
- [x] 5.2 `auth.service.ts` — add `registerWithPassword(email, password, name)` → POST `/api/auth/register`, same post-success handling
- [x] 5.3 `auth.service.ts` — add `setPassword(newPassword)` → POST `/api/auth/password`; on success update local `currentUser` signal so `hasPassword` flips to true
- [x] 5.4 Extend the user profile model/signal with `hasPassword: boolean` (sourced from `/api/users/me`)

## 6. Frontend — login page

- [x] 6.1 Update `pages/login/login.page.ts` — add email/password form using Signal Forms (Angular 21); two modes (`login` / `register`) toggled via a signal; `@if` control flow (no `*ngIf`)
- [x] 6.2 Wire submit handlers to `authService.loginWithPassword` and `authService.registerWithPassword`; on success, navigate to the same post-login route the Google flow uses
- [x] 6.3 Surface errors: generic "invalid credentials" on 401; "email already in use" on 409; inline validation for short password / bad email / empty name
- [x] 6.4 Keep the existing "Sign in with Google" button above/beside the form, layout via Tailwind layout utilities only — no colour utilities, no `dark:` prefix, PrimeNG components where applicable (e.g., `p-inputtext`, `p-password`, `p-button`)

## 7. Frontend — settings password section

- [x] 7.1 Locate the existing settings page; add a new "Password" section. If no suitable page exists, create a minimal settings page and add it to the router + side nav (follow existing routing patterns)
- [x] 7.2 Driven by `currentUser().hasPassword`: render "Set a password" heading when false, "Change password" when true; single new-password input + submit button
- [x] 7.3 Submit calls `authService.setPassword`; show a PrimeNG `p-message` or toast confirming success; clear input after success
- [x] 7.4 Inline validation for short password; disabled submit button while request is in flight

## 8. Tests

- [x] 8.1 Backend: handler unit tests per 3.4
- [x] 8.2 Backend: endpoint integration tests hitting a real Postgres (per project convention) — register happy path, duplicate email, login success, login failure, set-password success, set-password unauthorised
- [x] 8.3 Frontend: component tests for the login page covering login mode, register mode, and error surfaces
- [x] 8.4 Frontend: component tests for the settings password section covering "set" and "change" variants
- [x] 8.5 E2E: golden-path smoke — register a new user, log out, log back in via email/password, set-password-as-Google-user link flow (only if Google flow can be stubbed in E2E; otherwise cover by integration + unit)

## 9. Ship

- [x] 9.1 Run backend test suite: `dotnet test src/MentalMetal.slnx`
- [x] 9.2 Run frontend test suite: `(cd src/MentalMetal.Web/ClientApp && npx ng test --watch=false)`
- [ ] 9.3 Run E2E suite with Docker dev stack if applicable
- [ ] 9.4 Manually verify the login page, register flow, and settings password section in a browser with the dev stack running
- [x] 9.5 Open PR via the `/pr` skill (per project convention)
