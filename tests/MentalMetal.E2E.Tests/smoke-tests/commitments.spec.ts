import { test as authTest, API_BASE } from './fixtures/auth.fixture';
import { expect } from '@playwright/test';

authTest.describe('Commitments', () => {
  // 8.1 E2E test: create a commitment and verify it appears in the list
  authTest('Create a commitment and verify it appears in the list', async ({ authenticatedPage, testUser }) => {
    await authenticatedPage.goto('/dashboard');
    await expect(authenticatedPage).toHaveURL(/\/dashboard/);

    const token = await authenticatedPage.evaluate(() => localStorage.getItem('access_token'));

    // Create a person first (PersonId is required)
    const personResponse = await authenticatedPage.request.post(`${API_BASE}/api/people`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { name: 'Sarah Commitment Test', type: 'Stakeholder' },
    });
    expect(personResponse.status()).toBe(201);
    const person = await personResponse.json();

    // Create a commitment
    const createResponse = await authenticatedPage.request.post(`${API_BASE}/api/commitments`, {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        description: 'Send Q3 roadmap draft',
        direction: 'MineToThem',
        personId: person.id,
      },
    });

    expect(createResponse.status()).toBe(201);
    const created = await createResponse.json();
    expect(created.id).toBeTruthy();
    expect(created.description).toBe('Send Q3 roadmap draft');
    expect(created.direction).toBe('MineToThem');
    expect(created.status).toBe('Open');
    expect(created.personId).toBe(person.id);
    expect(created.isOverdue).toBe(false);

    // List commitments and verify the new one appears
    const listResponse = await authenticatedPage.request.get(`${API_BASE}/api/commitments`, {
      headers: { Authorization: `Bearer ${token}` },
    });

    expect(listResponse.ok()).toBeTruthy();
    const commitments = await listResponse.json();
    expect(commitments.some((c: { id: string }) => c.id === created.id)).toBeTruthy();
  });

  // 8.2 E2E test: complete and reopen a commitment
  authTest('Complete and reopen a commitment', async ({ authenticatedPage, testUser }) => {
    await authenticatedPage.goto('/dashboard');
    await expect(authenticatedPage).toHaveURL(/\/dashboard/);

    const token = await authenticatedPage.evaluate(() => localStorage.getItem('access_token'));

    // Create a person
    const personResponse = await authenticatedPage.request.post(`${API_BASE}/api/people`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { name: 'Bob Commitment Test', type: 'DirectReport' },
    });
    expect(personResponse.status()).toBe(201);
    const person = await personResponse.json();

    // Create a commitment
    const createResponse = await authenticatedPage.request.post(`${API_BASE}/api/commitments`, {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        description: 'Deliver design doc',
        direction: 'TheirsToMe',
        personId: person.id,
        dueDate: '2026-05-01',
      },
    });
    expect(createResponse.status()).toBe(201);
    const created = await createResponse.json();

    // Complete the commitment
    const completeResponse = await authenticatedPage.request.post(
      `${API_BASE}/api/commitments/${created.id}/complete`,
      {
        headers: { Authorization: `Bearer ${token}` },
        data: { notes: 'Delivered in leadership sync' },
      },
    );
    expect(completeResponse.ok()).toBeTruthy();
    const completed = await completeResponse.json();
    expect(completed.status).toBe('Completed');
    expect(completed.completedAt).toBeTruthy();
    expect(completed.notes).toContain('Delivered in leadership sync');

    // Verify cannot complete again (409 Conflict)
    const doubleComplete = await authenticatedPage.request.post(
      `${API_BASE}/api/commitments/${created.id}/complete`,
      {
        headers: { Authorization: `Bearer ${token}` },
        data: {},
      },
    );
    expect(doubleComplete.status()).toBe(409);

    // Reopen the commitment
    const reopenResponse = await authenticatedPage.request.post(
      `${API_BASE}/api/commitments/${created.id}/reopen`,
      {
        headers: { Authorization: `Bearer ${token}` },
        data: {},
      },
    );
    expect(reopenResponse.ok()).toBeTruthy();
    const reopened = await reopenResponse.json();
    expect(reopened.status).toBe('Open');
    expect(reopened.completedAt).toBeNull();
  });

  // 8.3 E2E test: user isolation — commitments are scoped per user
  authTest('User isolation — commitments are scoped per user', async ({ browser }) => {
    const context1 = await browser.newContext();
    const page1 = await context1.newPage();
    const context2 = await browser.newContext();
    const page2 = await context2.newPage();

    // Login as user 1
    const login1 = await page1.request.post(`${API_BASE}/api/auth/test-login`, {
      data: { email: 'commitment-isolation-user1@test.local', name: 'User One' },
    });
    expect(login1.ok()).toBeTruthy();
    const token1 = (await login1.json()).accessToken;

    // Login as user 2
    const login2 = await page2.request.post(`${API_BASE}/api/auth/test-login`, {
      data: { email: 'commitment-isolation-user2@test.local', name: 'User Two' },
    });
    expect(login2.ok()).toBeTruthy();
    const token2 = (await login2.json()).accessToken;

    // User 1 creates a person
    const personResponse = await page1.request.post(`${API_BASE}/api/people`, {
      headers: { Authorization: `Bearer ${token1}` },
      data: { name: 'Isolation Person', type: 'Stakeholder' },
    });
    expect(personResponse.status()).toBe(201);
    const person = await personResponse.json();

    // User 1 creates a commitment
    const createResponse = await page1.request.post(`${API_BASE}/api/commitments`, {
      headers: { Authorization: `Bearer ${token1}` },
      data: {
        description: 'User 1 private commitment',
        direction: 'MineToThem',
        personId: person.id,
      },
    });
    expect(createResponse.status()).toBe(201);
    const user1Commitment = await createResponse.json();

    // User 2 lists commitments — should NOT see user 1's commitment
    const listResponse = await page2.request.get(`${API_BASE}/api/commitments`, {
      headers: { Authorization: `Bearer ${token2}` },
    });
    expect(listResponse.ok()).toBeTruthy();
    const user2Commitments = await listResponse.json();
    expect(user2Commitments.every((c: { id: string }) => c.id !== user1Commitment.id)).toBeTruthy();

    // User 2 tries to get user 1's commitment directly — should get 404
    const getResponse = await page2.request.get(
      `${API_BASE}/api/commitments/${user1Commitment.id}`,
      { headers: { Authorization: `Bearer ${token2}` } },
    );
    expect(getResponse.status()).toBe(404);

    await context1.close();
    await context2.close();
  });
});
