## Context

The backend AI provider abstraction is fully implemented: REST endpoints for CRUD on provider config, API key validation, model catalog, taste budget tracking, and encryption at rest. The existing settings page has Profile and Preferences sections. This change adds the AI Provider configuration section and app-wide nudge framework, all frontend-only.

Existing backend endpoints consumed:
- `GET /api/users/me/ai-provider` — provider status + taste budget
- `PUT /api/users/me/ai-provider` — configure provider (apiKey optional for updates)
- `POST /api/users/me/ai-provider/validate` — test key+model
- `DELETE /api/users/me/ai-provider` — remove provider
- `GET /api/ai/models?provider={provider}` — available models per provider

## Goals / Non-Goals

**Goals:**
- Users can configure, validate, and remove their AI provider from the settings page
- Taste users see their remaining daily budget and contextual nudges to set up their own key
- Nudge framework provides integration points for future AI-powered features

**Non-Goals:**
- Backend changes (all endpoints exist)
- Building the AI-powered features themselves (capture, briefings, chat)
- Provider usage analytics or billing

## Dependencies

- `user-auth-tenancy` — authentication, settings page, auth guard (complete)
- `ai-provider-abstraction` — backend endpoints (complete)

## Decisions

### 1. AI Provider section on existing settings page (not a separate page)

Add the AI Provider section as a third section on the existing settings page, below Profile and Preferences. This keeps all user configuration in one place and avoids route proliferation for a single form.

*Alternative: Dedicated `/settings/ai` route* — rejected because the provider config is a single form, not complex enough to warrant its own page.

### 2. AiProviderService as a standalone injectable service

Create `AiProviderService` in `shared/services/` to encapsulate all provider API calls. The settings component consumes it. Future AI features will also inject this service for status checks.

### 3. AiNudgeService derives state from provider status signal

`AiNudgeService` exposes a `nudgeState` signal (Fresh/Tasting/Limited/Unlimited) computed from `AiProviderService.status()`. Components subscribe to the signal — no polling, updates reactively when status changes.

*State derivation:*
- `Unlimited` — `status.isConfigured === true`
- `Limited` — `!isConfigured && tasteRemaining === 0`
- `Tasting` — `!isConfigured && tasteRemaining > 0 && tasteRemaining < dailyLimit`
- `Fresh` — `!isConfigured && tasteRemaining === dailyLimit` (never used taste)

### 4. Provider cards use PrimeNG Card + RadioButton

Each provider (Anthropic, OpenAI, Google) rendered as a PrimeNG Card with a radio button for selection. Selecting a card reveals the key input, model dropdown, and deep link. Uses `@switch` control flow (per CLAUDE.md).

### 5. Auto-validate on key paste with debounce

Use `input` event on the API key field with a 500ms debounce. When the user pastes or types a key, auto-call the validate endpoint with the selected provider and model. Show inline status (loading spinner → green check with model name, or red error).

### 6. Nudge dismissal via localStorage with TTL

Contextual (Tasting) nudges dismissed for 7 days per nudge type, stored in localStorage with a timestamp. Blocking (Limited) nudges are not permanently dismissible — only dismissed for the current action.

### 7. Taste counter as a lightweight standalone component

`TasteCounterComponent` is a standalone component showing "N of M free AI operations remaining today". Placed in AI-powered sections by future features. Visibility controlled by `AiNudgeService.nudgeState` — hidden for Unlimited users.

## Risks / Trade-offs

- **[Risk] Auto-validate fires on every keystroke** → Mitigated by 500ms debounce and requiring minimum key length before triggering
- **[Risk] Nudge framework has no consumers yet** → Components are self-contained and inert until future features place them; no wasted overhead
- **[Risk] Taste counter shows stale data if budget changes in another tab** → Acceptable for MVP; can add BroadcastChannel sync later
- **[Trade-off] localStorage for nudge dismissal** → Simple but not synced across devices; acceptable since nudges are low-stakes UI hints

## Open Questions

None — all decisions are straightforward frontend patterns with existing backend support.
