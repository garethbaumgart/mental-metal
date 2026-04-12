## ADDED Requirements

### Requirement: AiProviderConfig value object

The system SHALL define an `AiProviderConfig` value object with: `Provider` (enum: Anthropic, OpenAI, Google), `EncryptedApiKey` (string), `Model` (string), and `MaxTokens` (int?, optional). The value object SHALL be embedded on the User aggregate.

#### Scenario: AiProviderConfig creation with valid inputs
- **WHEN** AiProviderConfig is created with Provider=Anthropic, EncryptedApiKey="enc_xxx", Model="claude-sonnet-4-20250514", MaxTokens=4096
- **THEN** the value object is created with all properties set

#### Scenario: AiProviderConfig equality
- **WHEN** two AiProviderConfig instances have identical Provider, EncryptedApiKey, Model, and MaxTokens
- **THEN** they are considered equal

### Requirement: User can configure AI provider

The User aggregate SHALL expose a `ConfigureAiProvider(provider, encryptedApiKey, model, maxTokens?)` action that sets the `AiProviderConfig` property and raises an `AiProviderConfigured` domain event.

#### Scenario: First-time AI provider configuration
- **WHEN** a user with no AI provider config calls ConfigureAiProvider with Provider=Anthropic, a valid encrypted key, and Model="claude-sonnet-4-20250514"
- **THEN** the user's AiProviderConfig is set with the given values
- **AND** an `AiProviderConfigured` event is raised with UserId and Provider

#### Scenario: Changing AI provider
- **WHEN** a user with an existing Anthropic config calls ConfigureAiProvider with Provider=OpenAI and a new key
- **THEN** the user's AiProviderConfig is replaced with the new config
- **AND** an `AiProviderConfigured` event is raised

#### Scenario: Invalid provider rejected
- **WHEN** ConfigureAiProvider is called with an empty API key
- **THEN** an ArgumentException is thrown

#### Scenario: Invalid model rejected
- **WHEN** ConfigureAiProvider is called with an empty model string
- **THEN** an ArgumentException is thrown

### Requirement: User can remove AI provider configuration

The User aggregate SHALL expose a `RemoveAiProvider()` action that clears `AiProviderConfig` to null and raises an `AiProviderRemoved` domain event. The action SHALL be idempotent — removing when already null is a no-op (no event raised).

#### Scenario: Successful removal
- **WHEN** a user with a configured provider calls RemoveAiProvider
- **THEN** AiProviderConfig is set to null
- **AND** an `AiProviderRemoved` event is raised

#### Scenario: Remove when not configured
- **WHEN** a user without a configured provider calls RemoveAiProvider
- **THEN** AiProviderConfig remains null
- **AND** no domain event is raised

### Requirement: API key encryption at rest

The system SHALL encrypt API keys using AES-256-GCM before storing them in the database. The encryption key SHALL be configurable via application settings (`AiProvider:EncryptionKey`). The stored format SHALL be Base64-encoded `nonce:ciphertext:tag`.

#### Scenario: API key is encrypted before storage
- **WHEN** a user configures an AI provider with a plaintext API key
- **THEN** the key is encrypted via `IApiKeyEncryptionService.Encrypt()` before being passed to the domain
- **AND** the database column contains only the encrypted value

#### Scenario: API key is decrypted for provider calls
- **WHEN** the system needs to make an AI completion call
- **THEN** the encrypted key is decrypted via `IApiKeyEncryptionService.Decrypt()` to obtain the plaintext key
- **AND** the plaintext key is used for the provider API call and is never persisted

#### Scenario: Missing encryption key at startup
- **WHEN** the application starts without `AiProvider:EncryptionKey` configured
- **THEN** the application SHALL fail fast with a clear configuration error

### Requirement: Provider abstraction interface

The system SHALL define an `IAiCompletionService` interface in the Application layer with a single `CompleteAsync(AiCompletionRequest, CancellationToken)` method. The request SHALL include: system prompt (string), user prompt (string), max tokens (int?, optional override), and temperature (float?, optional). The result SHALL include: content (string), input tokens used (int), output tokens used (int), model used (string), and provider used (AiProvider enum).

