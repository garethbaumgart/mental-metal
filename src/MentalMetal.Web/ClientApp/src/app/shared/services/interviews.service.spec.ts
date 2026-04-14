import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { InterviewsService } from './interviews.service';

describe('InterviewsService', () => {
  let service: InterviewsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(InterviewsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('lists interviews with no filters', () => {
    service.list().subscribe();
    const req = httpMock.expectOne('/api/interviews');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('filters by stage', () => {
    service.list(undefined, 'ScreenScheduled').subscribe();
    const req = httpMock.expectOne('/api/interviews?stage=ScreenScheduled');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('creates an interview', () => {
    service
      .create({ candidatePersonId: 'p', roleTitle: 'SWE' })
      .subscribe();
    const req = httpMock.expectOne('/api/interviews');
    expect(req.request.method).toBe('POST');
    expect(req.request.body.roleTitle).toBe('SWE');
    req.flush({});
  });

  it('advances a stage', () => {
    service.advance('id1', { targetStage: 'ScreenCompleted' }).subscribe();
    const req = httpMock.expectOne('/api/interviews/id1/advance');
    expect(req.request.method).toBe('POST');
    expect(req.request.body.targetStage).toBe('ScreenCompleted');
    req.flush({});
  });

  it('calls analyze', () => {
    service.analyze('id1').subscribe();
    const req = httpMock.expectOne('/api/interviews/id1/analyze');
    expect(req.request.method).toBe('POST');
    req.flush({});
  });
});
