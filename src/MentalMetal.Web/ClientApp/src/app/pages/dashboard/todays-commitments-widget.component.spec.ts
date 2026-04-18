import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { TodaysCommitmentsWidgetComponent } from './todays-commitments-widget.component';
import { Commitment } from '../../shared/models/commitment.model';
import { todayLocalIso } from './widget-shell';

describe('TodaysCommitmentsWidgetComponent', () => {
  let fixture: ComponentFixture<TodaysCommitmentsWidgetComponent>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TodaysCommitmentsWidgetComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: '**', children: [] }]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TodaysCommitmentsWidgetComponent);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  // Local calendar day — matches the widget's filtering so the tests
  // don't flake around local midnight in non-UTC timezones.
  function today(): string {
    return todayLocalIso();
  }

  function make(overrides: Partial<Commitment> = {}): Commitment {
    return {
      id: 'c1',
      userId: 'u1',
      description: 'Ship the spec',
      direction: 'MineToThem',
      personId: 'p1',
      initiativeId: null,
      sourceCaptureId: null,
      confidence: 'High',
      dueDate: today(),
      status: 'Open',
      completedAt: null,
      dismissedAt: null,
      notes: null,
      isOverdue: false,
      createdAt: '2026-04-01T00:00:00Z',
      updatedAt: '2026-04-01T00:00:00Z',
      ...overrides,
    };
  }

  it('renders empty state when no commitments match', () => {
    fixture.detectChanges();
    http.expectOne((r) => r.url.startsWith('/api/commitments')).flush([]);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Nothing due today');
  });

  it('shows overdue commitments before today-due ones, capped at 5', () => {
    fixture.detectChanges();
    const overdue = make({ id: 'a', description: 'Overdue A', isOverdue: true, dueDate: '2026-01-01' });
    const dueToday = [1, 2, 3, 4, 5, 6].map((n) =>
      make({ id: 'd' + n, description: `Due ${n}`, isOverdue: false, dueDate: today() }),
    );
    http
      .expectOne((r) => r.url.startsWith('/api/commitments'))
      .flush([...dueToday, overdue]);
    fixture.detectChanges();

    const listItems = fixture.nativeElement.querySelectorAll('li');
    expect(listItems.length).toBe(5);
    // Overdue always first
    expect((listItems[0] as HTMLElement).textContent).toContain('Overdue A');
  });

  it('renders error state on failure and Retry re-fetches', () => {
    fixture.detectChanges();
    http.expectOne((r) => r.url.startsWith('/api/commitments'))
      .flush('boom', { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain("Couldn't load commitments");

    // Click the Retry button (PrimeNG renders a native <button> inside p-button).
    const retry = Array.from(fixture.nativeElement.querySelectorAll('button'))
      .find((b) => ((b as HTMLElement).textContent ?? '').trim().toLowerCase() === 'retry') as HTMLButtonElement | undefined;
    expect(retry, 'retry button').toBeDefined();
    retry!.click();
    fixture.detectChanges();

    // A second request was issued by Retry.
    http.expectOne((r) => r.url.startsWith('/api/commitments')).flush([]);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Nothing due today');
  });

  it('Mark complete posts and re-fetches only this widget', () => {
    fixture.detectChanges();
    const row = make({ id: 'c-today', description: 'Finish X', dueDate: today() });
    http.expectOne((r) => r.url.startsWith('/api/commitments')).flush([row]);
    fixture.detectChanges();

    const btn = Array.from(fixture.nativeElement.querySelectorAll('button'))
      .find((b) => (b as HTMLElement).getAttribute('aria-label') === 'Mark complete') as HTMLButtonElement | undefined;
    expect(btn, 'mark-complete button').toBeDefined();
    btn!.click();

    http.expectOne((r) => r.url === '/api/commitments/c-today/complete' && r.method === 'POST').flush({});
    // After complete, the widget re-fetches the list.
    http.expectOne((r) => r.url.startsWith('/api/commitments')).flush([]);
  });
});
