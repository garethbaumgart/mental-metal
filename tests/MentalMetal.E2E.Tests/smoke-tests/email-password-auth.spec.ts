import { test as baseTest, expect, Page } from '@playwright/test';
import { API_BASE } from './fixtures/auth.fixture';

/**
 * Builds an email unique per worker + invocation so tests never collide across
 * retries or parallel shards.
 */
function uniqueEmail(prefix: string, workerIndex: number): string {
  return `e2e-pwd-${prefix}-worker${workerIndex}-${Date.now()}@test.local`;
}

/**
 * Fills the login page in register mode and submits. Assumes we start on /login
 * in default (login) mode — toggles over to register first.
 */
async function submitRegister(
  page: Page,
  email: string,
  name: string,
  password: string,
): Promise<void> {
  await page.goto('/login');

  // Default mode is 'login'; the toggle button is labelled with this text.
  await page.getByRole('button', { name: 'Need an account? Create one' }).click();

  await page.getByLabel('Name').fill(name);
  await page.getByLabel('Email').fill(email);
  // p-password wraps the actual <input type="password"> in a custom element, and
  // the label's for="password" points at the <p-password> host (not the input).
  // Playwright's getByLabel therefore does not resolve the input; select the
  // real input directly via its name attribute.
  const pwdInput = page.locator('input[name="password"]');
  await pwdInput.fill(password);
  // In register mode p-password shows a strength-meter overlay on focus which
  // intercepts clicks on the submit button. Clicking outside the password
  // control (the page heading) is what PrimeNG uses to dismiss the overlay.
  await page.locator('h1').click();

  await page.getByRole('button', { name: 'Create account' }).click();
}

/**
 * Fills the login page in login mode and submits. Assumes we start on /login
 * and the default mode is already 'login'.
 */
async function submitLogin(
  page: Page,
  email: string,
  password: string,
): Promise<void> {
  await page.goto('/login');

  await page.getByLabel('Email').fill(email);
  // See note in submitRegister re: p-password and the label/for association.
  await page.locator('input[name="password"]').fill(password);

  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
}

/**
 * Logs the user out via the API + clears local token state. We do this instead
 * of driving a UI control because the current app shell does not expose a
 * visible logout button.
 */
async function logout(page: Page): Promise<void> {
  const response = await page.request.post(`${API_BASE}/api/auth/logout`);
  expect(response.ok()).toBeTruthy();
  await page.evaluate(() => {
    localStorage.removeItem('access_token');
  });
  await page.context().clearCookies();
}

baseTest.describe('Email/Password Auth — Register', () => {
  baseTest('Registers a new user and lands in the app', async ({ page }, testInfo) => {
    const email = uniqueEmail('register', testInfo.workerIndex);
    const name = `E2E Register ${testInfo.workerIndex}`;
    const password = 'pw-test-12345';

    await submitRegister(page, email, name, password);

    // Login page navigates to '/', which redirects to /dashboard.
    await expect(page).toHaveURL(/\/dashboard/);

    const token = await page.evaluate(() =>
      localStorage.getItem('access_token'),
    );
    expect(token).toBeTruthy();

    const response = await page.request.get(`${API_BASE}/api/users/me`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    expect(response.ok()).toBeTruthy();
    const user = await response.json();
    expect(user.email).toBe(email);
    expect(user.hasPassword).toBe(true);
  });
});

baseTest.describe('Email/Password Auth — Register then Login', () => {
  baseTest('User can log out and sign back in with email + password', async ({
    page,
  }, testInfo) => {
    const email = uniqueEmail('roundtrip', testInfo.workerIndex);
    const name = `E2E Roundtrip ${testInfo.workerIndex}`;
    const password = 'pw-test-12345';

    // 1. Register.
    await submitRegister(page, email, name, password);
    await expect(page).toHaveURL(/\/dashboard/);

    // 2. Log out (API + clear local state — no visible logout UI control).
    await logout(page);

    // Visiting /dashboard without a token should redirect to /login.
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/\/login/);

    // 3. Log back in via email/password.
    await submitLogin(page, email, password);
    await expect(page).toHaveURL(/\/dashboard/);

    const token = await page.evaluate(() =>
      localStorage.getItem('access_token'),
    );
    expect(token).toBeTruthy();
  });
});

baseTest.describe('Email/Password Auth — Add password to Google-only user', () => {
  baseTest('Google-only user can set a password and then log in with it', async ({
    page,
  }, testInfo) => {
    // Build a FRESH Google-like user per invocation. We can't reuse the shared
    // auth fixture's worker-stable email here: once this test runs, that user
    // has a password, so the "Set a password" heading assertion would fail on
    // subsequent runs against the same database. The existing /api/auth/test-login
    // endpoint creates a user with no password — behaviourally equivalent to a
    // Google-only user for the linking flow.
    const email = uniqueEmail('link', testInfo.workerIndex);
    const name = `E2E Link ${testInfo.workerIndex}`;
    const password = `pw-link-${testInfo.workerIndex}-${Date.now()}`;

    const loginResponse = await page.request.post(
      `${API_BASE}/api/auth/test-login`,
      { data: { email, name } },
    );
    expect(loginResponse.ok()).toBeTruthy();
    const loginBody = await loginResponse.json();
    expect(loginBody.accessToken).toBeTruthy();

    // Use a sessionStorage-flagged init script so the token is only seeded on
    // the FIRST navigation — otherwise the logout() step later in this test
    // would be undone by the next goto() re-seeding localStorage.
    await page.addInitScript((token: string) => {
      if (!sessionStorage.getItem('__seededAuthToken')) {
        localStorage.setItem('access_token', token);
        sessionStorage.setItem('__seededAuthToken', '1');
      }
    }, loginBody.accessToken);

    // Navigate to settings — the freshly created user has no password yet, so
    // the password section should prompt to set one.
    await page.goto('/settings');
    await expect(page).toHaveURL(/\/settings/);

    await expect(
      page.getByRole('heading', { name: 'Set a password' }),
    ).toBeVisible();

    // p-password wraps the input; target the real <input> by its name attribute.
    const newPwdInput = page.locator('input[name="newPassword"]');
    await newPwdInput.fill(password);
    // Dismiss the p-password strength-meter overlay by clicking the heading;
    // it otherwise intercepts clicks on the submit button below.
    await page.getByRole('heading', { name: 'Set a password' }).click();
    await page.getByRole('button', { name: 'Set a password' }).click();

    // Success toast from MessageService.
    await expect(page.getByText('Password set')).toBeVisible();

    // Verify via API that hasPassword flipped to true.
    const token = await page.evaluate(() =>
      localStorage.getItem('access_token'),
    );
    const meResponse = await page.request.get(`${API_BASE}/api/users/me`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    expect(meResponse.ok()).toBeTruthy();
    const me = await meResponse.json();
    expect(me.hasPassword).toBe(true);

    // Log out and log back in via email/password using the newly set password.
    await logout(page);

    // Verify the init-script's seed-once guard actually prevents re-seeding
    // on the next navigation — otherwise the logout we just performed would
    // silently be undone and this test would pass for the wrong reason.
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/\/login/);
    const tokenAfterLogout = await page.evaluate(() =>
      localStorage.getItem('access_token'),
    );
    expect(tokenAfterLogout).toBeNull();

    await submitLogin(page, email, password);
    await expect(page).toHaveURL(/\/dashboard/);

    const newToken = await page.evaluate(() =>
      localStorage.getItem('access_token'),
    );
    expect(newToken).toBeTruthy();
  });
});
