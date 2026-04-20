## Why

The Deepgram transcription API key is currently an app-wide server config (`Deepgram__ApiKey` env var), meaning the operator pays for all users' transcription. This blocks the BYOK (Bring Your Own Key) model that already works for AI providers. Users should bring their own Deepgram key so transcription costs are self-service, matching the existing `ai-provider-abstraction` pattern.

## What Changes

- Add `TranscriptionProviderConfig` value object on the `User` aggregate (provider enum, encrypted API key, model) — mirrors `AiProviderConfig`
- Add `ConfigureTranscriptionProvider` / `RemoveTranscriptionProvider` domain actions and corresponding API endpoints
- Add validation endpoint to test a user's Deepgram key before saving
- Implement a real `DeepgramAudioTranscriptionProvider` (currently only a dev stub exists) that resolves the user's decrypted key per-request
- Update `UploadAudioCapture`, `TranscribeCapture`, and the WebSocket streaming endpoint to resolve the user's transcription key instead of reading from `IOptions<DeepgramSettings>`
- Add transcription provider settings UI in the frontend settings page
- Remove `ApiKey` from app-wide `DeepgramSettings` (retain non-secret defaults: `BaseUrl`, `Model`, `Language`, `Diarize`)

## Non-goals

- Supporting transcription providers other than Deepgram (enum is extensible, but only Deepgram is implemented)
- Taste key / free-tier system for transcription (users must bring their own key)
- Changing the transcription pipeline itself (upload, segment splitting, speaker identification are unchanged)

## Capabilities

### New Capabilities

- `transcription-provider-abstraction`: BYOK transcription provider config on User aggregate, encrypted key storage, per-request key resolution, real Deepgram implementation of `IAudioTranscriptionProvider`, and frontend settings UI

### Modified Capabilities

- `capture-audio`: Transcription handlers resolve the user's transcription key instead of app-wide config; endpoints return appropriate errors when no key is configured

## Impact

- **Domain:** `User` aggregate gains `TranscriptionProviderConfig` value object, new domain actions and events
- **Application:** `UploadAudioCapture` and `TranscribeCapture` handlers gain a dependency on user's transcription config; new `ConfigureTranscriptionProvider` handler
- **Infrastructure:** New `DeepgramAudioTranscriptionProvider` implementation; `AesApiKeyEncryptionService` reused (no changes); new EF Core migration for user columns; scoped factory to resolve per-user transcription provider
- **Web:** New API endpoints (`PUT/GET/DELETE /api/users/me/transcription-provider`, `POST .../validate`); WebSocket streaming endpoint updated; `DeepgramSettings.ApiKey` removed from config
- **Frontend:** New transcription provider section in settings page
- **Database:** New columns on Users table for transcription provider config (provider, encrypted key, model)
- **Tier:** This is a Tier 1 extension (extends `ai-provider-abstraction` pattern) and a prerequisite for production `capture-audio`
