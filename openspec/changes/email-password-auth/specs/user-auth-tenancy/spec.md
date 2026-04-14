## ADDED Requirements

### Requirement: User registration via email and password

The system SHALL allow a new user to register by providing email, password, and name. The system SHALL hash the password using a PBKDF2-based hasher before persisting it. The system SHALL reject registration if the email already belongs to any existing User. On success, the system SHALL create a User aggregate with a null `ExternalAuthId`, raise a `UserRegistered` domain event, and issue a JWT access token and refresh token identical in shape to those issued by the Google OAuth flow.

Password MUST be at least 8 characters. Email MUST be a valid email address. Name MUST NOT be empty.

#### Scenario: Successful email/password registration

- **WHEN** an unauthenticated client posts to `/api/auth/register` with a valid email, password (≥ 8 chars), and non-empty name, and no User exists with that email
- **THEN** the system creates a User with hashed password and null ExternalAuthId, sets CreatedAt and LastLoginAt to now, applies default UserPreferences, raises `UserRegistered`, and returns 200 with an access token in the body and a refresh token cookie

#### Scenario: Registration with existing email rejected

- **WHEN** a client posts to `/api/auth/register` with an email that already belongs to any User (whether that User has a password, an ExternalAuthId, or both)
- **THEN** the system returns HTTP 409 Conflict and does not create a new User

#### Scenario: Registration with short password rejected

- **WHEN** a client posts to `/api/auth/register` with a password shorter than 8 characters
- **THEN** the system returns HTTP 400 and does not create a User

#### Scenario: Registration with invalid email rejected

- **WHEN** a client posts to `/api/auth/register` with a malformed email
- **THEN** the system returns HTTP 400 and does not create a User

### Requirement: User login via email and password

The system SHALL authenticate a user by email and password and issue a JWT access token and refresh token when the credentials are valid. The system SHALL update `LastLoginAt` on successful login. The system SHALL return HTTP 401 for any failure — unknown email, wrong password, or a user with no password set (i.e., a Google-only user) — without disclosing which case applies.

#### Scenario: Successful email/password login

- **WHEN** an unauthenticated client posts to `/api/auth/login` with an email and password that match an existing User with a password set
- **THEN** the system updates LastLoginAt, issues a new access token and refresh token, and returns 200 with the access token in the body and the refresh token cookie

#### Scenario: Wrong password

- **WHEN** a client posts to `/api/auth/login` with an email that exists but the wrong password
- **THEN** the system returns HTTP 401 and does not issue tokens

#### Scenario: Unknown email

- **WHEN** a client posts to `/api/auth/login` with an email that does not belong to any User
- **THEN** the system returns HTTP 401 and does not issue tokens

#### Scenario: User has no password set

- **WHEN** a client posts to `/api/auth/login` with the email of a User whose `PasswordHash` is null (Google-only account)
- **THEN** the system returns HTTP 401 without disclosing the reason

### Requirement: Set or change password on current user

The system SHALL allow an authenticated user to set a password on their account (first time) or replace an existing one. This is the mechanism by which a user registered via Google OAuth attaches a password to their account. The endpoint SHALL require a valid JWT. The password MUST meet the same minimum-length rule as registration.

#### Scenario: Google-only user sets their first password

- **WHEN** an authenticated User whose `PasswordHash` is null posts a new password (≥ 8 chars) to `/api/auth/password`
- **THEN** the system stores the hashed password on the User aggregate and returns HTTP 204

#### Scenario: User with existing password replaces it

- **WHEN** an authenticated User whose `PasswordHash` is set posts a new password to `/api/auth/password`
- **THEN** the system replaces the stored hash with the new one and returns HTTP 204

#### Scenario: Short password rejected

- **WHEN** an authenticated user posts a password shorter than 8 characters to `/api/auth/password`
- **THEN** the system returns HTTP 400 and does not update the User

#### Scenario: Unauthenticated request rejected

- **WHEN** an unauthenticated client posts to `/api/auth/password`
- **THEN** the system returns HTTP 401

### Requirement: User aggregate supports optional password credential

The `User` aggregate SHALL include an optional `Password` value object (`PasswordHash`) and an optional `ExternalAuthId`. A User MUST have at least one of the two. A User MAY have both (meaning the user can sign in via either method). The `Password` value object SHALL encapsulate a password hash and expose a verification operation.

#### Scenario: User registered via password has null ExternalAuthId

