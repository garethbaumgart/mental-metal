/**
 * UX Exploration: drives the app as a fresh user, then as a user with seed data.
 * Captures screenshots, click counts, and timings for each primary flow.
 * Output: ./ux-review-output/screenshots/
 */
import { Page } from '@playwright/test';
import { test as authTest, API_BASE } from './fixtures/auth.fixture';
import * as fs from 'fs';
import * as path from 'path';

const OUT = path.join(__dirname, '..', 'ux-review-output');
const SHOTS = path.join(OUT, 'screenshots');
fs.mkdirSync(SHOTS, { recursive: true });

const log = (info: Record<string, unknown>) => console.log(JSON.stringify(info));

async function shot(page: Page, name: string): Promise<string> {
  const file = path.join(SHOTS, `${name}.png`);
  await page.screenshot({ path: file, fullPage: true });
  return path.relative(OUT, file);
}

async function api(page: Page, method: string, url: string, body?: unknown) {
  const token = await page.evaluate(() => localStorage.getItem('access_token'));
  const res = await page.request.fetch(`${API_BASE}${url}`, {
    method,
    headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
    data: body ? JSON.stringify(body) : undefined,
  });
  return res;
}

// ---- 1: First-time user experience (empty state) ----
authTest('FRESH_USER: empty-state walkthrough of every primary surface', async ({ authenticatedPage: page }) => {
  const pages = [
    ['dashboard', '/dashboard'],
    ['captures', '/capture'],
    ['people', '/people'],
    ['commitments', '/commitments'],
    ['initiatives', '/initiatives'],
    ['settings', '/settings'],
    ['daily-brief', '/briefing/daily'],
    ['weekly-brief', '/briefing/weekly'],
  ] as const;

  for (const [name, url] of pages) {
    const t0 = Date.now();
    await page.goto(url, { waitUntil: 'networkidle' }).catch(() => {});
    const ms = Date.now() - t0;
    const bodyText = await page.locator('body').innerText().catch(() => '');
    const snippet = bodyText.slice(0, 400).replace(/\s+/g, ' ');
    const screenshot = await shot(page, `01-fresh-${name}`);
    log({ flow: 'fresh-user', step: name, ms, url, screenshot, note: snippet });
  }
});

// ---- 2: Time to first capture (the critical "near-zero friction" claim) ----
authTest('TIME_TO_CAPTURE: from cold start to text capture saved', async ({ authenticatedPage: page }) => {
  const tStart = Date.now();
  let clicks = 0;
  await page.goto('/dashboard');
  // Look for a quick-capture widget on dashboard
  const quickCaptureOnDash = await page.locator('textarea, input[type="text"]').count();
  log({ flow: 'capture', step: 'dashboard-has-quick-capture-input', note: `inputs visible on dashboard: ${quickCaptureOnDash}` });
  await shot(page, '02a-dashboard-search-for-quick-capture');

  // Navigate to capture
  const captureLink = page.getByRole('link', { name: /capture/i }).first();
  await captureLink.click(); clicks++;
  await page.waitForURL(/\/capture/);
  await shot(page, '02b-capture-list');

  // Try to create a capture - look for a "New" / "Create" button
  const newButton = page.getByRole('button', { name: /new|create|add|capture/i }).first();
  const newBtnVisible = await newButton.isVisible().catch(() => false);
  log({ flow: 'capture', step: 'new-capture-button-visible', note: `visible: ${newBtnVisible}` });
  if (newBtnVisible) {
    await newButton.click(); clicks++;
    await page.waitForTimeout(500);
    await shot(page, '02c-capture-form');
  }

  // Find a textarea to type into
  const textarea = page.locator('textarea').first();
  if (await textarea.isVisible().catch(() => false)) {
    await textarea.fill('Team agreed to ship the API spec by Friday. Risk flagged on timeline.');
    await shot(page, '02d-capture-typed');
    clicks++;
    // Find submit — use the Capture button specifically
    const submit = page.getByRole('button', { name: 'Capture' });
    if (await submit.isVisible().catch(() => false)) {
      await submit.click({ force: true }); clicks++;
      await page.waitForTimeout(1500);
      await shot(page, '02e-capture-saved');
    }
  }
  const totalMs = Date.now() - tStart;
  log({ flow: 'capture', step: 'TOTAL-time-to-first-capture', ms: totalMs, clicks });
});

