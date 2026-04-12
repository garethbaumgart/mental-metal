import { test as authTest, API_BASE } from './fixtures/auth.fixture';
import { expect } from '@playwright/test';

authTest.describe('Delegations', () => {
  // 8.1 E2E test: create a delegation and verify it appears in the list
  authTest('Create a delegation and verify it appears in the list', async ({ authenticatedPage, testUser }) => {
    await authenticatedPage.goto('/dashboard');
    await expect(authenticatedPage).toHaveURL(/\/dashboard/);

    const token = await authenticatedPage.evaluate(() => localStorage.getItem('access_token'));

    // Create a person first (DelegatePersonId is required)
    const personResponse = await authenticatedPage.request.post(`${API_BASE}/api/people`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { name: 'Sarah Delegation Test', type: 'DirectReport' },
    });
    expect(personResponse.status()).toBe(201);
    const person = await personResponse.json();

    // Create a delegation
    const createResponse = await authenticatedPage.request.post(`${API_BASE}/api/delegations`, {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        description: 'Write the API spec for payments service',
        delegatePersonId: person.id,
      },
    });

    expect(createResponse.status()).toBe(201);
    const created = await createResponse.json();
    expect(created.id).toBeTruthy();
    expect(created.description).toBe('Write the API spec for payments service');
    expect(created.status).toBe('Assigned');
    expect(created.priority).toBe('Medium');
    expect(created.delegatePersonId).toBe(person.id);
    expect(created.lastFollowedUpAt).toBeNull();

    // List delegations and verify the new one appears
    const listResponse = await authenticatedPage.request.get(`${API_BASE}/api/delegations`, {
      headers: { Authorization: `Bearer ${token}` },
    });

    expect(listResponse.ok()).toBeTruthy();
    const delegations = await listResponse.json();
    expect(delegations.some((d: { id: string }) => d.id === created.id)).toBeTruthy();
  });

  // 8.2 E2E test: transition delegation through status lifecycle
  authTest('Transition delegation through status lifecycle (Assigned -> InProgress -> Completed)', async ({ authenticatedPage, testUser }) => {
    await authenticatedPage.goto('/dashboard');
    await expect(authenticatedPage).toHaveURL(/\/dashboard/);

    const token = await authenticatedPage.evaluate(() => localStorage.getItem('access_token'));

    // Create a person
    const personResponse = await authenticatedPage.request.post(`${API_BASE}/api/people`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { name: 'Bob Delegation Test', type: 'DirectReport' },
    });
    expect(personResponse.status()).toBe(201);
    const person = await personResponse.json();

    // Create a delegation
    const createResponse = await authenticatedPage.request.post(`${API_BASE}/api/delegations`, {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        description: 'Deliver design doc',
        delegatePersonId: person.id,
        dueDate: '2026-05-01',
        priority: 'High',
      },
    });
    expect(createResponse.status()).toBe(201);
    const created = await createResponse.json();
    expect(created.priority).toBe('High');

    // Start the delegation
    const startResponse = await authenticatedPage.request.post(
      `${API_BASE}/api/delegations/${created.id}/start`,
      { headers: { Authorization: `Bearer ${token}` } },
    );
    expect(startResponse.ok()).toBeTruthy();
    const started = await startResponse.json();
    expect(started.status).toBe('InProgress');

    // Cannot start again (409 Conflict)
    const doubleStart = await authenticatedPage.request.post(
      `${API_BASE}/api/delegations/${created.id}/start`,
      { headers: { Authorization: `Bearer ${token}` } },
    );
    expect(doubleStart.status()).toBe(409);

    // Complete the delegation
    const completeResponse = await authenticatedPage.request.post(
      `${API_BASE}/api/delegations/${created.id}/complete`,
      {
        headers: { Authorization: `Bearer ${token}` },
        data: { notes: 'Delivered on time' },
      },
    );
    expect(completeResponse.ok()).toBeTruthy();
    const completed = await completeResponse.json();
    expect(completed.status).toBe('Completed');
    expect(completed.completedAt).toBeTruthy();
    expect(completed.notes).toContain('Delivered on time');

    // Verify cannot complete again (409 Conflict)
    const doubleComplete = await authenticatedPage.request.post(
      `${API_BASE}/api/delegations/${created.id}/complete`,
      {
        headers: { Authorization: `Bearer ${token}` },
        data: {},
      },
    );
    expect(doubleComplete.status()).toBe(409);
  });

  // 8.3 E2E test: user isolation — delegations are scoped per user
  authTest('User isolation — delegations are scoped per user', async ({ browser }) => {
    const context1 = await browser.newContext();
    const context2 = await browser.newContext();

    try {
      const page1 = await context1.newPage();
      const page2 = await context2.newPage();

      // Login as user 1
      const login1 = await page1.request.post(`${API_BASE}/api/auth/test-login`, {
        data: { email: 'delegation-isolation-user1@test.local', name: 'User One' },
      });
      expect(login1.ok()).toBeTruthy();
      const token1 = (await login1.json()).accessToken;

      // Login as user 2
      const login2 = await page2.request.post(`${API_BASE}/api/auth/test-login`, {
        data: { email: 'delegation-isolation-user2@test.local', name: 'User Two' },
      });
      expect(login2.ok()).toBeTruthy();
      const token2 = (await login2.json()).accessToken;

      // User 1 creates a person
      const personResponse = await page1.request.post(`${API_BASE}/api/people`, {
        headers: { Authorization: `Bearer ${token1}` },
        data: { name: 'Isolation Delegate', type: 'DirectReport' },
      });
      expect(personResponse.status()).toBe(201);
      const person = await personResponse.json();

      // User 1 creates a delegation
      const createResponse = await page1.request.post(`${API_BASE}/api/delegations`, {
        headers: { Authorization: `Bearer ${token1}` },
        data: {
          description: 'User 1 private delegation',
          delegatePersonId: person.id,
        },
      });
      expect(createResponse.status()).toBe(201);
      const user1Delegation = await createResponse.json();

      // User 2 lists delegations — should NOT see user 1's delegation
      const listResponse = await page2.request.get(`${API_BASE}/api/delegations`, {
        headers: { Authorization: `Bearer ${token2}` },
      });
      expect(listResponse.ok()).toBeTruthy();
      const user2Delegations = await listResponse.json();
      expect(user2Delegations.every((d: { id: string }) => d.id !== user1Delegation.id)).toBeTruthy();

      // User 2 tries to get user 1's delegation directly — should get 404
      const getResponse = await page2.request.get(
        `${API_BASE}/api/delegations/${user1Delegation.id}`,
        { headers: { Authorization: `Bearer ${token2}` } },
      );
      expect(getResponse.status()).toBe(404);
    } finally {
      await context1.close();
      await context2.close();
    }
  });
});
