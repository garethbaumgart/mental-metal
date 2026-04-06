## 1. Domain Layer

- [ ] 1.1 Create `AiProvider` enum (Anthropic, OpenAI, Google) in Domain/Users
- [ ] 1.2 Create `AiProviderConfig` value object (Provider, EncryptedApiKey, Model, MaxTokens?) extending ValueObject
- [ ] 1.3 Add nullable `AiProviderConfig?` property to User aggregate (null by default on registration)
- [ ] 1.4 Implement `User.ConfigureAiProvider(provider, encryptedApiKey, model, maxTokens?)` action with validation, raising `AiProviderConfigured` event
- [ ] 1.5 Implement `User.RemoveAiProvider()` action raising `AiProviderRemoved` event (idempotent — no event if already null)
- [ ] 1.6 Add `AiProviderConfigured` and `AiProviderRemoved` domain events

## 2. Domain Tests

- [ ] 2.1 Test `AiProviderConfig` value object: creation, equality
- [ ] 2.2 Test `User.ConfigureAiProvider`: sets config, raises event, validates inputs (empty key/model rejected)
- [ ] 2.3 Test `User.RemoveAiProvider`: clears config, raises event, idempotent when already null
- [ ] 2.4 Test `User.Register`: new user has null AiProviderConfig

## 3. Application Layer — Interfaces and Models

- [ ] 3.1 Create `IApiKeyEncryptionService` interface in Application/Common (Encrypt, Decrypt methods)
- [ ] 3.2 Create `IAiCompletionService` interface in Application/Common with `CompleteAsync(AiCompletionRequest, CancellationToken)` returning `AiCompletionResult`
- [ ] 3.3 Create `AiCompletionRequest` record (SystemPrompt, UserPrompt, MaxTokens?, Temperature?)
- [ ] 3.4 Create `AiCompletionResult` record (Content, InputTokens, OutputTokens, Model, Provider)
- [ ] 3.5 Create `AiProviderException` and `TasteLimitExceededException` exception classes

## 4. Application Layer — Handlers

- [ ] 4.1 Create `ConfigureAiProvider` handler — encrypts key, calls User.ConfigureAiProvider, persists
- [ ] 4.2 Create `GetAiProviderStatus` handler — returns provider config (without key), taste budget remaining
- [ ] 4.3 Create `ValidateAiProvider` handler — accepts provider/key/model, calls IAiCompletionService with test prompt, returns success/failure
- [ ] 4.4 Create `RemoveAiProvider` handler — calls User.RemoveAiProvider, persists
- [ ] 4.5 Create `GetAvailableModels` handler — returns models from config for a given provider
- [ ] 4.6 Create DTOs: `ConfigureAiProviderRequest`, `AiProviderStatusResponse` (incl. taste budget), `ValidateAiProviderRequest`, `ValidateAiProviderResponse`, `AvailableModelsResponse`

## 5. Application Tests

- [ ] 5.1 Test `ConfigureAiProvider` handler: encrypts key, saves config, persists via UoW
- [ ] 5.2 Test `GetAiProviderStatus`: returns status for configured/unconfigured users, includes taste budget
- [ ] 5.3 Test `ValidateAiProvider`: success and failure paths
- [ ] 5.4 Test `RemoveAiProvider`: clears config, persists
- [ ] 5.5 Test `GetAvailableModels`: returns correct models per provider, rejects invalid provider

## 6. Infrastructure — Encryption

- [ ] 6.1 Create `AesApiKeyEncryptionService` implementing `IApiKeyEncryptionService` (AES-256-GCM, Base64 nonce:ciphertext:tag format)
- [ ] 6.2 Add `AiProviderSettings` options class with EncryptionKey, TasteKey config, and per-provider model config (DefaultModel, FallbackModels)
- [ ] 6.3 Add startup validation — fail fast if `AiProvider:EncryptionKey` is missing (taste key is optional — disabled if not set)
- [ ] 6.4 Register encryption service in DI

## 7. Infrastructure — Provider Adapters and Completion Service

