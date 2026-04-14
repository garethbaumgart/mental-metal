import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { MyQueuePageComponent } from './my-queue.page';
import { MyQueueResponse, QueueItem } from './my-queue.models';

describe('MyQueuePageComponent', () => {
  let fixture: ComponentFixture<MyQueuePageComponent>;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MyQueuePageComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: '**', children: [] }]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(MyQueuePageComponent);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  afterEach(() => httpMock.verify());

  const emptyResponse: MyQueueResponse = {
    items: [],
    counts: { overdue: 0, dueSoon: 0, staleCaptures: 0, staleDelegations: 0, total: 0 },
    filters: { scope: 'All', itemType: [], personId: null, initiativeId: null },
  };

  function buildItem(overrides: Partial<QueueItem> = {}): QueueItem {
    return {
      itemType: 'Commitment',
      id: 'c1',
      title: 'ship spec',
      status: 'Open',
      dueDate: '2026-04-13',
      isOverdue: true,
      personId: 'p1',
      personName: 'Sarah',
      initiativeId: null,
      initiativeName: null,
      daysSinceCaptured: null,
      lastFollowedUpAt: null,
      priorityScore: 150,
      suggestDelegate: false,
      ...overrides,
    };
  }

  function flushInitial(body: MyQueueResponse): void {
    fixture.detectChanges();
    httpMock.expectOne((r) => r.url === '/api/my-queue').flush(body);
    fixture.detectChanges();
  }

  it('shows the empty state when there are no items', () => {
    flushInitial(emptyResponse);
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('All clear');
  });

  it('renders queue items', () => {
    flushInitial({
      ...emptyResponse,
      items: [buildItem()],
      counts: { overdue: 1, dueSoon: 0, staleCaptures: 0, staleDelegations: 0, total: 1 },
    });

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('ship spec');
    expect(text).toContain('Sarah');
    expect(text).toContain('Total 1');
  });

  it('reloads when the scope signal changes', () => {
    flushInitial(emptyResponse);

    // Programmatically change scope via the component instance and call reload via the
    // public path the template uses. We exercise the page-level state explicitly
    // because template-driven SelectButton clicks are awkward to simulate reliably.
    const component = fixture.componentInstance as unknown as {
      scope: { set: (v: 'Overdue') => void };
      reload: () => void;
    };
    component.scope.set('Overdue');
    component.reload();

    const req = httpMock.expectOne((r) => r.url === '/api/my-queue');
    expect(req.request.params.get('scope')).toBe('overdue');
    req.flush(emptyResponse);
  });

  it('navigates to the delegation create form with prefill query params on delegate', () => {
    flushInitial({
      ...emptyResponse,
      items: [buildItem({ suggestDelegate: true })],
      counts: { overdue: 1, dueSoon: 0, staleCaptures: 0, staleDelegations: 0, total: 1 },
    });

    // Trigger via the public callback to avoid coupling to PrimeNG's button DOM.
    const component = fixture.componentInstance as unknown as {
      onDelegate: (item: QueueItem) => void;
    };
    component.onDelegate(buildItem({ suggestDelegate: true }));

    expect(router.navigate).toHaveBeenCalledWith(
      ['/delegations'],
      expect.objectContaining({
        queryParams: expect.objectContaining({
          description: 'ship spec',
          sourceCommitmentId: 'c1',
          personId: 'p1',
        }),
      }),
    );
  });

  it('shows the loading indicator while a request is in flight', () => {
    fixture.detectChanges();
    const inFlight = httpMock.expectOne((r) => r.url === '/api/my-queue');
    fixture.detectChanges();
    // Spinner icon is rendered via @if (service.loading()).
    const html = fixture.nativeElement.innerHTML as string;
    expect(html).toContain('pi-spinner');
    inFlight.flush(emptyResponse);
  });
});
