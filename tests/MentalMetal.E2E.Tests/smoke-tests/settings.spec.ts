import { test, expect, API_BASE } from './fixtures/auth.fixture';

test.describe('Settings Page', () => {
  test('Settings page loads with user profile data', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/settings');

    await expect(authenticatedPage).toHaveURL(/\/settings/);
    await expect(authenticatedPage.getByRole('heading', { name: 'Settings' })).toBeVisible();
    await expect(authenticatedPage.getByRole('heading', { name: 'Profile' })).toBeVisible();
    await expect(authenticatedPage.getByRole('heading', { name: 'Preferences' })).toBeVisible();
  });

  test('User can update profile name', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/settings');

    await expect(authenticatedPage).toHaveURL(/\/settings/);

    // Update the name field
    const nameInput = authenticatedPage.locator('#name');
    await nameInput.fill('Updated E2E Name');

    // Click Save Profile
    await authenticatedPage.getByRole('button', { name: 'Save Profile' }).click();

    // Wait for success toast
    await expect(authenticatedPage.getByText('Profile updated')).toBeVisible();

    // Verify the API reflects the change
    const token = await authenticatedPage.evaluate(() => localStorage.getItem('access_token'));
    const response = await authenticatedPage.request.get(`${API_BASE}/api/users/me`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    const user = await response.json();
    expect(user.name).toBe('Updated E2E Name');
  });

  test('User can update preferences', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/settings');

    await expect(authenticatedPage).toHaveURL(/\/settings/);

    // Toggle notifications off (default is on)
    const notificationsToggle = authenticatedPage.locator('#notifications');
    await notificationsToggle.click();

    // Click Save Preferences
    await authenticatedPage.getByRole('button', { name: 'Save Preferences' }).click();

    // Wait for success toast
    await expect(authenticatedPage.getByText('Preferences updated')).toBeVisible();

    // Verify the API reflects the change
    const token = await authenticatedPage.evaluate(() => localStorage.getItem('access_token'));
    const response = await authenticatedPage.request.get(`${API_BASE}/api/users/me`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    const user = await response.json();
    expect(user.preferences.notificationsEnabled).toBe(false);
  });

  test('Settings route redirects to login when unauthenticated', async ({
    page,
  }) => {
    // Use bare page (not authenticatedPage) to test unauthenticated access
    await page.goto('/settings');

    await expect(page).toHaveURL(/\/login/);
  });
});
