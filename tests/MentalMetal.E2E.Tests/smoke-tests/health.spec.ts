import { test, expect } from '@playwright/test';

test.describe('Health Check', () => {
  test('API health endpoint returns healthy', async ({ request }) => {
    const response = await request.get('http://localhost:5002/api/health');
    expect(response.ok()).toBeTruthy();
  });

  test('Frontend loads successfully', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveTitle(/Mental Metal/);
  });
});
