# Transcription Provider Abstraction

## Requirements

### Requirement: TranscriptionProviderConfig value object

The system SHALL define a `TranscriptionProviderConfig` value object with: `Provider` (enum: Deepgram), `EncryptedApiKey` (string), and `Model` (string). The value object SHALL be embedded on the `User` aggregate. `EncryptedApiKey` MUST NOT be empty. `Model` MUST NOT be empty.

#### Scenario: TranscriptionProviderConfig creation with valid inputs

- **WHEN** TranscriptionProviderConfig is created with Provider=Deepgram, EncryptedApiKey="enc_xxx", Model="nova-3"
- **THEN** the value object is created with all properties set

#### Scenario: TranscriptionProviderConfig equality

- **WHEN** two TranscriptionProviderConfig instances have identical Provider, EncryptedApiKey, and Model
- **THEN** they are considered equal

#### Scenario: Empty API key rejected

- **WHEN** TranscriptionProviderConfig is created with an empty EncryptedApiKey
- **THEN** an ArgumentException is thrown

#### Scenario: Empty model rejected

- **WHEN** TranscriptionProviderConfig is created with an empty Model
- **THEN** an ArgumentException is thrown

### Requirement: User can configure transcription provider

The `User` aggregate SHALL expose a `ConfigureTranscriptionProvider(provider, encryptedApiKey, model)` action that sets the `TranscriptionProviderConfig` property and raises a `TranscriptionProviderConfigured` domain event with `UserId` and `Provider`.

#### Scenario: First-time transcription provider configuration

- **WHEN** a user with no transcription provider config calls ConfigureTranscriptionProvider with Provider=Deepgram, a valid encrypted key, and Model="nova-3"
- **THEN** the user's TranscriptionProviderConfig is set with the given values
- **AND** a `TranscriptionProviderConfigured` event is raised with UserId and Provider

#### Scenario: Changing transcription provider config

- **WHEN** a user with an existing Deepgram config calls ConfigureTranscriptionProvider with a new key
- **THEN** the user's TranscriptionProviderConfig is replaced with the new config
- **AND** a `TranscriptionProviderConfigured` event is raised

#### Scenario: Empty API key rejected

- **WHEN** ConfigureTranscriptionProvider is called with an empty API key
- **THEN** an ArgumentException is thrown

#### Scenario: Invalid model rejected

- **WHEN** ConfigureTranscriptionProvider is called with an empty model string
- **THEN** an ArgumentException is thrown

### Requirement: User can remove transcription provider configuration

The `User` aggregate SHALL expose a `RemoveTranscriptionProvider()` action that clears `TranscriptionProviderConfig` to null and raises a `TranscriptionProviderRemoved` domain event. The action SHALL be idempotent — removing when already null is a no-op (no event raised).

#### Scenario: Successful removal

- **WHEN** a user with a configured transcription provider calls RemoveTranscriptionProvider
- **THEN** TranscriptionProviderConfig is set to null
- **AND** a `TranscriptionProviderRemoved` event is raised

#### Scenario: Remove when not configured

- **WHEN** a user without a configured transcription provider calls RemoveTranscriptionProvider
- **THEN** TranscriptionProviderConfig remains null
- **AND** no domain event is raised

### Requirement: Transcription API key encryption at rest

The system SHALL encrypt transcription provider API keys using the same `IApiKeyEncryptionService` (AES-256-GCM) used for AI provider keys. The stored format SHALL be `base64(nonce):base64(ciphertext):base64(tag)`.

#### Scenario: Transcription API key is encrypted before storage

- **WHEN** a user configures a transcription provider with a plaintext API key
- **THEN** the key is encrypted via `IApiKeyEncryptionService.Encrypt()` before being passed to the domain
- **AND** the database column contains only the encrypted value

#### Scenario: Transcription API key is decrypted for provider calls

- **WHEN** the system needs to make a transcription call
- **THEN** the encrypted key is decrypted via `IApiKeyEncryptionService.Decrypt()` to obtain the plaintext key
- **AND** the plaintext key is used for the Deepgram API call and is never persisted

### Requirement: Configure transcription provider API endpoint

The system SHALL expose `PUT /api/users/me/transcription-provider` to configure the user's transcription provider. The request body SHALL include `provider` (string), `apiKey` (plaintext string), and `model` (string). The endpoint SHALL encrypt the API key before passing to the domain.

#### Scenario: Successful configuration

- **WHEN** an authenticated user sends a PUT with provider="Deepgram", apiKey="dg-xxx", model="nova-3"
- **THEN** the system encrypts the API key, calls User.ConfigureTranscriptionProvider, persists changes, and returns 204 No Content

#### Scenario: Unsupported provider rejected

- **WHEN** an authenticated user sends a PUT with provider="UnsupportedProvider"
- **THEN** the system returns HTTP 400 with a validation error

#### Scenario: Unauthenticated request

- **WHEN** a request is sent without a valid JWT
- **THEN** the system returns 401 Unauthorized

### Requirement: Get transcription provider status endpoint

The system SHALL expose `GET /api/users/me/transcription-provider` to return the user's current transcription provider configuration (`provider`, `model`, `isConfigured`). The response SHALL NOT include the API key (encrypted or plaintext).

#### Scenario: User has transcription provider configured

