## Context

Mental Metal's Tier 2 features (capture extraction, living briefs, AI chat) all depend on calling an LLM. Users bring their own API keys. The User aggregate currently has no AI configuration. We need a provider abstraction that:

1. Stores user's provider config (provider, API key, model) on the User aggregate
2. Encrypts API keys at rest in the database
3. Provides a uniform interface (`IAiCompletionService`) that downstream features consume
4. Supports Anthropic (Claude), OpenAI (GPT), and Google (Gemini)
5. Gives new users immediate AI access via an app-managed "taste" key before they set up their own
6. Gracefully handles model deprecation via fallback chains

The `user-auth-tenancy` spec is already implemented — User aggregate, auth, preferences, and multi-tenant scoping are in place.

### Why Not OAuth?

No AI provider offers a sanctioned OAuth flow for third-party API delegation:
- **Anthropic:** Built OAuth only for first-party tools (Claude Code). Third-party use was explicitly banned (Feb 2026) and blocked via client fingerprinting (Apr 2026).
- **OpenAI:** OAuth exists only for ChatGPT plugin/action ecosystem (reversed flow — OpenAI calls your API, not the other way).
- **Google:** Generative Language API OAuth scopes exist only for tuning and retrieval, not general text generation.

API key paste is the industry standard for BYO-key apps (Cursor, Windsurf, Cody all use this pattern).

## Goals / Non-Goals

**Goals:**
- Users can configure their AI provider (Anthropic, OpenAI, Google) with an API key and model selection
- API keys are encrypted at rest using AES-256-GCM with a server-managed encryption key
- A single `IAiCompletionService` interface that all AI features consume — no provider-specific code leaks into Application/Domain layers
- Model fallback chain — if a model is deprecated/unavailable, automatically try alternatives before failing
- Config-driven model catalog with per-provider defaults — updatable without code changes
- App-managed taste key gives new users 5 free AI operations/day to demonstrate value before key setup
- Nudge framework guides users through Fresh → Tasting → Limited → Unlimited states
- Key validation endpoint so users can test their configuration
- Frontend settings UI with deep links to provider console pages for key creation

**Non-Goals:**
- Streaming responses — batch completion only for now (streaming added with AI chat specs)
- Prompt engineering or prompt templates — downstream specs define their own prompts
- Token usage tracking or cost estimation
- Multiple provider configurations per user (one active config at a time)
- Provider-specific advanced features (function calling, vision, etc.) — unified text-in/text-out for now
- Rate limiting or retry logic beyond basic HTTP resilience and model fallback
- OAuth-based provider access or third-party intermediaries (OpenRouter, LiteLLM)

## Decisions

### 1. AiProviderConfig as a Value Object embedded in User

The domain model specifies `AiProviderConfig` as an owned value object on User, not a separate entity. This is correct because:
- Config has no independent lifecycle — it exists only in the context of a User
- One config per user (not a collection)
- Changes are atomic with the User aggregate

EF Core maps this via `OwnsOne` with columns on the Users table (`AiProvider_Provider`, `AiProvider_EncryptedApiKey`, `AiProvider_Model`, `AiProvider_MaxTokens`).

**Alternative considered:** Separate `AiProviderConfigs` table — rejected because it adds a join for every AI operation and breaks aggregate encapsulation.

### 2. API Key Encryption via IApiKeyEncryptionService

API keys are encrypted before storage and decrypted only when making API calls. The encryption key is a server-side configuration value (`AiProvider:EncryptionKey` in appsettings / environment variable).