#### Scenario: Successful completion via user's own key
- **WHEN** a user has configured their own AI provider
- **AND** CompleteAsync is called with a system prompt and user prompt
- **THEN** the system uses the user's decrypted key and configured model
- **AND** returns an AiCompletionResult with the response content, token usage, and actual model used

#### Scenario: Successful completion via taste key
- **WHEN** a user has no configured AI provider and has remaining taste budget
- **AND** CompleteAsync is called
- **THEN** the system uses the app-managed taste key with the taste provider's default model
- **AND** decrements the user's daily taste budget
- **AND** returns an AiCompletionResult

#### Scenario: Taste budget exhausted
- **WHEN** a user has no configured AI provider and has exhausted their daily taste budget
- **AND** CompleteAsync is called
- **THEN** a `TasteLimitExceededException` is thrown

#### Scenario: User has own key — no taste budget consumed
- **WHEN** a user has their own AI provider configured
- **AND** CompleteAsync is called
- **THEN** the taste budget is NOT decremented (own key = unlimited)

### Requirement: Model fallback chain

The system SHALL attempt the configured default model first. If the provider API returns a model-not-found error, the system SHALL try each model in the provider's fallback list in order. Only model-not-found errors SHALL trigger fallback — auth errors, rate limit errors, and server errors SHALL bubble up immediately.

#### Scenario: Default model succeeds
- **WHEN** a completion request is made with the default model "claude-sonnet-4-20250514"
- **AND** the provider API accepts the model
- **THEN** the response is returned with model="claude-sonnet-4-20250514"

#### Scenario: Default model deprecated, fallback succeeds
- **WHEN** a completion request is made with default model "claude-sonnet-4-20250514"
- **AND** the provider API returns a model-not-found error
- **THEN** the system tries the first fallback model "claude-sonnet-4-6"
- **AND** if that succeeds, returns the response with model="claude-sonnet-4-6"

#### Scenario: All models fail
- **WHEN** all models in the chain (default + all fallbacks) return model-not-found
- **THEN** an `AiProviderException` is thrown indicating no available models

#### Scenario: Auth error does not trigger fallback
- **WHEN** a completion request fails with an authentication error
- **THEN** the error bubbles up immediately without trying fallback models

### Requirement: Provider adapter implementations

The system SHALL implement provider-specific adapters for Anthropic (using Anthropic .NET SDK), OpenAI (using OpenAI .NET SDK), and Google (using HttpClient with Gemini REST API). Adapters SHALL be stateless and receive the decrypted API key per request.

#### Scenario: Anthropic adapter maps request correctly
- **WHEN** the Anthropic adapter receives a completion request
- **THEN** it maps system prompt to the Anthropic `system` parameter
- **AND** maps user prompt to a user message
- **AND** sends the request using the Anthropic SDK

#### Scenario: OpenAI adapter maps request correctly
- **WHEN** the OpenAI adapter receives a completion request
- **THEN** it maps system prompt to a system message and user prompt to a user message
- **AND** sends the request using the OpenAI SDK

#### Scenario: Google adapter maps request correctly
- **WHEN** the Google adapter receives a completion request
- **THEN** it maps the prompts to the Gemini API format
- **AND** sends the request via HttpClient to the Gemini REST endpoint

#### Scenario: Provider API returns an error
- **WHEN** a provider API call fails (auth error, rate limit, server error)
- **THEN** the adapter throws an `AiProviderException` with the provider name, HTTP status, and error message

### Requirement: Config-driven model catalog

The system SHALL maintain model configuration in `appsettings.json` with a default model and ordered fallback list per provider. The system SHALL expose an endpoint to retrieve the available models. Model configuration SHALL be updatable without code changes.

#### Scenario: Retrieve models for a provider
- **WHEN** a client requests available models for Anthropic
- **THEN** the system returns the default model and fallback models from configuration

#### Scenario: Invalid provider
- **WHEN** a client requests models for an unrecognized provider
- **THEN** the system returns 400 Bad Request

### Requirement: Taste key system

