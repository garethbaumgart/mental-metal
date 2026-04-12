import { test as base, expect, Page } from '@playwright/test';

export const API_BASE = process.env.API_BASE_URL || 'http://localhost:5002';

interface TestLoginResult {
  accessToken: string;
  email: string;
  name: string;
}

/**
 * Logs in via the test-only /api/auth/test-login endpoint (Development only).
 * Sets the access token in localStorage so the Angular AuthService picks it up.
 * Each worker gets a unique user to avoid cross-test interference.
 */
async function testLogin(
  page: Page,
  workerIndex: number,
): Promise<TestLoginResult> {
  const email = `e2e-worker${workerIndex}@test.local`;
  const name = `E2E Worker ${workerIndex}`;

  // Call the test-login API directly to get tokens + set refresh cookie
  const response = await page.request.post(`${API_BASE}/api/auth/test-login`, {
    data: { email, name },
  });

  expect(response.ok()).toBeTruthy();
  const body = await response.json();
  expect(body.accessToken).toBeTruthy();

  // Set the token in localStorage before navigating, so AuthService finds it
  await page.addInitScript((token: string) => {
    localStorage.setItem('access_token', token);
  }, body.accessToken);

  return { accessToken: body.accessToken, email, name };
}

/** Playwright test fixture that provides an authenticated page with user info. */
export const test = base.extend<{
  authenticatedPage: Page;
  testUser: { email: string; name: string; accessToken: string };
}>({
  testUser: [async ({ page }, use, testInfo) => {
    const result = await testLogin(page, testInfo.workerIndex);
    await use(result);
  }, { scope: 'test' }],
  authenticatedPage: async ({ page, testUser }, use) => {
    await use(page);
  },
});

export { expect, testLogin, API_BASE as apiBase };
