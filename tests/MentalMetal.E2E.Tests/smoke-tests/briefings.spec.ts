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

  authTest('morning endpoint is reachable for an authenticated user', async ({ authenticatedPage }) => {
    // localStorage is only accessible after navigating to a same-origin page;
    // matching the pattern used by the other auth-aware specs.
    await authenticatedPage.goto('/dashboard');
    const token = await authenticatedPage.evaluate(() => localStorage.getItem('access_token'));
    // Guard against a null token: without it the request is unauthenticated and
    // we'd see a 401 instead of the intended error path, masking real regressions.
    expect(token).toBeTruthy();
    const headers = { Authorization: `Bearer ${token}` };

    // E2E users have no AI provider configured. The dev-stack ships with a
    // placeholder taste-key whose upstream call fails, so we cannot assert a
    // single status code here - upstream failures produce 502, missing-config
    // produces 409. Either is acceptable: this smoke test only asserts the
    // endpoint is wired (auth passes, route resolves) and never returns 200/201.
    const resp = await authenticatedPage.request.post(`${API_BASE}/api/briefings/morning`, {
      headers,
    });
    expect([409, 500, 502, 503]).toContain(resp.status());
  });
});