The system SHALL provide an app-managed AI key (the "taste key") configured in `appsettings.json`. New users without their own AI provider SHALL automatically use the taste key. Each user SHALL have a daily budget of AI operations (default: 5, configurable). The budget resets daily and never expires (a user who signed up months ago still gets taste operations).

#### Scenario: New user uses taste key
- **WHEN** a user with no AI provider config triggers an AI operation
- **THEN** the system uses the taste key
- **AND** the user's daily operation counter is incremented

#### Scenario: Taste budget exhausted
- **WHEN** a user has used all 5 daily taste operations
- **AND** they trigger another AI operation
- **THEN** the system throws `TasteLimitExceededException`
- **AND** the frontend displays the Limited nudge

#### Scenario: Taste budget resets daily
- **WHEN** a new UTC day begins
- **THEN** all users' taste budgets reset to 0 operations used

#### Scenario: User with own key bypasses taste system
- **WHEN** a user has their own AI provider configured
- **THEN** the taste key and budget are not involved in any AI operations

#### Scenario: Taste key not configured
- **WHEN** the application starts without `AiProvider:TasteKey` configured
- **THEN** the taste system is disabled (users without own key get an error prompting setup)
- **AND** the application SHALL NOT fail to start

### Requirement: AI setup nudge framework

The system SHALL guide users through four states based on their AI provider configuration and taste budget. The nudge type determines the UI treatment when AI features are encountered.

**States:**
- **Fresh** — No AI operations used, no provider configured. Passive indicators only.
- **Tasting** — Using taste key, under daily limit. Contextual suggestions after AI operations.
- **Limited** — Daily taste budget exhausted, no own key. Blocking — cannot proceed.
- **Unlimited** — Own key configured. No nudges.

#### Scenario: Fresh user sees passive indicator
- **WHEN** a user with no AI provider and no taste usage navigates the app
- **THEN** AI-powered sections show empty states with a subtle link to AI setup
- **AND** the settings page shows "AI Provider: Not configured"

#### Scenario: Tasting user sees contextual nudge
- **WHEN** a user without their own key completes an AI operation via the taste key
- **THEN** a non-blocking contextual message appears: "Add your own AI key for unlimited access"
- **AND** the message is dismissible for 7 days per nudge type

#### Scenario: Limited user sees blocking prompt
- **WHEN** a user without their own key triggers an AI operation with 0 remaining taste budget
- **THEN** a blocking prompt appears explaining the daily limit is reached
- **AND** offers "Set Up AI Provider" and "Continue tomorrow" actions
- **AND** the "Continue tomorrow" action dismisses for this action only (next AI trigger shows it again)

#### Scenario: Unlimited user sees no nudges
- **WHEN** a user with their own AI provider configured uses any AI feature
- **THEN** no AI setup nudges are shown

### Requirement: Taste counter display

The system SHALL display a subtle counter showing remaining taste operations in AI-powered sections of the app. The counter SHALL only be visible to users without their own AI provider configured.

#### Scenario: Counter visible for taste users
- **WHEN** a user without their own key is in an AI-powered section
- **THEN** a subtle indicator shows "3 of 5 free AI operations remaining today"

#### Scenario: Counter hidden for configured users
- **WHEN** a user with their own AI provider is in an AI-powered section
- **THEN** no taste counter is displayed

#### Scenario: Counter updates after AI operation
- **WHEN** a taste user completes an AI operation
- **THEN** the counter updates to reflect the new remaining count

### Requirement: API key validation endpoint

The system SHALL expose a `POST /api/users/me/ai-provider/validate` endpoint that sends a minimal completion request to verify the API key and model work. The endpoint SHALL accept provider, API key, and model as input (not requiring the config to be saved first).

#### Scenario: Valid API key
- **WHEN** a user submits a valid provider, API key, and model for validation
- **THEN** the system sends a lightweight completion request to the provider
- **AND** returns a success response with the model name confirmed

#### Scenario: Invalid API key
- **WHEN** a user submits an invalid API key for validation
- **THEN** the provider returns an authentication error
- **AND** the system returns a failure response indicating the key is invalid

#### Scenario: Invalid model
- **WHEN** a user submits a valid key but an unsupported model
- **THEN** the provider returns a model-not-found error
- **AND** the system returns a failure response indicating the model is not available

