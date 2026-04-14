import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { NudgesService } from './nudges.service';
import { Nudge } from './nudges.models';

describe('NudgesService', () => {
  let service: NudgesService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(NudgesService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  const sample: Nudge = {
    id: '11111111-1111-1111-1111-111111111111',
    userId: '22222222-2222-2222-2222-222222222222',
    title: 'Review risk log',
    cadence: { type: 'Daily', customIntervalDays: null, dayOfWeek: null, dayOfMonth: null },
    startDate: '2026-04-14',
    nextDueDate: '2026-04-14',
    lastNudgedAt: null,
    personId: null,
    initiativeId: null,
    notes: null,
    isActive: true,
    createdAtUtc: '2026-04-14T10:00:00Z',
    updatedAtUtc: '2026-04-14T10:00:00Z',
  };

  it('list() GETs /api/nudges with no params by default', () => {
    service.list().subscribe();
    const req = httpMock.expectOne((r) => r.url === '/api/nudges');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys()).toEqual([]);
    req.flush([sample]);
  });

  it('list() serialises filters', () => {
    service.list({ isActive: true, dueWithinDays: 7, personId: 'pid' }).subscribe();
    const req = httpMock.expectOne((r) => r.url === '/api/nudges');
    expect(req.request.params.get('isActive')).toBe('true');
    expect(req.request.params.get('dueWithinDays')).toBe('7');
    expect(req.request.params.get('personId')).toBe('pid');
    req.flush([]);
  });

  it('create() POSTs to /api/nudges', () => {
    service.create({ title: 't', cadenceType: 'Daily' }).subscribe();
    const req = httpMock.expectOne('/api/nudges');
    expect(req.request.method).toBe('POST');
    expect(req.request.body.title).toBe('t');
    req.flush(sample);
  });

  it('update() PATCHes title/notes/links', () => {
    service.update(sample.id, { title: 't', notes: null, personId: null, initiativeId: null }).subscribe();
    const req = httpMock.expectOne(`/api/nudges/${sample.id}`);
    expect(req.request.method).toBe('PATCH');
    req.flush(sample);
  });

  it('updateCadence() PATCHes /cadence', () => {
    service.updateCadence(sample.id, { cadenceType: 'Weekly', dayOfWeek: 'Thursday' }).subscribe();
    const req = httpMock.expectOne(`/api/nudges/${sample.id}/cadence`);
    expect(req.request.method).toBe('PATCH');
    req.flush(sample);
  });

  it('markNudged() POSTs to /mark-nudged', () => {
    service.markNudged(sample.id).subscribe();
    const req = httpMock.expectOne(`/api/nudges/${sample.id}/mark-nudged`);
    expect(req.request.method).toBe('POST');
    req.flush(sample);
  });

  it('pause() and resume() POST to their endpoints', () => {
    service.pause(sample.id).subscribe();
    httpMock.expectOne(`/api/nudges/${sample.id}/pause`).flush(sample);

    service.resume(sample.id).subscribe();
    httpMock.expectOne(`/api/nudges/${sample.id}/resume`).flush(sample);
  });

  it('delete() DELETEs', () => {
    service.delete(sample.id).subscribe();
    const req = httpMock.expectOne(`/api/nudges/${sample.id}`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
