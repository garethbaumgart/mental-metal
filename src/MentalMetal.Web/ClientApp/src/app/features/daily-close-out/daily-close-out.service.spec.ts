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

  describe('processAllRaw', () => {
    it('returns 0/0/0 for an empty id list (no HTTP calls)', async () => {
      const result = await service.processAllRaw([]);
      expect(result).toEqual({ attempted: 0, succeeded: 0, failed: 0, providerNotConfigured: false });
    });

    it('reports all-success when every POST returns OK', async () => {
      const ids = ['a', 'b', 'c'];
      const promise = service.processAllRaw(ids, 3);
      for (const id of ids) {
        const req = httpMock.expectOne(`/api/captures/${id}/process`);
        expect(req.request.method).toBe('POST');
        req.flush({});
      }
      const result = await promise;
      expect(result).toEqual({ attempted: 3, succeeded: 3, failed: 0, providerNotConfigured: false });
    });

    /**
     * Between a worker's flush() and its next HTTP dispatch, the rejection/
     * resolution has to settle through several microtask hops. Await a
     * few turns to let the queue drain before the next expectOne.
     */
    async function tick(turns = 3): Promise<void> {
      for (let i = 0; i < turns; i++) await Promise.resolve();
    }

    it('continues through failures and reports the failed count (serial)', async () => {
      const ids = ['a', 'b', 'c', 'd'];
      const promise = service.processAllRaw(ids, 1);

      await tick();
      httpMock.expectOne('/api/captures/a/process').flush('boom', { status: 500, statusText: 'X' });
      await tick();
      httpMock.expectOne('/api/captures/b/process').flush({});
      await tick();
      httpMock.expectOne('/api/captures/c/process').flush({});
      await tick();
      httpMock.expectOne('/api/captures/d/process').flush('boom', { status: 500, statusText: 'X' });

      const result = await promise;
      expect(result.attempted).toBe(4);
      expect(result.succeeded).toBe(2);
      expect(result.failed).toBe(2);
      expect(result.providerNotConfigured).toBe(false);
    });

    it('clamps non-positive parallelism up to 1 so all captures still run', async () => {
      const ids = ['a', 'b'];
      const promise = service.processAllRaw(ids, 0);

      await tick();
      httpMock.expectOne('/api/captures/a/process').flush({});
      await tick();
      httpMock.expectOne('/api/captures/b/process').flush({});

      const result = await promise;
      expect(result.attempted).toBe(2);
      expect(result.succeeded).toBe(2);
    });

    it('onItemDone fires after each success/failure for per-card refresh', async () => {
      const ids = ['a', 'b'];
      const seen: string[] = [];
      const promise = service.processAllRaw(ids, 1, (id) => seen.push(id));

      await tick();
      httpMock.expectOne('/api/captures/a/process').flush({});
      await tick();
      httpMock.expectOne('/api/captures/b/process').flush('boom', { status: 500, statusText: 'X' });

      await promise;
      expect(seen).toEqual(['a', 'b']);
    });

    it('with parallelism > 1, stops dispatching NEW work after provider-not-configured (in-flight may still complete)', async () => {
      const ids = ['a', 'b', 'c', 'd'];
      const promise = service.processAllRaw(ids, 2);

      await tick();
      // Both workers have dispatched a and b.
      httpMock.expectOne('/api/captures/a/process').flush(
        { code: 'ai_provider_not_configured', error: 'not configured' },
        { status: 409, statusText: 'Conflict' },
      );
      await tick();
      // b is already in flight; complete it so Promise.all resolves.
      httpMock.expectOne('/api/captures/b/process').flush({});

      const result = await promise;
      expect(result.providerNotConfigured).toBe(true);
      // c and d were never dispatched.
      httpMock.expectNone('/api/captures/c/process');
      httpMock.expectNone('/api/captures/d/process');
      // attempted is 2 (a and b), not 4.
      expect(result.attempted).toBe(2);
    });

    it('short-circuits on ai_provider_not_configured', async () => {
      const ids = ['a', 'b', 'c'];
      const promise = service.processAllRaw(ids, 1);

      await tick();
      httpMock.expectOne('/api/captures/a/process').flush(
        { code: 'ai_provider_not_configured', error: 'not configured' },
        { status: 409, statusText: 'Conflict' },
      );

      const result = await promise;
      expect(result.providerNotConfigured).toBe(true);
      expect(result.attempted).toBe(1);
      expect(result.succeeded).toBe(0);
      expect(result.failed).toBe(0);
      // No follow-on requests were dispatched.
      httpMock.expectNone('/api/captures/b/process');
      httpMock.expectNone('/api/captures/c/process');
    });
  });
});
