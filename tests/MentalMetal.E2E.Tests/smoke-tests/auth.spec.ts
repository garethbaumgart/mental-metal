import { test as baseTest, expect } from '@playwright/test';
import { test as authTest, API_BASE } from './fixtures/auth.fixture';

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
    const response = await request.get(`${API_BASE}/api/users/me`);
    expect(response.status()).toBe(401);
  });

  baseTest('Auth refresh endpoint returns 401 without cookie', async ({
    request,
  }) => {
    const response = await request.post(`${API_BASE}/api/auth/refresh`);
    expect(response.status()).toBe(401);
  });
});

authTest.describe('Authentication — Login Flow', () => {
  authTest('Authenticated user lands on dashboard after login', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/dashboard');

    // Should stay on dashboard (not redirect to login)
    await expect(authenticatedPage).toHaveURL(/\/dashboard/);
  });

  authTest('Authenticated user can access /api/users/me', async ({ authenticatedPage, testUser }) => {
    // Navigate first so localStorage init script runs for app origin
    await authenticatedPage.goto('/dashboard');
    await expect(authenticatedPage).toHaveURL(/\/dashboard/);

    const token = await authenticatedPage.evaluate(() => localStorage.getItem('access_token'));
    const response = await authenticatedPage.request.get(`${API_BASE}/api/users/me`, {
      headers: { Authorization: `Bearer ${token}` },
    });

    expect(response.ok()).toBeTruthy();
    const user = await response.json();
    expect(user.email).toBe(testUser.email);
    expect(user.name).toBe(testUser.name);
  });
});
