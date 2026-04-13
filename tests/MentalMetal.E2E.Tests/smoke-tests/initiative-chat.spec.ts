import { test as authTest, API_BASE, testLogin } from './fixtures/auth.fixture';
import { expect } from '@playwright/test';

authTest.describe('Initiative AI chat', () => {
  authTest('start a thread, post a message, receive a reply with both messages persisted', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/dashboard');
    const token = await authenticatedPage.evaluate(() => localStorage.getItem('access_token'));
    const headers = { Authorization: `Bearer ${token}` };

    const initiative = await (await authenticatedPage.request.post(`${API_BASE}/api/initiatives`, {
      headers, data: { title: 'Chat Test Initiative' },
    })).json();

    const start = await authenticatedPage.request.post(
      `${API_BASE}/api/initiatives/${initiative.id}/chat/threads`, { headers });
    expect(start.status()).toBe(201);
    const thread = await start.json();
    expect(thread.status).toBe('Active');
    expect(thread.messages).toEqual([]);

    const post = await authenticatedPage.request.post(
      `${API_BASE}/api/initiatives/${initiative.id}/chat/threads/${thread.id}/messages`,
      { headers, data: { content: 'What are we working on?' } });
    expect(post.status()).toBe(200);
    const reply = await post.json();
    expect(reply.userMessage.role).toBe('User');
    expect(reply.userMessage.content).toBe('What are we working on?');
    // Reply is either an Assistant (friendly fallback when no key) or System (daily-limit) message.
    expect(['Assistant', 'System']).toContain(reply.assistantMessage.role);

    // Full thread fetch should include both messages in ordinal order.
    const fresh = await (await authenticatedPage.request.get(
      `${API_BASE}/api/initiatives/${initiative.id}/chat/threads/${thread.id}`, { headers })).json();
    expect(fresh.messages.length).toBe(2);
    expect(fresh.messages[0].messageOrdinal).toBe(1);
    expect(fresh.messages[1].messageOrdinal).toBe(2);
    expect(fresh.title).toBe('What are we working on?');
  });

  authTest('archive a thread and it disappears from active listing', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/dashboard');
    const token = await authenticatedPage.evaluate(() => localStorage.getItem('access_token'));
    const headers = { Authorization: `Bearer ${token}` };

    const initiative = await (await authenticatedPage.request.post(`${API_BASE}/api/initiatives`, {
      headers, data: { title: 'Archive Chat Initiative' },
    })).json();

    const thread = await (await authenticatedPage.request.post(
      `${API_BASE}/api/initiatives/${initiative.id}/chat/threads`, { headers })).json();

    const archiveResp = await authenticatedPage.request.post(
      `${API_BASE}/api/initiatives/${initiative.id}/chat/threads/${thread.id}/archive`, { headers });
    expect(archiveResp.status()).toBe(200);

    const active = await (await authenticatedPage.request.get(
      `${API_BASE}/api/initiatives/${initiative.id}/chat/threads?status=Active`, { headers })).json();
    expect(active.find((t: any) => t.id === thread.id)).toBeUndefined();

    const archived = await (await authenticatedPage.request.get(
      `${API_BASE}/api/initiatives/${initiative.id}/chat/threads?status=Archived`, { headers })).json();
    expect(archived.find((t: any) => t.id === thread.id)).toBeDefined();
  });

  authTest('rename a thread persists across reloads', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/dashboard');
    const token = await authenticatedPage.evaluate(() => localStorage.getItem('access_token'));
    const headers = { Authorization: `Bearer ${token}` };

    const initiative = await (await authenticatedPage.request.post(`${API_BASE}/api/initiatives`, {
      headers, data: { title: 'Rename Chat Initiative' },
    })).json();

    const thread = await (await authenticatedPage.request.post(
      `${API_BASE}/api/initiatives/${initiative.id}/chat/threads`, { headers })).json();

    const rename = await authenticatedPage.request.put(
      `${API_BASE}/api/initiatives/${initiative.id}/chat/threads/${thread.id}`,
      { headers, data: { title: 'Q3 Planning' } });
    expect(rename.status()).toBe(200);

    const fresh = await (await authenticatedPage.request.get(
      `${API_BASE}/api/initiatives/${initiative.id}/chat/threads/${thread.id}`, { headers })).json();
    expect(fresh.title).toBe('Q3 Planning');
  });

  authTest('user isolation — User A cannot read User B thread', async ({ browser }) => {
    const ctxA = await browser.newContext();
    const pageA = await ctxA.newPage();
    const userA = await testLogin(pageA, 90);
    const headersA = { Authorization: `Bearer ${userA.accessToken}` };

    const ctxB = await browser.newContext();
    const pageB = await ctxB.newPage();
    const userB = await testLogin(pageB, 91);
    const headersB = { Authorization: `Bearer ${userB.accessToken}` };

    await pageA.goto('/dashboard');
    await pageB.goto('/dashboard');

    const initiativeA = await (await pageA.request.post(`${API_BASE}/api/initiatives`, {
      headers: headersA, data: { title: 'User A Initiative' },
    })).json();

    const threadA = await (await pageA.request.post(
      `${API_BASE}/api/initiatives/${initiativeA.id}/chat/threads`, { headers: headersA })).json();

    // User B should receive 404 for both the initiative and the thread.
    const getAsB = await pageB.request.get(
      `${API_BASE}/api/initiatives/${initiativeA.id}/chat/threads/${threadA.id}`, { headers: headersB });
    expect(getAsB.status()).toBe(404);

    await ctxA.close();
    await ctxB.close();
  });

  authTest('posting to an archived thread returns 409', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/dashboard');
    const token = await authenticatedPage.evaluate(() => localStorage.getItem('access_token'));
    const headers = { Authorization: `Bearer ${token}` };

    const initiative = await (await authenticatedPage.request.post(`${API_BASE}/api/initiatives`, {
      headers, data: { title: 'Archived Post Initiative' },
    })).json();

    const thread = await (await authenticatedPage.request.post(
      `${API_BASE}/api/initiatives/${initiative.id}/chat/threads`, { headers })).json();

    await authenticatedPage.request.post(
      `${API_BASE}/api/initiatives/${initiative.id}/chat/threads/${thread.id}/archive`, { headers });

    const post = await authenticatedPage.request.post(
      `${API_BASE}/api/initiatives/${initiative.id}/chat/threads/${thread.id}/messages`,
      { headers, data: { content: 'hi' } });
    expect(post.status()).toBe(409);
  });
});
