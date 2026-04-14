import { test, expect, API_BASE } from './fixtures/auth.fixture';

test.describe('AI Provider Settings', () => {
  test('AI provider section is visible on settings page', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/settings');

    await expect(authenticatedPage).toHaveURL(/\/settings/);
    await expect(authenticatedPage.getByRole('heading', { name: 'AI Provider' })).toBeVisible();
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

test.describe('AI Models Endpoint', () => {
  test('GET /api/ai/models returns model list for valid provider', async ({ authenticatedPage, testUser }) => {
    const response = await authenticatedPage.request.get(`${API_BASE}/api/ai/models`, {
      params: { provider: 'Anthropic' },
      headers: { Authorization: `Bearer ${testUser.accessToken}` },
    });

    expect(response.ok()).toBeTruthy();
    const body = await response.json();
    expect(body.provider).toBe('Anthropic');
    expect(body.models).toBeDefined();
    expect(body.models.length).toBeGreaterThan(0);
  });
});