// ---- 3: AI extraction round-trip (process -> confirm) ----
authTest('AI_EXTRACTION: create capture and trigger AI processing', async ({ authenticatedPage: page }) => {
  await page.goto('/capture');
  // Create directly via API to isolate timing
  const createRes = await api(page, 'POST', '/api/captures', {
    type: 'QuickNote',
    rawContent: 'Team committed to deliver the Q3 API spec by Friday. Risk raised on migration timeline. Decision: ship the v2 endpoint behind a flag.',
  });
  const captureBody = await createRes.json().catch(() => ({}));
  log({ flow: 'ai', step: 'create-capture-api', note: `status=${createRes.status()} id=${captureBody.id}` });

  await page.goto(`/capture/${captureBody.id}`, { waitUntil: 'networkidle' });
  await shot(page, '03a-capture-detail-fresh');

  // Look for "Process with AI" button
  const processBtn = page.getByRole('button', { name: /process|extract|analy/i }).first();
  const processVisible = await processBtn.isVisible().catch(() => false);
  log({ flow: 'ai', step: 'process-button-visible', note: `visible: ${processVisible}` });

  if (processVisible) {
    const t0 = Date.now();
    await processBtn.click();
    // Wait for either Processed or Failed state to appear
    await page.waitForFunction(
      () => document.body.innerText.match(/processed|failed|extraction|summary/i),
      { timeout: 60000 },
    ).catch(() => {});
    const ms = Date.now() - t0;
    log({ flow: 'ai', step: 'process-completion-time', ms });
    await shot(page, '03b-capture-after-process');
  }
});

// ---- 4: People - create + navigate to dossier view ----
authTest('PEOPLE: create person, navigate to dossier detail view', async ({ authenticatedPage: page }) => {
  await page.goto('/people');
  await shot(page, '04a-people-list');

  // Create via API
  const t0 = Date.now();
  const res = await api(page, 'POST', '/api/people', {
    name: 'Test Person', personType: 'Report', role: 'Senior Engineer', relationship: 'Direct report',
  });
  const body = await res.json().catch(() => ({}));
  log({ flow: 'people', step: 'create-person-api', ms: Date.now() - t0, note: `status=${res.status()} id=${body.id}` });

  if (body.id) {
    const t1 = Date.now();
    await page.goto(`/people/${body.id}`, { waitUntil: 'networkidle' });
    log({ flow: 'people', step: 'person-detail-load', ms: Date.now() - t1 });
    await shot(page, '04b-person-detail-dossier');
    // Verify the dossier page loaded with relevant content
    const sections = await page.locator('h2, h3, [role="heading"]').allInnerTexts();
    log({ flow: 'people', step: 'person-detail-sections', note: sections.join(' | ') });
  }
});

// ---- 5: Dashboard and daily brief section ----
authTest('BRIEFING: dashboard load and daily brief content audit', async ({ authenticatedPage: page }) => {
  const t0 = Date.now();
  await page.goto('/dashboard', { waitUntil: 'networkidle' });
  log({ flow: 'briefing', step: 'dashboard-load', ms: Date.now() - t0 });
  await shot(page, '05a-dashboard');
  const text = await page.locator('body').innerText();
  const hasBriefing = /briefing|today|good (morning|afternoon)|daily brief/i.test(text);
  const hasCommitments = /commitment/i.test(text);
  const hasOverdue = /overdue|due/i.test(text);
  log({ flow: 'briefing', step: 'dashboard-content-audit', note: `briefing-greeting:${hasBriefing} commitments:${hasCommitments} overdue:${hasOverdue}` });

  // Daily briefing
  const t1 = Date.now();
  await page.goto('/briefing/daily', { waitUntil: 'networkidle' });
  log({ flow: 'briefing', step: 'daily-briefing-load', ms: Date.now() - t1 });
  await shot(page, '05b-daily-briefing');

  // Weekly briefing
  const t2 = Date.now();
  await page.goto('/briefing/weekly', { waitUntil: 'networkidle' });
  log({ flow: 'briefing', step: 'weekly-briefing-load', ms: Date.now() - t2 });
  await shot(page, '05c-weekly-briefing');
});

// ---- 6: Sidebar navigation labels ----
authTest('NAV: sidebar item count and labels', async ({ authenticatedPage: page }) => {
  await page.goto('/dashboard');
  const links = await page.locator('nav a').allInnerTexts();
  log({ flow: 'nav', step: 'sidebar-items', note: `count=${links.length} labels=${links.join('|')}` });

  // Verify expected V2 labels are present
  const expectedLabels = ['Dashboard', 'Daily Brief', 'Weekly Brief', 'Captures', 'People', 'Commitments', 'Initiatives', 'Settings'];
  for (const label of expectedLabels) {
    const found = links.some(l => l.toLowerCase().includes(label.toLowerCase()));
    log({ flow: 'nav', step: `has-label-${label.toLowerCase().replace(/\s+/g, '-')}`, note: `found: ${found}` });
  }
});
