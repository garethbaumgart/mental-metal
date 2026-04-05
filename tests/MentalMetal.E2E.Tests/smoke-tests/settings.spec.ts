import { test, expect } from '@playwright/test';

test.describe('Settings Page', () => {
  // Note: Full settings page tests require an authenticated session.
  // These tests verify the page structure when accessed.
  // OAuth login flow cannot be fully E2E tested without a mock OAuth provider.

  test('Settings route redirects to login when unauthenticated', async ({
    page,
  }) => {
    await page.goto('/settings');

    await expect(page).toHaveURL(/\/login/);
  });
});
