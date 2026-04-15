import { HttpErrorResponse } from '@angular/common/http';

/**
 * Canonical states for a briefing-generation failure. Both the morning
 * briefing widget and the weekly briefing page use this so users get the
 * same diagnostic information (server-provided message + a settings link)
 * when the underlying AI provider fails.
 */
export type BriefingErrorState =
  | { kind: 'notConfigured' }
  | { kind: 'providerError'; message: string }
  | { kind: 'rateLimit'; message: string }
  | { kind: 'generic'; message: string };

const DEFAULT_GENERIC = 'Failed to generate briefing.';

/**
 * Classify an HTTP error from the briefing endpoints into an actionable
 * state. Surfaces the backend's `error` field when the AI provider itself
 * failed, so the user sees *why* rather than a generic message.
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
