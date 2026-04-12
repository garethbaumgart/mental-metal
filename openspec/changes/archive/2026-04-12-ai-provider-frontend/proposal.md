## Why

The backend AI provider abstraction is complete (endpoints, encryption, taste budget, model catalog) but users have no way to configure their own AI provider or see their taste budget status from the UI. Without the frontend, the entire AI capability is inaccessible to end users. This is the final piece needed before any AI-powered feature (capture extraction, briefings, AI chat) can be built.

## What Changes

- **AI Provider settings section** on the existing settings page — provider selection cards (Anthropic, OpenAI, Google), API key input with auto-validate on paste, model dropdown from config endpoint, save/remove flows, deep links to provider key creation pages
- **AiProviderService** — Angular service for all AI provider API calls (configure, get status, validate, remove, get models)
- **AiNudgeService** — derives nudge state (Fresh/Tasting/Limited/Unlimited) from provider status and taste budget
- **Taste counter component** — subtle remaining-operations indicator for taste users
- **Nudge components** — contextual (Tasting), blocking (Limited), and passive (Fresh) nudges for AI-powered sections
- **E2E tests** — authenticated tests for AI provider configuration and model listing

## Non-goals

- **Backend changes** — all API endpoints already exist and are tested
- **AI-powered features** — capture extraction, briefings, AI chat are separate specs; this provides the configuration layer they depend on
- **Additional OAuth providers** — not in scope
- **Admin/billing** — no provider usage billing or admin dashboards

## Capabilities

### New Capabilities
<!-- None — all frontend requirements already exist in ai-provider-abstraction spec -->

### Modified Capabilities
- `ai-provider-abstraction`: No requirement changes — implementing existing frontend requirements (Frontend AI provider settings, AI setup nudge framework, Taste counter display)

## Impact

- **Frontend**: New `AiProviderService`, `AiNudgeService`, AI provider settings section on settings page, taste counter component, nudge components
- **Settings page**: Extended with AI Provider configuration section below existing profile/preferences
- **App-wide**: Nudge components will be placed in AI-powered sections (placeholder integration points for future features)
- **No backend changes** — consuming existing endpoints only
- **Affected aggregates**: None (frontend only)
- **Tier**: Tier 1 (Foundation) — unblocks all Tier 2 AI features
