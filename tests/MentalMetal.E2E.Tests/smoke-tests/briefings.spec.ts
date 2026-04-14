import { test as authTest, API_BASE } from './fixtures/auth.fixture';
import { expect } from '@playwright/test';

authTest.describe('Briefings', () => {
  authTest('dashboard widget renders for an authenticated user', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/dashboard');
    // The Morning briefing widget renders its heading regardless of AI config state.
    await expect(authenticatedPage.getByRole('heading', { name: 'Morning briefing' })).toBeVisible();
  });

  authTest('weekly briefing page is reachable and renders heading', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/briefings/weekly');
    await expect(authenticatedPage.getByRole('heading', { name: 'Weekly briefing' })).toBeVisible();
  });

  authTest('morning endpoint returns 409 when no AI provider is configured', async ({ authenticatedPage }) => {
    // localStorage is only accessible after navigating to a same-origin page;
    // matching the pattern used by the other auth-aware specs.
    await authenticatedPage.goto('/dashboard');
    const token = await authenticatedPage.evaluate(() => localStorage.getItem('access_token'));
    const headers = { Authorization: `Bearer ${token}` };

    // E2E users have no AI provider configured by default - the endpoint should
    // surface that with HTTP 409 + the well-known error code.
    const resp = await authenticatedPage.request.post(`${API_BASE}/api/briefings/morning`, {
      headers,
    });
    expect(resp.status()).toBe(409);
    const body = await resp.json();
    expect(body.code).toBe('ai_provider_not_configured');
  });
});
