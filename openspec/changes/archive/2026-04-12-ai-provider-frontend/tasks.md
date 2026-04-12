## 1. AI Provider Service and Models

- [x] 1.1 Create AI provider TypeScript models: `AiProviderStatus`, `AiModelInfo`, `ConfigureAiProviderRequest`, `ValidateAiProviderRequest`, `ValidateAiProviderResponse`, `AvailableModelsResponse`
- [x] 1.2 Create `AiProviderService` with methods: `getStatus()`, `configure()`, `validate()`, `remove()`, `getModels(provider)`
- [x] 1.3 Add `status` signal to `AiProviderService` — loaded on init and refreshed after configure/remove

## 2. AI Provider Settings Section

- [x] 2.1 Create `AiProviderSettingsComponent` (standalone) with provider cards (Anthropic, OpenAI, Google) using PrimeNG Card + RadioButton
- [x] 2.2 Add API key input (password-masked) with "Don't have one?" deep link per provider
- [x] 2.3 Add model dropdown populated from `GET /api/ai/models?provider=` with default pre-selected
- [x] 2.4 Implement auto-validate on key paste — debounce 500ms, show inline status (spinner → green check/red error)
- [x] 2.5 Implement save flow — call `PUT /api/users/me/ai-provider`, show success toast, refresh status
- [x] 2.6 Implement remove flow — confirmation dialog, call `DELETE`, refresh status, clear form
- [x] 2.7 Pre-populate form when user has existing config (provider, model selected, API key placeholder)
- [x] 2.8 Add `AiProviderSettingsComponent` to settings page below Preferences section

## 3. AI Nudge Service

- [x] 3.1 Create `AiNudgeService` with `nudgeState` signal computed from `AiProviderService.status` — derives Fresh/Tasting/Limited/Unlimited
- [x] 3.2 Add nudge dismissal helpers using localStorage with 7-day TTL for contextual nudges

## 4. Taste Counter Component

- [x] 4.1 Create `TasteCounterComponent` (standalone) — displays "N of M free AI operations remaining today"
- [x] 4.2 Visibility controlled by `AiNudgeService.nudgeState` — hidden for Unlimited users

## 5. Nudge Components

- [x] 5.1 Create `AiNudgeContextualComponent` — non-blocking "Add your own AI key for unlimited access" message, dismissible for 7 days
- [x] 5.2 Create `AiNudgeLimitedComponent` — blocking prompt with "Set Up AI Provider" and "Continue tomorrow" actions
- [x] 5.3 Create `AiNudgeFreshComponent` — passive "AI Provider: Not configured" indicator with settings link

## 6. Frontend Tests

- [x] 6.1 Unit test `AiNudgeService` — state derivation for all four states (Fresh/Tasting/Limited/Unlimited)
- [x] 6.2 Unit test `AiProviderSettingsComponent` — provider selection, form population, validation display

## 7. E2E Tests

- [x] 7.1 E2E test: AI provider settings section visible on settings page for authenticated user
- [x] 7.2 E2E test: GET /api/ai/models returns model list for valid provider
- [x] 7.3 E2E test: GET /api/users/me/ai-provider returns isConfigured=false and taste budget for new user
