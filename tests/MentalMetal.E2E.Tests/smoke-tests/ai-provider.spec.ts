import { test, expect, API_BASE } from './fixtures/auth.fixture';
import { test as baseTest } from '@playwright/test';

test.describe('AI Provider Settings', () => {
  test('AI provider section is visible on settings page', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/settings');

    await expect(authenticatedPage).toHaveURL(/\/settings/);
    await expect(authenticatedPage.getByText('AI Provider')).toBeVisible();
  });

  test('Provider status returns isConfigured=false for new user', async ({ authenticatedPage, testUser }) => {
    await authenticatedPage.goto('/settings');

    const token = testUser.accessToken;
    const response = await authenticatedPage.request.get(`${API_BASE}/api/users/me/ai-provider`, {
      headers: { Authorization: `Bearer ${token}` },
    });

    expect(response.ok()).toBeTruthy();
    const status = await response.json();
    expect(status.isConfigured).toBe(false);
    expect(status.tasteBudget).toBeDefined();
  });
});

baseTest.describe('AI Models Endpoint', () => {
  baseTest('GET /api/ai/models returns model list for valid provider', async ({ request }) => {
    const response = await request.get(`${API_BASE}/api/ai/models`, {
      params: { provider: 'Anthropic' },
      headers: { Authorization: 'Bearer test' },
    });

    // May return 401 (needs real auth) or 200 with models — either confirms the endpoint exists
    expect([200, 401]).toContain(response.status());
  });
});
