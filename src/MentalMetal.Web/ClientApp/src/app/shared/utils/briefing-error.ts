import { HttpErrorResponse } from '@angular/common/http';

/**
 * Canonical states for a briefing-generation failure. Both the morning
 * briefing widget and the weekly briefing page use this so they render
 * the same error affordances (server message + a settings link) when the
 * AI provider fails or the user is rate-limited.
 *
 * Note: the briefing endpoints are fronted by a middleware in Program.cs
 * that sanitizes `502 Bad Gateway` responses to a fixed user-safe message
 * (we do not expose raw provider exceptions). `message` is therefore the
 * backend's pre-sanitized string, not the underlying provider error.
 */
export type BriefingErrorState =
  | { kind: 'notConfigured' }
  | { kind: 'providerError'; message: string }
  | { kind: 'rateLimit'; message: string }
  | { kind: 'generic'; message: string };

const DEFAULT_GENERIC = 'Failed to generate briefing.';

/**
 * Classify an HTTP error from the briefing endpoints into an actionable
 * state so callers can decide which hint to render (e.g. settings link
 * for provider/rate-limit failures).
 */
export function classifyBriefingError(err: unknown): BriefingErrorState {
  if (!(err instanceof HttpErrorResponse)) {
    return { kind: 'generic', message: DEFAULT_GENERIC };
  }

  const body = err.error as { code?: string; error?: string } | null | undefined;

  if (err.status === 409 && body?.code === 'ai_provider_not_configured') {
    return { kind: 'notConfigured' };
  }

  if (err.status === 429) {
    return {
      kind: 'rateLimit',
      message: body?.error?.trim() || 'AI request rate limit reached. Try again later.',
    };
  }

  if (err.status === 502) {
    return {
      kind: 'providerError',
      message: body?.error?.trim() || 'AI provider request failed.',
    };
  }

  return { kind: 'generic', message: DEFAULT_GENERIC };
}
