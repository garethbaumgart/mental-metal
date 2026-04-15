import { describe, it, expect } from 'vitest';
import { HttpErrorResponse } from '@angular/common/http';
import { classifyBriefingError } from './briefing-error';

describe('classifyBriefingError', () => {
  it('returns notConfigured on 409 with matching code', () => {
    const err = new HttpErrorResponse({
      status: 409,
      error: { code: 'ai_provider_not_configured', error: 'not configured' },
    });
    expect(classifyBriefingError(err)).toEqual({ kind: 'notConfigured' });
  });

  it('returns providerError on 502 with server message', () => {
    const err = new HttpErrorResponse({
      status: 502,
      error: { error: 'Gemini API error (BadRequest): invalid key' },
    });
    expect(classifyBriefingError(err)).toEqual({
      kind: 'providerError',
      message: 'Gemini API error (BadRequest): invalid key',
    });
  });

  it('falls back to default provider message when body is empty', () => {
    const err = new HttpErrorResponse({ status: 502, error: null });
    expect(classifyBriefingError(err)).toEqual({
      kind: 'providerError',
      message: 'AI provider request failed.',
    });
  });

  it('returns rateLimit on 429', () => {
    const err = new HttpErrorResponse({
      status: 429,
      error: { error: 'Daily limit exceeded' },
    });
    expect(classifyBriefingError(err)).toEqual({
      kind: 'rateLimit',
      message: 'Daily limit exceeded',
    });
  });

  it('returns generic on other HTTP errors', () => {
    const err = new HttpErrorResponse({ status: 500, error: 'boom' });
    expect(classifyBriefingError(err)).toEqual({
      kind: 'generic',
      message: 'Failed to generate briefing.',
    });
  });

  it('returns generic for non-HttpErrorResponse', () => {
    expect(classifyBriefingError(new Error('sync'))).toEqual({
      kind: 'generic',
      message: 'Failed to generate briefing.',
    });
  });
});
