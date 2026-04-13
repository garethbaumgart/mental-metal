import { test as authTest, API_BASE } from './fixtures/auth.fixture';
import { expect } from '@playwright/test';

authTest.describe('Living Brief', () => {
  authTest('manually log a decision and see it on the brief', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/dashboard');
    const token = await authenticatedPage.evaluate(() => localStorage.getItem('access_token'));
    const headers = { Authorization: `Bearer ${token}` };

    const initiative = await (await authenticatedPage.request.post(`${API_BASE}/api/initiatives`, {
      headers, data: { title: 'Living Brief Test Initiative' },
    })).json();

    const briefBefore = await (await authenticatedPage.request.get(
      `${API_BASE}/api/initiatives/${initiative.id}/brief`, { headers })).json();
    expect(briefBefore.briefVersion).toBe(0);
    expect(briefBefore.keyDecisions).toEqual([]);

    const briefAfter = await (await authenticatedPage.request.post(
      `${API_BASE}/api/initiatives/${initiative.id}/brief/decisions`,
      { headers, data: { description: 'Adopt Postgres', rationale: 'Performance and JSONB' } })).json();

    expect(briefAfter.briefVersion).toBe(1);
    expect(briefAfter.keyDecisions.length).toBe(1);
    expect(briefAfter.keyDecisions[0].description).toBe('Adopt Postgres');
    expect(briefAfter.keyDecisions[0].source).toBe('Manual');
  });

  authTest('rejecting a pending update leaves the brief unchanged', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/dashboard');
    const token = await authenticatedPage.evaluate(() => localStorage.getItem('access_token'));
    const headers = { Authorization: `Bearer ${token}` };

    const initiative = await (await authenticatedPage.request.post(`${API_BASE}/api/initiatives`, {
      headers, data: { title: 'Reject Test Initiative' },
    })).json();

    // Establish a baseline brief version
    await authenticatedPage.request.post(
      `${API_BASE}/api/initiatives/${initiative.id}/brief/decisions`,
      { headers, data: { description: 'Manual decision' } },
    );

    const beforeBrief = await (await authenticatedPage.request.get(
      `${API_BASE}/api/initiatives/${initiative.id}/brief`, { headers })).json();
    const versionBefore = beforeBrief.briefVersion;

    // Trigger a refresh — likely fails (no AI key) and yields a Failed proposal we can reject.
    const refreshResp = await authenticatedPage.request.post(
      `${API_BASE}/api/initiatives/${initiative.id}/brief/refresh`, { headers, data: {} });
    expect(refreshResp.status()).toBe(202);
    const { pendingUpdateId } = await refreshResp.json();
    expect(pendingUpdateId).toBeTruthy();

    // Poll for the pending update to materialize after background processing.
    const pollUrl = `${API_BASE}/api/initiatives/${initiative.id}/brief/pending-updates/${pendingUpdateId}`;
    let target: any = null;
    for (let i = 0; i < 20; i++) {
      const r = await authenticatedPage.request.get(pollUrl, { headers });
      if (r.status() === 200) { target = await r.json(); break; }
      await authenticatedPage.waitForTimeout(250);
    }
    expect(target).not.toBeNull();

    const rejectResponse = await authenticatedPage.request.post(
      `${API_BASE}/api/initiatives/${initiative.id}/brief/pending-updates/${target.id}/reject`,
      { headers, data: { reason: 'not useful' } });
    // Failed updates can't be rejected (they're terminal) — that's also fine
    expect([204, 409]).toContain(rejectResponse.status());

    const afterBrief = await (await authenticatedPage.request.get(
      `${API_BASE}/api/initiatives/${initiative.id}/brief`, { headers })).json();
    expect(afterBrief.briefVersion).toBe(versionBefore);
  });

  authTest('user isolation: User A cannot read or apply User B pending updates', async ({ browser }) => {
    // Create two distinct contexts (two users)
    const ctxA = await browser.newContext();
    const ctxB = await browser.newContext();
    const pageA = await ctxA.newPage();
    const pageB = await ctxB.newPage();

    const loginAndGetToken = async (page: typeof pageA, suffix: string) => {
      const email = `e2e-isolation-${suffix}@test.local`;
      const r = await page.request.post(`${API_BASE}/api/auth/test-login`, {
        data: { email, name: `Isolation ${suffix}` },
      });
      const body = await r.json();
      return body.accessToken as string;
    };

    const tokenA = await loginAndGetToken(pageA, `a-${Date.now()}`);
    const tokenB = await loginAndGetToken(pageB, `b-${Date.now()}`);

    // User A creates initiative + manual decision (which should NOT be readable by user B)
    const initA = await (await pageA.request.post(`${API_BASE}/api/initiatives`, {
      headers: { Authorization: `Bearer ${tokenA}` },
      data: { title: 'User A Initiative' },
    })).json();

    // User B tries to read User A's brief
    const briefForB = await pageB.request.get(
      `${API_BASE}/api/initiatives/${initA.id}/brief`,
      { headers: { Authorization: `Bearer ${tokenB}` } });
    expect(briefForB.status()).toBe(404);

    // User B tries to list user A's pending updates
    const pendingForB = await pageB.request.get(
      `${API_BASE}/api/initiatives/${initA.id}/brief/pending-updates`,
      { headers: { Authorization: `Bearer ${tokenB}` } });
    expect(pendingForB.status()).toBe(404);

    await ctxA.close();
    await ctxB.close();
  });

  authTest('apply against stale version returns Conflict', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/dashboard');
    const token = await authenticatedPage.evaluate(() => localStorage.getItem('access_token'));
    const headers = { Authorization: `Bearer ${token}` };

    const initiative = await (await authenticatedPage.request.post(`${API_BASE}/api/initiatives`, {
      headers, data: { title: 'Stale Test' },
    })).json();

    // 1. Seed a real pending update at briefVersion=0 via the refresh endpoint.
    const refreshResp = await authenticatedPage.request.post(
      `${API_BASE}/api/initiatives/${initiative.id}/brief/refresh`, { headers, data: {} });
    expect(refreshResp.status()).toBe(202);
    const { pendingUpdateId } = await refreshResp.json();

    // Poll until the background worker materializes the update.
    const pollUrl = `${API_BASE}/api/initiatives/${initiative.id}/brief/pending-updates/${pendingUpdateId}`;
    let appeared = false;
    for (let i = 0; i < 20; i++) {
      const r = await authenticatedPage.request.get(pollUrl, { headers });
      if (r.status() === 200) { appeared = true; break; }
      await authenticatedPage.waitForTimeout(250);
    }
    expect(appeared).toBe(true);

    // 2. Bump the brief version with a manual edit, making the pending update stale.
    const bumped = await (await authenticatedPage.request.post(
      `${API_BASE}/api/initiatives/${initiative.id}/brief/decisions`,
      { headers, data: { description: 'Force version bump' } })).json();
    expect(bumped.briefVersion).toBeGreaterThan(0);

    // 3. Attempting to apply the now-stale proposal MUST return 409.
    const applyResp = await authenticatedPage.request.post(
      `${API_BASE}/api/initiatives/${initiative.id}/brief/pending-updates/${pendingUpdateId}/apply`,
      { headers, data: {} });
    expect(applyResp.status()).toBe(409);
    const body = await applyResp.json();
    expect(body.currentBriefVersion).toBeGreaterThan(body.proposalBriefVersion);
  });
});