- **WHEN** an authenticated user with a configured transcription provider sends a GET
- **THEN** the system returns the provider name, model, and isConfigured=true

#### Scenario: User has no transcription provider configured

- **WHEN** an authenticated user without a transcription provider config sends a GET
- **THEN** the system returns isConfigured=false with null provider and model

### Requirement: Delete transcription provider configuration

The system SHALL expose `DELETE /api/users/me/transcription-provider` to remove the user's transcription provider configuration.

#### Scenario: Successful removal

- **WHEN** an authenticated user with a configured transcription provider sends a DELETE
- **THEN** the system calls User.RemoveTranscriptionProvider, persists changes, and returns 204 No Content

#### Scenario: Remove when not configured

- **WHEN** an authenticated user without a configured transcription provider sends a DELETE
- **THEN** the system returns 204 No Content (idempotent)

### Requirement: Transcription provider key validation endpoint

The system SHALL expose `POST /api/users/me/transcription-provider/validate` to test a Deepgram API key without saving it. The endpoint SHALL make a lightweight request to the Deepgram API (e.g., `GET /v1/projects`) to verify the key is valid. The endpoint SHALL require authentication and SHALL ensure API keys are never logged.

#### Scenario: Valid API key

- **WHEN** a user submits a valid Deepgram API key for validation
- **THEN** the system sends a test request to Deepgram and returns a success response

#### Scenario: Invalid API key

- **WHEN** a user submits an invalid Deepgram API key for validation
- **THEN** the Deepgram API returns an authentication error and the system returns a failure response indicating the key is invalid

### Requirement: Transcription provider factory

The system SHALL provide a scoped `ITranscriptionProviderFactory` that resolves the current user's `TranscriptionProviderConfig`, decrypts the API key, and returns a configured `IAudioTranscriptionProvider`. If the user has no transcription provider configured, the factory SHALL throw an `AudioTranscriptionUnavailableException` with a message indicating configuration is required.

#### Scenario: Factory returns configured provider

- **WHEN** the factory is called for a user with a configured Deepgram key
- **THEN** it returns an `IAudioTranscriptionProvider` configured with the user's decrypted key and model

#### Scenario: Factory throws when no provider configured

- **WHEN** the factory is called for a user without a configured transcription provider
- **THEN** it throws `AudioTranscriptionUnavailableException`

### Requirement: Deepgram audio transcription provider implementation

The system SHALL implement `IAudioTranscriptionProvider` as `DeepgramAudioTranscriptionProvider` using Deepgram's REST pre-recorded API (`POST /v1/listen`). The provider SHALL accept the API key and model at construction time (via factory). The provider SHALL send the audio stream with appropriate query parameters (`model`, `punctuate`, `diarize`, `language`) and map the Deepgram response to `AudioTranscriptionResult` with speaker-labeled segments.

#### Scenario: Successful transcription via REST API

- **WHEN** TranscribeAsync is called with a valid audio stream
- **THEN** the provider sends the audio to Deepgram's REST API, parses the response, and returns an `AudioTranscriptionResult` with full text and speaker-labeled segments

#### Scenario: Deepgram API returns an error

- **WHEN** Deepgram returns an HTTP error (auth failure, rate limit, server error)
- **THEN** the provider throws an appropriate exception that handlers translate to `transcription.failed` or `transcription.providerUnavailable`

### Requirement: Remove ApiKey from app-wide DeepgramSettings

The system SHALL remove the `ApiKey` property from `DeepgramSettings`. The remaining properties (`BaseUrl`, `Model`, `Punctuate`, `InterimResults`, `Language`, `Diarize`, `KeepAliveIntervalSeconds`) SHALL be retained as app-wide defaults for non-secret configuration.

#### Scenario: DeepgramSettings no longer contains ApiKey

- **WHEN** the application loads `DeepgramSettings` from configuration
- **THEN** no `ApiKey` property exists on the settings class
- **AND** the application does not require `Deepgram__ApiKey` environment variable

### Requirement: Frontend transcription provider settings

The settings page SHALL include a Transcription Provider section with: a provider indicator (Deepgram), an API key input field (password-masked), a "Don't have one?" deep link to Deepgram's key creation page (`https://console.deepgram.com/`), a model input with the app default pre-filled, a "Test Connection" button that calls the validate endpoint, and a "Save" button.

#### Scenario: User configures transcription provider for the first time

- **WHEN** a user navigates to settings and views the Transcription Provider section
- **THEN** a deep link to Deepgram's console appears
- **AND** the model field shows the app default (e.g., "nova-3")

#### Scenario: Test connection succeeds

- **WHEN** a user enters a valid API key and clicks "Test Connection"
- **THEN** the system calls the validate endpoint and shows a success indicator

#### Scenario: Test connection fails

- **WHEN** a user enters an invalid API key and clicks "Test Connection"
- **THEN** the system calls the validate endpoint and shows an error message

#### Scenario: User saves configuration

- **WHEN** a user clicks "Save" with a valid API key and model
- **THEN** the frontend calls `PUT /api/users/me/transcription-provider` and displays a success confirmation

#### Scenario: Existing configuration is shown

- **WHEN** a user with an existing transcription provider config navigates to settings
- **THEN** the provider and model are shown and the API key field shows a placeholder (not the actual key)
