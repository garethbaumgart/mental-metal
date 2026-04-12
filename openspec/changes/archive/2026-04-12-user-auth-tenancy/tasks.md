## 1. Domain Layer

- [x] 1.1 Create `Email` value object with format validation
- [x] 1.2 Create `UserPreferences` value object (theme, notification settings, briefing schedule time)
- [x] 1.3 Create `User` aggregate with properties: Id, ExternalAuthId, Email, Name, AvatarUrl, Preferences, Timezone, CreatedAt, LastLoginAt
- [x] 1.4 Implement `User.Register(authId, email, name, avatarUrl)` static factory method raising `UserRegistered`
- [x] 1.5 Implement `User.UpdateProfile(name, avatarUrl, timezone)` with validation, raising `UserProfileUpdated`
- [x] 1.6 Implement `User.UpdatePreferences(preferences)` raising `PreferencesUpdated`
- [x] 1.7 Implement `User.RecordLogin()` to update LastLoginAt
- [x] 1.8 Create `IUserRepository` interface in Domain
- [x] 1.9 Create `ICurrentUserService` interface in Domain (provides current UserId)
- [x] 1.10 Create `IUserScoped` interface with UserId property for global query filtering

## 2. Domain Tests

- [x] 2.1 Test `Email` value object: valid formats accepted, invalid formats rejected, equality
- [x] 2.2 Test `User.Register`: creates user with correct state, raises `UserRegistered`
- [x] 2.3 Test `User.UpdateProfile`: validates non-empty name, validates IANA timezone, raises event
- [x] 2.4 Test `User.UpdatePreferences`: updates preferences, raises event
- [x] 2.5 Test `User.RecordLogin`: updates LastLoginAt

## 3. Infrastructure Layer

- [x] 3.1 Create `UserConfiguration` EF Core entity type configuration with owned types for Email and UserPreferences
- [x] 3.2 Add unique indexes on ExternalAuthId and Email
- [x] 3.3 Register User entity in `MentalMetalDbContext` and add `IUserScoped` global query filter
- [x] 3.4 Create `ICurrentUserService` implementation (`CurrentUserService`) reading UserId from JWT claims
- [x] 3.5 Create `UserRepository` implementing `IUserRepository`
- [x] 3.6 Create `RefreshToken` entity and EF Core configuration (token, userId, expiresAt, isRevoked)
- [x] 3.7 Create `ITokenService` and implementation for JWT generation and refresh token management
- [x] 3.8 Add EF Core migration for Users and RefreshTokens tables
- [x] 3.9 Register infrastructure services in DI (`DependencyInjection.cs`)

## 4. Application Layer

- [x] 4.1 Create `RegisterOrLoginUser` handler — looks up by ExternalAuthId, registers or records login, issues tokens
- [x] 4.2 Create `GetCurrentUser` query handler — returns user profile and preferences DTO
- [x] 4.3 Create `UpdateUserProfile` command handler — validates and updates name, avatar, timezone
- [x] 4.4 Create `UpdateUserPreferences` command handler — validates and updates preferences
- [x] 4.5 Create `RefreshAccessToken` handler — validates refresh token, rotates, issues new tokens
- [x] 4.6 Create `LogoutUser` handler — revokes refresh token
- [x] 4.7 Create DTOs: `UserProfileResponse`, `UpdateProfileRequest`, `UpdatePreferencesRequest`

## 5. Application Tests

- [x] 5.1 Test `RegisterOrLoginUser`: new user registration path, returning user login path, duplicate email rejection
- [x] 5.2 Test `RefreshAccessToken`: successful rotation, expired token rejection, reused token revokes all
- [x] 5.3 Test `UpdateUserProfile`: successful update, empty name rejection, invalid timezone rejection

## 6. Web Layer — Auth Endpoints

- [x] 6.1 Configure Google OAuth and JWT authentication middleware in `Program.cs`
- [x] 6.2 Map `GET /api/auth/login` — initiates Google OAuth challenge
- [x] 6.3 Map `GET /api/auth/callback` — handles OAuth callback, calls RegisterOrLoginUser, sets refresh token cookie, redirects with access token
- [x] 6.4 Map `POST /api/auth/refresh` — calls RefreshAccessToken, sets new refresh token cookie
- [x] 6.5 Map `POST /api/auth/logout` — calls LogoutUser, clears refresh token cookie

## 7. Web Layer — User Endpoints

- [x] 7.1 Map `GET /api/users/me` — returns current user profile and preferences
- [x] 7.2 Map `PUT /api/users/me/profile` — updates user profile
- [x] 7.3 Map `PUT /api/users/me/preferences` — updates user preferences

## 8. Frontend — Auth Service and Interceptor

- [x] 8.1 Create `AuthService` with signals for auth state (isAuthenticated, currentUser, accessToken)
- [x] 8.2 Create `authInterceptor` HTTP interceptor — attaches Bearer token, handles 401 with silent refresh
- [x] 8.3 Create `authGuard` functional route guard — redirects to login if not authenticated
- [x] 8.4 Add auth interceptor and guard to app config and route definitions
- [x] 8.5 Create login page component with Google login button

## 9. Frontend — User Settings Page

- [x] 9.1 Create `UserService` for profile and preferences API calls
- [x] 9.2 Create settings page component with profile form (name, timezone) using Signal Forms
- [x] 9.3 Add preferences section to settings page (theme toggle, notification settings, briefing time)
- [x] 9.4 Integrate theme toggle with existing `ThemeService`
- [x] 9.5 Add settings route to app routes

## 10. E2E Tests

- [x] 10.1 E2E test: Login flow via test-login endpoint — user lands on dashboard after login
- [x] 10.2 E2E test: Unauthenticated user redirected to login page
- [x] 10.3 E2E test: User settings page — update profile and preferences
