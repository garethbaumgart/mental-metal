import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { InterviewsPipelineComponent } from './interviews-pipeline.component';
import { Person } from '../../../shared/models/person.model';

describe('InterviewsPipelineComponent', () => {
  let fixture: ComponentFixture<InterviewsPipelineComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InterviewsPipelineComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: '**', children: [] }]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(InterviewsPipelineComponent);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  function person(overrides: Partial<Person> = {}): Person {
    return {
      id: 'p1',
      userId: 'u1',
      name: 'Alex Candidate',
      type: 'Candidate',
      email: null,
      role: null,
      team: null,
      notes: null,
      careerDetails: null,
      candidateDetails: null,
      isArchived: false,
      archivedAt: null,
      createdAt: '2026-04-14T00:00:00Z',
      updatedAt: '2026-04-14T00:00:00Z',
      ...overrides,
    };
  }

  // Regression for issue #120: the component previously fetched the full people
  // list with no type filter, so the New Interview dropdown either mixed in
  // non-candidates or — in user reports — appeared empty depending on seed data.
  // It must now request people scoped to `type=Candidate`.
  it('requests people filtered to Candidate on init', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    const peopleReq = httpMock.expectOne(
      (r) => r.url === '/api/people' && r.method === 'GET',
    );
    expect(peopleReq.request.params.get('type')).toBe('Candidate');
    peopleReq.flush([person()]);

    const interviewsReq = httpMock.expectOne(
      (r) => r.url === '/api/interviews' && r.method === 'GET',
    );
    interviewsReq.flush([]);

    fixture.detectChanges();
    await fixture.whenStable();

    const options = (fixture.componentInstance as unknown as {
      peopleOptions(): Array<{ label: string; value: string }>;
    }).peopleOptions();
    expect(options).toEqual([{ label: 'Alex Candidate', value: 'p1' }]);
  });
});
