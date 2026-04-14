import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { OneOnOnesService } from './one-on-ones.service';
import { OneOnOne } from '../models/one-on-one.model';

describe('OneOnOnesService', () => {
  let service: OneOnOnesService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(OneOnOnesService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('lists one-on-ones without filter', () => {
    service.list().subscribe();
    const req = httpMock.expectOne('/api/one-on-ones');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('lists with personId filter', () => {
    service.list('person-1').subscribe();
    const req = httpMock.expectOne('/api/one-on-ones?personId=person-1');
    req.flush([]);
  });

  it('creates one-on-one via POST', () => {
    const body: OneOnOne = {
      id: '1',
      userId: 'u',
      personId: 'p',
      occurredAt: '2026-04-10',
      notes: null,
      moodRating: null,
      topics: [],
      actionItems: [],
      followUps: [],
      createdAt: '',
      updatedAt: '',
    };
    service.create({ personId: 'p', occurredAt: '2026-04-10' }).subscribe((o) => {
      expect(o.id).toBe('1');
    });
    const req = httpMock.expectOne('/api/one-on-ones');
    expect(req.request.method).toBe('POST');
    req.flush(body);
  });

  it('completes an action item', () => {
    service.completeActionItem('1', '2').subscribe();
    const req = httpMock.expectOne('/api/one-on-ones/1/action-items/2/complete');
    expect(req.request.method).toBe('POST');
    req.flush({});
  });
});
