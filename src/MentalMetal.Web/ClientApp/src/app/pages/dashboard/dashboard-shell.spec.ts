import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { DashboardPage } from './dashboard.page';

/**
 * Shell-level test: asserts the widget isolation contract. When the
 * briefing endpoint fails, sibling widgets must still render their
 * data; when a sibling fails, siblings must still render.
 */
describe('Dashboard shell (widget isolation)', () => {
  let fixture: ComponentFixture<DashboardPage>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: '**', children: [] }]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardPage);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  function flush(url: string, body: object | string | null, status = 200): void {
    const req = http.match((r) => r.url === url || r.url.startsWith(url + '?'));
    for (const r of req) {
      if (status >= 400) {
        r.flush(body as string, { status, statusText: 'Error' });
      } else {
        r.flush(body as object);
      }
    }
  }

  it('renders each widget section', () => {
    fixture.detectChanges();

    // Match and drain each endpoint the widgets fetch from.
    flush('/api/briefings/morning', { id: 'b', type: 'Morning', markdownBody: '# hi', generatedAtUtc: '2026-04-16T08:00:00Z', model: 'm', inputTokens: 0, outputTokens: 0, factsSummary: {}, scopeKey: 's' });
    flush('/api/commitments', []);
    flush('/api/one-on-ones', []);
    flush('/api/people', []);
    flush('/api/my-queue', { items: [], counts: { overdue: 0, dueSoon: 0, staleCaptures: 0, staleDelegations: 0, total: 0 }, filters: { scope: 'All', itemType: [], personId: null, initiativeId: null } });
    flush('/api/delegations', []);

    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain("Today's commitments");
    expect(text).toContain("Today's 1:1s");
    expect(text).toContain('Top of your queue');
    expect(text).toContain("What's slipping");
  });

  it('sibling widgets still render data when the briefing endpoint fails', () => {
    fixture.detectChanges();

    flush('/api/briefings/morning', { error: 'nope' }, 500);
    flush('/api/commitments', [
      { id: 'c1', userId: 'u', description: 'Important thing', direction: 'MineToThem', personId: 'p', initiativeId: null, sourceCaptureId: null, dueDate: new Date().toISOString().slice(0, 10), status: 'Open', completedAt: null, notes: null, isOverdue: false, createdAt: '2026-04-01T00:00:00Z', updatedAt: '2026-04-01T00:00:00Z' },
    ]);
    flush('/api/one-on-ones', []);
    flush('/api/people', []);
    flush('/api/my-queue', { items: [], counts: { overdue: 0, dueSoon: 0, staleCaptures: 0, staleDelegations: 0, total: 0 }, filters: { scope: 'All', itemType: [], personId: null, initiativeId: null } });
    flush('/api/delegations', []);

    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    // Briefing widget shows failure
    expect(text).toContain('Failed to generate briefing');
    // Sibling rendered its data
    expect(text).toContain('Important thing');
  });

  it('other widgets render even if commitments fails', () => {
    fixture.detectChanges();

    flush('/api/briefings/morning', { id: 'b', type: 'Morning', markdownBody: '# hi', generatedAtUtc: '2026-04-16T08:00:00Z', model: 'm', inputTokens: 0, outputTokens: 0, factsSummary: {}, scopeKey: 's' });
    flush('/api/commitments', 'boom', 500);
    flush('/api/one-on-ones', []);
    flush('/api/people', []);
    flush('/api/my-queue', { items: [], counts: { overdue: 0, dueSoon: 0, staleCaptures: 0, staleDelegations: 0, total: 0 }, filters: { scope: 'All', itemType: [], personId: null, initiativeId: null } });
    flush('/api/delegations', []);

    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    // Commitments widget shows its local error
    expect(text).toContain("Couldn't load commitments");
    // Other widgets still render headings and empty states
    expect(text).toContain("Today's 1:1s");
    expect(text).toContain('Top of your queue');
  });
});