- [ ] 7.1 Add NuGet packages: Anthropic SDK, OpenAI SDK
- [ ] 7.2 Create internal `IAiProviderAdapter` interface (CompleteAsync with decrypted key, model, request)
- [ ] 7.3 Implement `AnthropicAdapter` using Anthropic .NET SDK
- [ ] 7.4 Implement `OpenAiAdapter` using OpenAI .NET SDK
- [ ] 7.5 Implement `GoogleAdapter` using HttpClient with Gemini REST API
- [ ] 7.6 Create `AiCompletionService` implementing `IAiCompletionService` — resolves current user's config or taste key, decrypts key, dispatches to correct adapter, implements model fallback chain (retry on model-not-found only)
- [ ] 7.7 Register AI services in DI

## 8. Infrastructure — Taste Budget Tracking

- [ ] 8.1 Create `AiTasteBudget` entity (UserId, Date, OperationsUsed) — infrastructure concern, not a domain entity
- [ ] 8.2 Create `AiTasteBudgetConfiguration` EF Core config
- [ ] 8.3 Create `ITasteBudgetService` interface and implementation — check/decrement budget, per-user per-day
- [ ] 8.4 Register taste budget service in DI

## 9. Infrastructure — Persistence

- [ ] 9.1 Add `AiProviderConfig` owned entity configuration to `UserConfiguration` (OwnsOne with encrypted key column)
- [ ] 9.2 Create EF Core migration for AiProviderConfig columns on Users table and AiTasteBudgets table
- [ ] 9.3 Add `hasAiProvider` to `GetCurrentUser` response DTO

## 10. Web Layer — API Endpoints

- [ ] 10.1 Map `PUT /api/users/me/ai-provider` — configure provider (encrypt key, save)
- [ ] 10.2 Map `GET /api/users/me/ai-provider` — get provider status and taste budget
- [ ] 10.3 Map `POST /api/users/me/ai-provider/validate` — validate key/model via test completion
- [ ] 10.4 Map `DELETE /api/users/me/ai-provider` — remove provider config
- [ ] 10.5 Map `GET /api/ai/models?provider={provider}` — list available models from config
- [ ] 10.6 Register new application handlers in DI

## 11. Configuration and Documentation

- [ ] 11.1 Add AI provider configuration section to `appsettings.json` (encryption key, taste key, per-provider model defaults and fallbacks)
- [ ] 11.2 Add AI provider configuration section to `appsettings.Development.json` with development defaults
- [ ] 11.3 Document AI provider configuration in README (encryption key, taste key setup, model config, environment variables)

## 12. Frontend — AI Provider Settings

- [ ] 12.1 Create `AiProviderService` for provider API calls (configure, get status, validate, remove, get models)
- [ ] 12.2 Add AI Provider section to settings page: provider cards (Anthropic/OpenAI/Google), API key input (password-masked), "Don't have one?" deep link per provider
- [ ] 12.3 Implement model dropdown populated from config endpoint with default pre-selected
- [ ] 12.4 Implement auto-validate on key paste (instant feedback — green check or error)
- [ ] 12.5 Implement save flow calling PUT endpoint with success confirmation
- [ ] 12.6 Implement remove flow with confirmation dialog
- [ ] 12.7 Pre-populate form when user has existing config (placeholder for API key)

## 13. Frontend — Taste Counter and Nudge Framework

- [ ] 13.1 Create `AiNudgeService` — derives nudge state (Fresh/Tasting/Limited/Unlimited) from provider status and taste budget
- [ ] 13.2 Create subtle taste counter component ("3 of 5 free AI operations remaining today") — visible only for taste users
- [ ] 13.3 Create contextual nudge component for Tasting state ("Add your own AI key for unlimited access") — dismissible for 7 days
- [ ] 13.4 Create blocking nudge component for Limited state ("You've used your 5 free AI operations today") — "Set Up AI Provider" and "Continue tomorrow" actions
- [ ] 13.5 Create passive indicator for Fresh state (settings page "AI Provider: Not configured", empty states in AI sections)
- [ ] 13.6 Integrate nudge components into AI-powered sections (capture processing, AI chat, briefing, etc. — placeholder integration points for future features)

## 14. E2E Tests

- [ ] 14.1 E2E test: Unauthenticated requests to AI provider endpoints return 401
- [ ] 14.2 E2E test: GET /api/ai/models returns model list for valid provider
- [ ] 14.3 E2E test: GET /api/users/me/ai-provider returns isConfigured=false and taste budget for new user
