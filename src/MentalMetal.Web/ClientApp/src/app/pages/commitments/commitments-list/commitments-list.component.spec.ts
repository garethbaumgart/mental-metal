import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { CommitmentsListComponent } from './commitments-list.component';
import { Commitment } from '../../../shared/models/commitment.model';
import { Person } from '../../../shared/models/person.model';

describe('CommitmentsListComponent', () => {
  let fixture: ComponentFixture<CommitmentsListComponent>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CommitmentsListComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: '**', children: [] }]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CommitmentsListComponent);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  function makePerson(overrides: Partial<Person> = {}): Person {
    return {
      id: 'p1',
      userId: 'u1',
      name: 'Alice Smith',
      type: 'Peer',
      isArchived: false,
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: '2026-01-01T00:00:00Z',
      ...overrides,
    } as Person;
  }

  function makeCommitment(overrides: Partial<Commitment> = {}): Commitment {
    return {
      id: 'c1',
      userId: 'u1',
      description: 'Ship the spec',
      direction: 'MineToThem',
      personId: 'p1',
      initiativeId: null,
      sourceCaptureId: null,
      sourceStartOffset: null,
      sourceEndOffset: null,
      confidence: 'High',
      dueDate: '2026-05-01',
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

  /** Flush the initial people + commitments requests that fire on ngOnInit */
  function flushInit(commitments: Commitment[], people: Person[] = [makePerson()]): void {
    fixture.detectChanges();
    http.expectOne((r) => r.url === '/api/people').flush(people);
    http.expectOne((r) => r.url.startsWith('/api/commitments')).flush(commitments);
    fixture.detectChanges();
  }

  it('should not re-fetch the full list when completing a commitment', () => {
    const c1 = makeCommitment({ id: 'c1', description: 'Task A' });
    const c2 = makeCommitment({ id: 'c2', description: 'Task B' });
    flushInit([c1, c2]);

    // Click the Complete button on the first commitment
    const completeBtn = Array.from(fixture.nativeElement.querySelectorAll('button'))
      .find((b) => (b as HTMLElement).getAttribute('aria-label') === 'Complete') as HTMLButtonElement | undefined;
    expect(completeBtn, 'complete button').toBeDefined();
    completeBtn!.click();

    // The API call should be made, but NO subsequent list re-fetch
    const completedC1 = makeCommitment({
      id: 'c1',
      description: 'Task A',
      status: 'Completed',
      completedAt: '2026-04-21T00:00:00Z',
    });
    http.expectOne((r) => r.url === '/api/commitments/c1/complete' && r.method === 'POST')
      .flush(completedC1);
    fixture.detectChanges();

    // With default status filter "Open", completed item should be removed from the list
    // No additional /api/commitments list request should have been made
    http.expectNone((r) => r.url.startsWith('/api/commitments'));
  });

  it('should not re-fetch the full list when dismissing a commitment', () => {
    const c1 = makeCommitment({ id: 'c1', description: 'Task A' });
    flushInit([c1]);

    const dismissBtn = Array.from(fixture.nativeElement.querySelectorAll('button'))
      .find((b) => (b as HTMLElement).getAttribute('aria-label') === 'Dismiss') as HTMLButtonElement | undefined;
    expect(dismissBtn, 'dismiss button').toBeDefined();
    dismissBtn!.click();

    const dismissedC1 = makeCommitment({
      id: 'c1',
      description: 'Task A',
      status: 'Dismissed',
      dismissedAt: '2026-04-21T00:00:00Z',
    });
    http.expectOne((r) => r.url === '/api/commitments/c1/dismiss' && r.method === 'POST')
      .flush(dismissedC1);
    fixture.detectChanges();

    // No additional list re-fetch
    http.expectNone((r) => r.url.startsWith('/api/commitments'));
  });

  it('should remove completed item from list when status filter is Open', () => {
    const c1 = makeCommitment({ id: 'c1', description: 'Task A' });
    const c2 = makeCommitment({ id: 'c2', description: 'Task B' });
    flushInit([c1, c2]);

    // Default filter is "Open" — completing should remove the item
    const completeBtn = Array.from(fixture.nativeElement.querySelectorAll('button'))
      .find((b) => (b as HTMLElement).getAttribute('aria-label') === 'Complete') as HTMLButtonElement | undefined;
    completeBtn!.click();

    const completedC1 = makeCommitment({
      id: 'c1',
      description: 'Task A',
      status: 'Completed',
      completedAt: '2026-04-21T00:00:00Z',
    });
    http.expectOne((r) => r.url === '/api/commitments/c1/complete').flush(completedC1);
    fixture.detectChanges();

    // Only Task B should remain
    const text = fixture.nativeElement.textContent;
    expect(text).not.toContain('Task A');
    expect(text).toContain('Task B');
  });

  it('should not re-fetch the full list when reopening a commitment', () => {
    // Simulate viewing with "Completed" status filter
    const c1 = makeCommitment({ id: 'c1', description: 'Done task', status: 'Completed' });
    fixture.detectChanges();

    // Set the status filter to Completed before init loads (direct signal access)
    fixture.componentInstance.selectedStatus.set('Completed');

    http.expectOne((r) => r.url === '/api/people').flush([makePerson()]);
    // The init request with "Open" filter
    http.expectOne((r) => r.url.startsWith('/api/commitments')).flush([]);
    fixture.detectChanges();

    // Trigger filter change to load Completed — onFilterChange is protected,
    // so bracket notation is the only option without making it public.
    (fixture.componentInstance as unknown as { onFilterChange(): void }).onFilterChange();
    http.expectOne((r) => r.url.startsWith('/api/commitments')).flush([c1]);
    fixture.detectChanges();

    // Now click Reopen
    const reopenBtn = Array.from(fixture.nativeElement.querySelectorAll('button'))
      .find((b) => (b as HTMLElement).getAttribute('aria-label') === 'Reopen') as HTMLButtonElement | undefined;
    expect(reopenBtn, 'reopen button').toBeDefined();
    reopenBtn!.click();

    const reopenedC1 = makeCommitment({ id: 'c1', description: 'Done task', status: 'Open' });
    http.expectOne((r) => r.url === '/api/commitments/c1/reopen' && r.method === 'POST')
      .flush(reopenedC1);
    fixture.detectChanges();

    // Reopened item no longer matches "Completed" filter, so removed
    // No list re-fetch
    http.expectNone((r) => r.url.startsWith('/api/commitments'));
  });
});
