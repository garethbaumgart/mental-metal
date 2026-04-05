## ADDED Requirements

### Requirement: User registration via OAuth
The system SHALL create a new User aggregate when a user authenticates via Google OAuth for the first time. The User SHALL be populated with ExternalAuthId, Email, Name, and AvatarUrl from the Google identity token. The system SHALL raise a `UserRegistered` domain event upon successful registration.

#### Scenario: First-time Google OAuth login
- **WHEN** a user completes Google OAuth authentication and no User exists with that ExternalAuthId
- **THEN** the system creates a new User aggregate with the Google profile data, sets CreatedAt and LastLoginAt to the current time, applies default UserPreferences, and returns a JWT access token and refresh token

#### Scenario: Duplicate ExternalAuthId rejected
- **WHEN** a registration attempt is made with an ExternalAuthId that already exists
- **THEN** the system treats it as a returning user login (RecordLogin) rather than creating a duplicate

#### Scenario: Duplicate email rejected
- **WHEN** a registration attempt is made with an email that already belongs to a different User
- **THEN** the system rejects the registration and returns an error indicating the email is already in use

### Requirement: Returning user login
The system SHALL recognise returning users by their ExternalAuthId and update their LastLoginAt timestamp. The system SHALL issue new JWT access and refresh tokens on each login.

#### Scenario: Returning user logs in
- **WHEN** a user completes Google OAuth authentication and a User exists with that ExternalAuthId
- **THEN** the system updates LastLoginAt, issues new tokens, and returns the authenticated session

#### Scenario: Profile data sync on login
- **WHEN** a returning user logs in and their Google profile data (name, avatar) has changed
- **THEN** the system does NOT automatically overwrite the User's name or avatar (user-managed after registration)

### Requirement: JWT access token authentication
The system SHALL authenticate API requests using JWT access tokens sent in the `Authorization: Bearer` header. Access tokens SHALL have a 15-minute expiry and contain the UserId claim.

#### Scenario: Valid access token
- **WHEN** an API request includes a valid, non-expired JWT access token
- **THEN** the system authenticates the request and makes the UserId available to handlers via `ICurrentUserService`

#### Scenario: Expired access token
- **WHEN** an API request includes an expired JWT access token
- **THEN** the system returns HTTP 401 Unauthorized

#### Scenario: Missing access token on protected endpoint
- **WHEN** an API request to a protected endpoint does not include an access token
- **THEN** the system returns HTTP 401 Unauthorized

### Requirement: Refresh token rotation
The system SHALL support refresh tokens stored in HTTP-only secure cookies with a 7-day expiry. Each refresh token use SHALL invalidate the old token and issue a new refresh token alongside a new access token.

#### Scenario: Successful token refresh
- **WHEN** a client sends a valid refresh token to the refresh endpoint
- **THEN** the system issues a new access token and a new refresh token, invalidating the old refresh token

#### Scenario: Expired refresh token
- **WHEN** a client sends an expired refresh token
- **THEN** the system returns HTTP 401 and the user must re-authenticate via OAuth

#### Scenario: Reused (already rotated) refresh token
- **WHEN** a client sends a refresh token that has already been rotated (used and invalidated)
- **THEN** the system invalidates all refresh tokens for that user (potential token theft) and returns HTTP 401

### Requirement: User logout
The system SHALL invalidate the user's refresh token on logout and clear the refresh token cookie.

#### Scenario: Successful logout
- **WHEN** an authenticated user requests logout
- **THEN** the system invalidates their current refresh token, clears the refresh token cookie, and returns HTTP 200

### Requirement: Multi-tenant data isolation
The system SHALL automatically scope all database queries to the authenticated user's UserId. All entities implementing `IUserScoped` SHALL have a global query filter applied by EF Core.

#### Scenario: Authenticated query returns only user's data
- **WHEN** an authenticated user queries any entity that implements `IUserScoped`
- **THEN** the query results contain only records where UserId matches the authenticated user

#### Scenario: Unauthenticated access to scoped data
- **WHEN** an unauthenticated request attempts to access user-scoped data
- **THEN** the system returns HTTP 401 before any data query is executed

#### Scenario: Cross-tenant data inaccessible
- **WHEN** User A is authenticated and queries data
- **THEN** no records belonging to User B are returned, even if User A knows the record IDs

### Requirement: User profile management
The system SHALL allow authenticated users to update their profile information: name, avatar URL, and timezone. Name MUST NOT be empty. Timezone MUST be a valid IANA timezone string.

#### Scenario: Update profile successfully
- **WHEN** an authenticated user submits a profile update with a valid name and IANA timezone
- **THEN** the system updates the User aggregate, raises `UserProfileUpdated`, and returns the updated profile

#### Scenario: Empty name rejected
- **WHEN** a user submits a profile update with an empty or whitespace-only name
- **THEN** the system returns a validation error and does not update the profile

#### Scenario: Invalid timezone rejected
- **WHEN** a user submits a profile update with an invalid IANA timezone string
- **THEN** the system returns a validation error and does not update the profile

### Requirement: User preferences management
The system SHALL allow authenticated users to update their preferences including theme (light/dark), notification settings, and briefing schedule time. The system SHALL raise `PreferencesUpdated` on change.

#### Scenario: Update preferences successfully
- **WHEN** an authenticated user submits valid preference changes
- **THEN** the system updates the UserPreferences value object on the User aggregate and raises `PreferencesUpdated`

#### Scenario: Get current preferences
- **WHEN** an authenticated user requests their preferences
- **THEN** the system returns the current UserPreferences including theme, notification settings, and briefing schedule

### Requirement: Get current user profile
The system SHALL provide an endpoint to retrieve the authenticated user's profile and preferences.

#### Scenario: Get current user
- **WHEN** an authenticated user requests their profile via `/api/users/me`
- **THEN** the system returns the user's Id, Email, Name, AvatarUrl, Timezone, Preferences, CreatedAt, and LastLoginAt

### Requirement: Frontend authentication flow
The Angular application SHALL manage authentication state using signals, redirect unauthenticated users to the login page, and automatically attach access tokens to API requests.

#### Scenario: Unauthenticated user visits protected route
- **WHEN** an unauthenticated user navigates to a protected route
- **THEN** the application redirects to the login page

#### Scenario: Login button initiates OAuth
- **WHEN** a user clicks the login button
- **THEN** the application redirects to the backend OAuth endpoint which initiates the Google OAuth flow

#### Scenario: Automatic token refresh
- **WHEN** an API request receives a 401 response and a valid refresh token exists
- **THEN** the auth interceptor automatically attempts a token refresh and retries the original request

#### Scenario: Refresh failure redirects to login
- **WHEN** a token refresh attempt fails
- **THEN** the application clears auth state and redirects to the login page

### Requirement: Frontend user settings page
The Angular application SHALL provide a settings page where users can view and edit their profile and preferences.

#### Scenario: View settings page
- **WHEN** an authenticated user navigates to the settings page
- **THEN** the page displays their current profile (name, email, avatar, timezone) and preferences (theme, notifications, briefing time)

#### Scenario: Save profile changes
- **WHEN** a user edits their profile fields and clicks save
- **THEN** the application sends the update to the API and reflects the updated values on success

#### Scenario: Save preference changes
- **WHEN** a user changes preferences (e.g., theme toggle) and saves
- **THEN** the application sends the update to the API, applies the preference change immediately (e.g., theme switches), and shows a success confirmation
