## ADDED Requirements

### Requirement: User aggregate supports AI provider configuration

The User aggregate SHALL include an optional `AiProviderConfig` property (value object). When no AI provider is configured, the property SHALL be null. The User SHALL expose `ConfigureAiProvider` and `RemoveAiProvider` business actions.

#### Scenario: New user has no AI provider configured
- **WHEN** a new user is registered
- **THEN** the user's AiProviderConfig is null

#### Scenario: User configures AI provider
- **WHEN** ConfigureAiProvider is called with valid provider, encrypted key, and model
- **THEN** the user's AiProviderConfig is set and an `AiProviderConfigured` event is raised

#### Scenario: User removes AI provider
- **WHEN** RemoveAiProvider is called
- **THEN** the user's AiProviderConfig is set to null and an `AiProviderRemoved` event is raised

### Requirement: User profile response includes AI provider status

The `GET /api/users/me` response SHALL include a `hasAiProvider` boolean field indicating whether the user has configured an AI provider. The response SHALL NOT include any AI provider details (provider name, model, key).

#### Scenario: User with AI provider configured
- **WHEN** an authenticated user with a configured AI provider requests their profile
- **THEN** the response includes hasAiProvider=true

#### Scenario: User without AI provider configured
- **WHEN** an authenticated user without a configured AI provider requests their profile
- **THEN** the response includes hasAiProvider=false
