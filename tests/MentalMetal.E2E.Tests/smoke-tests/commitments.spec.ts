import { test as authTest, API_BASE } from './fixtures/auth.fixture';
import { expect } from '@playwright/test';

// V2: Manual commitment creation removed. Commitments are auto-extracted
// from captures by the AI pipeline (Phase D). E2E tests for commitment
// lifecycle (complete, dismiss, reopen) will be added after the extraction
// pipeline is implemented. For now, we verify the list endpoint works.

authTest.describe('Commitments', () => {
  authTest('List endpoint returns 200 for authenticated user', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/dashboard');
    const token = await authenticatedPage.evaluate(() => localStorage.getItem('access_token'));

    const listResponse = await authenticatedPage.request.get(`${API_BASE}/api/commitments`, {
      headers: { Authorization: `Bearer ${token}` },
    });

    expect(listResponse.ok()).toBeTruthy();
    const commitments = await listResponse.json();
    expect(Array.isArray(commitments)).toBeTruthy();
  });
});
