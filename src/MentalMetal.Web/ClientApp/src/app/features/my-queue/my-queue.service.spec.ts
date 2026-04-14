import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { MyQueueService } from './my-queue.service';
import { MyQueueResponse } from './my-queue.models';

describe('MyQueueService', () => {
  let service: MyQueueService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(MyQueueService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  const emptyResponse: MyQueueResponse = {
    items: [],
    counts: { overdue: 0, dueSoon: 0, staleCaptures: 0, staleDelegations: 0, total: 0 },
    filters: { scope: 'All', itemType: [], personId: null, initiativeId: null },
  };

  it('GETs the queue with no params by default', () => {
    service.load();
    const req = httpMock.expectOne('/api/my-queue');
    expect(req.request.method).toBe('GET');
    req.flush(emptyResponse);
    expect(service.response()).toEqual(emptyResponse);
    expect(service.loading()).toBe(false);
  });

  it('serialises scope and repeated itemType params', () => {
    service.load({ scope: 'Overdue', itemType: ['Commitment', 'Delegation'] });
    const req = httpMock.expectOne((r) => r.url === '/api/my-queue');
    expect(req.request.params.get('scope')).toBe('overdue');
    expect(req.request.params.getAll('itemType')).toEqual(['commitment', 'delegation']);
    req.flush(emptyResponse);
  });

  it('serialises personId and initiativeId', () => {
    service.load({ personId: 'p1', initiativeId: 'i1' });
    const req = httpMock.expectOne((r) => r.url === '/api/my-queue');
    expect(req.request.params.get('personId')).toBe('p1');
    expect(req.request.params.get('initiativeId')).toBe('i1');
    req.flush(emptyResponse);
  });

  it('records error when request fails', () => {
    service.load();
    const req = httpMock.expectOne('/api/my-queue');
    req.flush('boom', { status: 500, statusText: 'Server Error' });
    expect(service.error()).toBe('Failed to load queue.');
    expect(service.loading()).toBe(false);
  });
});