- **Algorithm:** AES-256-GCM (authenticated encryption)
- **Storage format:** Base64-encoded `nonce:ciphertext:tag`
- **Domain stores:** The encrypted string (domain doesn't know about encryption)
- **Infrastructure handles:** Encrypt on write, decrypt on read when building HTTP clients

The `IApiKeyEncryptionService` interface lives in Application; implementation in Infrastructure.

**Alternative considered:** Using ASP.NET Data Protection API — rejected because it ties key rotation to the machine/cluster and makes database portability harder. AES-256-GCM with a configurable key is simpler and more portable.

### 3. IAiCompletionService as the Provider Abstraction

A single interface in Application layer:

```csharp
public interface IAiCompletionService
{
    Task<AiCompletionResult> CompleteAsync(
        AiCompletionRequest request,
        CancellationToken cancellationToken);
}
```

Where `AiCompletionRequest` contains: system prompt, user prompt, max tokens (optional override), temperature (optional). `AiCompletionResult` contains: content, token usage (input/output), model used, provider used.

The implementation (`AiCompletionService` in Infrastructure) resolves the current user's config (or falls back to the taste key), decrypts the API key, and delegates to the appropriate provider adapter.

**Alternative considered:** One `IAiCompletionService` per provider injected via DI — rejected because it pushes provider selection into every consumer. A single interface with internal dispatch keeps consumers clean.

### 4. Provider Adapters in Infrastructure

Three adapter classes, each implementing a private `IAiProviderAdapter` interface internal to Infrastructure:

- `AnthropicAdapter` — uses Anthropic .NET SDK (`Anthropic` NuGet package)
- `OpenAiAdapter` — uses OpenAI .NET SDK (`OpenAI` NuGet package)
- `GoogleAdapter` — uses Google AI .NET SDK or direct REST via `HttpClient`

Adapters are stateless — they receive the decrypted API key and model per-request. No connection pooling or cached clients (keys differ per user).

**Alternative considered:** Using `IHttpClientFactory` with named clients — rejected because each user has a different API key, so we can't pre-configure named clients. Adapters create clients per-request with the user's key.

### 5. Config-Driven Model Catalog with Fallback Chains

Model configuration lives in `appsettings.json`, not hardcoded in source:

```json
{
  "AiProvider": {
    "Providers": {
      "Anthropic": {
        "DefaultModel": "claude-sonnet-4-20250514",
        "FallbackModels": ["claude-sonnet-4-6", "claude-haiku-4-5"]
      },
      "OpenAI": {
        "DefaultModel": "gpt-4o",
        "FallbackModels": ["gpt-4o-mini", "gpt-4.1-mini"]
      },
      "Google": {
        "DefaultModel": "gemini-2.5-flash",
        "FallbackModels": ["gemini-2.0-flash", "gemini-1.5-flash"]
      }
    }
  }
}
```

When the primary model returns a model-not-found error, the adapter walks the fallback list. Only model-not-found errors trigger fallback — auth errors, rate limits, and server errors bubble up immediately.

**Alternative considered:** Dynamic model listing from provider APIs — rejected for simplicity and reliability. Config-driven is updatable without code changes but doesn't add API call latency or failure modes. Also considered hardcoded in code — rejected because it requires a code deployment to update when models change.

### 6. Taste Key System (App-Managed Default Key)

New users get immediate AI access via an app-managed Google API key. This demonstrates value before asking users to invest effort in API key setup.

- **Provider:** Google (Gemini 2.5 Flash for development, 2.5 Pro for production)
- **Budget:** 5 AI operations per user per day (configurable)
- **Tracking:** Simple `AiTasteBudget` table (UserId, Date, OperationsUsed) — infrastructure concern, not a domain entity
- **Never expires:** A user who signed up months ago still gets taste operations
- **Same model defaults:** Taste and BYO-key users get the same default model per provider

The `AiCompletionService` resolution order:
1. User's own `AiProviderConfig` → use their key (no budget)
2. No config → use taste key → check budget → proceed or throw `TasteLimitExceededException`

```json
{
  "AiProvider": {
    "TasteKey": {
      "Provider": "Google",
      "ApiKey": "...",
      "DailyLimitPerUser": 5
    }
  }
}
```

**Alternative considered:** Require key setup before any AI features — rejected because users need to experience value before investing effort. The taste system creates a natural conversion funnel.

### 7. AI Setup Nudge Framework

Users move through four states that determine which nudge they see:

- **Fresh** — No AI used yet. Passive indicators only (settings page shows "not configured").
- **Tasting** — Using taste key, under daily limit. Contextual suggestions ("Add your own key for unlimited AI") after AI operations complete. Non-blocking, dismissible for 7 days.
- **Limited** — Daily taste budget exhausted. Blocking prompt — cannot proceed without own key or waiting until tomorrow. Clear messaging: "You've used your 5 free AI operations today."
- **Unlimited** — Own key configured. No nudges.

The nudge state is derived from `user.AiProviderConfig` (null or not) and `AiTasteBudget` (remaining operations). No separate state entity needed.

### 8. Key Validation via Lightweight API Call

The `/api/users/me/ai-provider/validate` endpoint sends a minimal completion request ("respond with OK") to verify the API key and model work. This gives users confidence before saving config.

Validation is a separate endpoint (not part of ConfigureAiProvider) because:
- Users may want to test before committing
- Validation involves external API calls which may be slow or fail
- The domain action (ConfigureAiProvider) should be fast and offline

### 9. Frontend Deep Links for Key Creation

Instead of generic "get an API key" instructions, the settings UI provides deep links to the exact key creation page per provider:

- **Anthropic:** `https://console.anthropic.com/settings/keys`
- **OpenAI:** `https://platform.openai.com/api-keys`
- **Google:** `https://aistudio.google.com/apikey`

Auto-validate triggers the moment the user pastes a key, providing instant feedback.

## Risks / Trade-offs

- **[Encryption key management]** — If the server encryption key is lost, all stored API keys become unrecoverable. → Mitigation: Document key backup procedures. Users can always re-enter their API key.
- **[Provider SDK version drift]** — Anthropic/OpenAI SDKs may have breaking changes. → Mitigation: Pin SDK versions; adapters are isolated so updates are contained.
- **[Taste key cost]** — App-managed key has real cost as users grow. → Mitigation: 5 ops/day cap limits exposure. Google free tier covers development. Move to paid tier at launch (~$0.003/op). Monitor and adjust limit.
- **[Taste key rate limits]** — Google free tier has RPM/RPD limits that could be hit with concurrent users. → Mitigation: Gemini 2.5 Flash has 50 RPD free tier. Sufficient for development. Fallback chain degrades gracefully to models with higher free limits (2.0 Flash: 1,500 RPD).
- **[Google free tier commercial use]** — Free tier is intended for prototyping, not production. → Mitigation: Switch to pay-as-you-go at launch. Free tier is fine during development.
- **[Per-request client creation]** — Creating HTTP clients per-request is less efficient than pooling. → Mitigation: Acceptable for now; AI calls are infrequent and high-latency anyway. Can add client caching later keyed by user+provider.
- **[Single provider per user]** — Users who want to use Claude for extraction and GPT for chat can't. → Mitigation: Single config keeps the UX simple for v1. Multi-config is a future enhancement if demand exists.
- **[Model fallback masking issues]** — Silent fallback might confuse users if quality drops. → Mitigation: Log fallback events. `AiCompletionResult` includes actual model used so callers can surface it if needed.

## Dependencies

- `user-auth-tenancy` (Tier 1, implemented) — User aggregate, auth, multi-tenant scoping
