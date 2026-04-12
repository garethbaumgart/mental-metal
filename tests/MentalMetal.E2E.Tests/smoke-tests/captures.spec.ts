import { test as authTest, API_BASE } from './fixtures/auth.fixture';
import { expect } from '@playwright/test';

authTest.describe('Captures', () => {
  // 8.1 E2E test: create a capture and verify it appears in the list
  authTest('Create a capture and verify it appears in the list', async ({ authenticatedPage, testUser }) => {
    await authenticatedPage.goto('/dashboard');
    await expect(authenticatedPage).toHaveURL(/\/dashboard/);

    const token = await authenticatedPage.evaluate(() => localStorage.getItem('access_token'));

    // Create a capture via API
    const createResponse = await authenticatedPage.request.post(`${API_BASE}/api/captures`, {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        rawContent: 'Follow up with Sarah on Q3 roadmap',
        type: 'QuickNote',
      },
    });

    expect(createResponse.status()).toBe(201);
    const created = await createResponse.json();
    expect(created.id).toBeTruthy();
    expect(created.rawContent).toBe('Follow up with Sarah on Q3 roadmap');
    expect(created.captureType).toBe('QuickNote');
    expect(created.processingStatus).toBe('Raw');

    // List captures and verify the new one appears
    const listResponse = await authenticatedPage.request.get(`${API_BASE}/api/captures`, {
      headers: { Authorization: `Bearer ${token}` },
    });

    expect(listResponse.ok()).toBeTruthy();
    const captures = await listResponse.json();
    expect(captures.some((c: { id: string }) => c.id === created.id)).toBeTruthy();
  });

  // 8.2 E2E test: link a capture to a person and verify the link
  authTest('Link a capture to a person and verify the link', async ({ authenticatedPage, testUser }) => {
    await authenticatedPage.goto('/dashboard');
    await expect(authenticatedPage).toHaveURL(/\/dashboard/);

    const token = await authenticatedPage.evaluate(() => localStorage.getItem('access_token'));

    // Create a person
    const personResponse = await authenticatedPage.request.post(`${API_BASE}/api/people`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { name: 'Sarah Test', type: 'Stakeholder' },
    });
    expect(personResponse.status()).toBe(201);
    const person = await personResponse.json();

    // Create a capture
    const captureResponse = await authenticatedPage.request.post(`${API_BASE}/api/captures`, {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        rawContent: 'Notes from meeting with Sarah',
        type: 'MeetingNotes',
      },
    });
    expect(captureResponse.status()).toBe(201);
    const capture = await captureResponse.json();

    // Link the capture to the person
    const linkResponse = await authenticatedPage.request.post(
      `${API_BASE}/api/captures/${capture.id}/link-person`,
      {
        headers: { Authorization: `Bearer ${token}` },
        data: { personId: person.id },
      },
    );
    expect(linkResponse.ok()).toBeTruthy();
    const linked = await linkResponse.json();
    expect(linked.linkedPersonIds).toContain(person.id);

    // Verify the link persists by fetching the capture
    const getResponse = await authenticatedPage.request.get(
      `${API_BASE}/api/captures/${capture.id}`,
      { headers: { Authorization: `Bearer ${token}` } },
    );
    expect(getResponse.ok()).toBeTruthy();
    const fetched = await getResponse.json();
    expect(fetched.linkedPersonIds).toContain(person.id);
  });

  // 8.3 E2E test: user isolation — captures are scoped per user
  authTest('User isolation — captures are scoped per user', async ({ browser }) => {
    // Create two separate browser contexts for two different users
    const context1 = await browser.newContext();
    const page1 = await context1.newPage();
    const context2 = await browser.newContext();
    const page2 = await context2.newPage();

    // Login as user 1
    const login1 = await page1.request.post(`${API_BASE}/api/auth/test-login`, {
      data: { email: 'isolation-user1@test.local', name: 'User One' },
    });
    expect(login1.ok()).toBeTruthy();
    const token1 = (await login1.json()).accessToken;

    // Login as user 2
    const login2 = await page2.request.post(`${API_BASE}/api/auth/test-login`, {
      data: { email: 'isolation-user2@test.local', name: 'User Two' },
    });
    expect(login2.ok()).toBeTruthy();
    const token2 = (await login2.json()).accessToken;

    // User 1 creates a capture
    const createResponse = await page1.request.post(`${API_BASE}/api/captures`, {
      headers: { Authorization: `Bearer ${token1}` },
      data: {
        rawContent: 'User 1 private note',
        type: 'QuickNote',
      },
    });
    expect(createResponse.status()).toBe(201);
    const user1Capture = await createResponse.json();

    // User 2 lists captures — should NOT see user 1's capture
    const listResponse = await page2.request.get(`${API_BASE}/api/captures`, {
      headers: { Authorization: `Bearer ${token2}` },
    });
    expect(listResponse.ok()).toBeTruthy();
    const user2Captures = await listResponse.json();
    expect(user2Captures.every((c: { id: string }) => c.id !== user1Capture.id)).toBeTruthy();

    // User 2 tries to get user 1's capture directly — should get 404
    const getResponse = await page2.request.get(
      `${API_BASE}/api/captures/${user1Capture.id}`,
      { headers: { Authorization: `Bearer ${token2}` } },
    );
    expect(getResponse.status()).toBe(404);

    await context1.close();
    await context2.close();
  });
});
