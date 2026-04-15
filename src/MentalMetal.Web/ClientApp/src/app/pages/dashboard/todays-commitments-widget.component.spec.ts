import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { TodaysCommitmentsWidgetComponent } from './todays-commitments-widget.component';
import { Commitment } from '../../shared/models/commitment.model';

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

  function today(): string {
    return new Date().toISOString().slice(0, 10);
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
      dueDate: today(),
      status: 'Open',
      completedAt: null,
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

    expect(fixture.nativeElement.textContent).toContain('clear inbox');
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

  it('renders error state on failure and allows retry', () => {
    fixture.detectChanges();
    http.expectOne((r) => r.url.startsWith('/api/commitments'))
      .flush('boom', { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain("Couldn't load commitments");
  });
});