### Requirement: Configure AI provider API endpoint

The system SHALL expose `PUT /api/users/me/ai-provider` to configure the user's AI provider. The request body SHALL include provider (string), apiKey (plaintext string), model (string), and maxTokens (int?, optional). The endpoint SHALL encrypt the API key before passing to the domain.

#### Scenario: Successful configuration
- **WHEN** an authenticated user sends a PUT with provider="Anthropic", apiKey="sk-ant-...", model="claude-sonnet-4-20250514"
- **THEN** the system encrypts the API key, calls User.ConfigureAiProvider, persists changes, and returns 204 No Content

#### Scenario: Unauthenticated request
- **WHEN** a request is sent without a valid JWT
- **THEN** the system returns 401 Unauthorized

### Requirement: Get AI provider status endpoint

The system SHALL expose `GET /api/users/me/ai-provider` to return the user's current AI provider configuration (provider, model, maxTokens, isConfigured) and taste budget status (remaining operations, daily limit). The response SHALL NOT include the API key (encrypted or plaintext).

#### Scenario: User has provider configured
- **WHEN** an authenticated user with a configured provider sends a GET
- **THEN** the system returns the provider name, model, maxTokens, isConfigured=true, and taste information

#### Scenario: User has no provider configured
- **WHEN** an authenticated user without a provider config sends a GET
- **THEN** the system returns isConfigured=false with null provider/model, and remaining taste budget

### Requirement: Get available models endpoint

The system SHALL expose `GET /api/ai/models?provider={provider}` to return the configured models for a given provider, including which is the default.

#### Scenario: Request models for a valid provider
- **WHEN** a client requests models with provider=Anthropic
- **THEN** the system returns the model list from configuration with the default flagged

#### Scenario: Request models for an invalid provider
- **WHEN** a client requests models with an unrecognized provider
- **THEN** the system returns 400 Bad Request

### Requirement: Delete AI provider configuration

The system SHALL expose `DELETE /api/users/me/ai-provider` to remove the user's AI provider configuration.

#### Scenario: Successful removal
- **WHEN** an authenticated user with a configured provider sends a DELETE
- **THEN** the system calls User.RemoveAiProvider, persists changes, and returns 204 No Content

#### Scenario: Remove when not configured
- **WHEN** an authenticated user without a configured provider sends a DELETE
- **THEN** the system returns 204 No Content (idempotent)

### Requirement: Frontend AI provider settings

The settings page SHALL include an AI Provider section with: provider cards (Anthropic, OpenAI, Google), an API key input field (password-masked), a "Don't have one?" deep link to the provider's key creation page, a model dropdown (populated from config with default flagged), an optional max tokens input, a "Test Connection" button (auto-validates on key paste), and a "Save" button.

#### Scenario: User configures provider for the first time
- **WHEN** a user navigates to settings and selects Anthropic
- **THEN** a deep link to `https://console.anthropic.com/settings/keys` appears as "Don't have one? → Open Anthropic Console"
- **AND** the model dropdown shows configured Anthropic models with the default pre-selected

#### Scenario: Auto-validate on key paste
- **WHEN** a user pastes an API key into the input field
- **THEN** the system automatically calls the validate endpoint
- **AND** shows a green check with "Connected to Claude Sonnet 4" on success, or an error message on failure

#### Scenario: User saves configuration
- **WHEN** a user clicks "Save" with valid provider, API key, and model
- **THEN** the frontend calls PUT /api/users/me/ai-provider
- **AND** displays a success confirmation

#### Scenario: Existing configuration is shown
- **WHEN** a user with an existing config navigates to settings
- **THEN** the provider and model are pre-selected
- **AND** the API key field shows a placeholder (not the actual key)

#### Scenario: Deep links per provider
- **WHEN** user selects Anthropic, the "Don't have one?" link opens `https://console.anthropic.com/settings/keys`
- **WHEN** user selects OpenAI, the link opens `https://platform.openai.com/api-keys`
- **WHEN** user selects Google, the link opens `https://aistudio.google.com/apikey`
