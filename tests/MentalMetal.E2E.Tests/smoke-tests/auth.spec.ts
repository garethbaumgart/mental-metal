import { test as baseTest, expect } from '@playwright/test';
import { test as authTest, testLogin } from './fixtures/auth.fixture';

baseTest.describe('Authentication — Unauthenticated', () => {
  baseTest('Unauthenticated user is redirected to login page', async ({ page }) => {
    await page.goto('/dashboard');

    // Should redirect to login since there's no auth token
    await expect(page).toHaveURL(/\/login/);
  });

  baseTest('Login page displays sign in button', async ({ page }) => {
    await page.goto('/login');

    await expect(page.getByText('Mental Metal')).toBeVisible();
    await expect(page.getByText('Sign in with Google')).toBeVisible();
  });

  baseTest('Protected API returns 401 without token', async ({ request }) => {
    const response = await request.get('http://localhost:5002/api/users/me');
    expect(response.status()).toBe(401);
  });

  baseTest('Auth refresh endpoint returns 401 without cookie', async ({
    request,
  }) => {
    const response = await request.post('http://localhost:5002/api/auth/refresh');
    expect(response.status()).toBe(401);
  });
});

authTest.describe('Authentication — Login Flow', () => {
  authTest('Authenticated user lands on dashboard after login', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/dashboard');

    // Should stay on dashboard (not redirect to login)
    await expect(authenticatedPage).toHaveURL(/\/dashboard/);
  });

  authTest('Authenticated user can access /api/users/me', async ({ authenticatedPage }) => {
    const response = await authenticatedPage.request.get('http://localhost:5002/api/users/me', {
      headers: {
        Authorization: `Bearer ${await authenticatedPage.evaluate(() => localStorage.getItem('access_token'))}`,
      },
    });

    expect(response.ok()).toBeTruthy();
    const user = await response.json();
    expect(user.email).toBe('e2e@test.local');
    expect(user.name).toBe('E2E Test User');
  });
});
