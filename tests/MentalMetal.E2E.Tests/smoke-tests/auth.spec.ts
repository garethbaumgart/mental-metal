import { test, expect } from '@playwright/test';

test.describe('Authentication', () => {
  test('Unauthenticated user is redirected to login page', async ({ page }) => {
    await page.goto('/dashboard');

    // Should redirect to login since there's no auth token
    await expect(page).toHaveURL(/\/login/);
  });

  test('Login page displays sign in button', async ({ page }) => {
    await page.goto('/login');

    await expect(page.getByText('Mental Metal')).toBeVisible();
    await expect(page.getByText('Sign in with Google')).toBeVisible();
  });

  test('Protected API returns 401 without token', async ({ request }) => {
    const response = await request.get('http://localhost:5002/api/users/me');
    expect(response.status()).toBe(401);
  });

  test('Auth refresh endpoint returns 401 without cookie', async ({
    request,
  }) => {
    const response = await request.post('http://localhost:5002/api/auth/refresh');
    expect(response.status()).toBe(401);
  });
});
