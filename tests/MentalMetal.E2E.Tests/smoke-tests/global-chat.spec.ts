import { test as authTest, API_BASE, testLogin } from './fixtures/auth.fixture';
import { expect } from '@playwright/test';

authTest.describe('Global AI chat', () => {
  authTest('start a global thread, post a message, receive a reply with both messages persisted', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/dashboard');
    const token = await authenticatedPage.evaluate(() => localStorage.getItem('access_token'));
    const headers = { Authorization: `Bearer ${token}` };

    const start = await authenticatedPage.request.post(`${API_BASE}/api/chat/threads`, { headers });
    expect(start.status()).toBe(201);
    const thread = await start.json();
    expect(thread.contextScopeType).toBe('Global');
    expect(thread.status).toBe('Active');

    // Posts "What's overdue?" — covers task 13.1. The classifier rule lights OverdueWork; the
    // assistant reply may be a friendly Assistant fallback or System (taste-limit) message.
    const post = await authenticatedPage.request.post(
      `${API_BASE}/api/chat/threads/${thread.id}/messages`,
      { headers, data: { content: "What's overdue?" } });
    expect(post.status()).toBe(200);
    const reply = await post.json();
    expect(reply.userMessage.role).toBe('User');
    expect(['Assistant', 'System']).toContain(reply.assistantMessage.role);

    const fresh = await (await authenticatedPage.request.get(
      `${API_BASE}/api/chat/threads/${thread.id}`, { headers })).json();
    expect(fresh.messages.length).toBe(2);
    expect(fresh.title).toBe("What's overdue?");
  });

  authTest('listing only returns global threads (initiative threads excluded)', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/dashboard');
    const token = await authenticatedPage.evaluate(() => localStorage.getItem('access_token'));
    const headers = { Authorization: `Bearer ${token}` };

    // Create one of each kind.
    const initiative = await (await authenticatedPage.request.post(`${API_BASE}/api/initiatives`, {
      headers, data: { title: 'Global Chat Test Initiative' },
    })).json();
    await authenticatedPage.request.post(`${API_BASE}/api/initiatives/${initiative.id}/chat/threads`, { headers });
    const globalThread = await (await authenticatedPage.request.post(`${API_BASE}/api/chat/threads`, { headers })).json();

    const globalList = await (await authenticatedPage.request.get(`${API_BASE}/api/chat/threads`, { headers })).json();
    expect(globalList.find((t: any) => t.id === globalThread.id)).toBeDefined();
    // Wrong-scope thread fetch via /api/chat returns 404.
    const initThreadList = await (await authenticatedPage.request.get(
      `${API_BASE}/api/initiatives/${initiative.id}/chat/threads`, { headers })).json();
    const initThreadId = initThreadList[0].id;
    const wrongScope = await authenticatedPage.request.get(`${API_BASE}/api/chat/threads/${initThreadId}`, { headers });
    expect(wrongScope.status()).toBe(404);
  });

  authTest('archive then unarchive a global thread', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/dashboard');
    const token = await authenticatedPage.evaluate(() => localStorage.getItem('access_token'));
    const headers = { Authorization: `Bearer ${token}` };

    const thread = await (await authenticatedPage.request.post(`${API_BASE}/api/chat/threads`, { headers })).json();

    expect((await authenticatedPage.request.post(
      `${API_BASE}/api/chat/threads/${thread.id}/archive`, { headers })).status()).toBe(200);

    const archived = await (await authenticatedPage.request.get(
      `${API_BASE}/api/chat/threads?status=Archived`, { headers })).json();
    expect(archived.find((t: any) => t.id === thread.id)).toBeDefined();

    expect((await authenticatedPage.request.post(
      `${API_BASE}/api/chat/threads/${thread.id}/unarchive`, { headers })).status()).toBe(200);

    const active = await (await authenticatedPage.request.get(
      `${API_BASE}/api/chat/threads?status=Active`, { headers })).json();
    expect(active.find((t: any) => t.id === thread.id)).toBeDefined();
  });

  authTest('rename a global thread persists', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/dashboard');
    const token = await authenticatedPage.evaluate(() => localStorage.getItem('access_token'));
    const headers = { Authorization: `Bearer ${token}` };

    const thread = await (await authenticatedPage.request.post(`${API_BASE}/api/chat/threads`, { headers })).json();
    const rename = await authenticatedPage.request.put(
      `${API_BASE}/api/chat/threads/${thread.id}`, { headers, data: { title: 'Weekly review questions' } });
    expect(rename.status()).toBe(200);

    const fresh = await (await authenticatedPage.request.get(`${API_BASE}/api/chat/threads/${thread.id}`, { headers })).json();
    expect(fresh.title).toBe('Weekly review questions');
  });

  authTest('user isolation — User A cannot read User B global thread', async ({ browser }) => {
    const ctxA = await browser.newContext();
    const pageA = await ctxA.newPage();
    const userA = await testLogin(pageA, 92);
    const headersA = { Authorization: `Bearer ${userA.accessToken}` };

    const ctxB = await browser.newContext();
    const pageB = await ctxB.newPage();
    const userB = await testLogin(pageB, 93);
    const headersB = { Authorization: `Bearer ${userB.accessToken}` };

    await pageA.goto('/dashboard');
    await pageB.goto('/dashboard');

    const threadA = await (await pageA.request.post(`${API_BASE}/api/chat/threads`, { headers: headersA })).json();

    const getAsB = await pageB.request.get(`${API_BASE}/api/chat/threads/${threadA.id}`, { headers: headersB });
    expect(getAsB.status()).toBe(404);

    const postAsB = await pageB.request.post(
      `${API_BASE}/api/chat/threads/${threadA.id}/messages`,
      { headers: headersB, data: { content: 'sneaky' } });
    expect(postAsB.status()).toBe(404);

    await ctxA.close();
    await ctxB.close();
  });

  authTest('posting to an archived global thread returns 409', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/dashboard');
    const token = await authenticatedPage.evaluate(() => localStorage.getItem('access_token'));
    const headers = { Authorization: `Bearer ${token}` };

    const thread = await (await authenticatedPage.request.post(`${API_BASE}/api/chat/threads`, { headers })).json();
    await authenticatedPage.request.post(`${API_BASE}/api/chat/threads/${thread.id}/archive`, { headers });

    const post = await authenticatedPage.request.post(
      `${API_BASE}/api/chat/threads/${thread.id}/messages`, { headers, data: { content: 'hi' } });
    expect(post.status()).toBe(409);
  });

  authTest('global chat launcher is visible to authenticated users and opens the slide-over', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/dashboard');

    const launcher = authenticatedPage.locator('app-global-chat-launcher button');
    await expect(launcher).toBeVisible();
    await launcher.click();

    // PrimeNG drawer renders into the body root.
    await expect(authenticatedPage.locator('.p-drawer')).toBeVisible();
  });
});