- **WHEN** a User is created via email/password registration
- **THEN** the User's `ExternalAuthId` is null and `PasswordHash` is set

#### Scenario: User registered via Google has null PasswordHash

- **WHEN** a User is created via Google OAuth registration (existing flow)
- **THEN** the User's `ExternalAuthId` is set and `PasswordHash` is null

#### Scenario: Linked user has both credentials

- **WHEN** a Google-registered User invokes the domain operation to set a password
- **THEN** the User has both `ExternalAuthId` and `PasswordHash` set, and can log in via either method

### Requirement: Frontend email and password login form

The Angular login page SHALL present an email/password form alongside the existing Google sign-in button. On submission the form SHALL call the appropriate backend endpoint (`/api/auth/login` or `/api/auth/register`), store the returned access token via the existing auth service, and redirect into the application.

#### Scenario: User logs in with email and password

- **WHEN** a user enters a valid email and password on the login page and submits
- **THEN** the app calls `/api/auth/login`, stores the returned access token, and navigates to the post-login route

#### Scenario: User registers from the login page

- **WHEN** a user switches to the register form, enters a valid email, name, and password, and submits
- **THEN** the app calls `/api/auth/register`, stores the returned access token, and navigates to the post-login route

#### Scenario: Failed login shows error

- **WHEN** a password login attempt returns HTTP 401
- **THEN** the login page shows a generic "invalid credentials" message and does not navigate away

#### Scenario: Duplicate email on register shows error

- **WHEN** a registration attempt returns HTTP 409
- **THEN** the register form shows a message indicating the email is already in use

### Requirement: Frontend password section in settings

The Angular settings page SHALL include a password section that allows the authenticated user to set a first password or replace an existing one, driven by the `hasPassword` field on the current-user response.

#### Scenario: Google-only user sees "Set a password"

- **WHEN** an authenticated user with `hasPassword=false` views the settings page
- **THEN** the password section shows a "Set a password" heading and a single new-password input

#### Scenario: User with password set sees "Change password"

- **WHEN** an authenticated user with `hasPassword=true` views the settings page
- **THEN** the password section shows a "Change password" heading and a new-password input

#### Scenario: Saving a password calls the endpoint

- **WHEN** the user enters a valid new password in settings and submits
- **THEN** the app calls `POST /api/auth/password`, shows a success confirmation on 204, and updates local state so that `hasPassword` is reflected as true

## MODIFIED Requirements

### Requirement: Get current user profile

The system SHALL provide an endpoint to retrieve the authenticated user's profile and preferences.

#### Scenario: Get current user

- **WHEN** an authenticated user requests their profile via `/api/users/me`
- **THEN** the system returns the user's Id, Email, Name, AvatarUrl, Timezone, Preferences, HasAiProvider, HasPassword, CreatedAt, and LastLoginAt

#### Scenario: HasPassword reflects password state

- **WHEN** an authenticated user with a non-null `PasswordHash` requests their profile
- **THEN** the response includes `hasPassword=true`; and when the `PasswordHash` is null the response includes `hasPassword=false`

### Requirement: User registration via OAuth

The system SHALL create a new User aggregate when a user authenticates via Google OAuth for the first time. The User SHALL be populated with ExternalAuthId, Email, Name, and AvatarUrl from the Google identity token, and its `PasswordHash` SHALL be null. The system SHALL raise a `UserRegistered` domain event upon successful registration. ExternalAuthId and Email SHALL have unique database indexes to enforce identity uniqueness at the persistence layer; the ExternalAuthId index SHALL treat null values as non-conflicting so that password-only users do not collide.

#### Scenario: First-time Google OAuth login

- **WHEN** a user completes Google OAuth authentication and no User exists with that ExternalAuthId
- **THEN** the system creates a new User aggregate with the Google profile data, sets CreatedAt and LastLoginAt to the current time, applies default UserPreferences, leaves `PasswordHash` null, and returns a JWT access token and refresh token

#### Scenario: Existing ExternalAuthId treated as returning login

- **WHEN** a registration attempt is made with an ExternalAuthId that already exists
- **THEN** the system treats it as a returning user login (RecordLogin) rather than creating a duplicate

#### Scenario: Duplicate email rejected

- **WHEN** a registration attempt is made with an email that already belongs to a different User
- **THEN** the system rejects the registration and returns an error indicating the email is already in use
