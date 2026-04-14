import { test, expect } from '@playwright/test';

test.describe('Health Check', () => {
  test('Unmatched API routes return 404', async ({ request }) => {
    const response = await request.get('http://localhost:5002/api/does-not-exist');
    expect(response.status()).toBe(404);
  });

  test('Health endpoint returns 200', async ({ request }) => {
    const response = await request.get('http://localhost:5002/api/health');
    expect(response.status()).toBe(200);
  });

  test('Frontend loads successfully', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveTitle(/Mental Metal/);
  });
});
