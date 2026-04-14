import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { GoalsService } from './goals.service';

describe('GoalsService', () => {
  let service: GoalsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(GoalsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('filters by status', () => {
    service.list(undefined, undefined, 'Active').subscribe();
    const req = httpMock.expectOne('/api/goals?status=Active');
    req.flush([]);
  });

  it('achieve hits POST endpoint', () => {
    service.achieve('g1').subscribe();
    const req = httpMock.expectOne('/api/goals/g1/achieve');
    expect(req.request.method).toBe('POST');
    req.flush({});
  });

  it('records check-in with progress', () => {
    service.recordCheckIn('g1', { note: 'hello', progress: 50 }).subscribe();
    const req = httpMock.expectOne('/api/goals/g1/check-ins');
    expect(req.request.body.progress).toBe(50);
    req.flush({});
  });

  it('fetches evidence summary for person', () => {
    service.getPersonEvidenceSummary('p1').subscribe();
    const req = httpMock.expectOne('/api/people/p1/evidence-summary');
    req.flush({});
  });
});
