## Context

Transcription is currently configured with an app-wide `DeepgramSettings` object bound from `appsettings.json`, including the `ApiKey`. The WebSocket streaming endpoint and the `IAudioTranscriptionProvider` interface both consume this config. In production, no real `IAudioTranscriptionProvider` implementation is registered — only a dev stub exists. Meanwhile, the AI provider abstraction already supports per-user BYOK keys via `AiProviderConfig` on the `User` aggregate, encrypted with AES-256-GCM and decrypted per-request.

## Dependencies

- `ai-provider-abstraction` — reuses `IApiKeyEncryptionService`, `AesApiKeyEncryptionService`, and the BYOK pattern
- `capture-audio` — the spec that defines `IAudioTranscriptionProvider` and the transcription lifecycle

## Goals / Non-Goals

**Goals:**

- Per-user Deepgram API key storage using the same encryption infrastructure as AI provider keys
- Real `DeepgramAudioTranscriptionProvider` implementation of `IAudioTranscriptionProvider` for file-based transcription
- WebSocket streaming endpoint uses per-user key
- Frontend settings UI for transcription provider config
- Clean separation: app-wide config retains non-secret defaults, user config holds the key

**Non-Goals:**

- Multi-provider transcription support (only Deepgram, though the enum is extensible)
- Taste key / free-tier for transcription
- Changes to the transcription pipeline itself (segment splitting, speaker ID, audio discard)
- Real-time streaming architecture changes

## Decisions

### 1. Mirror `AiProviderConfig` with `TranscriptionProviderConfig` value object

**Decision:** Add a new `TranscriptionProviderConfig` value object on the `User` aggregate with `TranscriptionProvider` (enum), `EncryptedApiKey`, and `Model`.

**Rationale:** This is the exact same pattern that works for AI providers. The User aggregate already owns provider config; transcription config is the same concern. A single enum value (`Deepgram`) is fine — adding `Whisper` or `AssemblyAI` later means adding an enum variant and an adapter.

**Alternative considered:** Storing the Deepgram key inside the existing `AiProviderConfig`. Rejected because transcription and AI completion are separate concerns with different providers and lifecycles.

### 2. Scoped factory for per-request key resolution

**Decision:** Register a scoped `ITranscriptionProviderFactory` that loads the current user's `TranscriptionProviderConfig`, decrypts the key, and returns a configured `IAudioTranscriptionProvider`. Handlers depend on the factory, not on `IAudioTranscriptionProvider` directly.

**Rationale:** The `IAudioTranscriptionProvider` interface doesn't carry credentials. Rather than changing the interface (which would couple it to BYOK), a factory resolves the correct provider with the correct key per-request. This keeps the transcription abstraction clean and testable.

**Alternative considered:** Adding the API key to `AudioTranscriptionRequest`. Rejected because it couples the domain-level abstraction to infrastructure concerns. The factory pattern keeps the boundary clean.

### 3. `DeepgramSettings` retains non-secret defaults only

**Decision:** Remove `ApiKey` from `DeepgramSettings`. Keep `BaseUrl`, `Model` (as default), `Language`, `Diarize`, `Punctuate`, `InterimResults`, `KeepAliveIntervalSeconds` as app-wide defaults. The user's `TranscriptionProviderConfig.Model` overrides the default model if set.

**Rationale:** Non-secret config like base URL and language defaults are operational concerns, not user concerns. Users only need to provide their key and optionally choose a model.

### 4. Reuse existing `IApiKeyEncryptionService`

**Decision:** Use the same `AesApiKeyEncryptionService` for transcription keys. No new encryption infrastructure.

**Rationale:** Same encryption key, same algorithm, same storage pattern. No reason to diverge.

### 5. Real `DeepgramAudioTranscriptionProvider` for file uploads

**Decision:** Implement `IAudioTranscriptionProvider` using Deepgram's REST pre-recorded API (`POST /v1/listen`). This handles the `UploadAudioCapture` and `TranscribeCapture` flows. The WebSocket streaming endpoint remains a separate code path (it's real-time, not file-based).

**Rationale:** The REST API is the right choice for uploaded files. The WebSocket endpoint already works differently (it's a proxy relay, not a provider implementation) and will just swap where it reads the API key from.

### 6. Graceful error when no key configured

**Decision:** When a user attempts transcription without a configured key, return a specific error code (`transcription.notConfigured`) rather than a 500. The frontend uses this to prompt the user to add their key in settings.

**Rationale:** This is a normal user state (especially for new users), not an error. Clear error codes let the frontend guide the user.

## Risks / Trade-offs

- **[Risk] User with invalid key gets repeated failures** → Mitigation: validation endpoint lets users test their key before saving; status endpoint checks connectivity
- **[Risk] Deepgram REST API adds latency vs. direct WebSocket** → Mitigation: REST API is only for uploaded files (already async from user perspective); real-time streaming still uses WebSocket
- **[Risk] Migration adds columns to Users table** → Mitigation: nullable columns, no data migration needed, backwards compatible

## Open Questions

- Should the user's model choice override or be constrained to a known list? Current leaning: free-text with the app default as suggestion, since Deepgram model names change over time.
