## 1. Domain — TranscriptionProviderConfig on User aggregate

- [x] 1.1 Add `TranscriptionProvider` enum (`Deepgram`) in `Domain/Users/`
- [x] 1.2 Add `TranscriptionProviderConfig` value object (Provider, EncryptedApiKey, Model) mirroring `AiProviderConfig`
- [x] 1.3 Add `ConfigureTranscriptionProvider` and `RemoveTranscriptionProvider` actions on the `User` aggregate with domain events (`TranscriptionProviderConfigured`, `TranscriptionProviderRemoved`)
- [x] 1.4 Unit tests for `TranscriptionProviderConfig` value object (creation, equality, validation)
- [x] 1.5 Unit tests for `User.ConfigureTranscriptionProvider` and `User.RemoveTranscriptionProvider` (happy path, idempotent removal, invalid inputs)

## 2. Infrastructure — Persistence and provider implementation

- [x] 2.1 Add EF Core configuration for `TranscriptionProviderConfig` owned entity on `UserConfiguration` (columns: `TranscriptionProviderProvider`, `TranscriptionProviderEncryptedApiKey`, `TranscriptionProviderModel`)
- [x] 2.2 Add EF Core migration for new User columns
- [x] 2.3 Implement `DeepgramAudioTranscriptionProvider` using Deepgram REST pre-recorded API (`POST /v1/listen`) — accepts API key and model at construction
- [x] 2.4 Implement `ITranscriptionProviderFactory` as scoped service — loads user's config, decrypts key, returns configured `DeepgramAudioTranscriptionProvider`
- [x] 2.5 Register `ITranscriptionProviderFactory` in DI (replace dev-only stub registration with factory-based resolution)

## 3. Application — Handlers for transcription provider config

- [x] 3.1 Add `ConfigureTranscriptionProvider` handler (mirrors `ConfigureAiProvider`) with `PUT /api/users/me/transcription-provider` endpoint
- [x] 3.2 Add `GetTranscriptionProviderStatus` handler with `GET /api/users/me/transcription-provider` endpoint (returns provider, model, isConfigured — never the key)
- [x] 3.3 Add `RemoveTranscriptionProvider` handler with `DELETE /api/users/me/transcription-provider` endpoint
- [x] 3.4 Add `ValidateTranscriptionProviderKey` handler with `POST /api/users/me/transcription-provider/validate` endpoint (tests key against Deepgram API)

## 4. Application — Update transcription handlers to use per-user keys

- [x] 4.1 Update `UploadAudioCapture` handler to resolve provider via `ITranscriptionProviderFactory` instead of `IAudioTranscriptionProvider`; return `transcription.notConfigured` when no key
- [x] 4.2 Update `TranscribeCapture` handler to resolve provider via `ITranscriptionProviderFactory`; return `transcription.notConfigured` when no key
- [x] 4.3 Update WebSocket streaming endpoint (`TranscriptionEndpoints.HandleStream`) to read API key from user's `TranscriptionProviderConfig` instead of `IOptions<DeepgramSettings>`
- [x] 4.4 Update transcription status endpoint to check user's config instead of app-wide settings

## 5. Web — Remove app-wide API key from config

- [x] 5.1 Remove `ApiKey` property from `DeepgramSettings`
- [x] 5.2 Remove `Deepgram.ApiKey` from `appsettings.json` and any environment variable references
- [x] 5.3 Verify remaining `DeepgramSettings` properties (BaseUrl, Model, Language, etc.) still load correctly

## 6. Frontend — Transcription provider settings UI

- [x] 6.1 Add transcription provider settings section to the settings page (provider label, API key input, model input, deep link to Deepgram console)
- [x] 6.2 Add "Test Connection" button wired to `POST /api/users/me/transcription-provider/validate`
- [x] 6.3 Add "Save" button wired to `PUT /api/users/me/transcription-provider` with success/error feedback
- [x] 6.4 Load existing config on settings page init via `GET /api/users/me/transcription-provider`
- [x] 6.5 Add "Remove" action wired to `DELETE /api/users/me/transcription-provider`

## 7. Testing

- [x] 7.1 E2E test: configure transcription provider via settings API, upload audio, verify transcription uses per-user key
- [x] 7.2 E2E test: attempt audio upload without configured transcription provider, verify `transcription.notConfigured` error
- [x] 7.3 E2E test: validate endpoint returns success for valid key and failure for invalid key
