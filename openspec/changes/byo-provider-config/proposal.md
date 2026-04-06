## Why

Every AI-powered feature in the app (capture extraction, living briefs, AI chat) depends on calling an LLM. Users must bring their own API key and choose their preferred provider (Anthropic, OpenAI, Google). Without a provider abstraction layer, each AI feature would need its own provider integration, and there's no way for users to configure or change providers. This is a Tier 1 foundation that blocks all Tier 2 AI capabilities.

Additionally, new users need to experience AI value immediately — before investing effort in API key setup. An app-managed "taste" key gives every user 5 free AI operations per day, demonstrating value and driving conversion to BYO key setup.

## What Changes

- Add `AiProviderConfig` value object to the User aggregate (provider, encrypted API key, model, max tokens)
- Add `ConfigureAiProvider` and `RemoveAiProvider` actions on User with validation and domain events
- Create a provider abstraction interface (`IAiCompletionService`) that all AI features consume
- Implement provider-specific adapters for Anthropic, OpenAI, and Google
- Add model fallback chain — if a configured model is unavailable (deprecated/renamed), automatically try fallback models before failing
- Add API key encryption at rest in the database (AES-256-GCM)
- Add app-managed "taste" key system — default Google API key with per-user daily operation budget (5/day, never expires)
- Add taste budget tracking (per-user, per-day operation counter)
- Add AI setup nudge framework — four user states (Fresh, Tasting, Limited, Unlimited) with contextual prompts encouraging users to add their own key
- Expose user-facing endpoints to configure and manage AI provider settings
- Add frontend settings UI for provider selection, API key entry with deep links to provider console pages, auto-validation, and model selection
- Add subtle taste counter UI in AI-powered sections ("3 of 5 free AI operations remaining today")
- Add provider health-check / key validation endpoint
- Config-driven model catalog and defaults in `appsettings.json`

## Non-goals

- Streaming responses (batch completion only — streaming comes with AI chat specs)
- Prompt engineering or templates (downstream specs define their own prompts)
- Token usage tracking or cost estimation
- Multiple provider configs per user (one active config at a time)
- Provider-specific advanced features (function calling, vision) — unified text-in/text-out
- OAuth-based provider access (no provider offers sanctioned 3rd-party OAuth for API delegation)
- Third-party intermediaries like OpenRouter

## Capabilities

### New Capabilities
- `ai-provider-abstraction`: BYO provider configuration (API key paste with deep links and auto-validate), API key encryption at rest, provider interface abstraction with model fallback chain, taste key system (app-managed default key with daily budget), AI setup nudge framework, config-driven model catalog with defaults. Covers domain model (AiProviderConfig VO, ConfigureAiProvider/RemoveAiProvider actions), infrastructure (encryption, HTTP adapters per provider, taste budget tracking), application handlers, API endpoints, and frontend settings UI.

### Modified Capabilities
- `user-auth-tenancy`: The User aggregate gains the `AiProviderConfig` property and `ConfigureAiProvider`/`RemoveAiProvider` actions. The user profile response gains `hasAiProvider` boolean. This is additive — no existing requirements change, but the User entity is extended.

## Impact

- **Domain:** User aggregate extended with `AiProviderConfig` value object, new business actions, new domain events
- **Infrastructure:** New EF Core migration for `AiProviderConfig` columns on Users table. New encryption service for API key storage. New HTTP client adapters for Anthropic, OpenAI, and Google APIs. New taste budget tracking table. Model fallback logic in completion service.
- **Application:** New handlers for configuring provider, getting provider status, validating keys, removing config, getting available models
- **Web:** New API endpoints under `/api/users/me/ai-provider` and `/api/ai/models`
- **Frontend:** New AI provider section on the settings page (provider selector, API key input with deep links, model selector, test connection, taste counter). Nudge components for Fresh/Tasting/Limited states.
- **Configuration:** Model catalog, defaults, fallback chains, taste key config, encryption key — all in `appsettings.json`, documented in README
- **Dependencies:** New NuGet packages for Anthropic SDK, OpenAI SDK. Google will use REST via HttpClient.
