import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { DailyCloseOutService } from './daily-close-out.service';

describe('DailyCloseOutService', () => {
  let service: DailyCloseOutService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(DailyCloseOutService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('GETs the queue', () => {
    service.getQueue().subscribe();
    const req = httpMock.expectOne('/api/daily-close-out/queue');
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], counts: { total: 0, raw: 0, processing: 0, processed: 0, failed: 0 } });
  });

  it('POSTs quick-discard', () => {
    service.quickDiscard('c1').subscribe();
    const req = httpMock.expectOne('/api/daily-close-out/captures/c1/quick-discard');
    expect(req.request.method).toBe('POST');
    req.flush(null);
  });

  it('POSTs reassign with body', () => {
    service.reassign('c1', { personIds: ['p1'], initiativeIds: [] }).subscribe();
    const req = httpMock.expectOne('/api/daily-close-out/captures/c1/reassign');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ personIds: ['p1'], initiativeIds: [] });
    req.flush({});
  });

  it('POSTs close with optional date', () => {
    service.closeOutDay({ date: '2026-04-14' }).subscribe();
    const req = httpMock.expectOne('/api/daily-close-out/close');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ date: '2026-04-14' });
    req.flush({});
  });

  it('GETs log with limit', () => {
    service.getLog(5).subscribe();
    const req = httpMock.expectOne('/api/daily-close-out/log?limit=5');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });
});
