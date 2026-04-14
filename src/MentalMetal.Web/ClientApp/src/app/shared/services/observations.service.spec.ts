import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ObservationsService } from './observations.service';

describe('ObservationsService', () => {
  let service: ObservationsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ObservationsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('filters by tag', () => {
    service.list(undefined, 'Win').subscribe();
    const req = httpMock.expectOne('/api/observations?tag=Win');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('creates an observation', () => {
    service
      .create({ personId: 'p', description: 'd', tag: 'Growth' })
      .subscribe();
    const req = httpMock.expectOne('/api/observations');
    expect(req.request.method).toBe('POST');
    expect(req.request.body.tag).toBe('Growth');
    req.flush({});
  });

  it('deletes via DELETE', () => {
    service.delete('abc').subscribe();
    const req = httpMock.expectOne('/api/observations/abc');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
